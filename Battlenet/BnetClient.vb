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

Namespace Bnet
    <ContractClass(GetType(ContractClassIBnetClient))>
    Public Interface IBnetClient
        Inherits INotifyingDisposable
        Inherits IGameSourceSink
        ReadOnly Property logger() As Logger
        ReadOnly Property username() As String
        ReadOnly Property Parent As MainBot
        ReadOnly Property profile As ClientProfile
        ReadOnly Property Name As String
        ReadOnly Property CurGame As BnetClient.GameSettings
        Function f_SendPacket(ByVal cp As BnetPacket) As IFuture(Of Outcome)
        Function f_SetListenPort(ByVal new_port As UShort) As IFuture(Of Outcome)
        Function f_StopAdvertisingGame(ByVal reason As String) As IFuture(Of Outcome)
        Function f_StartAdvertisingGame(ByVal header As W3GameHeader, ByVal server As IW3Server) As IFuture(Of Outcome)
        Function f_Disconnect(ByVal reason As String) As IFuture(Of Outcome)
        Function f_Connect(ByVal remoteHost As String) As IFuture(Of Outcome)
        Function f_Login(ByVal username As String, ByVal password As String) As IFuture(Of Outcome)
        Function f_GetUserServer(ByVal user As BotUser) As IFuture(Of IW3Server)
        Function f_SetUserServer(ByVal user As BotUser, ByVal server As IW3Server) As IFuture
        Function f_listenPort() As IFuture(Of UShort)
        Function f_GetState() As IFuture(Of BnetClient.States)
        Function f_SendText(ByVal text As String) As IFuture(Of outcome)
        Function f_SendWhisper(ByVal username As String, ByVal text As String) As IFuture(Of Outcome)
        Sub ClearAdvertisingPartner(ByVal other As IGameSourceSink)
        Event StateChanged(ByVal sender As IBnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States)
        Event ReceivedChatEvent(ByVal sender As IBnetClient, ByVal id As BnetPacket.ChatEventId, ByVal user As String, ByVal text As String)
        Event Disconnected(ByVal sender As IBnetClient, ByVal reason As String)
    End Interface
    <ContractClassFor(GetType(IBnetClient))>
    Public Class ContractClassIBnetClient
        Implements IBnetClient
        Public Event ReceivedChatEvent(ByVal sender As IBnetClient, ByVal id As BnetPacket.ChatEventId, ByVal user As String, ByVal text As String) Implements IBnetClient.ReceivedChatEvent
        Public Event StateChanged(ByVal sender As IBnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States) Implements IBnetClient.StateChanged
        Public Event Disconnected(ByVal sender As IBnetClient, ByVal reason As String) Implements IBnetClient.Disconnected

        Public Sub ClearAdvertisingPartner(ByVal other As Links.IGameSourceSink) Implements IBnetClient.ClearAdvertisingPartner
            Throw New NotSupportedException()
        End Sub

        Public ReadOnly Property CurGame As BnetClient.GameSettings Implements IBnetClient.CurGame
            Get
                Throw New NotSupportedException()
            End Get
        End Property

        Public Function f_Connect(ByVal remoteHost As String) As IFuture(Of Outcome) Implements IBnetClient.f_Connect
            Contract.Requires(remoteHost IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_Disconnect(ByVal reason As String) As IFuture(Of Outcome) Implements IBnetClient.f_Disconnect
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_GetState() As IFuture(Of BnetClient.States) Implements IBnetClient.f_GetState
            Contract.Ensures(Contract.Result(Of IFuture(Of BnetClient.States))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_GetUserServer(ByVal user As BotUser) As IFuture(Of Warcraft3.IW3Server) Implements IBnetClient.f_GetUserServer
            Contract.Ensures(Contract.Result(Of IFuture(Of Warcraft3.IW3Server))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_listenPort() As Functional.Futures.IFuture(Of UShort) Implements IBnetClient.f_listenPort
            Contract.Ensures(Contract.Result(Of IFuture(Of UShort))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_Login(ByVal username As String, ByVal password As String) As IFuture(Of Outcome) Implements IBnetClient.f_Login
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_SendPacket(ByVal cp As BnetPacket) As IFuture(Of Outcome) Implements IBnetClient.f_SendPacket
            Contract.Requires(cp IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_SendText(ByVal text As String) As IFuture(Of Outcome) Implements IBnetClient.f_SendText
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_SendWhisper(ByVal username As String, ByVal text As String) As IFuture(Of Outcome) Implements IBnetClient.f_SendWhisper
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_SetListenPort(ByVal new_port As UShort) As IFuture(Of Outcome) Implements IBnetClient.f_SetListenPort
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_SetUserServer(ByVal user As BotUser, ByVal server As Warcraft3.IW3Server) As IFuture Implements IBnetClient.f_SetUserServer
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_StartAdvertisingGame(ByVal header As Warcraft3.W3GameHeader, ByVal server As Warcraft3.IW3Server) As IFuture(Of Outcome) Implements IBnetClient.f_StartAdvertisingGame
            Contract.Requires(header IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public Function f_StopAdvertisingGame(ByVal reason As String) As Functional.Futures.IFuture(Of Functional.Outcome) Implements IBnetClient.f_StopAdvertisingGame
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException()
        End Function

        Public ReadOnly Property logger As Logger Implements IBnetClient.logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Throw New NotSupportedException()
            End Get
        End Property

        Public ReadOnly Property name As String Implements IBnetClient.Name
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException()
            End Get
        End Property

        Public ReadOnly Property parent As MainBot Implements IBnetClient.Parent
            Get
                Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                Throw New NotSupportedException()
            End Get
        End Property

        Public ReadOnly Property profile As ClientProfile Implements IBnetClient.profile
            Get
                Contract.Ensures(Contract.Result(Of ClientProfile)() IsNot Nothing)
                Throw New NotSupportedException()
            End Get
        End Property

        Public ReadOnly Property username As String Implements IBnetClient.username
            Get
                Throw New NotSupportedException()
            End Get
        End Property

#Region "Subinterfaces"
        Public Event Disposed() Implements INotifyingDisposable.Disposed
        Public ReadOnly Property IsDisposed As Boolean Implements INotifyingDisposable.IsDisposed
            Get
                Throw New NotSupportedException()
            End Get
        End Property
        Public Sub AddGame(ByVal game As Warcraft3.W3GameHeader, ByVal server As Warcraft3.IW3Server) Implements Links.IGameSink.AddGame
            Throw New NotSupportedException()
        End Sub
        Public Sub RemoveGame(ByVal game As Warcraft3.W3GameHeader, ByVal reason As String) Implements Links.IGameSink.RemoveGame
            Throw New NotSupportedException()
        End Sub
        Public Sub SetAdvertisingOptions(ByVal [private] As Boolean) Implements Links.IGameSink.SetAdvertisingOptions
            Throw New NotSupportedException()
        End Sub
        Public Event AddedGame(ByVal sender As Links.IGameSource, ByVal game As Warcraft3.W3GameHeader, ByVal server As Warcraft3.IW3Server) Implements Links.IGameSource.AddedGame
        Public Event DisposedLink(ByVal sender As Links.IGameSource, ByVal partner As Links.IGameSink) Implements Links.IGameSource.DisposedLink
        Public Event RemovedGame(ByVal sender As Links.IGameSource, ByVal game As Warcraft3.W3GameHeader, ByVal reason As String) Implements Links.IGameSource.RemovedGame
        Public Sub Dispose() Implements IDisposable.Dispose
            Throw New NotSupportedException()
        End Sub
#End Region
    End Class

    Public NotInheritable Class BnetClient
        Inherits NotifyingDisposable
        Implements IBnetClient
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
                For Each arg In header.options
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

        Private ReadOnly profile As ClientProfile
        Private ReadOnly parent As MainBot
        Private ReadOnly name As String = "unnamed_client"
        Private ReadOnly logger As Logger
        Private socket As BnetSocket = Nothing

        'refs
        Private ReadOnly eref As ICallQueue
        Private ReadOnly ref As ICallQueue
        Private ReadOnly wardenRef As ICallQueue

        'packets
        Private ReadOnly packetHandlers(0 To 255) As Action(Of Dictionary(Of String, Object))

        'game
        Private game_settings As GameSettings
        Private futureCreatedGame As New Future(Of Outcome)
        Private hostCount As Integer = 0
        Private ReadOnly gameRefreshTimer As New Timers.Timer(REFRESH_PERIOD)

        'crypto
        Private ReadOnly clientPrivateKey As Byte()
        Private ReadOnly clientPublicKey As Byte()
        Private clientPasswordProof As Byte()
        Private serverPasswordProof As Byte()

        'futures
        Private futureConnected As New Future(Of Outcome)
        Private futureLoggedIn As New Future(Of Outcome)

        'events
        Private Event StateChanged(ByVal sender As IBnetClient, ByVal old_state As States, ByVal new_state As States) Implements IBnetClient.StateChanged
        Private Event ReceivedChatEvent(ByVal sender As IBnetClient, ByVal id As BnetPacket.ChatEventId, ByVal user As String, ByVal text As String) Implements IBnetClient.ReceivedChatEvent

        'warden
        Private WithEvents warden As Warden.WardenPacketHandler
        Private wardenSeed As ModInt32

        'state
        Private listenPort As UShort
        Private poolPort As PortPool.PortHandle
        Private lastChannel As String = ""
        Private username As String
        Private password As String
        Private hostname As String
        Private state As States
#End Region

        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(parent IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eref IsNot Nothing)
            Contract.Invariant(clientPublicKey IsNot Nothing)
            Contract.Invariant(clientPrivateKey IsNot Nothing)
            Contract.Invariant(wardenRef IsNot Nothing)
            Contract.Invariant(profile IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
        End Sub

#Region "New"
        Public Sub New(ByVal parent As MainBot,
                       ByVal profile As ClientProfile,
                       ByVal name As String,
                       ByVal wardenRef As ICallQueue,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Requires(parent IsNot Nothing)
            Contract.Requires(profile IsNot Nothing)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(wardenRef IsNot Nothing)

            'Pass values
            Me.wardenRef = wardenRef
            Me.name = name
            Me.parent = parent
            Me.profile = profile
            Me.listenPort = profile.listen_port
            Me.logger = If(logger, New Logger)
            Me.eref = New ThreadPooledCallQueue
            Me.ref = New ThreadPooledCallQueue
            AddHandler gameRefreshTimer.Elapsed, Sub() c_RefreshTimerTick()

            'Init crypto
            With Bnet.Crypt.GeneratePublicPrivateKeyPair(New System.Random())
                clientPublicKey = .Value1
                clientPrivateKey = .Value2
            End With

            'Start packet machinery
            packetHandlers(Bnet.BnetPacketID.AuthenticationBegin) = AddressOf ReceivePacket_AUTHENTICATION_BEGIN
            packetHandlers(Bnet.BnetPacketID.AuthenticationFinish) = AddressOf ReceivePacket_AUTHENTICATION_FINISH
            packetHandlers(Bnet.BnetPacketID.AccountLogonBegin) = AddressOf ReceiveAccountLogonBegin
            packetHandlers(Bnet.BnetPacketID.AccountLogonFinish) = AddressOf ReceiveAccountLogonFinish
            packetHandlers(Bnet.BnetPacketID.ChatEvent) = AddressOf ReceiveChatEvent
            packetHandlers(Bnet.BnetPacketID.EnterChat) = AddressOf ReceivePacket_ENTER_CHAT
            packetHandlers(Bnet.BnetPacketID.Null) = AddressOf ReceiveNull
            packetHandlers(Bnet.BnetPacketID.Ping) = AddressOf ReceivePing
            packetHandlers(Bnet.BnetPacketID.MessageBox) = AddressOf ReceiveMessageBox
            packetHandlers(Bnet.BnetPacketID.CreateGame3) = AddressOf ReceivePacket_CREATE_GAME_3
            packetHandlers(Bnet.BnetPacketID.Warden) = AddressOf ReceivePacket_WARDEN
        End Sub
#End Region

#Region "Access"
        Private Function SendText(ByVal text As String) As outcome
            If text Is Nothing Or text.Length < 0 Then Return failure("Invalid text")
            Select Case state
                Case States.Channel
                    'fine
                Case States.CreatingGame, States.Game
                    If text(0) <> "/"c Then Return failure("Can only send commands when in games.")
                Case Else
                    Return failure("Can't send text unless you're logged in.")
            End Select

            SendPacket(BnetPacket.MakeChatCommand(text))
            Return success("sent")
        End Function

        Private Function SendWhisper(ByVal username As String, ByVal text As String) As Outcome
            Return SendText("/w {0} {1}".frmt(username, text))
        End Function

        Private Function SetListenPort(ByVal new_port As UShort) As Outcome
            If new_port = listenPort Then Return success("Already using that listen port.")
            Select Case state
                Case States.Channel, States.Disconnected
                    If poolPort IsNot Nothing Then
                        poolPort.Dispose()
                        poolPort = Nothing
                        logger.log("Returned port {0} to pool.".frmt(Me.listenPort), LogMessageTypes.Positive)
                    End If
                    listenPort = new_port
                    logger.log("Changed listen port to {0}.".frmt(new_port), LogMessageTypes.Typical)
                    If state <> States.Disconnected Then
                        SendPacket(BnetPacket.MakeNetGamePort(listenPort))
                    End If
                    Return success("Changed listen port to {0}.".frmt(new_port))
                Case Else
                    Return failure("Can only change listen port when disconnected or in a channel.")
            End Select
        End Function
#End Region

#Region "Events"
        Private Sub e_ThrowChatEvent(ByVal id As BnetPacket.ChatEventId, ByVal user As String, ByVal text As String)
            eref.QueueAction(
                Sub()
                    RaiseEvent ReceivedChatEvent(Me, id, user, text)
                End Sub
            )
        End Sub
        Private Sub e_ThrowStateChanged(ByVal old_state As States, ByVal new_state As States)
            eref.QueueAction(
                Sub()
                    RaiseEvent StateChanged(Me, old_state, new_state)
                End Sub
            )
        End Sub

        Private Sub c_SocketDisconnected(ByVal sender As BnetSocket, ByVal reason As String)
            ref.QueueAction(Sub()
                                Contract.Assume(reason IsNot Nothing)
                                Disconnect(reason)
                            End Sub)
        End Sub
        Private Sub c_SocketReceivedPacket(ByVal sender As BnetSocket, ByVal flag As Byte, ByVal id As Byte, ByVal data As IViewableList(Of Byte))
            ref.QueueAction(Sub()
                                Contract.Assume(data IsNot Nothing)
                                ReceivePacket(flag, CType(id, Bnet.BnetPacketID), data)
                            End Sub)
        End Sub

        Private Sub c_RefreshTimerTick()
            ref.QueueAction(Sub() AdvertiseGame(False, True))
        End Sub
#End Region

#Region "State"
        Protected Overrides Sub PerformDispose()
            ref.QueueAction(Sub() Disconnect("{0} Disposed".frmt(Me.GetType.Name)))
            parent.f_RemoveClient(Me.name)
        End Sub
        Private Sub ChangeState(ByVal newState As States)
            Dim oldState = state
            state = newState
            e_ThrowStateChanged(oldState, newState)
        End Sub
        Private Function BeginConnect(ByVal remoteHost As String) As IFuture(Of Outcome)
            Contract.Requires(remoteHost IsNot Nothing)
            Try
                If socket IsNot Nothing Then
                    Return failure("Client is already connected.").Futurize()
                End If
                hostCount = 0
                hostname = remoteHost

                'Allocate port
                If Me.listenPort = 0 Then
                    Dim out = parent.portPool.TryTakePortFromPool()
                    If Not out.succeeded Then
                        Return failure("No listen port specified, and no ports available in the pool.").Futurize()
                    End If
                    Me.poolPort = out.val
                    Me.listenPort = Me.poolPort.port
                    logger.log("Took port {0} from pool.".frmt(Me.listenPort), LogMessageTypes.Positive)
                End If

                'Establish connection
                logger.log("Connecting to {0}...".frmt(remoteHost), LogMessageTypes.Typical)
                Dim port = BNET_PORT
                If remoteHost Like "*:*" Then
                    Dim remotePortTemp = remoteHost.Split(":"c)(1)
                    Contract.Assume(remotePortTemp IsNot Nothing) 'remove once static verifier understands String.split
                    port = UShort.Parse(remotePortTemp)
                    remoteHost = remoteHost.Split(":"c)(0)
                    Contract.Assume(remoteHost IsNot Nothing) 'remove once static verifier understands String.split
                End If
                Return FutureConnectTo(remoteHost, port).EvalWhenValueReady(
                    Function(tcpClientOutcome)
                        Contract.Assume(ref IsNot Nothing) 'verifier has trouble with this type of thing
                        Return ref.QueueFunc(
                            Function()
                                Try
                                    If Not tcpClientOutcome.succeeded Then  Return tcpClientOutcome.Outcome.Futurize
                                    Contract.Assume(tcpClientOutcome.val IsNot Nothing)
                                    socket = New BnetSocket(tcpClientOutcome.val,
                                                            New TimeSpan(0, 1, 0),
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
                                                            bufferSize:=BnetSocket.DefaultBufferSize * 10)
                                    AddHandler socket.Disconnected, AddressOf c_SocketDisconnected
                                    AddHandler socket.ReceivedPacket, AddressOf c_SocketReceivedPacket
                                    socket.Name = "BNET"
                                    ChangeState(States.Connecting)

                                    'Replace connection future
                                    Me.futureConnected.TrySetValue(failure("Another connection was initiated."))
                                    Me.futureConnected = New Future(Of Outcome)()

                                    'Start log-on process
                                    socket.WriteWithMode({1}, PacketStream.InterfaceModes.RawBytes)
                                    SendPacket(BnetPacket.MakeAuthenticationBegin(MainBot.Wc3MajorVersion, GetCachedIpAddressBytes(external:=False)))
                                    socket.SetReading(True)
                                    Return Me.futureConnected
                                Catch e As Exception
                                    Disconnect("Failed to complete connection.")
                                    Return failure("Error connecting: " + e.Message).Futurize()
                                End Try
                            End Function).Defuturize
                    End Function
                ).Defuturize()
            Catch e As Exception
                Disconnect("Failed to start connection.")
                Return failure("Error connecting: " + e.Message).Futurize()
            End Try
        End Function

        Private Function BeginLogon(ByVal username As String,
                                    ByVal password As String) As IFuture(Of Outcome)
            If state <> States.EnterUsername Then
                Return failure("Incorrect state for logon.").Futurize()
            End If

            futureLoggedIn.TrySetValue(failure("Another login was initiated."))
            futureLoggedIn = New Future(Of Outcome)
            Me.username = username
            Me.password = password
            ChangeState(States.Logon)
            SendPacket(BnetPacket.MakeAccountLogonBegin(username, clientPublicKey))
            logger.log("Initiating logon with username " + username, LogMessageTypes.Typical)
            Return futureLoggedIn
        End Function

        Private Function Disconnect(ByVal reason As String) As Outcome
            Contract.Requires(reason IsNot Nothing)
            If socket IsNot Nothing Then
                socket.Disconnect(reason)
                RemoveHandler socket.Disconnected, AddressOf c_SocketDisconnected
                RemoveHandler socket.ReceivedPacket, AddressOf c_SocketReceivedPacket
                socket = Nothing
            ElseIf state = States.Disconnected Then
                Return success("Client is already disconnected")
            End If

            'Finalize futures
            futureConnected.TrySetValue(failure("Disconnected before connection completed ({0}).".frmt(reason)))
            futureLoggedIn.TrySetValue(failure("Disconnected before logon completed ({0}).".frmt(reason)))
            futureCreatedGame.TrySetValue(failure("Disconnected before game creation completed ({0}).".frmt(reason)))

            ChangeState(States.Disconnected)
            logger.log("Disconnected ({0})".frmt(reason), LogMessageTypes.Negative)
            warden = Nothing

            If poolPort IsNot Nothing Then
                poolPort.Dispose()
                poolPort = Nothing
                logger.log("Returned port {0} to pool.".frmt(Me.listenPort), LogMessageTypes.Positive)
                Me.listenPort = 0
            End If

            RaiseEvent Disconnected(Me, reason)
            Return success("Disconnected ({0})".frmt(reason))
        End Function

        Private Sub EnterChannel(ByVal channel As String)
            futureCreatedGame.TrySetValue(failure("Re-entered channel."))
            SendPacket(BnetPacket.MakeJoinChannel(BnetPacket.JoinChannelType.ForcedJoin, channel))
            ChangeState(States.Channel)
        End Sub

        Private Function BeginAdvertiseGame(ByVal game As W3GameHeader,
                                            ByVal server As IW3Server) As IFuture(Of Outcome)
            Contract.Requires(game IsNot Nothing)

            Select Case state
                Case States.Disconnected, States.Connecting, States.EnterUsername, States.Logon
                    Return failure("Can't advertise a game until connected.").Futurize()
                Case States.CreatingGame
                    Return failure("Already creating a game.").Futurize()
                Case States.Game
                    Return failure("Already advertising a game.").Futurize()
                Case States.Channel
                    game_settings = New GameSettings(game)
                    futureCreatedGame = New Future(Of Outcome)
                    ChangeState(States.CreatingGame)
                    hostCount += 1
                    Dim out = AdvertiseGame(False, False)
                    If Not out.succeeded Then
                        futureCreatedGame.TrySetValue(failure("Failed to send data."))
                        ChangeState(States.Channel)
                        Return out.Futurize()
                    End If

                    RaiseEvent AddedGame(Me, game, server)
                    If server IsNot Nothing Then
                        server.f_AddAvertiser(Me)
                        DisposeLink.CreateOneWayLink(New AdvertisingDisposeNotifier(Me), server.CreateAdvertisingDependency)
                        server.f_OpenPort(Me.listenPort).CallWhenValueReady(
                            Sub(listened)
                                ref.QueueAction(
                                    Sub()
                                        If Not listened.succeeded Then
                                            futureCreatedGame.TrySetValue(listened)
                                            StopAdvertisingGame(listened.Message)
                                        End If
                                    End Sub
                                )
                            End Sub
                        )
                    End If
                    Return futureCreatedGame
                Case Else
                    Return failure("Unrecognized client state for advertising a game.").Futurize()
            End Select
        End Function
        Private Function AdvertiseGame(Optional ByVal useFull As Boolean = False,
                                       Optional ByVal refreshing As Boolean = False) As Outcome
            If refreshing Then
                If state <> States.Game Then
                    Return failure("Must have already created game before refreshing")
                End If
                ChangeState(States.Game) '[throws event]
            End If

            Try
                Dim gameState = BnetPacket.GameStateFlags.UnknownFlag
                If game_settings.private Then gameState = gameState Or BnetPacket.GameStateFlags.Private
                If useFull Then gameState = gameState Or BnetPacket.GameStateFlags.Full
                'If in_progress Then gameState = gameState Or BnetPacket.GameStateFlags.InProgress
                'If Not empty Then game_state_flags = game_state_flags Or FLAG_NOT_EMPTY [causes problems: why?]

                Dim gameType = GameTypeFlags.CreateGameUnknown0 Or game_settings.header.map.gameType
                If game_settings.private Then
                    gameType = gameType Or GameTypeFlags.PrivateGame
                End If
                Select Case game_settings.header.map.observers
                    Case GameObserverOption.FullObservers, GameObserverOption.Referees
                        gameType = gameType Or GameTypeFlags.ObsFull
                    Case GameObserverOption.ObsOnDefeat
                        gameType = gameType Or GameTypeFlags.ObsOnDeath
                    Case GameObserverOption.NoObservers
                        gameType = gameType Or GameTypeFlags.ObsNone
                End Select

                Return SendPacket(BnetPacket.MakeCreateGame3(New W3GameHeaderAndState(gameState,
                                                                                      game_settings.header,
                                                                                      gameType),
                                                             hostCount))
            Catch e As ArgumentException
                Return failure("Error sending packet: {0}.".frmt(e.Message))
            End Try
        End Function

        Private Function StopAdvertisingGame(ByVal reason As String) As Outcome
            Contract.Requires(reason IsNot Nothing)

            Select Case state
                Case States.CreatingGame, States.Game
                    SendPacket(BnetPacket.MakeCloseGame3())
                    gameRefreshTimer.Stop()
                    EnterChannel(lastChannel)
                    futureCreatedGame.TrySetValue(failure("Advertising cancelled."))
                    RaiseEvent RemovedGame(Me, game_settings.header, reason)
                    Return success("Stopped advertising game.")

                Case Else
                    Return failure("Wasn't advertising any games.")
            End Select
        End Function
#End Region

#Region "Link"
        Private Event Disconnected(ByVal sender As IBnetClient, ByVal reason As String) Implements IBnetClient.Disconnected
        Private ReadOnly userLinkMap As New Dictionary(Of BotUser, ClientServerUserLink)

        Private Function GetUserServer(ByVal user As BotUser) As IW3Server
            If user Is Nothing Then Return Nothing
            If Not userLinkMap.ContainsKey(user) Then Return Nothing
            Return userLinkMap(user).server
        End Function
        Private Sub SetUserServer(ByVal user As BotUser, ByVal server As IW3Server)
            If user Is Nothing Then Return
            If userLinkMap.ContainsKey(user) Then
                userLinkMap(user).Dispose()
                userLinkMap.Remove(user)
            End If
            If server Is Nothing Then Return
            userLinkMap(user) = New ClientServerUserLink(Me, server, user)
        End Sub

        Private Class ClientServerUserLink
            Inherits NotifyingDisposable
            Public ReadOnly client As BnetClient
            Public ReadOnly server As IW3Server
            Public ReadOnly user As BotUser

            Public Sub New(ByVal client As BnetClient, ByVal server As IW3Server, ByVal user As BotUser)
                Contract.Requires(client IsNot Nothing)
                Contract.Requires(server IsNot Nothing)
                Contract.Requires(user IsNot Nothing)
                Me.client = client
                Me.server = server
                Me.user = user
                DisposeLink.CreateOneWayLink(client, Me)
                DisposeLink.CreateOneWayLink(server, Me)
            End Sub

            Protected Overrides Sub PerformDispose()
                client._f_SetUserServer(user, Nothing)
            End Sub
        End Class

        Private Event DisposedAdvertisingLink(ByVal sender As IGameSource, ByVal partner As IGameSink) Implements IGameSource.DisposedLink
        Private Event AddedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal server As IW3Server) Implements IGameSource.AddedGame
        Private Event RemovedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal reason As String) Implements IGameSource.RemovedGame
        Private Sub _f_AddGame(ByVal game As W3GameHeader, ByVal server As IW3Server) Implements IGameSourceSink.AddGame
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
                Function()
                    If state <> States.Game And state <> States.CreatingGame Then
                        Return failure("Not advertising any games.")
                    End If

                    game_settings.private = [private]
                    Me.c_RefreshTimerTick()
                    If [private] Then
                        Me.gameRefreshTimer.Stop()
                        Return success("Game will now be advertised as private.")
                    Else
                        Me.gameRefreshTimer.Start()
                        Return success("Game will now be advertised as public.")
                    End If
                End Function
            )
        End Sub
        Private Sub _f_RemoveAdvertisingPartner(ByVal other As IGameSourceSink) Implements IBnetClient.ClearAdvertisingPartner
            RaiseEvent DisposedAdvertisingLink(Me, other)
        End Sub
#End Region

#Region "Networking"
        Private Function SendPacket(ByVal packet As BnetPacket) As Outcome
            Contract.Requires(packet IsNot Nothing)

            logger.log(Function() "Sending {0} to BNET".frmt(packet.id), LogMessageTypes.DataEvent)
            logger.log(packet.payload.Description, LogMessageTypes.DataParsed)

            If socket Is Nothing OrElse Not socket.IsConnected Then
                Return failure("Couldn't send data: socket isn't open.")
            End If
            socket.Write(New Byte() {BnetPacket.PACKET_PREFIX, packet.id}, packet.payload.Data.ToArray)
            Return success("Sent data.")
        End Function

        Private Sub ReceivePacket(ByVal flag As Byte, ByVal id As Bnet.BnetPacketID, ByVal data As IViewableList(Of Byte))
            Contract.Requires(data IsNot Nothing)

            Try
                'Validate
                If flag <> BnetPacket.PACKET_PREFIX Then Throw New Pickling.PicklingException("Invalid packet prefix")

                'Log Event
                logger.log(Function() "Received {0} from BNET".frmt(id), LogMessageTypes.DataEvent)

                'Parse
                Dim p = BnetPacket.FromData(id, data).payload
                If p.Data.Length <> data.Length Then
                    Throw New Pickling.PicklingException("Data left over after parsing.")
                End If

                'Log Parsed Data
                logger.log(p.Description, LogMessageTypes.DataParsed)

                'Handle
                If packetHandlers(id) Is Nothing Then Throw New Pickling.PicklingException("No handler for parsed " + id.ToString())
                Call packetHandlers(id)(CType(p.Value, Dictionary(Of String, Object)))

            Catch e As Pickling.PicklingException
                'Ignore
                logger.log("Error Parsing {0}: {1} (ignored)".frmt(id, e.Message), LogMessageTypes.Negative)

            Catch e As Exception
                'Fail
                LogUnexpectedException("Error receiving data from bnet server", e)
                logger.log("Error receiving data from bnet server: {0}".frmt(e.Message), LogMessageTypes.Problem)
                Disconnect("Error receiving packet.")
            End Try
        End Sub
#End Region

#Region "Networking (Connect)"
        Private Sub ReceivePacket_AUTHENTICATION_BEGIN(ByVal vals As Dictionary(Of String, Object))
            Const LOGON_TYPE_WC3 As UInteger = 2

            If state <> States.Connecting Then
                Throw New Exception("Invalid state for receiving AUTHENTICATION_BEGIN")
            End If

            'validate
            If CType(vals("logon type"), UInteger) <> LOGON_TYPE_WC3 Then
                futureConnected.TrySetValue(failure("Failed to connect: Unrecognized logon type from server."))
                Throw New IO.InvalidDataException("Unrecognized logon type")
            End If

            'respond
            Dim serverCdKeySalt = CType(vals("server cd key salt"), Byte())
            Dim mpqNumberString = CStr(vals("mpq number string"))
            Dim mpqHashChallenge = CStr(vals("mpq hash challenge"))
            Dim war3Path = My.Settings.war3path
            Dim cdKeyOwner = My.Settings.cdKeyOwner
            Dim exeInfo = My.Settings.exeInformation
            Contract.Assume(serverCdKeySalt IsNot Nothing)
            Contract.Assume(mpqNumberString IsNot Nothing)
            Contract.Assume(mpqHashChallenge IsNot Nothing)
            Contract.Assume(war3Path IsNot Nothing)
            Contract.Assume(cdKeyOwner IsNot Nothing)
            Contract.Assume(exeInfo IsNot Nothing)
            If profile.CKL_server Like "*:#*" Then
                Dim pair = profile.CKL_server.Split(":"c)
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
                                                       port).CallWhenValueReady(
                    Sub(out)
                        ref.QueueAction(
                            Sub()
                                If out.succeeded Then
                                    Dim rocKey = CType(CType(out.val.payload.Value, Dictionary(Of String, Object))("ROC cd key"), Dictionary(Of String, Object))
                                    Dim rocHash = CType(rocKey("hash"), Byte())
                                    Contract.Assume(rocHash IsNot Nothing)
                                    Me.wardenSeed = rocHash.SubArray(0, 4).ToUInteger(ByteOrder.LittleEndian)
                                    logger.log(out.Message, LogMessageTypes.Positive)
                                    SendPacket(out.val)
                                Else
                                    logger.log(out.Message, LogMessageTypes.Negative)
                                    futureConnected.TrySetValue(failure("Failed to borrow keys: '{0}'.".frmt(out.Message)))
                                    Disconnect("Error borrowing keys.")
                                End If
                            End Sub
                        )
                    End Sub
                )
            Else
                Dim p = BnetPacket.MakeAuthenticationFinish(MainBot.Wc3Version,
                                                                    war3Path,
                                                                    mpqNumberString,
                                                                    mpqHashChallenge,
                                                                    serverCdKeySalt,
                                                                    cdKeyOwner,
                                                                    exeInfo,
                                                                    profile.roc_cd_key,
                                                                    profile.tft_cd_key)
                Dim rocKey = CType(CType(p.payload.Value, Dictionary(Of String, Object))("ROC cd key"), Dictionary(Of String, Object))
                Dim rocHash = CType(rocKey("hash"), Byte())
                Contract.Assume(rocHash IsNot Nothing)
                Me.wardenSeed = rocHash.SubArray(0, 4).ToUInteger(ByteOrder.LittleEndian)
                SendPacket(p)
            End If
        End Sub

        Private Enum AuthenticationFinishResult As UInteger
            Passed = &H0
            InvalidCodeMin = &H1
            InvalidCodeMax = &HFF
            OldVersion = &H100
            InvalidVersion = &H101
            FutureVersion = &H102
            InvalidCDKey = &H200
            UsedCDKey = &H201
            BannedCDKey = &H202
            WrongProduct = &H203
        End Enum
        Private Sub ReceivePacket_AUTHENTICATION_FINISH(ByVal vals As Dictionary(Of String, Object))
            If state <> States.Connecting Then
                Throw New Exception("Invalid state for receiving AUTHENTICATION_FINISHED")
            End If

            Dim result = CType(CUInt(vals("result")), AuthenticationFinishResult)
            Dim errmsg As String
            Select Case CType(CUInt(vals("result")), AuthenticationFinishResult)
                Case AuthenticationFinishResult.Passed
                    ChangeState(States.EnterUsername)
                    futureConnected.TrySetValue(success("Succesfully connected to battle.net server at {0}.".frmt(hostname)))
                    Return

                Case AuthenticationFinishResult.OldVersion
                    errmsg = "Out of date version"
                Case AuthenticationFinishResult.InvalidVersion
                    errmsg = "Invalid version"
                Case AuthenticationFinishResult.FutureVersion
                    errmsg = "Future version (need to downgrade apparently)"
                Case AuthenticationFinishResult.InvalidCDKey
                    errmsg = "Invalid CD key"
                Case AuthenticationFinishResult.UsedCDKey
                    errmsg = "CD key in use by:"
                Case AuthenticationFinishResult.BannedCDKey
                    errmsg = "CD key banned!"
                Case AuthenticationFinishResult.WrongProduct
                    errmsg = "Wrong product."
                Case AuthenticationFinishResult.InvalidCodeMin To AuthenticationFinishResult.InvalidCodeMax
                    errmsg = "Invalid version code."
                Case Else
                    errmsg = "Unknown authentication failure id: {0}.".frmt(result)
            End Select

            futureConnected.TrySetValue(failure("Failed to connect: {0} {1}".frmt(errmsg, vals("info"))))
            Throw New Exception(errmsg)
        End Sub

        Private Sub ReceiveAccountLogonBegin(ByVal vals As Dictionary(Of String, Object))
            Const RESULT_PASSED As UInteger = &H0
            Const RESULT_BAD_USERNAME As UInteger = &H1
            Const RESULT_UPGRADE_ACCOUNT As UInteger = &H5

            If state <> States.Logon Then
                Throw New Exception("Invalid state for receiving ACCOUNT_LOGON_BEGIN")
            End If

            Dim result = CUInt(vals("result"))
            If result <> RESULT_PASSED Then
                Dim errmsg As String
                Select Case result
                    Case RESULT_BAD_USERNAME
                        errmsg = "Username doesn't exist."
                    Case RESULT_UPGRADE_ACCOUNT
                        errmsg = "Account requires upgrade."
                    Case Else
                        errmsg = "Unrecognized logon problem: " + result.ToString()
                End Select
                futureLoggedIn.TrySetValue(failure("Failed to logon: " + errmsg))
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

        Private Enum AccountLogonFinishResult As UInteger
            Passed = &H0
            BadPassword = &H2
            NoEmail = &HE
            Custom = &HF
        End Enum
        Private Sub ReceiveAccountLogonFinish(ByVal vals As Dictionary(Of String, Object))
            If state <> States.Logon Then
                Throw New Exception("Invalid state for receiving ACCOUNT_LOGON_FINISH")
            End If

            Dim result = CType(CUInt(vals("result")), AccountLogonFinishResult)

            If result <> AccountLogonFinishResult.Passed Then
                Dim errmsg As String
                Select Case result
                    Case AccountLogonFinishResult.BadPassword
                        errmsg = "Incorrect password."
                    Case AccountLogonFinishResult.NoEmail
                        errmsg = "No email address associated with account"
                    Case AccountLogonFinishResult.Custom
                        errmsg = "Logon error: " + CType(vals("custom error info"), String)
                    Case Else
                        errmsg = "Unrecognized logon error: " + result.ToString()
                End Select
                futureLoggedIn.TrySetValue(failure("Failed to logon: " + errmsg))
                Throw New Exception(errmsg)
            End If

            'validate
            Dim removeServerPasswordProof = CType(vals("server password proof"), Byte())
            If serverPasswordProof Is Nothing Then Throw New InvalidStateException("Received AccountLogonFinish before server password proof computed.")
            Contract.Assume(removeServerPasswordProof IsNot Nothing)
            If Not ArraysEqual(Me.serverPasswordProof, removeServerPasswordProof) Then
                futureLoggedIn.TrySetValue(failure("Failed to logon: Server didn't give correct password proof"))
                Throw New IO.InvalidDataException("Server didn't give correct password proof.")
            End If
            Dim lan_host = profile.lanHost.Split(" "c)(0)
            If lan_host <> "" Then
                Try
                    Dim lan = New W3LanAdvertiser(parent, name, listenPort, lan_host)
                    parent.f_AddWidget(lan)
                    DisposeLink.CreateOneWayLink(Me, lan)
                    AdvertisingLink.CreateMultiWayLink({Me, lan.MakeAdvertisingLinkMember})
                Catch e As Exception
                    logger.log("Error creating lan advertiser: {0}".frmt(e.Message), LogMessageTypes.Problem)
                End Try
            End If
            'log
            logger.log("Logged on with username {0}.".frmt(username), LogMessageTypes.Typical)
            futureLoggedIn.TrySetValue(success("Succesfully logged on with username {0}.".frmt(username)))
            'respond
            SendPacket(BnetPacket.MakeNetGamePort(listenPort))
            SendPacket(BnetPacket.MakeEnterChat())
        End Sub

        Private Sub ReceivePacket_ENTER_CHAT(ByVal vals As Dictionary(Of String, Object))
            logger.log("Entered chat", LogMessageTypes.Typical)
            EnterChannel(profile.initial_channel)
            ref.QueueAction(Sub() SendPacket(BnetPacket.MakeQueryGamesList()))
        End Sub
#End Region

#Region "Networking (Warden)"
        Private Sub ReceivePacket_WARDEN(ByVal vals As Dictionary(Of String, Object))
            Try
                warden = If(warden, New Warden.WardenPacketHandler(wardenSeed, wardenRef, logger))
                Dim data = CType(vals("encrypted data"), Byte())
                warden.ReceiveData(data)
            Catch e As Exception
                c_WardenFail(e)
            End Try
        End Sub
        Private Sub c_WardenSend(ByVal data() As Byte) Handles warden.Send
            ref.QueueAction(Sub()
                                Contract.Assume(data IsNot Nothing)
                                SendPacket(BnetPacket.MakeWarden(data))
                            End Sub)
        End Sub
        Private Sub c_WardenFail(ByVal e As Exception) Handles warden.Fail
            Logging.LogUnexpectedException("Warden", e)
            logger.log("Error dealing with Warden packet. Disconnecting to be safe.", LogMessageTypes.Problem)
            ref.QueueAction(Sub() Disconnect("Error dealing with Warden packet."))
        End Sub
#End Region

#Region "Networking (Games)"
        Private Sub ReceivePacket_CREATE_GAME_3(ByVal vals As Dictionary(Of String, Object))
            Dim succeeded = CUInt(vals("result")) = 0

            If succeeded Then
                If state = States.CreatingGame Then
                    logger.log("Finished creating game.", LogMessageTypes.Positive)
                    ChangeState(States.Game)
                    If Not game_settings.private Then gameRefreshTimer.Start()
                    futureCreatedGame.TrySetValue(success("Succesfully created game {0} for map {1}.".frmt(game_settings.header.name, game_settings.header.map.relativePath)))
                Else
                    ChangeState(States.Game) 'throw event
                End If
            Else
                futureCreatedGame.TrySetValue(failure("BNET didn't allow game creation. Most likely cause is game name in use."))
                gameRefreshTimer.Stop()
                EnterChannel(lastChannel)
                RaiseEvent RemovedGame(Me, game_settings.header, "Client {0} failed to advertise the game. Most likely cause is game name in use.".frmt(Me.name))
            End If
        End Sub
        Private Sub ReceivePacket_GetGames(ByVal vals As Dictionary(Of String, Object))

        End Sub
#End Region

#Region "Networking (Misc)"
        Private Sub ReceiveChatEvent(ByVal vals As Dictionary(Of String, Object))
            Dim eventId = CType(vals("event id"), BnetPacket.ChatEventId)
            Dim user = CStr(vals("username"))
            Dim text = CStr(vals("text"))
            If eventId = BnetPacket.ChatEventId.Channel Then lastChannel = text
            e_ThrowChatEvent(eventId, user, text)
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
            logger.log(msg, LogMessageTypes.Problem)
        End Sub
#End Region

#Region "Interface"
        Private ReadOnly Property _username() As String Implements IBnetClient.username
            Get
                Return username
            End Get
        End Property
        Private ReadOnly Property _Name As String Implements IBnetClient.Name
            Get
                Return name
            End Get
        End Property
        Private ReadOnly Property _Parent As MainBot Implements IBnetClient.Parent
            Get
                Return parent
            End Get
        End Property
        Private ReadOnly Property _profile As ClientProfile Implements IBnetClient.profile
            Get
                Return profile
            End Get
        End Property
        Private ReadOnly Property _logger As Logging.Logger Implements IBnetClient.logger
            Get
                Return logger
            End Get
        End Property
        Private Function _f_SendText(ByVal text As String) As IFuture(Of outcome) Implements IBnetClient.f_SendText
            Return ref.QueueFunc(Function() SendText(text))
        End Function
        Private Function _f_SendWhisper(ByVal username As String, ByVal text As String) As IFuture(Of Outcome) Implements IBnetClient.f_SendWhisper
            Return ref.QueueFunc(Function() SendWhisper(username, text))
        End Function
        Private Function _f_SendPacket(ByVal cp As BnetPacket) As IFuture(Of Outcome) Implements IBnetClient.f_SendPacket
            Return ref.QueueFunc(Function() SendPacket(cp))
        End Function
        Private Function _f_SetListenPort(ByVal new_port As UShort) As IFuture(Of Outcome) Implements IBnetClient.f_SetListenPort
            Return ref.QueueFunc(Function() SetListenPort(new_port))
        End Function
        Private Function _f_StopAdvertisingGame(ByVal reason As String) As IFuture(Of Outcome) Implements IBnetClient.f_StopAdvertisingGame
            Return ref.QueueFunc(Function()
                                     Contract.Assume(reason IsNot Nothing)
                                     Return StopAdvertisingGame(reason)
                                 End Function)
        End Function
        Private Function _f_StartAdvertisingGame(ByVal header As W3GameHeader, ByVal server As IW3Server) As IFuture(Of Outcome) Implements IBnetClient.f_StartAdvertisingGame
            Return ref.QueueFunc(Function()
                                     Contract.Assume(header IsNot Nothing)
                                     Return BeginAdvertiseGame(header, server)
                                 End Function).Defuturize
        End Function
        Private Function _f_Disconnect(ByVal reason As String) As IFuture(Of Outcome) Implements IBnetClient.f_Disconnect
            Return ref.QueueFunc(Function()
                                     Contract.Assume(reason IsNot Nothing)
                                     Return Disconnect(reason)
                                 End Function)
        End Function
        Private Function _f_Connect(ByVal remoteHost As String) As IFuture(Of Outcome) Implements IBnetClient.f_Connect
            Return ref.QueueFunc(Function()
                                     Contract.Assume(remoteHost IsNot Nothing)
                                     Return BeginConnect(remoteHost)
                                 End Function).Defuturize
        End Function
        Private Function _f_Login(ByVal username As String, ByVal password As String) As IFuture(Of Outcome) Implements IBnetClient.f_Login
            Return ref.QueueFunc(Function() BeginLogon(username, password)).Defuturize
        End Function
        Private Function _f_GetUserServer(ByVal user As BotUser) As IFuture(Of IW3Server) Implements IBnetClient.f_GetUserServer
            Return ref.QueueFunc(Function() GetUserServer(user))
        End Function
        Private Function _f_SetUserServer(ByVal user As BotUser, ByVal server As IW3Server) As IFuture Implements IBnetClient.f_SetUserServer
            Return ref.QueueAction(Sub() SetUserServer(user, server))
        End Function
        Private Function _f_listenPort() As IFuture(Of UShort) Implements IBnetClient.f_listenPort
            Return ref.QueueFunc(Function() listenPort)
        End Function
        Private Function _f_GetState() As IFuture(Of States) Implements IBnetClient.f_GetState
            Return ref.QueueFunc(Function() state)
        End Function
#End Region

        Public ReadOnly Property CurGame As GameSettings Implements IBnetClient.CurGame
            Get
                Return Me.game_settings
            End Get
        End Property
    End Class
End Namespace
