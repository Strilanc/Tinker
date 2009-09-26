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
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Disconnect,
                           0, ArgumentLimits.min,
                           My.Resources.Command_Instance_Disconnect_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                target.QueueClose()
                Return "Disconnected".Futurized
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
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Open,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Open_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueOpenSlot(arguments(0)).EvalOnSuccess(Function() "Opened")
            End Function
        End Class

        '''<summary>A command which closes a slot.</summary>
        Public Class com_Close
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Close,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Close_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueCloseSlot(arguments(0)).EvalOnSuccess(Function() "Closed")
            End Function
        End Class

        '''<summary>A command which sets a slot's team.</summary>
        Public Class com_SetTeam
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetTeam,
                           2, ArgumentLimits.exact,
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
        Public Class com_SetTeams
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetTeams,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_SetTeams_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueTrySetTeamSizes(W3Game.XvX(arguments(0))).EvalOnSuccess(Function() "Set Teams")
            End Function
        End Class

        '''<summary>A command which sets a slot's handicap.</summary>
        Public Class com_SetHandicap
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetHandicap,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Instance_SetHandicap_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim arg_slot = arguments(0)
                Dim arg_handicap = arguments(1)
                Dim val_handicap As Byte = 0
                Byte.TryParse(arg_handicap, val_handicap)
                Select Case val_handicap
                    Case 50, 60, 70, 80, 90, 100
                        Return target.QueueSetSlotHandicap(arg_slot, val_handicap).EvalOnSuccess(Function() "Set Handicap")
                    Case Else
                        Throw New InvalidOperationException("Invalid handicap: '{0}'.".Frmt(arg_handicap))
                End Select
            End Function
        End Class

        '''<summary>A command which sets a slot's color.</summary>
        Public Class com_SetColor
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetColor,
                           2, ArgumentLimits.exact,
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
        Public Class com_Swap
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Swap,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Swap_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueSwapSlotContents(arguments(0), arguments(1)).EvalOnSuccess(Function() "Swapped Slots")
            End Function
        End Class

        '''<summary>A command which places a computer in a slot.</summary>
        Public Class com_SetComputer
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetComputer,
                           1, ArgumentLimits.min,
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
        Public Class com_Lock
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Lock,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Instance_Lock_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 Then
                    Return target.QueueSetAllSlotsLocked(W3Slot.Lock.sticky).EvalOnSuccess(Function() "Locked slots")
                Else
                    Return target.QueueSetSlotLocked(arguments(0), W3Slot.Lock.sticky).EvalOnSuccess(Function() "Locked slots")
                End If
            End Function
        End Class

        '''<summary>A command which enables players to leave and modify a slot.</summary>
        Public Class com_Unlock
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Unlock,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Instance_Unlock_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 Then
                    Return target.QueueSetAllSlotsLocked(W3Slot.Lock.unlocked).EvalOnSuccess(Function() "Unlocked slots")
                Else
                    Return target.QueueSetSlotLocked(arguments(0), W3Slot.Lock.unlocked).EvalOnSuccess(Function() "Unlocked slots")
                End If
            End Function
        End Class

        '''<summary>A command which stops players from modifying or leaving a slot.</summary>
        Public Class com_Freeze
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Freeze,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Instance_Freeze_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 Then
                    Return target.QueueSetAllSlotsLocked(W3Slot.Lock.frozen).EvalOnSuccess(Function() "Froze slots")
                Else
                    Return target.QueueSetSlotLocked(arguments(0), W3Slot.Lock.frozen).EvalOnSuccess(Function() "Froze slots")
                End If
            End Function
        End Class

        '''<summary>A command which reserves a slot for a player.</summary>
        Public Class com_Reserve
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Reserve,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Reserve_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueReserveSlot(arguments(0), arguments(1)).EvalOnSuccess(Function() "Reserved Slot")
            End Function
        End Class

        '''<summary>A command which starts the launch countdown.</summary>
        Public Class com_Start
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Start,
                           0, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Start_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueStartCountdown().EvalOnSuccess(Function() "Started Countdown")
            End Function
        End Class

        '''<summary>A command which kills the instance.</summary>
        Public Class com_Cancel
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Cancel,
                           0, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Cancel_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.server.QueueKill().EvalOnSuccess(Function() "Cancelled")
            End Function
        End Class
    End Class

    Public Class InstanceAdminCommands
        Inherits CommandSet(Of W3Game)

        Public Sub New()
            AddCommand(New com_Bot)
        End Sub

        Public Class com_Bot
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Bot,
                           0, ArgumentLimits.free,
                           My.Resources.Command_Instance_Bot_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
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
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Boot,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Boot_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueBootSlot(arguments(0)).EvalOnSuccess(Function() "Booted")
            End Function
        End Class

        Public Class com_GetSetting
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New("Get",
                           1, ArgumentLimits.exact,
                           "[Get setting] Displays a game setting. Available settings are tickperiod laglimit gamerate.")
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim val As Object
                Select Case arguments(0).ToLower()
                    Case "tickperiod"
                        val = target.SettingTickPeriod
                    Case "laglimit"
                        val = target.SettingLagLimit
                    Case "gamerate"
                        val = target.settingSpeedFactor
                    Case Else
                        Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(arguments(0)))
                End Select
                Return "{0} = '{1}'".Frmt(arguments(0), val).Futurized
            End Function
        End Class
        Public Class com_SetSetting
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New("Set",
                           2, ArgumentLimits.exact,
                           "[Set setting] Changes a game setting. Available settings are tickperiod laglimit gamerate.")
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim val_us As UShort
                Dim vald As Double
                Dim is_short = UShort.TryParse(arguments(1), val_us)
                Dim is_double = Double.TryParse(arguments(1), vald)
                Select Case arguments(0).ToLower()
                    Case "tickperiod"
                        If Not is_short Or val_us < 50 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                        target.SettingTickPeriod = val_us
                    Case "laglimit"
                        If Not is_short Or val_us < 1 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                        target.SettingLagLimit = val_us
                    Case "gamerate"
                        If Not is_double Or vald < 0.01 Or vald > 10 Then Throw New ArgumentException("Invalid value")
                        target.settingSpeedFactor = vald
                    Case Else
                        Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(arguments(0)))
                End Select
                Return "{0} set to {1}".Frmt(arguments(0), arguments(1)).Futurized
            End Function
        End Class
    End Class

    Public Class InstanceBaseCommands
        Inherits UICommandSet(Of W3Game)

        Public Sub New()
            AddCommand(New com_Ping)
            AddCommand(New com_Leave)
        End Sub

        '''<summary>A command which disconnects the bot from the instance.</summary>
        Public Class com_Ping
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Ping,
                           0, ArgumentLimits.min,
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

        Public Class com_Leave
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New("Leave",
                           0, ArgumentLimits.exact,
                           "Disconnects you from the game (for when countdown is cancelled and you can't leave normally).")
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If user Is Nothing Then Throw New InvalidOperationException("You are not in the game.")
                Return target.QueueBootSlot(user.name).EvalOnSuccess(Function() "Left.")
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
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_VoteStart,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Instance_VoteStart_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 1 AndAlso arguments(0).ToLower <> "cancel" Then Throw New ArgumentException("Incorrect argument.")
                If user Is Nothing Then Throw New InvalidOperationException("User not specified.")
                Return target.QueuePlayerVoteToStart(user.name, arguments.Count = 0).EvalOnSuccess(Function() "Voted to start")
            End Function
        End Class

        Public Class com_Elevate
            Inherits BaseCommand(Of W3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Elevate,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Instance_Elevate_Help)
            End Sub
            Public Overrides Function Process(ByVal target As W3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If user Is Nothing Then Throw New InvalidOperationException("User not specified.")
                Return target.QueueTryElevatePlayer(user.name, arguments(0)).EvalOnSuccess(Function() "Elevated")
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
