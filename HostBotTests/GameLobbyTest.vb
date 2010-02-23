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
    Private Shared ReadOnly TestOpenObsSlot As Slot = TestOpenSlot.WithTeam(Slot.ObserverTeamIndex).WithColor(Protocol.PlayerColor.Observer)
    Private Shared ReadOnly TestPlayerObsSlot As Slot = TestPlayerSlot.WithTeam(Slot.ObserverTeamIndex).WithColor(Protocol.PlayerColor.Observer)

    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_Open()
        Dim slotSet = New SlotSet({TestOpenSlot.WithContents(New SlotContentsClosed)})
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
                                   TestOpenSlot.WithIndex(1),
                                   TestOpenSlot.WithIndex(2).WithTeam(1),
                                   TestOpenSlot.WithIndex(3).WithTeam(1)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {1, 1})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsOpen)
        Assert.IsTrue(TypeOf result(1).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(2).Contents Is SlotContentsOpen)
        Assert.IsTrue(TypeOf result(3).Contents Is SlotContentsClosed)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_PlayersStay()
        Dim slotSet = New SlotSet({TestPlayerSlot,
                                   TestOpenSlot.WithIndex(1),
                                   TestOpenSlot.WithIndex(2).WithTeam(1),
                                   TestPlayerSlot.WithIndex(3).WithTeam(1)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {1, 1})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf result(1).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(2).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(3).Contents Is SlotContentsPlayer)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_PlayerMove()
        Dim slotSet = New SlotSet({TestPlayerSlot,
                                   TestOpenSlot.WithIndex(1),
                                   TestOpenSlot.WithIndex(2).WithTeam(1),
                                   TestOpenSlot.WithIndex(3).WithTeam(1)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {0, 1})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(1).Contents Is SlotContentsClosed)
        Assert.IsTrue(TypeOf result(2).Contents Is SlotContentsPlayer)
        Assert.IsTrue(TypeOf result(3).Contents Is SlotContentsClosed)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_ObsStay()
        Dim slotSet = New SlotSet({TestOpenSlot,
                                   TestPlayerObsSlot.WithIndex(1)})
        Dim result = GameLobby.SetupTeamSizesCustomForces(slotSet, {1})
        Assert.IsTrue(TypeOf result(0).Contents Is SlotContentsOpen)
        Assert.IsTrue(TypeOf result(1).Contents Is SlotContentsPlayer)
    End Sub
    <TestMethod()>
    Public Sub SetupTeamSizesCustomForcesTest_Large()
        Dim slotSet = New SlotSet({TestPlayerSlot,
                                   TestOpenSlot.WithIndex(1),
                                   TestPlayerSlot.WithIndex(2),
                                   TestPlayerSlot.WithIndex(3),
                                   TestOpenSlot.WithIndex(4),
                                   TestPlayerSlot.WithTeam(1).WithIndex(5),
                                   TestOpenSlot.WithTeam(1).WithIndex(6),
                                   TestPlayerSlot.WithTeam(1).WithIndex(7),
                                   TestOpenSlot.WithTeam(1).WithIndex(8),
                                   TestOpenSlot.WithTeam(1).WithIndex(9),
                                   TestOpenObsSlot.WithIndex(10),
                                   TestPlayerObsSlot.WithIndex(11)})
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
