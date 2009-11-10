Imports HostBot.Bnet
Imports HostBot.Bnet.BnetClient
Imports HostBot.Bnet.BnetPacket

Public Class BnetClientControl
    Implements IHookable(Of BnetClient)
    Private WithEvents client As BnetClient
    Private ReadOnly ref As ICallQueue = New InvokedCallQueue(Me)
    Private numPrimaryStates As Integer

    Private Function QueueDispose() As IFuture Implements IHookable(Of BnetClient).QueueDispose
        Return ref.QueueAction(Sub() Me.Dispose())
    End Function
    Private Function QueueGetCaption() As IFuture(Of String) Implements IHookable(Of BnetClient).QueueGetCaption
        Return ref.QueueFunc(Function() If(client Is Nothing, "[No Client]", "Client {0}".Frmt(client.Name)))
    End Function
    Public Function QueueHook(ByVal child As BnetClient) As IFuture Implements IHookable(Of BnetClient).QueueHook
        Return ref.QueueAction(
            Sub()
                If Me.client Is child Then  Return
                Me.client = child
                lstState.Items.Clear()

                If child Is Nothing Then
                    logClient.SetLogger(Nothing, Nothing)
                    lstState.Enabled = False
                    txtTalk.Enabled = False
                Else
                    logClient.SetLogger(child.logger, "Client")
                    child.QueueGetState.CallOnValueSuccess(Sub(state) CatchClientStateChanged(child, state, state))
                End If
            End Sub
        )
    End Function

    Private Sub CatchReceivedPacket(ByVal sender As BnetClient, ByVal packet As BnetPacket) Handles client.ReceivedPacket
        ref.QueueAction(
            Sub()
                If sender IsNot client Then Return
                Dim vals = CType(packet.Payload.Value, Dictionary(Of String, Object))
                Select Case packet.id
                    Case BnetPacketId.ChatEvent
                        Dim id = CType(vals("event id"), BnetPacket.ChatEventId)
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
                    Case BnetPacketId.QueryGamesList
                        While lstState.Items.Count > numPrimaryStates
                            lstState.Items.RemoveAt(lstState.Items.Count - 1)
                        End While
                        lstState.Items.Add("--------")
                        lstState.Items.Add("Games List")
                        lstState.Items.Add(Date.Now().ToString("hh:mm:ss", Globalization.CultureInfo.CurrentCulture))
                        For Each game In CType(vals("games"), IEnumerable(Of Dictionary(Of String, Object)))
                            lstState.Items.Add("---")
                            Dim stats = CType(game("game statstring"), Warcraft3.W3GameStats)
                            lstState.Items.Add(CStr(game("game name")))
                            lstState.Items.Add(CStr(stats.HostName))
                            lstState.Items.Add(stats.relativePath.Split("\"c).Last)
                        Next game
                End Select
            End Sub
        )
    End Sub

    Private Sub txtCommand_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtCommand.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtCommand.Text = "" Then Return
        If client Is Nothing Then Return
        e.Handled = True
        client.parent.ClientCommands.ProcessLocalText(client, txtCommand.Text, logClient.Logger())
        txtCommand.Text = ""
    End Sub

    Private Sub txtTalk_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtTalk.KeyPress
        If e.KeyChar <> ChrW(Keys.Enter) Then Return
        If txtTalk.Text = "" Then Return
        If client Is Nothing Then Return
        e.Handled = True
        client.QueueSendText(txtTalk.Text)
        logClient.LogMessage("{0}: {1}".Frmt(client.UserName, txtTalk.Text), Color.DarkBlue)
        txtTalk.Text = ""
    End Sub

    Private Sub CatchClientStateChanged(ByVal sender As BnetClient,
                                        ByVal oldState As BnetClientState,
                                        ByVal newState As BnetClientState) Handles client.StateChanged
        ref.QueueAction(
            Sub()
                If sender IsNot client Then Return
                txtTalk.Enabled = False
                lstState.Enabled = True
                lstState.BackColor = SystemColors.Window
                Select Case newState
                    Case BnetClientState.Channel, BnetClientState.CreatingGame
                        If oldState = BnetClientState.AdvertisingGame Then lstState.Items.Clear()
                        txtTalk.Enabled = True
                    Case BnetClientState.AdvertisingGame
                        lstState.Items.Clear()
                        lstState.Items.Add("Game")
                        Dim g = client.CurGame
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
