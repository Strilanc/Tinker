Imports Tinker.Pickling

Namespace WC3
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

    Partial Public NotInheritable Class Player
        Public Event ReceivedRequestDropLaggers(ByVal sender As Player)
        Public Event ReceivedGameActions(ByVal sender As Player, ByVal actions As IReadableList(Of Protocol.GameAction))

        Private ReadOnly tickQueue As New Queue(Of TickRecord)
        Private totalTockTime As Integer
        Private maxTockTime As Integer

        Public Sub GamePlayStart()
            state = PlayerState.Playing
            AddQueuedLocalPacketHandler(Protocol.Packets.Tock, AddressOf ReceiveTock)
            AddQueuedLocalPacketHandler(Protocol.Packets.RequestDropLaggers, AddressOf ReceiveRequestDropLaggers)
            AddQueuedLocalPacketHandler(Protocol.Packets.ClientConfirmHostLeaving, Sub() SendPacket(Protocol.MakeHostConfirmHostLeaving()))
        End Sub

        Private Sub SendTick(ByVal record As TickRecord,
                             ByVal actions As IReadableList(Of Protocol.PlayerActionSet))
            Contract.Requires(record IsNot Nothing)
            If isFake Then Return
            tickQueue.Enqueue(record)
            maxTockTime += record.length
            SendPacket(Protocol.MakeTick(record.length, actions))
        End Sub
        Public Function QueueSendTick(ByVal record As TickRecord,
                                      ByVal actions As IReadableList(Of Protocol.PlayerActionSet)) As Task
            Contract.Requires(record IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendTick(record, actions))
        End Function

        Private Sub ReceiveRequestDropLaggers(ByVal pickle As ISimplePickle)
            RaiseEvent ReceivedRequestDropLaggers(Me)
        End Sub

        Private Sub ReceiveGameAction(ByVal pickle As IPickle(Of IReadableList(Of Protocol.GameAction)))
            Contract.Requires(pickle IsNot Nothing)
            outQueue.QueueAction(Sub() RaiseEvent ReceivedGameActions(Me, pickle.Value))
        End Sub
        Private Sub ReceiveTock(ByVal pickle As IPickle(Of NamedValueMap))
            Contract.Requires(pickle IsNot Nothing)
            If tickQueue.Count <= 0 Then
                logger.Log("Banned behavior: {0} responded to a tick which wasn't sent.".Frmt(Name), LogMessageType.Problem)
                Disconnect(True, Protocol.PlayerLeaveReason.Disconnect, "overticked")
                Return
            End If

            Dim record = tickQueue.Dequeue()
            Contract.Assume(record IsNot Nothing)
            totalTockTime += record.length
            Contract.Assume(totalTockTime >= 0)

            'Dim checksum = CType(vals("game state checksum"), IReadableList(Of Byte))
            'If synced Then
            '    If Not record.provide_value(checksum) Then
            '        synced = False
            '        game.broadcast_message_R("Desync detected.".frmt(name, unpackHexString(checksum)))
            '    End If
            'End If
        End Sub

        Public ReadOnly Property GetTockTime() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Return totalTockTime
            End Get
        End Property
        Public Function QueueStartPlaying() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(AddressOf GamePlayStart)
        End Function
    End Class
End Namespace
