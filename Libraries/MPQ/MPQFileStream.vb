Imports HostBot.MPQ.Crypt
Imports HostBot.MPQ.MPQFileTable
Imports HostBot.MPQ.MPQFileTable.FileEntry

Namespace MPQ
    ''' <summary>
    ''' Exposes an IO.Stream around a file stored in an MPQ Archive.
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
    Public Class MPQFileStream
        Inherits IO.Stream

        'Init State
        Public ReadOnly mpqa As MPQArchive 'The MPQ archive storing this file
        Public ReadOnly fileTableEntry As FileEntry 'The file's entry in the MPQ archive's file table
        Public ReadOnly numBlocks As UInteger 'The number of blocks the file is made up of
        Public ReadOnly decryptionKey As UInteger 'Computed from the filename or extracted from known block offset table value
        Public ReadOnly canDecrypt As Boolean = False 'Indicates whether or not the decryptionKey is known
        Private ReadOnly blockOffsetTable() As Integer = Nothing 'Starts/ends of the file blocks (stored in offset table if encrypted or compressed)
        Private ReadOnly baseStream As IO.Stream 'stream from mpq file

        'Reading State
        Private logical_position As UInteger = 0 'logical position within the decompressed file
        Private blockStream As IO.Stream 'current block stream
        Private numBlockBytesLeft As UInteger = 0 'bytes left to read in the current block stream

        Public Enum COMPRESSION_TYPES As Byte
            'listed in order of compression if multiple compressions applied
            IMA_ADPCM_MONO = &H40
            IMA_ADPCM_STEREO = &H80
            HUFFMAN_ENCODED = &H1
            ZLIB_DEFLATED = &H2
            PKWARE_IMPLODED = &H8
            BZIP2 = &H10
        End Enum

#Region "Life"
        '''<summary>Creates a stream for the file with the given name stored in the given archive.</summary>
        Public Sub New(ByVal parent As MPQArchive, ByVal fileName As String)
            Me.new(parent, parent.hashTable.hash(fileName).fileIndex, fileName)
        End Sub
        '''<summary>Creates a stream for the file with the given index stored in the given archive.</summary>
        Public Sub New(ByVal parent As MPQArchive, ByVal fileIndex As UInteger)
            Me.new(parent, fileIndex, Nothing)
        End Sub
        '''<summary>Creates a stream for the file with the given index, and uses the given name for decryption.</summary>
        '''<remarks>Can still compute the decryption key if the blockOffsetTable is stored in the file.</remarks>
        Private Sub New(ByVal mpqa As MPQArchive, ByVal fileIndex As UInteger, ByVal filename As String)
            If fileIndex >= mpqa.fileTable.fileEntries.Count Then Throw New IO.IOException("File ID not in File Table")

            Me.mpqa = mpqa
            Me.baseStream = New IO.BufferedStream(mpqa.streamFactory.make())
            fileTableEntry = mpqa.fileTable.fileEntries(CInt(fileIndex))
            numBlocks = CUInt(Math.Ceiling(fileTableEntry.actualSize / mpqa.fileBlockSize))
            Dim might_be_past_file = False

            'Sanity check
            If (fileTableEntry.flags And FILE_FLAGS.EXISTS) = 0 Then
                Throw New IO.IOException("File ID is in File Table, but is flagged as does not exist.")
            ElseIf fileTableEntry.filePosition > mpqa.archiveSize + mpqa.filePosition Then
                Throw New IO.IOException("File starts past the end of MPQ Archive")
            ElseIf fileTableEntry.filePosition + fileTableEntry.compressedSize > mpqa.archiveSize + mpqa.filePosition Then
                '[File seems to end past end of mpq archive, but it may have a negative offset table]
                If (fileTableEntry.flags And FILE_FLAGS.CONTINUOUS) <> 0 Or (fileTableEntry.flags And (FILE_FLAGS.COMPRESSED Or FILE_FLAGS.ENCRYPTED)) = 0 Then
                    '[No offset table]
                    Throw New IO.InvalidDataException("File passes the end of MPQ Archive")
                Else
                    '[wait until block offset table is loaded to check]
                    might_be_past_file = True
                End If
            End If

            'Check if filename supplied
            If filename IsNot Nothing Then
                canDecrypt = True
                decryptionKey = getFileDecryptionKey(filename, fileTableEntry, mpqa)
            End If

            'Read offset table
            If (fileTableEntry.flags And FILE_FLAGS.CONTINUOUS) = 0 Then
                If (fileTableEntry.flags And (FILE_FLAGS.COMPRESSED Or FILE_FLAGS.ENCRYPTED)) <> 0 Then
                    blockStream = baseStream
                    ReDim blockOffsetTable(0 To CInt(numBlocks))

                    baseStream.Seek(fileTableEntry.filePosition, IO.SeekOrigin.Begin)
                    Dim tableSize = CUInt(blockOffsetTable.Length * 4)

                    'Check for encryption [in case the flags are lying]
                    With New IO.BinaryReader(baseStream)
                        'first value in the offset table should be the size of the offset table
                        If .ReadUInt32() <> tableSize Then
                            'this file must be encrypted
                            fileTableEntry.flags = fileTableEntry.flags Or FILE_FLAGS.ENCRYPTED
                        End If
                    End With

                    'Decryption
                    baseStream.Seek(fileTableEntry.filePosition, IO.SeekOrigin.Begin)
                    If (fileTableEntry.flags And FILE_FLAGS.ENCRYPTED) <> 0 Then
                        If Not canDecrypt Then
                            'try to decrypt using known plaintext attack
                            decryptionKey = getFileDecryptionKey(baseStream, tableSize)
                            canDecrypt = True
                            decryptionKey += CByte(1) 'the key for a block is offset by the block number (offset table is considered block -1)
                        End If
                        'wrap
                        blockStream = New Cypherer(decryptionKey - CUInt(1), Cypherer.modes.decrypt).streamThroughFrom(blockStream)
                    End If

                    'Read
                    baseStream.Seek(fileTableEntry.filePosition, IO.SeekOrigin.Begin)
                    With New IO.BinaryReader(blockStream)
                        For block_index = 0 To blockOffsetTable.Length - 1
                            blockOffsetTable(block_index) = .ReadInt32()
                            If might_be_past_file Then
                                If CLng(fileTableEntry.filePosition) + blockOffsetTable(block_index) > mpqa.archiveSize Then
                                    Throw New IO.InvalidDataException("File passes the end of MPQ Archive")
                                End If
                            End If
                        Next block_index
                    End With
                End If
            End If
        End Sub
#End Region

#Region "Stream"
        '''<summary>Seeks to the start of a block and preps for reading it</summary>
        Private Sub gotoBlock(ByVal blockIndex As UInteger)
            'Seek
            logical_position = blockIndex * mpqa.fileBlockSize
            Dim block_position_offset As Long
            If (fileTableEntry.flags And FILE_FLAGS.CONTINUOUS) <> 0 Then
                If blockIndex > 0 Then Throw New IO.IOException("Attempted to read a second block of a continuous file (should only have 1 block).")
                block_position_offset = 0
            ElseIf blockOffsetTable IsNot Nothing Then
                block_position_offset = blockOffsetTable(CInt(blockIndex))
            Else
                block_position_offset = blockIndex * mpqa.fileBlockSize
            End If
            baseStream.Seek(block_position_offset + fileTableEntry.filePosition, IO.SeekOrigin.Begin)
            blockStream = baseStream
            numBlockBytesLeft = mpqa.fileBlockSize

            'Decryption layer
            If (fileTableEntry.flags And FILE_FLAGS.ENCRYPTED) <> 0 Then
                If Not canDecrypt Then Throw New IO.IOException("Couldn't decrypt MPQ block data.")
                blockStream = New MPQ.Crypt.Cypherer(decryptionKey + blockIndex, Cypherer.modes.decrypt).streamThroughFrom(blockStream)
            End If

            'Decompression layer
            If (fileTableEntry.flags And FILE_FLAGS.COMPRESSED) <> 0 Then
                If fileTableEntry.compressedSize - If(blockOffsetTable Is Nothing, 0, blockOffsetTable.Length * 4) <> fileTableEntry.actualSize Then
                    Dim header = CType(blockStream.ReadByte(), COMPRESSION_TYPES)

                    'BZIP2
                    If (header And COMPRESSION_TYPES.BZIP2) <> 0 Then
                        Throw New NotSupportedException("Don't know how to decompress BZIP2.")
                    End If

                    'PKWARE_IMPLODED
                    If (header And COMPRESSION_TYPES.PKWARE_IMPLODED) <> 0 Then
                        blockStream = New MPQ.Compression.PkWare.Decoder().streamThroughFrom(blockStream)
                        header = header And Not COMPRESSION_TYPES.PKWARE_IMPLODED
                    End If

                    'DEFLATE
                    If (header And COMPRESSION_TYPES.ZLIB_DEFLATED) <> 0 Then
                        '[skip header from old zlib format]
                        blockStream.ReadByte()
                        blockStream.ReadByte()

                        blockStream = New IO.Compression.DeflateStream(blockStream, IO.Compression.CompressionMode.Decompress)
                        header = header And Not COMPRESSION_TYPES.ZLIB_DEFLATED
                    End If

                    'HUFFMAN
                    If (header And COMPRESSION_TYPES.HUFFMAN_ENCODED) <> 0 Then
                        blockStream = New MPQ.Compression.Huffman.Decoder().streamThroughFrom(blockStream)
                        header = header And Not COMPRESSION_TYPES.HUFFMAN_ENCODED
                    End If

                    'STEREO WAVE
                    If (header And COMPRESSION_TYPES.IMA_ADPCM_STEREO) <> 0 Then
                        blockStream = New MPQ.Compression.Wave.Decoder(2).streamThroughFrom(blockStream)
                        header = header And Not COMPRESSION_TYPES.IMA_ADPCM_STEREO
                    End If

                    'MONO WAVE
                    If (header And COMPRESSION_TYPES.IMA_ADPCM_MONO) <> 0 Then
                        blockStream = New MPQ.Compression.Wave.Decoder(1).streamThroughFrom(blockStream)
                        header = header And Not COMPRESSION_TYPES.IMA_ADPCM_MONO
                    End If

                    'Unknown Compression
                    If header <> 0 Then
                        Throw New IO.IOException("Don't know how to decompress Unknown Compression.")
                    End If
                End If
            End If
        End Sub

        Public Overrides ReadOnly Property Length() As Long
            Get
                Return fileTableEntry.actualSize
            End Get
        End Property

        '''<summary>The position in the decompressed/decrypted file.</summary>
        Public Overrides Property Position() As Long
            Get
                Return logical_position
            End Get
            Set(ByVal value As Long)
                'Check for invalid positions
                If value < 0 Then Throw New InvalidOperationException("Position can't go before beginning of stream.")
                If value > Length Then Throw New InvalidOperationException("Position can't go past end of stream.")
                'Go to position within block
                gotoBlock(CUInt(value \ mpqa.fileBlockSize))
                Dim offset = CInt(value Mod mpqa.fileBlockSize)
                If offset <> 0 Then
                    If blockStream.CanSeek Then
                        blockStream.Seek(offset, IO.SeekOrigin.Current)
                    Else
                        Dim bb(0 To offset - 1) As Byte
                        blockStream.Read(bb, 0, offset)
                    End If
                End If
            End Set
        End Property

        Public Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
            If origin = IO.SeekOrigin.Current Then offset += Position
            If origin = IO.SeekOrigin.End Then offset += Length
            Position = offset
        End Function

        Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
            Dim numCopied = 0
            While numCopied < count And Position < Length
                'Go to next block when the current one finishes
                If numBlockBytesLeft <= 0 Then
                    gotoBlock(CUInt(Position \ mpqa.fileBlockSize))
                    If (fileTableEntry.flags And FILE_FLAGS.CONTINUOUS) <> 0 Then numBlockBytesLeft = CUInt(Length - Position)
                End If

                'Delegate read to block stream
                Dim n = blockStream.Read(buffer, offset, Math.Min(CInt(Length - Position), Math.Min(count - numCopied, CInt(numBlockBytesLeft))))
                If n <= 0 Then
                    n = count - numCopied
                    Debug.Print("Weird Error: " + n.ToString() + " bytes missing from block.") '[one of the lich hero pissed .wavs sets this off]
                End If

                'Update state
                logical_position += CUInt(n)
                offset += n
                numCopied += n
                numBlockBytesLeft -= CUInt(n)
            End While
            Return CInt(numCopied)
        End Function

        Public Overrides ReadOnly Property CanRead() As Boolean
            Get
                Return True
            End Get
        End Property
        Public Overrides ReadOnly Property CanSeek() As Boolean
            Get
                Return True
            End Get
        End Property
        Public Overrides ReadOnly Property CanWrite() As Boolean
            Get
                Return False
            End Get
        End Property
        Public Overrides Sub Flush()
        End Sub
        Public Overrides Sub SetLength(ByVal value As Long)
            Throw New NotSupportedException()
        End Sub
        Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
            Throw New NotSupportedException()
        End Sub
        Public Overrides Sub Close()
            If blockStream IsNot Nothing Then blockStream.Close()
            MyBase.Close()
        End Sub
#End Region
    End Class
End Namespace
