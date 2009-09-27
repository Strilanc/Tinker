Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Warcraft3

Namespace Commands.Specializations
    Public Class BotCommands
        Inherits CommandSet(Of MainBot)
        Public Sub New()
            AddCommand(New CommandClient)
            AddCommand(New CommandConnect)
            AddCommand(New CommandCreateCKL)
            AddCommand(New CommandCreateClient)
            AddCommand(New CommandCreateServer)
            AddCommand(New CommandDownloadEpicWarMap)
            AddCommand(New CommandFindMaps)
            AddCommand(New CommandKillCKL)
            AddCommand(New CommandKillClient)
            AddCommand(New CommandKillServer)
            AddCommand(New CommandLoadPlugin)
            AddCommand(New CommandServer)
            AddCommand(New CommandRecacheIP)

            Add(name:="Get",
                help:="[Get setting] Returns the value of a global setting. Supported settings are tickperiod, laglimit, commandprefix, gamerate.",
                requiredPermissions:="root=1",
                argumentLimit:=1,
                argumentLimitType:=ArgumentLimitType.Exact,
                func:=Function(target, user, arguments)
                          Dim argSetting = arguments(0)

                          Dim settingValue As Object
                          Select Case argSetting.ToUpperInvariant()
                              Case "TICKPERIOD" : settingValue = My.Settings.game_tick_period
                              Case "LAGLIMIT" : settingValue = My.Settings.game_lag_limit
                              Case "COMMANDPREFIX" : settingValue = My.Settings.commandPrefix
                              Case "GAMERATE" : settingValue = My.Settings.game_speed_factor
                              Case Else : Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argSetting))
                          End Select
                          Return "{0} = '{1}'".Frmt(argSetting, settingValue).Futurized
                      End Function)

            Add(name:="Set",
                help:="[--Set setting value] Sets a global setting. Supported settings are tickperiod, laglimit, commandprefix, gamerate.",
                requiredPermissions:="root=2",
                argumentLimit:=2,
                argumentLimitType:=ArgumentLimitType.Exact,
                func:=Function(target, user, arguments)
                          Dim argSetting = arguments(0)
                          Dim argValue = arguments(1)

                          Dim valueIntegral As UShort
                          Dim valueFloat As Double
                          Dim isShort = UShort.TryParse(argValue, valueIntegral)
                          Dim isDouble = Double.TryParse(argValue, valueFloat)
                          Select Case argSetting.ToUpperInvariant()
                              Case "TICKPERIOD"
                                  If Not isShort Or valueIntegral < 1 Or valueIntegral > 20000 Then  Throw New ArgumentException("Invalid value")
                                  My.Settings.game_tick_period = valueIntegral
                              Case "LAGLIMIT"
                                  If Not isShort Or valueIntegral < 1 Or valueIntegral > 20000 Then  Throw New ArgumentException("Invalid value")
                                  My.Settings.game_lag_limit = valueIntegral
                              Case "COMMANDPREFIX"
                                  My.Settings.commandPrefix = argValue
                              Case "GAMERATE"
                                  If Not isDouble Or valueFloat < 0.01 Or valueFloat > 10 Then  Throw New ArgumentException("Invalid value")
                                  My.Settings.game_speed_factor = valueFloat
                              Case Else
                                  Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argSetting))
                          End Select
                          Return "{0} set to {1}".Frmt(argSetting, argValue).Futurized
                      End Function)

            Add(name:="CreateAdmin",
                help:="[--CreateAdmin name password server_port receiver=localhost] Creates a server with an admin game and a LAN advertiser for the server.",
                requiredPermissions:="root=2",
                argumentLimit:=2,
                shouldHideArguments:=True,
                func:=Function(target, user, arguments)
                          Dim argName = arguments(0)
                          Dim argPassword = arguments(1)
                          Dim argListenPort = CUShort(0)
                          Dim argRemoteHost = "localhost"
                          If arguments.Count >= 3 AndAlso Not UShort.TryParse(arguments(2), argListenPort) Then
                              Throw New ArgumentException("Invalid listen port.")
                          End If
                          If arguments.Count >= 4 Then  argRemoteHost = arguments(3)

                          Return target.QueueCreateLanAdmin(argName,
                                                            argPassword,
                                                            argRemoteHost,
                                                            argListenPort).EvalOnSuccess(Function() "Created Lan Admin.")
                      End Function)

            Add(name:="CreateLan",
                help:="[CreateLan name listen_port receiver=localhost] Creates a lan advertiser.",
                requiredPermissions:="root=5",
                argumentLimit:=1,
                func:=Function(target, user, arguments)
                          Dim argName = arguments(0)
                          Dim argListenPort = CUShort(0)
                          Dim argRemoteHost = "localhost"
                          If arguments.Count >= 2 AndAlso Not UShort.TryParse(arguments(1), argListenPort) Then
                              Throw New ArgumentException("Invalid listen port.")
                          End If
                          If arguments.Count >= 3 Then  argRemoteHost = arguments(2)

                          Dim futureLanAdvertiser As ifuture
                          If argListenPort = 0 Then
                              Dim out = target.portPool.TryAcquireAnyPort()
                              If out Is Nothing Then  Throw New OperationFailedException("Failed to get a port from pool.")
                              futureLanAdvertiser = target.QueueAddWidget(New W3LanAdvertiser(target, argName, out, argRemoteHost))
                          Else
                              futureLanAdvertiser = target.QueueAddWidget(New W3LanAdvertiser(target, argName, argListenPort, argRemoteHost))
                          End If
                          Return futureLanAdvertiser.EvalOnSuccess(Function() "Created lan advertiser.")
                      End Function)

            Add(name:="KillLan",
                help:="[KillLan name] Removes a lan advertiser.",
                requiredPermissions:="root=5",
                argumentLimit:=1,
                argumentLimitType:=ArgumentLimitType.Exact,
                func:=Function(target, user, arguments)
                          Dim argName = arguments(0)

                          Return target.QueueRemoveWidget(W3LanAdvertiser.WidgetTypeName,
                                                          argName).EvalOnSuccess(Function() "Removed Lan Advertiser")
                      End Function)
        End Sub

        '''<summary>A command which forwards sub-commands to a named battle.net client.</summary>
        Public Class CommandClient
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Client,
                            1, ArgumentLimitType.Min,
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
        Public Class CommandServer
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Server,
                           1, ArgumentLimitType.Min,
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
        Public Class CommandCreateClient
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateClient,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Bot_CreateClient_Help,
                           "root=4", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueCreateClient(arguments(0)).
                        EvalOnSuccess(Function() "Created client '{0}'.".Frmt(arguments(0)))
            End Function
        End Class

        '''<summary>A command which kills a battle.net client.</summary>
        Public Class CommandKillClient
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillClient,
                           1, ArgumentLimitType.Exact,
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
        Public Class CommandKillServer
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillServer,
                           1, ArgumentLimitType.Exact,
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
        Public Class CommandCreateServer
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateServer,
                           2, ArgumentLimitType.Min,
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
        Public Class CommandLoadPlugin
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_LoadPlugin,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Bot_LoadPlugin_Help,
                           "root=5", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueLoadPlugin(arguments(0)).Select(Function(plugin) "Loaded plugin. Description: {0}".Frmt(plugin.Description))
            End Function
        End Class

        Public Class CommandRecacheIP
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New("RecacheIP",
                           0, ArgumentLimitType.Exact,
                           "Recaches external and internal IP addresses",
                           "root=5", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                CacheIPAddresses()
                Return "Recaching addresses.".Futurized
            End Function
        End Class

        '''<summary>A command which creates a battle.net client and logs on to a battle.net server.</summary>
        Private Class CommandConnect
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_Connect,
                           1, ArgumentLimitType.Min,
                           My.Resources.Command_Bot_Connect_Help,
                           My.Resources.Command_Bot_Connect_Access,
                           My.Resources.Command_Bot_Connect_ExtraHelp)
            End Sub
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
                            Dim futureLogOn = client.QueueConnectAndLogOn(client.profile.server.Split(" "c)(0),
                                                                          client.profile.userName,
                                                                          client.profile.password)

                            'Cleanup client if connection or login fail
                            futureLogOn.CallWhenReady(
                                Sub(finishedException)
                                    If finishedException IsNot Nothing Then
                                        target.QueueRemoveClient(clientName, expected:=False, reason:="Failed to Connect")
                                    End If
                                End Sub
                            )

                            Return futureLogOn.EvalOnSuccess(Function() client)
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

        Public Class CommandDownloadEpicWarMap
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_DownloadEpicWarMap,
                           1, ArgumentLimitType.Exact,
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
                            Dim httpFile As String = http.DownloadString("http://epicwar.com/maps/{0}/".Frmt(epicWarNumber))

                            'Find download link
                            Dim i = httpFile.IndexOf("alt=""Download""", StringComparison.InvariantCultureIgnoreCase)
                            i = httpFile.IndexOf("a href=""", i, StringComparison.InvariantCultureIgnoreCase)
                            i += "a href=""".Length
                            Dim j = httpFile.IndexOf(">", i, StringComparison.CurrentCultureIgnoreCase)
                            Dim link = "http://epicwar.com" + httpFile.Substring(i, j - i)

                            'Find filename
                            i = httpFile.IndexOf("Download ", i, StringComparison.InvariantCultureIgnoreCase) + "Download ".Length
                            j = httpFile.IndexOf("<", i, StringComparison.InvariantCultureIgnoreCase)
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

        Public Class CommandFindMaps
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_FindMaps,
                           1, ArgumentLimitType.Min,
                           My.Resources.Command_Bot_FindMaps_Help,
                           "games=1", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Const MAX_RESULTS As Integer = 5
                Dim results = FindFilesMatching("*" + arguments(0) + "*", "*.[wW]3[mxMX]", My.Settings.mapPath, MAX_RESULTS)
                If results.Count = 0 Then Return "No matching maps.".Futurized
                Return results.StringJoin(", ").Futurized
            End Function
        End Class

        Public Class CommandCreateCKL
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_CreateCKL,
                           1, ArgumentLimitType.Min,
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
        Public Class CommandKillCKL
            Inherits BaseCommand(Of MainBot)
            Public Sub New()
                MyBase.New(My.Resources.Command_Bot_KillCKL,
                           1, ArgumentLimitType.Exact,
                           My.Resources.Command_Bot_KillCKL_Help,
                           "root=5", "")
            End Sub
            Public Overrides Function Process(ByVal target As MainBot, ByVal user As BotUser, ByVal arguments As IList(Of String)) As IFuture(Of String)
                Return target.QueueRemoveWidget(CKL.BotCKLServer.WidgetTypeName, arguments(0)).EvalOnSuccess(Function() "Removed CKL server {0}".Frmt(arguments(0)))
            End Function
        End Class
    End Class
End Namespace
