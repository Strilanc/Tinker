Namespace Warcraft3
    Public Enum W3GameStates
        AcceptingPlayers = 0
        PreCounting = 1
        CountingDown = 2
        Loading = 3
        Playing = 4
        Closed = 5
    End Enum

    Public Interface IW3Game
        ReadOnly Property map() As W3Map
        ReadOnly Property parent() As IW3Server
        ReadOnly Property name() As String
        ReadOnly Property logger() As Logger
        ReadOnly Property lobby() As IW3GameLobby
        ReadOnly Property load_screen() As IW3GameLoadScreen
        ReadOnly Property gameplay() As IW3GamePlay

        Function f_admin_player() As IFuture(Of IW3Player)
        Function f_fake_host_player() As IFuture(Of IW3Player)
        Function f_State() As IFuture(Of W3GameStates)
        Function f_BroadcastMessage(ByVal message As String) As IFuture
        Function f_SendMessageTo(ByVal message As String, ByVal player As IW3Player) As IFuture
        Function f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player)
        Function f_Close() As IFuture(Of Outcome)
        Function f_TryElevatePlayer(ByVal name As String, Optional ByVal password As String = Nothing) As IFuture(Of Outcome)
        Function f_EnumPlayers() As IFuture(Of List(Of IW3Player))
        Function f_BootSlot(ByVal query As String) As IFuture(Of Outcome)
        Function f_ThrowUpdated() As IFuture
        Function f_RemovePlayer(ByVal p As IW3Player, ByVal expected As Boolean, ByVal leave_type As W3PlayerLeaveTypes) As IFuture(Of Outcome)
        Function f_ReceivePacket_CLIENT_COMMAND(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture

        Event ChangedState(ByVal sender As IW3Game, ByVal old_state As W3GameStates, ByVal new_state As W3GameStates)
        Event Updated(ByVal sender As IW3Game, ByVal slots As List(Of W3Slot))
        Event PlayerTalked(ByVal sender As IW3Game, ByVal player As IW3Player, ByVal text As String)
        Event PlayerLeft(ByVal sender As IW3Game, ByVal game_state As W3GameStates, ByVal player As IW3Player, ByVal reason As W3PlayerLeaveTypes)

        Function f_CommandProcessLocalText(ByVal text As String, ByVal logger As Logger) As IFuture
        Function f_CommandProcessText(ByVal player As IW3Player, ByVal text As String) As IFuture(Of Outcome)
    End Interface

    Public Interface IW3GamePart
        ReadOnly Property game() As IW3Game
    End Interface

    Public Interface IW3GameLobby
        Inherits IW3GamePart

        ReadOnly Property download_scheduler() As TransferScheduler(Of Byte)

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

        Event PlayerEntered(ByVal sender As IW3GameLobby, ByVal player As IW3PlayerLobby)
    End Interface
    Public Interface IW3GameLoadScreen
        Inherits IW3GamePart
        Function f_ReceivePacket_READY(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture
    End Interface
    Public Interface IW3GamePlay
        Inherits IW3GamePart

        ReadOnly Property game_time() As Integer

        Function f_DropLagger() As IFuture
        Function f_QueueGameData(ByVal sender As IW3PlayerGameplay, ByVal data() As Byte) As IFuture

        Property setting_game_rate As Double
        Property setting_lag_limit As Double
        Property setting_tick_period As Double

        Event PlayerSentData(ByVal game As IW3GamePlay, ByVal player As IW3PlayerGameplay, ByVal data As Byte())
    End Interface
End Namespace
