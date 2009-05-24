Public Class BotWidgetControl
    Implements IHookable(Of IBotWidget)

#Region "Variables"
    Private WithEvents widget As IBotWidget
    Private uiRef As New InvokedCallQueue(Me)
#End Region

#Region "Hook"
    Private Function f_caption() As IFuture(Of String) Implements IHookable(Of IBotWidget).f_caption
        Return uiRef.enqueue(Function() If(widget Is Nothing, "[No Widget]", "{0} {1}".frmt(widget.type_name, widget.name)))
    End Function

    Public Function f_hook(ByVal widget As IBotWidget) As IFuture Implements IHookable(Of IBotWidget).f_hook
        Return uiRef.enqueue(Function() eval(AddressOf _f_hook, widget))
    End Function
    Private Sub _f_hook(ByVal widget As IBotWidget)
        If Me.widget Is widget Then Return

        Me.widget = Nothing
        Me.logControl.setLogger(Nothing, Nothing)
        lstState.Items.Clear()

        Me.widget = widget
        If widget IsNot Nothing Then
            Me.logControl.setLogger(widget.logger(), widget.type_name)
            widget.hooked()
        End If
    End Sub
#End Region

#Region "Events"
    Private Sub c_WidgetAddedStateString(ByVal state As String, ByVal insert_at_top As Boolean) Handles widget.add_state_string
        uiRef.enqueue(Function() eval(AddressOf _c_WidgetAddedStateString, state, insert_at_top))
    End Sub
    Private Sub _c_WidgetAddedStateString(ByVal state As String, ByVal insert_at_top As Boolean)
        If insert_at_top Then
            lstState.Items.Insert(0, state)
        Else
            lstState.Items.Add(state)
        End If
    End Sub

    Private Sub c_WidgetClearedStateStrings() Handles widget.clear_state_strings
        uiRef.enqueue(AddressOf _c_WidgetClearedStateStrings)
    End Sub
    Private Sub _c_WidgetClearedStateStrings()
        lstState.Items.Clear()
    End Sub

    Private Sub c_WidgetRemovedStateString(ByVal state As String) Handles widget.remove_state_string
        uiRef.enqueue(Function() eval(AddressOf _c_WidgetRemovedStateString, state))
    End Sub
    Private Sub _c_WidgetRemovedStateString(ByVal state As String)
        lstState.Items.Remove(state)
    End Sub

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        e.Handled = True
        widget.command(txtCommand.Text)
        txtCommand.Text = ""
    End Sub
#End Region
End Class
