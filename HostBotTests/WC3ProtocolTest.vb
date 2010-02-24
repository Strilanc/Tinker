Imports Strilbrary.Values
Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker.Pickling
Imports Tinker
Imports Tinker.WC3
Imports Tinker.WC3.Protocol
Imports TinkerTests.PicklingTest

<TestClass()>
Public Class WC3ProtocolTest
    <TestMethod()>
    Public Sub ClientConfirmHostLeavingTest()
        EmptyJarTest(Packets.ClientConfirmHostLeaving.Jar)
    End Sub
    <TestMethod()>
    Public Sub ClientMapInfoTest()
        JarTest(Packets.ClientMapInfo.Jar,
                data:={1, 0, 0, 0,
                       3,
                       128, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"map transfer key", 1},
                        {"transfer state", MapTransferState.Downloading},
                        {"total downloaded", 128}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameActionTest()
        JarTest(Packets.GameAction.Jar,
                equater:=Function(e1, e2) ObjectEqual(e1, e2),
                appendSafe:=False,
                data:={0, 0, 0, 0},
                value:=New List(Of GameAction)().AsReadableList)
    End Sub
    <TestMethod()>
    Public Sub GreetTest()
        JarTest(Packets.Greet.Jar,
                data:={0, 0,
                       2,
                       2, 0, &H17, &HE1, 127, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"slot data", New Byte() {}.AsReadableList},
                        {"player index", 2},
                        {"external address", New Net.IPEndPoint(Net.IPAddress.Loopback, 6113)}
                    })
    End Sub
    <TestMethod()>
    Public Sub HostConfirmHostLeavingTest()
        EmptyJarTest(Packets.HostConfirmHostLeaving.Jar)
    End Sub
    <TestMethod()>
    Public Sub HostMapInfoTest()
        Dim sha1 = (From i In Enumerable.Range(0, 20)
                    Select CByte(i)).ToArray.AsReadableList
        JarTest(Packets.HostMapInfo.Jar,
                data:=New Byte() _
                      {0, 0, 0, 0,
                       116, 101, 115, 116, 0,
                       15, 0, 0, 0,
                       32, 0, 0, 0,
                       13, 0, 0, 0}.Concat(
                       sha1).ToArray,
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"map transfer key", 0},
                        {"path", "test"},
                        {"size", 15},
                        {"crc32", 32},
                        {"xoro checksum", 13},
                        {"sha1 checksum", sha1}
                    })
    End Sub
    <TestMethod()>
    Public Sub KnockTest()
        JarTest(Packets.Knock.Jar,
                data:={42, 0, 0, 0,
                       99, 0, 0, 0,
                       0,
                       &HE0, &H17,
                       16, 0, 0, 0,
                       116, 101, 115, 116, 0,
                       1, 0,
                       2, 0, &H17, &HE1, 127, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"game id", 42},
                        {"entry key", 99},
                        {"unknown value", 0},
                        {"listen port", 6112},
                        {"peer key", 16},
                        {"name", "test"},
                        {"peer data", New Byte() {0}.AsReadableList},
                        {"internal address", New Net.IPEndPoint(Net.IPAddress.Loopback, 6113)}
                    })
    End Sub
    <TestMethod()>
    Public Sub LanCreateGameTest()
        JarTest(Packets.LanCreateGame.Jar,
                data:={Asc("3"), Asc("r"), Asc("a"), Asc("w"),
                       20, 0, 0, 0,
                       42, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"product id", "war3"},
                        {"major version", 20},
                        {"game id", 42}
                    })
    End Sub
    <TestMethod()>
    Public Sub LanGameDetailsTest()
        JarTest(Packets.LanGameDetails.Jar,
                data:=New Byte() _
                      {Asc("3"), Asc("r"), Asc("a"), Asc("w"),
                       20, 0, 0, 0,
                       42, 0, 0, 0,
                       16, 0, 0, 0,
                       116, 101, 115, 116, 0,
                       0}.Concat(
                       New GameStatsJar("test").Pack(TestStats).Data).Concat({
                       12, 0, 0, 0,
                       8, 0, 0, 0,
                       2, 0, 0, 0,
                       12, 0, 0, 0,
                       25, 0, 0, 0,
                       &HE0, &H17}).ToArray,
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"product id", "war3"},
                        {"major version", 20},
                        {"game id", 42},
                        {"entry key", 16},
                        {"name", "test"},
                        {"password", ""},
                        {"statstring", TestStats},
                        {"num slots", 12},
                        {"game type", GameTypes.AuthenticatedMakerBlizzard},
                        {"num players + 1", 2},
                        {"free slots + 1", 12},
                        {"age", 25},
                        {"listen port", 6112}
                    })
    End Sub
    <TestMethod()>
    Public Sub LanDestroyGameTest()
        JarTest(Packets.LanDestroyGame.Jar,
                data:={20, 0, 0, 0},
                value:=20)
    End Sub
    <TestMethod()>
    Public Sub LanRefreshGameTest()
        JarTest(Packets.LanRefreshGame.Jar,
                data:={42, 0, 0, 0,
                       2, 0, 0, 0,
                       1, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"game id", 42},
                        {"num players", 2},
                        {"free slots", 1}
                    })
    End Sub
    <TestMethod()>
    Public Sub LanRequestGameTest()
        JarTest(Packets.LanRequestGame.Jar,
                data:={Asc("3"), Asc("r"), Asc("a"), Asc("w"),
                       20, 0, 0, 0,
                       0, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"product id", "war3"},
                        {"major version", 20},
                        {"unknown1", 0}
                    })
    End Sub
    <TestMethod()>
    Public Sub LeavingTest()
        JarTest(Packets.Leaving.Jar,
                data:={7, 0, 0, 0},
                value:=PlayerLeaveReason.Quit,
                equater:=Function(e1, e2) e1 = e2)
    End Sub
    <TestMethod()>
    Public Sub LobbyStateTest()
        Dim slots = New List(Of Slot)()
        JarTest(Packets.LobbyState.Jar,
                data:={7, 0,
                       0,
                       13, 0, 0, 0,
                       3,
                       12},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"slots", New List(Of Dictionary(Of InvariantString, Object))()},
                        {"random seed", 13},
                        {"layout style", LobbyLayoutStyle.FixedPlayerSettings},
                        {"num player slots", 12}
                    })
    End Sub
    <TestMethod()>
    Public Sub MapFileDataTest()
        JarTest(Packets.MapFileData.Jar,
                appendSafe:=False,
                data:={2,
                       3,
                       0, 0, 0, 0,
                       128, 0, 0, 0,
                       205, 251, 60, 182,
                       1, 2, 3, 4},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player index", 2},
                        {"sending player index", 3},
                        {"map transfer key", 0},
                        {"file position", 128},
                        {"file data", New Byte() {1, 2, 3, 4}.AsReadableList}
                    })
    End Sub
    <TestMethod()>
    Public Sub MapFileDataProblemTest()
        JarTest(Packets.MapFileDataProblem.Jar,
                data:={2,
                       3,
                       0, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"sender index", 2},
                        {"receiver index", 3},
                        {"map transfer key", 0}
                    })
    End Sub
    <TestMethod()>
    Public Sub MapFileDataReceivedTest()
        JarTest(Packets.MapFileDataReceived.Jar,
                data:={2,
                       3,
                       0, 0, 0, 0,
                       128, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"sender index", 2},
                        {"receiver index", 3},
                        {"map transfer key", 0},
                        {"total downloaded", 128}
                    })
    End Sub
    <TestMethod()>
    Public Sub NewHostTest()
        JarTest(Packets.NewHost.Jar,
                data:={1},
                value:=1)
    End Sub
    <TestMethod()>
    Public Sub NonGameActionTest()
        JarTest(Packets.NonGameAction.Jar,
                data:={3, 1, 2, 3,
                       4,
                       32,
                       1, 0, 0, 0,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player indexes", New Byte() {1, 2, 3}.AsReadableList},
                        {"sending player", 4},
                        {"command type", NonGameAction.GameChat},
                        {"receiving group", ChatGroup.Allies},
                        {"message", "test"}
                    })
        JarTest(Packets.NonGameAction.Jar,
                data:={3, 1, 2, 3,
                       4,
                       16,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player indexes", New Byte() {1, 2, 3}.AsReadableList},
                        {"sending player", 4},
                        {"command type", NonGameAction.LobbyChat},
                        {"message", "test"}
                    })
        JarTest(Packets.NonGameAction.Jar,
                data:={3, 1, 2, 3,
                       4,
                       17,
                       1},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player indexes", New Byte() {1, 2, 3}.AsReadableList},
                        {"sending player", 4},
                        {"command type", NonGameAction.SetTeam},
                        {"new value", 1}
                    })
        JarTest(Packets.NonGameAction.Jar,
                data:={3, 1, 2, 3,
                       4,
                       20,
                       100},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player indexes", New Byte() {1, 2, 3}.AsReadableList},
                        {"sending player", 4},
                        {"command type", NonGameAction.SetHandicap},
                        {"new value", 100}
                    })
        JarTest(Packets.NonGameAction.Jar,
                data:={3, 1, 2, 3,
                       4,
                       18,
                       1},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player indexes", New Byte() {1, 2, 3}.AsReadableList},
                        {"sending player", 4},
                        {"command type", NonGameAction.SetColor},
                        {"new value", Protocol.PlayerColor.Blue}
                    })
        JarTest(Packets.NonGameAction.Jar,
                data:={3, 1, 2, 3,
                       4,
                       19,
                       2},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player indexes", New Byte() {1, 2, 3}.AsReadableList},
                        {"sending player", 4},
                        {"command type", NonGameAction.SetRace},
                        {"new value", Protocol.Races.Orc}
                    })
    End Sub
    <TestMethod()>
    Public Sub OtherPlayerJoinedTestTest()
        JarTest(Packets.OtherPlayerJoined.Jar,
                data:={27, 0, 0, 0,
                       1,
                       116, 101, 115, 116, 0,
                       1, 42,
                       2, 0, &H17, &HE0, 127, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
                       2, 0, &H17, &HE1, 127, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"peer key", 27},
                        {"index", 1},
                        {"name", "test"},
                        {"peer data", New Byte() {42}.AsReadableList},
                        {"external address", New Net.IPEndPoint(Net.IPAddress.Loopback, 6112)},
                        {"internal address", New Net.IPEndPoint(Net.IPAddress.Loopback, 6113)}
                    })
    End Sub
    <TestMethod()>
    Public Sub OtherPlayerLeftTest()
        JarTest(Packets.OtherPlayerLeft.Jar,
                data:={1,
                       7, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player index", 1},
                        {"reason", PlayerLeaveReason.Quit}
                    })
    End Sub
    <TestMethod()>
    Public Sub OtherPlayerReadyTest()
        JarTest(Packets.OtherPlayerReady.Jar,
                data:={3},
                value:=3)
    End Sub
    <TestMethod()>
    Public Sub PeerConnectionInfoTest()
        JarTest(Packets.PeerConnectionInfo.Jar,
                data:={7, 0},
                value:=7)
    End Sub
    <TestMethod()>
    Public Sub PeerKnockTest()
        JarTest(Packets.PeerKnock.Jar,
                data:={42, 0, 0, 0,
                       0, 0, 0, 0,
                       1,
                       0,
                       7, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiver peer key", 42},
                        {"unknown1", 0},
                        {"sender player id", 1},
                        {"unknown3", 0},
                        {"sender peer connection flags", 7}
                    })
    End Sub
    <TestMethod()>
    Public Sub PeerPingTest()
        JarTest(Packets.PeerPing.Jar,
                data:={&HEF, &HBE, &HAD, &HDE,
                       7, 0, 0, 0,
                       1, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"salt", &HDEADBEEFUI},
                        {"sender peer connection flags", 7},
                        {"unknown2", 1}
                    })
    End Sub
    <TestMethod()>
    Public Sub PeerPongTest()
        JarTest(Packets.PeerPong.Jar,
                data:={&HEF, &HBE, &HAD, &HDE},
                value:=&HDEADBEEFUI)
    End Sub
    <TestMethod()>
    Public Sub PingTest()
        JarTest(Packets.Ping.Jar,
                data:={&HEF, &HBE, &HAD, &HDE},
                value:=&HDEADBEEFUI)
    End Sub
    <TestMethod()>
    Public Sub PongTest()
        JarTest(Packets.Pong.Jar,
                data:={&HEF, &HBE, &HAD, &HDE},
                value:=&HDEADBEEFUI)
    End Sub
    <TestMethod()>
    Public Sub ReadyTest()
        EmptyJarTest(Packets.Ready.Jar)
    End Sub
    <TestMethod()>
    Public Sub RejectEntryTest()
        JarTest(Packets.RejectEntry.Jar,
                data:={27, 0, 0, 0},
                value:=RejectReason.IncorrectPassword,
                equater:=Function(e1, e2) e1 = e2)
    End Sub
    <TestMethod()>
    Public Sub RemovePlayerFromLagScreenTest()
        JarTest(Packets.RemovePlayerFromLagScreen.Jar,
                data:={4,
                       23, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player index", 4},
                        {"marginal milliseconds used", 23}
                    })
    End Sub
    <TestMethod()>
    Public Sub RequestDropLaggersTest()
        EmptyJarTest(Packets.RequestDropLaggers.Jar)
    End Sub
    <TestMethod()>
    Public Sub SetDownloadSourceTest()
        JarTest(Packets.SetDownloadSource.Jar,
                data:={0, 0, 0, 0,
                       2},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"map transfer key", 0},
                        {"sending player index", 2}
                    })
    End Sub
    <TestMethod()>
    Public Sub SetUploadTargetTest()
        JarTest(Packets.SetUploadTarget.Jar,
                data:={0, 0, 0, 0,
                       3,
                       128, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"map transfer key", 0},
                        {"receiving player index", 3},
                        {"starting file pos", 128}
                    })
    End Sub
    <TestMethod()>
    Public Sub ShowLagScreenTest()
        Dim lagger = New Dictionary(Of InvariantString, Object) From {
                             {"player index", 2},
                             {"initial milliseconds used", 25}
                         }
        JarTest(Packets.ShowLagScreen.Jar,
                equater:=Function(e1 As IList(Of Dictionary(Of InvariantString, Object)), e2 As IList(Of Dictionary(Of InvariantString, Object))) ObjectEqual(e1, e2),
                data:={2,
                       2, 25, 0, 0, 0,
                       2, 25, 0, 0, 0},
                value:={lagger, lagger})
    End Sub
    <TestMethod()>
    Public Sub StartCountdownTest()
        EmptyJarTest(Packets.StartCountdown.Jar)
    End Sub
    <TestMethod()>
    Public Sub StartLoadingTest()
        EmptyJarTest(Packets.StartLoading.Jar)
    End Sub
    <TestMethod()>
    Public Sub TextTest()
        JarTest(Packets.Text.Jar,
                data:={2, 2, 3,
                       1,
                       32,
                       1, 0, 0, 0,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving players", New List(Of Byte) From {2, 3}},
                        {"sending player index", 1},
                        {"type", ChatType.Game},
                        {"receiving group", ChatGroup.Allies},
                        {"message", "test"}
                    })
        JarTest(Packets.Text.Jar,
                data:={2, 2, 3,
                       1,
                       16,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving players", New List(Of Byte) From {2, 3}},
                        {"sending player index", 1},
                        {"type", ChatType.Lobby},
                        {"message", "test"}
                    })
    End Sub
    <TestMethod()>
    Public Sub TickTest()
        JarTest(Packets.Tick.Jar,
                appendSafe:=False,
                equater:=Function(e1 As Dictionary(Of InvariantString, Object), e2 As Dictionary(Of InvariantString, Object))
                             If e1.Count <> 2 Then Return False
                             If e2.Count <> 2 Then Return False
                             If Not ObjectEqual(e1("time span"), e2("time span")) Then Return False
                             Dim a1 = CType(e1("player action sets"), Tuple(Of Boolean, IReadableList(Of PlayerActionSet)))
                             Dim a2 = CType(e2("player action sets"), Tuple(Of Boolean, IReadableList(Of PlayerActionSet)))
                             If Not ObjectEqual(a1.Item1, a2.Item1) Then Return False
                             If Not ObjectEqual(a1.Item2, a2.Item2) Then Return False
                             Return True
                         End Function,
                data:={250, 0,
                       208, 15,
                            1,
                                6, 0,
                                39,
                                2,
                                100, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"time span", 250},
                        {"player action sets", Tuple(True, {New PlayerActionSet(New PlayerID(1),
                                           {GameAction.FromValue(GameActions.CheatGold,
                                                                 New Dictionary(Of InvariantString, Object) From {
                                                                     {"amount", 100},
                                                                     {"unknown", 2}})
                                            }.AsReadableList)
                                     }.AsReadableList)}
                    })
        JarTest(Packets.Tick.Jar,
                appendSafe:=False,
                data:={100, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"time span", 100},
                        {"player action sets", Tuple(False, CType(Nothing, IReadableList(Of PlayerActionSet)))}})
    End Sub
    <TestMethod()>
    Public Sub TockTest()
        JarTest(Packets.Tock.Jar,
                data:={1, 2, 3, 4, 5},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", 1},
                        {"game state checksum", &H5040302UI}
                    })
    End Sub
End Class
