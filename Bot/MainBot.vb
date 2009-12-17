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

Public NotInheritable Class MainBot
    Inherits FutureDisposable

    Public Shared ReadOnly TriggerCommandText As InvariantString = "?trigger"

    Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
    Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue

    Private _clientProfiles As IEnumerable(Of ClientProfile) = New List(Of ClientProfile)
    Private _pluginProfiles As IEnumerable(Of Plugins.PluginProfile) = New List(Of Plugins.PluginProfile)
    Private ReadOnly _portPool As PortPool
    Private ReadOnly _logger As Logger
    Private ReadOnly _components As New Components.ComponentSet()

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(inQueue IsNot Nothing)
        Contract.Invariant(outQueue IsNot Nothing)
        Contract.Invariant(_portPool IsNot Nothing)
        Contract.Invariant(_components IsNot Nothing)
        Contract.Invariant(_clientProfiles IsNot Nothing)
        Contract.Invariant(_pluginProfiles IsNot Nothing)
    End Sub

    Public Sub New(ByVal portPool As PortPool,
                   Optional ByVal logger As Logger = Nothing)
        Contract.Requires(portPool IsNot Nothing)
        Me._portPool = portPool
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

    Public Function QueueUpdateProfiles(ByVal clientProfiles As IEnumerable(Of ClientProfile), ByVal pluginProfiles As IEnumerable(Of Plugins.PluginProfile)) As IFuture
        Contract.Requires(clientProfiles IsNot Nothing)
        Contract.Requires(pluginProfiles IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return inQueue.QueueAction(Sub()
                                       Me._clientProfiles = clientProfiles.ToList
                                       Me._pluginProfiles = pluginProfiles.ToList
                                   End Sub)
    End Function
    Public Function QueueGetClientProfiles() As IFuture(Of IList(Of ClientProfile))
        Contract.Ensures(Contract.Result(Of IFuture(Of IList(Of ClientProfile)))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() _clientProfiles.ToList)
    End Function
    Public Function QueueGetPluginProfiles() As IFuture(Of IList(Of Plugins.PluginProfile))
        Contract.Ensures(Contract.Result(Of IFuture(Of IList(Of Plugins.PluginProfile)))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() _pluginProfiles.ToList)
    End Function

    Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
        If finalizing Then Return Nothing
        Return inQueue.QueueAction(Sub() _components.Dispose())
    End Function
End Class

Public Module BotExtensions
    <Extension()>
    Public Function QueueGetOrConstructGameServer(ByVal this As MainBot) As IFuture(Of Components.WC3GameServerManager)
        Contract.Requires(this IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of Components.WC3GameServerManager))() IsNot Nothing)
        Return this.Components.QueueGetOrConstructComponent(Of Components.WC3GameServerManager)(
            factory:=Function() New Components.WC3GameServerManager("Auto", New WC3.GameServer, this))
    End Function
End Module
