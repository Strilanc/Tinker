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
        End Sub

        Public Class com_GetSetting
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_GetSetting, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_GetSetting_Help, _
                           DictStrUInt("root=1"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
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
                MyBase.New(My.Resources.Command_Bot_SetSetting, _
                           2, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_SetSetting_Help, _
                           DictStrUInt("root=2"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
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
                MyBase.New(My.Resources.Command_Bot_CreateAdmin, _
                            2, ArgumentLimits.min, _
                            My.Resources.Command_Bot_CreateAdmin_Help, _
                            My.Resources.Command_Bot_CreateAdmin_Access, _
                            My.Resources.Command_Bot_CreateAdmin_ExtraHelp, _
                            True)
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
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
                MyBase.New(My.Resources.Command_Bot_CreateLan, _
                            1, ArgumentLimits.min, _
                            My.Resources.Command_Bot_CreateLan_Help, _
                            My.Resources.Command_Bot_CreateLan_Access, _
                            My.Resources.Command_Bot_CreateLan_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As MainBot, _
                                              ByVal user As BotUser, _
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
                    If out.outcome = Outcomes.failed Then Return futurize(Of Outcome)(out)
                    Return target.add_widget_R(New W3LanAdvertiser(target, name, out.val, remote_host))
                Else
                    Return target.add_widget_R(New W3LanAdvertiser(target, name, listen_port, remote_host))
                End If
            End Function
        End Class
        Public Class com_KillLan
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillLan, _
                            1, ArgumentLimits.exact, _
                            My.Resources.Command_Bot_KillLan_Help, _
                            My.Resources.Command_Bot_KillLan_Access, _
                            My.Resources.Command_Bot_KillLan_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.remove_widget_R(W3LanAdvertiser.TYPE_NAME, arguments(0))
            End Function
        End Class

        '''<summary>A command which forwards sub-commands to a named battle.net client.</summary>
        Public Class com_Client
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Client, _
                            1, ArgumentLimits.min, _
                            My.Resources.Command_Bot_Client_Help, _
                            My.Resources.Command_Bot_Client_Access, _
                            My.Resources.Command_Bot_Client_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
                Return futurefuture( _
                        FutureFunc(Of IFuture(Of Outcome)).frun( _
                            AddressOf runOnClient, _
                                target.find_client_R(arguments(0)), _
                                futurize(target), _
                                futurize(user), _
                                futurize(mendQuotedWords(arguments, 1))))
            End Function
            Private Function runOnClient(ByVal client As BnetClient, ByVal target As MainBot, ByVal user As BotUser, ByVal argument_text As String) As IFuture(Of Outcome)
                If client Is Nothing Then Return futurize(failure("No matching client"))
                Return target.client_commands.processText(client, user, argument_text)
            End Function
        End Class

        '''<summary>A command which forwards sub-commands to a named warcraft 3 game server.</summary>
        Public Class com_Server
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Server, _
                           1, ArgumentLimits.min, _
                           My.Resources.Command_Bot_Server_Help, _
                           DictStrUInt("root=3"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
                Return futurefuture( _
                        FutureFunc(Of IFuture(Of Outcome)).frun( _
                            AddressOf runOnServer, _
                                target.find_server_R(arguments(0)), _
                                futurize(target), _
                                futurize(user), _
                                futurize(mendQuotedWords(arguments, 1))))
            End Function
            Private Function runOnServer(ByVal server As IW3Server, _
                                         ByVal target As MainBot, _
                                         ByVal user As BotUser, _
                                         ByVal argument_text As String) As IFuture(Of Outcome)
                If server Is Nothing Then Return futurize(failure("No matching server"))
                Return target.server_commands.processText(server, user, argument_text)
            End Function
        End Class

        '''<summary>A command which creates a new battle.net client.</summary>
        Public Class com_CreateClient
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateClient, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_CreateClient_Help, _
                           DictStrUInt("root=4"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return stripFutureOutcome(target.create_client_R(arguments(0)))
            End Function
        End Class

        '''<summary>A command which kills a battle.net client.</summary>
        Public Class com_KillClient
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillClient, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_KillClient_Help, _
                           DictStrUInt("root=4"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.remove_client_R(arguments(0))
            End Function
        End Class

        '''<summary>A command which kills a battle.net server.</summary>
        Public Class com_KillServer
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillServer, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_KillServer_Help, _
                           DictStrUInt("root=4"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.remove_server_R(arguments(0))
            End Function
        End Class

        '''<summary>A command which creates a new warcraft 3 game server.</summary>
        Public Class com_CreateServer
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateServer, _
                           2, ArgumentLimits.min, _
                           My.Resources.Command_Bot_CreateServer_Help, _
                           My.Resources.Command_Bot_CreateServer_Access, _
                           My.Resources.Command_Bot_CreateServer_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As MainBot, _
                                              ByVal user As BotUser, _
                                              ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim map_out = W3Map.FromArgument(arguments(1))
                If map_out.outcome <> Outcomes.succeeded Then Return futurize(Of Outcome)(map_out)
                Dim map = map_out.val

                Dim settings = New ServerSettings(map, _
                                                  If(user Is Nothing, Nothing, user.name), _
                                                  arguments:=arguments)
                Return stripFutureOutcome(target.create_server_R(arguments(0), settings))
            End Function
        End Class

        '''<summary>A command which attempts to load a plugin from the specified assembly in the plugins folder.</summary>
        Public Class com_LoadPlugin
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_LoadPlugin, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_LoadPlugin_Help, _
                           DictStrUInt("root=5"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.loadPlugin_R(arguments(0))
            End Function
        End Class

        '''<summary>A command which creates a battle.net client and logs on to a battle.net server.</summary>
        Private Class com_Connect
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Connect, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_Connect_Help, _
                           My.Resources.Command_Bot_Connect_Access, _
                           My.Resources.Command_Bot_Connect_ExtraHelp)
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Dim f As New Future(Of Outcome)()
                FutureSub.frun(AddressOf process_created_client, _
                            futurize(f), _
                            target.create_client_R(arguments(0), arguments(0)), _
                            futurize(target), _
                            futurize(arguments))
                Return f
            End Function
            Private Sub process_created_client(ByVal f As Future(Of Outcome), ByVal out As Outcome(Of BnetClient), ByVal target As MainBot, ByVal arguments As IList(Of String))
                If out.outcome <> Outcomes.succeeded Then
                    f.setValue(out)
                    Return
                End If

                FutureSub.frun(AddressOf process_conected, _
                            futurize(f), _
                            out.val.connect_R(out.val.profile.server.Split(" "c)(0)), _
                            futurize(target), _
                            futurize(out.val), _
                            futurize(arguments))
            End Sub
            Private Sub process_conected(ByVal f As Future(Of Outcome), ByVal out As Outcome, ByVal target As MainBot, ByVal client As BnetClient, ByVal arguments As IList(Of String))
                If out.outcome <> Outcomes.succeeded Then
                    target.remove_client_R(arguments(0))
                    f.setValue(out)
                    Return
                End If

                FutureSub.frun(AddressOf process_logged_on, _
                            futurize(f), _
                            client.logon_R(client.profile.username, client.profile.password), _
                            futurize(target), _
                            futurize(client), _
                            futurize(arguments))
            End Sub
            Private Sub process_logged_on(ByVal f As Future(Of Outcome), ByVal out As Outcome, ByVal target As MainBot, ByVal client As BnetClient, ByVal arguments As IList(Of String))
                If out.outcome <> Outcomes.succeeded Then
                    target.remove_client_R(arguments(0))
                End If
                f.setValue(out)
            End Sub
        End Class

        Public Class com_DownloadEpicWarMap
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_DownloadEpicWarMap, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_DownloadEpicWarMap_Help, _
                           DictStrUInt("root=2"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As System.Collections.Generic.IList(Of String)) As IFuture(Of Outcome)
                Dim epicWarNumber As UInteger
                If Not UInteger.TryParse(arguments(0), epicWarNumber) Then
                    Return futurize(failure("Expected a numeric argument."))
                End If
                Dim f As New Future(Of Outcome)
                threadedCall(Function() eval(AddressOf dlmap, f, epicWarNumber), "Download Map")
                Return f
            End Function
            Private Sub dlmap(ByVal f As Future(Of Outcome), ByVal epicWarNumber As UInteger)
                Dim filename As String = Nothing
                Dim path As String = Nothing
                Try
                    Dim http As New Net.WebClient()
                    Dim httpFile As String = http.DownloadString("http://epicwar.com/maps/" + epicWarNumber.ToString() + "/")

                    'Find download link
                    Dim i As Integer = httpFile.IndexOf("alt=""Download""")
                    i = httpFile.IndexOf("a href=""", i)
                    i += "a href=""".Length
                    Dim j As Integer = httpFile.IndexOf(">", i)
                    Dim link As String = "http://epicwar.com" + httpFile.Substring(i, j - i)

                    'Find filename
                    i = httpFile.IndexOf("Download ", i) + "Download ".Length
                    j = httpFile.IndexOf("<", i)
                    filename = httpFile.Substring(i, j - i)
                    path = My.Settings.mapPath + filename

                    'Check for existing files
                    If IO.File.Exists(path + ".dl") Then
                        f.setValue(failure("A map with the filename '{0}' is already being downloaded.".frmt(filename)))
                        Return
                    End If
                    If IO.File.Exists(path) Then
                        f.setValue(failure("A map with the filename '{0}' already exists.".frmt(filename)))
                        Return
                    End If

                    'Download
                    http.DownloadFile(link, path + ".dl")
                    IO.File.Move(path + ".dl", path)

                    'Finished
                    f.setValue(success("Finished downloading map with filename '{0}'.".frmt(filename)))
                Catch e As Exception
                    If path IsNot Nothing Then
                        'cleanup
                        IO.File.Delete(path + ".dl")
                        IO.File.Delete(path)

                        f.setValue(failure("There was an error downloading the map '{0}'".frmt(filename)))
                    Else
                        f.setValue(failure("There was an error downloading the map '{0}'".frmt(epicWarNumber)))
                    End If
                End Try
            End Sub
        End Class

        Public Class com_FindMaps
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_FindMaps, _
                           1, ArgumentLimits.min, _
                           My.Resources.Command_Bot_FindMaps_Help, _
                           DictStrUInt("games=1"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Const MAX_RESULTS As Integer = 5
                Dim out = findFilesMatching("*" + arguments(0) + "*", "*.[wW]3[mxMX]", My.Settings.mapPath, MAX_RESULTS)
                Dim ret = ""
                For Each result In out.val
                    If ret <> "" Then ret += ", "
                    ret += result
                Next result
                If out.outcome = Outcomes.failed AndAlso out.val.Count = 0 Then
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
                MyBase.New(My.Resources.Command_Bot_CreateCKL, _
                           4, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_CreateCKL_Help, _
                           DictStrUInt("root=5"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
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
                MyBase.New(My.Resources.Command_Bot_KillCKL, _
                           1, ArgumentLimits.exact, _
                           My.Resources.Command_Bot_KillCKL_Help, _
                           DictStrUInt("root=5"))
            End Sub
            Public Overrides Function process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of Outcome)
                Return target.remove_widget_R(BotCKLServer.WIDGET_TYPE_NAME, arguments(0))
            End Function
        End Class
    End Class
End Namespace
