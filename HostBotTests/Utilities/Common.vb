Imports Strilbrary.Threading

Friend Module TestingCommon
    Public Function BlockOnFutureValue(Of T)(ByVal future As IFuture(Of T)) As IFuture(Of T)
        If BlockOnFuture(future) Then
            Return future
        Else
            Throw New InvalidOperationException("The future did not terminate properly.")
        End If
    End Function
    Public Function BlockOnFuture(ByVal future As IFuture) As Boolean
        Return BlockOnFuture(future, New TimeSpan(0, 0, seconds:=100))
    End Function
    Public Function BlockOnFuture(ByVal future As IFuture,
                                  ByVal timeout As TimeSpan) As Boolean
        Dim waitHandle = New System.Threading.ManualResetEvent(initialState:=False)
        AddHandler future.Ready, Sub() waitHandle.Set()
        If future.State <> FutureState.Unknown Then waitHandle.Set()
        Return waitHandle.WaitOne(timeout)
    End Function
End Module