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
Public Class ObservableWalker(Of T)
    Implements IObserver(Of T)

    Private ReadOnly _ready As New Queue(Of Task(Of Renullable(Of T)))
    Private ReadOnly _lock As New Object()
    Private _next As TaskCompletionSource(Of Renullable(Of T))

    Public Function TryNext() As Task(Of Renullable(Of T))
        SyncLock _lock
            If _ready.Count > 0 Then
                Dim r = _ready.Peek()
                If r.Status = TaskStatus.RanToCompletion AndAlso r.Result IsNot Nothing Then
                    _ready.Dequeue()
                End If
                Return r
            End If

            If _next IsNot Nothing Then Throw New InvalidOperationException("Overlapping TryNext calls.")
            _next = New TaskCompletionSource(Of Renullable(Of T))
            Return _next.Task
        End SyncLock
    End Function

    Private Sub OnCompleted() Implements IObserver(Of T).OnCompleted
        SyncLock _lock
            _next = If(_next, New TaskCompletionSource(Of Renullable(Of T)))
            _next.SetResult(Nothing)
            _ready.Enqueue(_next.Task)
        End SyncLock
    End Sub
    Private Sub OnError([error] As Exception) Implements IObserver(Of T).OnError
        SyncLock _lock
            _next = If(_next, New TaskCompletionSource(Of Renullable(Of T)))
            _next.SetException([error])
            _ready.Enqueue(_next.Task)
        End SyncLock
    End Sub
    Private Sub OnNext(value As T) Implements IObserver(Of T).OnNext
        SyncLock _lock
            If _next IsNot Nothing Then
                _next.SetResult(value)
            Else
                _ready.Enqueue(Task.FromResult(New Renullable(Of T)(value)))
            End If
        End SyncLock
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
