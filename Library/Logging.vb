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
    Public Event LoggedMessage(type As LogMessageType, message As Lazy(Of String))
    Public Event LoggedFutureMessage(type As LogMessageType, placeholder As Func(Of String), message As Task(Of Func(Of String)))
    Private ReadOnly outQueue As CallQueue

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(outQueue IsNot Nothing)
    End Sub

    Public Sub New(Optional outQueue As CallQueue = Nothing)
        Me.outQueue = If(outQueue, MakeTaskedCallQueue())
    End Sub

    Public Sub FutureLog(placeholder As String, message As Task(Of String), messageType As LogMessageType)
        Contract.Requires(placeholder IsNot Nothing)
        Contract.Requires(message IsNot Nothing)
        Dim msg = Async Function() As Task(Of Func(Of String))
                      Dim r = Await message
                      Return Function() r
                  End Function()
        outQueue.QueueAction(Sub() RaiseEvent LoggedFutureMessage(messageType, Function() placeholder, msg))
    End Sub
    Public Sub FutureLog(placeholder As Func(Of String), message As Task(Of Func(Of String)), messageType As LogMessageType)
        Contract.Requires(placeholder IsNot Nothing)
        Contract.Requires(message IsNot Nothing)
        outQueue.QueueAction(Sub() RaiseEvent LoggedFutureMessage(messageType, placeholder, message))
    End Sub
    Public Sub Log(message As Lazy(Of String), messageType As LogMessageType)
        Contract.Requires(message IsNot Nothing)
        outQueue.QueueAction(Sub() RaiseEvent LoggedMessage(messageType, message))
    End Sub
    Public Sub Log(message As Func(Of String), messageType As LogMessageType)
        Contract.Requires(message IsNot Nothing)
        Log(New Lazy(Of String)(message), messageType)
    End Sub
    Public Sub Log(message As String, messageType As LogMessageType)
        Contract.Requires(message IsNot Nothing)
        Log(Function() message, messageType)
    End Sub
End Class
