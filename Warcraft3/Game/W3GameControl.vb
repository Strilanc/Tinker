Namespace WC3
    'Verification disabled because of many warnings in generated code
    <ContractVerification(False)>
    Public Class W3GameControl
        Private ReadOnly inQueue As CallQueue = MakeControlCallQueue(Me)
        Private ReadOnly _manager As WC3.GameManager
        Private ReadOnly _game As WC3.Game
        Private ReadOnly _hooks As New List(Of IDisposable)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_manager IsNot Nothing)
            Contract.Invariant(_game IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
        End Sub

        Private Shadows Sub OnDisposed() Handles Me.Disposed
            For Each hook In _hooks
                Contract.Assume(hook IsNot Nothing)
                hook.Dispose()
            Next hook
        End Sub
        Private Sub OnCommand(sender As CommandControl, argument As String) Handles comGame.IssuedCommand
            Contract.Requires(argument IsNot Nothing)
            Tinker.Components.UIInvokeCommand(_manager, argument)
        End Sub

        Public Sub New(manager As WC3.GameManager)
            Contract.Assert(manager IsNot Nothing)
            InitializeComponent()

            Me._manager = manager
            Me._game = manager.Game
            logGame.SetGame(_game)

            AddHandler _game.Updated, AddressOf OnGameUpdated
            _hooks.Add(New DelegatedDisposable(Sub() RemoveHandler _game.Updated, AddressOf OnGameUpdated))

            _game.QueueThrowUpdated()
        End Sub

        Private Sub txtInput_KeyPress(sender As Object, e As System.Windows.Forms.KeyPressEventArgs) Handles txtInput.KeyPress
            If e.KeyChar <> Microsoft.VisualBasic.ChrW(Keys.Enter) Then Return
            If txtInput.Text = "" Then Return
            _game.QueueBroadcastMessage(txtInput.Text)
            txtInput.Text = ""
            e.Handled = True
        End Sub

        Private Sub OnGameUpdated(sender As WC3.Game, slots As SlotSet)
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(slots IsNot Nothing)
            Dim descriptions = (From slot In slots Select slot.AsyncGenerateDescription).ToArray
            descriptions.AsAggregateTask.QueueContinueWithAction(inQueue,
                Sub()
                    If IsDisposed Then Return
                    For Each i In descriptions.Length.Range
                        lstSlots.Items(i) = descriptions(i).Result
                    Next i
                End Sub
             )
        End Sub
    End Class
End Namespace
