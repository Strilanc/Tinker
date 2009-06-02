Imports HostBot.Logging
Imports HostBot.Logging.Logger

Public Class LoggerControl
    Private callback_mode As New Dictionary(Of LogMessageTypes, CallbackMode)
    Private callback_colors As New Dictionary(Of LogMessageTypes, Color)
    Private WithEvents logger As Logger
    Private blockEvents As Boolean = False
    Private ReadOnly uiRef As New InvokedCallQueue(Me)
    Private last_queued_message As New QueuedMessage(Nothing, Color.Black)
    Private next_queued_message As QueuedMessage
    Private num_queued_message As Integer
    Private lock As New Object()
    Private filename As String = Nothing
    Private filestream As IO.Stream = Nothing

#Region "Inner"
    Private Class QueuedMessage
        Public ReadOnly message As String
        Public ReadOnly color As Color
        Public ReadOnly replacement As QueuedMessage
        Public next_message As QueuedMessage
        Public inserted_char As Integer = 0
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
#End Region

#Region "Life"
    Public Sub New()
        InitializeComponent()
        callback_mode(LogMessageTypes.Typical) = CallbackMode.On
        callback_mode(LogMessageTypes.Problem) = CallbackMode.On
        callback_mode(LogMessageTypes.Negative) = CallbackMode.On
        callback_mode(LogMessageTypes.Positive) = CallbackMode.On
        callback_mode(LogMessageTypes.DataEvent) = CallbackMode.Off
        callback_mode(LogMessageTypes.DataParsed) = CallbackMode.Off
        callback_mode(LogMessageTypes.DataRaw) = CallbackMode.Off
        callback_colors(LogMessageTypes.Typical) = Color.Black
        callback_colors(LogMessageTypes.DataEvent) = Color.DarkBlue
        callback_colors(LogMessageTypes.DataParsed) = Color.DarkBlue
        callback_colors(LogMessageTypes.DataRaw) = Color.DarkBlue
        callback_colors(LogMessageTypes.Problem) = Color.Red
        callback_colors(LogMessageTypes.Positive) = Color.DarkGreen
        callback_colors(LogMessageTypes.Negative) = Color.DarkOrange
    End Sub
#End Region

#Region "State"
    Private unex_reged As Boolean = False
    Public Sub setLogUnexpected(ByVal b As Boolean)
        SyncLock lock
            If b = unex_reged Then Return
            unex_reged = b
            If unex_reged Then
                AddHandler Logging.CaughtUnexpectedException, AddressOf catch_logerror
            Else
                RemoveHandler Logging.CaughtUnexpectedException, AddressOf catch_logerror
            End If
        End SyncLock
    End Sub

    Public Sub setLogger(ByVal logger As Logger,
                         ByVal name As String,
                         Optional ByVal log_data_events As CallbackMode = CallbackMode.Unspecified,
                         Optional ByVal log_data_parsed As CallbackMode = CallbackMode.Unspecified,
                         Optional ByVal log_data_raw As CallbackMode = CallbackMode.Unspecified)
        SyncLock lock
            blockEvents = True
            If Me.logger IsNot Nothing Then
                If filestream IsNot Nothing Then
                    filestream.Dispose()
                    filestream = Nothing
                    filename = Nothing
                    tips.SetToolTip(chkSaveFile, "Determines if data is logged to a file.")
                End If
            End If
            Me.logger = logger
            If logger IsNot Nothing Then
                filename = name + " " + DateTime.Now().ToString("MMM d, yyyy, HH-mm-ss") + ".txt"
                tips.SetToolTip(chkSaveFile, "Outputs logged messages to a file." + vbNewLine + _
                                             "Unchecking does not remove messages from the file." + vbNewLine + _
                                             "Current Target File: '(My Documents)\HostBot\Logs\" + filename + "'")
            End If
            If log_data_events <> CallbackMode.Unspecified Then
                callback_mode(LogMessageTypes.DataEvent) = log_data_events
                sync_to_checkbox(chkDataEvents, LogMessageTypes.DataEvent)
            End If
            If log_data_parsed <> CallbackMode.Unspecified Then
                callback_mode(LogMessageTypes.DataParsed) = log_data_parsed
                sync_to_checkbox(chkParsedData, LogMessageTypes.DataParsed)
            End If
            If log_data_raw <> CallbackMode.Unspecified Then
                callback_mode(LogMessageTypes.DataRaw) = log_data_raw
                sync_to_checkbox(chkRawData, LogMessageTypes.DataRaw)
            End If
            blockEvents = False
        End SyncLock
    End Sub

    Private Function open_save_file() As Boolean
        SyncLock lock
            If filestream IsNot Nothing Then
                filestream.Dispose()
                filestream = Nothing
            End If

            Dim folder = GetDataFolderPath("Logs")
            If folder Is Nothing Then
                logMessage("Error opening log folder.", Color.Red)
                Return False
            End If

            Try
                filestream = New IO.FileStream(folder + IO.Path.DirectorySeparatorChar + filename, IO.FileMode.Append, IO.FileAccess.Write, IO.FileShare.Read)
            Catch e As Exception
                Logging.logUnexpectedException("Error opening log file '" + filename + "' in My Documents\HostBot Logs.", e)
                logMessage("Error opening log file '" + filename + "' in My Documents\HostBot Logs.", Color.Red)
                Return False
            End Try
            Dim bb = packString("-------------------------" + Environment.NewLine)
            filestream.Write(bb, 0, bb.Length)
            Return True
        End SyncLock
    End Function
    Public Function getLogger() As Logger
        Return Me.logger
    End Function

    Public Sub logMessage(ByVal sf As Func(Of String), ByVal c As Color, Optional ByVal file_only As Boolean = False)
        logMessage(sf(), c, file_only)
    End Sub
    Public Sub logMessage(ByVal s As String, ByVal c As Color, Optional ByVal file_only As Boolean = False)
        logMessage(New QueuedMessage(s, c), file_only)
    End Sub
    Private Sub logMessage(ByVal m As QueuedMessage, Optional ByVal file_only As Boolean = False)
        SyncLock lock
            If Not file_only Then
                last_queued_message.next_message = m
                last_queued_message = m
                If next_queued_message Is Nothing Then
                    next_queued_message = m
                End If
                num_queued_message += 1
            End If
            If filestream IsNot Nothing Then
                Dim bb = packString(("[{0}]: {1}" + Environment.NewLine).frmt(DateTime.Now().ToString("dd/MM/yy HH:mm:ss.ffff"), m.message))
                filestream.Write(bb, 0, bb.Length)
            End If
        End SyncLock

        If Not file_only Then
            uiRef.QueueAction(AddressOf emptyQueue_UI)
        End If
    End Sub

    Private Sub emptyQueue_UI()
        Try
            If txtLog.SelectionStart <> txtLog.TextLength Then
                SyncLock lock
                    lblNumBuffered.Text = "({0} messages buffered)".frmt(num_queued_message)
                    lblNumBuffered.Visible = True
                    lblBuffering.Visible = True
                End SyncLock
                Return
            End If

            'Buffer currently queued messages
            Dim bq As New Queue(Of QueuedMessage)
            SyncLock lock
                If next_queued_message Is Nothing Then Return
                Do
                    bq.Enqueue(next_queued_message)
                    next_queued_message = next_queued_message.next_message
                    num_queued_message -= 1
                Loop While next_queued_message IsNot Nothing
            End SyncLock

            'Log buffered messages
            While bq.Count > 0
                'Get message
                Dim n = txtLog.Text.Length
                Dim e = bq.Dequeue()
                Dim msg = e.message + Environment.NewLine
                e.inserted_char = n

                'Combine messages if possible [operations on txtLog are very expensive because they cause redraws, this minimizes that]
                If e.replacement Is Nothing Then
                    While bq.Count > 0 AndAlso bq.Peek().color = e.color AndAlso bq.Peek.replacement Is Nothing
                        n += e.message.Length + Environment.NewLine.Length
                        e.inserted_char = n
                        e = bq.Dequeue()
                        msg += e.message + Environment.NewLine
                    End While
                End If

                'Log message
                If e.replacement IsNot Nothing Then
                    Dim dn = e.message.Length - e.replacement.message.Length
                    Dim f = e.replacement.next_message
                    While f IsNot Nothing
                        f.inserted_char += dn
                        f = f.next_message
                    End While
                    e.inserted_char = e.replacement.inserted_char
                    txtLog.Select(e.replacement.inserted_char, e.replacement.message.Length)
                    txtLog.SelectionColor = e.color
                    txtLog.SelectedText = e.message
                Else
                    Dim prevLength = txtLog.TextLength
                    txtLog.AppendText(msg)
                    txtLog.Select(prevLength, txtLog.TextLength - prevLength)
                    txtLog.SelectionColor() = e.color
                End If
            End While

            txtLog.Select(txtLog.TextLength, 0)
        Catch e As ObjectDisposedException
            'happens sometimes when control disposed
        Catch e As Exception
            Logging.logUnexpectedException("Exception rose post LoggerControl.emptyQueue", e)
        End Try
    End Sub
    Private Sub logFutureMessage(ByVal placeholder As String, ByVal fout As IFuture(Of Outcome))
        SyncLock lock
            Dim m = New QueuedMessage(placeholder, Color.DarkGoldenrod)
            logMessage(m)
            FutureSub.Call(
                fout,
                Sub(out)
                    SyncLock lock
                        Dim color = callback_colors(If(out.succeeded, LogMessageTypes.Positive, LogMessageTypes.Problem))
                        logMessage(New QueuedMessage(out.message, color, m))
                    End SyncLock
                End Sub
            )
        End SyncLock
    End Sub
#End Region

#Region "Log Events"
    Private Sub catch_log(ByVal type As LogMessageTypes, ByVal message As Func(Of String)) Handles logger.LoggedMessage
        SyncLock lock
            If callback_mode(type) = CallbackMode.Off Then Return
            logMessage(message, callback_colors(type), callback_mode(type) = CallbackMode.File)
        End SyncLock
    End Sub
    Private Sub catch_log_future(ByVal placeholder As String, ByVal out As IFuture(Of Outcome)) Handles logger.LoggedFutureMessage
        uiRef.QueueAction(Sub() logFutureMessage(placeholder, out))
    End Sub
    Private Sub catch_logerror(ByVal context As String, ByVal e As Exception)
        logMessage(GenerateUnexpectedExceptionDescription(context, e), Color.Red)
    End Sub
#End Region

#Region "UI Events"
    Private Sub chkDataEvents_CheckedChanged() Handles chkDataEvents.CheckStateChanged
        sync_from_checkbox(chkDataEvents, LogMessageTypes.DataEvent)
    End Sub
    Private Sub chkParsedData_CheckedChanged() Handles chkParsedData.CheckStateChanged
        sync_from_checkbox(chkParsedData, LogMessageTypes.DataParsed)
    End Sub
    Private Sub chkRawData_CheckedChanged() Handles chkRawData.CheckStateChanged
        sync_from_checkbox(chkRawData, LogMessageTypes.DataRaw)
    End Sub

    Private Sub sync_from_checkbox(ByVal c As CheckBox, ByVal e As LogMessageTypes)
        SyncLock lock
            Select Case c.CheckState
                Case CheckState.Checked : callback_mode(e) = CallbackMode.On
                Case CheckState.Indeterminate : callback_mode(e) = CallbackMode.File
                Case CheckState.Unchecked : callback_mode(e) = CallbackMode.Off
            End Select
        End SyncLock
    End Sub
    Private Sub sync_to_checkbox(ByVal c As CheckBox, ByVal e As LogMessageTypes)
        SyncLock lock
            Select Case callback_mode(e)
                Case CallbackMode.On : c.CheckState = CheckState.Checked
                Case CallbackMode.File : c.CheckState = CheckState.Indeterminate
                Case CallbackMode.Off : c.CheckState = CheckState.Unchecked
            End Select
        End SyncLock
    End Sub

    Private Sub chkSaveFile_CheckStateChanged() Handles chkSaveFile.CheckStateChanged
        SyncLock lock
            If chkSaveFile.Checked Then
                If Not open_save_file() Then
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

    Private Sub btnClear_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnClear.Click
        txtLog.Clear()
    End Sub

    Private Sub LoggerControl_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        setLogger(Nothing, Nothing)
        setLogUnexpected(False)
    End Sub

    Private Sub txtLog_SelectionChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtLog.SelectionChanged
        If txtLog.SelectionStart = txtLog.TextLength AndAlso lblBuffering.Visible Then
            lblBuffering.Visible = False
            lblNumBuffered.Visible = False
            SyncLock lock
                If num_queued_message > 0 Then
                    uiRef.QueueAction(AddressOf emptyQueue_UI)
                End If
            End SyncLock
        End If
    End Sub
#End Region
End Class
