''HostBot - Warcraft 3 game hosting bot
''Copyright (C) 2008 Craig Gidney
''
''This program is free software: you can redistribute it and/or modify
''it under the terms of the GNU General Public License as published by
''the Free Software Foundation, either version 3 of the License, or
''(at your option) any later version.
''
''This program is distributed in the hope that it will be useful,
''but WITHOUT ANY WARRANTY; without even the implied warranty of
''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''GNU General Public License for more details.
''You should have received a copy of the GNU General Public License
''along with this program.  If not, see http://www.gnu.org/licenses/

Imports Tinker.Pickling

Namespace Bnet
    Public Enum ClientState As Integer
        Disconnected
        InitiatingConnection
        FinishedInitiatingConnection
        WaitingForProgramAuthenticationBegin
        EnterCDKeys
        WaitingForProgramAuthenticationFinish
        EnterUserCredentials
        WaitingForUserAuthenticationBegin
        WaitingForUserAuthenticationFinish
        WaitingForEnterChat
        Channel
        CreatingGame
        AdvertisingGame
    End Enum

    'verification disabled due to large amounts of impure methods
    <ContractVerification(False)>
    Public NotInheritable Class Client
        Inherits FutureDisposable

        Public Shared ReadOnly BnetServerPort As UShort = 6112
        Private Shared ReadOnly RefreshPeriod As TimeSpan = 20.Seconds

        Private ReadOnly outQueue As ICallQueue
        Private ReadOnly inQueue As ICallQueue

        Private ReadOnly _externalProvider As IExternalValues
        Private ReadOnly _clock As IClock
        Private ReadOnly _profile As Bot.ClientProfile
        Private ReadOnly _logger As Logger
        Private ReadOnly _packetHandler As Protocol.BnetPacketHandler
        Private _socket As PacketSocket
        Private WithEvents _wardenClient As Warden.Client

        'game
        Private _advertisedGameDescription As WC3.LocalGameDescription
        Private _advertisedPrivate As Boolean
        Private _advertiseTicker As IDisposable
        Private _futureAdvertisedGame As New FutureAction
        Private _reportedListenPort As UShort

        'connection
        Private _bnetRemoteHostName As String
        Private _userCredentials As ClientCredentials
        Private _clientCdKeySalt As UInt32
        Private _expectedServerPasswordProof As IReadableList(Of Byte)
        Private _allowRetryConnect As Boolean
        Private _futureConnected As New FutureAction
        Private _futureLoggedIn As New FutureAction

        Public Event StateChanged(ByVal sender As Client, ByVal oldState As ClientState, ByVal newState As ClientState)

        Private _lastChannel As InvariantString
        Private _state As ClientState

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_packetHandler IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_profile IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_futureLoggedIn IsNot Nothing)
            Contract.Invariant(_futureConnected IsNot Nothing)
            Contract.Invariant(_futureAdvertisedGame IsNot Nothing)
            Contract.Invariant(_externalProvider IsNot Nothing)
            Contract.Invariant((_socket IsNot Nothing) = (_state > ClientState.InitiatingConnection))
            Contract.Invariant((_wardenClient IsNot Nothing) = (_state > ClientState.WaitingForProgramAuthenticationBegin))
            Contract.Invariant((_state <= ClientState.EnterUserCredentials) OrElse (_userCredentials IsNot Nothing))
            Contract.Invariant((_advertisedGameDescription IsNot Nothing) = (_state >= ClientState.CreatingGame))
        End Sub

        Public Sub New(ByVal profile As Bot.ClientProfile,
                       ByVal externalProvider As IExternalValues,
                       ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(profile IsNot Nothing)
            Contract.Assume(externalProvider IsNot Nothing)
            Contract.Assume(clock IsNot Nothing)
            Me._futureConnected.SetHandled()
            Me._futureLoggedIn.SetHandled()
            Me._futureAdvertisedGame.SetHandled()

            'Pass values
            Me._clock = clock
            Me._profile = profile
            Me._externalProvider = externalProvider
            Me._logger = If(logger, New Logger)
            Me.outQueue = New TaskedCallQueue
            Me.inQueue = New TaskedCallQueue

            'Start packet machinery
            Me._packetHandler = New Protocol.BnetPacketHandler("BNET", Me._logger)

            AddQueuedPacketHandler(Protocol.ServerPackets.ProgramAuthenticationBegin, AddressOf ReceiveProgramAuthenticationBegin)
            AddQueuedPacketHandler(Protocol.ServerPackets.ProgramAuthenticationFinish, AddressOf ReceiveProgramAuthenticationFinish)
            AddQueuedPacketHandler(Protocol.ServerPackets.UserAuthenticationBegin, AddressOf ReceiveUserAuthenticationBegin)
            AddQueuedPacketHandler(Protocol.ServerPackets.UserAuthenticationFinish, AddressOf ReceiveUserAuthenticationFinish)
            AddQueuedPacketHandler(Protocol.ServerPackets.ChatEvent, AddressOf ReceiveChatEvent)
            AddQueuedPacketHandler(Protocol.ServerPackets.EnterChat, AddressOf ReceiveEnterChat)
            AddQueuedPacketHandler(Protocol.ServerPackets.MessageBox, AddressOf ReceiveMessageBox)
            AddQueuedPacketHandler(Protocol.ServerPackets.CreateGame3, AddressOf ReceiveCreateGame3)
            AddQueuedPacketHandler(Protocol.ServerPackets.Warden, AddressOf ReceiveWarden)
            AddQueuedPacketHandler(Protocol.PacketId.Ping, Protocol.ServerPackets.Ping, AddressOf ReceivePing)
            AddQueuedPacketHandler(Protocol.PacketId.Null, Protocol.ServerPackets.Null, AddressOf IgnorePacket)
            AddQueuedPacketHandler(Protocol.PacketId.GetFileTime, Protocol.ServerPackets.GetFileTime, AddressOf IgnorePacket)
            AddQueuedPacketHandler(Protocol.PacketId.GetIconData, Protocol.ServerPackets.GetIconData, AddressOf IgnorePacket)

            AddQueuedPacketHandler(Protocol.PacketId.QueryGamesList, Protocol.ServerPackets.QueryGamesList, AddressOf IgnorePacket)
            AddQueuedPacketHandler(Protocol.ServerPackets.FriendsUpdate, AddressOf IgnorePacket)
        End Sub

        Public ReadOnly Property Profile As Bot.ClientProfile
            Get
                Contract.Ensures(Contract.Result(Of Bot.ClientProfile)() IsNot Nothing)
                Return _profile
            End Get
        End Property
        Public ReadOnly Property Logger As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property
        Public ReadOnly Property UserName() As InvariantString
            Get
                If _userCredentials Is Nothing Then Throw New InvalidOperationException("No credentials to get username from.")
                Return Me._userCredentials.UserName
            End Get
        End Property
        Public ReadOnly Property AdvertisedGameDescription As WC3.LocalGameDescription
            Get
                Return Me._advertisedGameDescription
            End Get
        End Property
        Public ReadOnly Property AdvertisedPrivate As Boolean
            Get
                Return Me._advertisedPrivate
            End Get
        End Property
        Public Function QueueGetState() As IFuture(Of ClientState)
            Contract.Ensures(Contract.Result(Of IFuture(Of ClientState))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _state)
        End Function

        Private Function AddQueuedPacketHandler(ByVal jar As Protocol.SimplePacketDefinition,
                                                ByVal handler As Action(Of IPickle(Of Dictionary(Of InvariantString, Object)))) As IDisposable
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return AddQueuedPacketHandler(jar.id, jar, handler)
        End Function
        Private Function AddQueuedPacketHandler(Of T)(ByVal id As Protocol.PacketId,
                                                      ByVal jar As IParseJar(Of T),
                                                      ByVal handler As Action(Of IPickle(Of T))) As IDisposable
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            _packetHandler.AddLogger(id, jar.Weaken)
            Return _packetHandler.AddHandler(id, Function(data) inQueue.QueueAction(Sub() handler(jar.Parse(data))))
        End Function
        Public Function QueueAddPacketHandler(Of T)(ByVal id As Protocol.PacketId,
                                                    ByVal jar As IParseJar(Of T),
                                                    ByVal handler As Func(Of IPickle(Of T), ifuture)) As IFuture(Of IDisposable)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _packetHandler.AddHandler(id, Function(data) handler(jar.Parse(data))))
        End Function

        Private Sub SendText(ByVal text As String)
            Contract.Requires(_state >= ClientState.Channel)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(text.Length > 0)
            Dim isBnetCommand = text.StartsWith("/", StringComparison.Ordinal)

            Select Case _state
                Case ClientState.Channel
                    'fine
                Case ClientState.CreatingGame, ClientState.AdvertisingGame
                    If Not isBnetCommand Then
                        Throw New InvalidOperationException("Can't send normal messages when in a game (can still send commands).")
                    End If
            End Select

            Dim lines = SplitText(text, maxLineLength:=Protocol.ClientPackets.MaxChatCommandTextLength)
            If isBnetCommand AndAlso lines.Count > 1 Then
                Throw New InvalidOperationException("Can't send multi-line commands or commands larger than {0} characters.".Frmt(Protocol.ClientPackets.MaxChatCommandTextLength))
            End If
            For Each line In lines
                Contract.Assume(line IsNot Nothing)
                If line.Length = 0 Then Continue For
                SendPacket(Protocol.MakeChatCommand(line))
            Next line
        End Sub
        Public Function QueueSendText(ByVal text As String) As IFuture
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(text.Length > 0)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendText(text))
        End Function

        Private Sub SendWhisper(ByVal username As String, ByVal text As String)
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(username.Length > 0)
            Contract.Requires(text.Length > 0)
            Contract.Requires(_state >= ClientState.Channel)

            Dim prefix = "/w {0} ".Frmt(username)
            Contract.Assume(prefix.Length >= 5)
            If prefix.Length >= Protocol.ClientPackets.MaxChatCommandTextLength \ 2 Then
                Throw New ArgumentOutOfRangeException("username", "Username is too long.")
            End If

            For Each line In SplitText(text, maxLineLength:=Protocol.ClientPackets.MaxChatCommandTextLength - prefix.Length)
                Contract.Assume(line IsNot Nothing)
                SendPacket(Protocol.MakeChatCommand(prefix + line))
            Next line
        End Sub
        Public Function QueueSendWhisper(ByVal userName As String, ByVal text As String) As IFuture
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(userName.Length > 0)
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendWhisper(userName, text))
        End Function

        Private Sub SetReportedListenPort(ByVal port As UShort)
            If port = Me._reportedListenPort Then Return
            Me._reportedListenPort = port
            SendPacket(Protocol.MakeNetGamePort(Me._reportedListenPort))
        End Sub

        Private Sub OnSocketDisconnected(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String)
            inQueue.QueueAction(Sub() Disconnect(expected, reason))
        End Sub

#Region "State"
        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            Return QueueDisconnect(expected:=True, reason:="Disposed")
        End Function
        Private Sub ChangeState(ByVal newState As ClientState)
            Contract.Ensures(Me._state = newState)
            Dim oldState = _state
            _state = newState
            outQueue.QueueAction(Sub() RaiseEvent StateChanged(Me, oldState, newState))
        End Sub

        Private Function AsyncConnect(ByVal remoteHost As String) As IFuture
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Ifuture)() IsNot Nothing)
            If Me._state <> ClientState.Disconnected Then Throw New InvalidOperationException("Must disconnect before connecting again.")
            ChangeState(ClientState.InitiatingConnection)

            Dim port = BnetServerPort
            Try
                If Me._socket IsNot Nothing Then
                    Throw New InvalidOperationException("Client is already connected.")
                End If
                _bnetRemoteHostName = remoteHost

                'Establish connection
                Logger.Log("Connecting to {0}...".Frmt(remoteHost), LogMessageType.Typical)
                If remoteHost.Contains(":"c) Then
                    Dim remotePortTemp = remoteHost.Split(":"c)(1)
                    Contract.Assume(remotePortTemp IsNot Nothing) 'remove once static verifier understands String.split
                    port = UShort.Parse(remotePortTemp, CultureInfo.InvariantCulture)
                    remoteHost = remoteHost.Split(":"c)(0)
                End If
            Catch e As Exception
                Disconnect(expected:=False, reason:="Failed to start connection: {0}.".Frmt(e.Message))
                e.RaiseAsUnexpected("Failed to start bnet connection.")
                Throw
            End Try

            Dim futureSocket = (From hostEntry In AsyncDnsLookup(remoteHost)
                                Select address = hostEntry.AddressList(New Random().Next(hostEntry.AddressList.Count))
                                Select AsyncTcpConnect(address, port)
                               ).Defuturized
            Dim result = futureSocket.QueueEvalOnValueSuccess(inQueue,
                Function(tcpClient)
                    Dim socket = New PacketSocket(
                            stream:=New ThrottledWriteStream(
                                        subStream:=tcpClient.GetStream,
                                        initialSlack:=1000,
                                        costEstimator:=Function(data) 100 + data.Length,
                                        costLimit:=400,
                                        costRecoveredPerMillisecond:=0.048,
                                        clock:=_clock),
                            localendpoint:=CType(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                            remoteendpoint:=CType(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                            clock:=_clock,
                            timeout:=60.Seconds,
                            Logger:=Logger,
                            bufferSize:=PacketSocket.DefaultBufferSize * 10)
                    ChangeState(ClientState.FinishedInitiatingConnection)
                    Return AsyncConnect(socket)
                End Function).Defuturized()
            result.Catch(Sub(exception)
                             QueueDisconnect(expected:=False, reason:="Failed to complete connection: {0}.".Frmt(exception))
                         End Sub)
            Return result
        End Function
        Public Function QueueConnect(ByVal remoteHost As String) As IFuture
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncConnect(remoteHost)).Defuturized
        End Function

        Private Function AsyncConnect(ByVal socket As PacketSocket,
                                      Optional ByVal clientCdKeySalt As UInt32? = Nothing) As IFuture
            Contract.Requires(socket IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Ifuture)() IsNot Nothing)
            If Me._state <> ClientState.Disconnected AndAlso Me._state <> ClientState.FinishedInitiatingConnection Then
                Throw New InvalidOperationException("Must disconnect before connecting again.")
            End If

            If clientCdKeySalt Is Nothing Then
                Using rng = New System.Security.Cryptography.RNGCryptoServiceProvider()
                    Dim clientKeySaltBytes(0 To 3) As Byte
                    rng.GetBytes(clientKeySaltBytes)
                    clientCdKeySalt = clientKeySaltBytes.ToUInt32
                End Using
            End If
            Me._clientCdKeySalt = clientCdKeySalt.Value
            Me._socket = socket
            Me._socket.Name = "BNET"
            ChangeState(ClientState.WaitingForProgramAuthenticationBegin)

            'Reset the class future for the connection outcome
            Me._futureConnected.TrySetFailed(New InvalidStateException("Another connection was initiated."))
            Me._futureConnected = New FutureAction
            Me._futureConnected.SetHandled()

            'Introductions
            socket.SubStream.Write({1}, 0, 1) 'protocol version
            SendPacket(Protocol.MakeAuthenticationBegin(_externalProvider.WC3MajorVersion, New Net.IPAddress(GetCachedIPAddressBytes(external:=False))))

            BeginHandlingPackets()
            Return Me._futureConnected
        End Function
        Public Function QueueConnect(ByVal socket As PacketSocket,
                                     Optional ByVal clientCDKeySalt As UInt32? = Nothing) As IFuture
            Contract.Requires(socket IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncConnect(socket, clientCDKeySalt)).Defuturized
        End Function

        Private Sub BeginHandlingPackets()
            Contract.Requires(Me._state > ClientState.InitiatingConnection)
            AsyncProduceConsumeUntilError(
                producer:=AddressOf _socket.AsyncReadPacket,
                consumer:=AddressOf _packetHandler.HandlePacket,
                errorHandler:=Sub(exception) QueueDisconnect(expected:=False,
                                                             reason:="Error receiving packet: {0}".Frmt(exception.Message)))
        End Sub

        Private Function BeginLogOn(ByVal credentials As ClientCredentials) As IFuture
            Contract.Requires(credentials IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            If _state <> ClientState.EnterUserCredentials Then
                Throw New InvalidOperationException("Incorrect state for login.")
            End If

            Me._futureLoggedIn.TrySetFailed(New InvalidStateException("Another login was initiated."))
            Me._futureLoggedIn = New FutureAction
            Me._futureLoggedIn.SetHandled()

            Me._userCredentials = credentials
            ChangeState(ClientState.WaitingForUserAuthenticationBegin)
            SendPacket(Protocol.MakeAccountLogOnBegin(credentials))
            Logger.Log("Initiating logon with username {0}.".Frmt(credentials.UserName), LogMessageType.Typical)
            Return _futureLoggedIn
        End Function
        Public Function QueueLogOn(ByVal credentials As ClientCredentials) As IFuture
            Contract.Requires(credentials IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueFunc(Function() BeginLogOn(credentials)).Defuturized
        End Function

        Public Function QueueConnectAndLogOn(ByVal remoteHost As String,
                                             ByVal credentials As ClientCredentials) As IFuture
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Requires(credentials IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return QueueConnect(remoteHost).EvalOnSuccess(Function() QueueLogOn(credentials)).Defuturized
        End Function

        Private Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            If _socket IsNot Nothing Then
                _socket.QueueDisconnect(expected, reason)
                RemoveHandler _socket.Disconnected, AddressOf OnSocketDisconnected
                _socket = Nothing
            ElseIf _state = ClientState.Disconnected Then
                Return
            End If

            'Finalize class futures
            _futureConnected.TrySetFailed(New InvalidOperationException("Disconnected before connection completed ({0}).".Frmt(reason)))
            _futureLoggedIn.TrySetFailed(New InvalidOperationException("Disconnected before logon completed ({0}).".Frmt(reason)))
            _futureAdvertisedGame.TrySetFailed(New InvalidOperationException("Disconnected before game creation completed ({0}).".Frmt(reason)))

            ChangeState(ClientState.Disconnected)
            Logger.Log("Disconnected ({0})".Frmt(reason), LogMessageType.Negative)
            If _wardenClient IsNot Nothing Then
                _wardenClient.Dispose()
                _wardenClient = Nothing
            End If

            'outQueue.QueueAction(Sub() RaiseEvent Disconnected(Me, reason))

            If Not expected AndAlso _allowRetryConnect Then
                _allowRetryConnect = False
                _clock.AsyncWait(5.Seconds).CallWhenReady(
                    Sub()
                        Logger.Log("Attempting to reconnect...", LogMessageType.Positive)
                        QueueConnectAndLogOn(_bnetRemoteHostName, Me._userCredentials.Regenerate())
                    End Sub
                )
            End If
        End Sub
        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() Disconnect(expected, reason))
        End Function

        Private Sub EnterChannel(ByVal channel As String)
            _futureAdvertisedGame.TrySetFailed(New InvalidOperationException("Re-entered channel before game was created."))
            SendPacket(Protocol.MakeJoinChannel(Protocol.JoinChannelType.ForcedJoin, channel))
            ChangeState(ClientState.Channel)
        End Sub

        Private Function BeginAdvertiseGame(ByVal gameDescription As WC3.LocalGameDescription,
                                            ByVal isPrivate As Boolean) As IFuture
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)

            Select Case _state
                Case ClientState.Disconnected To ClientState.WaitingForEnterChat
                    Throw New InvalidOperationException("Can't advertise a game until connected.")
                Case ClientState.CreatingGame
                    If _advertisedGameDescription.GameId = gameDescription.GameId Then Return _futureAdvertisedGame
                    Throw New InvalidOperationException("Already creating a game.")
                Case ClientState.AdvertisingGame
                    If _advertisedGameDescription.GameId = gameDescription.GameId Then Return _futureAdvertisedGame
                    Throw New InvalidOperationException("Already advertising a game.")
                Case ClientState.Channel
                    SetReportedListenPort(gameDescription.Port)
                    Me._advertisedGameDescription = gameDescription
                    Me._advertisedPrivate = isPrivate
                    Me._futureAdvertisedGame.TrySetFailed(New OperationFailedException("Started advertising another game."))
                    Me._futureAdvertisedGame = New FutureAction
                    Me._futureAdvertisedGame.SetHandled()
                    ChangeState(ClientState.CreatingGame)
                    Try
                        AdvertiseGame(useFull:=False, refreshing:=False)
                    Catch ex As Exception
                        _futureAdvertisedGame.TrySetFailed(New OperationFailedException("Failed to send data."))
                        ChangeState(ClientState.Channel)
                        Throw
                    End Try
                    Return _futureAdvertisedGame
                Case Else
                    Throw _state.MakeImpossibleValueException
            End Select
        End Function
        Private Sub AdvertiseGame(Optional ByVal useFull As Boolean = False,
                                  Optional ByVal refreshing As Boolean = False)
            If refreshing Then
                If _state <> ClientState.AdvertisingGame Then
                    Throw New InvalidOperationException("Must have already created game before refreshing")
                End If
                ChangeState(ClientState.AdvertisingGame) '[throws event]
            End If

            Dim gameState = Protocol.GameStates.Unknown0x10
            If _advertisedPrivate Then gameState = gameState Or Protocol.GameStates.Private
            If useFull Then
                gameState = gameState Or Protocol.GameStates.Full
            Else
                gameState = gameState And Not Protocol.GameStates.Full
            End If
            'If in_progress Then gameState = gameState Or BnetPacket.GameStateFlags.InProgress
            'If Not empty Then game_state_flags = game_state_flags Or FLAG_NOT_EMPTY [causes problems: why?]

            Dim gameType = WC3.Protocol.GameTypes.CreateGameUnknown0 Or Me._advertisedGameDescription.GameType
            If _advertisedPrivate Then
                gameState = gameState Or Protocol.GameStates.Private
                gameType = gameType Or WC3.Protocol.GameTypes.PrivateGame
            Else
                gameState = gameState And Not Protocol.GameStates.Private
                gameType = gameType And Not WC3.Protocol.GameTypes.PrivateGame
            End If
            Select Case Me._advertisedGameDescription.GameStats.Observers
                Case WC3.GameObserverOption.FullObservers, WC3.GameObserverOption.Referees
                    gameType = gameType Or WC3.Protocol.GameTypes.ObsFull
                Case WC3.GameObserverOption.ObsOnDefeat
                    gameType = gameType Or WC3.Protocol.GameTypes.ObsOnDeath
                Case WC3.GameObserverOption.NoObservers
                    gameType = gameType Or WC3.Protocol.GameTypes.ObsNone
            End Select

            Me._advertisedGameDescription = New WC3.LocalGameDescription(
                    name:=_advertisedGameDescription.Name,
                    gamestats:=_advertisedGameDescription.GameStats,
                    hostport:=_advertisedGameDescription.Port,
                    gameid:=_advertisedGameDescription.GameId,
                    entrykey:=_advertisedGameDescription.EntryKey,
                    totalslotcount:=_advertisedGameDescription.TotalSlotCount,
                    gameType:=gameType,
                    state:=gameState,
                    usedslotcount:=_advertisedGameDescription.UsedSlotCount,
                    baseage:=_advertisedGameDescription.Age,
                    clock:=New SystemClock())
            SendPacket(Protocol.MakeCreateGame3(_advertisedGameDescription))
        End Sub
        Public Function QueueStartAdvertisingGame(ByVal gameDescription As WC3.LocalGameDescription,
                                                  ByVal isPrivate As Boolean) As IFuture
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueFunc(Function() BeginAdvertiseGame(gameDescription, isPrivate)).Defuturized
        End Function

        Private Sub StopAdvertisingGame(ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)

            Select Case _state
                Case ClientState.CreatingGame, ClientState.AdvertisingGame
                    SendPacket(Protocol.MakeCloseGame3())
                    If _advertiseTicker IsNot Nothing Then
                        _advertiseTicker.Dispose()
                        _advertiseTicker = Nothing
                    End If
                    EnterChannel(_lastChannel)
                    _advertisedGameDescription = Nothing
                    _futureAdvertisedGame.TrySetFailed(New OperationFailedException("Advertising cancelled: {0}.".Frmt(reason)))

                Case Else
                    Throw New InvalidOperationException("Wasn't advertising any games.")
            End Select
        End Sub
        Public Function QueueStopAdvertisingGame(ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() StopAdvertisingGame(reason))
        End Function
        Public Function QueueStopAdvertisingGame(ByVal id As UInt32, ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub()
                                           If _advertisedGameDescription IsNot Nothing AndAlso _advertisedGameDescription.GameId <> id Then
                                               Throw New InvalidOperationException("The game being advertised does not have that id.")
                                           End If
                                           StopAdvertisingGame(reason)
                                       End Sub)
        End Function
#End Region

#Region "Networking (Send)"
        Private Sub SendPacket(ByVal packet As Protocol.Packet)
            Contract.Requires(Me._state > ClientState.InitiatingConnection)
            Contract.Requires(packet IsNot Nothing)

            Try
                Logger.Log(Function() "Sending {0} to {1}".Frmt(packet.Id, _socket.Name), LogMessageType.DataEvent)
                Logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(packet.Id, _socket.Name, packet.Payload.Description.Value), LogMessageType.DataParsed)

                _socket.WritePacket(Concat({Protocol.ClientPackets.PacketPrefixValue, packet.Id, 0, 0}, packet.Payload.Data.ToArray))
            Catch e As Exception
                Disconnect(expected:=False, reason:="Error sending {0} to {1}: {2}".Frmt(packet.Id, _socket.Name, e.Message))
                e.RaiseAsUnexpected("Error sending {0} to {1}".Frmt(packet.Id, _socket.Name))
            End Try
        End Sub
        Public Function QueueSendPacket(ByVal packet As Protocol.Packet) As IFuture
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendPacket(packet))
        End Function
#End Region

#Region "Networking (Connect)"
        Private Sub ReceiveProgramAuthenticationBegin(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value
            Const LOGON_TYPE_WC3 As UInteger = 2
            If _state <> ClientState.WaitingForProgramAuthenticationBegin Then
                Throw New IO.InvalidDataException("Invalid state for receiving {0}".Frmt(Protocol.PacketId.ProgramAuthenticationBegin))
            End If

            'Check
            If CType(vals("logon type"), UInteger) <> LOGON_TYPE_WC3 Then
                _futureConnected.TrySetFailed(New IO.InvalidDataException("Failed to connect: Unrecognized logon type from server."))
                Throw New IO.InvalidDataException("Unrecognized logon type")
            End If

            'Salts
            Dim serverCdKeySalt = CUInt(vals("server cd key salt"))
            Dim clientCdKeySalt = _clientCdKeySalt

            'Async Enter Keys
            ChangeState(ClientState.EnterCDKeys)
            AsyncRetrieveKeys(clientCdKeySalt, serverCdKeySalt).QueueCallOnValueSuccess(inQueue,
                Sub(keys) EnterKeys(keys:=keys,
                                    revisionCheckSeedString:=CStr(vals("revision check seed")),
                                    revisionCheckChallenge:=CStr(vals("revision check challenge")),
                                    clientCdKeySalt:=clientCdKeySalt)
            ).Catch(
                Sub(exception)
                    exception.RaiseAsUnexpected("Error Handling {0}".Frmt(Protocol.PacketId.ProgramAuthenticationBegin))
                    QueueDisconnect(expected:=False, reason:="Error handling {0}: {1}".Frmt(Protocol.PacketId.ProgramAuthenticationBegin, exception.Message))
                End Sub
            )
        End Sub
        Private Function AsyncRetrieveKeys(ByVal clientCdKeySalt As UInt32, ByVal serverCdKeySalt As UInt32) As IFuture(Of CKL.WC3CredentialPair)
            If Profile.CKLServerAddress Like "*:#*" Then
                Dim remoteHost = Profile.CKLServerAddress.Split(":"c)(0)
                Dim port = UShort.Parse(Profile.CKLServerAddress.Split(":"c)(1).AssumeNotNull, CultureInfo.InvariantCulture)
                Dim result = CKL.Client.AsyncBorrowCredentials(remoteHost, port, clientCdKeySalt, serverCdKeySalt, _clock)
                result.CallOnSuccess(Sub() Logger.Log("Succesfully borrowed keys from CKL server.", LogMessageType.Positive)).SetHandled()
                Return result
            Else
                Dim roc = Profile.cdKeyROC.ToWC3CDKeyCredentials(clientCdKeySalt.Bytes, serverCdKeySalt.Bytes)
                Dim tft = Profile.cdKeyTFT.ToWC3CDKeyCredentials(clientCdKeySalt.Bytes, serverCdKeySalt.Bytes)
                If roc.Product <> ProductType.Warcraft3ROC Then Throw New IO.InvalidDataException("Invalid ROC cd key.")
                If tft.Product <> ProductType.Warcraft3TFT Then Throw New IO.InvalidDataException("Invalid TFT cd key.")
                Return New CKL.WC3CredentialPair(roc, tft).Futurized
            End If
        End Function
        Private Sub EnterKeys(ByVal keys As CKL.WC3CredentialPair,
                              ByVal revisionCheckSeedString As String,
                              ByVal revisionCheckChallenge As String,
                              ByVal clientCdKeySalt As UInt32)
            If _state <> ClientState.EnterCDKeys Then Throw New InvalidStateException("Incorrect state for entering cd keys.")
            Dim revisionCheckResponse As UInt32
            Try
                revisionCheckResponse = _externalProvider.GenerateRevisionCheck(
                    folder:=My.Settings.war3path,
                    challengeSeed:=revisionCheckSeedString,
                    challengeInstructions:=revisionCheckChallenge)
            Catch ex As ArgumentException
                If revisionCheckChallenge = "" Then
                    Throw New IO.InvalidDataException("Received an invalid revision check challenge from bnet. Try connecting again.")
                End If
                Throw New OperationCanceledException("Failed to compute revision check.", ex)
            End Try
            SendPacket(Protocol.MakeAuthenticationFinish(
                       version:=_externalProvider.WC3ExeVersion,
                       revisionCheckResponse:=revisionCheckResponse,
                       clientCDKeySalt:=clientCdKeySalt,
                       cdKeyOwner:=My.Settings.cdKeyOwner,
                       exeInformation:="war3.exe {0} {1}".Frmt(_externalProvider.WC3LastModifiedTime.ToString("MM/dd/yy hh:mm:ss", CultureInfo.InvariantCulture),
                                                               _externalProvider.WC3FileSize),
                       productAuthentication:=keys))

            ChangeState(ClientState.WaitingForProgramAuthenticationFinish)

            'Parse address setting
            Dim remoteHost = ""
            Dim remotePort = 0US
            Dim remoteEndPointArg As InvariantString = If(My.Settings.bnls, "")
            If remoteEndPointArg <> "" Then
                Dim hostPortPair = remoteEndPointArg.ToString.Split(":"c)
                remoteHost = hostPortPair(0)
                If hostPortPair.Length <> 2 OrElse Not UShort.TryParse(hostPortPair(1), remotePort) Then
                    Logger.Log("Invalid bnls server format specified. Expected hostname:port.", LogMessageType.Problem)
                End If
            End If

            'Attempt BNLS connection
            Dim seed = keys.AuthenticationROC.AuthenticationProof.Take(4).ToUInt32
            _wardenClient = New Warden.Client(remoteHost:=remoteHost,
                                              remotePort:=remotePort,
                                              seed:=seed,
                                              cookie:=seed,
                                              Logger:=Logger,
                                              clock:=_clock)
        End Sub

        Private Sub ReceiveProgramAuthenticationFinish(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Try
                Dim vals = value.Value
                Dim result = CType(CUInt(vals("result")), Protocol.ProgramAuthenticationFinishResult)
                If _state <> ClientState.WaitingForProgramAuthenticationFinish Then
                    Throw New IO.InvalidDataException("Invalid state for receiving {0}: {1}".Frmt(Protocol.PacketId.ProgramAuthenticationFinish, _state))
                ElseIf result <> Protocol.ProgramAuthenticationFinishResult.Passed Then
                    Throw New IO.InvalidDataException("Program authentication failed with error: {0} {1}.".Frmt(result, vals("info")))
                End If

                ChangeState(ClientState.EnterUserCredentials)
                _futureConnected.TrySetSucceeded()
                _allowRetryConnect = True
            Catch ex As Exception
                _futureConnected.TrySetFailed(ex)
                Throw
            End Try
        End Sub

        Private Sub ReceiveUserAuthenticationBegin(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Try
                Dim vals = value.Value
                Dim result = CType(vals("result"), Protocol.UserAuthenticationBeginResult)
                If _state <> ClientState.WaitingForUserAuthenticationBegin Then
                    Throw New IO.InvalidDataException("Invalid state for receiving {0}".Frmt(Protocol.PacketId.UserAuthenticationBegin))
                ElseIf result <> Protocol.UserAuthenticationBeginResult.Passed Then
                    Throw New IO.InvalidDataException("User authentication failed with error: {0}".Frmt(result))
                End If

                Dim accountPasswordSalt = CType(vals("account password salt"), IReadableList(Of Byte)).AssumeNotNull
                Dim serverPublicKey = CType(vals("server public key"), IReadableList(Of Byte)).AssumeNotNull

                If Me._userCredentials Is Nothing Then Throw New InvalidStateException("Received AccountLogOnBegin before credentials specified.")
                Dim clientProof = Me._userCredentials.ClientPasswordProof(accountPasswordSalt, serverPublicKey)
                Dim serverProof = Me._userCredentials.ServerPasswordProof(accountPasswordSalt, serverPublicKey)

                Me._expectedServerPasswordProof = serverProof
                ChangeState(ClientState.WaitingForUserAuthenticationFinish)
                SendPacket(Protocol.MakeAccountLogOnFinish(clientProof))
            Catch ex As Exception
                _futureLoggedIn.TrySetFailed(ex)
                _futureConnected.TrySetFailed(ex)
                Throw
            End Try
        End Sub

        Private Sub ReceiveUserAuthenticationFinish(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Try
                Dim vals = value.Value
                Dim result = CType(vals("result"), Protocol.UserAuthenticationFinishResult)
                Dim serverProof = CType(vals("server password proof"), IReadableList(Of Byte)).AssumeNotNull

                'validate
                If _state <> ClientState.WaitingForUserAuthenticationFinish Then
                    Throw New IO.InvalidDataException("Invalid state for receiving {0}: {1}".Frmt(Protocol.PacketId.UserAuthenticationFinish, _state))
                ElseIf result <> Protocol.UserAuthenticationFinishResult.Passed Then
                    Dim errorInfo = ""
                    Select Case result
                        Case Protocol.UserAuthenticationFinishResult.IncorrectPassword
                            errorInfo = "(Note: This can happen due to a bnet bug. You might want to try again.):"
                        Case Protocol.UserAuthenticationFinishResult.CustomError
                            errorInfo = "({0})".Frmt(CType(vals("custom error info"), Tuple(Of Boolean, String)).Item2)
                    End Select
                    Throw New IO.InvalidDataException("User authentication failed with error: {0} {1}".Frmt(result, errorInfo))
                ElseIf _expectedServerPasswordProof Is Nothing Then
                    Throw New InvalidStateException("Received {0} before the server password proof was knowable.".Frmt(Protocol.PacketId.UserAuthenticationFinish))
                ElseIf Not _expectedServerPasswordProof.SequenceEqual(serverProof) Then
                    Throw New IO.InvalidDataException("The server's password proof was incorrect.")
                End If

                ChangeState(ClientState.WaitingForEnterChat)
                Logger.Log("Logged on with username {0}.".Frmt(Me._userCredentials.UserName), LogMessageType.Typical)
                _futureLoggedIn.TrySetSucceeded()

                'respond
                SetReportedListenPort(6112)
                SendPacket(Protocol.MakeEnterChat())
            Catch ex As Exception
                _futureLoggedIn.TrySetFailed(ex)
                _futureConnected.TrySetFailed(ex)
                Throw
            End Try
        End Sub

        Private Sub ReceiveEnterChat(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value
            Logger.Log("Entered chat", LogMessageType.Typical)
            EnterChannel(Profile.initialChannel)
        End Sub
#End Region

#Region "Networking (Warden)"
        Private Sub ReceiveWarden(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(pickle IsNot Nothing)
            If _state < ClientState.WaitingForEnterChat Then Throw New IO.InvalidDataException("Warden packet in unexpected place.")
            Dim encryptedData = CType(pickle.Value("encrypted data"), IReadableList(Of Byte)).AssumeNotNull
            _wardenClient.QueueSendWardenData(encryptedData)
        End Sub
        Private Sub OnWardenReceivedResponseData(ByVal sender As Warden.Client, ByVal data As IReadableList(Of Byte)) Handles _wardenClient.ReceivedWardenData
            Contract.Requires(data IsNot Nothing)
            inQueue.QueueAction(Sub() SendPacket(Protocol.MakeWarden(data)))
        End Sub
        Private Sub OnWardenFail(ByVal sender As Warden.Client, ByVal exception As Exception) Handles _wardenClient.Failed
            Contract.Requires(exception IsNot Nothing)
            sender.Activated.CallOnSuccess(Sub()
                                               QueueDisconnect(expected:=False, reason:="Warden/BNLS Error: {0}.".Frmt(exception.Message))
                                           End Sub).SetHandled()
            If sender.Activated.State = FutureState.Unknown Then
                Logger.Log("Lost connection to BNLS server: {0}".Frmt(exception.Message), LogMessageType.Problem)
            End If
            exception.RaiseAsUnexpected("Warden/BNLS Error")
        End Sub
#End Region

#Region "Networking (Games)"
        Private Sub ReceiveCreateGame3(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value
            Dim result = CUInt(vals("result"))

            If result = 0 Then
                If _state = ClientState.CreatingGame Then
                    Logger.Log("Finished creating game.", LogMessageType.Positive)
                    ChangeState(ClientState.AdvertisingGame)
                    If Not _advertisedPrivate Then
                        _advertiseTicker = _clock.AsyncRepeat(RefreshPeriod, Sub() inQueue.QueueAction(Sub() AdvertiseGame(useFull:=False, refreshing:=True)))
                    End If
                    _futureAdvertisedGame.TrySetSucceeded()
                Else
                    ChangeState(ClientState.AdvertisingGame) 'throw event
                End If
            Else
                _futureAdvertisedGame.TrySetFailed(New OperationFailedException("BNET didn't allow game creation (error code: {0}). Most likely cause is game name in use.".Frmt(result)))
                If _advertiseTicker IsNot Nothing Then
                    _advertiseTicker.Dispose()
                    _advertiseTicker = Nothing
                End If
                EnterChannel(_lastChannel)
                'RaiseEvent RemovedGame(Me, _advertisedGameDescription, "Failed to advertise the game. Most likely cause is game name in use.")
            End If
        End Sub
        Private Sub IgnorePacket(ByVal value As Object)
            Contract.Requires(value IsNot Nothing)
        End Sub
#End Region

#Region "Networking (Misc)"
        Private Sub ReceiveChatEvent(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value
            Dim eventId = CType(vals("event id"), Protocol.ChatEventId)
            Dim text = CStr(vals("text"))
            If eventId = Protocol.ChatEventId.Channel Then _lastChannel = text
        End Sub

        Private Sub ReceivePing(ByVal value As IPickle(Of UInt32))
            Contract.Requires(value IsNot Nothing)
            SendPacket(Protocol.MakePing(salt:=value.Value))
        End Sub

        Private Sub ReceiveMessageBox(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value
            Dim msg = "MESSAGE BOX FROM BNET: " + CStr(vals("caption")) + ": " + CStr(vals("text"))
            Logger.Log(msg, LogMessageType.Problem)
        End Sub
#End Region
    End Class
End Namespace
