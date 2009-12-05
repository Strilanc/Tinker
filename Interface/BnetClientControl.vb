Imports Tinker.Bnet.Packet
Imports Tinker.Commands

Public Class BnetClientControl
    Private ReadOnly _manager As Components.BnetClientManager
    Private ReadOnly _client As Bnet.Client
    Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))
    Private ReadOnly inQueue As New StartableCallQueue(New InvokedCallQueue(Me))
    Private numPrimaryStates As Integer

    Private Shadows Sub OnParentChanged() Handles Me.ParentChanged
        If Me.Parent IsNot Nothing Then inQueue.Start()
    End Sub

    Public Sub New(ByVal manager As Components.BnetClientManager)
        Contract.Requires(manager IsNot Nothing)
        InitializeComponent()

        Me._client = manager.Client
        Me._manager = manager
        logClient.SetLogger(Me._client.logger, "Client")

        Me._hooks.Add(Me._client.QueueAddPacketHandler(
            id:=Bnet.PacketId.ChatEvent,
            jar:=Bnet.Packet.ServerPackets.ChatEvent,
            handler:=Function(pickle) inQueue.QueueAction(Sub() OnClientReceivedChatEvent(Me._client, pickle.Value))))
        Me._hooks.Add(Me._client.QueueAddPacketHandler(
            id:=Bnet.PacketId.QueryGamesList,
            jar:=Bnet.Packet.ServerPackets.QueryGamesList,
            handler:=Function(pickle) inQueue.QueueAction(Sub() OnClientReceivedQueryGamesList(Me._client, pickle.Value))))

        Me._client.QueueGetState.CallOnValueSuccess(Sub(state) OnClientStateChanged(Me._client, state, state))
        AddHandler Me._client.StateChanged, AddressOf OnClientStateChanged
        Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler Me._client.StateChanged, AddressOf OnClientStateChanged).Futurized)
    End Sub

    Public Function QueueDispose() As IFuture
        Return inQueue.QueueAction(Sub() Me.Dispose())
    End Function
    Private Sub BnetClientControl_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        For Each hook In _hooks
            hook.CallOnValueSuccess(Sub(value) value.Dispose()).SetHandled()
        Next hook
    End Sub

    Private Sub OnClientReceivedQueryGamesList(ByVal sender As Bnet.Client, ByVal value As QueryGamesListResponse)
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
    Private Sub OnClientReceivedChatEvent(ByVal sender As Bnet.Client, ByVal vals As Dictionary(Of InvariantString, Object))
        If IsDisposed Then Return
        If sender IsNot Me._client Then Return
        Dim id = CType(vals("event id"), ChatEventId)
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

    Private Sub OnClientStateChanged(ByVal sender As Bnet.Client,
                                     ByVal oldState As Bnet.ClientState,
                                     ByVal newState As Bnet.ClientState)
        inQueue.QueueAction(
            Sub()
                If IsDisposed Then Return
                If sender IsNot _client Then Return
                txtTalk.Enabled = False
                lstState.Enabled = True
                lstState.BackColor = SystemColors.Window
                Select Case newState
                    Case Bnet.ClientState.Channel, Bnet.ClientState.CreatingGame
                        If oldState = Bnet.ClientState.AdvertisingGame Then lstState.Items.Clear()
                        txtTalk.Enabled = True
                    Case Bnet.ClientState.AdvertisingGame
                        lstState.Items.Clear()
                        lstState.Items.Add("Game")
                        Dim g = _client.AdvertisedGameDescription
                        Dim p = _client.AdvertisedPrivate
                        If g IsNot Nothing Then
                            lstState.Items.Add(g.Name)
                            lstState.Items.Add(g.GameStats.relativePath)
                            lstState.Items.Add(If(p, "Private", "Public"))
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

    Private Sub comClient_IssuedCommand(ByVal sender As CommandControl, ByVal argument As String) Handles comClient.IssuedCommand
        Tinker.Components.UIInvokeCommand(_manager, argument)
    End Sub
End Class
