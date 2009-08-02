Namespace Functional
    <DebuggerDisplay("{ToString}")>
    Public Structure PossibleException(Of T, E As Exception)
        Public ReadOnly Exception As E
        Public ReadOnly Value As T
        Public Sub New(ByVal value As T)
            Me.Value = value
        End Sub
        Public Sub New(ByVal exception As E)
            If exception Is Nothing Then Throw New ArgumentException("exception")
            Me.Exception = exception
        End Sub
        Public Sub New(ByVal partialValue As T, ByVal exception As E)
            If exception Is Nothing Then Throw New ArgumentException("exception")
            Me.Value = partialValue
            Me.Exception = exception
        End Sub
        Public Shared Widening Operator CType(ByVal value As T) As PossibleException(Of T, E)
            Return New PossibleException(Of T, E)(value)
        End Operator
        Public Shared Widening Operator CType(ByVal exception As E) As PossibleException(Of T, E)
            Return New PossibleException(Of T, E)(exception)
        End Operator
        Public Overrides Function ToString() As String
            Return "Value: {0}{1}Exception: {2}".frmt(If(Value.IsNullReferenceGeneric, "Null", Value.ToString),
                                                      Environment.NewLine,
                                                      If(Exception Is Nothing, "Null", Exception.ToString))
        End Function
    End Structure

#Region "Outcomes"
    '''<summary>Stores the outcome of an operation that doesn't produce a value.</summary>
    Public Structure Outcome
        Public ReadOnly succeeded As Boolean
        Private ReadOnly _message As String
        Public ReadOnly Property Message As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Contract.Assume(_message IsNot Nothing) 'I hope you didn't use default(Outcome)!
                Return _message
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub Invariant()
            Contract.Invariant(_message IsNot Nothing)
        End Sub

        Public Sub New(ByVal succeeded As Boolean, ByVal message As String)
            Contract.Requires(message IsNot Nothing)
            Me._message = message
            Me.succeeded = succeeded
        End Sub
    End Structure

    '''<summary>Stores the outcome of an operation that produces a value.</summary>
    '''<typeparam name="R">The type of value produced by the operation</typeparam>
    '''<remarks>Doesn't inherit from Outcome to allow implicit conversions from Outcome to Outcome(Of R), using CType.</remarks>
    Public Structure Outcome(Of R)
        Public ReadOnly succeeded As Boolean
        Public ReadOnly val As R
        Private ReadOnly _message As String
        Public ReadOnly Property Message As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Contract.Assume(_message IsNot Nothing) 'I hope you didn't use default(Outcome)!
                Return _message
            End Get
        End Property
        <ContractInvariantMethod()> Private Sub Invariant()
            Contract.Invariant(_message IsNot Nothing)
        End Sub
        Public ReadOnly Property Outcome As Outcome
            Get
                Return CType(Me, Outcome)
            End Get
        End Property

        Public Sub New(ByVal val As R, ByVal succeeded As Boolean, Optional ByVal message As String = Nothing)
            Contract.Requires(message IsNot Nothing)
            If message Is Nothing Then Throw New ArgumentNullException("message")
            Me.succeeded = succeeded
            Me._message = message
            Me.val = val
        End Sub

        Public Shared Widening Operator CType(ByVal out As Outcome(Of R)) As Outcome
            Return New Outcome(out.succeeded, out.Message)
        End Operator
        Public Shared Widening Operator CType(ByVal out As Outcome) As Outcome(Of R)
            Return New Outcome(Of R)(Nothing, out.succeeded, out.Message)
        End Operator
    End Structure
#End Region

    Public Module Common
#Region "ThreadedCall"
        Public Function ThreadedAction(ByVal action As Action, Optional ByVal threadName As String = Nothing) As IFuture
            Contract.Requires(action IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim action_ = action 'avoid contract verification issue on hoisted arguments
            Dim f As New Future
            Dim t = New Threading.Thread(
                Sub()
                    If My.Settings.debugMode Then
                        Call action_()
                        Call f.SetReady()
                    Else
                        Try
                            Call action_()
                            Call f.SetReady()
                        Catch ex As Exception
                            Logging.LogUnexpectedException("Exception rose past ThreadedAction ({0}).".frmt(threadName), ex)
                        End Try
                    End If
                End Sub
            )
            t.Name = If(threadName, "ThreadedAction")
            t.Start()
            Return f
        End Function
        Public Function ThreadedFunc(Of R)(ByVal func As Func(Of R), Optional ByVal threadName As String = Nothing) As IFuture(Of R)
            Contract.Requires(func IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of R))() IsNot Nothing)
            Dim func_ = func 'avoid contract verification issue on hoisted arguments
            Dim f As New Future(Of R)
            ThreadedAction(Sub() f.SetValue(func_()), threadName)
            Return f
        End Function

        Public Function ThreadPooledAction(ByVal action As Action) As IFuture
            Contract.Requires(action IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim action_ = action 'avoid contract verification issue on hoisted arguments
            Dim f As New Future
            Threading.ThreadPool.QueueUserWorkItem(
                Sub()
                    If My.Settings.debugMode Then
                        Call action_()
                        Call f.SetReady()
                    Else
                        Try
                            Call action_()
                            Call f.SetReady()
                        Catch ex As Exception
                            Logging.LogUnexpectedException("Exception rose past ThreadPooledAction.", ex)
                        End Try
                    End If
                End Sub
            )
            Return f
        End Function
        Public Function ThreadPooledFunc(Of R)(ByVal func As Func(Of R)) As IFuture(Of R)
            Contract.Requires(func IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of R))() IsNot Nothing)
            Dim func_ = func 'avoid contract verification issue on hoisted arguments
            Dim f As New Future(Of R)
            ThreadPooledAction(Sub() f.SetValue(func_()))
            Return f
        End Function
#End Region

#Region "Outcomes"
        Public Function success(ByVal message As String) As Outcome
            Contract.Requires(message IsNot Nothing)
            Return New Outcome(succeeded:=True, message:=message)
        End Function
        Public Function successVal(Of R)(ByVal val As R, ByVal message As String) As Outcome(Of R)
            Contract.Requires(message IsNot Nothing)
            Return New Outcome(Of R)(val:=val, succeeded:=True, message:=message)
        End Function
        Public Function failure(ByVal message As String) As Outcome
            Contract.Requires(message IsNot Nothing)
            Return New Outcome(succeeded:=False, message:=message)
        End Function
        Public Function failure(Of R)(ByVal message As String) As Outcome(Of R)
            Contract.Requires(message IsNot Nothing)
            Return failureVal(Of R)(Nothing, message)
        End Function
        Public Function failureVal(Of R)(ByVal val As R, ByVal message As String) As Outcome(Of R)
            Contract.Requires(message IsNot Nothing)
            Return New Outcome(Of R)(val:=val, succeeded:=False, message:=message)
        End Function
#End Region

#Region "Future Casts"
        '''<summary>Casts a future of an outcome with a value  to a future of an outcome without a value.</summary>
        '''<typeparam name="R">The type returned by the outcome with value.</typeparam>
        <Pure()>
        Public Function stripFutureOutcome(Of R)(ByVal f As IFuture(Of Outcome(Of R))) As IFuture(Of Outcome)
            Contract.Requires(f IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Dim f_ = f
            Return f_.EvalWhenReady(Function() CType(f_.Value(), Outcome))
        End Function
#End Region
    End Module
End Namespace
