Imports HostBot.WC3

Public Class W3GameControl
    Implements IHookable(Of Game)
    Private WithEvents game As Game
    Private ReadOnly ref As New InvokedCallQueue(Me)

    Private commandHistory As New List(Of String) From {""}
    Private commandHistoryPointer As Integer
    Private Sub txtCommand_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles txtCommand.KeyDown
        Select Case e.KeyCode
            Case Keys.Enter
                If txtCommand.Text = "" Then Return
                game.QueueCommandProcessLocalText(txtCommand.Text, logGame.Logger())

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

    Private Function QueueDispose() As IFuture Implements IHookable(Of Game).QueueDispose
        Return ref.QueueAction(Sub() Me.Dispose())
    End Function

    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of WC3.Game).QueueGetCaption
        Return ref.QueueFunc(Function() If(game Is Nothing, "[No Game]", game.Name))
    End Function

    Public Function QueueHook(ByVal child As Game) As IFuture Implements IHookable(Of WC3.Game).QueueHook
        Return ref.QueueAction(
            Sub()
                For i = 0 To lstSlots.Items.Count - 1
                    lstSlots.Items(i) = "-"
                Next i
                Me.game = child
                If child Is Nothing Then
                    logGame.SetGame(Nothing)
                Else
                    logGame.SetGame(child)
                    child.QueueThrowUpdated()
                End If
            End Sub
        )
    End Function

    Private Sub txtInput_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtInput.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtInput.Text = "" Then Return
        If game Is Nothing Then Return
        game.QueueBroadcastMessage(txtInput.Text)
        txtInput.Text = ""
        e.Handled = True
    End Sub

    Private Sub CatchGameUpdated(ByVal sender As Game, ByVal slots As List(Of Slot)) Handles game.Updated
        Dim descriptions = (From slot In slots Select slot.GenerateDescription).ToList
        descriptions.Defuturized.QueueCallOnSuccess(ref,
            Sub()
                If sender IsNot game Then Return
                For i = 0 To descriptions.Count - 1
                    lstSlots.Items(i) = descriptions(i).Value
                Next i
            End Sub
         )
    End Sub
End Class
