Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Strilbrary.Values
Imports Tinker.WC3

<TestClass()>
Public Class GameLobbyTest
    Private Shared ReadOnly TestPlayerSlot As New Slot(index:=0,
                                                       contents:=New SlotContentsPlayer(TestPlayer),
                                                       color:=Protocol.PlayerColor.Red,
                                                       raceUnlocked:=True,
                                                       team:=0)
    Private Shared ReadOnly TestOpenSlot As New Slot(index:=0,
                                                     contents:=New SlotContentsOpen,
                                                     color:=Protocol.PlayerColor.Red,
                                                     raceUnlocked:=True,
                                                     team:=0)
    Private Shared ReadOnly TestOpenObsSlot As Slot = TestOpenSlot.With(team:=Slot.ObserverTeamIndex,
                                                                        color:=Protocol.PlayerColor.Observer)
    Private Shared ReadOnly TestPlayerObsSlot As Slot = TestPlayerSlot.With(team:=Slot.ObserverTeamIndex,
                                                                            color:=Protocol.PlayerColor.Observer)

    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_Open()
        Dim slotSet = New SlotSet({TestOpenSlot.With(contents:=New SlotContentsClosed)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {1})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsOpen)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_Close()
        Dim slotSet = New SlotSet({TestOpenSlot})
        Dim result = GameLobby.SetupTeamSizesCustomForces(SlotSet, {0})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsClosed)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_Player()
        Dim slotSet = New SlotSet({TestPlayerSlot})
        Assert.IsTrue(TypeOf GameLobby.SetupTeamSizesCustomForces(slotSet, {0})(0).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf GameLobby.SetupTeamSizesCustomForces(slotSet, {1})(0).Contents Is SlotContentsPlayer)
    End Sub

    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_2to1()
        Dim slotSet = New SlotSet({TestOpenSlot,
                                   TestOpenSlot.With(index:=1),
                                   TestOpenSlot.With(index:=2, team:=1),
                                   TestOpenSlot.With(index:=3, team:=1)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {1, 1})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsOpen)
        Assert.IsTrue(TypeOf result(1).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(2).Contents Is SlotContentsOpen)
        Assert.IsTrue(TypeOf result(3).Contents Is SlotContentsClosed)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_PlayersStay()
        Dim slotSet = New SlotSet({TestPlayerSlot,
                                   TestOpenSlot.With(index:=1),
                                   TestOpenSlot.With(index:=2, team:=1),
                                   TestPlayerSlot.With(index:=3, team:=1)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {1, 1})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf result(1).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(2).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(3).Contents Is SlotContentsPlayer)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_PlayerMove()
        Dim slotSet = New SlotSet({TestPlayerSlot,
                                   TestOpenSlot.With(index:=1),
                                   TestOpenSlot.With(index:=2, team:=1),
                                   TestOpenSlot.With(index:=3, team:=1)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {0, 1})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(1).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(2).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf result(3).Contents Is SlotContentsClosed)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_ObsStay()
        Dim slotSet = New SlotSet({TestOpenSlot,
                                   TestPlayerObsSlot.With(index:=1)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {1})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsOpen)
        Assert.IsTrue(TypeOf result(1).Contents Is SlotContentsPlayer)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_Large()
        Dim slotSet = New SlotSet({TestPlayerSlot,
                                   TestOpenSlot.With(index:=1),
                                   TestPlayerSlot.With(index:=2),
                                   TestPlayerSlot.With(index:=3),
                                   TestOpenSlot.With(index:=4),
                                   TestPlayerSlot.With(team:=1, index:=5),
                                   TestOpenSlot.With(team:=1, index:=6),
                                   TestPlayerSlot.With(team:=1, index:=7),
                                   TestOpenSlot.With(team:=1, index:=8),
                                   TestOpenSlot.With(team:=1, index:=9),
                                   TestOpenObsSlot.With(index:=10),
                                   TestPlayerObsSlot.With(index:=11)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {2, 5})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf result(1).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(2).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf result(3).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(4).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(5).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf result(6).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf result(7).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf result(8).Contents Is SlotContentsOpen)
        Assert.IsTrue(TypeOf result(9).Contents Is SlotContentsOpen)
        Assert.IsTrue(TypeOf result(10).Contents Is SlotContentsOpen)
        Assert.IsTrue(TypeOf result(11).Contents Is SlotContentsPlayer)
    End Sub
End Class
