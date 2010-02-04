Imports Tinker.Commands

Namespace Bot
    Public NotInheritable Class BotCommands
        Inherits CommandSet(Of MainBot)
        Public Sub New()
            AddCommand(New CTo)
            AddCommand(New CListComponents)
            AddCommand(New CConnect)
            AddCommand(New CCreateCKL)
            AddCommand(New CCreateClient)
            AddCommand(New GenericCommands.CommandFindMaps(Of MainBot))
            AddCommand(New GenericCommands.CommandDownloadMap(Of MainBot))
            AddCommand(New GenericCommands.CommandRecacheIP(Of MainBot))
            AddCommand(New CDispose)
            AddCommand(New CLoadPlugin)
            AddCommand(New CGet)
            AddCommand(New CSet)
            AddCommand(New CCreateAdminGame)
            AddCommand(New CCreateLan)
        End Sub

        Private NotInheritable Class CListComponents
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="Components",
                           template:="-type=type",
                           Description:="Lists all bot components. Use -type= to filter by component type.",
                           Permissions:="root:1")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim typeFilter = argument.TryGetOptionalNamedValue("type")
                If typeFilter Is Nothing Then
                    Return From components In target.Components.QueueGetAllComponents()
                           Select "Components: {0}.".Frmt((From component In components
                                                           Select "{0}:{1}".Frmt(component.Type, component.Name)
                                                          ).StringJoin(", "))
                Else
                    Return From components In target.Components.QueueGetAllComponents()
                           Select "{0} Components: {1}.".Frmt(typeFilter, (From component In components
                                                                           Where component.Type = typeFilter
                                                                           Select component.Name
                                                                           ).StringJoin(", "))
                End If
            End Function
        End Class

        Private NotInheritable Class CGet
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="Get",
                           template:="setting",
                           Description:="Returns a global setting's value {tickperiod, laglimit, commandprefix, gamerate}.",
                           Permissions:="root:1")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
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
            End Function
        End Class

        Private NotInheritable Class CSet
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="Set",
                           template:="setting value",
                           Description:="Sets a global setting {tickperiod, laglimit, commandprefix, gamerate}.",
                           Permissions:="root:2")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
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
            End Function
        End Class

        Private NotInheritable Class CCreateAdminGame
            Inherits TemplatedCommand(Of MainBot)

            Public Sub New()
                MyBase.New(Name:="CreateAdminGame",
                           template:="name password=x",
                           Description:="Picks or creates a game server, and adds an admin game to it.",
                           Permissions:="local:1",
                           hasPrivateArguments:=True)
            End Sub

            Protected Overloads Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As Commands.CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim name = argument.RawValue(0)
                Dim password = argument.NamedValue("password")
                Return From server In target.QueueGetOrConstructGameServer()
                       Select server.QueueAddAdminGame(name, password)
                       Select "Added admin game to server. Use Lan Advertiser on auto to advertise it."
            End Function
        End Class

        Private NotInheritable Class CCreateLan
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="CreateLan",
                           template:="name -receiver=localhost -manual",
                           Description:="Creates a lan advertiser. -Manual causes the advertiser to not automatically advertise any games hosted by the bot.",
                           Permissions:="root:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim name = argument.RawValue(0)
                Dim remoteHost = If(argument.TryGetOptionalNamedValue("receiver"), "localhost")
                Dim auto = Not argument.HasOptionalSwitch("manual")

                Dim advertiser = New Lan.Advertiser(defaultTargetHost:=If(argument.TryGetOptionalNamedValue("receiver"), "localhost"))
                Dim manager = New Lan.AdvertiserManager(name, target, advertiser)
                If auto Then manager.QueueSetAutomatic(auto)
                Dim finished = target.Components.QueueAddComponent(manager)
                finished.Catch(Sub() manager.Dispose())
                Return finished.EvalOnSuccess(Function() "Created lan advertiser.")
            End Function
        End Class

        Private NotInheritable Class CTo
            Inherits PartialCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="To",
                           headtype:="type:name",
                           Description:="Forwards commands to the named component.",
                           Permissions:="root:3")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argumentHead As String, ByVal argumentRest As String) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                'parse
                Dim args = argumentHead.Split(":"c)
                If args.Length <> 2 Then Throw New ArgumentException("Expected a component type:name.")
                Contract.Assume(args(1) IsNot Nothing)
                Dim type As InvariantString = args(0)
                Dim name As InvariantString = args(1)
                'send
                Return (From component In target.Components.QueueFindComponent(type, name)
                        Select component.InvokeCommand(user, argumentRest)
                       ).Defuturized()
            End Function
        End Class

        Private NotInheritable Class CCreateClient
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="CreateClient",
                           template:="name -profile=default -auto",
                           Description:="Creates a new bnet client. -Auto causes the client to automatically advertising any games hosted by the bot.",
                           Permissions:="root:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim profileName As InvariantString = If(argument.TryGetOptionalNamedValue("profile"), "default")
                Dim clientName As InvariantString = argument.RawValue(0)

                Return Bnet.ClientManager.AsyncCreateFromProfile(clientName, profileName, target).Select(
                    Function(manager)
                        Dim added = target.Components.QueueAddComponent(manager)
                        added.Catch(Sub() manager.Dispose())
                        If argument.HasOptionalSwitch("auto") Then manager.QueueSetAutomatic(True)
                        Return added.EvalOnSuccess(Function() "Created Client")
                    End Function).Defuturized
            End Function
        End Class

        Private NotInheritable Class CDispose
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="Dispose",
                           template:="type:name",
                           Description:="Disposes a bot component.",
                           Permissions:="root:5")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                'parse
                Dim args = argument.RawValue(0).Split(":"c)
                If args.Length <> 2 Then Throw New ArgumentException("Expected a component argument like: type:name.")
                Dim type As InvariantString = args(0)
                Dim name As InvariantString = args(1).AssumeNotNull
                'dispose
                Return target.Components.QueueFindComponent(type, name).Select(
                    Function(component)
                        component.Dispose()
                        Return "Disposed {0}".Frmt(argument.RawValue(0))
                    End Function)
            End Function
        End Class

        Private NotInheritable Class CLoadPlugin
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="LoadPlugin",
                           template:="name",
                           Description:="Loads the named plugin.",
                           Permissions:="root:5")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As CommandArgument) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim profile = (From p In target.Settings.PluginProfiles Where p.name = argument.RawValue(0)).FirstOrDefault
                If profile Is Nothing Then Throw New InvalidOperationException("No such plugin profile.")
                Dim socket = New Plugins.Socket(profile.name, target, profile.location)
                Dim manager = New Plugins.PluginManager(socket)
                Dim added = target.Components.QueueAddComponent(manager)
                added.Catch(Sub() manager.Dispose())
                Return added.EvalOnSuccess(Function() "Loaded plugin. Description: {0}".Frmt(socket.Plugin.Description))
            End Function
        End Class

        Private NotInheritable Class CConnect
            Inherits Command(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="Connect",
                           Format:="profile1 profile2 ...",
                           Description:="Creates and connects bnet clients, using the given profiles. All of the clients will be set to automatic hosting.",
                           Permissions:="root:4")
            End Sub
            Protected Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As String) As IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim profileNames = (From word In argument.Split(" "c) Where word <> "").ToArray
                If profileNames.Length = 0 Then Throw New ArgumentException("No profiles specified.")

                'Attempt to connect to each listed profile
                Dim futureManagers = New List(Of IFuture(Of Bnet.ClientManager))(capacity:=profileNames.Count)
                For Each profileName In profileNames
                    Contract.Assume(profileName IsNot Nothing)
                    'Create and connect
                    Dim futureManager = Bnet.ClientManager.AsyncCreateFromProfile(profileName, profileName, target)
                    futureManager.CallOnValueSuccess(Sub(manager) manager.QueueSetAutomatic(True)).SetHandled()
                    Dim futureAdded = (From manager In futureManager Select target.Components.QueueAddComponent(manager)).Defuturized
                    Dim futureClient = futureAdded.EvalOnSuccess(Function() futureManager.Value.Client)
                    Dim futureConnected = (From client In futureClient
                                           Select client.QueueConnectAndLogOn(
                                                            remoteHost:=client.Profile.server.Split(" "c)(0),
                                                            credentials:=New Bnet.ClientCredentials(client.Profile.userName, client.Profile.password))
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

        Private NotInheritable Class CCreateCKL
            Inherits TemplatedCommand(Of MainBot)
            Public Sub New()
                MyBase.New(Name:="CreateCKL",
                           Description:="Starts a CD Key Lending server that others can connect to and use to logon to bnet. This will NOT allow others to learn your cd keys, but WILL allow them to logon with your keys ONCE.",
                           template:="name",
                           Permissions:="root:5")
            End Sub
            Protected Overloads Overrides Function PerformInvoke(ByVal target As MainBot, ByVal user As BotUser, ByVal argument As Commands.CommandArgument) As Strilbrary.Threading.IFuture(Of String)
                Contract.Assume(target IsNot Nothing)
                Dim port = target.PortPool.TryAcquireAnyPort()
                If port Is Nothing Then Throw New OperationFailedException("No available ports in the pool.")
                Dim name = argument.RawValue(0)
                Dim server = New CKL.Server(name:=name,
                                            listenport:=port,
                                            clock:=New SystemClock())
                Dim manager = New CKL.ServerManager(server)
                Dim finished = target.Components.QueueAddComponent(manager)
                finished.Catch(Sub() manager.Dispose())
                Return finished.EvalOnSuccess(Function() "Added CKL server {0}".Frmt(name))
            End Function
        End Class
    End Class
End Namespace
