Imports Tinker.Commands

Namespace WC3
    Public NotInheritable Class GameCommands
        Private Sub New()
        End Sub

        Public Shared Function MakeBotAdminCommands(ByVal bot As Bot.MainBot) As CommandSet(Of Game)
            Contract.Requires(bot IsNot Nothing)
            Contract.Ensures(Contract.Result(Of CommandSet(Of Game))() IsNot Nothing)
            Dim result = New CommandSet(Of Game)
            result.AddCommand(New CommandBot(bot))
            Return result
        End Function
        Public Shared Function MakeGuestLobbyCommands() As CommandSet(Of Game)
            Contract.Ensures(Contract.Result(Of CommandSet(Of Game))() IsNot Nothing)
            Dim result = New CommandSet(Of Game)
            result.AddCommand(New CommandPing)
            Return result
        End Function
        Public Shared Function MakeGuestInGameCommands() As CommandSet(Of Game)
            Contract.Ensures(Contract.Result(Of CommandSet(Of Game))() IsNot Nothing)
            Dim result = New CommandSet(Of Game)
            result.AddCommand(New CommandElevate)
            result.AddCommand(New CommandPing)
            result.AddCommand(New CommandVoteStart)
            Return result
        End Function
        Public Shared Function MakeHostLobbyCommands() As CommandSet(Of Game)
            Contract.Ensures(Contract.Result(Of CommandSet(Of Game))() IsNot Nothing)
            Dim result = New CommandSet(Of Game)
            result.AddCommand(New CommandBoot)
            result.AddCommand(New CommandCancel)
            result.AddCommand(New CommandClose)
            result.AddCommand(New CommandColor)
            result.AddCommand(New CommandCPU)
            result.AddCommand(New CommandGet)
            result.AddCommand(New CommandHandicap)
            result.AddCommand(New CommandLock)
            result.AddCommand(New CommandOpen)
            result.AddCommand(New CommandPing)
            result.AddCommand(New CommandRace)
            result.AddCommand(New CommandReserve)
            result.AddCommand(New CommandSet)
            result.AddCommand(New CommandSetTeam)
            result.AddCommand(New CommandSetupTeams)
            result.AddCommand(New CommandStart)
            result.AddCommand(New CommandSwap)
            result.AddCommand(New CommandUnlock)
            Return result
        End Function
        Public Shared Function MakeHostInGameCommands() As CommandSet(Of Game)
            Contract.Ensures(Contract.Result(Of CommandSet(Of Game))() IsNot Nothing)
            Dim result = New CommandSet(Of Game)
            result.AddCommand(New CommandBoot)
            result.AddCommand(New CommandDisconnect)
            result.AddCommand(New CommandGet)
            result.AddCommand(New CommandPing)
            result.AddCommand(New CommandSet)
            Return result
        End Function

        Private NotInheritable Class CommandBoot
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Boot",
                           template:="Name/Color -close",
                           Description:="Kicks a player from the game. Closes their slot if -close is specified.")
            End Sub
            Protected Overloads Overrides Async Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim slotQuery = argument.RawValue(0)
                Dim shouldClose = argument.HasOptionalSwitch("close")
                Await target.QueueBoot(slotQuery, shouldClose)
                Return "Booted"
            End Function
        End Class

        Private NotInheritable Class CommandBot
            Inherits Command(Of WC3.Game)
            Private ReadOnly bot As Bot.MainBot

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(bot IsNot Nothing)
            End Sub

            Public Sub New(ByVal bot As Bot.MainBot)
                MyBase.New(Name:="Bot",
                           Format:="subcommand...",
                           Description:="Forwards commands to the bot.")
                Contract.Requires(bot IsNot Nothing)
                Me.bot = bot
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As WC3.Game, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Return Tinker.Bot.MainBotManager.BotCommands.Invoke(bot, user, argument)
            End Function
        End Class

        Private NotInheritable Class CommandCancel
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Cancel",
                           template:="",
                           Description:="Closes this game instance.")
            End Sub
            Protected Overloads Overrides Async Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                target.Dispose()
                Await target.DisposalTask
                Return "Cancelled"
            End Function
        End Class

        Private NotInheritable Class CommandClose
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Close",
                           template:="slot",
                           Description:="Closes a slot.")
            End Sub
            Protected Overloads Overrides Async Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Await target.QueueCloseSlot(argument.RawValue(0))
                Return "Closed"
            End Function
        End Class

        Private NotInheritable Class CommandColor
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Color",
                           template:="slot value",
                           Description:="Sets the color of a slot. Not allowed when the map uses Fixed Player Settings.")
            End Sub
            Protected Overloads Overrides Async Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim argSlot = argument.RawValue(0)
                Dim argColor = argument.RawValue(1)
                Dim color = argColor.EnumTryParse(Of Protocol.PlayerColor)(ignoreCase:=True)
                If Not color.HasValue Then Throw New InvalidOperationException("Unrecognized color: '{0}'.".Frmt(argColor))
                Await target.QueueSetSlotColor(argSlot, color.Value)
                Return "Set Color"
            End Function
        End Class

        Private NotInheritable Class CommandCPU
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="CPU",
                           template:="slot ?difficulty",
                           Description:="Places a computer in a slot, unless it contains a player.")
            End Sub
            Protected Overloads Overrides Async Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim argSlot = argument.RawValue(0)
                Dim argDifficulty = If(argument.RawValueCount >= 2, argument.RawValue(1), WC3.Protocol.ComputerLevel.Normal.ToString)
                Dim difficulty = argDifficulty.EnumTryParse(Of Protocol.ComputerLevel)(ignoreCase:=True)
                If Not difficulty.HasValue Then Throw New InvalidOperationException("Unrecognized difficulty: '{0}'.".Frmt(argDifficulty))
                Await target.QueueSetSlotCpu(argSlot, difficulty.Value)
                Return "Set {0} to Computer ({1})".Frmt(argSlot, argDifficulty)
            End Function
        End Class

        Private NotInheritable Class CommandDisconnect
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Disconnect",
                           template:="",
                           Description:="Causes the bot to disconnect from the game. The game might continue if one of the players can host.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                target.Dispose()
                Return target.DisposalTask.ContinueWithFunc(Function() "Disconnected")
            End Function
        End Class

        Private NotInheritable Class CommandElevate
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Elevate",
                           template:="password",
                           Description:="Gives access to admin or host commands.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                If user Is Nothing Then Throw New InvalidOperationException("User not specified.")
                Return target.QueueElevatePlayer(user.Name, argument.RawValue(0)).ContinueWithFunc(Function() "Elevated")
            End Function
        End Class

        Private NotInheritable Class CommandGet
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Get",
                           template:="setting",
                           Description:="Returns the current value of a game setting {tickperiod, laglimit, gamerate}.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim val As Task(Of Double)
                Dim argSetting = argument.RawValue(0).ToInvariant
                Select Case argSetting
                    Case "TickPeriod" : val = From e In target.Motor.QueueGetTickPeriod Select e.TotalMilliseconds
                    Case "LagLimit" : val = From e In target.Motor.QueueGetLagLimit Select e.TotalMilliseconds
                    Case "GameRate" : val = From e In target.Motor.QueueGetSpeedFactor
                    Case Else : Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argSetting))
                End Select
                Return From v In val
                       Select "{0} = '{1}'".Frmt(argSetting, v)
            End Function
        End Class

        Private NotInheritable Class CommandHandicap
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Handicap",
                           template:="slot value",
                           Description:="Sets the handicap of a slot.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim argSlot = argument.RawValue(0)
                Dim argHandicap = argument.RawValue(1)
                Dim newHandicap As Byte
                If Not Byte.TryParse(argHandicap, newHandicap) Then newHandicap = 0
                Select Case newHandicap
                    Case 50, 60, 70, 80, 90, 100
                        Return target.QueueSetSlotHandicap(argSlot, newHandicap).ContinueWithFunc(Function() "Set Handicap")
                    Case Else
                        Throw New InvalidOperationException("Invalid handicap: '{0}'.".Frmt(argHandicap))
                End Select
            End Function
        End Class

        Private NotInheritable Class CommandLock
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Lock",
                           template:="?slot -full",
                           Description:="Prevents players from leaving a slot or from changing slot properties (if -full). Omit the slot argument to affect all slots.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim lockType = If(argument.HasOptionalSwitch("full"), WC3.Slot.LockState.Frozen, WC3.Slot.LockState.Sticky)
                If argument.RawValueCount = 0 Then
                    Return target.QueueSetAllSlotsLocked(lockType).ContinueWithFunc(Function() "Locked slots")
                Else
                    Return target.QueueSetSlotLocked(argument.RawValue(0), lockType).ContinueWithFunc(Function() "Locked slot")
                End If
            End Function
        End Class

        Private NotInheritable Class CommandOpen
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Open",
                           template:="slot",
                           Description:="Opens a slot.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.QueueOpenSlot(argument.RawValue(0)).ContinueWithFunc(Function() "Opened")
            End Function
        End Class

        Private NotInheritable Class CommandPing
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Ping",
                           template:="",
                           Description:="Returns estimated network round trip times for each player.")
            End Sub
            <ContractVerification(False)>
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Return From players In target.QueueGetPlayers()
                       From latencies In (From player In players Select player.QueueGetLatencyDescription).Cache.AsAggregateTask
                       Select "Estimated RTT: {0}".Frmt((From pair In players.Zip(latencies)
                                                         Where Not pair.Item1.isFake
                                                         Select "{0}={1}".Frmt(pair.Item1.Name, pair.Item2)
                                                         ).StringJoin(" "))
            End Function
        End Class

        Private NotInheritable Class CommandRace
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Race",
                           template:="slot race",
                           Description:="Sets the race of a slot. Not allowed when the map uses Fixed Player Settings and the slot race is not Selectable.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim argSlot = argument.RawValue(0)
                Dim argRace = argument.RawValue(1)
                Dim race = argRace.EnumTryParse(Of Protocol.Races)(ignoreCase:=True)
                If race.HasValue Then
                    Return target.QueueSetSlotRace(argSlot, race.Value).ContinueWithFunc(Function() "Set Race")
                Else
                    Throw New InvalidOperationException("Unrecognized race: '{0}'.".Frmt(argRace))
                End If
            End Function
        End Class

        Private NotInheritable Class CommandReserve
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Reserve",
                           template:="name -slot=any",
                           Description:="Reserves a slot for a player.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim name = argument.RawValue(0)
                Dim slotQueryString = argument.TryGetOptionalNamedValue("slot")
                Dim slotQuery = If(slotQueryString Is Nothing, Nothing, New InvariantString?(slotQueryString))

                Return target.QueueReserveSlot(name, slotQuery).
                                  ContinueWithFunc(Function() "Reserved {0} for {1}.".Frmt(If(slotQueryString, "slot"), name))
            End Function
        End Class

        Private NotInheritable Class CommandSet
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Set",
                           template:="setting value",
                           Description:="Sets the value of a game setting {tickperiod, laglimit, gamerate}.")
            End Sub
            <ContractVerification(False)>
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Dim val_us As UShort
                Dim vald As Double
                Dim isShort = UShort.TryParse(argument.RawValue(1), val_us)
                Dim isDouble = Double.TryParse(argument.RawValue(1), vald)
                Dim argSetting = argument.RawValue(0).ToInvariant
                Select Case argSetting
                    Case "TickPeriod"
                        If Not isShort Or val_us < 1 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                        target.Motor.QueueSetTickPeriod(CInt(val_us).Milliseconds)
                    Case "LagLimit"
                        If Not isShort Or val_us < 1 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                        target.Motor.QueueSetLagLimit(CInt(val_us).Milliseconds)
                    Case "GameRate"
                        If Not isDouble Or vald < 0.01 Or vald > 10 Then Throw New ArgumentException("Invalid value")
                        target.Motor.QueueSetSpeedFactor(vald)
                    Case Else
                        Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argument.RawValue(0)))
                End Select
                Return "{0} set to {1}".Frmt(argument.RawValue(0), argument.RawValue(1)).AsTask
            End Function
        End Class

        Private NotInheritable Class CommandSetTeam
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="SetTeam",
                           template:="slot team",
                           Description:="Sets a slot's team. Only works in melee games.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim argSlot = argument.RawValue(0)
                Dim argTeam = argument.RawValue(1)
                Dim team As Byte
                If Not Byte.TryParse(argTeam, team) OrElse team < 1 OrElse team > 13 Then
                    Throw New ArgumentException("Invalid team: '{0}'.".Frmt(argTeam))
                End If
                team -= CByte(1)
                Return target.QueueSetSlotTeam(argSlot, team).ContinueWithFunc(Function() "Set Team")
            End Function
        End Class

        Private NotInheritable Class CommandSetupTeams
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="SetupTeams",
                           template:="teams",
                           Description:="Sets up the number of slots on each team (eg. 'SetupTeams 2v2' will leave two open slots on each team).")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.QueueTrySetTeamSizes(TeamVersusStringToTeamSizes(argument.RawValue(0))).ContinueWithFunc(Function() "Set Teams")
            End Function
        End Class

        Private NotInheritable Class CommandStart
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Start",
                           template:="",
                           Description:="Starts the launch countdown.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.QueueStartCountdown().ContinueWithFunc(Function() "Started Countdown")
            End Function
        End Class

        Private NotInheritable Class CommandSwap
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Swap",
                           template:="slot1 slot2",
                           Description:="Swaps the contents of two slots.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.QueueSwapSlotContents(argument.RawValue(0), argument.RawValue(1)).ContinueWithFunc(Function() "Swapped Slots")
            End Function
        End Class

        Private NotInheritable Class CommandUnlock
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="Unlock",
                           template:="?slot",
                           Description:="Allows players to move from a slot and change its properties. Omit the slot argument to affect all slots.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim lockType = WC3.Slot.LockState.Unlocked
                If argument.RawValueCount = 0 Then
                    Return target.QueueSetAllSlotsLocked(lockType).ContinueWithFunc(Function() "Unlocked slots")
                Else
                    Return target.QueueSetSlotLocked(argument.RawValue(0), lockType).ContinueWithFunc(Function() "Unlocked slot")
                End If
            End Function
        End Class

        Private NotInheritable Class CommandVoteStart
            Inherits TemplatedCommand(Of Game)
            Public Sub New()
                MyBase.New(Name:="VoteStart",
                           template:="-cancel",
                           Description:="Places or cancels a vote to prematurely start an autostarted game. Requires at least 2 players and at least a 2/3 majority.")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As Game, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                If user Is Nothing Then Throw New InvalidOperationException("User not specified.")
                Return target.QueueSetPlayerVoteToStart(user.Name,
                                                        wantsToStart:=Not argument.HasOptionalSwitch("cancel")
                                                        ).ContinueWithFunc(Function() "Voted to start")
            End Function
        End Class
    End Class
End Namespace
