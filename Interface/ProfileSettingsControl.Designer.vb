<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ProfileSettingsControl
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ProfileSettingsControl))
        Me.tabsProfile = New System.Windows.Forms.TabControl()
        Me.tabSettings = New System.Windows.Forms.TabPage()
        Me.btnDeleteProfile = New System.Windows.Forms.Button()
        Me.txtCKLServer = New System.Windows.Forms.TextBox()
        Me.lblCKLServer = New System.Windows.Forms.Label()
        Me.lblLanHost = New System.Windows.Forms.Label()
        Me.cboLanHost = New System.Windows.Forms.ComboBox()
        Me.lblRocKey = New System.Windows.Forms.Label()
        Me.txtRocKey = New System.Windows.Forms.TextBox()
        Me.txtTftKey = New System.Windows.Forms.TextBox()
        Me.lblTftKey = New System.Windows.Forms.Label()
        Me.lblUsername = New System.Windows.Forms.Label()
        Me.lblInitialChannel = New System.Windows.Forms.Label()
        Me.txtInitialChannel = New System.Windows.Forms.TextBox()
        Me.lblGateway = New System.Windows.Forms.Label()
        Me.cboGateway = New System.Windows.Forms.ComboBox()
        Me.lblPassword = New System.Windows.Forms.Label()
        Me.txtPassword = New System.Windows.Forms.TextBox()
        Me.txtUsername = New System.Windows.Forms.TextBox()
        Me.lblROCKeyError = New System.Windows.Forms.Label()
        Me.lblTFTKeyError = New System.Windows.Forms.Label()
        Me.tabUsers = New System.Windows.Forms.TabPage()
        Me.gridUsers = New System.Windows.Forms.DataGridView()
        Me.colName = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colAccess = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colSettings = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.tipNormal = New System.Windows.Forms.ToolTip()
        Me.tabsProfile.SuspendLayout()
        Me.tabSettings.SuspendLayout()
        Me.tabUsers.SuspendLayout()
        Me.SuspendLayout()
        '
        'tabsProfile
        '
        Me.tabsProfile.Controls.Add(Me.tabSettings)
        Me.tabsProfile.Controls.Add(Me.tabUsers)
        Me.tabsProfile.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabsProfile.Location = New System.Drawing.Point(0, 0)
        Me.tabsProfile.Name = "tabsProfile"
        Me.tabsProfile.SelectedIndex = 0
        Me.tabsProfile.Size = New System.Drawing.Size(482, 227)
        Me.tabsProfile.TabIndex = 43
        '
        'tabSettings
        '
        Me.tabSettings.AutoScroll = True
        Me.tabSettings.Controls.Add(Me.btnDeleteProfile)
        Me.tabSettings.Controls.Add(Me.txtCKLServer)
        Me.tabSettings.Controls.Add(Me.lblCKLServer)
        Me.tabSettings.Controls.Add(Me.lblLanHost)
        Me.tabSettings.Controls.Add(Me.cboLanHost)
        Me.tabSettings.Controls.Add(Me.lblRocKey)
        Me.tabSettings.Controls.Add(Me.txtRocKey)
        Me.tabSettings.Controls.Add(Me.txtTftKey)
        Me.tabSettings.Controls.Add(Me.lblTftKey)
        Me.tabSettings.Controls.Add(Me.lblUsername)
        Me.tabSettings.Controls.Add(Me.lblInitialChannel)
        Me.tabSettings.Controls.Add(Me.txtInitialChannel)
        Me.tabSettings.Controls.Add(Me.lblGateway)
        Me.tabSettings.Controls.Add(Me.cboGateway)
        Me.tabSettings.Controls.Add(Me.lblPassword)
        Me.tabSettings.Controls.Add(Me.txtPassword)
        Me.tabSettings.Controls.Add(Me.txtUsername)
        Me.tabSettings.Controls.Add(Me.lblROCKeyError)
        Me.tabSettings.Controls.Add(Me.lblTFTKeyError)
        Me.tabSettings.Location = New System.Drawing.Point(4, 22)
        Me.tabSettings.Name = "tabSettings"
        Me.tabSettings.Padding = New System.Windows.Forms.Padding(3)
        Me.tabSettings.Size = New System.Drawing.Size(474, 201)
        Me.tabSettings.TabIndex = 0
        Me.tabSettings.Text = "Settings"
        Me.tabSettings.UseVisualStyleBackColor = True
        '
        'btnDeleteProfile
        '
        Me.btnDeleteProfile.Location = New System.Drawing.Point(6, 164)
        Me.btnDeleteProfile.Name = "btnDeleteProfile"
        Me.btnDeleteProfile.Size = New System.Drawing.Size(446, 27)
        Me.btnDeleteProfile.TabIndex = 60
        Me.btnDeleteProfile.Text = "Delete this Profile"
        Me.btnDeleteProfile.UseVisualStyleBackColor = True
        '
        'txtCKLServer
        '
        Me.txtCKLServer.Font = New System.Drawing.Font("Courier New", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtCKLServer.Location = New System.Drawing.Point(6, 137)
        Me.txtCKLServer.Name = "txtCKLServer"
        Me.txtCKLServer.Size = New System.Drawing.Size(220, 20)
        Me.txtCKLServer.TabIndex = 58
        Me.tipNormal.SetToolTip(Me.txtCKLServer, resources.GetString("txtCKLServer.ToolTip"))
        '
        'lblCKLServer
        '
        Me.lblCKLServer.AutoSize = True
        Me.lblCKLServer.Location = New System.Drawing.Point(3, 121)
        Me.lblCKLServer.Name = "lblCKLServer"
        Me.lblCKLServer.Size = New System.Drawing.Size(174, 13)
        Me.lblCKLServer.TabIndex = 59
        Me.lblCKLServer.Text = "(Advanced) Cd Key Lending Server"
        '
        'lblLanHost
        '
        Me.lblLanHost.AutoSize = True
        Me.lblLanHost.Location = New System.Drawing.Point(229, 121)
        Me.lblLanHost.Name = "lblLanHost"
        Me.lblLanHost.Size = New System.Drawing.Size(100, 13)
        Me.lblLanHost.TabIndex = 57
        Me.lblLanHost.Text = "LAN Advertise Host"
        '
        'cboLanHost
        '
        Me.cboLanHost.FormattingEnabled = True
        Me.cboLanHost.Items.AddRange(New Object() {" (None)", "127.0.0.1 (LocalHost)", "255.255.255.255 (Broadcast Local LAN)"})
        Me.cboLanHost.Location = New System.Drawing.Point(232, 137)
        Me.cboLanHost.Name = "cboLanHost"
        Me.cboLanHost.Size = New System.Drawing.Size(220, 21)
        Me.cboLanHost.TabIndex = 56
        Me.tipNormal.SetToolTip(Me.cboLanHost, "Games hosted with this profile will be advertised on LAN to this address.")
        '
        'lblRocKey
        '
        Me.lblRocKey.AutoSize = True
        Me.lblRocKey.Location = New System.Drawing.Point(3, 82)
        Me.lblRocKey.Name = "lblRocKey"
        Me.lblRocKey.Size = New System.Drawing.Size(69, 13)
        Me.lblRocKey.TabIndex = 52
        Me.lblRocKey.Text = "ROC CD Key"
        '
        'txtRocKey
        '
        Me.txtRocKey.Font = New System.Drawing.Font("Courier New", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtRocKey.Location = New System.Drawing.Point(6, 98)
        Me.txtRocKey.Name = "txtRocKey"
        Me.txtRocKey.Size = New System.Drawing.Size(220, 20)
        Me.txtRocKey.TabIndex = 53
        Me.tipNormal.SetToolTip(Me.txtRocKey, "Reign of Chaos CD key." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Dashes and upper/lower case are ignored.")
        '
        'txtTftKey
        '
        Me.txtTftKey.Font = New System.Drawing.Font("Courier New", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtTftKey.Location = New System.Drawing.Point(232, 98)
        Me.txtTftKey.Name = "txtTftKey"
        Me.txtTftKey.Size = New System.Drawing.Size(220, 20)
        Me.txtTftKey.TabIndex = 54
        Me.tipNormal.SetToolTip(Me.txtTftKey, "Frozen Throne CD key." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Dashes and upper/lower case are ignored.")
        '
        'lblTftKey
        '
        Me.lblTftKey.AutoSize = True
        Me.lblTftKey.Location = New System.Drawing.Point(229, 82)
        Me.lblTftKey.Name = "lblTftKey"
        Me.lblTftKey.Size = New System.Drawing.Size(66, 13)
        Me.lblTftKey.TabIndex = 55
        Me.lblTftKey.Text = "TFT CD Key"
        '
        'lblUsername
        '
        Me.lblUsername.AutoSize = True
        Me.lblUsername.Location = New System.Drawing.Point(3, 43)
        Me.lblUsername.Name = "lblUsername"
        Me.lblUsername.Size = New System.Drawing.Size(55, 13)
        Me.lblUsername.TabIndex = 42
        Me.lblUsername.Text = "Username"
        '
        'lblInitialChannel
        '
        Me.lblInitialChannel.AutoSize = True
        Me.lblInitialChannel.Location = New System.Drawing.Point(229, 3)
        Me.lblInitialChannel.Name = "lblInitialChannel"
        Me.lblInitialChannel.Size = New System.Drawing.Size(73, 13)
        Me.lblInitialChannel.TabIndex = 51
        Me.lblInitialChannel.Text = "Initial Channel"
        '
        'txtInitialChannel
        '
        Me.txtInitialChannel.Location = New System.Drawing.Point(232, 19)
        Me.txtInitialChannel.Name = "txtInitialChannel"
        Me.txtInitialChannel.Size = New System.Drawing.Size(220, 20)
        Me.txtInitialChannel.TabIndex = 47
        Me.tipNormal.SetToolTip(Me.txtInitialChannel, "The channel the bot will enter after it connects.")
        '
        'lblGateway
        '
        Me.lblGateway.AutoSize = True
        Me.lblGateway.Location = New System.Drawing.Point(3, 3)
        Me.lblGateway.Name = "lblGateway"
        Me.lblGateway.Size = New System.Drawing.Size(49, 13)
        Me.lblGateway.TabIndex = 48
        Me.lblGateway.Text = "Gateway"
        '
        'cboGateway
        '
        Me.cboGateway.FormattingEnabled = True
        Me.cboGateway.Items.AddRange(New Object() {"asia.battle.net (Kalimdor)", "europe.battle.net (Northrend)", "useast.battle.net (Azeroth)", "uswest.battle.net (Lordaeron)", "beta.battle.net (Westfall)"})
        Me.cboGateway.Location = New System.Drawing.Point(6, 19)
        Me.cboGateway.Name = "cboGateway"
        Me.cboGateway.Size = New System.Drawing.Size(220, 21)
        Me.cboGateway.TabIndex = 46
        Me.tipNormal.SetToolTip(Me.cboGateway, "The battle.net server to connect to.")
        '
        'lblPassword
        '
        Me.lblPassword.AutoSize = True
        Me.lblPassword.Location = New System.Drawing.Point(229, 43)
        Me.lblPassword.Name = "lblPassword"
        Me.lblPassword.Size = New System.Drawing.Size(53, 13)
        Me.lblPassword.TabIndex = 45
        Me.lblPassword.Text = "Password"
        '
        'txtPassword
        '
        Me.txtPassword.Location = New System.Drawing.Point(232, 59)
        Me.txtPassword.Name = "txtPassword"
        Me.txtPassword.PasswordChar = Global.Microsoft.VisualBasic.ChrW(42)
        Me.txtPassword.Size = New System.Drawing.Size(220, 20)
        Me.txtPassword.TabIndex = 44
        Me.tipNormal.SetToolTip(Me.txtPassword, "The password of the account to logon with." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Passwords are not case sensitive.")
        '
        'txtUsername
        '
        Me.txtUsername.Location = New System.Drawing.Point(6, 59)
        Me.txtUsername.Name = "txtUsername"
        Me.txtUsername.Size = New System.Drawing.Size(220, 20)
        Me.txtUsername.TabIndex = 43
        Me.tipNormal.SetToolTip(Me.txtUsername, "The username of the account to logon with.")
        '
        'lblROCKeyError
        '
        Me.lblROCKeyError.Font = New System.Drawing.Font("Courier New", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblROCKeyError.ForeColor = System.Drawing.Color.Red
        Me.lblROCKeyError.Location = New System.Drawing.Point(6, 82)
        Me.lblROCKeyError.Name = "lblROCKeyError"
        Me.lblROCKeyError.Size = New System.Drawing.Size(220, 13)
        Me.lblROCKeyError.TabIndex = 61
        Me.lblROCKeyError.Text = "No Key Entered"
        Me.lblROCKeyError.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'lblTFTKeyError
        '
        Me.lblTFTKeyError.Font = New System.Drawing.Font("Courier New", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblTFTKeyError.ForeColor = System.Drawing.Color.Red
        Me.lblTFTKeyError.Location = New System.Drawing.Point(232, 82)
        Me.lblTFTKeyError.Name = "lblTFTKeyError"
        Me.lblTFTKeyError.Size = New System.Drawing.Size(220, 13)
        Me.lblTFTKeyError.TabIndex = 62
        Me.lblTFTKeyError.Text = "No Key Entered"
        Me.lblTFTKeyError.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'tabUsers
        '
        Me.tabUsers.Controls.Add(Me.gridUsers)
        Me.tabUsers.Location = New System.Drawing.Point(4, 22)
        Me.tabUsers.Name = "tabUsers"
        Me.tabUsers.Padding = New System.Windows.Forms.Padding(3)
        Me.tabUsers.Size = New System.Drawing.Size(474, 201)
        Me.tabUsers.TabIndex = 1
        Me.tabUsers.Text = "Users"
        Me.tabUsers.UseVisualStyleBackColor = True
        '
        'gridUsers
        '
        Me.gridUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.gridUsers.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.colName, Me.colAccess, Me.colSettings})
        Me.gridUsers.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridUsers.Location = New System.Drawing.Point(3, 3)
        Me.gridUsers.Name = "gridUsers"
        Me.gridUsers.Size = New System.Drawing.Size(468, 233)
        Me.gridUsers.TabIndex = 32
        '
        'colName
        '
        Me.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.colName.HeaderText = "Name"
        Me.colName.Name = "colName"
        '
        'colAccess
        '
        Me.colAccess.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.colAccess.HeaderText = "Access"
        Me.colAccess.Name = "colAccess"
        '
        'colSettings
        '
        Me.colSettings.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.colSettings.HeaderText = "Stored Values"
        Me.colSettings.Name = "colSettings"
        Me.colSettings.Visible = False
        '
        'tipNormal
        '
        Me.tipNormal.AutoPopDelay = 1000000
        Me.tipNormal.InitialDelay = 1
        Me.tipNormal.ReshowDelay = 1
        Me.tipNormal.ShowAlways = True
        '
        'ProfileSettingsControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.tabsProfile)
        Me.Name = "ProfileSettingsControl"
        Me.Size = New System.Drawing.Size(482, 227)
        Me.tabsProfile.ResumeLayout(False)
        Me.tabSettings.ResumeLayout(False)
        Me.tabSettings.PerformLayout()
        Me.tabUsers.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents tabsProfile As System.Windows.Forms.TabControl
    Friend WithEvents tabSettings As System.Windows.Forms.TabPage
    Friend WithEvents tabUsers As System.Windows.Forms.TabPage
    Friend WithEvents txtCKLServer As System.Windows.Forms.TextBox
    Friend WithEvents lblCKLServer As System.Windows.Forms.Label
    Friend WithEvents lblLanHost As System.Windows.Forms.Label
    Friend WithEvents cboLanHost As System.Windows.Forms.ComboBox
    Friend WithEvents lblRocKey As System.Windows.Forms.Label
    Friend WithEvents txtRocKey As System.Windows.Forms.TextBox
    Friend WithEvents txtTftKey As System.Windows.Forms.TextBox
    Friend WithEvents lblTftKey As System.Windows.Forms.Label
    Friend WithEvents lblUsername As System.Windows.Forms.Label
    Friend WithEvents lblInitialChannel As System.Windows.Forms.Label
    Friend WithEvents txtInitialChannel As System.Windows.Forms.TextBox
    Friend WithEvents lblGateway As System.Windows.Forms.Label
    Friend WithEvents cboGateway As System.Windows.Forms.ComboBox
    Friend WithEvents lblPassword As System.Windows.Forms.Label
    Friend WithEvents txtPassword As System.Windows.Forms.TextBox
    Friend WithEvents txtUsername As System.Windows.Forms.TextBox
    Friend WithEvents gridUsers As System.Windows.Forms.DataGridView
    Friend WithEvents colName As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colAccess As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colSettings As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents btnDeleteProfile As System.Windows.Forms.Button
    Friend WithEvents tipNormal As System.Windows.Forms.ToolTip
    Friend WithEvents lblTFTKeyError As System.Windows.Forms.Label
    Friend WithEvents lblROCKeyError As System.Windows.Forms.Label

End Class
