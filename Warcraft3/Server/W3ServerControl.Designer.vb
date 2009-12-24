Namespace WC3
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class W3ServerControl
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
            Me.tabsServer = New System.Windows.Forms.TabControl()
            Me.tabServer = New System.Windows.Forms.TabPage()
            Me.txtInfo = New System.Windows.Forms.TextBox()
            Me.logServer = New Tinker.LoggerControl()
            Me.comServer = New Tinker.CommandControl()
            Me.tabsServer.SuspendLayout()
            Me.tabServer.SuspendLayout()
            Me.SuspendLayout()
            '
            'tabsServer
            '
            Me.tabsServer.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.tabsServer.Controls.Add(Me.tabServer)
            Me.tabsServer.Location = New System.Drawing.Point(0, 0)
            Me.tabsServer.Name = "tabsServer"
            Me.tabsServer.SelectedIndex = 0
            Me.tabsServer.Size = New System.Drawing.Size(869, 533)
            Me.tabsServer.TabIndex = 0
            '
            'tabServer
            '
            Me.tabServer.Controls.Add(Me.comServer)
            Me.tabServer.Controls.Add(Me.txtInfo)
            Me.tabServer.Controls.Add(Me.logServer)
            Me.tabServer.Location = New System.Drawing.Point(4, 22)
            Me.tabServer.Name = "tabServer"
            Me.tabServer.Padding = New System.Windows.Forms.Padding(3)
            Me.tabServer.Size = New System.Drawing.Size(861, 507)
            Me.tabServer.TabIndex = 0
            Me.tabServer.Text = "Server"
            Me.tabServer.UseVisualStyleBackColor = True
            '
            'txtInfo
            '
            Me.txtInfo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.txtInfo.Location = New System.Drawing.Point(657, 0)
            Me.txtInfo.Multiline = True
            Me.txtInfo.Name = "txtInfo"
            Me.txtInfo.ReadOnly = True
            Me.txtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
            Me.txtInfo.Size = New System.Drawing.Size(204, 481)
            Me.txtInfo.TabIndex = 3
            '
            'logServer
            '
            Me.logServer.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.logServer.Location = New System.Drawing.Point(0, 0)
            Me.logServer.Name = "logServer"
            Me.logServer.Size = New System.Drawing.Size(651, 481)
            Me.logServer.TabIndex = 1
            '
            'comServer
            '
            Me.comServer.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.comServer.Location = New System.Drawing.Point(0, 487)
            Me.comServer.Name = "comServer"
            Me.comServer.Size = New System.Drawing.Size(861, 20)
            Me.comServer.TabIndex = 6
            '
            'W3ServerControl
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.tabsServer)
            Me.Name = "W3ServerControl"
            Me.Size = New System.Drawing.Size(869, 533)
            Me.tabsServer.ResumeLayout(False)
            Me.tabServer.ResumeLayout(False)
            Me.tabServer.PerformLayout()
            Me.ResumeLayout(False)

        End Sub
        Friend WithEvents tabsServer As System.Windows.Forms.TabControl
        Friend WithEvents tabServer As System.Windows.Forms.TabPage
        Friend WithEvents logServer As Tinker.LoggerControl
        Friend WithEvents txtInfo As System.Windows.Forms.TextBox
        Friend WithEvents comServer As Tinker.CommandControl

    End Class
End Namespace
