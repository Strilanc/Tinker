Public NotInheritable Class DelegatedDisposable
    Implements IDisposable
    Private ReadOnly _disposer As action
    Private ReadOnly _disposedLock As New OnetimeLock

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_disposer IsNot Nothing)
        Contract.Invariant(_disposedLock IsNot Nothing)
    End Sub

    Public Sub New(ByVal disposer As Action)
        Contract.Requires(disposer IsNot Nothing)
        Me._disposer = disposer
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If _disposedLock.TryAcquire Then _disposer()
    End Sub
End Class
