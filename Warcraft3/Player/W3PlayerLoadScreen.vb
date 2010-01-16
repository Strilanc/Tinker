Imports Tinker.Pickling

Namespace WC3
    Partial Public NotInheritable Class Player
        Public Property Ready As Boolean

        Public Sub LoadScreenStart()
            state = PlayerState.Loading
            SendPacket(Protocol.MakeStartLoading())
            AddQueuedPacketHandler(Protocol.Packets.Ready, AddressOf ReceiveReady)
            AddQueuedPacketHandler(Protocol.Packets.GameAction, AddressOf ReceiveGameAction)
        End Sub

        Public Event ReceivedReady(ByVal sender As Player)
        Private Sub ReceiveReady(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(pickle IsNot Nothing)
            Dim vals = CType(pickle.Value, Dictionary(Of InvariantString, Object))
            Ready = True
            logger.Log("{0} is ready".Frmt(Name), LogMessageType.Positive)
            'queued because otherwise the static verifier whines about invariants due to passing out 'me'
            outQueue.QueueAction(Sub()
                                     RaiseEvent ReceivedReady(Me)
                                 End Sub)
        End Sub

        Public Function QueueStartLoading() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(AddressOf LoadScreenStart)
        End Function
    End Class
End Namespace
