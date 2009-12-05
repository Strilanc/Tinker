<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ComponentsControl
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
        Me.tabsBot = New System.Windows.Forms.TabControl()
        Me.SuspendLayout()
        '
        'tabsBot
        '
        Me.tabsBot.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.tabsBot.Location = New System.Drawing.Point(0, 0)
        Me.tabsBot.Name = "tabsBot"
        Me.tabsBot.SelectedIndex = 0
        Me.tabsBot.Size = New System.Drawing.Size(1053, 585)
        Me.tabsBot.TabIndex = 0
        '
        'BotControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.tabsBot)
        Me.Name = "BotControl"
        Me.Size = New System.Drawing.Size(1053, 585)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents tabsBot As System.Windows.Forms.TabControl

End Class
