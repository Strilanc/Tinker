<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SettingsForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing AndAlso components IsNot Nothing Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SettingsForm))
        Me.tipNormal = New System.Windows.Forms.ToolTip(Me.components)
        Me.txtMapPath = New System.Windows.Forms.TextBox()
        Me.txtProgramPath = New System.Windows.Forms.TextBox()
        Me.txtExeVersion = New System.Windows.Forms.TextBox()
        Me.txtExeInformation = New System.Windows.Forms.TextBox()
        Me.txtCdKeyOwner = New System.Windows.Forms.TextBox()
        Me.txtCommandPrefix = New System.Windows.Forms.TextBox()
        Me.numTickPeriod = New System.Windows.Forms.NumericUpDown()
        Me.numLagLimit = New System.Windows.Forms.NumericUpDown()
        Me.txtNewProfileName = New System.Windows.Forms.TextBox()
        Me.txtInGameName = New System.Windows.Forms.TextBox()
        Me.txtInitialPlugins = New System.Windows.Forms.TextBox()
        Me.txtPortPool = New System.Windows.Forms.TextBox()
        Me.tabsSettings = New System.Windows.Forms.TabControl()
        Me.tabGlobalSettings = New System.Windows.Forms.TabPage()
        Me.lblPortPool = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.lblLagLimit = New System.Windows.Forms.Label()
        Me.lblTickPeriod = New System.Windows.Forms.Label()
        Me.lblCommandPrefix = New System.Windows.Forms.Label()
        Me.lblExeInfo = New System.Windows.Forms.Label()
        Me.lblOwner = New System.Windows.Forms.Label()
        Me.lblWc3Version = New System.Windows.Forms.Label()
        Me.lblMapPath = New System.Windows.Forms.Label()
        Me.lblPath = New System.Windows.Forms.Label()
        Me.tabPlugins = New System.Windows.Forms.TabPage()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.btnImportPlugin = New System.Windows.Forms.Button()
        Me.gridPlugins = New System.Windows.Forms.DataGridView()
        Me.colName = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colAccess = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colSettings = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.tabNewProfile = New System.Windows.Forms.TabPage()
        Me.btnCreateNewProfile = New System.Windows.Forms.Button()
        Me.lblNewProfileName = New System.Windows.Forms.Label()
        Me.btnPluginsHelp = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnUserHelp = New System.Windows.Forms.Button()
        Me.OpenFileDialog = New System.Windows.Forms.OpenFileDialog()
        Me.txtBnlsServer = New System.Windows.Forms.TextBox()
        Me.lblBnlsServer = New System.Windows.Forms.Label()
        CType(Me.numTickPeriod, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.numLagLimit, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.tabsSettings.SuspendLayout()
        Me.tabGlobalSettings.SuspendLayout()
        Me.tabPlugins.SuspendLayout()
        CType(Me.gridPlugins, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.tabNewProfile.SuspendLayout()
        Me.SuspendLayout()
        '
        'tipNormal
        '
        Me.tipNormal.AutoPopDelay = 1000000
        Me.tipNormal.InitialDelay = 1
        Me.tipNormal.ReshowDelay = 1
        Me.tipNormal.ShowAlways = True
        '
        'txtMapPath
        '
        Me.txtMapPath.Location = New System.Drawing.Point(6, 97)
        Me.txtMapPath.Name = "txtMapPath"
        Me.txtMapPath.Size = New System.Drawing.Size(446, 20)
        Me.txtMapPath.TabIndex = 36
        Me.tipNormal.SetToolTip(Me.txtMapPath, "The location of the folder where maps are stored." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Default: C:\Program Files\Warc" & _
                "raft III\Maps\HostBot\")
        '
        'txtProgramPath
        '
        Me.txtProgramPath.Location = New System.Drawing.Point(6, 58)
        Me.txtProgramPath.Name = "txtProgramPath"
        Me.txtProgramPath.Size = New System.Drawing.Size(446, 20)
        Me.txtProgramPath.TabIndex = 34
        Me.tipNormal.SetToolTip(Me.txtProgramPath, "The location of the folder containing the hash files (war3.exe, storm.dll, game.d" & _
                "ll)" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Default: C:\Program Files\Warcraft III\")
        '
        'txtExeVersion
        '
        Me.txtExeVersion.Location = New System.Drawing.Point(232, 19)
        Me.txtExeVersion.Name = "txtExeVersion"
        Me.txtExeVersion.Size = New System.Drawing.Size(220, 20)
        Me.txtExeVersion.TabIndex = 38
        Me.tipNormal.SetToolTip(Me.txtExeVersion, "The current version of warcraft 3." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "(shown in bottom right of wc3 intro screen)")
        '
        'txtExeInformation
        '
        Me.txtExeInformation.Location = New System.Drawing.Point(232, 136)
        Me.txtExeInformation.Name = "txtExeInformation"
        Me.txtExeInformation.Size = New System.Drawing.Size(220, 20)
        Me.txtExeInformation.TabIndex = 42
        Me.tipNormal.SetToolTip(Me.txtExeInformation, "Extra information about the program, such as when it was installed." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "(not particu" & _
                "larly important, but given to Bnet during authentication)")
        '
        'txtCdKeyOwner
        '
        Me.txtCdKeyOwner.Location = New System.Drawing.Point(6, 136)
        Me.txtCdKeyOwner.Name = "txtCdKeyOwner"
        Me.txtCdKeyOwner.Size = New System.Drawing.Size(220, 20)
        Me.txtCdKeyOwner.TabIndex = 40
        Me.tipNormal.SetToolTip(Me.txtCdKeyOwner, "The name displayed to people when they try to logon using a cd key already in use" & _
                ".")
        '
        'txtCommandPrefix
        '
        Me.txtCommandPrefix.Location = New System.Drawing.Point(6, 19)
        Me.txtCommandPrefix.Name = "txtCommandPrefix"
        Me.txtCommandPrefix.Size = New System.Drawing.Size(220, 20)
        Me.txtCommandPrefix.TabIndex = 46
        Me.tipNormal.SetToolTip(Me.txtCommandPrefix, "Prefix that indicates following text is a command.")
        '
        'numTickPeriod
        '
        Me.numTickPeriod.Location = New System.Drawing.Point(6, 175)
        Me.numTickPeriod.Maximum = New Decimal(New Integer() {10000, 0, 0, 0})
        Me.numTickPeriod.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.numTickPeriod.Name = "numTickPeriod"
        Me.numTickPeriod.Size = New System.Drawing.Size(220, 20)
        Me.numTickPeriod.TabIndex = 48
        Me.tipNormal.SetToolTip(Me.numTickPeriod, "Determines the game tick period in milliseconds." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Default value for bnet is 250, " & _
                "lan is 100." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Lower values decrease expected command latency, but increase networ" & _
                "k traffic.")
        Me.numTickPeriod.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'numLagLimit
        '
        Me.numLagLimit.Location = New System.Drawing.Point(232, 175)
        Me.numLagLimit.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.numLagLimit.Name = "numLagLimit"
        Me.numLagLimit.Size = New System.Drawing.Size(220, 20)
        Me.numLagLimit.TabIndex = 50
        Me.tipNormal.SetToolTip(Me.numLagLimit, resources.GetString("numLagLimit.ToolTip"))
        '
        'txtNewProfileName
        '
        Me.txtNewProfileName.Location = New System.Drawing.Point(6, 19)
        Me.txtNewProfileName.Name = "txtNewProfileName"
        Me.txtNewProfileName.Size = New System.Drawing.Size(220, 20)
        Me.txtNewProfileName.TabIndex = 48
        Me.txtNewProfileName.Text = "New Profile"
        Me.tipNormal.SetToolTip(Me.txtNewProfileName, "The name to assign to the new profile.")
        '
        'txtInGameName
        '
        Me.txtInGameName.Location = New System.Drawing.Point(6, 214)
        Me.txtInGameName.MaxLength = 15
        Me.txtInGameName.Name = "txtInGameName"
        Me.txtInGameName.Size = New System.Drawing.Size(220, 20)
        Me.txtInGameName.TabIndex = 52
        Me.tipNormal.SetToolTip(Me.txtInGameName, resources.GetString("txtInGameName.ToolTip"))
        '
        'txtInitialPlugins
        '
        Me.txtInitialPlugins.Location = New System.Drawing.Point(6, 294)
        Me.txtInitialPlugins.MaxLength = 15
        Me.txtInitialPlugins.Name = "txtInitialPlugins"
        Me.txtInitialPlugins.Size = New System.Drawing.Size(271, 20)
        Me.txtInitialPlugins.TabIndex = 56
        Me.tipNormal.SetToolTip(Me.txtInitialPlugins, "The names of plugins to load when the bot starts." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Separate names with a semi-col" & _
                "on, like this:" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "plugin1;plugin2;plugin3")
        '
        'txtPortPool
        '
        Me.txtPortPool.Location = New System.Drawing.Point(232, 214)
        Me.txtPortPool.MaxLength = 15
        Me.txtPortPool.Name = "txtPortPool"
        Me.txtPortPool.Size = New System.Drawing.Size(220, 20)
        Me.txtPortPool.TabIndex = 54
        Me.tipNormal.SetToolTip(Me.txtPortPool, resources.GetString("txtPortPool.ToolTip"))
        '
        'tabsSettings
        '
        Me.tabsSettings.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.tabsSettings.Controls.Add(Me.tabGlobalSettings)
        Me.tabsSettings.Controls.Add(Me.tabPlugins)
        Me.tabsSettings.Controls.Add(Me.tabNewProfile)
        Me.tabsSettings.Location = New System.Drawing.Point(12, 12)
        Me.tabsSettings.Name = "tabsSettings"
        Me.tabsSettings.SelectedIndex = 0
        Me.tabsSettings.Size = New System.Drawing.Size(481, 347)
        Me.tabsSettings.TabIndex = 30
        '
        'tabGlobalSettings
        '
        Me.tabGlobalSettings.AutoScroll = True
        Me.tabGlobalSettings.Controls.Add(Me.txtBnlsServer)
        Me.tabGlobalSettings.Controls.Add(Me.lblBnlsServer)
        Me.tabGlobalSettings.Controls.Add(Me.lblPortPool)
        Me.tabGlobalSettings.Controls.Add(Me.txtPortPool)
        Me.tabGlobalSettings.Controls.Add(Me.Label1)
        Me.tabGlobalSettings.Controls.Add(Me.txtInGameName)
        Me.tabGlobalSettings.Controls.Add(Me.numLagLimit)
        Me.tabGlobalSettings.Controls.Add(Me.lblLagLimit)
        Me.tabGlobalSettings.Controls.Add(Me.numTickPeriod)
        Me.tabGlobalSettings.Controls.Add(Me.lblTickPeriod)
        Me.tabGlobalSettings.Controls.Add(Me.lblCommandPrefix)
        Me.tabGlobalSettings.Controls.Add(Me.txtCommandPrefix)
        Me.tabGlobalSettings.Controls.Add(Me.lblExeInfo)
        Me.tabGlobalSettings.Controls.Add(Me.txtExeInformation)
        Me.tabGlobalSettings.Controls.Add(Me.lblOwner)
        Me.tabGlobalSettings.Controls.Add(Me.txtCdKeyOwner)
        Me.tabGlobalSettings.Controls.Add(Me.lblWc3Version)
        Me.tabGlobalSettings.Controls.Add(Me.txtExeVersion)
        Me.tabGlobalSettings.Controls.Add(Me.lblMapPath)
        Me.tabGlobalSettings.Controls.Add(Me.txtMapPath)
        Me.tabGlobalSettings.Controls.Add(Me.txtProgramPath)
        Me.tabGlobalSettings.Controls.Add(Me.lblPath)
        Me.tabGlobalSettings.Location = New System.Drawing.Point(4, 22)
        Me.tabGlobalSettings.Name = "tabGlobalSettings"
        Me.tabGlobalSettings.Size = New System.Drawing.Size(473, 321)
        Me.tabGlobalSettings.TabIndex = 3
        Me.tabGlobalSettings.Text = "Global"
        Me.tabGlobalSettings.UseVisualStyleBackColor = True
        '
        'lblPortPool
        '
        Me.lblPortPool.AutoSize = True
        Me.lblPortPool.Location = New System.Drawing.Point(229, 198)
        Me.lblPortPool.Name = "lblPortPool"
        Me.lblPortPool.Size = New System.Drawing.Size(50, 13)
        Me.lblPortPool.TabIndex = 55
        Me.lblPortPool.Text = "Port Pool"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(3, 198)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(74, 13)
        Me.Label1.TabIndex = 53
        Me.Label1.Text = "In-game name"
        '
        'lblLagLimit
        '
        Me.lblLagLimit.AutoSize = True
        Me.lblLagLimit.Location = New System.Drawing.Point(229, 159)
        Me.lblLagLimit.Name = "lblLagLimit"
        Me.lblLagLimit.Size = New System.Drawing.Size(117, 13)
        Me.lblLagLimit.TabIndex = 51
        Me.lblLagLimit.Text = "Default Game Lag Limit"
        '
        'lblTickPeriod
        '
        Me.lblTickPeriod.AutoSize = True
        Me.lblTickPeriod.Location = New System.Drawing.Point(3, 159)
        Me.lblTickPeriod.Name = "lblTickPeriod"
        Me.lblTickPeriod.Size = New System.Drawing.Size(129, 13)
        Me.lblTickPeriod.TabIndex = 49
        Me.lblTickPeriod.Text = "Default Game Tick Period"
        '
        'lblCommandPrefix
        '
        Me.lblCommandPrefix.AutoSize = True
        Me.lblCommandPrefix.Location = New System.Drawing.Point(3, 3)
        Me.lblCommandPrefix.Name = "lblCommandPrefix"
        Me.lblCommandPrefix.Size = New System.Drawing.Size(83, 13)
        Me.lblCommandPrefix.TabIndex = 47
        Me.lblCommandPrefix.Text = "Command Prefix"
        '
        'lblExeInfo
        '
        Me.lblExeInfo.AutoSize = True
        Me.lblExeInfo.Location = New System.Drawing.Point(229, 120)
        Me.lblExeInfo.Name = "lblExeInfo"
        Me.lblExeInfo.Size = New System.Drawing.Size(80, 13)
        Me.lblExeInfo.TabIndex = 43
        Me.lblExeInfo.Text = "Exe Information"
        '
        'lblOwner
        '
        Me.lblOwner.AutoSize = True
        Me.lblOwner.Location = New System.Drawing.Point(3, 120)
        Me.lblOwner.Name = "lblOwner"
        Me.lblOwner.Size = New System.Drawing.Size(77, 13)
        Me.lblOwner.TabIndex = 41
        Me.lblOwner.Text = "CD Key Owner"
        '
        'lblWc3Version
        '
        Me.lblWc3Version.AutoSize = True
        Me.lblWc3Version.Location = New System.Drawing.Point(229, 3)
        Me.lblWc3Version.Name = "lblWc3Version"
        Me.lblWc3Version.Size = New System.Drawing.Size(95, 13)
        Me.lblWc3Version.TabIndex = 39
        Me.lblWc3Version.Text = "Warcraft 3 Version"
        '
        'lblMapPath
        '
        Me.lblMapPath.AutoSize = True
        Me.lblMapPath.Location = New System.Drawing.Point(3, 81)
        Me.lblMapPath.Name = "lblMapPath"
        Me.lblMapPath.Size = New System.Drawing.Size(53, 13)
        Me.lblMapPath.TabIndex = 37
        Me.lblMapPath.Text = "Map Path"
        '
        'lblPath
        '
        Me.lblPath.AutoSize = True
        Me.lblPath.Location = New System.Drawing.Point(3, 42)
        Me.lblPath.Name = "lblPath"
        Me.lblPath.Size = New System.Drawing.Size(114, 13)
        Me.lblPath.TabIndex = 35
        Me.lblPath.Text = "Warcraft 3 Folder Path"
        '
        'tabPlugins
        '
        Me.tabPlugins.Controls.Add(Me.Label3)
        Me.tabPlugins.Controls.Add(Me.Label2)
        Me.tabPlugins.Controls.Add(Me.txtInitialPlugins)
        Me.tabPlugins.Controls.Add(Me.btnImportPlugin)
        Me.tabPlugins.Controls.Add(Me.gridPlugins)
        Me.tabPlugins.Location = New System.Drawing.Point(4, 22)
        Me.tabPlugins.Name = "tabPlugins"
        Me.tabPlugins.Padding = New System.Windows.Forms.Padding(3)
        Me.tabPlugins.Size = New System.Drawing.Size(473, 321)
        Me.tabPlugins.TabIndex = 4
        Me.tabPlugins.Text = "Plugins"
        Me.tabPlugins.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(3, 3)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(87, 13)
        Me.Label3.TabIndex = 58
        Me.Label3.Text = "Available Plugins"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(3, 278)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(123, 13)
        Me.Label2.TabIndex = 57
        Me.Label2.Text = "Plugins loaded at startup"
        '
        'btnImportPlugin
        '
        Me.btnImportPlugin.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnImportPlugin.Location = New System.Drawing.Point(370, 289)
        Me.btnImportPlugin.Name = "btnImportPlugin"
        Me.btnImportPlugin.Size = New System.Drawing.Size(97, 29)
        Me.btnImportPlugin.TabIndex = 37
        Me.btnImportPlugin.Text = "Import Plugin"
        Me.btnImportPlugin.UseVisualStyleBackColor = True
        '
        'gridPlugins
        '
        Me.gridPlugins.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.gridPlugins.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.gridPlugins.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.colName, Me.colAccess, Me.colSettings})
        Me.gridPlugins.Location = New System.Drawing.Point(0, 19)
        Me.gridPlugins.Name = "gridPlugins"
        Me.gridPlugins.Size = New System.Drawing.Size(473, 256)
        Me.gridPlugins.TabIndex = 33
        '
        'colName
        '
        Me.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.colName.FillWeight = 50.0!
        Me.colName.HeaderText = "Name"
        Me.colName.Name = "colName"
        '
        'colAccess
        '
        Me.colAccess.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.colAccess.HeaderText = "Location"
        Me.colAccess.Name = "colAccess"
        '
        'colSettings
        '
        Me.colSettings.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.colSettings.HeaderText = "Arguments"
        Me.colSettings.Name = "colSettings"
        Me.colSettings.Visible = False
        '
        'tabNewProfile
        '
        Me.tabNewProfile.AutoScroll = True
        Me.tabNewProfile.Controls.Add(Me.btnCreateNewProfile)
        Me.tabNewProfile.Controls.Add(Me.lblNewProfileName)
        Me.tabNewProfile.Controls.Add(Me.txtNewProfileName)
        Me.tabNewProfile.Location = New System.Drawing.Point(4, 22)
        Me.tabNewProfile.Name = "tabNewProfile"
        Me.tabNewProfile.Padding = New System.Windows.Forms.Padding(3)
        Me.tabNewProfile.Size = New System.Drawing.Size(473, 321)
        Me.tabNewProfile.TabIndex = 0
        Me.tabNewProfile.Text = "[ New Profile ... ]"
        Me.tabNewProfile.UseVisualStyleBackColor = True
        '
        'btnCreateNewProfile
        '
        Me.btnCreateNewProfile.Location = New System.Drawing.Point(6, 45)
        Me.btnCreateNewProfile.Name = "btnCreateNewProfile"
        Me.btnCreateNewProfile.Size = New System.Drawing.Size(220, 29)
        Me.btnCreateNewProfile.TabIndex = 50
        Me.btnCreateNewProfile.Text = "Create New Profile"
        Me.btnCreateNewProfile.UseVisualStyleBackColor = True
        '
        'lblNewProfileName
        '
        Me.lblNewProfileName.AutoSize = True
        Me.lblNewProfileName.Location = New System.Drawing.Point(3, 3)
        Me.lblNewProfileName.Name = "lblNewProfileName"
        Me.lblNewProfileName.Size = New System.Drawing.Size(92, 13)
        Me.lblNewProfileName.TabIndex = 49
        Me.lblNewProfileName.Text = "New Profile Name"
        '
        'btnPluginsHelp
        '
        Me.btnPluginsHelp.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnPluginsHelp.Location = New System.Drawing.Point(115, 365)
        Me.btnPluginsHelp.Name = "btnPluginsHelp"
        Me.btnPluginsHelp.Size = New System.Drawing.Size(97, 29)
        Me.btnPluginsHelp.TabIndex = 36
        Me.btnPluginsHelp.Text = "Plugins Help"
        Me.btnPluginsHelp.UseVisualStyleBackColor = True
        '
        'btnCancel
        '
        Me.btnCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnCancel.Location = New System.Drawing.Point(396, 365)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(97, 29)
        Me.btnCancel.TabIndex = 29
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'btnSave
        '
        Me.btnSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSave.Location = New System.Drawing.Point(293, 365)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(97, 29)
        Me.btnSave.TabIndex = 28
        Me.btnSave.Text = "Apply"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        'btnUserHelp
        '
        Me.btnUserHelp.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnUserHelp.Location = New System.Drawing.Point(12, 365)
        Me.btnUserHelp.Name = "btnUserHelp"
        Me.btnUserHelp.Size = New System.Drawing.Size(97, 29)
        Me.btnUserHelp.TabIndex = 35
        Me.btnUserHelp.Text = "Users Help"
        Me.btnUserHelp.UseVisualStyleBackColor = True
        '
        'OpenFileDialog
        '
        Me.OpenFileDialog.Title = "Select Plugin"
        '
        'txtBnlsServer
        '
        Me.txtBnlsServer.Font = New System.Drawing.Font("Courier New", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtBnlsServer.Location = New System.Drawing.Point(6, 253)
        Me.txtBnlsServer.Name = "txtBnlsServer"
        Me.txtBnlsServer.Size = New System.Drawing.Size(220, 20)
        Me.txtBnlsServer.TabIndex = 60
        Me.tipNormal.SetToolTip(Me.txtBnlsServer, "The address:port of a BNLS server which supports warden responses (e.g. example.com:9999)." _
                                                    & vbNewLine & "The server is not given your username, password, or CD Keys." _
                                                    & vbNewLine & "However, if the server accidentally or purposefully returns incorrect responses, bnet will think you are cheating.")
        '
        'lblBnlsServer
        '
        Me.lblBnlsServer.AutoSize = True
        Me.lblBnlsServer.Location = New System.Drawing.Point(3, 237)
        Me.lblBnlsServer.Name = "lblBnlsServer"
        Me.lblBnlsServer.Size = New System.Drawing.Size(165, 13)
        Me.lblBnlsServer.TabIndex = 61
        Me.lblBnlsServer.Text = "BattleNet Login Server (Warden)"
        '
        'FrmSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(505, 406)
        Me.ControlBox = False
        Me.Controls.Add(Me.btnUserHelp)
        Me.Controls.Add(Me.tabsSettings)
        Me.Controls.Add(Me.btnCancel)
        Me.Controls.Add(Me.btnSave)
        Me.Controls.Add(Me.btnPluginsHelp)
        Me.Name = "FrmSettings"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Settings"
        CType(Me.numTickPeriod, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.numLagLimit, System.ComponentModel.ISupportInitialize).EndInit()
        Me.tabsSettings.ResumeLayout(False)
        Me.tabGlobalSettings.ResumeLayout(False)
        Me.tabGlobalSettings.PerformLayout()
        Me.tabPlugins.ResumeLayout(False)
        Me.tabPlugins.PerformLayout()
        CType(Me.gridPlugins, System.ComponentModel.ISupportInitialize).EndInit()
        Me.tabNewProfile.ResumeLayout(False)
        Me.tabNewProfile.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents tipNormal As System.Windows.Forms.ToolTip
    Friend WithEvents tabsSettings As System.Windows.Forms.TabControl
    Friend WithEvents tabNewProfile As System.Windows.Forms.TabPage
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents btnUserHelp As System.Windows.Forms.Button
    Friend WithEvents tabGlobalSettings As System.Windows.Forms.TabPage
    Friend WithEvents lblMapPath As System.Windows.Forms.Label
    Friend WithEvents txtMapPath As System.Windows.Forms.TextBox
    Friend WithEvents txtProgramPath As System.Windows.Forms.TextBox
    Friend WithEvents lblPath As System.Windows.Forms.Label
    Friend WithEvents lblExeInfo As System.Windows.Forms.Label
    Friend WithEvents txtExeInformation As System.Windows.Forms.TextBox
    Friend WithEvents lblOwner As System.Windows.Forms.Label
    Friend WithEvents txtCdKeyOwner As System.Windows.Forms.TextBox
    Friend WithEvents lblWc3Version As System.Windows.Forms.Label
    Friend WithEvents txtExeVersion As System.Windows.Forms.TextBox
    Friend WithEvents lblCommandPrefix As System.Windows.Forms.Label
    Friend WithEvents txtCommandPrefix As System.Windows.Forms.TextBox
    Friend WithEvents numTickPeriod As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblTickPeriod As System.Windows.Forms.Label
    Friend WithEvents numLagLimit As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblLagLimit As System.Windows.Forms.Label
    Friend WithEvents btnCreateNewProfile As System.Windows.Forms.Button
    Friend WithEvents lblNewProfileName As System.Windows.Forms.Label
    Friend WithEvents txtNewProfileName As System.Windows.Forms.TextBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents txtInGameName As System.Windows.Forms.TextBox
    Friend WithEvents tabPlugins As System.Windows.Forms.TabPage
    Friend WithEvents gridPlugins As System.Windows.Forms.DataGridView
    Friend WithEvents btnPluginsHelp As System.Windows.Forms.Button
    Friend WithEvents btnImportPlugin As System.Windows.Forms.Button
    Friend WithEvents OpenFileDialog As System.Windows.Forms.OpenFileDialog
    Friend WithEvents colName As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colAccess As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colSettings As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtInitialPlugins As System.Windows.Forms.TextBox
    Friend WithEvents lblPortPool As System.Windows.Forms.Label
    Friend WithEvents txtPortPool As System.Windows.Forms.TextBox
    Friend WithEvents txtBnlsServer As System.Windows.Forms.TextBox
    Friend WithEvents lblBnlsServer As System.Windows.Forms.Label
End Class
