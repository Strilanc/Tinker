Imports HostBot.Bnet
Imports HostBot.Warcraft3

Public Class BotControl
    Implements IHookable(Of MainBot)
    Private WithEvents bot As MainBot
    Private ReadOnly uiRef As ICallQueue = New InvokedCallQueue(Me)
    Private clients As TabControlIHookableSet(Of BnetClient, BnetClientControl)
    Private servers As TabControlIHookableSet(Of IW3Server, W3GameServerControl)
    Private widgets As TabControlIHookableSet(Of IBotWidget, BotWidgetControl)

#Region "Hook"
    Private Function f_caption() As IFuture(Of String) Implements IHookable(Of MainBot).f_caption
        Return uiRef.QueueFunc(Function() If(bot Is Nothing, "[No Bot]", "Bot"))
    End Function

    Public Function f_hook(ByVal bot As MainBot) As IFuture Implements IHookable(Of MainBot).f_hook
        Return uiRef.QueueAction(
            Sub()
                If Me.bot Is bot Then  Return

                If clients Is Nothing Then
                    clients = New TabControlIHookableSet(Of BnetClient, BnetClientControl)(tabsBot)
                    servers = New TabControlIHookableSet(Of IW3Server, W3GameServerControl)(tabsBot)
                    widgets = New TabControlIHookableSet(Of IBotWidget, BotWidgetControl)(tabsBot)
                Else
                    clients.clear()
                    servers.clear()
                    widgets.clear()
                End If
                Me.bot = bot

                If bot Is Nothing Then
                    logBot.setLogger(Nothing, Nothing)
                Else
                    logBot.setLogger(bot.logger, "Main")
                End If
            End Sub
        )
    End Function
#End Region

#Region "Events"
    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        e.Handled = True
        bot.bot_commands.processLocalText(bot, txtCommand.Text, logBot.getLogger())
        txtCommand.Text = ""
    End Sub

    Private Sub c_BotAddedClient(ByVal client As BnetClient) Handles bot.added_client
        uiRef.QueueAction(Sub() clients.add(client))
    End Sub

    Private Sub c_BotRemovedClient(ByVal client As BnetClient) Handles bot.removed_client
        uiRef.QueueAction(Sub() clients.remove(client))
    End Sub

    Private Sub c_BotAddedServer(ByVal server As IW3Server) Handles bot.added_server
        uiRef.QueueAction(Sub() servers.add(server))
    End Sub

    Private Sub c_BotRemovedServer(ByVal server As IW3Server) Handles bot.removed_server
        uiRef.QueueAction(Sub() servers.remove(server))
    End Sub

    Private Sub c_BotAddedWidget(ByVal widget As IBotWidget) Handles bot.added_widget
        uiRef.QueueAction(Sub() widgets.add(widget))
    End Sub

    Private Sub c_BotRemovedWidget(ByVal widget As IBotWidget) Handles bot.removed_widget
        uiRef.QueueAction(Sub() widgets.remove(widget))
    End Sub

    Private Sub c_BotServerStateChanged(ByVal server As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates) Handles bot.server_state_changed
        uiRef.QueueAction(Sub() servers.update(server))
    End Sub
#End Region
End Class
