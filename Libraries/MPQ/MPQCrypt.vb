Imports HostBot.Mpq

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
Namespace Mpq.Crypt
    '''<summary>Implements a Converter for MPQ encryption and decryption</summary>
    Public Class Cypherer
        Implements IConverter(Of Byte)
        Private ReadOnly decrypt As Boolean
        Private ReadOnly initialKey1 As ModInt32
        Private Shared ReadOnly initialKey2 As ModInt32 = &HEEEEEEEE
        Private ReadOnly bytes(0 To 3) As Byte
        Public Enum modes
            encrypt
            decrypt
        End Enum

        Public Sub New(ByVal key As ModInt32, ByVal mode As modes)
            Me.initialKey1 = key
            Select Case mode
                Case modes.encrypt
                    Me.decrypt = False
                Case modes.decrypt
                    Me.decrypt = True
                Case Else
                    Throw New UnreachableException
            End Select
        End Sub

        Public Function Convert(ByVal sequence As IEnumerator(Of Byte)) As IEnumerator(Of Byte) Implements IConverter(Of Byte).Convert
            Dim k1 = initialKey1
            Dim k2 = initialKey2
            Dim T = cryptTable(HashType.ENCRYPT)
            Return New Enumerator(Of Byte)(
                Function(controller)
                    If Not sequence.MoveNext Then  Return controller.Break()
                    bytes(0) = sequence.Current
                    For i = 1 To 3
                        If Not sequence.MoveNext Then  Return controller.Multiple(bytes.SubArray(0, i))
                        bytes(i) = sequence.Current
                    Next i

                    Dim v As ModInt32 = bytes.ToUInteger(ByteOrder.LittleEndian)
                    Dim s = T(k1 And &HFF)
                    Dim c = v Xor (k1 + k2 + s)

                    k1 = (k1 >> 11) Or (((Not k1) << 21) + &H11111111) '[vulnerability: causes k1 to lose entropy via bits being forced set]
                    k2 = If(decrypt, c, v) + (k2 + s) * 33 + 3

                    Return controller.Multiple(CUInt(c).bytes(ByteOrder.LittleEndian))
                End Function)
        End Function
    End Class

    Module Common
        Public Enum HashType As Integer
            HASH_TABLE_OFFSET = 0
            NAME_A = 1
            NAME_B = 2
            FILE_KEY = 3
            ENCRYPT = 4
        End Enum
        Friend ReadOnly cryptTable As Dictionary(Of HashType, ModInt32()) = computeCryptTable()

        '''<summary>Creates the encryption table used for MPQ data</summary>
        Private Function computeCryptTable() As Dictionary(Of HashType, ModInt32())
            Const TableSize As Integer = 256 * 5
            Dim table(0 To TableSize - 1) As ModInt32
            Dim k As ModInt32 = &H100001
            Dim pos = 0

            While table(pos) = 0 '[every value in the table will have been initialized when this condition is no longer met]
                For word = 1 To 2
                    k = CUInt(k * 125 + 3) Mod 2796203UI
                    table(pos) <<= 16 '[don't overwrite value from first iteration]
                    table(pos) = table(pos) Or (k And &HFFFF)
                Next word

                pos += 256
                If pos > TableSize - 1 Then pos -= TableSize - 1
            End While

            Dim d = New Dictionary(Of HashType, ModInt32())
            For Each h In EnumValues(Of HashType)()
                d(h) = table.SubArray(CInt(h) * 256, 256)
            Next h
            Return d
        End Function

        '''<summary>Hashes a string into a key</summary>
        Public Function HashString(ByVal s As String, ByVal hashType As HashType) As ModInt32
            Dim k1 As ModInt32 = &H7FED7FED
            Dim k2 As ModInt32 = &HEEEEEEEE
            Dim T = cryptTable(hashType)
            For Each b In (From c In s.ToUpper() Select CByte(Asc(c)))
                k1 = (k1 + k2) Xor T(b)
                k2 = b + k1 + k2 * 33 + 3
            Next b
            Return k1
        End Function

        '''<summary>Computes the hash of a file name</summary>
        '''<remarks>Used to determine where a file is stored in the hash table</remarks>
        Public Function HashFileName(ByVal s As String) As UInt64
            Return CULng(HashString(s, HashType.NAME_A)) Or CULng(HashString(s, HashType.NAME_B)) << 32
        End Function

        '''<summary>Computes the decryption key of a file with known filename</summary>
        Public Function GetFileDecryptionKey(ByVal fileName As String, ByVal fileTableEntry As MpqFileTable.FileEntry, ByVal mpqa As MpqArchive) As ModInt32
            Dim key = HashString(GetFileNameSlash(fileName), HashType.FILE_KEY) 'key from hashed file name [without folders]

            'adjusted keys are offset by the file position
            If (fileTableEntry.flags And MpqFileTable.FileEntry.FILE_FLAGS.ADJUSTED_KEY) <> 0 Then
                key = fileTableEntry.actualSize Xor key + fileTableEntry.filePosition - mpqa.filePosition
            End If

            Return key
        End Function

        '''<summary>Attempts to recover the decryption key of a file using a known plaintext attack</summary>
        '''<remarks>
        '''Encryption:
        '''   seed1 = *VALUE_TO_FIND*
        '''   seed2 = 0xEEEEEEEEL
        '''   seed2b = seed2 + T[seed1 and 0xFF]
        '''   encryptedByte1 = targetByte1 Xor (seed1 + seed2b)
        '''Decryption:
        '''   Let s = encryptedByte1 xor targetByte1
        '''   Notice s = seed1 + seed2b
        '''   Notice s = seed1 + seed2 + T[seed1 and 0xFF]
        '''   Let n = s - seed2
        '''   Notice n = seed1 + T[seed1 and 0xFF]
        '''   Notice seed1 = n - T[seed1 and 0xFF]
        '''   Notice the right side has only 256 possible values because seed1 is AND-ed with 0xFF
        '''   Brute force seed1 by trying every possible value of (seed1 and 0xHFF) in the right side
        '''</remarks>
        Public Function BreakFileDecryptionKey(ByVal encryptedStream As IO.Stream, ByVal target1Value As UInt32) As ModInt32
            'Prep
            Dim T = cryptTable(HashType.ENCRYPT)
            Dim e1 As ModInt32, e2 As ModInt32
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
            Dim k2 As ModInt32 = &HEEEEEEEE
            Dim s = e1 Xor target1Value 'undo xor
            Dim n = s - k2 'undo addition

            'Brute force value of k1 by trying all possible values of (k1 & 0xFF)
            Dim found_min = False
            Dim minKey As ModInt32
            Dim minVal As ModInt32
            For possible_byte = 0 To &HFF
                Dim k1 = n - T(possible_byte)
                If (k1 And &HFF) <> possible_byte Then Continue For 'doesn't satisfy basic constraint

                m.Seek(0, IO.SeekOrigin.Begin)
                With New IO.BinaryReader(New Cypherer(k1, Cypherer.modes.decrypt).ConvertReadOnlyStream(m))
                    'check decryption for correctness
                    If .ReadUInt32() <> target1Value Then Continue For 'doesn't match plaintext
                    'keep track of key with lowest second value [lower values are more likely plaintexts]
                    Dim u = .ReadUInt32()
                    If Not found_min OrElse CUInt(u) < CUInt(minVal) Then
                        minKey = k1
                        minVal = u
                        found_min = True
                    End If
                End With
            Next possible_byte

            If Not found_min Then Throw New IO.InvalidDataException("No possible decryption key for provided plaintext.")
            Return minKey
        End Function
    End Module
End Namespace
