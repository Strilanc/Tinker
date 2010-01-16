Imports Tinker.Pickling

Namespace WC3.Protocol
    Public Module Packers
        <Pure()>
        Public Function MakeShowLagScreen(ByVal laggers As IEnumerable(Of Player)) As Packet
            Contract.Requires(laggers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.ShowLagScreen, New Dictionary(Of InvariantString, Object) From {
                    {"laggers", (From p In laggers
                                 Select New Dictionary(Of InvariantString, Object) From {
                                        {"player index", p.Index},
                                        {"initial milliseconds used", 2000}}).ToList()}})
        End Function
        <Pure()>
        Public Function MakeRemovePlayerFromLagScreen(ByVal player As Player,
                                                      ByVal lagTimeInMilliseconds As UInteger) As Packet
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.RemovePlayerFromLagScreen, New Dictionary(Of InvariantString, Object) From {
                    {"player index", player.Index},
                    {"marginal milliseconds used", lagTimeInMilliseconds}})
        End Function
        <Pure()>
        Public Function MakeText(ByVal text As String,
                                 ByVal chatType As ChatType,
                                 ByVal receiverType As ChatReceiverType,
                                 ByVal receivingPlayers As IEnumerable(Of Player),
                                 ByVal sender As Player) As Packet
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(receivingPlayers IsNot Nothing)
            Contract.Requires(sender IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Select Case chatType
                Case chatType.Game
                    Return New Packet(PacketId.Text, New Dictionary(Of InvariantString, Object) From {
                            {"receiving player indexes", (From p In receivingPlayers Select p.Index).ToList},
                            {"sending player index", sender.Index},
                            {"type", chatType},
                            {"message", text},
                            {"receiver type", receiverType}})
                Case chatType.Lobby
                    Return New Packet(PacketId.Text, New Dictionary(Of InvariantString, Object) From {
                            {"receiving player indexes", (From p In receivingPlayers Select p.Index).ToList},
                            {"sending player index", sender.Index},
                            {"type", chatType},
                            {"message", text}})
                Case Else
                    Throw chatType.MakeArgumentValueException("chatType")
            End Select
        End Function
        <Pure()>
        Public Function MakeGreet(ByVal player As Player,
                                  ByVal assignedIndex As Byte) As Packet
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.Greet, New Dictionary(Of InvariantString, Object) From {
                    {"slot data", New Byte() {}.AsReadableList},
                    {"player index", assignedIndex},
                    {"external address", player.RemoteEndPoint()}})
        End Function
        <Pure()>
        Public Function MakeReject(ByVal reason As RejectReason) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.RejectEntry, New Dictionary(Of InvariantString, Object) From {
                    {"reason", reason}})
        End Function
        <Pure()>
        Public Function MakeHostMapInfo(ByVal map As Map) As Packet
            Contract.Requires(map IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.HostMapInfo, New Dictionary(Of InvariantString, Object) From {
                    {"unknown", 1},
                    {"path", map.AdvertisedPath.ToString},
                    {"size", map.FileSize},
                    {"crc32", map.FileChecksumCRC32},
                    {"xoro checksum", map.MapChecksumXORO},
                    {"sha1 checksum", map.MapChecksumSHA1}})
        End Function
        <Pure()>
        Public Function MakeOtherPlayerJoined(ByVal stranger As Player,
                                              Optional ByVal overrideIndex As Byte = 0) As Packet
            Contract.Requires(stranger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim address = New Net.IPEndPoint(stranger.RemoteEndPoint.Address, stranger.listenPort)
            Return New Packet(PacketId.OtherPlayerJoined, New Dictionary(Of InvariantString, Object) From {
                    {"peer key", stranger.peerKey},
                    {"index", If(overrideIndex <> 0, overrideIndex, stranger.Index)},
                    {"name", stranger.Name.ToString},
                    {"unknown data", New Byte() {0}.AsReadableList},
                    {"external address", address},
                    {"internal address", address}})
        End Function
        <Pure()>
        Public Function MakePing(ByVal salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.Ping, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt}})
        End Function

        <Pure()>
        Public Function MakeOtherPlayerReady(ByVal player As Player) As Packet
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.OtherPlayerReady, New Dictionary(Of InvariantString, Object) From {
                    {"player index", player.Index}})
        End Function
        <Pure()>
        Public Function MakeOtherPlayerLeft(ByVal player As Player,
                                            ByVal leaveType As PlayerLeaveType) As Packet
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.OtherPlayerLeft, New Dictionary(Of InvariantString, Object) From {
                                {"player index", player.Index},
                                {"leave type", CByte(leaveType)}})
        End Function
        <Pure()>
        Public Function MakeLobbyState(ByVal receiver As Player,
                                       ByVal map As Map,
                                       ByVal slots As List(Of Slot),
                                       ByVal time As ModInt32,
                                       Optional ByVal hideSlots As Boolean = False) As Packet
            Contract.Requires(receiver IsNot Nothing)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(slots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LobbyState, New Dictionary(Of InvariantString, Object) From {
                    {"state size", CUShort(slots.Count() * 9 + 7)},
                    {"slots", (From slot In slots Select SlotJar.PackSlot(slot, receiver)).ToList()},
                    {"time", CUInt(time)},
                    {"layout style", If(map.IsMelee, 0, 3)},
                    {"num player slots", If(Not hideSlots, map.NumPlayerSlots, If(map.NumPlayerSlots = 12, 11, 12))}})
        End Function
        <Pure()>
        Public Function MakeStartCountdown() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.StartCountdown, New Dictionary(Of InvariantString, Object))
        End Function
        <Pure()>
        Public Function MakeStartLoading() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.StartLoading, New Dictionary(Of InvariantString, Object))
        End Function
        <Pure()>
        Public Function MakeHostConfirmHostLeaving() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.HostConfirmHostLeaving, New Dictionary(Of InvariantString, Object))
        End Function
        <Pure()>
        Public Function MakeTick(Optional ByVal delta As UShort = 250,
                                 Optional ByVal tickData As Byte() = Nothing) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            tickData = If(tickData, {})
            If tickData.Length > 0 Then
                tickData = Concat(tickData.CRC32.Bytes.SubArray(0, 2), tickData)
            End If

            Return New Packet(PacketId.Tick, New Dictionary(Of InvariantString, Object) From {
                    {"subpacket", tickData.AsReadableList},
                    {"time span", delta}})
        End Function

        <Pure()>
        Public Function MakeMapFileData(ByVal map As Map,
                                        ByVal receiverIndex As Byte,
                                        ByVal filePosition As Integer,
                                        ByRef refSizeDataSent As Integer,
                                        Optional ByVal senderIndex As Byte = 0) As Packet
            Contract.Requires(senderIndex >= 0)
            Contract.Requires(senderIndex <= 12)
            Contract.Requires(receiverIndex > 0)
            Contract.Requires(receiverIndex <= 12)
            Contract.Requires(filePosition >= 0)
            Contract.Requires(map IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim filedata = map.ReadChunk(filePosition)
            refSizeDataSent = 0
            If senderIndex = 0 Then senderIndex = If(receiverIndex = 1, CByte(2), CByte(1))

            refSizeDataSent = filedata.Count
            Return New Packet(PacketId.MapFileData, New Dictionary(Of InvariantString, Object) From {
                    {"receiving player index", receiverIndex},
                    {"sending player index", senderIndex},
                    {"unknown", 1},
                    {"file position", filePosition},
                    {"crc32", filedata.CRC32},
                    {"file data", filedata}})
        End Function
        <Pure()>
        Public Function MakeSetUploadTarget(ByVal receiverIndex As Byte,
                                            ByVal filePosition As UInteger) As Packet
            Contract.Requires(receiverIndex > 0)
            Contract.Requires(receiverIndex <= 12)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.SetUploadTarget, New Dictionary(Of InvariantString, Object) From {
                    {"unknown1", 1},
                    {"receiving player index", receiverIndex},
                    {"starting file pos", filePosition}})
        End Function
        <Pure()>
        Public Function MakeSetDownloadSource(ByVal senderIndex As Byte) As Packet
            Contract.Requires(senderIndex > 0)
            Contract.Requires(senderIndex <= 12)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.SetDownloadSource, New Dictionary(Of InvariantString, Object) From {
                    {"unknown", 1},
                    {"sending player index", senderIndex}})
        End Function
        <Pure()>
        Public Function MakeClientMapInfo(ByVal state As DownloadState,
                                          ByVal totalDownloaded As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.ClientMapInfo, New Dictionary(Of InvariantString, Object) From {
                    {"unknown", 1},
                    {"dl state", state},
                    {"total downloaded", totalDownloaded}})
        End Function
        <Pure()>
        Public Function MakeMapFileDataReceived(ByVal senderIndex As Byte,
                                                ByVal receiverIndex As Byte,
                                                ByVal totalDownloaded As UInteger) As Packet
            Contract.Requires(senderIndex > 0)
            Contract.Requires(senderIndex <= 12)
            Contract.Requires(receiverIndex > 0)
            Contract.Requires(receiverIndex <= 12)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.MapFileDataReceived, New Dictionary(Of InvariantString, Object) From {
                    {"sender index", senderIndex},
                    {"receiver index", receiverIndex},
                    {"unknown", 1},
                    {"total downloaded", totalDownloaded}})
        End Function

        <Pure()>
        Public Function MakeLanCreateGame(ByVal wc3MajorVersion As UInteger,
                                          ByVal gameId As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LanCreateGame, New Dictionary(Of InvariantString, Object) From {
                    {"product id", "W3XP"},
                    {"major version", wc3MajorVersion},
                    {"game id", gameId}})
        End Function
        <Pure()>
        Public Function MakeLanRefreshGame(ByVal gameId As UInteger,
                                           ByVal game As GameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LanRefreshGame, New Dictionary(Of InvariantString, Object) From {
                    {"game id", gameId},
                    {"num players", 0},
                    {"free slots", game.TotalSlotCount - game.UsedSlotCount}})
        End Function
        <Pure()>
        Public Function MakeLanDescribeGame(ByVal majorVersion As UInteger,
                                            ByVal game As LocalGameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LanDescribeGame, New Dictionary(Of InvariantString, Object) From {
                    {"product id", "W3XP"},
                    {"major version", majorVersion},
                    {"game id", game.GameId},
                    {"entry key", 2642024974UI},
                    {"name", game.Name.ToString},
                    {"password", ""},
                    {"statstring", game.GameStats},
                    {"num slots", game.TotalSlotCount()},
                    {"game type", game.GameType},
                    {"num players + 1", 1},
                    {"free slots + 1", game.TotalSlotCount + 1 - game.UsedSlotCount},
                    {"age", CUInt(game.Age.TotalMilliseconds)},
                    {"listen port", game.Port}})
        End Function
        <Pure()>
        Public Function MakeLanDestroyGame(ByVal gameId As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LanDestroyGame, New Dictionary(Of InvariantString, Object) From {
                    {"game id", gameId}})
        End Function

        <Pure()>
        Public Function MakeKnock(ByVal name As InvariantString,
                                  ByVal listenPort As UShort,
                                  ByVal sendingPort As UShort,
                                  Optional ByVal gameId As UInt32 = 0,
                                  Optional ByVal entryKey As UInt32 = 0,
                                  Optional ByVal peerKey As UInt32 = 0,
                                  Optional ByVal internalAddress As Net.IPAddress = Nothing) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            If internalAddress Is Nothing Then
                internalAddress = New Net.IPAddress(GetCachedIPAddressBytes(external:=True))
            End If
            Return New Packet(PacketId.Knock, New Dictionary(Of InvariantString, Object) From {
                    {"game id", gameId},
                    {"entry key", entryKey},
                    {"unknown value", 0},
                    {"listen port", listenPort},
                    {"peer key", peerKey},
                    {"name", name.ToString},
                    {"unknown data", New Byte() {0}.AsReadableList},
                    {"internal address", New Net.IPEndPoint(internalAddress, sendingPort)}})
        End Function
        <Pure()>
        Public Function MakeReady() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.Ready, New Dictionary(Of InvariantString, Object))
        End Function
        <Pure()>
        Public Function MakePong(ByVal salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.Pong, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt}})
        End Function
        <Pure()>
        Public Function MakeTock(Optional ByVal checksum As IReadableList(Of Byte) = Nothing) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            If checksum Is Nothing Then checksum = New Byte() {0, 0, 0, 0, 0}.AsReadableList
            If checksum.Count <> 5 Then Throw New ArgumentException("Checksum length must be 5.")
            Return New Packet(PacketId.Tock, New Dictionary(Of InvariantString, Object) From {
                    {"game state checksum", checksum}})
        End Function
        <Pure()>
        Public Function MakePeerConnectionInfo(ByVal indexes As IEnumerable(Of Byte)) As Packet
            Contract.Requires(indexes IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim bitFlags = From index In indexes Select CUShort(1) << (index - 1)
            Dim dword = bitFlags.ReduceUsing(Function(flag1, flag2) flag1 Or flag2)

            Return New Packet(PacketId.PeerConnectionInfo, New Dictionary(Of InvariantString, Object) From {
                    {"player bitflags", dword}})
        End Function

        <Pure()>
        Public Function MakePeerKnock(ByVal receiverPeerKey As UInteger,
                                      ByVal senderId As Byte,
                                      ByVal senderPeerConnectionFlags As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.PeerKnock, New Dictionary(Of InvariantString, Object) From {
                    {"receiver peer key", receiverPeerKey},
                    {"unknown1", 0},
                    {"sender player id", senderId},
                    {"unknown3", &HFF},
                    {"sender peer connection flags", senderPeerConnectionFlags}})
        End Function
        <Pure()>
        Public Function MakePeerPing(ByVal salt As UInt32,
                                     ByVal senderFlags As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.PeerPing, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt},
                    {"sender peer connection flags", senderFlags},
                    {"unknown2", 0}})
        End Function
        <Pure()>
        Public Function MakePeerPong(ByVal salt As UInt32) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.PeerPong, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt}})
        End Function
    End Module
End Namespace
