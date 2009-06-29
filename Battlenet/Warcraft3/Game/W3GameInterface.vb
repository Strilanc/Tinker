Namespace Warcraft3
    Public Enum W3GameStates
        AcceptingPlayers = 0
        PreCounting = 1
        CountingDown = 2
        Loading = 3
        Playing = 4
        Closed = 5
    End Enum

    <ContractClass(GetType(ContractClassForIW3Game))>
    Public Interface IW3Game
        ReadOnly Property map() As W3Map
        ReadOnly Property server() As IW3Server
        ReadOnly Property name() As String
        ReadOnly Property logger() As Logger

        Function f_AdminPlayer() As IFuture(Of IW3Player)
        Function f_FakeHostPlayer() As IFuture(Of IW3Player)
        Function f_State() As IFuture(Of W3GameStates)
        Function f_BroadcastMessage(ByVal message As String) As IFuture
        Function f_SendMessageTo(ByVal message As String, ByVal player As IW3Player) As IFuture
        Function f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player)
        Function f_Close() As IFuture(Of Outcome)
        Function f_TryElevatePlayer(ByVal name As String, Optional ByVal password As String = Nothing) As IFuture(Of Outcome)
        Function f_EnumPlayers() As IFuture(Of List(Of IW3Player))
        Function f_BootSlot(ByVal query As String) As IFuture(Of Outcome)
        Function f_ThrowUpdated() As IFuture
        Function f_RemovePlayer(ByVal p As IW3Player, ByVal expected As Boolean, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) As IFuture(Of Outcome)
        Function f_ReceiveNonGameAction(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture

        Event ChangedState(ByVal sender As IW3Game, ByVal oldState As W3GameStates, ByVal newState As W3GameStates)
        Event Updated(ByVal sender As IW3Game, ByVal slots As List(Of W3Slot))
        Event PlayerTalked(ByVal sender As IW3Game, ByVal player As IW3Player, ByVal text As String)
        Event PlayerLeft(ByVal sender As IW3Game, ByVal gameState As W3GameStates, ByVal player As IW3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String)

        Function f_CommandProcessLocalText(ByVal text As String, ByVal logger As Logger) As IFuture
        Function f_CommandProcessText(ByVal player As IW3Player, ByVal text As String) As IFuture(Of Outcome)

#Region "Lobby"
        ReadOnly Property DownloadScheduler() As TransferScheduler(Of Byte)

        Function f_PlayerVoteToStart(ByVal name As String, ByVal val As Boolean) As IFuture(Of Outcome)
        Function f_StartCountdown() As IFuture(Of Outcome)
        Function f_TryAddPlayer(ByVal new_player As W3ConnectingPlayer) As IFuture(Of Outcome)
        Function f_OpenSlot(ByVal query As String) As IFuture(Of Outcome)
        Function f_CloseSlot(ByVal query As String) As IFuture(Of Outcome)
        Function f_ReserveSlot(ByVal query As String, ByVal username As String) As IFuture(Of Outcome)
        Function f_SwapSlotContents(ByVal query1 As String, ByVal query2 As String) As IFuture(Of Outcome)
        Function f_SetSlotCpu(ByVal query As String, ByVal c As W3Slot.ComputerLevel) As IFuture(Of Outcome)
        Function f_SetSlotLocked(ByVal query As String, ByVal new_lock_state As W3Slot.Lock) As IFuture(Of Outcome)
        Function f_SetAllSlotsLocked(ByVal new_lock_state As W3Slot.Lock) As IFuture(Of Outcome)
        Function f_SetSlotHandicap(ByVal query As String, ByVal new_handicap As Byte) As IFuture(Of Outcome)
        Function f_SetSlotTeam(ByVal query As String, ByVal new_team As Byte) As IFuture(Of Outcome)
        Function f_SetSlotRace(ByVal query As String, ByVal new_race As W3Slot.RaceFlags) As IFuture(Of Outcome)
        Function f_SetSlotColor(ByVal query As String, ByVal new_color As W3Slot.PlayerColor) As IFuture(Of Outcome)
        Function f_UpdatedGameState() As IFuture
        Function f_TrySetTeamSizes(ByVal sizes As IList(Of Integer)) As IFuture(Of Outcome)

        Event PlayerEntered(ByVal sender As IW3Game, ByVal player As IW3Player)
#End Region

#Region "Load Screen"
        Function f_ReceiveReady(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture
#End Region

#Region "Game Play"
        ReadOnly Property GameTime() As Integer

        Function f_DropLagger() As IFuture
        Function f_QueueGameData(ByVal sender As IW3Player, ByVal data() As Byte) As IFuture

        Property SettingGameRate As Double
        Property SettingLagLimit As Double
        Property SettingTickPeriod As Double

        Event PlayerSentData(ByVal game As IW3Game, ByVal player As IW3Player, ByVal data As Byte())
#End Region
    End Interface

    <ContractClassFor(GetType(IW3Game))>
    Public Class ContractClassForIW3Game
        Implements IW3Game

        Public Event ChangedState(ByVal sender As IW3Game, ByVal oldState As W3GameStates, ByVal newState As W3GameStates) Implements IW3Game.ChangedState
        Public Event PlayerEntered(ByVal sender As IW3Game, ByVal player As IW3Player) Implements IW3Game.PlayerEntered
        Public Event PlayerLeft(ByVal sender As IW3Game, ByVal gameState As W3GameStates, ByVal player As IW3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) Implements IW3Game.PlayerLeft
        Public Event PlayerSentData(ByVal game As IW3Game, ByVal player As IW3Player, ByVal data() As Byte) Implements IW3Game.PlayerSentData
        Public Event PlayerTalked(ByVal sender As IW3Game, ByVal player As IW3Player, ByVal text As String) Implements IW3Game.PlayerTalked
        Public Event Updated(ByVal sender As IW3Game, ByVal slots As System.Collections.Generic.List(Of W3Slot)) Implements IW3Game.Updated

        Public ReadOnly Property DownloadScheduler As TransferScheduler(Of Byte) Implements IW3Game.DownloadScheduler
            Get
                Contract.Ensures(Contract.Result(Of TransferScheduler(Of Byte))() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public Function f_AdminPlayer() As IFuture(Of IW3Player) Implements IW3Game.f_AdminPlayer
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Player))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_BootSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3Game.f_BootSlot
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_BroadcastMessage(ByVal message As String) As IFuture Implements IW3Game.f_BroadcastMessage
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_Close() As IFuture(Of Outcome) Implements IW3Game.f_Close
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_CloseSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3Game.f_CloseSlot
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_CommandProcessLocalText(ByVal text As String, ByVal logger As Logging.Logger) As IFuture Implements IW3Game.f_CommandProcessLocalText
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_CommandProcessText(ByVal player As IW3Player, ByVal text As String) As IFuture(Of Outcome) Implements IW3Game.f_CommandProcessText
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_DropLagger() As IFuture Implements IW3Game.f_DropLagger
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_EnumPlayers() As IFuture(Of List(Of IW3Player)) Implements IW3Game.f_EnumPlayers
            Contract.Ensures(Contract.Result(Of IFuture(Of List(Of IW3Player)))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_FakeHostPlayer() As IFuture(Of IW3Player) Implements IW3Game.f_FakeHostPlayer
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Player))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player) Implements IW3Game.f_FindPlayer
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Player))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_OpenSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3Game.f_OpenSlot
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_PlayerVoteToStart(ByVal name As String,
                                            ByVal val As Boolean) As IFuture(Of Outcome) Implements IW3Game.f_PlayerVoteToStart
            Contract.Requires(name IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_QueueGameData(ByVal sender As IW3Player,
                                        ByVal data() As Byte) As IFuture Implements IW3Game.f_QueueGameData
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_ReceiveNonGameAction(ByVal player As IW3Player,
                                               ByVal vals As Dictionary(Of String, Object)) As IFuture Implements IW3Game.f_ReceiveNonGameAction
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_ReceiveReady(ByVal player As IW3Player,
                                       ByVal vals As Dictionary(Of String, Object)) As IFuture Implements IW3Game.f_ReceiveReady
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_RemovePlayer(ByVal p As IW3Player, ByVal expected As Boolean, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) As IFuture(Of Outcome) Implements IW3Game.f_RemovePlayer
            Contract.Requires(p IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_ReserveSlot(ByVal query As String, ByVal username As String) As IFuture(Of Outcome) Implements IW3Game.f_ReserveSlot
            Contract.Requires(query IsNot Nothing)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SendMessageTo(ByVal message As String, ByVal player As IW3Player) As IFuture Implements IW3Game.f_SendMessageTo
            Contract.Requires(message IsNot Nothing)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SetAllSlotsLocked(ByVal newLockState As W3Slot.Lock) As IFuture(Of Outcome) Implements IW3Game.f_SetAllSlotsLocked
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SetSlotColor(ByVal query As String, ByVal new_color As W3Slot.PlayerColor) As IFuture(Of Outcome) Implements IW3Game.f_SetSlotColor
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SetSlotCpu(ByVal query As String, ByVal c As W3Slot.ComputerLevel) As IFuture(Of Outcome) Implements IW3Game.f_SetSlotCpu
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SetSlotHandicap(ByVal query As String, ByVal new_handicap As Byte) As IFuture(Of Outcome) Implements IW3Game.f_SetSlotHandicap
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SetSlotLocked(ByVal query As String, ByVal new_lock_state As W3Slot.Lock) As IFuture(Of Outcome) Implements IW3Game.f_SetSlotLocked
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SetSlotRace(ByVal query As String, ByVal new_race As W3Slot.RaceFlags) As IFuture(Of Outcome) Implements IW3Game.f_SetSlotRace
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SetSlotTeam(ByVal query As String, ByVal new_team As Byte) As IFuture(Of Outcome) Implements IW3Game.f_SetSlotTeam
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_StartCountdown() As IFuture(Of Outcome) Implements IW3Game.f_StartCountdown
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_State() As IFuture(Of W3GameStates) Implements IW3Game.f_State
            Contract.Ensures(Contract.Result(Of IFuture(Of W3GameStates))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_SwapSlotContents(ByVal query1 As String, ByVal query2 As String) As IFuture(Of Outcome) Implements IW3Game.f_SwapSlotContents
            Contract.Requires(query1 IsNot Nothing)
            Contract.Requires(query2 IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_ThrowUpdated() As IFuture Implements IW3Game.f_ThrowUpdated
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_TryAddPlayer(ByVal new_player As W3ConnectingPlayer) As IFuture(Of Outcome) Implements IW3Game.f_TryAddPlayer
            Contract.Requires(new_player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_TryElevatePlayer(ByVal name As String, Optional ByVal password As String = Nothing) As IFuture(Of Outcome) Implements IW3Game.f_TryElevatePlayer
            Contract.Requires(name IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_TrySetTeamSizes(ByVal sizes As System.Collections.Generic.IList(Of Integer)) As IFuture(Of Outcome) Implements IW3Game.f_TrySetTeamSizes
            Contract.Requires(sizes IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function f_UpdatedGameState() As IFuture Implements IW3Game.f_UpdatedGameState
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public ReadOnly Property GameTime As Integer Implements IW3Game.GameTime
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property logger As Logger Implements IW3Game.logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property map As W3Map Implements IW3Game.map
            Get
                Contract.Ensures(Contract.Result(Of W3Map)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property name As String Implements IW3Game.name
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public ReadOnly Property server As IW3Server Implements IW3Game.server
            Get
                Contract.Ensures(Contract.Result(Of IW3Server)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property

        Public Property SettingGameRate As Double Implements IW3Game.SettingGameRate
            Get
                Contract.Ensures(Contract.Result(Of Double)() > 0)
                Contract.Ensures(Not Double.IsInfinity(Contract.Result(Of Double)()))
                Contract.Ensures(Not Double.IsNaN(Contract.Result(Of Double)()))
                Throw New NotSupportedException
            End Get
            Set(ByVal value As Double)
                Contract.Requires(value > 0)
                Contract.Requires(Not Double.IsInfinity(value))
                Contract.Requires(Not Double.IsNaN(value))
                Throw New NotSupportedException
            End Set
        End Property

        Public Property SettingLagLimit As Double Implements IW3Game.SettingLagLimit
            Get
                Contract.Ensures(Contract.Result(Of Double)() >= 0)
                Contract.Ensures(Not Double.IsInfinity(Contract.Result(Of Double)()))
                Contract.Ensures(Not Double.IsNaN(Contract.Result(Of Double)()))
                Throw New NotSupportedException
            End Get
            Set(ByVal value As Double)
                Contract.Requires(value >= 0)
                Contract.Requires(Not Double.IsInfinity(value))
                Contract.Requires(Not Double.IsNaN(value))
                Throw New NotSupportedException
            End Set
        End Property

        Public Property SettingTickPeriod As Double Implements IW3Game.SettingTickPeriod
            Get
                Contract.Ensures(Contract.Result(Of Double)() > 0)
                Contract.Ensures(Not Double.IsInfinity(Contract.Result(Of Double)()))
                Contract.Ensures(Not Double.IsNaN(Contract.Result(Of Double)()))
                Throw New NotSupportedException
            End Get
            Set(ByVal value As Double)
                Contract.Requires(value > 0)
                Contract.Requires(Not Double.IsInfinity(value))
                Contract.Requires(Not Double.IsNaN(value))
                Throw New NotSupportedException
            End Set
        End Property
    End Class
End Namespace
