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

Imports HostBot.Warcraft3
Imports HostBot.Links
Imports System.Net
Imports System.Net.Sockets

Namespace Bnet
    Public Enum BnetClientState
        Disconnected
        Connecting
        EnterUserName
        LogOn
        Channel
        CreatingGame
        Game
    End Enum

    Public NotInheritable Class BnetClient
        Inherits FutureDisposable
        Implements IGameSourceSink

#Region "Inner"
        Public NotInheritable Class GameSettings
            Public [private] As Boolean
            Private ReadOnly _header As W3GameDescription
            Public ReadOnly Property Header As W3GameDescription
                Get
                    Contract.Ensures(Contract.Result(Of W3GameDescription)() IsNot Nothing)
                    Return _header
                End Get
            End Property

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_header IsNot Nothing)
            End Sub

            Public Sub New(ByVal header As W3GameDescription)
                Contract.Requires(header IsNot Nothing)
                Me.private = [private]
                Me._header = header
                For Each arg In header.Options
                    Contract.Assume(arg IsNot Nothing)
                    Select Case arg.ToUpperInvariant.Trim()
                        Case "-P", "-PRIVATE"
                            Me.private = True
                    End Select
                Next arg
            End Sub
        End Class
#End Region

#Region "Variables"
        Private Const BNET_PORT As UShort = 6112
        Private Const REFRESH_PERIOD As Integer = 20000

        Public ReadOnly profile As ClientProfile
        Private ReadOnly _parent As MainBot
        Private ReadOnly _name As String = "unnamed_client"
        Public ReadOnly logger As Logger
        Private socket As BnetSocket

        'refs
        Private ReadOnly eref As ICallQueue
        Private ReadOnly ref As ICallQueue

        'packets
        Private ReadOnly packetHandlers(0 To 255) As Action(Of Dictionary(Of String, Object))

        'game
        Private advertisedGameSettings As GameSettings
        Private hostCount As Integer
        Private ReadOnly gameRefreshTimer As New Timers.Timer(REFRESH_PERIOD)

        'crypto
        Private ReadOnly clientPrivateKey As System.Numerics.BigInteger
        Private ReadOnly clientPublicKey As ViewableList(Of Byte)
        Private clientPasswordProof As ViewableList(Of Byte)
        Private serverPasswordProof As ViewableList(Of Byte)

        Private allowRetryConnect As Boolean

        'futures
        Private futureConnected As New FutureAction
        Private futureLoggedIn As New FutureAction
        Private futureCreatedGame As New FutureAction

        'events
        Public Event StateChanged(ByVal sender As BnetClient, ByVal oldState As BnetClientState, ByVal newState As BnetClientState)
        Public Event ReceivedPacket(ByVal sender As BnetClient, ByVal packet As BnetPacket)

        'warden
        Private futureWardenHandler As IFuture(Of BNLS.BNLSWardenClient)

        'state
        Private listenPort As UShort
        Private poolPort As PortPool.PortHandle
        Private lastChannel As String = ""
        Private accountUsername As String
        Private accountPassword As String
        Private hostname As String
        Private state As BnetClientState

        Public ReadOnly Property Name As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property
        Public ReadOnly Property Parent As MainBot
            Get
                Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                Return _parent
            End Get
        End Property
#End Region

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_name IsNot Nothing)
            Contract.Invariant(_parent IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eref IsNot Nothing)
            Contract.Invariant(clientPublicKey IsNot Nothing)
            Contract.Invariant(clientPrivateKey >= 0)
            Contract.Invariant(profile IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(futureLoggedIn IsNot Nothing)
            Contract.Invariant(futureConnected IsNot Nothing)
            Contract.Invariant(futureCreatedGame IsNot Nothing)
            Contract.Invariant(userLinkMap IsNot Nothing)
            Contract.Invariant(gameRefreshTimer IsNot Nothing)
            Contract.Invariant(hostCount >= 0)
        End Sub

#Region "New"
        Public Sub New(ByVal parent As MainBot,
                       ByVal profile As ClientProfile,
                       ByVal name As String,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(parent IsNot Nothing) 'bug in contracts required not using requires here
            Contract.Assume(profile IsNot Nothing)
            Contract.Assume(name IsNot Nothing)
            Me.futureConnected.MarkAnyExceptionAsHandled()
            Me.futureLoggedIn.MarkAnyExceptionAsHandled()
            Me.futureCreatedGame.MarkAnyExceptionAsHandled()

            'Pass values
            Me._name = name
            Me._parent = parent
            Me.profile = profile
            Me.listenPort = profile.listenPort
            Me.logger = If(logger, New Logger)
            Me.eref = New ThreadPooledCallQueue
            Me.ref = New ThreadPooledCallQueue
            AddHandler gameRefreshTimer.Elapsed, Sub() OnRefreshTimerTick()

            'Init crypto
            Using rng = New System.Security.Cryptography.RNGCryptoServiceProvider()
                With Bnet.GeneratePublicPrivateKeyPair(rng)
                    clientPublicKey = .Value1
                    clientPrivateKey = .Value2.ToUnsignedBigInteger
                End With
            End Using

            'Start packet machinery
            packetHandlers(Bnet.BnetPacketId.AuthenticationBegin) = AddressOf ReceiveAuthenticationBegin
            packetHandlers(Bnet.BnetPacketId.AuthenticationFinish) = AddressOf ReceiveAuthenticationFinish
            packetHandlers(Bnet.BnetPacketId.AccountLogOnBegin) = AddressOf ReceiveAccountLogonBegin
            packetHandlers(Bnet.BnetPacketId.AccountLogOnFinish) = AddressOf ReceiveAccountLogonFinish
            packetHandlers(Bnet.BnetPacketId.ChatEvent) = AddressOf ReceiveChatEvent
            packetHandlers(Bnet.BnetPacketId.EnterChat) = AddressOf ReceiveEnterChat
            packetHandlers(Bnet.BnetPacketId.Null) = AddressOf ReceiveNull
            packetHandlers(Bnet.BnetPacketId.Ping) = AddressOf ReceivePing
            packetHandlers(Bnet.BnetPacketId.MessageBox) = AddressOf ReceiveMessageBox
            packetHandlers(Bnet.BnetPacketId.CreateGame3) = AddressOf ReceiveCreateGame3
            packetHandlers(Bnet.BnetPacketId.Warden) = AddressOf ReceiveWarden

            packetHandlers(Bnet.BnetPacketId.QueryGamesList) = AddressOf IgnorePacket
            packetHandlers(Bnet.BnetPacketId.FriendsUpdate) = AddressOf IgnorePacket
        End Sub
#End Region

#Region "Access"
        Private Sub SendText(ByVal text As String)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(text.Length > 0)

            Select Case state
                Case BnetClientState.Channel
                    'fine
                Case BnetClientState.CreatingGame, BnetClientState.Game
                    If text(0) <> "/"c Then Throw New InvalidOperationException("Can only send commands when in games.")
                Case Else
                    Throw New InvalidOperationException("Can't send text unless you're logged in.")
            End Select

            SendPacket(BnetPacket.MakeChatCommand(text))
        End Sub

        Private Sub SendWhisper(ByVal username As String, ByVal text As String)
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(username.Length > 0)
            Contract.Requires(text.Length > 0)
            Dim message = "/w {0} {1}".Frmt(username, text)
            Contract.Assume(message.Length >= 6)
            SendText(message)
        End Sub

        Private Sub SetListenPort(ByVal newPort As UShort)
            If newPort = listenPort Then Return
            Select Case state
                Case BnetClientState.Channel, BnetClientState.Disconnected
                    If poolPort IsNot Nothing Then
                        poolPort.Dispose()
                        poolPort = Nothing
                        logger.Log("Returned port {0} to pool.".Frmt(Me.listenPort), LogMessageType.Positive)
                    End If
                    listenPort = newPort
                    logger.Log("Changed listen port to {0}.".Frmt(newPort), LogMessageType.Typical)
                    If state <> BnetClientState.Disconnected Then
                        SendPacket(BnetPacket.MakeNetGamePort(listenPort))
                    End If
                Case Else
                    Throw New InvalidOperationException("Can only change listen port when disconnected or in a channel.")
            End Select
        End Sub
#End Region

#Region "Events"
        Private Sub CatchSocketDisconnected(ByVal sender As BnetSocket, ByVal expected As Boolean, ByVal reason As String)
            ref.QueueAction(Sub()
                                Contract.Assume(reason IsNot Nothing)
                                Disconnect(expected, reason)
                            End Sub)
        End Sub

        Private Sub OnRefreshTimerTick()
            ref.QueueAction(Sub() AdvertiseGame(False, True))
        End Sub
#End Region

#Region "State"
        Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
            If Not finalizing Then
                ref.QueueAction(Sub() Disconnect(expected:=True, reason:="{0} Disposed".Frmt(Me.GetType.Name)))
                parent.QueueRemoveClient(Me.Name, expected:=True, reason:="Client Disposed")
            End If
        End Sub
        Private Sub ChangeState(ByVal newState As BnetClientState)
            Dim oldState = state
            state = newState
            eref.QueueAction(Sub() RaiseEvent StateChanged(Me, oldState, newState))
        End Sub
        Private Function BeginConnect(ByVal remoteHost As String) As IFuture
            Contract.Requires(remoteHost IsNot Nothing)
            Try
                If socket IsNot Nothing Then
                    Throw New InvalidOperationException("Client is already connected.")
                End If
                hostCount = 0
                hostname = remoteHost

                'Allocate port
                If Me.listenPort = 0 Then
                    Dim out = parent.PortPool.TryAcquireAnyPort()
                    If out Is Nothing Then
                        Throw New InvalidOperationException("No listen port specified, and no ports available in the pool.")
                    End If
                    Me.poolPort = out
                    Me.listenPort = Me.poolPort.Port
                    logger.Log("Took port {0} from pool.".Frmt(Me.listenPort), LogMessageType.Positive)
                End If

                'Establish connection
                logger.Log("Connecting to {0}...".Frmt(remoteHost), LogMessageType.Typical)
                Dim port = BNET_PORT
                If remoteHost Like "*:*" Then
                    Dim remotePortTemp = remoteHost.Split(":"c)(1)
                    Contract.Assume(remotePortTemp IsNot Nothing) 'remove once static verifier understands String.split
                    port = UShort.Parse(remotePortTemp, CultureInfo.InvariantCulture)
                    remoteHost = remoteHost.Split(":"c)(0)
                End If

                Return FutureCreateConnectedTcpClient(remoteHost, port).QueueEvalOnValueSuccess(ref,
                    Function(tcpClient)
                        Try
                            socket = New BnetSocket(New PacketSocket(
                                tcpClient,
                                60.Seconds,
                                logger,
                                Function(stream)
                                    Return New ThrottledWriteStream(stream,
                                                                    initialSlack:=1000,
                                                                    costPerWrite:=100,
                                                                    costPerCharacter:=1,
                                                                    costLimit:=480,
                                                                    costRecoveredPerSecond:=48)
                                End Function,
                                bufferSize:=PacketSocket.DefaultBufferSize * 10))
                                        AddHandler socket.Disconnected, AddressOf CatchSocketDisconnected
                                        socket.Name = "BNET"
                                        ChangeState(BnetClientState.Connecting)

                                        'Replace connection future
                                        Me.futureConnected.TrySetFailed(New InvalidStateException("Another connection was initiated."))
                                        Me.futureConnected = New FutureAction
                                        Me.futureConnected.MarkAnyExceptionAsHandled()

                                        'Start log-on process
                                        tcpClient.GetStream.Write({1}, 0, 1)
                                        SendPacket(BnetPacket.MakeAuthenticationBegin(MainBot.WC3MajorVersion, GetCachedIPAddressBytes(external:=False)))

                                        BeginHandlingPackets()

                                        Return Me.futureConnected
                                    Catch e As Exception
                                        Disconnect(expected:=False, reason:="Failed to complete connection: {0}.".Frmt(e))
                                        Throw
                                    End Try
                                End Function
                ).Defuturized
            Catch e As Exception
                Disconnect(expected:=False, reason:="Failed to start connection: {0}.".Frmt(e))
                Throw
            End Try
        End Function
        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
        Private Sub BeginHandlingPackets()
            Dim readLoop = FutureIterate(AddressOf socket.FutureReadPacket, Function(packetData, packetException) ref.AssumeNotNull.QueueFunc(
                Function()
                    If packetException IsNot Nothing Then
                        If TypeOf packetException Is Pickling.PicklingException Then
                            Return True
                        ElseIf Not (TypeOf packetException Is SocketException OrElse
                                    TypeOf packetException Is ObjectDisposedException OrElse
                                    TypeOf packetException Is IO.IOException) Then
                            packetException.RaiseAsUnexpected("Error receiving data from bnet server")
                        End If
                        Disconnect(expected:=False, reason:="Error receiving packet: {0}.".Frmt(packetException))
                        Return False
                    End If

                    Try
                        Dim packet = packetData

                        'Handle
                        eref.QueueAction(Sub() RaiseEvent ReceivedPacket(Me, packet))
                        If packetHandlers(packet.id) IsNot Nothing Then
                            Call packetHandlers(packet.id)(CType(packet.Payload.Value, Dictionary(Of String, Object)))
                        End If
                        Return True

                    Catch e As Exception
                        e.RaiseAsUnexpected("Error receiving data from bnet server")
                        Disconnect(expected:=False, reason:="Error handling packet: {0}.".Frmt(e))
                        Return False
                    End Try
                End Function
            ))
        End Sub
        Private Sub BeginConnectBnlsServer(ByVal seed As ModInt32)
            Dim address = My.Settings.bnls
            If address Is Nothing OrElse address = "" Then
                logger.Log("No bnls server is specified. Battle.net will most likely disconnect the bot after two minutes.", LogMessageType.Problem)
                Return
            End If
            Dim hostPortPair = address.Split(":"c)
            Dim port As UShort
            If hostPortPair.Length <> 2 OrElse Not UShort.TryParse(hostPortPair(1), port) Then
                logger.Log("Invalid bnls server format specified. Expected hostname:port.", LogMessageType.Problem)
                Return
            End If

            logger.Log("Connecting to bnls server at {0}...".Frmt(address), LogMessageType.Positive)
            futureWardenHandler = BNLS.BNLSWardenClient.FutureConnectToBNLSServer(hostPortPair(0), port, seed, logger).EvalWhenValueReady(
                Function(bnlsClient, bnlsClientException)
                    If bnlsClientException IsNot Nothing Then
                        logger.Log("Error connecting to bnls server: {0}".Frmt(bnlsClientException), LogMessageType.Problem)
                        Return Nothing
                    End If
                    Contract.Assume(bnlsClient IsNot Nothing)
                    logger.Log("Connected to bnls server.", LogMessageType.Positive)
                    AddHandler bnlsClient.Send, AddressOf OnWardenSend
                    AddHandler bnlsClient.Fail, AddressOf OnWardenFail
                    Return bnlsClient
                End Function)
        End Sub

        Private Function BeginLogOn(ByVal username As String,
                                    ByVal password As String) As IFuture
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            If state <> BnetClientState.EnterUserName Then
                Throw New InvalidOperationException("Incorrect state for login.")
            End If

            Me.futureLoggedIn.TrySetFailed(New InvalidStateException("Another login was initiated."))
            Me.futureLoggedIn = New FutureAction
            Me.futureLoggedIn.MarkAnyExceptionAsHandled()
            Me.accountUsername = username
            Me.accountPassword = password
            ChangeState(BnetClientState.LogOn)
            SendPacket(BnetPacket.MakeAccountLogOnBegin(username, clientPublicKey))
            logger.Log("Initiating logon with username " + username, LogMessageType.Typical)
            Return futureLoggedIn
        End Function

        Public Function QueueConnectAndLogOn(ByVal remoteHost As String,
                                             ByVal userName As String,
                                             ByVal password As String) As IFuture
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return QueueConnect(remoteHost).EvalOnSuccess(Function() QueueLogOn(userName, password)).Defuturized
        End Function

        Private Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            If socket IsNot Nothing Then
                socket.Disconnect(expected, reason)
                RemoveHandler socket.Disconnected, AddressOf CatchSocketDisconnected
                socket = Nothing
            ElseIf state = BnetClientState.Disconnected Then
                Return
            End If

            'Finalize futures
            futureConnected.TrySetFailed(New InvalidOperationException("Disconnected before connection completed ({0}).".Frmt(reason)))
            futureLoggedIn.TrySetFailed(New InvalidOperationException("Disconnected before logon completed ({0}).".Frmt(reason)))
            futureCreatedGame.TrySetFailed(New InvalidOperationException("Disconnected before game creation completed ({0}).".Frmt(reason)))

            ChangeState(BnetClientState.Disconnected)
            logger.Log("Disconnected ({0})".Frmt(reason), LogMessageType.Negative)
            If futureWardenHandler IsNot Nothing Then
                futureWardenHandler.CallOnValueSuccess(
                    Sub(bnlsClient)
                        Contract.Assume(bnlsClient IsNot Nothing)
                        bnlsClient.Dispose()
                        RemoveHandler bnlsClient.Send, AddressOf OnWardenSend
                        RemoveHandler bnlsClient.Fail, AddressOf OnWardenFail
                    End Sub)
                futureWardenHandler = Nothing
            End If

            If poolPort IsNot Nothing Then
                poolPort.Dispose()
                poolPort = Nothing
                logger.Log("Returned port {0} to pool.".Frmt(Me.listenPort), LogMessageType.Positive)
                Me.listenPort = 0
            End If

            eref.QueueAction(Sub()
                                 RaiseEvent Disconnected(Me, reason)
                             End Sub)

            If Not expected AndAlso allowRetryConnect Then
                allowRetryConnect = False
                FutureWait(5.Seconds).CallWhenReady(
                    Sub()
                        Contract.Assume(hostname IsNot Nothing)
                        Contract.Assume(accountUsername IsNot Nothing)
                        Contract.Assume(accountPassword IsNot Nothing)
                        logger.Log("Attempting to reconnect...", LogMessageType.Positive)
                        QueueConnectAndLogOn(hostname, accountUsername, accountPassword)
                    End Sub
                )
            End If
        End Sub

        Private Sub EnterChannel(ByVal channel As String)
            futureCreatedGame.TrySetFailed(New InvalidOperationException("Re-entered channel before game was created."))
            SendPacket(BnetPacket.MakeJoinChannel(BnetPacket.JoinChannelType.ForcedJoin, channel))
            ChangeState(BnetClientState.Channel)
        End Sub

        Private Function BeginAdvertiseGame(ByVal game As W3GameDescription,
                                            ByVal server As W3Server) As IFuture
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)

            Select Case state
                Case BnetClientState.Disconnected, BnetClientState.Connecting, BnetClientState.EnterUserName, BnetClientState.LogOn
                    Throw New InvalidOperationException("Can't advertise a game until connected.")
                Case BnetClientState.CreatingGame
                    Throw New InvalidOperationException("Already creating a game.")
                Case BnetClientState.Game
                    Throw New InvalidOperationException("Already advertising a game.")
                Case BnetClientState.Channel
                    advertisedGameSettings = New GameSettings(game)
                    Me.futureCreatedGame.TrySetFailed(New OperationFailedException("Started advertising another game."))
                    Me.futureCreatedGame = New FutureAction
                    Me.futureCreatedGame.MarkAnyExceptionAsHandled()
                    ChangeState(BnetClientState.CreatingGame)
                    hostCount += 1
                    Try
                        AdvertiseGame(False, False)
                    Catch e As Exception
                        futureCreatedGame.TrySetFailed(New OperationFailedException("Failed to send data."))
                        ChangeState(BnetClientState.Channel)
                        Throw
                    End Try

                    eref.QueueAction(Sub() RaiseEvent AddedGame(Me, game, server))
                    If server IsNot Nothing Then
                        server.QueueAddAdvertiser(Me).MarkAnyExceptionAsHandled()
                        DisposeLink.CreateOneWayLink(New AdvertisingDisposeNotifier(Me), server.CreateAdvertisingDependency)
                        server.QueueOpenPort(Me.listenPort).QueueCallWhenReady(ref,
                            Sub(listenException)
                                If listenException IsNot Nothing Then
                                    futureCreatedGame.TrySetFailed(listenException)
                                    Contract.Assume(listenException.Message IsNot Nothing)
                                    StopAdvertisingGame(reason:=listenException.Message)
                                End If
                            End Sub
                        ).MarkAnyExceptionAsHandled()
                    End If
                    Return futureCreatedGame
                Case Else
                    Throw state.MakeImpossibleValueException
            End Select
        End Function
        Private Sub AdvertiseGame(Optional ByVal useFull As Boolean = False,
                                  Optional ByVal refreshing As Boolean = False)
            If refreshing Then
                If state <> BnetClientState.Game Then
                    Throw New InvalidOperationException("Must have already created game before refreshing")
                End If
                ChangeState(BnetClientState.Game) '[throws event]
            End If

            Dim gameState = BnetPacket.GameStates.Unknown0x10
            If advertisedGameSettings.private Then gameState = gameState Or BnetPacket.GameStates.Private
            If useFull Then gameState = gameState Or BnetPacket.GameStates.Full
            'If in_progress Then gameState = gameState Or BnetPacket.GameStateFlags.InProgress
            'If Not empty Then game_state_flags = game_state_flags Or FLAG_NOT_EMPTY [causes problems: why?]

            Dim gameType = GameTypes.CreateGameUnknown0 Or advertisedGameSettings.Header.gameType
            If advertisedGameSettings.private Then
                gameType = gameType Or GameTypes.PrivateGame
            End If
            Select Case advertisedGameSettings.Header.GameStats.observers
                Case GameObserverOption.FullObservers, GameObserverOption.Referees
                    gameType = gameType Or GameTypes.ObsFull
                Case GameObserverOption.ObsOnDefeat
                    gameType = gameType Or GameTypes.ObsOnDeath
                Case GameObserverOption.NoObservers
                    gameType = gameType Or GameTypes.ObsNone
            End Select

            advertisedGameSettings.Header.Update(gameType, gameState)
            SendPacket(BnetPacket.MakeCreateGame3(advertisedGameSettings.Header,
                                                  hostCount))
        End Sub

        Private Sub StopAdvertisingGame(ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)

            Select Case state
                Case BnetClientState.CreatingGame, BnetClientState.Game
                    SendPacket(BnetPacket.MakeCloseGame3())
                    gameRefreshTimer.Stop()
                    EnterChannel(lastChannel)
                    futureCreatedGame.TrySetFailed(New OperationFailedException("Advertising cancelled."))
                    eref.QueueAction(Sub() RaiseEvent RemovedGame(Me, advertisedGameSettings.Header, reason))

                Case Else
                    Throw New InvalidOperationException("Wasn't advertising any games.")
            End Select
        End Sub
#End Region

#Region "Link"
        Private Event Disconnected(ByVal sender As BnetClient, ByVal reason As String)
        Private ReadOnly userLinkMap As New Dictionary(Of BotUser, ClientServerUserLink)

        Private Function GetUserServer(ByVal user As BotUser) As W3Server
            If user Is Nothing Then Return Nothing
            If Not userLinkMap.ContainsKey(user) Then Return Nothing
            Return userLinkMap(user).server
        End Function
        Private Sub SetUserServer(ByVal user As BotUser, ByVal server As W3Server)
            If user Is Nothing Then Return
            If userLinkMap.ContainsKey(user) Then
                Dim link = userLinkMap(user)
                Contract.Assume(link IsNot Nothing)
                link.Dispose()
                userLinkMap.Remove(user)
            End If
            If server Is Nothing Then Return
            userLinkMap(user) = New ClientServerUserLink(Me, server, user)
        End Sub

        Private NotInheritable Class ClientServerUserLink
            Inherits FutureDisposable
            Public ReadOnly client As BnetClient
            Public ReadOnly server As W3Server
            Public ReadOnly user As BotUser

            Public Sub New(ByVal client As BnetClient, ByVal server As W3Server, ByVal user As BotUser)
                'contract bug wrt interface event implementation requires this:
                'Contract.Requires(client IsNot Nothing)
                'Contract.Requires(server IsNot Nothing)
                'Contract.Requires(user IsNot Nothing)
                Contract.Assume(client IsNot Nothing)
                Contract.Assume(server IsNot Nothing)
                Contract.Assume(user IsNot Nothing)
                Me.client = client
                Me.server = server
                Me.user = user
                DisposeLink.CreateOneWayLink(client, Me)
                DisposeLink.CreateOneWayLink(server, Me)
            End Sub

            Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
                If Not finalizing Then
                    client.QueueSetUserServer(user, Nothing)
                End If
            End Sub
        End Class

        Private Event DisposedAdvertisingLink(ByVal sender As IGameSource, ByVal partner As IGameSink) Implements IGameSource.DisposedLink
        Private Event AddedGame(ByVal sender As IGameSource, ByVal game As W3GameDescription, ByVal server As W3Server) Implements IGameSource.AddedGame
        Private Event RemovedGame(ByVal sender As IGameSource, ByVal game As W3GameDescription, ByVal reason As String) Implements IGameSource.RemovedGame
        Private Sub _QueueAddGame(ByVal game As W3GameDescription, ByVal server As W3Server) Implements IGameSourceSink.AddGame
            ref.QueueAction(Sub() BeginAdvertiseGame(game, server)).MarkAnyExceptionAsHandled()
        End Sub
        Private Sub _QueueRemoveGame(ByVal game As W3GameDescription, ByVal reason As String) Implements IGameSourceSink.RemoveGame
            ref.QueueAction(Sub() StopAdvertisingGame(reason)).MarkAnyExceptionAsHandled()
        End Sub
        Private Sub _QueueSetAdvertisingOptions(ByVal [private] As Boolean) Implements Links.IGameSourceSink.SetAdvertisingOptions
            ref.QueueAction(
                Sub()
                    If state <> BnetClientState.Game And state <> BnetClientState.CreatingGame Then
                        Throw New InvalidOperationException("Not advertising any games.")
                    End If

                    advertisedGameSettings.private = [private]
                    Me.OnRefreshTimerTick()
                    If [private] Then
                        Me.gameRefreshTimer.Stop()
                    Else
                        Me.gameRefreshTimer.Start()
                    End If
                End Sub
            )
        End Sub
        <Pure()>
        Public Sub QueueRemoveAdvertisingPartner(ByVal other As IGameSourceSink)
            eref.QueueAction(Sub() RaiseEvent DisposedAdvertisingLink(Me, other))
        End Sub
#End Region

#Region "Networking"
        Private Sub SendPacket(ByVal packet As BnetPacket)
            Contract.Requires(packet IsNot Nothing)
            socket.SendPacket(packet)
        End Sub
#End Region

#Region "Networking (Connect)"
        Private Sub ReceiveAuthenticationBegin(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Const LOGON_TYPE_WC3 As UInteger = 2

            If state <> BnetClientState.Connecting Then
                Throw New Exception("Invalid state for receiving AUTHENTICATION_BEGIN")
            End If

            'validate
            If CType(vals("logon type"), UInteger) <> LOGON_TYPE_WC3 Then
                futureConnected.TrySetFailed(New IO.IOException("Failed to connect: Unrecognized logon type from server."))
                Throw New IO.InvalidDataException("Unrecognized logon type")
            End If

            'respond
            Dim serverCdKeySalt = CType(vals("server cd key salt"), Byte()).AssumeNotNull
            Dim mpqNumberString = CStr(vals("mpq number string")).AssumeNotNull
            Dim mpqHashChallenge = CStr(vals("mpq hash challenge")).AssumeNotNull
            Dim war3Path = My.Settings.war3path.AssumeNotNull
            Dim cdKeyOwner = My.Settings.cdKeyOwner.AssumeNotNull
            Dim exeInfo = My.Settings.exeInformation.AssumeNotNull
            Dim R = New System.Security.Cryptography.RNGCryptoServiceProvider()
            If profile.CKLServerAddress Like "*:#*" Then
                Dim pair = profile.CKLServerAddress.Split(":"c)
                Dim tempPort = pair(1)
                Contract.Assume(tempPort IsNot Nothing) 'can be removed once verifier understands String.split
                Dim port = UShort.Parse(tempPort, CultureInfo.InvariantCulture)
                BnetPacket.MakeCKLAuthenticationFinish(MainBot.WC3Version,
                                                       war3Path,
                                                       mpqNumberString,
                                                       mpqHashChallenge,
                                                       serverCdKeySalt,
                                                       cdKeyOwner,
                                                       exeInfo,
                                                       pair(0),
                                                       port,
                                                       R).QueueCallWhenValueReady(ref,
                    Sub(packet, packetException)
                        If packetException IsNot Nothing Then
                            logger.Log(packetException.Message, LogMessageType.Negative)
                            futureConnected.TrySetFailed(New IO.IOException("Failed to borrow keys: '{0}'.".Frmt(packetException.Message)))
                            Disconnect(expected:=False, reason:="Error borrowing keys.")
                            Return
                        End If

                        Contract.Assume(packet IsNot Nothing)
                        Dim sendVals = CType(packet.Payload.Value, Dictionary(Of String, Object)).AssumeNotNull
                        Dim rocKeyData = CType(sendVals("ROC cd key"), Dictionary(Of String, Object)).AssumeNotNull
                        Dim rocHash = CType(rocKeyData("hash"), Byte()).AssumeNotNull
                        BeginConnectBnlsServer(rocHash.SubArray(0, 4).ToUInt32())
                        logger.Log("Received response from CKL server.", LogMessageType.Positive)
                        SendPacket(packet)
                    End Sub
                )
            Else
                Dim rocKey = profile.cdKeyROC.AssumeNotNull
                Dim tftKey = profile.cdKeyTFT.AssumeNotNull
                Dim p = BnetPacket.MakeAuthenticationFinish(MainBot.WC3Version,
                                                            war3Path,
                                                            mpqNumberString,
                                                            mpqHashChallenge,
                                                            serverCdKeySalt,
                                                            cdKeyOwner,
                                                            exeInfo,
                                                            rocKey,
                                                            tftKey,
                                                            R)
                Dim sendVals = CType(p.Payload.Value, Dictionary(Of String, Object)).AssumeNotNull
                Dim rocKeyData = CType(sendVals("ROC cd key"), Dictionary(Of String, Object)).AssumeNotNull
                Dim rocHash = CType(rocKeyData("hash"), Byte()).AssumeNotNull
                Contract.Assume(rocHash.Length >= 4)
                BeginConnectBnlsServer(rocHash.SubArray(0, 4).ToUInt32())
                SendPacket(p)
            End If
        End Sub

        Private Sub ReceiveAuthenticationFinish(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            If state <> BnetClientState.Connecting Then
                Throw New Exception("Invalid state for receiving AUTHENTICATION_FINISHED")
            End If

            Dim result = CType(CUInt(vals("result")), BnetPacket.AuthenticationFinishResult)
            Dim errmsg As String
            Select Case result
                Case BnetPacket.AuthenticationFinishResult.Passed
                    ChangeState(BnetClientState.EnterUserName)
                    futureConnected.TrySetSucceeded()
                    allowRetryConnect = True
                    Return

                Case BnetPacket.AuthenticationFinishResult.OldVersion
                    errmsg = "Out of date version"
                Case BnetPacket.AuthenticationFinishResult.InvalidVersion
                    errmsg = "Invalid version"
                Case BnetPacket.AuthenticationFinishResult.FutureVersion
                    errmsg = "Future version (need to downgrade apparently)"
                Case BnetPacket.AuthenticationFinishResult.InvalidCDKey
                    errmsg = "Invalid CD key"
                Case BnetPacket.AuthenticationFinishResult.UsedCDKey
                    errmsg = "CD key in use by:"
                Case BnetPacket.AuthenticationFinishResult.BannedCDKey
                    errmsg = "CD key banned!"
                Case BnetPacket.AuthenticationFinishResult.WrongProduct
                    errmsg = "Wrong product."
                Case Else
                    errmsg = "Unknown authentication failure id: {0}.".Frmt(result)
            End Select

            futureConnected.TrySetFailed(New IO.IOException("Failed to connect: {0} {1}".Frmt(errmsg, vals("info"))))
            Throw New Exception(errmsg)
        End Sub

        Private Sub ReceiveAccountLogonBegin(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)

            If state <> BnetClientState.LogOn Then
                Throw New Exception("Invalid state for receiving ACCOUNT_LOGON_BEGIN")
            End If

            Dim result = CType(vals("result"), BnetPacket.AccountLogOnBeginResult)
            If result <> BnetPacket.AccountLogOnBeginResult.Passed Then
                Dim errmsg As String
                Select Case result
                    Case BnetPacket.AccountLogOnBeginResult.BadUserName
                        errmsg = "Username doesn't exist."
                    Case BnetPacket.AccountLogOnBeginResult.UpgradeAccount
                        errmsg = "Account requires upgrade."
                    Case Else
                        errmsg = "Unrecognized login problem: " + result.ToString()
                End Select
                futureLoggedIn.TrySetFailed(New IO.IOException("Failed to login: " + errmsg))
                Throw New Exception(errmsg)
            End If

            'generate password proofs
            Dim accountPasswordSalt = CType(vals("account password salt"), Byte())
            Dim serverPublicKey = CType(vals("server public key"), Byte())
            If accountUsername Is Nothing Then Throw New InvalidStateException("Received AccountLogOnBegin before username specified.")
            If accountPassword Is Nothing Then Throw New InvalidStateException("Received AccountLogOnBegin before password specified.")
            Contract.Assume(serverPublicKey IsNot Nothing)
            Contract.Assume(accountPasswordSalt IsNot Nothing)
            With Bnet.GenerateClientServerPasswordProofs(accountUsername,
                                                         accountPassword,
                                                         accountPasswordSalt.ToView,
                                                         serverPublicKey.ToView,
                                                         clientPrivateKey,
                                                         clientPublicKey)
                clientPasswordProof = .Value1
                serverPasswordProof = .Value2
            End With

            'respond
            SendPacket(BnetPacket.MakeAccountLogOnFinish(clientPasswordProof))
        End Sub

        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
        Private Sub ReceiveAccountLogonFinish(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            If state <> BnetClientState.LogOn Then
                Throw New Exception("Invalid state for receiving ACCOUNT_LOGON_FINISH")
            End If

            Dim result = CType(vals("result"), BnetPacket.AccountLogOnFinishResult)

            If result <> BnetPacket.AccountLogOnFinishResult.Passed Then
                Dim errmsg As String
                Select Case result
                    Case BnetPacket.AccountLogOnFinishResult.IncorrectPassword
                        errmsg = "Incorrect password."
                    Case BnetPacket.AccountLogOnFinishResult.NeedEmail
                        errmsg = "No email address associated with account"
                    Case BnetPacket.AccountLogOnFinishResult.CustomError
                        errmsg = "Logon error: " + CType(vals("custom error info"), String)
                    Case Else
                        errmsg = "Unrecognized logon error: " + result.ToString()
                End Select
                futureLoggedIn.TrySetFailed(New IO.IOException("Failed to logon: " + errmsg))
                Throw New Exception(errmsg)
            End If

            'validate
            Dim removeServerPasswordProof = CType(vals("server password proof"), Byte())
            If serverPasswordProof Is Nothing Then Throw New InvalidStateException("Received AccountLogOnFinish before server password proof computed.")
            Contract.Assume(removeServerPasswordProof IsNot Nothing)
            If Not Me.serverPasswordProof.HasSameItemsAs(removeServerPasswordProof) Then
                futureLoggedIn.TrySetFailed(New IO.IOException("Failed to logon: Server didn't give correct password proof"))
                Throw New IO.InvalidDataException("Server didn't give correct password proof.")
            End If
            Dim lan_host = profile.lanHost.Split(" "c)(0)
            If lan_host <> "" Then
                Try
                    Dim lan = New W3LanAdvertiser(parent, name, listenPort, lan_host)
                    parent.QueueAddWidget(lan)
                    DisposeLink.CreateOneWayLink(Me, lan)
                    AdvertisingLink.CreateMultiWayLink({Me, lan.MakeAdvertisingLinkMember})
                Catch e As Exception
                    logger.Log("Error creating lan advertiser: {0}".Frmt(e.ToString), LogMessageType.Problem)
                End Try
            End If
            'log
            logger.Log("Logged on with username {0}.".Frmt(accountUsername), LogMessageType.Typical)
            futureLoggedIn.TrySetSucceeded()
            'respond
            SendPacket(BnetPacket.MakeNetGamePort(listenPort))
            SendPacket(BnetPacket.MakeEnterChat())
        End Sub

        Private Sub ReceiveEnterChat(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            logger.Log("Entered chat", LogMessageType.Typical)
            EnterChannel(profile.initialChannel)
        End Sub
#End Region

#Region "Networking (Warden)"
        Private Sub ReceiveWarden(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Dim encryptedData = CType(vals("encrypted data"), Byte())
            Contract.Assume(encryptedData IsNot Nothing)
            If futureWardenHandler Is Nothing Then Return
            futureWardenHandler.CallOnValueSuccess(
                Sub(bnlsClient)
                    Contract.Assume(encryptedData IsNot Nothing)
                    bnlsClient.ProcessWardenPacket(encryptedData.ToView)
                End Sub)
        End Sub
        Private Sub OnWardenSend(ByVal data() As Byte)
            Contract.Requires(data IsNot Nothing)
            ref.QueueAction(Sub()
                                Contract.Assume(data IsNot Nothing)
                                SendPacket(BnetPacket.MakeWarden(data))
                            End Sub)
        End Sub
        Private Sub OnWardenFail(ByVal e As Exception)
            Contract.Requires(e IsNot Nothing)
            e.RaiseAsUnexpected("Warden")
            logger.Log("Error dealing with Warden packet. Disconnecting to be safe.", LogMessageType.Problem)
            ref.QueueAction(Sub() Disconnect(expected:=False, reason:="Error dealing with Warden packet."))
        End Sub
#End Region

#Region "Networking (Games)"
        Private Sub ReceiveCreateGame3(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Dim succeeded = CUInt(vals("result")) = 0

            If succeeded Then
                If state = BnetClientState.CreatingGame Then
                    logger.Log("Finished creating game.", LogMessageType.Positive)
                    ChangeState(BnetClientState.Game)
                    If Not advertisedGameSettings.private Then gameRefreshTimer.Start()
                    futureCreatedGame.TrySetSucceeded()
                Else
                    ChangeState(BnetClientState.Game) 'throw event
                End If
            Else
                futureCreatedGame.TrySetFailed(New OperationFailedException("BNET didn't allow game creation. Most likely cause is game name in use."))
                gameRefreshTimer.Stop()
                EnterChannel(lastChannel)
                RaiseEvent RemovedGame(Me, advertisedGameSettings.header, "Client {0} failed to advertise the game. Most likely cause is game name in use.".Frmt(Me.name))
            End If
        End Sub
        Private Sub IgnorePacket(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
        End Sub
#End Region

#Region "Networking (Misc)"
        Private Sub ReceiveChatEvent(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Dim eventId = CType(vals("event id"), BnetPacket.ChatEventId)
            Dim text = CStr(vals("text"))
            If eventId = BnetPacket.ChatEventId.Channel Then lastChannel = text
        End Sub

        Private Sub ReceivePing(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Dim salt = CUInt(vals("salt"))
            SendPacket(BnetPacket.MakePing(salt))
        End Sub

        Private Sub ReceiveNull(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            '[ignore]
        End Sub

        Private Sub ReceiveMessageBox(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Dim msg = "MESSAGE BOX FROM BNET: " + CStr(vals("caption")) + ": " + CStr(vals("text"))
            logger.Log(msg, LogMessageType.Problem)
        End Sub
#End Region

#Region "Remote"
        Public ReadOnly Property GetUserName() As String
            Get
                Return accountUsername
            End Get
        End Property
        Public Function QueueSendText(ByVal text As String) As IFuture
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(text.Length > 0)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() SendText(text))
        End Function
        Public Function QueueSendWhisper(ByVal userName As String,
                                         ByVal text As String) As IFuture
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(userName.Length > 0)
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() SendWhisper(userName, text))
        End Function
        Public Function QueueSendPacket(ByVal packet As BnetPacket) As IFuture
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() SendPacket(packet))
        End Function
        Public Function QueueSetListenPort(ByVal newPort As UShort) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() SetListenPort(newPort))
        End Function
        Public Function QueueStopAdvertisingGame(ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() StopAdvertisingGame(reason))
        End Function
        Public Function QueueStartAdvertisingGame(ByVal header As W3GameDescription,
                                                  ByVal server As W3Server) As IFuture
            Contract.Requires(header IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueFunc(Function() BeginAdvertiseGame(header, server)).Defuturized
        End Function
        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() Disconnect(expected, reason))
        End Function
        Public Function QueueConnect(ByVal remoteHost As String) As IFuture
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueFunc(Function() BeginConnect(remoteHost)).Defuturized
        End Function
        Public Function QueueLogOn(ByVal userName As String,
                                   ByVal password As String) As IFuture
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueFunc(Function() BeginLogOn(userName, password)).Defuturized
        End Function
        Public Function QueueGetUserServer(ByVal user As BotUser) As IFuture(Of W3Server)
            Contract.Ensures(Contract.Result(Of IFuture(Of Warcraft3.W3Server))() IsNot Nothing)
            Return ref.QueueFunc(Function() GetUserServer(user))
        End Function
        Public Function QueueSetUserServer(ByVal user As BotUser, ByVal server As W3Server) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() SetUserServer(user, server))
        End Function
        Public Function QueueGetListenPort() As IFuture(Of UShort)
            Contract.Ensures(Contract.Result(Of IFuture(Of UShort))() IsNot Nothing)
            Return ref.QueueFunc(Function() listenPort)
        End Function
        Public Function QueueGetState() As IFuture(Of BnetClientState)
            Contract.Ensures(Contract.Result(Of IFuture(Of BnetClientState))() IsNot Nothing)
            Return ref.QueueFunc(Function() state)
        End Function
#End Region

        Public ReadOnly Property CurGame As GameSettings
            Get
                Return Me.advertisedGameSettings
            End Get
        End Property
    End Class
End Namespace
