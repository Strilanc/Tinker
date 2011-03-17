Imports Tinker.Pickling

Namespace WC3.Replay
    Public Module Packers
        <Pure()>
        Public Function MakeStartOfReplay(primaryPlayerId As PlayerId,
                                          primaryPlayerName As InvariantString,
                                          primaryPlayerPeerData As IRist(Of Byte),
                                          gameName As InvariantString,
                                          gameStats As GameStats,
                                          playerCount As UInt32,
                                          gameType As Protocol.GameTypes,
                                          Optional language As UInt32 = &H18F8B0) As ReplayEntry
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
        Public Function MakePlayerJoined(id As PlayerId,
                                         name As InvariantString,
                                         peerData As IRist(Of Byte)) As ReplayEntry
            Contract.Requires(peerData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryPlayerJoined, New Dictionary(Of InvariantString, Object) From {
                           {"joiner id", id},
                           {"name", name.ToString},
                           {"shared data", peerData},
                           {"unknown", 0UI}})
        End Function
        <Pure()>
        Public Function MakeLobbyState(slots As IEnumerable(Of Slot),
                                       randomSeed As UInt32,
                                       layoutStyle As Protocol.LobbyLayoutStyle,
                                       defaultPlayerSlotCount As Byte) As ReplayEntry
            Contract.Requires(slots IsNot Nothing)
            Contract.Requires(defaultPlayerSlotCount > 0)
            Contract.Requires(defaultPlayerSlotCount <= 12)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryLobbyState, New Dictionary(Of InvariantString, Object) From {
                    {"slots", (From slot In slots Select Protocol.SlotJar.PackSlot(slot)).ToRist},
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
        Public Function MakePlayerLeft(unknown1 As UInt32,
                                       leaver As PlayerId,
                                       reportedReason As Protocol.PlayerLeaveReason,
                                       leaveCount As UInt32) As ReplayEntry
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryPlayerLeft, New Dictionary(Of InvariantString, Object) From {
                    {"unknown1", unknown1},
                    {"leaver", leaver},
                    {"reason", reportedReason},
                    {"session leave count", leaveCount}})
        End Function
        <Pure()>
        Public Function MakeGameStateChecksum(checksum As UInt32) As ReplayEntry
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryGameStateChecksum, checksum)
        End Function
        <Pure()>
        Public Function MakeLobbyChatMessage(sender As PlayerId,
                                             message As String) As ReplayEntry
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryChatMessage, New Dictionary(Of InvariantString, Object) From {
                    {"speaker", sender},
                    {"type group message", New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                        {"type group", Protocol.ChatType.Lobby.KeyValue(Of Object)(New NoValue)},
                        {"message", message}})}})
        End Function
        <Pure()>
        Public Function MakeGameChatMessage(sender As PlayerId,
                                            message As String,
                                            receivingGroup As Protocol.ChatGroup) As ReplayEntry
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryChatMessage, New Dictionary(Of InvariantString, Object) From {
                    {"speaker", sender},
                    {"type group message", New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                        {"type group", Protocol.ChatType.Game.KeyValue(Of Object)(receivingGroup)},
                        {"message", message}})}})
        End Function
        <Pure()>
        Public Function MakeTickPreOverflow(actions As IRist(Of Protocol.PlayerActionSet),
                                            Optional duration As UInt16 = Nothing) As ReplayEntry
            Contract.Requires(actions IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryTickPreOverflow, New Dictionary(Of InvariantString, Object) From {
                    {"time span", duration},
                    {"player action sets", actions}})
        End Function
        <Pure()>
        Public Function MakeTick(duration As UInt16,
                                 actions As IRist(Of Protocol.PlayerActionSet)) As ReplayEntry
            Contract.Requires(actions IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return ReplayEntry.FromDefinitionAndValue(Format.ReplayEntryTick, New Dictionary(Of InvariantString, Object) From {
                    {"time span", duration},
                    {"player action sets", actions}})
        End Function
    End Module
End Namespace
