Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Bnet.BnetClient
Imports HostBot.Warcraft3
Imports HostBot.Links

Namespace Commands.Specializations
    Public NotInheritable Class ClientCommands
        Inherits CommandSet(Of BnetClient)

        Private com_login As New ClientLogOnCommands()
        Private com_online As New ClientOnlineCommands()
        Private com_offline As New ClientOfflineCommands()

        Private Function GetCurrentCommandSet(ByVal target As BnetClient) As IFuture(Of BaseClientCommands)
            Return target.QueueGetState.Select(Of BaseClientCommands)(
                Function(state)
                    Select Case state
                        Case BnetClientState.Channel, BnetClientState.CreatingGame, BnetClientState.Game
                            Return com_online
                        Case BnetClientState.Connecting, BnetClientState.Disconnected
                            Return com_offline
                        Case BnetClientState.LogOn, BnetClientState.EnterUserName
                            Return com_login
                        Case Else
                            Throw New UnreachableException()
                    End Select
                End Function
            )
        End Function
        Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Return GetCurrentCommandSet(target).Select(Function(x) x.Process(target, user, arguments)).Defuturized()
        End Function
        Public Sub ProcessLocalText(ByVal target As Bnet.BnetClient, ByVal text As String, ByVal logger As Logger)
            GetCurrentCommandSet(target).CallOnValueSuccess(Sub(x) x.ProcessLocalText(target, text, logger))
        End Sub
    End Class

    Public MustInherit Class BaseClientCommands
        Inherits CommandSet(Of BnetClient)

        Protected Sub New()
            AddCommand(New CommandAdLink)
            AddCommand(New CommandAdUnlink)
            AddCommand(New CommandBot)
            AddCommand(New CommandAddUser)
            AddCommand(New CommandDemote)
            AddCommand(New CommandRemoveUser)
            AddCommand(New CommandDisconnect)
            AddCommand(New CommandParentCommand(New BotCommands.CommandFindMaps))
            AddCommand(New CommandGetPort)
            AddCommand(New CommandSetPort)
            AddCommand(New CommandPromote)
            AddCommand(New CommandUser)
        End Sub

        Public NotInheritable Class CommandAdLink
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AdLink,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Client_AdLink_Help,
                           My.Resources.Command_Client_AdLink_Access,
                           My.Resources.Command_Client_AdLink_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim client = target
                Dim other_client = target.parent.QueueFindClient(arguments(0))
                Return other_client.Select(
                    Function(client2)
                        If client2 Is Nothing Then
                            Throw New InvalidOperationException("No client matching that name.")
                        ElseIf client2 Is client Then
                            Throw New InvalidOperationException("Can't link to self.")
                        End If

                        AdvertisingLink.CreateMultiWayLink({client, client2})
                        Return "Created an advertising link between client {0} and client {1}.".Frmt(client.name, client2.name)
                    End Function
                )
            End Function
        End Class
        Public NotInheritable Class CommandAdUnlink
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AdUnlink,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Client_AdUnlink_Help,
                           My.Resources.Command_Client_AdUnlink_Access,
                           My.Resources.Command_Client_AdUnlink_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.parent.QueueFindClient(arguments(0)).Select(
                    Function(client2)
                        If client2 Is Nothing Then
                            Throw New ArgumentException("No client matching that name.")
                        ElseIf client2 Is target Then
                            Throw New ArgumentException("Can't link to self.")
                        End If

                        target.QueueRemoveAdvertisingPartner(client2)
                        Return "Any link between client {0} and client {1} has been removed.".Frmt(target.name, client2.name)
                    End Function
                )
            End Function
        End Class

        '''<summary>A command which forwards sub-commands to the main bot command set</summary>
        Public NotInheritable Class CommandBot
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Bot",
                           0, ArgumentLimitType.Free,
                           "[--bot command, --bot CreateUser Strilanc, --bot help] Forwards text commands to the main bot.",
                           "root=1", "")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.parent.BotCommands.ProcessCommand(target.parent, user, arguments)
            End Function
        End Class

        Public NotInheritable Class CommandParentCommand
            Inherits BaseCommand(Of BnetClient)
            Private subCommand As BaseCommand(Of MainBot)
            Public Sub New(ByVal subCommand As BaseCommand(Of MainBot))
                MyBase.New(subCommand.Name, subCommand.ArgumentLimit, subCommand.ArgumentLimitType, subCommand.Help, subCommand.requiredPermissions)
                Me.subCommand = subCommand
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return subCommand.ProcessCommand(target.parent, user, arguments)
            End Function
        End Class

        '''<summary>A command which returns the port the client is set to tell bnet it is listening on.</summary>
        Private NotInheritable Class CommandGetPort
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_GetPort,
                           0, ArgumentLimitType.Exact,
                           My.Resources.Command_Client_GetPort_Help,
                           My.Resources.Command_Client_GetPort_Access,
                           My.Resources.Command_Client_GetPort_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueGetListenPort.Select(Function(port) port.ToString(CultureInfo.InvariantCulture))
            End Function
        End Class

        '''<summary>A command which changes the port the client is set to tell bnet it is listening on.</summary>
        Private NotInheritable Class CommandSetPort
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_SetPort,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Client_SetPort_Help,
                           My.Resources.Command_Client_SetPort_Access,
                           My.Resources.Command_Client_SetPort_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim port As UShort
                If Not UShort.TryParse(arguments(0), port) Then Throw New ArgumentException("Invalid port")
                Return target.QueueSetListenPort(port).EvalOnSuccess(Function() "Set listen port to {0}.".Frmt(port))
            End Function
        End Class

        Private NotInheritable Class CommandDisconnect
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("disconnect",
                           0, ArgumentLimitType.Exact,
                           "[--disconnect]",
                           "root=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueDisconnect(expected:=True, reason:="Client Command").EvalOnSuccess(Function() "Disconnected")
            End Function
        End Class

        Public NotInheritable Class CommandAddUser
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AddUser,
                            1, ArgumentLimitType.Exact,
                            My.Resources.Command_Client_AddUser_Help,
                            My.Resources.Command_Client_AddUser_Access,
                            My.Resources.Command_Client_AddUser_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim newUser As BotUser = target.profile.users.CreateNewUser(arguments(0))
                Return "Created {0}".Frmt(newUser.name).Futurized
            End Function
        End Class

        Public NotInheritable Class CommandRemoveUser
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_RemoveUser,
                            1, ArgumentLimitType.Exact,
                            My.Resources.Command_Client_RemoveUser_Help,
                            My.Resources.Command_Client_RemoveUser_Access,
                            My.Resources.Command_Client_RemoveUser_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If Not target.profile.users.ContainsUser(arguments(0)) Then
                    Throw New ArgumentException("That user does not exist")
                End If
                Dim target_user As BotUser = target.profile.users(arguments(0))
                If user IsNot Nothing AndAlso Not target_user < user Then
                    Throw New ArgumentException("You can only destroy users with lower permissions.")
                End If
                target.profile.users.RemoveUser(arguments(0))
                Return "Removed {0}".Frmt(arguments(0)).Futurized
            End Function
        End Class

        Public NotInheritable Class CommandPromote
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Promote",
                            2, ArgumentLimitType.Min,
                            "[--Promote username permission level] Increases a permissions level for a user.",
                            "users=1", "")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                'get target user
                If Not target.profile.users.ContainsUser(arguments(0)) Then
                    Throw New ArgumentException("That user does not exist")
                End If
                Dim target_user As BotUser = target.profile.users(arguments(0))

                'get level
                Dim lvl As UInteger
                If arguments.Count < 3 Then
                    lvl = 1
                ElseIf Not UInteger.TryParse(arguments(2), lvl) Then
                    Throw New ArgumentException("Expected numeric argument for level")
                End If

                'check for demotion in disguise
                If lvl <= target_user.Permission(arguments(1)) Then
                    Throw New ArgumentException("That is not a promotion. Jerk.")
                End If

                'check for overpromotion
                If user IsNot Nothing AndAlso lvl > user.Permission(arguments(1)) Then
                    Throw New ArgumentException("You can't promote users past your own permission levels.")
                End If

                target_user.Permission(arguments(1)) = lvl
                Return "{0} had permission in {1} promoted to {2}".Frmt(arguments(0), arguments(1), lvl).Futurized
            End Function
        End Class

        Public NotInheritable Class CommandDemote
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Demote",
                            2, ArgumentLimitType.Min,
                            "[--Demote username permission level] Decreases a permissions level for a user.",
                            "users=3", "")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                'get target user
                If Not target.profile.users.ContainsUser(arguments(0)) Then
                    Throw New ArgumentException("That user does not exist")
                End If
                Dim target_user As BotUser = target.profile.users(arguments(0))

                'get level
                Dim lvl As UInteger
                If arguments.Count < 3 Then
                    lvl = 0
                ElseIf Not UInteger.TryParse(arguments(2), lvl) Then
                    Throw New ArgumentException("Expected numeric argument for level")
                End If

                'check for promotion in disguise
                If lvl >= target_user.Permission(arguments(1)) Then
                    Throw New ArgumentException("That is not a demotion.")
                End If

                'check for overdemotion
                If user IsNot Nothing AndAlso Not target_user < user Then
                    Throw New ArgumentException("You can only demote users with lower permissions.")
                End If

                target_user.Permission(arguments(1)) = lvl
                Return "{0} had permission in {1} demoted to {2}".Frmt(arguments(0), arguments(1), lvl).Futurized
            End Function
        End Class

        Public NotInheritable Class CommandUser
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_User,
                           1, ArgumentLimitType.Max,
                           My.Resources.Command_Client_User_Help,
                           My.Resources.Command_Client_User_Access,
                           My.Resources.Command_Client_User_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 And user Is Nothing Then Throw New ArgumentException("No user specified.")
                Dim username = If(arguments.Count = 0, user.name, arguments(0))
                If target.profile.users.ContainsUser(username) Then
                    Return target.profile.users(username).ToString().Futurized
                ElseIf target.profile.users.ContainsUser(BotUserSet.UnknownUserKey) Then
                    Return "{0} is an unknown user with the permissions of the user '*unknown'".Frmt(username).Futurized
                Else
                    Return "{0} is an ignored unknown user.".Frmt(username).Futurized
                End If
            End Function
        End Class
    End Class

    Public NotInheritable Class ClientLogOnCommands
        Inherits BaseClientCommands

        Public Sub New()
            MyBase.New()
            AddCommand(New CommandLogOn)
        End Sub

        Private NotInheritable Class CommandLogOn
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_LogOn,
                           2, ArgumentLimitType.Exact,
                           My.Resources.Command_Client_LogOn_Help,
                           My.Resources.Command_Client_LogOn_Access,
                           My.Resources.Command_Client_LogOn_ExtraHelp,
                           ShouldHideArguments:=True)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueLogOn(arguments(0), arguments(1)).EvalOnSuccess(Function() "Logged in as {0}".Frmt(arguments(0)))
            End Function
        End Class
    End Class

    Public NotInheritable Class ClientOnlineCommands
        Inherits BaseClientCommands

        Public Sub New()
            AddCommand(New CommandAdminCode)
            AddCommand(New CommandCancelHost)
            AddCommand(New CommandElevate)
            AddCommand(New CommandGame)
            AddCommand(New CommandHost)
            AddCommand(New CommandSay)
            AddCommand(New CommandStartAdvertising)
            AddCommand(New CommandStopAdvertising)
            AddCommand(New CommandRefreshGamesList)
        End Sub

        Public NotInheritable Class CommandHost
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Host,
                           2, ArgumentLimitType.Min,
                           My.Resources.Command_Client_Host_Help,
                           My.Resources.Command_Client_Host_Access,
                           My.Resources.Command_Client_Host_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim map = W3Map.FromArgument(arguments(1))

                'Server settings
                arguments = arguments.ToList
                For i = 0 To arguments.Count - 1
                    Select Case arguments(i).ToUpperInvariant
                        Case "-RESERVE", "-R"
                            If user IsNot Nothing Then
                                arguments(i) += "=" + user.name
                            Else
                                arguments(i) = ""
                            End If
                    End Select
                    If arguments(i).ToUpperInvariant Like "-PORT=*" AndAlso user IsNot Nothing AndAlso user.Permission("root") < 5 Then
                        Throw New InvalidOperationException("You need root=5 to use -port.")
                    End If
                Next i
                Dim header = New W3GameHeader(arguments(0),
                                              If(user Is Nothing, My.Resources.ProgramName, user.name),
                                              New W3MapSettings(arguments, map),
                                              0, 0, 0, arguments, map.NumPlayerSlots)
                Dim f_settings = target.QueueGetListenPort.Select(Function(port) New ServerSettings(map, header, defaultListenPorts:={port}))
                Dim f_server = f_settings.Select(Function(settings) target.parent.QueueCreateServer(target.name, settings, "[Linked]", True)).Defuturized()

                'Create the server, then advertise the game
                Dim client = target
                Return f_server.Select(
                    Function(server)
                        'Start advertising
                        client.QueueSetUserServer(user, server)
                        Return client.QueueStartAdvertisingGame(header, server).EvalWhenReady(
                            Function(advertiseException)
                                If advertiseException IsNot Nothing Then
                                    server.QueueKill()
                                    Throw New OperationFailedException(innerException:=advertiseException)
                                Else
                                    Return "Succesfully created game {0} for map {1}.".Frmt(header.Name, header.Map.relativePath)
                                End If
                            End Function
                        )
                    End Function
                ).Defuturized()
            End Function
        End Class

        Public NotInheritable Class CommandRefreshGamesList
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("RefreshGamesList",
                           0, ArgumentLimitType.Exact,
                           "[RefreshGamesList] Refreshes the bot's game list display. No useful effect from bnet.")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueSendPacket(BnetPacket.MakeQueryGamesList()).EvalOnSuccess(Function() "Sent request.")
            End Function
        End Class
        Public NotInheritable Class CommandGame
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Game",
                        1, ArgumentLimitType.Min,
                        "[Game name command..., Game boot red] Forwards commands to a game of your hosted server.")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                'Find hosted server, then find game, then pass command
                Return target.QueueGetUserServer(user).Select(
                    Function(server)
                        If server Is Nothing Then
                            Throw New InvalidOperationException("You don't have a hosted game to forward that command to.")
                        End If

                        'Find game, then pass command
                        Return server.QueueFindGame(arguments(0)).Select(
                            Function(game)
                                If game Is Nothing Then
                                    Throw New InvalidOperationException("No game with that name.")
                                End If

                                'Pass command
                                Return game.QueueCommandProcessText(target.parent, Nothing, arguments.SubToArray(1))
                            End Function
                        ).Defuturized()
                    End Function
                ).Defuturized()
            End Function
        End Class

        Public NotInheritable Class CommandCancelHost
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("CancelHost",
                           0, ArgumentLimitType.Exact,
                           "[--CancelHost] Cancels the last hosting command.")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueGetUserServer(user).Select(
                    Function(server)
                        If server Is Nothing Then
                            Throw New InvalidOperationException("You don't have a hosted game to cancel.")
                        End If

                        server.QueueKill()
                        Return "Cancelled hosting."
                    End Function
                )
            End Function
        End Class

        Public NotInheritable Class CommandAdminCode
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("AdminCode",
                           0, ArgumentLimitType.Exact,
                           "[--AdminCode] Repeats the admin code for a game you have hosted.")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueGetUserServer(user).Select(
                    Function(server)
                        If server Is Nothing Then
                            Throw New InvalidOperationException("You don't have a hosted game to cancel.")
                        End If

                        Return server.settings.adminPassword
                    End Function
                )
            End Function
        End Class

        Public NotInheritable Class CommandSay
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Say,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Client_Say_Help,
                           My.Resources.Command_Client_Say_Access,
                           My.Resources.Command_Client_Say_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                target.QueueSendText(arguments(0))
                Return "Said {0}".Frmt(arguments(0)).Futurized
            End Function
        End Class

        '''<summary>A command which tells the client to start advertising a game.</summary>
        Public NotInheritable Class CommandStartAdvertising
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_StartAdvertising,
                           2, ArgumentLimitType.Min,
                           My.Resources.Command_Client_StartAdvertising_Help,
                           My.Resources.Command_Client_StartAdvertising_Access,
                           My.Resources.Command_Client_StartAdvertising_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim map = W3Map.FromArgument(arguments(1))

                'create
                Return target.QueueStartAdvertisingGame(
                    New W3GameHeader(arguments(0),
                                     My.Resources.ProgramName,
                                     New W3MapSettings(arguments, map),
                                     0, 0, 0, arguments, map.NumPlayerSlots), Nothing).EvalOnSuccess(Function() "Started advertising")
            End Function
        End Class

        '''<summary>A command which tells the client to stop advertising a game.</summary>
        Public NotInheritable Class CommandStopAdvertising
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_StopAdvertising,
                           0, ArgumentLimitType.Exact,
                           My.Resources.Command_Client_StopAdvertising_Help,
                           My.Resources.Command_Client_StopAdvertising_Access,
                           My.Resources.Command_Client_StopAdvertising_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueStopAdvertisingGame("Advertising stopped manually.").EvalOnSuccess(Function() "Stopped advertising.")
            End Function
        End Class

        Public NotInheritable Class CommandElevate
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Elevate,
                        1, ArgumentLimitType.Max,
                        My.Resources.Command_Client_Elevate_Help,
                        My.Resources.Command_Client_Elevate_Access,
                        My.Resources.Command_Client_Elevate_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 AndAlso user Is Nothing Then
                    Throw New ArgumentException("You didn't specify a player to elevate.")
                End If
                Dim username = If(arguments.Count = 0, user.name, arguments(0))

                'Find hosted server, then find player's game, then elevate player
                Return target.QueueGetUserServer(user).Select(
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
            End Function
        End Class
    End Class

    Public NotInheritable Class ClientOfflineCommands
        Inherits BaseClientCommands

        Public Sub New()
            AddCommand(New CommandConnect)
        End Sub

        Private NotInheritable Class CommandConnect
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Connect,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Client_Connect_Help,
                           My.Resources.Command_Client_Connect_Access,
                           My.Resources.Command_Client_Connect_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueConnect(arguments(0)).EvalOnSuccess(Function() "Established connection to {0}".Frmt(arguments(0)))
            End Function
        End Class
    End Class
End Namespace
