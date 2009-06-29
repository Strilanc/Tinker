Imports HostBot.Warcraft3.W3PacketId
Imports HostBot.Warcraft3

Namespace Warcraft3
    Public Class W3DummyPlayer
        Private ReadOnly name As String
        Private ReadOnly listenPort As UShort
        Private ReadOnly ref As ICallQueue
        Private ReadOnly otherPlayers As New List(Of W3P2PPlayer)
        Private ReadOnly logger As Logger
        Private WithEvents socket As W3Socket
        Private WithEvents accepter As New W3P2PConnectionAccepter()
        Public readyDelay As TimeSpan = TimeSpan.Zero
        Private index As Byte
        Private dl As W3MapDownload
        Private poolPort As PortPool.PortHandle
        Public Enum Modes
            DownloadMap
            EnterGame
        End Enum
        Private mode As Modes

        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(otherPlayers IsNot Nothing)
        End Sub
#Region "Life"
        Public Sub New(ByVal name As String,
                       ByVal pool_port As PortPool.PortHandle,
                       Optional ByVal logger As Logger = Nothing,
                       Optional ByVal mode As Modes = Modes.DownloadMap)
            Me.New(name, pool_port.port, logger, mode)
            Me.poolPort = pool_port
        End Sub
        Public Sub New(ByVal name As String,
                       Optional ByVal listen_port As UShort = 0,
                       Optional ByVal logger As Logger = Nothing,
                       Optional ByVal mode As Modes = Modes.DownloadMap)
            Me.name = name
            Me.mode = mode
            Me.listenPort = listen_port
            Me.ref = New ThreadPooledCallQueue
            Me.logger = If(logger, New Logger)
            If listen_port <> 0 Then accepter.accepter.OpenPort(listen_port)
        End Sub
#End Region

#Region "Networking"
        Public Function f_Connect(ByVal hostname As String, ByVal port As UShort) As IFuture(Of Outcome)
            Contract.Requires(hostname IsNot Nothing)
            Dim hostname_ = hostname
            Return ref.QueueFunc(Function()
                                     Contract.Assume(hostname_ IsNot Nothing)
                                     Return Connect(hostname_, port)
                                 End Function)
        End Function
        Private Function Connect(ByVal hostname As String, ByVal port As UShort) As Outcome
            Contract.Requires(hostname IsNot Nothing)

            Try
                Dim tcp = New Net.Sockets.TcpClient()
                tcp.Connect(hostname, port)
                socket = New W3Socket(New BnetSocket(tcp, New TimeSpan(0, 1, 0), Me.logger))
                socket.SetReading(True)
                socket.SendPacket(W3Packet.MakeKnock(name, listenPort, CUShort(socket.getLocalPort)))
                Return success("Connection established.")
            Catch e As Exception
                Return failure(e.Message)
            End Try
        End Function

        Private Function ReceiveDLMapChunk(ByVal vals As Dictionary(Of String, Object)) As Boolean
            If dl Is Nothing OrElse dl.file Is Nothing Then Throw New InvalidOperationException()

            If dl.ReceiveChunk(CInt(vals("file position")), CType(vals("file data"), Byte())) Then
                socket.SendPacket(W3Packet.MakeClientMapInfo(W3Packet.DownloadState.NotDownloading, dl.size))
                Return True
            Else
                socket.SendPacket(W3Packet.MakeClientMapInfo(W3Packet.DownloadState.Downloading, CUInt(dl.file.Position)))
                Return False
            End If
        End Function
        Private Sub SendPlayersConnected()
            socket.SendPacket(W3Packet.MakePeerConnectionInfo(From p In otherPlayers Where p.socket IsNot Nothing Select p.index))
        End Sub

        Private Sub c_Disconnect(ByVal sender As W3Socket, ByVal reason As String) Handles socket.Disconnected
            ref.QueueAction(Sub() Disconnect(reason))
        End Sub
        Private Sub Disconnect(ByVal reason As String)
            socket.disconnect(reason)
            accepter.accepter.CloseAllPorts()
            For Each player In otherPlayers
                If player.socket IsNot Nothing Then
                    player.socket.disconnect(reason)
                    player.socket = Nothing
                    RemoveHandler player.ReceivedPacket, AddressOf c_P2PReceivedPacket
                    RemoveHandler player.Disconnected, AddressOf c_P2PDisconnection
                End If
            Next player
            otherPlayers.Clear()
            If poolPort IsNot Nothing Then
                poolPort.Dispose()
                poolPort = Nothing
            End If
        End Sub

        Private Sub c_ReceivedPacket(ByVal sender As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object)) Handles socket.ReceivedPacket
            ref.QueueAction(
                Sub()
                    Try
                        Select Case id
                            Case Greet
                                index = CByte(vals("player index"))
                            Case HostMapInfo
                                If mode = Modes.DownloadMap Then
                                    dl = New W3MapDownload(CStr(vals("path")), CUInt(vals("size")), CType(vals("crc32"), Byte()), CType(vals("xoro checksum"), Byte()), CType(vals("sha1 checksum"), Byte()))
                                    socket.SendPacket(W3Packet.MakeClientMapInfo(W3Packet.DownloadState.NotDownloading, 0))
                                Else
                                    socket.SendPacket(W3Packet.MakeClientMapInfo(W3Packet.DownloadState.NotDownloading, CUInt(vals("size"))))
                                End If
                            Case Ping
                                socket.SendPacket(W3Packet.MakePong(CUInt(vals("salt"))))
                            Case OtherPlayerJoined
                                Dim ext_addr = CType(vals("external address"), Dictionary(Of String, Object))
                                Dim player = New W3P2PPlayer(CStr(vals("name")),
                                                             CByte(vals("index")),
                                                             CUShort(ext_addr("port")),
                                                             CType(ext_addr("ip"), Byte()),
                                                             CUInt(vals("peer key")))
                                otherPlayers.Add(player)
                                AddHandler player.ReceivedPacket, AddressOf c_P2PReceivedPacket
                                AddHandler player.Disconnected, AddressOf c_P2PDisconnection
                            Case OtherPlayerLeft
                                Dim player = (From p In otherPlayers Where p.index = CByte(vals("player index"))).FirstOrDefault
                                If player IsNot Nothing Then
                                    otherPlayers.Remove(player)
                                    RemoveHandler player.ReceivedPacket, AddressOf c_P2PReceivedPacket
                                    RemoveHandler player.Disconnected, AddressOf c_P2PDisconnection
                                End If
                            Case StartLoading
                                If mode = Modes.DownloadMap Then
                                    Disconnect("Dummy player is in download mode but game is starting.")
                                ElseIf mode = Modes.EnterGame Then
                                    FutureWait(readyDelay).CallWhenReady(Function() socket.SendPacket(W3Packet.MakeReady()))
                                End If
                            Case Tick
                                If CUShort(vals("time span")) > 0 Then
                                    socket.SendPacket(W3Packet.MakeTock())
                                End If
                            Case MapFileData
                                Dim pos = CUInt(dl.file.Position)
                                If ReceiveDLMapChunk(vals) Then
                                    Disconnect("Download finished.")
                                Else
                                    socket.SendPacket(W3Packet.MakeMapFileDataReceived(1, Me.index, pos))
                                End If
                        End Select
                    Catch e As Exception
                        Dim msg = "(Ignored) Error handling packet of type {0} from {1}: {2}".frmt(id, name, e.Message)
                        logger.log(msg, LogMessageTypes.Problem)
                        Logging.LogUnexpectedException(msg, e)
                    End Try
                End Sub
            )
        End Sub
#End Region

#Region "P2P Networking"
        Private Sub c_P2PConnection(ByVal sender As W3P2PConnectionAccepter,
                                    ByVal accepted_player As W3P2PConnectingPlayer) Handles accepter.Connection
            ref.QueueAction(
                Sub()
                    Dim player = (From p In otherPlayers Where p.index = accepted_player.index).FirstOrDefault
                    Dim socket = accepted_player.socket
                    If player Is Nothing Then
                        Dim msg = "{0} was not another player in the game.".frmt(socket.name)
                        logger.log(msg, LogMessageTypes.Negative)
                        socket.disconnect(msg)
                    Else
                        logger.log("{0} is a p2p connection from {1}.".frmt(socket.name, player.name), LogMessageTypes.Positive)
                        socket.name = player.name
                        player.socket = socket
                        socket.SendPacket(W3Packet.MakeP2pKnock(player.p2pKey, Me.index, 0))
                        socket.SetReading(True)
                    End If
                End Sub
            )
        End Sub

        Private Sub c_P2PDisconnection(ByVal sender As W3P2PPlayer, ByVal reason As String)
            ref.QueueAction(
                Sub()
                    logger.log("{0}'s p2p connection has closed ({1}).".frmt(sender.name, reason), LogMessageTypes.Negative)
                    sender.socket = Nothing
                    SendPlayersConnected()
                End Sub
            )
        End Sub

        Private Sub c_P2PReceivedPacket(ByVal sender As W3P2PPlayer,
                                        ByVal id As W3PacketId,
                                        ByVal vals As Dictionary(Of String, Object))
            ref.QueueAction(
                Sub()
                    Try
                        Select Case id
                            Case P2pPing
                                sender.socket.SendPacket(W3Packet.MakeP2pPing(CType(vals("salt"), Byte()), 1))
                                sender.socket.SendPacket(W3Packet.MakeP2pPong(CType(vals("salt"), Byte())))
                            Case MapFileData
                                Dim pos = CUInt(dl.file.Position)
                                If ReceiveDLMapChunk(vals) Then
                                    Disconnect("Download finished.")
                                Else
                                    sender.socket.SendPacket(W3Packet.MakeMapFileDataReceived(sender.index, Me.index, pos))
                                End If
                        End Select
                    Catch e As Exception
                        Dim msg = "(Ignored) Error handling packet of type {0} from {1}: {2}".frmt(id, name, e.Message)
                        logger.log(msg, LogMessageTypes.Problem)
                        Logging.LogUnexpectedException(msg, e)
                    End Try
                End Sub
            )
        End Sub
#End Region
    End Class
End Namespace