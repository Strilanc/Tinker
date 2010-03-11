Namespace WC3
    Public Enum HostTestResult As Integer
        Fail = -1
        Test = 0
        Pass = 1
    End Enum
    Public Enum PlayerState
        Lobby
        Loading
        Playing
    End Enum

    Partial Public NotInheritable Class Player
        Inherits DisposableWithTask

        Private state As PlayerState = PlayerState.Lobby
        Private ReadOnly _id As PlayerId
        Private ReadOnly testCanHost As Task
        Private ReadOnly socket As W3Socket
        Private ReadOnly packetHandler As Protocol.W3PacketHandler
        Private ReadOnly inQueue As CallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As CallQueue = New TaskedCallQueue
        Private _numPeerConnections As Integer
        Private _downloadManager As Download.Manager
        Private ReadOnly pinger As Pinger

        Private ReadOnly _name As InvariantString
        Private ReadOnly _listenPort As UShort
        Public ReadOnly peerKey As UInteger
        Private ReadOnly _peerData As IReadableList(Of Byte)
        Public ReadOnly isFake As Boolean
        Private ReadOnly logger As Logger

        Public hasVotedToStart As Boolean
        Public adminAttemptCount As Integer
        Public Event Disconnected(ByVal sender As Player, ByVal expected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_numPeerConnections >= 0)
            Contract.Invariant(_numPeerConnections <= 12)
            Contract.Invariant(tickQueue IsNot Nothing)
            Contract.Invariant(packetHandler IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_peerData IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(socket Is Nothing = isFake)
            Contract.Invariant(testCanHost IsNot Nothing)
            Contract.Invariant(adminAttemptCount >= 0)
            Contract.Invariant(socket IsNot Nothing)
            Contract.Invariant(totalTockTime >= 0)
        End Sub

        '''<summary>Creates a fake player.</summary>
        Public Sub New(ByVal id As PlayerId,
                       ByVal name As InvariantString,
                       Optional ByVal logger As Logger = Nothing)
            Me.logger = If(logger, New Logger)
            Me.packetHandler = New Protocol.W3PacketHandler(name, Me.logger)
            Me._id = id
            Me._peerData = New Byte() {0}.AsReadableList
            If name.Length > Protocol.Packets.MaxPlayerNameLength Then Throw New ArgumentException("Player name must be less than 16 characters long.")
            Me._name = name
            isFake = True
            LobbyStart()
            Dim hostFail = New TaskCompletionSource(Of Boolean)
            hostFail.SetException(New ArgumentException("Fake players can't host."))
            Me.testCanHost = hostFail.Task
            Me.testCanHost.IgnoreExceptions()
        End Sub

        '''<summary>Creates a real player.</summary>
        Public Sub New(ByVal id As PlayerId,
                       ByVal connectingPlayer As W3ConnectingPlayer,
                       ByVal clock As IClock,
                       ByVal downloadManager As Download.Manager,
                       Optional ByVal logger As Logger = Nothing)
            'Contract.Requires(game IsNot Nothing)
            'Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Assume(connectingPlayer IsNot Nothing)

            Me.logger = If(logger, New Logger)
            Me.packetHandler = New Protocol.W3PacketHandler(connectingPlayer.Name, Me.logger)
            connectingPlayer.Socket.Logger = Me.logger
            Me.peerKey = connectingPlayer.PeerKey
            Me._peerData = connectingPlayer.PeerData

            Me._downloadManager = downloadManager
            Me.socket = connectingPlayer.Socket
            Me._name = connectingPlayer.Name
            Me._listenPort = connectingPlayer.ListenPort
            Me._id = id
            AddHandler socket.Disconnected, AddressOf OnSocketDisconnected

            AddRemotePacketHandler(Protocol.Packets.Pong, Function(pickle)
                                                              outQueue.QueueAction(Sub() RaiseEvent SuperficialStateUpdated(Me))
                                                              Return pinger.QueueReceivedPong(pickle.Value)
                                                          End Function)
            AddQueuedLocalPacketHandler(Protocol.Packets.NonGameAction, AddressOf ReceiveNonGameAction)
            AddQueuedLocalPacketHandler(Protocol.Packets.Leaving, AddressOf ReceiveLeaving)
            AddQueuedLocalPacketHandler(Protocol.Packets.MapFileDataReceived, AddressOf IgnorePacket)
            AddQueuedLocalPacketHandler(Protocol.Packets.MapFileDataProblem, AddressOf IgnorePacket)

            LobbyStart()

            'Test hosting
            Me.testCanHost = AsyncTcpConnect(socket.RemoteEndPoint.Address, ListenPort)
            Me.testCanHost.IgnoreExceptions()

            'Pings
            pinger = New Pinger(period:=5.Seconds, timeoutCount:=10, clock:=clock)
            AddHandler pinger.SendPing, Sub(sender, salt) QueueSendPacket(Protocol.MakePing(salt))
            AddHandler pinger.Timeout, Sub(sender) QueueDisconnect(expected:=False,
                                                                   reportedReason:=Protocol.PlayerLeaveReason.Disconnect,
                                                                   reasonDescription:="Stopped responding to pings.")
        End Sub

        Public ReadOnly Property Name As InvariantString Implements Download.IPlayerDownloadAspect.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Id As PlayerId Implements Download.IPlayerDownloadAspect.Id
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property ListenPort As UShort
            Get
                Return _listenPort
            End Get
        End Property
        Public ReadOnly Property PeerData As IReadableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                Return _peerData
            End Get
        End Property


        Public Sub QueueStart()
            inQueue.QueueAction(Sub() BeginReading())
        End Sub

        Private Sub BeginReading()
            AsyncProduceConsumeUntilError(
                producer:=AddressOf socket.AsyncReadPacket,
                consumer:=Function(packetData) packetHandler.HandlePacket(packetData),
                errorHandler:=Sub(exception) QueueDisconnect(expected:=False,
                                                             reportedReason:=Protocol.PlayerLeaveReason.Disconnect,
                                                             reasonDescription:="Error receiving packet: {0}.".Frmt(exception.Summarize)))
        End Sub

        '''<summary>Disconnects this player and removes them from the system.</summary>
        Private Sub Disconnect(ByVal expected As Boolean,
                               ByVal reportedReason As Protocol.PlayerLeaveReason,
                               ByVal reasonDescription As String)
            Contract.Requires(reasonDescription IsNot Nothing)
            If Not Me.isFake Then
                socket.Disconnect(expected, reasonDescription)
            End If
            If pinger IsNot Nothing Then pinger.Dispose()
            RaiseEvent Disconnected(Me, expected, reportedReason, reasonDescription)
            Me.Dispose()
        End Sub
        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String) As Task Implements Download.IPlayerDownloadAspect.QueueDisconnect
            Return inQueue.QueueAction(Sub() Disconnect(expected, reportedReason, reasonDescription))
        End Function

        Private Sub SendPacket(ByVal pk As Protocol.Packet)
            Contract.Requires(pk IsNot Nothing)
            If Me.isFake Then Return
            socket.SendPacket(pk)
        End Sub
        Public Function QueueSendPacket(ByVal packet As Protocol.Packet) As Task Implements Download.IPlayerDownloadAspect.QueueSendPacket
            Dim result = inQueue.QueueAction(Sub() SendPacket(packet))
            result.IgnoreExceptions()
            Return result
        End Function

        Private Sub OnSocketDisconnected(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reasonDescription As String)
            inQueue.QueueAction(Sub() Disconnect(expected, Protocol.PlayerLeaveReason.Disconnect, reasonDescription))
        End Sub

        Public ReadOnly Property CanHost() As HostTestResult
            Get
                Dim testState = testCanHost.Status
                Select Case testState
                    Case TaskStatus.Faulted : Return HostTestResult.Fail
                    Case TaskStatus.RanToCompletion : Return HostTestResult.Pass
                    Case Else : Return HostTestResult.Test
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
        Public Function QueueGetLatency() As Task(Of Double)
            Contract.Ensures(Contract.Result(Of Task(Of Double))() IsNot Nothing)
            If pinger Is Nothing Then
                Return 0.0.AsTask
            Else
                Return pinger.QueueGetLatency
            End If
        End Function
        <ContractVerification(False)>
        Public Function QueueGetLatencyDescription() As Task(Of String)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Return (From latency In QueueGetLatency()
                    Select latencyDesc = If(latency = 0, "?", "{0:0}ms".Frmt(latency))
                    Select If(_downloadManager Is Nothing,
                              latencyDesc.AsTask,
                              _downloadManager.QueueGetClientLatencyDescription(Me, latencyDesc))
                   ).Unwrap.AssumeNotNull
        End Function
        Public ReadOnly Property PeerConnectionCount() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return _numPeerConnections
            End Get
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return QueueDisconnect(expected:=True, reportedReason:=Protocol.PlayerLeaveReason.Disconnect, reasonDescription:="Disposed")
        End Function
    End Class
End Namespace
