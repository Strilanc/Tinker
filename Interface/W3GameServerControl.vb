Imports HostBot.Warcraft3

Public Class W3GameServerControl
    Implements IHookable(Of IW3Server)

#Region "Variables"
    Private WithEvents server As IW3Server = Nothing
    Private ReadOnly uiRef As New InvokedCallQueue(Me)
    Private games As TabControlIHookableSet(Of IW3Game, W3GameControl)
#End Region

#Region "Hook"
    Private Function f_caption() As IFuture(Of String) Implements IHookable(Of IW3Server).f_caption
        Return uiRef.QueueFunc(Function() If(server Is Nothing, "[No Server]", "Server {0}{1}".frmt(server.name, server.suffix)))
    End Function

    Public Function f_hook(ByVal server As IW3Server) As IFuture Implements IHookable(Of IW3Server).f_Hook
        Return uiRef.QueueAction(
            Sub()
                If Me.server Is server Then  Return
                Me.server = Nothing
                If games IsNot Nothing Then
                    games.Clear()
                Else
                    games = New TabControlIHookableSet(Of IW3Game, W3GameControl)(tabsServer)
                End If
                Me.server = server

                Me.txtInfo.Text = ""
                If server Is Nothing Then
                    logServer.SetLogger(Nothing, Nothing)
                Else
                    logServer.SetLogger(server.logger, "Server")
                    server.f_EnumGames().CallWhenValueReady(
                        Sub(games)
                            uiRef.QueueAction(
                                Sub()
                                    If server IsNot Me.server Then  Return
                                    For Each game In games
                                        If Me.games.Contains(game) Then  Continue For
                                        Me.games.Add(game)
                                    Next game
                                End Sub
                            )
                        End Sub
                    )
                    Dim map = server.settings.map

                    Me.txtInfo.Text = ("Map Name{0}{1}{0}{0}" +
                                      "Relative Path{0}{2}{0}{0}" +
                                      "Map Type{0}{4}{0}{0}" +
                                      "Player Count{0}{5}{0}{0}" +
                                      "Force Count{0}{6}{0}{0}" +
                                      "Playable Size{0}{7} x {8}{0}{0}" +
                                      "File Size{0}{3:###,###,###,###} bytes{0}{0}" +
                                      "File Checksum (crc32){0}{9}{0}{0}" +
                                      "Map Checksum (xoro){0}{11}{0}{0}" +
                                      "Map Checksum (sha1){0}{10}{0}").frmt(Environment.NewLine,
                                                                            map.name,
                                                                            map.RelativePath,
                                                                            map.FileSize,
                                                                            If(map.isMelee, "Melee", "Custom"),
                                                                            map.NumPlayerSlots,
                                                                            map.numForces,
                                                                            map.playableWidth, map.playableHeight,
                                                                            map.Crc32.ToHexString,
                                                                            map.ChecksumSha1.ToHexString,
                                                                            map.ChecksumXoro.ToHexString)
                End If
            End Sub
        )
    End Function
#End Region

#Region "Events"
    Private Sub c_AddedGame(ByVal sender As IW3Server, ByVal instance As IW3Game) Handles server.AddedGame
        uiRef.QueueAction(
            Sub()
                If sender IsNot server Then  Return
                If Not games.Contains(instance) Then
                    games.Add(instance)
                End If
            End Sub
        )
    End Sub

    Private Sub c_RemovedGame(ByVal sender As IW3Server, ByVal instance As IW3Game) Handles server.RemovedGame
        uiRef.QueueAction(
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
        server.parent.ServerCommands.ProcessLocalText(server, txtCommand.Text, logServer.Logger())
        txtCommand.Text = ""
    End Sub
#End Region
End Class
