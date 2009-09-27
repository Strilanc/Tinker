Public Class LoggerControl
    Private callbackModeMap As New Dictionary(Of LogMessageType, CallbackMode)
    Private callbackColorMap As New Dictionary(Of LogMessageType, Color)
    Private WithEvents _logger As Logger
    Private ReadOnly uiRef As New InvokedCallQueue(Me)
    Private lastQueuedMessage As New QueuedMessage(Nothing, Color.Black)
    Private nextQueuedMessage As QueuedMessage
    Private numQueuedMessages As Integer
    Protected lock As New Object()
    Private filename As String
    Private filestream As IO.Stream
    Private isLoggingUnexpectedExceptions As Boolean

    Private Class QueuedMessage
        Public ReadOnly message As String
        Public ReadOnly color As Color
        Public ReadOnly replacement As QueuedMessage
        Public nextMessage As QueuedMessage
        Public insertPosition As Integer
        Public Sub New(ByVal message As String, ByVal color As Color)
            Me.message = message
            Me.color = color
        End Sub
        Public Sub New(ByVal message As String, ByVal color As Color, ByVal replacement As QueuedMessage)
            Me.message = message
            Me.color = color
            Me.replacement = replacement
        End Sub
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
    Public Sub SetLogUnexpected(ByVal value As Boolean)
        SyncLock lock
            If value = isLoggingUnexpectedExceptions Then Return
            isLoggingUnexpectedExceptions = value
            If isLoggingUnexpectedExceptions Then
                AddHandler CaughtUnexpectedException, AddressOf OnLoggedUnexpectedException
            Else
                RemoveHandler CaughtUnexpectedException, AddressOf OnLoggedUnexpectedException
            End If
        End SyncLock
    End Sub

    Public Sub SetLogger(ByVal logger As Logger,
                         ByVal name As String,
                         Optional ByVal dataEventsMode As CallbackMode = CallbackMode.Unspecified,
                         Optional ByVal parsedDataMode As CallbackMode = CallbackMode.Unspecified,
                         Optional ByVal rawDataMode As CallbackMode = CallbackMode.Unspecified)
        SyncLock lock
            If Me._logger IsNot Nothing Then
                If filestream IsNot Nothing Then
                    filestream.Dispose()
                    filestream = Nothing
                    filename = Nothing
                    tips.SetToolTip(chkSaveFile, "Determines if data is logged to a file.")
                End If
            End If
            Me._logger = logger
            If logger IsNot Nothing Then
                filename = name + " " + DateTime.Now().ToString("MMM d, yyyy, HH-mm-ss", CultureInfo.InvariantCulture) + ".txt"
                tips.SetToolTip(chkSaveFile, "Outputs logged messages to a file." + vbNewLine + _
                                             "Unchecking does not remove messages from the file." + vbNewLine + _
                                             "Current Target File: '(Documents)\HostBot\Logs\" + filename + "'")
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
                filestream = New IO.FileStream(folder + IO.Path.DirectorySeparatorChar + filename, IO.FileMode.Append, IO.FileAccess.Write, IO.FileShare.Read)
            Catch e As Exception
                LogUnexpectedException("Error opening log file '" + filename + "' in My Documents\HostBot Logs.", e)
                LogMessage("Error opening log file '" + filename + "' in Documents.", Color.Red)
                Return False
            End Try
            Dim bb = ("-------------------------" + Environment.NewLine).ToAscBytes
            filestream.Write(bb, 0, bb.Length)
            Return True
        End SyncLock
    End Function
    Public ReadOnly Property Logger() As Logger
        Get
            Return Me._logger
        End Get
    End Property

    Public Sub LogMessage(ByVal message As ExpensiveValue(Of String),
                          ByVal color As Color,
                          Optional ByVal fileOnly As Boolean = False)
        LogMessage(message.Value, color, fileOnly)
    End Sub
    Public Sub LogMessage(ByVal message As String,
                          ByVal color As Color,
                          Optional ByVal fileOnly As Boolean = False)
        LogMessage(New QueuedMessage(message, color), fileOnly)
    End Sub
    Private Sub LogMessage(ByVal message As QueuedMessage,
                           Optional ByVal fileOnly As Boolean = False)
        SyncLock lock
            If Not fileOnly Then
                lastQueuedMessage.nextMessage = message
                lastQueuedMessage = message
                nextQueuedMessage = If(nextQueuedMessage, message)
                numQueuedMessages += 1
            End If
            If filestream IsNot Nothing Then
                Dim data = (("[{0}]: {1}{2}").Frmt(DateTime.Now().ToString("dd/MM/yy HH:mm:ss.ffff", CultureInfo.InvariantCulture), message.message, Environment.NewLine)).ToAscBytes
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
                    lblNumBuffered.Text = "({0} messages buffered)".Frmt(numQueuedMessages)
                    lblNumBuffered.Visible = True
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
                    nextQueuedMessage = nextQueuedMessage.nextMessage
                    numQueuedMessages -= 1
                Loop While nextQueuedMessage IsNot Nothing
            End SyncLock

            'Log buffered messages
            While bq.Count > 0
                'Get message
                Dim n = txtLog.Text.Length
                Dim em = bq.Dequeue()
                Dim msg = em.message + Environment.NewLine
                em.insertPosition = n

                'Combine messages if possible [operations on txtLog are very expensive because they cause redraws, this minimizes that]
                If em.replacement Is Nothing Then
                    While bq.Count > 0 AndAlso bq.Peek().color = em.color AndAlso bq.Peek.replacement Is Nothing
                        n += em.message.Length + Environment.NewLine.Length
                        em.insertPosition = n
                        em = bq.Dequeue()
                        msg += em.message + Environment.NewLine
                    End While
                End If

                'Log message
                If em.replacement IsNot Nothing Then
                    Dim dn = em.message.Length - em.replacement.message.Length
                    Dim f = em.replacement.nextMessage
                    While f IsNot Nothing
                        f.insertPosition += dn
                        f = f.nextMessage
                    End While
                    em.insertPosition = em.replacement.insertPosition
                    txtLog.Select(em.replacement.insertPosition, em.replacement.message.Length)
                    txtLog.SelectionColor = em.color
                    txtLog.SelectedText = em.message
                Else
                    Dim prevLength = txtLog.TextLength
                    txtLog.AppendText(msg)
                    txtLog.Select(prevLength, txtLog.TextLength - prevLength)
                    txtLog.SelectionColor() = em.color
                End If
            End While

            txtLog.Select(txtLog.TextLength, 0)
        Catch e As Exception
            LogUnexpectedException("Exception rose post LoggerControl.emptyQueue", e)
        End Try
    End Sub
    Private Sub LogFutureMessage(ByVal placeholder As String,
                                 ByVal futureMessage As IFuture(Of String))
        Dim m = New QueuedMessage(placeholder, Color.DarkGoldenrod)
        LogMessage(m)
        futureMessage.CallWhenValueReady(
            Sub(message, messageException)
                SyncLock lock
                    If messageException IsNot Nothing Then  message = messageException.ToString
                    Dim color = callbackColorMap(If(messageException Is Nothing AndAlso Not message Like "Failed: *",
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
                                ByVal message As ExpensiveValue(Of String)) Handles _logger.LoggedMessage
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
                                      ByVal out As IFuture(Of String)) Handles _logger.LoggedFutureMessage
        uiRef.QueueAction(Sub() LogFutureMessage(placeholder, out))
    End Sub
    Private Sub OnLoggedUnexpectedException(ByVal context As String,
                                            ByVal exception As Exception)
        LogMessage(GenerateUnexpectedExceptionDescription(context, exception), Color.Red)
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
                If filestream IsNot Nothing Then
                    filestream.Dispose()
                    filestream = Nothing
                End If
            End If
        End SyncLock
    End Sub

    Private Sub OnClickClear() Handles btnClear.Click
        txtLog.Clear()
    End Sub

    Private Sub OnDisposed() Handles Me.Disposed
        SetLogger(Nothing, Nothing)
        SetLogUnexpected(False)
    End Sub

    Private Sub OnSelectionChangedLog() Handles txtLog.SelectionChanged
        If txtLog.SelectionStart = txtLog.TextLength AndAlso lblBuffering.Visible Then
            lblBuffering.Visible = False
            lblNumBuffered.Visible = False
            SyncLock lock
                If numQueuedMessages > 0 Then
                    uiRef.QueueAction(AddressOf EmptyQueue)
                End If
            End SyncLock
        End If
    End Sub
#End Region
End Class
