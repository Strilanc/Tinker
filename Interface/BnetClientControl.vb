Imports HostBot.Bnet
Imports HostBot.Bnet.Packet

Public Class BnetClientControl
    Implements IHookable(Of Client)
    Private WithEvents _client As Client
    Private _clientHooks As IFuture(Of IEnumerable(Of IDisposable))
    Private ReadOnly inQueue As ICallQueue = New InvokedCallQueue(Me)
    Private numPrimaryStates As Integer

    Private commandHistory As New List(Of String) From {""}
    Private commandHistoryPointer As Integer

    '<ContractInvariantMethod()> Private Sub ObjectInvariant()
    'Contract.Invariant((_client IsNot Nothing) = (_clientHooks IsNot Nothing))
    'Contract.Invariant(inQueue IsNot Nothing)
    'Contract.Invariant(numPrimaryStates >= 0)
    'Contract.Invariant(commandHistory IsNot Nothing)
    'Contract.Invariant(commandHistoryPointer >= 0)
    'Contract.Invariant(commandHistoryPointer < commandHistory.Count)
    'End Sub
    Private Sub txtCommand_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles txtCommand.KeyDown
        Select Case e.KeyCode
            Case Keys.Enter
                If txtCommand.Text = "" Then Return
                _client.Parent.ClientCommands.ProcessLocalText(_client, txtCommand.Text, logClient.Logger())

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

    Private Function QueueDispose() As IFuture Implements IHookable(Of Client).QueueDispose
        Return inQueue.QueueAction(Sub() Me.Dispose())
    End Function
    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of Client).QueueGetCaption
        Return inQueue.QueueFunc(Function() If(_client Is Nothing, "[No Client]", "Client {0}".Frmt(_client.Name)))
    End Function
    Public Function QueueHook(ByVal child As Client) As IFuture Implements IHookable(Of Client).QueueHook
        Return inQueue.QueueAction(Sub() PerformHook(child))
    End Function
    Private Sub PerformHook(ByVal child As Client)
        If Me._client Is child Then Return

        'Unhook
        If _client IsNot Nothing Then
            _clientHooks.CallOnValueSuccess(
                Sub(hooks)
                    For Each hook In hooks
                        hook.Dispose()
                    Next hook
                End Sub)
            _clientHooks = Nothing
            _client = Nothing
        End If
        lstState.Items.Clear()

        'Hook
        _client = child
        If child Is Nothing Then
            logClient.SetLogger(Nothing, Nothing)
            lstState.Enabled = False
            txtTalk.Enabled = False
        Else
            logClient.SetLogger(child.logger, "Client")
            Me._client.QueueGetState.CallOnValueSuccess(Sub(state) CatchClientStateChanged(child, state, state))
            Dim hooks = New List(Of IFuture(Of IDisposable))
            hooks.Add(Me._client.QueueAddPacketHandler(
                id:=PacketId.ChatEvent,
                jar:=Packet.Parsers.ChatEvent,
                handler:=Function(pickle)
                             Return inQueue.QueueAction(Sub() OnClientReceivedChatEvent(child, pickle.Value))
                         End Function))
            hooks.Add(Me._client.QueueAddPacketHandler(
                id:=PacketId.QueryGamesList,
                jar:=Packet.Parsers.QueryGamesList,
                handler:=Function(pickle)
                             Return inQueue.QueueAction(Sub() OnClientReceivedQueryGamesList(child, pickle.Value))
                         End Function))
            Me._clientHooks = hooks.Defuturized
        End If
    End Sub

    Private Sub OnClientReceivedQueryGamesList(ByVal sender As Client, ByVal value As QueryGamesListResponse)
        If sender IsNot _client Then Return
        While lstState.Items.Count > numPrimaryStates
            lstState.Items.RemoveAt(lstState.Items.Count - 1)
        End While
        lstState.Items.Add("--------")
        lstState.Items.Add("Games List")
        lstState.Items.Add(Date.Now().ToString("hh:mm:ss", Globalization.CultureInfo.CurrentCulture))
        For Each game In value.Games
            lstState.Items.Add("---")
            lstState.Items.Add(game.Name)
            lstState.Items.Add(game.GameStats.HostName)
            lstState.Items.Add(game.GameStats.relativePath.Split("\"c).Last)
        Next game
    End Sub
    Private Sub OnClientReceivedChatEvent(ByVal sender As Client, ByVal vals As Dictionary(Of String, Object))
        If sender IsNot Me._client Then Return
        Dim id = CType(vals("event id"), Packet.ChatEventId)
        Dim user = CStr(vals("username"))
        Dim text = CStr(vals("text"))
        Select Case id
            Case ChatEventId.ShowUser, ChatEventId.UserJoined
                If Not lstState.Items.Contains(user) OrElse lstState.Items.IndexOf(user) >= numPrimaryStates Then
                    lstState.Items.Insert(numPrimaryStates, user)
                    numPrimaryStates += 1
                End If
                logClient.LogMessage("{0} entered the channel".Frmt(user), Color.LightGray)
            Case ChatEventId.UserLeft
                If lstState.Items.Contains(user) AndAlso lstState.Items.IndexOf(user) < numPrimaryStates Then
                    numPrimaryStates -= 1
                    lstState.Items.Remove(user)
                End If
                logClient.LogMessage("{0} left the channel".Frmt(user), Color.LightGray)
            Case ChatEventId.Channel
                logClient.LogMessage("--- Entered Channel: " + text, Color.DarkGray)
                lstState.Items.Clear()
                lstState.Items.Add("Channel " + text)
                lstState.Items.Add(New String("-"c, 50))
                numPrimaryStates = 2
            Case ChatEventId.Whisper
                logClient.LogMessage("{0} whispers: {1}".Frmt(user, text), Color.DarkGreen)
            Case ChatEventId.Talk
                logClient.LogMessage("{0}: {1}".Frmt(user, text), Color.Black)
            Case ChatEventId.Broadcast
                logClient.LogMessage("(server broadcast) {0}: {1}".Frmt(user, text), Color.Red)
            Case ChatEventId.Channel
                logClient.LogMessage("Entered channel {0}".Frmt(text), Color.DarkGray)
            Case ChatEventId.WhisperSent
                logClient.LogMessage("You whisper to {0}: {1}".Frmt(user, text), Color.DarkGreen)
            Case ChatEventId.ChannelFull
                logClient.LogMessage("Channel was full", Color.Red)
            Case ChatEventId.ChannelDoesNotExist
                logClient.LogMessage("Channel didn't exist", Color.Red)
            Case ChatEventId.ChannelRestricted
                logClient.LogMessage("Channel was restricted", Color.Red)
            Case ChatEventId.Info
                logClient.LogMessage(text, Color.Gray)
            Case ChatEventId.Errors
                logClient.LogMessage(text, Color.Red)
            Case ChatEventId.Emote
                logClient.LogMessage("{0} {1}".Frmt(user, text), Color.DarkGray)
        End Select
    End Sub

    Private Sub txtTalk_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtTalk.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtTalk.Text = "" Then Return
        If _client Is Nothing Then Return
        e.Handled = True
        _client.QueueSendText(txtTalk.Text)
        logClient.LogMessage("{0}: {1}".Frmt(_client.UserName, txtTalk.Text), Color.DarkBlue)
        txtTalk.Text = ""
    End Sub

    Private Sub CatchClientStateChanged(ByVal sender As Client,
                                        ByVal oldState As ClientState,
                                        ByVal newState As ClientState) Handles _client.StateChanged
        inQueue.QueueAction(
            Sub()
                If sender IsNot _client Then Return
                txtTalk.Enabled = False
                lstState.Enabled = True
                lstState.BackColor = SystemColors.Window
                Select Case newState
                    Case ClientState.Channel, ClientState.CreatingGame
                        If oldState = ClientState.AdvertisingGame Then lstState.Items.Clear()
                        txtTalk.Enabled = True
                    Case ClientState.AdvertisingGame
                        lstState.Items.Clear()
                        lstState.Items.Add("Game")
                        Dim g = _client.CurGame
                        If g IsNot Nothing Then
                            lstState.Items.Add(g.Header.Name)
                            lstState.Items.Add(g.Header.GameStats.relativePath)
                            lstState.Items.Add(If(g.private, "Private", "Public"))
                            lstState.Items.Add("Refreshed: {0}".Frmt(Now.ToString("hh:mm:ss", Globalization.CultureInfo.CurrentCulture)))
                        End If
                        numPrimaryStates = lstState.Items.Count
                    Case Else
                        lstState.Items.Clear()
                        lstState.Enabled = False
                        lstState.BackColor = SystemColors.ButtonFace
                End Select
            End Sub
        )
    End Sub
End Class
