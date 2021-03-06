﻿''' <summary>A collection with support for asynchronously observing its items.</summary>
''' <remarks>
''' Incoming calls (including invoking the Observe method) must be synchronized.
''' The adder and remover delegates given to Observe may assume no re-entrency, but little else about thread safety.
''' </remarks>
Public Class ObservableCollection(Of T)
    Implements ICollection(Of T)

    Private ReadOnly outQueue As CallQueue
    Private ReadOnly _items As New List(Of T)

    Private Event Added As Action(Of T)
    Private Event Removed As Action(Of T)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(outQueue IsNot Nothing)
        Contract.Invariant(_items IsNot Nothing)
    End Sub

    ''' <summary>
    ''' Constructs an AsyncViewableCollection which queues synchronizing events on an action queue.
    ''' </summary>
    ''' <param name="outQueue">The action queue async events are queued on. Uses a MakeTaskedCallQueue if null.</param>
    Public Sub New(Optional outQueue As CallQueue = Nothing)
        Me.outQueue = If(outQueue, MakeTaskedCallQueue())
    End Sub

    Public Sub Add(item As T) Implements ICollection(Of T).Add
        _items.Add(item)
        outQueue.QueueAction(Sub() RaiseEvent Added(item))
    End Sub
    Public Sub Clear() Implements ICollection(Of T).Clear
        For Each e In _items
            Dim e_ = e
            outQueue.QueueAction(Sub() RaiseEvent Removed(e_))
        Next e
        _items.Clear()
    End Sub
    Public Function Remove(item As T) As Boolean Implements ICollection(Of T).Remove
        Dim result = _items.Remove(item)
        If result Then outQueue.QueueAction(Sub() RaiseEvent Removed(item))
        Return result
    End Function

    Public Function Contains(item As T) As Boolean Implements ICollection(Of T).Contains
        Return _items.Contains(item)
    End Function
    Public Sub CopyTo(array As T(), arrayIndex As Integer) Implements ICollection(Of T).CopyTo
        If arrayIndex >= array.Length Then Throw New ArgumentException("arrayIndex >= array.Length")
        _items.CopyTo(array, arrayIndex)
    End Sub
    Public ReadOnly Property Count As Integer Implements ICollection(Of T).Count
        Get
            Contract.Ensures(Contract.Result(Of Integer)() = _items.Count)
            Return _items.Count
        End Get
    End Property
    Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of T).IsReadOnly
        Get
            Return False
        End Get
    End Property
    Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        Return _items.GetEnumerator()
    End Function
    Private Function GetEnumeratorObj() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return _items.GetEnumerator()
    End Function

    ''' <summary>
    ''' Begins calling 'adder' with items in the collection and 'remover' with items no longer in the collection.
    ''' </summary>
    ''' <param name="adder">A callback for items in the collection. Never called concurrently, but calls may migrate across threads.</param>
    ''' <param name="remover">A callback for items removed from the collection. Never called concurrently, but calls may migrate across threads.</param>
    ''' <returns>An IDisposable which, when disposed, begins unregistering 'adder' and 'remover'.</returns>
    ''' <remarks>The 'never called concurrently' clause applies between adder and remover (eg. adder will not be called during remover).</remarks>
    Public Function Observe(adder As Action(Of T),
                            remover As Action(Of T)) As IDisposable
        Contract.Requires(adder IsNot Nothing)
        Contract.Requires(remover IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
        'Current items
        For Each item In _items
            Dim item_ = item
            outQueue.QueueAction(Sub() adder(item_))
        Next item
        'Future items
        outQueue.QueueAction(
            Sub()
                AddHandler Added, adder
                AddHandler Removed, remover
            End Sub)
        'Disposable
        Return New DelegatedDisposable(Sub() outQueue.QueueAction(
            Sub()
                RemoveHandler Added, adder
                RemoveHandler Removed, remover
            End Sub))
    End Function
End Class
