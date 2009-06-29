Imports HostBot.Mpq.Crypt

Namespace Mpq
    '''<summary>
    '''The file table from an MPQ Archive.
    '''Each entry in the file table tells you where some file is and how to access it.
    '''</summary>
    Public Class MpqFileTable
        Public ReadOnly fileEntries As New List(Of FileEntry)
        Public ReadOnly parent As Mpq.MpqArchive

        Public Class FileEntry
            Public filePosition As UInteger 'Absolute position of the file within the parent file of the archive
            Public compressedSize As UInteger 'Size of the file data stored in the archive in bytes
            Public actualSize As UInteger 'Actual size of the file in bytes
            Public flags As FILE_FLAGS 'Properties of the file
            Public Enum FILE_FLAGS As UInteger
                IMPLODED = &H100
                COMPRESSED = &H200
                EXISTS = &H80000000L
                CONTINUOUS = &H1000000
                ADJUSTED_KEY = &H20000
                ENCRYPTED = &H10000
            End Enum
        End Class

        '''<summary>Reads the file table from an MPQ archive</summary>
        Public Sub New(ByVal mpqa As MpqArchive)
            Me.parent = mpqa

            'Read (with decryption)
            Using stream = mpqa.streamFactory()
                stream.Seek(mpqa.fileTablePosition, IO.SeekOrigin.Begin)
                Using br = New IO.BinaryReader( _
                            New IO.BufferedStream( _
                             New Cypherer(HashString("(block table)", HashType.FILE_KEY), Cypherer.modes.decrypt).ConvertReadOnlyStream(stream)))
                    For i = 0 To CInt(mpqa.numFileTableEntries) - 1
                        Dim f = New FileEntry()
                        f.filePosition = br.ReadUInt32()
                        f.compressedSize = br.ReadUInt32()
                        f.actualSize = br.ReadUInt32()
                        f.flags = CType(br.ReadUInt32(), FileEntry.FILE_FLAGS)
                        fileEntries.Add(f)
                    Next i
                End Using
            End Using

            'Correct positions from relative to absolute
            For Each entry In fileEntries
                entry.filePosition += mpqa.filePosition
            Next entry
        End Sub

        '''<summary>Throws an exception if the file table is invalid.</summary>
        Public Sub checkValidity()
            Dim end_of_archive = parent.archiveSize + parent.filePosition
            For Each entry In fileEntries
                If entry.filePosition < 0 OrElse entry.filePosition >= end_of_archive Then
                    Throw New MPQException("Invalid file table. [file entry has header position outside of archive]")
                ElseIf entry.compressedSize < 0 Then
                    Throw New MPQException("Invalid file table. [file entry has negative compressed size]")
                End If
            Next entry
        End Sub
    End Class
End Namespace
