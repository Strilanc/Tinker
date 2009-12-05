
Public Class W3GameControl
    Private ReadOnly _manager As Components.WC3GameManager
    Private ReadOnly _game As WC3.Game
    Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))
    Private ReadOnly inQueue As New StartableCallQueue(New InvokedCallQueue(Me))

    Private Shadows Sub OnParentChanged() Handles Me.ParentChanged
        If Me.Parent IsNot Nothing Then inQueue.Start()
    End Sub
    Private Shadows Sub OnDisposed() Handles Me.Disposed
        For Each hook In _hooks
            hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
        Next hook
    End Sub
    Private Sub OnCommand(ByVal sender As CommandControl, ByVal argument As String) Handles comGame.IssuedCommand
        Tinker.Components.UIInvokeCommand(_manager, argument)
    End Sub

    Public Sub New(ByVal manager As Components.WC3GameManager)
        Contract.Requires(manager IsNot Nothing)
        InitializeComponent()

        Me._manager = manager
        Me._game = manager.Game
        logGame.SetGame(_game)

        AddHandler _game.Updated, AddressOf OnGameUpdated
        _hooks.Add(New DelegatedDisposable(Sub() RemoveHandler _game.Updated, AddressOf OnGameUpdated).Futurized)

        _game.QueueThrowUpdated()
    End Sub

    Private Sub txtInput_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtInput.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtInput.Text = "" Then Return
        _game.QueueBroadcastMessage(txtInput.Text)
        txtInput.Text = ""
        e.Handled = True
    End Sub

    Private Sub OnGameUpdated(ByVal sender As WC3.Game, ByVal slots As List(Of WC3.Slot))
        Dim descriptions = (From slot In slots Select slot.GenerateDescription).ToList
        descriptions.Defuturized.QueueCallOnSuccess(inQueue,
            Sub()
                If IsDisposed Then Return
                For i = 0 To descriptions.Count - 1
                    lstSlots.Items(i) = descriptions(i).Value
                Next i
            End Sub
         )
    End Sub
End Class
