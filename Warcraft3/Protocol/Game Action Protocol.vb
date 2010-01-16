Imports Tinker.Pickling

Namespace WC3.Protocol
    '''<summary>Game actions which can be performed by players.</summary>
    '''<original-source> http://www.wc3c.net/tools/specs/W3GActions.txt </original-source>
    Public Enum W3GameActionId As Byte
        PauseGame = &H1
        ResumeGame = &H2
        SetGameSpeed = &H3
        IncreaseGameSpeed = &H4
        DecreaseGameSpeed = &H5
        SaveGameStarted = &H6
        SaveGameFinished = &H7
        '_Unseen0x08 = &H8
        '_Unseen0x09 = &H9
        '_Unseen0x0A = &HA
        '_Unseen0x0B = &HB
        '_Unseen0x0C = &HC
        '_Unseen0x0D = &HD
        '_Unseen0x0E = &HE
        '_Unseen0x0F = &HF
        SelfOrder = &H10
        PointOrder = &H11
        ObjectOrder = &H12
        DropOrGiveItem = &H13
        FogObjectOrder = &H14
        '_Unseen0x15 = &H15
        ChangeSelection = &H16
        AssignGroupHotkey = &H17
        SelectGroupHotkey = &H18
        SelectSubGroup = &H19
        PreSubGroupSelection = &H1A
        TriggerSelectionEvent = &H1B
        SelectGroundItem = &H1C
        CancelHeroRevive = &H1D
        DequeueBuildingOrder = &H1E
        '_Unseen0x1F = &H1F
        CheatFastCooldown = &H20
        '_Unseen0x21 = &H21
        CheatInstantDefeat = &H22
        CheatSpeedConstruction = &H23
        CheatFastDeathDecay = &H24
        CheatNoFoodLimit = &H25
        CheatGodMode = &H26
        CheatGold = &H27
        CheatLumber = &H28
        CheatUnlimitedMana = &H29
        CheatNoDefeat = &H2A
        CheatDisableVictoryConditions = &H2B
        CheatEnableResearch = &H2C
        CheatGoldAndLumber = &H2D
        CheatSetTimeOfDay = &H2E
        CheatRemoveFogOfWar = &H2F
        CheatDisableTechRequirements = &H30
        CheatResearchUpgrades = &H31
        CheatInstantVictory = &H32
        '_Unseen0x33 = &H33
        '_Unseen0x34 = &H34
        '_Unseen0x35 = &H35
        '_Unseen0x36 = &H36
        '_Unseen0x37 = &H37
        '_Unseen0x38 = &H38
        '_Unseen0x39 = &H39
        '_Unseen0x3A = &H3A
        '_Unseen0x3B = &H3B
        '_Unseen0x3C = &H3C
        '_Unseen0x3D = &H3D
        '_Unseen0x3E = &H3E
        '_Unseen0x3F = &H3F
        '_Unseen0x40 = &H40
        '_Unseen0x41 = &H41
        '_Unseen0x42 = &H42
        '_Unseen0x43 = &H43
        '_Unseen0x44 = &H44
        '_Unseen0x45 = &H45
        '_Unseen0x46 = &H46
        '_Unseen0x47 = &H47
        '_Unseen0x48 = &H48
        '_Unseen0x49 = &H49
        '_Unseen0x4A = &H4A
        '_Unseen0x4B = &H4B
        '_Unseen0x4C = &H4C
        '_Unseen0x4D = &H4D
        '_Unseen0x4E = &H4E
        '_Unseen0x4F = &H4F
        ChangeAllyOptions = &H50
        TransferResources = &H51
        '_Unseen0x52 = &H52
        '_Unseen0x53 = &H53
        '_Unseen0x54 = &H54
        '_Unseen0x55 = &H55
        '_Unseen0x56 = &H56
        '_Unseen0x57 = &H57
        '_Unseen0x58 = &H58
        '_Unseen0x59 = &H59
        '_Unseen0x5A = &H5A
        '_Unseen0x5B = &H5B
        '_Unseen0x5C = &H5C
        '_Unseen0x5D = &H5D
        '_Unseen0x5E = &H5E
        '_Unseen0x5F = &H5F
        TriggerChatEvent = &H60
        PressedEscape = &H61
        TriggerWaitFinished = &H62
        '_Unseen0x63 = &H63
        TriggerMouseClickedTrackable = &H64
        TriggerMouseTouchedTrackable = &H65
        EnterChooseHeroSkillSubmenu = &H66
        EnterChooseBuildingSubmenu = &H67
        MinimapPing = &H68
        TriggerDialogButtonClicked2 = &H69
        TriggerDialogButtonClicked = &H6A
        GameCacheSyncInteger = &H6B
        GameCacheSyncReal = &H6C
        GameCacheSyncBoolean = &H6D
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        GameCacheSyncString = &H6E
        GameCacheSyncUnit = &H6F
        '_Unseen0x70 = &H70
        '_Unseen0x71 = &H71
        '_Unseen0x72 = &H72
        '_Unseen0x73 = &H73
        '_Unseen0x74 = &H74
        TriggerArrowKeyEvent = &H75
    End Enum

    Public Enum GameSpeedSetting As Byte
        Slow = 0
        Normal = 1
        Fast = 2
    End Enum

    <Flags()>
    Public Enum OrderTypes As UShort
        Queue = 1 << 0
        Train = 1 << 1
        Construct = 1 << 2
        Group = 1 << 3
        NoFormation = 1 << 4
        SubGroup = 1 << 6
        AutoCastOn = 1 << 8
    End Enum

    Public Enum SelectionOperation As Byte
        Add = 1
        Remove = 2
    End Enum

    <Flags()>
    Public Enum AllianceTypes As UInteger
        Passive = 1 << 0
        HelpRequest = 1 << 1
        HelpResponse = 1 << 2
        SharedXP = 1 << 3
        SharedSpells = 1 << 4
        SharedVision = 1 << 5
        SharedControl = 1 << 6
        FullSharedControl = 1 << 7
        Rescuable = 1 << 8
        SharedVisionForced = 1 << 9
        AlliedVictory = 1 << 10
    End Enum

    Public Enum ArrowKeyEvent As Byte
        PressedLeftArrow = 0
        ReleasedLeftArrow = 1
        PressedRightArrow = 2
        ReleasedRightArrow = 3
        PressedDownArrow = 4
        ReleasedDownArrow = 5
        PressedUpArrow = 6
        ReleasedUpArrow = 7
    End Enum

    Public Enum OrderId As UInteger
        Smart = &HD0003 'right-click
        [Stop] = &HD0004
        SetRallyPoint = &HD000C
        GetItem = &HD000D
        Attack = &HD000F
        AttackGround = &HD0010
        AttackOnce = &HD0011
        Move = &HD0012
        AIMove = &HD0014
        Patrol = &HD0016
        HoldPosition = &HD0019
        Build = &HD001A
        HumanBuild = &HD001B
        OrcBuild = &HD001C
        NightElfBuild = &HD001D
        UndeadBuild = &HD001E
        ResumeBuild = &HD001F
        GiveOrDropItem = &HD0021
        SwapItemWithItemInSlot1 = &HD0022
        SwapItemWithItemInSlot2 = &HD0023
        SwapItemWithItemInSlot3 = &HD0024
        SwapItemWithItemInSlot4 = &HD0025
        SwapItemWithItemInSlot5 = &HD0026
        SwapItemWithItemInSlot6 = &HD0027
        UseItemInSlot1 = &HD0028
        UseItemInSlot2 = &HD0029
        UseItemInSlot3 = &HD002A
        UseItemInSlot4 = &HD002B
        UseItemInSlot5 = &HD002C
        UseItemInSlot6 = &HD002D
        ResumeHarvest = &HD0031
        Harvest = &HD0032
        ReturnResources = &HD0034
        AutoHarvestGold = &HD0035
        AutoHarvestLumber = &HD0036
        NeutralDetectAOE = &HD0037
        Repair = &HD0038
        RepairOn = &HD0039
        RepairOff = &HD003A
        '... many many more ...
    End Enum

    Public Structure GameObjectId
        Public ReadOnly AllocatedId As UInteger
        Public ReadOnly CounterId As UInteger
        Public Sub New(ByVal allocatedId As UInteger, ByVal counterId As UInteger)
            Me.AllocatedId = allocatedId
            Me.CounterId = counterId
        End Sub
    End Structure

    Public NotInheritable Class GameActions
        Private Sub New()
        End Sub

        Public Class SimpleDefinition
            Inherits TupleJar
            Public ReadOnly id As W3GameActionId
            Public Sub New(ByVal id As W3GameActionId, ByVal ParamArray subjars() As IJar(Of Object))
                MyBase.New(id.ToString, subjars)
                Contract.Requires(subjars IsNot Nothing)
                Me.id = id
            End Sub
        End Class

        Public Shared ReadOnly PauseGame As New SimpleDefinition(W3GameActionId.PauseGame)
        Public Shared ReadOnly ResumeGame As New SimpleDefinition(W3GameActionId.ResumeGame)
        Public Shared ReadOnly SetGameSpeed As New SimpleDefinition(W3GameActionId.SetGameSpeed,
                    New EnumByteJar(Of GameSpeedSetting)("speed").Weaken)
        Public Shared ReadOnly IncreaseGameSpeed As New SimpleDefinition(W3GameActionId.IncreaseGameSpeed)
        Public Shared ReadOnly DecreaseGameSpeed As New SimpleDefinition(W3GameActionId.DecreaseGameSpeed)
        Public Shared ReadOnly SaveGameStarted As New SimpleDefinition(W3GameActionId.SaveGameStarted,
                    New StringJar("filename").Weaken)
        Public Shared ReadOnly SaveGameFinished As New SimpleDefinition(W3GameActionId.SaveGameFinished,
                    New UInt32Jar("unknown").Weaken)

        Public Shared ReadOnly SelfOrder As New SimpleDefinition(W3GameActionId.SelfOrder,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken)
        Public Shared ReadOnly PointOrder As New SimpleDefinition(W3GameActionId.PointOrder,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken,
                    New Float32Jar("target x").Weaken,
                    New Float32Jar("target y").Weaken)
        Public Shared ReadOnly ObjectOrder As New SimpleDefinition(W3GameActionId.ObjectOrder,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken,
                    New Float32Jar("x").Weaken,
                    New Float32Jar("y").Weaken,
                    New GameObjectIdJar("target").Weaken)
        Public Shared ReadOnly DropOrGiveItem As New SimpleDefinition(W3GameActionId.DropOrGiveItem,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken,
                    New Float32Jar("x").Weaken,
                    New Float32Jar("y").Weaken,
                    New GameObjectIdJar("receiver").Weaken,
                    New GameObjectIdJar("item").Weaken)
        Public Shared ReadOnly FogObjectOrder As New SimpleDefinition(W3GameActionId.FogObjectOrder,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken,
                    New Float32Jar("fog target x").Weaken,
                    New Float32Jar("fog target y").Weaken,
                    New ObjectTypeJar("fog target type").Weaken,
                    New RawDataJar("unknown2", Size:=9).Weaken,
                    New Float32Jar("actual target x").Weaken,
                    New Float32Jar("actual target y").Weaken)

        Public Shared ReadOnly EnterChooseHeroSkillSubmenu As New SimpleDefinition(W3GameActionId.EnterChooseHeroSkillSubmenu)
        Public Shared ReadOnly EnterChooseBuildingSubmenu As New SimpleDefinition(W3GameActionId.EnterChooseBuildingSubmenu)
        Public Shared ReadOnly PressedEscape As New SimpleDefinition(W3GameActionId.PressedEscape)
        Public Shared ReadOnly CancelHeroRevive As New SimpleDefinition(W3GameActionId.CancelHeroRevive,
                    New GameObjectIdJar("target").Weaken)
        Public Shared ReadOnly DequeueBuildingOrder As New SimpleDefinition(W3GameActionId.DequeueBuildingOrder,
                    New ByteJar("slot number").Weaken,
                    New ObjectTypeJar("type").Weaken)
        Public Shared ReadOnly MinimapPing As New SimpleDefinition(W3GameActionId.MinimapPing,
                    New Float32Jar("x").Weaken,
                    New Float32Jar("y").Weaken,
                    New RawDataJar("unknown", Size:=4).Weaken)

        Public Shared ReadOnly ChangeAllyOptions As New SimpleDefinition(W3GameActionId.ChangeAllyOptions,
                    New ByteJar("player slot id").Weaken,
                    New EnumUInt32Jar(Of AllianceTypes)("flags").Weaken)
        Public Shared ReadOnly TransferResources As New SimpleDefinition(W3GameActionId.TransferResources,
                    New ByteJar("player slot id").Weaken,
                    New UInt32Jar("gold").Weaken,
                    New UInt32Jar("lumber").Weaken)

        Public Shared ReadOnly ChangeSelection As New SimpleDefinition(W3GameActionId.ChangeSelection,
                    New EnumByteJar(Of SelectionOperation)("operation").Weaken,
                    New ListJar(Of Object)("targets",
                        New GameObjectIdJar("target").Weaken, prefixsize:=2).Weaken)
        Public Shared ReadOnly AssignGroupHotkey As New SimpleDefinition(W3GameActionId.AssignGroupHotkey,
                    New ByteJar("group index").Weaken,
                    New ListJar(Of Object)("targets",
                        New GameObjectIdJar("target").Weaken, prefixsize:=2).Weaken)
        Public Shared ReadOnly SelectGroupHotkey As New SimpleDefinition(W3GameActionId.SelectGroupHotkey,
                    New ByteJar("group index").Weaken,
                    New ByteJar("unknown").Weaken)
        Public Shared ReadOnly SelectSubGroup As New SimpleDefinition(W3GameActionId.SelectSubGroup,
                    New ObjectTypeJar("unit type").Weaken,
                    New GameObjectIdJar("target").Weaken)
        Public Shared ReadOnly PreSubGroupSelection As New SimpleDefinition(W3GameActionId.PreSubGroupSelection)
        Public Shared ReadOnly SelectGroundItem As New SimpleDefinition(W3GameActionId.SelectGroundItem,
                    New ByteJar("unknown").Weaken,
                    New GameObjectIdJar("target").Weaken)

        Public Shared ReadOnly CheatDisableTechRequirements As New SimpleDefinition(W3GameActionId.CheatDisableTechRequirements)
        Public Shared ReadOnly CheatDisableVictoryConditions As New SimpleDefinition(W3GameActionId.CheatDisableVictoryConditions)
        Public Shared ReadOnly CheatEnableResearch As New SimpleDefinition(W3GameActionId.CheatEnableResearch)
        Public Shared ReadOnly CheatFastCooldown As New SimpleDefinition(W3GameActionId.CheatFastCooldown)
        Public Shared ReadOnly CheatFastDeathDecay As New SimpleDefinition(W3GameActionId.CheatFastDeathDecay)
        Public Shared ReadOnly CheatGodMode As New SimpleDefinition(W3GameActionId.CheatGodMode)
        Public Shared ReadOnly CheatInstantDefeat As New SimpleDefinition(W3GameActionId.CheatInstantDefeat)
        Public Shared ReadOnly CheatInstantVictory As New SimpleDefinition(W3GameActionId.CheatInstantVictory)
        Public Shared ReadOnly CheatNoDefeat As New SimpleDefinition(W3GameActionId.CheatNoDefeat)
        Public Shared ReadOnly CheatNoFoodLimit As New SimpleDefinition(W3GameActionId.CheatNoFoodLimit)
        Public Shared ReadOnly CheatRemoveFogOfWar As New SimpleDefinition(W3GameActionId.CheatRemoveFogOfWar)
        Public Shared ReadOnly CheatResearchUpgrades As New SimpleDefinition(W3GameActionId.CheatResearchUpgrades)
        Public Shared ReadOnly CheatSpeedConstruction As New SimpleDefinition(W3GameActionId.CheatSpeedConstruction)
        Public Shared ReadOnly CheatUnlimitedMana As New SimpleDefinition(W3GameActionId.CheatUnlimitedMana)
        Public Shared ReadOnly CheatSetTimeOfDay As New SimpleDefinition(W3GameActionId.CheatSetTimeOfDay,
                    New Float32Jar("time").Weaken)
        Public Shared ReadOnly CheatGold As New SimpleDefinition(W3GameActionId.CheatGold,
                    New ByteJar("unknown").Weaken,
                    New UInt32Jar("amount").Weaken)
        Public Shared ReadOnly CheatGoldAndLumber As New SimpleDefinition(W3GameActionId.CheatGoldAndLumber,
                    New ByteJar("unknown").Weaken,
                    New UInt32Jar("amount").Weaken)
        Public Shared ReadOnly CheatLumber As New SimpleDefinition(W3GameActionId.CheatLumber,
                    New ByteJar("unknown").Weaken,
                    New UInt32Jar("amount").Weaken)

        Public Shared ReadOnly TriggerChatEvent As New SimpleDefinition(W3GameActionId.TriggerChatEvent,
                    New GameObjectIdJar("trigger event").Weaken,
                    New StringJar("text").Weaken)
        Public Shared ReadOnly TriggerWaitFinished As New SimpleDefinition(W3GameActionId.TriggerWaitFinished,
                    New GameObjectIdJar("trigger thread").Weaken,
                    New UInt32Jar("thread wait count").Weaken)
        Public Shared ReadOnly TriggerMouseTouchedTrackable As New SimpleDefinition(W3GameActionId.TriggerMouseTouchedTrackable,
                    New GameObjectIdJar("trackable").Weaken)
        Public Shared ReadOnly TriggerMouseClickedTrackable As New SimpleDefinition(W3GameActionId.TriggerMouseClickedTrackable,
                    New GameObjectIdJar("trackable").Weaken)
        Public Shared ReadOnly TriggerDialogButtonClicked As New SimpleDefinition(W3GameActionId.TriggerDialogButtonClicked,
                    New GameObjectIdJar("dialog").Weaken,
                    New GameObjectIdJar("button").Weaken)
        Public Shared ReadOnly TriggerDialogButtonClicked2 As New SimpleDefinition(W3GameActionId.TriggerDialogButtonClicked2,
                    New GameObjectIdJar("button").Weaken,
                    New GameObjectIdJar("dialog").Weaken)
        Public Shared ReadOnly TriggerArrowKeyEvent As New SimpleDefinition(W3GameActionId.TriggerArrowKeyEvent,
                    New EnumByteJar(Of ArrowKeyEvent)("event type").Weaken)
        Public Shared ReadOnly TriggerSelectionEvent As New SimpleDefinition(W3GameActionId.TriggerSelectionEvent,
                    New EnumByteJar(Of SelectionOperation)("operation").Weaken,
                    New GameObjectIdJar("target").Weaken)

        Public Shared ReadOnly GameCacheSyncInteger As New SimpleDefinition(W3GameActionId.GameCacheSyncInteger,
                    New StringJar("filename").Weaken,
                    New StringJar("mission key").Weaken,
                    New StringJar("key").Weaken,
                    New UInt32Jar("value").Weaken)
        Public Shared ReadOnly GameCacheSyncBoolean As New SimpleDefinition(W3GameActionId.GameCacheSyncBoolean,
                    New StringJar("filename").Weaken,
                    New StringJar("mission key").Weaken,
                    New StringJar("key").Weaken,
                    New UInt32Jar("value").Weaken)
        Public Shared ReadOnly GameCacheSyncReal As New SimpleDefinition(W3GameActionId.GameCacheSyncReal,
                    New StringJar("filename").Weaken,
                    New StringJar("mission key").Weaken,
                    New StringJar("key").Weaken,
                    New Float32Jar("value").Weaken)
        Public Shared ReadOnly GameCacheSyncUnit As New SimpleDefinition(W3GameActionId.GameCacheSyncUnit,
                    New StringJar("filename").Weaken,
                    New StringJar("mission key").Weaken,
                    New StringJar("key").Weaken,
                    New ObjectTypeJar("unit type").Weaken,
                    New RawDataJar("unknown data", Size:=86).Weaken)
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        Public Shared ReadOnly GameCacheSyncString As New SimpleDefinition(W3GameActionId.GameCacheSyncString,
                    New StringJar("filename").Weaken,
                    New StringJar("mission key").Weaken,
                    New StringJar("key").Weaken,
                    New StringJar("value").Weaken)

        <Pure()>
        Public Shared Function TypeIdString(ByVal value As UInt32) As String
            Dim bytes = value.Bytes()
            If (From b In bytes Where b < 32 Or b >= 128).None Then
                'Ascii identifier (eg. 'hfoo' for human footman)
                Return bytes.Reverse.ParseChrString(nullTerminated:=False)
            Else
                'Not ascii values, better just output hex
                Return bytes.ToHexString
            End If
        End Function
    End Class
End Namespace
