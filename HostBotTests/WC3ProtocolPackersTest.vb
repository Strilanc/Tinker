Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Strilbrary.Values
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker.WC3
Imports Tinker
Imports Tinker.WC3.Protocol

<TestClass()>
Public Class WC3ProtocolPackersTest

    <TestMethod()>
    Public Sub MakeShowLagScreenTest()
        MakeShowLagScreen({New PID(1)})
    End Sub
    <TestMethod()>
    Public Sub MakeRemovePlayerFromLagScreenTest()
        MakeRemovePlayerFromLagScreen(New PID(1), 100)
    End Sub
    <TestMethod()>
    Public Sub MakeTextTest()
        MakeText("test", ChatType.Lobby, ChatReceiverType.Allies, {}, New PID(1))
        MakeText("test", ChatType.Game, ChatReceiverType.Allies, {}, New PID(1))
    End Sub
    <TestMethod()>
    Public Sub MakeGreetTest()
        MakeGreet(New Net.IPEndPoint(Net.IPAddress.Loopback, 6112), New PID(1))
    End Sub
    <TestMethod()>
    Public Sub MakeRejectTest()
        MakeReject(RejectReason.GameAlreadyStarted)
    End Sub
    <TestMethod()>
    Public Sub MakeHostMapInfoTest()
        MakeHostMapInfo(map:=TestMap, mapTransferKey:=1)
    End Sub
    <TestMethod()>
    Public Sub MakeOtherPlayerJoinedTest()
        MakeOtherPlayerJoined("test",
                              New PID(1),
                              1,
                              New Byte() {0}.AsReadableList,
                              New Net.IPEndPoint(Net.IPAddress.Loopback, 6112))
    End Sub
    <TestMethod()>
    Public Sub MakePingTest()
        MakePing(0)
    End Sub
    <TestMethod()>
    Public Sub MakeOtherPlayerReadyTest()
        MakeOtherPlayerReady(New PID(1))
    End Sub
    <TestMethod()>
    Public Sub MakeOtherPlayerLeftTest()
        MakeOtherPlayerLeft(New PID(1), PlayerLeaveType.Disconnect)
    End Sub
    <TestMethod()>
    Public Sub MakeLobbyStateTest()
        MakeLobbyState(TestPlayer, TestMap, New List(Of WC3.Slot)(), 0)
    End Sub
    <TestMethod()>
    Public Sub MakeStartCountdownTest()
        MakeStartCountdown()
    End Sub
    <TestMethod()>
    Public Sub MakeStartLoadingTest()
        MakeStartLoading()
    End Sub
    <TestMethod()>
    Public Sub MakeHostConfirmHostLeavingTest()
        MakeHostConfirmHostLeaving()
    End Sub
    <TestMethod()>
    Public Sub MakeTickTest()
        MakeTick(250, {New PlayerActionSet(New PID(1),
                                           {GameAction.FromValue(GameActionId.CheatGold,
                                                                 GameActions.CheatGold,
                                                                 New Dictionary(Of InvariantString, Object) From {
                                                                     {"amount", 100},
                                                                     {"unknown", 0}})
                                            }.AsReadableList)
                       }.AsReadableList)
    End Sub
    <TestMethod()>
    Public Sub MakeMapFileDataTest()
        MakeMapFileData(5, New Byte() {}.AsReadableList, New PID(1), New PID(2))
    End Sub
    <TestMethod()>
    Public Sub MakeSetUploadTargetTest()
        MakeSetUploadTarget(New PID(1), 0)
    End Sub
    <TestMethod()>
    Public Sub MakeSetDownloadSourceTest()
        MakeSetDownloadSource(New PID(1))
    End Sub
    <TestMethod()>
    Public Sub MakeClientMapInfoTest()
        MakeClientMapInfo(WC3.Protocol.MapTransferState.Downloading, totalDownloaded:=100, mapTransferKey:=1)
    End Sub
    <TestMethod()>
    Public Sub MakeMapFileDataReceivedTest()
        MakeMapFileDataReceived(New PID(1), New PID(2), 0)
    End Sub
    <TestMethod()>
    Public Sub MakeLanCreateGameTest()
        MakeLanCreateGame(20, 20)
    End Sub
    <TestMethod()>
    Public Sub MakeLanRefreshGameTest()
        MakeLanRefreshGame(20, TestDesc)
    End Sub
    <TestMethod()>
    Public Sub MakeLanDescribeGameTest()
        MakeLanGameDetails(20, TestDesc)
    End Sub
    <TestMethod()>
    Public Sub MakeLanDestroyGameTest()
        MakeLanDestroyGame(20)
    End Sub
    <TestMethod()>
    Public Sub MakeKnockTest()
        MakeKnock("test", 6112, 2116)
    End Sub
    <TestMethod()>
    Public Sub MakeReadyTest()
        MakeReady()
    End Sub
    <TestMethod()>
    Public Sub MakePongTest()
        MakePong(20)
    End Sub
    <TestMethod()>
    Public Sub MakeTockTest()
        MakeTock(0, 0)
    End Sub
    <TestMethod()>
    Public Sub MakePeerConnectionInfoTest()
        MakePeerConnectionInfo({New PID(1), New PID(2)})
    End Sub
    <TestMethod()>
    Public Sub MakePeerKnockTest()
        MakePeerKnock(20, New PID(1), {New PID(1), New PID(3)})
    End Sub
    <TestMethod()>
    Public Sub MakePeerPingTest()
        MakePeerPing(42, {New PID(1), New PID(3)})
    End Sub
    <TestMethod()>
    Public Sub MakePeerPongTest()
        MakePeerPong(42)
    End Sub
End Class
