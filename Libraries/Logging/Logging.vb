Namespace Logging
    Public Enum LogMessageTypes
        Typical
        DataEvent
        DataParsed
        DataRaw
        Problem
        Negative
        Positive
    End Enum

    Public Class Logger
        Public Event LoggedMessage(ByVal type As LogMessageTypes, ByVal message As Func(Of String))
        Public Event LoggedFutureMessage(ByVal placeholder As String, ByVal message As IFuture(Of Outcome))
        Private ReadOnly ref As ICallQueue

        Public Sub New(Optional ByVal ref As ICallQueue = Nothing)
            Me.ref = If(ref, New ThreadPoolCallQueue())
        End Sub

        Public Sub futurelog(ByVal placeholder As String, ByVal message As IFuture(Of Outcome))
            ref.enqueueAction(
                Sub()
                    RaiseEvent LoggedFutureMessage(placeholder, message)
                End Sub
            )
        End Sub
        Public Sub log(ByVal message As Func(Of String), ByVal t As LogMessageTypes)
            ref.enqueueAction(
                Sub()
                    RaiseEvent LoggedMessage(t, message)
                End Sub
            )
        End Sub
        Public Sub log(ByVal message As String, ByVal t As LogMessageTypes)
            log(Function() message, t)
        End Sub
    End Class

    '''<summary>Implements a simple way to log unexpected exceptions.</summary>
    '''<remarks>One of those rare cases where a global is appropriate.</remarks>
    Public Module UnexpectedExceptionLogging
        Public Event CaughtUnexpectedException(ByVal context As String, ByVal e As Exception)
        Private ReadOnly ref As New ThreadedCallQueue("Unexpected Exceptions")

        Public Function GenerateUnexpectedExceptionDescription(ByVal context As String, ByVal e As Exception) As String
            If context Is Nothing Then Throw New ArgumentNullException("context")
            If e Is Nothing Then Throw New ArgumentNullException("e")

            'Generate Message
            Dim message As String
            message = "Context: " + context
            'exception information
            For inner_recurse = 0 To 10
                'info
                message += Environment.NewLine + "Exception Type: " + e.GetType.Name
                message += Environment.NewLine + "Exception Message: " + e.Message
                message += Environment.NewLine + "Stack Trace: " + Environment.NewLine + indent(e.StackTrace.ToString())
                'next
                e = e.InnerException
                If e Is Nothing Then Exit For
                message += Environment.NewLine + "[Inner Exception]"
            Next inner_recurse
            'wrapper formating
            message = "UNEXPECTED EXCEPTION:" + Environment.NewLine + indent(message)
            message = New String("!"c, 20) + Environment.NewLine + message
            message += Environment.NewLine + New String("!"c, 20)
            Return message
        End Function

        Public Sub LogUnexpectedException(ByVal context As String, ByVal e As Exception)
            If context Is Nothing Then Throw New ArgumentNullException("context")
            If e Is Nothing Then Throw New ArgumentNullException("e")
            ref.enqueueAction(
                Sub()
                    RaiseEvent CaughtUnexpectedException(context, e)
                End Sub
            )
        End Sub
    End Module
End Namespace
