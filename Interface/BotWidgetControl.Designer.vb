<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class BotWidgetControl
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
        Me.logControl = New HostBot.LoggerControl
        Me.lstState = New System.Windows.Forms.ListBox
        Me.txtCommand = New System.Windows.Forms.TextBox
        Me.SuspendLayout()
        '
        'logControl
        '
        Me.logControl.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.logControl.Location = New System.Drawing.Point(0, 0)
        Me.logControl.Name = "logControl"
        Me.logControl.Size = New System.Drawing.Size(563, 349)
        Me.logControl.TabIndex = 0
        '
        'lstState
        '
        Me.lstState.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstState.FormattingEnabled = True
        Me.lstState.IntegralHeight = False
        Me.lstState.Location = New System.Drawing.Point(569, 0)
        Me.lstState.Name = "lstState"
        Me.lstState.Size = New System.Drawing.Size(122, 349)
        Me.lstState.TabIndex = 1
        '
        'txtCommand
        '
        Me.txtCommand.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtCommand.BackColor = System.Drawing.SystemColors.Info
        Me.txtCommand.Location = New System.Drawing.Point(0, 355)
        Me.txtCommand.Name = "txtCommand"
        Me.txtCommand.Size = New System.Drawing.Size(691, 20)
        Me.txtCommand.TabIndex = 2
        '
        'BotWidgetControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.txtCommand)
        Me.Controls.Add(Me.lstState)
        Me.Controls.Add(Me.logControl)
        Me.Name = "BotWidgetControl"
        Me.Size = New System.Drawing.Size(691, 375)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents logControl As HostBot.LoggerControl
    Friend WithEvents lstState As System.Windows.Forms.ListBox
    Friend WithEvents txtCommand As System.Windows.Forms.TextBox

End Class
