Imports HostBot.Warcraft3

Public Class GameLoggerControl
    Private WithEvents game As W3Game
    Private actionMode As CallbackMode = CallbackMode.Off
    Public Sub SetGame(ByVal game As W3Game)
        SyncLock lock
            If Me.game Is game Then Return
            Me.game = game
            SetLogger(If(game Is Nothing, Nothing, game.logger), "Game")
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

    Private Sub OnPlayerAction(ByVal sender As Warcraft3.W3Game,
                               ByVal player As Warcraft3.W3Player,
                               ByVal action As W3GameAction) Handles game.PlayerAction
        Dim mode = actionMode
        If mode = CallbackMode.Off Then Return
        LogMessage(New LazyValue(Of String)(Function() "{0}: {1}".Frmt(player.name, action.Payload.Description.Value)),
                   Color.DarkBlue,
                   mode = CallbackMode.File)
    End Sub
End Class
