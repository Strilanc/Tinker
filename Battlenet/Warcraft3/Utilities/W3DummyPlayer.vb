Imports HostBot.Warcraft3.W3PacketId
Imports HostBot.Warcraft3

Namespace Warcraft3
    Public Class W3DummyPlayer
        Private ReadOnly name As String
        Private ReadOnly listenPort As UShort
        Private ReadOnly ref As ICallQueue
        Private ReadOnly otherPlayers As New List(Of W3Peer)
        Private ReadOnly logger As Logger
        Private WithEvents socket As W3Socket
        Private WithEvents accepter As New W3PeerConnectionAccepter()
        Public readyDelay As TimeSpan = TimeSpan.Zero
        Private index As Byte
        Private dl As W3MapDownload
        Private poolPort As PortPool.PortHandle
        Public Enum Modes
            DownloadMap
            EnterGame
        End Enum
        Private mode As Modes

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
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
            Me.New(name, pool_port.Port, logger, mode)
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
            If listen_port <> 0 Then accepter.Accepter.OpenPort(listen_port)
        End Sub
#End Region

#Region "Networking"
        Public Function QueueConnect(ByVal hostname As String, ByVal port As UShort) As IFuture(Of Outcome)
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
                socket = New W3Socket(New PacketSocket(tcp, 60.Seconds, Me.logger))

                FutureIterate(AddressOf socket.FutureReadPacket, Function(result) ref.QueueFunc(
                    Function()
                        If result.Exception IsNot Nothing Then
                            Return False
                        End If

                        Dim id = result.Value.id
                        Dim vals = CType(result.Value.payload.Value, Dictionary(Of String, Object))
                        Contract.Assume(vals IsNot Nothing)
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
                                    Dim player = New W3Peer(CStr(vals("name")),
                                                            CByte(vals("index")),
                                                            CUShort(ext_addr("port")),
                                                            New Net.IPAddress(CType(ext_addr("ip"), Byte())),
                                                            CUInt(vals("peer key")))
                                    otherPlayers.Add(player)
                                    AddHandler player.ReceivedPacket, AddressOf c_PeerReceivedPacket
                                    AddHandler player.Disconnected, AddressOf c_PeerDisconnection
                                Case OtherPlayerLeft
                                    Dim player = (From p In otherPlayers Where p.index = CByte(vals("player index"))).FirstOrDefault
                                    If player IsNot Nothing Then
                                        otherPlayers.Remove(player)
                                        RemoveHandler player.ReceivedPacket, AddressOf c_PeerReceivedPacket
                                        RemoveHandler player.Disconnected, AddressOf c_PeerDisconnection
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
                            Dim msg = "(Ignored) Error handling packet of type {0} from {1}: {2}".frmt(id, name, e)
                            logger.log(msg, LogMessageType.Problem)
                            LogUnexpectedException(msg, e)
                        End Try

                        Return socket.connected
                    End Function
                ))

                socket.SendPacket(W3Packet.MakeKnock(name, listenPort, CUShort(socket.LocalEndPoint.Port)))
                Return success("Connection established.")
            Catch e As Exception
                Return failure(e.ToString)
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
            accepter.Accepter.CloseAllPorts()
            For Each player In otherPlayers
                If player.socket IsNot Nothing Then
                    player.socket.disconnect(reason)
                    player.SetSocket(Nothing)
                    RemoveHandler player.ReceivedPacket, AddressOf c_PeerReceivedPacket
                    RemoveHandler player.Disconnected, AddressOf c_PeerDisconnection
                End If
            Next player
            otherPlayers.Clear()
            If poolPort IsNot Nothing Then
                poolPort.Dispose()
                poolPort = Nothing
            End If
        End Sub
#End Region

#Region "Peer Networking"
        Private Sub c_PeerConnection(ByVal sender As W3PeerConnectionAccepter,
                                     ByVal acceptedPlayer As W3ConnectingPeer) Handles accepter.Connection
            ref.QueueAction(
                Sub()
                    Dim player = (From p In otherPlayers Where p.index = acceptedPlayer.index).FirstOrDefault
                    Dim socket = acceptedPlayer.socket
                    If player Is Nothing Then
                        Dim msg = "{0} was not another player in the game.".frmt(socket.Name)
                        logger.log(msg, LogMessageType.Negative)
                        socket.disconnect(msg)
                    Else
                        logger.log("{0} is a peer connection from {1}.".frmt(socket.Name, player.name), LogMessageType.Positive)
                        socket.Name = player.name
                        player.SetSocket(socket)
                        socket.SendPacket(W3Packet.MakePeerKnock(player.peerKey, Me.index, 0))
                    End If
                End Sub
            )
        End Sub

        Private Sub c_PeerDisconnection(ByVal sender As W3Peer, ByVal reason As String)
            ref.QueueAction(
                Sub()
                    logger.log("{0}'s peer connection has closed ({1}).".frmt(sender.name, reason), LogMessageType.Negative)
                    sender.SetSocket(Nothing)
                    SendPlayersConnected()
                End Sub
            )
        End Sub

        Private Sub c_PeerReceivedPacket(ByVal sender As W3Peer,
                                        ByVal id As W3PacketId,
                                        ByVal vals As Dictionary(Of String, Object))
            ref.QueueAction(
                Sub()
                    Try
                        Select Case id
                            Case PeerPing
                                sender.socket.SendPacket(W3Packet.MakePeerPing(CType(vals("salt"), Byte()), 1))
                                sender.socket.SendPacket(W3Packet.MakePeerPong(CType(vals("salt"), Byte())))
                            Case MapFileData
                                Dim pos = CUInt(dl.file.Position)
                                If ReceiveDLMapChunk(vals) Then
                                    Disconnect("Download finished.")
                                Else
                                    sender.socket.SendPacket(W3Packet.MakeMapFileDataReceived(sender.index, Me.index, pos))
                                End If
                        End Select
                    Catch e As Exception
                        Dim msg = "(Ignored) Error handling packet of type {0} from {1}: {2}".frmt(id, name, e)
                        logger.log(msg, LogMessageType.Problem)
                        LogUnexpectedException(msg, e)
                    End Try
                End Sub
            )
        End Sub
#End Region
    End Class
End Namespace