Imports HostBot.Commands
Imports HostBot.BNET
Imports HostBot.BNET.BnetClient
Imports HostBot.Warcraft3
Imports HostBot.Links

Namespace Commands.Specializations
    Public Class ClientCommands
        Inherits UICommandSet(Of BnetClient)

        Private com_login As New ClientLoginCommands()
        Private com_online As New ClientOnlineCommands()
        Private com_offline As New ClientOfflineCommands()

        Private Function get_current_command_set(ByVal target As BnetClient) As BaseClientCommands
            Select Case target.state_P
                Case States.channel, States.creating_game, States.game
                    Return com_online
                Case States.connecting, States.disconnected
                    Return com_offline
                Case States.logon, States.enter_username
                    Return com_login
                Case Else
                    Throw New UnreachableStateException("Unrecognized bnet client state.")
            End Select
        End Function
        Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
            Return get_current_command_set(target).process(target, user, arguments)
        End Function
        Public Overrides Sub processLocalText(ByVal target As BNET.BnetClient, ByVal text As String, ByVal logger As MultiLogger)
            get_current_command_set(target).processLocalText(target, text, logger)
        End Sub
    End Class

    Public MustInherit Class BaseClientCommands
        Inherits UICommandSet(Of BnetClient)

        Public Sub New()
            add_subcommand(New com_AdLink)
            add_subcommand(New com_AdUnlink)
            add_subcommand(New com_Bot)
            add_subcommand(New com_AddUser)
            add_subcommand(New com_Demote)
            add_subcommand(New com_RemoveUser)
            add_subcommand(New com_Disconnect)
            add_subcommand(New com_ParentCommand(New BotCommands.com_FindMaps))
            add_subcommand(New com_GetPort)
            add_subcommand(New com_SetPort)
            add_subcommand(New com_Promote)
            add_subcommand(New com_User)
        End Sub

        Public Class com_AdLink
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AdLink, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Client_AdLink_Help, _
                           My.Resources.Command_Client_AdLink_Access, _
                           My.Resources.Command_Client_AdLink_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim other_client = target.parent.find_client_R(arguments(0))
                Dim f = New Future(Of Outcome)
                FutureSub.frun( _
                            AddressOf process_2, _
                            futurize(f), _
                            futurize(target), _
                            other_client)
                Return f
            End Function
            Private Sub process_2(ByVal f As Future(Of Outcome), ByVal client As BnetClient, ByVal client2 As BnetClient)
                If client2 Is Nothing Then
                    f.setValue(failure("No client matching that name."))
                    Return
                End If

                Dim link = New AdvertisingLink(client, client2)
                f.setValue(success("Created an advertising link between client {0} and client {1}.".frmt(client.name, client2.name)))
            End Sub
        End Class
        Public Class com_AdUnlink
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AdUnlink, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Client_AdUnlink_Help, _
                           My.Resources.Command_Client_AdUnlink_Access, _
                           My.Resources.Command_Client_AdUnlink_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim other_client = target.parent.find_client_R(arguments(0))
                Dim f = New Future(Of Outcome)
                FutureSub.frun( _
                            AddressOf process_2, _
                            futurize(f), _
                            futurize(target), _
                            other_client)
                Return f
            End Function
            Private Sub process_2(ByVal f As Future(Of Outcome), ByVal client As BnetClient, ByVal client2 As BnetClient)
                If client2 Is Nothing Then
                    f.setValue(failure("No client matching that name."))
                    Return
                End If

                client.clear_advertising_partner(client2)
                f.setValue(success("Any link between client {0} and client {1} has been removed.".frmt(client.name, client2.name)))
            End Sub
        End Class

        '''<summary>A command which forwards sub-commands to the main bot command set</summary>
        Public Class com_Bot
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Bot", _
                           0, ArgumentLimits.free, _
                           "[--bot command, --bot CreateUser Strilanc, --bot help] Forwards text commands to the main bot.")
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.parent.bot_commands.processText(target.parent, user, mendQuotedWords(arguments))
            End Function
        End Class

        Public Class com_ParentCommand
            Inherits BaseCommand(Of BnetClient)
            Private parent_command As BaseCommand(Of MainBot)
            Public Sub New(ByVal parent_command As BaseCommand(Of MainBot))
                MyBase.New(parent_command.name, parent_command.argument_limit_value, parent_command.argument_limit_type, parent_command.help, parent_command.required_permissions)
                Me.parent_command = parent_command
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return parent_command.processText(target.parent, user, mendQuotedWords(arguments))
            End Function
        End Class

        '''<summary>A command which returns the port the client is set to tell bnet it is listening on.</summary>
        Private Class com_GetPort
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_GetPort, _
                           0, ArgumentLimits.exact, _
                           My.Resources.Command_Client_GetPort_Help, _
                           My.Resources.Command_Client_GetPort_Access, _
                           My.Resources.Command_Client_GetPort_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return futurize(success(target.listen_port_P.ToString()))
            End Function
        End Class

        '''<summary>A command which changes the port the client is set to tell bnet it is listening on.</summary>
        Private Class com_SetPort
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_SetPort, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Client_SetPort_Help, _
                           My.Resources.Command_Client_SetPort_Access, _
                           My.Resources.Command_Client_SetPort_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim port As UShort
                If Not UShort.TryParse(arguments(0), port) Then Return futurize(failure("Invalid port"))
                Return target.set_listen_port_R(port)
            End Function
        End Class

        Private Class com_Disconnect
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("disconnect", _
                            0, ArgumentLimits.exact, _
                            "[--disconnect]", _
                            DictStrUInt("root=4"))
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                target.disconnect_R()
                Return futurize(New Outcome(Outcomes.succeeded))
            End Function
        End Class

        Public Class com_AddUser
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AddUser, _
                            1, ArgumentLimits.exact, _
                            My.Resources.Command_Client_AddUser_Help, _
                            My.Resources.Command_Client_AddUser_Access, _
                            My.Resources.Command_Client_AddUser_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Try
                    Dim new_user As BotUser = target.profile.users.create_new_user(arguments(0))
                    Return futurize(success("Created " + new_user.name))
                Catch e As InvalidOperationException
                    Return futurize(failure(e.Message))
                End Try
            End Function
        End Class

        Public Class com_RemoveUser
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_RemoveUser, _
                            1, ArgumentLimits.exact, _
                            My.Resources.Command_Client_RemoveUser_Help, _
                            My.Resources.Command_Client_RemoveUser_Access, _
                            My.Resources.Command_Client_RemoveUser_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If Not target.profile.users.containsUser(arguments(0)) Then
                    Return futurize(failure("That user does not exist"))
                End If
                Dim target_user As BotUser = target.profile.users(arguments(0))
                If user IsNot Nothing AndAlso Not target_user < user Then
                    Return futurize(failure("You can only destroy users with lower permissions."))
                End If
                target.profile.users.remove_user(arguments(0))
                Return futurize(success("Removed " + arguments(0)))
            End Function
        End Class

        Public Class com_Promote
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Promote", _
                            2, ArgumentLimits.min, _
                            "[--Promote username permission level] Increases a permissions level for a user.", _
                            DictStrUInt("users=1"))
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'get target user
                If Not target.profile.users.containsUser(arguments(0)) Then
                    Return futurize(failure("That user does not exist"))
                End If
                Dim target_user As BotUser = target.profile.users(arguments(0))

                'get level
                Dim lvl As UInteger
                If arguments.Count < 3 Then
                    lvl = 1
                ElseIf Not UInteger.TryParse(arguments(2), lvl) Then
                    Return futurize(failure("Expected numeric argument for level"))
                End If

                'check for demotion in disguise
                If lvl <= target_user.permission(arguments(1)) Then
                    Return futurize(failure("That is not a promotion. Jerk."))
                End If

                'check for overpromotion
                If user IsNot Nothing AndAlso lvl > user.permission(arguments(1)) Then
                    Return futurize(failure("You can't promote users past your own permission levels."))
                End If

                target_user.permission(arguments(1)) = lvl
                Return futurize(success(arguments(0) + " had permission in " + arguments(1) + " promoted to " + lvl.ToString()))
            End Function
        End Class

        Public Class com_Demote
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Demote", _
                            2, ArgumentLimits.min, _
                            "[--Demote username permission level] Decreases a permissions level for a user.", _
                            DictStrUInt("users=3"))
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'get target user
                If Not target.profile.users.containsUser(arguments(0)) Then
                    Return futurize(failure("That user does not exist"))
                End If
                Dim target_user As BotUser = target.profile.users(arguments(0))

                'get level
                Dim lvl As UInteger
                If arguments.Count < 3 Then
                    lvl = 0
                ElseIf Not UInteger.TryParse(arguments(2), lvl) Then
                    Return futurize(failure("Expected numeric argument for level"))
                End If

                'check for promotion in disguise
                If lvl >= target_user.permission(arguments(1)) Then
                    Return futurize(failure("That is not a demotion."))
                End If

                'check for overdemotion
                If user IsNot Nothing AndAlso Not target_user < user Then
                    Return futurize(failure("You can only demote users with lower permissions."))
                End If

                target_user.permission(arguments(1)) = lvl
                Return futurize(success(arguments(0) + " had permission in " + arguments(1) + " demoted to " + lvl.ToString()))
            End Function
        End Class

        Public Class com_User
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_User, _
                           1, ArgumentLimits.max, _
                           My.Resources.Command_Client_User_Help, _
                           My.Resources.Command_Client_User_Access, _
                           My.Resources.Command_Client_User_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                If arguments.Count = 0 And user Is Nothing Then Return futurize(failure("No user specified."))
                Dim username = If(arguments.Count = 0, user.name, arguments(0))
                If target.profile.users.containsUser(username) Then
                    Return futurize(success(target.profile.users(username).ToString()))
                ElseIf target.profile.users.containsUser(BotUserSet.NAME_UNKNOWN_USER) Then
                    Return futurize(success("{0} is an unknown user with the permissions of the user '*unknown'".frmt(username)))
                Else
                    Return futurize(success("{0} is an ignored unknown user.".frmt(username)))
                End If
            End Function
        End Class
    End Class

    Public Class ClientLoginCommands
        Inherits BaseClientCommands

        Public Sub New()
            MyBase.New()
            add_subcommand(New com_Login)
        End Sub

        Private Class com_Login
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Login, _
                           2, ArgumentLimits.exact, _
                           My.Resources.Command_Client_Login_Help, _
                           My.Resources.Command_Client_Login_Access, _
                           My.Resources.Command_Client_Login_ExtraHelp, _
                           True)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.logon_R(arguments(0), arguments(1))
            End Function
        End Class
    End Class

    Public Class ClientOnlineCommands
        Inherits BaseClientCommands

        Public Sub New()
            add_subcommand(New com_AdminCode)
            add_subcommand(New com_CancelHost)
            add_subcommand(New com_Elevate)
            add_subcommand(New com_Instance)
            add_subcommand(New com_Host)
            add_subcommand(New com_Say)
            add_subcommand(New com_StartAdvertising)
            add_subcommand(New com_StopAdvertising)
        End Sub

        Public Class com_Host
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Host, _
                           2, ArgumentLimits.min, _
                           My.Resources.Command_Client_Host_Help, _
                           My.Resources.Command_Client_Host_Access, _
                           My.Resources.Command_Client_Host_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'Map
                Dim map_out = W3Map.FromArgument(arguments(1))
                If map_out.outcome <> Outcomes.succeeded Then Return futurize(Of Outcome)(map_out)
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
                        Return futurize(failure("You need root=5 to use -port."))
                    End If
                Next i
                Dim settings = New ServerSettings(map, _
                                                  If(user Is Nothing, Nothing, user.name), _
                                                  arguments:=arguments, _
                                                  default_listen_ports:=New UShort() {target.listen_port_P})

                Dim f = New Future(Of Outcome)
                'request advertise client and create server
                Dim created_server = target.parent.create_server_R(target.name, settings, "[Linked]", True)
                Call FutureSub.frun(AddressOf process_0, _
                            futurize(f), _
                            futurize(user), _
                            futurize(target), _
                            futurize(arguments), _
                            created_server)
                Return f
            End Function

            Private Sub process_0( _
                        ByVal f As Future(Of Outcome), _
                        ByVal user As BotUser, _
                        ByVal client As BnetClient, _
                        ByVal arguments As IList(Of String), _
                        ByVal created_server As Outcome(Of IW3Server))
                If created_server.outcome <> Outcomes.succeeded Then
                    f.setValue(created_server)
                    Return
                End If

                Dim server = created_server.val
                client.set_user_server_R(user, server)
                Dim advertised = client.start_advertising_game_R(server, arguments(0), server.settings.map, arguments)

                FutureSub.frun(AddressOf process_1, futurize(f), futurize(server), advertised)
            End Sub
            Private Sub process_1(ByVal f As Future(Of Outcome), ByVal server As IW3Server, ByVal advertised As Outcome)
                If advertised.outcome = Outcomes.succeeded Then
                    f.setValue(success("Game hosted succesfully. Admin code is '{0}'.".frmt(server.settings.admin_password)))
                Else
                    server.f_Kill()
                    f.setValue(advertised)
                End If
            End Sub
        End Class

        Public Class com_Instance
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Instance", _
                        1, ArgumentLimits.min, _
                        "[Instance name command..., Instance 0 boot red] Forwards commands to an instance of your hosted server.")
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim f = New Future(Of Outcome)
                FutureSub.frun( _
                            AddressOf process_2, _
                            futurize(f), _
                            futurize(target), _
                            futurize(user), _
                            futurize(arguments), _
                            target.get_user_server_R(user))
                Return f
            End Function
            Private Sub process_2(ByVal f As Future(Of Outcome), _
                                  ByVal target As BnetClient, _
                                  ByVal user As BotUser, _
                                  ByVal arguments As IList(Of String), _
                                  ByVal server As IW3Server)
                If server Is Nothing Then
                    f.setValue(failure("You don't have a hosted game to forward that command to."))
                    Return
                End If

                FutureSub.frun( _
                            AddressOf process_3, _
                            futurize(f), _
                            futurize(user), _
                            futurize(arguments), _
                            server.f_FindGame(arguments(0)))
            End Sub
            Private Sub process_3(ByVal f As Future(Of Outcome), ByVal user As BotUser, ByVal arguments As IList(Of String), ByVal game As IW3Game)
                If game Is Nothing Then
                    f.setValue(failure("No game with that name."))
                    Return
                End If
                FutureSub.frun(AddressOf f.setValue, game.f_CommandProcessText(Nothing, mendQuotedWords(arguments.Skip(1).ToList())))
            End Sub
        End Class

        Public Class com_CancelHost
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("CancelHost", _
                        0, ArgumentLimits.exact, _
                        "[--CancelHost] Cancels the last hosting command.")
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim f = New Future(Of Outcome)
                FutureSub.frun( _
                            AddressOf process_2, _
                            futurize(f), _
                            futurize(target), _
                            futurize(user), _
                            futurize(arguments), _
                            target.get_user_server_R(user))
                Return f
            End Function
            Private Sub process_2(ByVal f As Future(Of Outcome), _
                                  ByVal target As BnetClient, _
                                  ByVal user As BotUser, _
                                  ByVal arguments As IList(Of String), _
                                  ByVal server As IW3Server)
                If server Is Nothing Then
                    f.setValue(failure("You don't have a hosted game to cancel."))
                    Return
                End If

                server.f_Kill()
                f.setValue(success("Cancelled hosting."))
            End Sub
        End Class

        Public Class com_AdminCode
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("AdminCode", _
                        0, ArgumentLimits.exact, _
                        "[--AdminCode] Repeats the admin code for a game you have hosted.")
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, _
                                              ByVal user As BotUser, _
                                              ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim f = New Future(Of Outcome)
                FutureSub.frun( _
                            AddressOf process_2, _
                            futurize(f), _
                            futurize(target), _
                            futurize(user), _
                            futurize(arguments), _
                            target.get_user_server_R(user))
                Return f
            End Function
            Private Sub process_2(ByVal f As Future(Of Outcome), _
                                  ByVal target As BnetClient, _
                                  ByVal user As BotUser, _
                                  ByVal arguments As IList(Of String), _
                                  ByVal server As IW3Server)
                If server Is Nothing Then
                    f.setValue(failure("You don't have a hosted game."))
                    Return
                End If
                f.setValue(success(server.settings.admin_password))
            End Sub
        End Class

        Public Class com_Say
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Say, _
                            1, ArgumentLimits.exact, _
                            My.Resources.Command_Client_Say_Help, _
                            My.Resources.Command_Client_Say_Access, _
                            My.Resources.Command_Client_Say_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                target.send_text_R(arguments(0))
                Return futurize(New Outcome(Outcomes.succeeded))
            End Function
        End Class

        '''<summary>A command which tells the client to start advertising a game.</summary>
        Public Class com_StartAdvertising
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_StartAdvertising, _
                            2, ArgumentLimits.min, _
                            My.Resources.Command_Client_StartAdvertising_Help, _
                            My.Resources.Command_Client_StartAdvertising_Access, _
                            My.Resources.Command_Client_StartAdvertising_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                'Map
                Dim map_out = W3Map.FromArgument(arguments(1))
                If map_out.outcome <> Outcomes.succeeded Then Return futurize(Of Outcome)(map_out)
                Dim map = map_out.val

                'create
                Return target.start_advertising_game_R(Nothing, arguments(0), map, arguments)
            End Function
        End Class

        '''<summary>A command which tells the client to stop advertising a game.</summary>
        Public Class com_StopAdvertising
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_StopAdvertising, _
                            0, ArgumentLimits.exact, _
                            My.Resources.Command_Client_StopAdvertising_Help, _
                            My.Resources.Command_Client_StopAdvertising_Access, _
                            My.Resources.Command_Client_StopAdvertising_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.stop_advertising_game_R("Advertising stopped manually.")
            End Function
        End Class

        Public Class com_Elevate
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Elevate, _
                        1, ArgumentLimits.max, _
                        My.Resources.Command_Client_Elevate_Help, _
                        My.Resources.Command_Client_Elevate_Access, _
                        My.Resources.Command_Client_Elevate_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim f = New Future(Of Outcome)
                FutureSub.frun( _
                            AddressOf process_2, _
                            futurize(f), _
                            futurize(user), _
                            futurize(arguments), _
                            target.get_user_server_R(user))
                Return f
            End Function
            Private Sub process_2(ByVal f As Future(Of Outcome), ByVal user As BotUser, ByVal arguments As IList(Of String), ByVal server As IW3Server)
                If server Is Nothing Then
                    f.setValue(failure("You don't have a hosted game."))
                    Return
                End If

                Dim username As String
                If arguments.Count = 0 Then
                    username = user.name
                Else
                    username = arguments(0)
                End If

                FutureSub.frun( _
                            AddressOf process_3, _
                            futurize(f), _
                            futurize(username), _
                            server.f_FindPlayerGame(username))
            End Sub
            Private Sub process_3(ByVal f As Future(Of Outcome), ByVal username As String, ByVal game As IW3Game)
                If game Is Nothing Then
                    f.setValue(failure("No matching user found."))
                    Return
                End If

                FutureSub.frun(AddressOf f.setValue, game.f_TryElevatePlayer(username))
            End Sub
        End Class
    End Class

    Public Class ClientOfflineCommands
        Inherits BaseClientCommands

        Public Sub New()
            add_subcommand(New com_Connect)
        End Sub

        Private Class com_Connect
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Connect, _
                            1, ArgumentLimits.exact, _
                            My.Resources.Command_Client_Connect_Help, _
                            My.Resources.Command_Client_Connect_Access, _
                            My.Resources.Command_Client_Connect_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.connect_R(arguments(0))
            End Function
        End Class
    End Class
End Namespace
