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

Namespace Bot
    Public NotInheritable Class MainBot
        Inherits FutureDisposable

        Public Shared ReadOnly TriggerCommandText As InvariantString = "?trigger"

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue

        Private ReadOnly _settings As New Bot.Settings()
        Private ReadOnly _portPool As New PortPool()
        Private ReadOnly _logger As Logger
        Private ReadOnly _components As New Components.ComponentSet()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
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
        Public ReadOnly Property Components As Components.ComponentSet
            Get
                Contract.Ensures(Contract.Result(Of Components.ComponentSet)() IsNot Nothing)
                Return _components
            End Get
        End Property
        Public ReadOnly Property Settings As Bot.Settings
            Get
                Contract.Ensures(Contract.Result(Of Bot.Settings)() IsNot Nothing)
                Return _settings
            End Get
        End Property

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(Sub() _components.Dispose())
        End Function
    End Class

    Public Module BotExtensions
        <Extension()>
        Public Function QueueGetOrConstructGameServer(ByVal this As MainBot) As IFuture(Of WC3.GameServerManager)
            Contract.Requires(this IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of WC3.GameServerManager))() IsNot Nothing)
            Return this.Components.QueueGetOrConstructComponent(Of WC3.GameServerManager)(
                factory:=Function() New WC3.GameServerManager("Auto", New WC3.GameServer, this))
        End Function
        <Extension()>
        Public Function QueueCreateActiveGameSetsAsyncView(ByVal this As MainBot,
                                                           ByVal adder As Action(Of MainBot, WC3.GameServer, WC3.GameSet),
                                                           ByVal remover As Action(Of MainBot, WC3.GameServer, WC3.GameSet)) As IFuture(Of IDisposable)
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)

            Dim inQueue = New StartableCallQueue(New TaskedCallQueue())
            Dim hooks = New List(Of IFuture(Of IDisposable))
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
                               Call {gameSetLink, manager.Server.FutureDisposed}.Defuturized.QueueCallOnSuccess(inQueue, Sub() gameSetLink.Value.Dispose()).SetHandled()
                           End Sub),
                remover:=Sub(sender, server)
                             'no action needed
                         End Sub)
            hooks.Add(viewHook)

            inQueue.Start()
            Return viewHook.Select(Function() New DelegatedDisposable(Sub() inQueue.QueueAction(
                Sub()
                    If hooks Is Nothing Then Return
                    For Each hook In hooks
                        hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
                    Next hook
                    hooks = Nothing
                End Sub)))
        End Function
    End Module
End Namespace
