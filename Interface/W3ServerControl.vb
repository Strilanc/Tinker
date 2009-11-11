Imports HostBot.Commands
Imports HostBot.Warcraft3

Public Class W3ServerControl
    Implements IHookable(Of W3Server)
    Private WithEvents server As W3Server = Nothing
    Private ReadOnly ref As ICallQueue = New InvokedCallQueue(Me)
    Private games As TabControlIHookableSet(Of W3Game, W3GameControl)

    Private commandHistory As New List(Of String) From {""}
    Private commandHistoryPointer As Integer
    Private Sub txtCommand_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles txtCommand.KeyDown
        Select Case e.KeyCode
            Case Keys.Enter
                If txtCommand.Text = "" Then Return
                server.Parent.ServerCommands.ProcessLocalText(server, txtCommand.Text, logServer.Logger())

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

    Private Function QueueDispose() As IFuture Implements IHookable(Of W3Server).QueueDispose
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(Sub() Me.Dispose())
    End Function

    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of W3Server).QueueGetCaption
        Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
        Return ref.QueueFunc(Function() If(server Is Nothing, "[No Server]", "Server {0}{1}".Frmt(server.Name, server.Suffix)))
    End Function

    Public Function QueueHook(ByVal child As W3Server) As IFuture Implements IHookable(Of W3Server).QueueHook
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(
            Sub()
                If Me.server Is child Then Return
                Me.server = Nothing
                If games IsNot Nothing Then
                    games.Clear()
                Else
                    games = New TabControlIHookableSet(Of W3Game, W3GameControl)(tabsServer)
                End If
                Me.server = child

                Me.txtInfo.Text = ""
                If child Is Nothing Then
                    logServer.SetLogger(Nothing, Nothing)
                Else
                    logServer.SetLogger(child.logger, "Server")
                    Dim map = child.Settings.Map

                    Dim info = "Map Name\n{0}\n\n" +
                               "Relative Path\n{1}\n\n" +
                               "Map Type\n{2}\n\n" +
                               "Player Count\n{3}\n\n" +
                               "Playable Size\n{4} x {5}\n\n" +
                               "File Size\n{6:###,###,###,###} bytes\n\n" +
                               "File Checksum (crc32)\n{7}\n\n" +
                               "Map Checksum (xoro)\n{8}\n\n" +
                               "Map Checksum (sha1)\n{9}\n"
                    info = info.Replace("\n", Environment.NewLine)
                    info = info.Frmt(map.name,
                                     map.RelativePath,
                                     If(map.isMelee, "Melee", "Custom"),
                                     map.NumPlayerSlots,
                                     map.playableWidth,
                                     map.playableHeight,
                                     map.FileSize,
                                     map.FileChecksumCRC32.Bytes.ToHexString,
                                     map.MapChecksumXORO.Bytes.ToHexString,
                                     map.MapChecksumSHA1.ToHexString)
                    txtInfo.Text = info

                    child.QueueGetGames().CallOnValueSuccess(Sub(games) ref.QueueAction(
                        Sub()
                            If child IsNot Me.server Then Return
                            For Each game In games
                                If Me.games.Contains(game) Then Continue For
                                Me.games.Add(game)
                            Next game
                        End Sub
                    ))
                            End If
                        End Sub
        )
    End Function

    Private Sub CatchAddedGame(ByVal sender As W3Server, ByVal instance As W3Game) Handles server.AddedGame
        ref.QueueAction(
            Sub()
                If sender IsNot server Then  Return
                If Not games.Contains(instance) Then
                    games.Add(instance)
                End If
            End Sub
        )
    End Sub

    Private Sub CatchRemovedGame(ByVal sender As W3Server, ByVal instance As W3Game) Handles server.RemovedGame
        ref.QueueAction(
            Sub()
                If sender IsNot server Then  Return
                If games.Contains(instance) Then
                    games.Remove(instance)
                End If
            End Sub
        )
    End Sub
End Class
