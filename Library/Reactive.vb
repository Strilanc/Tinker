Public Interface IReacter(Of T, R)
    Function OnNext(value As T) As Task(Of R)
    Function OnCompleted() As Task(Of R)
    Function OnError(ex As Exception) As Task(Of R)
End Interface
Public Interface IReactable(Of T, R)
    Function Subscribe(reacter As IReacter(Of T, R)) As IDisposable
End Interface

Public Class Reacter(Of T, R)
    Implements IReacter(Of T, R)
    Private ReadOnly _onCompleted As Func(Of Task(Of R))
    Private ReadOnly _onError As Func(Of Exception, Task(Of R))
    Private ReadOnly _onNext As Func(Of T, Task(Of R))
    Public Sub New(onNext As Func(Of T, Task(Of R)), onCompleted As Func(Of Task(Of R)), onError As Func(Of Exception, Task(Of R)))
        Me._onCompleted = onCompleted
        Me._onError = onError
        Me._onNext = onNext
    End Sub
    Public Function OnCompleted() As Task(Of R) Implements IReacter(Of T, R).OnCompleted
        Return _onCompleted()
    End Function
    Public Function OnError(ex As Exception) As Task(Of R) Implements IReacter(Of T, R).OnError
        Return _onError(ex)
    End Function
    Public Function OnNext(value As T) As Task(Of R) Implements IReacter(Of T, R).OnNext
        Return _onNext(value)
    End Function
End Class
Public Class Reactable(Of T, R)
    Implements IReactable(Of T, R)
    Private ReadOnly _subscribe As Func(Of IReacter(Of T, R), IDisposable)
    Public Sub New(subscribe As Func(Of IReacter(Of T, R), IDisposable))
        Me._subscribe = subscribe
    End Sub
    Public Function Subscribe(reacter As IReacter(Of T, R)) As IDisposable Implements IReactable(Of T, R).Subscribe
        Return _subscribe(reacter)
    End Function
End Class
Public Class ManualReactable(Of T, R)
    Implements IReactable(Of T, R)
    Private ReadOnly _reacters As New List(Of IReacter(Of T, R))()
    Public Function Subscribe(reacter As IReacter(Of T, R)) As IDisposable Implements IReactable(Of T, R).Subscribe
        _reacters.Add(reacter)
        Return New DelegatedDisposable(Sub() _reacters.Remove(reacter))
    End Function
    Public Function PushNext(value As T) As Task(Of R())
        Return Task.WhenAll(_reacters.Select(Function(e) e.OnNext(value)))
    End Function
    Public Function PushCompleted() As Task(Of R())
        Return Task.WhenAll(_reacters.Select(Function(e) e.OnCompleted()))
    End Function
    Public Function PushError(ex As Exception) As Task(Of R())
        Return Task.WhenAll(_reacters.Select(Function(e) e.OnError(ex)))
    End Function
End Class

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
    Public Function ReactTo(Of T, R)(reactable As IReactable(Of T, R), onNext As Func(Of T, Task(Of R)), onCompleted As Func(Of Task(Of R)), onError As Func(Of Exception, Task(Of R))) As IDisposable
        Return reactable.Subscribe(New Reacter(Of T, R)(onNext, onCompleted, onError))
    End Function
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
    Public Function [Select](Of T, R, T2)(reactable As IReactable(Of T, R), proj As Func(Of T, T2)) As IReactable(Of T2, R)
        Return New Reactable(Of T2, R)(Function(reacter) reactable.ReactTo(
            Function(value)
                Dim v As T2
                Try
                    v = proj(value)
                Catch ex As Exception
                    Return reacter.OnError(ex)
                End Try
                Return reacter.OnNext(v)
            End Function,
            AddressOf reacter.OnCompleted,
            AddressOf reacter.OnError))
    End Function
    <Extension> <Pure>
    Public Function Where(Of T, R)(reactable As IReactable(Of T, R), filter As Func(Of T, Boolean), Optional [default] As R = Nothing) As IReactable(Of T, R)
        Return New Reactable(Of T, R)(Function(reacter) reactable.ReactTo(
            Function(value)
                Dim keep As Boolean
                Try
                    keep = filter(value)
                Catch ex As Exception
                    Return reacter.OnError(ex)
                End Try
                If Not keep Then Return Task.FromResult([default])
                Return reacter.OnNext(value)
            End Function,
            AddressOf reacter.OnCompleted,
            AddressOf reacter.OnError))
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
    <Extension> <Pure>
    Public Function InCurrentSyncContext(Of T, R)(reactable As IReactable(Of T, R)) As IReactable(Of T, R)
        Dim context = SynchronizationContext.Current
        Dim post = If(context Is Nothing,
                      Function(e As Func(Of Task(Of R))) e(),
                      Function(e As Func(Of Task(Of R)))
                          Dim tr = New TaskCompletionSource(Of Task(Of R))
                          context.Post(Sub() tr.SetResult(e()), Nothing)
                          Return tr.Task.Unwrap()
                      End Function)
        Return New Reactable(Of T, R)(Function(observer) reactable.ReactTo(
            Function(value) post(Function() observer.OnNext(value)),
            Function() post(AddressOf observer.OnCompleted),
            Function(err) post(Function() observer.OnError(err))))
    End Function
End Module
