<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class W3SlotControl
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
        Me.components = New System.ComponentModel.Container
        Me.txtSlot = New System.Windows.Forms.TextBox
        Me.mnuSlotCommands = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.mnuOpen = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuClose = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuComputerEasy = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuComputerNormal = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuComputerInsane = New System.Windows.Forms.ToolStripMenuItem
        Me.sep1 = New System.Windows.Forms.ToolStripSeparator
        Me.mnuCboSwap = New System.Windows.Forms.ToolStripComboBox
        Me.mnuSwap = New System.Windows.Forms.ToolStripMenuItem
        Me.sep2 = New System.Windows.Forms.ToolStripSeparator
        Me.mnuTxtReserve = New System.Windows.Forms.ToolStripTextBox
        Me.mnuReserve = New System.Windows.Forms.ToolStripMenuItem
        Me.sep3 = New System.Windows.Forms.ToolStripSeparator
        Me.mnuCboTeam = New System.Windows.Forms.ToolStripComboBox
        Me.mnuCboRace = New System.Windows.Forms.ToolStripComboBox
        Me.mnuCboColor = New System.Windows.Forms.ToolStripComboBox
        Me.mnuCboHandicap = New System.Windows.Forms.ToolStripComboBox
        Me.sep4 = New System.Windows.Forms.ToolStripSeparator
        Me.mnuBoot = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuChkLock = New System.Windows.Forms.ToolStripMenuItem
        Me.sep5 = New System.Windows.Forms.ToolStripSeparator
        Me.mnuShowLog = New System.Windows.Forms.ToolStripMenuItem
        Me.mnuSlotCommands.SuspendLayout()
        Me.SuspendLayout()
        '
        'txtSlot
        '
        Me.txtSlot.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtSlot.BackColor = System.Drawing.SystemColors.Window
        Me.txtSlot.ContextMenuStrip = Me.mnuSlotCommands
        Me.txtSlot.Location = New System.Drawing.Point(0, 0)
        Me.txtSlot.Name = "txtSlot"
        Me.txtSlot.ReadOnly = True
        Me.txtSlot.Size = New System.Drawing.Size(681, 20)
        Me.txtSlot.TabIndex = 0
        Me.txtSlot.Text = "W3Slot"
        '
        'mnuSlotCommands
        '
        Me.mnuSlotCommands.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuOpen, Me.mnuClose, Me.mnuComputerEasy, Me.mnuComputerNormal, Me.mnuComputerInsane, Me.sep1, Me.mnuCboSwap, Me.mnuSwap, Me.sep2, Me.mnuTxtReserve, Me.mnuReserve, Me.sep3, Me.mnuCboTeam, Me.mnuCboRace, Me.mnuCboColor, Me.mnuCboHandicap, Me.sep4, Me.mnuBoot, Me.mnuChkLock, Me.sep5, Me.mnuShowLog})
        Me.mnuSlotCommands.Name = "mnuSlotCommands"
        Me.mnuSlotCommands.Size = New System.Drawing.Size(261, 402)
        '
        'mnuOpen
        '
        Me.mnuOpen.Name = "mnuOpen"
        Me.mnuOpen.ShortcutKeys = CType((System.Windows.Forms.Keys.Alt Or System.Windows.Forms.Keys.O), System.Windows.Forms.Keys)
        Me.mnuOpen.Size = New System.Drawing.Size(260, 22)
        Me.mnuOpen.Text = "Open"
        '
        'mnuClose
        '
        Me.mnuClose.Name = "mnuClose"
        Me.mnuClose.ShortcutKeys = CType((System.Windows.Forms.Keys.Alt Or System.Windows.Forms.Keys.C), System.Windows.Forms.Keys)
        Me.mnuClose.Size = New System.Drawing.Size(260, 22)
        Me.mnuClose.Text = "Close"
        '
        'mnuComputerEasy
        '
        Me.mnuComputerEasy.Name = "mnuComputerEasy"
        Me.mnuComputerEasy.Size = New System.Drawing.Size(260, 22)
        Me.mnuComputerEasy.Text = "Computer (Easy)"
        '
        'mnuComputerNormal
        '
        Me.mnuComputerNormal.Name = "mnuComputerNormal"
        Me.mnuComputerNormal.Size = New System.Drawing.Size(260, 22)
        Me.mnuComputerNormal.Text = "Computer (Normal)"
        '
        'mnuComputerInsane
        '
        Me.mnuComputerInsane.Name = "mnuComputerInsane"
        Me.mnuComputerInsane.Size = New System.Drawing.Size(260, 22)
        Me.mnuComputerInsane.Text = "Computer (Insane)"
        '
        'sep1
        '
        Me.sep1.Name = "sep1"
        Me.sep1.Size = New System.Drawing.Size(257, 6)
        '
        'mnuCboSwap
        '
        Me.mnuCboSwap.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.mnuCboSwap.MaxDropDownItems = 15
        Me.mnuCboSwap.Name = "mnuCboSwap"
        Me.mnuCboSwap.Size = New System.Drawing.Size(200, 21)
        '
        'mnuSwap
        '
        Me.mnuSwap.Name = "mnuSwap"
        Me.mnuSwap.Size = New System.Drawing.Size(260, 22)
        Me.mnuSwap.Text = "Swap with Selected Slot"
        '
        'sep2
        '
        Me.sep2.Name = "sep2"
        Me.sep2.Size = New System.Drawing.Size(257, 6)
        '
        'mnuTxtReserve
        '
        Me.mnuTxtReserve.Name = "mnuTxtReserve"
        Me.mnuTxtReserve.Size = New System.Drawing.Size(200, 21)
        '
        'mnuReserve
        '
        Me.mnuReserve.Name = "mnuReserve"
        Me.mnuReserve.Size = New System.Drawing.Size(260, 22)
        Me.mnuReserve.Text = "Reserve for Player"
        '
        'sep3
        '
        Me.sep3.Name = "sep3"
        Me.sep3.Size = New System.Drawing.Size(257, 6)
        '
        'mnuCboTeam
        '
        Me.mnuCboTeam.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.mnuCboTeam.MaxDropDownItems = 15
        Me.mnuCboTeam.Name = "mnuCboTeam"
        Me.mnuCboTeam.Size = New System.Drawing.Size(121, 21)
        '
        'mnuCboRace
        '
        Me.mnuCboRace.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.mnuCboRace.MaxDropDownItems = 15
        Me.mnuCboRace.Name = "mnuCboRace"
        Me.mnuCboRace.Size = New System.Drawing.Size(121, 21)
        '
        'mnuCboColor
        '
        Me.mnuCboColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.mnuCboColor.MaxDropDownItems = 15
        Me.mnuCboColor.Name = "mnuCboColor"
        Me.mnuCboColor.Size = New System.Drawing.Size(121, 21)
        '
        'mnuCboHandicap
        '
        Me.mnuCboHandicap.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.mnuCboHandicap.MaxDropDownItems = 15
        Me.mnuCboHandicap.Name = "mnuCboHandicap"
        Me.mnuCboHandicap.Size = New System.Drawing.Size(121, 21)
        '
        'sep4
        '
        Me.sep4.Name = "sep4"
        Me.sep4.Size = New System.Drawing.Size(257, 6)
        '
        'mnuBoot
        '
        Me.mnuBoot.Enabled = False
        Me.mnuBoot.Name = "mnuBoot"
        Me.mnuBoot.ShortcutKeys = CType((System.Windows.Forms.Keys.Alt Or System.Windows.Forms.Keys.B), System.Windows.Forms.Keys)
        Me.mnuBoot.Size = New System.Drawing.Size(260, 22)
        Me.mnuBoot.Text = "Boot Player"
        '
        'mnuChkLock
        '
        Me.mnuChkLock.CheckOnClick = True
        Me.mnuChkLock.Name = "mnuChkLock"
        Me.mnuChkLock.Size = New System.Drawing.Size(260, 22)
        Me.mnuChkLock.Text = "Lock"
        Me.mnuChkLock.ToolTipText = "Prevents players from modifying or leaving this slot."
        '
        'sep5
        '
        Me.sep5.Name = "sep5"
        Me.sep5.Size = New System.Drawing.Size(257, 6)
        '
        'mnuShowLog
        '
        Me.mnuShowLog.Name = "mnuShowLog"
        Me.mnuShowLog.Size = New System.Drawing.Size(260, 22)
        Me.mnuShowLog.Text = "Show Log"
        '
        'W3SlotControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.txtSlot)
        Me.MaximumSize = New System.Drawing.Size(9999999, 20)
        Me.MinimumSize = New System.Drawing.Size(100, 20)
        Me.Name = "W3SlotControl"
        Me.Size = New System.Drawing.Size(681, 20)
        Me.mnuSlotCommands.ResumeLayout(False)
        Me.mnuSlotCommands.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents txtSlot As System.Windows.Forms.TextBox
    Friend WithEvents mnuSlotCommands As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents mnuOpen As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuClose As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuComputerEasy As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuComputerNormal As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuComputerInsane As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents sep1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuCboSwap As System.Windows.Forms.ToolStripComboBox
    Friend WithEvents sep2 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuTxtReserve As System.Windows.Forms.ToolStripTextBox
    Friend WithEvents mnuReserve As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents sep3 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuCboTeam As System.Windows.Forms.ToolStripComboBox
    Friend WithEvents mnuCboRace As System.Windows.Forms.ToolStripComboBox
    Friend WithEvents mnuCboColor As System.Windows.Forms.ToolStripComboBox
    Friend WithEvents mnuCboHandicap As System.Windows.Forms.ToolStripComboBox
    Friend WithEvents sep4 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuChkLock As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuSwap As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuBoot As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents sep5 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuShowLog As System.Windows.Forms.ToolStripMenuItem

End Class
