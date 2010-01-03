<ContractVerification(False)>
Public Class ProfileSettingsControl
    Public lastLoadedProfile As Bot.ClientProfile
    Public Event Delete(ByVal sender As ProfileSettingsControl)

    Public Sub LoadFromProfile(ByVal profile As Bot.ClientProfile)
        If profile Is Nothing Then Return

        txtUsername.Text = profile.userName
        txtPassword.Text = profile.password
        txtTftKey.Text = profile.cdKeyTFT
        txtRocKey.Text = profile.cdKeyROC
        cboGateway.Text = profile.server
        txtInitialChannel.Text = profile.initialChannel
        txtCKLServer.Text = profile.CKLServerAddress
        cboLanHost.Text = profile.LanHost

        gridUsers.Rows.Clear()
        For Each user In profile.Users.Users
            Contract.Assume(user IsNot Nothing)
            gridUsers.Rows.Add(user.Name.ToString, user.PackPermissions(), user.PackSettings())
        Next user

        btnDeleteProfile.Enabled = profile.name <> "Default"

        lastLoadedProfile = profile
    End Sub
    Public Sub SaveToProfile(ByVal profile As Bot.ClientProfile)
        If profile Is Nothing Then Return

        profile.userName = txtUsername.Text.AssumeNotNull
        profile.password = txtPassword.Text.AssumeNotNull
        profile.cdKeyTFT = txtTftKey.Text.AssumeNotNull
        profile.cdKeyROC = txtRocKey.Text.AssumeNotNull
        profile.server = cboGateway.Text.AssumeNotNull
        profile.initialChannel = txtInitialChannel.Text.AssumeNotNull
        profile.CKLServerAddress = txtCKLServer.Text.AssumeNotNull
        profile.LanHost = cboLanHost.Text.AssumeNotNull

        Dim currentUsers = New List(Of InvariantString)
        For i = 0 To gridUsers.RowCount - 1
            With gridUsers.Rows(i)
                If .Cells(0).Value Is Nothing Then Continue For
                Dim s = CStr(.Cells(0).Value)
                currentUsers.Add(s)
                profile.Users.UpdateUser(New BotUser( _
                            s,
                            CStr(.Cells(1).Value),
                            CStr(.Cells(2).Value)))
            End With
        Next i
        profile.Users.RemoveAllExcept(currentUsers)
    End Sub

    Private Sub btnDeleteProfile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnDeleteProfile.Click
        RaiseEvent Delete(Me)
    End Sub

    Private Sub txtRocKey_TextChanged() Handles txtRocKey.TextChanged
        lblROCKeyError.Text = ""
        If txtCKLServer.Text <> "" Then
            lblROCKeyError.Text = "(Using CKL)"
            Return
        End If
        If txtRocKey.Text = "" Then
            lblROCKeyError.Text = "No Key Entered"
            Return
        End If
        Try
            Dim key = Bnet.ToWC3CDKeyCredentials(txtRocKey.Text, {}, {})
            If key.Product <> Bnet.ProductType.Warcraft3ROC Then
                lblROCKeyError.Text = "Not a ROC Key"
            End If
        Catch ex As ArgumentException
            lblROCKeyError.Text = ex.Message
        End Try
    End Sub
    Private Sub txtTftKey_TextChanged() Handles txtTftKey.TextChanged
        lblTFTKeyError.Text = ""
        If txtCKLServer.Text <> "" Then
            lblTFTKeyError.Text = "(Using CKL)"
            Return
        End If
        If txtTftKey.Text = "" Then
            lblTFTKeyError.Text = "No Key Entered"
            Return
        End If
        Try
            Dim key = Bnet.ToWC3CDKeyCredentials(txtTftKey.Text, {}, {})
            If key.Product <> Bnet.ProductType.Warcraft3TFT Then
                lblTFTKeyError.Text = "Not a TFT Key"
            End If
        Catch ex As ArgumentException
            lblTFTKeyError.Text = ex.Message
        End Try
    End Sub

    Private Sub txtCKLServer_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtCKLServer.TextChanged
        txtRocKey_TextChanged()
        txtTftKey_TextChanged()
    End Sub
End Class
