Namespace Warcraft3
    Partial Public Class W3Player
        Implements IW3Player
        Public Property ready As Boolean Implements IW3Player.Ready

        Public Sub LoadScreenStart()
            LobbyStop()
            state = W3PlayerStates.Loading
            SendPacket(W3Packet.MakeStartLoading())
            handlers(W3PacketId.Ready) = AddressOf ReceiveReady
        End Sub
        Public Sub LoadScreenStop()
            handlers(W3PacketId.Ready) = Nothing
        End Sub

        Private Sub ReceiveReady(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            ready = True
            If game.server.settings.loadInGame Then
                handlers(W3PacketId.GameAction) = AddressOf ReceiveGameAction
            End If
            logger.log(name + " is ready", LogMessageTypes.Positive)
            'queued because otherwise the static verifier whines about invariants due to passing out 'me'
            eref.QueueAction(Sub()
                                 Contract.Assume(vals IsNot Nothing)
                                 game.QueueReceiveReady(Me, vals)
                             End Sub)
        End Sub

        Private Function _f_start() As IFuture Implements IW3Player.QueueStartLoading
            Return ref.QueueAction(AddressOf LoadScreenStart)
        End Function
    End Class
End Namespace
