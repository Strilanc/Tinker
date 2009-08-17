<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class W3GameControl
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
        Me.txtCommand = New System.Windows.Forms.TextBox
        Me.lstSlots = New System.Windows.Forms.ListBox
        Me.logInstance = New HostBot.LoggerControl
        Me.SuspendLayout()
        '
        'txtInput
        '
        Me.txtInput.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtInput.Location = New System.Drawing.Point(0, 487)
        Me.txtInput.Name = "txtInput"
        Me.txtInput.Size = New System.Drawing.Size(783, 20)
        Me.txtInput.TabIndex = 3
        '
        'txtCommand
        '
        Me.txtCommand.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtCommand.BackColor = System.Drawing.SystemColors.Info
        Me.txtCommand.Location = New System.Drawing.Point(0, 513)
        Me.txtCommand.Name = "txtCommand"
        Me.txtCommand.Size = New System.Drawing.Size(783, 20)
        Me.txtCommand.TabIndex = 4
        '
        'lstSlots
        '
        Me.lstSlots.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lstSlots.Font = New System.Drawing.Font("Courier New", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lstSlots.FormattingEnabled = True
        Me.lstSlots.ItemHeight = 14
        Me.lstSlots.Items.AddRange(New Object() {"-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-", "-"})
        Me.lstSlots.Location = New System.Drawing.Point(0, 0)
        Me.lstSlots.Name = "lstSlots"
        Me.lstSlots.Size = New System.Drawing.Size(783, 172)
        Me.lstSlots.TabIndex = 0
        '
        'logInstance
        '
        Me.logInstance.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.logInstance.Location = New System.Drawing.Point(0, 178)
        Me.logInstance.Name = "logInstance"
        Me.logInstance.Size = New System.Drawing.Size(783, 303)
        Me.logInstance.TabIndex = 2
        '
        'W3GameControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.Controls.Add(Me.lstSlots)
        Me.Controls.Add(Me.txtCommand)
        Me.Controls.Add(Me.txtInput)
        Me.Controls.Add(Me.logInstance)
        Me.Name = "W3GameControl"
        Me.Size = New System.Drawing.Size(783, 533)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents txtInput As System.Windows.Forms.TextBox
    Friend WithEvents logInstance As HostBot.LoggerControl
    Friend WithEvents txtCommand As System.Windows.Forms.TextBox
    Friend WithEvents lstSlots As System.Windows.Forms.ListBox
End Class
