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
        Private ReadOnly _productAuthenticator As IProductAuthenticator
        Private ReadOnly _logger As Logger
        Private ReadOnly _packetHandler As Protocol.BnetPacketHandler
        Private _socket As PacketSocket
        Private WithEvents _wardenClient As Warden.Client

        'game
        Private Class AdvertisementEntry
            Public ReadOnly BaseGameDescription As WC3.LocalGameDescription
            Public ReadOnly IsPrivate As Boolean
            Private ReadOnly _initialGameDescription As New FutureFunction(Of WC3.LocalGameDescription)
            Private _failCount As UInt32
            Public Sub New(ByVal gameDescription As WC3.LocalGameDescription, ByVal isPrivate As Boolean)
                Me.BaseGameDescription = gameDescription
                Me.IsPrivate = isPrivate
                _initialGameDescription.SetHandled()
            End Sub

            Public Sub SetNameFailed()
                _failCount += 1UI
            End Sub
            Public Sub SetNameSucceeded()
                _initialGameDescription.TrySetSucceeded(UpdatedGameDescription(New ManualClock()))
            End Sub
            Public Sub SetRemoved()
                _initialGameDescription.TrySetFailed(New InvalidOperationException("Removed before advertising succeeded."))
            End Sub

            Public ReadOnly Property FutureDescription As IFuture(Of WC3.LocalGameDescription)
                Get
                    Contract.Ensures(Contract.Result(Of IFuture(Of WC3.LocalGameDescription))() IsNot Nothing)
                    Return _initialGameDescription
                End Get
            End Property
            Private ReadOnly Property GameName As String
                Get
                    Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                    Const SuffixCharacters As String = "~!@#$%^&*(+-=,./\'"":;"
                    Dim result = BaseGameDescription.Name
                    Dim suffix = New String((From b In _failCount.Bytes.ConvertFromBaseToBase(256, CUInt(SuffixCharacters.Length))
                                             Select SuffixCharacters(b)).ToArray)
                    If result.Length + suffix.Length > Bnet.Protocol.Packets.ClientToServer.MaxGameNameLength Then
                        result = result.Substring(0, Bnet.Protocol.Packets.ClientToServer.MaxGameNameLength - suffix.Length)
                    End If
                    result += suffix
                    Return result
                End Get
            End Property
            Public ReadOnly Property UpdatedGameDescription(ByVal clock As IClock) As WC3.LocalGameDescription
                Get
                    Dim useFull = False
                    Dim gameState = Protocol.GameStates.Unknown0x10
                    If IsPrivate Then gameState = gameState Or Protocol.GameStates.Private
                    If useFull Then
                        gameState = gameState Or Protocol.GameStates.Full
                    Else
                        gameState = gameState And Not Protocol.GameStates.Full
                    End If
                    'If in_progress Then gameState = gameState Or BnetPacket.GameStateFlags.InProgress
                    'If Not empty Then game_state_flags = game_state_flags Or FLAG_NOT_EMPTY [causes problems: why?]

                    Dim gameType = WC3.Protocol.GameTypes.CreateGameUnknown0 Or BaseGameDescription.GameType
                    If IsPrivate Then
                        gameState = gameState Or Protocol.GameStates.Private
                        gameType = gameType Or WC3.Protocol.GameTypes.PrivateGame
                    Else
                        gameState = gameState And Not Protocol.GameStates.Private
                        gameType = gameType And Not WC3.Protocol.GameTypes.PrivateGame
                    End If
                    Select Case BaseGameDescription.GameStats.Observers
                        Case WC3.GameObserverOption.FullObservers, WC3.GameObserverOption.Referees
                            gameType = gameType Or WC3.Protocol.GameTypes.ObsFull
                        Case WC3.GameObserverOption.ObsOnDefeat
                            gameType = gameType Or WC3.Protocol.GameTypes.ObsOnDeath
                        Case WC3.GameObserverOption.NoObservers
                            gameType = gameType Or WC3.Protocol.GameTypes.ObsNone
                    End Select

                    Return New WC3.LocalGameDescription(name:=Me.GameName,
                                                        gamestats:=BaseGameDescription.GameStats,
                                                        hostport:=BaseGameDescription.Port,
                                                        gameid:=BaseGameDescription.GameId,
                                                        entrykey:=BaseGameDescription.EntryKey,
                                                        totalslotcount:=BaseGameDescription.TotalSlotCount,
                                                        gameType:=gameType,
                                                        state:=gameState,
                                                        usedslotcount:=BaseGameDescription.UsedSlotCount,
                                                        baseage:=BaseGameDescription.Age,
                                                        clock:=clock)
                End Get
            End Property
        End Class
        Private ReadOnly _advertisementList As New List(Of AdvertisementEntry)
        Private _curAdvertisement As AdvertisementEntry
        Private _advertiseRefreshTicker As IDisposable
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
        Public Event AdvertisedGame(ByVal sender As Client, ByVal gameDescription As WC3.LocalGameDescription, ByVal [private] As Boolean, ByVal refreshed As Boolean)

        Private _lastChannel As InvariantString
        Private _state As ClientState

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_advertisementList IsNot Nothing)
            Contract.Invariant(_packetHandler IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_profile IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_productAuthenticator IsNot Nothing)
            Contract.Invariant(_futureLoggedIn IsNot Nothing)
            Contract.Invariant(_futureConnected IsNot Nothing)
            Contract.Invariant(_externalProvider IsNot Nothing)
            Contract.Invariant((_socket IsNot Nothing) = (_state > ClientState.InitiatingConnection))
            Contract.Invariant((_wardenClient IsNot Nothing) = (_state > ClientState.WaitingForProgramAuthenticationBegin))
            Contract.Invariant((_state <= ClientState.EnterUserCredentials) OrElse (_userCredentials IsNot Nothing))
            Contract.Invariant((_curAdvertisement IsNot Nothing) = (_state >= ClientState.CreatingGame))
        End Sub

        Public Sub New(ByVal profile As Bot.ClientProfile,
                       ByVal externalProvider As IExternalValues,
                       ByVal productAuthenticator As IProductAuthenticator,
                       ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(profile IsNot Nothing)
            Contract.Assume(externalProvider IsNot Nothing)
            Contract.Assume(clock IsNot Nothing)
            Contract.Assume(productAuthenticator IsNot Nothing)
            Me._futureConnected.SetHandled()
            Me._futureLoggedIn.SetHandled()

            'Pass values
            Me._clock = clock
            Me._profile = profile
            Me._externalProvider = externalProvider
            Me._logger = If(logger, New Logger)
            Me.outQueue = New TaskedCallQueue
            Me.inQueue = New TaskedCallQueue
            Me._productAuthenticator = productAuthenticator

            'Start packet machinery
            Me._packetHandler = New Protocol.BnetPacketHandler("BNET", Me._logger)

            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.ProgramAuthenticationBegin, AddressOf ReceiveProgramAuthenticationBegin)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.ProgramAuthenticationFinish, AddressOf ReceiveProgramAuthenticationFinish)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.UserAuthenticationBegin, AddressOf ReceiveUserAuthenticationBegin)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.UserAuthenticationFinish, AddressOf ReceiveUserAuthenticationFinish)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.ChatEvent, AddressOf ReceiveChatEvent)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.EnterChat, AddressOf ReceiveEnterChat)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.MessageBox, AddressOf ReceiveMessageBox)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.CreateGame3, AddressOf ReceiveCreateGame3)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.Warden, AddressOf ReceiveWarden)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.Ping, AddressOf ReceivePing)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.Null, AddressOf IgnorePacket)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.GetFileTime, AddressOf IgnorePacket)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.GetIconData, AddressOf IgnorePacket)

            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.QueryGamesList, AddressOf IgnorePacket)
            AddQueuedLocalPacketHandler(Protocol.Packets.ServerToClient.FriendsUpdate, AddressOf IgnorePacket)
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
        Public Function QueueGetState() As IFuture(Of ClientState)
            Contract.Ensures(Contract.Result(Of IFuture(Of ClientState))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _state)
        End Function

        Private Function AddQueuedLocalPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                           ByVal handler As Action(Of IPickle(Of T))) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            _packetHandler.AddLogger(packetDefinition.Id, packetDefinition.Jar.Weaken)
            Return _packetHandler.AddHandler(packetDefinition.Id, Function(data) inQueue.QueueAction(Sub() handler(packetDefinition.Jar.Parse(data))))
        End Function

        Private Function AddRemotePacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                      ByVal handler As Func(Of IPickle(Of T), IFuture)) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _packetHandler.AddHandler(packetDefinition.Id, Function(data) handler(packetDefinition.Jar.Parse(data)))
        End Function
        Public Function QueueAddPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                    ByVal handler As Func(Of IPickle(Of T), ifuture)) As IFuture(Of IDisposable)
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AddRemotePacketHandler(packetDefinition, handler))
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

            Dim lines = SplitText(text, maxLineLength:=Protocol.Packets.ClientToServer.MaxChatCommandTextLength)
            If isBnetCommand AndAlso lines.Count > 1 Then
                Throw New InvalidOperationException("Can't send multi-line commands or commands larger than {0} characters.".Frmt(Protocol.Packets.ClientToServer.MaxChatCommandTextLength))
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
            If prefix.Length >= Protocol.Packets.ClientToServer.MaxChatCommandTextLength \ 2 Then
                Throw New ArgumentOutOfRangeException("username", "Username is too long.")
            End If

            For Each line In SplitText(text, maxLineLength:=Protocol.Packets.ClientToServer.MaxChatCommandTextLength - prefix.Length)
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

            'Port
            Dim port = BnetServerPort
            If remoteHost.Contains(":"c) Then
                Dim parts = remoteHost.Split(":"c)
                If parts.Count <> 2 OrElse Not UShort.TryParse(parts.Last, NumberStyles.Integer, CultureInfo.InvariantCulture, port) Then
                    Throw New ArgumentException(paramName:="remoteHost", Message:="Invalid hostname.")
                End If
                remoteHost = parts.First
            End If

            'Connect
            Logger.Log("Connecting to {0}...".Frmt(remoteHost), LogMessageType.Typical)
            Dim futureSocket = From hostEntry In AsyncDnsLookup(remoteHost)
                               Select address = hostEntry.AddressList(New Random().Next(hostEntry.AddressList.Count))
                               From tcpClient In AsyncTcpConnect(address, port)
                               Let stream = New ThrottledWriteStream(subStream:=tcpClient.GetStream,
                                                                     initialSlack:=1000,
                                                                     costEstimator:=Function(data) 100 + data.Length,
                                                                     costLimit:=400,
                                                                     costRecoveredPerMillisecond:=0.048,
                                                                     clock:=_clock)
                               Select New PacketSocket(stream:=stream,
                                                       localEndPoint:=CType(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                                       remoteEndPoint:=CType(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                                       clock:=_clock,
                                                       timeout:=60.Seconds,
                                                       Logger:=Logger,
                                                       bufferSize:=PacketSocket.DefaultBufferSize * 10)

            'Continue
            Dim result = futureSocket.QueueEvalOnValueSuccess(inQueue,
                Function(socket)
                    ChangeState(ClientState.FinishedInitiatingConnection)
                    Return AsyncConnect(socket)
                End Function).Defuturized
            result.QueueCatch(inQueue, Sub(exception) Disconnect(expected:=False, reason:="Failed to complete connection: {0}.".Frmt(exception.Message)))
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
            _curAdvertisement = Nothing

            ChangeState(ClientState.Disconnected)
            Logger.Log("Disconnected ({0})".Frmt(reason), LogMessageType.Negative)
            If _wardenClient IsNot Nothing Then
                _wardenClient.Dispose()
                _wardenClient = Nothing
            End If

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
            SendPacket(Protocol.MakeJoinChannel(Protocol.JoinChannelType.ForcedJoin, channel))
            ChangeState(ClientState.Channel)
            SyncAdvertisements()
        End Sub
#End Region

#Region "Advertising"
        Private Sub SyncAdvertisements()
            Select Case _state
                Case ClientState.Channel
                    If _advertisementList.None Then Return
                    ChangeState(ClientState.CreatingGame)
                    _curAdvertisement = _advertisementList.First
                    SetReportedListenPort(_curAdvertisement.BaseGameDescription.Port)
                    SendPacket(Protocol.MakeCreateGame3(_curAdvertisement.UpdatedGameDescription(_clock)))
                Case ClientState.AdvertisingGame, ClientState.CreatingGame
                    If _advertisementList.Contains(_curAdvertisement) Then Return
                    SendPacket(Protocol.MakeCloseGame3())
                    If _advertiseRefreshTicker IsNot Nothing Then
                        _advertiseRefreshTicker.Dispose()
                        _advertiseRefreshTicker = Nothing
                    End If
                    _curAdvertisement = Nothing
                    EnterChannel(_lastChannel)
            End Select
        End Sub

        Private Function AddAdvertisableGame(ByVal gameDescription As WC3.LocalGameDescription, ByVal isPrivate As Boolean) As IFuture(Of WC3.LocalGameDescription)
            Contract.Requires(gameDescription IsNot Nothing)
            Dim entry = (From e In _advertisementList Where e.BaseGameDescription.Equals(gameDescription)).FirstOrDefault
            If entry Is Nothing Then
                entry = New AdvertisementEntry(gameDescription, isPrivate)
                _advertisementList.Add(entry)
                SyncAdvertisements()
            End If
            Return entry.FutureDescription
        End Function
        Public Function QueueAddAdvertisableGame(ByVal gameDescription As WC3.LocalGameDescription,
                                                 ByVal isPrivate As Boolean) As IFuture(Of WC3.LocalGameDescription)
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AddAdvertisableGame(gameDescription, isPrivate)).Defuturized
        End Function

        Private Sub RemoveAdvertisableGame(ByVal gameDescription As WC3.LocalGameDescription)
            For Each entry In From e In _advertisementList.ToList Where e.BaseGameDescription.Equals(gameDescription)
                entry.SetRemoved()
                _advertisementList.Remove(entry)
            Next entry
            SyncAdvertisements()
        End Sub
        Public Function QueueRemoveAdvertisableGame(ByVal gameDescription As WC3.LocalGameDescription) As IFuture
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() RemoveAdvertisableGame(gameDescription))
        End Function

        Private Sub RemoveAllAdvertisableGames()
            _advertisementList.Clear()
            SyncAdvertisements()
        End Sub
        Public Function QueueRemoveAllAdvertisableGames() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() RemoveAllAdvertisableGames())
        End Function
#End Region

        Private Sub SendPacket(ByVal packet As Protocol.Packet)
            Contract.Requires(Me._state > ClientState.InitiatingConnection)
            Contract.Requires(packet IsNot Nothing)

            Try
                Logger.Log(Function() "Sending {0} to {1}".Frmt(packet.Id, _socket.Name), LogMessageType.DataEvent)
                Logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(packet.Id, _socket.Name, packet.Payload.Description.Value), LogMessageType.DataParsed)

                _socket.WritePacket(Concat({Protocol.Packets.PacketPrefixValue, packet.Id, 0, 0}, packet.Payload.Data.ToArray))
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse
                                       TypeOf ex Is InvalidOperationException OrElse
                                       TypeOf ex Is ObjectDisposedException OrElse
                                       TypeOf ex Is Net.Sockets.SocketException
                Disconnect(expected:=False, reason:="Error sending {0} to {1}: {2}".Frmt(packet.Id, _socket.Name, ex.Message))
                ex.RaiseAsUnexpected("Error sending {0} to {1}".Frmt(packet.Id, _socket.Name))
            End Try
        End Sub
        Public Function QueueSendPacket(ByVal packet As Protocol.Packet) As IFuture
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendPacket(packet))
        End Function

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
            _productAuthenticator.AsyncAuthenticate(clientCdKeySalt.Bytes, serverCdKeySalt.Bytes).QueueCallOnValueSuccess(inQueue,
                Sub(keys) EnterKeys(keys:=keys,
                                    revisionCheckSeed:=CStr(vals("revision check seed")),
                                    revisionCheckInstructions:=CStr(vals("revision check challenge")),
                                    clientCdKeySalt:=clientCdKeySalt)
            ).Catch(
                Sub(exception)
                    exception.RaiseAsUnexpected("Error Handling {0}".Frmt(Protocol.PacketId.ProgramAuthenticationBegin))
                    QueueDisconnect(expected:=False, reason:="Error handling {0}: {1}".Frmt(Protocol.PacketId.ProgramAuthenticationBegin, exception.Message))
                End Sub
            )
        End Sub
        Private Sub EnterKeys(ByVal keys As ProductCredentialPair,
                              ByVal revisionCheckSeed As String,
                              ByVal revisionCheckInstructions As String,
                              ByVal clientCdKeySalt As UInt32)
            Contract.Requires(keys IsNot Nothing)
            Contract.Requires(revisionCheckSeed IsNot Nothing)
            Contract.Requires(revisionCheckInstructions IsNot Nothing)
            If _state <> ClientState.EnterCDKeys Then Throw New InvalidStateException("Incorrect state for entering cd keys.")
            Dim revisionCheckResponse As UInt32
            Try
                revisionCheckResponse = _externalProvider.GenerateRevisionCheck(
                    folder:=My.Settings.war3path,
                    challengeSeed:=revisionCheckSeed,
                    challengeInstructions:=revisionCheckInstructions)
            Catch ex As ArgumentException
                If revisionCheckInstructions = "" Then
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
        Private Sub ReceiveWarden(ByVal pickle As IPickle(Of IReadableList(Of Byte)))
            Contract.Requires(pickle IsNot Nothing)
            If _state < ClientState.WaitingForEnterChat Then Throw New IO.InvalidDataException("Warden packet in unexpected place.")
            Dim encryptedData = pickle.Value
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
        Private Sub ReceiveCreateGame3(ByVal pickle As IPickle(Of UInt32))
            Contract.Requires(pickle IsNot Nothing)
            Dim result = pickle.Value

            Select Case _state
                Case ClientState.AdvertisingGame
                    If result = 0 Then
                        'Refresh succeeded
                        outQueue.QueueAction(Sub() RaiseEvent AdvertisedGame(Me, _curAdvertisement.UpdatedGameDescription(_clock), _curAdvertisement.IsPrivate, True))
                    Else
                        'Refresh failed (No idea why it happened, better return to channel and try again)
                        If _advertiseRefreshTicker IsNot Nothing Then
                            _advertiseRefreshTicker.Dispose()
                            _advertiseRefreshTicker = Nothing
                        End If
                        _curAdvertisement = Nothing
                        EnterChannel(_lastChannel)
                    End If
                Case ClientState.CreatingGame
                    If result = 0 Then
                        'Initial advertisement succeeded, start refreshing
                        ChangeState(ClientState.AdvertisingGame)
                        If Not _curAdvertisement.IsPrivate Then
                            _advertiseRefreshTicker = _clock.AsyncRepeat(RefreshPeriod, Sub() inQueue.QueueAction(
                                Sub()
                                    If _state <> ClientState.AdvertisingGame Then Return
                                    SendPacket(Protocol.MakeCreateGame3(_curAdvertisement.UpdatedGameDescription(_clock)))
                                End Sub))
                        End If

                        _curAdvertisement.SetNameSucceeded()
                        outQueue.QueueAction(Sub() RaiseEvent AdvertisedGame(Me, _curAdvertisement.UpdatedGameDescription(_clock), _curAdvertisement.IsPrivate, False))
                    Else
                        'Initial advertisement failed, probably because of game name in use, try again with a new name
                        _curAdvertisement.SetNameFailed()
                        SendPacket(Protocol.MakeCreateGame3(_curAdvertisement.UpdatedGameDescription(_clock)))
                    End If
            End Select
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
