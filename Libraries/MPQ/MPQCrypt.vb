Imports HostBot.MPQ

''''<summary>
''''Encapsulates the decryption process used in MPQ files.
''''</summary>
''''<copyright>
''''Copyright (C) 2008 Craig Gidney, craig.gidney@gmail.com
''''
''''This source was adepted from the C version of mpqlib.
''''The C version belongs to the following authors,
''''
''''Maik Broemme, mbroemme@plusserver.de
''''
''''This program is free software; you can redistribute it and/or modify
''''it under the terms of the GNU General Public License as published by
''''the Free Software Foundation; either version 2 of the License, or
''''(at your option) any later version.
''''
''''This program is distributed in the hope that it will be useful,
''''but WITHOUT ANY WARRANTY; without even the implied warranty of
''''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''''GNU General Public License for more details.
''''
''''You should have received a copy of the GNU General Public License
''''along with this program; if not, write to the Free Software
''''Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
''''</copyright>
Namespace MPQ.Crypt
    '''<summary>Implements a Block Converter for MPQ encryption and decryption</summary>
    Public Class Cypherer
        Implements IBlockConverter

        Public Enum modes
            encrypt
            decrypt
        End Enum

        Private ReadOnly inBitBuffer As New BitBuffer
        Private ReadOnly outBitBuffer As New BitBuffer
        Private ReadOnly decrypt As Boolean
        Private k1 As UInt64 'cypher state [outside key initializes k1]
        Private k2 As UInt64 = &HEEEEEEEEL 'cypher state [the ks are UInt32s, but stored in UInt64s to make dealing with overflows easier]

        Public Sub New(ByVal key As UInt32, ByVal mode As modes)
            Me.k1 = key
            Select Case mode
                Case modes.encrypt
                    Me.decrypt = False
                Case modes.decrypt
                    Me.decrypt = True
                Case Else
                    Throw New ArgumentException("Unreconized mode.", "mode")
            End Select
        End Sub

        Public Function needs(ByVal outputSize As Integer) As Integer Implements IBlockConverter.needs
            Return alignedReadCount(outputSize, inBitBuffer.numBits \ 8, outBitBuffer.numBits \ 8, 4)
        End Function

        Public Sub convert(ByVal ReadView As ReadOnlyArrayView(Of Byte),
                           ByVal WriteView As ArrayView(Of Byte),
                           ByRef OutReadCount As Integer,
                           ByRef OutWriteCount As Integer) _
                           Implements IBlockConverter.convert
            OutWriteCount = 0
            OutReadCount = 0

            Do
                'Write word
                For i = 1 To outBitBuffer.numBytes
                    If OutWriteCount >= WriteView.length Then Exit Do
                    WriteView(OutWriteCount) = outBitBuffer.takeByte()
                    OutWriteCount += 1
                Next i

                'Read word
                For i = inBitBuffer.numBytes To 4 - 1
                    If OutReadCount >= ReadView.length Then Exit Do
                    inBitBuffer.queueByte(ReadView(OutReadCount))
                    OutReadCount += 1
                Next i

                'Cypher word
                outBitBuffer.queueUInteger(cypher(inBitBuffer.takeUInteger()))
            Loop
        End Sub

        Private Function cypher(ByVal v As UInteger) As UInteger
            k2 = uCUInt(k2 + cryptTable(HashType.ENCRYPT * &H100 + CInt(k1 And CByte(&HFF)))) '[give k2 entropy from k1]
            cypher = v Xor uCUInt(k1 + k2) '[give v entropy from k1 and k2]
            k1 = uCUInt((((Not CUInt(k1)) << 21) + CULng(&H11111111)) Or (k1 >> 11)) '[cycle k1, loses entropy because some bits get forced set]
            k2 = uCUInt(If(decrypt, cypher, v) + k2 * CULng(33) + CULng(3)) '[give k2 entropy from v]
        End Function
    End Class

    Module Common
        Friend ReadOnly cryptTable() As UInt32 = computeCryptTable()

        '''<summary>Creates the encryption table used for MPQ data</summary>
        Private Function computeCryptTable() As UInt32()
            Const N As Integer = 256 * 5 'table size
            Dim T(0 To N - 1) As UInt32 'table
            Dim k As UInt32 = &H100001 'key generator
            Dim p = 0 'position

            While T(p) = 0 '[every value in the table will have been initialized when this condition is no longer met]
                For repeat = 1 To 2
                    k = CUInt((k * 125 + 3) Mod 2796203)
                    T(p) <<= 16 '[don't overwrite value from first iteration]
                    T(p) = T(p) Or CUInt(k And &HFFFF)
                Next repeat

                p += 256
                If p > N - 1 Then p -= N - 1
            End While

            Return T
        End Function

        Public Enum HashType As Byte
            HASH_TABLE_OFFSET = 0
            NAME_A = 1
            NAME_B = 2
            FILE_KEY = 3
            ENCRYPT = 4
        End Enum

        '''<summary>Hashes a string into a key</summary>
        Public Function HashString(ByVal s As String, ByVal hashType As HashType) As UInt32
            Dim k1 As UInt64 = &H7FED7FEDL
            Dim k2 As UInt64 = &HEEEEEEEEL

            For Each c In s.ToUpper()
                Dim b = CByte(Asc(c))
                k1 = uCUInt(k1 + k2) Xor cryptTable(hashType * &H100 + b)
                k2 = uCUInt(b + k1 + k2 * CByte(33) + CByte(3))
            Next c
            Return CUInt(k1)
        End Function

        '''<summary>Computes the hash of a file name</summary>
        '''<remarks>Used to determine where a file is stored in the hash table</remarks>
        Public Function HashFileName(ByVal s As String) As UInt64
            Return HashString(s, HashType.NAME_A) Or (CULng(HashString(s, HashType.NAME_B)) << 32)
        End Function

        '''<summary>Computes the decryption key of a file with known filename</summary>
        Public Function getFileDecryptionKey(ByVal fileName As String, ByVal fileTableEntry As MPQFileTable.FileEntry, ByVal mpqa As MPQArchive) As UInt32
            Dim key = HashString(getFileNameSlash(fileName), HashType.FILE_KEY) 'key from hashed file name [without folders]

            'adjusted keys are offset by the file position
            If (fileTableEntry.flags And MPQFileTable.FileEntry.FILE_FLAGS.ADJUSTED_KEY) <> 0 Then
                key = uCUInt((key + CULng(fileTableEntry.filePosition) - mpqa.filePosition) Xor fileTableEntry.actualSize)
            End If

            Return key
        End Function

        '''<summary>Attempts to recover the decryption key of a file using a known plaintext attack</summary>
        '''<remarks>
        '''Encryption:
        '''   seed1 = *VALUE_TO_FIND*
        '''   seed2 = 0xEEEEEEEEL
        '''   seed2b = seed2 + cryptTable[0x400 + (seed1 and 0xFF)]
        '''   encryptedByte1 = targetByte1 Xor (seed1 + seed2b)
        '''Decryption:
        '''   Let s = encryptedByte1 xor targetByte1
        '''   Notice s = seed1 + seed2b
        '''   Notice s = seed1 + seed2 + cryptTable[0x400 + (seed1 and 0xFF)]
        '''   Let n = s - seed2
        '''   Notice n = seed1 + cryptTable[0x400 + (seed1 and 0xFF)]
        '''   Notice seed1 = n - cryptTable[0x400 + (seed1 and 0xFF)]
        '''   Notice the right side has only 256 possible values because seed1 is AND-ed with 0xFF
        '''   Brute force seed1 by trying every possible value of (seed1 and 0xHFF) in the right side
        '''</remarks>
        Public Function getFileDecryptionKey(ByVal encryptedStream As IO.Stream, ByVal target1Value As UInt32) As UInt32
            'Prep
            Dim e1 As UInt32, e2 As UInt32
            With New IO.BinaryReader(encryptedStream)
                e1 = .ReadUInt32()
                e2 = .ReadUInt32()
            End With
            Dim m = New IO.MemoryStream(8)
            With New IO.BinaryWriter(m)
                .Write(e1)
                .Write(e2)
            End With
            'Initial values
            Dim k2 As UInt32 = &HEEEEEEEEL
            Dim s = e1 Xor target1Value 'undo xor
            Dim n = uCUInt(CLng(s) - k2) 'undo addition

            'Brute force value of k1 by trying all possible values of (k1 & 0xFF)
            Dim found_min = False
            Dim min_key = CUInt(0)
            Dim min_val = CUInt(0)
            For possible_byte = 0 To &HFF
                Dim k1 = uCUInt(CLng(n) - cryptTable(&H400 + possible_byte))
                If (k1 And &HFF) <> possible_byte Then Continue For 'doesn't satisfy basic constraint

                m.Seek(0, IO.SeekOrigin.Begin)
                With New IO.BinaryReader(New Cypherer(k1, Cypherer.modes.decrypt).streamThroughFrom(m))
                    'check decryption for correctness
                    If .ReadUInt32() <> target1Value Then Continue For 'doesn't match plaintext
                    'keep track of key with lowest second value [lower values are more likely plaintexts]
                    Dim u = .ReadUInt32()
                    If Not found_min OrElse u < min_val Then
                        min_key = k1
                        min_val = u
                        found_min = True
                    End If
                End With
            Next possible_byte

            If Not found_min Then Throw New IO.InvalidDataException("No possible decryption key for provided plaintext.")
            Return CUInt(min_key)
        End Function
    End Module
End Namespace
