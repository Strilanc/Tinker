Public Interface INotifyingDisposable
    Inherits IDisposable
    Event Disposed()
    ReadOnly Property IsDisposed As Boolean
End Interface

Public Class NotifyingDisposable
    Implements INotifyingDisposable

    Private ReadOnly lockDisposed As New OneTimeLock

    Public Event Disposed() Implements INotifyingDisposable.Disposed

    Public ReadOnly Property IsDisposed As Boolean Implements INotifyingDisposable.IsDisposed
        Get
            Return lockDisposed.WasAcquired
        End Get
    End Property

    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If lockDisposed.TryAcquire Then
            Dispose(True)
            RaiseEvent Disposed()
        End If
        GC.SuppressFinalize(Me)
    End Sub
    Protected NotOverridable Overrides Sub Finalize()
        If lockDisposed.TryAcquire Then
            Dispose(False)
        End If
    End Sub
End Class
