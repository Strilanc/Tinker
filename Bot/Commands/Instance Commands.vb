Imports HostBot.Commands
Imports HostBot.Warcraft3

Namespace Commands.Specializations
    Public Class InstancePlayCommands
        Inherits InstanceCommands(Of IW3GamePlay)

        Public Sub New()
            add_subcommand(New com_Disconnect)
        End Sub

        '''<summary>A command which disconnects the bot from the instance.</summary>
        Public Class com_Disconnect
            Inherits BaseCommand(Of IW3GamePlay)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Disconnect, _
                           0, ArgumentLimits.min, _
                           My.Resources.Command_Instance_Disconnect_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GamePlay, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                target.game.f_Close()
                Return futurize(New Outcome(Outcomes.succeeded))
            End Function
        End Class
    End Class

    Public Class InstanceSetupCommands
        Inherits InstanceCommands(Of IW3GameLobby)

        Public Sub New()
            add_subcommand(New com_Cancel)
            add_subcommand(New com_Close)
            add_subcommand(New com_Freeze)
            add_subcommand(New com_Lock)
            add_subcommand(New com_Open)
            add_subcommand(New com_Reserve)
            add_subcommand(New com_SetColor)
            add_subcommand(New com_SetComputer)
            add_subcommand(New com_SetHandicap)
            add_subcommand(New com_SetTeam)
            add_subcommand(New com_SetTeams)
            add_subcommand(New com_Start)
            add_subcommand(New com_Swap)
            add_subcommand(New com_Unlock)
        End Sub

        '''<summary>A command which opens a slot.</summary>
        Public Class com_Open
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Open, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_Open_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_OpenSlot(arguments(0))
            End Function
        End Class

        '''<summary>A command which closes a slot.</summary>
        Public Class com_Close
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Close, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_Close_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_CloseSlot(arguments(0))
            End Function
        End Class

        '''<summary>A command which sets a slot's team.</summary>
        Public Class com_SetTeam
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetTeam, _
                           2, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_SetTeam_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim arg_slot = arguments(0)
                Dim arg_team = arguments(1)
                Dim val_team As Byte
                If Not Byte.TryParse(arg_team, val_team) Then
                    Return futurize(failure("Invalid team: '{0}'.".frmt(arg_team)))
                End If
                Return target.f_SetSlotTeam(arg_slot, val_team)
            End Function
        End Class

        '''<summary>A command which preps slots for a particular number of players.</summary>
        Public Class com_SetTeams
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetTeams, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_SetTeams_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim out = W3Game.XvX(arguments(0))
                If out.outcome <> Outcomes.succeeded Then Return futurize(CType(out, Outcome))
                Return target.f_TrySetTeamSizes(out.val)
            End Function
        End Class

        '''<summary>A command which sets a slot's handicap.</summary>
        Public Class com_SetHandicap
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetHandicap, _
                           2, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_SetHandicap_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim arg_slot = arguments(0)
                Dim arg_handicap = arguments(1)
                Dim val_handicap As Byte = 0
                Byte.TryParse(arg_handicap, val_handicap)
                Select Case val_handicap
                    Case 50, 60, 70, 80, 90, 100
                        Return target.f_SetSlotHandicap(arg_slot, val_handicap)
                    Case Else
                        Return futurize(failure("Invalid handicap: '{0}'.".frmt(arg_handicap)))
                End Select
            End Function
        End Class

        '''<summary>A command which sets a slot's color.</summary>
        Public Class com_SetColor
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetColor, _
                           2, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_SetColor_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim arg_slot = arguments(0)
                Dim arg_color = arguments(1)
                Dim ret_color As W3Slot.PlayerColor
                If EnumTryParse(Of W3Slot.PlayerColor)(arg_color, True, ret_color) Then
                    Return target.f_SetSlotColor(arg_slot, ret_color)
                End If
                Return futurize(failure("Unrecognized color: '{0}'.".frmt(arg_color)))
            End Function
        End Class

        '''<summary>A command which swaps the contents of two slots.</summary>
        Public Class com_Swap
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Swap, _
                           2, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_Swap_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_SwapSlotContents(arguments(0), arguments(1))
            End Function
        End Class

        '''<summary>A command which places a computer in a slot.</summary>
        Public Class com_SetComputer
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_SetComputer, _
                           2, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_SetComputer_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim arg_slot = arguments(0)
                Dim arg_difficulty = arguments(1)
                Dim ret_difficulty As W3Slot.ComputerLevel
                If EnumTryParse(Of W3Slot.ComputerLevel)(arg_difficulty, True, ret_difficulty) Then
                    Return target.f_SetSlotCpu(arg_slot, ret_difficulty)
                End If
                Return futurize(failure("Unrecognized difficulty: '{0}'.".frmt(arg_difficulty)))
            End Function
        End Class

        '''<summary>A command which stops players from leaving a slot.</summary>
        Public Class com_Lock
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Lock, _
                           1, ArgumentLimits.max, _
                           My.Resources.Command_Instance_Lock_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 Then
                    Return target.f_SetAllSlotsLocked(W3Slot.Lock.sticky)
                Else
                    Return target.f_SetSlotLocked(arguments(0), W3Slot.Lock.sticky)
                End If
            End Function
        End Class

        '''<summary>A command which enables players to leave and modify a slot.</summary>
        Public Class com_Unlock
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Unlock, _
                           1, ArgumentLimits.max, _
                           My.Resources.Command_Instance_Unlock_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 Then
                    Return target.f_SetAllSlotsLocked(W3Slot.Lock.unlocked)
                Else
                    Return target.f_SetSlotLocked(arguments(0), W3Slot.Lock.unlocked)
                End If
            End Function
        End Class

        '''<summary>A command which stops players from modifying or leaving a slot.</summary>
        Public Class com_Freeze
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Freeze, _
                           1, ArgumentLimits.max, _
                           My.Resources.Command_Instance_Freeze_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 Then
                    Return target.f_SetAllSlotsLocked(W3Slot.Lock.frozen)
                Else
                    Return target.f_SetSlotLocked(arguments(0), W3Slot.Lock.frozen)
                End If
            End Function
        End Class

        '''<summary>A command which reserves a slot for a player.</summary>
        Public Class com_Reserve
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Reserve, _
                           2, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_Reserve_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_ReserveSlot(arguments(0), arguments(1))
            End Function
        End Class

        '''<summary>A command which starts the launch countdown.</summary>
        Public Class com_Start
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Start, _
                           0, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_Start_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_StartCountdown()
            End Function
        End Class

        '''<summary>A command which kills the instance.</summary>
        Public Class com_Cancel
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Cancel, _
                           0, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_Cancel_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.game.parent.f_Kill()
            End Function
        End Class
    End Class

    Public Class InstanceAdminCommands
        Inherits CommandSet(Of IW3Game)

        Public Sub New()
            add_subcommand(New com_Bot)
        End Sub

        Public Class com_Bot
            Inherits BaseCommand(Of IW3Game)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Bot, _
                           0, ArgumentLimits.free, _
                           My.Resources.Command_Instance_Bot_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3Game, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.parent.parent.bot_commands.processText(target.parent.parent, user, mendQuotedWords(arguments))
            End Function
        End Class
    End Class

    Public Class InstanceCommands(Of T As IW3GamePart)
        Inherits InstanceBaseCommands(Of T)

        Public Sub New()
            add_subcommand(New com_Boot)
        End Sub

        '''<summary>A command which boots players from a slot.</summary>
        Public Class com_Boot
            Inherits BaseCommand(Of T)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Boot, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_Boot_Help)
            End Sub
            Public Overrides Function process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.game.f_BootSlot(arguments(0))
            End Function
        End Class
    End Class

    Public Class InstanceBaseCommands(Of T As IW3GamePart)
        Inherits UICommandSet(Of T)

        Public Sub New()
            add_subcommand(New com_Ping)
            add_subcommand(New com_Leave)
        End Sub

        '''<summary>A command which disconnects the bot from the instance.</summary>
        Public Class com_Ping
            Inherits BaseCommand(Of T)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Ping, _
                           0, ArgumentLimits.min, _
                           My.Resources.Command_Instance_Ping_Help)
            End Sub
            Public Overrides Function process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return FutureFunc(Of Outcome).frun(AddressOf process2, target.game.f_EnumPlayers())
            End Function
            Private Function process2(ByVal players As List(Of IW3Player)) As Outcome
                Dim msg = "Estimated RTT:"
                For Each player In players
                    If player.is_fake Then Continue For
                    msg += " " + player.name + "=" + player.latency_P.ToString("0") + "ms"
                Next player
                Return success(msg)
            End Function
        End Class

        Public Class com_Leave
            Inherits BaseCommand(Of T)
            Public Sub New()
                MyBase.New("Leave", _
                           0, ArgumentLimits.exact, _
                           "Disconnects you from the game (for when countdown is cancelled and you can't leave normally).")
            End Sub
            Public Overrides Function process(ByVal target As T, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If user Is Nothing Then Return futurize(failure("You are not in the game."))
                Return target.game.f_BootSlot(user.name)
            End Function
        End Class
    End Class

    Public Class InstanceGuestSetupCommands
        Inherits InstanceBaseCommands(Of IW3GameLobby)

        Public Sub New()
            add_subcommand(New com_Elevate)
            add_subcommand(New com_VoteStart)
        End Sub

        Public Class com_VoteStart
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_VoteStart, _
                           1, ArgumentLimits.max, _
                           My.Resources.Command_Instance_VoteStart_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 1 AndAlso arguments(0).ToLower <> "cancel" Then Return futurize(failure("Incorrect argument."))
                If user Is Nothing Then Return futurize(failure("User not specified."))
                Return target.f_PlayerVoteToStart(user.name, arguments.Count = 0)
            End Function
        End Class

        Public Class com_Elevate
            Inherits BaseCommand(Of IW3GameLobby)
            Public Sub New()
                MyBase.New(My.Resources.Command_Instance_Elevate, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Instance_Elevate_Help)
            End Sub
            Public Overrides Function process(ByVal target As IW3GameLobby, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If user Is Nothing Then Return futurize(failure("User not specified."))
                Return target.game.f_TryElevatePlayer(user.name, arguments(0))
            End Function
        End Class
    End Class
    Public Class InstanceGuestLoadCommands
        Inherits InstanceBaseCommands(Of IW3GameLoadScreen)
    End Class
    Public Class InstanceGuestPlayCommands
        Inherits InstanceBaseCommands(Of IW3GamePlay)
    End Class
End Namespace
