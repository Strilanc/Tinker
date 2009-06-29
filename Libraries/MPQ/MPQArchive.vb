Imports HostBot.Mpq.Crypt
Imports HostBot.Mpq.MpqFileTable.FileEntry

Namespace Mpq
    Public Class MPQException
        Inherits Exception
        Public Sub New(ByVal message As String, Optional ByVal innerException As Exception = Nothing)
            MyBase.New(message, innerException)
        End Sub
    End Class

    ''' <summary>
    ''' Opens MPQ files used by Blizzard and others.
    ''' </summary>
    ''' 
    ''' <copyright>
    ''' Copyright (C) 2008 Craig Gidney, craig.gidney@gmail.com
    '''
    ''' This source was adepted from the C version of mpqlib.
    ''' The C version belongs to the following authors,
    '''
    ''' Maik Broemme, mbroemme@plusserver.de
    ''' 
    ''' This program is free software; you can redistribute it and/or modify
    ''' it under the terms of the GNU General Public License as published by
    ''' the Free Software Foundation; either version 2 of the License, or
    ''' (at your option) any later version.
    '''
    ''' This program is distributed in the hope that it will be useful,
    ''' but WITHOUT ANY WARRANTY; without even the implied warranty of
    ''' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    ''' GNU General Public License for more details.
    '''
    ''' You should have received a copy of the GNU General Public License
    ''' along with this program; if not, write to the Free Software
    ''' Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
    ''' </copyright>
    ''' 
    ''' <remarks>
    ''' Targetted at warcraft 3 maps, may not work for other MPQs.
    ''' </remarks>
    Public Class MpqArchive
        Public Const ID_MPQ As UInteger = &H1A51504D 'MPQ\x1A

        Public ReadOnly streamFactory As Func(Of IO.Stream)
        Public ReadOnly hashTable As MpqHashTable 'Map from hashes filesnames to file table indexes
        Public ReadOnly fileTable As MpqFileTable 'Stores the position, size, and other information about all files in the archive

        Public ReadOnly archiveSize As UInteger 'in bytes
        Public ReadOnly hashTablePosition As UInteger 'Absolute position within the parent file
        Public ReadOnly fileTablePosition As UInteger 'Absolute position within the parent file
        Public ReadOnly numHashTableEntries As UInteger 'Number of entries
        Public ReadOnly numFileTableEntries As UInteger 'Number of entries
        Public ReadOnly fileBlockSize As UInteger 'Size of the blocks files in the archive are divided into
        Public ReadOnly filePosition As UInteger 'Position of MPQ archive in the file

        Public Sub New(ByVal path As String)
            Me.New(Function() New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
        End Sub

        Public Sub New(ByVal streamFactory As Func(Of IO.Stream))
            Me.streamFactory = streamFactory
            Using stream = streamFactory()
                Using br = New IO.BinaryReader(stream)
                    'Find valid header
                    Dim found = False
                    For Me.filePosition = 0 To CUInt(stream.Length - 1) Step 512
                        stream.Seek(filePosition, IO.SeekOrigin.Begin)

                        Dim id = br.ReadUInt32()
                        Dim headerSize = br.ReadUInt32()
                        archiveSize = br.ReadUInt32()
                        br.ReadUInt16() 'format version
                        fileBlockSize = br.ReadUInt16() 'order of block size relative to &H200 (corrected later)
                        hashTablePosition = br.ReadUInt32() 'relative to position of archive (corrected later)
                        fileTablePosition = br.ReadUInt32() 'relative to position of archive (corrected later)
                        numHashTableEntries = br.ReadUInt32()
                        numFileTableEntries = br.ReadUInt32()

                        'Protected MPQs mess with values
                        If archiveSize = 0 Then
                            archiveSize = CUInt(stream.Length) - filePosition
                        End If

                        'Check for invalid signature
                        If id <> ID_MPQ Then Continue For
                        If hashTablePosition >= archiveSize Then Continue For
                        If fileTablePosition >= archiveSize Then Continue For

                        'Valid signature!
                        found = True
                        Exit For
                    Next filePosition

                    If Not found Then Throw New MPQException("MPQ archive header not found.")

                    stream.Close()

                    'Correct values
                    fileBlockSize = CUInt(512) << CInt(fileBlockSize) 'correct size to actual size in bytes
                    hashTablePosition += filePosition 'correct position from relative to absolute
                    fileTablePosition += filePosition 'correct position from relative to absolute

                    'Load tables
                    hashTable = New MpqHashTable(Me)
                    fileTable = New MpqFileTable(Me)

                    'Check tables
                    fileTable.checkValidity()
                End Using
            End Using
        End Sub

        Public Function OpenFile(ByVal fileIndex As UInteger) As IO.Stream
            If fileIndex >= fileTable.fileEntries.Count Then Throw New IO.IOException("File ID not in file table")
            Return New MpqFileStream(Me, fileTable.fileEntries(CInt(fileIndex)))
        End Function
        Public Function OpenFile(ByVal filename As String) As IO.Stream
            Dim fileIndex = hashTable.hash(filename).fileIndex
            If fileIndex >= fileTable.fileEntries.Count Then Throw New IO.IOException("File ID not in file table")
            Return New MpqFileStream(Me, fileTable.fileEntries(CInt(fileIndex)), filename)
        End Function

        Public Sub WriteToFile(ByVal targetPath As String, ByVal ParamArray commands() As String)
            Using bbf As New IO.BufferedStream(New IO.FileStream(targetPath, IO.FileMode.CreateNew, IO.FileAccess.ReadWrite, IO.FileShare.None))
                Dim w = New IO.BinaryWriter(bbf)
                Dim stream = streamFactory()
                Dim sep = (Environment.NewLine + "===" + Environment.NewLine).ToAscBytes

                'before archive
                stream.Seek(0, IO.SeekOrigin.Begin)
                With New IO.BinaryReader(New IO.BufferedStream(stream))
                    For i = 0 To CInt(Me.filePosition) - 1
                        w.Write(.ReadByte())
                    Next i
                End With

                Dim file_streams As New Dictionary(Of UInteger, IO.Stream)
                Dim actual_size_map As New Dictionary(Of UInteger, Integer)
                Dim compressed_files As New HashSet(Of UInteger)
                Dim del_files As New HashSet(Of UInteger)

                'Buffer mpq files into memory streams
                For i = 0 To fileTable.fileEntries.Count - 1
                    Dim u = CUInt(i)
                    file_streams(u) = Me.OpenFile(u)
                    actual_size_map(u) = CInt(file_streams(u).Length)
                Next i

                'Apply commands
                For i = 0 To commands.Length - 1 Step 2
                    Dim k = HashFileName(commands(i))
                    Dim com_name = commands(i + 1).ToLower.Split(" "c)(0)
                    Dim com_arg = If(com_name = commands(i + 1), "", commands(i + 1).Substring(com_name.Length + 1))
                    If com_name = "add" Then
                        Dim h = hashTable.getEmpty(commands(i))
                        h.key = k
                        h.language = 0
                        h.fileIndex = del_files.First()
                        del_files.Remove(h.fileIndex)
                        file_streams(h.fileIndex) = New IO.MemoryStream
                        actual_size_map(h.fileIndex) = 0
                        compressed_files.Remove(h.fileIndex)
                        Continue For
                    End If

                    Dim found = False
                    For Each e In hashTable.hashes
                        If e.key = k Then
                            found = True
                            Dim u = e.fileIndex
                            If del_files.Contains(u) Then Throw New InvalidOperationException("Can't delete then apply more operations.")
                            Select Case com_name
                                Case "delete"
                                    del_files.Add(u)
                                    e.fileIndex = Mpq.MpqHashTable.HashEntry.FILE_INDEX_DELETED

                                Case "replace"
                                    file_streams(u) = New IO.FileStream(com_arg, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                                    actual_size_map(u) = CInt(file_streams(u).Length)
                                    compressed_files.Remove(u)

                                Case "prepend"
                                    If compressed_files.Contains(u) Then Throw New InvalidOperationException("Can't compress then prepend.")
                                    Dim bb = com_arg.FromHexStringToBytes
                                    file_streams(u) = New ConcatStream({New IO.MemoryStream(bb), file_streams(u)})
                                    actual_size_map(u) += bb.Length

                                Case "append"
                                    If compressed_files.Contains(u) Then Throw New InvalidOperationException("Can't compress then append.")
                                    Dim bb = com_arg.FromHexStringToBytes
                                    file_streams(u) = New ConcatStream({file_streams(u), New IO.MemoryStream(bb)})
                                    actual_size_map(u) += bb.Length

                                Case "compress"
                                    compressed_files.Add(e.fileIndex)

                                    'Divide into blocks
                                    Dim cur_s = file_streams(u)
                                    Dim block_size = CInt(Me.fileBlockSize)
                                    Dim table_size = CInt(Math.Ceiling(cur_s.Length / block_size) + 1)
                                    Dim blocks(0 To table_size - 2) As IO.MemoryStream
                                    Dim bb(0 To block_size - 1) As Byte
                                    For b = 0 To blocks.Length - 1
                                        cur_s.Seek(b * block_size, IO.SeekOrigin.Begin)
                                        Dim n = cur_s.Read(bb, 0, bb.Length)
                                        blocks(b) = New IO.MemoryStream
                                        blocks(b).Write(bb, 0, n)
                                    Next b

                                    'Compress blocks
                                    For j = 0 To blocks.Length - 1
                                        Dim b = blocks(j)
                                        Dim m = New IO.MemoryStream()
                                        m.WriteByte(Mpq.MpqFileStream.COMPRESSION_TYPES.ZLibDeflate)
                                        Using df As New ZLibStream(m, IO.Compression.CompressionMode.Compress, True)
                                            b.Seek(0, IO.SeekOrigin.Begin)
                                            Dim n = b.Read(bb, 0, bb.Length)
                                            df.Write(bb, 0, n)
                                            df.Flush()
                                        End Using
                                        blocks(j) = m
                                        blocks(j).Seek(0, IO.SeekOrigin.Begin)
                                    Next j

                                    'Write
                                    Dim new_s = New IO.MemoryStream
                                    Dim br = New IO.BinaryWriter(new_s)
                                    Dim tt = CUInt(table_size * 4)
                                    br.Write(tt)
                                    For Each b In blocks
                                        tt += CUInt(b.Length)
                                        br.Write(CUInt(tt))
                                    Next b
                                    For Each b In blocks
                                        b.Seek(0, IO.SeekOrigin.Begin)
                                        bb = streamBytes(b)
                                        br.Write(bb, 0, bb.Length)
                                    Next b
                                    new_s.Seek(0, IO.SeekOrigin.Begin)
                                    file_streams(u) = new_s

                                Case Else
                                    Throw New InvalidOperationException("Unrecognized operation: " + com_name)
                            End Select
                        End If
                    Next e
                    If Not found Then Throw New InvalidOperationException("No file matched operation key.")
                Next i

                'Build new file table layout
                Dim num_file_entries = CUInt(0)
                Dim file_index_map As New Dictionary(Of UInteger, UInteger)
                For i = 0 To fileTable.fileEntries.Count - 1
                    Dim e = fileTable.fileEntries(i)
                    Dim u = CUInt(i)
                    If Not del_files.Contains(u) Then
                        file_index_map(u) = num_file_entries
                        num_file_entries += CUInt(1)
                    End If
                Next i

                'Write header
                w.Write(ID_MPQ)
                w.Write(CUInt(32))
                Dim sizePos = w.BaseStream.Position()
                w.Write(CUInt(0)) 'size placeholder
                w.Write(CShort(21536))
                w.Write(CShort(Math.Log(fileBlockSize \ &H200, 2)))
                w.Write(CUInt(32 + num_file_entries * 16))
                w.Write(CUInt(32))
                w.Write(numHashTableEntries)
                w.Write(num_file_entries)

                'Write file table
                Dim t = CUInt(32 + 16 * (num_file_entries + hashTable.hashes.Count))
                With New IO.BinaryWriter(
                      New Mpq.Crypt.Cypherer(HashString("(block table)", HashType.FILE_KEY), Cypherer.modes.encrypt).
                          ConvertWriteOnlyStream(bbf))
                    For i = 0 To fileTable.fileEntries.Count - 1
                        Dim e = fileTable.fileEntries(i)
                        Dim u = CUInt(i)
                        If del_files.Contains(u) Then Continue For
                        .Write(t + CUInt(sep.Length))
                        .Write(CUInt(file_streams(u).Length))
                        .Write(CUInt(actual_size_map(u)))
                        If compressed_files.Contains(u) Then
                            .Write(FILE_FLAGS.EXISTS Or FILE_FLAGS.COMPRESSED)
                        Else
                            .Write(FILE_FLAGS.EXISTS Or FILE_FLAGS.CONTINUOUS)
                        End If
                        t += CUInt(file_streams(u).Length) + CUInt(sep.Length)
                    Next i
                End With

                'Write hash table
                With New IO.BinaryWriter(
                        New Mpq.Crypt.Cypherer(HashString("(hash table)", HashType.FILE_KEY), Cypherer.modes.encrypt).
                            ConvertWriteOnlyStream(bbf))
                    For Each e In hashTable.hashes
                        .Write(e.key)
                        .Write(e.language)
                        If del_files.Contains(e.fileIndex) OrElse Not file_index_map.ContainsKey(e.fileIndex) Then
                            .Write(Mpq.MpqHashTable.HashEntry.FILE_INDEX_DELETED)
                        Else
                            .Write(file_index_map(e.fileIndex))
                        End If
                    Next e
                End With

                'Write mpq files
                For i = 0 To fileTable.fileEntries.Count - 1
                    Dim u = CUInt(i)
                    If del_files.Contains(u) Then Continue For
                    w.Write(sep, 0, sep.Length)
                    file_streams(u).Seek(0, IO.SeekOrigin.Begin)
                    Dim bb = streamBytes(file_streams(u))
                    w.Write(bb, 0, CInt(file_streams(u).Length))
                Next i

                'Go back and write size
                w.BaseStream.Seek(sizePos, IO.SeekOrigin.Begin)
                w.Write(t)
                w.Close()
                stream.Close()
            End Using
        End Sub
    End Class
End Namespace
