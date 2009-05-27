''''MPQPkWare.vb - explode function of PKWARE data compression library.
''''
''''Copyright (C) 2008 Craig Gidney <craig.gidney@gmail.com>
''''
''''This source was adepted from the C version of explode.c.
''''The C version belongs to the following authors,
''''
''''Maik Broemme <mbroemme@plusserver.de>
''''
''''This source was adepted from the C++ version of pkware.cpp included
''''in stormlib. The C++ version belongs to the following authors,
''''
''''Ladislav Zezula <ladik.zezula.net>
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
''''
''''===== original copyright notice included in code =====
''''PKWARE Data Compression Library for Win32
''''Copyright 1989-1995 PKWARE Inc.  All Rights Reserved
''''Patent No. 5,051,745
''''PKWARE Data Compression Library Reg. U.S. Pat. and Tm. Off.
''''Version 1.11
''''======================================================

Namespace MPQ.Compression.PkWare
    Public Class Decoder
        Implements IBlockConverter
#Region "Data"
        Private Shared ReadOnly TREE_JUMP_LENGTH As New CodeTree( _
                New UShort() { _
                        &H3, &HD, &H5, &H19, &H9, &H11, &H1, &H3E, &H1E, &H2E, &HE, &H36, &H16, &H26, &H6, &H3A,
                        &H1A, &H2A, &HA, &H32, &H12, &H22, &H42, &H2, &H7C, &H3C, &H5C, &H1C, &H6C, &H2C, &H4C, &HC,
                        &H74, &H34, &H54, &H14, &H64, &H24, &H44, &H4, &H78, &H38, &H58, &H18, &H68, &H28, &H48, &H8,
                        &HF0, &H70, &HB0, &H30, &HD0, &H50, &H90, &H10, &HE0, &H60, &HA0, &H20, &HC0, &H40, &H80, &H0 _
                }, New Byte() { _
                         2, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                         7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8 _
                })
        Private Shared ReadOnly TREE_COPY_LENGTH As New CodeTree( _
                    New UShort() {5, 3, 1, 6, 10, 2, 12, 20, 4, 24, 8, 48, 16, 32, 64, 0},
                    New Byte() {3, 2, 3, 3, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 7, 7})

        Private Shared ReadOnly TREE_ASCII As New CodeTree( _
                New UShort() { _
                    &H490, &HFE0, &H7E0, &HBE0, &H3E0, &HDE0, &H5E0, &H9E0, &H1E0, &HB8, &H62, &HEE0, &H6E0, &H22, &HAE0, &H2E0,
                    &HCE0, &H4E0, &H8E0, &HE0, &HF60, &H760, &HB60, &H360, &HD60, &H560, &H1240, &H960, &H160, &HE60, &H660, &HA60,
                    &HF, &H250, &H38, &H260, &H50, &HC60, &H390, &HD8, &H42, &H2, &H58, &H1B0, &H7C, &H29, &H3C, &H98,
                    &H5C, &H9, &H1C, &H6C, &H2C, &H4C, &H18, &HC, &H74, &HE8, &H68, &H460, &H90, &H34, &HB0, &H710,
                    &H860, &H31, &H54, &H11, &H21, &H17, &H14, &HA8, &H28, &H1, &H310, &H130, &H3E, &H64, &H1E, &H2E,
                    &H24, &H510, &HE, &H36, &H16, &H44, &H30, &HC8, &H1D0, &HD0, &H110, &H48, &H610, &H150, &H60, &H88,
                    &HFA0, &H7, &H26, &H6, &H3A, &H1B, &H1A, &H2A, &HA, &HB, &H210, &H4, &H13, &H32, &H3, &H1D,
                    &H12, &H190, &HD, &H15, &H5, &H19, &H8, &H78, &HF0, &H70, &H290, &H410, &H10, &H7A0, &HBA0, &H3A0,
                    &H240, &H1C40, &HC40, &H1440, &H440, &H1840, &H840, &H1040, &H40, &H1F80, &HF80, &H1780, &H780, &H1B80, &HB80, &H1380,
                    &H380, &H1D80, &HD80, &H1580, &H580, &H1980, &H980, &H1180, &H180, &H1E80, &HE80, &H1680, &H680, &H1A80, &HA80, &H1280,
                    &H280, &H1C80, &HC80, &H1480, &H480, &H1880, &H880, &H1080, &H80, &H1F00, &HF00, &H1700, &H700, &H1B00, &HB00, &H1300,
                    &HDA0, &H5A0, &H9A0, &H1A0, &HEA0, &H6A0, &HAA0, &H2A0, &HCA0, &H4A0, &H8A0, &HA0, &HF20, &H720, &HB20, &H320,
                    &HD20, &H520, &H920, &H120, &HE20, &H620, &HA20, &H220, &HC20, &H420, &H820, &H20, &HFC0, &H7C0, &HBC0, &H3C0,
                    &HDC0, &H5C0, &H9C0, &H1C0, &HEC0, &H6C0, &HAC0, &H2C0, &HCC0, &H4C0, &H8C0, &HC0, &HF40, &H740, &HB40, &H340,
                    &H300, &HD40, &H1D00, &HD00, &H1500, &H540, &H500, &H1900, &H900, &H940, &H1100, &H100, &H1E00, &HE00, &H140, &H1600,
                    &H600, &H1A00, &HE40, &H640, &HA40, &HA00, &H1200, &H200, &H1C00, &HC00, &H1400, &H400, &H1800, &H800, &H1000, &H0 _
                }, New Byte() { _
                    &HB, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &H8, &H7, &HC, &HC, &H7, &HC, &HC,
                    &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HD, &HC, &HC, &HC, &HC, &HC,
                    &H4, &HA, &H8, &HC, &HA, &HC, &HA, &H8, &H7, &H7, &H8, &H9, &H7, &H6, &H7, &H8,
                    &H7, &H6, &H7, &H7, &H7, &H7, &H8, &H7, &H7, &H8, &H8, &HC, &HB, &H7, &H9, &HB,
                    &HC, &H6, &H7, &H6, &H6, &H5, &H7, &H8, &H8, &H6, &HB, &H9, &H6, &H7, &H6, &H6,
                    &H7, &HB, &H6, &H6, &H6, &H7, &H9, &H8, &H9, &H9, &HB, &H8, &HB, &H9, &HC, &H8,
                    &HC, &H5, &H6, &H6, &H6, &H5, &H6, &H6, &H6, &H5, &HB, &H7, &H5, &H6, &H5, &H5,
                    &H6, &HA, &H5, &H5, &H5, &H5, &H8, &H7, &H8, &H8, &HA, &HB, &HB, &HC, &HC, &HC,
                    &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD,
                    &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD,
                    &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD,
                    &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC,
                    &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC,
                    &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC, &HC,
                    &HD, &HC, &HD, &HD, &HD, &HC, &HD, &HD, &HD, &HC, &HD, &HD, &HD, &HD, &HC, &HD,
                    &HD, &HD, &HC, &HC, &HC, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD _
                })
#End Region

#Region "Inner"
        '''<summary>Maps sequences of bits to characters.</summary>
        Private Class CodeTree
            Public Class CodeNode
                Public left As CodeNode
                Public right As CodeNode
                Public val As Integer
                Public leaf As Boolean
            End Class

            Public ReadOnly root As New CodeNode()

            '''<summary>Generates a code-tree with each character code(i) at level lengths(i)</summary>
            Public Sub New(ByVal codes() As UShort, ByVal lengths() As Byte)
                If Not (codes IsNot Nothing) Then Throw New ArgumentException()
                If Not (lengths IsNot Nothing) Then Throw New ArgumentException()
                If codes.Length <> lengths.Length Then Throw New ArgumentException("Must have the same number of codes and lengths.")
                Dim num_leaves = 0
                Dim num_nodes = 1

                For i = 0 To codes.Length - 1
                    Dim n = root
                    Dim c = codes(i)
                    For repeat = 0 To lengths(i) - 1
                        'Mark current node as an internal node
                        If n.leaf Then
                            Throw New ArgumentException("The path for {0} passes through the leaf for {1}.".frmt(n.val, i))
                        End If
                        If n.left Is Nothing Then
                            n.left = New CodeNode()
                            num_nodes += 1
                        End If
                        If n.right Is Nothing Then
                            n.right = New CodeNode()
                            num_nodes += 1
                        End If

                        'Travel to next child
                        n = If((c And 1) <> 0, n.left, n.right)
                        c >>= 1
                    Next repeat

                    'Assign value to the selected leaf
                    If n.leaf Then
                        Throw New ArgumentException("The code for {0} and {1} are the same.".frmt(i, n.val))
                    ElseIf n.left IsNot Nothing OrElse n.right IsNot Nothing Then
                        Throw New ArgumentException("The code for {0} terminates on an internal node.".frmt(i))
                    End If
                    n.val = i
                    n.leaf = True
                    num_leaves += 1
                Next i

                'Check that all leaves are coded
                If num_leaves * 2 <> num_nodes + 1 Then
                    Throw New ArgumentException("There are non-terminated paths in the code tree. It is non-optimal.")
                End If
            End Sub
        End Class

        Public Enum modes As Byte
            binary = 0
            ascii = 1
        End Enum
#End Region

#Region "Variables"
        Private initialized As Boolean = False
        Private mode As modes
        Private num_extra_jump_bits As Integer

        'The memory buffer is used for look-back run encoding
        Private memory_buf(0 To &H3000 - 1) As Byte
        Private memory_pos_w As Integer = &H1000
        Private memory_pos_r As Integer = &H1000

        'The bit buffer is used for buffering input instructions
        Private buffer As New BitBuffer()
        Private backup_buffer As New BitBuffer() 'stores used instruction bits in case of need-more-data
        Private last_val As Byte 'the last unbuffered value
#End Region

#Region "Process"
        '''<summary>Travels down to the leaf of a coding tree, unbuffering bits for direction.</summary>
        Private Function tree_unbuffer(ByVal c As CodeTree) As Boolean
            Dim n = c.root
            While Not n.leaf
                If Not unbuffer(1) Then Return False
                n = If(last_val = 1, n.left, n.right)
            End While
            last_val = CByte(n.val)
            Return True
        End Function

        '''<summary>
        '''Takes some bits from the bit buffer, backs them up, and stores them in last_val.
        '''If there are not enough bits, restores the backup and returns false.
        '''</summary>
        Private Function unbuffer(ByVal n As Integer) As Boolean
            If n > 8 Then Throw New ArgumentOutOfRangeException("n")
            If buffer.numBits < n Then
                'not enough data! restore and abort
                Dim nb = backup_buffer.numBits
                buffer.stack(backup_buffer.take(nb), nb)
                Return False
            End If

            'unbuffer and backup
            last_val = CByte(buffer.take(n))
            backup_buffer.queue(last_val, n)
            Return True
        End Function

        Public Sub convert(ByVal ReadView As ReadOnlyArrayView(Of Byte),
                           ByVal WriteView As ArrayView(Of Byte),
                           ByRef OutReadCount As Integer,
                           ByRef OutWriteCount As Integer) _
                           Implements IBlockConverter.convert

            'Initialize
            If Not initialized Then
                While buffer.numBits < 16
                    If OutReadCount >= ReadView.length Then Return
                    buffer.queueByte(ReadView(OutReadCount))
                    OutReadCount += 1
                End While

                'Mode
                mode = CType(buffer.takeByte(), Decoder.modes)
                Select Case mode
                    Case modes.binary, modes.ascii
                    Case Else
                        Throw New IO.InvalidDataException("PkWare Stream has unrecognized mode.")
                End Select

                'Dictionary size
                num_extra_jump_bits = buffer.takeByte()
                If num_extra_jump_bits < 4 OrElse num_extra_jump_bits > 6 Then
                    Throw New IO.InvalidDataException("PkWare Stream specified an invalid number of jump bits.")
                End If

                initialized = True
            End If

            'Decompress
            Do
                'Cleanup previous operations
                backup_buffer.clear()
                While memory_pos_r < memory_pos_w AndAlso OutWriteCount < WriteView.length
                    WriteView(OutWriteCount) = memory_buf(memory_pos_r)
                    memory_pos_r += 1
                    OutWriteCount += 1
                End While

                'Stop if we can't read anymore or can't write anymore
                If OutReadCount >= ReadView.length Then Return
                If OutWriteCount >= WriteView.length Then Return

                'Keep buffered data near center of buffer array
                If memory_pos_w >= &H2000 Then
                    For i = 0 To memory_pos_w - &H1000 - 1
                        memory_buf(i) = memory_buf(i + &H1000)
                    Next i
                    memory_pos_w -= &H1000
                    memory_pos_r -= &H1000
                End If

                'Try to keep enough data buffered to satisfy any encoding
                While OutReadCount < ReadView.length AndAlso buffer.numBits < 32
                    buffer.queueByte(ReadView(OutReadCount))
                    OutReadCount += 1
                End While

                If Not unbuffer(1) Then Exit Do
                If last_val = 0 Then
                    '[Single encoded byte]
                    Select Case Me.mode
                        Case modes.binary 'raw byte
                            If Not unbuffer(8) Then Exit Do
                            memory_buf(memory_pos_w) = last_val
                            memory_pos_w += 1
                        Case modes.ascii 'byte compressed using a huffman tree based on ascii frequencies
                            If Not tree_unbuffer(TREE_ASCII) Then Exit Do
                            memory_buf(memory_pos_w) = last_val
                            memory_pos_w += 1
                    End Select
                Else '[Look-back run encoding]
                    'copy length
                    If Not tree_unbuffer(TREE_COPY_LENGTH) Then '[max 7 bits]
                        Exit Do
                    End If
                    Dim copy_length = CUShort(last_val) '[range 0 to 15]
                    Dim r = Math.Max(0, copy_length - 7) '[range 0 to 8]
                    If r > 0 Then
                        Dim LEN_BASE = New UShort() {0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 14, 22, 38, 70, 134, 262}
                        If Not unbuffer(r) Then Exit Do
                        copy_length = LEN_BASE(copy_length) + last_val
                    End If
                    copy_length += CUShort(2) '[range 2 to 518]

                    'jump-back length
                    If Not tree_unbuffer(TREE_JUMP_LENGTH) Then '[max 8 bits, range 0 to 63]
                        Exit Do
                    End If
                    Dim jump = CUShort(last_val)
                    Dim d = If(copy_length = 2, 2, num_extra_jump_bits)
                    If Not unbuffer(d) Then Exit Do '[max 6 bits]
                    jump = (jump << d) Or last_val
                    jump += CUShort(1) '[range 1 to 4096]

                    'perform
                    For i = memory_pos_w To memory_pos_w + copy_length - 1
                        memory_buf(i) = memory_buf(i - jump)
                    Next i
                    memory_pos_w += copy_length
                End If
            Loop
        End Sub

        Public Function needs(ByVal outputSize As Integer) As Integer Implements IBlockConverter.needs
            Return CInt(outputSize * 0.8 + 4)
        End Function
#End Region
    End Class
End Namespace
