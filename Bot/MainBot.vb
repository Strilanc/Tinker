''Tinker - Warcraft 3 game hosting bot
''Copyright (C) 2009 Craig Gidney
''
''This program is free software: you can redistribute it and/or modify
''it under the terms of the GNU General Public License as published by
''the Free Software Foundation, either version 3 of the License, or
''(at your option) any later version.
''
''This program is distributed in the hope that it will be useful,
''but WITHOUT ANY WARRANTY; without even the implied warranty of
''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''GNU General Public License for more details.
''You should have received a copy of the GNU General Public License
''along with this program.  If not, see http://www.gnu.org/licenses/

Imports Tinker.Commands
Imports Tinker.Components

Namespace Bot
    Public NotInheritable Class MainBot
        Inherits DisposableWithTask

        Public Shared ReadOnly TriggerCommandText As InvariantString = "?trigger"

        Private ReadOnly _settings As New Bot.Settings()
        Private ReadOnly _portPool As New PortPool()
        Private ReadOnly _logger As Logger
        Private ReadOnly _components As New ComponentSet()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_settings IsNot Nothing)
            Contract.Invariant(_portPool IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_components IsNot Nothing)
        End Sub

        Public Sub New(Optional logger As Logger = Nothing)
            Me._logger = If(logger, New Logger)
        End Sub

        Public ReadOnly Property PortPool As PortPool
            Get
                Contract.Ensures(Contract.Result(Of PortPool)() IsNot Nothing)
                Return _portPool
            End Get
        End Property
        Public ReadOnly Property Logger As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property
        Public ReadOnly Property Components As ComponentSet
            Get
                Contract.Ensures(Contract.Result(Of ComponentSet)() IsNot Nothing)
                Return _components
            End Get
        End Property
        Public ReadOnly Property Settings As Bot.Settings
            Get
                Contract.Ensures(Contract.Result(Of Bot.Settings)() IsNot Nothing)
                Return _settings
            End Get
        End Property

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            _components.Dispose()
            Return _components.DisposalTask
        End Function
    End Class

    Public Module BotExtensions
        <Extension()>
        Public Async Function InvokeCommand(this As MainBot, user As BotUser, argument As String) As Task(Of String)
            Contract.Assume(this IsNot Nothing)
            Contract.Assume(argument IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing) 'Incompability between AsyncCTP and code contracts
            Dim components = (Await this.Components.QueueGetAllComponents()).OfType(Of MainBotManager)()
            Return Await components.Single.InvokeCommand(user, argument)
        End Function
        <Extension()>
        Public Function QueueGetOrConstructGameServer(this As MainBot, clockToUseIfConstructing As IClock) As Task(Of WC3.GameServerManager)
            Contract.Requires(this IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of WC3.GameServerManager))() IsNot Nothing)
            Return this.Components.QueueFindOrElseConstructComponent(Of WC3.GameServerManager)(
                factory:=Function() New WC3.GameServerManager("Auto", New WC3.GameServer(clockToUseIfConstructing), this))
        End Function
        <Extension()>
        Public Async Function ObserveGameSets(this As MainBot,
                                              adder As Action(Of WC3.GameServer, WC3.GameSet),
                                              remover As Action(Of WC3.GameServer, WC3.GameSet)) As Task(Of IDisposable)
            Contract.Assume(this IsNot Nothing)
            Contract.Assume(adder IsNot Nothing)
            Contract.Assume(remover IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)

            Dim inQueue = MakeTaskedCallQueue()
            Dim hooks = New List(Of Task(Of IDisposable))
            Dim viewHook = Await this.Components.ObserveComponentsOfType(Of WC3.GameServerManager)(
                adder:=Sub(manager) inQueue.QueueAction(
                    Sub()
                        If hooks Is Nothing Then Return
                        Dim gameSetLink = manager.Server.ObserveActiveGameSets(
                                 adder:=Sub(gameSet) adder(manager.Server, gameSet),
                                 remover:=Sub(gameSet) remover(manager.Server, gameSet))
                        'async remove when view is disposed
                        hooks.Add(gameSetLink)
                        'async auto-remove when server is disposed
                        Call {gameSetLink, manager.Server.DisposalTask}.AsAggregateTask.QueueContinueWithAction(inQueue, Sub() gameSetLink.Result.Dispose()).IgnoreExceptions()
                    End Sub),
                remover:=Sub(server)
                             'no action needed
                         End Sub)

            Return New DelegatedDisposable(Sub() inQueue.QueueAction(
                Sub()
                    hooks.DisposeAllAsync()
                    viewHook.Dispose()
                    hooks = Nothing
                End Sub))
        End Function

        ''' <summary>
        ''' Adds a command to all present and future components of the given type.
        ''' Removes the commands when the result is disposed.
        ''' </summary>
        <Extension()>
        Public Function IncludeCommandInAllComponentsOfType(Of T As IBotComponent)(this As MainBot, command As ICommand(Of T)) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(command IsNot Nothing)
            Dim weakCommand = command.ProjectedFrom(Function(x As IBotComponent) DirectCast(x, T))

            Dim inQueue = MakeTaskedCallQueue()
            Dim commandDisposers = New Dictionary(Of T, Task(Of IDisposable))()

            'Include commands
            Dim view = this.Components.ObserveComponentsOfType(Of T)(
                adder:=Sub(component) inQueue.QueueAction(
                    Sub()
                        If commandDisposers Is Nothing Then Return
                        commandDisposers.Add(component, component.IncludeCommand(weakCommand))
                    End Sub),
                remover:=Sub(component) inQueue.QueueAction(
                    Sub()
                        If commandDisposers Is Nothing Then Return
                        commandDisposers.Remove(component)
                    End Sub))

            'Dispose commands when disposed
            Return New DelegatedDisposable(Sub() inQueue.QueueAction(
                Sub()
                    commandDisposers.Values.Append(view).DisposeAllAsync()
                    commandDisposers = Nothing
                End Sub))
        End Function

        <Extension()>
        Public Function IncludeCommandsInAllComponentsOfType(Of T As IBotComponent)(this As MainBot, commands As IEnumerable(Of ICommand(Of T))) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(commands IsNot Nothing)
            Dim weakCommands = (From command In commands
                                Select command.ProjectedFrom(Function(x As IBotComponent) DirectCast(x, T))
                                ).ToArray()

            Dim inQueue = MakeTaskedCallQueue()
            Dim commandDisposers = New Dictionary(Of T, Task(Of IDisposable))()

            'Include commands
            Dim view = this.Components.ObserveComponentsOfType(Of T)(
                adder:=Sub(component) inQueue.QueueAction(
                    Sub()
                        If commandDisposers Is Nothing Then Return
                        commandDisposers.Add(component, component.IncludeAllCommands(weakCommands))
                    End Sub),
                remover:=Sub(component) inQueue.QueueAction(
                    Sub()
                        If commandDisposers Is Nothing Then Return
                        commandDisposers.Remove(component)
                    End Sub))

            'Dispose commands when disposed
            Return New DelegatedDisposable(Sub() inQueue.QueueAction(
                Sub()
                    commandDisposers.Values.Append(view).DisposeAllAsync()
                    commandDisposers = Nothing
                End Sub))
        End Function

        <Extension()>
        Public Function IncludeBasicBotCommands(this As MainBot, clock As IClock) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Dim conv = Function(x As MainBotManager) x.Bot
            Return this.IncludeCommandsInAllComponentsOfType(Of Bot.MainBotManager)(
                From command In New ICommand(Of MainBot)() {
                    New GenericCommands.CommandFindMaps(Of MainBot),
                    New GenericCommands.CommandDownloadMap(Of MainBot),
                    New GenericCommands.CommandRecacheIP(Of MainBot),
                    New Bot.Commands.CommandConnect(clock),
                    New Bot.Commands.CommandCreateAdminGame(clock),
                    New Bot.Commands.CommandCreateCKL(clock),
                    New Bot.Commands.CommandCreateClient(clock),
                    New Bot.Commands.CommandCreateLan(clock),
                    New Bot.Commands.CommandDispose(),
                    New Bot.Commands.CommandGet(),
                    New Bot.Commands.CommandHost(clock),
                    New Bot.Commands.CommandListComponents(),
                    New Bot.Commands.CommandLoadPlugin(),
                    New Bot.Commands.CommandSet(),
                    New Bot.Commands.CommandTo()
                } Select command.ProjectedFrom(conv)
            )
        End Function
        <Extension()>
        Public Function IncludeBasicBnetClientCommands(this As MainBot, clock As IClock) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Dim conv = Function(x As Bnet.ClientComponent) x.Client
            Return this.IncludeCommandsInAllComponentsOfType(Of Bnet.ClientComponent)(
                Concat(
                    New ICommand(Of Bnet.ClientComponent)() {
                        New Bot.GenericCommands.CommandFindMaps(Of Bnet.ClientComponent),
                        New Bot.GenericCommands.CommandDownloadMap(Of Bnet.ClientComponent),
                        New Bnet.Commands.CommandBot,
                        New Bnet.Commands.CommandAdminCode,
                        New Bnet.Commands.CommandCancelHost,
                        New Bnet.Commands.CommandElevate,
                        New Bnet.Commands.CommandGame,
                        New Bnet.Commands.CommandHost(clock),
                        New Bnet.Commands.CommandAuto
                    },
                    From command In New ICommand(Of Bnet.Client)() {
                        New Bnet.Commands.CommandAddUser,
                        New Bnet.Commands.CommandDemote,
                        New Bnet.Commands.CommandRemoveUser,
                        New Bnet.Commands.CommandDisconnect,
                        New Bnet.Commands.CommandPromote,
                        New Bnet.Commands.CommandUser,
                        New Bnet.Commands.CommandConnect,
                        New Bnet.Commands.CommandLogOn,
                        New Bnet.Commands.CommandSay,
                        New Bnet.Commands.CommandCancelAllHost,
                        New Bnet.Commands.CommandRefreshGamesList
                    } Select command.ProjectedFrom(conv)
                )
            )
        End Function
        <Extension()>
        Public Function IncludeBasicLanAdvertiserCommands(this As MainBot, clock As IClock) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Dim conv = Function(x As Lan.UDPAdvertiserComponent) x.Advertiser
            Return this.IncludeCommandsInAllComponentsOfType(Of Lan.UDPAdvertiserComponent)(
                Concat(
                    New ICommand(Of Lan.UDPAdvertiserComponent)() {
                        New Lan.Commands.CommandAuto,
                        New Lan.Commands.CommandHost(clock)
                    },
                    From command In New ICommand(Of Lan.UDPAdvertiser)() {
                        New Lan.Commands.CommandAdd(clock),
                        New Lan.Commands.CommandRemove
                    } Select command.ProjectedFrom(conv)
                )
            )
        End Function
        <Extension()>
        Public Function IncludeBasicGameServerCommands(this As MainBot) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return this.IncludeCommandsInAllComponentsOfType(Of WC3.GameServerManager)(
                New ICommand(Of WC3.GameServerManager)() {
                    New WC3.ServerCommands.CommandAddGame
                }
            )
        End Function
        <Extension()>
        Public Function IncludeBasicCKLServerCommands(this As MainBot) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Dim conv = Function(x As CKL.ServerManager) x.Server
            Return this.IncludeCommandsInAllComponentsOfType(Of CKL.ServerManager)(
                From command In New ICommand(Of CKL.Server)() {
                    New CKL.ServerCommands.CommandAddKey,
                    New CKL.ServerCommands.CommandRemoveKey
                } Select command.ProjectedFrom(conv)
            )
        End Function
    End Module
End Namespace
