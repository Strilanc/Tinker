Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Bnet.BnetClient
Imports HostBot.Warcraft3
Imports HostBot.Links

Namespace Commands.Specializations
    Public Class ClientCommands
        Inherits UICommandSet(Of BnetClient)

        Private com_login As New ClientLoginCommands()
        Private com_online As New ClientOnlineCommands()
        Private com_offline As New ClientOfflineCommands()

        Private Function GetCurrentCommandSet(ByVal target As BnetClient) As IFuture(Of BaseClientCommands)
            Return target.QueueGetState.Select(Of BaseClientCommands)(
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
        Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Return GetCurrentCommandSet(target).Select(Function(x) x.Process(target, user, arguments)).Defuturized()
        End Function
        Public Overrides Sub ProcessLocalText(ByVal target As Bnet.BnetClient, ByVal text As String, ByVal logger As Logger)
            GetCurrentCommandSet(target).CallOnValueSuccess(Sub(x) x.ProcessLocalText(target, text, logger))
        End Sub
    End Class

    Public MustInherit Class BaseClientCommands
        Inherits UICommandSet(Of BnetClient)

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
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AdLink,
                           1, ArgumentLimits.exact,
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
        Public Class com_AdUnlink
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AdUnlink,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Client_AdUnlink_Help,
                           My.Resources.Command_Client_AdUnlink_Access,
                           My.Resources.Command_Client_AdUnlink_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal client As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return client.parent.QueueFindClient(arguments(0)).Select(
                    Function(client2)
                        If client2 Is Nothing Then
                            Throw New ArgumentException("No client matching that name.")
                        ElseIf client2 Is client Then
                            Throw New ArgumentException("Can't link to self.")
                        End If

                        client.QueueRemoveAdvertisingPartner(client2)
                        Return "Any link between client {0} and client {1} has been removed.".Frmt(client.name, client2.name)
                    End Function
                )
            End Function
        End Class

        '''<summary>A command which forwards sub-commands to the main bot command set</summary>
        Public Class com_Bot
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Bot",
                           0, ArgumentLimits.free,
                           "[--bot command, --bot CreateUser Strilanc, --bot help] Forwards text commands to the main bot.",
                           "root=1", "")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.parent.BotCommands.ProcessCommand(target.parent, user, arguments)
            End Function
        End Class

        Public Class com_ParentCommand
            Inherits BaseCommand(Of BnetClient)
            Private parent_command As BaseCommand(Of MainBot)
            Public Sub New(ByVal parent_command As BaseCommand(Of MainBot))
                MyBase.New(parent_command.name, parent_command.argumentLimit, parent_command.argumentLimitType, parent_command.help, parent_command.requiredPermissions)
                Me.parent_command = parent_command
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return parent_command.ProcessCommand(target.parent, user, arguments)
            End Function
        End Class

        '''<summary>A command which returns the port the client is set to tell bnet it is listening on.</summary>
        Private Class com_GetPort
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_GetPort,
                           0, ArgumentLimits.exact,
                           My.Resources.Command_Client_GetPort_Help,
                           My.Resources.Command_Client_GetPort_Access,
                           My.Resources.Command_Client_GetPort_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueGetListenPort.Select(Function(port) port.ToString())
            End Function
        End Class

        '''<summary>A command which changes the port the client is set to tell bnet it is listening on.</summary>
        Private Class com_SetPort
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_SetPort,
                           1, ArgumentLimits.exact,
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

        Private Class com_Disconnect
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("disconnect",
                           0, ArgumentLimits.exact,
                           "[--disconnect]",
                           "root=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueDisconnect(expected:=True, reason:="Client Command").EvalOnSuccess(Function() "Disconnected")
            End Function
        End Class

        Public Class com_AddUser
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_AddUser,
                            1, ArgumentLimits.exact,
                            My.Resources.Command_Client_AddUser_Help,
                            My.Resources.Command_Client_AddUser_Access,
                            My.Resources.Command_Client_AddUser_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim newUser As BotUser = target.profile.users.CreateNewUser(arguments(0))
                Return "Created {0}".Frmt(newUser.name).Futurized
            End Function
        End Class

        Public Class com_RemoveUser
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_RemoveUser,
                            1, ArgumentLimits.exact,
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

        Public Class com_Promote
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Promote",
                            2, ArgumentLimits.min,
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
                If lvl <= target_user.permission(arguments(1)) Then
                    Throw New ArgumentException("That is not a promotion. Jerk.")
                End If

                'check for overpromotion
                If user IsNot Nothing AndAlso lvl > user.permission(arguments(1)) Then
                    Throw New ArgumentException("You can't promote users past your own permission levels.")
                End If

                target_user.permission(arguments(1)) = lvl
                Return "{0} had permission in {1} promoted to {2}".Frmt(arguments(0), arguments(1), lvl).Futurized
            End Function
        End Class

        Public Class com_Demote
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Demote",
                            2, ArgumentLimits.min,
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
                If lvl >= target_user.permission(arguments(1)) Then
                    Throw New ArgumentException("That is not a demotion.")
                End If

                'check for overdemotion
                If user IsNot Nothing AndAlso Not target_user < user Then
                    Throw New ArgumentException("You can only demote users with lower permissions.")
                End If

                target_user.permission(arguments(1)) = lvl
                Return "{0} had permission in {1} demoted to {2}".Frmt(arguments(0), arguments(1), lvl).Futurized
            End Function
        End Class

        Public Class com_User
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_User,
                           1, ArgumentLimits.max,
                           My.Resources.Command_Client_User_Help,
                           My.Resources.Command_Client_User_Access,
                           My.Resources.Command_Client_User_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count = 0 And user Is Nothing Then Throw New ArgumentException("No user specified.")
                Dim username = If(arguments.Count = 0, user.name, arguments(0))
                If target.profile.users.ContainsUser(username) Then
                    Return target.profile.users(username).ToString().Futurized
                ElseIf target.profile.users.ContainsUser(BotUserSet.NAME_UNKNOWN_USER) Then
                    Return "{0} is an unknown user with the permissions of the user '*unknown'".Frmt(username).Futurized
                Else
                    Return "{0} is an ignored unknown user.".Frmt(username).Futurized
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
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Login,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Client_Login_Help,
                           My.Resources.Command_Client_Login_Access,
                           My.Resources.Command_Client_Login_ExtraHelp,
                           shouldHideArguments:=True)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueLogin(arguments(0), arguments(1)).EvalOnSuccess(Function() "Logged in as {0}".Frmt(arguments(0)))
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
            AddCommand(New com_RefreshGamesList)
        End Sub

        Public Class com_Host
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Host,
                           2, ArgumentLimits.min,
                           My.Resources.Command_Client_Host_Help,
                           My.Resources.Command_Client_Host_Access,
                           My.Resources.Command_Client_Host_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim map = W3Map.FromArgument(arguments(1))

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
                        Throw New InvalidOperationException("You need root=5 to use -port.")
                    End If
                Next i
                Dim header = New W3GameHeader(arguments(0),
                                              If(user Is Nothing, My.Resources.ProgramName, user.name),
                                              New W3MapSettings(arguments, map),
                                              0, 0, 0, arguments, map.NumPlayerSlots)
                Dim f_settings = target.QueueGetListenPort.Select(Function(port) New ServerSettings(map, header, default_listen_ports:={port}))
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

        Public Class com_RefreshGamesList
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("RefreshGamesList",
                           0, ArgumentLimits.exact,
                           "[RefreshGamesList] Refreshes the bot's game list display. No useful effect from bnet.")
            End Sub
            Public Overrides Function Process(ByVal target As BnetClient,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueSendPacket(BnetPacket.MakeQueryGamesList()).EvalOnSuccess(Function() "Sent request.")
            End Function
        End Class
        Public Class com_Game
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("Game",
                        1, ArgumentLimits.min,
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
                                Return game.QueueProcessCommand(Nothing, arguments.SubToArray(1))
                            End Function
                        ).Defuturized()
                    End Function
                ).Defuturized()
            End Function
        End Class

        Public Class com_CancelHost
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("CancelHost",
                           0, ArgumentLimits.exact,
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

        Public Class com_AdminCode
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New("AdminCode",
                           0, ArgumentLimits.exact,
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

        Public Class com_Say
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Say,
                           1, ArgumentLimits.exact,
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
        Public Class com_StartAdvertising
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_StartAdvertising,
                           2, ArgumentLimits.min,
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
        Public Class com_StopAdvertising
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_StopAdvertising,
                           0, ArgumentLimits.exact,
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

        Public Class com_Elevate
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Elevate,
                        1, ArgumentLimits.max,
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

    Public Class ClientOfflineCommands
        Inherits BaseClientCommands

        Public Sub New()
            AddCommand(New com_Connect)
        End Sub

        Private Class com_Connect
            Inherits BaseCommand(Of BnetClient)
            Public Sub New()
                MyBase.New(My.Resources.Command_Client_Connect,
                           1, ArgumentLimits.exact,
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
