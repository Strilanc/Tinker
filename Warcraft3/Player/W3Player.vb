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
        Private ReadOnly _peerData As IRist(Of Byte)
        Private ReadOnly _listenEndPoint As Net.IPEndPoint

        Private ReadOnly _isFake As Boolean
        Private ReadOnly _logger As Logger
        Private ReadOnly _inQueue As CallQueue
        Private ReadOnly _outQueue As CallQueue

        Private ReadOnly _pinger As Pinger
        Private ReadOnly _socket As W3Socket
        Private ReadOnly _tickQueue As New Queue(Of TickRecord)
        Private ReadOnly _packetHandlerLogger As PacketHandlerLogger(Of Protocol.PacketId)
        Private ReadOnly _taskTestCanHost As Task
        Private ReadOnly _startedReading As New OnetimeLock

        Private _ready As Boolean
        Private _state As PlayerState
        Private _maxTockTime As Integer
        Private _totalTockTime As Integer
        Private _downloadManager As Download.Manager
        Private _numPeerConnections As Integer
        Private _reportedDownloadPosition As UInt32?

        Public Property HasVotedToStart As Boolean
        Public Property AdminAttemptCount As Integer

        Public Event SuperficialStateUpdated(sender As Player)
        Public Event StateUpdated(sender As Player)
        Public Event Disconnected(sender As Player, expected As Boolean, reportedReason As Protocol.PlayerLeaveReason, reasonDescription As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_inQueue IsNot Nothing)
            Contract.Invariant(_outQueue IsNot Nothing)
            Contract.Invariant(_peerData IsNot Nothing)
            Contract.Invariant(_tickQueue IsNot Nothing)
            Contract.Invariant(_packetHandlerLogger IsNot Nothing)
            Contract.Invariant(_listenEndPoint IsNot Nothing)
            Contract.Invariant(_listenEndPoint.Address IsNot Nothing)
            Contract.Invariant(_taskTestCanHost IsNot Nothing)
            Contract.Invariant(_startedReading IsNot Nothing)

            'Contract.Invariant(_socket Is Nothing = IsFake)
            Contract.Invariant(_totalTockTime >= 0)
            Contract.Invariant(AdminAttemptCount >= 0)
            Contract.Invariant(_numPeerConnections >= 0)
            Contract.Invariant(_numPeerConnections <= 12)
        End Sub

        Public Sub New(id As PlayerId,
                       name As InvariantString,
                       isFake As Boolean,
                       logger As Logger,
                       peerKey As UInt32,
                       peerData As IRist(Of Byte),
                       packetHandlerLogger As PacketHandlerLogger(Of Protocol.PacketId),
                       listenEndPoint As Net.IPEndPoint,
                       taskTestCanHost As Task,
                       Optional pinger As Pinger = Nothing,
                       Optional socket As W3Socket = Nothing,
                       Optional initialState As PlayerState = PlayerState.Lobby,
                       Optional downloadManager As Download.Manager = Nothing,
                       Optional inQueue As CallQueue = Nothing,
                       Optional outQueue As CallQueue = Nothing)
            Contract.Requires(packetHandlerLogger IsNot Nothing)
            Contract.Requires(peerData IsNot Nothing)
            Contract.Requires(listenEndPoint IsNot Nothing)
            Contract.Requires(listenEndPoint.Address IsNot Nothing)
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
            Me._packetHandlerLogger = packetHandlerLogger
            Me._listenEndPoint = listenEndPoint
            Me._taskTestCanHost = taskTestCanHost
            Me._downloadManager = downloadManager
            Me._inQueue = If(inQueue, MakeTaskedCallQueue)
            Me._outQueue = If(outQueue, MakeTaskedCallQueue)
        End Sub

        Public Shared Function MakeFake(id As PlayerId,
                                        name As InvariantString,
                                        logger As Logger) As Player
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            Dim hostFail = New TaskCompletionSource(Of NoValue)
            hostFail.Task.ConsiderExceptionsHandled()
            hostFail.SetException(New ArgumentException("Fake players can't host."))
            Contract.Assume(hostFail.Task IsNot Nothing)

            Dim player = New Player(id:=id,
                                    name:=name,
                                    IsFake:=True,
                                    logger:=logger,
                                    PeerKey:=0,
                                    PeerData:=MakeRist(Of Byte)(0),
                                    PacketHandlerLogger:=Protocol.MakeW3PacketHandlerLogger(name, logger),
                                    ListenEndPoint:=New Net.IPAddress({0, 0, 0, 0}).WithPort(0),
                                    taskTestCanHost:=hostFail.Task)
            player._ready = True
            Return player
        End Function

        Public Shared Function MakeRemote(id As PlayerId,
                                          knockData As Protocol.KnockData,
                                          socket As W3Socket,
                                          clock As IClock,
                                          downloadManager As Download.Manager,
                                          logger As Logger) As Player
            Contract.Requires(knockData IsNot Nothing)
            Contract.Requires(socket IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Contract.Requires(downloadManager IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            socket.Logger = logger
            Dim listenEndPoint = socket.RemoteEndPoint.Address.WithPort(knockData.ListenPort)
            Dim taskTestCanHost = AsyncTcpConnect(listenEndPoint.Address.AssumeNotNull(), CUShort(listenEndPoint.Port))
            Call Async Sub()
                     Try
                         Dim testSocket = Await taskTestCanHost
                         testSocket.Close()
                     Catch ex As Exception
                         'ignore
                     End Try
                 End Sub
            Dim pinger = New Pinger(period:=5.Seconds, timeoutCount:=10, clock:=clock)

            Dim player = New Player(id:=id,
                                    Name:=knockData.Name,
                                    IsFake:=False,
                                    logger:=logger,
                                    PeerKey:=knockData.PeerKey,
                                    PeerData:=knockData.PeerData,
                                    PacketHandlerLogger:=Protocol.MakeW3PacketHandlerLogger(knockData.Name, logger),
                                    listenEndPoint:=listenEndPoint,
                                    taskTestCanHost:=taskTestCanHost,
                                    pinger:=pinger,
                                    socket:=socket,
                                    downloadManager:=downloadManager)

            player.AddRemotePacketHandler(Protocol.ClientPackets.Pong, AddressOf player._pinger.AssumeNotNull().QueueReceivedPong)
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
                Return _ready
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
        Public ReadOnly Property PeerData As IRist(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Return _peerData
            End Get
        End Property
        Public ReadOnly Property ListenEndPoint As Net.IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address IsNot Nothing)
                Return _listenEndPoint
            End Get
        End Property
        Public ReadOnly Property AdvertisedDownloadPercent() As Byte
            Get
                If _state <> PlayerState.Lobby Then Return 100
                If IsFake OrElse _downloadManager Is Nothing Then Return 254 'Not a real player, show "|CF"
                If _reportedDownloadPosition Is Nothing Then Return 255
                Return CByte((_reportedDownloadPosition.Value * 100UL) \ _downloadManager.FileSize)
            End Get
        End Property
        Public ReadOnly Property TockTime() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Return _totalTockTime
            End Get
        End Property
        Public ReadOnly Property PeerConnectionCount() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return _numPeerConnections
            End Get
        End Property

        Public Function QueueGetLatency() As Task(Of Double)
            Contract.Ensures(Contract.Result(Of Task(Of Double))() IsNot Nothing)
            If _pinger Is Nothing Then
                Return 0.0.AsTask
            Else
                Return _pinger.QueueGetLatency
            End If
        End Function
        Public Function QueueGetLatencyDescription() As Task(Of String)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Return (From latency In QueueGetLatency()
                    Select latencyDesc = If(latency = 0, "?", "{0:0}ms".Frmt(latency))
                    Select If(_downloadManager Is Nothing,
                              latencyDesc.AsTask,
                              _downloadManager.QueueGetClientLatencyDescription(Me, latencyDesc))
                   ).Unwrap.AssumeNotNull
        End Function
        Public Async Function AsyncDescription() As Task(Of String)
            'Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            'Information differing based on the current player state
            Dim contextInfo As String
            Select Case _state
                Case PlayerState.Lobby
                    Dim p = AdvertisedDownloadPercent
                    Dim dlText As String
                    Select Case p
                        Case 255 : dlText = "?"
                        Case 254 : dlText = "fake"
                        Case Else : dlText = "{0}%".Frmt(p)
                    End Select
                    Dim rateDescription = Await _downloadManager.QueueGetClientBandwidthDescription(Me)
                    contextInfo = "DL={0}".Frmt(dlText).Padded(9) + "EB={0}".Frmt(rateDescription)
                Case PlayerState.Loading
                    contextInfo = "Ready={0}".Frmt(IsReady)
                Case PlayerState.Playing
                    contextInfo = "DT={0}gms".Frmt(Me._maxTockTime - Me._totalTockTime)
                Case Else
                    Throw _state.MakeImpossibleValueException
            End Select
            Contract.Assert(contextInfo IsNot Nothing)

            Dim latencyDesc = Await QueueGetLatencyDescription()
            Return Name.Value.Padded(20) +
                   Me.Id.ToString.Padded(6) +
                   "Host={0}".Frmt(CanHost()).Padded(12) +
                   "{0}c".Frmt(_numPeerConnections).Padded(5) +
                   latencyDesc.Padded(12) +
                   contextInfo
        End Function

        Private Function TryAddPacketLogger(packetDefinition As Protocol.Packets.Definition) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Return _packetHandlerLogger.TryIncludeLogger(packetDefinition.Id, packetDefinition.Jar)
        End Function
        Private Function AddQueuedLocalPacketHandler(Of T)(packetDefinition As Protocol.Packets.Definition(Of T),
                                                           handler As Action(Of T)) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _packetHandlerLogger.IncludeHandler(
                packetDefinition.Id,
                packetDefinition.Jar,
                Function(pickle) _inQueue.QueueAction(Sub() handler(pickle.Value)))
        End Function
        Private Function AddRemotePacketHandler(Of T)(packetDefinition As Protocol.Packets.Definition(Of T),
                                                      handler As Func(Of T, Task)) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _packetHandlerLogger.IncludeHandler(Of T)(
                packetDefinition.Id,
                packetDefinition.Jar,
                Function(pickle) handler(pickle.Value))
        End Function
        Public Function QueueAddPacketHandler(Of T)(packetDefinition As Protocol.Packets.Definition(Of T),
                                                    handler As Func(Of T, Task)) As Task(Of IDisposable) _
                                                    Implements Download.IPlayerDownloadAspect.QueueAddPacketHandler
            Return _inQueue.QueueFunc(Function() AddRemotePacketHandler(packetDefinition, handler))
        End Function

        Private Sub OnReceivePong(salt As UInt32)
            _outQueue.QueueAction(Sub() RaiseEvent SuperficialStateUpdated(Me))
        End Sub
        Private Sub OnReceivePeerConnectionInfo(flags As UInt16)
            _numPeerConnections = (From i In 12.Range Where flags.HasBitSet(i)).Count
            Contract.Assume(_numPeerConnections <= 12)
            RaiseEvent SuperficialStateUpdated(Me)
        End Sub
        Private Sub OnReceiveClientMapInfo(vals As NamedValueMap)
            Contract.Requires(vals IsNot Nothing)
            _reportedDownloadPosition = vals.ItemAs(Of UInt32)("total downloaded")
            _outQueue.QueueAction(Sub() RaiseEvent StateUpdated(Me))
        End Sub
        Private Sub OnReceiveReady(value As NoValue)
            _ready = True
            _logger.Log("{0} is ready".Frmt(Name), LogMessageType.Positive)
        End Sub
        Private Sub OnReceiveLeaving(leaveType As Protocol.PlayerLeaveReason)
            Disconnect(True, leaveType, "Controlled exit with reported result: {0}".Frmt(leaveType))
        End Sub
        Private Sub OnReceiveTock(vals As NamedValueMap)
            Contract.Requires(vals IsNot Nothing)
            If _tickQueue.Count <= 0 Then
                _logger.Log("Banned behavior: {0} responded to a tick which wasn't sent.".Frmt(Name), LogMessageType.Problem)
                Disconnect(True, Protocol.PlayerLeaveReason.Disconnect, "overticked")
                Return
            End If

            Dim record = _tickQueue.Dequeue()
            Contract.Assume(record IsNot Nothing)
            _totalTockTime += record.length
        End Sub

        Private Sub OnSocketDisconnected(sender As W3Socket, expected As Boolean, reasonDescription As String)
            _inQueue.QueueAction(Sub() Disconnect(expected, Protocol.PlayerLeaveReason.Disconnect, reasonDescription))
        End Sub

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return QueueDisconnect(expected:=True, reportedReason:=Protocol.PlayerLeaveReason.Disconnect, reasonDescription:="Disposed")
        End Function

        <Pure()>
        Public Function MakePacketOtherPlayerJoined() As Protocol.Packet Implements Download.IPlayerDownloadAspect.MakePacketOtherPlayerJoined
            Contract.Ensures(Contract.Result(Of Protocol.Packet)() IsNot Nothing)
            Return Protocol.MakeOtherPlayerJoined(Name, Id, PeerKey, PeerData, listenEndPoint)
        End Function

        Private Async Sub BeginReading()
            If _socket Is Nothing Then Return
            If Not _startedReading.TryAcquire Then Return

            'packets with no handler [within the Player class; received packets without a logger or handler kill the connection]
            TryAddPacketLogger(Protocol.PeerPackets.MapFileDataProblem)
            TryAddPacketLogger(Protocol.PeerPackets.MapFileDataReceived)
            TryAddPacketLogger(Protocol.ClientPackets.NonGameAction)
            TryAddPacketLogger(Protocol.ClientPackets.RequestDropLaggers)
            TryAddPacketLogger(Protocol.ClientPackets.GameAction)

            'packets with associated handler
            AddQueuedLocalPacketHandler(Protocol.ClientPackets.ClientConfirmHostLeaving, Sub() SendPacket(Protocol.MakeHostConfirmHostLeaving()))
            AddQueuedLocalPacketHandler(Protocol.ClientPackets.ClientMapInfo, AddressOf OnReceiveClientMapInfo)
            AddQueuedLocalPacketHandler(Protocol.ClientPackets.Leaving, AddressOf OnReceiveLeaving)
            AddQueuedLocalPacketHandler(Protocol.ClientPackets.PeerConnectionInfo, AddressOf OnReceivePeerConnectionInfo)
            AddQueuedLocalPacketHandler(Protocol.ClientPackets.Pong, AddressOf OnReceivePong)
            AddQueuedLocalPacketHandler(Protocol.ClientPackets.Ready, AddressOf OnReceiveReady)
            AddQueuedLocalPacketHandler(Protocol.ClientPackets.Tock, AddressOf OnReceiveTock)

            Try
                Do
                    Dim data = Await _socket.AsyncReadPacket()
                    Await _packetHandlerLogger.HandlePacket(data)
                Loop
            Catch ex As Exception
                QueueDisconnect(expected:=False,
                                reportedReason:=Protocol.PlayerLeaveReason.Disconnect,
                                reasonDescription:="Error receiving packet: {0}.".Frmt(ex.Summarize))
            End Try
        End Sub
        Public Sub QueueStart()
            _inQueue.QueueAction(Sub() BeginReading())
        End Sub

        Private Sub Disconnect(expected As Boolean,
                               reportedReason As Protocol.PlayerLeaveReason,
                               reasonDescription As String)
            Contract.Requires(reasonDescription IsNot Nothing)
            If _socket IsNot Nothing Then
                _socket.Disconnect(expected, reasonDescription)
            End If
            If _pinger IsNot Nothing Then _pinger.Dispose()
            RaiseEvent Disconnected(Me, expected, reportedReason, reasonDescription)
            Me.Dispose()
        End Sub
        Public Function QueueDisconnect(expected As Boolean, reportedReason As Protocol.PlayerLeaveReason, reasonDescription As String) As Task Implements Download.IPlayerDownloadAspect.QueueDisconnect
            Return _inQueue.QueueAction(Sub() Disconnect(expected, reportedReason, reasonDescription))
        End Function

        Private Sub SendPacket(pk As Protocol.Packet)
            Contract.Requires(pk IsNot Nothing)
            If _socket Is Nothing Then Return
            _socket.SendPacket(pk)
        End Sub
        Public Function QueueSendPacket(packet As Protocol.Packet) As Task Implements Download.IPlayerDownloadAspect.QueueSendPacket
            Dim result = _inQueue.QueueAction(Sub() SendPacket(packet))
            result.ConsiderExceptionsHandled()
            Return result
        End Function

        Private Sub StartLoading()
            _state = PlayerState.Loading
            SendPacket(Protocol.MakeStartLoading())
        End Sub
        Public Function QueueStartLoading() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _inQueue.QueueAction(AddressOf StartLoading)
        End Function

        Private Sub GamePlayStart()
            _state = PlayerState.Playing
        End Sub
        Public Function QueueStartPlaying() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _inQueue.QueueAction(AddressOf GamePlayStart)
        End Function

        Private Sub SendTick(record As TickRecord,
                             actionStreaks As IEnumerable(Of IRist(Of Protocol.PlayerActionSet)))
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
        Public Function QueueSendTick(record As TickRecord,
                                      actionStreaks As IEnumerable(Of IRist(Of Protocol.PlayerActionSet))) As Task
            Contract.Requires(record IsNot Nothing)
            Contract.Requires(actionStreaks IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _inQueue.QueueAction(Sub() SendTick(record, actionStreaks))
        End Function
    End Class
End Namespace
