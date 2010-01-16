Imports Strilbrary.Values
Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker.Pickling
Imports Tinker.WC3
Imports Tinker.WC3.Protocol
Imports TinkerTests.PicklingTest

<TestClass()>
Public Class WC3ProtocolTest
    <TestMethod()>
    Public Sub TestClientConfirmHostLeavingTest()
        JarTest(Jars.ClientConfirmHostLeaving,
                data:={},
                value:=New Dictionary(Of InvariantString, Object) From {})
    End Sub
    <TestMethod()>
    Public Sub TestClientMapInfo()
        JarTest(Jars.ClientMapInfo,
                data:={1, 0, 0, 0,
                       3,
                       128, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", 1},
                        {"dl state", DownloadState.Downloading},
                        {"total downloaded", 128}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestGameAction()
        JarTest(Jars.GameAction,
                appendSafe:=False,
                data:={32, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"crc32", 32},
                        {"actions", New List(Of GameAction)()}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestGreet()
        JarTest(Jars.Greet,
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
    Public Sub TestHostConfirmHostLeaving()
        JarTest(Jars.HostConfirmHostLeaving,
                data:={},
                value:=New Dictionary(Of InvariantString, Object) From {})
    End Sub
    <TestMethod()>
    Public Sub TestHostMapInfo()
        Dim sha1 = (From i In Enumerable.Range(0, 20)
                    Select CByte(i)).ToArray.AsReadableList
        JarTest(Jars.HostMapInfo,
                data:=New Byte() _
                      {0, 0, 0, 0,
                       116, 101, 115, 116, 0,
                       15, 0, 0, 0,
                       32, 0, 0, 0,
                       13, 0, 0, 0}.Concat(
                       sha1).ToArray,
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", 0},
                        {"path", "test"},
                        {"size", 15},
                        {"crc32", 32},
                        {"xoro checksum", 13},
                        {"sha1 checksum", sha1}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestKnock()
        JarTest(Jars.Knock,
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
                        {"unknown data", New Byte() {0}.AsReadableList},
                        {"internal address", New Net.IPEndPoint(Net.IPAddress.Loopback, 6113)}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestLanCreateGame()
        JarTest(Jars.LanCreateGame,
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
    Public Sub TestLanDescribeGame()
        JarTest(Jars.LanDescribeGame,
                data:=New Byte() _
                      {Asc("3"), Asc("r"), Asc("a"), Asc("w"),
                       20, 0, 0, 0,
                       42, 0, 0, 0,
                       16, 0, 0, 0,
                       116, 101, 115, 116, 0,
                       0}.Concat(
                       New GameStatsJar("test").Pack(BnetProtocolTest.TestStats).Data).Concat({
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
                        {"statstring", BnetProtocolTest.TestStats},
                        {"num slots", 12},
                        {"game type", GameTypes.AuthenticatedMakerBlizzard},
                        {"num players + 1", 2},
                        {"free slots + 1", 12},
                        {"age", 25},
                        {"listen port", 6112}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestLanDestroyGame()
        JarTest(Jars.LanDestroyGame,
                data:={20, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"game id", 20}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestLanRefreshGame()
        JarTest(Jars.LanRefreshGame,
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
    Public Sub TestLanRequestGame()
        JarTest(Jars.LanRequestGame,
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
    Public Sub TestLeaving()
        JarTest(Jars.Leaving,
                data:={7, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"leave type", PlayerLeaveType.Lose}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestLobbyState()
        Dim slots = New List(Of Slot)()
        JarTest(Jars.LobbyState,
                data:={7, 0,
                       0,
                       13, 0, 0, 0,
                       1,
                       12},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"state size", 7},
                        {"slots", New List(Of Dictionary(Of InvariantString, Object))()},
                        {"time", 13},
                        {"layout style", 1},
                        {"num player slots", 12}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestMapFileData()
        JarTest(Jars.MapFileData,
                appendSafe:=False,
                requireAllData:=False,
                data:={2,
                       3,
                       0, 0, 0, 0,
                       128, 0, 0, 0,
                       32, 0, 0, 0,
                       1, 2, 3, 4},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player index", 2},
                        {"sending player index", 3},
                        {"unknown", 0},
                        {"file position", 128},
                        {"crc32", 32},
                        {"file data", New Byte() {1, 2, 3, 4}.AsReadableList}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestMapFileDataProblem()
        JarTest(Jars.MapFileDataProblem,
                data:={2,
                       3,
                       0, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"sender index", 2},
                        {"receiver index", 3},
                        {"unknown", 0}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestMapFileDataReceived()
        JarTest(Jars.MapFileDataReceived,
                data:={2,
                       3,
                       0, 0, 0, 0,
                       128, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"sender index", 2},
                        {"receiver index", 3},
                        {"unknown", 0},
                        {"total downloaded", 128}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestNewHost()
        JarTest(Jars.NewHost,
                data:={1},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player index", 1}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestNonGameAction()
        JarTest(Jars.NonGameAction,
                data:={3, 1, 2, 3,
                       4,
                       32,
                       1, 0, 0, 0,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player indexes", New Byte() {1, 2, 3}.AsReadableList},
                        {"sending player", 4},
                        {"command type", NonGameAction.GameChat},
                        {"receiver type", ChatReceiverType.Allies},
                        {"message", "test"}
                    })
        JarTest(Jars.NonGameAction,
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
        JarTest(Jars.NonGameAction,
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
        JarTest(Jars.NonGameAction,
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
        JarTest(Jars.NonGameAction,
                data:={3, 1, 2, 3,
                       4,
                       18,
                       1},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player indexes", New Byte() {1, 2, 3}.AsReadableList},
                        {"sending player", 4},
                        {"command type", NonGameAction.SetColor},
                        {"new value", Slot.PlayerColor.Blue}
                    })
        JarTest(Jars.NonGameAction,
                data:={3, 1, 2, 3,
                       4,
                       19,
                       2},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"receiving player indexes", New Byte() {1, 2, 3}.AsReadableList},
                        {"sending player", 4},
                        {"command type", NonGameAction.SetRace},
                        {"new value", Slot.Races.Orc}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestOtherPlayerJoined()
        JarTest(Jars.OtherPlayerJoined,
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
                        {"unknown data", New Byte() {42}.AsReadableList},
                        {"external address", New Net.IPEndPoint(Net.IPAddress.Loopback, 6112)},
                        {"internal address", New Net.IPEndPoint(Net.IPAddress.Loopback, 6113)}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestOtherPlayerLeft()
        JarTest(Jars.OtherPlayerLeft,
                data:={1,
                       7, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player index", 1},
                        {"leave type", PlayerLeaveType.Lose}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestOtherPlayerReady()
        JarTest(Jars.OtherPlayerReady,
                data:={3},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player index", 3}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestPeerConnectionInfo()
        JarTest(Jars.PeerConnectionInfo,
                data:={7, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player bitflags", 7}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestPeerKnock()
        JarTest(Jars.PeerKnock,
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
    Public Sub TestPeerPing()
        JarTest(Jars.PeerPing,
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
    Public Sub TestPeerPong()
        JarTest(Jars.PeerPong,
                data:={&HEF, &HBE, &HAD, &HDE},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"salt", &HDEADBEEFUI}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestPing()
        JarTest(Jars.Ping,
                data:={&HEF, &HBE, &HAD, &HDE},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"salt", &HDEADBEEFUI}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestPong()
        JarTest(Jars.Pong,
                data:={&HEF, &HBE, &HAD, &HDE},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"salt", &HDEADBEEFUI}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestReady()
        JarTest(Jars.Ready,
                data:={},
                value:=New Dictionary(Of InvariantString, Object) From {})
    End Sub
    <TestMethod()>
    Public Sub TestRejectEntry()
        JarTest(Jars.RejectEntry,
                data:={27, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"reason", RejectReason.IncorrectPassword}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestRemovePlayerFromLagScreen()
        JarTest(Jars.RemovePlayerFromLagScreen,
                data:={4,
                       23, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player index", 4},
                        {"marginal milliseconds used", 23}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestRequestDropLaggers()
        JarTest(Jars.RequestDropLaggers,
                data:={},
                value:=New Dictionary(Of InvariantString, Object) From {})
    End Sub
    <TestMethod()>
    Public Sub TestSetDownloadSource()
        JarTest(Jars.SetDownloadSource,
                data:={0, 0, 0, 0,
                       2},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", 0},
                        {"sending player index", 2}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestSetUploadTarget()
        JarTest(Jars.SetUploadTarget,
                data:={0, 0, 0, 0,
                       3,
                       128, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown1", 0},
                        {"receiving player index", 3},
                        {"starting file pos", 128}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestShowLagScreen()
        Dim lagger = New Dictionary(Of InvariantString, Object) From {
                             {"player index", 2},
                             {"initial milliseconds used", 25}
                         }
        JarTest(Jars.ShowLagScreen,
                data:={2,
                       2, 25, 0, 0, 0,
                       2, 25, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"laggers", {lagger, lagger}}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestStartCountdown()
        JarTest(Jars.StartCountdown,
                data:={},
                value:=New Dictionary(Of InvariantString, Object) From {})
    End Sub
    <TestMethod()>
    Public Sub TestStartLoading()
        JarTest(Jars.StartLoading,
                data:={},
                value:=New Dictionary(Of InvariantString, Object) From {})
    End Sub
    <TestMethod()>
    Public Sub TestTick()
        JarTest(Jars.Tick,
                requireAllData:=False,
                appendSafe:=False,
                data:={250, 0,
                       1, 2, 3, 4},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"time span", 250},
                        {"subpacket", New Byte() {1, 2, 3, 4}.AsReadableList}
                    })
    End Sub
    <TestMethod()>
    Public Sub TestTock()
        JarTest(Jars.Tock,
                data:={1, 2, 3, 4, 5},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"game state checksum", New Byte() {1, 2, 3, 4, 5}.AsReadableList}
                    })
    End Sub
End Class
