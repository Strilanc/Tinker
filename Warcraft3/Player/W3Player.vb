Namespace WC3
    Public Enum HostTestResult As Integer
        Fail = -1
        Test = 0
        Pass = 1
    End Enum
    Public Enum PlayerLeaveType As Byte
        Disconnect = 1
        Lose = 7
        MeleeLose = 8
        Win = 9
        Draw = 10
        Observer = 11
        Lobby = 13
    End Enum
    Public Enum PlayerState
        Lobby
        Loading
        Playing
    End Enum

    Partial Public NotInheritable Class Player
        Inherits FutureDisposable

        Private state As PlayerState = PlayerState.Lobby
        Private ReadOnly _index As Byte
        Private ReadOnly testCanHost As IFuture
        Private Const MAX_NAME_LENGTH As Integer = 15
        Private ReadOnly socket As W3Socket
        Private ReadOnly packetHandler As W3PacketHandler
        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue
        Private _numPeerConnections As Integer
        Private ReadOnly settings As GameSettings
        Private ReadOnly scheduler As TransferScheduler(Of Byte)
        Private ReadOnly pinger As Pinger

        Private ReadOnly _name As InvariantString
        Public ReadOnly listenPort As UShort
        Public ReadOnly peerKey As UInteger
        Public ReadOnly isFake As Boolean
        Private ReadOnly logger As Logger

        Public hasVotedToStart As Boolean
        Public numAdminTries As Integer
        Public Event Disconnected(ByVal sender As Player, ByVal expected As Boolean, ByVal leaveType As PlayerLeaveType, ByVal reason As String)

        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Index As Byte
            Get
                Contract.Ensures(Contract.Result(Of Byte)() > 0)
                Contract.Ensures(Contract.Result(Of Byte)() <= 12)
                Return _index
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_numPeerConnections >= 0)
            Contract.Invariant(_numPeerConnections <= 12)
            Contract.Invariant(tickQueue IsNot Nothing)
            Contract.Invariant(packetHandler IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(socket Is Nothing = isFake)
            Contract.Invariant(testCanHost IsNot Nothing)
            Contract.Invariant(numAdminTries >= 0)
            Contract.Invariant(settings IsNot Nothing)
            Contract.Invariant(socket IsNot Nothing)
            Contract.Invariant(scheduler IsNot Nothing)
            Contract.Invariant(_index > 0)
            Contract.Invariant(_index <= 12)
            Contract.Invariant(totalTockTime >= 0)
            Contract.Invariant(mapDownloadPosition >= 0)
        End Sub

        '''<summary>Creates a fake player.</summary>
        Public Sub New(ByVal index As Byte,
                       ByVal settings As GameSettings,
                       ByVal scheduler As TransferScheduler(Of Byte),
                       ByVal name As InvariantString,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(index > 0)
            Contract.Assume(index <= 12)

            Me.settings = settings
            Me.scheduler = scheduler
            Me.logger = If(logger, New Logger)
            Me.packetHandler = New W3PacketHandler(Me.logger)
            Me._index = index
            If name.Length > MAX_NAME_LENGTH Then Throw New ArgumentException("Player name must be less than 16 characters long.")
            Me._name = name
            isFake = True
            LobbyStart()
            Dim hostFail = New FutureAction
            hostFail.SetFailed(New ArgumentException("Fake players can't host."))
            Me.testCanHost = hostFail
            Me.testCanHost.SetHandled()
        End Sub

        '''<summary>Creates a real player.</summary>
        Public Sub New(ByVal index As Byte,
                       ByVal settings As GameSettings,
                       ByVal scheduler As TransferScheduler(Of Byte),
                       ByVal connectingPlayer As W3ConnectingPlayer,
                       ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            'Contract.Requires(index > 0)
            'Contract.Requires(index <= 12)
            'Contract.Requires(game IsNot Nothing)
            'Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Assume(index > 0)
            Contract.Assume(index <= 12)
            Contract.Assume(connectingPlayer IsNot Nothing)

            Me.settings = settings
            Me.scheduler = scheduler
            Me.logger = If(logger, New Logger)
            Me.packetHandler = New W3PacketHandler(Me.logger)
            connectingPlayer.Socket.Logger = Me.logger
            Me.peerKey = connectingPlayer.PeerKey

            Me.socket = connectingPlayer.Socket
            Me._name = connectingPlayer.Name
            Me.listenPort = connectingPlayer.ListenPort
            Me._index = index
            AddHandler socket.Disconnected, AddressOf CatchSocketDisconnected

            AddQueuedPacketHandler(PacketId.Pong,
                                   Packet.Jars.Pong,
                                   handler:=Function(pickle)
                                                outQueue.QueueAction(Sub() RaiseEvent SuperficialStateUpdated(Me))
                                                Return pinger.QueueReceivedPong(CUInt(pickle.Value("salt")))
                                            End Function)
            AddQueuedPacketHandler(PacketId.NonGameAction,
                                   Packet.Jars.NonGameAction,
                                   handler:=AddressOf ReceiveNonGameAction)
            AddQueuedPacketHandler(Packet.Jars.Leaving, AddressOf ReceiveLeaving)
            AddQueuedPacketHandler(Packet.Jars.MapFileDataReceived, AddressOf IgnorePacket)
            AddQueuedPacketHandler(Packet.Jars.MapFileDataProblem, AddressOf IgnorePacket)

            LobbyStart()
            BeginReading()

            'Test hosting
            Me.testCanHost = AsyncTcpConnect(socket.RemoteEndPoint.Address, listenPort)
            Me.testCanHost.SetHandled()

            'Pings
            pinger = New Pinger(period:=5.Seconds, timeoutCount:=10, clock:=clock)
            AddHandler pinger.SendPing, Sub(sender, salt) QueueSendPacket(Packet.MakePing(salt))
            AddHandler pinger.Timeout, Sub(sender) QueueDisconnect(expected:=False,
                                                                   leaveType:=PlayerLeaveType.Disconnect,
                                                                   reason:="Stopped responding to pings.")
        End Sub

        Private Sub BeginReading()
            AsyncProduceConsumeUntilError(
                producer:=AddressOf socket.AsyncReadPacket,
                consumer:=AddressOf ProcessPacket,
                errorHandler:=Sub(exception) Me.QueueDisconnect(expected:=False,
                                                                leaveType:=PlayerLeaveType.Disconnect,
                                                                reason:="Error receiving packet: {0}".Frmt(exception.Message))
            )
        End Sub

        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Private Function ProcessPacket(ByVal packetData As IReadableList(Of Byte)) As ifuture
            Contract.Requires(packetData IsNot Nothing)
            Contract.Requires(packetData.Count >= 4)
            Dim result = packetHandler.HandlePacket(packetData, "test")
            result.Catch(Sub(exception) QueueDisconnect(expected:=False,
                                                        leaveType:=PlayerLeaveType.Disconnect,
                                                        reason:=exception.Message))
            Return result
        End Function

        '''<summary>Disconnects this player and removes them from the system.</summary>
        Private Sub Disconnect(ByVal expected As Boolean,
                               ByVal leaveType As PlayerLeaveType,
                               ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            If Not Me.isFake Then
                socket.Disconnect(expected, reason)
            End If
            If pinger IsNot Nothing Then pinger.Dispose()
            RaiseEvent Disconnected(Me, expected, leaveType, reason)
        End Sub

        Private Sub SendPacket(ByVal pk As Packet)
            Contract.Requires(pk IsNot Nothing)
            If Me.isFake Then Return
            socket.SendPacket(pk)
        End Sub

        Private Sub CatchSocketDisconnected(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reason As String)
            inQueue.QueueAction(Sub() Disconnect(expected, PlayerLeaveType.Disconnect, reason))
        End Sub

        Public ReadOnly Property CanHost() As HostTestResult
            Get
                Dim testState = testCanHost.State
                Select Case testState
                    Case FutureState.Failed : Return HostTestResult.Fail
                    Case FutureState.Succeeded : Return HostTestResult.Pass
                    Case FutureState.Unknown : Return HostTestResult.Test
                    Case Else : Throw testState.MakeImpossibleValueException()
                End Select
            End Get
        End Property

        Public ReadOnly Property RemoteEndPoint As Net.IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address IsNot Nothing)
                If isFake Then Return New Net.IPEndPoint(New Net.IPAddress({0, 0, 0, 0}), 0)
                Return socket.RemoteEndPoint
            End Get
        End Property
        Public Function QueueGetLatency() As IFuture(Of FiniteDouble)
            Contract.Ensures(Contract.Result(Of IFuture(Of FiniteDouble))() IsNot Nothing)
            If pinger Is Nothing Then
                Return New FiniteDouble().Futurized
            Else
                Return pinger.QueueGetLatency
            End If
        End Function
        Public ReadOnly Property QueueGetLatencyDescription As IFuture(Of String)
            Get
                Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
                Dim futureLatency = QueueGetLatency()
                Dim futureTransferState = scheduler.GetClientState(Me.Index)
                Return New IFuture() {futureLatency, futureTransferState}.Defuturized.EvalOnSuccess(
                    Function()
                        Dim downloadState = futureTransferState.Value
                        Dim r = futureLatency.Value
                        Select Case downloadState
                            Case ClientTransferState.None : Return "-"
                            Case ClientTransferState.Downloading : Return "(dl)"
                            Case ClientTransferState.Uploading : Return "(ul)"
                            Case ClientTransferState.Idle : Return If(r = 0, "?", "{0:0}ms".Frmt(r.Value))
                            Case ClientTransferState.Ready : Return If(r = 0, "?", "{0:0}ms".Frmt(r.Value))
                            Case Else : Throw downloadState.MakeImpossibleValueException()
                        End Select
                    End Function)
            End Get
        End Property
        Public ReadOnly Property NumPeerConnections() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return _numPeerConnections
            End Get
        End Property

        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal leaveType As PlayerLeaveType, ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() Disconnect(expected, leaveType, reason))
        End Function
        Public Function QueueSendPacket(ByVal packet As Packet) As IFuture
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendPacket(packet))
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            Return QueueDisconnect(expected:=True, leaveType:=PlayerLeaveType.Disconnect, reason:="Disposed")
        End Function
    End Class
End Namespace
