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
        ElseIf TryFindComponent(component.Type, component.Name) IsNot Nothing Then
            Throw New InvalidOperationException("There is already a {0} named {1}.".Frmt(component.Type, component.Name))
        End If
        _components.Add(component)
        'Automatic cleanup
        component.FutureDisposed.QueueCallWhenReady(inQueue,
                Sub() _components.Remove(component))
    End Sub
    Public Function QueueAddComponent(ByVal component As Components.IBotComponent) As IFuture
        Contract.Requires(component IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return inQueue.QueueAction(Sub() AddComponent(component))
    End Function

    <Pure()>
    Private Function EnumComponents(Of T As Components.IBotComponent)() As IEnumerable(Of T)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of T))() IsNot Nothing)
        Return From component In _components
               Where TypeOf component Is T
               Select CType(component, T)
    End Function
    Public Function QueueGetAllComponents() As IFuture(Of IList(Of Components.IBotComponent))
        Contract.Ensures(Contract.Result(Of IFuture(Of IList(Of Components.IBotComponent)))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() _components.ToList)
    End Function
    Public Function QueueGetAllComponents(Of T As Components.IBotComponent)() As IFuture(Of IList(Of T))
        Contract.Ensures(Contract.Result(Of IFuture(Of IList(Of T)))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() EnumComponents(Of T)().ToList)
    End Function

    <Pure()>
    Private Function TryFindComponent(ByVal type As InvariantString,
                                       ByVal name As InvariantString) As Components.IBotComponent
        Return (From c In _components
                Where c.Type = type _
                AndAlso c.Name = name).FirstOrDefault
    End Function
    Public Function QueueTryFindComponent(ByVal type As InvariantString,
                                          ByVal name As InvariantString) As IFuture(Of Components.IBotComponent)
        Contract.Ensures(Contract.Result(Of IFuture(Of Components.IBotComponent))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() TryFindComponent(type, name))
    End Function
    <Pure()>
    Private Function FindComponent(ByVal type As InvariantString,
                                   ByVal name As InvariantString) As Components.IBotComponent
        Contract.Ensures(Contract.Result(Of Components.IBotComponent)() IsNot Nothing)
        Dim result = TryFindComponent(type, name)
        If result Is Nothing Then Throw New InvalidOperationException("No component of type {0} named {1}.".Frmt(type, name))
        Return result
    End Function
    Public Function QueueFindComponent(ByVal type As InvariantString,
                                       ByVal name As InvariantString) As IFuture(Of Components.IBotComponent)
        Contract.Ensures(Contract.Result(Of IFuture(Of Components.IBotComponent))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() FindComponent(type, name))
    End Function
    <Pure()>
    Private Function TryFindComponent(Of T As Components.IBotComponent)(ByVal name As InvariantString) As T
        Return (From c In Me.EnumComponents(Of T)()
                Where c.Name = name).FirstOrDefault
    End Function
    Public Function QueueTryFindComponent(Of T As Components.IBotComponent)(ByVal name As InvariantString) As IFuture(Of T)
        Contract.Ensures(Contract.Result(Of IFuture(Of T))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() TryFindComponent(Of T)(name))
    End Function
    <Pure()>
    Private Function FindComponent(Of T As Components.IBotComponent)(ByVal name As InvariantString) As T
        Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
        Dim result = TryFindComponent(Of T)(name)
        If result Is Nothing Then Throw New InvalidOperationException("No component of type {0} named {1}.".Frmt(GetType(T), name))
        Return result
    End Function
    Public Function QueueFindComponent(Of T As Components.IBotComponent)(ByVal name As InvariantString) As IFuture(Of T)
        Contract.Ensures(Contract.Result(Of IFuture(Of T))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() FindComponent(Of T)(name))
    End Function

    Private Function GetOrConstructComponent(Of T As Components.IBotComponent)(ByVal factory As Func(Of T)) As T
        Contract.Requires(factory IsNot Nothing)
        Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
        Dim result = CType((From c In _components Where TypeOf c Is T).FirstOrDefault, T)
        If result Is Nothing Then
            result = factory()
            Contract.Assume(result IsNot Nothing)
            AddComponent(result)
        End If
        Return result
    End Function
    Public Function QueueGetOrConstructComponent(Of T As Components.IBotComponent)(ByVal factory As Func(Of T)) As IFuture(Of T)
        Contract.Requires(factory IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of T))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() GetOrConstructComponent(factory))
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
    Public Function QueueCreateComponentsAsyncView(Of T As Components.IBotComponent)(
                            ByVal adder As Action(Of MainBot, T),
                            ByVal remover As Action(Of MainBot, T)) As IFuture(Of IDisposable)
        Contract.Requires(adder IsNot Nothing)
        Contract.Requires(remover IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
        Return Me.QueueCreateComponentsAsyncView(
                adder:=Sub(sender, component) If TypeOf component Is T Then adder(sender, CType(component, T)),
                remover:=Sub(sender, component) If TypeOf component Is T Then remover(sender, CType(component, T)))
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

Public Module MainBotExtensions
    <Extension()>
    Public Function QueueGetOrConstructGameServer(ByVal this As MainBot) As IFuture(Of Components.WC3GameServerManager)
        Contract.Requires(this IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of Components.WC3GameServerManager))() IsNot Nothing)
        Return this.QueueGetOrConstructComponent(Of Components.WC3GameServerManager)(
            factory:=Function() New Components.WC3GameServerManager("Auto", New WC3.GameServer, this))
    End Function
End Module
