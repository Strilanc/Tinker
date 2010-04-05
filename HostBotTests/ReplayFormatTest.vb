Imports Strilbrary.Values
Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker.Pickling
Imports Tinker
Imports Tinker.WC3
Imports Tinker.WC3.Replay
Imports TinkerTests.PicklingTest

<TestClass()>
Public Class ReplayFormatTest
    <TestMethod()>
    Public Sub ReplayEntryChatMessageTest()
        JarTest(Format.ReplayEntryChatMessage.Jar,
                data:={1,
                       6, 0,
                       &H10,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"speaker", New PlayerId(1)},
                        {"type group message", New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                            {"type group", New KeyValuePair(Of Protocol.ChatType, Object)(Protocol.ChatType.Lobby,
                                                                                          New EmptyJar.EmptyValue)},
                            {"message", "test"}})}})
        JarTest(Format.ReplayEntryChatMessage.Jar,
                data:={1,
                       10, 0,
                       &H20,
                       2, 0, 0, 0,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"speaker", New PlayerId(1)},
                        {"type group message", New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                            {"type group", New KeyValuePair(Of Protocol.ChatType, Object)(Protocol.ChatType.Game,
                                                                                          Protocol.ChatGroup.Observers)},
                            {"message", "test"}})}})
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryGameStartedTest()
        JarTest(Format.ReplayEntryGameStarted.Jar,
                Data:={1, 0, 0, 0},
                value:=1UI)
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryGameStateChecksumTest()
        JarTest(Format.ReplayEntryGameStateChecksum.Jar,
                data:={4,
                       2, 3, 4, 5},
                value:=&H5040302UI)
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryLoadStarted1Test()
        JarTest(Format.ReplayEntryLoadStarted1.Jar,
                Data:={1, 0, 0, 0},
                value:=1UI)
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryLoadStarted2Test()
        JarTest(Format.ReplayEntryLoadStarted2.Jar,
                Data:={1, 0, 0, 0},
                value:=1UI)
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryLobbyStateTest()
        Dim slots = New List(Of Slot)()
        JarTest(Format.ReplayEntryLobbyState.Jar,
                Data:={7, 0,
                       0,
                       13, 0, 0, 0,
                       3,
                       12},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"slots", New List(Of NamedValueMap)().ToReadableList},
                        {"random seed", 13UI},
                        {"layout style", Protocol.LobbyLayoutStyle.FixedPlayerSettings},
                        {"num player slots", CByte(12)}
                    })
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryPlayerJoinedTest()
        JarTest(Format.ReplayEntryPlayerJoined.Jar,
                Data:={2,
                       116, 101, 115, 116, 0,
                       1, 0,
                       5, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"joiner id", New PlayerId(2)},
                        {"name", "test"},
                        {"shared data", {CByte(0)}.AsReadableList},
                        {"unknown", 5UI}})
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryPlayerLeftTest()
        JarTest(Format.ReplayEntryPlayerLeft.Jar,
                Data:={6, 0, 0, 0,
                       2,
                       8, 0, 0, 0,
                       1, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown1", 6UI},
                        {"leaver", New PlayerId(2)},
                        {"reason", Protocol.PlayerLeaveReason.Defeat},
                        {"session leave count", 1UI}})
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryStartOfReplayTest()
        JarTest(Format.ReplayEntryStartOfReplay.Jar,
                Data:=New Byte() {2, 0, 0, 0,
                       1,
                       116, 101, 115, 116, 0,
                       1, 1,
                       116, 101, 115, 0,
                       3
                       }.Concat(New Protocol.GameStatsJar().Pack(TestStats)).Concat({
                       5, 0, 0, 0,
                       8, 0, 0, 0,
                       7, 0, 0, 0}),
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown1", 2UI},
                        {"primary player id", New PlayerId(1)},
                        {"primary player name", "test"},
                        {"primary player shared data", {CByte(1)}.AsReadableList},
                        {"game name", "tes"},
                        {"unknown2", CByte(3)},
                        {"game stats", TestStats},
                        {"player count", 5UI},
                        {"game type", Protocol.GameTypes.AuthenticatedMakerBlizzard},
                        {"language", 7UI}})
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryTickTest()
        JarTest(Format.ReplayEntryTick.Jar,
                Data:={2, 0,
                       250, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"time span", 250US},
                        {"player action sets", New Protocol.PlayerActionSet() {}.AsReadableList}})

        JarTest(Format.ReplayEntryTick.Jar,
                Data:={11, 0,
                       250, 0,
                       2,
                           6, 0,
                               39,
                               1,
                               5, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"time span", 250US},
                        {"player action sets", New Protocol.PlayerActionSet() {
                            New Protocol.PlayerActionSet(New PlayerId(2),
                                                         New Protocol.GameAction() {
                                Protocol.GameAction.FromValue(Protocol.GameActions.CheatGold, New Dictionary(Of InvariantString, Object) From {
                                    {"unknown", CByte(1)},
                                    {"amount", 5UI}})}.AsReadableList)}.AsReadableList}})
    End Sub
    <TestMethod()>
    Public Sub ReplayEntryTournamentForcedCountdownTest()
        JarTest(Format.ReplayEntryTournamentForcedCountdown.Jar,
                Data:={2, 0, 0, 0,
                       3, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"counter state", 2UI},
                        {"counter time", 3UI}})
    End Sub

    <TestMethod()>
    Public Sub MakeGameChatMessageTest()
        MakeGameChatMessage(New PlayerId(1), "test", Protocol.ChatGroup.AllPlayers)
    End Sub
    <TestMethod()>
    Public Sub MakeGameStartedTest()
        MakeGameStarted()
    End Sub
    <TestMethod()>
    Public Sub MakeGameStateChecksumTest()
        MakeGameStateChecksum(1UI)
    End Sub
    <TestMethod()>
    Public Sub MakeLoadStarted1Test()
        MakeLoadStarted1()
    End Sub
    <TestMethod()>
    Public Sub MakeLoadStarted2Test()
        MakeLoadStarted2()
    End Sub
    <TestMethod()>
    Public Sub MakeLobbyChatMessageTest()
        MakeLobbyChatMessage(New PlayerId(1), "test")
    End Sub
    <TestMethod()>
    Public Sub MakeLobbyStateTest()
        MakeLobbyState(New Slot() {New Slot(1, New SlotContentsOpen(), Protocol.PlayerColor.Red, True, 1)}, 1UI, Protocol.LobbyLayoutStyle.AutoMatch, 5)
    End Sub
    <TestMethod()>
    Public Sub MakePlayerJoinedTest()
        MakePlayerJoined(New PlayerId(1), "test", New Byte() {0}.AsReadableList)
    End Sub
    <TestMethod()>
    Public Sub MakePlayerLeftTest()
        MakePlayerLeft(0, New PlayerId(1), Protocol.PlayerLeaveReason.Defeat, 1)
    End Sub
    <TestMethod()>
    Public Sub MakeStartOfReplayTest()
        MakeStartOfReplay(New PlayerId(1), "test", New Byte() {0}.AsReadableList, "test", TestStats, 5, Protocol.GameTypes.MakerBlizzard)
    End Sub
    <TestMethod()>
    Public Sub MakeTickTest()
        MakeTick(250US, New Protocol.PlayerActionSet() {}.AsReadableList)
    End Sub
End Class
