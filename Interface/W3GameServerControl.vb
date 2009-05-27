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
        Return uiRef.enqueueFunc(Function() If(server Is Nothing, "[No Server]", "Server {0}{1}".frmt(server.name, server.suffix)))
    End Function

    Public Function f_hook(ByVal server As IW3Server) As IFuture Implements IHookable(Of IW3Server).f_hook
        Return uiRef.enqueueAction(
            Sub()
                If Me.server Is server Then  Return
                Me.server = Nothing
                If games IsNot Nothing Then
                    games.clear()
                Else
                    games = New TabControlIHookableSet(Of IW3Game, W3GameControl)(tabsServer.TabPages)
                End If
                Me.server = server

                Me.txtInfo.Text = ""
                If server Is Nothing Then
                    logServer.setLogger(Nothing, Nothing)
                Else
                    logServer.setLogger(server.logger, "Server")
                    FutureSub.frun( _
                            AddressOf r_AddExistingGames,
                            futurize(server),
                            server.f_EnumGames())
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
                                                                            map.relative_path,
                                                                            map.fileSize,
                                                                            If(map.isMelee, "Melee", "Custom"),
                                                                            map.numPlayerSlots,
                                                                            map.numForces,
                                                                            map.playableWidth, map.playableHeight,
                                                                            unpackHexString(map.crc32),
                                                                            unpackHexString(map.checksum_sha1),
                                                                            unpackHexString(map.checksum_xoro))
                End If
            End Sub
        )
    End Function

    Private Sub r_AddExistingGames(ByVal sender As IW3Server, ByVal games As IEnumerable(Of IW3Game))
        uiRef.enqueueAction(
            Sub()
                If sender IsNot server Then  Return
                For Each game In games
                    If Me.games.contains(game) Then  Continue For
                    Me.games.add(game)
                Next game
            End Sub
        )
    End Sub
#End Region

#Region "Events"
    Private Sub c_AddedGame(ByVal sender As IW3Server, ByVal instance As IW3Game) Handles server.AddedGame
        uiRef.enqueueAction(
            Sub()
                If sender IsNot server Then  Return
                If Not games.contains(instance) Then
                    games.add(instance)
                End If
            End Sub
        )
    End Sub

    Private Sub c_RemovedGame(ByVal sender As IW3Server, ByVal instance As IW3Game) Handles server.RemovedGame
        uiRef.enqueueAction(
            Sub()
                If sender IsNot server Then  Return
                If games.contains(instance) Then
                    games.remove(instance)
                End If
            End Sub
        )
    End Sub

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        e.Handled = True
        server.parent.server_commands.processLocalText(server, txtCommand.Text, logServer.getLogger())
        txtCommand.Text = ""
    End Sub
#End Region
End Class
