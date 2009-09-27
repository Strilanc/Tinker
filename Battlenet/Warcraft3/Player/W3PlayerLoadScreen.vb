Namespace Warcraft3
    Partial Public Class W3Player
        Public Property Ready As Boolean

        Public Sub LoadScreenStart()
            LobbyStop()
            state = W3PlayerState.Loading
            SendPacket(W3Packet.MakeStartLoading())
            handlers(W3PacketId.Ready) = AddressOf ReceiveReady
        End Sub
        Public Sub LoadScreenStop()
            handlers(W3PacketId.Ready) = Nothing
        End Sub

        Private Sub ReceiveReady(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
            Ready = True
            If game.server.settings.loadInGame Then
                handlers(W3PacketId.GameAction) = AddressOf ReceiveGameAction
            End If
            logger.Log(name + " is ready", LogMessageType.Positive)
            'queued because otherwise the static verifier whines about invariants due to passing out 'me'
            eref.QueueAction(Sub()
                                 Contract.Assume(vals IsNot Nothing)
                                 game.QueueReceiveReady(Me, vals)
                             End Sub)
        End Sub

        Public Function QueueStartLoading() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(AddressOf LoadScreenStart)
        End Function
    End Class
End Namespace
