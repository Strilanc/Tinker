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
        Private ReadOnly _index As PID
        Private ReadOnly testCanHost As IFuture
        Private ReadOnly socket As W3Socket
        Private ReadOnly packetHandler As Protocol.W3PacketHandler
        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue
        Private _numPeerConnections As Integer
        Private _downloadManager As DownloadManager
        Private ReadOnly settings As GameSettings
        Private ReadOnly pinger As Pinger

        Private ReadOnly _name As InvariantString
        Private ReadOnly _listenPort As UShort
        Public ReadOnly peerKey As UInteger
        Public ReadOnly isFake As Boolean
        Private ReadOnly logger As Logger

        Public hasVotedToStart As Boolean
        Public numAdminTries As Integer
        Public Event Disconnected(ByVal sender As Player, ByVal expected As Boolean, ByVal leaveType As PlayerLeaveType, ByVal reason As String)

        Public ReadOnly Property Name As InvariantString Implements IPlayerDownloadAspect.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property PID As PID Implements IPlayerDownloadAspect.PID
            Get
                Return _index
            End Get
        End Property
        Public ReadOnly Property ListenPort As UShort
            Get
                Return _listenPort
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
            Contract.Invariant(totalTockTime >= 0)
        End Sub

        '''<summary>Creates a fake player.</summary>
        Public Sub New(ByVal index As PID,
                       ByVal settings As GameSettings,
                       ByVal name As InvariantString,
                       Optional ByVal logger As Logger = Nothing)

            Me.settings = settings
            Me.logger = If(logger, New Logger)
            Me.packetHandler = New Protocol.W3PacketHandler(name, Me.logger)
            Me._index = index
            If name.Length > Protocol.Packets.MaxPlayerNameLength Then Throw New ArgumentException("Player name must be less than 16 characters long.")
            Me._name = name
            isFake = True
            LobbyStart()
            Dim hostFail = New FutureAction
            hostFail.SetFailed(New ArgumentException("Fake players can't host."))
            Me.testCanHost = hostFail
            Me.testCanHost.SetHandled()
        End Sub

        '''<summary>Creates a real player.</summary>
        Public Sub New(ByVal index As PID,
                       ByVal settings As GameSettings,
                       ByVal connectingPlayer As W3ConnectingPlayer,
                       ByVal clock As IClock,
                       ByVal downloadManager As DownloadManager,
                       Optional ByVal logger As Logger = Nothing)
            'Contract.Requires(game IsNot Nothing)
            'Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Assume(connectingPlayer IsNot Nothing)

            Me.settings = settings
            Me.logger = If(logger, New Logger)
            Me.packetHandler = New Protocol.W3PacketHandler(connectingPlayer.Name, Me.logger)
            connectingPlayer.Socket.Logger = Me.logger
            Me.peerKey = connectingPlayer.PeerKey

            Me._downloadManager = downloadManager
            Me.socket = connectingPlayer.Socket
            Me._name = connectingPlayer.Name
            Me._listenPort = connectingPlayer.ListenPort
            Me._index = index
            AddHandler socket.Disconnected, AddressOf OnSocketDisconnected

            AddQueuedPacketHandler(Protocol.PacketId.Pong,
                                   Protocol.Packets.Pong,
                                   handler:=Function(pickle)
                                                outQueue.QueueAction(Sub() RaiseEvent SuperficialStateUpdated(Me))
                                                Return pinger.QueueReceivedPong(pickle.Value)
                                            End Function)
            AddQueuedPacketHandler(Protocol.PacketId.NonGameAction,
                                   Protocol.Packets.NonGameAction,
                                   handler:=AddressOf ReceiveNonGameAction)
            AddQueuedPacketHandler(Protocol.PacketId.Leaving,
                                   Protocol.Packets.Leaving,
                                   handler:=AddressOf ReceiveLeaving)
            AddQueuedPacketHandler(Protocol.Packets.MapFileDataReceived, AddressOf IgnorePacket)
            AddQueuedPacketHandler(Protocol.Packets.MapFileDataProblem, AddressOf IgnorePacket)

            LobbyStart()

            'Test hosting
            Me.testCanHost = AsyncTcpConnect(socket.RemoteEndPoint.Address, ListenPort)
            Me.testCanHost.SetHandled()

            'Pings
            pinger = New Pinger(period:=5.Seconds, timeoutCount:=10, clock:=clock)
            AddHandler pinger.SendPing, Sub(sender, salt) QueueSendPacket(Protocol.MakePing(salt))
            AddHandler pinger.Timeout, Sub(sender) QueueDisconnect(expected:=False,
                                                                   leaveType:=PlayerLeaveType.Disconnect,
                                                                   reason:="Stopped responding to pings.")
        End Sub
        Public Sub QueueStart()
            inQueue.QueueAction(Sub() BeginReading())
        End Sub

        Private Sub BeginReading()
            AsyncProduceConsumeUntilError(
                producer:=AddressOf socket.AsyncReadPacket,
                consumer:=Function(packetData) packetHandler.HandlePacket(packetData),
                errorHandler:=Sub(exception)
                                  exception.RaiseAsUnexpected("Receiving packet")
                                  QueueDisconnect(expected:=False,
                                                  leaveType:=PlayerLeaveType.Disconnect,
                                                  reason:="Error receiving packet: {0}.".Frmt(exception.Message))
                              End Sub
            )
        End Sub

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
            Me.Dispose()
        End Sub
        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal leaveType As PlayerLeaveType, ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() Disconnect(expected, leaveType, reason))
        End Function

        Private Sub SendPacket(ByVal pk As Protocol.Packet)
            Contract.Requires(pk IsNot Nothing)
            If Me.isFake Then Return
            socket.SendPacket(pk)
        End Sub
        Public Function QueueSendPacket(ByVal packet As Protocol.Packet) As IFuture Implements IPlayerDownloadAspect.QueueSendPacket
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim result = inQueue.QueueAction(Sub() SendPacket(packet))
            result.SetHandled()
            Return result
        End Function

        Private Sub OnSocketDisconnected(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reason As String)
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
        Public Function QueueGetLatency() As IFuture(Of Double)
            Contract.Ensures(Contract.Result(Of IFuture(Of Double))() IsNot Nothing)
            If pinger Is Nothing Then
                Return 0.0.Futurized
            Else
                Return pinger.QueueGetLatency
            End If
        End Function
        Public Function QueueGetLatencyDescription() As IFuture(Of String)
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
            Return (From latency In QueueGetLatency()
                    Select latencyDesc = If(latency = 0, "?", "{0:0}ms".Frmt(latency))
                    Select _downloadManager.QueueGetClientLatencyDescription(Me, latencyDesc)
                   ).Defuturized
        End Function
        Public ReadOnly Property NumPeerConnections() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return _numPeerConnections
            End Get
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            Return QueueDisconnect(expected:=True, leaveType:=PlayerLeaveType.Disconnect, reason:="Disposed")
        End Function
    End Class
End Namespace
