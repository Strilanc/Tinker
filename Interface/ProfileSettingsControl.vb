Public Class ProfileSettingsControl
    Public last_loaded_profile As ClientProfile
    Public Event delete(ByVal sender As ProfileSettingsControl)

    Private Sub repair_values()
        txtTftKey.Text = txtTftKey.Text.Replace("-", "").ToUpper()
        txtRocKey.Text = txtRocKey.Text.Replace("-", "").ToUpper()
    End Sub

    Public Sub load_from_profile(ByVal p As ClientProfile)
        If p Is Nothing Then Return

        txtUsername.Text = p.username
        txtPassword.Text = p.password
        txtTftKey.Text = p.tft_cd_key
        txtRocKey.Text = p.roc_cd_key
        cboGateway.Text = p.server
        numLocalPort.Value = p.listen_port
        txtInitialChannel.Text = p.initial_channel
        txtCKLServer.Text = p.CKL_server
        cboLanHost.Text = p.lan_host

        gridUsers.Rows.Clear()
        For Each user As BotUser In p.users.users
            gridUsers.Rows.Add(user.name, user.packPermissions(), user.packSettings())
        Next user

        btnDeleteProfile.Enabled = p.name <> "Default"

        last_loaded_profile = p
    End Sub
    Public Sub save_to_profile(ByVal p As ClientProfile)
        If p Is Nothing Then Return
        repair_values()

        p.username = txtUsername.Text
        p.password = txtPassword.Text
        p.tft_cd_key = txtTftKey.Text
        p.roc_cd_key = txtRocKey.Text
        p.server = cboGateway.Text
        p.listen_port = CUShort(numLocalPort.Value)
        p.initial_channel = txtInitialChannel.Text
        p.CKL_server = txtCKLServer.Text
        p.lan_host = cboLanHost.Text

        Dim existing_users As New List(Of String)
        For i = 0 To gridUsers.RowCount - 1
            With gridUsers.Rows(i)
                If .Cells(0).Value Is Nothing Then Continue For
                Dim s = CStr(.Cells(0).Value)
                existing_users.Add(s)
                p.users.update_user(New BotUser( _
                            s, _
                            CStr(.Cells(1).Value), _
                            CStr(.Cells(2).Value)))
            End With
        Next i
        p.users.remove_other_users(existing_users)
    End Sub

    Private Sub btnDeleteProfile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteProfile.Click
        RaiseEvent delete(Me)
    End Sub
End Class
