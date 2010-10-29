Imports Tinker.Bot
Imports Tinker.Commands

Namespace Bnet
    Public NotInheritable Class ClientCommands
        Inherits CommandSet(Of Bnet.ClientManager)

        Public Sub New()
            AddCommand(New CommandBot)
            AddCommand(New CommandAddUser)
            AddCommand(New CommandDemote)
            AddCommand(New CommandRemoveUser)
            AddCommand(New CommandDisconnect)
            AddCommand(New Bot.GenericCommands.CommandFindMaps(Of Bnet.ClientManager))
            AddCommand(New Bot.GenericCommands.CommandDownloadMap(Of Bnet.ClientManager))
            AddCommand(New CommandPromote)
            AddCommand(New CommandUser)
            AddCommand(New CommandConnect)
            AddCommand(New CommandLogOn)
            AddCommand(New CommandAdminCode)
            AddCommand(New CommandCancelHost)
            AddCommand(New CommandElevate)
            AddCommand(New CommandGame)
            AddCommand(New CommandHost)
            AddCommand(New CommandSay)
            AddCommand(New CommandCancelAllHost)
            AddCommand(New CommandRefreshGamesList)
            AddCommand(New CommandAuto)
        End Sub

        Public Overloads Function AddCommand(ByVal command As Command(Of Bnet.Client)) As IDisposable
            Contract.Requires(command IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return AddCommand(New ProjectedCommand(Of Bnet.ClientManager, Bnet.Client)(
                    command:=command,
                    projection:=Function(manager) manager.Client))
        End Function

        Private NotInheritable Class CommandBot
            Inherits Command(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Bot",
                           Format:="...",
                           Description:="Forwards commands to the main bot.",
                           Permissions:="root:1")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.Bot.InvokeCommand(user, argument)
            End Function
        End Class

        Private NotInheritable Class CommandDisconnect
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Disconnect",
                           template:="",
                           Description:="Disconnects from bnet.",
                           Permissions:="root:4")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Await target.QueueDisconnect(expected:=True, reason:="Disconnect Command")
                Return "Disconnected"
            End Function
        End Class

        Private NotInheritable Class CommandAddUser
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="AddUser",
                           template:="username",
                           Description:="Adds a new user (permissions default to same as *NewUser).",
                           Permissions:="users:2")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim newUser = target.Profile.Users.CreateNewUser(argument.RawValue(0))
                Return "Created {0}".Frmt(newUser.Name).AsTask
            End Function
        End Class

        Private NotInheritable Class CommandRemoveUser
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="RemoveUser",
                           template:="username",
                           Description:="Removes an existing user.",
                           Permissions:="users:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                If Not target.Profile.Users.ContainsUser(argument.RawValue(0)) Then
                    Throw New ArgumentException("That user does not exist")
                End If
                Dim targetUser = target.Profile.Users(argument.RawValue(0))
                Contract.Assume(targetUser IsNot Nothing)
                If user IsNot Nothing AndAlso Not targetUser < user Then
                    Throw New ArgumentException("You can only destroy users with lower permissions.")
                End If
                target.Profile.Users.RemoveUser(argument.RawValue(0))
                Return "Removed {0}".Frmt(argument.RawValue(0)).AsTask
            End Function
        End Class

        Private NotInheritable Class CommandPromote
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Promote",
                           template:="username targetPermission newLevel",
                           Description:="Increases a user's permission's level.",
                           Permissions:="users:2")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                'Target user
                Contract.Assume(target IsNot Nothing)
                If Not target.Profile.Users.ContainsUser(argument.RawValue(0)) Then
                    Throw New ArgumentException("That user does not exist")
                End If
                Dim targetUser = target.Profile.Users(argument.RawValue(0))
                Contract.Assume(targetUser IsNot Nothing)

                'Level
                Dim lvl As UInteger
                If Not UInteger.TryParse(argument.RawValue(2), lvl) Then
                    Throw New ArgumentException("Expected numeric argument for level")
                End If

                'Check for demotion in disguise
                If lvl <= targetUser.Permission(argument.RawValue(1)) Then
                    Throw New ArgumentException("That is not a promotion. Jerk.")
                End If

                'Check for overpromotion
                If user IsNot Nothing AndAlso lvl > user.Permission(argument.RawValue(1)) Then
                    Throw New ArgumentException("You can't promote users past your own permission levels.")
                End If

                targetUser.Permission(argument.RawValue(1)) = lvl
                Return "{0} had permission in {1} promoted to {2}".Frmt(argument.RawValue(0), argument.RawValue(1), lvl).AsTask
            End Function
        End Class

        Private NotInheritable Class CommandDemote
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Demote",
                           template:="username targetPermission newLevel",
                           Description:="Decreases a user's permission's level.",
                           Permissions:="users:3")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                'Target user
                Contract.Assume(target IsNot Nothing)
                If Not target.Profile.Users.ContainsUser(argument.RawValue(0)) Then
                    Throw New ArgumentException("That user does not exist")
                End If
                Dim targetUser = target.Profile.Users(argument.RawValue(0))
                Contract.Assume(targetUser IsNot Nothing)

                'Level
                Dim lvl As UInteger
                If Not UInteger.TryParse(argument.RawValue(2), lvl) Then
                    Throw New ArgumentException("Expected numeric argument for level")
                End If

                'Check for promotion in disguise
                If lvl >= targetUser.Permission(argument.RawValue(1)) Then
                    Throw New ArgumentException("That is not a demotion.")
                End If

                'Check for overdemotion
                If user IsNot Nothing AndAlso Not targetUser < user Then
                    Throw New ArgumentException("You can only demote users with lower permissions.")
                End If

                targetUser.Permission(argument.RawValue(1)) = lvl
                Return "{0} had permission in {1} demoted to {2}".Frmt(argument.RawValue(0), argument.RawValue(1), lvl).AsTask
            End Function
        End Class

        Private NotInheritable Class CommandUser
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="User",
                           template:="?username",
                           Description:="Shows a user's permissions.",
                           Permissions:="")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Dim username = If(argument.TryGetRawValue(0), If(user Is Nothing, Nothing, user.Name.Value))
                If username Is Nothing Then
                    Throw New ArgumentException("No user specified.")
                End If
                Contract.Assume(target IsNot Nothing)
                If target.Profile.Users.ContainsUser(username) Then
                    Dim targetUser = target.Profile.Users(username)
                    Contract.Assume(targetUser IsNot Nothing)
                    Return targetUser.ToString.AsTask
                ElseIf target.Profile.Users.ContainsUser(BotUserSet.UnknownUserKey) Then
                    Return "{0} is an unknown user with the permissions of the user '*unknown'".Frmt(username).AsTask
                Else
                    Return "{0} is an ignored unknown user.".Frmt(username).AsTask
                End If
            End Function
        End Class

        Private NotInheritable Class CommandAuto
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Auto",
                           template:="On|Off",
                           Description:="Causes the client to automatically advertise any games on any server when 'On'.")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Select Case New InvariantString(argument.RawValue(0))
                    Case "On"
                        Await target.QueueSetAutomatic(True)
                        Return "Now automatically advertising games."
                    Case "Off"
                        Await target.QueueSetAutomatic(False)
                        Return "Now not automatically advertising games."
                    Case Else
                        Throw New ArgumentException("Must specify 'On' or 'Off' as an argument.")
                End Select
            End Function
        End Class

        Private NotInheritable Class CommandLogOn
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="LogOn",
                           template:="username password",
                           Description:="Identifies and authenticates the client as a particular bnet user.",
                           Permissions:="root:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim userName = argument.RawValue(0)
                Dim password = argument.RawValue(1)
                Return target.QueueLogOn(Bnet.ClientAuthenticator.GeneratedFrom(userName, password)).
                              ContinueWithFunc(Function() "Logged in as {0}".Frmt(userName))
            End Function
        End Class

        Private NotInheritable Class CommandHost
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Host",
                            template:=Concat({"name=<game name>", "map=<search query>"},
                                             WC3.GameSettings.PartialArgumentTemplates,
                                             WC3.GameStats.PartialArgumentTemplates).StringJoin(" "),
                            Description:="Hosts a game in the custom games list. See 'Help Host *' for more help topics.",
                            Permissions:="games:1",
                            extraHelp:=Concat(WC3.GameSettings.PartialArgumentHelp,
                                              WC3.GameStats.PartialArgumentHelp).StringJoin(Environment.NewLine))
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim server = Await target.Bot.QueueGetOrConstructGameServer()
                Dim gameSet = Await server.QueueAddGameFromArguments(argument, user)
                Try
                    'Link user to gameSet
                    If user IsNot Nothing Then
                        target.QueueSetUserGameSet(user, gameSet)
                        gameSet.DisposalTask.ContinueWithAction(Sub() target.QueueResetUserGameSet(user, gameSet)).IgnoreExceptions()
                    End If

                    'Setup auto-dispose
                    Dim onStarted As WC3.GameSet.StateChangedEventHandler
                    onStarted = Sub(sender, active)
                                    If active Then Return
                                    RemoveHandler gameSet.StateChanged, onStarted
                                    target.Client.QueueRemoveAdvertisableGame(gameSet.GameSettings.GameDescription)
                                End Sub
                    AddHandler gameSet.StateChanged, onStarted

                    'Start advertising game
                    Dim gameDescription = Await target.Client.QueueAddAdvertisableGame(gameDescription:=gameSet.GameSettings.GameDescription,
                                                                                       isPrivate:=gameSet.GameSettings.IsPrivate)
                    Return "Hosted game '{0}' for map '{1}'. Admin Code: {2}".Frmt(gameDescription.Name,
                                                                                   gameDescription.GameStats.AdvertisedPath,
                                                                                   gameSet.GameSettings.AdminPassword)
                Catch ex As Exception
                    gameSet.Dispose()
                    Throw
                End Try
            End Function
        End Class

        Private NotInheritable Class CommandGame
            Inherits PartialCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Game",
                           headType:="InstanceName",
                           Description:="Forwards commands to an instance in your hosted game. By default game instances are numbered, starting with 0.")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                If user Is Nothing Then Throw New InvalidOperationException("This command is not meant for local usage.")
                Dim gameName = argumentHead

                'Find hosted game set
                Dim gameSet = Await target.QueueTryGetUserGameSet(user)
                If gameSet Is Nothing Then
                    Throw New InvalidOperationException("You don't have a hosted game.")
                End If

                'Find named instance
                Dim game = Await gameSet.QueueTryFindGame(gameName)
                If game Is Nothing Then Throw New InvalidOperationException("No matching game instance found.")

                'Pass command
                Return Await game.QueueCommandProcessText(target.Bot, Nothing, argumentRest)
            End Function
        End Class

        Private NotInheritable Class CommandRefreshGamesList
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="RefreshGamesList",
                           template:="",
                           Description:="Refreshes the bot's game list display. No useful effect from bnet.",
                           Permissions:="local:1")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Await target.QueueSendPacket(Protocol.MakeQueryGamesList())
                Return "Sent request."
            End Function
        End Class

        Private NotInheritable Class CommandCancelHost
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="CancelHost",
                template:="",
                Description:="Cancels a host command if it was issued by you, and unlinks the attached server.",
                Permissions:="")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                If user Is Nothing Then Throw New InvalidOperationException("This command is not meant for local usage.")

                Dim gameSet = Await target.QueueTryGetUserGameSet(user)
                If gameSet Is Nothing Then Throw New InvalidOperationException("You don't have a hosted game to cancel.")

                gameSet.Dispose()
                Return "Cancelled hosting."
            End Function
        End Class

        Private NotInheritable Class CommandAdminCode
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="AdminCode",
                           template:="",
                           Description:="Returns the admin code for a game you have hosted.",
                           Permissions:="")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                If user Is Nothing Then Throw New InvalidOperationException("This command is not meant for local usage.")

                Dim gameSet = Await target.QueueTryGetUserGameSet(user)
                If gameSet Is Nothing Then Throw New InvalidOperationException("You don't have a hosted game to cancel.")

                Return gameSet.GameSettings.AdminPassword
            End Function
        End Class

        Private NotInheritable Class CommandSay
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Say",
                           template:="text",
                           Description:="Causes the bot to say the given text, including any bnet commands.",
                           Permissions:="root:1")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim text = argument.RawValue(0)
                If text.Length = 0 Then Throw New ArgumentException("Must say something.")
                Await target.QueueSendText(text)
                Return "Said {0}".Frmt(text)
            End Function
        End Class

        Private NotInheritable Class CommandCancelAllHost
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="CancelAllHost",
                           template:="",
                           Description:="Stops placing a game in the custom games list and unlinks from any linked server.",
                           Permissions:="root:4,games:5")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Await target.QueueRemoveAllAdvertisableGames()
                Return "Cancelled advertising of all games."
            End Function
        End Class

        Private NotInheritable Class CommandElevate
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Elevate",
                           template:="-player=name",
                           Description:="Elevates you or a specified player to admin in your hosted game.",
                           Permissions:="games:1")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                If user Is Nothing Then Throw New InvalidOperationException("This command is not meant for local usage.")

                Dim username = If(argument.TryGetOptionalNamedValue("player"), user.Name.Value)
                If username Is Nothing Then Throw New ArgumentException("No player specified.")

                'Find player's game set
                Dim gameSet = Await target.QueueTryGetUserGameSet(user)
                If gameSet Is Nothing Then Throw New InvalidOperationException("You don't have a hosted game.")

                'Find player's game
                Dim game = Await gameSet.QueueTryFindPlayerGame(username)
                If game Is Nothing Then Throw New InvalidOperationException("No matching user found.")

                'Elevate player
                Await game.QueueElevatePlayer(username)
                return "'{0}' is now the admin.".Frmt(username)
            End Function
        End Class

        Private NotInheritable Class CommandConnect
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Connect",
                           template:="Hostname",
                           Description:="Connects to a battle.net server at the given hostname.",
                           Permissions:="root:4")
            End Sub
            Protected Overrides Async Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim remoteHost = argument.RawValue(0)
                Dim port = Bnet.Client.BnetServerPort
                If remoteHost.Contains(":"c) Then
                    Dim parts = remoteHost.Split(":"c)
                    If parts.Count <> 2 OrElse Not UShort.TryParse(parts.Last, NumberStyles.Integer, CultureInfo.InvariantCulture, port) Then
                        Throw New ArgumentException("Invalid hostname.")
                    End If
                    remoteHost = parts.First.AssumeNotNull
                End If

                Await target.QueueConnect(remoteHost, port)
                Return "Established connection to {0}".Frmt(remoteHost)
            End Function
        End Class
    End Class
End Namespace
