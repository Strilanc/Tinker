Imports HostBot.Warcraft3.W3PacketId
Imports HostBot.Warcraft3

Namespace Warcraft3
    Public Class W3DummyPlayer
        Private ReadOnly name As String
        Private ReadOnly listen_port As UShort
        Private ReadOnly ref As ICallQueue
        Private ReadOnly other_players As New List(Of W3P2PPlayer)
        Private ReadOnly logger As MultiLogger
        Private WithEvents socket As W3Socket
        Private WithEvents accepter As New W3P2PConnectionAccepter()
        Public ready_delay As TimeSpan = TimeSpan.Zero
        Private index As Byte
        Private dl As W3MapDownload
        Private pool_port As PortPool.PortHandle
        Public Enum Modes
            DownloadMap
            EnterGame
        End Enum
        Private mode As Modes

#Region "Life"
        Public Sub New(ByVal name As String, _
                       ByVal pool_port As PortPool.PortHandle, _
                       Optional ByVal logger As MultiLogger = Nothing, _
                       Optional ByVal mode As Modes = Modes.DownloadMap)
            Me.New(name, pool_port.port, logger, mode)
            Me.pool_port = pool_port
        End Sub
        Public Sub New(ByVal name As String, _
                       Optional ByVal listen_port As UShort = 0, _
                       Optional ByVal logger As MultiLogger = Nothing, _
                       Optional ByVal mode As Modes = Modes.DownloadMap)
            Me.name = name
            Me.mode = mode
            Me.listen_port = listen_port
            Me.ref = New ThreadedCallQueue("{0} {1} ref".frmt(Me.GetType.Name, name))
            Me.logger = If(logger, New MultiLogger)
            If listen_port <> 0 Then accepter.accepter.OpenPort(listen_port)
        End Sub
#End Region

#Region "Networking"
        Public Function f_Connect(ByVal hostname As String, ByVal port As UShort) As IFuture(Of Outcome)
            Return ref.enqueue(Function() Connect(hostname, port))
        End Function
        Private Function Connect(ByVal hostname As String, ByVal port As UShort) As Outcome
            Try
                Dim tcp = New Net.Sockets.TcpClient()
                tcp.Connect(hostname, port)
                socket = New W3Socket(New BnetSocket(tcp, Me.logger))
                socket.set_reading(True)
                socket.SendPacket(W3Packet.MakePacket_KNOCK(name, listen_port, CUShort(socket.getLocalPort)))
                Return success("Connection established.")
            Catch e As Exception
                Return failure(e.Message)
            End Try
        End Function

        Private Function ReceiveDLMapChunk(ByVal vals As Dictionary(Of String, Object)) As Boolean
            If dl Is Nothing OrElse dl.file Is Nothing Then Throw New InvalidOperationException()

            If dl.receive_chunk(CInt(vals("file position")), CType(vals("file data"), Byte())) Then
                socket.SendPacket(W3Packet.MakePacket_DL_TOTAL_RECEIVED(W3Packet.DownloadState.NotDownloading, dl.size))
                Return True
            Else
                socket.SendPacket(W3Packet.MakePacket_DL_TOTAL_RECEIVED(W3Packet.DownloadState.Downloading, CUInt(dl.file.Position)))
                Return False
            End If
        End Function
        Private Sub SendPlayersConnected()
            socket.SendPacket(W3Packet.MakePacket_PLAYERS_CONNECTED(From p In other_players Where p.socket IsNot Nothing Select p.index))
        End Sub

        Private Sub c_Disconnect(ByVal sender As W3Socket) Handles socket.Disconnected
            ref.enqueue(AddressOf Disconnect)
        End Sub
        Private Sub Disconnect()
            socket.disconnect()
            accepter.accepter.CloseAllPorts()
            For Each player In other_players
                If player.socket IsNot Nothing Then
                    player.socket.disconnect()
                    player.socket = Nothing
                    RemoveHandler player.ReceivedPacket, AddressOf c_P2PReceivedPacket
                    RemoveHandler player.Disconnected, AddressOf c_P2PDisconnection
                End If
            Next player
            other_players.Clear()
            If pool_port IsNot Nothing Then
                pool_port.Dispose()
                pool_port = Nothing
            End If
        End Sub

        Private Sub c_ReceivedPacket(ByVal sender As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object)) Handles socket.ReceivedPacket
            ref.enqueue(Function() eval(AddressOf _c_ReceivedPacket, id, vals))
        End Sub
        Private Sub _c_ReceivedPacket(ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
            Try
                Select Case id
                    Case GREET
                        index = CByte(vals("player index"))
                    Case MAP_INFO
                        If mode = Modes.DownloadMap Then
                            dl = New W3MapDownload(CStr(vals("path")), CUInt(vals("size")), CType(vals("crc32"), Byte()), CType(vals("xoro checksum"), Byte()), CType(vals("sha1 checksum"), Byte()))
                            socket.SendPacket(W3Packet.MakePacket_DL_TOTAL_RECEIVED(W3Packet.DownloadState.NotDownloading, 0))
                        Else
                            socket.SendPacket(W3Packet.MakePacket_DL_TOTAL_RECEIVED(W3Packet.DownloadState.NotDownloading, CUInt(vals("size"))))
                        End If
                    Case PING
                        socket.SendPacket(W3Packet.MakePacket_PONG(CType(vals("salt"), Byte())))
                    Case OTHER_PLAYER_JOINED
                        Dim ext_addr = CType(vals("external address"), Dictionary(Of String, Object))
                        Dim player = New W3P2PPlayer(CStr(vals("name")), _
                                                     CByte(vals("index")), _
                                                     CUShort(ext_addr("port")), _
                                                     CType(ext_addr("ip"), Byte()), _
                                                     CUInt(vals("p2p key")))
                        other_players.Add(player)
                        AddHandler player.ReceivedPacket, AddressOf c_P2PReceivedPacket
                        AddHandler player.Disconnected, AddressOf c_P2PDisconnection
                    Case OTHER_PLAYER_LEFT
                        Dim player = (From p In other_players Where p.index = CByte(vals("player index"))).FirstOrDefault
                        If player IsNot Nothing Then
                            other_players.Remove(player)
                            RemoveHandler player.ReceivedPacket, AddressOf c_P2PReceivedPacket
                            RemoveHandler player.Disconnected, AddressOf c_P2PDisconnection
                        End If
                    Case START_LOADING
                        If mode = Modes.DownloadMap Then
                            Disconnect()
                        ElseIf mode = Modes.EnterGame Then
                            FutureSub.schedule(Function() socket.SendPacket(W3Packet.MakePacket_READY()), futurewait(ready_delay))
                        End If
                    Case TICK
                        If CUShort(vals("time span")) > 0 Then
                            socket.SendPacket(W3Packet.MakePacket_TOCK())
                        End If
                    Case DL_MAP_CHUNK
                        Dim pos = CUInt(dl.file.Position)
                        If ReceiveDLMapChunk(vals) Then
                            Disconnect()
                        Else
                            socket.SendPacket(W3Packet.MakePacket_DL_RECEIVED_CHUNK(1, Me.index, pos))
                        End If
                End Select
            Catch e As Exception
                Dim msg = "(Ignored) Error handling packet of type {0} from {1}: {2}".frmt(id, name, e.Message)
                logger.log(msg, LogMessageTypes.Problem)
                Logging.logUnexpectedException(msg, e)
            End Try
        End Sub
#End Region

#Region "P2P Networking"
        Private Sub c_P2PConnection(ByVal sender As W3P2PConnectionAccepter, _
                                 ByVal accepted_player As W3P2PConnectingPlayer) Handles accepter.Connection
            ref.enqueue(Function() eval(AddressOf _c_P2PConnection, sender, accepted_player))
        End Sub
        Private Sub _c_P2PConnection(ByVal sender As W3P2PConnectionAccepter, _
                                 ByVal accepted_player As W3P2PConnectingPlayer)
            Dim player = (From p In other_players Where p.index = accepted_player.index).FirstOrDefault
            Dim socket = accepted_player.socket
            If player Is Nothing Then
                logger.log("{0} was not another player in the game.".frmt(socket.name), LogMessageTypes.NegativeEvent)
                socket.disconnect()
            Else
                logger.log("{0} is a p2p connection from {1}.".frmt(socket.name, player.name), LogMessageTypes.PositiveEvent)
                socket.name = player.name
                player.socket = socket
                socket.SendPacket(W3Packet.MakePacket_P2P_KNOCK(player.p2p_key, Me.index, 0))
                socket.set_reading(True)
            End If
        End Sub

        Private Sub c_P2PDisconnection(ByVal sender As W3P2PPlayer)
            ref.enqueue(Function() eval(AddressOf _c_P2PDisconnection, sender))
        End Sub
        Private Sub _c_P2PDisconnection(ByVal sender As W3P2PPlayer)
            logger.log("{0}'s p2p connection has closed.".frmt(sender.name), LogMessageTypes.NegativeEvent)
            sender.socket = Nothing
            SendPlayersConnected()
        End Sub

        Private Sub c_P2PReceivedPacket(ByVal sender As W3P2PPlayer, _
                                        ByVal id As W3PacketId, _
                                        ByVal vals As Dictionary(Of String, Object))
            ref.enqueue(Function() eval(AddressOf _c_P2PReceivedPacket, sender, id, vals))
        End Sub
        Private Sub _c_P2PReceivedPacket(ByVal sender As W3P2PPlayer, _
                                         ByVal id As W3PacketId, _
                                         ByVal vals As Dictionary(Of String, Object))
            Try
                Select Case id
                    Case P2P_PING
                        sender.socket.SendPacket(W3Packet.MakePacket_P2P_PING(CType(vals("salt"), Byte()), 1))
                        sender.socket.SendPacket(W3Packet.MakePacket_P2P_PONG(CType(vals("salt"), Byte())))
                    Case DL_MAP_CHUNK
                        Dim pos = CUInt(dl.file.Position)
                        If ReceiveDLMapChunk(vals) Then
                            Disconnect()
                        Else
                            sender.socket.SendPacket(W3Packet.MakePacket_DL_RECEIVED_CHUNK(sender.index, Me.index, pos))
                        End If
                End Select
            Catch e As Exception
                Dim msg = "(Ignored) Error handling packet of type {0} from {1}: {2}".frmt(id, name, e.Message)
                logger.log(msg, LogMessageTypes.Problem)
                Logging.logUnexpectedException(msg, e)
            End Try
        End Sub
#End Region
    End Class
End Namespace