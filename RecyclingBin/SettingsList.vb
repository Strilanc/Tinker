Public Class SettingsList
    'Private ReadOnly uiRef As New InvokedCallQueue(Me, Me.gettype.name + " ui ref")
    'Private settings As Settings.Settings
    'Public Sub UIREF_hook(ByVal settings As Settings.Settings)
    '    If uiRef.queueIfRemote(Function() eval(AddressOf UIREF_hook, settings)) Then Return
    '    If Me.settings Is settings Then Return
    '    Me.settings = settings
    '    gridSettings.Rows.Clear()
    '    If settings Is Nothing Then Return
    '    For Each s As Settings.itfSetting In settings.settings
    '        gridSettings.Rows.Add(s.name(), s.type(), s.value())
    '    Next s
    'End Sub

    'Private Sub gridSettings_CellEndEdit(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles gridSettings.CellEndEdit
    '    Dim c As DataGridViewCell = gridSettings.Rows(e.RowIndex).Cells(e.ColumnIndex)
    '    Dim val As String = c.Value().ToString()
    '    Dim s As Settings.itfSetting = settings.settings(e.RowIndex)
    '    If s.validate(val) Then
    '        s.value = val
    '    Else
    '        c.Value = s.value
    '    End If
    'End Sub
End Class
