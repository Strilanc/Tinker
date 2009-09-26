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
    Public NotInheritable Class BnetClient
        Inherits FutureDisposable
        Implements IGameSourceSink
#Region "Inner"
        Public Enum States
            Disconnected
            Connecting
            EnterUsername
            Logon
            Channel
            CreatingGame
            Game
        End Enum

        Public Class GameSettings
            Public [private] As Boolean
            Public ReadOnly header As W3GameHeader
            Public Sub New(ByVal header As W3GameHeader)
                Contract.Requires(header IsNot Nothing)
                Me.private = [private]
                Me.header = header
                For Each arg In header.Options
                    Select Case arg.ToLower.Trim()
                        Case "-p", "-private"
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
        Public ReadOnly parent As MainBot
        Public ReadOnly name As String = "unnamed_client"
        Public ReadOnly logger As Logger
        Private socket As BnetSocket = Nothing

        'refs
        Private ReadOnly eref As ICallQueue
        Private ReadOnly ref As ICallQueue
        Private ReadOnly wardenRef As ICallQueue

        'packets
        Private ReadOnly packetHandlers(0 To 255) As Action(Of Dictionary(Of String, Object))

        'game
        Private advertisedGameSettings As GameSettings
        Private hostCount As Integer = 0
        Private ReadOnly gameRefreshTimer As New Timers.Timer(REFRESH_PERIOD)

        'crypto
        Private ReadOnly clientPrivateKey As Byte()
        Private ReadOnly clientPublicKey As Byte()
        Private clientPasswordProof As Byte()
        Private serverPasswordProof As Byte()

        Private connectRetriesLeft As Integer

        'futures
        Private futureConnected As New FutureAction
        Private futureLoggedIn As New FutureAction
        Private futureCreatedGame As New FutureAction

        'events
        Public Event StateChanged(ByVal sender As BnetClient, ByVal oldState As States, ByVal newState As States)
        Public Event ReceivedPacket(ByVal sender As BnetClient, ByVal packet As BnetPacket)

        'warden
        Private futureWardenHandler As IFuture(Of BattleNetLogonServer.BnlsClient)

        'state
        Private listenPort As UShort
        Private poolPort As PortPool.PortHandle
        Private lastChannel As String = ""
        Private username As String
        Private password As String
        Private hostname As String
        Private state As States
#End Region

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(parent IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eref IsNot Nothing)
            Contract.Invariant(clientPublicKey IsNot Nothing)
            Contract.Invariant(clientPrivateKey IsNot Nothing)
            Contract.Invariant(wardenRef IsNot Nothing)
            Contract.Invariant(profile IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(futureLoggedIn IsNot Nothing)
            Contract.Invariant(futureConnected IsNot Nothing)
            Contract.Invariant(futureCreatedGame IsNot Nothing)
        End Sub

#Region "New"
        Public Sub New(ByVal parent As MainBot,
                       ByVal profile As ClientProfile,
                       ByVal name As String,
                       ByVal wardenRef As ICallQueue,
                       Optional ByVal logger As Logger = Nothing)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(parent IsNot Nothing)
            'Contract.Requires(profile IsNot Nothing)
            'Contract.Requires(name IsNot Nothing)
            'Contract.Requires(wardenRef IsNot Nothing)
            Contract.Assume(parent IsNot Nothing)
            Contract.Assume(profile IsNot Nothing)
            Contract.Assume(name IsNot Nothing)
            Contract.Assume(wardenRef IsNot Nothing)

            'Pass values
            Me.wardenRef = wardenRef
            Me.name = name
            Me.parent = parent
            Me.profile = profile
            Me.listenPort = profile.listenPort
            Me.logger = If(logger, New Logger)
            Me.eref = New ThreadPooledCallQueue
            Me.ref = New ThreadPooledCallQueue
            AddHandler gameRefreshTimer.Elapsed, Sub() OnRefreshTimerTick()

            'Init crypto
            With Bnet.Crypt.GeneratePublicPrivateKeyPair(New System.Security.Cryptography.RNGCryptoServiceProvider())
                clientPublicKey = .Value1
                clientPrivateKey = .Value2
            End With

            'Start packet machinery
            packetHandlers(Bnet.BnetPacketID.AuthenticationBegin) = AddressOf ReceiveAuthenticationBegin
            packetHandlers(Bnet.BnetPacketID.AuthenticationFinish) = AddressOf ReceiveAuthenticationFinish
            packetHandlers(Bnet.BnetPacketID.AccountLogonBegin) = AddressOf ReceiveAccountLogonBegin
            packetHandlers(Bnet.BnetPacketID.AccountLogonFinish) = AddressOf ReceiveAccountLogonFinish
            packetHandlers(Bnet.BnetPacketID.ChatEvent) = AddressOf ReceiveChatEvent
            packetHandlers(Bnet.BnetPacketID.EnterChat) = AddressOf ReceiveEnterChat
            packetHandlers(Bnet.BnetPacketID.Null) = AddressOf ReceiveNull
            packetHandlers(Bnet.BnetPacketID.Ping) = AddressOf ReceivePing
            packetHandlers(Bnet.BnetPacketID.MessageBox) = AddressOf ReceiveMessageBox
            packetHandlers(Bnet.BnetPacketID.CreateGame3) = AddressOf ReceiveCreateGame3
            packetHandlers(Bnet.BnetPacketID.Warden) = AddressOf ReceiveWarden

            packetHandlers(Bnet.BnetPacketID.QueryGamesList) = AddressOf IgnorePacket
            packetHandlers(Bnet.BnetPacketID.FriendsUpdate) = AddressOf IgnorePacket
        End Sub
#End Region

#Region "Access"
        Private Sub SendText(ByVal text As String)
            Contract.Requires(text IsNot Nothing)
            Select Case state
                Case States.Channel
                    'fine
                Case States.CreatingGame, States.Game
                    If text(0) <> "/"c Then Throw New InvalidOperationException("Can only send commands when in games.")
                Case Else
                    Throw New InvalidOperationException("Can't send text unless you're logged in.")
            End Select

            SendPacket(BnetPacket.MakeChatCommand(text))
        End Sub

        Private Sub SendWhisper(ByVal username As String, ByVal text As String)
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            SendText("/w {0} {1}".Frmt(username, text))
        End Sub

        Private Sub SetListenPort(ByVal newPort As UShort)
            If newPort = listenPort Then Return
            Select Case state
                Case States.Channel, States.Disconnected
                    If poolPort IsNot Nothing Then
                        poolPort.Dispose()
                        poolPort = Nothing
                        logger.Log("Returned port {0} to pool.".Frmt(Me.listenPort), LogMessageType.Positive)
                    End If
                    listenPort = newPort
                    logger.Log("Changed listen port to {0}.".Frmt(newPort), LogMessageType.Typical)
                    If state <> States.Disconnected Then
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
                parent.QueueRemoveClient(Me.name, expected:=True, reason:="Client Disposed")
            End If
        End Sub
        Private Sub ChangeState(ByVal newState As States)
            Dim oldState = state
            state = newState
            eref.QueueAction(Sub()
                                 RaiseEvent StateChanged(Me, oldState, newState)
                             End Sub)
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
                    Dim out = parent.portPool.TryAcquireAnyPort()
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
                    port = UShort.Parse(remotePortTemp)
                    remoteHost = remoteHost.Split(":"c)(0)
                    Contract.Assume(remoteHost IsNot Nothing) 'remove once static verifier understands String.split
                End If

                Return FutureCreateConnectedTcpClient(remoteHost, port).QueueEvalOnValueSuccess(ref,
                    Function(tcpClient)
                        Try
                            Contract.Assume(tcpClient IsNot Nothing)
                            socket = New BnetSocket(New PacketSocket(
                                tcpClient,
                                60.Seconds,
                                logger,
                                Function(stream)
                                    Contract.Assume(stream IsNot Nothing)
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
                            ChangeState(States.Connecting)

                            'Replace connection future
                            Me.futureConnected.TrySetFailed(New InvalidStateException("Another connection was initiated."))
                            Me.futureConnected = New FutureAction

                            'Start log-on process
                            tcpClient.GetStream.Write({1}, 0, 1)
                            SendPacket(BnetPacket.MakeAuthenticationBegin(MainBot.Wc3MajorVersion, GetCachedIpAddressBytes(external:=False)))

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
        Private Sub BeginHandlingPackets()
            Dim readLoop = FutureIterate(AddressOf socket.FutureReadPacket, Function(packetData, packetException) ref.QueueFunc(
                Function()
                                                                                                                                    If packetException IsNot Nothing Then
                                                                                                                                        If TypeOf packetException Is Pickling.PicklingException Then
                                                                                                                                            Return True
                                                                                                                                        ElseIf Not (TypeOf packetException Is SocketException OrElse
                                                                                                                                                    TypeOf packetException Is ObjectDisposedException OrElse
                                                                                                                                                    TypeOf packetException Is IO.IOException) Then
                                                                                                                                            LogUnexpectedException("Error receiving data from bnet server", packetException)
                                                                                                                                        End If
                                                                                                                                        Disconnect(expected:=False, reason:="Error receiving packet: {0}.".Frmt(packetException))
                                                                                                                                        Return False
                                                                                                                                    End If

                                                                                                                                    Try
                                                                                                                                        Dim packet = packetData

                                                                                                                                        'Handle
                                                                                                                                        eref.QueueAction(Sub()
                                                                                                                                                             RaiseEvent ReceivedPacket(Me, packet)
                                                                                                                                                         End Sub)
                                                                                                                                        If packetHandlers(packet.id) IsNot Nothing Then
                                                                                                                                            Call packetHandlers(packet.id)(CType(packet.payload.Value, Dictionary(Of String, Object)))
                                                                                                                                        End If
                                                                                                                                        Return True

                                                                                                                                    Catch e As Exception
                                                                                                                                        LogUnexpectedException("Error receiving data from bnet server", e)
                                                                                                                                        Disconnect(expected:=False, reason:="Error handling packet: {0}.".Frmt(e))
                                                                                                                                        Return False
                                                                                                                                    End Try
                                                                                                                                End Function
            ))
        End Sub
        Private Sub BeginConnectBnlsServer(ByVal seed As ModInt32)
            Dim address = My.Settings.bnls
            If address = "" Then
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
            futureWardenHandler = BattleNetLogonServer.BnlsClient.FutureConnectToBnlsServer(hostPortPair(0), port, seed, logger).EvalWhenValueReady(
                Function(bnlsClient, bnlsClientException)
                    If bnlsClientException IsNot Nothing Then
                        logger.Log("Error connecting to bnls server: {0}".Frmt(bnlsClientException), LogMessageType.Problem)
                        Return Nothing
                    End If
                    logger.Log("Connected to bnls server.", LogMessageType.Positive)
                    AddHandler bnlsClient.Send, AddressOf OnWardenSend
                    AddHandler bnlsClient.Fail, AddressOf OnWardenFail
                    Return bnlsClient
                End Function)
        End Sub

        Private Function BeginLogin(ByVal username As String,
                                    ByVal password As String) As IFuture
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            If state <> States.EnterUsername Then
                Throw New InvalidOperationException("Incorrect state for login.")
            End If

            futureLoggedIn.TrySetFailed(New InvalidStateException("Another login was initiated."))
            futureLoggedIn = New FutureAction
            Me.username = username
            Me.password = password
            ChangeState(States.Logon)
            SendPacket(BnetPacket.MakeAccountLogonBegin(username, clientPublicKey))
            logger.Log("Initiating logon with username " + username, LogMessageType.Typical)
            Return futureLoggedIn
        End Function

        Public Function QueueConnectAndLogin(ByVal remoteHost As String,
                                             ByVal username As String,
                                             ByVal password As String) As IFuture
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return QueueConnect(remoteHost).EvalOnSuccess(Function() QueueLogin(username, password)).Defuturized
        End Function

        Private Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            If socket IsNot Nothing Then
                socket.disconnect(expected, reason)
                RemoveHandler socket.Disconnected, AddressOf CatchSocketDisconnected
                socket = Nothing
            ElseIf state = States.Disconnected Then
                Return
            End If

            'Finalize futures
            futureConnected.TrySetFailed(New InvalidOperationException("Disconnected before connection completed ({0}).".Frmt(reason)))
            futureLoggedIn.TrySetFailed(New InvalidOperationException("Disconnected before logon completed ({0}).".Frmt(reason)))
            futureCreatedGame.TrySetFailed(New InvalidOperationException("Disconnected before game creation completed ({0}).".Frmt(reason)))

            ChangeState(States.Disconnected)
            logger.Log("Disconnected ({0})".Frmt(reason), LogMessageType.Negative)
            If futureWardenHandler IsNot Nothing Then
                futureWardenHandler.CallOnValueSuccess(
                    Sub(bnlsClient)
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

            If Not expected AndAlso connectRetriesLeft > 0 Then
                connectRetriesLeft -= 1
                FutureWait(5.Seconds).CallWhenReady(
                    Sub()
                        logger.Log("Attempting to reconnect...", LogMessageType.Positive)
                        QueueConnectAndLogin(hostname, username, password)
                    End Sub
                )
            End If
        End Sub

        Private Sub EnterChannel(ByVal channel As String)
            futureCreatedGame.TrySetFailed(New InvalidOperationException("Re-entered channel."))
            SendPacket(BnetPacket.MakeJoinChannel(BnetPacket.JoinChannelType.ForcedJoin, channel))
            ChangeState(States.Channel)
        End Sub

        Private Function BeginAdvertiseGame(ByVal game As W3GameHeader,
                                            ByVal server As W3Server) As IFuture
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)

            Select Case state
                Case States.Disconnected, States.Connecting, States.EnterUsername, States.Logon
                    Throw New InvalidOperationException("Can't advertise a game until connected.")
                Case States.CreatingGame
                    Throw New InvalidOperationException("Already creating a game.")
                Case States.Game
                    Throw New InvalidOperationException("Already advertising a game.")
                Case States.Channel
                    advertisedGameSettings = New GameSettings(game)
                    futureCreatedGame.TrySetFailed(New OperationFailedException("Started advertising another game."))
                    futureCreatedGame = New FutureAction
                    ChangeState(States.CreatingGame)
                    hostCount += 1
                    Try
                        AdvertiseGame(False, False)
                    Catch e As Exception
                        futureCreatedGame.TrySetFailed(New OperationFailedException("Failed to send data."))
                        ChangeState(States.Channel)
                        Throw
                    End Try

                    eref.QueueAction(Sub()
                                         RaiseEvent AddedGame(Me, game, server)
                                     End Sub)
                    If server IsNot Nothing Then
                        server.QueueAddAvertiser(Me)
                        DisposeLink.CreateOneWayLink(New AdvertisingDisposeNotifier(Me), server.CreateAdvertisingDependency)
                        server.QueueOpenPort(Me.listenPort).CallWhenReady(Sub(listenException) ref.QueueAction(
                            Sub()
                                If listenException IsNot Nothing Then
                                    futureCreatedGame.TrySetFailed(listenException)
                                    StopAdvertisingGame(reason:=listenException.Message)
                                End If
                            End Sub
                        ))
                    End If
                    '[verifier fails to realize passing 'Me' out won't ruin these variables]
                    Contract.Assume(futureCreatedGame IsNot Nothing)
                    Contract.Assume(futureConnected IsNot Nothing)
                    Contract.Assume(futureLoggedIn IsNot Nothing)
                    Return futureCreatedGame
                Case Else
                    Throw state.MakeImpossibleValueException
            End Select
        End Function
        Private Sub AdvertiseGame(Optional ByVal useFull As Boolean = False,
                                  Optional ByVal refreshing As Boolean = False)
            If refreshing Then
                If state <> States.Game Then
                    Throw New InvalidOperationException("Must have already created game before refreshing")
                End If
                ChangeState(States.Game) '[throws event]
            End If

            Dim gameState = BnetPacket.GameStateFlags.UnknownFlag
            If advertisedGameSettings.private Then gameState = gameState Or BnetPacket.GameStateFlags.Private
            If useFull Then gameState = gameState Or BnetPacket.GameStateFlags.Full
            'If in_progress Then gameState = gameState Or BnetPacket.GameStateFlags.InProgress
            'If Not empty Then game_state_flags = game_state_flags Or FLAG_NOT_EMPTY [causes problems: why?]

            Dim gameType = GameTypeFlags.CreateGameUnknown0 Or advertisedGameSettings.header.Map.gameType
            If advertisedGameSettings.private Then
                gameType = gameType Or GameTypeFlags.PrivateGame
            End If
            Select Case advertisedGameSettings.header.Map.observers
                Case GameObserverOption.FullObservers, GameObserverOption.Referees
                    gameType = gameType Or GameTypeFlags.ObsFull
                Case GameObserverOption.ObsOnDefeat
                    gameType = gameType Or GameTypeFlags.ObsOnDeath
                Case GameObserverOption.NoObservers
                    gameType = gameType Or GameTypeFlags.ObsNone
            End Select

            SendPacket(BnetPacket.MakeCreateGame3(New W3GameHeaderAndState(gameState,
                                                                           advertisedGameSettings.header,
                                                                           gameType),
                                                  hostCount))
        End Sub

        Private Sub StopAdvertisingGame(ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)

            Select Case state
                Case States.CreatingGame, States.Game
                    SendPacket(BnetPacket.MakeCloseGame3())
                    gameRefreshTimer.Stop()
                    EnterChannel(lastChannel)
                    futureCreatedGame.TrySetFailed(New OperationFailedException("Advertising cancelled."))
                    Dim reason_ = reason
                    eref.QueueAction(Sub()
                                         RaiseEvent RemovedGame(Me, advertisedGameSettings.header, reason_)
                                     End Sub)

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
                userLinkMap(user).Dispose()
                userLinkMap.Remove(user)
            End If
            If server Is Nothing Then Return
            userLinkMap(user) = New ClientServerUserLink(Me, server, user)
        End Sub

        Private Class ClientServerUserLink
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
        Private Event AddedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal server As W3Server) Implements IGameSource.AddedGame
        Private Event RemovedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal reason As String) Implements IGameSource.RemovedGame
        Private Sub _f_AddGame(ByVal game As W3GameHeader, ByVal server As W3Server) Implements IGameSourceSink.AddGame
            ref.QueueAction(Sub()
                                Contract.Assume(game IsNot Nothing)
                                BeginAdvertiseGame(game, server)
                            End Sub)
        End Sub
        Private Sub _f_RemoveGame(ByVal game As W3GameHeader, ByVal reason As String) Implements IGameSourceSink.RemoveGame
            ref.QueueAction(Sub()
                                Contract.Assume(reason IsNot Nothing)
                                StopAdvertisingGame(reason)
                            End Sub)
        End Sub
        Private Sub _f_SetAdvertisingOptions(ByVal [private] As Boolean) Implements Links.IGameSourceSink.SetAdvertisingOptions
            ref.QueueAction(
                Sub()
                    If state <> States.Game And state <> States.CreatingGame Then
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
            Dim other_ = other
            eref.QueueAction(Sub()
                                 RaiseEvent DisposedAdvertisingLink(Me, other_)
                             End Sub)
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

            If state <> States.Connecting Then
                Throw New Exception("Invalid state for receiving AUTHENTICATION_BEGIN")
            End If

            'validate
            If CType(vals("logon type"), UInteger) <> LOGON_TYPE_WC3 Then
                futureConnected.TrySetFailed(New IO.IOException("Failed to connect: Unrecognized logon type from server."))
                Throw New IO.InvalidDataException("Unrecognized logon type")
            End If

            'respond
            Dim serverCdKeySalt = CType(vals("server cd key salt"), Byte())
            Dim mpqNumberString = CStr(vals("mpq number string"))
            Dim mpqHashChallenge = CStr(vals("mpq hash challenge"))
            Dim war3Path = My.Settings.war3path
            Dim cdKeyOwner = My.Settings.cdKeyOwner
            Dim exeInfo = My.Settings.exeInformation
            Dim R = New System.Security.Cryptography.RNGCryptoServiceProvider()
            If profile.keyServerAddress Like "*:#*" Then
                Dim pair = profile.keyServerAddress.Split(":"c)
                Dim tempPort = pair(1)
                Contract.Assume(tempPort IsNot Nothing) 'can be removed once verifier understands String.split
                Dim port = UShort.Parse(tempPort)
                BnetPacket.MakeCklAuthenticationFinish(MainBot.Wc3Version,
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
                        Dim rocKeyData = CType(CType(packet.payload.Value, Dictionary(Of String, Object))("ROC cd key"), Dictionary(Of String, Object))
                        Dim rocHash = CType(rocKeyData("hash"), Byte())
                        Contract.Assume(rocHash IsNot Nothing)
                        BeginConnectBnlsServer(rocHash.SubArray(0, 4).ToUInt32())
                        logger.Log("Received response from CKL server.", LogMessageType.Positive)
                        SendPacket(packet)
                    End Sub
                )
            Else
                Dim rocKey = profile.rocCdKey
                Dim tftKey = profile.tftCdKey
                Contract.Assume(rocKey IsNot Nothing)
                Contract.Assume(tftKey IsNot Nothing)
                Dim p = BnetPacket.MakeAuthenticationFinish(MainBot.Wc3Version,
                                                                    war3Path,
                                                                    mpqNumberString,
                                                                    mpqHashChallenge,
                                                                    serverCdKeySalt,
                                                                    cdKeyOwner,
                                                                    exeInfo,
                                                                    rocKey,
                                                                    tftKey,
                                                                    R)
                Dim rocKeyData = CType(CType(p.payload.Value, Dictionary(Of String, Object))("ROC cd key"), Dictionary(Of String, Object))
                Dim rocHash = CType(rocKeyData("hash"), Byte())
                Contract.Assume(rocHash IsNot Nothing)
                BeginConnectBnlsServer(rocHash.SubArray(0, 4).ToUInt32())
                SendPacket(p)
            End If
        End Sub

        Private Sub ReceiveAuthenticationFinish(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            If state <> States.Connecting Then
                Throw New Exception("Invalid state for receiving AUTHENTICATION_FINISHED")
            End If

            Dim result = CType(CUInt(vals("result")), BnetPacket.AuthenticationFinishResult)
            Dim errmsg As String
            Select Case result
                Case BnetPacket.AuthenticationFinishResult.Passed
                    ChangeState(States.EnterUsername)
                    futureConnected.TrySetSucceeded()
                    connectRetriesLeft = 2
                    Return

                Case BnetPacket.AuthenticationFinishResult.OldVersion
                    errmsg = "Out of date version"
                Case BnetPacket.AuthenticationFinishResult.InvalidVersion
                    errmsg = "Invalid version"
                Case BnetPacket.AuthenticationFinishResult.FutureVersion
                    errmsg = "Future version (need to downgrade apparently)"
                Case BnetPacket.AuthenticationFinishResult.InvalidCdKey
                    errmsg = "Invalid CD key"
                Case BnetPacket.AuthenticationFinishResult.UsedCdKey
                    errmsg = "CD key in use by:"
                Case BnetPacket.AuthenticationFinishResult.BannedCdKey
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

            If state <> States.Logon Then
                Throw New Exception("Invalid state for receiving ACCOUNT_LOGON_BEGIN")
            End If

            Dim result = CType(vals("result"), BnetPacket.AccountLogonBeginResult)
            If result <> BnetPacket.AccountLogonBeginResult.Passed Then
                Dim errmsg As String
                Select Case result
                    Case BnetPacket.AccountLogonBeginResult.BadUsername
                        errmsg = "Username doesn't exist."
                    Case BnetPacket.AccountLogonBeginResult.UpgradeAccount
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
            If username Is Nothing Then Throw New InvalidStateException("Received AccountLogonBegin before username specified.")
            If password Is Nothing Then Throw New InvalidStateException("Received AccountLogonBegin before password specified.")
            Contract.Assume(serverPublicKey IsNot Nothing)
            Contract.Assume(accountPasswordSalt IsNot Nothing)
            With Bnet.Crypt.GenerateClientServerPasswordProofs(username,
                                                               password,
                                                               accountPasswordSalt,
                                                               serverPublicKey,
                                                               clientPrivateKey,
                                                               clientPublicKey)
                clientPasswordProof = .Value1
                serverPasswordProof = .Value2
            End With

            'respond
            SendPacket(BnetPacket.MakeAccountLogonFinish(clientPasswordProof))
        End Sub

        Private Sub ReceiveAccountLogonFinish(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            If state <> States.Logon Then
                Throw New Exception("Invalid state for receiving ACCOUNT_LOGON_FINISH")
            End If

            Dim result = CType(vals("result"), BnetPacket.AccountLogonFinishResult)

            If result <> BnetPacket.AccountLogonFinishResult.Passed Then
                Dim errmsg As String
                Select Case result
                    Case BnetPacket.AccountLogonFinishResult.IncorrectPassword
                        errmsg = "Incorrect password."
                    Case BnetPacket.AccountLogonFinishResult.NeedEmail
                        errmsg = "No email address associated with account"
                    Case BnetPacket.AccountLogonFinishResult.CustomError
                        errmsg = "Logon error: " + CType(vals("custom error info"), String)
                    Case Else
                        errmsg = "Unrecognized logon error: " + result.ToString()
                End Select
                futureLoggedIn.TrySetFailed(New IO.IOException("Failed to logon: " + errmsg))
                Throw New Exception(errmsg)
            End If

            'validate
            Dim removeServerPasswordProof = CType(vals("server password proof"), Byte())
            If serverPasswordProof Is Nothing Then Throw New InvalidStateException("Received AccountLogonFinish before server password proof computed.")
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
            logger.Log("Logged on with username {0}.".Frmt(username), LogMessageType.Typical)
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
            Dim data = CType(vals("encrypted data"), Byte()).ToView
            If futureWardenHandler Is Nothing Then Return
            futureWardenHandler.CallOnValueSuccess(
                Sub(bnlsClient)
                    bnlsClient.ProcessWardenPacket(data)
                End Sub)
        End Sub
        Private Sub OnWardenSend(ByVal data() As Byte)
            ref.QueueAction(Sub()
                                Contract.Assume(data IsNot Nothing)
                                SendPacket(BnetPacket.MakeWarden(data))
                            End Sub)
        End Sub
        Private Sub OnWardenFail(ByVal e As Exception)
            LogUnexpectedException("Warden", e)
            logger.Log("Error dealing with Warden packet. Disconnecting to be safe.", LogMessageType.Problem)
            ref.QueueAction(Sub() Disconnect(expected:=False, reason:="Error dealing with Warden packet."))
        End Sub
#End Region

#Region "Networking (Games)"
        Private Sub ReceiveCreateGame3(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Dim succeeded = CUInt(vals("result")) = 0

            If succeeded Then
                If state = States.CreatingGame Then
                    logger.Log("Finished creating game.", LogMessageType.Positive)
                    ChangeState(States.Game)
                    If Not advertisedGameSettings.private Then gameRefreshTimer.Start()
                    futureCreatedGame.TrySetSucceeded()
                Else
                    ChangeState(States.Game) 'throw event
                End If
            Else
                futureCreatedGame.TrySetFailed(New OperationFailedException("BNET didn't allow game creation. Most likely cause is game name in use."))
                gameRefreshTimer.Stop()
                EnterChannel(lastChannel)
                RaiseEvent RemovedGame(Me, advertisedGameSettings.header, "Client {0} failed to advertise the game. Most likely cause is game name in use.".Frmt(Me.name))
                '[verifier fails to realize passing 'Me' out won't ruin these variables]
                Contract.Assume(futureCreatedGame IsNot Nothing)
                Contract.Assume(futureConnected IsNot Nothing)
                Contract.Assume(futureLoggedIn IsNot Nothing)
            End If
        End Sub
        Private Sub IgnorePacket(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
        End Sub
#End Region

#Region "Networking (Misc)"
        Private Sub ReceiveChatEvent(ByVal vals As Dictionary(Of String, Object))
            Dim eventId = CType(vals("event id"), BnetPacket.ChatEventId)
            Dim text = CStr(vals("text"))
            If eventId = BnetPacket.ChatEventId.Channel Then lastChannel = text
        End Sub

        Private Sub ReceivePing(ByVal vals As Dictionary(Of String, Object))
            Dim salt = CUInt(vals("salt"))
            SendPacket(BnetPacket.MakePing(salt))
        End Sub

        Private Sub ReceiveNull(ByVal vals As Dictionary(Of String, Object))
            '[ignore]
        End Sub

        Private Sub ReceiveMessageBox(ByVal vals As Dictionary(Of String, Object))
            Dim msg = "MESSAGE BOX FROM BNET: " + CStr(vals("caption")) + ": " + CStr(vals("text"))
            logger.Log(msg, LogMessageType.Problem)
        End Sub
#End Region

#Region "Remote"
        Public ReadOnly Property GetUsername() As String
            Get
                Return username
            End Get
        End Property
        Public Function QueueSendText(ByVal text As String) As IFuture
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(text IsNot Nothing)
                                       SendText(text)
                                   End Sub)
        End Function
        Public Function QueueSendWhisper(ByVal username As String,
                                         ByVal text As String) As IFuture
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(username IsNot Nothing)
                                       Contract.Assume(text IsNot Nothing)
                                       SendWhisper(username, text)
                                   End Sub)
        End Function
        Public Function QueueSendPacket(ByVal packet As BnetPacket) As IFuture
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(packet IsNot Nothing)
                                       SendPacket(packet)
                                   End Sub)
        End Function
        Public Function QueueSetListenPort(ByVal new_port As UShort) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() SetListenPort(new_port))
        End Function
        Public Function QueueStopAdvertisingGame(ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(reason IsNot Nothing)
                                       StopAdvertisingGame(reason)
                                   End Sub)
        End Function
        Public Function QueueStartAdvertisingGame(ByVal header As W3GameHeader,
                                                  ByVal server As W3Server) As IFuture
            Contract.Requires(header IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueFunc(Function()
                                     Contract.Assume(header IsNot Nothing)
                                     Return BeginAdvertiseGame(header, server)
                                 End Function).Defuturized
        End Function
        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(reason IsNot Nothing)
                                       Disconnect(expected, reason)
                                   End Sub)
        End Function
        Public Function QueueConnect(ByVal remoteHost As String) As IFuture
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueFunc(Function()
                                     Contract.Assume(remoteHost IsNot Nothing)
                                     Return BeginConnect(remoteHost)
                                 End Function).Defuturized
        End Function
        Public Function QueueLogin(ByVal username As String, ByVal password As String) As IFuture
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueFunc(Function()
                                     Contract.Assume(username IsNot Nothing)
                                     Contract.Assume(password IsNot Nothing)
                                     Return BeginLogin(username, password)
                                 End Function).Defuturized
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
        Public Function QueueGetState() As IFuture(Of States)
            Contract.Ensures(Contract.Result(Of IFuture(Of BnetClient.States))() IsNot Nothing)
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
