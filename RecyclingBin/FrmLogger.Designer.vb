<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FrmLogger
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
        Me.logMain = New HostBot.LoggerControl
        Me.SuspendLayout()
        '
        'logMain
        '
        Me.logMain.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.logMain.Location = New System.Drawing.Point(12, 12)
        Me.logMain.Name = "logMain"
        Me.logMain.Size = New System.Drawing.Size(523, 339)
        Me.logMain.TabIndex = 0
        '
        'FrmLogger
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(547, 363)
        Me.Controls.Add(Me.logMain)
        Me.Name = "FrmLogger"
        Me.Text = "Logger"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents logMain As HostBot.LoggerControl
End Class
