Imports HostBot.Warcraft3

Public Class W3GameControl
    Implements IHookable(Of IW3Game)

#Region "Variables"
    Private WithEvents instance As IW3Game
    Private ReadOnly uiRef As New InvokedCallQueue(Me)
#End Region

#Region "Hook"
    Private Function f_caption() As IFuture(Of String) Implements IHookable(Of Warcraft3.IW3Game).f_caption
        Return uiRef.enqueue(Function() If(instance Is Nothing, "[No Instance]", instance.name))
    End Function

    Public Function f_hook(ByVal game As IW3Game) As IFuture Implements IHookable(Of Warcraft3.IW3Game).f_hook
        Return uiRef.enqueue(Function() eval(AddressOf _f_hook, game))
    End Function
    Private Sub _f_hook(ByVal game As IW3Game)
        For i As Integer = 0 To lstSlots.Items.Count - 1
            lstSlots.Items(i) = "-"
        Next i
        Me.instance = game
        If game Is Nothing Then
            logInstance.setLogger(Nothing, Nothing)
        Else
            logInstance.setLogger(game.logger, "Instance")
            game.f_ThrowUpdated()
        End If
    End Sub
#End Region

#Region "Events"
    Private Sub txtInput_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtInput.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtInput.Text = "" Then Return
        If instance Is Nothing Then Return
        instance.f_BroadcastMessage(txtInput.Text)
        instance.logger.log(My.Resources.ProgramName + ": " + txtInput.Text, LogMessageTypes.NormalEvent)
        txtInput.Text = ""
        e.Handled = True
    End Sub

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        If instance Is Nothing Then Return
        e.Handled = True
        instance.f_CommandProcessLocalText(txtCommand.Text, logInstance.getLogger())
        txtCommand.Text = ""
    End Sub

    Private Sub c_InstanceUpdated(ByVal sender As IW3Game, ByVal slotClones As List(Of W3Slot)) Handles instance.Updated
        uiRef.enqueue(Function() eval(AddressOf _c_InstanceUpdated, sender, slotClones))
    End Sub
    Private Sub _c_InstanceUpdated(ByVal sender As IW3Game, ByVal slotClones As List(Of W3Slot))
        If sender IsNot instance Then Return
        For i As Integer = 0 To slotClones.Count - 1
            lstSlots.Items(i) = slotClones(i).toString()
        Next i
    End Sub
#End Region
End Class
