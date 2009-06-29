Public Interface INotifyingDisposable
    Inherits IDisposable
    Event Disposed()
    ReadOnly Property IsDisposed As Boolean
End Interface

Public Class NotifyingDisposable
    Implements INotifyingDisposable

    Private ReadOnly lockDisposed As New OneTimeLock

    Public Event Disposed() Implements INotifyingDisposable.Disposed

    Protected Overridable Sub PerformDispose()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If lockDisposed.TryAcquire Then
            GC.SuppressFinalize(Me)
            PerformDispose()
            RaiseEvent Disposed()
        End If
    End Sub

    Public ReadOnly Property IsDisposed As Boolean Implements INotifyingDisposable.IsDisposed
        Get
            Return lockDisposed.WasAcquired
        End Get
    End Property
End Class
