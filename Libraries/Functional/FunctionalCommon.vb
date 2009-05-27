Namespace Functional
#Region "Actions"
    Public Delegate Sub Action(Of T1, T2, T3, T4, T5)(ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4, ByVal arg5 As T5)
    Public Delegate Sub Action(Of T1, T2, T3, T4, T5, T6)(ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4, ByVal arg5 As T5, ByVal arg6 As T6)
    Public Delegate Sub Action(Of T1, T2, T3, T4, T5, T6, T7)(ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4, ByVal arg5 As T5, ByVal arg6 As T6, ByVal arg7 As T7)
#End Region

#Region "Outcomes"
    '''<summary>Stores the outcome of an operation that doesn't produce a value.</summary>
    Public Class Outcome
        Public ReadOnly succeeded As Boolean
        Public ReadOnly message As String
        Public Sub New(ByVal succeeded As Boolean, Optional ByVal message As String = Nothing)
            Me.message = message
            Me.succeeded = succeeded
        End Sub
    End Class

    '''<summary>Stores the outcome of an operation that produces a value.</summary>
    '''<typeparam name="R">The type of value produced by the operation</typeparam>
    Public Class Outcome(Of R)
        Public ReadOnly succeeded As Boolean
        Public ReadOnly message As String
        Public ReadOnly val As R
        Public Sub New(ByVal val As R, ByVal succeeded As Boolean, Optional ByVal message As String = Nothing)
            Me.succeeded = succeeded
            Me.message = message
            Me.val = val
        End Sub

        Public Shared Widening Operator CType(ByVal out As Outcome(Of R)) As Outcome
            Return New Outcome(out.succeeded, out.message)
        End Operator
        Public Shared Widening Operator CType(ByVal out As Outcome) As Outcome(Of R)
            Return New Outcome(Of R)(Nothing, out.succeeded, out.message)
        End Operator
    End Class
#End Region

    Public Module Common
#Region "ThreadedCall"
        Public Function ThreadedAction(ByVal action As Action, Optional ByVal threadName As String = Nothing) As IFuture
            Dim f As New Future
            Dim t = New Threading.Thread(
                Sub()
                    Try
                        Call action()
                        f.setReady()
                    Catch ex As Exception
                        Logging.LogUnexpectedException("Exception rose past ThreadedAction ({0}).".frmt(threadName), ex)
                    End Try
                End Sub
            )
            t.Name = If(threadName, "ThreadedAction")
            t.Start()
            Return f
        End Function
        Public Function ThreadedFunc(Of R)(ByVal func As Func(Of R), Optional ByVal threadName As String = Nothing) As IFuture(Of R)
            Dim f As New Future(Of R)
            Dim t = New Threading.Thread(
                Sub()
                    Try
                        f.setValue(func())
                    Catch ex As Exception
                        Logging.LogUnexpectedException("Exception rose past ThreadedFunc ({0}).".frmt(threadName), ex)
                    End Try
                End Sub
            )
            t.Name = If(threadName, "ThreadedFunc")
            t.Start()
            Return f
        End Function
#End Region

#Region "Outcomes"
        Public Function success(ByVal message As String) As Outcome
            Return New Outcome(succeeded:=True, message:=message)
        End Function
        Public Function successVal(Of R)(ByVal val As R, ByVal message As String) As Outcome(Of R)
            Return New Outcome(Of R)(val:=val, succeeded:=True, message:=message)
        End Function
        Public Function failure(ByVal message As String) As Outcome
            Return New Outcome(succeeded:=False, message:=message)
        End Function
        Public Function failureVal(Of R)(ByVal val As R, ByVal message As String) As Outcome(Of R)
            Return New Outcome(Of R)(val:=val, succeeded:=False, message:=message)
        End Function
#End Region

#Region "Future Casts"
        '''<summary>Casts a future's return type from a child type to a parent type.</summary>
        '''<typeparam name="P">The parent type.</typeparam>
        '''<typeparam name="C">The child type.</typeparam>
        '''<param name="f">The future to cast.</param>
        Public Function CTypeFuture(Of P, C As P)(ByVal f As IFuture(Of C)) As IFuture(Of P)
            Return FutureFunc.schedule(Function() CType(f.getValue(), P), {f})
        End Function
        '''<summary>Casts a future of an outcome with a value  to a future of an outcome without a value.</summary>
        '''<typeparam name="R">The type returned by the outcome with value.</typeparam>
        Public Function stripFutureOutcome(Of R)(ByVal f As IFuture(Of Outcome(Of R))) As IFuture(Of Outcome)
            Return FutureFunc.schedule(Function() CType(f.getValue(), Outcome), {f})
        End Function
#End Region
    End Module
End Namespace
