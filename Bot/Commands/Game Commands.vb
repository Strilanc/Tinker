Imports HostBot.Commands
Imports HostBot.Warcraft3

Namespace Commands.Specializations
    Public NotInheritable Class InstancePlayCommands
        Inherits InstanceCommands

        Public Sub New()
            AddCommand(New CommandDisconnect)
        End Sub

        '''<summary>A command which disconnects the bot from the instance.</summary>
        Public NotInheritable Class CommandDisconnect
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Disconnect,
                           0, ArgumentLimitType.Min,
                           My.Resources.Command_Instance_Disconnect_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                target.QueueClose()
                Return "Disconnected".Futurized
            End Function
        End Class
    End Class

    Public NotInheritable Class InstanceSetupCommands
        Inherits InstanceCommands

        Public Sub New()
            AddCommand(New CommandCancel)
            AddCommand(New CommandClose)
            AddCommand(New CommandFreeze)
            AddCommand(New CommandLock)
            AddCommand(New CommandOpen)
            AddCommand(New CommandReserve)
            AddCommand(New CommandSetColor)
            AddCommand(New CommandSetComputer)
            AddCommand(New CommandSetHandicap)
            AddCommand(New CommandSetTeam)
            AddCommand(New CommandSetTeams)
            AddCommand(New CommandStart)
            AddCommand(New CommandSwap)
            AddCommand(New CommandUnlock)
        End Sub

        '''<summary>A command which opens a slot.</summary>
        Public NotInheritable Class CommandOpen
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Open,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_Open_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueOpenSlot(arguments(0)).EvalOnSuccess(Function() "Opened")
            End Function
        End Class

        '''<summary>A command which closes a slot.</summary>
        Public NotInheritable Class CommandClose
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Close,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_Close_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueCloseSlot(arguments(0)).EvalOnSuccess(Function() "Closed")
            End Function
        End Class

        '''<summary>A command which sets a slot's team.</summary>
        Public NotInheritable Class CommandSetTeam
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetTeam,
                           2, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_SetTeam_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim arg_slot = arguments(0)
                Dim arg_team = arguments(1)
                Dim val_team As Byte
                If Not Byte.TryParse(arg_team, val_team) Then
                    Throw New ArgumentException("Invalid team: '{0}'.".Frmt(arg_team))
                End If
                Return target.QueueSetSlotTeam(arg_slot, val_team).EvalOnSuccess(Function() "Set Team")
            End Function
        End Class

        '''<summary>A command which preps slots for a particular number of players.</summary>
        Public NotInheritable Class CommandSetTeams
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetTeams,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_SetTeams_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueTrySetTeamSizes(W3Game.TeamVersusStringToTeamSizes(arguments(0))).EvalOnSuccess(Function() "Set Teams")
            End Function
        End Class

        '''<summary>A command which sets a slot's handicap.</summary>
        Public NotInheritable Class CommandSetHandicap
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetHandicap,
                           2, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_SetHandicap_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim argSlot = arguments(0)
                Dim argHandicap = arguments(1)
                Dim newHandicap As Byte
                If Not Byte.TryParse(argHandicap, newHandicap) Then newHandicap = 0
                Select Case newHandicap
                    Case 50, 60, 70, 80, 90, 100
                        Return target.QueueSetSlotHandicap(argSlot, newHandicap).EvalOnSuccess(Function() "Set Handicap")
                    Case Else
                        Throw New InvalidOperationException("Invalid handicap: '{0}'.".Frmt(argHandicap))
                End Select
            End Function
        End Class

        '''<summary>A command which sets a slot's color.</summary>
        Public NotInheritable Class CommandSetColor
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetColor,
                           2, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_SetColor_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim arg_slot = arguments(0)
                Dim arg_color = arguments(1)
                Dim ret_color As W3Slot.PlayerColor
                If EnumTryParse(Of W3Slot.PlayerColor)(arg_color, True, ret_color) Then
                    Return target.QueueSetSlotColor(arg_slot, ret_color).EvalOnSuccess(Function() "Set Color")
                End If
                Throw New InvalidOperationException("Unrecognized color: '{0}'.".Frmt(arg_color))
            End Function
        End Class

        '''<summary>A command which swaps the contents of two slots.</summary>
        Public NotInheritable Class CommandSwap
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Swap,
                           2, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_Swap_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueSwapSlotContents(arguments(0), arguments(1)).EvalOnSuccess(Function() "Swapped Slots")
            End Function
        End Class

        '''<summary>A command which places a computer in a slot.</summary>
        Public NotInheritable Class CommandSetComputer
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetComputer,
                           1, ArgumentLimitType.Min,
                           My.Resources.Command_Instance_SetComputer_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim arg_slot = arguments(0)
                Dim arg_difficulty = If(arguments.Count >= 2, arguments(1), W3Slot.ComputerLevel.Normal.ToString)
                Dim ret_difficulty As W3Slot.ComputerLevel
                If EnumTryParse(Of W3Slot.ComputerLevel)(arg_difficulty, True, ret_difficulty) Then
                    Return target.QueueSetSlotCpu(arg_slot, ret_difficulty).EvalOnSuccess(Function() "Set Computer Slot")
                End If
                Throw New InvalidOperationException("Unrecognized difficulty: '{0}'.".Frmt(arg_difficulty))
            End Function
        End Class

        '''<summary>A command which stops players from leaving a slot.</summary>
        Public NotInheritable Class CommandLock
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Lock,
                           1, ArgumentLimitType.Max,
                           My.Resources.Command_Instance_Lock_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 Then
                    Return target.QueueSetAllSlotsLocked(W3Slot.Lock.Sticky).EvalOnSuccess(Function() "Locked slots")
                Else
                    Return target.QueueSetSlotLocked(arguments(0), W3Slot.Lock.Sticky).EvalOnSuccess(Function() "Locked slots")
                End If
            End Function
        End Class

        '''<summary>A command which enables players to leave and modify a slot.</summary>
        Public NotInheritable Class CommandUnlock
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Unlock,
                           1, ArgumentLimitType.Max,
                           My.Resources.Command_Instance_Unlock_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 Then
                    Return target.QueueSetAllSlotsLocked(W3Slot.Lock.Unlocked).EvalOnSuccess(Function() "Unlocked slots")
                Else
                    Return target.QueueSetSlotLocked(arguments(0), W3Slot.Lock.Unlocked).EvalOnSuccess(Function() "Unlocked slots")
                End If
            End Function
        End Class

        '''<summary>A command which stops players from modifying or leaving a slot.</summary>
        Public NotInheritable Class CommandFreeze
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Freeze,
                           1, ArgumentLimitType.Max,
                           My.Resources.Command_Instance_Freeze_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 Then
                    Return target.QueueSetAllSlotsLocked(W3Slot.Lock.Frozen).EvalOnSuccess(Function() "Froze slots")
                Else
                    Return target.QueueSetSlotLocked(arguments(0), W3Slot.Lock.Frozen).EvalOnSuccess(Function() "Froze slots")
                End If
            End Function
        End Class

        '''<summary>A command which reserves a slot for a player.</summary>
        Public NotInheritable Class CommandReserve
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Reserve,
                           2, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_Reserve_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueReserveSlot(arguments(0), arguments(1)).EvalOnSuccess(Function() "Reserved Slot")
            End Function
        End Class

        '''<summary>A command which starts the launch countdown.</summary>
        Public NotInheritable Class CommandStart
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Start,
                           0, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_Start_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueStartCountdown().EvalOnSuccess(Function() "Started Countdown")
            End Function
        End Class

        '''<summary>A command which kills the instance.</summary>
        Public NotInheritable Class CommandCancel
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Cancel,
                           0, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_Cancel_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueClose.EvalOnSuccess(Function() "Cancelled")
            End Function
        End Class
    End Class

    Public NotInheritable Class InstanceAdminCommands
        Inherits CommandSet(Of W3Game)

        Public Sub New(ByVal bot As MainBot)
            AddCommand(New CommandBot(bot))
        End Sub

        Public NotInheritable Class CommandBot
            Inherits BaseCommand(Of W3Game)
            Private ReadOnly bot As MainBot
            Public Sub New(ByVal bot As MainBot)
                MyBase.New(My.Resources.Command_Instance_Bot,
                           0, ArgumentLimitType.Free,
                           My.Resources.Command_Instance_Bot_Help)
                Me.bot = bot
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return bot.BotCommands.ProcessCommand(bot, user, arguments)
            End Function
        End Class
    End Class

    Public Class InstanceCommands
        Inherits InstanceBaseCommands

        Public Sub New()
            AddCommand(New CommandBoot)
            AddCommand(New CommandGetSetting)
            AddCommand(New CommandSetSetting)
        End Sub

        '''<summary>A command which boots players from a slot.</summary>
        Public NotInheritable Class CommandBoot
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Boot,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_Boot_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueBootSlot(arguments(0)).EvalOnSuccess(Function() "Booted")
            End Function
        End Class

        Public NotInheritable Class CommandGetSetting
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New("Get",
                           1, ArgumentLimitType.Exact,
                           "[Get setting] Displays a game setting. Available settings are tickperiod laglimit gamerate.")
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim val As Object
                Select Case arguments(0).ToUpperInvariant
                    Case "TICKPERIOD" : val = target.SettingTickPeriod
                    Case "LAGLIMIT" : val = target.SettingLagLimit
                    Case "GAMERATE" : val = target.SettingSpeedFactor
                    Case Else : Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(arguments(0)))
                End Select
                Return "{0} = '{1}'".Frmt(arguments(0), val).Futurized
            End Function
        End Class
        Public NotInheritable Class CommandSetSetting
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New("Set",
                           2, ArgumentLimitType.Exact,
                           "[Set setting] Changes a game setting. Available settings are tickperiod laglimit gamerate.")
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim val_us As UShort
                Dim vald As Double
                Dim is_short = UShort.TryParse(arguments(1), val_us)
                Dim is_double = Double.TryParse(arguments(1), vald)
                Select Case arguments(0).ToUpperInvariant
                    Case "TICKPERIOD"
                        If Not is_short Or val_us < 50 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                        target.SettingTickPeriod = val_us
                    Case "LAGLIMIT"
                        If Not is_short Or val_us < 1 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                        target.SettingLagLimit = val_us
                    Case "GAMERATE"
                        If Not is_double Or vald < 0.01 Or vald > 10 Then Throw New ArgumentException("Invalid value")
                        target.SettingSpeedFactor = vald
                    Case Else
                        Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(arguments(0)))
                End Select
                Return "{0} set to {1}".Frmt(arguments(0), arguments(1)).Futurized
            End Function
        End Class
    End Class

    Public Class InstanceBaseCommands
        Inherits CommandSet(Of W3Game)

        Public Sub New()
            AddCommand(New CommandPing)
            AddCommand(New CommandLeave)
        End Sub

        '''<summary>A command which disconnects the bot from the instance.</summary>
        Public NotInheritable Class CommandPing
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Ping,
                           0, ArgumentLimitType.Min,
                           My.Resources.Command_Instance_Ping_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim futurePlayers = target.QueueGetPlayers()

                Dim futureLatencies = futurePlayers.Select(
                    Function(players)
                        Return (From player In players
                                Select player.GetLatencyDescription).
                                ToList.Defuturized
                    End Function
                ).Defuturized

                Return futureLatencies.Select(
                    Function(latencies)
                        Dim players = futurePlayers.Value
                        Dim msg = "Estimated RTT:"
                        For i = 0 To players.Count - 1
                            If players(i).isFake Then  Continue For
                            msg += " {0}={1}".Frmt(players(i).name, latencies(i))
                        Next i
                        Return msg
                    End Function
                )
            End Function
        End Class

        Public NotInheritable Class CommandLeave
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New("Leave",
                           0, ArgumentLimitType.Exact,
                           "Disconnects you from the game (for when countdown is cancelled and you can't leave normally).")
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If user Is Nothing Then Throw New InvalidOperationException("You are not in the game.")
                Return target.QueueBootSlot(user.Name).EvalOnSuccess(Function() "Left.")
            End Function
        End Class
    End Class

    Public NotInheritable Class InstanceGuestSetupCommands
        Inherits InstanceBaseCommands

        Public Sub New()
            AddCommand(New CommandElevate)
            AddCommand(New CommandVoteStart)
        End Sub

        Public NotInheritable Class CommandVoteStart
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_VoteStart,
                           1, ArgumentLimitType.Max,
                           My.Resources.Command_Instance_VoteStart_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 1 AndAlso arguments(0).ToUpperInvariant <> "CANCEL" Then Throw New ArgumentException("Incorrect argument.")
                If user Is Nothing Then Throw New InvalidOperationException("User not specified.")
                Return target.QueueSetPlayerVoteToStart(user.name, arguments.Count = 0).EvalOnSuccess(Function() "Voted to start")
            End Function
        End Class

        Public NotInheritable Class CommandElevate
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Elevate,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Instance_Elevate_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If user Is Nothing Then Throw New InvalidOperationException("User not specified.")
                Return target.QueueTryElevatePlayer(user.name, arguments(0)).EvalOnSuccess(Function() "Elevated")
            End Function
        End Class
    End Class
    Public NotInheritable Class InstanceGuestLoadCommands
        Inherits InstanceBaseCommands
    End Class
    Public NotInheritable Class InstanceGuestPlayCommands
        Inherits InstanceBaseCommands
    End Class
End Namespace
