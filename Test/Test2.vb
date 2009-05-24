'Public Module temptemp
'    '    Public Sub gen()
'    '        remf()
'    '        genx(1, 0)
'    '    End Sub

'    '    Private ReadOnly ss_FILE_REPLACE() As String = { _
'    '        "Scripts\common.j", "Scripts\blizzard.j" _
'    '    }
'    '    Private ReadOnly ss_FILE_KEEP() As String = { _
'    ' _
'    '    } '"war3map.doo" _

'    Public Sub remf()
'        Dim base = "C:\Program Files\Warcraft III\Maps\"
'        Dim src = base + "Others\DiabloDuel v4.5.w3x"
'        Dim dst_folder1 = base + "Test\"
'        Dim dst1 = dst_folder1 + "m.w3x"
'        IO.Directory.CreateDirectory(dst_folder1)
'        IO.File.Delete(dst1)
'        Dim mpqa = New MPQ.Archive(src)

'        Dim ss = New String() { _
'                        "war3map.j", _
'                        "Scripts\common.j", _
'                        "Scripts\blizzard.j", _
'                        "scripts\war3map.j", _
'                        "war3map.w3e", _
'                        "war3map.wpm", _
'                        "war3map.doo", _
'                        "war3map.w3u", _
'                        "war3map.w3b", _
'                        "war3map.w3d", _
'                        "war3map.w3a", _
'                        "war3map.w3i", _
'                        "war3map.w3q"}

'        Dim i = 0
'        Dim ff As New List(Of UInteger)
'        For Each h In mpqa.hashTable.hashes
'            i += 1
'            Dim b = False
'            For Each s In ss
'                If mpqa.hashTable.contains(s) Then
'                    b = b OrElse mpqa.hashTable.hash(s) Is h
'                End If
'            Next s
'            Debug.Print(i.ToString + " => " + h.key.ToString)
'            If Not b Then '((i > 0 And i <= 102) Or i > 103) Then
'                If h.key < UInteger.MaxValue - 1 Then
'                    h.key = 58238753
'                    h.fileIndex = MPQ.HashTable.HashEntry.FILE_INDEX_DELETED
'                End If
'            Else
'                ff.Add(h.fileIndex)
'            End If
'        Next h
'        For i = 0 To mpqa.fileTable.fileEntries.Count - 1
'            If ff.Contains(CUInt(i)) Then Continue For
'            mpqa.fileTable.fileEntries(i).actualSize = 0
'        Next i

'        'For i As Integer = 0 To ss2.Length - 1
'        '    Dim hb As MPQ.HashTable.HashEntry = mpqa.hashTable.hash(ss4(i))
'        '    hb.key = 58238753
'        '    With mpqa.hashTable.getEmpty(ss2(i))
'        '        .language = hb.language
'        '        .fileIndex = hb.fileIndex
'        '        .key = MPQ.Crypt.HashFileName(ss2(i))
'        '    End With
'        'Next i

'        mpqa.plainWriteToFile(dst1)
'        Debug.Print("done")
'    End Sub
'    '    Public Sub genx(ByVal i As UInteger, ByVal j As UInteger)
'    '        Dim base As String = "C:\Program Files\Warcraft III\Maps\Test\aaa\"
'    '        Dim src As String = base + "m.w3x"
'    '        Dim subfile As String = "war3map.j"
'    '        Dim subfiles() As String = concat(ss_FILE_REPLACE, ss_FILE_KEEP)
'    '        Dim dst_folder1 As String = base + "size" + i.ToString() + "\"
'    '        Dim dst1 As String = dst_folder1 + "m.w3x"
'    '        IO.Directory.CreateDirectory(dst_folder1)
'    '        IO.File.Delete(dst1)
'    '        Dim mpqa As New MPQ.Archive(src)
'    '        With New MPQ.FileStream(mpqa, subfile)
'    '            .fileTableEntry.actualSize = CUInt(i)
'    '        End With
'    '        For Each s As String In subfiles
'    '            With New MPQ.FileStream(mpqa, s)
'    '                .fileTableEntry.actualSize = 0
'    '            End With
'    '        Next s

'    '        mpqa.createSimpleArchive(dst1)

'    '        mpqa = New MPQ.Archive(dst1)
'    '        Dim pos_file1 As UInteger
'    '        With New MPQ.FileStream(mpqa, subfile)
'    '            pos_file1 = .fileTableEntry.filePosition
'    '        End With
'    '        Dim w As IO.Stream = mpqa.streamFactory.make()
'    '        w.Seek(pos_file1, IO.SeekOrigin.Begin)
'    '        For j = 1 To i
'    '            If j = 1 Then
'    '                w.WriteByte(0)
'    '            Else
'    '                Select Case j Mod 4
'    '                    Case 0
'    '                        w.WriteByte(CByte(&H7E))
'    '                    Case 1
'    '                        w.WriteByte(CByte(&H5A))
'    '                    Case 2
'    '                        w.WriteByte(CByte(&H42))
'    '                    Case 3
'    '                        w.WriteByte(CByte(&H66))
'    '                End Select
'    '            End If
'    '        Next j
'    '        w.Close()
'    '        fixAttributes(dst1, subfile)
'    '        For Each s As String In subfiles
'    '            fixAttributes(dst1, s)
'    '        Next s
'    '    End Sub
'    '    Public Sub fixAttributes(ByVal archivePath As String, ByVal filename As String)
'    '        Dim mpqa As New MPQ.Archive(archivePath)
'    '        Dim f As New MPQ.FileStream(mpqa, filename)
'    '        Dim pos_sign As Long
'    '        Try
'    '            pos_sign = New MPQ.FileStream(mpqa, "(attributes)").fileTableEntry.filePosition + mpqa.fileTable.fileEntries.IndexOf(f.fileTableEntry) * 4 + 8
'    '        Catch e As Exception
'    '            Return
'    '        End Try
'    '        Dim w As IO.Stream = mpqa.streamFactory.make()
'    '        w.Seek(pos_sign, IO.SeekOrigin.Begin)
'    '        With New IO.BinaryWriter(w)
'    '            .Write(BnetCrypt.crc32(f))
'    '        End With
'    '        w.Close()
'    '        f.Close()
'    '    End Sub
'End Module
