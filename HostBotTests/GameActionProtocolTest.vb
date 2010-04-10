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
Public Class GameActionProtocolTest
    <TestMethod()>
    Public Sub AllActionsDefinedTest()
        Assert.IsTrue(EnumValues(Of GameActionId)().Count = GameActions.AllDefinitions.Count)
        For Each e In EnumValues(Of GameActionId)()
            Assert.IsTrue(GameActions.DefinitionFor(e) IsNot Nothing)
        Next e
    End Sub

    <TestMethod()>
    Public Sub AssignGroupHotkeyTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.AssignGroupHotkey) Is GameActions.AssignGroupHotkey)
        JarTest(GameActions.AssignGroupHotkey.Jar,
                data:={1,
                       1, 0,
                       1, 0, 0, 0, 2, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"group index", CByte(1)},
                        {"targets", {New GameObjectId(1, 2)}.ToReadableList}
                    })
    End Sub
    <TestMethod()>
    Public Sub CancelHeroReviveTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CancelHeroRevive) Is GameActions.CancelHeroRevive)
        JarTest(GameActions.CancelHeroRevive.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0},
                value:=New GameObjectId(2, 3))
    End Sub
    <TestMethod()>
    Public Sub ChangeAllyOptionsTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.ChangeAllyOptions) Is GameActions.ChangeAllyOptions)
        JarTest(GameActions.ChangeAllyOptions.Jar,
                data:={1,
                       1 << 5, 1 << (10 - 8), 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player slot id", CByte(1)},
                        {"flags", AllianceTypes.AlliedVictory Or AllianceTypes.SharedVision}
                    })
    End Sub
    <TestMethod()>
    Public Sub ChangeSelectionTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.ChangeSelection) Is GameActions.ChangeSelection)
        JarTest(GameActions.ChangeSelection.Jar,
                data:={2,
                       1, 0,
                       1, 0, 0, 0, 2, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"operation", SelectionOperation.Remove},
                        {"targets", {New GameObjectId(1, 2)}.ToReadableList}
                    })
    End Sub
    <TestMethod()>
    Public Sub CheatDisableTechRequirementsTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatDisableTechRequirements) Is GameActions.CheatDisableTechRequirements)
        JarTest(GameActions.CheatDisableTechRequirements.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatDisableVictoryConditionsTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatDisableVictoryConditions) Is GameActions.CheatDisableVictoryConditions)
        JarTest(GameActions.CheatDisableVictoryConditions.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatEnableResearchTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatEnableResearch) Is GameActions.CheatEnableResearch)
        JarTest(GameActions.CheatEnableResearch.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatFastCooldownTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatFastCooldown) Is GameActions.CheatFastCooldown)
        JarTest(GameActions.CheatFastCooldown.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatFastDeathDecayTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatFastDeathDecay) Is GameActions.CheatFastDeathDecay)
        JarTest(GameActions.CheatFastDeathDecay.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatGodModeTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatGodMode) Is GameActions.CheatGodMode)
        JarTest(GameActions.CheatGodMode.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatGoldTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatGold) Is GameActions.CheatGold)
        JarTest(GameActions.CheatGold.Jar,
                data:={0,
                       100, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", CByte(0)},
                        {"amount", 100UI}
                    })
    End Sub
    <TestMethod()>
    Public Sub CheatGoldAndLumberTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatGoldAndLumber) Is GameActions.CheatGoldAndLumber)
        JarTest(GameActions.CheatGoldAndLumber.Jar,
                data:={0,
                       100, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", CByte(0)},
                        {"amount", 100UI}
                    })
    End Sub
    <TestMethod()>
    Public Sub CheatInstantDefeatTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatInstantDefeat) Is GameActions.CheatInstantDefeat)
        JarTest(GameActions.CheatInstantDefeat.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatInstantVictoryTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatInstantVictory) Is GameActions.CheatInstantVictory)
        JarTest(GameActions.CheatInstantVictory.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatLumberTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatLumber) Is GameActions.CheatLumber)
        JarTest(GameActions.CheatLumber.Jar,
                data:={0,
                       100, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", CByte(0)},
                        {"amount", 100UI}
                    })
    End Sub
    <TestMethod()>
    Public Sub CheatNoDefeatTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatNoDefeat) Is GameActions.CheatNoDefeat)
        JarTest(GameActions.CheatNoDefeat.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatNoFoodLimitTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatNoFoodLimit) Is GameActions.CheatNoFoodLimit)
        JarTest(GameActions.CheatNoFoodLimit.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatRemoveFogOfWarTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatRemoveFogOfWar) Is GameActions.CheatRemoveFogOfWar)
        JarTest(GameActions.CheatRemoveFogOfWar.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatResearchUpgradesTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatResearchUpgrades) Is GameActions.CheatResearchUpgrades)
        JarTest(GameActions.CheatResearchUpgrades.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatSetTimeOfDayTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatSetTimeOfDay) Is GameActions.CheatSetTimeOfDay)
        JarTest(GameActions.CheatSetTimeOfDay.Jar,
                data:=BitConverter.GetBytes(CSng(12.0)),
                value:=CSng(12.0))
    End Sub
    <TestMethod()>
    Public Sub CheatSpeedConstructionTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatSpeedConstruction) Is GameActions.CheatSpeedConstruction)
        JarTest(GameActions.CheatSpeedConstruction.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub CheatUnlimitedManaTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.CheatUnlimitedMana) Is GameActions.CheatUnlimitedMana)
        JarTest(GameActions.CheatUnlimitedMana.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub DecreaseGameSpeedTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.DecreaseGameSpeed) Is GameActions.DecreaseGameSpeed)
        JarTest(GameActions.DecreaseGameSpeed.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub DequeueBuildingOrderTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.DequeueBuildingOrder) Is GameActions.DequeueBuildingOrder)
        JarTest(GameActions.DequeueBuildingOrder.Jar,
                data:={1,
                       &HFE, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"slot number", CByte(1)},
                        {"type", &HFEUI}
                    })
    End Sub
    <TestMethod()>
    Public Sub DropOrGiveItemTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.DropOrGiveItem) Is GameActions.DropOrGiveItem)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.EnterChooseBuildingSubmenu) Is GameActions.EnterChooseBuildingSubmenu)
        JarTest(GameActions.EnterChooseBuildingSubmenu.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub EnterChooseHeroSkillSubmenuTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.EnterChooseHeroSkillSubmenu) Is GameActions.EnterChooseHeroSkillSubmenu)
        JarTest(GameActions.EnterChooseHeroSkillSubmenu.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub FogObjectOrderTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.FogObjectOrder) Is GameActions.FogObjectOrder)
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
                        {"target type", &HFEEDUI},
                        {"target flags", 1UL},
                        {"target owner", CByte(2)},
                        {"target x", CSng(7.0)},
                        {"target y", CSng(8.0)}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncBooleanTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.GameCacheSyncBoolean) Is GameActions.GameCacheSyncBoolean)
        JarTest(GameActions.GameCacheSyncBoolean.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0,
                       1, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"},
                        {"value", 1UI}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncIntegerTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.GameCacheSyncInteger) Is GameActions.GameCacheSyncInteger)
        JarTest(GameActions.GameCacheSyncInteger.Jar,
                data:={116, 101, 115, 116, 0,
                       101, 115, 116, 0,
                       116, 101, 115, 0,
                       1, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filename", "test"},
                        {"mission key", "est"},
                        {"key", "tes"},
                        {"value", 1UI}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncRealTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.GameCacheSyncReal) Is GameActions.GameCacheSyncReal)
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
    Public Sub GameCacheSyncUnitTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.GameCacheSyncUnit) Is GameActions.GameCacheSyncUnit)
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
                        {"inventory", {New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"item", "ratf".ToAscBytes.Reverse.ToUInt32}, {"charges", 0UI}, {"unknown", &H3500UI}}),
                                       New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"item", "ckng".ToAscBytes.Reverse.ToUInt32}, {"charges", 0UI}, {"unknown", &H3500UI}}),
                                       New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"item", "desc".ToAscBytes.Reverse.ToUInt32}, {"charges", 0UI}, {"unknown", &H3700UI}}),
                                       New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"item", "modt".ToAscBytes.Reverse.ToUInt32}, {"charges", 0UI}, {"unknown", &H3500UI}}),
                                       New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"item", "ofro".ToAscBytes.Reverse.ToUInt32}, {"charges", 0UI}, {"unknown", &H3500UI}}),
                                       New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"item", 0UI}, {"charges", 0UI}, {"unknown", 0UI}})
                                       }.ToReadableList},
                        {"experience", 2700UI},
                        {"level ups", 6UI},
                        {"skill points", 2UI},
                        {"proper name index", 7US},
                        {"unknown1", 1US},
                        {"base strength", 18UI},
                        {"bonus strength per level", CSng(2.0)},
                        {"base agility", 23UI},
                        {"bonus move speed", CSng(0.0)},
                        {"bonus attack speed", CSng(0.68)},
                        {"bonus agility per level", CSng(1.75000012)},
                        {"base intelligence", 16UI},
                        {"bonus intelligence per level", CSng(2.25)},
                        {"hero skills", {New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"ability", "AOwk".ToAscBytes.Reverse.ToUInt32}, {"level", 2UI}}),
                                         New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"ability", "AOcr".ToAscBytes.Reverse.ToUInt32}, {"level", 1UI}}),
                                         New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"ability", "AOmi".ToAscBytes.Reverse.ToUInt32}, {"level", 1UI}}),
                                         New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"ability", "AOww".ToAscBytes.Reverse.ToUInt32}, {"level", 1UI}}),
                                         New NamedValueMap(New Dictionary(Of InvariantString, Object) From {{"ability", 0UI}, {"level", 0UI}})
                                         }.ToReadableList},
                        {"bonus health", CSng(0)},
                        {"bonus mana", CSng(0)},
                        {"sight radius (day)", CSng(1800)},
                        {"unknown2", 2UI},
                        {"unknown3", New Byte() {0, 0, 0, 0}.AsReadableList},
                        {"unknown4", New Byte() {0, 0, 0, 0}.AsReadableList},
                        {"unknown5", New Byte() {0, 0, 0, 0}.AsReadableList},
                        {"hotkey flags", 0US}
                    })
    End Sub
    <TestMethod()>
    Public Sub GameCacheSyncEmptyBooleanTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.GameCacheSyncEmptyBoolean) Is GameActions.GameCacheSyncEmptyBoolean)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.GameCacheSyncEmptyInteger) Is GameActions.GameCacheSyncEmptyInteger)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.GameCacheSyncEmptyReal) Is GameActions.GameCacheSyncEmptyReal)
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
    Public Sub GameCacheSyncEmptyUnitTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.GameCacheSyncEmptyUnit) Is GameActions.GameCacheSyncEmptyUnit)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.IncreaseGameSpeed) Is GameActions.IncreaseGameSpeed)
        JarTest(GameActions.IncreaseGameSpeed.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub MinimapPingTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.MinimapPing) Is GameActions.MinimapPing)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.ObjectOrder) Is GameActions.ObjectOrder)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.PauseGame) Is GameActions.PauseGame)
        JarTest(GameActions.PauseGame.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub PointOrderTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.PointOrder) Is GameActions.PointOrder)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.PressedEscape) Is GameActions.PressedEscape)
        JarTest(GameActions.PressedEscape.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub PreSubGroupSelectionTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.PreSubGroupSelection) Is GameActions.PreSubGroupSelection)
        JarTest(GameActions.PreSubGroupSelection.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub ResumeGameTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.ResumeGame) Is GameActions.ResumeGame)
        JarTest(GameActions.ResumeGame.Jar, data:={}, value:=New Pickling.EmptyJar.EmptyValue)
    End Sub
    <TestMethod()>
    Public Sub SaveGameFinishedTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.SaveGameFinished) Is GameActions.SaveGameFinished)
        JarTest(GameActions.SaveGameFinished.Jar,
                data:={1, 0, 0, 0},
                value:=1UI)
    End Sub
    <TestMethod()>
    Public Sub SaveGameStartedTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.SaveGameStarted) Is GameActions.SaveGameStarted)
        JarTest(GameActions.SaveGameStarted.Jar,
                data:={116, 101, 115, 116, 0},
                value:="test")
    End Sub
    <TestMethod()>
    Public Sub SelectGroundItemTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.SelectGroundItem) Is GameActions.SelectGroundItem)
        JarTest(GameActions.SelectGroundItem.Jar,
                data:={1,
                       2, 0, 0, 0, 3, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", CByte(1)},
                        {"target", New GameObjectId(2, 3)}
                    })
    End Sub
    <TestMethod()>
    Public Sub SelectGroupHotkeyTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.SelectGroupHotkey) Is GameActions.SelectGroupHotkey)
        JarTest(GameActions.SelectGroupHotkey.Jar,
                data:={1, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"group index", CByte(1)},
                        {"unknown", CByte(0)}
                    })
    End Sub
    <TestMethod()>
    Public Sub SelectSubGroupTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.SelectSubGroup) Is GameActions.SelectSubGroup)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.SelfOrder) Is GameActions.SelfOrder)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.SetGameSpeed) Is GameActions.SetGameSpeed)
        JarTest(GameActions.SetGameSpeed.Jar,
                Function(e1 As GameSpeedSetting, e2 As GameSpeedSetting) e1 = e2,
                data:={2},
                value:=GameSpeedSetting.Fast)
    End Sub
    <TestMethod()>
    Public Sub TransferResourcesTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.TransferResources) Is GameActions.TransferResources)
        JarTest(GameActions.TransferResources.Jar,
                data:={1,
                       100, 0, 0, 0,
                       200, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"player slot id", CByte(1)},
                        {"gold", 100UI},
                        {"lumber", 200UI}
                    })
    End Sub
    <TestMethod()>
    Public Sub TriggerArrowKeyEventTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.TriggerArrowKeyEvent) Is GameActions.TriggerArrowKeyEvent)
        JarTest(GameActions.TriggerArrowKeyEvent.Jar,
                Function(e1 As ArrowKeyEvent, e2 As ArrowKeyEvent) e1 = e2,
                data:={4},
                value:=ArrowKeyEvent.PressedDownArrow)
    End Sub
    <TestMethod()>
    Public Sub TriggerChatEventTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.TriggerChatEvent) Is GameActions.TriggerChatEvent)
        JarTest(GameActions.TriggerChatEvent.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"trigger event", New GameObjectId(2, 3)},
                        {"text", "test"}
                    })
    End Sub
    <TestMethod()>
    Public Sub DialogAnyButtonClickedTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.DialogAnyButtonClicked) Is GameActions.DialogAnyButtonClicked)
        JarTest(GameActions.DialogAnyButtonClicked.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0,
                       4, 0, 0, 0, 5, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"dialog", New GameObjectId(2, 3)},
                        {"button", New GameObjectId(4, 5)}
                    })
    End Sub
    <TestMethod()>
    Public Sub DialogButtonClickedTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.DialogButtonClicked) Is GameActions.DialogButtonClicked)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.TriggerMouseClickedTrackable) Is GameActions.TriggerMouseClickedTrackable)
        JarTest(GameActions.TriggerMouseClickedTrackable.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0},
                value:=New GameObjectId(2, 3))
    End Sub
    <TestMethod()>
    Public Sub TriggerMouseTouchedTrackableTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.TriggerMouseTouchedTrackable) Is GameActions.TriggerMouseTouchedTrackable)
        JarTest(GameActions.TriggerMouseTouchedTrackable.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0},
                value:=New GameObjectId(2, 3))
    End Sub
    <TestMethod()>
    Public Sub TriggerSelectionEventTest()
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.TriggerSelectionEvent) Is GameActions.TriggerSelectionEvent)
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
        Assert.IsTrue(GameActions.DefinitionFor(GameActionId.TriggerWaitFinished) Is GameActions.TriggerWaitFinished)
        JarTest(GameActions.TriggerWaitFinished.Jar,
                data:={2, 0, 0, 0, 3, 0, 0, 0,
                       1, 0, 0, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"trigger thread", New GameObjectId(2, 3)},
                        {"thread wait count", 1UI}
                    })
    End Sub
End Class
