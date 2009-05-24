<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmClient
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
        Me.components = New System.ComponentModel.Container
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FrmClient))
        Me.btnSettings = New System.Windows.Forms.Button
        Me.trayIcon = New System.Windows.Forms.NotifyIcon(Me.components)
        Me.mnuTray = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.mnuShowHide = New System.Windows.Forms.ToolStripMenuItem
        Me.sep1 = New System.Windows.Forms.ToolStripSeparator
        Me.mnuClose = New System.Windows.Forms.ToolStripMenuItem
        Me.botcMain = New HostBot.BotControl
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
        Me.trayIcon.Visible = True
        '
        'mnuTray
        '
        Me.mnuTray.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuShowHide, Me.sep1, Me.mnuClose})
        Me.mnuTray.Name = "mnuTray"
        Me.mnuTray.Size = New System.Drawing.Size(112, 54)
        '
        'mnuShowHide
        '
        Me.mnuShowHide.Checked = True
        Me.mnuShowHide.CheckState = System.Windows.Forms.CheckState.Checked
        Me.mnuShowHide.Name = "mnuShowHide"
        Me.mnuShowHide.Size = New System.Drawing.Size(111, 22)
        Me.mnuShowHide.Text = "Show"
        '
        'sep1
        '
        Me.sep1.Name = "sep1"
        Me.sep1.Size = New System.Drawing.Size(108, 6)
        '
        'mnuClose
        '
        Me.mnuClose.Name = "mnuClose"
        Me.mnuClose.Size = New System.Drawing.Size(111, 22)
        Me.mnuClose.Text = "Close"
        '
        'botcMain
        '
        Me.botcMain.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.botcMain.Location = New System.Drawing.Point(12, 12)
        Me.botcMain.Name = "botcMain"
        Me.botcMain.Size = New System.Drawing.Size(981, 388)
        Me.botcMain.TabIndex = 11
        '
        'FrmClient
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1005, 447)
        Me.Controls.Add(Me.botcMain)
        Me.Controls.Add(Me.btnSettings)
        Me.DoubleBuffered = True
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "FrmClient"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "{ProgramName}"
        Me.mnuTray.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents btnSettings As System.Windows.Forms.Button
    Friend WithEvents botcMain As HostBot.BotControl
    Friend WithEvents trayIcon As System.Windows.Forms.NotifyIcon
    Friend WithEvents mnuTray As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents mnuShowHide As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents sep1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuClose As System.Windows.Forms.ToolStripMenuItem
End Class
