Imports HostBot.Bnet
Imports HostBot.Warcraft3

Public Class BotControl
    Implements IHookable(Of MainBot)
    Private WithEvents bot As MainBot
    Private ReadOnly uiRef As ICallQueue = New InvokedCallQueue(Me)
    Private clients As TabControlIHookableSet(Of IBnetClient, BnetClientControl)
    Private servers As TabControlIHookableSet(Of IW3Server, W3GameServerControl)
    Private widgets As TabControlIHookableSet(Of IBotWidget, BotWidgetControl)

#Region "Hook"
    Private Function f_caption() As IFuture(Of String) Implements IHookable(Of MainBot).f_caption
        Return uiRef.QueueFunc(Function() If(bot Is Nothing, "[No Bot]", "Bot"))
    End Function

    Public Function f_hook(ByVal bot As MainBot) As IFuture Implements IHookable(Of MainBot).f_Hook
        Return uiRef.QueueAction(
            Sub()
                If Me.bot Is bot Then  Return

                If clients Is Nothing Then
                    clients = New TabControlIHookableSet(Of IBnetClient, BnetClientControl)(tabsBot)
                    servers = New TabControlIHookableSet(Of IW3Server, W3GameServerControl)(tabsBot)
                    widgets = New TabControlIHookableSet(Of IBotWidget, BotWidgetControl)(tabsBot)
                Else
                    clients.Clear()
                    servers.Clear()
                    widgets.Clear()
                End If
                Me.bot = bot

                If bot Is Nothing Then
                    logBot.SetLogger(Nothing, Nothing)
                Else
                    logBot.SetLogger(bot.logger, "Main")
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
        bot.BotCommands.ProcessLocalText(bot, txtCommand.Text, logBot.Logger())
        txtCommand.Text = ""
    End Sub

    Private Sub c_BotAddedClient(ByVal client As IBnetClient) Handles bot.AddedClient
        uiRef.QueueAction(Sub() clients.Add(client))
    End Sub

    Private Sub c_BotRemovedClient(ByVal client As IBnetClient) Handles bot.RemovedClient
        uiRef.QueueAction(Sub() clients.Remove(client))
    End Sub

    Private Sub c_BotAddedServer(ByVal server As IW3Server) Handles bot.AddedServer
        uiRef.QueueAction(Sub() servers.Add(server))
    End Sub

    Private Sub c_BotRemovedServer(ByVal server As IW3Server) Handles bot.RemovedServer
        uiRef.QueueAction(Sub() servers.Remove(server))
    End Sub

    Private Sub c_BotAddedWidget(ByVal widget As IBotWidget) Handles bot.AddedWidget
        uiRef.QueueAction(Sub() widgets.Add(widget))
    End Sub

    Private Sub c_BotRemovedWidget(ByVal widget As IBotWidget) Handles bot.RemovedWidget
        uiRef.QueueAction(Sub() widgets.Remove(widget))
    End Sub

    Private Sub c_BotServerStateChanged(ByVal server As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates) Handles bot.ServerStateChanged
        uiRef.QueueAction(Sub() servers.Update(server))
    End Sub
#End Region
End Class
