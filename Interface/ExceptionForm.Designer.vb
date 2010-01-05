<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ExceptionForm
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ExceptionForm))
        Me.trayIcon = New System.Windows.Forms.NotifyIcon()
        Me.mnuTray = New System.Windows.Forms.ContextMenuStrip()
        Me.mnuRestore = New System.Windows.Forms.ToolStripMenuItem()
        Me.sep1 = New System.Windows.Forms.ToolStripSeparator()
        Me.mnuClose = New System.Windows.Forms.ToolStripMenuItem()
        Me.txtExceptions = New System.Windows.Forms.TextBox()
        Me.btnUpdate = New System.Windows.Forms.Button()
        Me.lblBuffering = New System.Windows.Forms.Label()
        Me.mnuTray.SuspendLayout()
        Me.SuspendLayout()
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
        'txtExceptions
        '
        Me.txtExceptions.Dock = System.Windows.Forms.DockStyle.Fill
        Me.txtExceptions.Location = New System.Drawing.Point(0, 0)
        Me.txtExceptions.Multiline = True
        Me.txtExceptions.Name = "txtExceptions"
        Me.txtExceptions.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.txtExceptions.Size = New System.Drawing.Size(484, 250)
        Me.txtExceptions.TabIndex = 1
        Me.txtExceptions.WordWrap = False
        '
        'btnUpdate
        '
        Me.btnUpdate.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnUpdate.Location = New System.Drawing.Point(330, 204)
        Me.btnUpdate.Name = "btnUpdate"
        Me.btnUpdate.Size = New System.Drawing.Size(128, 21)
        Me.btnUpdate.TabIndex = 2
        Me.btnUpdate.Text = "Update"
        Me.btnUpdate.UseVisualStyleBackColor = True
        '
        'lblBuffering
        '
        Me.lblBuffering.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblBuffering.AutoSize = True
        Me.lblBuffering.BackColor = System.Drawing.Color.White
        Me.lblBuffering.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblBuffering.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblBuffering.Location = New System.Drawing.Point(165, 208)
        Me.lblBuffering.Name = "lblBuffering"
        Me.lblBuffering.Size = New System.Drawing.Size(159, 15)
        Me.lblBuffering.TabIndex = 6
        Me.lblBuffering.Text = "More Exceptions Caught..."
        Me.lblBuffering.Visible = False
        '
        'ExceptionForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoScroll = True
        Me.ClientSize = New System.Drawing.Size(484, 250)
        Me.Controls.Add(Me.lblBuffering)
        Me.Controls.Add(Me.btnUpdate)
        Me.Controls.Add(Me.txtExceptions)
        Me.DoubleBuffered = True
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MinimumSize = New System.Drawing.Size(500, 250)
        Me.Name = "ExceptionForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Exception Log"
        Me.mnuTray.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents trayIcon As System.Windows.Forms.NotifyIcon
    Friend WithEvents mnuTray As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents mnuRestore As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents sep1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuClose As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents txtExceptions As System.Windows.Forms.TextBox
    Friend WithEvents btnUpdate As System.Windows.Forms.Button
    Friend WithEvents lblBuffering As System.Windows.Forms.Label
End Class
