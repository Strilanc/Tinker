<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class TimeGraphControl
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
        Me.picGraph = New System.Windows.Forms.PictureBox
        Me.lblTitle = New System.Windows.Forms.Label
        CType(Me.picGraph, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'picGraph
        '
        Me.picGraph.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.picGraph.BackColor = System.Drawing.SystemColors.Window
        Me.picGraph.Location = New System.Drawing.Point(1, 20)
        Me.picGraph.Name = "picGraph"
        Me.picGraph.Size = New System.Drawing.Size(262, 243)
        Me.picGraph.TabIndex = 0
        Me.picGraph.TabStop = False
        '
        'lblTitle
        '
        Me.lblTitle.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblTitle.Location = New System.Drawing.Point(0, 0)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(264, 17)
        Me.lblTitle.TabIndex = 1
        Me.lblTitle.Text = "Rate Graph"
        Me.lblTitle.TextAlign = System.Drawing.ContentAlignment.TopCenter
        '
        'TimeGraphControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.lblTitle)
        Me.Controls.Add(Me.picGraph)
        Me.DoubleBuffered = True
        Me.Name = "TimeGraphControl"
        Me.Size = New System.Drawing.Size(264, 264)
        CType(Me.picGraph, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents picGraph As System.Windows.Forms.PictureBox
    Friend WithEvents lblTitle As System.Windows.Forms.Label

End Class
