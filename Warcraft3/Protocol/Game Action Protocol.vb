Imports Tinker.Pickling

Namespace WC3.Protocol
    '''<summary>Game actions which can be performed by players.</summary>
    '''<original-source> http://www.wc3c.net/tools/specs/W3GActions.txt </original-source>
    '''<remarks>Ids prefixed with 'Trigger' only occur if there is a trigger to catch them.</remarks>
    Public Enum GameActionId As Byte
        'Global actions [0x00 to 0x07 (spaced to 0x0F)]
        PauseGame = &H1
        ResumeGame = &H2
        SetGameSpeed = &H3
        IncreaseGameSpeed = &H4
        DecreaseGameSpeed = &H5
        SaveGameStarted = &H6
        SaveGameFinished = &H7

        'Order actions [0x10 to 0x1E (spaced to 0x1F)]
        SelfOrder = &H10
        PointOrder = &H11
        ObjectOrder = &H12
        DropOrGiveItem = &H13
        FogObjectOrder = &H14
        '_unseen0x15 = &H15
        ChangeSelection = &H16
        AssignGroupHotkey = &H17
        SelectGroupHotkey = &H18
        SelectSubGroup = &H19
        PreSubGroupSelection = &H1A
        TriggerSelectionEvent = &H1B
        SelectGroundItem = &H1C
        CancelHeroRevive = &H1D
        DequeueBuildingOrder = &H1E

        'Cheat actions [0x20 to 0x32 (spaced to 0x4F)]
        CheatFastCooldown = &H20
        '_unseen0x21 = &H21
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

        'Alliance actions [0x50 to 0x51 (spaced to 0x5F)]
        ChangeAllyOptions = &H50
        TransferResources = &H51

        'Trigger related actions [0x60 to 0x75] [note: TriggerSelectionEvent is at 0x1B in the order actions] 
        TriggerChatEvent = &H60
        PressedEscape = &H61
        TriggerWaitFinished = &H62
        '_unseen0x63 = &H63
        TriggerMouseClickedTrackable = &H64
        TriggerMouseTouchedTrackable = &H65
        EnterChooseHeroSkillSubmenu = &H66
        EnterChooseBuildingSubmenu = &H67
        MinimapPing = &H68
        DialogButtonClicked = &H69
        DialogAnyButtonClicked = &H6A
        GameCacheSyncInteger = &H6B
        GameCacheSyncReal = &H6C
        GameCacheSyncBoolean = &H6D
        GameCacheSyncUnit = &H6E
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        GameCacheSyncString = &H6F
        GameCacheSyncEmptyInteger = &H70
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        GameCacheSyncEmptyString = &H71
        GameCacheSyncEmptyBoolean = &H72
        GameCacheSyncEmptyUnit = &H73
        GameCacheSyncEmptyReal = &H74
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
        Unknown5 = 1 << 5 'seen in farseer summon wolf
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

    <DebuggerDisplay("{ToString}")>
    Public Structure GameObjectId
        Implements IEquatable(Of GameObjectId)

        Public ReadOnly AllocatedId As UInteger
        Public ReadOnly CounterId As UInteger
        Public Sub New(ByVal allocatedId As UInteger, ByVal counterId As UInteger)
            Me.AllocatedId = allocatedId
            Me.CounterId = counterId
        End Sub

        Public Overrides Function GetHashCode() As Integer
            Return CounterId.GetHashCode
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            If Not TypeOf obj Is GameObjectId Then Return False
            Return Me.Equals(CType(obj, GameObjectId))
        End Function
        Public Overloads Function Equals(ByVal other As GameObjectId) As Boolean Implements IEquatable(Of GameObjectId).Equals
            Return Me.AllocatedId = other.AllocatedId AndAlso Me.CounterId = other.CounterId
        End Function

        Public Shared Operator =(ByVal value1 As GameObjectId, ByVal value2 As GameObjectId) As Boolean
            Return value1.equals(value2)
        End Operator
        Public Shared Operator <>(ByVal value1 As GameObjectId, ByVal value2 As GameObjectId) As Boolean
            Return Not value1 = value2
        End Operator

        Public Overrides Function ToString() As String
            If AllocatedId = UInt32.MaxValue AndAlso CounterId = UInt32.MaxValue Then Return "[none]"
            Return "allocated id: {0}, counter id: {1}".Frmt(AllocatedId, CounterId)
        End Function
    End Structure

    'verification disabled because this class causes the verifier to go OutOfMemory
    <ContractVerification(False)>
    Public NotInheritable Class GameActions
        Private Sub New()
        End Sub

        Public Class SimpleDefinition
            Inherits TupleJar
            Public ReadOnly id As GameActionId
            Public Sub New(ByVal id As GameActionId, ByVal ParamArray subJars() As IJar(Of Object))
                MyBase.New(id.ToString, subJars)
                Contract.Requires(subJars IsNot Nothing)
                Me.id = id
            End Sub
        End Class

        Public Shared ReadOnly DecreaseGameSpeed As New SimpleDefinition(GameActionId.DecreaseGameSpeed)
        Public Shared ReadOnly IncreaseGameSpeed As New SimpleDefinition(GameActionId.IncreaseGameSpeed)
        Public Shared ReadOnly PauseGame As New SimpleDefinition(GameActionId.PauseGame)
        Public Shared ReadOnly ResumeGame As New SimpleDefinition(GameActionId.ResumeGame)
        Public Shared ReadOnly SaveGameFinished As New SimpleDefinition(GameActionId.SaveGameFinished,
                    New UInt32Jar("unknown").Weaken)
        Public Shared ReadOnly SaveGameStarted As New SimpleDefinition(GameActionId.SaveGameStarted,
                    New NullTerminatedStringJar("filename").Weaken)
        Public Shared ReadOnly SetGameSpeed As New SimpleDefinition(GameActionId.SetGameSpeed,
                    New EnumByteJar(Of GameSpeedSetting)("speed").Weaken)

        Public Shared ReadOnly SelfOrder As New SimpleDefinition(GameActionId.SelfOrder,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken)
        Public Shared ReadOnly PointOrder As New SimpleDefinition(GameActionId.PointOrder,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken,
                    New Float32Jar("target x").Weaken,
                    New Float32Jar("target y").Weaken)
        Public Shared ReadOnly ObjectOrder As New SimpleDefinition(GameActionId.ObjectOrder,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken,
                    New Float32Jar("x").Weaken,
                    New Float32Jar("y").Weaken,
                    New GameObjectIdJar("target").Weaken)
        Public Shared ReadOnly DropOrGiveItem As New SimpleDefinition(GameActionId.DropOrGiveItem,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken,
                    New Float32Jar("x").Weaken,
                    New Float32Jar("y").Weaken,
                    New GameObjectIdJar("receiver").Weaken,
                    New GameObjectIdJar("item").Weaken)
        Public Shared ReadOnly FogObjectOrder As New SimpleDefinition(GameActionId.FogObjectOrder,
                    New EnumUInt16Jar(Of OrderTypes)("flags").Weaken,
                    New OrderTypeJar("order").Weaken,
                    New GameObjectIdJar("unknown").Weaken,
                    New Float32Jar("x").Weaken,
                    New Float32Jar("y").Weaken,
                    New ObjectTypeJar("target type").Weaken,
                    New UInt64Jar("target flags", showhex:=True).Weaken,
                    New ByteJar("target owner").Weaken,
                    New Float32Jar("target x").Weaken,
                    New Float32Jar("target y").Weaken)

        Public Shared ReadOnly EnterChooseHeroSkillSubmenu As New SimpleDefinition(GameActionId.EnterChooseHeroSkillSubmenu)
        Public Shared ReadOnly EnterChooseBuildingSubmenu As New SimpleDefinition(GameActionId.EnterChooseBuildingSubmenu)
        Public Shared ReadOnly PressedEscape As New SimpleDefinition(GameActionId.PressedEscape)
        Public Shared ReadOnly CancelHeroRevive As New SimpleDefinition(GameActionId.CancelHeroRevive,
                    New GameObjectIdJar("target").Weaken)
        Public Shared ReadOnly DequeueBuildingOrder As New SimpleDefinition(GameActionId.DequeueBuildingOrder,
                    New ByteJar("slot number").Weaken,
                    New ObjectTypeJar("type").Weaken)
        Public Shared ReadOnly MinimapPing As New SimpleDefinition(GameActionId.MinimapPing,
                    New Float32Jar("x").Weaken,
                    New Float32Jar("y").Weaken,
                    New Float32Jar("duration").Weaken)

        Public Shared ReadOnly ChangeAllyOptions As New SimpleDefinition(GameActionId.ChangeAllyOptions,
                    New ByteJar("player slot id").Weaken,
                    New EnumUInt32Jar(Of AllianceTypes)("flags").Weaken)
        Public Shared ReadOnly TransferResources As New SimpleDefinition(GameActionId.TransferResources,
                    New ByteJar("player slot id").Weaken,
                    New UInt32Jar("gold").Weaken,
                    New UInt32Jar("lumber").Weaken)

        Public Shared ReadOnly AssignGroupHotkey As New SimpleDefinition(GameActionId.AssignGroupHotkey,
                    New ByteJar("group index").Weaken,
                    New GameObjectIdJar("target").RepeatedWithCountPrefix("targets", prefixSize:=2).Weaken)
        Public Shared ReadOnly ChangeSelection As New SimpleDefinition(GameActionId.ChangeSelection,
                    New EnumByteJar(Of SelectionOperation)("operation").Weaken,
                    New GameObjectIdJar("target").RepeatedWithCountPrefix("targets", prefixSize:=2).Weaken)
        Public Shared ReadOnly PreSubGroupSelection As New SimpleDefinition(GameActionId.PreSubGroupSelection)
        Public Shared ReadOnly SelectGroundItem As New SimpleDefinition(GameActionId.SelectGroundItem,
                    New ByteJar("unknown").Weaken,
                    New GameObjectIdJar("target").Weaken)
        Public Shared ReadOnly SelectGroupHotkey As New SimpleDefinition(GameActionId.SelectGroupHotkey,
                    New ByteJar("group index").Weaken,
                    New ByteJar("unknown").Weaken)
        Public Shared ReadOnly SelectSubGroup As New SimpleDefinition(GameActionId.SelectSubGroup,
                    New ObjectTypeJar("unit type").Weaken,
                    New GameObjectIdJar("target").Weaken)

        Public Shared ReadOnly CheatDisableTechRequirements As New SimpleDefinition(GameActionId.CheatDisableTechRequirements)
        Public Shared ReadOnly CheatDisableVictoryConditions As New SimpleDefinition(GameActionId.CheatDisableVictoryConditions)
        Public Shared ReadOnly CheatEnableResearch As New SimpleDefinition(GameActionId.CheatEnableResearch)
        Public Shared ReadOnly CheatFastCooldown As New SimpleDefinition(GameActionId.CheatFastCooldown)
        Public Shared ReadOnly CheatFastDeathDecay As New SimpleDefinition(GameActionId.CheatFastDeathDecay)
        Public Shared ReadOnly CheatGodMode As New SimpleDefinition(GameActionId.CheatGodMode)
        Public Shared ReadOnly CheatGold As New SimpleDefinition(GameActionId.CheatGold,
                    New ByteJar("unknown").Weaken,
                    New UInt32Jar("amount").Weaken)
        Public Shared ReadOnly CheatGoldAndLumber As New SimpleDefinition(GameActionId.CheatGoldAndLumber,
                    New ByteJar("unknown").Weaken,
                    New UInt32Jar("amount").Weaken)
        Public Shared ReadOnly CheatInstantDefeat As New SimpleDefinition(GameActionId.CheatInstantDefeat)
        Public Shared ReadOnly CheatInstantVictory As New SimpleDefinition(GameActionId.CheatInstantVictory)
        Public Shared ReadOnly CheatLumber As New SimpleDefinition(GameActionId.CheatLumber,
                    New ByteJar("unknown").Weaken,
                    New UInt32Jar("amount").Weaken)
        Public Shared ReadOnly CheatNoDefeat As New SimpleDefinition(GameActionId.CheatNoDefeat)
        Public Shared ReadOnly CheatNoFoodLimit As New SimpleDefinition(GameActionId.CheatNoFoodLimit)
        Public Shared ReadOnly CheatRemoveFogOfWar As New SimpleDefinition(GameActionId.CheatRemoveFogOfWar)
        Public Shared ReadOnly CheatResearchUpgrades As New SimpleDefinition(GameActionId.CheatResearchUpgrades)
        Public Shared ReadOnly CheatSetTimeOfDay As New SimpleDefinition(GameActionId.CheatSetTimeOfDay,
                    New Float32Jar("time").Weaken)
        Public Shared ReadOnly CheatSpeedConstruction As New SimpleDefinition(GameActionId.CheatSpeedConstruction)
        Public Shared ReadOnly CheatUnlimitedMana As New SimpleDefinition(GameActionId.CheatUnlimitedMana)

        Public Shared ReadOnly TriggerArrowKeyEvent As New SimpleDefinition(GameActionId.TriggerArrowKeyEvent,
                    New EnumByteJar(Of ArrowKeyEvent)("event type").Weaken)
        Public Shared ReadOnly TriggerChatEvent As New SimpleDefinition(GameActionId.TriggerChatEvent,
                    New GameObjectIdJar("trigger event").Weaken,
                    New NullTerminatedStringJar("text").Weaken)
        Public Shared ReadOnly DialogAnyButtonClicked As New SimpleDefinition(GameActionId.DialogAnyButtonClicked,
                    New GameObjectIdJar("dialog").Weaken,
                    New GameObjectIdJar("button").Weaken)
        Public Shared ReadOnly DialogButtonClicked As New SimpleDefinition(GameActionId.DialogButtonClicked,
                    New GameObjectIdJar("button").Weaken,
                    New GameObjectIdJar("dialog").Weaken)
        Public Shared ReadOnly TriggerMouseClickedTrackable As New SimpleDefinition(GameActionId.TriggerMouseClickedTrackable,
                    New GameObjectIdJar("trackable").Weaken)
        Public Shared ReadOnly TriggerMouseTouchedTrackable As New SimpleDefinition(GameActionId.TriggerMouseTouchedTrackable,
                    New GameObjectIdJar("trackable").Weaken)
        Public Shared ReadOnly TriggerSelectionEvent As New SimpleDefinition(GameActionId.TriggerSelectionEvent,
                    New EnumByteJar(Of SelectionOperation)("operation").Weaken,
                    New GameObjectIdJar("target").Weaken)
        Public Shared ReadOnly TriggerWaitFinished As New SimpleDefinition(GameActionId.TriggerWaitFinished,
                    New GameObjectIdJar("trigger thread").Weaken,
                    New UInt32Jar("thread wait count").Weaken)

        Public Shared ReadOnly GameCacheSyncInteger As New SimpleDefinition(GameActionId.GameCacheSyncInteger,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken,
                    New UInt32Jar("value").Weaken)
        Public Shared ReadOnly GameCacheSyncBoolean As New SimpleDefinition(GameActionId.GameCacheSyncBoolean,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken,
                    New UInt32Jar("value").Weaken)
        Public Shared ReadOnly GameCacheSyncReal As New SimpleDefinition(GameActionId.GameCacheSyncReal,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken,
                    New Float32Jar("value").Weaken)
        Public Shared ReadOnly GameCacheSyncUnit As New SimpleDefinition(GameActionId.GameCacheSyncUnit,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken,
                    New ObjectTypeJar("unit type").Weaken,
                    New TupleJar("item slot", True,
                            New ObjectTypeJar("item").Weaken,
                            New UInt32Jar("charges").Weaken,
                            New UInt32Jar("unknown").Weaken
                        ).RepeatedWithCountPrefix("inventory", prefixSize:=4).Weaken,
                    New UInt32Jar("experience").Weaken,
                    New UInt32Jar("level ups").Weaken,
                    New UInt32Jar("skill points").Weaken,
                    New UInt16Jar("proper name index").Weaken,
                    New UInt16Jar("unknown1").Weaken,
                    New UInt32Jar("base strength").Weaken,
                    New Float32Jar("bonus strength per level").Weaken,
                    New UInt32Jar("base agility").Weaken,
                    New Float32Jar("bonus move speed").Weaken,
                    New Float32Jar("bonus attack speed").Weaken,
                    New Float32Jar("bonus agility per level").Weaken,
                    New UInt32Jar("base intelligence").Weaken,
                    New Float32Jar("bonus intelligence per level").Weaken,
                    New TupleJar("skill slot", True,
                            New ObjectTypeJar("ability").Weaken,
                            New UInt32Jar("level").Weaken
                        ).RepeatedWithCountPrefix("hero skills", prefixSize:=4).Weaken,
                    New Float32Jar("bonus health").Weaken,
                    New Float32Jar("bonus mana").Weaken,
                    New Float32Jar("sight radius (day)").Weaken,
                    New UInt32Jar("unknown2").Weaken,
                    New RawDataJar("unknown3", Size:=4).Weaken,
                    New RawDataJar("unknown4", Size:=4).Weaken,
                    New RawDataJar("unknown5", Size:=4).Weaken,
                    New UInt16Jar("hotkey flags", showhex:=True).Weaken)
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        Public Shared ReadOnly GameCacheSyncString As New SimpleDefinition(GameActionId.GameCacheSyncString,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken,
                    New NullTerminatedStringJar("value").Weaken)

        Public Shared ReadOnly GameCacheSyncEmptyInteger As New SimpleDefinition(GameActionId.GameCacheSyncEmptyInteger,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken)
        Public Shared ReadOnly GameCacheSyncEmptyBoolean As New SimpleDefinition(GameActionId.GameCacheSyncEmptyBoolean,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken)
        Public Shared ReadOnly GameCacheSyncEmptyReal As New SimpleDefinition(GameActionId.GameCacheSyncEmptyReal,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken)
        Public Shared ReadOnly GameCacheSyncEmptyUnit As New SimpleDefinition(GameActionId.GameCacheSyncEmptyUnit,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken)
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        Public Shared ReadOnly GameCacheSyncEmptyString As New SimpleDefinition(GameActionId.GameCacheSyncEmptyString,
                    New NullTerminatedStringJar("filename").Weaken,
                    New NullTerminatedStringJar("mission key").Weaken,
                    New NullTerminatedStringJar("key").Weaken)

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
