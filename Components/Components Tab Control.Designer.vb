Namespace Components
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class TabControl
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
            Me.tabsComponents = New System.Windows.Forms.TabControl()
            Me.SuspendLayout()
            '
            'tabsComponents
            '
            Me.tabsComponents.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                        Or System.Windows.Forms.AnchorStyles.Left) _
                        Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
            Me.tabsComponents.Location = New System.Drawing.Point(0, 0)
            Me.tabsComponents.Name = "tabsComponents"
            Me.tabsComponents.SelectedIndex = 0
            Me.tabsComponents.Size = New System.Drawing.Size(1053, 585)
            Me.tabsComponents.TabIndex = 0
            '
            'ComponentsControl
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.tabsComponents)
            Me.Name = "ComponentsControl"
            Me.Size = New System.Drawing.Size(1053, 585)
            Me.ResumeLayout(False)

        End Sub
        Friend WithEvents tabsComponents As System.Windows.Forms.TabControl

    End Class
End Namespace
