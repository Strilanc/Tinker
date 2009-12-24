Namespace Components
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class GenericBotComponentControl
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
            Me.logControl = New Tinker.LoggerControl()
            Me.comWidget = New Tinker.CommandControl()
            Me.SuspendLayout()
            '
            'logControl
            '
            Me.logControl.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.logControl.Location = New System.Drawing.Point(0, 0)
            Me.logControl.Name = "logControl"
            Me.logControl.Size = New System.Drawing.Size(691, 349)
            Me.logControl.TabIndex = 0
            '
            'comWidget
            '
            Me.comWidget.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.comWidget.Location = New System.Drawing.Point(0, 355)
            Me.comWidget.Name = "comWidget"
            Me.comWidget.Size = New System.Drawing.Size(691, 20)
            Me.comWidget.TabIndex = 6
            '
            'GenericBotComponentControl
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.comWidget)
            Me.Controls.Add(Me.logControl)
            Me.Name = "GenericBotComponentControl"
            Me.Size = New System.Drawing.Size(691, 375)
            Me.ResumeLayout(False)

        End Sub
        Friend WithEvents logControl As Tinker.LoggerControl
        Friend WithEvents comWidget As Tinker.CommandControl

    End Class
End Namespace
