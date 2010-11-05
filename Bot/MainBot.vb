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

        Public Sub New(Optional ByVal logger As Logger = Nothing)
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

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            _components.Dispose()
            Return _components.DisposalTask
        End Function
    End Class

    Public Module BotExtensions
        <Extension()>
        <ContractVerification(False)>
        Public Async Function InvokeCommand(ByVal this As MainBot, ByVal user As BotUser, ByVal argument As String) As Task(Of String)
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Dim components = Await this.Components.QueueGetAllComponents(Of MainBotManager)()
            Return Await components.Single.InvokeCommand(user, argument)
        End Function
        <Extension()>
        Public Function QueueGetOrConstructGameServer(ByVal this As MainBot) As Task(Of WC3.GameServerManager)
            Contract.Requires(this IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of WC3.GameServerManager))() IsNot Nothing)
            Return this.Components.QueueFindOrElseConstructComponent(Of WC3.GameServerManager)(
                factory:=Function() New WC3.GameServerManager("Auto", New WC3.GameServer(New SystemClock()), this))
        End Function
        <Extension()>
        Public Async Function QueueCreateActiveGameSetsAsyncView(ByVal this As MainBot,
                                                                 ByVal adder As Action(Of MainBot, WC3.GameServer, WC3.GameSet),
                                                                 ByVal remover As Action(Of MainBot, WC3.GameServer, WC3.GameSet)) As Task(Of IDisposable)
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)

            Dim inQueue = New TaskedCallQueue(initiallyStarted:=False)
            Dim hooks = New List(Of Task(Of IDisposable))
            Dim viewHook = this.Components.QueueCreateAsyncView(Of WC3.GameServerManager)(
                adder:=Sub(sender, manager) inQueue.QueueAction(
                    Sub()
                        If hooks Is Nothing Then Return
                        Dim gameSetLink = manager.Server.QueueCreateActiveGameSetsAsyncView(
                                 adder:=Sub(sender2, gameSet) adder(this, sender2, gameSet),
                                 remover:=Sub(sender2, gameSet) remover(this, sender2, gameSet))
                        'async remove when view is disposed
                        hooks.Add(gameSetLink)
                        'async auto-remove when server is disposed
                        Call {gameSetLink, manager.Server.DisposalTask}.AsAggregateTask.QueueContinueWithAction(inQueue, Sub() gameSetLink.Result.Dispose()).IgnoreExceptions()
                    End Sub),
                remover:=Sub(sender, server)
                             'no action needed
                         End Sub)
            hooks.Add(viewHook)

            inQueue.Start()
            Await viewHook
            Return New DelegatedDisposable(Sub() inQueue.QueueAction(
                Sub()
                    hooks.DisposeAllAsync()
                    hooks = Nothing
                End Sub))
        End Function

        ''' <summary>
        ''' Adds a command to all present and future components of the given type.
        ''' Removes the commands when the result is disposed.
        ''' </summary>
        <Extension()>
        Public Function IncludeCommandInAllComponentsOfType(Of T As IBotComponent)(ByVal this As MainBot, ByVal command As ICommand(Of T)) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(command IsNot Nothing)
            Dim weakCommand = command.ProjectedFrom(Function(x As IBotComponent) DirectCast(x, T))

            Dim inQueue = New TaskedCallQueue()
            Dim commandDisposers = New Dictionary(Of T, Task(Of IDisposable))()

            'Include commands
            Dim view = this.Components.QueueCreateAsyncView(Of T)(
                adder:=Sub(bot, component) inQueue.QueueAction(
                    Sub()
                        If commandDisposers Is Nothing Then Return
                        commandDisposers.Add(component, component.IncludeCommand(weakCommand))
                    End Sub),
                remover:=Sub(bot, component) inQueue.QueueAction(
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
        Public Function IncludeCommandsInAllComponentsOfType(Of T As IBotComponent)(ByVal this As MainBot, ByVal commands As IEnumerable(Of ICommand(Of T))) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(commands IsNot Nothing)
            Dim weakCommands = (From command In commands
                                Select command.ProjectedFrom(Function(x As IBotComponent) DirectCast(x, T))
                                ).ToArray()

            Dim inQueue = New TaskedCallQueue()
            Dim commandDisposers = New Dictionary(Of T, Task(Of IDisposable))()

            'Include commands
            Dim view = this.Components.QueueCreateAsyncView(Of T)(
                adder:=Sub(bot, component) inQueue.QueueAction(
                    Sub()
                        If commandDisposers Is Nothing Then Return
                        commandDisposers.Add(component, component.IncludeAllCommands(weakCommands))
                    End Sub),
                remover:=Sub(bot, component) inQueue.QueueAction(
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
        Public Function IncludeBasicBotCommands(ByVal this As MainBot) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Dim conv = Function(x As MainBotManager) x.Bot
            Return this.IncludeCommandsInAllComponentsOfType(Of Bot.MainBotManager)(
                From command In New ICommand(Of MainBot)() {
                    New GenericCommands.CommandFindMaps(Of MainBot),
                    New GenericCommands.CommandDownloadMap(Of MainBot),
                    New GenericCommands.CommandRecacheIP(Of MainBot),
                    New Bot.Commands.CommandConnect(),
                    New Bot.Commands.CommandCreateAdminGame(),
                    New Bot.Commands.CommandCreateCKL(),
                    New Bot.Commands.CommandCreateClient(),
                    New Bot.Commands.CommandCreateLan(),
                    New Bot.Commands.CommandDispose(),
                    New Bot.Commands.CommandGet(),
                    New Bot.Commands.CommandHost(),
                    New Bot.Commands.CommandListComponents(),
                    New Bot.Commands.CommandLoadPlugin(),
                    New Bot.Commands.CommandSet(),
                    New Bot.Commands.CommandTo()
                } Select command.ProjectedFrom(conv)
            )
        End Function
        <Extension()>
        Public Function IncludeBasicBnetClientCommands(ByVal this As MainBot) As IDisposable
            Contract.Requires(this IsNot Nothing)
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
                        New Bnet.Commands.CommandHost,
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
        Public Function IncludeBasicLanAdvertiserCommands(ByVal this As MainBot) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Dim conv = Function(x As Lan.AdvertiserManager) x.Advertiser
            Return this.IncludeCommandsInAllComponentsOfType(Of Lan.AdvertiserManager)(
                Concat(
                    New ICommand(Of Lan.AdvertiserManager)() {
                        New Lan.Commands.CommandAuto,
                        New Lan.Commands.CommandHost
                    },
                    From command In New ICommand(Of Lan.Advertiser)() {
                        New Lan.Commands.CommandAdd,
                        New Lan.Commands.CommandRemove
                    } Select command.ProjectedFrom(conv)
                )
            )
        End Function
        <Extension()>
        Public Function IncludeBasicGameServerCommands(ByVal this As MainBot) As IDisposable
            Contract.Requires(this IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return this.IncludeCommandsInAllComponentsOfType(Of WC3.GameServerManager)(
                New ICommand(Of WC3.GameServerManager)() {
                    New WC3.ServerCommands.CommandAddGame
                }
            )
        End Function
        <Extension()>
        Public Function IncludeBasicCKLServerCommands(ByVal this As MainBot) As IDisposable
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
