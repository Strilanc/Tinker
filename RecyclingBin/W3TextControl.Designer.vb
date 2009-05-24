<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class W3TextControl
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
        Me.txtInput = New System.Windows.Forms.TextBox
        Me.txtMain = New System.Windows.Forms.RichTextBox
        Me.SuspendLayout()
        '
        'txtInput
        '
        Me.txtInput.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtInput.Location = New System.Drawing.Point(0, 345)
        Me.txtInput.Name = "txtInput"
        Me.txtInput.Size = New System.Drawing.Size(390, 20)
        Me.txtInput.TabIndex = 1
        '
        'txtMain
        '
        Me.txtMain.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtMain.BackColor = System.Drawing.SystemColors.Window
        Me.txtMain.Location = New System.Drawing.Point(0, 0)
        Me.txtMain.Name = "txtMain"
        Me.txtMain.ReadOnly = True
        Me.txtMain.Size = New System.Drawing.Size(390, 339)
        Me.txtMain.TabIndex = 3
        Me.txtMain.Text = ""
        '
        'W3TextControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.txtMain)
        Me.Controls.Add(Me.txtInput)
        Me.Name = "W3TextControl"
        Me.Size = New System.Drawing.Size(390, 365)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents txtInput As System.Windows.Forms.TextBox
    Friend WithEvents txtMain As System.Windows.Forms.RichTextBox

End Class
