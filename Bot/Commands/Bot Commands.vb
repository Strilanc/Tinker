Imports HostBot.Commands
Imports HostBot.Bnet
Imports HostBot.Warcraft3

Namespace Commands.Specializations
    Public NotInheritable Class BotCommands
        Inherits CommandSet(Of MainBot)
        Public Sub New()
            AddCommand(New CommandClient)
            AddCommand(New CommandConnect)
            AddCommand(CreateCKL)
            AddCommand(New CommandCreateClient)
            AddCommand(New CommandCreateServer)
            AddCommand(New CommandDownloadEpicWarMap)
            AddCommand(New CommandFindMaps)
            AddCommand(KillCKL)
            AddCommand(New CommandKillClient)
            AddCommand(New CommandKillServer)
            AddCommand(New CommandLoadPlugin)
            AddCommand(New CommandServer)
            AddCommand(New CommandRecacheIP)
            AddCommand([Get])
            AddCommand([Set])
            AddCommand(CreateAdmin)
            AddCommand(CreateLan)
            AddCommand(KillLan)
        End Sub

        Private Shared ReadOnly [Get] As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="Get",
            template:="setting",
            Description:="Returns a global setting's value {tickperiod, laglimit, commandprefix, gamerate}.",
            Permissions:="root=1",
            func:=Function(target, user, argument)
                      Dim argSetting = argument.RawValue(0)

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

        Private Shared ReadOnly [Set] As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="Set",
            template:="setting value",
            Description:="Sets a global setting {tickperiod, laglimit, commandprefix, gamerate}.",
            Permissions:="root=2",
            func:=Function(target, user, argument)
                      Dim argSetting = argument.RawValue(0)
                      Dim argValue = argument.RawValue(1)

                      Dim valueIntegral As UShort
                      Dim valueFloat As Double
                      Dim isShort = UShort.TryParse(argValue, valueIntegral)
                      Dim isDouble = Double.TryParse(argValue, valueFloat)
                      Select Case argSetting.ToUpperInvariant()
                          Case "TICKPERIOD"
                              If Not isShort Or valueIntegral < 1 Or valueIntegral > 20000 Then Throw New ArgumentException("Invalid value")
                              My.Settings.game_tick_period = valueIntegral
                          Case "LAGLIMIT"
                              If Not isShort Or valueIntegral < 1 Or valueIntegral > 20000 Then Throw New ArgumentException("Invalid value")
                              My.Settings.game_lag_limit = valueIntegral
                          Case "COMMANDPREFIX"
                              My.Settings.commandPrefix = argValue
                          Case "GAMERATE"
                              If Not isDouble Or valueFloat < 0.01 Or valueFloat > 10 Then Throw New ArgumentException("Invalid value")
                              My.Settings.game_speed_factor = valueFloat
                          Case Else
                              Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argSetting))
                      End Select
                      Return "{0} set to {1}".Frmt(argSetting, argValue).Futurized
                  End Function)

        Private Shared ReadOnly CreateAdmin As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="CreateAdmin",
            template:="name password=x -port=pool -receiver=localhost",
            Description:="Creates a server with an admin game and a lan advertiser for the server.",
            HasPrivateArguments:=True,
            Permissions:="root=2",
            func:=Function(target, user, argument)
                      Dim argName = argument.RawValue(0)
                      Dim argPassword = argument.NamedValue("password")
                      Dim argListenPort = CUShort(0)
                      If argument.TryGetOptionalNamedValue("port") IsNot Nothing AndAlso Not UShort.TryParse(argument.TryGetOptionalNamedValue("port"), argListenPort) Then
                          Throw New ArgumentException("Invalid listen port.")
                      End If
                      Dim argRemoteHost = If(argument.TryGetOptionalNamedValue("receiver"), "localhost")

                      Return target.QueueCreateLanAdmin(argName,
                                                        argPassword,
                                                        argRemoteHost,
                                                        argListenPort).EvalOnSuccess(Function() "Created Lan Admin.")
                  End Function)

        Private Shared ReadOnly CreateLan As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="CreateLan",
            template:="name -port=pool -receiver=localhost",
            Description:="Creates a lan advertiser.",
            Permissions:="root=5",
            func:=Function(target, user, argument)
                      Dim argName = argument.RawValue(0)
                      Dim argListenPort = CUShort(0)
                      If argument.TryGetOptionalNamedValue("port") IsNot Nothing AndAlso Not UShort.TryParse(argument.TryGetOptionalNamedValue("port"), argListenPort) Then
                          Throw New ArgumentException("Invalid listen port.")
                      End If
                      Dim argRemoteHost = If(argument.TryGetOptionalNamedValue("receiver"), "localhost")

                      Dim futureLanAdvertiser As ifuture
                      If argListenPort = 0 Then
                          Dim out = target.portPool.TryAcquireAnyPort()
                          If out Is Nothing Then Throw New OperationFailedException("Failed to get a port from pool.")
                          futureLanAdvertiser = target.QueueAddWidget(New W3LanAdvertiser(target, argName, out, argRemoteHost))
                      Else
                          futureLanAdvertiser = target.QueueAddWidget(New W3LanAdvertiser(target, argName, argListenPort, argRemoteHost))
                      End If
                      Return futureLanAdvertiser.EvalOnSuccess(Function() "Created lan advertiser.")
                  End Function)

        Private Shared ReadOnly KillLan As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="KillLan",
            template:="name",
            Description:="Removes a lan advertiser.",
            Permissions:="root=5",
            func:=Function(target, user, argument)
                      Dim argName = argument.RawValue(0)
                      Return target.QueueRemoveWidget(W3LanAdvertiser.WidgetTypeName,
                                                      argName).EvalOnSuccess(Function() "Removed Lan Advertiser")
                  End Function)

        ''''<summary>A command which forwards sub-commands to a named battle.net client.</summary>
        Public NotInheritable Class CommandClient
            Inherits PartialCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="Client",
                           headtype:="clientCommand",
                           Description:="Forwards commands to the named battlenet client run by the bot.",
                           Permissions:="root=3")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As Strilbrary.Threading.IFuture(Of String)
                Return target.QueueFindClient(argumentHead).Select(
                    Function(client)
                        If client Is Nothing Then Throw New ArgumentException("No matching client")
                        Return target.ClientCommands.Invoke(client, user, argumentRest)
                    End Function
                ).Defuturized()
            End Function
        End Class

        ''''<summary>A command which forwards sub-commands to a named warcraft 3 game server.</summary>
        Public NotInheritable Class CommandServer
            Inherits PartialCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="Server",
                           headtype:="serverCommand",
                           Description:="Forwards commands to the named wc3 game server run by the bot.",
                           Permissions:="root=3")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As Strilbrary.Threading.IFuture(Of String)
                Return target.QueueFindServer(argumentHead).Select(
                    Function(server)
                        If server Is Nothing Then Throw New ArgumentException("No matching server")
                        Return target.ServerCommands.Invoke(server, user, argumentRest)
                    End Function
                ).Defuturized()
            End Function
        End Class

        ''''<summary>A command which creates a new battle.net client.</summary>
        Public NotInheritable Class CommandCreateClient
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="CreateClient",
                           template:="name -profile=default",
                           Description:="Creates a bnet client.",
                           permissions:="root=4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                Return target.QueueCreateClient(argument.NamedValue("name"), argument.TryGetOptionalNamedValue("profile")).
                EvalOnSuccess(Function() "Created client '{0}'.".Frmt(argument.RawValue(0)))
            End Function
        End Class

        ''''<summary>A command which kills a battle.net client.</summary>
        Public NotInheritable Class CommandKillClient
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="KillClient",
                           template:="name",
                           Description:="Kills the named bnet client.",
                           permissions:="root=4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                Dim name = argument.RawValue(0)
                Return target.QueueRemoveClient(name, expected:=True, reason:="KillClient Command").
                EvalOnSuccess(Function() "Removed client named {0}.".Frmt(name))
            End Function
        End Class

        ''''<summary>A command which kills a game server.</summary>
        Public NotInheritable Class CommandKillServer
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="KillServer",
                           template:="name",
                           Description:="Kills the named warcraft 3 game server.",
                           permissions:="root=4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                Dim name = argument.RawValue(0)
                Return target.QueueRemoveServer(name).
                EvalOnSuccess(Function() "Removed server named {0}.".Frmt(name))
            End Function
        End Class

        ''''<summary>A command which creates a new warcraft 3 game server.</summary>
        Public NotInheritable Class CommandCreateServer
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="CreateServer",
                           template:="name",
                           Description:="Creates a new wc3 game server. 'Help CreateSever *' for help with options.",
                           permissions:="root=4",
                           extraHelp:={"Admin=-Admin=Strilanc,-a=user,-a: Sets the auto-elevated username. Use no argument to match your name.",
                                       "Autostart=-Autostart, -as: Instances will start automatically when they fill up.",
                                       "Instances=-Instances=X, -i=X: Sets the initial number of instances. Use =0 for unlimited instances.",
                                       "FullShare=-FullShare: Turns on wc3's 'full shared control' option.",
                                       "MultiObs=-MultiObs,-mo: Turns on observers, and creates a special slot which can accept large amounts of players. The map must have two available obs slots for this to work.",
                                       "NoUL=-NoUL: Turns off uploads from the bot, but still allows players to download from each other.",
                                       "NoDL=-NoDL: Boots players who need to DL the map.",
                                       "Obs=-Obs, -o: Turns on full observers.",
                                       "ObsOnDefeat=-ObsOnDefeat, -od: Turns on observers on defeat.",
                                       "Permanent=-Permanent, -perm: Automatically recreate closed instances and automatically advertises when new instances are available.",
                                       "RandomHero=-RandomHero, -rh: Turns on the wc3 'random hero' option.",
                                       "RandomRace=-RandomRace, -rr: Turns on the wc3 'random race' option.",
                                       "Referees=-Referees,-ref: Turns on observer referees.",
                                       "Reserve=-Reserve=Name, -r: Reserves the first available slot for the player with the given name. Omit =Name to match your name.",
                                       "Speed=-Speed=X: Changes default speed. Possible values for X are medium/slow.",
                                       "Teams=-Teams=XvX, -t=XvXvX: Sets the initial number of open slots for each team.",
                                       "TeamsApart=-TeamsApart: Turns off wc3's 'teams together' option.",
                                       "UnlockTeams=-UnlockTeams: Turns off wc3's 'lock teams' option.",
                                       "Visibility=-Visibility=X, -vis=X: Changes the default visibility option from map default. Possible values of X are All/Explored/None."
                                       }.StringJoin(Environment.NewLine))
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Throw New NotImplementedException
                'Contract.Assume(arguments.Count >= 2)
                'Contract.Assume(arguments(0) IsNot Nothing)
                'Contract.Assume(arguments(1) IsNot Nothing)
                'Dim map = W3Map.FromArgument(arguments(1))
                'Dim settings = New ServerSettings(map,
                'W3GameDescription.FromArguments(arguments(0),
                'arguments(1),
                'If(user Is Nothing, My.Resources.ProgramName, user.Name),
                'arguments))
                'Dim name = arguments(0)
                'Return target.QueueCreateServer(name, settings).
                'EvalOnSuccess(Function() "Created server with name '{0}'. Admin password is {1}.".Frmt(name, settings.adminPassword))
            End Function
        End Class

        Public NotInheritable Class CommandLoadPlugin
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="LoadPlugin",
                           template:="name",
                           Description:="Loads the named plugin.",
                           permissions:="root=5")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                Return target.QueueLoadPlugin(argument.RawValue(0)).Select(Function(plugin) "Loaded plugin. Description: {0}".Frmt(plugin.Description))
            End Function
        End Class

        Public NotInheritable Class CommandRecacheIP
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="RecacheIP",
                           template:="",
                           Description:="Recaches external and internal IP addresses",
                           permissions:="root=5")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                CacheIPAddresses()
                Return "Recaching addresses.".Futurized
            End Function
        End Class

        ''''<summary>A command which creates a battle.net client and logs on to a battle.net server.</summary>
        Private NotInheritable Class CommandConnect
            Inherits Command(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="Connect",
                           Format:="profile1 profile2 ...",
                           Description:="Creates and connects bnet clients, using the given profiles.",
                           permissions:="root=4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
                Dim arguments = (From arg In argument.Split(" "c) Where arg <> "").ToArray
                If arguments.Length = 0 Then Throw New ArgumentException("No profiles specified.")

                'Attempt to connect to each listed profile
                Dim futureClients = New List(Of IFuture(Of BnetClient))(capacity:=arguments.Count)
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

                'Once all connection attempts have resolved, link them together, or dispose them all if any fail to connect
                Return futureClients.Defuturized.EvalWhenReady(
                    Function(exception)
                        Dim clients = From e In futureClients Where e.State = FutureState.Succeeded Select e.Value
                        If exception IsNot Nothing Then
                            'cleanup other clients
                            For Each e In clients
                                target.QueueRemoveClient(e.Name, expected:=False, reason:="Linked client failed to Connect")
                            Next e
                            Throw New OperationFailedException(innerException:=exception)
                        End If
                        Links.AdvertisingLink.CreateMultiWayLink(clients)
                        Return "Connected"
                    End Function
                )
            End Function
        End Class

        Public NotInheritable Class CommandDownloadEpicWarMap
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="DownloadEpicWarMap",
                           template:="EpicWarMapId",
                           Description:="Downloads a map from the EpicWar website.",
                           permissions:="root=2")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                Dim epicWarNumber As UInteger
                If Not UInteger.TryParse(argument.RawValue(0), epicWarNumber) Then
                    Throw New ArgumentException("Expected a numeric argument.")
                End If
                Return ThreadedFunc(
                    Function()
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

        Public NotInheritable Class CommandFindMaps
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="FindMaps",
                           Description:="Returns the first five maps matching a search query. The first match is the map used by other commands given the same query (eg. host).",
                           template:="MapQuery",
                           permissions:="games=1")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                Const MAX_RESULTS As Integer = 5
                Dim results = FindFilesMatching("*{0}*".Frmt(argument.RawValue(0)), "*.[wW]3[mxMX]", My.Settings.mapPath.AssumeNotNull, MAX_RESULTS)
                If results.Count = 0 Then Return "No matching maps.".Futurized
                Return results.StringJoin(", ").Futurized
            End Function
        End Class

        Private Shared ReadOnly CreateCKL As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="CreateCKL",
            Description:="Starts a CD Key Lending server that others can connect to and use to logon to bnet. This will NOT allow others to learn your cd keys, but WILL allow them to logon with your keys ONCE.",
            template:="name -port=#",
            Permissions:="root=5",
            func:=Function(target, user, argument)
                      If argument.TryGetOptionalNamedValue("port") Is Nothing Then
                          Dim port = target.portPool.TryAcquireAnyPort()
                          If port Is Nothing Then Throw New OperationFailedException("Failed to get a port from pool.")
                          Return target.QueueAddWidget(New CKL.BotCKLServer(argument.RawValue(0), port)).EvalOnSuccess(Function() "Added CKL server {0}".Frmt(argument.RawValue(0)))
                      Else
                          Dim port As UShort
                          If Not UShort.TryParse(argument.TryGetOptionalNamedValue("port"), port) Then
                              Throw New OperationFailedException("Expected port number for second argument.")
                          End If
                          Dim widget = New CKL.BotCKLServer(argument.RawValue(0), port)
                          Return target.QueueAddWidget(widget).EvalOnSuccess(Function() "Added CKL server {0}".Frmt(argument.RawValue(0)))
                      End If
                  End Function)

        Private Shared ReadOnly KillCKL As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="KillCKL",
            Description:="Removes a CD Key Lending server.",
            template:="name",
            Permissions:="root=5",
            func:=Function(target, user, argument)
                      Return target.QueueRemoveWidget(CKL.BotCKLServer.WidgetTypeName, argument.RawValue(0)).
                                    EvalOnSuccess(Function() "Removed CKL server {0}".Frmt(argument.RawValue(0)))
                  End Function)
    End Class
End Namespace
