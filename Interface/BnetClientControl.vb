Imports HostBot.Bnet
Imports HostBot.Bnet.BnetClient
Imports HostBot.Bnet.BnetPacket

Public Class BnetClientControl
    Implements IHookable(Of IBnetClient)
    Private WithEvents client As IBnetClient
    Private ReadOnly uiRef As New InvokedCallQueue(Me)

#Region "Hook"
    Private Function f_caption() As IFuture(Of String) Implements IHookable(Of IBnetClient).f_caption
        Return uiRef.QueueFunc(Function() If(client Is Nothing, "[No Client]", "Client {0}".frmt(client.Name)))
    End Function

    Public Function f_hook(ByVal client As IBnetClient) As IFuture Implements IHookable(Of IBnetClient).f_Hook
        Return uiRef.QueueAction(
            Sub()
                If Me.client Is client Then  Return
                Me.client = client
                lstState.Items.Clear()

                If client Is Nothing Then
                    logClient.SetLogger(Nothing, Nothing)
                    lstState.Enabled = False
                    txtTalk.Enabled = False
                Else
                    logClient.SetLogger(client.logger, "Client")
                    client.f_GetState.CallWhenValueReady(Sub(state) c_ClientStateChanged(client, state, state))
                End If
            End Sub
        )
    End Function
#End Region

#Region "Events"
    Private Sub c_ChatEvent(ByVal sender As IBnetClient, ByVal id As ChatEventId, ByVal user As String, ByVal text As String) Handles client.ReceivedChatEvent
        uiRef.QueueAction(
            Sub()
                If sender IsNot client Then  Return
                Select Case id
                    Case ChatEventId.ShowUser, ChatEventId.UserJoined
                        If Not lstState.Items.Contains(user) Then
                            lstState.Items.Add(user)
                        End If
                        logClient.LogMessage("{0} entered the channel".frmt(user), Color.LightGray)
                    Case ChatEventId.UserLeft
                        lstState.Items.Remove(user)
                        logClient.LogMessage("{0} left the channel".frmt(user), Color.LightGray)
                    Case ChatEventId.Channel
                        logClient.LogMessage("--- Entered Channel: " + text, Color.DarkGray)
                        lstState.Items.Clear()
                        lstState.Items.Add("Channel " + text)
                        lstState.Items.Add(New String("-"c, 50))
                    Case ChatEventId.Whisper
                        logClient.LogMessage("{0} whispers: {1}".frmt(user, text), Color.DarkGreen)
                    Case ChatEventId.Talk
                        logClient.LogMessage("{0}: {1}".frmt(user, text), Color.Black)
                    Case ChatEventId.Broadcast
                        logClient.LogMessage("(server broadcast) {0}: {1}".frmt(user, text), Color.Red)
                    Case ChatEventId.Channel
                        logClient.LogMessage("Entered channel {0}".frmt(text), Color.DarkGray)
                    Case ChatEventId.WhisperSent
                        logClient.LogMessage("You whisper to {0}: {1}".frmt(user, text), Color.DarkGreen)
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
                        logClient.LogMessage("{0} {1}".frmt(user, text), Color.DarkGray)
                End Select
            End Sub
        )
    End Sub

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        If client Is Nothing Then Return
        e.Handled = True
        client.Parent.ClientCommands.ProcessLocalText(client, txtCommand.Text, logClient.Logger())
        txtCommand.Text = ""
    End Sub

    Private Sub txtTalk_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtTalk.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtTalk.Text = "" Then Return
        If client Is Nothing Then Return
        e.Handled = True
        client.f_SendText(txtTalk.Text)
        logClient.LogMessage(client.username + ": " + txtTalk.Text, Color.DarkBlue)
        txtTalk.Text = ""
    End Sub

    Private Sub c_ClientStateChanged(ByVal sender As IBnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States) Handles client.StateChanged
        uiRef.QueueAction(
            Sub()
                If sender IsNot client Then  Return
                txtTalk.Enabled = False
                lstState.Enabled = True
                lstState.BackColor = SystemColors.Window
                Select Case new_state
                    Case States.Channel, States.CreatingGame
                        If old_state = States.Game Then  lstState.Items.Clear()
                        txtTalk.Enabled = True
                    Case States.Game
                        lstState.Items.Clear()
                        lstState.Items.Add("Game")
                        Dim g = client.CurGame
                        If g IsNot Nothing Then
                            lstState.Items.Add(g.header.name)
                            lstState.Items.Add(g.header.map.relativePath)
                            lstState.Items.Add(If(g.private, "Private", "Public"))
                            lstState.Items.Add("Refreshed: {0}".frmt(Now.ToString("hh:mm:ss", Globalization.CultureInfo.CurrentCulture)))
                        End If
                    Case Else
                        lstState.Items.Clear()
                        lstState.Enabled = False
                        lstState.BackColor = SystemColors.ButtonFace
                End Select
            End Sub
        )
    End Sub
#End Region
End Class
