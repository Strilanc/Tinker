Namespace Warcraft3
    Partial Public Class W3Player
        Public MustInherit Class W3PlayerPart
            Implements IW3PlayerPart

            Protected ReadOnly player As W3Player
            Protected ReadOnly handlers(0 To 255) As Action(Of Dictionary(Of String, Object))

            Public Sub New(ByVal container As W3Player)
                Me.player = container
                container.soul = Me
                handlers(W3PacketId.PONG) = AddressOf receivePacket_PONG_L
                handlers(W3PacketId.LEAVING) = AddressOf receivePacket_LEAVING_L
                handlers(W3PacketId.CLIENT_COMMAND) = AddressOf ReceivePacket_CLIENT_COMMAND
                handlers(W3PacketId.DL_RECEIVED_CHUNK) = AddressOf IgnorePacket
                handlers(W3PacketId.DL_CHUNK_PROBLEM) = AddressOf IgnorePacket
            End Sub

#Region "Networking"
            '''<summary>Processes packets coming from the remote computer.</summary>
            Private Sub ReceivePacket(ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object)) Implements IW3PlayerPart.receivePacket_L
                Try
                    If handlers(id) Is Nothing Then
                        Dim msg = String.Format("(Ignored) No handler for parsed packet of type {0} from {1}.", id, player.name)
                        player.logger.log(msg, LogMessageTypes.NegativeEvent)
                    Else
                        Call handlers(id)(vals)
                    End If
                Catch e As Exception
                    Dim msg = String.Format("(Ignored) Error handling packet of type {0} from {1}: {2}", id, player.name, e.Message)
                    player.logger.log(msg, LogMessageTypes.Problem)
                    Logging.logUnexpectedException(msg, e)
                End Try
            End Sub

            Private Sub ReceivePacket_CLIENT_COMMAND(ByVal vals As Dictionary(Of String, Object))
                player.game.f_ReceivePacket_CLIENT_COMMAND(player, vals)
            End Sub

            Private Sub IgnorePacket(ByVal vals As Dictionary(Of String, Object))
            End Sub

            Private Sub receivePacket_PONG_L(ByVal vals As Dictionary(Of String, Object))
                Dim lambda = 0.5
                Dim tick = Environment.TickCount
                Dim salt = ToUInteger(CType(vals("salt"), Byte()))

                If player.ping_queue.Count <= 0 Then
                    player.logger.log("Banned behavior: {0} responded to a ping which wasn't sent.".frmt(player.name), LogMessageTypes.Problem)
                    player.disconnect_L(True, W3PlayerLeaveTypes.disc)
                    Return
                End If

                Dim stored = player.ping_queue.Dequeue()
                If salt <> stored.salt Then
                    player.logger.log("Banned behavior: {0} responded incorrectly to a ping. {1} was returned instead of {2}.".frmt(player.name, salt, stored.salt), LogMessageTypes.Problem)
                    player.disconnect_L(True, W3PlayerLeaveTypes.disc)
                    Return
                End If

                player.latency *= 1 - lambda
                player.latency += lambda * TickCountDelta(tick, stored.time)
            End Sub

            Private Sub receivePacket_LEAVING_L(ByVal vals As Dictionary(Of String, Object))
                Dim leave_type = CType(vals("leave type"), W3PlayerLeaveTypes)
                player.disconnect_L(True, leave_type)
            End Sub
#End Region

#Region "Misc"
            '''<summary>Returns the player who is a better host.</summary>
            Public Shared Function ReduceBetterHost(ByVal p1 As IW3Player, ByVal p2 As IW3Player) As IW3Player
                If p1 Is Nothing OrElse p1.is_fake Then Return p2
                If p2 Is Nothing OrElse p2.is_fake Then Return p1

                'To start with: can they host at all?
                Dim h1 = p1.canHost, h2 = p2.canHost
                If h1 <> h2 Then Return If(h1 > h2, p1, p2)

                'Ok, then are they connected to more people?
                Dim n1 = p1.num_p2p_connections_P, n2 = p2.num_p2p_connections_P
                If n1 <> n2 Then Return If(n1 > n2, p1, p2)

                'Pretty close, but who has the better latency?
                Dim d1 = p1.latency_P(), d2 = p2.latency_P()
                If d1 <> d2 Then Return If(d1 < d2, p1, p2)

                'Doesn't seem to matter who we pick at this point
                Return p1
            End Function
#End Region

#Region "Interface"
            Protected Overridable ReadOnly Property _get_percent_dl() As Byte Implements IW3PlayerPart.get_percent_dl
                Get
                    Return 100
                End Get
            End Property

            Private ReadOnly Property _container() As IW3Player Implements IW3PlayerPart.player
                Get
                    Return player
                End Get
            End Property
#End Region

            Public Overridable Function Description() As String Implements IW3PlayerPart.Description
                Return padded(player.name, 20) _
                        + padded("Host={0}".frmt(player.canHost()), 12) _
                        + padded("{0}c".frmt(player.num_p2p_connections), 5) _
                        + padded("RTT={0:0}ms".frmt(player.latency), 12)
            End Function
        End Class
    End Class
End Namespace
