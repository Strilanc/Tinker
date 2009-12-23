Imports Strilbrary.Threading
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Friend Module TestingCommon
    Public Sub ExpectException(Of E As Exception)(ByVal action As Action)
        Try
            Call action()
        Catch ex As E
            Return
        Catch ex As Exception
            Assert.Fail("Expected an exception of type " + GetType(E).ToString + " but hit an exception type " + ex.GetType.ToString)
        End Try
        Assert.Fail("Expected an exception of type " + GetType(E).ToString + " but hit no exception.")
    End Sub
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