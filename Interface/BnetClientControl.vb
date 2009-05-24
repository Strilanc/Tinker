Imports HostBot.Bnet
Imports HostBot.Bnet.BnetClient
Imports HostBot.Bnet.BnetPacket

Public Class BnetClientControl
    Implements IHookable(Of BnetClient)

#Region "Variables"
    Private WithEvents client As BnetClient
    Private ReadOnly uiRef As New InvokedCallQueue(Me)
#End Region

#Region "Hook"
    Private Function f_caption() As IFuture(Of String) Implements IHookable(Of Bnet.BnetClient).f_caption
        Return uiRef.enqueue(Function() If(client Is Nothing, "[No Client]", "Client {0}".frmt(client.name)))
    End Function

    Public Function f_hook(ByVal client As BnetClient) As IFuture Implements IHookable(Of Bnet.BnetClient).f_hook
        Return uiRef.enqueue(Function() eval(AddressOf _f_hook, client))
    End Function
    Private Sub _f_hook(ByVal client As BnetClient)
        If Me.client Is client Then Return
        Me.client = client
        lstUsers.Items.Clear()

        If client Is Nothing Then
            logClient.setLogger(Nothing, Nothing)
            lstUsers.Enabled = False
            txtTalk.Enabled = False
        Else
            logClient.setLogger(client.logger, "Client")
            Dim cur_state = client.state_P
            _c_ClientStateChanged(client, cur_state, cur_state)
        End If
    End Sub
#End Region

#Region "Events"
    Private Sub c_ChatEvent(ByVal sender As BnetClient, ByVal id As CHAT_EVENT_ID, ByVal user As String, ByVal text As String) Handles client.chat_event
        uiRef.enqueue(Function() eval(AddressOf _c_ChatEvent, sender, id, user, Text))
    End Sub
    Private Sub _c_ChatEvent(ByVal sender As BnetClient, ByVal id As CHAT_EVENT_ID, ByVal user As String, ByVal text As String)
        If sender IsNot client Then Return
        Select Case id
            Case CHAT_EVENT_ID.SHOW_USER, CHAT_EVENT_ID.USER_JOINED
                If Not lstUsers.Items.Contains(user) Then
                    lstUsers.Items.Add(user)
                End If
                logClient.logMessage(user + " entered the channel", Color.LightGray)
            Case CHAT_EVENT_ID.USER_LEFT
                lstUsers.Items.Remove(user)
                logClient.logMessage(user + " left the channel", Color.LightGray)
            Case CHAT_EVENT_ID.CHANNEL
                logClient.logMessage("--- Entered Channel: " + text, Color.DarkGray)
                lstUsers.Items.Clear()
                lstUsers.Items.Add("Channel " + text)
                lstUsers.Items.Add(New String("-"c, 50))
            Case CHAT_EVENT_ID.WHISPER
                logClient.logMessage(user + " whispers: " + text, Color.DarkGreen)
            Case CHAT_EVENT_ID.TALK
                logClient.logMessage(user + ": " + text, Color.Black)
            Case CHAT_EVENT_ID.BROADCAST
                logClient.logMessage("(server broadcast) " + user + ": " + text, Color.Red)
            Case CHAT_EVENT_ID.CHANNEL
                logClient.logMessage("Entered channel " + text, Color.DarkGray)
            Case CHAT_EVENT_ID.WHISPER_SENT
                logClient.logMessage("You whisper to " + user + ": " + text, Color.DarkGreen)
            Case CHAT_EVENT_ID.CHANNEL_FULL
                logClient.logMessage("Channel was full", Color.Red)
            Case CHAT_EVENT_ID.CHANNEL_DOES_NOT_EXIST
                logClient.logMessage("Channel didn't exist", Color.Red)
            Case CHAT_EVENT_ID.CHANNEL_RESTRICTED
                logClient.logMessage("Channel was restricted", Color.Red)
            Case CHAT_EVENT_ID.INFO
                logClient.logMessage(text, Color.Gray)
            Case CHAT_EVENT_ID.ERRORS
                logClient.logMessage(text, Color.Red)
            Case CHAT_EVENT_ID.EMOTE
                logClient.logMessage(user + " " + text, Color.DarkGray)
        End Select
    End Sub

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        If client Is Nothing Then Return
        e.Handled = True
        client.parent.client_commands.processLocalText(client, txtCommand.Text, logClient.getLogger())
        txtCommand.Text = ""
    End Sub

    Private Sub txtTalk_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtTalk.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtTalk.Text = "" Then Return
        If client Is Nothing Then Return
        e.Handled = True
        client.send_text_R(txtTalk.Text)
        logClient.logMessage(client.username_P + ": " + txtTalk.Text, Color.DarkBlue)
        txtTalk.Text = ""
    End Sub

    Private Sub c_ClientStateChanged(ByVal sender As BnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States) Handles client.state_changed
        uiRef.enqueue(Function() eval(AddressOf _c_ClientStateChanged, sender, old_state, new_state))
    End Sub
    Private Sub _c_ClientStateChanged(ByVal sender As BnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States)
        If sender IsNot client Then Return
        Dim cur_state = client.state_P
        Dim in_channel = cur_state = States.channel OrElse cur_state = States.creating_game
        txtTalk.Enabled = in_channel
        lstUsers.Enabled = in_channel
        lstUsers.BackColor = If(in_channel, SystemColors.Window, SystemColors.ButtonFace)
        If old_state <> States.creating_game AndAlso new_state <> States.creating_game Then
            lstUsers.Items.Clear()
        End If
    End Sub
#End Region
End Class
