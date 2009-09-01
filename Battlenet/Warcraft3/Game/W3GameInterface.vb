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
        ReadOnly Property server() As W3Server
        ReadOnly Property name() As String
        ReadOnly Property logger() As Logger

        Function QueueGetAdminPlayer() As IFuture(Of IW3Player)
        Function QueueGetFakeHostPlayer() As IFuture(Of IW3Player)
        Function QueueGetState() As IFuture(Of W3GameStates)
        Function QueueBroadcastMessage(ByVal message As String) As IFuture
        Function QueueSendMessageTo(ByVal message As String, ByVal player As IW3Player) As IFuture
        Function QueueFindPlayer(ByVal username As String) As IFuture(Of IW3Player)
        Function QueueClose() As IFuture(Of Outcome)
        Function QueueTryElevatePlayer(ByVal name As String, Optional ByVal password As String = Nothing) As IFuture(Of Outcome)
        Function QueueGetPlayers() As IFuture(Of List(Of IW3Player))
        Function QueueBootSlot(ByVal query As String) As IFuture(Of Outcome)
        Function QueueThrowUpdated() As IFuture
        Function QueueRemovePlayer(ByVal p As IW3Player, ByVal expected As Boolean, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) As IFuture(Of Outcome)
        Function QueueReceiveNonGameAction(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture

        Event ChangedState(ByVal sender As IW3Game, ByVal oldState As W3GameStates, ByVal newState As W3GameStates)
        Event Updated(ByVal sender As IW3Game, ByVal slots As List(Of W3Slot))
        Event PlayerTalked(ByVal sender As IW3Game, ByVal player As IW3Player, ByVal text As String)
        Event PlayerLeft(ByVal sender As IW3Game, ByVal gameState As W3GameStates, ByVal player As IW3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String)

        Function QueueCommandProcessLocalText(ByVal text As String, ByVal logger As Logger) As IFuture
        Function QueueProcessCommand(ByVal player As IW3Player, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)

#Region "Lobby"
        ReadOnly Property DownloadScheduler() As TransferScheduler(Of Byte)

        Function QueuePlayerVoteToStart(ByVal name As String, ByVal val As Boolean) As IFuture(Of Outcome)
        Function QueueStartCountdown() As IFuture(Of Outcome)
        Function QueueTryAddPlayer(ByVal new_player As W3ConnectingPlayer) As IFuture(Of Outcome)
        Function QueueOpenSlot(ByVal query As String) As IFuture(Of Outcome)
        Function QueueCloseSlot(ByVal query As String) As IFuture(Of Outcome)
        Function QueueReserveSlot(ByVal query As String, ByVal username As String) As IFuture(Of Outcome)
        Function QueueSwapSlotContents(ByVal query1 As String, ByVal query2 As String) As IFuture(Of Outcome)
        Function QueueSetSlotCpu(ByVal query As String, ByVal c As W3Slot.ComputerLevel) As IFuture(Of Outcome)
        Function QueueSetSlotLocked(ByVal query As String, ByVal new_lock_state As W3Slot.Lock) As IFuture(Of Outcome)
        Function QueueSetAllSlotsLocked(ByVal new_lock_state As W3Slot.Lock) As IFuture(Of Outcome)
        Function QueueSetSlotHandicap(ByVal query As String, ByVal new_handicap As Byte) As IFuture(Of Outcome)
        Function QueueSetSlotTeam(ByVal query As String, ByVal new_team As Byte) As IFuture(Of Outcome)
        Function QueueSetSlotRace(ByVal query As String, ByVal new_race As W3Slot.RaceFlags) As IFuture(Of Outcome)
        Function QueueSetSlotColor(ByVal query As String, ByVal new_color As W3Slot.PlayerColor) As IFuture(Of Outcome)
        Function QueueUpdatedGameState() As IFuture
        Function QueueTrySetTeamSizes(ByVal sizes As IList(Of Integer)) As IFuture(Of Outcome)

        Event PlayerEntered(ByVal sender As IW3Game, ByVal player As IW3Player)
#End Region

#Region "Load Screen"
        Function QueueReceiveReady(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture
#End Region

#Region "Game Play"
        ReadOnly Property GameTime() As Integer

        Function QueueDropLagger() As IFuture
        Function QueueSendGameData(ByVal sender As IW3Player, ByVal data() As Byte) As IFuture

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

        Public Function QueueAdminPlayer() As IFuture(Of IW3Player) Implements IW3Game.QueueGetAdminPlayer
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Player))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueBootSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3Game.QueueBootSlot
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueBroadcastMessage(ByVal message As String) As IFuture Implements IW3Game.QueueBroadcastMessage
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueClose() As IFuture(Of Outcome) Implements IW3Game.QueueClose
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueCloseSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3Game.QueueCloseSlot
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueCommandProcessLocalText(ByVal text As String, ByVal logger As Logger) As IFuture Implements IW3Game.QueueCommandProcessLocalText
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueProcessCommand(ByVal player As IW3Player, ByVal arguments As IList(Of String)) As IFuture(Of Outcome) Implements IW3Game.QueueProcessCommand
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(arguments IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueDropLagger() As IFuture Implements IW3Game.QueueDropLagger
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueEnumPlayers() As IFuture(Of List(Of IW3Player)) Implements IW3Game.QueueGetPlayers
            Contract.Ensures(Contract.Result(Of IFuture(Of List(Of IW3Player)))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueFakeHostPlayer() As IFuture(Of IW3Player) Implements IW3Game.QueueGetFakeHostPlayer
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Player))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueFindPlayer(ByVal username As String) As IFuture(Of IW3Player) Implements IW3Game.QueueFindPlayer
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Player))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueOpenSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3Game.QueueOpenSlot
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueuePlayerVoteToStart(ByVal name As String,
                                            ByVal val As Boolean) As IFuture(Of Outcome) Implements IW3Game.QueuePlayerVoteToStart
            Contract.Requires(name IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueQueueGameData(ByVal sender As IW3Player,
                                        ByVal data() As Byte) As IFuture Implements IW3Game.QueueSendGameData
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueReceiveNonGameAction(ByVal player As IW3Player,
                                               ByVal vals As Dictionary(Of String, Object)) As IFuture Implements IW3Game.QueueReceiveNonGameAction
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueReceiveReady(ByVal player As IW3Player,
                                       ByVal vals As Dictionary(Of String, Object)) As IFuture Implements IW3Game.QueueReceiveReady
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueRemovePlayer(ByVal p As IW3Player, ByVal expected As Boolean, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) As IFuture(Of Outcome) Implements IW3Game.QueueRemovePlayer
            Contract.Requires(p IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueReserveSlot(ByVal query As String, ByVal username As String) As IFuture(Of Outcome) Implements IW3Game.QueueReserveSlot
            Contract.Requires(query IsNot Nothing)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueSendMessageTo(ByVal message As String, ByVal player As IW3Player) As IFuture Implements IW3Game.QueueSendMessageTo
            Contract.Requires(message IsNot Nothing)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueSetAllSlotsLocked(ByVal newLockState As W3Slot.Lock) As IFuture(Of Outcome) Implements IW3Game.QueueSetAllSlotsLocked
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueSetSlotColor(ByVal query As String, ByVal new_color As W3Slot.PlayerColor) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotColor
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueSetSlotCpu(ByVal query As String, ByVal c As W3Slot.ComputerLevel) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotCpu
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueSetSlotHandicap(ByVal query As String, ByVal new_handicap As Byte) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotHandicap
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueSetSlotLocked(ByVal query As String, ByVal new_lock_state As W3Slot.Lock) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotLocked
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueSetSlotRace(ByVal query As String, ByVal new_race As W3Slot.RaceFlags) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotRace
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueSetSlotTeam(ByVal query As String, ByVal new_team As Byte) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotTeam
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueStartCountdown() As IFuture(Of Outcome) Implements IW3Game.QueueStartCountdown
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueState() As IFuture(Of W3GameStates) Implements IW3Game.QueueGetState
            Contract.Ensures(Contract.Result(Of IFuture(Of W3GameStates))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueSwapSlotContents(ByVal query1 As String, ByVal query2 As String) As IFuture(Of Outcome) Implements IW3Game.QueueSwapSlotContents
            Contract.Requires(query1 IsNot Nothing)
            Contract.Requires(query2 IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueThrowUpdated() As IFuture Implements IW3Game.QueueThrowUpdated
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueTryAddPlayer(ByVal new_player As W3ConnectingPlayer) As IFuture(Of Outcome) Implements IW3Game.QueueTryAddPlayer
            Contract.Requires(new_player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueTryElevatePlayer(ByVal name As String, Optional ByVal password As String = Nothing) As IFuture(Of Outcome) Implements IW3Game.QueueTryElevatePlayer
            Contract.Requires(name IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueTrySetTeamSizes(ByVal sizes As System.Collections.Generic.IList(Of Integer)) As IFuture(Of Outcome) Implements IW3Game.QueueTrySetTeamSizes
            Contract.Requires(sizes IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function

        Public Function QueueUpdatedGameState() As IFuture Implements IW3Game.QueueUpdatedGameState
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

        Public ReadOnly Property server As W3Server Implements IW3Game.server
            Get
                Contract.Ensures(Contract.Result(Of W3Server)() IsNot Nothing)
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
