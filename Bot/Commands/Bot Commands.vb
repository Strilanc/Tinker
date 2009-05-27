Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Warcraft3

Namespace Commands.Specializations
    Public Class BotCommands
        Inherits UICommandSet(Of MainBot)
        Public Sub New()
            add_subcommand(New com_Client)
            add_subcommand(New com_Connect)
            add_subcommand(New com_CreateCKL)
            add_subcommand(New com_CreateClient)
            add_subcommand(New com_CreateLan)
            add_subcommand(New com_CreateServer)
            add_subcommand(New com_DownloadEpicWarMap)
            add_subcommand(New com_FindMaps)
            add_subcommand(New com_GetSetting)
            add_subcommand(New com_KillCKL)
            add_subcommand(New com_KillClient)
            add_subcommand(New com_KillServer)
            add_subcommand(New com_LoadPlugin)
            add_subcommand(New com_Server)
            add_subcommand(New com_SetSetting)
            add_subcommand(New com_CreateAdmin)
            add_subcommand(New com_KillLan)
            add_subcommand(New com_RecacheIP)
        End Sub

        Public Class com_GetSetting
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_GetSetting,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_GetSetting_Help,
                           DictStrUInt("root=1"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
                Dim val As Object
                Select Case arguments(0).ToLower()
                    Case "tickperiod"
                        val = My.Settings.game_tick_period
                    Case "laglimit"
                        val = My.Settings.game_lag_limit
                    Case "commandprefix"
                        val = My.Settings.commandPrefix
                    Case "gamerate"
                        val = My.Settings.game_speed_factor
                    Case Else
                        Return futurize(failure("Unrecognized setting '{0}'.".frmt(arguments(0))))
                End Select
                Return futurize(success("{0} = '{1}'".frmt(arguments(0), val)))
            End Function
        End Class
        Public Class com_SetSetting
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_SetSetting,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Bot_SetSetting_Help,
                           DictStrUInt("root=2"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
                Dim val_us As UShort
                Dim vald As Double
                Dim is_short = UShort.TryParse(arguments(1), val_us)
                Dim is_double = Double.TryParse(arguments(1), vald)
                Select Case arguments(0).ToLower()
                    Case "tickperiod"
                        If Not is_short Or val_us < 1 Or val_us > 20000 Then Return futurize(failure("Invalid value"))
                        My.Settings.game_tick_period = val_us
                    Case "laglimit"
                        If Not is_short Or val_us < 1 Or val_us > 20000 Then Return futurize(failure("Invalid value"))
                        My.Settings.game_lag_limit = val_us
                    Case "commandprefix"
                        My.Settings.commandPrefix = arguments(1)
                    Case "gamerate"
                        If Not is_double Or vald < 0.01 Or vald > 10 Then Return futurize(failure("Invalid value"))
                        My.Settings.game_speed_factor = vald
                    Case Else
                        Return futurize(failure("Unrecognized setting '{0}'.".frmt(arguments(0))))
                End Select
                Return futurize(success("{0} set to {1}".frmt(arguments(0), arguments(1))))
            End Function
        End Class

        Private Class com_CreateAdmin
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateAdmin,
                            2, ArgumentLimits.min,
                            My.Resources.Command_Bot_CreateAdmin_Help,
                            My.Resources.Command_Bot_CreateAdmin_Access,
                            My.Resources.Command_Bot_CreateAdmin_ExtraHelp,
                            True)
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim name = arguments(0)
                Dim password = arguments(1)
                Dim remote_host = "localhost"
                Dim listen_port = CUShort(0)
                If arguments.Count >= 3 AndAlso Not UShort.TryParse(arguments(2), listen_port) Then
                    Return futurize(failure("Invalid listen port."))
                End If
                If arguments.Count >= 4 Then remote_host = arguments(3)
                Return target.create_lan_admin_R(name, password, remote_host, listen_port)
            End Function
        End Class

        Private Class com_CreateLan
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateLan,
                            1, ArgumentLimits.min,
                            My.Resources.Command_Bot_CreateLan_Help,
                            My.Resources.Command_Bot_CreateLan_Access,
                            My.Resources.Command_Bot_CreateLan_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As MainBot,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim name = arguments(0)
                Dim listen_port = CUShort(0)
                Dim remote_host = "localhost"
                If arguments.Count >= 2 AndAlso Not UShort.TryParse(arguments(1), listen_port) Then
                    Return futurize(failure("Invalid listen port."))
                End If
                If arguments.Count >= 3 Then remote_host = arguments(2)

                If listen_port = 0 Then
                    Dim out = target.port_pool.TryTakePortFromPool()
                    If Not out.succeeded Then Return futurize(Of Outcome)(out)
                    Return target.add_widget_R(New W3LanAdvertiser(target, name, out.val, remote_host))
                Else
                    Return target.add_widget_R(New W3LanAdvertiser(target, name, listen_port, remote_host))
                End If
            End Function
        End Class
        Public Class com_KillLan
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillLan,
                            1, ArgumentLimits.exact,
                            My.Resources.Command_Bot_KillLan_Help,
                            My.Resources.Command_Bot_KillLan_Access,
                            My.Resources.Command_Bot_KillLan_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.remove_widget_R(W3LanAdvertiser.TYPE_NAME, arguments(0))
            End Function
        End Class

        '''<summary>A command which forwards sub-commands to a named battle.net client.</summary>
        Public Class com_Client
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Client,
                            1, ArgumentLimits.min,
                            My.Resources.Command_Bot_Client_Help,
                            My.Resources.Command_Bot_Client_Access,
                            My.Resources.Command_Bot_Client_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
                Return FutureFunc.ffrun(
                    target.find_client_R(arguments(0)),
                    Function(client)
                        If client Is Nothing Then  Return futurize(failure("No matching client"))
                        Return target.client_commands.processText(client, user, mendQuotedWords(arguments, 1))
                    End Function
                )
            End Function
        End Class

        '''<summary>A command which forwards sub-commands to a named warcraft 3 game server.</summary>
        Public Class com_Server
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Server,
                           1, ArgumentLimits.min,
                           My.Resources.Command_Bot_Server_Help,
                           DictStrUInt("root=3"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
                'Find the server, then pass the command to it
                Return FutureFunc.ffrun(
                    target.find_server_R(arguments(0)),
                    Function(server)
                        If server Is Nothing Then  Return futurize(failure("No matching server"))
                        'Pass the command
                        Return target.server_commands.processText(server, user, mendQuotedWords(arguments, 1))
                    End Function
                )
            End Function
        End Class

        '''<summary>A command which creates a new battle.net client.</summary>
        Public Class com_CreateClient
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateClient,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_CreateClient_Help,
                           DictStrUInt("root=4"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return stripFutureOutcome(target.f_CreateClient(arguments(0)))
            End Function
        End Class

        '''<summary>A command which kills a battle.net client.</summary>
        Public Class com_KillClient
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillClient,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_KillClient_Help,
                           DictStrUInt("root=4"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.f_RemoveClient(arguments(0))
            End Function
        End Class

        '''<summary>A command which kills a battle.net server.</summary>
        Public Class com_KillServer
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillServer,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_KillServer_Help,
                           DictStrUInt("root=4"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.remove_server_R(arguments(0))
            End Function
        End Class

        '''<summary>A command which creates a new warcraft 3 game server.</summary>
        Public Class com_CreateServer
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateServer,
                           2, ArgumentLimits.min,
                           My.Resources.Command_Bot_CreateServer_Help,
                           My.Resources.Command_Bot_CreateServer_Access,
                           My.Resources.Command_Bot_CreateServer_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As MainBot,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim map_out = W3Map.FromArgument(arguments(1))
                If Not map_out.succeeded Then Return futurize(Of Outcome)(map_out)
                Dim map = map_out.val

                Dim settings = New ServerSettings(map,
                                                  If(user Is Nothing, Nothing, user.name),
                                                  arguments:=arguments)
                Return stripFutureOutcome(target.create_server_R(arguments(0), settings))
            End Function
        End Class

        '''<summary>A command which attempts to load a plugin from the specified assembly in the plugins folder.</summary>
        Public Class com_LoadPlugin
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_LoadPlugin,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_LoadPlugin_Help,
                           DictStrUInt("root=5"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.loadPlugin_R(arguments(0))
            End Function
        End Class

        Public Class com_RecacheIP
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New("RecacheIP",
                           0, ArgumentLimits.exact,
                           "Recaches external and internal IP addresses",
                           DictStrUInt("root=5"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                CacheIPAddresses()
                Return futurize(success("Recaching addresses."))
            End Function
        End Class

        '''<summary>A command which creates a battle.net client and logs on to a battle.net server.</summary>
        Private Class com_Connect
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Connect,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_Connect_Help,
                           My.Resources.Command_Bot_Connect_Access,
                           My.Resources.Command_Bot_Connect_ExtraHelp)
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim client_name = arguments(0)
                Dim profile_name = arguments(0) '[Yes, client named same as profile]
                'Create client, then connect to bnet, then login
                Return FutureFunc.ffrun(
                    target.f_CreateClient(client_name, profile_name),
                    Function(createdClient)
                        If Not createdClient.succeeded Then
                            Return futurize(CType(createdClient, Outcome)) 'failed
                        End If
                        Dim client = createdClient.val

                        'Connect to bnet, then login
                        Dim connectedAndLoggedIn = FutureFunc.ffrun(
                            client.f_Connect(client.profile.server.Split(" "c)(0)),
                            Function(connected)
                                If Not connected.succeeded Then
                                    Return futurize(connected) 'failed
                                End If

                                'Login
                                Return client.f_Login(client.profile.username, client.profile.password)
                            End Function
                        )

                        'Cleanup client if connection or login fail
                        FutureSub.frun(
                            connectedAndLoggedIn,
                            Sub(finished)
                                If Not finished.succeeded Then
                                    target.f_RemoveClient(arguments(0))
                                End If
                            End Sub
                        )

                        Return connectedAndLoggedIn
                    End Function
                )
            End Function
        End Class

        Public Class com_DownloadEpicWarMap
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_DownloadEpicWarMap,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_DownloadEpicWarMap_Help,
                           DictStrUInt("root=2"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
                Dim epicWarNumber As UInteger
                If Not UInteger.TryParse(arguments(0), epicWarNumber) Then
                    Return futurize(failure("Expected a numeric argument."))
                End If
                Return ThreadedFunc(
                    Function()
                        Dim filename As String = Nothing
                        Dim path As String = Nothing
                        Try
                            Dim http As New Net.WebClient()
                            Dim httpFile As String = http.DownloadString("http://epicwar.com/maps/" + epicWarNumber.ToString() + "/")

                            'Find download link
                            Dim i = httpFile.IndexOf("alt=""Download""")
                            i = httpFile.IndexOf("a href=""", i)
                            i += "a href=""".Length
                            Dim j = httpFile.IndexOf(">", i)
                            Dim link As String = "http://epicwar.com" + httpFile.Substring(i, j - i)

                            'Find filename
                            i = httpFile.IndexOf("Download ", i) + "Download ".Length
                            j = httpFile.IndexOf("<", i)
                            filename = httpFile.Substring(i, j - i)
                            path = My.Settings.mapPath + filename

                            'Check for existing files
                            If IO.File.Exists(path + ".dl") Then
                                Return failure("A map with the filename '{0}' is already being downloaded.".frmt(filename))
                            ElseIf IO.File.Exists(path) Then
                                Return failure("A map with the filename '{0}' already exists.".frmt(filename))
                            End If

                            'Download
                            http.DownloadFile(link, path + ".dl")
                            IO.File.Move(path + ".dl", path)

                            'Finished
                            Return success("Finished downloading map with filename '{0}'.".frmt(filename))
                        Catch e As Exception
                            If path IsNot Nothing Then
                                'cleanup
                                IO.File.Delete(path + ".dl")
                                IO.File.Delete(path)

                                Return failure("There was an error downloading the map '{0}'".frmt(filename))
                            Else
                                Return failure("There was an error downloading the map '{0}'".frmt(epicWarNumber))
                            End If
                        End Try
                    End Function
                )
            End Function
        End Class

        Public Class com_FindMaps
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_FindMaps,
                           1, ArgumentLimits.min,
                           My.Resources.Command_Bot_FindMaps_Help,
                           DictStrUInt("games=1"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Const MAX_RESULTS As Integer = 5
                Dim out = findFilesMatching("*" + arguments(0) + "*", "*.[wW]3[mxMX]", My.Settings.mapPath, MAX_RESULTS)
                Dim ret = ""
                For Each result In out.val
                    If ret <> "" Then ret += ", "
                    ret += result
                Next result
                If Not out.succeeded AndAlso out.val.Count = 0 Then
                    Return futurize(failure(out.message))
                ElseIf out.val.Count = 0 Then
                    Return futurize(success("No matching maps."))
                End If
                Return futurize(success(ret))
            End Function
        End Class

        Public Class com_CreateCKL
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateCKL,
                           4, ArgumentLimits.exact,
                           My.Resources.Command_Bot_CreateCKL_Help,
                           DictStrUInt("root=5"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim port As UShort
                If Not UShort.TryParse(arguments(1), port) Then
                    Return futurize(failure("Expected port number for second argument."))
                End If
                Dim widget = New BotCKLServer(Nothing, arguments(0), port, arguments(2), arguments(3))
                Return target.add_widget_R(widget)
            End Function
        End Class
        Public Class com_KillCKL
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillCKL,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_KillCKL_Help,
                           DictStrUInt("root=5"))
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.remove_widget_R(BotCKLServer.WIDGET_TYPE_NAME, arguments(0))
            End Function
        End Class
    End Class
End Namespace
