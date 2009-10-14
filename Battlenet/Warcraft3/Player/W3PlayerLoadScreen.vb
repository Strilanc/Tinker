Namespace Warcraft3
    Partial Public NotInheritable Class W3Player
        Public Property Ready As Boolean

        Public Sub LoadScreenStart()
            LobbyStop()
            state = W3PlayerState.Loading
            SendPacket(W3Packet.MakeStartLoading())
            packetHandlers(W3PacketId.Ready) = AddressOf ReceiveReady
        End Sub
        Public Sub LoadScreenStop()
            packetHandlers.Remove(W3PacketId.Ready)
        End Sub

        Public Event ReceivedReady(ByVal sender As W3Player)
        Private Sub ReceiveReady(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
            Ready = True
            If settings.loadInGame Then
                packetHandlers(W3PacketId.GameAction) = AddressOf ReceiveGameAction
            End If
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
