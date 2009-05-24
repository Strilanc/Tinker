<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class BotControl
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
        Me.tabsBot = New System.Windows.Forms.TabControl
        Me.tabMain = New System.Windows.Forms.TabPage
        Me.txtCommand = New System.Windows.Forms.TextBox
        Me.logBot = New HostBot.LoggerControl
        Me.tabsBot.SuspendLayout()
        Me.tabMain.SuspendLayout()
        Me.SuspendLayout()
        '
        'tabsBot
        '
        Me.tabsBot.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.tabsBot.Controls.Add(Me.tabMain)
        Me.tabsBot.Location = New System.Drawing.Point(0, 0)
        Me.tabsBot.Name = "tabsBot"
        Me.tabsBot.SelectedIndex = 0
        Me.tabsBot.Size = New System.Drawing.Size(1053, 585)
        Me.tabsBot.TabIndex = 0
        '
        'tabMain
        '
        Me.tabMain.Controls.Add(Me.txtCommand)
        Me.tabMain.Controls.Add(Me.logBot)
        Me.tabMain.Location = New System.Drawing.Point(4, 22)
        Me.tabMain.Name = "tabMain"
        Me.tabMain.Padding = New System.Windows.Forms.Padding(3)
        Me.tabMain.Size = New System.Drawing.Size(1045, 559)
        Me.tabMain.TabIndex = 0
        Me.tabMain.Text = "Main"
        Me.tabMain.UseVisualStyleBackColor = True
        '
        'txtCommand
        '
        Me.txtCommand.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtCommand.BackColor = System.Drawing.SystemColors.Info
        Me.txtCommand.Location = New System.Drawing.Point(0, 539)
        Me.txtCommand.Name = "txtCommand"
        Me.txtCommand.Size = New System.Drawing.Size(1045, 20)
        Me.txtCommand.TabIndex = 1
        '
        'logBot
        '
        Me.logBot.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.logBot.Location = New System.Drawing.Point(0, 0)
        Me.logBot.Name = "logBot"
        Me.logBot.Size = New System.Drawing.Size(1045, 533)
        Me.logBot.TabIndex = 0
        '
        'BotControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.tabsBot)
        Me.Name = "BotControl"
        Me.Size = New System.Drawing.Size(1053, 585)
        Me.tabsBot.ResumeLayout(False)
        Me.tabMain.ResumeLayout(False)
        Me.tabMain.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents tabsBot As System.Windows.Forms.TabControl
    Friend WithEvents tabMain As System.Windows.Forms.TabPage
    Friend WithEvents txtCommand As System.Windows.Forms.TextBox
    Friend WithEvents logBot As HostBot.LoggerControl

End Class
