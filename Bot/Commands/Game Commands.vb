Imports HostBot.Commands
Imports HostBot.Warcraft3

Namespace Commands.Specializations
    Public Class InstancePlayCommands
        Inherits InstanceCommands

        Public Sub New()
            AddCommand(New com_Disconnect)
        End Sub

        '''<summary>A command which disconnects the bot from the instance.</summary>
        Public Class com_Disconnect
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Disconnect,
                           0, ArgumentLimits.min,
                           My.Resources.Command_Instance_Disconnect_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                target.QueueClose()
                Return success("Disconnected").Futurize
            End Function
        End Class
    End Class

    Public Class InstanceSetupCommands
        Inherits InstanceCommands

        Public Sub New()
            AddCommand(New com_Cancel)
            AddCommand(New com_Close)
            AddCommand(New com_Freeze)
            AddCommand(New com_Lock)
            AddCommand(New com_Open)
            AddCommand(New com_Reserve)
            AddCommand(New com_SetColor)
            AddCommand(New com_SetComputer)
            AddCommand(New com_SetHandicap)
            AddCommand(New com_SetTeam)
            AddCommand(New com_SetTeams)
            AddCommand(New com_Start)
            AddCommand(New com_Swap)
            AddCommand(New com_Unlock)
        End Sub

        '''<summary>A command which opens a slot.</summary>
        Public Class com_Open
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Open,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Open_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.QueueOpenSlot(arguments(0))
            End Function
        End Class

        '''<summary>A command which closes a slot.</summary>
        Public Class com_Close
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Close,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Close_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.QueueCloseSlot(arguments(0))
            End Function
        End Class

        '''<summary>A command which sets a slot's team.</summary>
        Public Class com_SetTeam
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetTeam,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Instance_SetTeam_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim arg_slot = arguments(0)
                Dim arg_team = arguments(1)
                Dim val_team As Byte
                If Not Byte.TryParse(arg_team, val_team) Then
                    Return failure("Invalid team: '{0}'.".frmt(arg_team)).Futurize
                End If
                Return target.QueueSetSlotTeam(arg_slot, val_team)
            End Function
        End Class

        '''<summary>A command which preps slots for a particular number of players.</summary>
        Public Class com_SetTeams
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetTeams,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_SetTeams_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim out = W3Game.XvX(arguments(0))
                If Not out.succeeded Then Return out.Outcome.Futurize
                Return target.QueueTrySetTeamSizes(out.Value)
            End Function
        End Class

        '''<summary>A command which sets a slot's handicap.</summary>
        Public Class com_SetHandicap
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetHandicap,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Instance_SetHandicap_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim arg_slot = arguments(0)
                Dim arg_handicap = arguments(1)
                Dim val_handicap As Byte = 0
                Byte.TryParse(arg_handicap, val_handicap)
                Select Case val_handicap
                    Case 50, 60, 70, 80, 90, 100
                        Return target.QueueSetSlotHandicap(arg_slot, val_handicap)
                    Case Else
                        Return failure("Invalid handicap: '{0}'.".frmt(arg_handicap)).Futurize
                End Select
            End Function
        End Class

        '''<summary>A command which sets a slot's color.</summary>
        Public Class com_SetColor
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetColor,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Instance_SetColor_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim arg_slot = arguments(0)
                Dim arg_color = arguments(1)
                Dim ret_color As W3Slot.PlayerColor
                If EnumTryParse(Of W3Slot.PlayerColor)(arg_color, True, ret_color) Then
                    Return target.QueueSetSlotColor(arg_slot, ret_color)
                End If
                Return failure("Unrecognized color: '{0}'.".frmt(arg_color)).Futurize
            End Function
        End Class

        '''<summary>A command which swaps the contents of two slots.</summary>
        Public Class com_Swap
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Swap,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Swap_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.QueueSwapSlotContents(arguments(0), arguments(1))
            End Function
        End Class

        '''<summary>A command which places a computer in a slot.</summary>
        Public Class com_SetComputer
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetComputer,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Instance_SetComputer_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim arg_slot = arguments(0)
                Dim arg_difficulty = arguments(1)
                Dim ret_difficulty As W3Slot.ComputerLevel
                If EnumTryParse(Of W3Slot.ComputerLevel)(arg_difficulty, True, ret_difficulty) Then
                    Return target.QueueSetSlotCpu(arg_slot, ret_difficulty)
                End If
                Return failure("Unrecognized difficulty: '{0}'.".frmt(arg_difficulty)).Futurize
            End Function
        End Class

        '''<summary>A command which stops players from leaving a slot.</summary>
        Public Class com_Lock
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Lock,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Instance_Lock_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 Then
                    Return target.QueueSetAllSlotsLocked(W3Slot.Lock.sticky)
                Else
                    Return target.QueueSetSlotLocked(arguments(0), W3Slot.Lock.sticky)
                End If
            End Function
        End Class

        '''<summary>A command which enables players to leave and modify a slot.</summary>
        Public Class com_Unlock
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Unlock,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Instance_Unlock_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 Then
                    Return target.QueueSetAllSlotsLocked(W3Slot.Lock.unlocked)
                Else
                    Return target.QueueSetSlotLocked(arguments(0), W3Slot.Lock.unlocked)
                End If
            End Function
        End Class

        '''<summary>A command which stops players from modifying or leaving a slot.</summary>
        Public Class com_Freeze
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Freeze,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Instance_Freeze_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 Then
                    Return target.QueueSetAllSlotsLocked(W3Slot.Lock.frozen)
                Else
                    Return target.QueueSetSlotLocked(arguments(0), W3Slot.Lock.frozen)
                End If
            End Function
        End Class

        '''<summary>A command which reserves a slot for a player.</summary>
        Public Class com_Reserve
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Reserve,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Reserve_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.QueueReserveSlot(arguments(0), arguments(1))
            End Function
        End Class

        '''<summary>A command which starts the launch countdown.</summary>
        Public Class com_Start
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Start,
                           0, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Start_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.QueueStartCountdown()
            End Function
        End Class

        '''<summary>A command which kills the instance.</summary>
        Public Class com_Cancel
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Cancel,
                           0, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Cancel_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.server.QueueKill()
            End Function
        End Class
    End Class

    Public Class InstanceAdminCommands
        Inherits CommandSet(Of IW3Game)

        Public Sub New()
            AddCommand(New com_Bot)
        End Sub

        Public Class com_Bot
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Bot,
                           0, ArgumentLimits.free,
                           My.Resources.Command_Instance_Bot_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.server.parent.BotCommands.ProcessCommand(target.server.parent, user, arguments)
            End Function
        End Class
    End Class

    Public Class InstanceCommands
        Inherits InstanceBaseCommands

        Public Sub New()
            AddCommand(New com_Boot)
            AddCommand(New com_GetSetting)
            AddCommand(New com_SetSetting)
        End Sub

        '''<summary>A command which boots players from a slot.</summary>
        Public Class com_Boot
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Boot,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Boot_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.QueueBootSlot(arguments(0))
            End Function
        End Class

        Public Class com_GetSetting
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New("GetSetting",
                           1, ArgumentLimits.exact,
                           "[GetSetting setting] Displays a game setting. Available settings are tickperiod laglimit gamerate.")
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim val As Object
                Select Case arguments(0).ToLower()
                    Case "tickperiod"
                        val = target.SettingTickPeriod
                    Case "laglimit"
                        val = target.SettingLagLimit
                    Case "gamerate"
                        val = target.SettingGameRate
                    Case Else
                        Return failure("Unrecognized setting '{0}'.".frmt(arguments(0))).Futurize
                End Select
                Return success("{0} = '{1}'".frmt(arguments(0), val)).Futurize
            End Function
        End Class
        Public Class com_SetSetting
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New("SetSetting",
                           2, ArgumentLimits.exact,
                           "[SetSetting setting] Changes a game setting. Available settings are tickperiod laglimit gamerate.")
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim val_us As UShort
                Dim vald As Double
                Dim is_short = UShort.TryParse(arguments(1), val_us)
                Dim is_double = Double.TryParse(arguments(1), vald)
                Select Case arguments(0).ToLower()
                    Case "tickperiod"
                        If Not is_short Or val_us < 50 Or val_us > 20000 Then Return failure("Invalid value").Futurize
                        target.SettingTickPeriod = val_us
                    Case "laglimit"
                        If Not is_short Or val_us < 1 Or val_us > 20000 Then Return failure("Invalid value").Futurize
                        target.SettingLagLimit = val_us
                    Case "gamerate"
                        If Not is_double Or vald < 0.01 Or vald > 10 Then Return failure("Invalid value").Futurize
                        target.SettingGameRate = vald
                    Case Else
                        Return failure("Unrecognized setting '{0}'.".frmt(arguments(0))).Futurize
                End Select
                Return success("{0} set to {1}".frmt(arguments(0), arguments(1))).Futurize
            End Function
        End Class
    End Class

    Public Class InstanceBaseCommands
        Inherits UICommandSet(Of IW3Game)

        Public Sub New()
            AddCommand(New com_Ping)
            AddCommand(New com_Leave)
        End Sub

        '''<summary>A command which disconnects the bot from the instance.</summary>
        Public Class com_Ping
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Ping,
                           0, ArgumentLimits.min,
                           My.Resources.Command_Instance_Ping_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.QueueGetPlayers().EvalWhenValueReady(
                    Function(players)
                                                                       Dim msg = "Estimated RTT:"
                                                                       For Each player In players
                                                                           If player.IsFake Then  Continue For
                                                                           msg += " {0}={1}ms".frmt(player.name, player.latency.ToString("0"))
                                                                       Next player
                                                                       Return success(msg)
                                                                   End Function
                )
            End Function
        End Class

        Public Class com_Leave
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New("Leave",
                           0, ArgumentLimits.exact,
                           "Disconnects you from the game (for when countdown is cancelled and you can't leave normally).")
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If user Is Nothing Then Return failure("You are not in the game.").Futurize
                Return target.QueueBootSlot(user.name)
            End Function
        End Class
    End Class

    Public Class InstanceGuestSetupCommands
        Inherits InstanceBaseCommands

        Public Sub New()
            AddCommand(New com_Elevate)
            AddCommand(New com_VoteStart)
        End Sub

        Public Class com_VoteStart
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_VoteStart,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Instance_VoteStart_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 1 AndAlso arguments(0).ToLower <> "cancel" Then Return failure("Incorrect argument.").Futurize
                If user Is Nothing Then Return failure("User not specified.").Futurize
                Return target.QueuePlayerVoteToStart(user.name, arguments.Count = 0)
            End Function
        End Class

        Public Class com_Elevate
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Elevate,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Elevate_Help)
            End Sub
            Public Overrides Function Process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If user Is Nothing Then Return failure("User not specified.").Futurize
                Return target.QueueTryElevatePlayer(user.name, arguments(0))
            End Function
        End Class
    End Class
    Public Class InstanceGuestLoadCommands
        Inherits InstanceBaseCommands
    End Class
    Public Class InstanceGuestPlayCommands
        Inherits InstanceBaseCommands
    End Class
End Namespace
