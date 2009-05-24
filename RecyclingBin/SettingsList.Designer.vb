<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SettingsList
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
        Me.gridSettings = New System.Windows.Forms.DataGridView
        Me.colName = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.colType = New System.Windows.Forms.DataGridViewTextBoxColumn
        Me.colValue = New System.Windows.Forms.DataGridViewTextBoxColumn
        CType(Me.gridSettings, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'DataGridView1
        '
        Me.gridSettings.AllowUserToAddRows = False
        Me.gridSettings.AllowUserToDeleteRows = False
        Me.gridSettings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.gridSettings.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.colName, Me.colType, Me.colValue})
        Me.gridSettings.Location = New System.Drawing.Point(0, 0)
        Me.gridSettings.Name = "DataGridView1"
        Me.gridSettings.Size = New System.Drawing.Size(549, 402)
        Me.gridSettings.TabIndex = 0
        '
        'colName
        '
        Me.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells
        Me.colName.HeaderText = "Name"
        Me.colName.Name = "colName"
        Me.colName.ReadOnly = True
        Me.colName.Width = 60
        '
        'colType
        '
        Me.colType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells
        Me.colType.HeaderText = "Type"
        Me.colType.Name = "colType"
        Me.colType.ReadOnly = True
        Me.colType.Width = 56
        '
        'colValue
        '
        Me.colValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.colValue.HeaderText = "Value"
        Me.colValue.Name = "colValue"
        '
        'SettingsList
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.gridSettings)
        Me.Name = "SettingsList"
        Me.Size = New System.Drawing.Size(549, 402)
        CType(Me.gridSettings, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents gridSettings As System.Windows.Forms.DataGridView
    Friend WithEvents colName As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colType As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colValue As System.Windows.Forms.DataGridViewTextBoxColumn

End Class
