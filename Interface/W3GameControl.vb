Imports HostBot.Warcraft3

Public Class W3GameControl
    Implements IHookable(Of W3Game)
    Private WithEvents game As W3Game
    Private ReadOnly ref As New InvokedCallQueue(Me)

    Private Function QueueDispose() As IFuture Implements IHookable(Of W3Game).QueueDispose
        Return ref.QueueAction(Sub() Me.Dispose())
    End Function

    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of Warcraft3.W3Game).QueueGetCaption
        Return ref.QueueFunc(Function() If(game Is Nothing, "[No Game]", game.Name))
    End Function

    Public Function QueueHook(ByVal game As W3Game) As IFuture Implements IHookable(Of Warcraft3.W3Game).QueueHook
        Return ref.QueueAction(
            Sub()
                For i = 0 To lstSlots.Items.Count - 1
                    lstSlots.Items(i) = "-"
                Next i
                Me.game = game
                If game Is Nothing Then
                    logGame.SetGame(Nothing)
                Else
                    logGame.SetGame(game)
                    game.QueueThrowUpdated()
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

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        If game Is Nothing Then Return
        e.Handled = True
        game.QueueCommandProcessLocalText(txtCommand.Text, logGame.Logger())
        txtCommand.Text = ""
    End Sub

    Private Sub CatchGameUpdated(ByVal sender As W3Game, ByVal slots As List(Of W3Slot)) Handles game.Updated
        Dim descriptions = (From slot In slots Select slot.GenerateDescription).ToList
        descriptions.Defuturized.QueueCallWhenReady(ref,
            Sub()
                If sender IsNot game Then  Return
                For i = 0 To descriptions.Count - 1
                    lstSlots.Items(i) = descriptions(i).Value
                Next i
            End Sub
         )
    End Sub
End Class
