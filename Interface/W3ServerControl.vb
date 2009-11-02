Imports HostBot.Commands
Imports HostBot.Warcraft3

Public Class W3ServerControl
    Implements IHookable(Of W3Server)
    Private WithEvents server As W3Server = Nothing
    Private ReadOnly ref As ICallQueue = New InvokedCallQueue(Me)
    Private games As TabControlIHookableSet(Of W3Game, W3GameControl)

    Private Function QueueDispose() As IFuture Implements IHookable(Of W3Server).QueueDispose
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(Sub() Me.Dispose())
    End Function

    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of W3Server).QueueGetCaption
        Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
        Return ref.QueueFunc(Function() If(server Is Nothing, "[No Server]", "Server {0}{1}".Frmt(server.Name, server.GetSuffix)))
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

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        e.Handled = True
        server.Parent.ServerCommands.ProcessLocalText(server, txtCommand.Text, logServer.Logger())
        txtCommand.Text = ""
    End Sub
End Class
