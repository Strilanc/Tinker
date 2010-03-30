Imports Tinker.Pickling

Namespace WC3.Protocol
    Public Module Packers
        <Pure()>
        Public Function MakeShowLagScreen(ByVal laggers As IEnumerable(Of PlayerId)) As Packet
            Contract.Requires(laggers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.ShowLagScreen, (From lagger In laggers
                                                            Select New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                                                                    {"id", lagger},
                                                                    {"initial milliseconds used", 2000UI}})).ToReadableList)
        End Function
        <Pure()>
        Public Function MakeRemovePlayerFromLagScreen(ByVal lagger As PlayerId,
                                                      ByVal lagTimeInMilliseconds As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.RemovePlayerFromLagScreen, New Dictionary(Of InvariantString, Object) From {
                    {"lagger", lagger},
                    {"marginal milliseconds used", lagTimeInMilliseconds}})
        End Function
        <Pure()>
        Public Function MakeText(ByVal text As String,
                                 ByVal chatType As ChatType,
                                 ByVal receivingGroup As ChatGroup?,
                                 ByVal receivers As IEnumerable(Of PlayerId),
                                 ByVal sender As PlayerId) As Packet
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(receivers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Select Case chatType
                Case chatType.Game
                    Contract.Assume(receivingGroup.HasValue)
                    Return Packet.FromValue(Packets.Text, New Dictionary(Of InvariantString, Object) From {
                            {"requested receivers", receivers.ToReadableList},
                            {"speaker", sender},
                            {"type group", New KeyValuePair(Of ChatType, Object)(chatType, receivingGroup.Value)},
                            {"message", text}})
                Case chatType.Lobby
                    Return Packet.FromValue(Packets.Text, New Dictionary(Of InvariantString, Object) From {
                            {"requested receivers", receivers.ToReadableList},
                            {"speaker", sender},
                            {"type group", New KeyValuePair(Of ChatType, Object)(chatType, New Object)},
                            {"message", text}})
                Case Else
                    Throw chatType.MakeArgumentValueException("chatType")
            End Select
        End Function
        <Pure()>
        Public Function MakeGreet(ByVal remoteEndPoint As Net.IPEndPoint,
                                  ByVal assignedId As PlayerId) As Packet
            Contract.Requires(remoteEndPoint IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.Greet, New Dictionary(Of InvariantString, Object) From {
                    {"slot data", New Byte() {}.AsReadableList},
                    {"assigned id", assignedId},
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
                                              ByVal joiner As PlayerId,
                                              ByVal peerKey As UInt32,
                                              ByVal peerData As IReadableList(Of Byte),
                                              ByVal listenAddress As Net.IPEndPoint) As Packet
            Contract.Requires(listenAddress IsNot Nothing)
            Contract.Requires(peerData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.OtherPlayerJoined, New Dictionary(Of InvariantString, Object) From {
                    {"peer key", peerKey},
                    {"joiner id", joiner},
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
        Public Function MakeOtherPlayerReady(ByVal readiedPlayer As PlayerId) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.OtherPlayerReady, readiedPlayer)
        End Function
        <Pure()>
        Public Function MakeOtherPlayerLeft(ByVal leaver As PlayerId,
                                            ByVal reportedReason As PlayerLeaveReason) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.OtherPlayerLeft, New Dictionary(Of InvariantString, Object) From {
                                {"leaver", leaver},
                                {"reason", reportedReason}})
        End Function
        <Pure()>
        Public Function MakeLobbyState(ByVal layoutStyle As LobbyLayoutStyle,
                                       ByVal slots As IEnumerable(Of Slot),
                                       ByVal randomSeed As UInt32,
                                       Optional ByVal receiver As Player = Nothing,
                                       Optional ByVal hideSlots As Boolean = False) As Packet
            Contract.Requires(slots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim reportedPlayerSlots = CByte(slots.Count)
            If hideSlots Then
                reportedPlayerSlots = 13 '[making the reported count larger than the true count causes wc3 to not update the slot layout]
            End If

            Return Packet.FromValue(Packets.LobbyState, New Dictionary(Of InvariantString, Object) From {
                    {"slots", (From slot In slots Select SlotJar.PackSlot(slot, receiver)).ToReadableList},
                    {"random seed", randomSeed},
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
                    {"player action sets", Tuple.Create(actions IsNot Nothing, actions)}})
        End Function

        <Pure()>
        Public Function MakeMapFileData(ByVal filePosition As UInt32,
                                        ByVal fileData As IReadableList(Of Byte),
                                        ByVal downloader As PlayerId,
                                        ByVal uploader As PlayerId,
                                        Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Requires(filePosition >= 0)
            Contract.Requires(fileData IsNot Nothing)
            Contract.Requires(downloader <> uploader)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            Return Packet.FromValue(Packets.MapFileData, New Dictionary(Of InvariantString, Object) From {
                    {"downloader", downloader},
                    {"uploader", uploader},
                    {"map transfer key", mapTransferKey},
                    {"file position", filePosition},
                    {"file data", fileData}})
        End Function
        <Pure()>
        Public Function MakeSetUploadTarget(ByVal downloader As PlayerId,
                                            ByVal filePosition As UInteger,
                                            Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.SetUploadTarget, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"downloader", downloader},
                    {"starting file pos", filePosition}})
        End Function
        <Pure()>
        Public Function MakeSetDownloadSource(ByVal uploader As PlayerId,
                                              Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.SetDownloadSource, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"uploader", uploader}})
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
        Public Function MakeMapFileDataReceived(ByVal downloader As PlayerId,
                                                ByVal uploader As PlayerId,
                                                ByVal totalDownloaded As UInteger,
                                                Optional ByVal mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.MapFileDataReceived, New Dictionary(Of InvariantString, Object) From {
                    {"downloader", downloader},
                    {"uploader", uploader},
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
                    {"num players", 0UI},
                    {"free slots", CUInt(game.TotalSlotCount - game.UsedSlotCount)}})
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
                    {"num slots", CUInt(game.TotalSlotCount)},
                    {"game type", game.GameType},
                    {"num players + 1", 1UI},
                    {"free slots + 1", CUInt(game.TotalSlotCount + 1 - game.UsedSlotCount)},
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
                    {"unknown value", CByte(0)},
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
        Public Function MakePeerConnectionInfo(ByVal pids As IEnumerable(Of PlayerId)) As Packet
            Contract.Requires(pids IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim peerFlags = (From pid In pids Select 1US << (pid.Index - 1)).Aggregate(0US, Function(flag1, flag2) flag1 Or flag2)
            Return Packet.FromValue(Packets.PeerConnectionInfo, peerFlags)
        End Function

        <Pure()>
        Public Function MakePeerKnock(ByVal receiverPeerKey As UInteger,
                                      ByVal sender As PlayerId,
                                      ByVal connectedPeers As IEnumerable(Of PlayerId)) As Packet
            Contract.Requires(connectedPeers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim peerFlags = (From pid In connectedPeers Select 1UI << (pid.Index - 1)).Aggregate(0UI, Function(flag1, flag2) flag1 Or flag2)
            Return Packet.FromValue(Packets.PeerKnock, New Dictionary(Of InvariantString, Object) From {
                    {"receiver peer key", receiverPeerKey},
                    {"unknown1", 0UI},
                    {"sender id", sender},
                    {"unknown3", CByte(&HFF)},
                    {"sender peer connection flags", peerFlags}})
        End Function
        <Pure()>
        Public Function MakePeerPing(ByVal salt As UInt32,
                                     ByVal senderConnectedPeers As IEnumerable(Of PlayerId)) As Packet
            Contract.Requires(senderConnectedPeers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim peerFlags = (From pid In senderConnectedPeers Select 1UI << (pid.Index - 1)).Aggregate(0UI, Function(flag1, flag2) flag1 Or flag2)
            Return Packet.FromValue(Packets.PeerPing, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt},
                    {"sender peer connection flags", peerFlags},
                    {"unknown2", 0UI}})
        End Function
        <Pure()>
        Public Function MakePeerPong(ByVal salt As UInt32) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(Packets.PeerPong, salt)
        End Function
    End Module
End Namespace
