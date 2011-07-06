'Verification disabled because of many warnings in generated code
<ContractVerification(False)>
Public Class ExceptionForm
    Private _exceptions As New List(Of Tuple(Of String, Exception))()
    Private inQueue As CallQueue = MakeControlCallQueue(Me)
    Private ReadOnly _addThrottle As New Throttle(1.Seconds, New PhysicalClock(), MakeThreadPoolSynchronizationContext())

    Public Sub New()
        InitializeComponent()
        AddHandler UnexpectedException, Sub(ex, context) inQueue.QueueAction(Sub() AddException(ex, context))
    End Sub

    Public Event ExceptionCountChanged(sender As ExceptionForm)
    Public ReadOnly Property ExceptionCount As Integer
        Get
            Return _exceptions.Count
        End Get
    End Property

    Private Sub AddException(ex As Exception, context As String)
        'Unwrap simple aggregated exceptions
        Dim ax = TryCast(ex, AggregateException)
        If ax IsNot Nothing AndAlso ax.InnerExceptions.Count = 1 AndAlso ax.Message = "One or more errors occurred." Then
            AddException(ax.InnerExceptions.Single, context)
            Return
        End If

        'Skip double-reported exceptions
        If (From entry In _exceptions.TakeLast(5)
            Where entry.Item2 Is ex
            Where entry.Item1 = context).Any Then Return

        'Log exception
        _exceptions.Add(Tuple.Create(context, ex))
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

    Private Sub ExceptionForm_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Me.Visible = False
        End If
    End Sub

    Private Sub ExceptionForm_VisibleChanged(sender As Object, e As System.EventArgs) Handles Me.VisibleChanged
        UpdateExceptionText()
    End Sub
End Class
