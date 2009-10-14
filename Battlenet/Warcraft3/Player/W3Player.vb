Namespace Warcraft3
    Public Enum HostTestResult As Integer
        Fail = -1
        Test = 0
        Pass = 1
    End Enum
    Public Enum W3PlayerLeaveType As Byte
        Disconnect = 1
        Lose = 7
        MeleeLose = 8
        Win = 9
        Draw = 10
        Observer = 11
        Lobby = 13
    End Enum
    Public Enum W3PlayerState
        Lobby
        Loading
        Playing
    End Enum

    Partial Public NotInheritable Class W3Player
        Private state As W3PlayerState = W3PlayerState.Lobby
        Private ReadOnly _index As Byte
        Private ReadOnly testCanHost As IFuture
        Private Const MAX_NAME_LENGTH As Integer = 15
        Private ReadOnly socket As W3Socket
        Private ReadOnly ref As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly eref As ICallQueue = New ThreadPooledCallQueue
        Private numPeerConnections As Integer
        Private ReadOnly settings As ServerSettings
        Private ReadOnly scheduler As TransferScheduler(Of Byte)
        Private ReadOnly pinger As Pinger

        Public ReadOnly name As String
        Public ReadOnly listenPort As UShort
        Public ReadOnly peerKey As UInteger
        Public ReadOnly isFake As Boolean
        Public ReadOnly logger As Logger

        Public hasVotedToStart As Boolean
        Public numAdminTries As Integer
        Public Event Disconnected(ByVal sender As W3Player, ByVal expected As Boolean, ByVal leaveType As W3PlayerLeaveType, ByVal reason As String)

        Public ReadOnly Property Index As Byte
            Get
                Contract.Ensures(Contract.Result(Of Byte)() > 0)
                Contract.Ensures(Contract.Result(Of Byte)() <= 12)
                Return _index
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(numPeerConnections >= 0)
            Contract.Invariant(numPeerConnections <= 12)
            Contract.Invariant(tickQueue IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eref IsNot Nothing)
            Contract.Invariant(socket Is Nothing = isFake)
            Contract.Invariant(testCanHost IsNot Nothing)
            Contract.Invariant(numAdminTries >= 0)
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(settings IsNot Nothing)
            Contract.Invariant(scheduler IsNot Nothing)
            Contract.Invariant(packetHandlers IsNot Nothing)
            Contract.Invariant(_index > 0)
            Contract.Invariant(_index <= 12)
            Contract.Invariant(totalTockTime >= 0)
            Contract.Invariant(mapDownloadPosition >= 0)
        End Sub

        '''<summary>Creates a fake player.</summary>
        Public Sub New(ByVal index As Byte,
                       ByVal settings As ServerSettings,
                       ByVal scheduler As TransferScheduler(Of Byte),
                       ByVal name As String,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Requires(index > 0)
            Contract.Requires(index <= 12)
            Contract.Requires(name IsNot Nothing)

            Me.settings = settings
            Me.scheduler = scheduler
            Me.logger = If(logger, New Logger)
            Me._index = index
            If name.Length > MAX_NAME_LENGTH Then
                name = name.Substring(0, MAX_NAME_LENGTH)
            End If
            Me.name = name
            isFake = True
            LobbyStart()
            Dim hostFail = New FutureAction
            hostFail.SetFailed(New ArgumentException("Fake players can't host."))
            Me.testCanHost = hostFail
            Me.testCanHost.MarkAnyExceptionAsHandled()
        End Sub



        '''<summary>Creates a real player.</summary>
        Public Sub New(ByVal index As Byte,
                       ByVal settings As ServerSettings,
                       ByVal scheduler As TransferScheduler(Of Byte),
                       ByVal connectingPlayer As W3ConnectingPlayer,
                       Optional ByVal logging As Logger = Nothing)
            'Contract.Requires(index > 0)
            'Contract.Requires(index <= 12)
            'Contract.Requires(game IsNot Nothing)
            'Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Assume(index > 0)
            Contract.Assume(index <= 12)
            Contract.Assume(connectingPlayer IsNot Nothing)

            Me.settings = settings
            Me.scheduler = scheduler
            Me.logger = If(logging, New Logger)
            connectingPlayer.Socket.Logger = Me.logger
            Me.peerKey = connectingPlayer.PeerKey

            Me.socket = connectingPlayer.Socket
            Me.name = connectingPlayer.Name
            Me.listenPort = connectingPlayer.ListenPort
            Me._index = index
            AddHandler socket.Disconnected, AddressOf CatchSocketDisconnected

            packetHandlers(W3PacketId.Pong) = Sub(val As W3Packet) ReceivePong(CType(val.Payload.Value, Dictionary(Of String, Object)))
            packetHandlers(W3PacketId.Leaving) = AddressOf ReceiveLeaving
            packetHandlers(W3PacketId.NonGameAction) = AddressOf ReceiveNonGameAction
            packetHandlers(W3PacketId.MapFileDataReceived) = AddressOf IgnorePacket
            packetHandlers(W3PacketId.MapFileDataProblem) = AddressOf IgnorePacket

            LobbyStart()
            BeginReading()

            'Test hosting
            Me.testCanHost = FutureCreateConnectedTcpClient(socket.RemoteEndPoint.Address, listenPort)
            Me.testCanHost.MarkAnyExceptionAsHandled()

            'Pings
            pinger = New Pinger(5.Seconds, 10)
            AddHandler pinger.SendPing, Sub(sender, salt) QueueSendPacket(W3Packet.MakePing(salt))
            AddHandler pinger.Timeout, Sub(sender) QueueDisconnect(expected:=False,
                                                                   leaveType:=W3PlayerLeaveType.Disconnect,
                                                                   reason:="Stopped responding to pings.")
        End Sub

        Private Sub BeginReading()
            Dim readLoop = FutureIterateExcept(AddressOf socket.FutureReadPacket, Sub(packet) ref.QueueAction(
                Sub()
                    Contract.Assume(packet IsNot Nothing)
                    Try
                        'If testHandlers.ContainsKey(packet.id) Then
                        '    Dim r = testHandlers(packet.id).TryParseAndHandle(packet.Payload.Data)
                        '    r.parseResult.CallWhenReady(
                        '        Sub(exception)
                        '            If exception Is Nothing Then  Return
                        '            logger.Log("Error parsing packet: {0}".Frmt(exception.ToString), LogMessageType.Problem)
                        '        End Sub)
                        '    r.handleResult.CallWhenReady(
                        '        Sub(exception)
                        '            If exception Is Nothing Then  Return
                        '            logger.Log("Error handling packet: {0}".Frmt(exception.ToString), LogMessageType.Problem)
                        '        End Sub)
                        'Else
                        If Not packetHandlers.ContainsKey(packet.id) Then
                            Dim msg = "Ignored a packet of type {0} from {1} because there is no parser for that packet type.".Frmt(packet.id, name)
                            logger.Log(msg, LogMessageType.Negative)
                        Else
                            Call packetHandlers(packet.id)(packet)
                        End If
                    Catch e As Exception
                        Dim msg = "Ignored a packet of type {0} from {1} because there was an error handling it: {2}".Frmt(packet.id, name, e)
                        logger.Log(msg, LogMessageType.Problem)
                        e.RaiseAsUnexpected(msg)
                    End Try
                End Sub
            ))
            readLoop.QueueCallWhenReady(ref,
                Sub(exception)
                    If exception Is Nothing Then  Return
                    Me.Disconnect(expected:=False,
                                  leaveType:=W3PlayerLeaveType.Disconnect,
                                  reason:="Error receiving packet from {0}: {1}".Frmt(name, exception.Message))
                End Sub
            )
        End Sub

        '''<summary>Disconnects this player and removes them from the system.</summary>
        Private Sub Disconnect(ByVal expected As Boolean,
                               ByVal leaveType As W3PlayerLeaveType,
                               ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            If Not Me.isFake Then
                socket.Disconnect(expected, reason)
            End If
            If pinger IsNot Nothing Then pinger.Dispose()
            RaiseEvent Disconnected(Me, expected, leaveType, reason)
        End Sub

        Private Sub SendPacket(ByVal pk As W3Packet)
            Contract.Requires(pk IsNot Nothing)
            If Me.isFake Then Return
            socket.SendPacket(pk)
        End Sub

        Private Sub CatchSocketDisconnected(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reason As String)
            ref.QueueAction(Sub()
                                Contract.Assume(reason IsNot Nothing)
                                Disconnect(expected, W3PlayerLeaveType.Disconnect, reason)
                            End Sub)
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

        Public ReadOnly Property GetRemoteEndPoint As Net.IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address IsNot Nothing)
                Return socket.RemoteEndPoint
            End Get
        End Property
        Public Function GetLatency() As IFuture(Of FiniteDouble)
            If pinger Is Nothing Then
                Return New FiniteDouble().Futurized
            Else
                Return pinger.QueueGetLatency
            End If
        End Function
        Public ReadOnly Property GetLatencyDescription As IFuture(Of String)
            Get
                Dim futureLatency = GetLatency()
                Dim futureTransferState = scheduler.GetClientState(Me.Index)
                Return New IFuture() {futureLatency, futureTransferState}.Defuturized.EvalOnSuccess(
                    Function()
                        Dim downloadState = futureTransferState.Value
                        Dim r = futureLatency.Value
                        Select Case downloadState
                            Case ClientTransferState.Downloading : Return "(dl)"
                            Case ClientTransferState.Uploading : Return "(ul)"
                            Case ClientTransferState.Idle : Return If(r = 0, "?", "{0:0}ms".Frmt(r))
                            Case ClientTransferState.Ready : Return If(r = 0, "?", "{0:0}ms".Frmt(r))
                            Case Else : Throw downloadState.MakeImpossibleValueException()
                        End Select
                    End Function)
            End Get
        End Property
        Public ReadOnly Property GetNumPeerConnections() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return numPeerConnections
            End Get
        End Property

        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal leaveType As W3PlayerLeaveType, ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(reason IsNot Nothing)
                                       Disconnect(expected, leaveType, reason)
                                   End Sub)
        End Function
        Public Function QueueSendPacket(ByVal packet As W3Packet) As IFuture
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(packet IsNot Nothing)
                                       SendPacket(packet)
                                   End Sub)
        End Function
    End Class
End Namespace
