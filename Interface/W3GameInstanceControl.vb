Imports HostBot.Warcraft3

Public Class W3GameControl
    Implements IHookable(Of IW3Game)
    Private WithEvents game As IW3Game
    Private ReadOnly uiRef As New InvokedCallQueue(Me)

#Region "Hook"
    Private Function f_caption() As IFuture(Of String) Implements IHookable(Of Warcraft3.IW3Game).f_caption
        Return uiRef.QueueFunc(Function() If(game Is Nothing, "[No Instance]", game.name))
    End Function

    Public Function f_hook(ByVal game As IW3Game) As IFuture Implements IHookable(Of Warcraft3.IW3Game).f_hook
        Return uiRef.QueueAction(
            Sub()
                For i As Integer = 0 To lstSlots.Items.Count - 1
                    lstSlots.Items(i) = "-"
                Next i
                Me.game = game
                If game Is Nothing Then
                    logInstance.setLogger(Nothing, Nothing)
                Else
                    logInstance.setLogger(game.logger, "Instance")
                    game.f_ThrowUpdated()
                End If
            End Sub
        )
    End Function
#End Region

#Region "Events"
    Private Sub txtInput_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtInput.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtInput.Text = "" Then Return
        If game Is Nothing Then Return
        game.f_BroadcastMessage(txtInput.Text)
        txtInput.Text = ""
        e.Handled = True
    End Sub

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        If game Is Nothing Then Return
        e.Handled = True
        game.f_CommandProcessLocalText(txtCommand.Text, logInstance.getLogger())
        txtCommand.Text = ""
    End Sub

    Private Sub c_InstanceUpdated(ByVal sender As IW3Game, ByVal slotClones As List(Of W3Slot)) Handles game.Updated
        uiRef.QueueAction(
            Sub()
                If sender IsNot game Then  Return
                For i As Integer = 0 To slotClones.Count - 1
                    lstSlots.Items(i) = slotClones(i).toString()
                Next i
            End Sub
         )
    End Sub
#End Region
End Class
