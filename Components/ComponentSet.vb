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

Namespace Components
    ''' <summary>
    ''' Stores an asynchronous set of components.
    ''' </summary>
    Public NotInheritable Class ComponentSet
        Inherits FutureDisposable

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly _components As New AsyncViewableCollection(Of IBotComponent)(outQueue:=outQueue)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_components IsNot Nothing)
        End Sub

        ''' <summary>
        ''' Adds a component to the set.
        ''' The component will be automatically removed when it is disposed.
        ''' Throws an InvalidOperationException if the component is already included.
        ''' Throws an InvalidOperationException if a component with the same name and type identifiers is already included.
        ''' </summary>
        Private Sub AddComponent(ByVal component As IBotComponent)
            Contract.Requires(component IsNot Nothing)
            If _components.Contains(component) Then
                Throw New InvalidOperationException("Component already added.")
            ElseIf TryFindComponent(component.Type, component.Name) IsNot Nothing Then
                Throw New InvalidOperationException("There is already a {0} named {1}.".Frmt(component.Type, component.Name))
            End If
            _components.Add(component)
            component.FutureDisposed.QueueCallWhenReady(inQueue, Sub() _components.Remove(component)) 'automatic cleanup
        End Sub
        ''' <summary>
        ''' Asynchronously adds a component to the set.
        ''' The component will be automatically removed when it is disposed.
        ''' Asynchronously fails with an InvalidOperationException if the component is already included.
        ''' Asynchronously fails with an InvalidOperationException if a component with the same name and type identifiers is already included.
        ''' </summary>
        Public Function QueueAddComponent(ByVal component As IBotComponent) As IFuture
            Contract.Requires(component IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() AddComponent(component))
        End Function

        ''' <summary>Asynchronously determines a list of all components in the set.</summary>
        Public Function QueueGetAllComponents() As IFuture(Of IList(Of IBotComponent))
            Contract.Ensures(Contract.Result(Of IFuture(Of IList(Of IBotComponent)))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _components.ToList)
        End Function

        ''' <summary>Returns an enumeration of components of a type in the set.</summary>
        <Pure()>
        Private Function EnumComponents(Of T As IBotComponent)() As IEnumerable(Of T)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of T))() IsNot Nothing)
            Return From component In _components
                   Where TypeOf component Is T
                   Select CType(component, T)
        End Function
        ''' <summary>Asynchronously determines a list of all components of a type in the set.</summary>
        Public Function QueueGetAllComponents(Of T As IBotComponent)() As IFuture(Of IList(Of T))
            Contract.Ensures(Contract.Result(Of IFuture(Of IList(Of T)))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() EnumComponents(Of T)().ToList)
        End Function

        ''' <summary>
        ''' Determines a component, from the set, with the given type and name identifiers.
        ''' Returns null if there is no such component.
        ''' </summary>
        <Pure()>
        <ContractVerification(False)>
        Private Function TryFindComponent(ByVal type As InvariantString,
                                          ByVal name As InvariantString) As IBotComponent
            'verification disabled due to stupid verifier
            Contract.Ensures(Contract.Result(Of IBotComponent)() Is Nothing OrElse Contract.Result(Of IBotComponent).Name = name)
            Contract.Ensures(Contract.Result(Of IBotComponent)() Is Nothing OrElse Contract.Result(Of IBotComponent).Type = type)
            Dim result = (From c In _components Where c.Type = type AndAlso c.Name = name).FirstOrDefault
            Contract.Assume(result Is Nothing OrElse result.Name = name)
            Contract.Assume(result Is Nothing OrElse result.Type = type)
            Return result
        End Function
        ''' <summary>
        ''' Determines a component, from the set, with the given type and name identifiers.
        ''' Throws an InvalidOperationException if there is no such component.
        ''' </summary>
        <Pure()>
        <ContractVerification(False)>
        Private Function FindComponent(ByVal type As InvariantString,
                                       ByVal name As InvariantString) As IBotComponent
            'verification disabled due to stupid verifier
            Contract.Ensures(Contract.Result(Of IBotComponent)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IBotComponent)().Name = name)
            Contract.Ensures(Contract.Result(Of IBotComponent)().Type = type)
            Dim result = TryFindComponent(type, name)
            If result Is Nothing Then Throw New InvalidOperationException("No component of type {0} named {1}.".Frmt(type, name))
            Return result
        End Function
        ''' <summary>
        ''' Asynchronously determines a component, from the set, with the given type and name identifiers.
        ''' Asynchronously fails with an InvalidOperationExceptionFails if there is no such component.
        ''' </summary>
        Public Function QueueFindComponent(ByVal type As InvariantString,
                                           ByVal name As InvariantString) As IFuture(Of IBotComponent)
            Contract.Ensures(Contract.Result(Of IFuture(Of IBotComponent))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() FindComponent(type, name))
        End Function

        ''' <summary>
        ''' Determines a component, from the set, with the given type and name.
        ''' Returns null if there is no such component.
        ''' </summary>
        <Pure()>
        <ContractVerification(False)>
        Private Function TryFindComponent(Of T As IBotComponent)(ByVal name As InvariantString) As T
            'verification disabled due to stupid verifier
            Contract.Ensures(Contract.Result(Of T)() Is Nothing OrElse Contract.Result(Of T).Name = name)
            Dim result = (From c In Me.EnumComponents(Of T)() Where c.Name = name).FirstOrDefault
            Contract.Assume(result Is Nothing OrElse result.Name = name)
            Return result
        End Function
        ''' <summary>
        ''' Determines a component, from the set, with the given type and name.
        ''' Throws an InvalidOperationException if there is no such component.
        ''' </summary>
        <Pure()>
        <ContractVerification(False)>
        Private Function FindComponent(Of T As IBotComponent)(ByVal name As InvariantString) As T
            'verification disabled due to stupid verifier
            Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of T).Name = name)
            Dim result = TryFindComponent(Of T)(name)
            If result Is Nothing Then Throw New InvalidOperationException("No component of type {0} named {1}.".Frmt(GetType(T), name))
            Return result
        End Function
        ''' <summary>
        ''' Asynchronously determines a component, from the set, with the given type and name.
        ''' Asynchronously fails with an InvalidOperationExceptionFails if there is no such component.
        ''' </summary>
        Public Function QueueFindComponent(Of T As IBotComponent)(ByVal name As InvariantString) As IFuture(Of T)
            Contract.Ensures(Contract.Result(Of IFuture(Of T))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() FindComponent(Of T)(name))
        End Function

        ''' <summary>
        ''' Determines a component, from the set, with the given type.
        ''' If there was no such component then the result is a new component produced by the given factory and added to the set.
        ''' </summary>
        Private Function GetOrConstructComponent(Of T As IBotComponent)(ByVal factory As Func(Of T)) As T
            Contract.Requires(factory IsNot Nothing)
            Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
            Dim result = EnumComponents(Of T)().FirstOrDefault
            If result Is Nothing Then
                result = factory()
                Contract.Assume(result IsNot Nothing)
                AddComponent(result)
            End If
            Return result
        End Function
        ''' <summary>
        ''' Asynchronously determines a component, from the set, with the given type.
        ''' If there was no such component then the result is a new component produced by the given factory and added to the set.
        ''' </summary>
        Public Function QueueGetOrConstructComponent(Of T As IBotComponent)(ByVal factory As Func(Of T)) As IFuture(Of T)
            Contract.Requires(factory IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of T))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() GetOrConstructComponent(factory))
        End Function

        ''' <summary>
        ''' Registers methods to be used for syncing with components in the set.
        ''' The result is an IDisposable which, if disposed, unregisters the methods.
        ''' </summary>
        ''' <param name="adder">Asynchronously called with the initial components in the set, as well as new components as they are added.</param>
        ''' <param name="remover">Asynchronously called with components as they are removed.</param>
        Private Function CreateAsyncView(ByVal adder As Action(Of ComponentSet, IBotComponent),
                                         ByVal remover As Action(Of ComponentSet, IBotComponent)) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _components.BeginSync(adder:=Sub(sender, item) adder(Me, item),
                                         remover:=Sub(sender, item) remover(Me, item))
        End Function
        ''' <summary>
        ''' Asychronously registers methods to be used for syncing with components in the set.
        ''' The result is an IDisposable which, if disposed, unregisters the methods.
        ''' </summary>
        ''' <param name="adder">Asynchronously called with the initial components in the set, as well as new components as they are added.</param>
        ''' <param name="remover">Asynchronously called with components as they are removed.</param>
        Public Function QueueCreateAsyncView(ByVal adder As Action(Of ComponentSet, IBotComponent),
                                             ByVal remover As Action(Of ComponentSet, IBotComponent)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CreateAsyncView(adder, remover))
        End Function
        ''' <summary>
        ''' Asychronously registers methods to be used for syncing with components of a type in the set.
        ''' The result is an IDisposable which, if disposed, unregisters the methods.
        ''' </summary>
        ''' <param name="adder">Asynchronously called with the initial components in the set, as well as new components as they are added.</param>
        ''' <param name="remover">Asynchronously called with components as they are removed.</param>
        Public Function QueueCreateAsyncView(Of T As IBotComponent)(ByVal adder As Action(Of ComponentSet, T),
                                                                    ByVal remover As Action(Of ComponentSet, T)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return Me.QueueCreateAsyncView(
                    adder:=Sub(sender, component) If TypeOf component Is T Then adder(sender, CType(component, T)),
                    remover:=Sub(sender, component) If TypeOf component Is T Then remover(sender, CType(component, T)))
        End Function

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
End Namespace
