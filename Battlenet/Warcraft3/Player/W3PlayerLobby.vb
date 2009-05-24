Namespace Warcraft3
    Partial Public Class W3Player
        Public Class W3PlayerLobby
            Inherits W3Player.W3PlayerPart
            Implements IW3PlayerLobby

            Private know_map_state As Boolean = False
            Private downloaded_map_size_L As Integer = -1
            Private getting_map_from_bot As Boolean = False
            Private sent_map_size As Integer = 0
            Private countdowns As Integer
            Private Const MAX_BUFFERED_MAP_SIZE As UInteger = 64000


            Public Sub New(ByVal body As W3Player)
                MyBase.New(body)
                handlers(W3PacketId.DL_STATE) = AddressOf receivePacket_DL_STATE_L
                handlers(W3PacketId.PLAYERS_CONNECTED) = AddressOf receivePacket_PLAYERS_CONNECTED_L
            End Sub
            Public Sub Start()
                player.soul = Me
                handlers(W3PacketId.DL_STATE) = AddressOf receivePacket_DL_STATE_L
                handlers(W3PacketId.PLAYERS_CONNECTED) = AddressOf receivePacket_PLAYERS_CONNECTED_L
            End Sub
            Public Sub [Stop]()
                handlers(W3PacketId.DL_STATE) = Nothing
                handlers(W3PacketId.PLAYERS_CONNECTED) = Nothing
            End Sub

#Region "Networking"
            Private Sub receivePacket_PLAYERS_CONNECTED_L(ByVal vals As Dictionary(Of String, Object))
                Dim n = CUInt(vals("player bitflags"))
                player.num_p2p_connections = 0
                For p As Byte = 1 To 12
                    Dim connected = (n And &H1) <> 0
                    player.game.lobby.download_scheduler.set_link(player.index, p, connected)
                    If connected Then player.num_p2p_connections += 1
                    n >>= 1
                Next p
                player.game.f_ThrowUpdated()
            End Sub
            Private Sub receivePacket_DL_STATE_L(ByVal vals As Dictionary(Of String, Object))
                Dim new_downloaded_size = CInt(vals("total downloaded"))
                Dim delta = new_downloaded_size - downloaded_map_size_L
                If delta < 0 Then
                    player.logger.log("Banned behavior: {0} moved download state backwards from {1} to {2}.".frmt(player.name, downloaded_map_size_L, new_downloaded_size), LogMessageTypes.Problem)
                    player.disconnect_L(True, W3PlayerLeaveTypes.disc)
                    Return
                ElseIf new_downloaded_size > player.game.map.fileSize Then
                    player.logger.log("Banned behavior: {0} moved download state past file size.".frmt(player.name), LogMessageTypes.Problem)
                    player.disconnect_L(True, W3PlayerLeaveTypes.disc)
                    Return
                ElseIf downloaded_map_size_L = player.game.map.fileSize Then
                    '[previously finished download]
                    Return
                End If

                downloaded_map_size_L = new_downloaded_size
                sent_map_size = Math.Max(downloaded_map_size_L, sent_map_size)
                If Not know_map_state Then
                    know_map_state = True
                    Dim has_map = CBool(downloaded_map_size_L = player.game.map.fileSize)

                    If has_map Then
                        player.game.lobby.download_scheduler.add(player.index, has_map)
                    Else
                        If Not player.game.parent.settings.allowDownloads Then
                            player.logger.log(player.name + " doesn't have the map and DLs are not allowed.", LogMessageTypes.NegativeEvent)
                            player.disconnect_L(True, W3PlayerLeaveTypes.disc)
                            Return
                        End If
                        player.game.lobby.download_scheduler.add(player.index, has_map)
                    End If
                    player.game.lobby.download_scheduler.set_link(player.index, 255, True)
                ElseIf downloaded_map_size_L = player.game.map.fileSize Then
                    player.logger.log(player.name + " finished downloading the map.", LogMessageTypes.PositiveEvent)
                    player.game.lobby.download_scheduler.stop_transfer(player.index, True)
                Else
                    player.game.lobby.download_scheduler.update_progress(player.index, downloaded_map_size_L)
                    If getting_map_from_bot Then
                        BufferMap()
                    End If
                End If

                player.game.lobby.f_UpdatedGameState()
            End Sub
#End Region

#Region "Interface"
            Protected Overrides ReadOnly Property _get_percent_dl() As Byte
                Get
                    Return get_percent_dl()
                End Get
            End Property
            Private ReadOnly Property _downloaded_map_size_P() As Integer Implements IW3PlayerLobby.downloaded_map_size_P
                Get
                    Return downloaded_map_size_L
                End Get
            End Property
            Private ReadOnly Property _overcounted() As Boolean Implements IW3PlayerLobby.overcounted
                Get
                    Return countdowns > 1
                End Get
            End Property
            Private Property _getting_map_from_bot() As Boolean Implements IW3PlayerLobby.getting_map_from_bot
                Get
                    Return getting_map_from_bot
                End Get
                Set(ByVal value As Boolean)
                    getting_map_from_bot = value
                End Set
            End Property
            Private Function _buffer_map_R() As IFuture Implements IW3PlayerLobby.f_BufferMap
                Return player.ref.enqueue(AddressOf BufferMap)
            End Function
            Private Function _start_countdown_R() As IFuture Implements IW3PlayerLobby.f_StartCountdown
                Return player.ref.enqueue(AddressOf start_countdown_L)
            End Function
#End Region

#Region "Misc"
            Private Sub start_countdown_L()
                countdowns += 1
                If countdowns > 1 Then Return
                player.send_packet_L(W3Packet.MakePacket_START_COUNTDOWN())
            End Sub
            Private Function get_percent_dl() As Byte
                Dim buffered_downloaded_size = downloaded_map_size_L
                If player.is_fake Then Return 254 'Not a real player, show "|CF"
                If buffered_downloaded_size = -1 Then Return 255 'Not known yet, show "?"
                If buffered_downloaded_size >= player.game.map.fileSize Then Return 100 'No DL, show nothing
                Return CByte((100 * buffered_downloaded_size) \ player.game.map.fileSize) 'DL running, show % done
            End Function

            Private Sub BufferMap()
                Dim f_host = player.game.f_fake_host_player
                Dim f_index = FutureFunc(Of Byte).frun(Function(player) If(player Is Nothing, CByte(0), player.index), f_host)
                FutureSub.frun(AddressOf _BufferMap, f_index)
            End Sub
            Private Sub _BufferMap(ByVal sender_index As Byte)
                player.ref.enqueue(Function() eval(AddressOf __BufferMap, sender_index))
            End Sub
            Private Sub __BufferMap(ByVal sender_index As Byte)
                If sent_map_size - downloaded_map_size_L >= MAX_BUFFERED_MAP_SIZE Then Return
                If sent_map_size >= player.game.map.fileSize Then Return

                Dim data_size = 0
                Dim pk = W3Packet.MakePacket_DL_MAP_CHUNK(sender_index, player.game.map, player.index, sent_map_size, data_size)
                If pk Is Nothing Then Return
                sent_map_size += data_size
                player.send_packet_L(pk)
            End Sub
#End Region

            Public Overrides Function Description() As String
                Return MyBase.Description() _
                        + padded("DL={0}%".frmt(get_percent_dl), 9) _
                        + "EB={0}".frmt(player.game.lobby.download_scheduler.rate_estimate_string(player.index))
            End Function
        End Class
    End Class
End Namespace