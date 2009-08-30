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
        Event Disconnected(ByVal sender As IBnetClient, ByVal reason As String)
        Event ReceivedPacket(ByVal sender As IBnetClient, ByVal packet As BnetPacket)
    End Interface
    <ContractClassFor(GetType(IBnetClient))>
    Public NotInheritable Class ContractClassIBnetClient
        Implements IBnetClient
        Public Event StateChanged(ByVal sender As IBnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States) Implements IBnetClient.StateChanged
        Public Event Disconnected(ByVal sender As IBnetClient, ByVal reason As String) Implements IBnetClient.Disconnected
        Private Event ReceivedPacket(ByVal sender As IBnetClient, ByVal packet As BnetPacket) Implements IBnetClient.ReceivedPacket

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

        Public Function f_listenPort() As IFuture(Of UShort) Implements IBnetClient.f_listenPort
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

        Public Function f_StopAdvertisingGame(ByVal reason As String) As IFuture(Of Outcome) Implements IBnetClient.f_StopAdvertisingGame
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
        Private advertisedGameSettings As GameSettings
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
        Private futureCreatedGame As New Future(Of Outcome)

        'events
        Private Event StateChanged(ByVal sender As IBnetClient, ByVal old_state As States, ByVal new_state As States) Implements IBnetClient.StateChanged
        Private Event ReceivedPacket(ByVal sender As IBnetClient, ByVal packet As BnetPacket) Implements IBnetClient.ReceivedPacket

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

        <ContractInvariantMethod()> Protected Overrides Sub Invariant()
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
            AddHandler gameRefreshTimer.Elapsed, Sub() c_RefreshTimerTick()

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
        Private Function SendText(ByVal text As String) As outcome
            Contract.Requires(text IsNot Nothing)
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
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
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
        Private Sub CatchSocketDisconnected(ByVal sender As BnetSocket, ByVal reason As String)
            ref.QueueAction(Sub()
                                Contract.Assume(reason IsNot Nothing)
                                Disconnect(reason)
                            End Sub)
        End Sub

        Private Sub c_RefreshTimerTick()
            ref.QueueAction(Sub() AdvertiseGame(False, True))
        End Sub
#End Region

#Region "State"
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            ref.QueueAction(Sub() Disconnect("{0} Disposed".frmt(Me.GetType.Name)))
            parent.QueueRemoveClient(Me.name)
        End Sub
        Private Sub ChangeState(ByVal newState As States)
            Dim oldState = state
            state = newState
            eref.QueueAction(Sub()
                                 RaiseEvent StateChanged(Me, oldState, newState)
                             End Sub)
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
                    If out Is Nothing Then
                        Return failure("No listen port specified, and no ports available in the pool.").Futurize()
                    End If
                    Me.poolPort = out
                    Me.listenPort = Me.poolPort.Port
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
                Return FutureConnectTo(remoteHost, port).EvalWhenValueReady(Function(result) ref.QueueFunc(
                    Function()
                        Try
                            If result.Exception IsNot Nothing Then  Return failure("Failed to connect: {0}".frmt(result.Exception.Message)).Futurize
                            Contract.Assume(result.Value IsNot Nothing)
                            socket = New BnetSocket(New PacketSocket(result.Value,
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
                            Me.futureConnected.TrySetValue(failure("Another connection was initiated."))
                            Me.futureConnected = New Future(Of Outcome)()

                            'Start log-on process
                            result.Value.GetStream.Write({1}, 0, 1)
                            SendPacket(BnetPacket.MakeAuthenticationBegin(MainBot.Wc3MajorVersion, GetCachedIpAddressBytes(external:=False)))

                            'Start handling incoming packets
                            FutureIterate(AddressOf socket.FutureReadPacket, Function(result2) ref.QueueFunc(
                                Function()
                                    If result2.Exception IsNot Nothing Then
                                        If TypeOf result2.Exception Is Pickling.PicklingException Then
                                            Return True
                                        ElseIf Not (TypeOf result2.Exception Is SocketException OrElse
                                                    TypeOf result2.Exception Is ObjectDisposedException OrElse
                                                    TypeOf result2.Exception Is IO.IOException) Then
                                            LogUnexpectedException("Error receiving data from bnet server", result2.Exception)
                                        End If
                                        Disconnect("Error receiving packet: {0}.".frmt(result2.Exception))
                                        Return False
                                    End If

                                    Try
                                        Dim packet = result2.Value

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
                                        Disconnect("Error handling packet: {0}.".frmt(e))
                                        Return False
                                    End Try
                                End Function
                            ))
                            Return Me.futureConnected
                        Catch e As Exception
                            Disconnect("Failed to complete connection.")
                            Return failure("Error connecting: " + e.ToString).Futurize()
                        End Try
                    End Function
                )).Defuturize.Defuturize
            Catch e As Exception
                Disconnect("Failed to start connection.")
                Return failure("Error connecting: " + e.ToString).Futurize()
            End Try
        End Function

        Private Function BeginLogon(ByVal username As String,
                                    ByVal password As String) As IFuture(Of Outcome)
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
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
                RemoveHandler socket.Disconnected, AddressOf CatchSocketDisconnected
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

            Dim reason_ = reason
            eref.QueueAction(Sub()
                                 RaiseEvent Disconnected(Me, reason_)
                             End Sub)
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
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)

            Select Case state
                Case States.Disconnected, States.Connecting, States.EnterUsername, States.Logon
                    Return failure("Can't advertise a game until connected.").Futurize()
                Case States.CreatingGame
                    Return failure("Already creating a game.").Futurize()
                Case States.Game
                    Return failure("Already advertising a game.").Futurize()
                Case States.Channel
                    advertisedGameSettings = New GameSettings(game)
                    futureCreatedGame = New Future(Of Outcome)
                    ChangeState(States.CreatingGame)
                    hostCount += 1
                    Dim out = AdvertiseGame(False, False)
                    If Not out.succeeded Then
                        futureCreatedGame.TrySetValue(failure("Failed to send data."))
                        ChangeState(States.Channel)
                        Return out.Futurize()
                    End If

                    Dim game_ = game
                    Dim server_ = server
                    eref.QueueAction(Sub()
                                         RaiseEvent AddedGame(Me, game_, server_)
                                     End Sub)
                    If server IsNot Nothing Then
                        server.QueueAddAvertiser(Me)
                        DisposeLink.CreateOneWayLink(New AdvertisingDisposeNotifier(Me), server.CreateAdvertisingDependency)
                        server.QueueOpenPort(Me.listenPort).CallWhenValueReady(Sub(listened) ref.QueueAction(
                            Sub()
                                If Not listened.succeeded Then
                                    futureCreatedGame.TrySetValue(listened)
                                    StopAdvertisingGame(listened.Message)
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

                Return SendPacket(BnetPacket.MakeCreateGame3(New W3GameHeaderAndState(gameState,
                                                                                      advertisedGameSettings.header,
                                                                                      gameType),
                                                             hostCount))
            Catch e As ArgumentException
                Return failure("Error sending packet: {0}.".frmt(e.ToString))
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
                    Dim reason_ = reason
                    eref.QueueAction(Sub()
                                         RaiseEvent RemovedGame(Me, advertisedGameSettings.header, reason_)
                                     End Sub)
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

            Protected Overrides Sub Dispose(ByVal disposing As Boolean)
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

                    advertisedGameSettings.private = [private]
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
        <Pure()> Private Sub _f_RemoveAdvertisingPartner(ByVal other As IGameSourceSink) Implements IBnetClient.ClearAdvertisingPartner
            Dim other_ = other
            eref.QueueAction(Sub()
                                 RaiseEvent DisposedAdvertisingLink(Me, other_)
                             End Sub)
        End Sub
#End Region

#Region "Networking"
        Private Function SendPacket(ByVal packet As BnetPacket) As Outcome
            Contract.Requires(packet IsNot Nothing)
            Return socket.SendPacket(packet)
        End Function
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
                                                       R).CallWhenValueReady(
                    Sub(out)
                        ref.QueueAction(
                            Sub()
                                If out.succeeded Then
                                    Contract.Assume(out.Value IsNot Nothing)
                                    Dim rocKeyData = CType(CType(out.Value.payload.Value, Dictionary(Of String, Object))("ROC cd key"), Dictionary(Of String, Object))
                                    Dim rocHash = CType(rocKeyData("hash"), Byte())
                                    Contract.Assume(rocHash IsNot Nothing)
                                    Me.wardenSeed = rocHash.SubArray(0, 4).ToUInt32(ByteOrder.LittleEndian)
                                    logger.Log(out.Message, LogMessageTypes.Positive)
                                    SendPacket(out.Value)
                                Else
                                    logger.Log(out.Message, LogMessageTypes.Negative)
                                    futureConnected.TrySetValue(Failure("Failed to borrow keys: '{0}'.".Frmt(out.Message)))
                                    Disconnect("Error borrowing keys.")
                                End If
                            End Sub
                        )
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
                Me.wardenSeed = rocHash.SubArray(0, 4).ToUInt32(ByteOrder.LittleEndian)
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
        Private Sub ReceiveAuthenticationFinish(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
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
            Contract.Requires(vals IsNot Nothing)
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
            Contract.Requires(vals IsNot Nothing)
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
                    parent.QueueAddWidget(lan)
                    DisposeLink.CreateOneWayLink(Me, lan)
                    AdvertisingLink.CreateMultiWayLink({Me, lan.MakeAdvertisingLinkMember})
                Catch e As Exception
                    logger.log("Error creating lan advertiser: {0}".frmt(e.ToString), LogMessageTypes.Problem)
                End Try
            End If
            'log
            logger.log("Logged on with username {0}.".frmt(username), LogMessageTypes.Typical)
            futureLoggedIn.TrySetValue(success("Succesfully logged on with username {0}.".frmt(username)))
            'respond
            SendPacket(BnetPacket.MakeNetGamePort(listenPort))
            SendPacket(BnetPacket.MakeEnterChat())
        End Sub

        Private Sub ReceiveEnterChat(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            logger.log("Entered chat", LogMessageTypes.Typical)
            EnterChannel(profile.initialChannel)
        End Sub
#End Region

#Region "Networking (Warden)"
        Private Sub ReceiveWarden(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            'Try
            '    warden = If(warden, New Warden.WardenPacketHandler(wardenSeed, wardenRef, logger))
            '    Dim data = CType(vals("encrypted data"), Byte())
            '    warden.ReceiveData(data)
            'Catch e As Exception
            '    c_WardenFail(e)
            'End Try
        End Sub
        Private Sub c_WardenSend(ByVal data() As Byte) Handles warden.Send
            ref.QueueAction(Sub()
                                Contract.Assume(data IsNot Nothing)
                                SendPacket(BnetPacket.MakeWarden(data))
                            End Sub)
        End Sub
        Private Sub c_WardenFail(ByVal e As Exception) Handles warden.Fail
            LogUnexpectedException("Warden", e)
            logger.log("Error dealing with Warden packet. Disconnecting to be safe.", LogMessageTypes.Problem)
            ref.QueueAction(Sub() Disconnect("Error dealing with Warden packet."))
        End Sub
#End Region

#Region "Networking (Games)"
        Private Sub ReceiveCreateGame3(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Dim succeeded = CUInt(vals("result")) = 0

            If succeeded Then
                If state = States.CreatingGame Then
                    logger.log("Finished creating game.", LogMessageTypes.Positive)
                    ChangeState(States.Game)
                    If Not advertisedGameSettings.private Then gameRefreshTimer.Start()
                    futureCreatedGame.TrySetValue(success("Succesfully created game {0} for map {1}.".frmt(advertisedGameSettings.header.Name, advertisedGameSettings.header.Map.relativePath)))
                Else
                    ChangeState(States.Game) 'throw event
                End If
            Else
                futureCreatedGame.TrySetValue(failure("BNET didn't allow game creation. Most likely cause is game name in use."))
                gameRefreshTimer.Stop()
                EnterChannel(lastChannel)
                RaiseEvent RemovedGame(Me, advertisedGameSettings.header, "Client {0} failed to advertise the game. Most likely cause is game name in use.".frmt(Me.name))
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
        Private ReadOnly Property _logger As Logger Implements IBnetClient.logger
            Get
                Return logger
            End Get
        End Property
        Private Function _f_SendText(ByVal text As String) As IFuture(Of outcome) Implements IBnetClient.f_SendText
            Return ref.QueueFunc(Function()
                                     Contract.Assume(text IsNot Nothing)
                                     Return SendText(text)
                                 End Function)
        End Function
        Private Function _f_SendWhisper(ByVal username As String, ByVal text As String) As IFuture(Of Outcome) Implements IBnetClient.f_SendWhisper
            Return ref.QueueFunc(Function()
                                     Contract.Assume(username IsNot Nothing)
                                     Contract.Assume(text IsNot Nothing)
                                     Return SendWhisper(username, text)
                                 End Function)
        End Function
        Private Function _f_SendPacket(ByVal cp As BnetPacket) As IFuture(Of Outcome) Implements IBnetClient.f_SendPacket
            Return ref.QueueFunc(Function()
                                     Contract.Assume(cp IsNot Nothing)
                                     Return SendPacket(cp)
                                 End Function)
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
            Return ref.QueueFunc(Function()
                                     Contract.Assume(username IsNot Nothing)
                                     Contract.Assume(password IsNot Nothing)
                                     Return BeginLogon(username, password)
                                 End Function).Defuturize
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
                Return Me.advertisedGameSettings
            End Get
        End Property
    End Class
End Namespace
