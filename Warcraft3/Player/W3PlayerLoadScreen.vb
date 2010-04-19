Imports Tinker.Pickling

Namespace WC3
    Partial Public NotInheritable Class Player
        Private _ready As Boolean

        Public ReadOnly Property IsReady As Boolean
            Get
                Return isFake OrElse _ready
            End Get
        End Property

        Public Event ReceivedReady(ByVal sender As Player)
        Private Sub ReceiveReady(ByVal pickle As ISimplePickle)
            Contract.Requires(pickle IsNot Nothing)
            _ready = True
            logger.Log("{0} is ready".Frmt(Name), LogMessageType.Positive)
            outQueue.QueueAction(Sub() RaiseEvent ReceivedReady(Me))
        End Sub

        Private Sub StartLoading()
            state = PlayerState.Loading
            SendPacket(Protocol.MakeStartLoading())
            AddQueuedLocalPacketHandler(Protocol.ClientPackets.Ready, AddressOf ReceiveReady)
            AddQueuedLocalPacketHandler(Protocol.ClientPackets.GameAction, AddressOf ReceiveGameAction)
        End Sub
        Public Function QueueStartLoading() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(AddressOf StartLoading)
        End Function
    End Class
End Namespace
