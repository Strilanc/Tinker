Imports Tinker.Pickling

Namespace WC3
    Public Enum DummyPlayerMode
        DownloadMap
        EnterGame
    End Enum

    'verification disabled until this class can be looked at more closely
    <ContractVerification(False)>
    Public NotInheritable Class W3DummyPlayer
        Private ReadOnly name As String
        Private ReadOnly listenPort As UShort
        Private ReadOnly inQueue As ICallQueue
        Private ReadOnly otherPlayers As New List(Of W3Peer)
        Private ReadOnly logger As Logger
        Private WithEvents socket As W3Socket
        Private WithEvents accepter As New W3PeerConnectionAccepter(New SystemClock())
        Public readyDelay As TimeSpan = TimeSpan.Zero
        Private index As PlayerID
        Private dl As MapDownload
        Private poolPort As PortPool.PortHandle
        Private mode As DummyPlayerMode
        Private ReadOnly _playerHooks As New Dictionary(Of W3Peer, List(Of IDisposable))
        Private ReadOnly _packetHandler As Protocol.W3PacketHandler

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(otherPlayers IsNot Nothing)
            Contract.Invariant(_playerHooks IsNot Nothing)
            Contract.Invariant(_packetHandler IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal poolPort As PortPool.PortHandle,
                       Optional ByVal logger As Logger = Nothing,
                       Optional ByVal mode As DummyPlayerMode = DummyPlayerMode.DownloadMap)
            Me.New(name, poolPort.Port, logger, mode)
            Contract.Requires(poolPort IsNot Nothing)
            Me.poolPort = poolPort
        End Sub
        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal listenPort As UShort = 0,
                       Optional ByVal logger As Logger = Nothing,
                       Optional ByVal mode As DummyPlayerMode = DummyPlayerMode.DownloadMap)
            Me.name = name
            Me.mode = mode
            Me.listenPort = listenPort
            Me.inQueue = New TaskedCallQueue
            Me.logger = If(logger, New Logger)
            If listenPort <> 0 Then accepter.Accepter.OpenPort(listenPort)
        End Sub

#Region "Networking"
        Private Function AddPacketHandler(Of T)(ByVal packet As Protocol.Packets.Definition(Of T),
                                                ByVal handler As Func(Of IPickle(Of T), IFuture)) As IDisposable
            Contract.Requires(packet IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _packetHandler.AddHandler(packet.Id, Function(data) handler(packet.Jar.Parse(data)))
        End Function
        Private Function AddQueuedPacketHandler(Of T)(ByVal packet As Protocol.Packets.Definition(Of T),
                                                      ByVal handler As Action(Of IPickle(Of T))) As IDisposable
            Contract.Requires(packet IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return AddPacketHandler(packet, Function(pickle) inQueue.QueueAction(Sub() handler(pickle)))
        End Function

        Public Function QueueConnect(ByVal hostName As String, ByVal port As UShort) As IFuture
            Contract.Requires(hostName IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub()
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
                                                   logger:=Me.logger,
                                                   clock:=New SystemClock))

            AddQueuedPacketHandler(Protocol.Packets.Greet, AddressOf OnReceiveGreet)
            AddQueuedPacketHandler(Protocol.Packets.HostMapInfo, AddressOf OnReceiveHostMapInfo)
            AddQueuedPacketHandler(Protocol.Packets.Ping, AddressOf OnReceivePing)
            AddQueuedPacketHandler(Protocol.Packets.OtherPlayerJoined, AddressOf OnReceiveOtherPlayerJoined)
            AddQueuedPacketHandler(Protocol.Packets.OtherPlayerLeft, AddressOf OnReceiveOtherPlayerLeft)
            AddQueuedPacketHandler(Protocol.Packets.StartLoading, AddressOf OnReceiveStartLoading)
            AddQueuedPacketHandler(Protocol.Packets.Tick, AddressOf OnReceiveTick)
            AddQueuedPacketHandler(Protocol.Packets.MapFileData, AddressOf OnReceiveMapFileData)

            AsyncProduceConsumeUntilError(
                producer:=AddressOf socket.AsyncReadPacket,
                consumer:=AddressOf _packetHandler.HandlePacket,
                errorHandler:=Sub(exception)
                                  'ignore
                              End Sub
            )

            socket.SendPacket(Protocol.MakeKnock(name, listenPort, CUShort(socket.LocalEndPoint.Port)))
        End Sub
        Private Sub OnReceiveGreet(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            index = New PlayerID(CByte(pickle.Value("player index")))
        End Sub
        Private Sub OnReceiveHostMapInfo(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            If mode = DummyPlayerMode.DownloadMap Then
                dl = New MapDownload(CStr(pickle.Value("path")),
                                     CUInt(pickle.Value("size")),
                                     CUInt(pickle.Value("crc32")),
                                     CUInt(pickle.Value("xoro checksum")),
                                     CType(pickle.Value("sha1 checksum"), IReadableList(Of Byte)))
                socket.SendPacket(Protocol.MakeClientMapInfo(Protocol.MapTransferState.Idle, 0))
            Else
                socket.SendPacket(Protocol.MakeClientMapInfo(Protocol.MapTransferState.Idle, CUInt(pickle.Value("size"))))
            End If
        End Sub
        Private Sub OnReceivePing(ByVal pickle As IPickle(Of UInt32))
            socket.SendPacket(Protocol.MakePong(pickle.Value))
        End Sub
        Private Sub OnReceiveOtherPlayerJoined(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Dim ext_addr = CType(pickle.Value("external address"), Dictionary(Of InvariantString, Object))
            Dim player = New W3Peer(CStr(pickle.Value("name")),
                                    New PlayerID(CByte(pickle.Value("index"))),
                                    CUShort(ext_addr("port")),
                                    New Net.IPAddress(CType(ext_addr("ip"), Byte())),
                                    CUInt(pickle.Value("peer key")))
            otherPlayers.Add(player)
            Dim hooks = New List(Of IDisposable)
            hooks.Add(player.AddPacketHandler(Protocol.Packets.PeerPing, Function(value) inQueue.QueueAction(Sub() OnPeerReceivePeerPing(player, value))))
            hooks.Add(player.AddPacketHandler(Protocol.Packets.MapFileData, Function(value) inQueue.QueueAction(Sub() OnPeerReceiveMapFileData(player, value))))
            AddHandler player.Disconnected, AddressOf OnPeerDisconnect
            hooks.Add(New DelegatedDisposable(Sub() RemoveHandler player.Disconnected, AddressOf OnPeerDisconnect))
            _playerHooks(player) = hooks
        End Sub
        Private Sub OnReceiveOtherPlayerLeft(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Dim player = (From p In otherPlayers Where p.PID.Index = CByte(pickle.Value("player index"))).FirstOrDefault
            If player IsNot Nothing Then
                otherPlayers.Remove(player)
                For Each e In _playerHooks(player)
                    e.Dispose()
                Next e
                _playerHooks.Remove(player)
            End If
        End Sub
        Private Sub OnReceiveStartLoading(ByVal pickle As IPickle(Of Object))
            If mode = DummyPlayerMode.DownloadMap Then
                Disconnect(expected:=False, reason:="Dummy player is in download mode but game is starting.")
            ElseIf mode = DummyPlayerMode.EnterGame Then
                Call New SystemClock().AsyncWait(readyDelay).CallWhenReady(Sub() socket.SendPacket(Protocol.MakeReady()))
            End If
        End Sub
        Private Sub OnReceiveTick(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            If CUShort(pickle.Value("time span")) > 0 Then
                socket.SendPacket(Protocol.MakeTock(0, 0))
            End If
        End Sub
        Private Sub OnReceiveMapFileData(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Dim pos = CUInt(dl.file.Position)
            If ReceiveDLMapChunk(pickle.Value) Then
                Disconnect(expected:=True, reason:="Download finished.")
            Else
                socket.SendPacket(Protocol.MakeMapFileDataReceived(New PlayerID(1), Me.index, pos))
            End If
        End Sub

        Private Function ReceiveDLMapChunk(ByVal vals As Dictionary(Of InvariantString, Object)) As Boolean
            Contract.Requires(vals IsNot Nothing)
            If dl Is Nothing OrElse dl.file Is Nothing Then Throw New InvalidOperationException()
            Dim position = CInt(CUInt(vals("file position")))
            Dim fileData = CType(vals("file data"), IReadableList(Of Byte))
            Contract.Assume(position > 0)
            Contract.Assume(fileData IsNot Nothing)

            If dl.ReceiveChunk(position, fileData) Then
                socket.SendPacket(Protocol.MakeClientMapInfo(Protocol.MapTransferState.Idle, dl.size))
                Return True
            Else
                socket.SendPacket(Protocol.MakeClientMapInfo(Protocol.MapTransferState.Downloading, CUInt(dl.file.Position)))
                Return False
            End If
        End Function
        Private Sub SendPlayersConnected()
            socket.SendPacket(Protocol.MakePeerConnectionInfo(From p In otherPlayers Where p.Socket IsNot Nothing Select p.PID))
        End Sub

        Private Sub OnDisconnect(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reason As String) Handles socket.Disconnected
            inQueue.QueueAction(Sub()
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
                    For Each e In _playerHooks(player)
                        e.Dispose()
                    Next e
                    _playerHooks.Remove(player)
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
        Private Sub OnPeerConnection(ByVal sender As W3PeerConnectionAccepter,
                                     ByVal acceptedPlayer As W3ConnectingPeer) Handles accepter.Connection
            inQueue.QueueAction(
                Sub()
                    Dim player = (From p In otherPlayers Where p.PID = acceptedPlayer.pid).FirstOrDefault
                    Dim socket = acceptedPlayer.socket
                    If player Is Nothing Then
                        Dim msg = "{0} was not another player in the game.".Frmt(socket.Name)
                        logger.Log(msg, LogMessageType.Negative)
                        socket.Disconnect(expected:=True, reason:=msg)
                    Else
                        logger.Log("{0} is a peer connection from {1}.".Frmt(socket.Name, player.name), LogMessageType.Positive)
                        socket.Name = player.name
                        player.SetSocket(socket)
                        socket.SendPacket(Protocol.MakePeerKnock(player.peerKey, Me.index, {}))
                    End If
                End Sub
            )
        End Sub

        Private Sub OnPeerDisconnect(ByVal sender As W3Peer, ByVal expected As Boolean, ByVal reason As String)
            inQueue.QueueAction(
                Sub()
                    logger.Log("{0}'s peer connection has closed ({1}).".Frmt(sender.name, reason), LogMessageType.Negative)
                    sender.SetSocket(Nothing)
                    SendPlayersConnected()
                End Sub
            )
        End Sub

        Private Sub OnPeerReceivePeerPing(ByVal sender As W3Peer,
                                          ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(pickle IsNot Nothing)
            Dim vals = pickle.Value
            sender.Socket.SendPacket(Protocol.MakePeerPing(CUInt(vals("salt")), {New PlayerID(1)}))
            sender.Socket.SendPacket(Protocol.MakePeerPong(CUInt(vals("salt"))))
        End Sub
        Private Sub OnPeerReceiveMapFileData(ByVal sender As W3Peer,
                                             ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(pickle IsNot Nothing)
            Dim vals = pickle.Value
            Dim pos = CUInt(dl.file.Position)
            If ReceiveDLMapChunk(vals) Then
                Disconnect(expected:=True, reason:="Download finished.")
            Else
                sender.Socket.SendPacket(Protocol.MakeMapFileDataReceived(sender.PID, Me.index, pos))
            End If
        End Sub
#End Region
    End Class
End Namespace