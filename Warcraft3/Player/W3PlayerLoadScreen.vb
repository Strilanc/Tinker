Imports Tinker.Pickling

Namespace WC3
    Partial Public NotInheritable Class Player
        Private _ready As Boolean

        Public ReadOnly Property IsReady As Boolean
            Get
                Return isFake OrElse _ready
            End Get
        End Property

        Public Sub LoadScreenStart()
            state = PlayerState.Loading
            SendPacket(Protocol.MakeStartLoading())
            AddQueuedLocalPacketHandler(Protocol.Packets.Ready, AddressOf ReceiveReady)
            AddQueuedLocalPacketHandler(Protocol.Packets.GameAction, AddressOf ReceiveGameAction)
        End Sub

        Public Event ReceivedReady(ByVal sender As Player)
        Private Sub ReceiveReady(ByVal pickle As ISimplePickle)
            Contract.Requires(pickle IsNot Nothing)
            _ready = True
            logger.Log("{0} is ready".Frmt(Name), LogMessageType.Positive)
            'queued because otherwise the static verifier whines about invariants due to passing out 'me'
            outQueue.QueueAction(Sub()
                                     RaiseEvent ReceivedReady(Me)
                                 End Sub)
        End Sub

        Public Function QueueStartLoading() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(AddressOf LoadScreenStart)
        End Function
    End Class
End Namespace
