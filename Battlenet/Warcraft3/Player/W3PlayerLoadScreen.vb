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
            ready = True
            logger.log(name + " is ready", LogMessageTypes.Positive)
            game.f_ReceiveReady(Me, vals)
        End Sub


        Private Function _f_start() As IFuture Implements IW3Player.f_StartLoading
            Return ref.QueueAction(AddressOf LoadScreenStart)
        End Function
    End Class
End Namespace
