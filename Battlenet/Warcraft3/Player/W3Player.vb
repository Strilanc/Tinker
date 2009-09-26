Namespace Warcraft3
    Public Enum W3PlayerStates
        Lobby
        Loading
        Playing
    End Enum
    Partial Public NotInheritable Class W3Player
        Private state As W3PlayerStates = W3PlayerStates.Lobby
        Public ReadOnly index As Byte
        Public ReadOnly game As W3Game
        Public ReadOnly name As String
        Public ReadOnly listenPort As UShort
        Public ReadOnly peerKey As UInteger
        Public ReadOnly isFake As Boolean
        Private ReadOnly testCanHost As IFuture
        Private Const MAX_NAME_LENGTH As Integer = 15
        Private ReadOnly socket As W3Socket
        Public ReadOnly logger As Logger
        Private ReadOnly ref As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly eref As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly pingQueue As New Queue(Of W3PlayerPingRecord)
        Private numPeerConnections As Integer
        Private latency As Double
        Public hasVotedToStart As Boolean
        Public numAdminTries As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(numPeerConnections >= 0)
            Contract.Invariant(numPeerConnections <= 12)
            Contract.Invariant(latency >= 0)
            Contract.Invariant(Not Double.IsNaN(latency))
            Contract.Invariant(Not Double.IsInfinity(latency))
            Contract.Invariant(pingQueue IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eref IsNot Nothing)
            Contract.Invariant(socket Is Nothing = isFake)
            Contract.Invariant(testCanHost IsNot Nothing)
            Contract.Invariant(numAdminTries >= 0)
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(game IsNot Nothing)
            Contract.Invariant(index > 0)
            Contract.Invariant(index <= 12)
            Contract.Invariant(totalTockTime >= 0)
            Contract.Invariant(mapDownloadPosition >= 0)
        End Sub

        '''<summary>Creates a fake player.</summary>
        Public Sub New(ByVal index As Byte,
                       ByVal game As W3Game,
                       ByVal name As String,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Requires(index > 0)
            Contract.Requires(index <= 12)
            Contract.Requires(game IsNot Nothing)
            Contract.Requires(name IsNot Nothing)

            Me.logger = If(logger, New Logger)
            Me.game = game
            Me.index = index
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

            Contract.Assume(Not Double.IsNaN(latency))
            Contract.Assume(Not Double.IsInfinity(latency))
        End Sub

        '''<summary>Creates a real player.</summary>
        '''<remarks>Real players are assigned a game by the lobby.</remarks>
        Public Sub New(ByVal index As Byte,
                       ByVal game As W3Game,
                       ByVal connectingPlayer As W3ConnectingPlayer,
                       Optional ByVal logging As Logger = Nothing)
            'Contract.Requires(index > 0)
            'Contract.Requires(index <= 12)
            'Contract.Requires(game IsNot Nothing)
            'Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Assume(index > 0)
            Contract.Assume(index <= 12)
            Contract.Assume(game IsNot Nothing)
            Contract.Assume(connectingPlayer IsNot Nothing)

            Me.logger = If(logging, New Logger)
            connectingPlayer.Socket.Logger = Me.logger
            Me.game = game
            Me.peerKey = connectingPlayer.PeerKey

            Me.socket = connectingPlayer.Socket
            Me.name = connectingPlayer.Name
            Me.listenPort = connectingPlayer.ListenPort
            Me.index = index
            AddHandler socket.Disconnected, AddressOf CatchSocketDisconnected

            handlers(W3PacketId.Pong) = AddressOf ReceivePong
            handlers(W3PacketId.Leaving) = AddressOf ReceiveLeaving
            handlers(W3PacketId.NonGameAction) = AddressOf ReceiveNonGameAction
            handlers(W3PacketId.MapFileDataReceived) = AddressOf IgnorePacket
            handlers(W3PacketId.MapFileDataProblem) = AddressOf IgnorePacket

            LobbyStart()
            Dim readLoop = FutureIterateExcept(AddressOf socket.FutureReadPacket, Sub(packet) ref.QueueAction(
                Sub()
                    Try
                        If handlers(packet.id) Is Nothing Then
                            Dim msg = "Ignored a packet of type {0} from {1} because there is no parser for that packet type.".Frmt(packet.id, name)
                            logger.Log(msg, LogMessageType.Negative)
                        Else
                            Call handlers(packet.id)(packet)
                        End If
                    Catch e As Exception
                        Dim msg = "Ignored a packet of type {0} from {1} because there was an error handling it: {2}".Frmt(packet.id, name, e)
                        logger.Log(msg, LogMessageType.Problem)
                        LogUnexpectedException(msg, e)
                    End Try
                End Sub
            ))
            readLoop.CallWhenReady(Sub(exception) ref.QueueAction(
                Sub()
                    Me.Disconnect(expected:=False,
                                  leaveType:=W3PlayerLeaveTypes.Disconnect,
                                  reason:="Error receiving packet from {0}: {1}".Frmt(name, exception.Message))
                End Sub))


            'Test hosting
            Me.testCanHost = FutureCreateConnectedTcpClient(socket.RemoteEndPoint.Address, listenPort)
            Me.testCanHost.MarkAnyExceptionAsHandled()
            Contract.Assume(Not Double.IsNaN(latency))
            Contract.Assume(Not Double.IsInfinity(latency))
        End Sub

        Public ReadOnly Property CanHost() As HostTestResults
            Get
                Dim state = testCanHost.State
                Select Case state
                    Case FutureState.Failed : Return HostTestResults.Fail
                    Case FutureState.Succeeded : Return HostTestResults.Pass
                    Case FutureState.Unknown : Return HostTestResults.Test
                    Case Else : Throw state.MakeImpossibleValueException()
                End Select
            End Get
        End Property

        '''<summary>Disconnects this player and removes them from the system.</summary>
        Private Sub Disconnect(ByVal expected As Boolean,
                               ByVal leaveType As W3PlayerLeaveTypes,
                               ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            If Not Me.isFake Then
                socket.Disconnect(expected, reason)
            End If
            'this queueing is just to get the verifier to stop whining about passing 'me' out breaking invariants
            Dim reason_ = reason
            eref.QueueAction(Sub()
                                 Contract.Assume(reason_ IsNot Nothing)
                                 game.QueueRemovePlayer(Me, expected, leaveType, reason_).MarkAnyExceptionAsHandled()
                             End Sub)
        End Sub

        Private Sub Ping(ByVal record As W3PlayerPingRecord)
            Contract.Requires(record IsNot Nothing)
            If Me.isFake Then Return
            pingQueue.Enqueue(record)
            If pingQueue.Count > 20 Then
                logger.Log(name + " has not responded to pings for a significant amount of time.", LogMessageType.Problem)
                Disconnect(True, W3PlayerLeaveTypes.Disconnect, "No response to pings.")
            End If
            SendPacket(W3Packet.MakePing(record.salt))
        End Sub

        Private Sub SendPacket(ByVal pk As W3Packet)
            Contract.Requires(pk IsNot Nothing)
            If Me.isFake Then Return
            socket.SendPacket(pk)
        End Sub

        Private Sub CatchSocketDisconnected(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reason As String)
            ref.QueueAction(Sub()
                                Contract.Assume(reason IsNot Nothing)
                                Disconnect(expected, W3PlayerLeaveTypes.Disconnect, reason)
                            End Sub)
        End Sub

#Region "Interface"
        Public ReadOnly Property GetRemoteEndPoint As Net.IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address IsNot Nothing)
                Return socket.RemoteEndPoint
            End Get
        End Property
        Public ReadOnly Property GetLatency() As Double
            Get
                Return latency
            End Get
        End Property
        Public ReadOnly Property GetLatencyDescription As IFuture(Of String)
            Get
                Return Me.game.DownloadScheduler.GetClientState(Me.index).QueueEvalOnValueSuccess(ref,
                    Function(downloadState)
                        Select Case downloadState
                            Case ClientTransferState.Downloading : Return "(dl)"
                            Case ClientTransferState.Uploading : Return "(ul)"
                            Case ClientTransferState.Idle : Return If(latency = 0, "?", "{0:0}ms".Frmt(latency))
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
        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal leave_type As W3PlayerLeaveTypes, ByVal reason As String) As IFuture
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(reason IsNot Nothing)
                                       Disconnect(expected, leave_type, reason)
                                   End Sub)
        End Function
        Public Function QueueSendPacket(ByVal pk As W3Packet) As IFuture
            Contract.Requires(pk IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(pk IsNot Nothing)
                                       SendPacket(pk)
                                   End Sub)
        End Function
        Public Function QueuePing(ByVal record As W3PlayerPingRecord) As IFuture
            Contract.Requires(record IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(record IsNot Nothing)
                                       Ping(record)
                                   End Sub)
        End Function
#End Region
    End Class
End Namespace
