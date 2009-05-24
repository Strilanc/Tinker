Namespace Functional
#Region "Actions"
    Public Delegate Sub Action(Of T1, T2, T3, T4, T5)(ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4, ByVal arg5 As T5)
    Public Delegate Sub Action(Of T1, T2, T3, T4, T5, T6)(ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4, ByVal arg5 As T5, ByVal arg6 As T6)
    Public Delegate Sub Action(Of T1, T2, T3, T4, T5, T6, T7)(ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4, ByVal arg5 As T5, ByVal arg6 As T6, ByVal arg7 As T7)
#End Region

#Region "Outcomes"
    '''<summary>A set of common operation outcomes.</summary>
    Public Enum Outcomes
        '''<summary>Indicates the operation completed succesfully.</summary>
        succeeded
        '''<summary>Indicates the operation failed to complete succesfully.</summary>
        failed
    End Enum

    '''<summary>Stores the outcome of an operation that doesn't produce a value.</summary>
    Public Class Outcome
        Public ReadOnly outcome As Outcomes
        Public ReadOnly message As String
        Public Sub New(ByVal outcome As Outcomes, Optional ByVal message As String = Nothing)
            Me.message = message
            Me.outcome = outcome
        End Sub
    End Class

    '''<summary>Stores the outcome of an operation that produces a value.</summary>
    '''<typeparam name="R">The type of value produced by the operation</typeparam>
    Public Class Outcome(Of R)
        Public ReadOnly outcome As Outcomes
        Public ReadOnly message As String
        Public ReadOnly val As R
        Public Sub New(ByVal val As R, ByVal outcome As Outcomes, Optional ByVal message As String = Nothing)
            Me.outcome = outcome
            Me.message = message
            Me.val = val
        End Sub

        Public Shared Widening Operator CType(ByVal out As Outcome(Of R)) As Outcome
            Return New Outcome(out.outcome, out.message)
        End Operator
        Public Shared Widening Operator CType(ByVal out As Outcome) As Outcome(Of R)
            Return New Outcome(Of R)(Nothing, out.outcome, out.message)
        End Operator
    End Class
#End Region

    Public Module Common
#Region "ThreadedCall"
        '''<summary>Runs a call on a new thread.</summary>
        Public Function threadedCall(ByVal s As Action, Optional ByVal name As String = Nothing) As Threading.Thread
            Dim t = New Threading.Thread(AddressOf threadedCallHelper)
            If name IsNot Nothing Then t.Name = name
            t.Start(s)
            Return t
        End Function
        Private Sub threadedCallHelper(ByVal o As Object)
            Dim e = debug_trap(CType(o, Action))
            If e IsNot Nothing Then
                Logging.logUnexpectedException("Exception rose past threadedCallHelper ({0}).".frmt(Threading.Thread.CurrentThread.Name), e)
            End If
        End Sub
#End Region

#Region "Eval"
        ''' <summary>
        ''' Returns true and runs the Action if passed a non-null Action.
        ''' Used for placing subroutines inside closures/lambda expressions.
        ''' </summary>
        Public Function eval(ByVal s As Action) As Boolean
            If s Is Nothing Then Return False
            Call s()
            Return True
        End Function
        ''' <summary>
        ''' Returns true and runs the Action with the given arguments if passed a non-null Action.
        ''' Used for placing subroutines inside closures/lambda expressions.
        ''' </summary>
        Public Function eval(Of T1)(ByVal s As Action(Of T1), ByVal arg1 As T1) As Boolean
            If s Is Nothing Then Return False
            Call s(arg1)
            Return True
        End Function
        ''' <summary>
        ''' Returns true and runs the Action with the given arguments if passed a non-null Action.
        ''' Used for placing subroutines inside closures/lambda expressions.
        ''' </summary>
        Public Function eval(Of T1, T2)(ByVal s As Action(Of T1, T2), ByVal arg1 As T1, ByVal arg2 As T2) As Boolean
            If s Is Nothing Then Return False
            Call s(arg1, arg2)
            Return True
        End Function
        ''' <summary>
        ''' Returns true and runs the Action with the given arguments if passed a non-null Action.
        ''' Used for placing subroutines inside closures/lambda expressions.
        ''' </summary>
        Public Function eval(Of T1, T2, T3)(ByVal s As Action(Of T1, T2, T3), ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3) As Boolean
            If s Is Nothing Then Return False
            Call s(arg1, arg2, arg3)
            Return True
        End Function
        ''' <summary>
        ''' Returns true and runs the Action with the given arguments if passed a non-null Action.
        ''' Used for placing subroutines inside closures/lambda expressions.
        ''' </summary>
        Public Function eval(Of T1, T2, T3, T4)(ByVal s As Action(Of T1, T2, T3, T4), ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4) As Boolean
            If s Is Nothing Then Return False
            Call s(arg1, arg2, arg3, arg4)
            Return True
        End Function
        ''' <summary>
        ''' Returns true and runs the Action with the given arguments if passed a non-null Action.
        ''' Used for placing subroutines inside closures/lambda expressions.
        ''' </summary>
        Public Function eval(Of T1, T2, T3, T4, T5)(ByVal s As Action(Of T1, T2, T3, T4, T5), ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4, ByVal arg5 As T5) As Boolean
            If s Is Nothing Then Return False
            Call s(arg1, arg2, arg3, arg4, arg5)
            Return True
        End Function
        ''' <summary>
        ''' Returns true and runs the Action with the given arguments if passed a non-null Action.
        ''' Used for placing subroutines inside closures/lambda expressions.
        ''' </summary>
        Public Function eval(Of T1, T2, T3, T4, T5, T6)(ByVal s As Action(Of T1, T2, T3, T4, T5, T6), ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4, ByVal arg5 As T5, ByVal arg6 As T6) As Boolean
            If s Is Nothing Then Return False
            Call s(arg1, arg2, arg3, arg4, arg5, arg6)
            Return True
        End Function
        ''' <summary>
        ''' Returns true and runs the Action with the given arguments if passed a non-null Action.
        ''' Used for placing subroutines inside closures/lambda expressions.
        ''' </summary>
        Public Function eval(Of T1, T2, T3, T4, T5, T6, T7)(ByVal s As Action(Of T1, T2, T3, T4, T5, T6, T7), ByVal arg1 As T1, ByVal arg2 As T2, ByVal arg3 As T3, ByVal arg4 As T4, ByVal arg5 As T5, ByVal arg6 As T6, ByVal arg7 As T7) As Boolean
            If s Is Nothing Then Return False
            Call s(arg1, arg2, arg3, arg4, arg5, arg6, arg7)
            Return True
        End Function
#End Region

#Region "Outcomes"
        Public Function success(ByVal message As String) As Outcome
            Return New Outcome(Outcomes.succeeded, message)
        End Function
        Public Function successVal(Of R)(ByVal val As R, ByVal message As String) As Outcome(Of R)
            Return New Outcome(Of R)(val, Outcomes.succeeded, message)
        End Function
        Public Function failure(ByVal message As String) As Outcome
            Return New Outcome(Outcomes.failed, message)
        End Function
        Public Function failureVal(Of R)(ByVal val As R, ByVal message As String) As Outcome(Of R)
            Return New Outcome(Of R)(val, Outcomes.failed, message)
        End Function
#End Region

#Region "Future Casts"
        '''<summary>Casts a future's return type from a child type to a parent type.</summary>
        '''<typeparam name="P">The parent type.</typeparam>
        '''<typeparam name="C">The child type.</typeparam>
        '''<param name="f">The future to cast.</param>
        Public Function CTypeFuture(Of P, C As P)(ByVal f As IFuture(Of C)) As IFuture(Of P)
            Return FutureFunc(Of P).schedule(Function() f.getValue(), New IFuture() {f})
        End Function
        '''<summary>Casts a future of an outcome with a value  to a future of an outcome without a value.</summary>
        '''<typeparam name="R">The type returned by the outcome with value.</typeparam>
        Public Function stripFutureOutcome(Of R)(ByVal f As IFuture(Of Outcome(Of R))) As IFuture(Of Outcome)
            Return FutureFunc(Of Outcome).schedule(Function() f.getValue(), New IFuture() {f})
        End Function
#End Region
    End Module
End Namespace
