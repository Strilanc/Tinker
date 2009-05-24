Imports HostBot.Immutable
Imports HostBot.MPQ
Imports HostBot.MPQ.Compression
Imports HostBot.MPQ.Common
Imports HostBot.Pickling.Jars

Public Module TestModule
    '    'Public Sub connect_dummy_players(ByVal port As UShort, ByVal ParamArray names() As String)
    '    '    For Each name In names
    '    '        Dim d = New DummyPlayer(name)
    '    '        d.connect("localhost", port)
    '    '    Next name
    '    'End Sub

    'Public Sub unprotect(ByVal mpqpath As String)
    '    Dim mpqa = New MPQ.Archive(mpqpath)
    '    mpqa.plainWriteToFile(mpqpath.Substring(0, mpqpath.Length - 4) + "_unp.mpq")
    'End Sub

    '    'Public Sub testmpq()
    '    'Dim folder = "C:\Program Files\Warcraft III\Maps\Test\"
    '    'Dim mpqa = New MPQ.Archive(folder + "footmen frenzy v4.2 -ar.w3x")
    '    'Using f = New MPQ.FileStream(mpqa, "scripts\war3map.j")
    '    '    write_stream_to_disk(f, "C:\test_output.dat")
    '    'End Using
    '    'Dim m = New Warcraft3.W3Map(folder, "footmen frenzy v4.2 -ar.w3x", My.Settings.war3path)
    '    'Debug.Print(unpackHexString(m.xoroKey))
    '    'End Sub

    '    'send different data to each player, for multiple players in a single slot
    '    '    Case "Download\multimap.w3x"
    '    '        Dim address = unpackUInteger(packHexString("28 06 00 00"))
    '    '        For Each receiver In players_L
    '    '            If receiver.is_fake Then Continue For
    '    '            Dim data = New Byte() {}
    '    '            For Each player In players_L
    '    '                If player.is_fake Then Continue For
    '    '                Dim slot = get_player_slot_L(player)
    '    '                If player Is receiver Then
    '    '                    slot = slot.oppslot
    '    '                End If
    '    '                Dim text = "hero" + slot.index.ToString() + player.name_P
    '    '                Dim pd = packeteer.packData_fireChatTrigger(slots_L(0).player, text, address)
    '    '                data = concat(data, New Byte() {slots_L(0).player.index_I}, packUInteger(CUShort(pd.Length), 2), pd)
    '    '            Next player
    '    '            receiver.queue_tick_R(New W3PlayerTickRecord(100, Environment.TickCount))
    '    '            receiver.send_packet_R(packeteer.packPacket_GAME_TICK_HOST(100, data))
    '    '        Next receiver

    '    '    Public Sub testExtractMPQ()
    '    '        Dim map As New Dictionary(Of ULong, String)
    '    '        'Dim mpqa As New Archive("C:\Program Files\Warcraft III\war3.mpq")
    '    '        Dim mpqa As New Archive("C:\Program Files\Warcraft III\Maps\Mine\Chat Trigger Packet Test.w3x")
    '    '        readStreamMPQListFile(New IO.FileStream("C:\Program Files\Warcraft III\war3listfile.txt", IO.FileMode.Open, IO.FileAccess.Read), map)
    '    '        extractMPQ("C:\Documents and Settings\Craig_2\Desktop\current\dotampq\", mpqa, map)
    '    '    End Sub
    '    '    Public Sub test()
    '    '        Dim mpqa As New Archive("C:\Program Files\Warcraft III\maps\hostbot\DotA Allstars v6.51.w3x")
    '    '        'Dim mpqa As New Archive("C:\Program Files\Warcraft III\maps\mine\Power Towers v1.25.w3x")
    '    '        'Dim mpq As New MPQArchive("C:\Documents and Settings\Craig_2\Desktop\Power Towers v1.25b CV.w3x")
    '    '        'For Each hash As MPQArchive.MPQHashTable.MPQHash In mpqa.hashTable.hashes
    '    '        '    If hash.fileIndex = MPQArchive.MPQHashTable.MPQHash.FILE_INDEX_DELETED Or hash.fileIndex = MPQArchive.MPQHashTable.MPQHash.FILE_INDEX_EMPTY Then Continue For
    '    '        '    Dim f As MPQFileStream
    '    '        '    Try
    '    '        '        f = New MPQFileStream(mpq, hash.fileIndex)
    '    '        '    Catch e As Exception
    '    '        '        Debug.Print(e.Message)
    '    '        '        Continue For
    '    '        '    End Try
    '    '        '    Dim bb(0 To Math.Min(10, CInt(f.Length) - 1)) As Byte
    '    '        '    f.Read(bb, 0, bb.Length)
    '    '        '    Dim s As String = ""
    '    '        '    For Each b As Byte In bb
    '    '        '        s += Chr(b)
    '    '        '    Next b
    '    '        '    Debug.Print(s)
    '    '        'Next hash
    '    '        'tryFile(mpq, "(listfile)")
    '    '        'tryFile(mpq, "(signature)")
    '    '        'tryFile(mpq, "(attributes)")
    '    '        'tryFile(mpq, "war3map.w3e")
    '    '        'tryFile(mpq, "war3map.w3i")
    '    '        'tryFile(mpq, "war3map.wtg")
    '    '        'tryFile(mpq, "war3map.wct")
    '    '        'tryFile(mpq, "war3map.wts")
    '    '        tryFile(mpqa, "war3map.j")
    '    '        tryFile(mpqa, "Scripts\war3map.j")
    '    '        'tryFile(mpq, "war3map.shd")
    '    '        'tryFile(mpq, "war3mapMap.blp")
    '    '        'tryFile(mpq, "war3mapMap.b00")
    '    '        'tryFile(mpq, "war3mapMap.tga")
    '    '        'tryFile(mpq, "war3mapPreview.tga")
    '    '        'tryFile(mpq, "war3map.mmp")
    '    '        'tryFile(mpq, "war3mapPath.tga")
    '    '        'tryFile(mpq, "war3map.wmp")
    '    '        'tryFile(mpq, "war3map.doo")
    '    '        'tryFile(mpq, "war3mapUnits.doo")
    '    '        'tryFile(mpq, "war3map.w3r")
    '    '        'tryFile(mpq, "war3map.w3c")
    '    '        'tryFile(mpq, "war3map.w3s")
    '    '        'tryFile(mpq, "war3map.w3u")
    '    '        'tryFile(mpq, "war3map.w3t")
    '    '        'tryFile(mpq, "war3map.w3a")
    '    '        'tryFile(mpq, "war3map.w3b")
    '    '        'tryFile(mpq, "war3map.w3d")
    '    '        'tryFile(mpq, "war3map.w3q")
    '    '        'tryFile(mpq, "war3mapMisc.txt")
    '    '        'tryFile(mpq, "war3mapSkin.txt")
    '    '        'tryFile(mpq, "war3mapExtra.txt")
    '    '        'tryFile(mpq, "war3map.imp")
    '    '        'threadedCall(AddressOf test1)
    '    '        'Threading.Thread.Sleep(15000)
    '    '        'Debug.Print("timeout")
    '    '    End Sub
    '    '    Private Function tryFile(ByVal mpqa As Archive, ByVal s As String) As Boolean
    '    '        Dim x As IO.FileStream = Nothing
    '    '        Try
    '    '            Dim f As New FileStream(mpqa, s)
    '    '            x = New IO.FileStream("C:\temp" + s.Replace("\", "-") + ".txt", IO.FileMode.CreateNew)
    '    '            For i As Integer = 0 To CInt(f.Length) - 1
    '    '                x.WriteByte(CByte(f.ReadByte()))
    '    '            Next i
    '    '            x.Close()
    '    '            Debug.Print(s + " succeeded")
    '    '            Return True
    '    '        Catch e As Exception
    '    '            If x IsNot Nothing Then x.Close()
    '    '            Debug.Print(s + " failed: " + e.Message)
    '    '            Return False
    '    '        End Try
    '    '    End Function
    '   Public Sub testXoro(ByVal folder As String, ByVal file As String)
    '       Debug.Print(unpackHexString(New Warcraft3.W3Map(folder, file, My.Settings.war3path).xoroKey))
    '   End Sub
    '   Public Sub test()
    '       Dim fwc3 = "C:\program files\warcraft III\"
    '       Dim fwc3m = fwc3 + "maps\"
    '       Dim list = New Dictionary(Of ULong, String)
    '       MPQ.Common.readStreamMPQListFile(New IO.FileStream(fwc3 + "war3listfile.txt", IO.FileMode.Open), list)
    '       'MPQ.Common.Common.extractMPQ(fwc3m + "test\", New MPQ.Archive(fwc3m + "others\Castle Fight v1.12.w3x disabled"), list)
    '       MPQ.Common.Common.extractMPQ(fwc3m + "test2\", New MPQ.Archive(fwc3m + "test\Castle Fight Hacked2 v1.12.w3x"), list)
    '   End Sub
    '   Public Sub test2()
    '       gen_hacked_w3u("C:\Program Files\Warcraft III\Maps\Test\war3map.w3u")
    '       gen_hacked_w3j("C:\Program Files\Warcraft III\Maps\Test\scripts\war3map.j")
    '       Dim arg = New String("a"c, 32)
    '       If True Then
    '           Dim target = unpackUInteger(New Warcraft3.W3Map("C:\Program Files\Warcraft III\Maps\", "Others\Castle Fight v1.12.w3x disabled", My.Settings.war3path).xoroKey)
    '           gen_hacked_map(arg)
    '           Dim val = unpackUInteger(New Warcraft3.W3Map("C:\Program Files\Warcraft III\Maps\", "Test\Castle Fight Hacked2 v1.12.w3x", My.Settings.war3path).xoroKey)
    '           For i = 0 To 31
    '               Dim arg2 = arg.Substring(0, i) + "c" + arg.Substring(i + 1)
    '               gen_hacked_map(arg2)
    '               Dim val2 = unpackUInteger(New Warcraft3.W3Map("C:\Program Files\Warcraft III\Maps\", "Test\Castle Fight Hacked2 v1.12.w3x", My.Settings.war3path).xoroKey)
    '               If bitdif(val2, target) < bitdif(val, target) Then
    '                   arg = arg2
    '                   val = val2
    '               End If
    '               Debug.Print("{0:00} arg={1}, dif={2}", i, arg, bin(val Xor target, 32))
    '           Next i
    '           Debug.Print(arg)
    '       Else
    '           arg = "aaaaccacaaaccccaaccccaaacacccaca"
    '       End If
    '       gen_hacked_map(arg)
    '       Debug.Print(unpackHexString(New Warcraft3.W3Map("C:\Program Files\Warcraft III\Maps\", "Test\Castle Fight Hacked2 v1.12.w3x", My.Settings.war3path).xoroKey))
    '   End Sub
    '   Private Function bitdif(ByVal u1 As UInteger, ByVal u2 As UInteger) As Integer
    '       Dim n = 0
    '       For i = 0 To 31
    '           If (u1 And (CUInt(1) << i)) <> (u2 And (CUInt(1) << i)) Then
    '               n += 1
    '           End If
    '       Next i
    '       Return n
    '   End Function
    '   Public Sub gen_hacked_w3u(ByVal filepath As String)
    '       Using si As New IO.FileStream(filepath, IO.FileMode.Open, IO.FileAccess.Read)
    '           Dim bb(0 To CInt(si.Length) - 1) As Byte
    '           si.Read(bb, 0, bb.Length)
    '           Dim cc(0 To bb.Length - 1) As Char
    '           For i = 0 To bb.Length - 1
    '               cc(i) = Chr(bb(i))
    '           Next i
    '           Dim s As String = cc
    '           Dim mode = 0
    '           Dim new_len = 0
    '           For i = 0 To s.Length - 1
    '               If i < s.Length - 10 Then
    '                   If s.Substring(i, 4) = "utyp" Then
    '                       mode = 8
    '                   ElseIf mode > 0 Then
    '                       If Asc(s(i)) = 0 And mode = 1 Then mode = 0
    '                       If mode > 1 Then mode -= 1
    '                       If s.Substring(i, 5) = ",ward" Then
    '                           i += 4
    '                           mode = 0
    '                           Continue For
    '                       End If
    '                   End If
    '               End If
    '               cc(new_len) = s(i)
    '               new_len += 1
    '           Next i
    '           For i = 0 To new_len - 1
    '               bb(i) = CByte(Asc(cc(i)))
    '           Next i

    '           IO.File.Delete(filepath + ".new")
    '           Using so As New IO.FileStream(filepath + ".new", IO.FileMode.Create, IO.FileAccess.Write)
    '               so.Write(bb, 0, new_len)
    '           End Using
    '       End Using
    '   End Sub
    '   Public Sub gen_hacked_w3j(ByVal filepath As String)
    '       Using si As New IO.FileStream(filepath, IO.FileMode.Open, IO.FileAccess.Read)
    '           Dim bb(0 To CInt(si.Length) - 1) As Byte
    '           si.Read(bb, 0, bb.Length)
    '           Dim cc(0 To bb.Length - 1) As Char
    '           For i = 0 To bb.Length - 1
    '               cc(i) = Chr(bb(i))
    '           Next i
    '           Dim s As String = cc
    '           s = s.Replace(Chr(13), Environment.NewLine)
    '           ReDim bb(0 To s.Length - 1)
    '           For i = 0 To s.Length - 1
    '               bb(i) = CByte(Asc(s(i)))
    '           Next i

    '           IO.File.Delete(filepath + ".new")
    '           Using so As New IO.FileStream(filepath + ".new", IO.FileMode.Create, IO.FileAccess.Write)
    '               so.Write(bb, 0, bb.Length)
    '           End Using
    '       End Using
    '   End Sub
    '   Public Sub gen_hacked_map(ByVal arg As String)
    '       Dim src = "C:\Program Files\Warcraft III\Maps\Others\Castle Fight v1.12.w3x disabled"
    '       Dim dst = "C:\Program Files\Warcraft III\Maps\Test\Castle Fight Hacked2 v1.12.w3x"
    '       Dim mpq As New MPQ.Archive(src)
    '       IO.File.Delete("C:\Program Files\Warcraft III\Maps\Test\Castle Fight Hacked2 v1.12.w3x")
    '       mpq.plainWriteToFile(dst, _
    '                            "war3map.w3u", "replace C:\Program Files\Warcraft III\Maps\Test\war3map.w3u.new", _
    '_
    '                            "war3map.w3a", "append " + unpackHexString(packString(arg)), _
    '_
    '                            "war3map.w3u", "compress", _
    '                            "war3map.doo", "compress", _
    '                            "war3map.shd", "compress", _
    '                            "war3map.w3a", "compress", _
    '                            "scripts\war3map.j", "compress", _
    '_
    '                            "UI\Glues\Loading\Load-Generic\Generic-Loading-BotLeft.blp", "delete", _
    '                            "UI\Glues\Loading\Load-Generic\Generic-Loading-BotRight.blp", "delete", _
    '                            "UI\Glues\Loading\Load-Generic\Generic-Loading-TopLeft.blp", "delete", _
    '                            "UI\Glues\Loading\Load-Generic\Generic-Loading-TopRight.blp", "delete", _
    '                            "(attributes)", "delete", _
    '                            "war3mapPreview.tga", "delete")

    '       '"scripts\war3map.j", "delete", _
    '       '"war3map.j", "add", _
    '       '"war3map.j", "replace C:\Program Files\Warcraft III\Maps\Test\scripts\war3map.j.new", _
    '       '"war3map.j", "prepend " + unpackHexString(packString("globals" + Environment.NewLine + " //" + arg)), _

    '       'Pad remainder
    '       Dim n As Long = 0
    '       Using f As New IO.FileStream(src, IO.FileMode.Open, IO.FileAccess.Read)
    '           n = f.Length
    '       End Using
    '       Using f As New IO.FileStream(dst, IO.FileMode.Append, IO.FileAccess.Write)
    '           If f.Length > n Then Debug.Print("Created file is too large.")
    '           If f.Length < n Then
    '               Dim bb(0 To CInt(n - f.Length - 1)) As Byte
    '               f.Write(bb, 0, bb.Length)
    '           End If
    '       End Using
    '   End Sub
End Module
