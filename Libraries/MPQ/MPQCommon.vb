Imports HostBot.Mpq
Imports HostBot.Mpq.Crypt

Namespace Mpq.Common
    Public Module Common
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

        Public Sub readMPQListFile(ByVal mpqa As MpqArchive, ByVal map As Dictionary(Of UInt64, String))
            map(HashFileName("(listfile)")) = "(listfile)"
            Try
                readStreamMPQListFile(mpqa.OpenFile("(listfile)"), map)
            Catch e As Exception
                'no list file
            End Try
        End Sub

        Public Function listMPQ(ByVal mpqa As MpqArchive, ByVal map As Dictionary(Of UInt64, String)) As List(Of String)
            Dim L As New List(Of String)
            readMPQListFile(mpqa, map)
            For Each h As MpqHashTable.HashEntry In mpqa.hashTable.hashes
                If h.fileIndex = MpqHashTable.HashEntry.FILE_INDEX_DELETED Then Continue For
                If h.fileIndex = MpqHashTable.HashEntry.FILE_INDEX_EMPTY Then Continue For
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

        Public Sub extractMPQ(ByVal targetpath As String, ByVal archive As MpqArchive, ByVal map As Dictionary(Of UInt64, String))
            targetpath = targetpath.Replace(IO.Path.AltDirectorySeparatorChar, IO.Path.DirectorySeparatorChar)
            If targetpath(targetpath.Length - 1) <> IO.Path.AltDirectorySeparatorChar Then
                targetpath += IO.Path.AltDirectorySeparatorChar
            End If
            readMPQListFile(archive, map)
            For Each h As MpqHashTable.HashEntry In archive.hashTable.hashes
                If h.fileIndex = MpqHashTable.HashEntry.FILE_INDEX_DELETED Then Continue For
                If h.fileIndex = MpqHashTable.HashEntry.FILE_INDEX_EMPTY Then Continue For
                Dim filename = ""
                Dim m As IO.Stream = Nothing
                Try
                    'Open file
                    If map.ContainsKey(h.key) Then
                        filename = map(h.key)
                        m = archive.OpenFile(filename)
                    Else
                        filename = "Unknown" + CStr(h.key)
                        m = archive.OpenFile(h.fileIndex)
                    End If
                    'Create sub directories
                    Dim ss() As String = filename.Split("\"c, "/"c)
                    Dim curpath As String = targetpath
                    For i = 0 To ss.Length - 2
                        curpath += ss(i) + "\"
                        If Not IO.Directory.Exists(curpath) Then IO.Directory.CreateDirectory(curpath)
                    Next i
                    'Write to file
                    Dim buffer(0 To 511) As Byte
                    write_stream_to_disk(m, targetpath + filename)
                    Debug.Print("Extracted " + filename)
                Catch e As IO.InvalidDataException
                    Debug.Print("Error extracting " + filename + ": " + e.ToString)
                Catch e As IO.IOException
                    Debug.Print("Error extracting " + filename + ": " + e.ToString)
                End Try
                If m IsNot Nothing Then m.Close()
            Next h
        End Sub
    End Module
End Namespace
