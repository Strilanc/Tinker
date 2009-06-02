Namespace Warcraft3
    Partial Public Class W3Player
        Public Class W3PlayerLoadScreen
            Inherits W3Player.W3PlayerPart
            Implements IW3PlayerLoadScreen

            Public Sub New(ByVal body As W3Player)
                MyBase.New(body)
            End Sub

            Public Sub Start()
                player.soul = Me
                player.lobby.Stop()
                player.send_packet_L(W3Packet.MakePacket_START_LOADING())
                handlers(W3PacketId.READY) = AddressOf receivePacket_READY_L
                handlers(W3PacketId.PLAYERS_CONNECTED) = AddressOf receivePacket_PLAYERS_CONNECTED_L
            End Sub
            Public Sub [Stop]()
                handlers(W3PacketId.READY) = Nothing
                handlers(W3PacketId.PLAYERS_CONNECTED) = Nothing
            End Sub

            Private Sub receivePacket_READY_L(ByVal vals As System.Collections.Generic.Dictionary(Of String, Object))
                player.ready = True
                player.logger.log(player.name + " is ready", LogMessageTypes.Positive)
                player.game.load_screen.f_ReceivePacket_READY(player, vals)
            End Sub
            Private Sub receivePacket_PLAYERS_CONNECTED_L(ByVal vals As Dictionary(Of String, Object))
                Dim n = CUInt(vals("player bitflags"))
                player.num_p2p_connections = 0
                For p As Byte = 1 To 12
                    player.num_p2p_connections += CInt(n And &H1)
                    n >>= 1
                Next p
            End Sub

            Public Overrides Function Description() As String
                Return MyBase.Description() _
                        + "Ready={0}".frmt(player.ready)
            End Function

            Public Property _ready() As Boolean Implements IW3PlayerLoadScreen.ready
                Get
                    Return player.ready
                End Get
                Set(ByVal value As Boolean)
                    player.ready = value
                End Set
            End Property

            Private Function _f_start() As IFuture Implements IW3PlayerLoadScreen.f_Start
                Return player.ref.QueueAction(AddressOf Start)
            End Function
        End Class
    End Class
End Namespace
