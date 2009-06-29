Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Bnet.BnetClient
Imports HostBot.Warcraft3
Imports HostBot.Links

Namespace Commands.Specializations
    Public Class ClientCommands
        Inherits UICommandSet(Of IBnetClient)

        Private com_login As New ClientLoginCommands()
        Private com_online As New ClientOnlineCommands()
        Private com_offline As New ClientOfflineCommands()

        Private Function GetCurrentCommandSet(ByVal target As IBnetClient) As IFuture(Of BaseClientCommands)
            Return target.f_GetState.EvalWhenValueReady(Of BaseClientCommands)(
                Function(state)
                    Select Case state
                        Case States.Channel, States.CreatingGame, States.Game
                            Return com_online
                        Case States.Connecting, States.Disconnected
                            Return com_offline
                        Case States.Logon, States.EnterUsername
                            Return com_login
                        Case Else
                            Throw New UnreachableException()
                    End Select
                End Function
            )
        End Function
        Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
            Return GetCurrentCommandSet(target).EvalWhenValueReady(Function(x) x.Process(target, user, arguments)).Defuturize()
        End Function
        Public Overrides Sub ProcessLocalText(ByVal target As Bnet.IBnetClient, ByVal text As String, ByVal logger As Logger)
            GetCurrentCommandSet(target).CallWhenValueReady(Sub(x) x.ProcessLocalText(target, text, logger))
        End Sub
    End Class

    Public MustInherit Class BaseClientCommands
        Inherits UICommandSet(Of IBnetClient)

        Public Sub New()
            AddCommand(New com_AdLink)
            AddCommand(New com_AdUnlink)
            AddCommand(New com_Bot)
            AddCommand(New com_AddUser)
            AddCommand(New com_Demote)
            AddCommand(New com_RemoveUser)
            AddCommand(New com_Disconnect)
            AddCommand(New com_ParentCommand(New BotCommands.com_FindMaps))
            AddCommand(New com_GetPort)
            AddCommand(New com_SetPort)
            AddCommand(New com_Promote)
            AddCommand(New com_User)
        End Sub

        Public Class com_AdLink
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AdLink,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Client_AdLink_Help,
                           My.Resources.Command_Client_AdLink_Access,
                           My.Resources.Command_Client_AdLink_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim client = target
                Dim other_client = target.Parent.f_FindClient(arguments(0))
                Return other_client.EvalWhenValueReady(
                    Function(client2)
                        If client2 Is Nothing Then
                            Return failure("No client matching that name.")
                        ElseIf client2 Is client Then
                            Return failure("Can't link to self.")
                        End If

                        AdvertisingLink.CreateMultiWayLink({client, client2})
                        Return success("Created an advertising link between client {0} and client {1}.".frmt(client.Name, client2.Name))
                    End Function
                )
            End Function
        End Class
        Public Class com_AdUnlink
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AdUnlink,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Client_AdUnlink_Help,
                           My.Resources.Command_Client_AdUnlink_Access,
                           My.Resources.Command_Client_AdUnlink_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal client As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return client.Parent.f_FindClient(arguments(0)).EvalWhenValueReady(
                    Function(client2)
                                                                                       If client2 Is Nothing Then
                                                                                           Return failure("No client matching that name.")
                                                                                       ElseIf client2 Is client Then
                                                                                           Return failure("Can't link to self.")
                                                                                       End If

                                                                                       client.ClearAdvertisingPartner(client2)
                                                                                       Return success("Any link between client {0} and client {1} has been removed.".frmt(client.Name, client2.Name))
                                                                                   End Function
                )
            End Function
        End Class

        '''<summary>A command which forwards sub-commands to the main bot command set</summary>
        Public Class com_Bot
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New("Bot",
                           0, ArgumentLimits.free,
                           "[--bot command, --bot CreateUser Strilanc, --bot help] Forwards text commands to the main bot.",
                           "root=1", "")
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.Parent.BotCommands.ProcessText(target.Parent, user, mendQuotedWords(arguments))
            End Function
        End Class

        Public Class com_ParentCommand
            Inherits BaseCommand(Of IBnetClient)
            Private parent_command As BaseCommand(Of MainBot)
            Public Sub New(ByVal parent_command As BaseCommand(Of MainBot))
                MyBase.New(parent_command.name, parent_command.argumentLimit, parent_command.argumentLimitType, parent_command.help, parent_command.requiredPermissions)
                Me.parent_command = parent_command
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return parent_command.ProcessText(target.Parent, user, mendQuotedWords(arguments))
            End Function
        End Class

        '''<summary>A command which returns the port the client is set to tell bnet it is listening on.</summary>
        Private Class com_GetPort
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_GetPort,
                           0, ArgumentLimits.exact,
                           My.Resources.Command_Client_GetPort_Help,
                           My.Resources.Command_Client_GetPort_Access,
                           My.Resources.Command_Client_GetPort_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return success(target.f_listenPort.ToString()).Futurize
            End Function
        End Class

        '''<summary>A command which changes the port the client is set to tell bnet it is listening on.</summary>
        Private Class com_SetPort
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_SetPort,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Client_SetPort_Help,
                           My.Resources.Command_Client_SetPort_Access,
                           My.Resources.Command_Client_SetPort_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim port As UShort
                If Not UShort.TryParse(arguments(0), port) Then Return failure("Invalid port").Futurize
                Return target.f_SetListenPort(port)
            End Function
        End Class

        Private Class com_Disconnect
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New("disconnect",
                            0, ArgumentLimits.exact,
                            "[--disconnect]",
                            DictStrUInt("root=4"))
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                target.f_Disconnect("Client Command")
                Return success("Disconnected").Futurize
            End Function
        End Class

        Public Class com_AddUser
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AddUser,
                            1, ArgumentLimits.exact,
                            My.Resources.Command_Client_AddUser_Help,
                            My.Resources.Command_Client_AddUser_Access,
                            My.Resources.Command_Client_AddUser_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Try
                    Dim new_user As BotUser = target.profile.users.create_new_user(arguments(0))
                    Return success("Created " + new_user.name).Futurize
                Catch e As InvalidOperationException
                    Return failure(e.Message).Futurize
                End Try
            End Function
        End Class

        Public Class com_RemoveUser
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_RemoveUser,
                            1, ArgumentLimits.exact,
                            My.Resources.Command_Client_RemoveUser_Help,
                            My.Resources.Command_Client_RemoveUser_Access,
                            My.Resources.Command_Client_RemoveUser_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If Not target.profile.users.ContainsUser(arguments(0)) Then
                    Return failure("That user does not exist").Futurize
                End If
                Dim target_user As BotUser = target.profile.users(arguments(0))
                If user IsNot Nothing AndAlso Not target_user < user Then
                    Return failure("You can only destroy users with lower permissions.").Futurize
                End If
                target.profile.users.RemoveUser(arguments(0))
                Return success("Removed " + arguments(0)).Futurize
            End Function
        End Class

        Public Class com_Promote
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New("Promote",
                            2, ArgumentLimits.min,
                            "[--Promote username permission level] Increases a permissions level for a user.",
                            DictStrUInt("users=1"))
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'get target user
                If Not target.profile.users.ContainsUser(arguments(0)) Then
                    Return failure("That user does not exist").Futurize
                End If
                Dim target_user As BotUser = target.profile.users(arguments(0))

                'get level
                Dim lvl As UInteger
                If arguments.Count < 3 Then
                    lvl = 1
                ElseIf Not UInteger.TryParse(arguments(2), lvl) Then
                    Return failure("Expected numeric argument for level").Futurize
                End If

                'check for demotion in disguise
                If lvl <= target_user.permission(arguments(1)) Then
                    Return failure("That is not a promotion. Jerk.").Futurize
                End If

                'check for overpromotion
                If user IsNot Nothing AndAlso lvl > user.permission(arguments(1)) Then
                    Return failure("You can't promote users past your own permission levels.").Futurize
                End If

                target_user.permission(arguments(1)) = lvl
                Return success(arguments(0) + " had permission in " + arguments(1) + " promoted to " + lvl.ToString()).Futurize
            End Function
        End Class

        Public Class com_Demote
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New("Demote",
                            2, ArgumentLimits.min,
                            "[--Demote username permission level] Decreases a permissions level for a user.",
                            DictStrUInt("users=3"))
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'get target user
                If Not target.profile.users.ContainsUser(arguments(0)) Then
                    Return failure("That user does not exist").Futurize
                End If
                Dim target_user As BotUser = target.profile.users(arguments(0))

                'get level
                Dim lvl As UInteger
                If arguments.Count < 3 Then
                    lvl = 0
                ElseIf Not UInteger.TryParse(arguments(2), lvl) Then
                    Return failure("Expected numeric argument for level").Futurize
                End If

                'check for promotion in disguise
                If lvl >= target_user.permission(arguments(1)) Then
                    Return failure("That is not a demotion.").Futurize
                End If

                'check for overdemotion
                If user IsNot Nothing AndAlso Not target_user < user Then
                    Return failure("You can only demote users with lower permissions.").Futurize
                End If

                target_user.permission(arguments(1)) = lvl
                Return success(arguments(0) + " had permission in " + arguments(1) + " demoted to " + lvl.ToString()).Futurize
            End Function
        End Class

        Public Class com_User
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_User,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Client_User_Help,
                           My.Resources.Command_Client_User_Access,
                           My.Resources.Command_Client_User_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 And user Is Nothing Then Return failure("No user specified.").Futurize
                Dim username = If(arguments.Count = 0, user.name, arguments(0))
                If target.profile.users.ContainsUser(username) Then
                    Return success(target.profile.users(username).ToString()).Futurize
                ElseIf target.profile.users.ContainsUser(BotUserSet.NAME_UNKNOWN_USER) Then
                    Return success("{0} is an unknown user with the permissions of the user '*unknown'".frmt(username)).Futurize
                Else
                    Return success("{0} is an ignored unknown user.".frmt(username)).Futurize
                End If
            End Function
        End Class
    End Class

    Public Class ClientLoginCommands
        Inherits BaseClientCommands

        Public Sub New()
            MyBase.New()
            AddCommand(New com_Login)
        End Sub

        Private Class com_Login
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Login,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Client_Login_Help,
                           My.Resources.Command_Client_Login_Access,
                           My.Resources.Command_Client_Login_ExtraHelp,
                           True)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_Login(arguments(0), arguments(1))
            End Function
        End Class
    End Class

    Public Class ClientOnlineCommands
        Inherits BaseClientCommands

        Public Sub New()
            AddCommand(New com_AdminCode)
            AddCommand(New com_CancelHost)
            AddCommand(New com_Elevate)
            AddCommand(New com_Game)
            AddCommand(New com_Host)
            AddCommand(New com_Say)
            AddCommand(New com_StartAdvertising)
            AddCommand(New com_StopAdvertising)
        End Sub

        Public Class com_Host
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Host,
                           2, ArgumentLimits.min,
                           My.Resources.Command_Client_Host_Help,
                           My.Resources.Command_Client_Host_Access,
                           My.Resources.Command_Client_Host_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'Map
                Dim map_out = W3Map.FromArgument(arguments(1))
                If Not map_out.succeeded Then Return map_out.Outcome.Futurize
                Dim map = map_out.val

                'Server settings
                arguments = arguments.ToList
                For i = 0 To arguments.Count - 1
                    Select Case arguments(i).ToLower()
                        Case "-reserve", "-r"
                            If user IsNot Nothing Then
                                arguments(i) += "=" + user.name
                            Else
                                arguments(i) = ""
                            End If
                    End Select
                    If arguments(i).ToLower Like "-port=*" AndAlso user IsNot Nothing AndAlso user.permission("root") < 5 Then
                        Return failure("You need root=5 to use -port.").Futurize
                    End If
                Next i
                Dim header = New W3GameHeader(arguments(0),
                                              If(user Is Nothing, My.Resources.ProgramName, user.name),
                                              New W3MapSettings(arguments, map),
                                              0, 0, 0, arguments, map.numPlayerSlots)
                Dim f_settings = target.f_listenPort.EvalWhenValueReady(Function(port) New ServerSettings(map, header, default_listen_ports:={port}))
                Dim f_server = f_settings.EvalWhenValueReady(Function(settings) target.Parent.f_CreateServer(target.Name, settings, "[Linked]", True)).Defuturize()

                'Create the server, then advertise the game
                Dim client = target
                Return f_server.EvalWhenValueReady(
                    Function(created_server)
                        If Not created_server.succeeded Then
                            Return created_server.Outcome.Futurize
                        End If

                        'Start advertising
                        Dim server = created_server.val
                        client.f_SetUserServer(user, server)
                        Return client.f_StartAdvertisingGame(header, server).EvalWhenValueReady(
                            Function(advertised)
                                If advertised.succeeded Then
                                    Return success("Game hosted succesfully. Admin code is '{0}'.".frmt(server.settings.adminPassword))
                                Else
                                    server.f_Kill()
                                    Return advertised
                                End If
                            End Function
                        )
                    End Function
                ).Defuturize()
            End Function
        End Class

        Public Class com_Game
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New("Game",
                        1, ArgumentLimits.min,
                        "[Game name command..., Instance 0 boot red] Forwards commands to a game of your hosted server.")
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'Find hosted server, then find game, then pass command
                Return target.f_GetUserServer(user).EvalWhenValueReady(
                    Function(server)
                        If server Is Nothing Then
                            Return failure("You don't have a hosted game to forward that command to.").Futurize
                        End If

                        'Find game, then pass command
                        Return server.f_FindGame(arguments(0)).EvalWhenValueReady(
                            Function(game)
                                If game Is Nothing Then
                                    Return failure("No game with that name.").Futurize
                                End If

                                'Pass command
                                Return game.f_CommandProcessText(Nothing, mendQuotedWords(arguments.Skip(1).ToList()))
                            End Function
                        ).Defuturize()
                    End Function
                ).Defuturize()
            End Function
        End Class

        Public Class com_CancelHost
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New("CancelHost",
                           0, ArgumentLimits.exact,
                           "[--CancelHost] Cancels the last hosting command.")
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_GetUserServer(user).EvalWhenValueReady(
                    Function(server)
                        If server Is Nothing Then
                            Return failure("You don't have a hosted game to cancel.")
                        End If

                        server.f_Kill()
                        Return success("Cancelled hosting.")
                    End Function
                )
            End Function
        End Class

        Public Class com_AdminCode
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New("AdminCode",
                           0, ArgumentLimits.exact,
                           "[--AdminCode] Repeats the admin code for a game you have hosted.")
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_GetUserServer(user).EvalWhenValueReady(
                    Function(server)
                        If server Is Nothing Then
                            Return failure("You don't have a hosted game to cancel.")
                        End If

                        Return success(server.settings.adminPassword)
                    End Function
                )
            End Function
        End Class

        Public Class com_Say
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Say,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Client_Say_Help,
                           My.Resources.Command_Client_Say_Access,
                           My.Resources.Command_Client_Say_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                target.f_SendText(arguments(0))
                Return success("Said {0}".frmt(arguments(0))).Futurize
            End Function
        End Class

        '''<summary>A command which tells the client to start advertising a game.</summary>
        Public Class com_StartAdvertising
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_StartAdvertising,
                           2, ArgumentLimits.min,
                           My.Resources.Command_Client_StartAdvertising_Help,
                           My.Resources.Command_Client_StartAdvertising_Access,
                           My.Resources.Command_Client_StartAdvertising_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'Map
                Dim map_out = W3Map.FromArgument(arguments(1))
                If Not map_out.succeeded Then Return map_out.Outcome.Futurize
                Dim map = map_out.val

                'create
                Return target.f_StartAdvertisingGame(New W3GameHeader(arguments(0),
                                                                      My.Resources.ProgramName,
                                                                      New W3MapSettings(arguments, map),
                                                                      0, 0, 0, arguments, map.numPlayerSlots), Nothing)
            End Function
        End Class

        '''<summary>A command which tells the client to stop advertising a game.</summary>
        Public Class com_StopAdvertising
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_StopAdvertising,
                           0, ArgumentLimits.exact,
                           My.Resources.Command_Client_StopAdvertising_Help,
                           My.Resources.Command_Client_StopAdvertising_Access,
                           My.Resources.Command_Client_StopAdvertising_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_StopAdvertisingGame("Advertising stopped manually.")
            End Function
        End Class

        Public Class com_Elevate
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Elevate,
                        1, ArgumentLimits.max,
                        My.Resources.Command_Client_Elevate_Help,
                        My.Resources.Command_Client_Elevate_Access,
                        My.Resources.Command_Client_Elevate_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 AndAlso user Is Nothing Then
                    Return failure("You didn't specify a player to elevate.").Futurize
                End If
                Dim username = If(arguments.Count = 0, user.name, arguments(0))

                'Find hosted server, then find player's game, then elevate player
                Return target.f_GetUserServer(user).EvalWhenValueReady(
                    Function(server)
                        If server Is Nothing Then
                            Return failure("You don't have a hosted game.").Futurize
                        End If

                        'Find player's game, then elevate player
                        Return server.f_FindPlayerGame(username).EvalWhenValueReady(
                            Function(game)
                                If game.val Is Nothing Then
                                    Return failure("No matching user found.").Futurize
                                End If

                                'Elevate player
                                Return game.val.f_TryElevatePlayer(username)
                            End Function
                        ).Defuturize()
                    End Function
                ).Defuturize()
            End Function
        End Class
    End Class

    Public Class ClientOfflineCommands
        Inherits BaseClientCommands

        Public Sub New()
            AddCommand(New com_Connect)
        End Sub

        Private Class com_Connect
            Inherits BaseCommand(Of IBnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Connect,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Client_Connect_Help,
                           My.Resources.Command_Client_Connect_Access,
                           My.Resources.Command_Client_Connect_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As IBnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_Connect(arguments(0))
            End Function
        End Class
    End Class
End Namespace
