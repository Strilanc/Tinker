Imports Tinker.Bnet.Protocol

Namespace Bnet
    <ContractVerification(False)>
    Public Class BnetClientControl
        Private ReadOnly inQueue As New StartableCallQueue(New InvokedCallQueue(Me))
        Private ReadOnly _manager As Bnet.ClientManager
        Private ReadOnly _client As Bnet.Client
        Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))
        Private numPrimaryStates As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_manager IsNot Nothing)
            Contract.Invariant(_client IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
        End Sub

        Private Shadows Sub OnParentChanged() Handles Me.ParentChanged
            If Me.Parent IsNot Nothing Then inQueue.Start()
        End Sub

        Public Sub New(ByVal manager As Bnet.ClientManager)
            Contract.Assert(manager IsNot Nothing)
            InitializeComponent()

            Me._client = manager.Client
            Me._manager = manager
            logClient.SetLogger(Me._client.Logger, "Client")

            Me._hooks.Add(Me._client.QueueAddPacketHandler(Packets.ServerToClient.ChatEvent,
                    handler:=Function(pickle) inQueue.QueueAction(Sub() OnClientReceivedChatEvent(Me._client, pickle.Value))))
            Me._hooks.Add(Me._client.QueueAddPacketHandler(Packets.ServerToClient.QueryGamesList,
                    handler:=Function(pickle) inQueue.QueueAction(Sub() OnClientReceivedQueryGamesList(Me._client, pickle.Value))))

            Me._client.QueueGetState.QueueCallOnValueSuccess(inQueue, Sub(state) OnClientStateChanged(Me._client, state, state))
            Dim stateChangedHandler As Client.StateChangedEventHandler = Sub(sender, oldState, newState) inQueue.QueueAction(
                    Sub() OnClientStateChanged(sender, oldState, newState))
            Dim advertisedHandler As Client.AdvertisedGameEventHandler = Sub(sender, gameDescription, [private], refreshed) inQueue.QueueAction(
                    Sub() OnClientAdvertisedGame(sender, gameDescription, [private], refreshed))
            AddHandler Me._client.StateChanged, stateChangedHandler
            AddHandler Me._client.AdvertisedGame, advertisedHandler
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler Me._client.StateChanged, stateChangedHandler).Futurized)
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler Me._client.AdvertisedGame, advertisedHandler).Futurized)
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
                lstState.Items.Add(game.GameStats.AdvertisedPath.ToString.Split("\"c).Last)
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

        Private Sub txtTalk_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles txtTalk.KeyDown
            If e.KeyCode <> Keys.Enter Then Return
            If e.Shift Then Return
            If txtTalk.Text = "" Then Return
            If _client Is Nothing Then Return
            e.Handled = True
            e.SuppressKeyPress = True
            _client.QueueSendText(txtTalk.Text).CallOnSuccess(
                Sub()
                    logClient.LogMessage("{0}: {1}".Frmt(_client.UserName, txtTalk.Text), Color.DarkBlue)
                End Sub
            ).Catch(
                Sub(ex)
                    logClient.LogMessage("Error sending text: {0}".Frmt(ex.Message), Color.Red)
                    ex.RaiseAsUnexpected("Sending bnet client text.")
                End Sub
            )
            txtTalk.Text = ""
        End Sub

        Private Sub OnClientStateChanged(ByVal sender As Bnet.Client,
                                         ByVal oldState As Bnet.ClientState,
                                         ByVal newState As Bnet.ClientState)
            Contract.Requires(sender IsNot Nothing)

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
                    'advertised event will handle it
                Case Else
                    lstState.Items.Clear()
                    lstState.Enabled = False
                    lstState.BackColor = SystemColors.ButtonFace
            End Select
        End Sub
        Private Sub OnClientAdvertisedGame(ByVal sender As Bnet.Client,
                                           ByVal gameDescription As WC3.LocalGameDescription,
                                           ByVal [private] As Boolean,
                                           ByVal refreshed As Boolean)
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(gameDescription IsNot Nothing)

            If IsDisposed Then Return
            If sender IsNot _client Then Return
            txtTalk.Enabled = False
            lstState.Enabled = True
            lstState.BackColor = SystemColors.Window

            lstState.Items.Clear()
            lstState.Items.Add("Game")
            lstState.Items.Add(gameDescription.Name)
            lstState.Items.Add(gameDescription.GameStats.AdvertisedPath)
            lstState.Items.Add(If([private], "Private", "Public"))
            lstState.Items.Add("{0}: {1}".Frmt(If(refreshed, "Refreshed", "Created"),
                                               DateTime.Now.ToString("hh:mm:ss", Globalization.CultureInfo.CurrentCulture)))
            numPrimaryStates = lstState.Items.Count
        End Sub

        Private Sub comClient_IssuedCommand(ByVal sender As CommandControl, ByVal argument As String) Handles comClient.IssuedCommand
            Contract.Requires(argument IsNot Nothing)
            Tinker.Components.UIInvokeCommand(_manager, argument)
        End Sub
    End Class
End Namespace
