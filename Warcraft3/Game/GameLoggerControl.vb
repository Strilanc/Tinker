Imports Tinker.Pickling

Namespace WC3
    <ContractVerification(False)>
    Public Class GameLoggerControl
        Private WithEvents game As WC3.Game
        Private actionMode As CallbackMode = CallbackMode.Off
        Public Sub SetGame(ByVal game As WC3.Game)
            SyncLock lock
                If Me.game Is game Then Return
                Me.game = game
                SetLogger(If(game Is Nothing, Nothing, game.Logger), "Game")
            End SyncLock
        End Sub

        Private Sub OnCheckStateChangedActions() Handles chkActions.CheckStateChanged
            Select Case chkActions.CheckState
                Case CheckState.Checked : actionMode = CallbackMode.On
                Case CheckState.Indeterminate : actionMode = CallbackMode.File
                Case CheckState.Unchecked : actionMode = CallbackMode.Off
                Case Else
                    Throw chkActions.CheckState.MakeImpossibleValueException()
            End Select
        End Sub

        Private Sub OnPlayerAction(ByVal sender As WC3.Game,
                                   ByVal player As WC3.Player,
                                   ByVal actions As IRist(Of WC3.Protocol.GameAction)) Handles game.ReceivedPlayerActions
            Dim mode = actionMode
            If mode = CallbackMode.Off Then Return
            For Each action In actions
                Dim action_ = action
                LogMessage(New Lazy(Of String)(Function() "Action by {0}: {1}".Frmt(player.Name, action_)),
                           Color.DarkBlue,
                           mode = CallbackMode.File)
            Next action
        End Sub
    End Class
End Namespace
