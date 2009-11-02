Public Class ProfileSettingsControl
    Public lastLoadedProfile As ClientProfile
    Public Event Delete(ByVal sender As ProfileSettingsControl)

    Private Sub RepairValues()
        txtTftKey.Text = txtTftKey.Text.Replace("-", "").ToUpperInvariant
        txtRocKey.Text = txtRocKey.Text.Replace("-", "").ToUpperInvariant
    End Sub

    Public Sub LoadFromProfile(ByVal profile As ClientProfile)
        If profile Is Nothing Then Return

        txtUsername.Text = profile.userName
        txtPassword.Text = profile.password
        txtTftKey.Text = profile.cdKeyTFT
        txtRocKey.Text = profile.cdKeyROC
        cboGateway.Text = profile.server
        numLocalPort.Value = profile.listenPort
        txtInitialChannel.Text = profile.initialChannel
        txtCKLServer.Text = profile.CKLServerAddress
        cboLanHost.Text = profile.LanHost

        gridUsers.Rows.Clear()
        For Each user As BotUser In profile.users.Users
            gridUsers.Rows.Add(user.Name, user.PackPermissions(), user.PackSettings())
        Next user

        btnDeleteProfile.Enabled = profile.name <> "Default"

        lastLoadedProfile = profile
    End Sub
    Public Sub SaveToProfile(ByVal profile As ClientProfile)
        If profile Is Nothing Then Return
        RepairValues()

        profile.userName = txtUsername.Text
        profile.password = txtPassword.Text
        profile.cdKeyTFT = txtTftKey.Text
        profile.cdKeyROC = txtRocKey.Text
        profile.server = cboGateway.Text
        profile.listenPort = CUShort(numLocalPort.Value)
        profile.initialChannel = txtInitialChannel.Text
        profile.CKLServerAddress = txtCKLServer.Text
        profile.LanHost = cboLanHost.Text

        Dim existing_users As New List(Of String)
        For i = 0 To gridUsers.RowCount - 1
            With gridUsers.Rows(i)
                If .Cells(0).Value Is Nothing Then Continue For
                Dim s = CStr(.Cells(0).Value)
                existing_users.Add(s)
                profile.users.UpdateUser(New BotUser( _
                            s,
                            CStr(.Cells(1).Value),
                            CStr(.Cells(2).Value)))
            End With
        Next i
        profile.users.RemoveOtherUsers(existing_users)
    End Sub

    Private Sub btnDeleteProfile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteProfile.Click
        RaiseEvent Delete(Me)
    End Sub

    Private Sub tabSettings_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tabSettings.Click

    End Sub

    Private Sub txtRocKey_TextChanged() Handles txtRocKey.TextChanged
        lblROCKeyError.Text = ""
        If txtRocKey.Text = "" Then
            lblROCKeyError.Text = "No Key Entered"
            Return
        End If
        Try
            Dim key = Bnet.CDKey.FromWC3StyleKey(txtRocKey.Text)
            If key.Product <> Bnet.CDKeyProduct.Warcraft3ROC Then
                lblROCKeyError.Text = "Not a ROC Key"
            End If
        Catch ex As ArgumentException
            lblROCKeyError.Text = ex.Message
        End Try
    End Sub
    Private Sub txtTftKey_TextChanged() Handles txtTftKey.TextChanged
        lblTFTKeyError.Text = ""
        If txtTftKey.Text = "" Then
            lblTFTKeyError.Text = "No Key Entered"
            Return
        End If
        Try
            Dim key = Bnet.CDKey.FromWC3StyleKey(txtTftKey.Text)
            If key.Product <> Bnet.CDKeyProduct.Warcraft3TFT Then
                lblTFTKeyError.Text = "Not a TFT Key"
            End If
        Catch ex As ArgumentException
            lblTFTKeyError.Text = ex.Message
        End Try
    End Sub
End Class
