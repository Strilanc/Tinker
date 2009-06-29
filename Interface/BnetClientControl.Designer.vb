<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class BnetClientControl
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
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
        Me.lstState = New System.Windows.Forms.ListBox
        Me.txtCommand = New System.Windows.Forms.TextBox
        Me.txtTalk = New System.Windows.Forms.TextBox
        Me.logClient = New HostBot.LoggerControl
        Me.SuspendLayout()
        '
        'lstUsers
        '
        Me.lstState.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstState.FormattingEnabled = True
        Me.lstState.IntegralHeight = False
        Me.lstState.Location = New System.Drawing.Point(593, 0)
        Me.lstState.Name = "lstUsers"
        Me.lstState.Size = New System.Drawing.Size(147, 389)
        Me.lstState.TabIndex = 2
        '
        'txtCommand
        '
        Me.txtCommand.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtCommand.BackColor = System.Drawing.SystemColors.Info
        Me.txtCommand.Location = New System.Drawing.Point(0, 395)
        Me.txtCommand.Name = "txtCommand"
        Me.txtCommand.Size = New System.Drawing.Size(740, 20)
        Me.txtCommand.TabIndex = 3
        '
        'txtTalk
        '
        Me.txtTalk.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtTalk.Location = New System.Drawing.Point(0, 369)
        Me.txtTalk.Name = "txtTalk"
        Me.txtTalk.Size = New System.Drawing.Size(587, 20)
        Me.txtTalk.TabIndex = 1
        '
        'logClient
        '
        Me.logClient.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.logClient.Location = New System.Drawing.Point(0, 0)
        Me.logClient.Name = "logClient"
        Me.logClient.Size = New System.Drawing.Size(587, 363)
        Me.logClient.TabIndex = 0
        '
        'BnetClientControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.txtTalk)
        Me.Controls.Add(Me.txtCommand)
        Me.Controls.Add(Me.logClient)
        Me.Controls.Add(Me.lstState)
        Me.Name = "BnetClientControl"
        Me.Size = New System.Drawing.Size(740, 415)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents lstState As System.Windows.Forms.ListBox
    Friend WithEvents txtCommand As System.Windows.Forms.TextBox
    Friend WithEvents logClient As HostBot.LoggerControl
    Friend WithEvents txtTalk As System.Windows.Forms.TextBox

End Class
