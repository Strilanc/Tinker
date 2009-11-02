Namespace Warcraft3
    Public NotInheritable Class TickRecord
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

    Partial Public NotInheritable Class W3Player
        Public Event ReceivedDropLagger(ByVal sender As W3Player)
        Public Event ReceivedGameAction(ByVal sender As W3Player, ByVal action As W3GameAction)
        Public Event ReceivedGameData(ByVal sender As W3Player, ByVal data As Byte())

        Private ReadOnly tickQueue As New Queue(Of TickRecord)
        Private totalTockTime As Integer
        Private maxTockTime As Integer

        Public Sub GamePlayStart()
            state = W3PlayerState.Playing
            AddQueuePacketHandler(W3Packet.Jars.Tock, AddressOf ReceiveTock)
            AddQueuePacketHandler(W3Packet.Jars.ClientDropLagger, AddressOf ReceiveDropLagger)
        End Sub

        Private Sub SendTick(ByVal record As TickRecord, ByVal data As Byte())
            Contract.Requires(record IsNot Nothing)
            If isFake Then Return
            tickQueue.Enqueue(record)
            maxTockTime += record.length
            SendPacket(W3Packet.MakeTick(record.length, data))
        End Sub

#Region "Networking"
        Private Sub ReceiveDropLagger(ByVal pickle As IPickle(Of Dictionary(Of String, Object)))
            RaiseEvent ReceivedDropLagger(Me)
        End Sub

        Private Sub ReceiveGameAction(ByVal pickle As IPickle(Of Dictionary(Of String, Object)))
            Contract.Requires(Pickle IsNot Nothing)
            Dim vals = CType(Pickle.Value, Dictionary(Of String, Object))
            Dim actions = CType(vals("actions"), IEnumerable(Of W3GameAction))
            Contract.Assume(actions IsNot Nothing)
            For Each action In actions
                RaiseEvent ReceivedGameAction(Me, action)
            Next action
            RaiseEvent ReceivedGameData(Me, pickle.Data.ToArray)
        End Sub
        Private Sub ReceiveTock(ByVal pickle As IPickle(Of Dictionary(Of String, Object)))
            Contract.Requires(Pickle IsNot Nothing)
            Dim vals = CType(Pickle.Value, Dictionary(Of String, Object))
            If tickQueue.Count <= 0 Then
                logger.Log("Banned behavior: {0} responded to a tick which wasn't sent.".Frmt(name), LogMessageType.Problem)
                Disconnect(True, W3PlayerLeaveType.Disconnect, "overticked")
                Return
            End If

            Dim record = tickQueue.Dequeue()
            Contract.Assume(record IsNot Nothing)
            totalTockTime += record.length
            Contract.Assume(totalTockTime >= 0)

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
#End Region
    End Class
End Namespace
