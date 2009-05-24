Imports HostBot.MPQ
Imports HostBot.MPQ.Crypt

Namespace MPQ.Common
    Module Common
        Public Sub readStreamMPQListFile(ByVal s As IO.Stream, ByVal map As Dictionary(Of UInt64, String))
            Try
                With New IO.StreamReader(s)
                    While Not .EndOfStream
                        Dim path As String = .ReadLine()
                        map(HashFileName(path)) = path
                    End While
                End With
            Catch e As Exception
                'invalid list file
            End Try
        End Sub

        Public Sub readMPQListFile(ByVal mpqa As MPQArchive, ByVal map As Dictionary(Of UInt64, String))
            map(HashFileName("(listfile)")) = "(listfile)"
            Try
                readStreamMPQListFile(New MPQFileStream(mpqa, "(listfile)"), map)
            Catch e As Exception
                'no list file
            End Try
        End Sub

        Public Function listMPQ(ByVal mpqa As MPQArchive, ByVal map As Dictionary(Of UInt64, String)) As List(Of String)
            Dim L As New List(Of String)
            readMPQListFile(mpqa, map)
            For Each h As MPQHashTable.HashEntry In mpqa.hashTable.hashes
                If h.fileIndex = MPQHashTable.HashEntry.FILE_INDEX_DELETED Then Continue For
                If h.fileIndex = MPQHashTable.HashEntry.FILE_INDEX_EMPTY Then Continue For
                If map.ContainsKey(h.key) Then
                    L.Add(map(h.key))
                Else
                    L.Add("unknown@" + h.fileIndex.ToString() + "=" + Hex(h.key))
                End If
            Next h
            Return L
        End Function

        Public Sub write_stream_to_disk(ByVal src As IO.Stream, ByVal dst As String)
            Dim b = New IO.BufferedStream(src)
            Using f = New IO.BufferedStream(New IO.FileStream(dst, IO.FileMode.Create, IO.FileAccess.Write))
                Do
                    Dim i = src.ReadByte()
                    If i = -1 Then Exit Do
                    f.WriteByte(CByte(i))
                Loop
            End Using
        End Sub

        Public Sub extractMPQ(ByVal targetpath As String, ByVal mpqa As MPQArchive, ByVal map As Dictionary(Of UInt64, String))
            readMPQListFile(mpqa, map)
            For Each h As MPQHashTable.HashEntry In mpqa.hashTable.hashes
                If h.fileIndex = MPQHashTable.HashEntry.FILE_INDEX_DELETED Then Continue For
                If h.fileIndex = MPQHashTable.HashEntry.FILE_INDEX_EMPTY Then Continue For
                Dim s As String = ""
                Dim m As MPQFileStream = Nothing
                Dim f As IO.FileStream = Nothing
                Try
                    'Open file
                    If map.ContainsKey(h.key) Then
                        s = map(h.key)
                        m = New MPQFileStream(mpqa, s)
                    Else
                        s = "Unknown" + CStr(h.key)
                        m = New MPQFileStream(mpqa, h.fileIndex)
                    End If
                    'Create sub directories
                    Dim ss() As String = s.Split("\"c, "/"c)
                    Dim curpath As String = targetpath
                    For i As Integer = 0 To ss.Length - 2
                        curpath += ss(i) + "\"
                        If Not IO.Directory.Exists(curpath) Then IO.Directory.CreateDirectory(curpath)
                    Next i
                    'Write to file
                    Dim buffer(0 To 511) As Byte
                    f = New IO.FileStream(targetpath + s, IO.FileMode.CreateNew, IO.FileAccess.Write)
                    For i As Integer = 0 To CInt(m.Length - 1)
                        f.Write(buffer, 0, m.Read(buffer, 0, buffer.Length))
                    Next i
                    'cleanup
                    f.Close()
                    m.Close()
                    Debug.Print("Extracted " + s)
                Catch e As IO.InvalidDataException
                    Debug.Print("Error extracting " + s + ": " + e.Message)
                    If f IsNot Nothing Then f.Close() : IO.File.Delete(targetpath + s)
                    If m IsNot Nothing Then m.Close()
                Catch e As IO.IOException
                    Debug.Print("Error extracting " + s + ": " + e.Message)
                    If f IsNot Nothing Then f.Close() : IO.File.Delete(targetpath + s)
                    If m IsNot Nothing Then m.Close()
                End Try
            Next h
        End Sub
    End Module
End Namespace
