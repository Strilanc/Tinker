Namespace Warcraft3
    Partial Public NotInheritable Class W3Player
        Public Property Ready As Boolean

        Public Sub LoadScreenStart()
            state = W3PlayerState.Loading
            SendPacket(W3Packet.MakeStartLoading())
            AddQueuePacketHandler(W3Packet.Jars.Ready, AddressOf ReceiveReady)
            AddQueuePacketHandler(W3Packet.Jars.GameAction, AddressOf ReceiveGameAction)
        End Sub

        Public Event ReceivedReady(ByVal sender As W3Player)
        Private Sub ReceiveReady(ByVal pickle As IPickle(Of Dictionary(Of String, Object)))
            Contract.Requires(Pickle IsNot Nothing)
            Dim vals = CType(Pickle.Value, Dictionary(Of String, Object))
            Ready = True
            logger.Log(name + " is ready", LogMessageType.Positive)
            'queued because otherwise the static verifier whines about invariants due to passing out 'me'
            eref.QueueAction(Sub()
                                 RaiseEvent ReceivedReady(Me)
                             End Sub)
        End Sub

        Public Function QueueStartLoading() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(AddressOf LoadScreenStart)
        End Function
    End Class
End Namespace
