Imports Tinker.Pickling

Namespace WC3.Protocol
    '''<summary>Game actions which can be performed by players.</summary>
    '''<original-source> http://www.wc3c.net/tools/specs/W3GActions.txt </original-source>
    '''<remarks>Ids prefixed with 'Trigger' only occur if there is a trigger to catch them.</remarks>
    Public Enum GameActionId As Byte
        '''<summary>Occurs when a player pauses the game (no effect per player after three times in multiplayer).</summary>
        '''<remarks>Entering a menu in singleplayer counts (as do other things which automatically hold the game).</remarks>
        PauseGame = &H1
        '''<summary>Occurs when a player unpauses the game.</summary>
        ResumeGame = &H2
        '''<summary>Occurs when a player sets the game speed (no effect in multiplayer).</summary>
        SetGameSpeed = &H3
        '''<summary>Occurs when a player increases the game speed (by hitting +, no effect in multiplayer).</summary>
        IncreaseGameSpeed = &H4
        '''<summary>Occurs when a player decreases the game speed (by hitting -, no effect in multiplayer).</summary>
        DecreaseGameSpeed = &H5
        '''<summary>Occurs when a player initiate a save game.</summary>
        SaveGameStarted = &H6
        '''<summary>Occurs when a player has finished performing a save game.</summary>
        SaveGameFinished = &H7

        '''<summary>Occurs when a player issues an order with no target (eg. berserk/autocaston/etc).</summary>
        SelfOrder = &H10
        '''<summary>Occurs when a player issues an order targeting a point (eg. attackground/earthquake/etc).</summary>
        PointOrder = &H11
        '''<summary>Occurs when a player issues an order targeting an object (eg. stormbolt/heal/harvest/etc).</summary>
        ObjectOrder = &H12
        '''<summary>Occurs when a player issues an order to drop or give an item.</summary>
        DropOrGiveItem = &H13
        '''<summary>Occurs when a player issues an order targeting a thing they see behind fog of war (eg. a tree or previously spotted building).</summary>
        FogObjectOrder = &H14
        '_unseen0x15 = &H15
        '''<summary>Occurs when the set of units a player has selected changes (including when a selected unit dies).</summary>
        ChangeSelection = &H16
        '''<summary>Occurs when a player assigns a unit group to a number key.</summary>
        AssignGroupHotkey = &H17
        '''<summary>Occurs when a player hits a number key to select one of their assigned unit groups.</summary>
        SelectGroupHotkey = &H18
        '''<summary>Occurs when a player changes the selected subgroup (eg. hitting tab to work with a different unit type from the group selected).</summary>
        SelectSubGroup = &H19
        '''<summary>Seems to always occur before the SelectSubGroup action. Unsure what this action is for.</summary>
        PreSubGroupSelection = &H1A
        '''<summary>Occurs when a player selects a unit, but only if there is a trigger waiting for the event.</summary>
        TriggerSelectionEvent = &H1B
        '''<summary>Occurs when a player selects an item (or doodad?) on the ground.</summary>
        SelectGroundItem = &H1C
        '''<summary>Occurs when a player cancels a hero revival.</summary>
        CancelHeroRevive = &H1D
        '''<summary>Occurs when a player cancels/dequeues a building training/research order.</summary>
        DequeueBuildingOrder = &H1E

        '''<summary>Occurs when a player uses the TheDudeAbides cheat (no effect in multiplayer).</summary>
        CheatFastCooldown = &H20
        '_unseen0x21 = &H21
        '''<summary>Occurs when a player uses the SomebodySetUpUsTheBomb cheat (no effect in multiplayer).</summary>
        CheatInstantDefeat = &H22
        '''<summary>Occurs when a player uses the WarpTen cheat (no effect in multiplayer).</summary>
        CheatSpeedConstruction = &H23
        '''<summary>Occurs when a player uses the IocainePowder cheat (no effect in multiplayer).</summary>
        CheatFastDeathDecay = &H24
        '''<summary>Occurs when a player uses the PointBreak cheat (no effect in multiplayer).</summary>
        CheatNoFoodLimit = &H25
        '''<summary>Occurs when a player uses the WhosYourDaddy cheat (no effect in multiplayer).</summary>
        CheatGodMode = &H26
        '''<summary>Occurs when a player uses the KeyserSoze cheat (no effect in multiplayer).</summary>
        CheatGold = &H27
        '''<summary>Occurs when a player uses the LeafItToMe cheat (no effect in multiplayer).</summary>
        CheatLumber = &H28
        '''<summary>Occurs when a player uses the ThereIsNoSpoon cheat (no effect in multiplayer).</summary>
        CheatUnlimitedMana = &H29
        '''<summary>Occurs when a player uses the StrengthAndHonor cheat (no effect in multiplayer).</summary>
        CheatNoDefeat = &H2A
        '''<summary>Occurs when a player uses the ItVexesMe cheat (no effect in multiplayer).</summary>
        CheatDisableVictoryConditions = &H2B
        '''<summary>Occurs when a player uses the WhoIsJohnGalt cheat (no effect in multiplayer).</summary>
        CheatEnableResearch = &H2C
        '''<summary>Occurs when a player uses the GreedIsGood cheat (no effect in multiplayer).</summary>
        CheatGoldAndLumber = &H2D
        '''<summary>Occurs when a player uses the RiseAndShine/LightsOut/DayLightSavings cheats (no effect in multiplayer).</summary>
        CheatSetTimeOfDay = &H2E
        '''<summary>Occurs when a player uses the ISeeDeadPeople cheat (no effect in multiplayer).</summary>
        CheatRemoveFogOfWar = &H2F
        '''<summary>Occurs when a player uses the Synergy cheat (no effect in multiplayer).</summary>
        CheatDisableTechRequirements = &H30
        '''<summary>Occurs when a player uses the SharpAndShiny cheat (no effect in multiplayer).</summary>
        CheatResearchUpgrades = &H31
        '''<summary>Occurs when a player uses the AllYourBaseAreBelongToUs cheat (no effect in multiplayer).</summary>
        CheatInstantVictory = &H32

        '''<summary>Occurs when a player changes alliance options.</summary>
        ChangeAllyOptions = &H50
        '''<summary>Occurs when a player sends resources to another player.</summary>
        TransferResources = &H51

        '''<summary>Occurs when a player says something, but only if the thing said matches a trigger chat event filter.</summary>
        TriggerChatEvent = &H60
        '''<summary>Occurs when a player presses the escape key.</summary>
        PressedEscape = &H61
        '''<summary>Occurs when a TriggerSleepAction finishes.</summary>
        '''<remarks>This action exists to avoid desyncs because TriggerSleepAction inexplicably waits in real time instead of game time.</remarks>
        TriggerWaitFinished = &H62
        '_unseen0x63 = &H63
        '''<summary>Occurs when a player clicks a trackable, but only if there is a trigger waiting for the event.</summary>
        TriggerMouseClickedTrackable = &H64
        '''<summary>Occurs when a player mouses over a trackable, but only if there is a trigger waiting for the event.</summary>
        TriggerMouseTouchedTrackable = &H65
        '''<summary>Occurs when a player enters the skills sub-menu of a hero.</summary>
        EnterChooseHeroSkillSubmenu = &H66
        '''<summary>Occurs when a player enters the construction sub-menu of a builder.</summary>
        EnterChooseBuildingSubmenu = &H67
        '''<summary>Occurs when a player pings the minimap.</summary>
        MinimapPing = &H68
        '''<summary>Occurs when a player clicks a dialog button, whether or not a trigger is waiting for the event.</summary>
        '''<remarks>
        ''' This action is always paired with DialogAnyButtonClicked by wc3.
        ''' However, if just this action is sent, only the 'specific button' type trigger events will fire.
        '''</remarks>
        DialogButtonClicked = &H69
        '''<summary>Occurs when a player clicks a dialog button, whether or not a trigger is waiting for the event.</summary>
        '''<remarks>
        ''' This action is always paired with DialogButtonClicked by wc3.
        ''' However, if just this action is sent, only the 'any button on dialog' type trigger events will fire.
        '''</remarks>
        DialogAnyButtonClicked = &H6A
        '''<summary>Occurs when the map syncs an integer in game cache.</summary>
        GameCacheSyncInteger = &H6B
        '''<summary>Occurs when the map syncs a real in game cache.</summary>
        GameCacheSyncReal = &H6C
        '''<summary>Occurs when the map syncs a boolean in game cache.</summary>
        GameCacheSyncBoolean = &H6D
        '''<summary>Occurs when the map syncs a unit in game cache.</summary>
        GameCacheSyncUnit = &H6E
        '''<summary>Occurs when the map syncs a string in game cache.</summary>
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        GameCacheSyncString = &H6F
        '''<summary>Occurs when the map syncs an integer without an assigned value in game cache.</summary>
        GameCacheSyncEmptyInteger = &H70
        '''<summary>Occurs when the map syncs a string without an assigned value in game cache.</summary>
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        GameCacheSyncEmptyString = &H71
        '''<summary>Occurs when the map syncs a boolean without an assigned value in game cache.</summary>
        GameCacheSyncEmptyBoolean = &H72
        '''<summary>Occurs when the map syncs a unit without an assigned value in game cache.</summary>
        GameCacheSyncEmptyUnit = &H73
        '''<summary>Occurs when the map syncs a real without an assigned value in game cache.</summary>
        GameCacheSyncEmptyReal = &H74
        '''<summary>Occurs when a player presses or releases an arrow key, but only if there is a trigger waiting for the event.</summary>
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
        Summon = 1 << 5
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
        '? = &HD0000
        '? = &HD0001
        '? = &HD0002
        Smart = &HD0003 'right-click
        [Stop] = &HD0004
        Stunned = &HD0005
        '? = &HD0006
        '? = &HD0007
        Cancel = &HD0008
        '? = &HD0009
        '? = &HD000A
        '? = &HD000B
        SetRally = &HD000C
        GetItem = &HD000D
        '? = &HD000E
        Attack = &HD000F
        AttackGround = &HD0010
        AttackOnce = &HD0011
        Move = &HD0012
        '? = &HD0013
        AIMove = &HD0014
        '? = &HD0015
        Patrol = &HD0016
        '? = &HD0017
        '? = &HD0018
        HoldPosition = &HD0019
        BuildMenu = &HD001A
        HumanBuild = &HD001B
        OrcBuild = &HD001C
        NightElfBuild = &HD001D
        UndeadBuild = &HD001E
        ResumeBuild = &HD001F
        SkillMenu = &HD0020
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
        '? = &HD002E
        DetectAOE = &HD002F
        '? = &HD0030
        ResumeHarvest = &HD0031
        Harvest = &HD0032
        '? = &HD0033
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
            Return Me.Equals(DirectCast(obj, GameObjectId))
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
        Public Shared Function Parse(ByVal text As String) As GameObjectId
            Contract.Requires(text IsNot Nothing)
            If text = "[none]" Then Return New GameObjectId(UInt32.MaxValue, UInt32.MaxValue)
            If Not text Like "allocated id: #*, counter id: #*" Then Throw New FormatException("Not a recognized GameObjectId format.")
            Dim words = text.Split({": ", ", "}, StringSplitOptions.RemoveEmptyEntries)
            Return New GameObjectId(AllocatedId:=UInt32.Parse(words(1), NumberStyles.Integer, CultureInfo.InvariantCulture),
                                    CounterId:=UInt32.Parse(words(3), NumberStyles.Integer, CultureInfo.InvariantCulture))
        End Function
    End Structure

    'verification disabled because this class causes the verifier to go OutOfMemory
    <ContractVerification(False)>
    Public NotInheritable Class GameActions
        Private Sub New()
        End Sub

        Private Shared ReadOnly _allDefinitions As New List(Of Definition)
        Public Shared ReadOnly Property AllDefinitions As IEnumerable(Of Definition)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of Definition))() IsNot Nothing)
                Return _allDefinitions.AsReadOnly
            End Get
        End Property

        Public MustInherit Class Definition
            Private ReadOnly _id As GameActionId
            Private ReadOnly _jar As ISimpleJar

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_jar IsNot Nothing)
            End Sub

            Friend Sub New(ByVal id As GameActionId, ByVal jar As ISimpleJar)
                Contract.Requires(jar IsNot Nothing)
                Me._id = id
                Me._jar = jar
            End Sub

            Public ReadOnly Property Id As GameActionId
                Get
                    Return _id
                End Get
            End Property
            Public ReadOnly Property Jar As ISimpleJar
                Get
                    Contract.Ensures(Contract.Result(Of ISimpleJar)() IsNot Nothing)
                    Return _jar
                End Get
            End Property
        End Class
        Public NotInheritable Class Definition(Of T)
            Inherits Definition
            Private ReadOnly _jar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_jar IsNot Nothing)
            End Sub

            Friend Sub New(ByVal id As GameActionId, ByVal jar As IJar(Of T))
                MyBase.New(id, jar)
                Contract.Requires(jar IsNot Nothing)
                Me._jar = jar
            End Sub

            Public Shadows ReadOnly Property Jar As IJar(Of T)
                Get
                    Contract.Ensures(Contract.Result(Of IJar(Of T))() IsNot Nothing)
                    Return _jar
                End Get
            End Property
        End Class

        Private Shared Function IncludeDefinitionInAll(Of T As Definition)(ByVal def As T) As T
            Contract.Requires(def IsNot Nothing)
            Contract.Ensures(Contract.Result(Of T)() Is def)
            _allDefinitions.Add(def)
            Return def
        End Function
        Private Shared Function Define(Of T)(ByVal id As GameActionId, ByVal jar As IJar(Of T)) As Definition(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Definition(Of T))() IsNot Nothing)
            Return IncludeDefinitionInAll(New Definition(Of T)(id, jar))
        End Function
        Private Shared Function Define(ByVal id As GameActionId,
                                       ByVal jar1 As ISimpleNamedJar,
                                       ByVal jar2 As ISimpleNamedJar,
                                       ByVal ParamArray jars() As ISimpleNamedJar) As Definition(Of NamedValueMap)
            Contract.Requires(jar1 IsNot Nothing)
            Contract.Requires(jar2 IsNot Nothing)
            Contract.Requires(jars IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Definition(Of NamedValueMap))() IsNot Nothing)
            Return Define(id, New TupleJar(jars.Prepend(jar1, jar2).ToArray))
        End Function

        Public Shared ReadOnly DecreaseGameSpeed As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.DecreaseGameSpeed,
                    New EmptyJar())
        Public Shared ReadOnly IncreaseGameSpeed As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.IncreaseGameSpeed,
                    New EmptyJar())
        Public Shared ReadOnly PauseGame As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.PauseGame,
                    New EmptyJar())
        Public Shared ReadOnly ResumeGame As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.ResumeGame,
                    New EmptyJar())
        Public Shared ReadOnly SaveGameFinished As Definition(Of UInt32) = Define(GameActionId.SaveGameFinished,
                    New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly SaveGameStarted As Definition(Of String) = Define(GameActionId.SaveGameStarted,
                    New UTF8Jar().NullTerminated.Named("filename"))
        Public Shared ReadOnly SetGameSpeed As Definition(Of GameSpeedSetting) = Define(GameActionId.SetGameSpeed,
                    New EnumByteJar(Of GameSpeedSetting)().Named("speed"))

        Public Shared ReadOnly SelfOrder As Definition(Of NamedValueMap) = Define(GameActionId.SelfOrder,
                    New EnumUInt16Jar(Of OrderTypes)().Named("flags"),
                    New OrderIdJar().Named("order"),
                    New GameObjectIdJar().Named("unknown"))
        Public Shared ReadOnly PointOrder As Definition(Of NamedValueMap) = Define(GameActionId.PointOrder,
                    New EnumUInt16Jar(Of OrderTypes)().Named("flags"),
                    New OrderIdJar().Named("order"),
                    New GameObjectIdJar().Named("unknown"),
                    New Float32Jar().Named("target x"),
                    New Float32Jar().Named("target y"))
        Public Shared ReadOnly ObjectOrder As Definition(Of NamedValueMap) = Define(GameActionId.ObjectOrder,
                    New EnumUInt16Jar(Of OrderTypes)().Named("flags"),
                    New OrderIdJar().Named("order"),
                    New GameObjectIdJar().Named("unknown"),
                    New Float32Jar().Named("x"),
                    New Float32Jar().Named("y"),
                    New GameObjectIdJar().Named("target"))
        Public Shared ReadOnly DropOrGiveItem As Definition(Of NamedValueMap) = Define(GameActionId.DropOrGiveItem,
                    New EnumUInt16Jar(Of OrderTypes)().Named("flags"),
                    New OrderIdJar().Named("order"),
                    New GameObjectIdJar().Named("unknown"),
                    New Float32Jar().Named("x"),
                    New Float32Jar().Named("y"),
                    New GameObjectIdJar().Named("receiver"),
                    New GameObjectIdJar().Named("item"))
        Public Shared ReadOnly FogObjectOrder As Definition(Of NamedValueMap) = Define(GameActionId.FogObjectOrder,
                    New EnumUInt16Jar(Of OrderTypes)().Named("flags"),
                    New OrderIdJar().Named("order"),
                    New GameObjectIdJar().Named("unknown"),
                    New Float32Jar().Named("x"),
                    New Float32Jar().Named("y"),
                    New ObjectTypeJar().Named("target type"),
                    New UInt64Jar(showhex:=True).Named("target flags"),
                    New ByteJar().Named("target owner"),
                    New Float32Jar().Named("target x"),
                    New Float32Jar().Named("target y"))

        Public Shared ReadOnly EnterChooseHeroSkillSubmenu As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.EnterChooseHeroSkillSubmenu,
                    New EmptyJar())
        Public Shared ReadOnly EnterChooseBuildingSubmenu As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.EnterChooseBuildingSubmenu,
                    New EmptyJar())
        Public Shared ReadOnly PressedEscape As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.PressedEscape,
                    New EmptyJar())
        Public Shared ReadOnly CancelHeroRevive As Definition(Of GameObjectId) = Define(GameActionId.CancelHeroRevive,
                    New GameObjectIdJar().Named("target"))
        Public Shared ReadOnly DequeueBuildingOrder As Definition(Of NamedValueMap) = Define(GameActionId.DequeueBuildingOrder,
                    New ByteJar().Named("slot number"),
                    New ObjectTypeJar().Named("type"))
        Public Shared ReadOnly MinimapPing As Definition(Of NamedValueMap) = Define(GameActionId.MinimapPing,
                    New Float32Jar().Named("x"),
                    New Float32Jar().Named("y"),
                    New Float32Jar().Named("duration"))

        Public Shared ReadOnly ChangeAllyOptions As Definition(Of NamedValueMap) = Define(GameActionId.ChangeAllyOptions,
                    New ByteJar().Named("player slot id"),
                    New EnumUInt32Jar(Of AllianceTypes)().Named("flags"))
        Public Shared ReadOnly TransferResources As Definition(Of NamedValueMap) = Define(GameActionId.TransferResources,
                    New ByteJar().Named("player slot id"),
                    New UInt32Jar().Named("gold"),
                    New UInt32Jar().Named("lumber"))

        Public Shared ReadOnly AssignGroupHotkey As Definition(Of NamedValueMap) = Define(GameActionId.AssignGroupHotkey,
                    New ByteJar().Named("group index"),
                    New GameObjectIdJar().RepeatedWithCountPrefix(prefixSize:=2).Named("targets"))
        Public Shared ReadOnly ChangeSelection As Definition(Of NamedValueMap) = Define(GameActionId.ChangeSelection,
                    New EnumByteJar(Of SelectionOperation)().Named("operation"),
                    New GameObjectIdJar().RepeatedWithCountPrefix(prefixSize:=2).Named("targets"))
        Public Shared ReadOnly PreSubGroupSelection As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.PreSubGroupSelection,
                    New EmptyJar())
        Public Shared ReadOnly SelectGroundItem As Definition(Of NamedValueMap) = Define(GameActionId.SelectGroundItem,
                    New ByteJar().Named("unknown"),
                    New GameObjectIdJar().Named("target"))
        Public Shared ReadOnly SelectGroupHotkey As Definition(Of NamedValueMap) = Define(GameActionId.SelectGroupHotkey,
                    New ByteJar().Named("group index"),
                    New ByteJar().Named("unknown"))
        Public Shared ReadOnly SelectSubGroup As Definition(Of NamedValueMap) = Define(GameActionId.SelectSubGroup,
                    New ObjectTypeJar().Named("unit type"),
                    New GameObjectIdJar().Named("target"))

        Public Shared ReadOnly CheatDisableTechRequirements As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatDisableTechRequirements,
                    New EmptyJar())
        Public Shared ReadOnly CheatDisableVictoryConditions As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatDisableVictoryConditions,
                    New EmptyJar())
        Public Shared ReadOnly CheatEnableResearch As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatEnableResearch,
                    New EmptyJar())
        Public Shared ReadOnly CheatFastCooldown As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatFastCooldown,
                    New EmptyJar())
        Public Shared ReadOnly CheatFastDeathDecay As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatFastDeathDecay,
                    New EmptyJar())
        Public Shared ReadOnly CheatGodMode As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatGodMode,
                    New EmptyJar())
        Public Shared ReadOnly CheatGold As Definition(Of NamedValueMap) = Define(GameActionId.CheatGold,
                    New ByteJar().Named("unknown"),
                    New UInt32Jar().Named("amount"))
        Public Shared ReadOnly CheatGoldAndLumber As Definition(Of NamedValueMap) = Define(GameActionId.CheatGoldAndLumber,
                    New ByteJar().Named("unknown"),
                    New UInt32Jar().Named("amount"))
        Public Shared ReadOnly CheatInstantDefeat As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatInstantDefeat,
                    New EmptyJar())
        Public Shared ReadOnly CheatInstantVictory As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatInstantVictory,
                    New EmptyJar())
        Public Shared ReadOnly CheatLumber As Definition(Of NamedValueMap) = Define(GameActionId.CheatLumber,
                    New ByteJar().Named("unknown"),
                    New UInt32Jar().Named("amount"))
        Public Shared ReadOnly CheatNoDefeat As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatNoDefeat,
                    New EmptyJar())
        Public Shared ReadOnly CheatNoFoodLimit As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatNoFoodLimit,
                    New EmptyJar())
        Public Shared ReadOnly CheatRemoveFogOfWar As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatRemoveFogOfWar,
                    New EmptyJar())
        Public Shared ReadOnly CheatResearchUpgrades As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatResearchUpgrades,
                    New EmptyJar())
        Public Shared ReadOnly CheatSetTimeOfDay As Definition(Of Single) = Define(GameActionId.CheatSetTimeOfDay,
                    New Float32Jar().Named("time"))
        Public Shared ReadOnly CheatSpeedConstruction As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatSpeedConstruction,
                    New EmptyJar())
        Public Shared ReadOnly CheatUnlimitedMana As Definition(Of EmptyJar.EmptyValue) = Define(GameActionId.CheatUnlimitedMana,
                    New EmptyJar())

        Public Shared ReadOnly TriggerArrowKeyEvent As Definition(Of ArrowKeyEvent) = Define(GameActionId.TriggerArrowKeyEvent,
                    New EnumByteJar(Of ArrowKeyEvent)().Named("event type"))
        Public Shared ReadOnly TriggerChatEvent As Definition(Of NamedValueMap) = Define(GameActionId.TriggerChatEvent,
                    New GameObjectIdJar().Named("trigger event"),
                    New UTF8Jar().NullTerminated.Named("text"))
        Public Shared ReadOnly DialogAnyButtonClicked As Definition(Of NamedValueMap) = Define(GameActionId.DialogAnyButtonClicked,
                    New GameObjectIdJar().Named("dialog"),
                    New GameObjectIdJar().Named("button"))
        Public Shared ReadOnly DialogButtonClicked As Definition(Of NamedValueMap) = Define(GameActionId.DialogButtonClicked,
                    New GameObjectIdJar().Named("button"),
                    New GameObjectIdJar().Named("dialog"))
        Public Shared ReadOnly TriggerMouseClickedTrackable As Definition(Of GameObjectId) = Define(GameActionId.TriggerMouseClickedTrackable,
                    New GameObjectIdJar().Named("trackable"))
        Public Shared ReadOnly TriggerMouseTouchedTrackable As Definition(Of GameObjectId) = Define(GameActionId.TriggerMouseTouchedTrackable,
                    New GameObjectIdJar().Named("trackable"))
        Public Shared ReadOnly TriggerSelectionEvent As Definition(Of NamedValueMap) = Define(GameActionId.TriggerSelectionEvent,
                    New EnumByteJar(Of SelectionOperation)().Named("operation"),
                    New GameObjectIdJar().Named("target"))
        Public Shared ReadOnly TriggerWaitFinished As Definition(Of NamedValueMap) = Define(GameActionId.TriggerWaitFinished,
                    New GameObjectIdJar().Named("trigger thread"),
                    New UInt32Jar().Named("thread wait count"))

        Public Shared ReadOnly GameCacheSyncInteger As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncInteger,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"),
                    New UInt32Jar().Named("value"))
        Public Shared ReadOnly GameCacheSyncBoolean As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncBoolean,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"),
                    New UInt32Jar().Named("value"))
        Public Shared ReadOnly GameCacheSyncReal As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncReal,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"),
                    New Float32Jar().Named("value"))
        Public Shared ReadOnly GameCacheSyncUnit As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncUnit,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"),
                    New ObjectTypeJar().Named("unit type"),
                    New TupleJar(True,
                            New ObjectTypeJar().Named("item"),
                            New UInt32Jar().Named("charges"),
                            New UInt32Jar().Named("unknown")
                        ).RepeatedWithCountPrefix(prefixSize:=4).Named("inventory"),
                    New UInt32Jar().Named("experience"),
                    New UInt32Jar().Named("level ups"),
                    New UInt32Jar().Named("skill points"),
                    New UInt16Jar().Named("proper name index"),
                    New UInt16Jar().Named("unknown1"),
                    New UInt32Jar().Named("base strength"),
                    New Float32Jar().Named("bonus strength per level"),
                    New UInt32Jar().Named("base agility"),
                    New Float32Jar().Named("bonus move speed"),
                    New Float32Jar().Named("bonus attack speed"),
                    New Float32Jar().Named("bonus agility per level"),
                    New UInt32Jar().Named("base intelligence"),
                    New Float32Jar().Named("bonus intelligence per level"),
                    New TupleJar(True,
                            New ObjectTypeJar().Named("ability"),
                            New UInt32Jar().Named("level")
                        ).RepeatedWithCountPrefix(prefixSize:=4).Named("hero skills"),
                    New Float32Jar().Named("bonus health"),
                    New Float32Jar().Named("bonus mana"),
                    New Float32Jar().Named("sight radius (day)"),
                    New UInt32Jar().Named("unknown2"),
                    New DataJar().Fixed(exactDataCount:=4).Named("unknown3"),
                    New DataJar().Fixed(exactDataCount:=4).Named("unknown4"),
                    New DataJar().Fixed(exactDataCount:=4).Named("unknown5"),
                    New UInt16Jar(showhex:=True).Named("hotkey flags"))
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        Public Shared ReadOnly GameCacheSyncString As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncString,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"),
                    New UTF8Jar().NullTerminated.Named("value"))

        Public Shared ReadOnly GameCacheSyncEmptyInteger As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncEmptyInteger,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"))
        Public Shared ReadOnly GameCacheSyncEmptyBoolean As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncEmptyBoolean,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"))
        Public Shared ReadOnly GameCacheSyncEmptyReal As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncEmptyReal,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"))
        Public Shared ReadOnly GameCacheSyncEmptyUnit As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncEmptyUnit,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"))
        '''<remarks>This is a guess based on the other syncs. I've never actually recorded this packet (the jass function to trigger it has a bug).</remarks>
        Public Shared ReadOnly GameCacheSyncEmptyString As Definition(Of NamedValueMap) = Define(GameActionId.GameCacheSyncEmptyString,
                    New UTF8Jar().NullTerminated.Named("filename"),
                    New UTF8Jar().NullTerminated.Named("mission key"),
                    New UTF8Jar().NullTerminated.Named("key"))
    End Class
End Namespace
