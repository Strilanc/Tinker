'''<summary>The UI for a player slot in a w3game</summary>
Public Class W3SlotControl
    '#Region "Members"
    '    Private slotIndex As Integer
    '    Private WithEvents game As W3GameInstance
    '    Private holdEvents As Boolean = False
    '    Private ReadOnly uiRef As New InvokedCallQueue(Me, Me.gettype.name + " uiRef")
    '#End Region

    '#Region "New"
    '    Public Sub UI_REF_hook(ByVal game As W3GameInstance, ByVal slotClone As W3Slot)
    '        Me.slotIndex = slotClone.index
    '        Me.game = game

    '        With mnuCboSwap.Items
    '            .Clear()
    '            For i As Integer = 0 To game.map.slots.Count - 1
    '                .Add("Slot Name Placeholder")
    '            Next i
    '            mnuCboSwap.SelectedIndex = 0
    '        End With

    '        With mnuCboTeam
    '            .Enabled = False
    '            .Items.Clear()
    '            If game.map.isMelee Then
    '                .Enabled = True
    '                For i As Integer = 1 To game.map.slots.Count
    '                    .Items.Add("Team " + i.ToString())
    '                Next i
    '            Else
    '                .Items.Add("Team " + (slotClone.team + 1).ToString())
    '            End If
    '        End With

    '        With mnuCboRace.Items
    '            .Clear()
    '            .Add(W3Slot.RaceFlags.Random)
    '            .Add(W3Slot.RaceFlags.Orc)
    '            .Add(W3Slot.RaceFlags.Human)
    '            .Add(W3Slot.RaceFlags.Undead)
    '            .Add(W3Slot.RaceFlags.NightElf)
    '        End With

    '        With mnuCboHandicap.Items
    '            .Clear()
    '            .Add(CByte(100))
    '            .Add(CByte(90))
    '            .Add(CByte(80))
    '            .Add(CByte(70))
    '            .Add(CByte(60))
    '            .Add(CByte(50))
    '            mnuCboHandicap.SelectedIndex = 0
    '        End With

    '        With mnuCboColor.Items
    '            .Clear()
    '            For c As W3Slot.PlayerColor = W3Slot.PlayerColor.Red To W3Slot.PlayerColor.Brown
    '                .Add(c)
    '            Next c
    '        End With

    '        FutureSub.schedule(AddressOf UIREF_update, game.REF_FUTURE_deepCopyOfSlots())
    '    End Sub
    '#End Region

    '#Region "Update"
    '    Public Sub UIREF_update(ByVal slots As List(Of W3Slot)) Handles game.updated
    '        If uiRef.queueIfRemote(curry(AddressOf UIREF_update, slots)) Then Return
    '        If game.PEEK_STATE = States.Closed Then Return

    '        Dim slotClone As W3Slot = Nothing
    '        For Each s As W3Slot In slots
    '            If s.index = slotIndex Then slotClone = s
    '        Next s
    '        If slotClone Is Nothing Then Return

    '        holdEvents = True
    '        mnuSlotCommands.Enabled = game.PEEK_STATE() < States.Loading
    '        txtSlot.Enabled = mnuSlotCommands.Enabled
    '        Dim b As Boolean = slotClone.state <> SlotStates.Player
    '        mnuBoot.Enabled = Not b
    '        mnuReserve.Enabled = b
    '        mnuOpen.Enabled = b AndAlso (slotClone.state <> SlotStates.Open)
    '        mnuClose.Enabled = b AndAlso (slotClone.state <> SlotStates.Closed)
    '        mnuComputerEasy.Enabled = b AndAlso (slotClone.state <> SlotStates.Computer OrElse slotClone.cpu <> W3Slot.CpuDifficulties.Easy)
    '        mnuComputerNormal.Enabled = b AndAlso (slotClone.state <> SlotStates.Computer OrElse slotClone.cpu <> W3Slot.CpuDifficulties.Normal)
    '        mnuComputerInsane.Enabled = b AndAlso (slotClone.state <> SlotStates.Computer OrElse slotClone.cpu <> W3Slot.CpuDifficulties.Insane)
    '        mnuShowLog.Enabled = slotClone.state = SlotStates.Player

    '        If game.PEEK_STATE() >= States.Playing Then
    '            txtSlot.Text = slotClone.toString()
    '            holdEvents = False
    '            Return
    '        End If

    '        mnuCboRace.SelectedIndex = mnuCboRace.Items.IndexOf(slotClone.race)
    '        mnuCboColor.SelectedIndex = mnuCboColor.Items.IndexOf(slotClone.color)
    '        mnuCboHandicap.SelectedItem = slotClone.handicap
    '        mnuCboTeam.SelectedIndex = switch(mnuCboTeam.Enabled, CInt(slotClone.team), 0)
    '        mnuChkLock.Checked = slotClone.locked = W3Slot.LockStates.sticky
    '        txtSlot.Text = slotClone.toString()

    '        For i As Integer = 0 To slots.Count - 1
    '            Dim s As String = (i + 1).ToString() + ": " + slots(i).toString()
    '            If CStr(mnuCboSwap.Items(i)) <> s Then mnuCboSwap.Items(i) = s
    '        Next i
    '        holdEvents = False
    '    End Sub
    '#End Region

    '#Region "UI Events"
    '    Private Sub uiMnuOpen_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuOpen.Click
    '        game.REF_openSlot(slotIndex)
    '    End Sub
    '    Private Sub uiMnuClose_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuClose.Click
    '        game.REF_closeSlot(slotIndex)
    '    End Sub
    '    Private Sub uiMnuComputerEasy_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuComputerEasy.Click
    '        game.REF_setSlotCPU(slotIndex, W3Slot.CpuDifficulties.Easy)
    '    End Sub
    '    Private Sub uiMnuComputerNormal_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuComputerNormal.Click
    '        game.REF_setSlotCPU(slotIndex, W3Slot.CpuDifficulties.Normal)
    '    End Sub
    '    Private Sub uiMnuComputerInsane_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuComputerInsane.Click
    '        game.REF_setSlotCPU(slotIndex, W3Slot.CpuDifficulties.Insane)
    '    End Sub
    '    Private Sub uiMnuSwap_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuSwap.Click
    '        game.REF_swapSlots(slotIndex, mnuCboSwap.SelectedIndex)
    '    End Sub
    '    Private Sub uiMnuReserve_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuReserve.Click
    '        If mnuTxtReserve.Text = "" Then Return
    '        game.REF_reserveSlot(slotIndex, mnuTxtReserve.Text)
    '    End Sub
    '    Private Sub uiMnuBoot_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuBoot.Click
    '        game.REF_bootSlot(slotIndex)
    '    End Sub
    '    Private Sub uiMnuCboHandicap_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuCboHandicap.SelectedIndexChanged
    '        If holdEvents Then Return
    '        If mnuCboHandicap.SelectedIndex < 0 Then Return
    '        game.REF_setSlotHandicap(slotIndex, CByte(mnuCboHandicap.SelectedItem))
    '    End Sub
    '    Private Sub uiMnuCboRace_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuCboRace.SelectedIndexChanged
    '        If holdEvents Then Return
    '        If mnuCboRace.SelectedIndex < 0 Then Return
    '        game.REF_setSlotRace(slotIndex, CType(mnuCboRace.SelectedItem, W3Slot.RaceFlags))
    '    End Sub
    '    Private Sub uiMnuCboTeam_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuCboTeam.SelectedIndexChanged
    '        If holdEvents Then Return
    '        If mnuCboTeam.Enabled = False Or mnuCboTeam.SelectedIndex < 0 Then Return
    '        game.REF_setSlotTeam(slotIndex, CByte(mnuCboTeam.SelectedIndex))
    '    End Sub
    '    Private Sub uiMnuCboColor_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuCboColor.SelectedIndexChanged
    '        If holdEvents Then Return
    '        If mnuCboColor.SelectedIndex < 0 Then Return
    '        game.REF_setSlotColor(slotIndex, CType(mnuCboColor.SelectedItem, W3Slot.PlayerColor))
    '    End Sub
    '    Private Sub uiMnuChkLock_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuChkLock.Click
    '        If mnuChkLock.Checked Then
    '            game.REF_lockSlot(slotIndex, W3Slot.LockStates.sticky)
    '        Else
    '            game.REF_lockSlot(slotIndex, W3Slot.LockStates.unlocked)
    '        End If
    '    End Sub

    '    Private Sub mnuShowLog_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles mnuShowLog.Click
    '        FutureSub.schedule( _
    '            AddressOf UIREF_showLog, _
    '                Me.game.REF_FUTURE_deepCopyOfSlots())
    '    End Sub
    '    Private Sub UIREF_showLog(ByVal slots As List(Of W3Slot))
    '        If uiRef.queueIfRemote(curry(AddressOf UIREF_showLog, slots)) Then Return
    '        For Each s As W3Slot In slots
    '            If s.index = Me.slotIndex AndAlso s.state = SlotStates.Player Then
    '                Dim f As New FrmLogger(s.player.PEEK_name, s.player.logger, True, False, False)
    '                f.Show()
    '                Return
    '            End If
    '        Next s
    '    End Sub
    '#End Region
End Class
