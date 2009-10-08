Public Enum LogMessageType
    Typical
    DataEvent
    DataParsed
    DataRaw
    Problem
    Negative
    Positive
End Enum

Public NotInheritable Class Logger
    Public Event LoggedMessage(ByVal type As LogMessageType, ByVal message As LazyValue(Of String))
    Public Event LoggedFutureMessage(ByVal placeholder As String, ByVal message As IFuture(Of String))
    Private ReadOnly ref As ICallQueue

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(ref IsNot Nothing)
    End Sub

    Public Sub New(Optional ByVal ref As ICallQueue = Nothing)
        Me.ref = If(ref, New ThreadPooledCallQueue())
    End Sub

    Public Sub FutureLog(ByVal placeholder As String, ByVal message As IFuture(Of String))
        ref.QueueAction(Sub()
                            Contract.Assume(Me IsNot Nothing)
                            RaiseEvent LoggedFutureMessage(placeholder, message)
                        End Sub)
    End Sub
    Public Sub Log(ByVal message As LazyValue(Of String), ByVal messageType As LogMessageType)
        Contract.Requires(message IsNot Nothing)
        ref.QueueAction(Sub()
                            Contract.Assume(Me IsNot Nothing)
                            RaiseEvent LoggedMessage(messageType, message)
                        End Sub)
    End Sub
    Public Sub Log(ByVal message As Func(Of String), ByVal messageType As LogMessageType)
        Contract.Requires(message IsNot Nothing)
        Log(New LazyValue(Of String)(message), messageType)
    End Sub
End Class
