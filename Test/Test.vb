Imports HostBot.Immutable
Imports HostBot.Mpq
Imports HostBot.Mpq.Compression
Imports HostBot.Mpq.Common
Imports HostBot.Pickling.Jars
Imports System.IO.Compression

Public Module TestModule
    Public Sub Test()
        Dim archive = New MpqArchive("C:\Users\Craig\Downloads\SpiritOfVengeanceFeatures.w3x")
        Dim bb = streamBytes(archive.OpenFile("(attributes)"))
    End Sub
    'send different initialization data to each player, for multiple players in a single slot
    '    Case "Download\multimap.w3x"
    '        Dim address = unpackUInteger(packHexString("28 06 00 00"))
    '        For Each receiver In players_L
    '            If receiver.is_fake Then Continue For
    '            Dim data = New Byte() {}
    '            For Each player In players_L
    '                If player.is_fake Then Continue For
    '                Dim slot = get_player_slot_L(player)
    '                If player Is receiver Then
    '                    slot = slot.oppslot
    '                End If
    '                Dim text = "hero" + slot.index.ToString() + player.name_P
    '                Dim pd = packeteer.packData_fireChatTrigger(slots_L(0).player, text, address)
    '                data = concat(data, New Byte() {slots_L(0).player.index_I}, packUInteger(CUShort(pd.Length), 2), pd)
    '            Next player
    '            receiver.queue_tick_R(New W3PlayerTickRecord(100, Environment.TickCount))
    '            receiver.send_packet_R(packeteer.packPacket_GAME_TICK_HOST(100, data))
    '        Next receiver
End Module
