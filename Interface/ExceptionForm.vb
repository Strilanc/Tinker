<ContractVerification(False)>
Public Class ExceptionForm
    Private _exceptions As New List(Of Tuple(Of String, Exception))()
    Private inQueue As CallQueue = New InvokedCallQueue(Me, initiallyStarted:=True)
    Private ReadOnly _addThrottle As New Throttle(1.Seconds, New SystemClock())

    Public Sub New()
        InitializeComponent()
        AddHandler Strilbrary.Exceptions.UnexpectedException, Sub(ex, context) inQueue.QueueAction(Sub() AddException(ex, context))
    End Sub

    Public Event ExceptionCountChanged(ByVal sender As ExceptionForm)
    Public ReadOnly Property ExceptionCount As Integer
        Get
            Return _exceptions.Count
        End Get
    End Property

    Private Sub AddException(ByVal ex As Exception, ByVal context As String)
        'Skip double-reported exceptions
        For i = Math.Max(0, _exceptions.Count - 5) To _exceptions.Count - 1
            If _exceptions(i).Item2 Is ex AndAlso _exceptions(i).Item1 = context Then
                Return
            End If
        Next i

        _exceptions.Add(New Tuple(Of String, Exception)(context, ex))
        If Me.Visible Then
            If txtExceptions.SelectionStart < txtExceptions.TextLength Then
                btnUpdate.Visible = True
                lblBuffering.Visible = True
            Else
                _addThrottle.SetActionToRun(Sub() inQueue.QueueAction(Sub() UpdateExceptionText()))
            End If
        End If
        RaiseEvent ExceptionCountChanged(Me)
    End Sub
    Private Sub UpdateExceptionText() Handles btnUpdate.Click
        btnUpdate.Visible = False
        lblBuffering.Visible = False
        Dim descriptions = (From ex In _exceptions Select "{0}: {1}".Frmt(ex.Item1, ex.Item2))
        txtExceptions.Text = "{0} exceptions:".Frmt(_exceptions.Count) + Environment.NewLine +
                             descriptions.StringJoin(Environment.NewLine + New String("-"c, 20) + Environment.NewLine) + Environment.NewLine
        txtExceptions.Select(txtExceptions.TextLength, 0)
    End Sub

    Private Sub ExceptionForm_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Me.Visible = False
        End If
    End Sub

    Private Sub ExceptionForm_VisibleChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.VisibleChanged
        UpdateExceptionText()
    End Sub
End Class
