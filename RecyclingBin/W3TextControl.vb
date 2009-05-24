Imports HostBot.Bnet

Public Class W3TextControl
    'Private WithEvents texter As W3Texter
    'Private ReadOnly uiRef As New InvokedCallQueue(Me, Me.gettype.name + " uiRef")

    'Public Sub UIREF_hook(ByVal texter As W3Texter)
    '    If uiRef.queueIfRemote(curry(AddressOf UIREF_hook, texter)) Then Return
    '    Me.texter = texter
    '    Dim b As Boolean = texter IsNot Nothing AndAlso texter.client IsNot Nothing
    '    txtMain.Enabled = b
    '    txtInput.Enabled = b
    '    If b Then UIREF_showMessage("--------------------------", Color.Black)
    'End Sub

    'Private Sub UIREF_showMessage(ByVal s As String, ByVal c As System.Drawing.Color) Handles texter.showMessage
    '    If uiRef.queueIfRemote(curry(AddressOf UIREF_showMessage, s, c)) Then Return
    '    If s = "" Then Exit Sub
    '    txtMain.AppendText(s + Environment.NewLine)
    '    txtMain.Select(txtMain.TextLength - s.Length - 1, s.Length)
    '    txtMain.SelectionColor() = c
    '    txtMain.Select(txtMain.TextLength, 0)
    '    txtMain.ScrollToCaret()
    '    txtMain.Height = txtInput.Top - txtMain.Margin.Bottom
    'End Sub

    'Private Sub txtInput_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles txtInput.KeyPress
    '    If texter Is Nothing Then Return
    '    If e.KeyChar = ChrW(Keys.Enter) Then
    '        If txtInput.Text = "" Or texter Is Nothing Then Exit Sub
    '        If txtInput.Text.Chars(0) <> "/"c Then
    '            UIREF_showMessage(My.Settings.username + ": " + txtInput.Text, Color.Black)
    '        End If
    '        threadedCall(curry(AddressOf unsafe_sendText, txtInput.Text), "Send Text")
    '        txtInput.Text = ""
    '    End If
    'End Sub
    'Private Sub unsafe_sendText(ByVal s As String)
    '    texter.sendText(s)
    'End Sub
End Class

'Public MustInherit Class W3Texter
'    Public WithEvents client As BnetClient = Nothing
'    Public Event showMessage(ByVal s As String, ByVal c As Color)

'    Public Sub say(ByVal s As String)
'        say(s, Color.Black)
'    End Sub
'    Public Sub say(ByVal s As String, ByVal c As Color)
'        RaiseEvent showMessage(s, c)
'    End Sub
'    Public MustOverride Sub sendText(ByVal s As String)

'    Private Sub client_chatEvent(ByVal sender As BnetClient, ByVal id As Bnet.CHAT_EVENT_ID, ByVal user As String, ByVal text As String) Handles client.chat_event
'        Select Case id
'            Case Bnet.CHAT_EVENT_ID.WHISPER
'                say(user + " whispers: " + text, Color.DarkGreen)
'            Case Bnet.CHAT_EVENT_ID.TALK
'                say(user + ": " + text)
'            Case Bnet.CHAT_EVENT_ID.BROADCAST
'                say("(server broadcast) " + user + ": " + text, Color.Red)
'            Case Bnet.CHAT_EVENT_ID.CHANNEL
'                say("Entered channel " + text, Color.DarkGray)
'            Case Bnet.CHAT_EVENT_ID.WHISPER_SENT
'                say("You whisper to " + user + ": " + text, Color.DarkGreen)
'            Case Bnet.CHAT_EVENT_ID.CHANNEL_FULL
'                say("Channel was full", Color.Red)
'            Case Bnet.CHAT_EVENT_ID.CHANNEL_DOES_NOT_EXIST
'                say("Channel didn't exist", Color.Red)
'            Case Bnet.CHAT_EVENT_ID.CHANNEL_RESTRICTED
'                say("Channel was restricted", Color.Red)
'            Case Bnet.CHAT_EVENT_ID.INFO
'                say(text, Color.Gray)
'            Case Bnet.CHAT_EVENT_ID.ERRORS
'                say(text, Color.Red)
'            Case Bnet.CHAT_EVENT_ID.EMOTE
'                say(user + " " + text, Color.DarkGray)
'        End Select
'    End Sub
'End Class

'Public Class W3ChannelTexter
'    Inherits W3Texter
'    Public Sub New(ByVal client As BnetClient)
'        Me.client = client
'    End Sub
'    Public Overrides Sub sendText(ByVal s As String)
'        client.sendText(s)
'    End Sub
'End Class
