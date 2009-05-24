'''<summary>Implements some of the mundane work for enumeration. I wish we could yield.</summary>
Public MustInherit Class BaseEnumerator(Of T)
    Implements IEnumerator(Of T)

    Public MustOverride ReadOnly Property Current() As T Implements IEnumerator(Of T).Current
    Public MustOverride Function MoveNext() As Boolean Implements Collections.IEnumerator.MoveNext
    Public MustOverride Sub Reset() Implements Collections.IEnumerator.Reset

    Protected Overridable Sub DisposeSelf()
    End Sub
    Protected Overridable Sub DisposeOthers()
    End Sub

    Private ReadOnly Property CurrentObj() As Object Implements Collections.IEnumerator.Current
        Get
            Return Current()
        End Get
    End Property

    Private disposedValue As Boolean = False
    Private Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then DisposeOthers()
            DisposeSelf()
        End If
        Me.disposedValue = True
    End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
End Class

'''<summary>Implements some of the mundane work for enumeration. I wish we could yield.</summary>
Public MustInherit Class BaseIterator(Of T)
    Inherits BaseEnumerator(Of T)
    Implements IEnumerable(Of T)

    Public MustOverride Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
    Private Function GetEnumeratorObj() As Collections.IEnumerator Implements Collections.IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function
End Class
