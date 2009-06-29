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

Namespace Mpq.Compression.PkWare
    Public Class Decoder
        Implements IConverter(Of Byte)
        Public Enum modes As Byte
            binary = 0
            ascii = 1
        End Enum
        Private Const BUFFER_SIZE As Integer = 4096

        '''<summary>Travels down to the leaf of a coding tree, unbuffering bits for direction.</summary>
        Private Function ReadTreeNodeFrom(ByVal c As CodeTree, ByVal readBuf As ByteSequenceBitBuffer) As Byte
            Dim n = c.root
            While Not n.isLeaf
                n = If(readBuf.TakeBit, n.leftChild, n.rightChild)
            End While
            Return CByte(n.val)
        End Function

        Private ReadOnly Property CyclePosition(ByVal position As Integer) As Integer
            Get
                Return (position + BUFFER_SIZE) Mod BUFFER_SIZE
            End Get
        End Property

        Public Function Convert(ByVal seq As IEnumerator(Of Byte)) As IEnumerator(Of Byte) Implements IConverter(Of Byte).Convert
            Dim seqBuf = New ByteSequenceBitBuffer(seq)
            If Not seqBuf.TryBufferBits(16) Then Throw New IO.InvalidDataException("Invalid PkWare Stream (too short to even include header).")

            'Mode
            Dim mode = CType(seqBuf.TakeByte(), Decoder.modes)
            Select Case mode
                Case modes.binary, modes.ascii
                Case Else
                    Throw New IO.InvalidDataException("PkWare Stream data has unrecognized mode.")
            End Select

            'Dictionary size
            Dim numExtraJumpBits = seqBuf.TakeByte()
            If numExtraJumpBits < 4 OrElse numExtraJumpBits > 6 Then
                Throw New IO.InvalidDataException("PkWare Stream data specified an invalid number of jump bits.")
            End If

            'The memory buffer is used for look-back run encoding
            Dim outBuf(0 To BUFFER_SIZE - 1) As Byte
            Dim writePos = 0
            Dim readPos = 0

            Return New Enumerator(Of Byte)(
                Function(controller)
                    'Decompress
                    Do
                        'Cleanup previous operations
                        If readPos < writePos Then
                            Dim b = outBuf(readPos)
                            readPos = CyclePosition(readPos + 1)
                            Return b
                        End If

                        'Check for end of stream
                        If Not seqBuf.TryBufferBits(1) Then  Return controller.Break()

                        If Not seqBuf.TakeBit Then
                            '[Single encoded byte]
                            Select Case mode
                                Case modes.binary 'raw byte
                                    outBuf(writePos) = seqBuf.TakeByte()
                                    writePos = CyclePosition(writePos + 1)
                                Case modes.ascii 'byte compressed using a huffman tree based on ascii frequencies
                                    outBuf(writePos) = ReadTreeNodeFrom(AsciiTree, seqBuf)
                                    writePos = CyclePosition(writePos + 1)
                                Case Else
                                    Throw New UnreachableException()
                            End Select
                        Else '[Look-back run encoding]
                            'copy length
                            Dim runLength = CUShort(ReadTreeNodeFrom(CopyLengthTree, seqBuf)) '[range 0 to 15]
                            Dim r = runLength - 7
                            If r > 0 Then  runLength = CByte(seqBuf.Take(r)) + CopyLengthTable(runLength)
                            runLength += CUShort(2) '[range 2 to 518]

                            'jump-back length
                            Dim runOffset = CUShort(ReadTreeNodeFrom(JumpLengthTree, seqBuf)) '[range 0 to 63]
                            Dim d = If(runLength = 2, 2, numExtraJumpBits)
                            runOffset = (runOffset << d) Or CByte(seqBuf.Take(d))
                            runOffset += CUShort(1) '[range 1 to 4096]

                            'perform
                            For i = 0 To runLength - 1
                                writePos = CyclePosition(writePos + 1)
                                outBuf(writePos) = outBuf(CyclePosition(writePos - runOffset))
                            Next i
                            writePos += runLength
                        End If
                    Loop
                End Function
            )
        End Function
    End Class

    Friend Module Common
#Region "Data"
        Public ReadOnly JumpLengthTree As New CodeTree(
                {
                        &H3, &HD, &H5, &H19, &H9, &H11, &H1, &H3E, &H1E, &H2E, &HE, &H36, &H16, &H26, &H6, &H3A,
                        &H1A, &H2A, &HA, &H32, &H12, &H22, &H42, &H2, &H7C, &H3C, &H5C, &H1C, &H6C, &H2C, &H4C, &HC,
                        &H74, &H34, &H54, &H14, &H64, &H24, &H44, &H4, &H78, &H38, &H58, &H18, &H68, &H28, &H48, &H8,
                        &HF0, &H70, &HB0, &H30, &HD0, &H50, &H90, &H10, &HE0, &H60, &HA0, &H20, &HC0, &H40, &H80, &H0 _
                }, {
                         2, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                         7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
                })

        Public ReadOnly CopyLengthTree As New CodeTree(
                    {5, 3, 1, 6, 10, 2, 12, 20, 4, 24, 8, 48, 16, 32, 64, 0},
                    {3, 2, 3, 3, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 7, 7})
        Public ReadOnly CopyLengthTable() As UShort = {0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 14, 22, 38, 70, 134, 262}
        Public ReadOnly AsciiTree As New CodeTree(
                 {
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
                    &H600, &H1A00, &HE40, &H640, &HA40, &HA00, &H1200, &H200, &H1C00, &HC00, &H1400, &H400, &H1800, &H800, &H1000, &H0
                }, {
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
                    &HD, &HD, &HC, &HC, &HC, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD, &HD
                })
#End Region

        '''<summary>Maps sequences of bits to characters.</summary>
        Public Class CodeTree
            Public ReadOnly root As New CodeNode()

            Public Class CodeNode
                Public leftChild As CodeNode
                Public rightChild As CodeNode
                Public val As Integer
                Public isLeaf As Boolean
            End Class

            '''<summary>Generates a code-tree with each character code(i) at level lengths(i)</summary>
            Public Sub New(ByVal codes() As UShort, ByVal lengths() As Byte)
                If codes Is Nothing Then Throw New ArgumentNullException("codes")
                If lengths Is Nothing Then Throw New ArgumentNullException("lengths")
                If codes.Length <> lengths.Length Then Throw New ArgumentException("Must have the same number of codes and lengths.")
                Dim numLeaves = 0
                Dim numNodes = 1

                For i = 0 To codes.Length - 1
                    Dim n = root
                    Dim c = codes(i)
                    For repeat = 0 To lengths(i) - 1
                        'Mark current node as an internal node
                        If n.isLeaf Then
                            Throw New ArgumentException("The path for {0} passes through the leaf for {1}.".frmt(n.val, i))
                        End If
                        If n.leftChild Is Nothing Then
                            n.leftChild = New CodeNode()
                            numNodes += 1
                        End If
                        If n.rightChild Is Nothing Then
                            n.rightChild = New CodeNode()
                            numNodes += 1
                        End If

                        'Travel to next child
                        n = If((c And 1) <> 0, n.leftChild, n.rightChild)
                        c >>= 1
                    Next repeat

                    'Assign value to the selected leaf
                    If n.isLeaf Then
                        Throw New ArgumentException("The code for {0} and {1} are the same.".frmt(i, n.val))
                    ElseIf n.leftChild IsNot Nothing OrElse n.rightChild IsNot Nothing Then
                        Throw New ArgumentException("The code for {0} terminates on an internal node.".frmt(i))
                    End If
                    n.val = i
                    n.isLeaf = True
                    numLeaves += 1
                Next i

                'Check that all leaves are coded
                If numLeaves * 2 <> numNodes + 1 Then
                    Throw New ArgumentException("There are non-terminated paths in the code tree. It is non-optimal.")
                End If
            End Sub
        End Class
    End Module
End Namespace
