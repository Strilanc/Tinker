Imports Tinker.Pickling

Namespace WC3.Protocol
    Public Module Packers
        <Pure()>
        Public Function MakeShowLagScreen(ByVal laggers As IEnumerable(Of PID)) As Packet
            Contract.Requires(laggers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.ShowLagScreen, (From p In laggers
                                                            Select New Dictionary(Of InvariantString, Object) From {
                                                                    {"player index", p.Index},
                                                                    {"initial milliseconds used", 2000}}).ToList)
        End Function
        <Pure()>
        Public Function MakeRemovePlayerFromLagScreen(ByVal pid As PID,
                                                      ByVal lagTimeInMilliseconds As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.RemovePlayerFromLagScreen, New Dictionary(Of InvariantString, Object) From {
                    {"player index", pid.Index},
                    {"marginal milliseconds used", lagTimeInMilliseconds}})
        End Function
        <Pure()>
        Public Function MakeText(ByVal text As String,
                                 ByVal chatType As ChatType,
                                 ByVal receivingGroup As ChatGroup?,
                                 ByVal receivers As IEnumerable(Of PID),
                                 ByVal sender As PID) As Packet
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(receivers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Select Case chatType
                Case chatType.Game
                    Contract.Assume(receivingGroup.HasValue)
                    Return Packet.FromValue(Packets.Text, New Dictionary(Of InvariantString, Object) From {
                            {"receiving players", (From p In receivers Select p.Index).ToList},
                            {"sending player index", sender.Index},
                            {"type", chatType},
                            {"message", text},
                            {"receiving group", receivingGroup.Value}})
                Case chatType.Lobby
                    Return Packet.FromValue(Packets.Text, New Dictionary(Of InvariantString, Object) From {
                            {"receiving players", (From p In receivers Select p.Index).ToList},
                            {"sending player index", sender.Index},
                            {"type", chatType},
                            {"message", text}})
                Case Else
                    Throw chatType.MakeArgumentValueException("chatType")
            End Select
        End Function
        <Pure()>
        Public Function MakeGreet(ByVal remoteEndPoint As Net.IPEndPoint,
                                  ByVal assignedIndex As PID) As Packet
            Contract.Requires(remoteEndPoint IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.Greet, New Dictionary(Of InvariantString, Object) From {
                    {"slot data", New Byte() {}.AsReadableList},
                    {"player index", assignedIndex.Index},
                    {"external address", remoteEndPoint}})
        End Function
        <Pure()>
        Public Function MakeReject(ByVal reason As RejectReason) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.RejectEntry, reason)
        End Function
        <Pure()>
        Public Function MakeHostMapInfo(ByVal map As Map,
                                        Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Requires(map IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.HostMapInfo, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"path", map.AdvertisedPath.ToString},
                    {"size", map.FileSize},
                    {"crc32", map.FileChecksumCRC32},
                    {"xoro checksum", map.MapChecksumXORO},
                    {"sha1 checksum", map.MapChecksumSHA1}})
        End Function
        <Pure()>
        Public Function MakeOtherPlayerJoined(ByVal name As InvariantString,
                                              ByVal pid As PID,
                                              ByVal peerKey As UInt32,
                                              ByVal peerData As IReadableList(Of Byte),
                                              ByVal listenAddress As Net.IPEndPoint) As Packet
            Contract.Requires(listenAddress IsNot Nothing)
            Contract.Requires(peerData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.OtherPlayerJoined, New Dictionary(Of InvariantString, Object) From {
                    {"peer key", peerKey},
                    {"index", pid.Index},
                    {"name", name.ToString},
                    {"peer data", peerData},
                    {"external address", listenAddress},
                    {"internal address", listenAddress}})
        End Function
        <Pure()>
        Public Function MakePing(ByVal salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.Ping, salt)
        End Function

        <Pure()>
        Public Function MakeOtherPlayerReady(ByVal pid As PID) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.OtherPlayerReady, pid.Index)
        End Function
        <Pure()>
        Public Function MakeOtherPlayerLeft(ByVal pid As PID,
                                            ByVal reportedReason As PlayerLeaveReason) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.OtherPlayerLeft, New Dictionary(Of InvariantString, Object) From {
                                {"player index", pid.Index},
                                {"reason", CByte(reportedReason)}})
        End Function
        <Pure()>
        Public Function MakeLobbyState(ByVal layoutStyle As LobbyLayoutStyle,
                                       ByVal slots As IEnumerable(Of Slot),
                                       ByVal randomSeed As ModInt32,
                                       Optional ByVal receiver As Player = Nothing,
                                       Optional ByVal hideSlots As Boolean = False) As Packet
            Contract.Requires(slots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim reportedPlayerSlots = slots.Count
            If hideSlots Then
                reportedPlayerSlots = 13 '[making the reported count larger than the true count causes wc3 to not update the slot layout]
            End If

            Return Packet.FromValue(Packets.LobbyState, New Dictionary(Of InvariantString, Object) From {
                    {"slots", (From slot In slots Select SlotJar.PackSlot(slot, receiver)).ToList},
                    {"random seed", CUInt(randomSeed)},
                    {"layout style", layoutStyle},
                    {"num player slots", reportedPlayerSlots}})
        End Function
        <Pure()>
        Public Function MakeStartCountdown() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.StartCountdown, New Object)
        End Function
        <Pure()>
        Public Function MakeStartLoading() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.StartLoading, New Object)
        End Function
        <Pure()>
        Public Function MakeHostConfirmHostLeaving() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.HostConfirmHostLeaving, New Object)
        End Function
        <Pure()>
        Public Function MakeTick(ByVal timeSpan As UShort,
                                 Optional ByVal actions As IReadableList(Of PlayerActionSet) = Nothing) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.Tick, New Dictionary(Of InvariantString, Object) From {
                    {"time span", timeSpan},
                    {"player action sets", Tuple(actions IsNot Nothing, actions)}})
        End Function

        <Pure()>
        Public Function MakeMapFileData(ByVal filePosition As UInt32,
                                        ByVal fileData As IReadableList(Of Byte),
                                        ByVal receiverIndex As PID,
                                        ByVal senderIndex As PID,
                                        Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Requires(filePosition >= 0)
            Contract.Requires(fileData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            Return Packet.FromValue(Packets.MapFileData, New Dictionary(Of InvariantString, Object) From {
                    {"receiving player index", receiverIndex.Index},
                    {"sending player index", senderIndex.Index},
                    {"map transfer key", mapTransferKey},
                    {"file position", filePosition},
                    {"crc32", fileData.CRC32},
                    {"file data", fileData}})
        End Function
        <Pure()>
        Public Function MakeSetUploadTarget(ByVal receiverIndex As PID,
                                            ByVal filePosition As UInteger,
                                            Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.SetUploadTarget, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"receiving player index", receiverIndex.Index},
                    {"starting file pos", filePosition}})
        End Function
        <Pure()>
        Public Function MakeSetDownloadSource(ByVal senderIndex As PID,
                                              Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.SetDownloadSource, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"sending player index", senderIndex.Index}})
        End Function
        <Pure()>
        Public Function MakeClientMapInfo(ByVal transferState As MapTransferState,
                                          ByVal totalDownloaded As UInteger,
                                          Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.ClientMapInfo, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"transfer state", transferState},
                    {"total downloaded", totalDownloaded}})
        End Function
        <Pure()>
        Public Function MakeMapFileDataReceived(ByVal senderIndex As PID,
                                                ByVal receiverIndex As PID,
                                                ByVal totalDownloaded As UInteger,
                                                Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.MapFileDataReceived, New Dictionary(Of InvariantString, Object) From {
                    {"sender index", senderIndex.Index},
                    {"receiver index", receiverIndex.Index},
                    {"map transfer key", mapTransferKey},
                    {"total downloaded", totalDownloaded}})
        End Function

        <Pure()>
        Public Function MakeLanCreateGame(ByVal wc3MajorVersion As UInteger,
                                          ByVal gameId As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.LanCreateGame, New Dictionary(Of InvariantString, Object) From {
                    {"product id", "W3XP"},
                    {"major version", wc3MajorVersion},
                    {"game id", gameId}})
        End Function
        <Pure()>
        Public Function MakeLanRefreshGame(ByVal gameId As UInteger,
                                           ByVal game As GameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.LanRefreshGame, New Dictionary(Of InvariantString, Object) From {
                    {"game id", gameId},
                    {"num players", 0},
                    {"free slots", game.TotalSlotCount - game.UsedSlotCount}})
        End Function
        <Pure()>
        Public Function MakeLanGameDetails(ByVal majorVersion As UInteger,
                                           ByVal game As LocalGameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.LanGameDetails, New Dictionary(Of InvariantString, Object) From {
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
            Return Packet.FromValue(Packets.LanDestroyGame, gameId)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
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
            Return Packet.FromValue(Packets.Knock, New Dictionary(Of InvariantString, Object) From {
                    {"game id", gameId},
                    {"entry key", entryKey},
                    {"unknown value", 0},
                    {"listen port", listenPort},
                    {"peer key", peerKey},
                    {"name", name.ToString},
                    {"peer data", New Byte() {0}.AsReadableList},
                    {"internal address", New Net.IPEndPoint(internalAddress, sendingPort)}})
        End Function
        <Pure()>
        Public Function MakeReady() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.Ready, New Object)
        End Function
        <Pure()>
        Public Function MakePong(ByVal salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.Pong, salt)
        End Function
        <Pure()>
        Public Function MakeTock(ByVal unknown As Byte,
                                 ByVal checksum As UInt32) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.Tock, New Dictionary(Of InvariantString, Object) From {
                    {"unknown", unknown},
                    {"game state checksum", checksum}})
        End Function
        <Pure()>
        Public Function MakePeerConnectionInfo(ByVal pids As IEnumerable(Of PID)) As Packet
            Contract.Requires(pids IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim peerFlags = (From pid In pids Select CUShort(1) << (pid.Index - 1)).ReduceUsing(Function(flag1, flag2) flag1 Or flag2)
            Return Packet.FromValue(Packets.PeerConnectionInfo, peerFlags)
        End Function

        <Pure()>
        Public Function MakePeerKnock(ByVal receiverPeerKey As UInteger,
                                      ByVal senderId As PID,
                                      ByVal connectedPeers As IEnumerable(Of PID)) As Packet
            Contract.Requires(connectedPeers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim peerFlags = (From pid In connectedPeers Select CUShort(1) << (pid.Index - 1)).ReduceUsing(Function(flag1, flag2) flag1 Or flag2)
            Return Packet.FromValue(Packets.PeerKnock, New Dictionary(Of InvariantString, Object) From {
                    {"receiver peer key", receiverPeerKey},
                    {"unknown1", 0},
                    {"sender player id", senderId.Index},
                    {"unknown3", &HFF},
                    {"sender peer connection flags", peerFlags}})
        End Function
        <Pure()>
        Public Function MakePeerPing(ByVal salt As UInt32,
                                     ByVal senderConnectedPeers As IEnumerable(Of PID)) As Packet
            Contract.Requires(senderConnectedPeers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim peerFlags = (From pid In senderConnectedPeers Select CUShort(1) << (pid.Index - 1)).ReduceUsing(Function(flag1, flag2) flag1 Or flag2)
            Return Packet.FromValue(Packets.PeerPing, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt},
                    {"sender peer connection flags", peerFlags},
                    {"unknown2", 0}})
        End Function
        <Pure()>
        Public Function MakePeerPong(ByVal salt As UInt32) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.PeerPong, salt)
        End Function
    End Module
End Namespace
