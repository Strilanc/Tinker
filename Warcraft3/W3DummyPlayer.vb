Imports Tinker.WC3.PacketId

Namespace WC3
    Public Enum DummyPlayerMode
        DownloadMap
        EnterGame
    End Enum

    Public NotInheritable Class W3DummyPlayer
        Private ReadOnly name As String
        Private ReadOnly listenPort As UShort
        Private ReadOnly ref As ICallQueue
        Private ReadOnly otherPlayers As New List(Of W3Peer)
        Private ReadOnly logger As Logger
        Private WithEvents socket As W3Socket
        Private WithEvents accepter As New W3PeerConnectionAccepter()
        Public readyDelay As TimeSpan = TimeSpan.Zero
        Private index As Byte
        Private dl As MapDownload
        Private poolPort As PortPool.PortHandle
        Private mode As DummyPlayerMode

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(index > 0)
            Contract.Invariant(index <= 12)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(otherPlayers IsNot Nothing)
        End Sub
#Region "Life"
        Public Sub New(ByVal name As InvariantString,
                       ByVal poolPort As PortPool.PortHandle,
                       Optional ByVal logger As Logger = Nothing,
                       Optional ByVal mode As DummyPlayerMode = DummyPlayerMode.DownloadMap)
            Me.New(name, poolPort.Port, logger, mode)
            Me.poolPort = poolPort
        End Sub
        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal listenPort As UShort = 0,
                       Optional ByVal logger As Logger = Nothing,
                       Optional ByVal mode As DummyPlayerMode = DummyPlayerMode.DownloadMap)
            Me.name = name
            Me.mode = mode
            Me.listenPort = listenPort
            Me.ref = New TaskedCallQueue
            Me.logger = If(logger, New Logger)
            If listenPort <> 0 Then accepter.Accepter.OpenPort(listenPort)
        End Sub
#End Region

#Region "Networking"
        Public Function QueueConnect(ByVal hostName As String, ByVal port As UShort) As IFuture
            Contract.Requires(hostName IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(hostName IsNot Nothing)
                                       Connect(hostName, port)
                                   End Sub)
        End Function
        Private Sub Connect(ByVal hostName As String, ByVal port As UShort)
            Contract.Requires(hostName IsNot Nothing)

            Dim tcp = New Net.Sockets.TcpClient()
            tcp.Connect(hostName, port)
            socket = New W3Socket(New PacketSocket(stream:=tcp.GetStream,
                                                   localendpoint:=CType(tcp.Client.LocalEndPoint, Net.IPEndPoint),
                                                   remoteendpoint:=CType(tcp.Client.RemoteEndPoint, Net.IPEndPoint),
                                                   timeout:=60.Seconds,
                                                   logger:=Me.logger))

            AsyncProduceConsumeUntilError(
                producer:=AddressOf socket.AsyncReadPacket,
                consumer:=Function(packetData) ref.QueueAction(
                    Sub()
                        Dim packet = WC3.Packet.FromData(CType(packetData(1), PacketId), packetData.SubView(4))
                        Dim id = packet.id
                        Dim vals = CType(packet.Payload.Value, Dictionary(Of InvariantString, Object))
                        Contract.Assume(vals IsNot Nothing)
                        Try
                            Select Case id
                                Case Greet
                                    index = CByte(vals("player index"))
                                Case HostMapInfo
                                    If mode = DummyPlayerMode.DownloadMap Then
                                        dl = New MapDownload(CStr(vals("path")),
                                                               CUInt(vals("size")),
                                                               CUInt(vals("crc32")),
                                                               CUInt(vals("xoro checksum")),
                                                               CType(vals("sha1 checksum"), ViewableList(Of Byte)))
                                        socket.SendPacket(packet.MakeClientMapInfo(packet.DownloadState.NotDownloading, 0))
                                    Else
                                        socket.SendPacket(packet.MakeClientMapInfo(packet.DownloadState.NotDownloading, CUInt(vals("size"))))
                                    End If
                                Case Ping
                                    socket.SendPacket(packet.MakePong(CUInt(vals("salt"))))
                                Case OtherPlayerJoined
                                    Dim ext_addr = CType(vals("external address"), Dictionary(Of InvariantString, Object))
                                    Dim player = New W3Peer(CStr(vals("name")),
                                                            CByte(vals("index")),
                                                            CUShort(ext_addr("port")),
                                                            New Net.IPAddress(CType(ext_addr("ip"), Byte())),
                                                            CUInt(vals("peer key")))
                                    otherPlayers.Add(player)
                                    AddHandler player.ReceivedPacket, AddressOf OnPeerReceivePacket
                                    AddHandler player.Disconnected, AddressOf OnPeerDisconnect
                                Case OtherPlayerLeft
                                    Dim player = (From p In otherPlayers Where p.Index = CByte(vals("player index"))).FirstOrDefault
                                    If player IsNot Nothing Then
                                        otherPlayers.Remove(player)
                                        RemoveHandler player.ReceivedPacket, AddressOf OnPeerReceivePacket
                                        RemoveHandler player.Disconnected, AddressOf OnPeerDisconnect
                                    End If
                                Case StartLoading
                                    If mode = DummyPlayerMode.DownloadMap Then
                                        Disconnect(expected:=False, reason:="Dummy player is in download mode but game is starting.")
                                    ElseIf mode = DummyPlayerMode.EnterGame Then
                                        readyDelay.AsyncWait().CallWhenReady(Sub() socket.SendPacket(packet.MakeReady()))
                                    End If
                                Case Tick
                                    If CUShort(vals("time span")) > 0 Then
                                        socket.SendPacket(packet.MakeTock())
                                    End If
                                Case MapFileData
                                    Dim pos = CUInt(dl.file.Position)
                                    If ReceiveDLMapChunk(vals) Then
                                        Disconnect(expected:=True, reason:="Download finished.")
                                    Else
                                        socket.SendPacket(packet.MakeMapFileDataReceived(1, Me.index, pos))
                                    End If
                            End Select
                        Catch e As Exception
                            Dim msg = "(Ignored) Error handling packet of type {0} from {1}: {2}".Frmt(id, name, e)
                            logger.Log(msg, LogMessageType.Problem)
                            e.RaiseAsUnexpected(msg)
                        End Try
                    End Sub),
                errorHandler:=Sub(exception)
                                  'ignore
                              End Sub
            )

            socket.SendPacket(Packet.MakeKnock(name, listenPort, CUShort(socket.LocalEndPoint.Port)))
        End Sub

        Private Function ReceiveDLMapChunk(ByVal vals As Dictionary(Of InvariantString, Object)) As Boolean
            If dl Is Nothing OrElse dl.file Is Nothing Then Throw New InvalidOperationException()
            Dim position = CInt(CUInt(vals("file position")))
            Dim fileData = CType(vals("file data"), Byte())
            Contract.Assume(position > 0)
            Contract.Assume(fileData IsNot Nothing)

            If dl.ReceiveChunk(position, fileData) Then
                socket.SendPacket(Packet.MakeClientMapInfo(Packet.DownloadState.NotDownloading, dl.size))
                Return True
            Else
                socket.SendPacket(Packet.MakeClientMapInfo(Packet.DownloadState.Downloading, CUInt(dl.file.Position)))
                Return False
            End If
        End Function
        Private Sub SendPlayersConnected()
            socket.SendPacket(Packet.MakePeerConnectionInfo(From p In otherPlayers Where p.Socket IsNot Nothing Select p.Index))
        End Sub

        Private Sub c_Disconnect(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reason As String) Handles socket.Disconnected
            ref.QueueAction(Sub()
                                Contract.Assume(reason IsNot Nothing)
                                Disconnect(expected, reason)
                            End Sub)
        End Sub
        Private Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            socket.Disconnect(expected, reason)
            accepter.Accepter.CloseAllPorts()
            For Each player In otherPlayers
                If player.Socket IsNot Nothing Then
                    player.Socket.Disconnect(expected, reason)
                    player.SetSocket(Nothing)
                    RemoveHandler player.ReceivedPacket, AddressOf OnPeerReceivePacket
                    RemoveHandler player.Disconnected, AddressOf OnPeerDisconnect
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
                    Dim player = (From p In otherPlayers Where p.Index = acceptedPlayer.index).FirstOrDefault
                    Dim socket = acceptedPlayer.socket
                    If player Is Nothing Then
                        Dim msg = "{0} was not another player in the game.".Frmt(socket.Name)
                        logger.Log(msg, LogMessageType.Negative)
                        socket.Disconnect(expected:=True, reason:=msg)
                    Else
                        logger.Log("{0} is a peer connection from {1}.".Frmt(socket.Name, player.name), LogMessageType.Positive)
                        socket.Name = player.name
                        player.SetSocket(socket)
                        socket.SendPacket(Packet.MakePeerKnock(player.peerKey, Me.index, 0))
                    End If
                End Sub
            )
        End Sub

        Private Sub OnPeerDisconnect(ByVal sender As W3Peer, ByVal expected As Boolean, ByVal reason As String)
            ref.QueueAction(
                Sub()
                    logger.Log("{0}'s peer connection has closed ({1}).".Frmt(sender.name, reason), LogMessageType.Negative)
                    sender.SetSocket(Nothing)
                    SendPlayersConnected()
                End Sub
            )
        End Sub

        Private Sub OnPeerReceivePacket(ByVal sender As W3Peer,
                                        ByVal packet As Packet)
            ref.QueueAction(
                Sub()
                    Try
                        Select Case packet.id
                            Case PeerPing
                                Dim vals = CType(packet.Payload.Value, Dictionary(Of InvariantString, Object))
                                sender.Socket.SendPacket(packet.MakePeerPing(CUInt(vals("salt")), 1))
                                sender.Socket.SendPacket(packet.MakePeerPong(CUInt(vals("salt"))))
                            Case MapFileData
                                Dim vals = CType(packet.Payload.Value, Dictionary(Of InvariantString, Object))
                                Dim pos = CUInt(dl.file.Position)
                                If ReceiveDLMapChunk(vals) Then
                                    Disconnect(expected:=True, reason:="Download finished.")
                                Else
                                    sender.Socket.SendPacket(packet.MakeMapFileDataReceived(sender.Index, Me.index, pos))
                                End If
                        End Select
                    Catch e As Exception
                        Dim msg = "(Ignored) Error handling packet of type {0} from {1}: {2}".Frmt(packet.id, name, e)
                        logger.Log(msg, LogMessageType.Problem)
                        e.RaiseAsUnexpected(msg)
                    End Try
                End Sub
            )
        End Sub
#End Region
    End Class
End Namespace