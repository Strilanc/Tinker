Public Class ProfileSettingsControl
    Public last_loaded_profile As ClientProfile
    Public Event delete(ByVal sender As ProfileSettingsControl)

    Private Sub RepairValues()
        txtTftKey.Text = txtTftKey.Text.Replace("-", "").ToUpper()
        txtRocKey.Text = txtRocKey.Text.Replace("-", "").ToUpper()
    End Sub

    Public Sub LoadFromProfile(ByVal p As ClientProfile)
        If p Is Nothing Then Return

        txtUsername.Text = p.username
        txtPassword.Text = p.password
        txtTftKey.Text = p.tftCdKey
        txtRocKey.Text = p.rocCdKey
        cboGateway.Text = p.server
        numLocalPort.Value = p.listenPort
        txtInitialChannel.Text = p.initialChannel
        txtCKLServer.Text = p.keyServerAddress
        cboLanHost.Text = p.lanHost

        gridUsers.Rows.Clear()
        For Each user As BotUser In p.users.users
            gridUsers.Rows.Add(user.name, user.packPermissions(), user.packSettings())
        Next user

        btnDeleteProfile.Enabled = p.name <> "Default"

        last_loaded_profile = p
    End Sub
    Public Sub SaveToProfile(ByVal p As ClientProfile)
        If p Is Nothing Then Return
        RepairValues()

        p.username = txtUsername.Text
        p.password = txtPassword.Text
        p.tftCdKey = txtTftKey.Text
        p.rocCdKey = txtRocKey.Text
        p.server = cboGateway.Text
        p.listenPort = CUShort(numLocalPort.Value)
        p.initialChannel = txtInitialChannel.Text
        p.keyServerAddress = txtCKLServer.Text
        p.lanHost = cboLanHost.Text

        Dim existing_users As New List(Of String)
        For i = 0 To gridUsers.RowCount - 1
            With gridUsers.Rows(i)
                If .Cells(0).Value Is Nothing Then Continue For
                Dim s = CStr(.Cells(0).Value)
                existing_users.Add(s)
                p.users.UpdateUser(New BotUser( _
                            s,
                            CStr(.Cells(1).Value),
                            CStr(.Cells(2).Value)))
            End With
        Next i
        p.users.RemoveOtherUsers(existing_users)
    End Sub

    Private Sub btnDeleteProfile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteProfile.Click
        RaiseEvent delete(Me)
    End Sub
End Class
