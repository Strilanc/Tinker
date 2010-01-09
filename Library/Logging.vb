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
    Public Event LoggedMessage(ByVal type As LogMessageType, ByVal message As Lazy(Of String))
    Public Event LoggedFutureMessage(ByVal placeholder As String, ByVal message As IFuture(Of String))
    Private ReadOnly outQueue As ICallQueue

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(outQueue IsNot Nothing)
    End Sub

    Public Sub New(Optional ByVal outQueue As ICallQueue = Nothing)
        Me.outQueue = If(outQueue, New TaskedCallQueue())
    End Sub

    Public Sub FutureLog(ByVal placeholder As String, ByVal message As IFuture(Of String))
        Contract.Requires(placeholder IsNot Nothing)
        Contract.Requires(message IsNot Nothing)
        outQueue.QueueAction(Sub() RaiseEvent LoggedFutureMessage(placeholder, message))
    End Sub
    Public Sub Log(ByVal message As Lazy(Of String), ByVal messageType As LogMessageType)
        Contract.Requires(message IsNot Nothing)
        outQueue.QueueAction(Sub() RaiseEvent LoggedMessage(messageType, message))
    End Sub
    Public Sub Log(ByVal message As Func(Of String), ByVal messageType As LogMessageType)
        Contract.Requires(message IsNot Nothing)
        Log(New Lazy(Of String)(message), messageType)
    End Sub
    Public Sub Log(ByVal message As String, ByVal messageType As LogMessageType)
        Contract.Requires(message IsNot Nothing)
        Log(Function() message, messageType)
    End Sub
End Class
