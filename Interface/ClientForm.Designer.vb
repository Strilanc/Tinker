<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ClientForm
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ClientForm))
        Me.btnSettings = New System.Windows.Forms.Button()
        Me.trayIcon = New System.Windows.Forms.NotifyIcon()
        Me.mnuTray = New System.Windows.Forms.ContextMenuStrip()
        Me.mnuRestore = New System.Windows.Forms.ToolStripMenuItem()
        Me.sep1 = New System.Windows.Forms.ToolStripSeparator()
        Me.mnuClose = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnMinimizeToTray = New System.Windows.Forms.Button()
        Me.panelBotControl = New System.Windows.Forms.Panel()
        Me.btnShowExceptionLog = New System.Windows.Forms.Button()
        Me.tip = New System.Windows.Forms.ToolTip()
        Me.mnuTray.SuspendLayout()
        Me.SuspendLayout()
        '
        'btnSettings
        '
        Me.btnSettings.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSettings.Location = New System.Drawing.Point(847, 406)
        Me.btnSettings.Name = "btnSettings"
        Me.btnSettings.Size = New System.Drawing.Size(146, 29)
        Me.btnSettings.TabIndex = 7
        Me.btnSettings.Text = "Settings"
        Me.btnSettings.UseVisualStyleBackColor = True
        '
        'trayIcon
        '
        Me.trayIcon.ContextMenuStrip = Me.mnuTray
        Me.trayIcon.Icon = CType(resources.GetObject("trayIcon.Icon"), System.Drawing.Icon)
        Me.trayIcon.Text = "{ProgramName}"
        '
        'mnuTray
        '
        Me.mnuTray.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuRestore, Me.sep1, Me.mnuClose})
        Me.mnuTray.Name = "mnuTray"
        Me.mnuTray.Size = New System.Drawing.Size(114, 54)
        '
        'mnuRestore
        '
        Me.mnuRestore.Name = "mnuRestore"
        Me.mnuRestore.Size = New System.Drawing.Size(113, 22)
        Me.mnuRestore.Text = "Restore"
        '
        'sep1
        '
        Me.sep1.Name = "sep1"
        Me.sep1.Size = New System.Drawing.Size(110, 6)
        '
        'mnuClose
        '
        Me.mnuClose.Name = "mnuClose"
        Me.mnuClose.Size = New System.Drawing.Size(113, 22)
        Me.mnuClose.Text = "Close"
        '
        'btnMinimizeToTray
        '
        Me.btnMinimizeToTray.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnMinimizeToTray.Location = New System.Drawing.Point(695, 406)
        Me.btnMinimizeToTray.Name = "btnMinimizeToTray"
        Me.btnMinimizeToTray.Size = New System.Drawing.Size(146, 29)
        Me.btnMinimizeToTray.TabIndex = 12
        Me.btnMinimizeToTray.Text = "Minimize to Tray"
        Me.btnMinimizeToTray.UseVisualStyleBackColor = True
        '
        'panelBotControl
        '
        Me.panelBotControl.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.panelBotControl.Location = New System.Drawing.Point(0, 0)
        Me.panelBotControl.Name = "panelBotControl"
        Me.panelBotControl.Size = New System.Drawing.Size(1005, 400)
        Me.panelBotControl.TabIndex = 13
        '
        'btnShowExceptionLog
        '
        Me.btnShowExceptionLog.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnShowExceptionLog.Location = New System.Drawing.Point(12, 406)
        Me.btnShowExceptionLog.Name = "btnShowExceptionLog"
        Me.btnShowExceptionLog.Size = New System.Drawing.Size(146, 29)
        Me.btnShowExceptionLog.TabIndex = 14
        Me.btnShowExceptionLog.Text = "Exception Log (0)"
        Me.btnShowExceptionLog.UseVisualStyleBackColor = True
        '
        'ClientForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoScroll = True
        Me.ClientSize = New System.Drawing.Size(1005, 447)
        Me.Controls.Add(Me.btnShowExceptionLog)
        Me.Controls.Add(Me.btnMinimizeToTray)
        Me.Controls.Add(Me.panelBotControl)
        Me.Controls.Add(Me.btnSettings)
        Me.DoubleBuffered = True
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(500, 250)
        Me.Name = "ClientForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "{ProgramName}"
        Me.mnuTray.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents btnSettings As System.Windows.Forms.Button
    Friend WithEvents trayIcon As System.Windows.Forms.NotifyIcon
    Friend WithEvents mnuTray As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents mnuRestore As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents sep1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuClose As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents btnMinimizeToTray As System.Windows.Forms.Button
    Friend WithEvents panelBotControl As System.Windows.Forms.Panel
    Friend WithEvents btnShowExceptionLog As System.Windows.Forms.Button
    Friend WithEvents tip As System.Windows.Forms.ToolTip
End Class
