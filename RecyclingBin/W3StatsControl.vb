Public Class W3StatsControl
    'Private WithEvents stats As BotStats
    'Private ReadOnly uiRef As New InvokedCallQueue(Me, Me.gettype.name + " uiRef")

    'Public Sub UIREF_hook(ByVal stats As BotStats)
    '    If uiRef.queueIfRemote(curry(AddressOf UIREF_hook, stats)) Then Return
    '    Me.stats = stats
    '    txtStats.Text = "Statistics"
    '    If stats Is Nothing Then
    '        graphJoin.hook(Nothing)
    '        graphLeave.hook(Nothing)
    '    Else
    '        graphJoin.hook(stats.playersJoined)
    '        graphLeave.hook(stats.playersLeft)
    '    End If
    '    UIREF_update()
    'End Sub

    'Private Sub UIREF_update() Handles stats.updated
    '    If uiRef.queueIfRemote(AddressOf UIREF_update) Then Return
    '    If stats Is Nothing Then
    '        txtStats.Text = "Statistics"
    '    Else
    '        txtStats.Text = stats.uiToString()
    '    End If
    'End Sub
End Class
