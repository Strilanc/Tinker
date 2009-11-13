Imports HostBot.Commands

Public Class BotControl
    Implements IHookable(Of MainBot)
    Private WithEvents bot As MainBot
    Private ReadOnly ref As ICallQueue = New InvokedCallQueue(Me)
    Private clients As TabControlIHookableSet(Of Bnet.Client, BnetClientControl)
    Private servers As TabControlIHookableSet(Of WC3.GameServer, W3ServerControl)
    Private widgets As TabControlIHookableSet(Of IBotWidget, BotWidgetControl)

    Private commandHistory As New List(Of String) From {""}
    Private commandHistoryPointer As Integer
    Private Sub txtCommand_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles txtCommand.KeyDown
        Select Case e.KeyCode
            Case Keys.Enter
                If txtCommand.Text = "" Then Return
                bot.BotCommands.ProcessLocalText(bot, txtCommand.Text, logBot.Logger())

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

    Private Function QueueDispose() As IFuture Implements IHookable(Of MainBot).QueueDispose
        Return ref.QueueAction(Sub() Me.Dispose())
    End Function

    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of MainBot).QueueGetCaption
        Return ref.QueueFunc(Function() If(bot Is Nothing, "[No Bot]", "Bot"))
    End Function

    Public Function QueueHook(ByVal child As MainBot) As IFuture Implements IHookable(Of MainBot).QueueHook
        Return ref.QueueAction(
            Sub()
                If Me.bot Is child Then Return

                If clients Is Nothing Then
                    clients = New TabControlIHookableSet(Of Bnet.Client, BnetClientControl)(tabsBot)
                    servers = New TabControlIHookableSet(Of WC3.GameServer, W3ServerControl)(tabsBot)
                    widgets = New TabControlIHookableSet(Of IBotWidget, BotWidgetControl)(tabsBot)
                Else
                    clients.Clear()
                    servers.Clear()
                    widgets.Clear()
                End If
                Me.bot = child

                If child Is Nothing Then
                    logBot.SetLogger(Nothing, Nothing)
                Else
                    logBot.SetLogger(child.logger, "Main")
                End If
            End Sub
        )
    End Function

    Private Sub CatchBotAddedClient(ByVal client As Bnet.Client) Handles bot.AddedClient
        ref.QueueAction(Sub() clients.Add(client))
    End Sub

    Private Sub CatchBotRemovedClient(ByVal client As Bnet.Client) Handles bot.RemovedClient
        ref.QueueAction(Sub() clients.Remove(client))
    End Sub

    Private Sub CatchBotAddedServer(ByVal server As WC3.GameServer) Handles bot.AddedServer
        ref.QueueAction(Sub() servers.Add(server))
    End Sub

    Private Sub CatchBotRemovedServer(ByVal server As WC3.GameServer) Handles bot.RemovedServer
        ref.QueueAction(Sub() servers.Remove(server))
    End Sub

    Private Sub CatchBotAddedWidget(ByVal widget As IBotWidget) Handles bot.AddedWidget
        ref.QueueAction(Sub() widgets.Add(widget))
    End Sub

    Private Sub CatchBotRemovedWidget(ByVal widget As IBotWidget) Handles bot.RemovedWidget
        ref.QueueAction(Sub() widgets.Remove(widget))
    End Sub

    Private Sub CatchBotServerStateChanged(ByVal server As WC3.GameServer, ByVal oldState As WC3.ServerState, ByVal newState As WC3.ServerState) Handles bot.ServerStateChanged
        ref.QueueAction(Sub() servers.Update(server))
    End Sub
End Class
