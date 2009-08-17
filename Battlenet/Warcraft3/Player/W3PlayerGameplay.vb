Namespace Warcraft3
    Public Class TickRecord
        Public ReadOnly length As UShort
        Public ReadOnly startTime As Integer
        Public ReadOnly Property EndTime() As Integer
            Get
                Return length + startTime
            End Get
        End Property

        Public Sub New(ByVal length As UShort, ByVal start_time As Integer)
            Me.length = length
            Me.startTime = start_time
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
        Implements IW3Player

        Private ReadOnly tickQueue As New Queue(Of TickRecord)
        Private totalTockTime As Integer

        Public Sub GamePlayStart()
            loadscreenStop()
            state = W3PlayerStates.Playing
            handlers(W3PacketId.AcceptHost) = AddressOf ReceiveAcceptHost
            handlers(W3PacketId.GameAction) = AddressOf ReceiveGameAction
            handlers(W3PacketId.Tock) = AddressOf ReceiveTock
            handlers(W3PacketId.ClientDropLagger) = AddressOf ReceiveDropLagger
        End Sub
        Public Sub GamePlayStop()
            handlers(W3PacketId.AcceptHost) = Nothing
            handlers(W3PacketId.GameAction) = Nothing
            handlers(W3PacketId.Tock) = Nothing
            handlers(W3PacketId.ClientDropLagger) = Nothing
        End Sub

        Private Sub SendTick(ByVal record As TickRecord, ByVal data As Byte())
            Contract.Requires(record IsNot Nothing)
            If isFake Then Return
            tickQueue.Enqueue(record)
            SendPacket(W3Packet.MakeTick(record.length, data))
        End Sub

#Region "Networking"
        Private Sub ReceiveDropLagger(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            game.QueueDropLagger()
        End Sub
        Private Sub ReceiveAcceptHost(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            SendPacket(W3Packet.MakeConfirmHost())
        End Sub
        Private Sub ReceiveGameAction(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Dim id = CByte(vals("id"))
            Dim data = CType(vals("data"), Byte())
            Contract.Assume(data IsNot Nothing)
            'queued because otherwise the static verifier whines about invariants due to passing out 'me'
            eref.QueueAction(Sub()
                                 game.QueueSendGameData(Me, Concat({id}, data))
                             End Sub)
        End Sub
        Private Sub ReceiveTock(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            If tickQueue.Count <= 0 Then
                logger.log("Banned behavior: {0} responded to a tick which wasn't sent.".frmt(name), LogMessageTypes.Problem)
                Disconnect(True, W3PlayerLeaveTypes.Disconnect, "overticked")
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
        Private ReadOnly Property _TockTime() As Integer Implements IW3Player.TockTime
            Get
                Return totalTockTime
            End Get
        End Property
        Private Function _QueueSendTick(ByVal record As TickRecord, ByVal data As Byte()) As IFuture Implements IW3Player.QueueSendTick
            Return ref.QueueAction(Sub()
                                       Contract.Assume(record IsNot Nothing)
                                       SendTick(record, data)
                                   End Sub)
        End Function
        Private Function _QueueStartPlaying() As IFuture Implements IW3Player.QueueStartPlaying
            Return ref.QueueAction(AddressOf GamePlayStart)
        End Function
        Private Function _QueueStopPlaying() As IFuture Implements IW3Player.QueueStopPlaying
            Return ref.QueueAction(AddressOf GamePlayStop)
        End Function
#End Region
    End Class
End Namespace
