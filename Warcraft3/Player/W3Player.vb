Imports Tinker.Pickling

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
        Implements Download.IPlayerDownloadAspect

        Private ReadOnly _id As PlayerId
        Private ReadOnly _name As InvariantString
        Private ReadOnly _peerKey As UInt32
        Private ReadOnly _peerData As IReadableList(Of Byte)
        Private ReadOnly _listenPort As UShort
        Private ReadOnly _remoteEndPoint As Net.IPEndPoint

        Private ReadOnly _isFake As Boolean
        Private ReadOnly _logger As Logger
        Private ReadOnly _inQueue As CallQueue
        Private ReadOnly _outQueue As CallQueue

        Private ReadOnly _pinger As Pinger
        Private ReadOnly _socket As W3Socket
        Private ReadOnly _tickQueue As New Queue(Of TickRecord)
        Private ReadOnly _packetHandler As Protocol.W3PacketHandler
        Private ReadOnly _taskTestCanHost As Task

        Private _ready As Boolean
        Private _state As PlayerState
        Private _maxTockTime As Integer
        Private _totalTockTime As Integer
        Private _downloadManager As Download.Manager
        Private _numPeerConnections As Integer
        Private _reportedDownloadPosition As UInt32?

        Public Property HasVotedToStart As Boolean
        Public Property AdminAttemptCount As Integer

        Public Event SuperficialStateUpdated(ByVal sender As Player)
        Public Event StateUpdated(ByVal sender As Player)
        Public Event Disconnected(ByVal sender As Player, ByVal expected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_inQueue IsNot Nothing)
            Contract.Invariant(_outQueue IsNot Nothing)
            Contract.Invariant(_peerData IsNot Nothing)
            Contract.Invariant(_tickQueue IsNot Nothing)
            Contract.Invariant(_packetHandler IsNot Nothing)
            Contract.Invariant(_remoteEndPoint IsNot Nothing)
            Contract.Invariant(_remoteEndPoint.Address IsNot Nothing)
            Contract.Invariant(_taskTestCanHost IsNot Nothing)

            Contract.Invariant(_socket Is Nothing = IsFake)
            Contract.Invariant(_totalTockTime >= 0)
            Contract.Invariant(AdminAttemptCount >= 0)
            Contract.Invariant(_numPeerConnections >= 0)
            Contract.Invariant(_numPeerConnections <= 12)
        End Sub

        Public Sub New(ByVal id As PlayerId,
                       ByVal name As InvariantString,
                       ByVal isFake As Boolean,
                       ByVal logger As Logger,
                       ByVal peerKey As UInt32,
                       ByVal peerData As IReadableList(Of Byte),
                       ByVal listenPort As UInt16,
                       ByVal packetHandler As Protocol.W3PacketHandler,
                       ByVal remoteEndPoint As Net.IPEndPoint,
                       ByVal taskTestCanHost As Task,
                       Optional ByVal pinger As Pinger = Nothing,
                       Optional ByVal socket As W3Socket = Nothing,
                       Optional ByVal initialState As PlayerState = PlayerState.Lobby,
                       Optional ByVal downloadManager As Download.Manager = Nothing,
                       Optional ByVal inQueue As CallQueue = Nothing,
                       Optional ByVal outQueue As CallQueue = Nothing)
            Contract.Requires(peerData IsNot Nothing)
            Contract.Requires(remoteEndPoint IsNot Nothing)
            Contract.Requires(remoteEndPoint.Address IsNot Nothing)
            Contract.Requires(taskTestCanHost IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            If name.Length > Protocol.Packets.MaxPlayerNameLength Then Throw New ArgumentException("Player name must be less than 16 characters long.")

            Me._id = id
            Me._name = name
            Me._state = initialState
            Me._isFake = isFake
            Me._logger = logger
            Me._pinger = pinger
            Me._socket = socket
            Me._peerKey = peerKey
            Me._peerData = peerData
            Me._listenPort = listenPort
            Me._packetHandler = packetHandler
            Me._remoteEndPoint = remoteEndPoint
            Me._taskTestCanHost = taskTestCanHost
            Me._downloadManager = downloadManager
            Me._inQueue = If(inQueue, New TaskedCallQueue)
            Me._outQueue = If(outQueue, New TaskedCallQueue)

            Me._taskTestCanHost.IgnoreExceptions()
        End Sub

        Public Shared Function MakeFake(ByVal id As PlayerId,
                                        ByVal name As InvariantString,
                                        Optional ByVal logger As Logger = Nothing) As Player
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            Dim hostFail = New TaskCompletionSource(Of Boolean)
            hostFail.SetException(New ArgumentException("Fake players can't host."))
            logger = If(logger, New Logger)

            Dim player = New Player(id:=id,
                                    name:=name,
                                    IsFake:=True,
                                    logger:=logger,
                                    PeerKey:=0,
                                    PeerData:=New Byte() {0}.AsReadableList,
                                    ListenPort:=0,
                                    PacketHandler:=New Protocol.W3PacketHandler(name, logger),
                                    RemoteEndPoint:=New Net.IPEndPoint(New Net.IPAddress({0, 0, 0, 0}), 0),
                                    taskTestCanHost:=hostFail.Task)
            Return player
        End Function

        Public Shared Function MakeRemote(ByVal id As PlayerId,
                                          ByVal knockData As Protocol.KnockData,
                                          ByVal socket As W3Socket,
                                          ByVal clock As IClock,
                                          ByVal downloadManager As Download.Manager,
                                          Optional ByVal logger As Logger = Nothing) As Player
            Contract.Assume(knockData IsNot Nothing)
            Contract.Assume(socket IsNot Nothing)
            Contract.Assume(clock IsNot Nothing)
            Contract.Assume(downloadManager IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            logger = If(logger, New Logger)
            socket.Logger = logger
            Dim taskTestCanHost = AsyncTcpConnect(socket.RemoteEndPoint.Address, knockData.ListenPort)
            taskTestCanHost.ContinueWithAction(Sub(value) value.Close()).IgnoreExceptions()
            Dim pinger = New Pinger(period:=5.Seconds, timeoutCount:=10, clock:=clock)

            Dim player = New Player(id:=id,
                                    Name:=knockData.Name,
                                    IsFake:=False,
                                    logger:=logger,
                                    PeerKey:=knockData.PeerKey,
                                    PeerData:=knockData.PeerData,
                                    ListenPort:=knockData.ListenPort,
                                    PacketHandler:=New Protocol.W3PacketHandler(knockData.Name, logger),
                                    RemoteEndPoint:=socket.RemoteEndPoint,
                                    taskTestCanHost:=taskTestCanHost,
                                    pinger:=pinger,
                                    socket:=socket,
                                    downloadManager:=downloadManager)

            'packets with no handler [within the Player class; received packets without a logger or handler kill the connection]
            player.AddPacketLogger(Protocol.PeerPackets.MapFileDataProblem)
            player.AddPacketLogger(Protocol.PeerPackets.MapFileDataReceived)
            player.AddPacketLogger(Protocol.ClientPackets.NonGameAction)
            player.AddPacketLogger(Protocol.ClientPackets.RequestDropLaggers)
            player.AddPacketLogger(Protocol.ClientPackets.GameAction)

            'packets with associated handler
            player.AddQueuedLocalPacketHandler(Protocol.ClientPackets.ClientConfirmHostLeaving, Sub() player.SendPacket(Protocol.MakeHostConfirmHostLeaving()))
            player.AddQueuedLocalPacketHandler(Protocol.ClientPackets.ClientMapInfo, AddressOf player.OnReceiveClientMapInfo)
            player.AddQueuedLocalPacketHandler(Protocol.ClientPackets.Leaving, AddressOf player.OnReceiveLeaving)
            player.AddQueuedLocalPacketHandler(Protocol.ClientPackets.PeerConnectionInfo, AddressOf player.OnReceivePeerConnectionInfo)
            player.AddQueuedLocalPacketHandler(Protocol.ClientPackets.Pong, AddressOf player.OnReceivePong)
            player.AddQueuedLocalPacketHandler(Protocol.ClientPackets.Ready, AddressOf player.OnReceiveReady)
            player.AddQueuedLocalPacketHandler(Protocol.ClientPackets.Tock, AddressOf player.OnReceiveTock)

            player.AddRemotePacketHandler(Protocol.ClientPackets.Pong, AddressOf player._pinger.QueueReceivedPong)
            AddHandler socket.Disconnected, AddressOf player.OnSocketDisconnected
            AddHandler pinger.SendPing, Sub(sender, salt) player.QueueSendPacket(Protocol.MakePing(salt))
            AddHandler pinger.Timeout, Sub(sender) player.QueueDisconnect(expected:=False,
                                                                          reportedReason:=Protocol.PlayerLeaveReason.Disconnect,
                                                                          reasonDescription:="Stopped responding to pings.")

            Return player
        End Function

        Public ReadOnly Property Id As PlayerId Implements Download.IPlayerDownloadAspect.Id
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Name As InvariantString Implements Download.IPlayerDownloadAspect.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property IsFake As Boolean
            Get
                Return _isFake
            End Get
        End Property
        Public ReadOnly Property IsReady As Boolean
            Get
                Return IsFake OrElse _ready
            End Get
        End Property
        Public ReadOnly Property CanHost As HostTestResult
            Get
                Select Case _taskTestCanHost.Status
                    Case TaskStatus.Faulted : Return HostTestResult.Fail
                    Case TaskStatus.RanToCompletion : Return HostTestResult.Pass
                    Case Else : Return HostTestResult.Test
                End Select
            End Get
        End Property
        Public ReadOnly Property PeerKey As UInt32
            Get
                Return _peerKey
            End Get
        End Property
        Public ReadOnly Property PeerData As IReadableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                Return _peerData
            End Get
        End Property
        Public ReadOnly Property ListenPort As UShort
            Get
                Return _listenPort
            End Get
        End Property
        Public ReadOnly Property RemoteEndPoint As Net.IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address IsNot Nothing)
                Return _remoteEndPoint
            End Get
        End Property

        Private Sub AddPacketLogger(ByVal packetDefinition As Protocol.Packets.Definition)
            Contract.Requires(packetDefinition IsNot Nothing)
            _packetHandler.AddLogger(packetDefinition.Id, packetDefinition.Jar)
        End Sub
        Private Function AddQueuedLocalPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                           ByVal handler As Action(Of T)) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            AddPacketLogger(packetDefinition)
            Return _packetHandler.AddHandler(packetDefinition.Id, Function(data) _inQueue.QueueAction(Sub() handler(packetDefinition.Jar.Parse(data).Value)))
        End Function

        Private Function AddRemotePacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                      ByVal handler As Func(Of T, Task)) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            _packetHandler.AddLogger(packetDefinition.Id, packetDefinition.Jar)
            Return _packetHandler.AddHandler(packetDefinition.Id, Function(data) handler(packetDefinition.Jar.Parse(data).Value))
        End Function
        Public Function QueueAddPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                    ByVal handler As Func(Of T, Task)) As Task(Of IDisposable) _
                                                    Implements Download.IPlayerDownloadAspect.QueueAddPacketHandler
            Return _inQueue.QueueFunc(Function() AddRemotePacketHandler(packetDefinition, handler))
        End Function

        Public Sub QueueStart()
            _inQueue.QueueAction(Sub() BeginReading())
        End Sub

        Private Sub BeginReading()
            AsyncProduceConsumeUntilError(
                producer:=AddressOf _socket.AsyncReadPacket,
                consumer:=Function(packetData) _packetHandler.HandlePacket(packetData),
                errorHandler:=Sub(exception) QueueDisconnect(expected:=False,
                                                             reportedReason:=Protocol.PlayerLeaveReason.Disconnect,
                                                             reasonDescription:="Error receiving packet: {0}.".Frmt(exception.Summarize)))
        End Sub

        '''<summary>Disconnects this player and removes them from the system.</summary>
        Private Sub Disconnect(ByVal expected As Boolean,
                               ByVal reportedReason As Protocol.PlayerLeaveReason,
                               ByVal reasonDescription As String)
            Contract.Requires(reasonDescription IsNot Nothing)
            If Not Me.IsFake Then
                _socket.Disconnect(expected, reasonDescription)
            End If
            If _pinger IsNot Nothing Then _pinger.Dispose()
            RaiseEvent Disconnected(Me, expected, reportedReason, reasonDescription)
            Me.Dispose()
        End Sub
        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String) As Task Implements Download.IPlayerDownloadAspect.QueueDisconnect
            Return _inQueue.QueueAction(Sub() Disconnect(expected, reportedReason, reasonDescription))
        End Function

        Private Sub SendPacket(ByVal pk As Protocol.Packet)
            Contract.Requires(pk IsNot Nothing)
            If Me.IsFake Then Return
            _socket.SendPacket(pk)
        End Sub
        Public Function QueueSendPacket(ByVal packet As Protocol.Packet) As Task Implements Download.IPlayerDownloadAspect.QueueSendPacket
            Dim result = _inQueue.QueueAction(Sub() SendPacket(packet))
            result.IgnoreExceptions()
            Return result
        End Function

        Private Sub OnSocketDisconnected(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reasonDescription As String)
            _inQueue.QueueAction(Sub() Disconnect(expected, Protocol.PlayerLeaveReason.Disconnect, reasonDescription))
        End Sub

        Public Function QueueGetLatency() As Task(Of Double)
            Contract.Ensures(Contract.Result(Of Task(Of Double))() IsNot Nothing)
            If _pinger Is Nothing Then
                Return 0.0.AsTask
            Else
                Return _pinger.QueueGetLatency
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

        Private Sub OnReceivePong(ByVal salt As UInt32)
            _outQueue.QueueAction(Sub() RaiseEvent SuperficialStateUpdated(Me))
        End Sub

#Region "Lobby"
        <Pure()>
        <ContractVerification(False)>
        Public Function MakePacketOtherPlayerJoined() As Protocol.Packet Implements Download.IPlayerDownloadAspect.MakePacketOtherPlayerJoined
            Contract.Ensures(Contract.Result(Of Protocol.Packet)() IsNot Nothing)
            Return Protocol.MakeOtherPlayerJoined(Name, Id, PeerKey, PeerData, New Net.IPEndPoint(RemoteEndPoint.Address, ListenPort))
        End Function

        Private Sub OnReceivePeerConnectionInfo(ByVal flags As UInt16)
            _numPeerConnections = (From i In 12.Range Where flags.HasBitSet(i)).Count
            Contract.Assume(_numPeerConnections <= 12)
            RaiseEvent SuperficialStateUpdated(Me)
        End Sub
        Private Sub OnReceiveClientMapInfo(ByVal vals As NamedValueMap)
            Contract.Requires(vals IsNot Nothing)
            _reportedDownloadPosition = vals.ItemAs(Of UInt32)("total downloaded")
            _outQueue.QueueAction(Sub() RaiseEvent StateUpdated(Me))
        End Sub
#End Region

#Region "Load Screen"
        Private Sub OnReceiveReady(ByVal value As EmptyJar.EmptyValue)
            _ready = True
            _logger.Log("{0} is ready".Frmt(Name), LogMessageType.Positive)
        End Sub

        Private Sub StartLoading()
            _state = PlayerState.Loading
            SendPacket(Protocol.MakeStartLoading())
        End Sub
        Public Function QueueStartLoading() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _inQueue.QueueAction(AddressOf StartLoading)
        End Function
#End Region

#Region "GamePlay"
        Public Sub GamePlayStart()
            _state = PlayerState.Playing
        End Sub

        Private Sub SendTick(ByVal record As TickRecord,
                             ByVal actionStreaks As IEnumerable(Of IReadableList(Of Protocol.PlayerActionSet)))
            Contract.Requires(actionStreaks IsNot Nothing)
            Contract.Requires(record IsNot Nothing)
            If IsFake Then Return
            _tickQueue.Enqueue(record)
            _maxTockTime += record.length
            For Each preOverflowActionStreak In actionStreaks.SkipLast(1)
                Contract.Assume(preOverflowActionStreak IsNot Nothing)
                SendPacket(Protocol.MakeTickPreOverflow(preOverflowActionStreak))
            Next preOverflowActionStreak
            If actionStreaks.Any Then
                SendPacket(Protocol.MakeTick(record.length, actionStreaks.Last.AssumeNotNull.Maybe))
            Else
                SendPacket(Protocol.MakeTick(record.length))
            End If
        End Sub
        Public Function QueueSendTick(ByVal record As TickRecord,
                                      ByVal actionStreaks As IEnumerable(Of IReadableList(Of Protocol.PlayerActionSet))) As Task
            Contract.Requires(record IsNot Nothing)
            Contract.Requires(actionStreaks IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _inQueue.QueueAction(Sub() SendTick(record, actionStreaks))
        End Function

        Private Sub OnReceiveTock(ByVal vals As NamedValueMap)
            Contract.Requires(vals IsNot Nothing)
            If _tickQueue.Count <= 0 Then
                _logger.Log("Banned behavior: {0} responded to a tick which wasn't sent.".Frmt(Name), LogMessageType.Problem)
                Disconnect(True, Protocol.PlayerLeaveReason.Disconnect, "overticked")
                Return
            End If

            Dim record = _tickQueue.Dequeue()
            Contract.Assume(record IsNot Nothing)
            _totalTockTime += record.length
            Contract.Assume(_totalTockTime >= 0)
        End Sub

        Public ReadOnly Property GetTockTime() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Return _totalTockTime
            End Get
        End Property
        Public Function QueueStartPlaying() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _inQueue.QueueAction(AddressOf GamePlayStart)
        End Function
#End Region

#Region "Part"
        Private Sub OnReceiveLeaving(ByVal leaveType As Protocol.PlayerLeaveReason)
            Disconnect(True, leaveType, "Controlled exit with reported result: {0}".Frmt(leaveType))
        End Sub

        Public ReadOnly Property AdvertisedDownloadPercent() As Byte
            Get
                If _state <> PlayerState.Lobby Then Return 100
                If IsFake OrElse _downloadManager Is Nothing Then Return 254 'Not a real player, show "|CF"
                If _reportedDownloadPosition Is Nothing Then Return 255
                Return CByte((_reportedDownloadPosition.Value * 100UL) \ _downloadManager.FileSize)
            End Get
        End Property

        Public Function Description() As Task(Of String)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Return QueueGetLatencyDescription.Select(
                Function(latencyDesc)
                    Dim contextInfo As Task(Of String)
                    Select Case _state
                        Case PlayerState.Lobby
                            Dim p = AdvertisedDownloadPercent
                            Dim dlText As String
                            Select Case p
                                Case 255 : dlText = "?"
                                Case 254 : dlText = "fake"
                                Case Else : dlText = "{0}%".Frmt(p)
                            End Select
                            contextInfo = From rateDescription In _downloadManager.QueueGetClientBandwidthDescription(Me)
                                          Select "DL={0}".Frmt(dlText).Padded(9) + _
                                                 "EB={0}".Frmt(rateDescription)
                        Case PlayerState.Loading
                            contextInfo = "Ready={0}".Frmt(IsReady).AsTask
                        Case PlayerState.Playing
                            contextInfo = "DT={0}gms".Frmt(Me._maxTockTime - Me._totalTockTime).AsTask
                        Case Else
                            Throw _state.MakeImpossibleValueException
                    End Select
                    Contract.Assert(contextInfo IsNot Nothing)

                    Return From text In contextInfo
                           Select Name.Value.Padded(20) +
                                  Me.Id.ToString.Padded(6) +
                                  "Host={0}".Frmt(CanHost()).Padded(12) +
                                  "{0}c".Frmt(_numPeerConnections).Padded(5) +
                                  latencyDesc.Padded(12) +
                                  text
                End Function).Unwrap.AssumeNotNull
        End Function
#End Region
    End Class
End Namespace
