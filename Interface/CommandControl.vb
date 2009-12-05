Public Class CommandControl
    Private commandHistoryPointer As Integer
    Private ReadOnly commandHistory As New List(Of String) From {""}

    Public Event IssuedCommand(ByVal sender As CommandControl, ByVal argument As String)

    Private Sub txtCommand_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles txtCommand.KeyDown
        Select Case e.KeyCode
            Case Keys.Enter
                If txtCommand.Text = "" Then Return
                RaiseEvent IssuedCommand(Me, txtCommand.Text)

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

    Private Sub BotWidgetControl_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        If Me.Height <> txtCommand.Height Then Me.Height = txtCommand.Height
    End Sub
End Class
