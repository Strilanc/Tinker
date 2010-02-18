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
Public Class GameActionProtocolTest
    <TestMethod()>
    Public Sub AssignGroupHotkeyTest()
        JarTest(GameActions.AssignGroupHotkey.Jar,
                data:={1,
                       1, 0,
                       1, 0, 0, 0, 2, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"group index", 1},
                        {"targets", New List(Of GameObjectId)({New GameObjectId(1, 2)})}
                    })
    End Sub
    <TestMethod()>
    Public Sub CancelHeroReviveTest()
        JarTest(GameActions.CancelHeroRevive.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0},
                value:=New GameObjectId(2, 3))
    End Sub
    <TestMethod()>
    Public Sub ChangeAllyOptionsTest()
        JarTest(GameActions.ChangeAllyOptions.Jar,
                data:={1,
                       1 << 5, 1 << (10 - 8), 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player slot id", 1},
                        {"flags", AllianceTypes.AlliedVictory Or AllianceTypes.SharedVision}
                    })
    End Sub
    <TestMethod()>
    Public Sub ChangeSelectionTest()
        JarTest(GameActions.ChangeSelection.Jar,
                data:={2,
                       1, 0,
                       1, 0, 0, 0, 2, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"operation", SelectionOperation.Remove},
                        {"targets", New List(Of GameObjectId)({New GameObjectId(1, 2)})}
                    })
    End Sub
    <TestMethod()>
    Public Sub CheatDisableTechRequirementsTest()
        EmptyJarTest(GameActions.CheatDisableTechRequirements.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatDisableVictoryConditionsTest()
        EmptyJarTest(GameActions.CheatDisableVictoryConditions.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatEnableResearchTest()
        EmptyJarTest(GameActions.CheatEnableResearch.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatFastCooldownTest()
        EmptyJarTest(GameActions.CheatFastCooldown.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatFastDeathDecayTest()
        EmptyJarTest(GameActions.CheatFastDeathDecay.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatGodModeTest()
        EmptyJarTest(GameActions.CheatGodMode.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatGoldTest()
        JarTest(GameActions.CheatGold.Jar,
                data:={0,
                       100, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", 0},
                        {"amount", 100}
                    })
    End Sub
    <TestMethod()>
    Public Sub CheatGoldAndLumberTest()
        JarTest(GameActions.CheatGoldAndLumber.Jar,
                data:={0,
                       100, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", 0},
                        {"amount", 100}
                    })
    End Sub
    <TestMethod()>
    Public Sub CheatInstantDefeatTest()
        EmptyJarTest(GameActions.CheatInstantDefeat.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatInstantVictoryTest()
        EmptyJarTest(GameActions.CheatInstantVictory.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatLumberTest()
        JarTest(GameActions.CheatLumber.Jar,
                data:={0,
                       100, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", 0},
                        {"amount", 100}
                    })
    End Sub
    <TestMethod()>
    Public Sub CheatNoDefeatTest()
        EmptyJarTest(GameActions.CheatNoDefeat.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatNoFoodLimitTest()
        EmptyJarTest(GameActions.CheatNoFoodLimit.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatRemoveFogOfWarTest()
        EmptyJarTest(GameActions.CheatRemoveFogOfWar.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatResearchUpgradesTest()
        EmptyJarTest(GameActions.CheatResearchUpgrades.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatSetTimeOfDayTest()
        JarTest(GameActions.CheatSetTimeOfDay.Jar,
                data:=BitConverter.GetBytes(CSng(12.0)),
                value:=CSng(12.0))
    End Sub
    <TestMethod()>
    Public Sub CheatSpeedConstructionTest()
        EmptyJarTest(GameActions.CheatSpeedConstruction.Jar)
    End Sub
    <TestMethod()>
    Public Sub CheatUnlimitedManaTest()
        EmptyJarTest(GameActions.CheatUnlimitedMana.Jar)
    End Sub
    <TestMethod()>
    Public Sub DecreaseGameSpeedTest()
        EmptyJarTest(GameActions.DecreaseGameSpeed.Jar)
    End Sub
    <TestMethod()>
    Public Sub DequeueBuildingOrderTest()
        JarTest(GameActions.DequeueBuildingOrder.Jar,
                data:={1,
                       &HFE, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"slot number", 1},
                        {"type", &HFE}
                    })
    End Sub
    <TestMethod()>
    Public Sub DropOrGiveItemTest()
        JarTest(GameActions.DropOrGiveItem.Jar,
                data:=New Byte() _
                      {1, 0,
                       3, 0, &HD, 0,
                       2, 0, 0, 0, 3, 0, 0, 0}.Concat(
                       BitConverter.GetBytes(CSng(5.0))).Concat(
                       BitConverter.GetBytes(CSng(6.0))).Concat({
                       3, 0, 0, 0, 2, 0, 0, 0,
                       5, 0, 0, 0, 8, 0, 0, 0}).ToArray,
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"flags", OrderTypes.Queue},
                        {"order", OrderId.Smart},
                        {"unknown", New GameObjectId(2, 3)},
                        {"x", CSng(5.0)},
                        {"y", CSng(6.0)},
                        {"receiver", New GameObjectId(3, 2)},
                        {"item", New GameObjectId(5, 8)}
                    })
    End Sub
    <TestMethod()>
    Public Sub EnterChooseBuildingSubmenuTest()
        EmptyJarTest(GameActions.EnterChooseBuildingSubmenu.Jar)
    End Sub
    <TestMethod()>
    Public Sub EnterChooseHeroSkillSubmenuTest()
        EmptyJarTest(GameActions.EnterChooseHeroSkillSubmenu.Jar)
    End Sub
    <TestMethod()>
    Public Sub FogObjectOrderTest()
        JarTest(GameActions.FogObjectOrder.Jar,
                data:=New Byte() _
                      {1, 0,
                       3, 0, &HD, 0,
                       2, 0, 0, 0, 3, 0, 0, 0}.Concat(
                       BitConverter.GetBytes(CSng(5.0))).Concat(
                       BitConverter.GetBytes(CSng(6.0))).Concat({
                       &HED, &HFE, 0, 0,
                       1, 0, 0, 0, 0, 0, 0, 0,
                       2}).Concat(
                       BitConverter.GetBytes(CSng(7.0))).Concat(
                       BitConverter.GetBytes(CSng(8.0))).ToArray,
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"flags", OrderTypes.Queue},
                        {"order", OrderId.Smart},
                        {"unknown", New GameObjectId(2, 3)},
                        {"x", CSng(5.0)},
                        {"y", CSng(6.0)},
                        {"target type", &HFEED},
                        {"target flags", 1},
                        {"target owner", 2},
                        {"target x", CSng(7.0)},
                        {"target y", CSng(8.0)}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncBooleanTest()
        JarTest(GameActions.GameCacheSyncBoolean.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0,
                       1, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"},
                        {"value", 1}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncIntegerTest()
        JarTest(GameActions.GameCacheSyncInteger.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0,
                       1, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"},
                        {"value", 1}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncRealTest()
        JarTest(GameActions.GameCacheSyncReal.Jar,
                data:=New Byte() _
                      {116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0}.Concat(
                       BitConverter.GetBytes(CSng(1))).ToArray,
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"},
                        {"value", CSng(1)}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncStringTest()
        JarTest(GameActions.GameCacheSyncString.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0,
                       101, 115, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"},
                        {"value", "es"}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncUnitTest()
        JarTest(GameActions.GameCacheSyncUnit.Jar,
                data:=New Byte() {
                       48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 0,
                       104, 0,
                       117, 0,
                       97, 108, 98, 79,
                       6, 0, 0, 0,
                           102, 116, 97, 114,
                           0, 0, 0, 0,
                           0, 53, 0, 0,
                           103, 110, 107, 99,
                           0, 0, 0, 0,
                           0, 53, 0, 0,
                           99, 115, 101, 100,
                           0, 0, 0, 0,
                           0, 55, 0, 0,
                           116, 100, 111, 109,
                           0, 0, 0, 0,
                           0, 53, 0, 0,
                           111, 114, 102, 111,
                           0, 0, 0, 0,
                           0, 53, 0, 0,
                           0, 0, 0, 0,
                           0, 0, 0, 0,
                           0, 0, 0, 0,
                       140, 10, 0, 0,
                       6, 0, 0, 0,
                       2, 0, 0, 0,
                       7, 0, 1, 0,
                       18, 0, 0, 0,
                       0, 0, 0, 64,
                       23, 0, 0, 0,
                       0, 0, 0, 0,
                       &H7B, &H14, &H2E, &H3F,
                       &H1, &H0, &HE0, &H3F,
                       16, 0, 0, 0,
                       0, 0, 16, 64,
                       5, 0, 0, 0,
                           107, 119, 79, 65,
                           2, 0, 0, 0,
                           114, 99, 79, 65,
                           1, 0, 0, 0,
                           105, 109, 79, 65,
                           1, 0, 0, 0,
                           119, 119, 79, 65,
                           1, 0, 0, 0,
                           0, 0, 0, 0,
                           0, 0, 0, 0,
                       0, 0, 0, 0,
                       0, 0, 0, 0,
                       0, 0, 225, 68,
                       2, 0, 0, 0,
                       0, 0, 0, 0,
                       0, 0, 0, 0,
                       0, 0, 0, 0,
                       0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "0123456789"},
                        {"mission key", "h"},
                        {"key", "u"},
                        {"unit type", "Obla".ToAscBytes.Reverse.ToUInt32},
                        {"inventory", New List(Of Dictionary(Of InvariantString, Object)) From {
                            New Dictionary(Of InvariantString, Object) From {{"item", "ratf".ToAscBytes.Reverse.ToUInt32}, {"charges", 0}, {"unknown", &H3500}},
                            New Dictionary(Of InvariantString, Object) From {{"item", "ckng".ToAscBytes.Reverse.ToUInt32}, {"charges", 0}, {"unknown", &H3500}},
                            New Dictionary(Of InvariantString, Object) From {{"item", "desc".ToAscBytes.Reverse.ToUInt32}, {"charges", 0}, {"unknown", &H3700}},
                            New Dictionary(Of InvariantString, Object) From {{"item", "modt".ToAscBytes.Reverse.ToUInt32}, {"charges", 0}, {"unknown", &H3500}},
                            New Dictionary(Of InvariantString, Object) From {{"item", "ofro".ToAscBytes.Reverse.ToUInt32}, {"charges", 0}, {"unknown", &H3500}},
                            New Dictionary(Of InvariantString, Object) From {{"item", 0}, {"charges", 0}, {"unknown", 0}}}},
                        {"experience", 2700},
                        {"level ups", 6},
                        {"skill points", 2},
                        {"proper name index", 7},
                        {"unknown1", 1},
                        {"base strength", 18},
                        {"bonus strength per level", CSng(2.0)},
                        {"base agility", 23},
                        {"bonus move speed", CSng(0.0)},
                        {"bonus attack speed", CSng(0.68)},
                        {"bonus agility per level", CSng(1.75000012)},
                        {"base intelligence", 16},
                        {"bonus intelligence per level", CSng(2.25)},
                        {"hero skills", New List(Of Dictionary(Of InvariantString, Object)) From {
                            New Dictionary(Of InvariantString, Object) From {{"ability", "AOwk".ToAscBytes.Reverse.ToUInt32}, {"level", 2}},
                            New Dictionary(Of InvariantString, Object) From {{"ability", "AOcr".ToAscBytes.Reverse.ToUInt32}, {"level", 1}},
                            New Dictionary(Of InvariantString, Object) From {{"ability", "AOmi".ToAscBytes.Reverse.ToUInt32}, {"level", 1}},
                            New Dictionary(Of InvariantString, Object) From {{"ability", "AOww".ToAscBytes.Reverse.ToUInt32}, {"level", 1}},
                            New Dictionary(Of InvariantString, Object) From {{"ability", 0}, {"level", 0}}}},
                        {"bonus health", CSng(0)},
                        {"bonus mana", CSng(0)},
                        {"sight radius (day)", CSng(1800)},
                        {"unknown2", 2},
                        {"unknown3", New Byte() {0, 0, 0, 0}.AsReadableList},
                        {"unknown4", New Byte() {0, 0, 0, 0}.AsReadableList},
                        {"unknown5", New Byte() {0, 0, 0, 0}.AsReadableList},
                        {"hotkey flags", 0}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncEmptyBooleanTest()
        JarTest(GameActions.GameCacheSyncEmptyBoolean.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncEmptyIntegerTest()
        JarTest(GameActions.GameCacheSyncEmptyInteger.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncEmptyRealTest()
        JarTest(GameActions.GameCacheSyncEmptyReal.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncEmptyStringTest()
        JarTest(GameActions.GameCacheSyncEmptyString.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncEmptyUnitTest()
        Dim raw = Enumerable.Repeat(CByte(0), 86).ToArray
        JarTest(GameActions.GameCacheSyncEmptyUnit.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"}
                    })
    End Sub
    <TestMethod()>
    Public Sub IncreaseGameSpeedTest()
        EmptyJarTest(GameActions.IncreaseGameSpeed.Jar)
    End Sub
    <TestMethod()>
    Public Sub MinimapPingTest()
        JarTest(GameActions.MinimapPing.Jar,
                data:=BitConverter.GetBytes(CSng(5)).Concat(
                      BitConverter.GetBytes(CSng(6))).Concat(
                      BitConverter.GetBytes(CSng(7))).ToArray,
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"x", CSng(5.0)},
                        {"y", CSng(6.0)},
                        {"duration", CSng(7.0)}
                    })
    End Sub
    <TestMethod()>
    Public Sub ObjectOrderTest()
        JarTest(GameActions.ObjectOrder.Jar,
                data:=New Byte() _
                      {1, 0,
                       3, 0, &HD, 0,
                       2, 0, 0, 0, 3, 0, 0, 0}.Concat(
                       BitConverter.GetBytes(CSng(5.0))).Concat(
                       BitConverter.GetBytes(CSng(6.0))).Concat({
                       3, 0, 0, 0, 2, 0, 0, 0}).ToArray,
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"flags", OrderTypes.Queue},
                        {"order", OrderId.Smart},
                        {"unknown", New GameObjectId(2, 3)},
                        {"x", CSng(5.0)},
                        {"y", CSng(6.0)},
                        {"target", New GameObjectId(3, 2)}
                    })
    End Sub
    <TestMethod()>
    Public Sub PauseGameTest()
        EmptyJarTest(GameActions.PauseGame.Jar)
    End Sub
    <TestMethod()>
    Public Sub PointOrderTest()
        JarTest(GameActions.PointOrder.Jar,
                data:=New Byte() _
                      {1, 0,
                       3, 0, &HD, 0,
                       2, 0, 0, 0, 3, 0, 0, 0}.Concat(
                       BitConverter.GetBytes(CSng(5.0))).Concat(
                       BitConverter.GetBytes(CSng(6.0))).ToArray,
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"flags", OrderTypes.Queue},
                        {"order", OrderId.Smart},
                        {"unknown", New GameObjectId(2, 3)},
                        {"target x", CSng(5.0)},
                        {"target y", CSng(6.0)}
                    })
    End Sub
    <TestMethod()>
    Public Sub PressedEscapeTest()
        EmptyJarTest(GameActions.PressedEscape.Jar)
    End Sub
    <TestMethod()>
    Public Sub PreSubGroupSelectionTest()
        EmptyJarTest(GameActions.PreSubGroupSelection.Jar)
    End Sub
    <TestMethod()>
    Public Sub ResumeGameTest()
        EmptyJarTest(GameActions.ResumeGame.Jar)
    End Sub
    <TestMethod()>
    Public Sub SaveGameFinishedTest()
        JarTest(GameActions.SaveGameFinished.Jar,
                data:={1, 0, 0, 0},
                value:=1UI)
    End Sub
    <TestMethod()>
    Public Sub SaveGameStartedTest()
        JarTest(GameActions.SaveGameStarted.Jar,
                data:={116, 101, 115, 116, 0},
                value:="test")
    End Sub
    <TestMethod()>
    Public Sub SelectGroundItemTest()
        JarTest(GameActions.SelectGroundItem.Jar,
                data:={1,
                       2, 0, 0, 0, 3, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", 1},
                        {"target", New GameObjectId(2, 3)}
                    })
    End Sub
    <TestMethod()>
    Public Sub SelectGroupHotkeyTest()
        JarTest(GameActions.SelectGroupHotkey.Jar,
                data:={1, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"group index", 1},
                        {"unknown", 0}
                    })
    End Sub
    <TestMethod()>
    Public Sub SelectSubGroupTest()
        JarTest(GameActions.SelectSubGroup.Jar,
                data:={&HEF, &HBE, &HED, &HFE,
                       2, 0, 0, 0, 3, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unit type", &HFEEDBEEFUI},
                        {"target", New GameObjectId(2, 3)}
                    })
    End Sub
    <TestMethod()>
    Public Sub SelfOrderTest()
        JarTest(GameActions.SelfOrder.Jar,
                data:={1, 0,
                       3, 0, &HD, 0,
                       2, 0, 0, 0, 3, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"flags", OrderTypes.Queue},
                        {"order", OrderId.Smart},
                        {"unknown", New GameObjectId(2, 3)}
                    })
    End Sub
    <TestMethod()>
    Public Sub SetGameSpeedTest()
        JarTest(GameActions.SetGameSpeed.Jar,
                Function(e1 As GameSpeedSetting, e2 As GameSpeedSetting) e1 = e2,
                data:={2},
                value:=GameSpeedSetting.Fast)
    End Sub
    <TestMethod()>
    Public Sub TransferResourcesTest()
        JarTest(GameActions.TransferResources.Jar,
                data:={1,
                       100, 0, 0, 0,
                       200, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player slot id", 1},
                        {"gold", 100},
                        {"lumber", 200}
                    })
    End Sub
    <TestMethod()>
    Public Sub TriggerArrowKeyEventTest()
        JarTest(GameActions.TriggerArrowKeyEvent.Jar,
                Function(e1 As ArrowKeyEvent, e2 As ArrowKeyEvent) e1 = e2,
                data:={4},
                value:=ArrowKeyEvent.PressedDownArrow)
    End Sub
    <TestMethod()>
    Public Sub TriggerChatEventTest()
        JarTest(GameActions.TriggerChatEvent.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"trigger event", New GameObjectId(2, 3)},
                        {"text", "test"}
                    })
    End Sub
    <TestMethod()>
    Public Sub TriggerDialogButtonClickedTest()
        JarTest(GameActions.DialogAnyButtonClicked.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0,
                       4, 0, 0, 0, 5, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"dialog", New GameObjectId(2, 3)},
                        {"button", New GameObjectId(4, 5)}
                    })
    End Sub
    <TestMethod()>
    Public Sub TriggerDialogButtonClicked2Test()
        JarTest(GameActions.DialogButtonClicked.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0,
                       4, 0, 0, 0, 5, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"button", New GameObjectId(2, 3)},
                        {"dialog", New GameObjectId(4, 5)}
                    })
    End Sub
    <TestMethod()>
    Public Sub TriggerMouseClickedTrackableTest()
        JarTest(GameActions.TriggerMouseClickedTrackable.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0},
                value:=New GameObjectId(2, 3))
    End Sub
    <TestMethod()>
    Public Sub TriggerMouseTouchedTrackableTest()
        JarTest(GameActions.TriggerMouseTouchedTrackable.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0},
                value:=New GameObjectId(2, 3))
    End Sub
    <TestMethod()>
    Public Sub TriggerSelectionEventTest()
        JarTest(GameActions.TriggerSelectionEvent.Jar,
                data:={1,
                       2, 0, 0, 0, 3, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"operation", SelectionOperation.Add},
                        {"target", New GameObjectId(2, 3)}
                    })
    End Sub
    <TestMethod()>
    Public Sub TriggerWaitFinishedTest()
        JarTest(GameActions.TriggerWaitFinished.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0,
                       1, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"trigger thread", New GameObjectId(2, 3)},
                        {"thread wait count", 1}
                    })
    End Sub
End Class
