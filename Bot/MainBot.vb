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
    Private ReadOnly _components As New AsyncViewableCollection(Of Components.IBotComponent)

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

    Private Function GetAnyGameServerManager() As Components.WC3GameServerManager
        Contract.Ensures(Contract.Result(Of Components.WC3GameServerManager)() IsNot Nothing)
        Dim manager = (From component In _components
                       Select castedComponent = TryCast(component, Components.WC3GameServerManager)
                       Where castedComponent IsNot Nothing).FirstOrDefault
        If manager Is Nothing Then
            manager = New Components.WC3GameServerManager("Auto", New WC3.GameServer, Me)
            AddComponent(manager)
        End If
        Return manager
    End Function
    Public Function QueueGetAnyGameServerManager() As IFuture(Of Components.WC3GameServerManager)
        Contract.Ensures(Contract.Result(Of IFuture(Of Components.WC3GameServerManager))() IsNot Nothing)
        Return inQueue.QueueFunc(AddressOf GetAnyGameServerManager)
    End Function

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

#Region "Components"
    Private Sub AddComponent(ByVal component As Components.IBotComponent)
        Contract.Requires(component IsNot Nothing)
        If _components.Contains(component) Then
            Throw New InvalidOperationException("Component already added.")
        ElseIf HaveComponent(component.Type, component.Name) Then
            Throw New InvalidOperationException("There is already a {0} named {1}.".Frmt(component.Type, component.Name))
        End If
        _components.Add(component)
        'Automatic cleanup
        component.FutureDisposed.CallWhenReady(Sub() RemoveComponent(component)).SetHandled()
    End Sub
    Public Function QueueAddComponent(ByVal component As Components.IBotComponent) As IFuture
        Contract.Requires(component IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return inQueue.QueueAction(Sub() AddComponent(component))
    End Function

    Private Sub RemoveComponent(ByVal component As Components.IBotComponent)
        Contract.Requires(component IsNot Nothing)
        If Not _components.Contains(component) Then Throw New InvalidOperationException("Component not found.")
        _components.Remove(component)
        component.Dispose()
    End Sub
    Public Function QueueRemoveComponent(ByVal type As InvariantString, ByVal name As InvariantString) As IFuture
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return inQueue.QueueAction(
            Sub()
                If Not HaveComponent(type, name) Then Throw New InvalidOperationException("There is no {0} named {1} to remove.".Frmt(type, name))
                RemoveComponent(TryFindComponent(type, name))
            End Sub)
    End Function
    Public Function QueueRemoveComponent(ByVal component As Components.IBotComponent) As IFuture
        Contract.Requires(component IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return inQueue.QueueAction(Sub() RemoveComponent(component))
    End Function

    <Pure()>
    Private Function HaveComponent(ByVal type As InvariantString, ByVal name As InvariantString) As Boolean
        Return (From c In _components Where c.Type = type AndAlso c.Name = name).Any
    End Function
    <Pure()>
    Private Function TryFindComponent(ByVal type As InvariantString, ByVal name As InvariantString) As Components.IBotComponent
        Contract.Ensures((Contract.Result(Of Components.IBotComponent)() IsNot Nothing) = HaveComponent(type, name))
        Return (From c In _components Where c.Type = type AndAlso c.Name = name).FirstOrDefault
    End Function
    Public Function QueueTryFindComponent(ByVal type As InvariantString, ByVal name As InvariantString) As IFuture(Of Components.IBotComponent)
        Contract.Ensures(Contract.Result(Of IFuture(Of Components.IBotComponent))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() TryFindComponent(type, name))
    End Function

    Public Function QueueGetAllComponents() As IFuture(Of IList(Of Components.IBotComponent))
        Contract.Ensures(Contract.Result(Of IFuture(Of IList(Of Components.IBotComponent)))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() _components.ToList)
    End Function

    Private Function CreateComponentsAsyncView(ByVal adder As Action(Of MainBot, Components.IBotComponent),
                                          ByVal remover As Action(Of MainBot, Components.IBotComponent)) As IDisposable
        Contract.Requires(adder IsNot Nothing)
        Contract.Requires(remover IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
        Return _components.BeginSync(adder:=Sub(sender, item) adder(Me, item),
                                     remover:=Sub(sender, item) remover(Me, item))
    End Function
    Public Function QueueCreateComponentsAsyncView(ByVal adder As Action(Of MainBot, Components.IBotComponent),
                                              ByVal remover As Action(Of MainBot, Components.IBotComponent)) As IFuture(Of IDisposable)
        Contract.Requires(adder IsNot Nothing)
        Contract.Requires(remover IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() CreateComponentsAsyncView(adder, remover))
    End Function
#End Region

    Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
        If finalizing Then Return Nothing
        Return inQueue.QueueAction(
            Sub()
                For Each component In _components
                    component.Dispose()
                Next component
            End Sub)
    End Function
End Class
