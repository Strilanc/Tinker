Imports HostBot.Warcraft3.W3PacketId
Imports HostBot.Warcraft3

Namespace Warcraft3
    Public Class W3P2PConnectingPlayer
        Public ReadOnly socket As W3Socket
        Public ReadOnly receiver_p2p_key As Byte
        Public ReadOnly index As Byte
        Public ReadOnly connection_flags As UShort
        Public Sub New(ByVal socket As W3Socket, ByVal receiver_p2p_key As Byte, ByVal index As Byte, ByVal connection_flags As UShort)
            Me.socket = socket
            Me.receiver_p2p_key = receiver_p2p_key
            Me.index = index
            Me.connection_flags = connection_flags
        End Sub
    End Class

    Public Class W3P2PPlayer
        Public ReadOnly name As String
        Public ReadOnly index As Byte
        Public ReadOnly listen_port As UShort
        Public ReadOnly ip As Byte()
        Public ReadOnly p2p_key As UInteger
        Public WithEvents socket As W3Socket
        Public Event ReceivedPacket(ByVal sender As W3P2PPlayer, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
        Public Event Disconnected(ByVal sender As W3P2PPlayer)

        Public Sub New(ByVal name As String, ByVal index As Byte, ByVal listen_port As UShort, ByVal ip As Byte(), ByVal p2p_key As UInteger)
            Me.name = name
            Me.index = index
            Me.listen_port = listen_port
            Me.ip = ip
            Me.p2p_key = p2p_key
        End Sub

        Private Sub socket_Disconnected(ByVal sender As Warcraft3.W3Socket) Handles socket.Disconnected
            RaiseEvent Disconnected(Me)
        End Sub

        Private Sub socket_ReceivedPacket(ByVal sender As Warcraft3.W3Socket, ByVal id As W3PacketId, ByVal vals As System.Collections.Generic.Dictionary(Of String, Object)) Handles socket.ReceivedPacket
            RaiseEvent ReceivedPacket(Me, id, vals)
        End Sub
    End Class
End Namespace