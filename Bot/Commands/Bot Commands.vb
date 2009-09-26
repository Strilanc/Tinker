Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Warcraft3

Namespace Commands.Specializations
    Public Class BotCommands
        Inherits UICommandSet(Of MainBot)
        Public Sub New()
            AddCommand(New com_Client)
            AddCommand(New com_Connect)
            AddCommand(New com_CreateCKL)
            AddCommand(New com_CreateClient)
            AddCommand(New com_CreateLan)
            AddCommand(New com_CreateServer)
            AddCommand(New com_DownloadEpicWarMap)
            AddCommand(New com_FindMaps)
            AddCommand(New com_GetSetting)
            AddCommand(New com_KillCKL)
            AddCommand(New com_KillClient)
            AddCommand(New com_KillServer)
            AddCommand(New com_LoadPlugin)
            AddCommand(New com_Server)
            AddCommand(New com_SetSetting)
            AddCommand(New com_CreateAdmin)
            AddCommand(New com_KillLan)
            AddCommand(New com_RecacheIP)
        End Sub

        Public Class com_GetSetting
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_GetSetting,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_GetSetting_Help,
                           "root=1", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
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
                        Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(arguments(0)))
                End Select
                Return "{0} = '{1}'".Frmt(arguments(0), val).Futurized
            End Function
        End Class
        Public Class com_SetSetting
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_SetSetting,
                           2, ArgumentLimits.exact,
                           My.Resources.Command_Bot_SetSetting_Help,
                           "root=2", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim val_us As UShort
                Dim vald As Double
                Dim is_short = UShort.TryParse(arguments(1), val_us)
                Dim is_double = Double.TryParse(arguments(1), vald)
                Select Case arguments(0).ToLower()
                    Case "tickperiod"
                        If Not is_short Or val_us < 1 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                        My.Settings.game_tick_period = val_us
                    Case "laglimit"
                        If Not is_short Or val_us < 1 Or val_us > 20000 Then Throw New ArgumentException("Invalid value")
                        My.Settings.game_lag_limit = val_us
                    Case "commandprefix"
                        My.Settings.commandPrefix = arguments(1)
                    Case "gamerate"
                        If Not is_double Or vald < 0.01 Or vald > 10 Then Throw New ArgumentException("Invalid value")
                        My.Settings.game_speed_factor = vald
                    Case Else
                        Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(arguments(0)))
                End Select
                Return "{0} set to {1}".Frmt(arguments(0), arguments(1)).Futurized
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
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim name = arguments(0)
                Dim password = arguments(1)
                Dim remote_host = "localhost"
                Dim listen_port = CUShort(0)
                If arguments.Count >= 3 AndAlso Not UShort.TryParse(arguments(2), listen_port) Then
                    Throw New ArgumentException("Invalid listen port.")
                End If
                If arguments.Count >= 4 Then remote_host = arguments(3)
                Return target.QueueCreateLanAdmin(name, password, remote_host, listen_port).EvalOnSuccess(Function() "Created Lan Admin.")
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
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim name = arguments(0)
                Dim listen_port = CUShort(0)
                Dim remote_host = "localhost"
                If arguments.Count >= 2 AndAlso Not UShort.TryParse(arguments(1), listen_port) Then
                    Throw New ArgumentException("Invalid listen port.")
                End If
                If arguments.Count >= 3 Then remote_host = arguments(2)

                If listen_port = 0 Then
                    Dim out = target.portPool.TryAcquireAnyPort()
                    If out Is Nothing Then Throw New OperationFailedException("Failed to get a port from pool.")
                    Return target.QueueAddWidget(New W3LanAdvertiser(target, name, out, remote_host)).EvalOnSuccess(Function() "Created lan advertiser.")

                Else
                    Return target.QueueAddWidget(New W3LanAdvertiser(target, name, listen_port, remote_host)).EvalOnSuccess(Function() "Created lan advertiser.")
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
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueRemoveWidget(W3LanAdvertiser.TYPE_NAME, arguments(0)).EvalOnSuccess(Function() "Removed Lan Advertiser")
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
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueFindClient(arguments(0)).Select(
                    Function(client)
                        If client Is Nothing Then  Throw New ArgumentException("No matching client")
                        Return target.ClientCommands.ProcessCommand(client, user, arguments.SubToArray(1))
                    End Function
                ).Defuturized()
            End Function
        End Class

        '''<summary>A command which forwards sub-commands to a named warcraft 3 game server.</summary>
        Public Class com_Server
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Server,
                           1, ArgumentLimits.min,
                           My.Resources.Command_Bot_Server_Help,
                           "root=3", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                'Find the server, then pass the command to it
                Return target.QueueFindServer(arguments(0)).Select(
                    Function(server)
                        If server Is Nothing Then  Throw New ArgumentException("No matching server")
                        'Pass the command
                        Return target.ServerCommands.ProcessCommand(server, user, arguments.SubToArray(1))
                    End Function
                ).Defuturized()
            End Function
        End Class

        '''<summary>A command which creates a new battle.net client.</summary>
        Public Class com_CreateClient
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateClient,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_CreateClient_Help,
                           "root=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueCreateClient(arguments(0)).
                        EvalOnSuccess(Function() "Created client '{0}'.".Frmt(arguments(0)))
            End Function
        End Class

        '''<summary>A command which kills a battle.net client.</summary>
        Public Class com_KillClient
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillClient,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_KillClient_Help,
                           "root=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim name = arguments(0)
                Return target.QueueRemoveClient(name, expected:=True, reason:="KillClient Command").
                        EvalOnSuccess(Function() "Removed client named {0}.".Frmt(name))
            End Function
        End Class

        '''<summary>A command which kills a battle.net server.</summary>
        Public Class com_KillServer
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillServer,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_KillServer_Help,
                           "root=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim name = arguments(0)
                Return target.QueueRemoveServer(name).
                    EvalOnSuccess(Function() "Removed server named {0}.".Frmt(name))
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
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim map = W3Map.FromArgument(arguments(1))
                Dim settings = New ServerSettings(map,
                                                  New W3GameHeader(arguments(0),
                                                                   If(user Is Nothing, My.Resources.ProgramName, user.name),
                                                                   New W3MapSettings(arguments, map),
                                                                   0, 0, 0, arguments, map.NumPlayerSlots))
                Dim name = arguments(0)
                Return target.QueueCreateServer(name, settings).
                    EvalOnSuccess(Function() "Created server with name '{0}'. Admin password is {1}.".Frmt(name, settings.adminPassword))
            End Function
        End Class

        '''<summary>A command which attempts to load a plugin from the specified assembly in the plugins folder.</summary>
        Public Class com_LoadPlugin
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_LoadPlugin,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_LoadPlugin_Help,
                           "root=5", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueLoadPlugin(arguments(0)).Select(Function(plugin) "Loaded plugin. Description: {0}".Frmt(plugin.description))
            End Function
        End Class

        Public Class com_RecacheIP
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New("RecacheIP",
                           0, ArgumentLimits.exact,
                           "Recaches external and internal IP addresses",
                           "root=5", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                CacheIPAddresses()
                Return "Recaching addresses.".Futurized
            End Function
        End Class

        '''<summary>A command which creates a battle.net client and logs on to a battle.net server.</summary>
        Private Class com_Connect
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Connect,
                           1, ArgumentLimits.min,
                           My.Resources.Command_Bot_Connect_Help,
                           My.Resources.Command_Bot_Connect_Access,
                           My.Resources.Command_Bot_Connect_ExtraHelp)
            End Sub
            Private Class ClientOutcome
                Public ReadOnly client As BnetClient
                Public ReadOnly message As String
                Public Sub New(ByVal client As BnetClient, ByVal message As String)
                    Me.message = message
                    Me.client = client
                End Sub
            End Class
            Public Overrides Function Process(ByVal target As MainBot,
                                              ByVal user As BotUser,
                                              ByVal arguments As IList(Of String)) As IFuture(Of String)
                'Attempt to connect to each listed profile
                Dim futureClients = New List(Of IFuture(Of BnetClient))(arguments.Count)
                For Each arg In arguments
                    Dim clientName = arg
                    Dim profileName = arg '[Yes, client named same as profile]
                    'Create client, then connect to bnet, then login
                    Dim f = target.QueueCreateClient(clientName, profileName).Select(
                        Function(client)
                            'Connect to bnet, then login
                            Dim futureLogin = client.QueueConnectAndLogin(client.profile.server.Split(" "c)(0),
                                                                          client.profile.username,
                                                                          client.profile.password)

                            'Cleanup client if connection or login fail
                            futureLogin.CallWhenReady(
                                Sub(finishedException)
                                    If finishedException IsNot Nothing Then
                                        target.QueueRemoveClient(clientName, expected:=False, reason:="Failed to Connect")
                                    End If
                                End Sub
                            )

                            Return futureLogin.EvalOnSuccess(Function() client)
                        End Function
                    ).Defuturized
                    futureClients.Add(f)
                Next arg

                'Once the profiles all connect, link them together, or dispose them if any fail to connect
                Return futureClients.Defuturized.EvalWhenReady(
                    Function(exception)
                        Dim clients = From e In futureClients Where e.State = FutureState.Succeeded Select e.Value
                        If exception IsNot Nothing Then
                            'cleanup other clients
                            For Each e In clients
                                target.QueueRemoveClient(e.name, expected:=False, reason:="Linked client failed to Connect")
                            Next e
                            Throw New OperationFailedException(innerException:=exception)
                        End If
                        Links.AdvertisingLink.CreateMultiWayLink(clients)
                        Return "Connected"
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
                           "root=2", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Dim epicWarNumber As UInteger
                If Not UInteger.TryParse(arguments(0), epicWarNumber) Then
                    Throw New ArgumentException("Expected a numeric argument.")
                End If
                Return ThreadedFunc(
                    Function() As String
                        Dim filename As String = Nothing
                        Dim path As String = Nothing
                        Dim started = False
                        Try
                            Dim http As New Net.WebClient()
                            Dim httpFile As String = http.DownloadString("http://epicwar.com/maps/" + epicWarNumber.ToString() + "/")

                            'Find download link
                            Dim i = httpFile.IndexOf("alt=""Download""")
                            i = httpFile.IndexOf("a href=""", i)
                            i += "a href=""".Length
                            Dim j = httpFile.IndexOf(">", i)
                            Dim link = "http://epicwar.com" + httpFile.Substring(i, j - i)

                            'Find filename
                            i = httpFile.IndexOf("Download ", i) + "Download ".Length
                            j = httpFile.IndexOf("<", i)
                            filename = httpFile.Substring(i, j - i)
                            path = My.Settings.mapPath + filename

                            'Check for existing files
                            If IO.File.Exists(path + ".dl") Then
                                Throw New InvalidOperationException("A map with the filename '{0}' is already being downloaded.".Frmt(filename))
                            ElseIf IO.File.Exists(path) Then
                                Throw New InvalidOperationException("A map with the filename '{0}' already exists.".Frmt(filename))
                            End If

                            'Download
                            started = True
                            http.DownloadFile(link, path + ".dl")
                            IO.File.Move(path + ".dl", path)

                            'Finished
                            Return "Finished downloading map with filename '{0}'.".Frmt(filename)
                        Catch e As Exception
                            If started Then
                                'cleanup
                                IO.File.Delete(path + ".dl")
                                IO.File.Delete(path)
                            End If
                            Throw
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
                           "games=1", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Const MAX_RESULTS As Integer = 5
                Dim results = findFilesMatching("*" + arguments(0) + "*", "*.[wW]3[mxMX]", My.Settings.mapPath, MAX_RESULTS)
                If results.Count = 0 Then Return "No matching maps.".Futurized
                Return results.StringJoin(", ").Futurized
            End Function
        End Class

        Public Class com_CreateCKL
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateCKL,
                           1, ArgumentLimits.min,
                           My.Resources.Command_Bot_CreateCKL_Help,
                           "root=5", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                If arguments.Count < 2 Then
                    Dim port = target.portPool.TryAcquireAnyPort()
                    If port Is Nothing Then Throw New OperationFailedException("Failed to get a port from pool.")
                    Return target.QueueAddWidget(New CKL.BotCKLServer(arguments(0), port)).EvalOnSuccess(Function() "Added CKL server {0}".Frmt(arguments(0)))
                Else
                    Dim port As UShort
                    If Not UShort.TryParse(arguments(1), port) Then
                        Throw New OperationFailedException("Expected port number for second argument.")
                    End If
                    Dim widget = New CKL.BotCKLServer(arguments(0), port)
                    Return target.QueueAddWidget(widget).EvalOnSuccess(Function() "Added CKL server {0}".Frmt(arguments(0)))
                End If
            End Function
        End Class
        Public Class com_KillCKL
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillCKL,
                           1, ArgumentLimits.exact,
                           My.Resources.Command_Bot_KillCKL_Help,
                           "root=5", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueRemoveWidget(CKL.BotCKLServer.WIDGET_TYPE_NAME, arguments(0)).EvalOnSuccess(Function() "Removed CKL server {0}".Frmt(arguments(0)))
            End Function
        End Class
    End Class
End Namespace
