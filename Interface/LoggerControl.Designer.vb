<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class LoggerControl
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId:="filestream")>
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
        Me.txtLog = New System.Windows.Forms.RichTextBox()
        Me.chkDataEvents = New System.Windows.Forms.CheckBox()
        Me.chkParsedData = New System.Windows.Forms.CheckBox()
        Me.chkRawData = New System.Windows.Forms.CheckBox()
        Me.btnClear = New System.Windows.Forms.Button()
        Me.lblBuffering = New System.Windows.Forms.Label()
        Me.chkSaveFile = New System.Windows.Forms.CheckBox()
        Me.tips = New System.Windows.Forms.ToolTip()
        Me.btnUnbuffer = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'txtLog
        '
        Me.txtLog.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtLog.BackColor = System.Drawing.SystemColors.Window
        Me.txtLog.HideSelection = False
        Me.txtLog.Location = New System.Drawing.Point(0, 0)
        Me.txtLog.Name = "txtLog"
        Me.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical
        Me.txtLog.Size = New System.Drawing.Size(938, 640)
        Me.txtLog.TabIndex = 0
        Me.txtLog.Text = ""
        '
        'chkDataEvents
        '
        Me.chkDataEvents.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.chkDataEvents.AutoSize = True
        Me.chkDataEvents.BackColor = System.Drawing.SystemColors.Window
        Me.chkDataEvents.Location = New System.Drawing.Point(500, 619)
        Me.chkDataEvents.Name = "chkDataEvents"
        Me.chkDataEvents.Size = New System.Drawing.Size(85, 17)
        Me.chkDataEvents.TabIndex = 1
        Me.chkDataEvents.Text = "Data Events"
        Me.chkDataEvents.ThreeState = True
        Me.chkDataEvents.UseVisualStyleBackColor = False
        '
        'chkParsedData
        '
        Me.chkParsedData.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.chkParsedData.AutoSize = True
        Me.chkParsedData.BackColor = System.Drawing.SystemColors.Window
        Me.chkParsedData.Location = New System.Drawing.Point(591, 619)
        Me.chkParsedData.Name = "chkParsedData"
        Me.chkParsedData.Size = New System.Drawing.Size(85, 17)
        Me.chkParsedData.TabIndex = 2
        Me.chkParsedData.Text = "Parsed Data"
        Me.chkParsedData.ThreeState = True
        Me.chkParsedData.UseVisualStyleBackColor = False
        '
        'chkRawData
        '
        Me.chkRawData.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.chkRawData.AutoSize = True
        Me.chkRawData.BackColor = System.Drawing.SystemColors.Window
        Me.chkRawData.Location = New System.Drawing.Point(682, 619)
        Me.chkRawData.Name = "chkRawData"
        Me.chkRawData.Size = New System.Drawing.Size(74, 17)
        Me.chkRawData.TabIndex = 3
        Me.chkRawData.Text = "Raw Data"
        Me.chkRawData.ThreeState = True
        Me.tips.SetToolTip(Me.chkRawData, "Shows raw data such as received packet data." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Half-check to only save to file.")
        Me.chkRawData.UseVisualStyleBackColor = False
        '
        'btnClear
        '
        Me.btnClear.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnClear.Location = New System.Drawing.Point(819, 615)
        Me.btnClear.Name = "btnClear"
        Me.btnClear.Size = New System.Drawing.Size(94, 22)
        Me.btnClear.TabIndex = 4
        Me.btnClear.Text = "Clear"
        Me.btnClear.UseVisualStyleBackColor = True
        '
        'lblBuffering
        '
        Me.lblBuffering.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblBuffering.AutoSize = True
        Me.lblBuffering.BackColor = System.Drawing.Color.White
        Me.lblBuffering.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblBuffering.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblBuffering.Location = New System.Drawing.Point(506, 594)
        Me.lblBuffering.Name = "lblBuffering"
        Me.lblBuffering.Size = New System.Drawing.Size(307, 15)
        Me.lblBuffering.TabIndex = 5
        Me.lblBuffering.Text = "Buffering new messages (cursor not at end-of-text)..."
        Me.lblBuffering.Visible = False
        '
        'chkSaveFile
        '
        Me.chkSaveFile.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.chkSaveFile.AutoSize = True
        Me.chkSaveFile.BackColor = System.Drawing.SystemColors.Window
        Me.chkSaveFile.Location = New System.Drawing.Point(762, 619)
        Me.chkSaveFile.Name = "chkSaveFile"
        Me.chkSaveFile.Size = New System.Drawing.Size(51, 17)
        Me.chkSaveFile.TabIndex = 7
        Me.chkSaveFile.Text = "Save"
        Me.chkSaveFile.UseVisualStyleBackColor = False
        '
        'btnUnbuffer
        '
        Me.btnUnbuffer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnUnbuffer.Location = New System.Drawing.Point(819, 590)
        Me.btnUnbuffer.Name = "btnUnbuffer"
        Me.btnUnbuffer.Size = New System.Drawing.Size(94, 21)
        Me.btnUnbuffer.TabIndex = 8
        Me.btnUnbuffer.Text = "Unbuffer (0)"
        Me.btnUnbuffer.UseVisualStyleBackColor = True
        Me.btnUnbuffer.Visible = False
        '
        'LoggerControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.chkSaveFile)
        Me.Controls.Add(Me.lblBuffering)
        Me.Controls.Add(Me.btnClear)
        Me.Controls.Add(Me.chkRawData)
        Me.Controls.Add(Me.chkParsedData)
        Me.Controls.Add(Me.chkDataEvents)
        Me.Controls.Add(Me.btnUnbuffer)
        Me.Controls.Add(Me.txtLog)
        Me.Name = "LoggerControl"
        Me.Size = New System.Drawing.Size(938, 640)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Private WithEvents txtLog As System.Windows.Forms.RichTextBox
    Private WithEvents chkDataEvents As System.Windows.Forms.CheckBox
    Private WithEvents chkParsedData As System.Windows.Forms.CheckBox
    Private WithEvents chkRawData As System.Windows.Forms.CheckBox
    Private WithEvents btnClear As System.Windows.Forms.Button
    Friend WithEvents lblBuffering As System.Windows.Forms.Label
    Private WithEvents chkSaveFile As System.Windows.Forms.CheckBox
    Friend WithEvents tips As System.Windows.Forms.ToolTip
    Friend WithEvents btnUnbuffer As System.Windows.Forms.Button

End Class
