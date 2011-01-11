'Verification disabled because of many warnings in generated code
<ContractVerification(False)>
Public Class LoggerControl
    Private callbackModeMap As New Dictionary(Of LogMessageType, CallbackMode)
    Private callbackColorMap As New Dictionary(Of LogMessageType, Color)
    Private WithEvents _logger As Logger
    Private ReadOnly uiRef As CallQueue = MakeControlCallQueue(Me)
    Private lastQueuedMessage As New QueuedMessage(Nothing, Color.Black)
    Private nextQueuedMessage As QueuedMessage
    Private numQueuedMessages As Integer
    Protected lock As New Object()
    Private _logFilename As String
    Private filestream As IO.Stream

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(callbackModeMap IsNot Nothing)
        Contract.Invariant(callbackColorMap IsNot Nothing)
        Contract.Invariant(uiRef IsNot Nothing)
        Contract.Invariant(lastQueuedMessage IsNot Nothing)
        Contract.Invariant(lastQueuedMessage.NextMessage Is Nothing)
        Contract.Invariant((_logger Is Nothing) = (_logFilename Is Nothing))
        Contract.Invariant((nextQueuedMessage IsNot Nothing) = (numQueuedMessages > 0))
    End Sub

    Private NotInheritable Class QueuedMessage
        Private ReadOnly _message As String
        Private ReadOnly _color As Color
        Private ReadOnly _replacement As QueuedMessage
        Private _nextMessage As QueuedMessage
        Public Property InsertPosition As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_message IsNot Nothing)
        End Sub

        Public Sub New(ByVal message As String, ByVal color As Color, Optional ByVal replacement As QueuedMessage = Nothing)
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Me.Message = message)
            Contract.Ensures(Me.Color = color)
            Contract.Ensures(Me.Replacement Is replacement)
            Contract.Ensures(Me.NextMessage Is Nothing)
            Me._message = message
            Me._color = color
            Me._replacement = replacement
        End Sub

        Public ReadOnly Property Message As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of String)() = _message)
                Return _message
            End Get
        End Property
        Public ReadOnly Property Color As Color
            Get
                Contract.Ensures(Contract.Result(Of Color)() = _color)
                Return _color
            End Get
        End Property
        Public ReadOnly Property Replacement As QueuedMessage
            Get
                Contract.Ensures(Contract.Result(Of QueuedMessage)() Is _replacement)
                Return _replacement
            End Get
        End Property
        Public Property NextMessage As QueuedMessage
            Get
                Contract.Ensures(Contract.Result(Of QueuedMessage)() Is _nextMessage)
                Return _nextMessage
            End Get
            Set(ByVal value As QueuedMessage)
                Contract.Requires(value IsNot Nothing)
                Contract.Requires(NextMessage Is Nothing)
                _nextMessage = value
            End Set
        End Property
    End Class
    Public Enum CallbackMode As Byte
        Unspecified = 0
        [On] = 1
        File = 2
        Off = 3
    End Enum

    Public Sub New()
        InitializeComponent()
        callbackModeMap(LogMessageType.Typical) = CallbackMode.On
        callbackModeMap(LogMessageType.Problem) = CallbackMode.On
        callbackModeMap(LogMessageType.Negative) = CallbackMode.On
        callbackModeMap(LogMessageType.Positive) = CallbackMode.On
        callbackModeMap(LogMessageType.DataEvent) = CallbackMode.Off
        callbackModeMap(LogMessageType.DataParsed) = CallbackMode.Off
        callbackModeMap(LogMessageType.DataRaw) = CallbackMode.Off
        callbackColorMap(LogMessageType.Typical) = Color.Black
        callbackColorMap(LogMessageType.DataEvent) = Color.DarkBlue
        callbackColorMap(LogMessageType.DataParsed) = Color.DarkBlue
        callbackColorMap(LogMessageType.DataRaw) = Color.DarkBlue
        callbackColorMap(LogMessageType.Problem) = Color.Red
        callbackColorMap(LogMessageType.Positive) = Color.DarkGreen
        callbackColorMap(LogMessageType.Negative) = Color.DarkOrange
    End Sub

#Region "State"
    Public Sub SetLogger(ByVal logger As Logger,
                         ByVal name As InvariantString,
                         Optional ByVal dataEventsMode As CallbackMode = CallbackMode.Unspecified,
                         Optional ByVal parsedDataMode As CallbackMode = CallbackMode.Unspecified,
                         Optional ByVal rawDataMode As CallbackMode = CallbackMode.Unspecified)
        SyncLock lock
            If Me._logger IsNot Nothing Then
                If filestream IsNot Nothing Then
                    filestream.Dispose()
                    filestream = Nothing
                    _logFilename = Nothing
                    tips.SetToolTip(chkSaveFile, "Determines if data is logged to a file.")
                End If
            End If
            Me._logger = logger
            If logger IsNot Nothing Then
                _logFilename = "{0} {1}.txt".Frmt(name, DateTime.Now().ToString("MMM d, yyyy, HH-mm-ss", CultureInfo.InvariantCulture))
                tips.SetToolTip(chkSaveFile, {"Outputs logged messages to a file.",
                                              "Unchecking does not remove messages from the file.",
                                              "Current Target File: '(Documents)\{0}\Logs\{1}'"
                                             }.StringJoin(Environment.NewLine).Frmt(Application.ProductName, _logFilename))
            End If
            If dataEventsMode <> CallbackMode.Unspecified Then
                callbackModeMap(LogMessageType.DataEvent) = dataEventsMode
                SyncToCheckbox(chkDataEvents, LogMessageType.DataEvent)
            End If
            If parsedDataMode <> CallbackMode.Unspecified Then
                callbackModeMap(LogMessageType.DataParsed) = parsedDataMode
                SyncToCheckbox(chkParsedData, LogMessageType.DataParsed)
            End If
            If rawDataMode <> CallbackMode.Unspecified Then
                callbackModeMap(LogMessageType.DataRaw) = rawDataMode
                SyncToCheckbox(chkRawData, LogMessageType.DataRaw)
            End If
        End SyncLock
    End Sub

    Private Function OpenSaveFile() As Boolean
        SyncLock lock
            If filestream IsNot Nothing Then
                filestream.Dispose()
                filestream = Nothing
            End If

            Dim folder = GetDataFolderPath("Logs")
            If folder Is Nothing Then
                LogMessage("Error opening log folder.", Color.Red)
                Return False
            End If

            Try
                filestream = New IO.FileStream(IO.Path.Combine(folder, _logFilename), IO.FileMode.Append, IO.FileAccess.Write, IO.FileShare.Read)
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse
                                       TypeOf ex Is Security.SecurityException
                Dim msg = "Error opening file for log {0}: {1}".Frmt(_logFilename, ex.Summarize)
                ex.RaiseAsUnexpected(msg)
                LogMessage(msg, Color.Red)
                Return False
            End Try
            Dim bb = ("-------------------------" + Environment.NewLine).ToAsciiBytes.ToArray
            filestream.Write(bb, 0, bb.Length)
            Return True
        End SyncLock
    End Function
    Public ReadOnly Property Logger() As Logger
        Get
            Return Me._logger
        End Get
    End Property

    Public Sub LogMessage(ByVal message As Lazy(Of String),
                          ByVal color As Color,
                          Optional ByVal fileOnly As Boolean = False)
        Contract.Requires(message IsNot Nothing)
        LogMessage(message.Value, color, fileOnly)
    End Sub
    Public Sub LogMessage(ByVal message As String,
                          ByVal color As Color,
                          Optional ByVal fileOnly As Boolean = False)
        Contract.Requires(message IsNot Nothing)
        LogMessage(New QueuedMessage(message, color), fileOnly)
    End Sub
    Private Sub LogMessage(ByVal message As QueuedMessage,
                           Optional ByVal fileOnly As Boolean = False)
        Contract.Requires(message IsNot Nothing)
        SyncLock lock
            If Not fileOnly Then
                lastQueuedMessage.NextMessage = message
                lastQueuedMessage = message
                nextQueuedMessage = If(nextQueuedMessage, message)
                numQueuedMessages += 1
            End If
            If filestream IsNot Nothing Then
                Dim data = "[{0}]: {1}{2}".Frmt(DateTime.Now().ToString("dd/MM/yy HH:mm:ss.ffff", CultureInfo.InvariantCulture),
                                                message.Message,
                                                Environment.NewLine).ToAsciiBytes.ToArray
                filestream.Write(data, 0, data.Length)
            End If
        End SyncLock

        If Not fileOnly Then
            uiRef.QueueAction(AddressOf EmptyQueue)
        End If
    End Sub

    Private Sub EmptyQueue()
        Try
            If txtLog.SelectionStart <> txtLog.TextLength Then
                SyncLock lock
                    btnUnbuffer.Text = "Unbuffer ({0})".Frmt(numQueuedMessages)
                    btnUnbuffer.Visible = True
                    lblBuffering.Visible = True
                End SyncLock
                Return
            End If

            'Buffer currently queued messages
            Dim bq As New Queue(Of QueuedMessage)
            SyncLock lock
                If nextQueuedMessage Is Nothing Then Return
                Do
                    bq.Enqueue(nextQueuedMessage)
                    nextQueuedMessage = nextQueuedMessage.NextMessage
                    numQueuedMessages -= 1
                Loop While nextQueuedMessage IsNot Nothing
            End SyncLock

            'Log buffered messages
            While bq.Count > 0
                'Get message
                Dim n = txtLog.Text.Length
                Dim em = bq.Dequeue()
                Dim msg = em.Message + Environment.NewLine
                em.InsertPosition = n

                'Combine messages if possible [operations on txtLog are very expensive because they cause redraws, this minimizes that]
                If em.Replacement Is Nothing Then
                    While bq.Count > 0 AndAlso bq.Peek().Color = em.Color AndAlso bq.Peek.Replacement Is Nothing
                        n += em.Message.Length + Environment.NewLine.Length
                        em.InsertPosition = n
                        em = bq.Dequeue()
                        msg += em.Message + Environment.NewLine
                    End While
                End If

                'Log message
                If em.Replacement IsNot Nothing Then
                    Dim dn = em.Message.Length - em.Replacement.Message.Length
                    Dim f = em.Replacement.NextMessage
                    While f IsNot Nothing
                        f.InsertPosition += dn
                        f = f.NextMessage
                    End While
                    em.InsertPosition = em.Replacement.InsertPosition
                    txtLog.Select(em.Replacement.InsertPosition, em.Replacement.Message.Length)
                    txtLog.SelectionColor = em.Color
                    txtLog.SelectedText = em.Message
                Else
                    Dim prevLength = txtLog.TextLength
                    txtLog.AppendText(msg)
                    txtLog.Select(prevLength, txtLog.TextLength - prevLength)
                    txtLog.SelectionColor() = em.Color
                End If
            End While

            txtLog.Select(txtLog.TextLength, 0)
        Catch e As InvalidOperationException
            e.RaiseAsUnexpected("Exception rose post LoggerControl.emptyQueue")
        End Try
    End Sub
    Private Sub LogFutureMessage(ByVal placeholder As String, ByVal futureMessage As Task(Of String))
        Contract.Requires(placeholder IsNot Nothing)
        Contract.Requires(futureMessage IsNot Nothing)

        Dim m = New QueuedMessage(placeholder, Color.DarkGoldenrod)
        LogMessage(m)
        futureMessage.ContinueWith(
            Sub(task)
                SyncLock lock
                    Dim message = If(task.Status = TaskStatus.Faulted,
                                     task.Exception.Summarize,
                                     task.Result)
                    Dim color = callbackColorMap(If(task.Status = TaskStatus.RanToCompletion AndAlso Not message Like "Failed: *",
                                                    LogMessageType.Positive,
                                                    LogMessageType.Problem))
                    LogMessage(New QueuedMessage(message, color, m))
                End SyncLock
            End Sub
        )
    End Sub
#End Region

#Region "Log Events"
    Private Sub OnLoggedMessage(ByVal type As LogMessageType,
                                ByVal message As Lazy(Of String)) Handles _logger.LoggedMessage
        Dim color As Color
        Dim fileOnly As Boolean
        SyncLock lock
            If callbackModeMap(type) = CallbackMode.Off Then Return
            color = callbackColorMap(type)
            fileOnly = callbackModeMap(type) = CallbackMode.File
        End SyncLock
        LogMessage(message, color, fileOnly)
    End Sub
    Private Sub OnLoggedFutureMessage(ByVal placeholder As String,
                                      ByVal out As Task(Of String)) Handles _logger.LoggedFutureMessage
        uiRef.QueueAction(Sub() LogFutureMessage(placeholder, out))
    End Sub
#End Region

#Region "UI Events"
    Private Sub OnCheckedChangedDataEvents() Handles chkDataEvents.CheckStateChanged
        SyncFromCheckbox(chkDataEvents, LogMessageType.DataEvent)
    End Sub
    Private Sub OnCheckChangedParsedData() Handles chkParsedData.CheckStateChanged
        SyncFromCheckbox(chkParsedData, LogMessageType.DataParsed)
    End Sub
    Private Sub OnCheckChangedRawData() Handles chkRawData.CheckStateChanged
        SyncFromCheckbox(chkRawData, LogMessageType.DataRaw)
    End Sub

    Private Sub SyncFromCheckbox(ByVal c As CheckBox, ByVal e As LogMessageType)
        SyncLock lock
            Select Case c.CheckState
                Case CheckState.Checked : callbackModeMap(e) = CallbackMode.On
                Case CheckState.Indeterminate : callbackModeMap(e) = CallbackMode.File
                Case CheckState.Unchecked : callbackModeMap(e) = CallbackMode.Off
            End Select
        End SyncLock
    End Sub
    Private Sub SyncToCheckbox(ByVal c As CheckBox, ByVal e As LogMessageType)
        SyncLock lock
            Select Case callbackModeMap(e)
                Case CallbackMode.On : c.CheckState = CheckState.Checked
                Case CallbackMode.File : c.CheckState = CheckState.Indeterminate
                Case CallbackMode.Off : c.CheckState = CheckState.Unchecked
            End Select
        End SyncLock
    End Sub

    Private Sub OnCheckChangedSaveFile() Handles chkSaveFile.CheckStateChanged
        SyncLock lock
            If chkSaveFile.Checked Then
                If Not OpenSaveFile() Then
                    chkSaveFile.Checked = False
                    Return
                End If
            Else
                SyncLock lock
                    If filestream IsNot Nothing Then
                        filestream.Dispose()
                        filestream = Nothing
                    End If
                End SyncLock
            End If
        End SyncLock
    End Sub

    Private Sub OnClickClear() Handles btnClear.Click
        txtLog.Clear()
    End Sub

    Private Sub OnDisposed() Handles Me.Disposed
        SetLogger(Nothing, Nothing)
    End Sub

    Private Sub OnSelectionChangedLog() Handles txtLog.SelectionChanged
        If txtLog.SelectionStart = txtLog.TextLength AndAlso lblBuffering.Visible Then
            lblBuffering.Visible = False
            btnUnbuffer.Visible = False
            SyncLock lock
                If numQueuedMessages > 0 Then
                    uiRef.QueueAction(AddressOf EmptyQueue)
                End If
            End SyncLock
        End If
    End Sub

    Private Sub btnUnbuffer_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnUnbuffer.Click
        txtLog.Select(txtLog.TextLength, 0)
    End Sub
#End Region
End Class
