Imports Tinker.Pickling

Public Class Observer(Of T)
    Implements IObserver(Of T)
    Private ReadOnly _onCompleted As Action
    Private ReadOnly _onError As Action(Of Exception)
    Private ReadOnly _onNext As Action(Of T)
    Public Sub New(onNext As Action(Of T), onCompleted As Action, onError As Action(Of Exception))
        Me._onCompleted = onCompleted
        Me._onError = onError
        Me._onNext = onNext
    End Sub
    Public Sub OnCompleted() Implements IObserver(Of T).OnCompleted
        _onCompleted()
    End Sub
    Public Sub OnError([error] As Exception) Implements IObserver(Of T).OnError
        _onError([error])
    End Sub
    Public Sub OnNext(value As T) Implements IObserver(Of T).OnNext
        _onNext(value)
    End Sub
End Class
Public Class Observable(Of T)
    Implements IObservable(Of T)
    Private ReadOnly _subscribe As Func(Of IObserver(Of T), IDisposable)
    Public Sub New(subscribe As Func(Of IObserver(Of T), IDisposable))
        Contract.Requires(subscribe IsNot Nothing)
        Me._subscribe = subscribe
    End Sub
    Public Function Subscribe(observer As IObserver(Of T)) As IDisposable Implements IObservable(Of T).Subscribe
        Return _subscribe(observer)
    End Function
End Class
Public Class ManualObservable(Of T)
    Implements IObservable(Of T)
    Private ReadOnly _observers As New List(Of IObserver(Of T))()
    Public Function Subscribe(observer As IObserver(Of T)) As IDisposable Implements IObservable(Of T).Subscribe
        _observers.Add(observer)
        Return New DelegatedDisposable(Sub() _observers.Remove(observer))
    End Function
    Public Sub PushNext(value As T)
        For Each obs In _observers
            obs.OnNext(value)
        Next
    End Sub
    Public Sub PushCompleted()
        For Each obs In _observers
            obs.OnCompleted()
        Next
    End Sub
    Public Sub PushError([error] As Exception)
        For Each obs In _observers
            obs.OnError([error])
        Next
    End Sub
End Class

Public Module ReactiveUtil
    <Extension> <Pure>
    Public Function Observe(Of T)(observable As IObservable(Of T), onNext As Action(Of T), onCompleted As Action, onError As Action(Of Exception)) As IDisposable
        Return observable.Subscribe(New Observer(Of T)(onNext, onCompleted, onError))
    End Function
    <Extension> <Pure>
    Public Function CollectListAsync(Of T)(observable As IObservable(Of T), Optional ct As CancellationToken = Nothing) As Task(Of List(Of T))
        Dim r = New List(Of T)()
        Dim s = New TaskCompletionSource(Of List(Of T))()
        Dim reg = observable.Observe(
            Sub(value) r.Add(value),
            Sub() s.TrySetResult(r),
            Sub([error]) s.TrySetException([error]))
        ct.Register(Sub()
                        reg.Dispose()
                        s.TrySetCanceled()
                    End Sub)
        Return s.Task
    End Function
    <Extension> <Pure>
    Public Function [Select](Of T, R)(observable As IObservable(Of T), projection As Func(Of T, R)) As IObservable(Of R)
        Return New Observable(Of R)(Function(observer) observable.Observe(
            Sub(value)
                Dim v As R
                Try
                    v = projection(value)
                Catch ex As Exception
                    observer.OnError(ex)
                    Return
                End Try
                observer.OnNext(v)
            End Sub,
            AddressOf observer.OnCompleted,
            AddressOf observer.OnError))
    End Function
    <Extension> <Pure>
    Public Function Where(Of T)(observable As IObservable(Of T), filter As Func(Of T, Boolean)) As IObservable(Of T)
        Return New Observable(Of T)(Function(observer) observable.Observe(
            Sub(value)
                Dim keep As Boolean
                Try
                    keep = filter(value)
                Catch ex As Exception
                    observer.OnError(ex)
                    Return
                End Try
                If keep Then observer.OnNext(value)
            End Sub,
            AddressOf observer.OnCompleted,
            AddressOf observer.OnError))
    End Function
    <Extension> <Pure>
    Public Function InCurrentSyncContext(Of T)(observable As IObservable(Of T)) As IObservable(Of T)
        Dim context = SynchronizationContext.Current
        Dim post = If(context Is Nothing,
                      Sub(e As Action) e(),
                      Sub(e As Action) context.Post(Sub() e(), Nothing))
        Return New Observable(Of T)(Function(observer) observable.Observe(
            Sub(value) post(Sub() observer.OnNext(value)),
            Sub() post(AddressOf observer.OnCompleted),
            Sub(err) post(Sub() observer.OnError(err))))
    End Function
End Module

Public Interface IValuePusher(Of T, R)
    Function Push(value As T) As Task(Of R)
    Function CreateWalker() As IValueWalker(Of T, R)
End Interface
Public Interface IValueWalker(Of T, R)
    Inherits IDisposable
    Function Split() As IValueWalker(Of T, R)
    Function WalkAsync(filter As Func(Of T, Boolean), parser As Func(Of T, R)) As Task(Of R)
End Interface

Public Class ValuePusher(Of T, R)
    Implements IValuePusher(Of T, R)
    Private ReadOnly _createWalker As Func(Of IValueWalker(Of T, R))
    Private ReadOnly _push As Func(Of T, Task(Of R))
    Public Sub New(createWalker As Func(Of IValueWalker(Of T, R)), push As Func(Of T, Task(Of R)))
        Me._createWalker = createWalker
        Me._push = push
    End Sub
    Public Function CreateWalker() As IValueWalker(Of T, R) Implements IValuePusher(Of T, R).CreateWalker
        Return _createWalker()
    End Function
    Public Function Push(value As T) As Task(Of R) Implements IValuePusher(Of T, R).Push
        Return _push(value)
    End Function
End Class
Public Class PacketPusher(Of K)
    Private ReadOnly _pusher As IValuePusher(Of IKeyValue(Of K, IRist(Of Byte)), IPickle(Of Object))
    Public Sub New(Optional pusher As IValuePusher(Of IKeyValue(Of K, IRist(Of Byte)), IPickle(Of Object)) = Nothing)
        Contract.Requires(pusher IsNot Nothing)
        Me._pusher = If(pusher, New ManualValuePusher(Of IKeyValue(Of K, IRist(Of Byte)), IPickle(Of Object))())
    End Sub
    Public Function Push(id As K, data As IRist(Of Byte)) As Task(Of IPickle(Of Object))
        Return _pusher.Push(id.KeyValue(data))
    End Function
    Public Function CreateWalker() As PacketWalker(Of K)
        Return New PacketWalker(Of K)(_pusher.CreateWalker())
    End Function
End Class
Public Class PacketWalker(Of K)
    Implements IDisposable
    Private ReadOnly _walker As IValueWalker(Of IKeyValue(Of K, IRist(Of Byte)), IPickle(Of Object))
    Public Sub New(walker As IValueWalker(Of IKeyValue(Of K, IRist(Of Byte)), IPickle(Of Object)))
        Contract.Requires(walker IsNot Nothing)
        Me._walker = walker
    End Sub
    Public Async Function WalkAsync(Of R)(id As K, jar As IJar(Of R)) As Task(Of IPickle(Of R))
        Dim pickle = Await _walker.WalkAsync(Function(e) Object.Equals(e.Key, id), Function(e) jar.ParsePickle(e.Value).Weaken())
        Return New Pickle(Of R)(pickle.Jar, DirectCast(pickle.Value, R), pickle.Data)
    End Function
    Public Async Function WalkValueAsync(Of R)(id As K, jar As IJar(Of R)) As Task(Of R)
        Dim pickle = Await _walker.WalkAsync(Function(e) Object.Equals(e.Key, id), Function(e) jar.ParsePickle(e.Value).Weaken())
        Return DirectCast(pickle.Value, R)
    End Function
    Public Function Split() As PacketWalker(Of K)
        Return New PacketWalker(Of K)(_walker.Split())
    End Function
    Public Sub Dispose() Implements IDisposable.Dispose
        _walker.Dispose()
    End Sub
End Class
Public Module WalkerEx
    <Extension>
    Public Function WalkValueAsync(Of T)(this As PacketWalker(Of Bnet.Protocol.PacketId), definition As Bnet.Protocol.Packets.Definition(Of T)) As Task(Of T)
        Return this.WalkValueAsync(definition.Id, definition.Jar)
    End Function
End Module

Public Class ManualValuePusher(Of T, R)
    Implements IValuePusher(Of T, R)
    Private _tail As New Node(Nothing, 0)
    Public Function Push(value As T) As Task(Of R) Implements IValuePusher(Of T, R).Push
        _tail = _tail.Push(value)
        Return _tail.ParsedAsync
    End Function
    Public Function CreateWalker() As IValueWalker(Of T, R) Implements IValuePusher(Of T, R).CreateWalker
        Return New Walker(_tail)
    End Function

    Private Class Node
        Private ReadOnly _next As New TaskCompletionSource(Of Node)
        Private ReadOnly _data As T
        Private ReadOnly _parsed As New TaskCompletionSource(Of R)
        Private _headRefCount As Int32
        Private _restRefCount As Int32

        Public ReadOnly Property Data As T
            Get
                Return _data
            End Get
        End Property
        Public ReadOnly Property NextAsync As Task(Of Node)
            Get
                Return _next.Task
            End Get
        End Property
        Public ReadOnly Property ParsedAsync As Task(Of R)
            Get
                Return _parsed.Task
            End Get
        End Property

        Public Sub New(data As T, refCount As Integer)
            Me._data = data
            Me._headRefCount = refCount
            Me._restRefCount = refCount
            If refCount = 0 Then _parsed.SetResult(Nothing)
        End Sub

        Public Function Push(data As T) As Node
            Dim n = New Node(data, _restRefCount)
            _next.SetResult(n)
            Return n
        End Function
        Public Sub PassAndHandle(value As R)
            _parsed.TrySetResult(value)
            Interlocked.Decrement(_headRefCount)
        End Sub
        Public Sub PassWithoutHandling()
            If Interlocked.Decrement(_headRefCount) = 0 Then
                _parsed.TrySetResult(Nothing)
            End If
        End Sub
        Public Sub AddRefToRest()
            Interlocked.Increment(_restRefCount)
            If _next.Task.Status = TaskStatus.RanToCompletion Then
                _next.Task.Result.AddRefToRest()
            End If
        End Sub
        Public Sub RemoveRefToRest()
            Interlocked.Decrement(_restRefCount)
            If _next.Task.Status = TaskStatus.RanToCompletion Then
                _next.Task.Result.RemoveRefToRest()
            End If
        End Sub
    End Class

    Private Class Walker
        Implements IValueWalker(Of T, R)
        Private _head As Node
        Private ReadOnly _lock As New Object()
        Public Sub New(head As Node)
            Me._head = head
            head.AddRefToRest()
        End Sub
        Protected Overrides Sub Finalize()
            Dispose()
        End Sub

        Public Function Split() As IValueWalker(Of T, R) Implements IValueWalker(Of T, R).Split
            Return New Walker(_head)
        End Function

        Public Async Function WalkAsync(filter As Func(Of T, Boolean), parser As Func(Of T, R)) As Task(Of R) Implements IValueWalker(Of T, R).WalkAsync
            Do
                Dim p = _head
                If p Is Nothing Then Throw New ObjectDisposedException("Walker")
                Dim n = Await p.NextAsync
                SyncLock _lock
                    If _head Is Nothing Then Throw New ObjectDisposedException("Walker")
                    If p IsNot _head Then Throw New InvalidOperationException("Overlapping WalkAsync calls")
                    If filter(n.Data) Then
                        Dim r = parser(n.Data)
                        _head = n
                        _head.PassAndHandle(r)
                        Return r
                    Else
                        _head = n
                        _head.PassWithoutHandling()
                    End If
                End SyncLock
            Loop
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            GC.SuppressFinalize(Me)
            SyncLock _lock
                If _head Is Nothing Then Return
                _head.RemoveRefToRest()
                _head = Nothing
            End SyncLock
        End Sub
    End Class
End Class
