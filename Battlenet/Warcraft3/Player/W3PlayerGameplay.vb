Namespace Warcraft3
    Public Class TickRecord
        Public ReadOnly length As UShort
        Public ReadOnly start_time As Integer
        Public ReadOnly Property end_time() As Integer
            Get
                Return length + start_time
            End Get
        End Property

        Public Sub New(ByVal length As UShort, ByVal start_time As Integer)
            Me.length = length
            Me.start_time = start_time
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
        Private totalTockTime As Integer = 0

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

        Private Sub QueueTick(ByVal record As TickRecord)
            If isFake Then Return
            tickQueue.Enqueue(record)
        End Sub

#Region "Networking"
        Private Sub ReceiveDropLagger(ByVal vals As Dictionary(Of String, Object))
            game.f_DropLagger()
        End Sub
        Private Sub ReceiveAcceptHost(ByVal vals As Dictionary(Of String, Object))
            SendPacket(W3Packet.MakeConfirmHost())
        End Sub
        Private Sub ReceiveGameAction(ByVal vals As Dictionary(Of String, Object))
            Dim id = CByte(vals("id"))
            Dim data = CType(vals("data"), Byte())
            game.f_QueueGameData(Me, Concat({New Byte() {id}, data}))
        End Sub
        Private Sub ReceiveTock(ByVal vals As Dictionary(Of String, Object))
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
        Private Function _f_QueueTick(ByVal record As TickRecord) As IFuture Implements IW3Player.f_QueueTick
            Return ref.QueueAction(Sub() QueueTick(record))
        End Function
#End Region

        Private Function _f_StartPlaying() As IFuture Implements IW3Player.f_StartPlaying
            Return ref.QueueAction(AddressOf GamePlayStart)
        End Function
        Private Function _f_StopPlaying() As IFuture Implements IW3Player.f_StopPlaying
            Return ref.QueueAction(AddressOf GamePlayStop)
        End Function
    End Class
End Namespace
