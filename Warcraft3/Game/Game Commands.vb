Imports Tinker.Commands

Namespace WC3
    Public NotInheritable Class InstancePlayCommands
        Inherits InstanceCommands

        Public Sub New()
            AddCommand(Disconnect)
        End Sub

        Private Shared ReadOnly Disconnect As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Disconnect",
            template:="",
            Description:="Causes the bot to disconnect from the game. The game may continue if one of the players can host.",
            func:=Function(target, user, argument)
                      target.Dispose()
                      Return target.FutureDisposed.EvalOnSuccess(Function() "Disconnected")
                  End Function)
    End Class

    Public NotInheritable Class InstanceSetupCommands
        Inherits InstanceCommands

        Public Sub New()
            AddCommand(Cancel)
            AddCommand(Close)
            AddCommand(Lock)
            AddCommand(Open)
            AddCommand(Reserve)
            AddCommand(Color)
            AddCommand(CPU)
            AddCommand(Handicap)
            AddCommand(SetTeam)
            AddCommand(SetupTeams)
            AddCommand(Start)
            AddCommand(Swap)
            AddCommand(Unlock)
        End Sub

        Private Shared ReadOnly Open As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Open",
            template:="slot",
            Description:="Opens a slot.",
            func:=Function(target, user, argument)
                      Return target.QueueOpenSlot(argument.RawValue(0)).EvalOnSuccess(Function() "Opened")
                  End Function)

        Private Shared ReadOnly Close As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Close",
            template:="slot",
            Description:="Closes a slot.",
            func:=Function(target, user, argument)
                      Return target.QueueCloseSlot(argument.RawValue(0)).EvalOnSuccess(Function() "Closed")
                  End Function)

        Private Shared ReadOnly SetTeam As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="SetTeam",
            template:="slot team",
            Description:="Sets a slot's team. Only works in melee games.",
            func:=Function(target, user, argument)
                      If Not target.Map.isMelee Then Throw New InvalidOperationException("A slot's team is fixed in a custom game.")
                      Dim arg_slot = argument.RawValue(0)
                      Dim arg_team = argument.RawValue(1)
                      Dim val_team As Byte
                      If Not Byte.TryParse(arg_team, val_team) Then
                          Throw New ArgumentException("Invalid team: '{0}'.".Frmt(arg_team))
                      End If
                      Return target.QueueSetSlotTeam(arg_slot, val_team).EvalOnSuccess(Function() "Set Team")
                  End Function)

        Private Shared ReadOnly SetupTeams As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="SetupTeams",
            template:="teams",
            Description:="Sets up the number of slots on each team (eg. 'SetupTeams 2v2' will leave two open slots on each team).",
            func:=Function(target, user, argument)
                      Return target.QueueTrySetTeamSizes(TeamVersusStringToTeamSizes(argument.RawValue(0))).EvalOnSuccess(Function() "Set Teams")
                  End Function)

        Private Shared ReadOnly Handicap As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Handicap",
            template:="slot value",
            Description:="Sets the handicap of a slot.",
            func:=Function(target, user, argument)
                      Dim argSlot = argument.RawValue(0)
                      Dim argHandicap = argument.RawValue(1)
                      Dim newHandicap As Byte
                      If Not Byte.TryParse(argHandicap, newHandicap) Then newHandicap = 0
                      Select Case newHandicap
                          Case 50, 60, 70, 80, 90, 100
                              Return target.QueueSetSlotHandicap(argSlot, newHandicap).EvalOnSuccess(Function() "Set Handicap")
                          Case Else
                              Throw New InvalidOperationException("Invalid handicap: '{0}'.".Frmt(argHandicap))
                      End Select
                  End Function)

        Private Shared ReadOnly Color As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Color",
            template:="slot value",
            Description:="Sets the color of a slot. Only works in melee games.",
            func:=Function(target, user, argument)
                      If Not target.Map.isMelee Then Throw New InvalidOperationException("A slot's color is fixed in a custom game.")
                      Dim arg_slot = argument.RawValue(0)
                      Dim arg_color = argument.RawValue(1)
                      Dim ret_color As WC3.Slot.PlayerColor
                      If EnumTryParse(Of WC3.Slot.PlayerColor)(arg_color, True, ret_color) Then
                          Return target.QueueSetSlotColor(arg_slot, ret_color).EvalOnSuccess(Function() "Set Color")
                      End If
                      Throw New InvalidOperationException("Unrecognized color: '{0}'.".Frmt(arg_color))
                  End Function)

        Private Shared ReadOnly Swap As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Swap",
            template:="slot1 slot2",
            Description:="Swaps the contents of two slots.",
            func:=Function(target, user, argument)
                      Return target.QueueSwapSlotContents(argument.RawValue(0), argument.RawValue(1)).EvalOnSuccess(Function() "Swapped Slots")
                  End Function)

        Private Shared ReadOnly CPU As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="CPU",
            template:="slot ?difficulty",
            Description:="Places a computer in a slot, unless it contains a player.",
            func:=Function(target, user, argument)
                      Dim arg_slot = argument.RawValue(0)
                      Dim arg_difficulty = If(argument.rawvalueCount >= 2, argument.RawValue(1), WC3.Slot.ComputerLevel.Normal.ToString)
                      Dim ret_difficulty As WC3.Slot.ComputerLevel
                      If arg_difficulty.EnumTryParse(ignoreCase:=True, result:=ret_difficulty) Then
                          Return target.QueueSetSlotCpu(arg_slot, ret_difficulty).EvalOnSuccess(Function() "Set Computer Wc3.Slot")
                      End If
                      Throw New InvalidOperationException("Unrecognized difficulty: '{0}'.".Frmt(arg_difficulty))
                  End Function)

        Private Shared ReadOnly Lock As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Lock",
            template:="?slot -full",
            Description:="Prevents players from leaving a slot or from changing slot properties (if -full). Omit the slot argument to affect all slots.",
            func:=Function(target, user, argument)
                      Dim lockType = If(argument.HasOptionalSwitch("full"), WC3.Slot.Lock.Frozen, WC3.Slot.Lock.Sticky)
                      If argument.RawValueCount = 0 Then
                          Return target.QueueSetAllSlotsLocked(lockType).EvalOnSuccess(Function() "Locked slots")
                      Else
                          Return target.QueueSetSlotLocked(argument.TryGetRawValue(0), lockType).EvalOnSuccess(Function() "Locked slot")
                      End If
                  End Function)

        Private Shared ReadOnly Unlock As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Unlock",
            template:="?slot",
            Description:="Allows players to leave a slot and change its properties. Omit the slot argument to affect all slots.",
            func:=Function(target, user, argument)
                      Dim lockType = WC3.Slot.Lock.Unlocked
                      If argument.RawValueCount = 0 Then
                          Return target.QueueSetAllSlotsLocked(lockType).EvalOnSuccess(Function() "Unlocked slots")
                      Else
                          Return target.QueueSetSlotLocked(argument.TryGetRawValue(0), lockType).EvalOnSuccess(Function() "Unlocked slot")
                      End If
                  End Function)

        Private Shared ReadOnly Reserve As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Reserve",
            template:="name -slot=any",
            Description:="Reserves a slot for a player.",
            func:=Function(target, user, argument)
                      Dim name = argument.RawValue(0)
                      Dim slotQueryString = argument.TryGetOptionalNamedValue("slot")
                      Dim slotQuery = If(slotQueryString Is Nothing, Nothing, New InvariantString?(slotQueryString))

                      Return target.QueueReserveSlot(name, slotQuery).
                                        EvalOnSuccess(Function() "Reserved {0} for {1}.".Frmt(If(slotQueryString, "slot"), name))
                  End Function)

        Private Shared ReadOnly Start As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Start",
            template:="",
            Description:="Starts the launch countdown.",
            func:=Function(target, user, argument)
                      Return target.QueueStartCountdown().EvalOnSuccess(Function() "Started Countdown")
                  End Function)

        Private Shared ReadOnly Cancel As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Cancel",
            template:="",
            Description:="Closes this game instance.",
            func:=Function(target, user, argument)
                      target.Dispose()
                      Return target.futuredisposed.EvalOnSuccess(Function() "Cancelled")
                  End Function)
    End Class

    Public NotInheritable Class InstanceAdminCommands
        Inherits CommandSet(Of WC3.Game)

        Public Sub New(ByVal bot As Bot.MainBot)
            Contract.Requires(bot IsNot Nothing)
            AddCommand(New CommandBot(bot))
        End Sub

        Public NotInheritable Class CommandBot
            Inherits Command(Of WC3.Game)
            Private ReadOnly bot As Bot.MainBot
            Public Sub New(ByVal bot As Bot.MainBot)
                MyBase.New(Name:="Bot",
                           Format:="subcommand...",
                           Description:="Forwards commands to the bot.")
                Contract.Requires(bot IsNot Nothing)
                Me.bot = bot
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As WC3.Game, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
                Return Tinker.Bot.MainBotManager.BotCommands.Invoke(bot, user, argument)
            End Function
        End Class
    End Class

    Public Class InstanceCommands
        Inherits InstanceBaseCommands

        Public Sub New()
            AddCommand(Boot)
            AddCommand([Get])
            AddCommand([Set])
        End Sub

        Private Shared ReadOnly Boot As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Boot",
            template:="Name/Color",
            Description:="Kicks a player from the game.",
            func:=Function(target, user, argument)
                      Return target.QueueBoot(argument.rawvalue(0)).EvalOnSuccess(Function() "Booted")
                  End Function)

        Private Shared ReadOnly [Get] As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Get",
            template:="setting",
            Description:="Returns settings for this game {tickperiod, laglimit, gamerate}.",
            func:=Function(target, user, argument)
                      Dim val As Object
                      Dim argSetting As InvariantString = argument.RawValue(0)
                      Select Case argSetting
                          Case "TickPeriod" : val = target.SettingTickPeriod
                          Case "LagLimit" : val = target.SettingLagLimit
                          Case "GameRate" : val = target.SettingSpeedFactor
                          Case Else : Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argument.RawValue(0)))
                      End Select
                      Return "{0} = '{1}'".Frmt(argument.RawValue(0), val).Futurized
                  End Function)

        Private Shared ReadOnly [Set] As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Set",
            template:="setting value",
            Description:="Sets settings for this game {tickperiod, laglimit, gamerate}.",
            func:=Function(target, user, argument)
                      Dim val_us As UShort
                      Dim vald As Double
                      Dim is_short = UShort.TryParse(argument.RawValue(1), val_us)
                      Dim is_double = Double.TryParse(argument.RawValue(1), vald)
                      Dim argSetting As InvariantString = argument.RawValue(0)
                      Select Case argSetting
                          Case "TickPeriod"
                              If Not is_short Or val_us < 50 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                              target.SettingTickPeriod = val_us
                          Case "LagLimit"
                              If Not is_short Or val_us < 1 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                              target.SettingLagLimit = val_us
                          Case "GameRate"
                              If Not is_double Or vald < 0.01 Or vald > 10 Then Throw New ArgumentException("Invalid value")
                              target.SettingSpeedFactor = vald
                          Case Else
                              Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argument.RawValue(0)))
                      End Select
                      Return "{0} set to {1}".Frmt(argument.RawValue(0), argument.RawValue(1)).Futurized
                  End Function)
    End Class

    Public Class InstanceBaseCommands
        Inherits CommandSet(Of WC3.Game)

        Public Sub New()
            AddCommand(Ping)
        End Sub

        Private Shared ReadOnly Ping As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Ping",
            template:="",
            Description:="Returns estimated network round trip times for each player.",
            func:=Function(target, user, argument)
                      Dim futurePlayers = target.QueueGetPlayers()
                      Return futurePlayers.Select(
                          Function(players)
                              Dim futureLatencies = (From player In players Select player.GetLatencyDescription).ToList.Defuturized
                              Return futureLatencies.Select(
                                  Function(latencies)
                                      Return "Estimated RTT: {0}".Frmt((From i In Enumerable.Range(0, players.Count)
                                                                        Select "{0}={1}".Frmt(players(i).Name, latencies(i))
                                                                        ).StringJoin(" "))
                                  End Function
                              )
                                  End Function
                      ).defuturized
                          End Function)
    End Class

    Public NotInheritable Class InstanceGuestSetupCommands
        Inherits InstanceBaseCommands

        Public Sub New()
            AddCommand(Elevate)
            AddCommand(VoteStart)
        End Sub

        Private Shared ReadOnly VoteStart As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="VoteStart",
            template:="-cancel",
            Description:="Places or cancels a vote to prematurely start an autostarted game. Requires at least 2 players and at least a 2/3 majority.",
            func:=Function(target, user, argument)
                      If user Is Nothing Then Throw New InvalidOperationException("User not specified.")
                      Return target.QueueSetPlayerVoteToStart(user.name,
                                                              wantsToStart:=Not argument.HasOptionalSwitch("cancel")
                                                              ).EvalOnSuccess(Function() "Voted to start")
                  End Function)

        Private Shared ReadOnly Elevate As New DelegatedTemplatedCommand(Of WC3.Game)(
            Name:="Elevate",
            template:="password",
            Description:="Gives access to admin or host commands.",
            func:=Function(target, user, argument)
                      If user Is Nothing Then Throw New InvalidOperationException("User not specified.")
                      Return target.QueueElevatePlayer(user.Name, argument.RawValue(0)).EvalOnSuccess(Function() "Elevated")
                  End Function)
    End Class
    Public NotInheritable Class InstanceGuestPlayCommands
        Inherits InstanceBaseCommands
    End Class
End Namespace
