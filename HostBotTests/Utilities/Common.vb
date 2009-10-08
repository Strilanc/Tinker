Imports Strilbrary.Threading

Friend Module TestingCommon
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