Imports HostBot.Commands
Imports HostBot.Links

Namespace Commands.Specializations
    Public NotInheritable Class ClientCommands
        Inherits Command(Of Bnet.Client)

        Private com_login As CommandSet(Of Bnet.Client) = New ClientLogOnCommands()
        Private com_online As CommandSet(Of Bnet.Client) = New ClientOnlineCommands()
        Private com_offline As CommandSet(Of Bnet.Client) = New ClientOfflineCommands()

        Public Sub New()
            MyBase.New("Bnet.Client", "...", "Bnet.Client Commands")
        End Sub

        Private Function GetCurrentCommandSet(ByVal target As Bnet.Client) As IFuture(Of CommandSet(Of Bnet.Client))
            Contract.Ensures(Contract.Result(Of IFuture(Of BaseClientCommands))() IsNot Nothing)
            Return target.QueueGetState.Select(
                Function(state)
                    Select Case state
                        Case Bnet.ClientState.Channel, Bnet.ClientState.CreatingGame, Bnet.ClientState.AdvertisingGame
                            Return com_online
                        Case Bnet.ClientState.Connecting, Bnet.ClientState.Disconnected
                            Return com_offline
                        Case Bnet.ClientState.AuthenticatingUser, Bnet.ClientState.EnterUserCredentials
                            Return com_login
                        Case Else
                            Throw state.MakeImpossibleValueException()
                    End Select
                End Function
            )
        End Function
        Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
            Return GetCurrentCommandSet(target).Select(Function(x) x.Invoke(target, user, argument)).Defuturized()
        End Function
        Public Sub ProcessLocalText(ByVal target As Bnet.Client, ByVal text As String, ByVal logger As Logger)
            GetCurrentCommandSet(target).CallOnValueSuccess(Sub(x) x.ProcessLocalText(target, text, logger))
        End Sub

        Private MustInherit Class BaseClientCommands
            Inherits CommandSet(Of Bnet.Client)
            Protected Sub New()
                AddCommand(AdLink)
                AddCommand(AdUnlink)
                AddCommand(Bot)
                AddCommand(AddUser)
                AddCommand(Demote)
                AddCommand(RemoveUser)
                AddCommand(Disconnect)
                AddCommand(New InheritedCommand(New BotCommands.CommandFindMaps))
                AddCommand(GetPort)
                AddCommand(SetPort)
                AddCommand(Promote)
                AddCommand(User)
            End Sub
        End Class
        Private NotInheritable Class ClientOfflineCommands
            Inherits BaseClientCommands
            Public Sub New()
                AddCommand(Connect)
            End Sub
        End Class
        Private NotInheritable Class ClientLogOnCommands
            Inherits BaseClientCommands
            Public Sub New()
                AddCommand(LogOn)
            End Sub
        End Class
        Private NotInheritable Class ClientOnlineCommands
            Inherits BaseClientCommands
            Public Sub New()
                AddCommand(AdminCode)
                AddCommand(CancelHost)
                AddCommand(Elevate)
                AddCommand(Game)
                AddCommand(Host)
                AddCommand(Say)
                AddCommand(StartAdvertising)
                AddCommand(StopAdvertising)
                AddCommand(RefreshGamesList)
            End Sub
        End Class

        Private Shared ReadOnly AdLink As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="AdLink",
            template:="otherClientName",
            Description:="Causes the other client to advertise the same games as this client.",
            Permissions:="root=3",
            func:=Function(client, user, argument)
                      Dim otherClient = client.Parent.QueueFindClient(argument.RawValue(0))
                      Return otherClient.Select(
                      Function(client2)
                          If client2 Is Nothing Then
                              Throw New InvalidOperationException("No client matching that name.")
                          ElseIf client2 Is client Then
                              Throw New InvalidOperationException("Can't link to self.")
                          End If

                          AdvertisingLink.CreateMultiWayLink({client, client2})
                          Return "Created an advertising link between client {0} and client {1}.".Frmt(client.Name, client2.Name)
                      End Function
                      )
                      End Function)
        Private Shared ReadOnly AdUnlink As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="AdUnlink",
            template:="otherClientName",
            Description:="Breaks any advertising link to the other client.",
            Permissions:="root=4",
            func:=Function(client, user, argument)
                      Dim otherClient = client.Parent.QueueFindClient(argument.RawValue(0))
                      Return otherClient.Select(
                          Function(client2)
                              If client2 Is Nothing Then
                                  Throw New ArgumentException("No client matching that name.")
                              ElseIf client2 Is client Then
                                  Throw New ArgumentException("Can't link to self.")
                              End If

                              client.QueueRemoveAdvertisingPartner(client2)
                              Return "Any link between client {0} and client {1} has been removed.".Frmt(client.Name, client2.Name)
                          End Function
                      )
                          End Function)

        Private Shared ReadOnly Bot As New DelegatedCommand(Of Bnet.Client)(
            Name:="Bot",
            Format:="...",
            Description:="Forwards commands to the main bot.",
            Permissions:="root=1",
            func:=Function(client, user, argument)
                      Return client.Parent.BotCommands.invoke(client.Parent, user, argument)
                  End Function)

        Public NotInheritable Class InheritedCommand
            Inherits Command(Of Bnet.Client)
            Private subCommand As Command(Of MainBot)
            Public Sub New(ByVal subCommand As Command(Of MainBot))
                MyBase.New(subCommand.Name, subCommand.Format, subCommand.Description, "", "", subCommand.HasPrivateArguments)
                Me.subCommand = subCommand
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As Bnet.Client, ByVal user As BotUser, ByVal argument As String) As Strilbrary.Threading.IFuture(Of String)
                Return subCommand.Invoke(target.Parent, user, argument)
            End Function
        End Class

        Private Shared ReadOnly GetPort As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="GetPort",
            template:="",
            Description:="Returns the listen port reported to bnet.",
            Permissions:="root=1",
            func:=Function(client, user, argument)
                      Return client.QueueGetListenPort.Select(Function(port) port.ToString(CultureInfo.InvariantCulture))
                  End Function)

        Private Shared ReadOnly SetPort As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="SetPort",
            template:="port",
            Description:="Changes listen port reported to bnet.",
            Permissions:="root=4",
            func:=Function(client, user, argument)
                      Dim port As UShort
                      If Not UShort.TryParse(argument.RawValue(0), port) Then Throw New ArgumentException("Invalid port")
                      Return client.QueueSetListenPort(port).EvalOnSuccess(Function() "Set listen port to {0}.".Frmt(port))
                  End Function)

        Private Shared ReadOnly Disconnect As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="Disconnect",
            template:="",
            Description:="Disconnects from bnet.",
            Permissions:="root=4",
            func:=Function(client, user, argument)
                      Return client.QueueDisconnect(expected:=True, reason:="Disconnect Command").EvalOnSuccess(Function() "Disconnected")
                  End Function)

        Private Shared ReadOnly AddUser As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="AddUser",
            template:="username",
            Description:="Adds a new user, with the default new user permissions.",
            Permissions:="users=2",
            func:=Function(client, user, argument)
                      Dim newUser = client.profile.users.CreateNewUser(argument.RawValue(0))
                      Return "Created {0}".Frmt(newUser.Name).Futurized
                  End Function)

        Private Shared ReadOnly RemoveUser As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="RemoveUser",
            template:="username",
            Description:="Removes an existing user.",
            Permissions:="users=4",
            func:=Function(client, user, argument)
                      If Not client.profile.users.ContainsUser(argument.RawValue(0)) Then
                          Throw New ArgumentException("That user does not exist")
                      End If
                      Dim targetUser = client.profile.users(argument.RawValue(0))
                      Contract.Assume(targetUser IsNot Nothing)
                      If user IsNot Nothing AndAlso Not targetUser < user Then
                          Throw New ArgumentException("You can only destroy users with lower permissions.")
                      End If
                      client.profile.users.RemoveUser(argument.RawValue(0))
                      Return "Removed {0}".Frmt(argument.RawValue(0)).Futurized
                  End Function)

        Private Shared ReadOnly Promote As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="Promote",
            template:="username targetPermission newLevel",
            Description:="Increases a user's permission's level.",
            Permissions:="users=2",
            func:=Function(client, user, argument)
                      'Target user
                      If Not client.profile.users.ContainsUser(argument.RawValue(0)) Then
                          Throw New ArgumentException("That user does not exist")
                      End If
                      Dim target_user As BotUser = client.profile.users(argument.RawValue(0))

                      'Level
                      Dim lvl As UInteger
                      If Not UInteger.TryParse(argument.RawValue(2), lvl) Then
                          Throw New ArgumentException("Expected numeric argument for level")
                      End If

                      'Check for demotion in disguise
                      If lvl <= target_user.Permission(argument.RawValue(1)) Then
                          Throw New ArgumentException("That is not a promotion. Jerk.")
                      End If

                      'Check for overpromotion
                      If user IsNot Nothing AndAlso lvl > user.Permission(argument.RawValue(1)) Then
                          Throw New ArgumentException("You can't promote users past your own permission levels.")
                      End If

                      target_user.Permission(argument.RawValue(1)) = lvl
                      Return "{0} had permission in {1} promoted to {2}".Frmt(argument.RawValue(0), argument.RawValue(1), lvl).Futurized
                  End Function)

        Private Shared ReadOnly Demote As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="Demote",
            template:="username targetPermission newLevel",
            Description:="Decreases a user's permission's level.",
            Permissions:="users=3",
            func:=Function(client, user, argument)
                      'Target user
                      If Not client.profile.users.ContainsUser(argument.RawValue(0)) Then
                          Throw New ArgumentException("That user does not exist")
                      End If
                      Dim target_user As BotUser = client.profile.users(argument.RawValue(0))

                      'Level
                      Dim lvl As UInteger
                      If Not UInteger.TryParse(argument.RawValue(2), lvl) Then
                          Throw New ArgumentException("Expected numeric argument for level")
                      End If

                      'Check for promotion in disguise
                      If lvl >= target_user.Permission(argument.RawValue(1)) Then
                          Throw New ArgumentException("That is not a demotion.")
                      End If

                      'Check for overdemotion
                      If user IsNot Nothing AndAlso Not target_user < user Then
                          Throw New ArgumentException("You can only demote users with lower permissions.")
                      End If

                      target_user.Permission(argument.RawValue(1)) = lvl
                      Return "{0} had permission in {1} demoted to {2}".Frmt(argument.RawValue(0), argument.RawValue(1), lvl).Futurized
                  End Function)

        Private Shared ReadOnly User As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="User",
            template:="?username",
            Description:="Shows a user's permissions.",
            Permissions:="",
            func:=Function(client, user, argument)
                      Dim username = If(argument.TryGetRawValue(0), If(user Is Nothing, Nothing, user.Name))
                      If username Is Nothing Then
                          Throw New ArgumentException("No user specified.")
                      End If
                      If client.profile.users.ContainsUser(username) Then
                          Return client.profile.users(username).ToString().Futurized
                      ElseIf client.profile.users.ContainsUser(BotUserSet.UnknownUserKey) Then
                          Return "{0} is an unknown user with the permissions of the user '*unknown'".Frmt(username).Futurized
                      Else
                          Return "{0} is an ignored unknown user.".Frmt(username).Futurized
                      End If
                  End Function)

        Private Shared ReadOnly LogOn As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="LogOn",
            template:="username password",
            Description:="Identifies and authenticates the client as a particular bnet user.",
            Permissions:="root=4",
            func:=Function(client, user, argument)
                      Return client.QueueLogOn(New Bnet.ClientCredentials(argument.RawValue(0), argument.RawValue(1))).
                                    EvalOnSuccess(Function() "Logged in as {0}".Frmt(argument.RawValue(0)))
                  End Function)

        Private Shared ReadOnly Host As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="Host",
            template:=Concat({"name=<game name>", "map=<search query>", "-private -p"},
                             WC3.ServerSettings.PartialArgumentTemplates,
                             WC3.GameStats.PartialArgumentTemplates).StringJoin(" "),
            Description:="Hosts a game in the custom games list. See 'Help Host *' for more help topics.",
            Permissions:="games=1",
            extraHelp:=Concat({"Private=-Private, -p: Creates a private game instead of a public game."},
                              WC3.ServerSettings.PartialArgumentHelp,
                              WC3.GameStats.PartialArgumentHelp).StringJoin(Environment.NewLine),
            func:=Function(client, user, argument)
                      Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))

                      If argument.TryGetOptionalNamedValue("Port") IsNot Nothing AndAlso user IsNot Nothing AndAlso user.Permission("root") < 5 Then
                          Throw New InvalidOperationException("You need root:5 to use -port.")
                      End If
                      Dim stats = New WC3.GameStats(map, If(user Is Nothing, My.Resources.ProgramName, user.Name), argument)
                      Dim desc = WC3.GameDescription.FromArguments(argument.NamedValue("name"),
                                                                 map,
                                                                 stats)
                      Dim f_settings = client.QueueGetListenPort.Select(Function(port) New WC3.ServerSettings(map, desc, argument, defaultListenPorts:={port}))
                      Dim f_server = f_settings.Select(Function(settings) client.Parent.QueueCreateServer(client.Name, settings, "[Linked]", True)).Defuturized()

                      'Create the server, then advertise the game
                      Return f_server.Select(
                          Function(server)
                              'Start advertising
                              client.QueueSetUserServer(User, server)
                              Return client.QueueStartAdvertisingGame(desc, argument.HasOptionalSwitch("Private") OrElse argument.HasOptionalSwitch("p"), server).EvalWhenReady(
                                  Function(advertiseException)
                                      If advertiseException IsNot Nothing Then
                                          server.QueueKill()
                                          Throw New OperationFailedException(innerException:=advertiseException)
                                      Else
                                          Return "Succesfully created game {0} for map {1}.".Frmt(desc.Name, desc.GameStats.relativePath)
                                      End If
                                  End Function
                              )
                                  End Function
                      ).Defuturized()
                          End Function)

        Private Shared ReadOnly Game As New DelegatedPartialCommand(Of Bnet.Client)(
            Name:="Game",
            headType:="InstanceName",
            Description:="Forwards commands to an instance in your hosted game. The default instance is '0'.",
            func:=Function(client, user, head, rest)
                      'Find hosted server, then find game, then pass command
                      Return client.QueueGetUserServer(user).Select(
                          Function(server)
                              If server Is Nothing Then
                                  Throw New InvalidOperationException("You don't have a hosted game to forward that command to.")
                              End If

                              'Find game, then pass command
                              Return server.QueueFindGame(head).Select(
                                  Function(game)
                                      If game Is Nothing Then
                                          Throw New InvalidOperationException("No game with that name.")
                                      End If

                                      'Pass command
                                      Return game.QueueCommandProcessText(client.Parent, Nothing, rest)
                                  End Function
                              ).Defuturized()
                                  End Function
                      ).Defuturized()
                          End Function)

        Private Shared ReadOnly RefreshGamesList As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="RefreshGamesList",
            template:="",
            Description:="Refreshes the bot's game list display. No useful effect from bnet.",
            Permissions:="local=1",
            func:=Function(client, user, argument)
                      Return client.QueueSendPacket(Bnet.Packet.MakeQueryGamesList()).EvalOnSuccess(Function() "Sent request.")
                  End Function)

        Private Shared ReadOnly CancelHost As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="CancelHost",
            template:="",
            Description:="Cancels a host command if it was issued by you, and unlinks the attached server.",
            Permissions:="",
            func:=Function(client, user, argument)
                      Return client.QueueGetUserServer(user).Select(
                          Function(server)
                              If server Is Nothing Then
                                  Throw New InvalidOperationException("You don't have a hosted game to cancel.")
                              End If

                              server.QueueKill()
                              Return "Cancelled hosting."
                          End Function
                      )
                          End Function)

        Private Shared ReadOnly AdminCode As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="AdminCode",
            template:="",
            Description:="Returns the admin code for a game you have hosted.",
            Permissions:="",
            func:=Function(client, user, argument) client.QueueGetUserServer(User).Select(
                      Function(server)
                          If server Is Nothing Then
                              Throw New InvalidOperationException("You don't have a hosted game to cancel.")
                          End If

                          Return server.settings.adminPassword
                      End Function))

        Private Shared ReadOnly Say As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="Say",
            template:="text",
            Description:="Causes the bot to say the given text, including any bnet commands.",
            Permissions:="root=1",
            func:=Function(client, user, argument)
                      client.QueueSendText(argument.RawValue(0))
                      Return "Said {0}".Frmt(argument.RawValue(0)).Futurized
                  End Function)

        Private Shared ReadOnly StartAdvertising As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="StartAdvertising",
            template:="name=<game name> map=<search query> -private -p",
            Description:="Places a game in the custom games list, but doesn't start a new game server to accept the joiners.",
            Permissions:="root=4;games=5",
            func:=Function(client, user, argument)
                      Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))
                      Return client.QueueStartAdvertisingGame(
                          WC3.GameDescription.FromArguments(argument.NamedValue("name"),
                                                          map,
                                                          New WC3.GameStats(map, If(user Is Nothing, My.Resources.ProgramName, user.Name), argument)),
                          [private]:=argument.HasOptionalSwitch("Private") OrElse argument.hasoptionalswitch("p"),
                          server:=Nothing).EvalOnSuccess(Function() "Started advertising")
                  End Function)

        Private Shared ReadOnly StopAdvertising As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="StopAdvertising",
            template:="",
            Description:="Stops placing a game in the custom games list and unlinks from any linked server.",
            Permissions:="root=4;games=5",
            func:=Function(client, user, argument)
                      Return client.QueueStopAdvertisingGame("Advertising stopped manually.").EvalOnSuccess(Function() "Stopped advertising.")
                  End Function)

        Private Shared ReadOnly Elevate As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="Elevate",
            template:="-player=name",
            Description:="Elevates you or a specified player to admin in your hosted game.",
            Permissions:="games=1",
            func:=Function(client, user, argument)
                      Dim username = If(argument.TryGetOptionalNamedValue("player"),
                                        If(user Is Nothing, Nothing, user.Name))
                      If username Is Nothing Then
                          Throw New ArgumentException("No player specified.")
                      End If

                      'Find hosted server, then find player's game, then elevate player
                      Return client.QueueGetUserServer(user).Select(
                          Function(server)
                              If server Is Nothing Then
                                  Throw New InvalidOperationException("You don't have a hosted game.")
                              End If

                              'Find player's game, then elevate player
                              Return server.QueueFindPlayerGame(username).Select(
                                  Function(game)
                                      If game Is Nothing Then
                                          Throw New InvalidOperationException("No matching user found.")
                                      End If

                                      'Elevate player
                                      Return game.QueueTryElevatePlayer(username)
                                  End Function
                              )
                                  End Function
                      ).Defuturized.Defuturized.EvalOnSuccess(Function() "'{0}' is now the admin.".Frmt(username))
                          End Function)

        Private Shared ReadOnly Connect As New DelegatedTemplatedCommand(Of Bnet.Client)(
            Name:="StopAdvertising",
            template:="Hostname",
            Description:="Connects to a battle.net server at the given hostname.",
            Permissions:="root=4",
            func:=Function(client, user, argument)
                      Return client.QueueConnect(argument.RawValue(0)).EvalOnSuccess(Function() "Established connection to {0}".Frmt(argument.RawValue(0)))
                  End Function)
    End Class
End Namespace
