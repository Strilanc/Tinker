<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class LanAdvertiserControl
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
        Me.lstState = New System.Windows.Forms.ListBox()
        Me.logClient = New Tinker.LoggerControl()
        Me.comLanAdvertiser = New Tinker.CommandControl()
        Me.SuspendLayout()
        '
        'lstState
        '
        Me.lstState.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstState.FormattingEnabled = True
        Me.lstState.IntegralHeight = False
        Me.lstState.Location = New System.Drawing.Point(534, 0)
        Me.lstState.Name = "lstState"
        Me.lstState.Size = New System.Drawing.Size(206, 389)
        Me.lstState.TabIndex = 2
        '
        'logClient
        '
        Me.logClient.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.logClient.Location = New System.Drawing.Point(0, 0)
        Me.logClient.Name = "logClient"
        Me.logClient.Size = New System.Drawing.Size(528, 389)
        Me.logClient.TabIndex = 0
        '
        'comClient
        '
        Me.comLanAdvertiser.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.comLanAdvertiser.Location = New System.Drawing.Point(0, 395)
        Me.comLanAdvertiser.Name = "comClient"
        Me.comLanAdvertiser.Size = New System.Drawing.Size(740, 20)
        Me.comLanAdvertiser.TabIndex = 4
        '
        'LanAdvertiserControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.comLanAdvertiser)
        Me.Controls.Add(Me.logClient)
        Me.Controls.Add(Me.lstState)
        Me.Name = "LanAdvertiserControl"
        Me.Size = New System.Drawing.Size(740, 415)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents lstState As System.Windows.Forms.ListBox
    Friend WithEvents logClient As Tinker.LoggerControl
    Friend WithEvents comLanAdvertiser As Tinker.CommandControl

End Class
