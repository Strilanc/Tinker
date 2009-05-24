Imports HostBot.Warcraft3

Public Class W3GameServerControl
    Implements IHookable(Of IW3Server)

#Region "Variables"
    Private WithEvents server As IW3Server = Nothing
    Private ReadOnly uiRef As New InvokedCallQueue(Me)
    Private instances As TabControlIHookableSet(Of IW3Game, W3GameControl)
#End Region

#Region "Hook"
    Private Function f_caption() As IFuture(Of String) Implements IHookable(Of IW3Server).f_caption
        Return uiRef.enqueue(Function() If(server Is Nothing, "[No Server]", "Server {0}{1}".frmt(server.name, server.suffix)))
    End Function

    Public Function f_hook(ByVal server As IW3Server) As IFuture Implements IHookable(Of IW3Server).f_hook
        Return uiRef.enqueue(Function() eval(AddressOf _f_hook, server))
    End Function
    Private Sub _f_hook(ByVal server As IW3Server)
        If Me.server Is server Then Return
        Me.server = Nothing
        If instances IsNot Nothing Then
            instances.clear()
        Else
            instances = New TabControlIHookableSet(Of IW3Game, W3GameControl)(tabsServer.TabPages)
        End If
        Me.server = server

        Me.txtInfo.Text = ""
        If server Is Nothing Then
            logServer.setLogger(Nothing, Nothing)
        Else
            logServer.setLogger(server.logger, "Server")
            FutureSub.frun( _
                    AddressOf r_AddExistingGames, _
                    futurize(server), _
                    server.f_EnumGames())
            Dim map = server.settings.map

            Me.txtInfo.Text = ("Map Name{0}{1}{0}{0}" + _
                              "Relative Path{0}{2}{0}{0}" + _
                              "Map Type{0}{4}{0}{0}" + _
                              "Player Count{0}{5}{0}{0}" + _
                              "Force Count{0}{6}{0}{0}" + _
                              "Playable Size{0}{7} x {8}{0}{0}" + _
                              "File Size{0}{3:###,###,###,###} bytes{0}{0}" + _
                              "File Checksum (crc32){0}{9}{0}{0}" + _
                              "Map Checksum (xoro){0}{11}{0}{0}" + _
                              "Map Checksum (sha1){0}{10}{0}").frmt(Environment.NewLine, _
                                                                map.name, _
                                                                map.relative_path, _
                                                                map.fileSize, _
                                                                If(map.isMelee, "Melee", "Custom"), _
                                                                map.numPlayerSlots, _
                                                                map.numForces, _
                                                                map.playableWidth, map.playableHeight, _
                                                                unpackHexString(map.crc32), _
                                                                unpackHexString(map.checksum_sha1), _
                                                                unpackHexString(map.checksum_xoro))
        End If
    End Sub

    Private Sub r_AddExistingGames(ByVal sender As IW3Server, ByVal games As IEnumerable(Of IW3Game))
        uiRef.enqueue(Function() eval(AddressOf _r_AddExistingGames, sender, games))
    End Sub
    Private Sub _r_AddExistingGames(ByVal sender As IW3Server, ByVal games As IEnumerable(Of IW3Game))
        If sender IsNot server Then Return
        For Each game In games
            If instances.contains(game) Then Continue For
            instances.add(game)
        Next game
    End Sub
#End Region

#Region "Events"
    Private Sub c_AddedGame(ByVal sender As IW3Server, ByVal instance As IW3Game) Handles server.AddedGame
        uiRef.enqueue(Function() eval(AddressOf _c_AddedGame, sender, instance))
    End Sub
    Private Sub _c_AddedGame(ByVal sender As IW3Server, ByVal instance As IW3Game)
        If sender IsNot server Then Return
        If Not instances.contains(instance) Then
            instances.add(instance)
        End If
    End Sub

    Private Sub c_RemovedGame(ByVal sender As IW3Server, ByVal instance As IW3Game) Handles server.RemovedGame
        uiRef.enqueue(Function() eval(AddressOf _c_RemovedGame, sender, instance))
    End Sub
    Private Sub _c_RemovedGame(ByVal sender As IW3Server, ByVal instance As IW3Game)
        If sender IsNot server Then Return
        If instances.contains(instance) Then
            instances.remove(instance)
        End If
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
