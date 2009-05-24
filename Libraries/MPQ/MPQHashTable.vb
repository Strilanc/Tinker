Imports HostBot.MPQ.Crypt

Namespace MPQ
    '''<summary>
    '''The hashtable from an MPQ archive.
    '''Each entry tells you a file index and the hash of the file's original name.
    '''The hashtable doesn't tell you a file's original name, only what it hashes to.
    '''</summary>
    Public Class MPQHashTable
        Public ReadOnly hashes As New List(Of HashEntry)
        Public ReadOnly parent As MPQ.MPQArchive

        Public Class HashEntry
            Public Const FILE_INDEX_EMPTY As UInteger = &HFFFFFFFFL
            Public Const FILE_INDEX_DELETED As UInteger = &HFFFFFFFEL

            Public key As ULong 'hashed file name
            Public language As UInteger 'LANG_ID
            Public fileIndex As UInteger 'index in file table
        End Class

#Region "Life"
        '''<summary>Reads the hashtable from an MPQ archive</summary>
        Public Sub New(ByVal mpqa As MPQArchive)
            Me.parent = mpqa

            'Prepare reader
            Using stream = mpqa.streamFactory.make()
                stream.Seek(mpqa.hashTablePosition, IO.SeekOrigin.Begin)
                Using br = New IO.BinaryReader( _
                            New IO.BufferedStream( _
                             New Cypherer(HashString("(hash table)", HashType.FILE_KEY), Cypherer.modes.decrypt).streamThroughFrom(stream)))

                    'Read values
                    For repeat = CUInt(1) To mpqa.numHashTableEntries
                        Dim h As New HashEntry()
                        h.key = br.ReadUInt64()
                        h.language = br.ReadUInt32()
                        h.fileIndex = br.ReadUInt32()
                        hashes.Add(h)
                    Next repeat
                End Using
            End Using
        End Sub
#End Region

#Region "Hash"
        Private Function getHash(ByVal filename As String) As HashEntry
            Dim key = HashFileName(filename)
            Dim start_index = CInt(HashString(filename, HashType.HASH_TABLE_OFFSET) Mod hashes.Count)
            Dim first_empty_entry As HashEntry = Nothing
            For offset_index = 0 To hashes.Count - 1
                Dim cur_entry = hashes((offset_index + start_index) Mod hashes.Count)
                If cur_entry.fileIndex = HashEntry.FILE_INDEX_EMPTY Or cur_entry.fileIndex = HashEntry.FILE_INDEX_DELETED Then
                    If first_empty_entry Is Nothing Then first_empty_entry = cur_entry
                    If cur_entry.fileIndex = HashEntry.FILE_INDEX_EMPTY Then Exit For
                    If cur_entry.fileIndex = HashEntry.FILE_INDEX_DELETED Then Continue For
                End If
                If cur_entry.key = key Then
                    If hashes((offset_index + start_index + 1) Mod hashes.Count).key = key Then
                        'This mpq is protected, the first file is a fake to cause WE to crash, but wc3 skips it
                        Continue For
                    End If

                    Dim invalid = True
                    If cur_entry.fileIndex <> HashEntry.FILE_INDEX_EMPTY Then invalid = False
                    If cur_entry.fileIndex = HashEntry.FILE_INDEX_DELETED Then invalid = False
                    If cur_entry.language = HashEntry.FILE_INDEX_EMPTY Then invalid = False
                    If cur_entry.fileIndex >= 0 AndAlso cur_entry.fileIndex < parent.numFileTableEntries Then invalid = False
                    If invalid Then
                        Throw New MPQException("Invalid MPQ hash table entry accessed. The entry's file index points outside the file table.")
                    End If
                    Return cur_entry
                End If
            Next offset_index
            Return first_empty_entry
        End Function

        '''<summary>
        '''Returns the MPQHash corresponding to the given filename.
        '''Throws an exception if there is no hash for the filename
        '''</summary>
        Public Function hash(ByVal filename As String) As HashEntry
            If Not contains(filename) Then Throw New IO.IOException("Filekey not in Hash Table")
            Return getHash(filename)
        End Function

        Public Function contains(ByVal filename As String) As Boolean
            Dim h = getHash(filename)
            If h Is Nothing Then Return False
            If h.language = HashEntry.FILE_INDEX_EMPTY Or h.language = HashEntry.FILE_INDEX_DELETED Then Return False
            If h.fileIndex = HashEntry.FILE_INDEX_EMPTY Or h.fileIndex = HashEntry.FILE_INDEX_DELETED Then Return False
            Return True
        End Function

        Public Function getEmpty(ByVal filename As String) As HashEntry
            If contains(filename) Then Throw New IO.IOException("Filekey already in Hash Table")
            Return getHash(filename)
        End Function
#End Region
    End Class
End Namespace