Imports Tinker.Bot
Imports Tinker.Commands

Namespace Bnet
    Public NotInheritable Class ClientCommands
        Inherits CommandSet(Of Bnet.ClientManager)

        Public Sub New()
            AddCommand(New CBot)
            AddCommand(New CAddUser)
            AddCommand(New CDemote)
            AddCommand(New CRemoveUser)
            AddCommand(New CDisconnect)
            AddCommand(New Bot.GenericCommands.CFindMaps(Of Bnet.ClientManager))
            AddCommand(New Bot.GenericCommands.CDownloadMap(Of Bnet.ClientManager))
            AddCommand(New CPromote)
            AddCommand(New CUser)
            AddCommand(New CConnect)
            AddCommand(New CLogOn)
            AddCommand(New CAdminCode)
            AddCommand(New CCancelHost)
            AddCommand(New CElevate)
            AddCommand(New CGame)
            AddCommand(New CHost)
            AddCommand(New CSay)
            AddCommand(New CStartAdvertising)
            AddCommand(New CStopAdvertising)
            AddCommand(New CRefreshGamesList)
            AddCommand(New CAuto)
        End Sub

        Public Overloads Function AddCommand(ByVal command As Command(Of Bnet.Client)) As IDisposable
            Contract.Requires(command IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return AddCommand(New ProjectedCommand(Of Bnet.ClientManager, Bnet.Client)(
                    command:=command,
                    projection:=Function(manager) manager.Client))
        End Function

        Private NotInheritable Class CBot
            Inherits Command(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Bot",
                           Format:="...",
                           Description:="Forwards commands to the main bot.",
                           Permissions:="root:1")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.Bot.InvokeCommand(user, argument)
            End Function
        End Class

        Private NotInheritable Class CDisconnect
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Disconnect",
                           template:="",
                           Description:="Disconnects from bnet.",
                           Permissions:="root:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.QueueDisconnect(expected:=True, reason:="Disconnect Command").EvalOnSuccess(Function() "Disconnected")
            End Function
        End Class

        Private NotInheritable Class CAddUser
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="AddUser",
                           template:="username",
                           Description:="Adds a new user (permissions default to same as *NewUser).",
                           Permissions:="users:2")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim newUser = target.Profile.Users.CreateNewUser(argument.RawValue(0))
                Return "Created {0}".Frmt(newUser.Name).Futurized
            End Function
        End Class

        Private NotInheritable Class CRemoveUser
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="RemoveUser",
                           template:="username",
                           Description:="Removes an existing user.",
                           Permissions:="users:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
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
                Return "Removed {0}".Frmt(argument.RawValue(0)).Futurized
            End Function
        End Class

        Private NotInheritable Class CPromote
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Promote",
                           template:="username targetPermission newLevel",
                           Description:="Increases a user's permission's level.",
                           Permissions:="users:2")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
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
                Return "{0} had permission in {1} promoted to {2}".Frmt(argument.RawValue(0), argument.RawValue(1), lvl).Futurized
            End Function
        End Class

        Private NotInheritable Class CDemote
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Demote",
                           template:="username targetPermission newLevel",
                           Description:="Decreases a user's permission's level.",
                           Permissions:="users:3")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
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
                Return "{0} had permission in {1} demoted to {2}".Frmt(argument.RawValue(0), argument.RawValue(1), lvl).Futurized
            End Function
        End Class

        Private NotInheritable Class CUser
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="User",
                           template:="?username",
                           Description:="Shows a user's permissions.",
                           Permissions:="")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Dim username = If(argument.TryGetRawValue(0), If(user Is Nothing, Nothing, user.Name.Value))
                If username Is Nothing Then
                    Throw New ArgumentException("No user specified.")
                End If
                Contract.Assume(target IsNot Nothing)
                If target.Profile.Users.ContainsUser(username) Then
                    Dim targetUser = target.Profile.Users(username)
                    Contract.Assume(targetUser IsNot Nothing)
                    Return targetUser.ToString.Futurized
                ElseIf target.Profile.Users.ContainsUser(BotUserSet.UnknownUserKey) Then
                    Return "{0} is an unknown user with the permissions of the user '*unknown'".Frmt(username).Futurized
                Else
                    Return "{0} is an ignored unknown user.".Frmt(username).Futurized
                End If
            End Function
        End Class

        Private NotInheritable Class CAuto
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Auto",
                           template:="On|Off",
                           Description:="Causes the client to automatically advertise any games on any server when 'On'.")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Select Case New InvariantString(argument.RawValue(0))
                    Case "On"
                        Return target.QueueSetAutomatic(True).EvalOnSuccess(Function() "Now automatically advertising games.")
                    Case "Off"
                        Return target.QueueSetAutomatic(False).EvalOnSuccess(Function() "Now not automatically advertising games.")
                    Case Else
                        Throw New ArgumentException("Must specify 'On' or 'Off' as an argument.")
                End Select
            End Function
        End Class

        Private NotInheritable Class CLogOn
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="LogOn",
                           template:="username password",
                           Description:="Identifies and authenticates the client as a particular bnet user.",
                           Permissions:="root:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.QueueLogOn(New Bnet.ClientCredentials(argument.RawValue(0), argument.RawValue(1))).
                              EvalOnSuccess(Function() "Logged in as {0}".Frmt(argument.RawValue(0)))
            End Function
        End Class

        Private NotInheritable Class CHost
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Host",
                            template:=Fold({New String() {"name=<game name>", "map=<search query>"},
                                            WC3.GameSettings.PartialArgumentTemplates,
                                            WC3.GameStats.PartialArgumentTemplates}).StringJoin(" "),
                            Description:="Hosts a game in the custom games list. See 'Help Host *' for more help topics.",
                            Permissions:="games:1",
                            extraHelp:=Fold({WC3.GameSettings.PartialArgumentHelp,
                                             WC3.GameStats.PartialArgumentHelp}).StringJoin(Environment.NewLine))
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim futureServer = target.Bot.QueueGetOrConstructGameServer()
                Dim futureGameSet = (From server In futureServer
                                     Select server.QueueAddGameFromArguments(argument, user)
                                     ).Defuturized
                Dim futureAdvertised = futureGameSet.Select(
                    Function(gameSet)
                        Dim result = target.Client.QueueStartAdvertisingGame(gameDescription:=gameSet.GameSettings.GameDescription,
                                                                             isPrivate:=gameSet.GameSettings.IsPrivate)
                        If user IsNot Nothing Then
                            target.QueueSetUserGameSet(user, gameSet)
                            gameSet.FutureDisposed.CallOnSuccess(Sub() target.QueueResetUserGameSet(user, gameSet)).SetHandled()
                        End If
                        Dim onStarted As WC3.GameSet.StateChangedEventHandler
                        onStarted = Sub(sender, active)
                                        If active Then Return
                                        RemoveHandler gameSet.StateChanged, onStarted
                                        target.Client.QueueStopAdvertisingGame(id:=gameSet.GameSettings.GameDescription.GameId, reason:="Game Started")
                                    End Sub
                        AddHandler gameSet.StateChanged, onStarted
                        Return result
                    End Function).Defuturized
                futureAdvertised.Catch(Sub() If futureGameSet.State = FutureState.Succeeded Then futureGameSet.Value.Dispose())
                Dim futureDesc = futureAdvertised.EvalOnSuccess(Function() futureGameSet.Value.GameSettings.GameDescription)
                Return futureDesc.Select(Function(desc) "Hosted game '{0}' for map '{1}'. Admin Code: {2}".Frmt(desc.Name,
                                                                                                                desc.GameStats.AdvertisedPath,
                                                                                                                futureGameSet.Value.GameSettings.AdminPassword))
            End Function
        End Class

        Private NotInheritable Class CGame
            Inherits PartialCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Game",
                           headType:="InstanceName",
                           Description:="Forwards commands to an instance in your hosted game. By default game instances are numbered, starting with 0.")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As Strilbrary.Threading.IFuture(Of String)
                If user Is Nothing Then Throw New InvalidOperationException("This command is not meant for local usage.")
                Dim gameName = argumentHead

                'Find hosted game set, then find named instance, then pass command
                Contract.Assume(target IsNot Nothing)
                Return target.QueueTryGetUserGameSet(user).Select(
                    Function(gameSet)
                        If gameSet Is Nothing Then
                            Throw New InvalidOperationException("You don't have a hosted game.")
                        End If

                        'Find named instance, then pass command
                        Return gameSet.QueueTryFindGame(gameName).Select(
                            Function(game)
                                If game Is Nothing Then
                                    Throw New InvalidOperationException("No matching game instance found.")
                                End If

                                Return game.QueueCommandProcessText(target.Bot, Nothing, argumentRest)
                            End Function
                        )
                            End Function
                ).Defuturized.Defuturized
            End Function
        End Class

        Private NotInheritable Class CRefreshGamesList
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="RefreshGamesList",
                           template:="",
                           Description:="Refreshes the bot's game list display. No useful effect from bnet.",
                           Permissions:="local:1")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.QueueSendPacket(Protocol.MakeQueryGamesList()).EvalOnSuccess(Function() "Sent request.")
            End Function
        End Class

        Private NotInheritable Class CCancelHost
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="CancelHost",
                template:="",
                Description:="Cancels a host command if it was issued by you, and unlinks the attached server.",
                Permissions:="")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                If user Is Nothing Then Throw New InvalidOperationException("This command is not meant for local usage.")
                Return target.QueueTryGetUserGameSet(user).Select(
                  Function(gameSet)
                      If gameSet Is Nothing Then
                          Throw New InvalidOperationException("You don't have a hosted game to cancel.")
                      End If

                      gameSet.Dispose()
                      Return "Cancelled hosting."
                  End Function
              )
            End Function
        End Class

        Private NotInheritable Class CAdminCode
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="AdminCode",
                           template:="",
                           Description:="Returns the admin code for a game you have hosted.",
                           Permissions:="")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                If user Is Nothing Then Throw New InvalidOperationException("This command is not meant for local usage.")
                Return target.QueueTryGetUserGameSet(user).Select(
                    Function(gameSet)
                        If gameSet Is Nothing Then
                            Throw New InvalidOperationException("You don't have a hosted game to cancel.")
                        End If
                        Return gameSet.GameSettings.AdminPassword
                    End Function)
            End Function
        End Class

        Private NotInheritable Class CSay
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Say",
                           template:="text",
                           Description:="Causes the bot to say the given text, including any bnet commands.",
                           Permissions:="root:1")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim text = argument.RawValue(0)
                If text.Length = 0 Then Throw New ArgumentException("Must say something.")
                Return target.QueueSendText(text).EvalOnSuccess(Function() "Said {0}".Frmt(text))
            End Function
        End Class

        Private NotInheritable Class CStartAdvertising
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="StartAdvertising",
                           template:="name=<game name> map=<search query> -private -p",
                           Description:="Places a game in the custom games list, but doesn't start a new game server to accept the joiners.",
                           Permissions:="root:4,games:5")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))
                Dim hostname As InvariantString = If(user Is Nothing, Application.ProductName.AssumeNotNull, user.Name.Value)
                Return target.QueueStartAdvertisingGame(
                    gameDescription:=WC3.LocalGameDescription.FromArguments(
                        name:=argument.NamedValue("name"),
                        map:=map,
                        stats:=New WC3.GameStats(map, hostname, argument),
                        clock:=New SystemClock()),
                    isPrivate:=argument.HasOptionalSwitch("Private") OrElse argument.HasOptionalSwitch("p")).EvalOnSuccess(Function() "Started advertising")
            End Function
        End Class

        Private NotInheritable Class CStopAdvertising
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="StopAdvertising",
                           template:="",
                           Description:="Stops placing a game in the custom games list and unlinks from any linked server.",
                           Permissions:="root:4,games:5")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Return target.QueueStopAdvertisingGame("Advertising stopped manually.").EvalOnSuccess(Function() "Stopped advertising.")
            End Function
        End Class

        Private NotInheritable Class CElevate
            Inherits TemplatedCommand(Of Bnet.ClientManager)
            Public Sub New()
                MyBase.New(Name:="Elevate",
                           template:="-player=name",
                           Description:="Elevates you or a specified player to admin in your hosted game.",
                           Permissions:="games:1")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.ClientManager, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                If user Is Nothing Then Throw New InvalidOperationException("This command is not meant for local usage.")
                Dim username = If(argument.TryGetOptionalNamedValue("player"), user.Name.Value)
                If username Is Nothing Then Throw New ArgumentException("No player specified.")

                'Find hosted server, then find player's game, then elevate player
                Contract.Assume(target IsNot Nothing)
                Return target.QueueTryGetUserGameSet(user).Select(
                    Function(gameSet)
                        If gameSet Is Nothing Then
                            Throw New InvalidOperationException("You don't have a hosted game.")
                        End If

                        'Find player's game, then elevate player
                        Return gameSet.QueueTryFindPlayerGame(username).Select(
                            Function(game)
                                If game Is Nothing Then
                                    Throw New InvalidOperationException("No matching user found.")
                                End If

                                'Elevate player
                                Return game.QueueElevatePlayer(username)
                            End Function
                        )
                            End Function
                ).Defuturized.Defuturized.EvalOnSuccess(Function() "'{0}' is now the admin.".Frmt(username))
            End Function
        End Class

        Private NotInheritable Class CConnect
            Inherits TemplatedCommand(Of Bnet.Client)
            Public Sub New()
                MyBase.New(Name:="Connect",
                           template:="Hostname",
                           Description:="Connects to a battle.net server at the given hostname.",
                           Permissions:="root:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim remoteHost = argument.RawValue(0)
                Return target.QueueConnect(remoteHost).EvalOnSuccess(Function() "Established connection to {0}".Frmt(remoteHost))
            End Function
        End Class
    End Class
End Namespace
