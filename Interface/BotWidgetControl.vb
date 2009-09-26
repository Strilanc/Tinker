Public Class BotWidgetControl
    Implements IHookable(Of IBotWidget)
    Private WithEvents widget As IBotWidget
    Private ReadOnly ref As New InvokedCallQueue(Me)

    Private Function QueueDispose() As IFuture Implements IHookable(Of IBotWidget).QueueDispose
        Return ref.QueueAction(Sub() Me.Dispose())
    End Function

    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of IBotWidget).QueueGetCaption
        Return ref.QueueFunc(Function() If(widget Is Nothing, "[No Widget]", "{0} {1}".Frmt(widget.TypeName, widget.Name)))
    End Function

    Public Function QueueHook(ByVal widget As IBotWidget) As IFuture Implements IHookable(Of IBotWidget).QueueHook
        Return ref.QueueAction(
            Sub()
                If Me.widget Is widget Then  Return

                Me.widget = Nothing
                Me.logControl.SetLogger(Nothing, Nothing)
                lstState.Items.Clear()

                Me.widget = widget
                If widget IsNot Nothing Then
                    Me.logControl.SetLogger(widget.Logger(), widget.TypeName)
                    widget.Hooked()
                End If
            End Sub
        )
    End Function

    Private Sub CatchWidgetAddedStateString(ByVal state As String, ByVal insert_at_top As Boolean) Handles widget.AddStateString
        ref.QueueAction(
            Sub()
                If insert_at_top Then
                    lstState.Items.Insert(0, state)
                Else
                    lstState.Items.Add(state)
                End If
            End Sub
        )
    End Sub

    Private Sub CatchWidgetClearedStateStrings() Handles widget.ClearStateStrings
        ref.QueueAction(Sub() lstState.Items.Clear())
    End Sub

    Private Sub CatchWidgetRemovedStateString(ByVal state As String) Handles widget.RemoveStateString
        ref.QueueAction(Sub() lstState.Items.Remove(state))
    End Sub

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        e.Handled = True
        widget.ProcessCommand(txtCommand.Text)
        txtCommand.Text = ""
    End Sub
End Class
