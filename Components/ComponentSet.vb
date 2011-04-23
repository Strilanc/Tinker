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
        Inherits DisposableWithTask

        Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue
        Private ReadOnly outQueue As CallQueue = MakeTaskedCallQueue
        Private ReadOnly _components As New ObservableCollection(Of IBotComponent)(outQueue:=outQueue)

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
        Private Sub AddComponent(component As IBotComponent)
            Contract.Requires(component IsNot Nothing)
            If _components.Contains(component) Then
                Throw New InvalidOperationException("Component already added.")
            ElseIf TryFindComponent(component.Type, component.Name).HasValue Then
                Throw New InvalidOperationException("There is already a {0} named {1}.".Frmt(component.Type, component.Name))
            End If
            _components.Add(component)
            component.DisposalTask.QueueContinueWithAction(inQueue, Sub() _components.Remove(component)) 'automatic cleanup
        End Sub
        ''' <summary>
        ''' Asynchronously adds a component to the set.
        ''' The component will be automatically removed when it is disposed.
        ''' Asynchronously fails with an InvalidOperationException if the component is already included.
        ''' Asynchronously fails with an InvalidOperationException if a component with the same name and type identifiers is already included.
        ''' </summary>
        Public Function QueueAddComponent(component As IBotComponent) As Task
            Contract.Requires(component IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() AddComponent(component))
        End Function

        ''' <summary>Asynchronously determines a list of all components in the set.</summary>
        Public Function QueueGetAllComponents() As Task(Of IRist(Of IBotComponent))
            Contract.Ensures(Contract.Result(Of Task(Of IRist(Of IBotComponent)))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _components.ToRist())
        End Function

        ''' <summary>
        ''' Determines a component, from the set, with the given type and name identifiers.
        ''' Returns null if there is no such component.
        ''' </summary>
        <Pure()>
        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Not Contract.Result(Of Maybe(Of IBotComponent))().HasValue OrElse Contract.Result(Of Maybe(Of IBotComponent)).Value.Name = name")>
        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Not Contract.Result(Of Maybe(Of IBotComponent))().HasValue OrElse Contract.Result(Of Maybe(Of IBotComponent)).Value.Type = type")>
        Private Function TryFindComponent(type As InvariantString,
                                          name As InvariantString) As Maybe(Of IBotComponent)
            Contract.Ensures(Not Contract.Result(Of Maybe(Of IBotComponent))().HasValue OrElse Contract.Result(Of Maybe(Of IBotComponent)).Value.Name = name)
            Contract.Ensures(Not Contract.Result(Of Maybe(Of IBotComponent))().HasValue OrElse Contract.Result(Of Maybe(Of IBotComponent)).Value.Type = type)
            Return (From c In _components Where c.Type = type AndAlso c.Name = name).MaybeFirst()
        End Function
        ''' <summary>
        ''' Determines a component, from the set, with the given type and name identifiers.
        ''' Throws an InvalidOperationException if there is no such component.
        ''' </summary>
        <Pure()>
        Private Function FindComponent(type As InvariantString,
                                       name As InvariantString) As IBotComponent
            Contract.Ensures(Contract.Result(Of IBotComponent)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IBotComponent)().Name = name)
            Contract.Ensures(Contract.Result(Of IBotComponent)().Type = type)
            Dim result = TryFindComponent(type, name)
            If Not result.HasValue Then Throw New InvalidOperationException("No component of type {0} named {1}.".Frmt(type, name))
            Contract.Assume(result.Value.Name = name)
            Contract.Assume(result.Value.Type = type)
            Return result.Value
        End Function
        ''' <summary>
        ''' Asynchronously determines a component, from the set, with the given type and name identifiers.
        ''' Asynchronously fails with an InvalidOperationExceptionFails if there is no such component.
        ''' </summary>
        Public Function QueueFindComponent(type As InvariantString,
                                           name As InvariantString) As Task(Of IBotComponent)
            Contract.Ensures(Contract.Result(Of Task(Of IBotComponent))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() FindComponent(type, name))
        End Function

        ''' <summary>
        ''' Determines a component, from the set, with the given type and name.
        ''' Returns null if there is no such component.
        ''' </summary>
        <Pure()>
        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Not Contract.Result(Of Maybe(Of T))().HasValue OrElse Contract.Result(Of Maybe(Of T))().Value.Name = name")>
        Private Function TryFindComponent(Of T As IBotComponent)(name As InvariantString) As Maybe(Of T)
            Contract.Ensures(Not Contract.Result(Of Maybe(Of T))().HasValue OrElse Contract.Result(Of Maybe(Of T))().Value.Name = name)
            Return (From c In _components.OfType(Of T)() Where c.Name = name).MaybeFirst()
        End Function
        ''' <summary>
        ''' Determines a component, from the set, with the given type and name.
        ''' Throws an InvalidOperationException if there is no such component.
        ''' </summary>
        <Pure()>
        Private Function FindComponent(Of T As IBotComponent)(name As InvariantString) As T
            Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of T).Name = name)
            Dim result = TryFindComponent(Of T)(name)
            If Not result.HasValue Then Throw New InvalidOperationException("No component of type {0} named {1}.".Frmt(GetType(T), name))
            Contract.Assume(result.Value.Name = name)
            Return result.Value
        End Function
        ''' <summary>
        ''' Asynchronously determines a component, from the set, with the given type and name.
        ''' Asynchronously fails with an InvalidOperationExceptionFails if there is no such component.
        ''' </summary>
        Public Function QueueFindComponent(Of T As IBotComponent)(name As InvariantString) As Task(Of T)
            Contract.Ensures(Contract.Result(Of Task(Of T))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() FindComponent(Of T)(name))
        End Function

        ''' <summary>
        ''' Determines a component, from the set, with the given type.
        ''' If there was no such component then the result is a new component produced by the given factory and added to the set.
        ''' </summary>
        Private Function FindOrElseConstructComponent(Of T As IBotComponent)(factory As Func(Of T)) As T
            Contract.Requires(factory IsNot Nothing)
            Contract.Ensures(Contract.Result(Of T)() IsNot Nothing)
            Dim result = _components.OfType(Of T)().MaybeFirst()
            If Not result.HasValue Then
                result = factory().AssumeNotNull()
                AddComponent(result.Value)
            End If
            Return result.Value
        End Function
        ''' <summary>
        ''' Asynchronously determines a component, from the set, with the given type.
        ''' If there was no such component then the result is a new component produced by the given factory and added to the set.
        ''' </summary>
        Public Function QueueFindOrElseConstructComponent(Of T As IBotComponent)(factory As Func(Of T)) As Task(Of T)
            Contract.Requires(factory IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of T))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() FindOrElseConstructComponent(factory))
        End Function

        ''' <summary>
        ''' Asychronously registers methods to be used for syncing with components in the set.
        ''' The result is an IDisposable which, if disposed, unregisters the methods.
        ''' </summary>
        ''' <param name="adder">Asynchronously called with the initial components in the set, as well as new components as they are added.</param>
        ''' <param name="remover">Asynchronously called with components as they are removed.</param>
        Public Function ObserveComponents(adder As Action(Of IBotComponent),
                                          remover As Action(Of IBotComponent)) As Task(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _components.Observe(adder, remover))
        End Function

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(
                Sub()
                    For Each component In _components
                        component.Dispose()
                    Next component
                End Sub)
        End Function
    End Class
    Public Module ComponentSetExtensions
        ''' <summary>
        ''' Asychronously registers methods to be used for syncing with components of a type in the set.
        ''' The result is an IDisposable which, if disposed, unregisters the methods.
        ''' </summary>
        ''' <param name="adder">Asynchronously called with the initial components in the set, as well as new components as they are added.</param>
        ''' <param name="remover">Asynchronously called with components as they are removed.</param>
        <Extension()> <Pure()>
        Public Function ObserveComponentsOfType(Of T As IBotComponent)(this As ComponentSet,
                                                                       adder As Action(Of T),
                                                                       remover As Action(Of T)) As Task(Of IDisposable)
            Contract.Requires(this IsNot Nothing)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return this.ObserveComponents(
                adder:=Sub(component) If TypeOf component Is T Then adder(DirectCast(component, T)),
                remover:=Sub(component) If TypeOf component Is T Then remover(DirectCast(component, T)))
        End Function
    End Module
End Namespace
