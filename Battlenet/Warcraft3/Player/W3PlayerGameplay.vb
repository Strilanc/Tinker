Namespace Warcraft3
    Public Class TickRecord
        Public ReadOnly length As UShort
        Public ReadOnly startTime As Integer
        Public ReadOnly Property EndTime() As Integer
            Get
                Return length + startTime
            End Get
        End Property

        Public Sub New(ByVal length As UShort, ByVal startTime As Integer)
            Me.length = length
            Me.startTime = startTime
        End Sub

        'Private checksum As Byte() = Nothing
        'Public Function provide_value(ByVal checksum As Byte()) As Boolean
        '    SyncLock Me
        '        If Me.checksum Is Nothing Then
        '            Me.checksum = checksum
        '            Return True
        '        End If
        '    End SyncLock

        '    Return ArraysEqual(Me.checksum, checksum)
        'End Function
    End Class

    Partial Public Class W3Player
        Private ReadOnly tickQueue As New Queue(Of TickRecord)
        Private totalTockTime As Integer

        Public Sub GamePlayStart()
            loadscreenStop()
            state = W3PlayerState.Playing
            packetHandlers(W3PacketId.AcceptHost) = AddressOf ReceiveAcceptHost
            packetHandlers(W3PacketId.GameAction) = AddressOf ReceiveGameAction
            packetHandlers(W3PacketId.Tock) = AddressOf ReceiveTock
            packetHandlers(W3PacketId.ClientDropLagger) = AddressOf ReceiveDropLagger
        End Sub
        Public Sub GamePlayStop()
            packetHandlers.Remove(W3PacketId.AcceptHost)
            packetHandlers.Remove(W3PacketId.GameAction)
            packetHandlers.Remove(W3PacketId.Tock)
            packetHandlers.Remove(W3PacketId.ClientDropLagger)
        End Sub

        Private Sub SendTick(ByVal record As TickRecord, ByVal data As Byte())
            Contract.Requires(record IsNot Nothing)
            If isFake Then Return
            tickQueue.Enqueue(record)
            SendPacket(W3Packet.MakeTick(record.length, data))
        End Sub

#Region "Networking"
        Private Sub ReceiveDropLagger(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
            game.QueueDropLagger()
        End Sub
        Private Sub ReceiveAcceptHost(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
            SendPacket(W3Packet.MakeConfirmHost())
        End Sub

        Private Sub ReceiveGameAction(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)

            Dim vals = CType(packet.Payload.Value, Dictionary(Of String, Object))
            Contract.Assume(vals IsNot Nothing)
            Dim actions = CType(vals("actions"), IEnumerable(Of W3GameAction))
            Contract.Assume(actions IsNot Nothing)
            For Each action In actions
                Contract.Assume(action IsNot Nothing)
                game.QueueReceiveGameAction(Me, action)
            Next action
            game.QueueSendGameData(Me, packet.payload.Data.SubView(4).ToArray)
        End Sub
        Private Sub ReceiveTock(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
            If tickQueue.Count <= 0 Then
                logger.Log("Banned behavior: {0} responded to a tick which wasn't sent.".Frmt(name), LogMessageType.Problem)
                Disconnect(True, W3PlayerLeaveType.Disconnect, "overticked")
                Return
            End If

            Dim record = tickQueue.Dequeue()
            totalTockTime += record.length

            'Dim checksum = CType(vals("game state checksum"), Byte())
            'If synced Then
            '    If Not record.provide_value(checksum) Then
            '        synced = False
            '        game.broadcast_message_R("Desync detected.".frmt(name, unpackHexString(checksum)))
            '    End If
            'End If
        End Sub
#End Region

#Region "Interface"
        Public ReadOnly Property GetTockTime() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Return totalTockTime
            End Get
        End Property
        Public Function QueueSendTick(ByVal record As TickRecord, ByVal data As Byte()) As IFuture
            Contract.Requires(record IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(record IsNot Nothing)
                                       SendTick(record, data)
                                   End Sub)
        End Function
        Public Function QueueStartPlaying() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(AddressOf GamePlayStart)
        End Function
        Public Function QueueStopPlaying() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(AddressOf GamePlayStop)
        End Function
#End Region
    End Class
End Namespace
