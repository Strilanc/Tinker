Imports Tinker.Pickling

Namespace WC3.Protocol
    Public Module Packers
        <Pure()>
        Public Function MakePlayersLagging(laggers As IEnumerable(Of PlayerId)) As Packet
            Contract.Requires(laggers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.PlayersLagging, (From lagger In laggers
                                                                   Select New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                                                                           {"id", lagger},
                                                                           {"initial milliseconds used", 2000UI}})).ToRist)
        End Function
        <Pure()>
        Public Function MakePlayerStoppedLagging(lagger As PlayerId,
                                                 lagTimeInMilliseconds As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.PlayerStoppedLagging, New Dictionary(Of InvariantString, Object) From {
                    {"lagger", lagger},
                    {"marginal milliseconds used", lagTimeInMilliseconds}})
        End Function
        <Pure()>
        Public Function MakeText(text As String,
                                 chatType As ChatType,
                                 receivingGroup As ChatGroup?,
                                 receivers As IEnumerable(Of PlayerId),
                                 sender As PlayerId) As Packet
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(receivers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Select Case chatType
                Case chatType.Game
                    Contract.Assume(receivingGroup.HasValue)
                    Return Packet.FromValue(ServerPackets.Text, New Dictionary(Of InvariantString, Object) From {
                            {"requested receivers", receivers.ToRist},
                            {"speaker", sender},
                            {"type group", chatType.KeyValue(Of Object)(receivingGroup.Value)},
                            {"message", text}})
                Case chatType.Lobby
                    Return Packet.FromValue(ServerPackets.Text, New Dictionary(Of InvariantString, Object) From {
                            {"requested receivers", receivers.ToRist},
                            {"speaker", sender},
                            {"type group", chatType.KeyValue(Of Object)(New NoValue)},
                            {"message", text}})
                Case Else
                    Throw chatType.MakeArgumentValueException("chatType")
            End Select
        End Function
        <Pure()>
        Public Function MakeGreet(remoteExternalEndPoint As Net.IPEndPoint,
                                  assignedId As PlayerId) As Packet
            Contract.Requires(remoteExternalEndPoint IsNot Nothing)
            Contract.Requires(remoteExternalEndPoint.Address IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.Greet, New Dictionary(Of InvariantString, Object) From {
                    {"lobby state", [Default](Of NullableValue(Of NamedValueMap))()},
                    {"assigned id", assignedId},
                    {"external address", remoteExternalEndPoint}})
        End Function
        <Pure()>
        Public Function MakeReject(reason As RejectReason) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.RejectEntry, reason)
        End Function
        <Pure()>
        Public Function MakeHostMapInfo(map As Map,
                                        Optional mapTransferKey As UInt32 = 1) As Packet
            Contract.Requires(map IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.HostMapInfo, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"path", map.AdvertisedPath.ToString},
                    {"size", map.FileSize},
                    {"crc32", map.FileChecksumCRC32},
                    {"xoro checksum", map.MapChecksumXORO},
                    {"sha1 checksum", map.MapChecksumSHA1}})
        End Function
        <Pure()>
        Public Function MakeOtherPlayerJoined(name As InvariantString,
                                              joiner As PlayerId,
                                              peerKey As UInt32,
                                              peerData As IRist(Of Byte),
                                              listenAddress As Net.IPEndPoint) As Packet
            Contract.Requires(peerData IsNot Nothing)
            Contract.Requires(listenAddress IsNot Nothing)
            Contract.Requires(listenAddress.Address IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.OtherPlayerJoined, New Dictionary(Of InvariantString, Object) From {
                    {"peer key", peerKey},
                    {"joiner id", joiner},
                    {"name", name.ToString},
                    {"peer data", peerData},
                    {"external address", listenAddress},
                    {"internal address", listenAddress}})
        End Function
        <Pure()>
        Public Function MakePing(salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.Ping, salt)
        End Function

        <Pure()>
        Public Function MakeOtherPlayerReady(readiedPlayer As PlayerId) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.OtherPlayerReady, readiedPlayer)
        End Function
        <Pure()>
        Public Function MakeOtherPlayerLeft(leaver As PlayerId,
                                            reportedReason As PlayerLeaveReason) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.OtherPlayerLeft, New Dictionary(Of InvariantString, Object) From {
                                {"leaver", leaver},
                                {"reason", reportedReason}})
        End Function
        <Pure()>
        Public Function MakeLobbyState(layoutStyle As LobbyLayoutStyle,
                                       slots As IEnumerable(Of Slot),
                                       randomSeed As UInt32,
                                       Optional receiver As Player = Nothing,
                                       Optional hideSlots As Boolean = False) As Packet
            Contract.Requires(slots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim reportedPlayerSlots = CByte(slots.Count)
            If hideSlots Then
                reportedPlayerSlots = 13 '[making the reported count larger than the true count causes wc3 to not update the slot layout]
            End If

            Return Packet.FromValue(ServerPackets.LobbyState, New Dictionary(Of InvariantString, Object) From {
                    {"slots", (From slot In slots Select SlotJar.PackSlot(slot, receiver)).ToRist},
                    {"random seed", randomSeed},
                    {"layout style", layoutStyle},
                    {"num player slots", reportedPlayerSlots}})
        End Function
        <Pure()>
        Public Function MakeStartCountdown() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.StartCountdown, New NoValue)
        End Function
        <Pure()>
        Public Function MakeStartLoading() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.StartLoading, New NoValue)
        End Function
        <Pure()>
        Public Function MakeHostConfirmHostLeaving() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.HostConfirmHostLeaving, New NoValue)
        End Function
        <Pure()>
        Public Function MakeTickPreOverflow(actions As IRist(Of PlayerActionSet),
                                            Optional timeSpan As UShort = 0) As Packet
            Contract.Requires(actions IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.TickPreOverflow, New Dictionary(Of InvariantString, Object) From {
                    {"time span", timeSpan},
                    {"player action sets", actions}})
        End Function
        <Pure()>
        Public Function MakeTick(timeSpan As UShort,
                                 Optional actions As NullableValue(Of IRist(Of PlayerActionSet)) = Nothing) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.Tick, New Dictionary(Of InvariantString, Object) From {
                    {"time span", timeSpan},
                    {"player action sets", actions}})
        End Function

        <Pure()>
        Public Function MakeMapFileData(filePosition As UInt32,
                                        fileData As IRist(Of Byte),
                                        downloader As PlayerId,
                                        uploader As PlayerId,
                                        Optional mapTransferKey As UInt32 = 1) As Packet
            Contract.Requires(fileData IsNot Nothing)
            Contract.Requires(downloader <> uploader)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            Return Packet.FromValue(PeerPackets.MapFileData, New Dictionary(Of InvariantString, Object) From {
                    {"downloader", downloader},
                    {"uploader", uploader},
                    {"map transfer key", mapTransferKey},
                    {"file position", filePosition},
                    {"file data", fileData}})
        End Function
        <Pure()>
        Public Function MakeSetUploadTarget(downloader As PlayerId,
                                            filePosition As UInteger,
                                            Optional mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.SetUploadTarget, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"downloader", downloader},
                    {"starting file pos", filePosition}})
        End Function
        <Pure()>
        Public Function MakeSetDownloadSource(uploader As PlayerId,
                                              Optional mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.SetDownloadSource, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"uploader", uploader}})
        End Function
        <Pure()>
        Public Function MakeClientMapInfo(transferState As MapTransferState,
                                          totalDownloaded As UInteger,
                                          Optional mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ClientPackets.ClientMapInfo, New Dictionary(Of InvariantString, Object) From {
                    {"map transfer key", mapTransferKey},
                    {"transfer state", transferState},
                    {"total downloaded", totalDownloaded}})
        End Function
        <Pure()>
        Public Function MakeMapFileDataReceived(downloader As PlayerId,
                                                uploader As PlayerId,
                                                totalDownloaded As UInteger,
                                                Optional mapTransferKey As UInt32 = 1) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(PeerPackets.MapFileDataReceived, New Dictionary(Of InvariantString, Object) From {
                    {"downloader", downloader},
                    {"uploader", uploader},
                    {"map transfer key", mapTransferKey},
                    {"total downloaded", totalDownloaded}})
        End Function

        <Pure()>
        Public Function MakeLanCreateGame(wc3MajorVersion As UInteger,
                                          gameId As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.LanCreateGame, New Dictionary(Of InvariantString, Object) From {
                    {"product id", "W3XP"},
                    {"major version", wc3MajorVersion},
                    {"game id", gameId}})
        End Function
        <Pure()>
        Public Function MakeLanRefreshGame(gameId As UInteger,
                                           game As GameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.LanRefreshGame, New Dictionary(Of InvariantString, Object) From {
                    {"game id", gameId},
                    {"num players", 0UI},
                    {"free slots", CUInt(game.TotalSlotCount - game.UsedSlotCount)}})
        End Function
        <Pure()>
        Public Function MakeLanGameDetails(majorVersion As UInteger,
                                           game As LocalGameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.LanGameDetails, New Dictionary(Of InvariantString, Object) From {
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
                    {"age", CUInt(game.AgeClock.ElapsedTime.TotalMilliseconds)},
                    {"listen port", game.Port}})
        End Function
        <Pure()>
        Public Function MakeLanDestroyGame(gameId As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ServerPackets.LanDestroyGame, gameId)
        End Function

        <Pure()>
        Public Function MakeKnock(name As InvariantString,
                                  listenPort As UShort,
                                  sendingPort As UShort,
                                  Optional gameId As UInt32 = 0,
                                  Optional entryKey As UInt32 = 0,
                                  Optional peerKey As UInt32 = 0,
                                  Optional internalAddress As Net.IPAddress = Nothing) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            If internalAddress Is Nothing Then
                internalAddress = New Net.IPAddress(GetCachedIPAddressBytes(external:=True))
            End If
            Return Packet.FromValue(ClientPackets.Knock, New KnockData(
                    gameId:=gameId,
                    entryKey:=entryKey,
                    unknown:=CByte(0),
                    listenPort:=listenPort,
                    peerKey:=peerKey,
                    name:=name,
                    peerData:=MakeRist(Of Byte)(0),
                    internalEndPoint:=internalAddress.WithPort(sendingPort)))
        End Function
        <Pure()>
        Public Function MakeReady() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ClientPackets.Ready, New NoValue)
        End Function
        <Pure()>
        Public Function MakePong(salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ClientPackets.Pong, salt)
        End Function
        <Pure()>
        Public Function MakeTock(unknown As Byte,
                                 checksum As UInt32) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(ClientPackets.Tock, New Dictionary(Of InvariantString, Object) From {
                    {"unknown", unknown},
                    {"game state checksum", checksum}})
        End Function
        <Pure()>
        Public Function MakePeerConnectionInfo(pids As IEnumerable(Of PlayerId)) As Packet
            Contract.Requires(pids IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim peerFlags = (From pid In pids Select 1US << (pid.Index - 1)).Aggregate(0US, Function(flag1, flag2) flag1 Or flag2)
            Return Packet.FromValue(ClientPackets.PeerConnectionInfo, peerFlags)
        End Function

        <Pure()>
        Public Function MakePeerKnock(receiverPeerKey As UInteger,
                                      sender As PlayerId,
                                      connectedPeers As IEnumerable(Of PlayerId)) As Packet
            Contract.Requires(connectedPeers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim peerFlags = (From pid In connectedPeers Select 1UI << (pid.Index - 1)).Aggregate(0UI, Function(flag1, flag2) flag1 Or flag2)
            Return Packet.FromValue(PeerPackets.PeerKnock, New Dictionary(Of InvariantString, Object) From {
                    {"receiver peer key", receiverPeerKey},
                    {"unknown1", 0UI},
                    {"sender id", sender},
                    {"unknown3", CByte(&HFF)},
                    {"sender peer connection flags", peerFlags}})
        End Function
        <Pure()>
        Public Function MakePeerPing(salt As UInt32,
                                     senderConnectedPeers As IEnumerable(Of PlayerId)) As Packet
            Contract.Requires(senderConnectedPeers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim peerFlags = (From pid In senderConnectedPeers Select 1UI << (pid.Index - 1)).Aggregate(0UI, Function(flag1, flag2) flag1 Or flag2)
            Return Packet.FromValue(PeerPackets.PeerPing, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt},
                    {"sender peer connection flags", peerFlags},
                    {"unknown2", 0UI}})
        End Function
        <Pure()>
        Public Function MakePeerPong(salt As UInt32) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return Packet.FromValue(PeerPackets.PeerPong, salt)
        End Function
    End Module
End Namespace
