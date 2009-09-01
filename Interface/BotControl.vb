Imports HostBot.Bnet
Imports HostBot.Warcraft3

Public Class BotControl
    Implements IHookable(Of MainBot)
    Private WithEvents bot As MainBot
    Private ReadOnly ref As ICallQueue = New InvokedCallQueue(Me)
    Private clients As TabControlIHookableSet(Of BnetClient, BnetClientControl)
    Private servers As TabControlIHookableSet(Of IW3Server, W3ServerControl)
    Private widgets As TabControlIHookableSet(Of IBotWidget, BotWidgetControl)

    Private Function QueueDispose() As IFuture Implements IHookable(Of MainBot).QueueDispose
        Return ref.QueueAction(Sub() Me.Dispose())
    End Function

    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of MainBot).QueueGetCaption
        Return ref.QueueFunc(Function() If(bot Is Nothing, "[No Bot]", "Bot"))
    End Function

    Public Function QueueHook(ByVal bot As MainBot) As IFuture Implements IHookable(Of MainBot).QueueHook
        Return ref.QueueAction(
            Sub()
                If Me.bot Is bot Then  Return

                If clients Is Nothing Then
                    clients = New TabControlIHookableSet(Of BnetClient, BnetClientControl)(tabsBot)
                    servers = New TabControlIHookableSet(Of IW3Server, W3ServerControl)(tabsBot)
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

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        e.Handled = True
        bot.BotCommands.ProcessLocalText(bot, txtCommand.Text, logBot.Logger())
        txtCommand.Text = ""
    End Sub

    Private Sub CatchBotAddedClient(ByVal client As BnetClient) Handles bot.AddedClient
        ref.QueueAction(Sub() clients.Add(client))
    End Sub

    Private Sub CatchBotRemovedClient(ByVal client As BnetClient) Handles bot.RemovedClient
        ref.QueueAction(Sub() clients.Remove(client))
    End Sub

    Private Sub CatchBotAddedServer(ByVal server As IW3Server) Handles bot.AddedServer
        ref.QueueAction(Sub() servers.Add(server))
    End Sub

    Private Sub CatchBotRemovedServer(ByVal server As IW3Server) Handles bot.RemovedServer
        ref.QueueAction(Sub() servers.Remove(server))
    End Sub

    Private Sub CatchBotAddedWidget(ByVal widget As IBotWidget) Handles bot.AddedWidget
        ref.QueueAction(Sub() widgets.Add(widget))
    End Sub

    Private Sub CatchBotRemovedWidget(ByVal widget As IBotWidget) Handles bot.RemovedWidget
        ref.QueueAction(Sub() widgets.Remove(widget))
    End Sub

    Private Sub CatchBotServerStateChanged(ByVal server As IW3Server, ByVal oldState As W3ServerStates, ByVal newState As W3ServerStates) Handles bot.ServerStateChanged
        ref.QueueAction(Sub() servers.Update(server))
    End Sub
End Class
