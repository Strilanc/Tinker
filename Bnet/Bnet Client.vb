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

    Public NotInheritable Class Client
        Inherits FutureDisposable

        Public Shared ReadOnly BnetServerPort As UShort = 6112
        Private Shared ReadOnly RefreshPeriod As TimeSpan = 20.Seconds

        Private ReadOnly outQueue As ICallQueue
        Private ReadOnly inQueue As ICallQueue

        Private ReadOnly _profile As Bot.ClientProfile
        Private ReadOnly _logger As Logger
        Private ReadOnly _packetHandler As BnetPacketHandler
        Private _socket As PacketSocket
        Private _wardenClient As Warden.Client

        'game
        Private _advertisedGameDescription As WC3.LocalGameDescription
        Private _advertisedPrivate As Boolean
        Private ReadOnly _advertiseRefreshTimer As New Timers.Timer(RefreshPeriod.TotalMilliseconds)
        Private _futureAdvertisedGame As New FutureAction
        Private _reportedListenPort As UShort

        'connection
        Private _bnetRemoteHostName As String
        Private _userCredentials As ClientCredentials
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
            Contract.Invariant(_profile IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_futureLoggedIn IsNot Nothing)
            Contract.Invariant(_futureConnected IsNot Nothing)
            Contract.Invariant(_futureAdvertisedGame IsNot Nothing)
            Contract.Invariant(_advertiseRefreshTimer IsNot Nothing)
            Contract.Invariant((_socket IsNot Nothing) = (_state > ClientState.InitiatingConnection))
            Contract.Invariant((_wardenClient IsNot Nothing) = (_state > ClientState.WaitingForProgramAuthenticationBegin))
            Contract.Invariant((_state <= ClientState.EnterUserCredentials) OrElse (_userCredentials IsNot Nothing))
            Contract.Invariant((_advertisedGameDescription IsNot Nothing) = (_state >= ClientState.CreatingGame))
        End Sub

        Public Sub New(ByVal profile As Bot.ClientProfile,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(profile IsNot Nothing)
            Me._futureConnected.SetHandled()
            Me._futureLoggedIn.SetHandled()
            Me._futureAdvertisedGame.SetHandled()

            'Pass values
            Me._profile = profile
            Me._logger = If(logger, New Logger)
            Me.outQueue = New TaskedCallQueue
            Me.inQueue = New TaskedCallQueue
            AddHandler _advertiseRefreshTimer.Elapsed, Sub() OnRefreshTimerTick()

            'Start packet machinery
            Me._packetHandler = New BnetPacketHandler(Me._logger)

            AddQueuedPacketHandler(Packet.ServerPackets.ProgramAuthenticationBegin, AddressOf ReceiveProgramAuthenticationBegin)
            AddQueuedPacketHandler(Packet.ServerPackets.ProgramAuthenticationFinish, AddressOf ReceiveProgramAuthenticationFinish)
            AddQueuedPacketHandler(Packet.ServerPackets.UserAuthenticationBegin, AddressOf ReceiveUserAuthenticationBegin)
            AddQueuedPacketHandler(Packet.ServerPackets.UserAuthenticationFinish, AddressOf ReceiveUserAuthenticationFinish)
            AddQueuedPacketHandler(Packet.ServerPackets.ChatEvent, AddressOf ReceiveChatEvent)
            AddQueuedPacketHandler(Packet.ServerPackets.EnterChat, AddressOf ReceiveEnterChat)
            AddQueuedPacketHandler(Packet.ServerPackets.MessageBox, AddressOf ReceiveMessageBox)
            AddQueuedPacketHandler(Packet.ServerPackets.CreateGame3, AddressOf ReceiveCreateGame3)
            AddQueuedPacketHandler(Packet.ServerPackets.Warden, AddressOf ReceiveWarden)
            AddQueuedPacketHandler(PacketId.Ping, Packet.ServerPackets.Ping, AddressOf ReceivePing)
            AddQueuedPacketHandler(PacketId.Null, Packet.ServerPackets.Null, AddressOf IgnorePacket)

            AddQueuedPacketHandler(PacketId.QueryGamesList, Packet.ServerPackets.QueryGamesList, AddressOf IgnorePacket)
            AddQueuedPacketHandler(Packet.ServerPackets.FriendsUpdate, AddressOf IgnorePacket)
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

        Private Function AddQueuedPacketHandler(ByVal jar As Packet.DefJar,
                                                ByVal handler As Action(Of IPickle(Of Dictionary(Of InvariantString, Object)))) As IDisposable
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return AddQueuedPacketHandler(jar.id, jar, handler)
        End Function
        Private Function AddQueuedPacketHandler(Of T)(ByVal id As PacketId,
                                                      ByVal jar As IParseJar(Of T),
                                                      ByVal handler As Action(Of IPickle(Of T))) As IDisposable
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            _packetHandler.AddLogger(id, jar.Weaken)
            Return _packetHandler.AddHandler(id, Function(data) inQueue.QueueAction(Sub() handler(jar.Parse(data))))
        End Function
        Public Function QueueAddPacketHandler(Of T)(ByVal id As PacketId,
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

            Dim lines = SplitText(text, maxLineLength:=Packet.MaxChatCommandTextLength)
            If isBnetCommand AndAlso lines.Count > 1 Then
                Throw New InvalidOperationException("Can't send multi-line commands or commands larger than {0} characters.".Frmt(Packet.MaxChatCommandTextLength))
            End If
            For Each line In lines
                Contract.Assume(line IsNot Nothing)
                If line.Length = 0 Then Continue For
                SendPacket(Bnet.Packet.MakeChatCommand(line))
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
            If prefix.Length >= Bnet.Packet.MaxChatCommandTextLength \ 2 Then
                Throw New ArgumentOutOfRangeException("username", "Username is too long.")
            End If

            For Each line In SplitText(text, maxLineLength:=Bnet.Packet.MaxChatCommandTextLength - prefix.Length)
                Contract.Assume(line IsNot Nothing)
                SendPacket(Bnet.Packet.MakeChatCommand(prefix + line))
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
            SendPacket(Bnet.Packet.MakeNetGamePort(Me._reportedListenPort))
        End Sub

        Private Sub OnSocketDisconnected(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String)
            inQueue.QueueAction(Sub() Disconnect(expected, reason))
        End Sub

        Private Sub OnRefreshTimerTick()
            inQueue.QueueAction(Sub() AdvertiseGame(False, True))
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
                Disconnect(expected:=False, reason:="Failed to start connection: {0}.".Frmt(e))
                Throw
            End Try

            Dim result = AsyncTcpConnect(remoteHost, port).QueueEvalOnValueSuccess(inQueue,
                Function(tcpClient)
                    Me._socket = New PacketSocket(
                            stream:=New ThrottledWriteStream(
                                        substream:=tcpClient.GetStream,
                                        initialSlack:=1000,
                                        costEstimator:=Function(data) 100 + data.Length,
                                        costLimit:=400,
                                        costRecoveredPerMillisecond:=0.048),
                            localendpoint:=CType(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                            remoteendpoint:=CType(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                            timeout:=60.Seconds,
                            Logger:=Logger,
                            bufferSize:=PacketSocket.DefaultBufferSize * 10)
                    AddHandler Me._socket.Disconnected, AddressOf OnSocketDisconnected
                    Me._socket.Name = "BNET"
                    ChangeState(ClientState.WaitingForProgramAuthenticationBegin)

                    'Reset the class future for the connection outcome
                    Me._futureConnected.TrySetFailed(New InvalidStateException("Another connection was initiated."))
                    Me._futureConnected = New FutureAction
                    Me._futureConnected.SetHandled()

                    'Introductions
                    tcpClient.GetStream.Write({1}, 0, 1) 'protocol version
                    SendPacket(Bnet.Packet.MakeAuthenticationBegin(GetWC3MajorVersion, New Net.IPAddress(GetCachedIPAddressBytes(external:=False))))

                    BeginHandlingPackets()
                    Return Me._futureConnected
                End Function
            ).Defuturized
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

        Private Sub BeginHandlingPackets()
            Contract.Requires(Me._state > ClientState.InitiatingConnection)
            AsyncProduceConsumeUntilError(
                producer:=AddressOf _socket.AsyncReadPacket,
                consumer:=AddressOf ProcessPacket,
                errorHandler:=Sub(exception) QueueDisconnect(expected:=False,
                                                             reason:="Error receiving packet: {0}.".Frmt(exception.Message))
            )
        End Sub
        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Private Function ProcessPacket(ByVal packetData As IReadableList(Of Byte)) As ifuture
            Contract.Requires(packetData IsNot Nothing)
            Contract.Requires(packetData.Count >= 4)
            Dim result = Me._packetHandler.HandlePacket(packetData, _socket.Name)
            result.Catch(Sub(exception) QueueDisconnect(expected:=False,
                                                        reason:="Error handling packet {0}: {1}.".Frmt(CType(packetData(1), PacketId), exception.Message)))
            Return result
        End Function

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
            SendPacket(Bnet.Packet.MakeAccountLogOnBegin(credentials))
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
                _socket.Disconnect(expected, reason)
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
                Call 5.Seconds.AsyncWait.CallWhenReady(
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
            SendPacket(Bnet.Packet.MakeJoinChannel(Bnet.Packet.JoinChannelType.ForcedJoin, channel))
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
                    Throw New InvalidOperationException("Already creating a game.")
                Case ClientState.AdvertisingGame
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
                    Catch e As Exception
                        _futureAdvertisedGame.TrySetFailed(New OperationFailedException("Failed to send data."))
                        ChangeState(ClientState.Channel)
                        Throw
                    End Try

                    'outQueue.QueueAction(Sub() RaiseEvent AddedGame(Me, gameDescription, server))
                    'If server IsNot Nothing Then
                    'server.QueueAddAdvertiser(Me).SetHandled()
                    'DisposeLink.CreateOneWayLink(New AdvertisingDisposeNotifier(Me), server.CreateAdvertisingDependency)
                    ''server.QueueOpenPort(Me.listenPort).QueueCallWhenReady(inQueue,
                    ''Sub(listenException)
                    ''If listenException IsNot Nothing Then
                    ''_futureCreatedGame.TrySetFailed(listenException)
                    ''Contract.Assume(listenException.Message IsNot Nothing)
                    ''StopAdvertisingGame(reason:=listenException.Message)
                    ''End If
                    ''End Sub
                    '').MarkAnyExceptionAsHandled()
                    'End If
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

            Dim gameState = Bnet.Packet.GameStates.Unknown0x10
            If _advertisedPrivate Then gameState = gameState Or Bnet.Packet.GameStates.Private
            If useFull Then
                gameState = gameState Or Bnet.Packet.GameStates.Full
            Else
                gameState = gameState And Not Bnet.Packet.GameStates.Full
            End If
            'If in_progress Then gameState = gameState Or BnetPacket.GameStateFlags.InProgress
            'If Not empty Then game_state_flags = game_state_flags Or FLAG_NOT_EMPTY [causes problems: why?]

            Dim gameType = WC3.GameTypes.CreateGameUnknown0 Or Me._advertisedGameDescription.GameType
            If _advertisedPrivate Then
                gameType = gameType Or WC3.GameTypes.PrivateGame
            Else
                gameType = gameType And Not WC3.GameTypes.PrivateGame
            End If
            Select Case Me._advertisedGameDescription.GameStats.observers
                Case WC3.GameObserverOption.FullObservers, WC3.GameObserverOption.Referees
                    gameType = gameType Or WC3.GameTypes.ObsFull
                Case WC3.GameObserverOption.ObsOnDefeat
                    gameType = gameType Or WC3.GameTypes.ObsOnDeath
                Case WC3.GameObserverOption.NoObservers
                    gameType = gameType Or WC3.GameTypes.ObsNone
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
                    baseAgeMilliSeconds:=_advertisedGameDescription.AgeMilliseconds)
            SendPacket(Bnet.Packet.MakeCreateGame3(_advertisedGameDescription))
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
                    SendPacket(Bnet.Packet.MakeCloseGame3())
                    _advertiseRefreshTimer.Stop()
                    EnterChannel(_lastChannel)
                    _advertisedGameDescription = Nothing
                    _futureAdvertisedGame.TrySetFailed(New OperationFailedException("Advertising cancelled."))
                    'outQueue.QueueAction(Sub() RaiseEvent RemovedGame(Me, _advertisedGameDescription, reason))

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

        '#Region "Link"
        'Private Event Disconnected(ByVal sender As Client, ByVal reason As String)
        'Private ReadOnly userLinkMap As New Dictionary(Of BotUser, ClientServerUserLink)

        'Private Function GetUserServer(ByVal user As BotUser) As WC3.GameServer
        'If user Is Nothing Then Return Nothing
        'If Not userLinkMap.ContainsKey(user) Then Return Nothing
        'Return userLinkMap(user).server
        'End Function
        'Private Sub SetUserServer(ByVal user As BotUser, ByVal server As WC3.GameServer)
        'If user Is Nothing Then Return
        'If userLinkMap.ContainsKey(user) Then
        'Dim link = userLinkMap(user)
        'Contract.Assume(link IsNot Nothing)
        'link.Dispose()
        'userLinkMap.Remove(user)
        'End If
        'If server Is Nothing Then Return
        'userLinkMap(user) = New ClientServerUserLink(Me, server, user)
        'End Sub

        'Private NotInheritable Class ClientServerUserLink
        'Inherits FutureDisposable
        'Public ReadOnly client As Client
        'Public ReadOnly server As WC3.GameServer
        'Public ReadOnly user As BotUser

        'Public Sub New(ByVal client As Client, ByVal server As WC3.GameServer, ByVal user As BotUser)
        ''contract bug wrt interface event implementation requires this:
        ''Contract.Requires(client IsNot Nothing)
        ''Contract.Requires(server IsNot Nothing)
        ''Contract.Requires(user IsNot Nothing)
        'Contract.Assume(client IsNot Nothing)
        'Contract.Assume(server IsNot Nothing)
        'Contract.Assume(user IsNot Nothing)
        'Me.client = client
        'Me.server = server
        'Me.user = user
        'DisposeLink.CreateOneWayLink(client, Me)
        'DisposeLink.CreateOneWayLink(server, Me)
        'End Sub

        'Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
        'If finalizing Then Return Nothing
        'Return client.QueueSetUserServer(user, Nothing)
        'End Function
        'End Class
        '#End Region

#Region "Networking (Send)"
        Private Sub SendPacket(ByVal packet As Bnet.Packet)
            Contract.Requires(Me._state > ClientState.InitiatingConnection)
            Contract.Requires(packet IsNot Nothing)

            Try
                Logger.Log(Function() "Sending {0} to {1}".Frmt(packet.Id, _socket.Name), LogMessageType.DataEvent)
                Logger.Log(packet.Payload.Description, LogMessageType.DataParsed)

                _socket.WritePacket(Concat({Bnet.Packet.PacketPrefixValue, packet.Id, 0, 0}, packet.Payload.Data.ToArray))
            Catch e As Exception
                Disconnect(expected:=False, reason:="Error sending {0} to {1}: {2}".Frmt(packet.Id, _socket.Name, e))
            End Try
        End Sub
        Public Function QueueSendPacket(ByVal packet As Bnet.Packet) As IFuture
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
                Throw New IO.InvalidDataException("Invalid state for receiving {0}".Frmt(PacketId.ProgramAuthenticationBegin))
            End If
            ChangeState(ClientState.EnterCDKeys)

            'Check
            If CType(vals("logon type"), UInteger) <> LOGON_TYPE_WC3 Then
                _futureConnected.TrySetFailed(New IO.InvalidDataException("Failed to connect: Unrecognized logon type from server."))
                Throw New IO.InvalidDataException("Unrecognized logon type")
            End If

            'Salts
            Dim serverCdKeySalt = CUInt(vals("server cd key salt"))
            Dim clientCdKeySalt As UInt32
            Using rng = New System.Security.Cryptography.RNGCryptoServiceProvider()
                Dim clientKeySaltBytes(0 To 3) As Byte
                rng.GetBytes(clientKeySaltBytes)
                clientCdKeySalt = clientKeySaltBytes.ToUInt32
            End Using

            'Pack or borrow CD Keys
            Dim futureKeys As IFuture(Of CKL.WC3CredentialPair)
            If Profile.CKLServerAddress Like "*:#*" Then
                Dim remoteHost = Profile.CKLServerAddress.Split(":"c)(0)
                Dim port = UShort.Parse(Profile.CKLServerAddress.Split(":"c)(1).AssumeNotNull, CultureInfo.InvariantCulture)
                futureKeys = CKL.Client.AsyncBorrowCredentials(remoteHost, port, clientCdKeySalt, serverCdKeySalt)
                futureKeys.CallOnSuccess(
                    Sub() Logger.Log("Succesfully borrowed keys from CKL server.", LogMessageType.Positive)
                ).Catch(
                    Sub(exception) Disconnect(expected:=False, reason:="Error borrowing keys: {0}".Frmt(exception.Message))
                )
            Else
                Dim roc = Profile.cdKeyROC.ToWC3CDKeyCredentials(clientCdKeySalt.Bytes, serverCdKeySalt.Bytes)
                Dim tft = Profile.cdKeyTFT.ToWC3CDKeyCredentials(clientCdKeySalt.Bytes, serverCdKeySalt.Bytes)
                If roc.Product <> ProductType.Warcraft3ROC Then Throw New IO.InvalidDataException("Invalid ROC cd key.")
                If tft.Product <> ProductType.Warcraft3TFT Then Throw New IO.InvalidDataException("Invalid TFT cd key.")
                futureKeys = New CKL.WC3CredentialPair(roc, tft).Futurized
            End If

            'Respond and begin BNLS connection
            futureKeys.CallOnValueSuccess(
                Sub(keys)
                    If _state <> ClientState.EnterCDKeys Then Throw New InvalidStateException("Incorrect state for entering cd keys.")
                    Dim revisionCheckResponse = GenerateRevisionCheck(
                                    folder:=My.Settings.war3path,
                                    seedString:=CStr(vals("revision check seed")),
                                    challengeString:=CStr(vals("revision check challenge")))
                    SendPacket(Bnet.Packet.MakeAuthenticationFinish(
                                    version:=GetWC3ExeVersion,
                                    revisionCheckResponse:=revisionCheckResponse,
                                    clientCDKeySalt:=clientCdKeySalt,
                                    serverCDKeySalt:=serverCdKeySalt,
                                    cdKeyOwner:=My.Settings.cdKeyOwner,
                                    exeInformation:="war3.exe {0} {1}".Frmt(GetWC3LastModifiedTime.ToString("MM/dd/yy hh:mm:ss"), GetWC3FileSize),
                                    productAuthentication:=keys))

                    ChangeState(ClientState.WaitingForProgramAuthenticationFinish)

                    'Parse address setting
                    Dim remoteHost = ""
                    Dim remotePort = 0US
                    Dim address = My.Settings.bnls
                    If address Is Nothing OrElse address = "" Then
                        Logger.Log("No bnls server is specified. Battle.net will most likely disconnect the bot after two minutes.", LogMessageType.Problem)
                    Else
                        Dim hostPortPair = address.Split(":"c)
                        remoteHost = hostPortPair(0)
                        If hostPortPair.Length <> 2 OrElse Not UShort.TryParse(hostPortPair(1), remotePort) Then
                            Logger.Log("Invalid bnls server format specified. Expected hostname:port.", LogMessageType.Problem)
                        End If
                    End If

                    'Attempt connection
                    Dim seed = keys.AuthenticationROC.AuthenticationProof.Take(4).ToUInt32
                    _wardenClient = New Warden.Client(remoteHost:=remoteHost, remotePort:=remotePort, seed:=seed, cookie:=seed, Logger:=Logger)
                End Sub
            ).Catch(
                Sub(exception)
                    QueueDisconnect(expected:=False, reason:="Error handling authentication begin: {0}".Frmt(exception.Message))
                End Sub
            )
        End Sub

        Private Sub ReceiveProgramAuthenticationFinish(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value
            If _state <> ClientState.WaitingForProgramAuthenticationFinish Then
                Throw New IO.InvalidDataException("Invalid state for receiving {0}: {1}".Frmt(PacketId.ProgramAuthenticationFinish, _state))
            End If

            Dim result = CType(CUInt(vals("result")), Bnet.Packet.ProgramAuthenticationFinishResult)
            Dim errmsg As String
            Select Case result
                Case Bnet.Packet.ProgramAuthenticationFinishResult.Passed
                    ChangeState(ClientState.EnterUserCredentials)
                    _futureConnected.TrySetSucceeded()
                    _allowRetryConnect = True
                    Return

                Case Bnet.Packet.ProgramAuthenticationFinishResult.OldVersion
                    errmsg = "Out of date version"
                Case Bnet.Packet.ProgramAuthenticationFinishResult.InvalidVersion
                    errmsg = "Invalid version"
                Case Bnet.Packet.ProgramAuthenticationFinishResult.FutureVersion
                    errmsg = "Future version (need to downgrade apparently)"
                Case Bnet.Packet.ProgramAuthenticationFinishResult.InvalidCDKey
                    errmsg = "Invalid CD key"
                Case Bnet.Packet.ProgramAuthenticationFinishResult.UsedCDKey
                    errmsg = "CD key in use by:"
                Case Bnet.Packet.ProgramAuthenticationFinishResult.BannedCDKey
                    errmsg = "CD key banned!"
                Case Bnet.Packet.ProgramAuthenticationFinishResult.WrongProduct
                    errmsg = "Wrong product."
                Case Else
                    errmsg = "Unknown authentication failure id: {0}.".Frmt(result)
            End Select

            _futureConnected.TrySetFailed(New IO.IOException("Failed to connect: {0} {1}".Frmt(errmsg, vals("info"))))
            Throw New Exception(errmsg)
        End Sub

        Private Sub ReceiveUserAuthenticationBegin(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value

            If _state <> ClientState.WaitingForUserAuthenticationBegin Then
                Throw New Exception("Invalid state for receiving {0}".Frmt(PacketId.UserAuthenticationBegin))
            End If

            Dim result = CType(vals("result"), Bnet.Packet.UserAuthenticationBeginResult)
            If result <> Bnet.Packet.UserAuthenticationBeginResult.Passed Then
                Dim errmsg As String
                Select Case result
                    Case Bnet.Packet.UserAuthenticationBeginResult.BadUserName
                        errmsg = "Username doesn't exist."
                    Case Bnet.Packet.UserAuthenticationBeginResult.UpgradeAccount
                        errmsg = "Account requires upgrade."
                    Case Else
                        errmsg = "Unrecognized login problem: " + result.ToString()
                End Select
                _futureLoggedIn.TrySetFailed(New IO.IOException("Failed to login: " + errmsg))
                Throw New Exception(errmsg)
            End If

            Dim accountPasswordSalt = CType(vals("account password salt"), IReadableList(Of Byte)).AssumeNotNull
            Dim serverPublicKey = CType(vals("server public key"), IReadableList(Of Byte)).AssumeNotNull

            If Me._userCredentials Is Nothing Then Throw New InvalidStateException("Received AccountLogOnBegin before credentials specified.")
            Dim clientProof = Me._userCredentials.ClientPasswordProof(accountPasswordSalt, serverPublicKey)
            Dim serverProof = Me._userCredentials.ServerPasswordProof(accountPasswordSalt, serverPublicKey)

            Me._expectedServerPasswordProof = serverProof
            ChangeState(ClientState.WaitingForUserAuthenticationFinish)
            SendPacket(Bnet.Packet.MakeAccountLogOnFinish(clientProof))
        End Sub

        Private Sub ReceiveUserAuthenticationFinish(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value
            If _state <> ClientState.WaitingForUserAuthenticationFinish Then
                Throw New Exception("Invalid state for receiving {0}: {1}".Frmt(PacketId.UserAuthenticationFinish, _state))
            End If

            Dim result = CType(vals("result"), Bnet.Packet.UserAuthenticationFinishResult)

            If result <> Bnet.Packet.UserAuthenticationFinishResult.Passed Then
                Dim errmsg As String
                Select Case result
                    Case Bnet.Packet.UserAuthenticationFinishResult.IncorrectPassword
                        errmsg = "Incorrect password."
                    Case Bnet.Packet.UserAuthenticationFinishResult.NeedEmail
                        errmsg = "No email address associated with account"
                    Case Bnet.Packet.UserAuthenticationFinishResult.CustomError
                        errmsg = "Logon error: " + CType(vals("custom error info"), String)
                    Case Else
                        errmsg = "Unrecognized logon error: " + result.ToString()
                End Select
                _futureLoggedIn.TrySetFailed(New IO.IOException("Failed to logon: " + errmsg))
                Throw New Exception(errmsg)
            End If

            'validate
            Dim serverProof = CType(vals("server password proof"), IReadableList(Of Byte)).AssumeNotNull
            If Me._expectedServerPasswordProof Is Nothing Then Throw New InvalidStateException("Received AccountLogOnFinish before server password proof computed.")
            If Not Me._expectedServerPasswordProof.SequenceEqual(serverProof) Then
                _futureLoggedIn.TrySetFailed(New IO.InvalidDataException("Failed to logon: Server didn't give correct password proof"))
                Throw New IO.InvalidDataException("Server didn't give correct password proof.")
            End If
            'Dim lan_host = profile.LanHost.Split(" "c)(0)
            'If lan_host <> "" Then
            'Try
            'Dim lan = New WC3.LanAdvertiser(Parent, Name, lan_host)
            'Parent.QueueAddWidget(lan)
            'DisposeLink.CreateOneWayLink(Me, lan)
            'AdvertisingLink.CreateMultiWayLink({Me, lan.MakeAdvertisingLinkMember})
            'Catch e As Exception
            'logger.Log("Error creating lan advertiser: {0}".Frmt(e.ToString), LogMessageType.Problem)
            'End Try
            'End If
            'log
            ChangeState(ClientState.WaitingForEnterChat)
            Logger.Log("Logged on with username {0}.".Frmt(Me._userCredentials.UserName), LogMessageType.Typical)
            _futureLoggedIn.TrySetSucceeded()
            'respond
            SetReportedListenPort(6112)
            SendPacket(Bnet.Packet.MakeEnterChat())
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
        Private Sub OnWardenSend(ByVal data As IReadableList(Of Byte))
            Contract.Requires(data IsNot Nothing)
            inQueue.QueueAction(Sub() SendPacket(Bnet.Packet.MakeWarden(data)))
        End Sub
        Private Sub OnWardenFail(ByVal exception As Exception)
            Contract.Requires(exception IsNot Nothing)
            QueueDisconnect(expected:=False, reason:="Warden/BNLS Error: {0}.".Frmt(exception.Message))
        End Sub
#End Region

#Region "Networking (Games)"
        Private Sub ReceiveCreateGame3(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value
            Dim succeeded = CUInt(vals("result")) = 0

            If succeeded Then
                If _state = ClientState.CreatingGame Then
                    Logger.Log("Finished creating game.", LogMessageType.Positive)
                    ChangeState(ClientState.AdvertisingGame)
                    If Not _advertisedPrivate Then _advertiseRefreshTimer.Start()
                    _futureAdvertisedGame.TrySetSucceeded()
                Else
                    ChangeState(ClientState.AdvertisingGame) 'throw event
                End If
            Else
                _futureAdvertisedGame.TrySetFailed(New OperationFailedException("BNET didn't allow game creation. Most likely cause is game name in use."))
                _advertiseRefreshTimer.Stop()
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
            Dim eventId = CType(vals("event id"), Bnet.Packet.ChatEventId)
            Dim text = CStr(vals("text"))
            If eventId = Bnet.Packet.ChatEventId.Channel Then _lastChannel = text
        End Sub

        Private Sub ReceivePing(ByVal value As IPickle(Of UInt32))
            Contract.Requires(value IsNot Nothing)
            SendPacket(Bnet.Packet.MakePing(salt:=value.Value))
        End Sub

        Private Sub ReceiveMessageBox(ByVal value As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(value IsNot Nothing)
            Dim vals = value.Value
            Dim msg = "MESSAGE BOX FROM BNET: " + CStr(vals("caption")) + ": " + CStr(vals("text"))
            Logger.Log(msg, LogMessageType.Problem)
        End Sub
#End Region

        Public Function QueueGetUserServer(ByVal user As BotUser) As IFuture(Of WC3.GameServer)
            Contract.Ensures(Contract.Result(Of IFuture(Of WC3.GameServer))() IsNot Nothing)
            'Return inQueue.QueueFunc(Function() GetUserServer(user))
            Throw New NotImplementedException
        End Function
        Public Function QueueSetUserServer(ByVal user As BotUser, ByVal server As WC3.GameServer) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            'Return inQueue.QueueAction(Sub() SetUserServer(user, server))
            Throw New NotImplementedException
        End Function
    End Class
End Namespace
