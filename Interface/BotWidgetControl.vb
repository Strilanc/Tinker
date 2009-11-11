Public Class BotWidgetControl
    Implements IHookable(Of IBotWidget)
    Private WithEvents widget As IBotWidget
    Private ReadOnly ref As New InvokedCallQueue(Me)

    Private commandHistory As New List(Of String) From {""}
    Private commandHistoryPointer As Integer
    Private Sub txtCommand_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles txtCommand.KeyDown
        Select Case e.KeyCode
            Case Keys.Enter
                If txtCommand.Text = "" Then Return
                widget.ProcessCommand(txtCommand.Text)

                commandHistoryPointer = commandHistory.Count
                commandHistory(commandHistoryPointer - 1) = txtCommand.Text
                commandHistory.Add("")
                txtCommand.Text = ""
                e.Handled = True
            Case Keys.Up
                commandHistory(commandHistoryPointer) = txtCommand.Text
                commandHistoryPointer = (commandHistoryPointer - 1).Between(0, commandHistory.Count - 1)
                txtCommand.Text = commandHistory(commandHistoryPointer)
                txtCommand.SelectionStart = txtCommand.TextLength
                e.Handled = True
            Case Keys.Down
                commandHistory(commandHistoryPointer) = txtCommand.Text
                commandHistoryPointer = (commandHistoryPointer + 1).Between(0, commandHistory.Count - 1)
                txtCommand.Text = commandHistory(commandHistoryPointer)
                txtCommand.SelectionStart = txtCommand.TextLength
                e.Handled = True
        End Select
    End Sub

    Private Function QueueDispose() As IFuture Implements IHookable(Of IBotWidget).QueueDispose
        Return ref.QueueAction(Sub() Me.Dispose())
    End Function

    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of IBotWidget).QueueGetCaption
        Return ref.QueueFunc(Function() If(widget Is Nothing, "[No Widget]", "{0} {1}".Frmt(widget.TypeName, widget.Name)))
    End Function

    Public Function QueueHook(ByVal child As IBotWidget) As IFuture Implements IHookable(Of IBotWidget).QueueHook
        Return ref.QueueAction(
            Sub()
                If Me.widget Is child Then  Return

                Me.widget = Nothing
                Me.logControl.SetLogger(Nothing, Nothing)
                lstState.Items.Clear()

                Me.widget = child
                If child IsNot Nothing Then
                    Me.logControl.SetLogger(child.Logger(), child.TypeName)
                    child.Hooked()
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
End Class
