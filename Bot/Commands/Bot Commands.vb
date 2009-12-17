Namespace Commands
    Public NotInheritable Class BotCommands
        Inherits CommandSet(Of MainBot)
        Public Sub New()
            AddCommand([To])
            AddCommand(ListComponents)
            AddCommand(New Connect)
            'AddCommand(CreateCKL)
            AddCommand(CreateClient)
            'AddCommand(New CommandCreateServer)
            AddCommand(New GenericCommands.FindMaps(Of MainBot))
            AddCommand(New GenericCommands.DownloadMap(Of MainBot))
            AddCommand(New GenericCommands.RecacheIP(Of MainBot))
            AddCommand(Dispose)
            AddCommand(New CommandLoadPlugin)
            AddCommand([Get])
            AddCommand([Set])
            'AddCommand(CreateAdmin)
            AddCommand(Lan)
        End Sub

        Private Shared ReadOnly ListComponents As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="Components",
            template:="-type=type",
            Description:="Lists all bot components. Use -type= to filter by component type.",
            Permissions:="root:1",
            func:=Function(target, user, argument)
                      Dim futureComponents = target.QueueGetAllComponents()
                      Dim typeFilter = argument.TryGetOptionalNamedValue("type")
                      If typeFilter Is Nothing Then
                          Return From components In futureComponents
                                 Select "Components: {0}.".Frmt(
                                     (From component In components
                                      Select "{0}:{1}".Frmt(component.type, component.name)
                                      ).StringJoin(", "))
                      Else
                          Return From components In futureComponents
                                 Select "{0} Components: {1}.".Frmt(typeFilter,
                                     (From component In components
                                      Where component.type = typeFilter
                                      Select component.name
                                      ).StringJoin(", "))
                      End If
                  End Function)

        Private Shared ReadOnly [Get] As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="Get",
            template:="setting",
            Description:="Returns a global setting's value {tickperiod, laglimit, commandprefix, gamerate}.",
            Permissions:="root:1",
            func:=Function(target, user, argument)
                      Dim argSetting As InvariantString = argument.RawValue(0)

                      Dim settingValue As Object
                      Select Case argSetting
                          Case "TickPeriod" : settingValue = My.Settings.game_tick_period
                          Case "LagLimit" : settingValue = My.Settings.game_lag_limit
                          Case "CommandPrefix" : settingValue = My.Settings.commandPrefix
                          Case "GameRate" : settingValue = My.Settings.game_speed_factor
                          Case Else : Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argSetting))
                      End Select
                      Return "{0} = '{1}'".Frmt(argSetting, settingValue).Futurized
                  End Function)

        Private Shared ReadOnly [Set] As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="Set",
            template:="setting value",
            Description:="Sets a global setting {tickperiod, laglimit, commandprefix, gamerate}.",
            Permissions:="root:2",
            func:=Function(target, user, argument)
                      Dim argSetting As InvariantString = argument.RawValue(0)
                      Dim argValue = argument.RawValue(1)

                      Dim valueIntegral As UShort
                      Dim valueFloat As Double
                      Dim isShort = UShort.TryParse(argValue, valueIntegral)
                      Dim isDouble = Double.TryParse(argValue, valueFloat)
                      Select Case argSetting
                          Case "TickPeriod"
                              If Not isShort Or valueIntegral < 1 Or valueIntegral > 20000 Then Throw New ArgumentException("Invalid value")
                              My.Settings.game_tick_period = valueIntegral
                          Case "LagLimit"
                              If Not isShort Or valueIntegral < 1 Or valueIntegral > 20000 Then Throw New ArgumentException("Invalid value")
                              My.Settings.game_lag_limit = valueIntegral
                          Case "CommandPrefix"
                              My.Settings.commandPrefix = argValue
                          Case "GameRate"
                              If Not isDouble Or valueFloat < 0.01 Or valueFloat > 10 Then Throw New ArgumentException("Invalid value")
                              My.Settings.game_speed_factor = valueFloat
                          Case Else
                              Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argSetting))
                      End Select
                      Return "{0} set to {1}".Frmt(argSetting, argValue).Futurized
                  End Function)

        'Private Shared ReadOnly CreateAdmin As New DelegatedTemplatedCommand(Of MainBot)(
        'Name:="CreateAdmin",
        'template:="name password=x -port=pool -receiver=localhost",
        'Description:="Creates a server with an admin game and a lan advertiser for the server.",
        'HasPrivateArguments:=True,
        'Permissions:="root:2",
        'func:=Function(target, user, argument)
        'Dim argName = argument.RawValue(0)
        'Dim argPassword = argument.NamedValue("password")
        'Dim argListenPort = CUShort(0)
        'If argument.TryGetOptionalNamedValue("port") IsNot Nothing AndAlso Not UShort.TryParse(argument.TryGetOptionalNamedValue("port"), argListenPort) Then
        'Throw New ArgumentException("Invalid listen port.")
        'End If
        'Dim argRemoteHost = If(argument.TryGetOptionalNamedValue("receiver"), "localhost")

        'Return target.QueueAddWidget(WC3.LanAdvertiser.CreateLanAdmin(argName,
        'argPassword,
        'argRemoteHost,
        'argListenPort)).EvalOnSuccess(Function() "Created Lan Admin.")
        'End Function)

        Private Shared ReadOnly Lan As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="Lan",
            template:="name -receiver=localhost",
            Description:="Creates a lan advertiser.",
            Permissions:="root:5",
            func:=Function(target, user, argument)
                      Dim argName = argument.RawValue(0)
                      Dim argRemoteHost = If(argument.TryGetOptionalNamedValue("receiver"), "localhost")

                      Dim advertiser = New WC3.LanAdvertiser(defaultTargetHost:=If(argument.TryGetOptionalNamedValue("receiver"), "localhost"))
                      Dim manager = New Components.LanAdvertiserManager(argName, target, advertiser)
                      Return target.QueueAddComponent(manager).EvalOnSuccess(Function() "Created lan advertiser.")
                  End Function)

        Public Shared ReadOnly [To] As New DelegatedPartialCommand(Of MainBot)(
            Name:="To",
            headtype:="type:name",
            Description:="Forwards commands to the named component.",
            Permissions:="root:3",
            func:=Function(target, user, argumentHead, argumentRest)
                      Dim args = argumentHead.Split(":"c)
                      If args.Length <> 2 Then Throw New ArgumentException("Expected widget type:name.")
                      Dim type As InvariantString = args(0)
                      Dim name As InvariantString = args(1)
                      Return target.QueueTryFindComponent(type, name).Select(
                          Function(component)
                              If component Is Nothing Then Throw New ArgumentException("No {0} named {1}.".Frmt(Type, Name))
                              Return component.InvokeCommand(user, argumentRest)
                          End Function
                      ).Defuturized()
                          End Function)

        Private Shared ReadOnly CreateClient As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="CreateClient",
            template:="name -profile=default",
            Description:="Creates a new bnet client.",
            Permissions:="root:4",
            func:=Function(target, user, argument)
                      Dim profileName As InvariantString = If(argument.TryGetOptionalNamedValue("profile"), "default")
                      Dim clientName As InvariantString = argument.RawValue(0)

                      Return Components.BnetClientManager.AsyncCreateFromProfile(clientName, profileName, target).Select(
                          Function(manager)
                              Dim added = target.QueueAddComponent(manager)
                              added.Catch(Sub() manager.Dispose())
                              Return added.EvalOnSuccess(Function() "Created Client")
                          End Function).defuturized
                          End Function)

        Public Shared ReadOnly Dispose As New DelegatedTemplatedCommand(Of MainBot)(
            Name:="Dispose",
            template:="type:name",
            Description:="Disposes a bot component.",
            Permissions:="root:4",
            func:=Function(target, user, argument)
                      Dim args = argument.RawValue(0).Split(":"c)
                      If args.Length <> 2 Then Throw New ArgumentException("Expected a component argument like: type:name.")
                      Dim type As InvariantString = args(0)
                      Dim name As InvariantString = args(1)
                      Dim futureComponent = target.QueueFindComponent(type, name)
                      Return futureComponent.Select(
                          Function(component)
                              component.dispose()
                              Return "Disposed {0}".Frmt(argument.RawValue(0))
                          End Function)
                          End Function)

        '''''<summary>A command which creates a new warcraft 3 game server.</summary>
        'Public NotInheritable Class CommandCreateServer
        'Inherits TemplatedCommand(Of MainBot)
        'Public Sub New()
        'MyBase.New(Name:="CreateServer",
        'template:=Concat({"name", "map=<search query>"}, WC3.GameSettings.PartialArgumentTemplates).StringJoin(" "),
        'Description:="Creates a new wc3 game server. 'Help CreateSever *' for help with options.",
        'Permissions:="root:4",
        'extraHelp:=WC3.GameSettings.PartialArgumentHelp.StringJoin(Environment.NewLine))
        'End Sub
        'Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
        'Dim name = argument.RawValue(0)
        'Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))
        'Dim stats = New WC3.GameStats(map,
        'If(user Is Nothing, My.Resources.ProgramName, user.Name),
        'argument)
        'Dim desc = WC3.LocalGameDescription.FromArguments(name, map, stats)
        'Dim settings = New WC3.GameSettings(map, desc, argument)
        'Throw New NotImplementedException
        ''Return target.QueueCreateServer(name, settings).
        ''EvalOnSuccess(Function() "Created server with name '{0}'. Admin password is {1}.".Frmt(name, settings.AdminPassword))
        'End Function
        'End Class

        Public NotInheritable Class CommandLoadPlugin
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="LoadPlugin",
                template:="name",
                Description:="Loads the named plugin.",
                Permissions:="root:5")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                Dim futureProfile = From profiles In target.QueueGetPluginProfiles()
                                    Select (From profile In profiles
                                            Where profile.name = argument.RawValue(0)).FirstOrDefault

                Return futureProfile.Select(
                    Function(profile)
                        If profile Is Nothing Then Throw New InvalidOperationException("No such plugin profile.")
                        Dim socket = New Plugins.PluginSocket(profile.name, target, profile.location)
                        Dim manager = New Components.PluginManager(socket)
                        Dim added = target.QueueAddComponent(manager)
                        added.Catch(Sub() manager.Dispose())
                        Return added.EvalOnSuccess(Function() "Loaded plugin. Description: {0}".Frmt(socket.Plugin.Description))
                    End Function).Defuturized
            End Function
        End Class

        ''''<summary>A command which creates a battle.net client and logs on to a battle.net server.</summary>
        Private NotInheritable Class Connect
            Inherits Command(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="Connect",
                Format:="profile1 profile2 ...",
                Description:="Creates and connects bnet clients, using the given profiles.",
                Permissions:="root:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
                Dim profileNames = (From word In argument.Split(" "c) Where word <> "").ToArray
                If profileNames.Length = 0 Then Throw New ArgumentException("No profiles specified.")

                'Attempt to connect to each listed profile
                Dim futureManagers = New List(Of IFuture(Of Components.BnetClientManager))(capacity:=profileNames.Count)
                For Each profileName In profileNames
                    Contract.Assume(profileName IsNot Nothing)
                    'Create and connect
                    Dim futureManager = Components.BnetClientManager.AsyncCreateFromProfile(profileName, profileName, target)
                    Dim futureAdded = (From manager In futureManager Select target.QueueAddComponent(manager)).Defuturized
                    Dim futureClient = futureAdded.EvalOnSuccess(Function() futureManager.Value.Client)
                    Dim futureConnected = (From client In futureClient
                                           Select client.QueueConnectAndLogOn(
                                                            remoteHost:=client.profile.server.Split(" "c)(0),
                                                            credentials:=New Bnet.ClientCredentials(client.profile.userName, client.profile.password))
                                                        ).Defuturized
                    'Cleanup on failure
                    futureManager.CallOnValueSuccess(Sub(manager) futureConnected.Catch(Sub() manager.Dispose())).SetHandled()
                    'Store
                    futureManagers.Add(futureConnected.EvalOnSuccess(Function() futureManager.Value))
                Next profileName

                'Link connected clients when connections completed
                Dim result = New FutureFunction(Of String)()
                futureManagers.Defuturized.CallOnSuccess(
                    Sub()
                        'Links.AdvertisingLink.CreateMultiWayLink(clients)
                        result.SetSucceeded("Connected")
                    End Sub
                ).Catch(
                    Sub(exception)
                        'Dispose all upon failure
                        For Each futureManager In futureManagers
                            If futureManager.State = FutureState.Succeeded Then
                                futureManager.Value.Dispose()
                            End If
                        Next futureManager
                        'Propagate
                        result.SetFailed(exception)
                    End Sub
                )
                Return result
            End Function
        End Class

        'Private Shared ReadOnly CreateCKL As New DelegatedTemplatedCommand(Of MainBot)(
        'Name:="CreateCKL",
        'Description:="Starts a CD Key Lending server that others can connect to and use to logon to bnet. This will NOT allow others to learn your cd keys, but WILL allow them to logon with your keys ONCE.",
        'template:="name -port=#",
        'Permissions:="root:5",
        'func:=Function(target, user, argument)
        'If argument.TryGetOptionalNamedValue("port") Is Nothing Then
        'Dim port = target.portPool.TryAcquireAnyPort()
        'If port Is Nothing Then Throw New OperationFailedException("Failed to get a port from pool.")
        'Return target.QueueAddWidget(New CKL.BotCKLServer(argument.RawValue(0), port)).EvalOnSuccess(Function() "Added CKL server {0}".Frmt(argument.RawValue(0)))
        'Else
        'Dim port As UShort
        'If Not UShort.TryParse(argument.TryGetOptionalNamedValue("port"), port) Then
        'Throw New OperationFailedException("Expected port number for second argument.")
        'End If
        'Dim widget = New CKL.BotCKLServer(argument.RawValue(0), port)
        'Return target.QueueAddWidget(widget).EvalOnSuccess(Function() "Added CKL server {0}".Frmt(argument.RawValue(0)))
        'End If
        'End Function)
    End Class
End Namespace
