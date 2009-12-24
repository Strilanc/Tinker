Namespace WC3
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class GameLoggerControl
        Inherits Tinker.LoggerControl

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
            Me.chkActions = New System.Windows.Forms.CheckBox()
            Me.SuspendLayout()
            '
            'CheckBox1
            '
            Me.chkActions.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.chkActions.AutoSize = True
            Me.chkActions.BackColor = System.Drawing.SystemColors.Window
            Me.chkActions.Location = New System.Drawing.Point(433, 619)
            Me.chkActions.Name = "CheckBox1"
            Me.chkActions.Size = New System.Drawing.Size(61, 17)
            Me.chkActions.TabIndex = 8
            Me.chkActions.Text = "Actions"
            Me.chkActions.ThreeState = True
            Me.chkActions.UseVisualStyleBackColor = False
            Me.tips.SetToolTip(Me.chkActions, "Shows game actions such as unit orders." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Half-check to only save to file.")
            '
            'GameLoggerControl
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.Controls.Add(Me.chkActions)
            Me.Name = "GameLoggerControl"
            Me.Controls.SetChildIndex(Me.chkActions, 0)
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Private WithEvents chkActions As System.Windows.Forms.CheckBox

    End Class
End Namespace
