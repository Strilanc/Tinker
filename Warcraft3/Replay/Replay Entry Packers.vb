Imports Tinker.Pickling

Namespace WC3.Replay
    Public Module Packers
        <Pure()>
        Public Function MakeStartOfReplay(ByVal primaryPlayerId As PlayerId,
                                          ByVal primaryPlayerName As InvariantString,
                                          ByVal primaryPlayerPeerData As IRist(Of Byte),
                                          ByVal gameName As InvariantString,
                                          ByVal gameStats As GameStats,
                                          ByVal playerCount As UInt32,
                                          ByVal gameType As Protocol.GameTypes,
                                          Optional ByVal language As UInt32 = &H18F8B0) As ReplayEntry
            Contract.Requires(primaryPlayerPeerData IsNot Nothing)
            Contract.Requires(gameStats IsNot Nothing)
            Contract.Requires(playerCount > 0)
            Contract.Requires(playerCount <= 12)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryStartOfReplay, New Dictionary(Of InvariantString, Object) From {
                    {"unknown1", 1UI},
                    {"primary player id", primaryPlayerId},
                    {"primary player name", primaryPlayerName.ToString},
                    {"primary player shared data", primaryPlayerPeerData},
                    {"game name", gameName.ToString},
                    {"unknown2", CByte(0)},
                    {"game stats", gameStats},
                    {"player count", playerCount},
                    {"game type", gameType},
                    {"language", language}})
        End Function
        <Pure()>
        Public Function MakePlayerJoined(ByVal id As PlayerId,
                                         ByVal name As InvariantString,
                                         ByVal peerData As IRist(Of Byte)) As ReplayEntry
            Contract.Requires(peerData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryPlayerJoined, New Dictionary(Of InvariantString, Object) From {
                           {"joiner id", id},
                           {"name", name.ToString},
                           {"shared data", peerData},
                           {"unknown", 0UI}})
        End Function
        <Pure()>
        Public Function MakeLobbyState(ByVal slots As IEnumerable(Of Slot),
                                       ByVal randomSeed As UInt32,
                                       ByVal layoutStyle As Protocol.LobbyLayoutStyle,
                                       ByVal defaultPlayerSlotCount As Byte) As ReplayEntry
            Contract.Requires(slots IsNot Nothing)
            Contract.Requires(defaultPlayerSlotCount > 0)
            Contract.Requires(defaultPlayerSlotCount <= 12)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryLobbyState, New Dictionary(Of InvariantString, Object) From {
                    {"slots", (From slot In slots Select Protocol.SlotJar.PackSlot(slot)).ToReadableList},
                    {"random seed", randomSeed},
                    {"layout style", layoutStyle},
                    {"num player slots", defaultPlayerSlotCount}})
        End Function
        <Pure()>
        Public Function MakeLoadStarted1() As ReplayEntry
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryLoadStarted1, 1UI)
        End Function
        <Pure()>
        Public Function MakeLoadStarted2() As ReplayEntry
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryLoadStarted2, 1UI)
        End Function
        <Pure()>
        Public Function MakeGameStarted() As ReplayEntry
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryGameStarted, 1UI)
        End Function
        <Pure()>
        Public Function MakePlayerLeft(ByVal unknown1 As UInt32,
                                       ByVal leaver As PlayerId,
                                       ByVal reportedReason As Protocol.PlayerLeaveReason,
                                       ByVal leaveCount As UInt32) As ReplayEntry
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryPlayerLeft, New Dictionary(Of InvariantString, Object) From {
                    {"unknown1", unknown1},
                    {"leaver", leaver},
                    {"reason", reportedReason},
                    {"session leave count", leaveCount}})
        End Function
        <Pure()>
        Public Function MakeGameStateChecksum(ByVal checksum As UInt32) As ReplayEntry
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryGameStateChecksum, checksum)
        End Function
        <Pure()>
        Public Function MakeLobbyChatMessage(ByVal sender As PlayerId,
                                             ByVal message As String) As ReplayEntry
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryChatMessage, New Dictionary(Of InvariantString, Object) From {
                    {"speaker", sender},
                    {"type group message", New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                        {"type group", Protocol.ChatType.Lobby.KeyValue(Of Object)(New NoValue)},
                        {"message", message}})}})
        End Function
        <Pure()>
        Public Function MakeGameChatMessage(ByVal sender As PlayerId,
                                            ByVal message As String,
                                            ByVal receivingGroup As Protocol.ChatGroup) As ReplayEntry
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryChatMessage, New Dictionary(Of InvariantString, Object) From {
                    {"speaker", sender},
                    {"type group message", New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                        {"type group", Protocol.ChatType.Game.KeyValue(Of Object)(receivingGroup)},
                        {"message", message}})}})
        End Function
        <Pure()>
        Public Function MakeTickPreOverflow(ByVal actions As IRist(Of Protocol.PlayerActionSet),
                                            Optional ByVal duration As UInt16 = Nothing) As ReplayEntry
            Contract.Requires(actions IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryTickPreOverflow, New Dictionary(Of InvariantString, Object) From {
                    {"time span", duration},
                    {"player action sets", actions}})
        End Function
        <Pure()>
        Public Function MakeTick(ByVal duration As UInt16,
                                 ByVal actions As IRist(Of Protocol.PlayerActionSet)) As ReplayEntry
            Contract.Requires(actions IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryTick, New Dictionary(Of InvariantString, Object) From {
                    {"time span", duration},
                    {"player action sets", actions}})
        End Function
    End Module
End Namespace
