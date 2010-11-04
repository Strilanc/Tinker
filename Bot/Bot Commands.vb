Imports Tinker.Commands
Imports Tinker.Bot.Commands

Namespace Bot.Commands
    Public NotInheritable Class CommandConnect
        Inherits Command(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="Connect",
                       Format:="profile1 profile2 ...",
                       Description:="Creates and connects bnet clients, using the given profiles. All of the clients will be set to automatic hosting.",
                       Permissions:="root:4")
        End Sub
        Protected Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
            Contract.Assume(target IsNot Nothing)
            Dim profileNames = (From word In argument.Split(" "c) Where word <> "").Cache
            If profileNames.None Then Throw New ArgumentException("No profiles specified.")

            'Attempt to connect to each listed profile
            Dim allClients = New List(Of Task(Of Bnet.ClientManager))()
            For Each profileName In profileNames
                Contract.Assume(profileName IsNot Nothing)
                allClients.Add(CreateLoggedOnClientManagerAsync(target, profileName))
            Next profileName

            'Wait for all to complete
            Try
                Await allClients.AsAggregateTask()
            Catch ex As Exception
                For Each client In allClients
                    If client.Status = TaskStatus.RanToCompletion Then client.Dispose()
                Next client
                Throw
            End Try
            Return "Connected"
        End Function

        Private Async Function CreateLoggedOnClientManagerAsync(ByVal parent As MainBot, ByVal profileName As String) As Task(Of Bnet.ClientManager)
            Contract.Requires(parent IsNot Nothing)
            Contract.Requires(profileName IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of Bnet.ClientManager))() IsNot Nothing)

            Dim manager = Await Bnet.ClientManager.AsyncCreateFromProfile(profileName, profileName, parent)
            Try
                manager.QueueSetAutomatic(True)
                Await parent.Components.QueueAddComponent(manager)
                Dim client = manager.Client
                Dim profile = client.Profile
                Await client.QueueConnect(remoteHost:=profile.server.Split(" "c).First,
                                          remotePort:=Bnet.Client.BnetServerPort)
                Await client.QueueLogOn(Bnet.ClientAuthenticator.GeneratedFrom(profile.userName, profile.password))
                Return manager
            Catch ex As Exception
                manager.Dispose()
                Throw
            End Try
        End Function
    End Class

    Public NotInheritable Class CommandCreateAdminGame
        Inherits TemplatedCommand(Of MainBot)

        Public Sub New()
            MyBase.New(Name:="CreateAdminGame",
                       template:="name password=x",
                       Description:="Picks or creates a game server, and adds an admin game to it.",
                       Permissions:="local:1",
                       hasPrivateArguments:=True)
        End Sub

        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Contract.Assume(target IsNot Nothing)
            Dim name = argument.RawValue(0)
            Dim password = argument.NamedValue("password")
            Dim server = Await target.QueueGetOrConstructGameServer()
            Dim game = Await server.QueueAddAdminGame(name, password)
            Return "Added admin game to server. Use Lan Advertiser on auto to advertise it."
        End Function
    End Class

    Public NotInheritable Class CommandCreateCKL
        Inherits TemplatedCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="CreateCKL",
                       Description:="Starts a CD Key Lending server that others can connect to and use to logon to bnet. This will NOT allow others to learn your cd keys, but WILL allow them to logon with your keys ONCE.",
                       template:="name",
                       Permissions:="root:5")
        End Sub
        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Contract.Assume(target IsNot Nothing)
            Dim port = target.PortPool.TryAcquireAnyPort()
            If port Is Nothing Then Throw New OperationFailedException("No available ports in the pool.")
            Dim name = argument.RawValue(0)
            Dim server = New CKL.Server(name:=name,
                                        listenport:=port,
                                        clock:=New SystemClock())
            Dim manager = New CKL.ServerManager(server)
            Try
                Await target.Components.QueueAddComponent(manager)
                Return "Added CKL server {0}".Frmt(name)
            Catch ex As Exception
                manager.Dispose()
                Throw
            End Try
        End Function
    End Class

    Public NotInheritable Class CommandCreateClient
        Inherits TemplatedCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="CreateClient",
                       template:="name -profile=default -auto",
                       Description:="Creates a new bnet client. -Auto causes the client to automatically advertising any games hosted by the bot.",
                       Permissions:="root:4")
        End Sub
        Protected Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Contract.Assume(target IsNot Nothing)
            Dim profileName = If(argument.TryGetOptionalNamedValue("profile"), "default").ToInvariant
            Dim clientName = argument.RawValue(0).ToInvariant

            Dim manager = Await Bnet.ClientManager.AsyncCreateFromProfile(clientName, profileName, target)
            Try
                Await target.Components.QueueAddComponent(manager)
                If argument.HasOptionalSwitch("auto") Then manager.QueueSetAutomatic(True)
                Return "Created Client"
            Catch ex As Exception
                manager.Dispose()
                Throw
            End Try
        End Function
    End Class

    Public NotInheritable Class CommandCreateLan
        Inherits TemplatedCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="CreateLan",
                       template:="name -receiver=localhost -manual",
                       Description:="Creates a lan advertiser. -Manual causes the advertiser to not automatically advertise any games hosted by the bot.",
                       Permissions:="root:4")
        End Sub
        Protected Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Contract.Assume(target IsNot Nothing)
            Dim name = argument.RawValue(0)
            Dim remoteHost = If(argument.TryGetOptionalNamedValue("receiver"), "localhost")
            Dim auto = Not argument.HasOptionalSwitch("manual")

            Dim advertiser = New Lan.Advertiser(defaultTargetHost:=If(argument.TryGetOptionalNamedValue("receiver"), "localhost"))
            Dim manager = New Lan.AdvertiserManager(name, target, advertiser)
            Try
                If auto Then manager.QueueSetAutomatic(auto)
                Await target.Components.QueueAddComponent(manager)
                Return "Created lan advertiser."
            Catch ex As Exception
                manager.Dispose()
                Throw
            End Try
        End Function
    End Class

    Public NotInheritable Class CommandDispose
        Inherits TemplatedCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="Dispose",
                       template:="type:name",
                       Description:="Disposes a bot component.",
                       Permissions:="root:5")
        End Sub
        Protected Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Contract.Assume(target IsNot Nothing)
            'parse
            Dim args = argument.RawValue(0).Split(":"c)
            If args.Length <> 2 Then Throw New ArgumentException("Expected a component argument like: type:name.")
            Dim type = args(0).ToInvariant
            Dim name = args(1).AssumeNotNull.ToInvariant
            'dispose
            Dim component = Await target.Components.QueueFindComponent(type, name)
            component.Dispose()
            Return "Disposed {0}".Frmt(argument.RawValue(0))
        End Function
    End Class

    Public NotInheritable Class CommandGet
        Inherits TemplatedCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="Get",
                       template:="setting",
                       Description:="Returns a global setting's value {tickperiod, laglimit, commandprefix, gamerate}.",
                       Permissions:="root:1")
        End Sub
        Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Dim argSetting = argument.RawValue(0).ToInvariant

            Dim settingValue As Object
            Select Case argSetting
                Case "TickPeriod" : settingValue = My.Settings.game_tick_period
                Case "LagLimit" : settingValue = My.Settings.game_lag_limit
                Case "CommandPrefix" : settingValue = My.Settings.commandPrefix
                Case "GameRate" : settingValue = My.Settings.game_speed_factor
                Case Else : Throw New ArgumentException("Unrecognized setting '{0}'.".Frmt(argSetting))
            End Select
            Return "{0} = '{1}'".Frmt(argSetting, settingValue).AsTask
        End Function
    End Class

    Public NotInheritable Class CommandHost
        Inherits TemplatedCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="Host",
                       template:=Concat({"name=<game name>", "map=<search query>"},
                                        WC3.GameSettings.PartialArgumentTemplates,
                                        WC3.GameStats.PartialArgumentTemplates).StringJoin(" "),
                       Description:="Creates a server of a game and advertises it on lan. More help topics under 'Help Host *'.",
                       Permissions:="games:1",
                       extraHelp:=Concat(WC3.GameSettings.PartialArgumentHelp, WC3.GameStats.PartialArgumentHelp).StringJoin(Environment.NewLine))
        End Sub
        <ContractVerification(False)>
        Protected Overloads Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Dim server = Await target.QueueGetOrConstructGameServer()
            Dim gameSet = Await server.QueueAddGameFromArguments(argument, user)
            Dim name = gameSet.GameSettings.GameDescription.Name
            Dim path = gameSet.GameSettings.GameDescription.GameStats.AdvertisedPath
            Return "Hosted game '{0}' for map '{1}'.".Frmt(name, path)
        End Function
    End Class

    Public NotInheritable Class CommandListComponents
        Inherits TemplatedCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="Components",
                       template:="-type=type",
                       Description:="Lists all bot components. Use -type= to filter by component type.",
                       Permissions:="root:1")
        End Sub
        Protected Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Contract.Assume(target IsNot Nothing)
            Dim typeFilter = argument.TryGetOptionalNamedValue("type")
            If typeFilter Is Nothing Then
                Dim components = Await target.Components.QueueGetAllComponents()
                Return "Components: {0}.".Frmt((From component In components
                                                Select "{0}:{1}".Frmt(component.Type, component.Name)
                                                ).StringJoin(", "))
            Else
                Dim components = Await target.Components.QueueGetAllComponents()
                Return "{0} Components: {1}.".Frmt(typeFilter, (From component In components
                                                                Where component.Type = typeFilter
                                                                Select component.Name
                                                                ).StringJoin(", "))
            End If
        End Function
    End Class

    Public NotInheritable Class CommandLoadPlugin
        Inherits TemplatedCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="LoadPlugin",
                       template:="name",
                       Description:="Loads the named plugin.",
                       Permissions:="root:5")
        End Sub
        Protected Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Contract.Assume(target IsNot Nothing)
            Dim profile = (From p In target.Settings.PluginProfiles Where p.name = argument.RawValue(0)).FirstOrDefault
            If profile Is Nothing Then Throw New InvalidOperationException("No such plugin profile.")
            Dim socket = New Plugins.Socket(profile.name, target, profile.location)
            Dim manager = New Plugins.PluginManager(socket)
            Try
                Await target.Components.QueueAddComponent(manager)
                Return "Loaded plugin. Description: {0}".Frmt(socket.Plugin.Description)
            Catch ex As Exception
                manager.Dispose()
                Throw
            End Try
        End Function
    End Class

    Public NotInheritable Class CommandSet
        Inherits TemplatedCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="Set",
                       template:="setting value",
                       Description:="Sets a global setting {tickperiod, laglimit, commandprefix, gamerate}.",
                       Permissions:="root:2")
        End Sub
        Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As Task(Of String)
            Dim argSetting = argument.RawValue(0).ToInvariant
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
            Return "{0} set to {1}".Frmt(argSetting, argValue).AsTask
        End Function
    End Class

    Public NotInheritable Class CommandTo
        Inherits PartialCommand(Of MainBot)
        Public Sub New()
            MyBase.New(Name:="To",
                       headtype:="type:name",
                       Description:="Forwards commands to the named component.",
                       Permissions:="root:3")
        End Sub
        Protected Overrides Async Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As Task(Of String)
            Contract.Assume(target IsNot Nothing)
            'parse
            Dim args = argumentHead.Split(":"c)
            If args.Length <> 2 Then Throw New ArgumentException("Expected a component type:name.")
            Contract.Assume(args(1) IsNot Nothing)
            Dim type = args(0).ToInvariant
            Dim name = args(1).ToInvariant
            'send
            Dim component = Await target.Components.QueueFindComponent(type, name)
            Return Await component.InvokeCommand(user, argumentRest)
        End Function
    End Class
End Namespace
