Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class GameAction
        Private Shared ReadOnly ActionJar As PrefixSwitchJar(Of GameActionId) = MakeJar()
        Public ReadOnly id As GameActionId
        Private ReadOnly _payload As IPickle(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(ByVal payload As IPickle(Of PrefixPickle(Of GameActionId)))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload.Value.Payload
            Me.id = payload.Value.Key
        End Sub

        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Private Shared Sub reg(ByVal jar As PrefixSwitchJar(Of GameActionId),
                               ByVal definition As GameActions.SimpleDefinition)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(definition IsNot Nothing)
            jar.AddPackerParser(definition.id, definition.Weaken)
        End Sub

        Private Shared Function MakeJar() As PrefixSwitchJar(Of GameActionId)
            Contract.Ensures(Contract.Result(Of PrefixSwitchJar(Of GameActionId))() IsNot Nothing)
            Dim jar = New PrefixSwitchJar(Of GameActionId)("W3GameAction")

            reg(jar, GameActions.PauseGame)
            reg(jar, GameActions.ResumeGame)
            reg(jar, GameActions.SetGameSpeed)
            reg(jar, GameActions.IncreaseGameSpeed)
            reg(jar, GameActions.DecreaseGameSpeed)
            reg(jar, GameActions.SaveGameStarted)
            reg(jar, GameActions.SaveGameFinished)
            reg(jar, GameActions.SelfOrder)
            reg(jar, GameActions.PointOrder)
            reg(jar, GameActions.ObjectOrder)
            reg(jar, GameActions.DropOrGiveItem)
            reg(jar, GameActions.FogObjectOrder)
            reg(jar, GameActions.EnterChooseHeroSkillSubmenu)
            reg(jar, GameActions.EnterChooseBuildingSubmenu)
            reg(jar, GameActions.PressedEscape)
            reg(jar, GameActions.CancelHeroRevive)
            reg(jar, GameActions.DequeueBuildingOrder)
            reg(jar, GameActions.MinimapPing)
            reg(jar, GameActions.ChangeAllyOptions)
            reg(jar, GameActions.TransferResources)
            reg(jar, GameActions.ChangeSelection)
            reg(jar, GameActions.AssignGroupHotkey)
            reg(jar, GameActions.SelectGroupHotkey)
            reg(jar, GameActions.SelectSubGroup)
            reg(jar, GameActions.PreSubGroupSelection)
            reg(jar, GameActions.SelectGroundItem)
            reg(jar, GameActions.CheatDisableTechRequirements)
            reg(jar, GameActions.CheatDisableVictoryConditions)
            reg(jar, GameActions.CheatEnableResearch)
            reg(jar, GameActions.CheatFastCooldown)
            reg(jar, GameActions.CheatFastDeathDecay)
            reg(jar, GameActions.CheatGodMode)
            reg(jar, GameActions.CheatInstantDefeat)
            reg(jar, GameActions.CheatInstantVictory)
            reg(jar, GameActions.CheatNoDefeat)
            reg(jar, GameActions.CheatNoFoodLimit)
            reg(jar, GameActions.CheatRemoveFogOfWar)
            reg(jar, GameActions.CheatResearchUpgrades)
            reg(jar, GameActions.CheatSpeedConstruction)
            reg(jar, GameActions.CheatUnlimitedMana)
            reg(jar, GameActions.CheatSetTimeOfDay)
            reg(jar, GameActions.CheatGold)
            reg(jar, GameActions.CheatGoldAndLumber)
            reg(jar, GameActions.CheatLumber)
            reg(jar, GameActions.TriggerChatEvent)
            reg(jar, GameActions.TriggerWaitFinished)
            reg(jar, GameActions.TriggerMouseTouchedTrackable)
            reg(jar, GameActions.TriggerMouseClickedTrackable)
            reg(jar, GameActions.TriggerDialogButtonClicked)
            reg(jar, GameActions.TriggerDialogButtonClicked2)
            reg(jar, GameActions.TriggerArrowKeyEvent)
            reg(jar, GameActions.TriggerSelectionEvent)
            reg(jar, GameActions.GameCacheSyncInteger)
            reg(jar, GameActions.GameCacheSyncBoolean)
            reg(jar, GameActions.GameCacheSyncReal)
            reg(jar, GameActions.GameCacheSyncUnit)
            reg(jar, GameActions.GameCacheSyncString)
            reg(jar, GameActions.GameCacheSyncEmptyInteger)
            reg(jar, GameActions.GameCacheSyncEmptyBoolean)
            reg(jar, GameActions.GameCacheSyncEmptyReal)
            reg(jar, GameActions.GameCacheSyncEmptyUnit)
            reg(jar, GameActions.GameCacheSyncEmptyString)
            Return jar
        End Function

        Public Shared Function FromData(ByVal data As IReadableList(Of Byte)) As GameAction
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Return New GameAction(ActionJar.Parse(data))
        End Function

        Public Overrides Function ToString() As String
            Return "{0} = {1}".Frmt(id, Payload.Description.Value())
        End Function
    End Class

    Public NotInheritable Class W3GameActionJar
        Inherits BaseJar(Of GameAction)
        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Public Overrides Function Pack(Of TValue As GameAction)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Return New Pickle(Of TValue)(Name, value, Concat({value.id}, value.Payload.Data.ToArray).AsReadableList)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of GameAction)
            Dim val = GameAction.FromData(data)
            Dim datum = data.SubView(0, val.Payload.Data.Count + 1) 'include the id
            Return New Pickle(Of GameAction)(Name, val, datum)
        End Function
    End Class
End Namespace
