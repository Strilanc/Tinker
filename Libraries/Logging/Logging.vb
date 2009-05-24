Namespace Logging
    Public Class SimpleLogger
        Public Event LoggedMessage(ByVal message As Func(Of String))
        Private ReadOnly ref As ICallQueue
        Public Sub New(Optional ByVal pq As ICallQueue = Nothing)
            Me.ref = If(pq, New ThreadedCallQueue(Me.GetType.Name))
        End Sub

        Private Sub log_L(ByVal message As Func(Of String))
            RaiseEvent LoggedMessage(message)
        End Sub

        Public Sub log(ByVal message As Func(Of String))
            ref.enqueue(Function() eval(AddressOf log_L, message))
        End Sub
        Public Sub log(ByVal message As String)
            log(Function() message)
        End Sub
    End Class

    Public Enum LogMessageTypes
        NormalEvent
        DataEvents
        ParsedData
        RawData
        Problem
        NegativeEvent
        PositiveEvent
    End Enum

    Public Class MultiLogger
        Private ReadOnly loggerMap As New Dictionary(Of LogMessageTypes, SimpleLogger)
        Private ReadOnly lock As New Object()
        Public Event LoggedMessage(ByVal type As LogMessageTypes, ByVal message As Func(Of String))
        Public Event LoggedFutureMessage(ByVal placeholder As String, ByVal message As IFuture(Of Outcome))
        Private ReadOnly ref As New ThreadedCallQueue(Me.GetType.Name)

        Private Function raise(ByVal type As LogMessageTypes, ByVal message As Func(Of String)) As Boolean
            RaiseEvent LoggedMessage(type, message)
            Return True
        End Function
        Private Function raisefuture(ByVal placeholder As String, ByVal message As IFuture(Of Outcome)) As Boolean
            RaiseEvent LoggedFutureMessage(placeholder, message)
            Return True
        End Function
        Public Sub futurelog(ByVal placeholder As String, ByVal message As IFuture(Of Outcome))
            ref.enqueue(Function() raisefuture(placeholder, message))
        End Sub

        Public ReadOnly Property loggers(ByVal t As LogMessageTypes) As SimpleLogger
            Get
                SyncLock lock
                    If Not loggerMap.ContainsKey(t) Then
                        loggerMap(t) = New SimpleLogger(Me.ref)
                        AddHandler loggerMap(t).LoggedMessage, Function(message) raise(t, message)
                    End If
                    Return loggerMap(t)
                End SyncLock
            End Get
        End Property

        Public Sub log(ByVal message As String, ByVal t As LogMessageTypes)
            loggers(t).log(message)
        End Sub
        Public Sub log(ByVal message As Func(Of String), ByVal t As LogMessageTypes)
            loggers(t).log(message)
        End Sub
    End Class

    '''<summary>Implements a simple way to log unexpected exceptions.</summary>
    '''<remarks>One of those rare cases where a global is appropriate.
    ''' [because multiple listeners and writers can not interfere with each other, except for parent functions]</remarks>
    Public Module UnexpectedExceptionLogging
        '''<summary>A simple logger for logging unexpected exceptions. Logged data is passed to all registered callbacks.
        ''' Avoid using parent functions, because they might be cleared elsewhere.</summary>
        Public ReadOnly UnexpectedExceptionLogger As New SimpleLogger()

        '''<summary>Logs detailed exception information to the Unexpected Exception Logger.</summary>
        Public Sub logUnexpectedException(ByVal context As String, ByVal e As Exception)
            If Not (e IsNot Nothing) Then Throw New ArgumentException()

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

            'Log Message
            Debug.Print(message)
            UnexpectedExceptionLogger.log(message)
        End Sub
    End Module
End Namespace
