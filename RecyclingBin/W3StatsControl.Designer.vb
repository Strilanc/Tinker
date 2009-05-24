<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class W3StatsControl
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
        Me.TabControl1 = New System.Windows.Forms.TabControl
        Me.tabSummary = New System.Windows.Forms.TabPage
        Me.txtStats = New System.Windows.Forms.TextBox
        Me.tabJoin = New System.Windows.Forms.TabPage
        Me.graphJoin = New HostBot.TimeGraphControl
        Me.tabLeave = New System.Windows.Forms.TabPage
        Me.graphLeave = New HostBot.TimeGraphControl
        Me.TabControl1.SuspendLayout()
        Me.tabSummary.SuspendLayout()
        Me.tabJoin.SuspendLayout()
        Me.tabLeave.SuspendLayout()
        Me.SuspendLayout()
        '
        'TabControl1
        '
        Me.TabControl1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TabControl1.Controls.Add(Me.tabSummary)
        Me.TabControl1.Controls.Add(Me.tabJoin)
        Me.TabControl1.Controls.Add(Me.tabLeave)
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(345, 260)
        Me.TabControl1.TabIndex = 1
        '
        'tabSummary
        '
        Me.tabSummary.Controls.Add(Me.txtStats)
        Me.tabSummary.Location = New System.Drawing.Point(4, 22)
        Me.tabSummary.Name = "tabSummary"
        Me.tabSummary.Padding = New System.Windows.Forms.Padding(3)
        Me.tabSummary.Size = New System.Drawing.Size(337, 234)
        Me.tabSummary.TabIndex = 0
        Me.tabSummary.Text = "Summary"
        Me.tabSummary.UseVisualStyleBackColor = True
        '
        'txtStats
        '
        Me.txtStats.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtStats.BackColor = System.Drawing.SystemColors.Window
        Me.txtStats.Location = New System.Drawing.Point(0, 0)
        Me.txtStats.Multiline = True
        Me.txtStats.Name = "txtStats"
        Me.txtStats.ReadOnly = True
        Me.txtStats.Size = New System.Drawing.Size(337, 234)
        Me.txtStats.TabIndex = 1
        Me.txtStats.Text = "Statistics"
        '
        'tabJoin
        '
        Me.tabJoin.Controls.Add(Me.graphJoin)
        Me.tabJoin.Location = New System.Drawing.Point(4, 22)
        Me.tabJoin.Name = "tabJoin"
        Me.tabJoin.Padding = New System.Windows.Forms.Padding(3)
        Me.tabJoin.Size = New System.Drawing.Size(337, 234)
        Me.tabJoin.TabIndex = 1
        Me.tabJoin.Text = "Join"
        Me.tabJoin.UseVisualStyleBackColor = True
        '
        'graphJoin
        '
        Me.graphJoin.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.graphJoin.Location = New System.Drawing.Point(0, 0)
        Me.graphJoin.Name = "graphJoin"
        Me.graphJoin.Size = New System.Drawing.Size(337, 234)
        Me.graphJoin.TabIndex = 0
        Me.graphJoin.title = "Join Rate Variation"
        '
        'tabLeave
        '
        Me.tabLeave.Controls.Add(Me.graphLeave)
        Me.tabLeave.Location = New System.Drawing.Point(4, 22)
        Me.tabLeave.Name = "tabLeave"
        Me.tabLeave.Padding = New System.Windows.Forms.Padding(3)
        Me.tabLeave.Size = New System.Drawing.Size(337, 234)
        Me.tabLeave.TabIndex = 3
        Me.tabLeave.Text = "Leave"
        Me.tabLeave.UseVisualStyleBackColor = True
        '
        'graphLeave
        '
        Me.graphLeave.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.graphLeave.Location = New System.Drawing.Point(0, 0)
        Me.graphLeave.Name = "graphLeave"
        Me.graphLeave.Size = New System.Drawing.Size(337, 234)
        Me.graphLeave.TabIndex = 1
        Me.graphLeave.title = "Leave Rate Variation"
        '
        'W3StatsControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.TabControl1)
        Me.Name = "W3StatsControl"
        Me.Size = New System.Drawing.Size(345, 260)
        Me.TabControl1.ResumeLayout(False)
        Me.tabSummary.ResumeLayout(False)
        Me.tabSummary.PerformLayout()
        Me.tabJoin.ResumeLayout(False)
        Me.tabLeave.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents tabSummary As System.Windows.Forms.TabPage
    Friend WithEvents txtStats As System.Windows.Forms.TextBox
    Friend WithEvents tabJoin As System.Windows.Forms.TabPage
    Friend WithEvents graphJoin As HostBot.TimeGraphControl
    Friend WithEvents tabLeave As System.Windows.Forms.TabPage
    Friend WithEvents graphLeave As HostBot.TimeGraphControl

End Class
