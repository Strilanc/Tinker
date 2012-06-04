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

Public Module ReactiveUtil
    <Extension> <Pure>
    Public Function CollectListAsync(Of T)(observable As IObservable(Of T), Optional ct As CancellationToken = Nothing) As Task(Of List(Of T))
        Dim r = New List(Of T)()
        Dim s = New TaskCompletionSource(Of List(Of T))()
        Dim reg = observable.Subscribe(New Observer(Of T)(
            Sub(value) r.Add(value),
            Sub() s.TrySetResult(r),
            Sub([error]) s.TrySetException([error])))
        ct.Register(Sub()
                        reg.Dispose()
                        s.TrySetCanceled()
                    End Sub)
        Return s.Task
    End Function
    <Extension> <Pure>
    Public Function [Select](Of T, R)(observable As IObservable(Of T), projection As Func(Of T, R)) As IObservable(Of R)
        Return New Observable(Of R)(Function(observer) observable.Subscribe(New Observer(Of T)(
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
            AddressOf observer.OnError)))
    End Function
    <Extension> <Pure>
    Public Function Where(Of T)(observable As IObservable(Of T), filter As Func(Of T, Boolean)) As IObservable(Of T)
        Return New Observable(Of T)(Function(observer) observable.Subscribe(New Observer(Of T)(
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
            AddressOf observer.OnError)))
    End Function
End Module
