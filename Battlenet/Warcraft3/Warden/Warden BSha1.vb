Namespace Warcraft3.Warden
    ''' <summary>
    ''' Performs preprocessing on a data stream meant to be hashed.
    ''' Appends to the data stream, and makes sure it will be a multiple of 512 bits in length.
    ''' </summary>
    Public Class BSha1DataStream
        Inherits WrappedReadOnlyConversionStream
        Private Enum modes
            data
            pad_1
            pad_0
            pad_length
            done
        End Enum

        Private total_read As ULong
        Private total_output As ULong
        Private buffered_byte(0 To 0) As Byte
        Private buffered As Boolean
        Private length_count As Integer
        Private mode As modes = modes.data

        Public Sub New(ByVal substream As IO.Stream)
            MyBase.New(substream)
            buffered = RawRead(buffered_byte, 0, 1) = 1
            If Not buffered Then mode = modes.done
        End Sub

        ''' <summary>
        ''' Reads from the data stream.
        ''' Also ensures that IsDone will have the correct value by trying to buffer the next byte.
        ''' </summary>
        Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
            If count <= 0 Then Return 0
            Dim n = 0
            If buffered Then
                buffer(offset) = buffered_byte(0)
                offset += 1
                count -= 1
                buffered = False
                n += 1
            End If
            n += RawRead(buffer, offset, count)
            buffered = RawRead(buffered_byte, 0, 1) = 1
            Return n
        End Function

        '''<summary>Reads from the data stream.</summary>
        Private Function RawRead(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
            Dim n = 0
            While count > 0
                Dim b As Byte
                Select Case mode
                    Case modes.data 'First, the stream data is returned
                        n += MyBase.Read(buffer, offset, count)
                        offset += n
                        count -= n
                        total_read += CULng(n * 8)
                        total_output += CULng(n)
                        If count > 0 Then mode = modes.pad_1
                        Continue While

                    Case modes.pad_1 'Second, a '1' bit is appended to the stream
                        b = 128
                        mode = modes.pad_0

                    Case modes.pad_0 'Third, the stream is extended with 0bits so its length will end up being a multiple of 512
                        If total_output Mod 64 = 64 - 8 Then
                            mode = modes.pad_length
                            Continue While
                        End If
                        b = 0

                    Case modes.pad_length 'Finally, the data's original length is appended as a little-endian UInt64
                        Dim u = ReverseByteEndian(total_read)
                        b = CByte((u >> length_count) And CByte(&HFF))
                        length_count += 8
                        If length_count >= 64 Then mode = modes.done

                    Case modes.done
                        Exit While
                End Select

                buffer(offset) = b
                offset += 1
                count -= 1
                n += 1
                total_output += CULng(1)
            End While
            Return n
        End Function

        '''<summary>Returns true if there is no more data to read.</summary>
        Public Function isDone() As Boolean
            Return mode = modes.done And Not buffered
        End Function
    End Class

    '''<summary>Implements methods for computing the BSha1 hash.</summary>
    Public Class BSha1Processor
        Public Shared Function process(ByVal data_ As IO.Stream) As Byte()
            'Prep initial state
            Dim S(0 To 4) As UInteger
            S(0) = &H67452301L
            S(1) = &HEFCDAB89L
            S(2) = &H98BADCFEL
            S(3) = &H10325476L
            S(4) = &HC3D2E1F0L

            'Process data stream
            Dim data = New BSha1DataStream(data_)
            Dim br As New IO.BinaryReader(data)
            While Not data.isDone
                Dim P(0 To 15) As UInteger
                For j = 0 To 15
                    P(j) = br.ReadUInt32()
                Next j
                S = iterate(P, S)
            End While

            'Return final state
            For i = 0 To 4
                S(i) = ReverseByteEndian(S(i))
            Next i
            Return concat(S(0).bytes(ByteOrder.LittleEndian),
                          S(1).bytes(ByteOrder.LittleEndian),
                          S(2).bytes(ByteOrder.LittleEndian),
                          S(3).bytes(ByteOrder.LittleEndian),
                          S(4).bytes(ByteOrder.LittleEndian))
        End Function

        Private Shared Function iterate(ByVal data() As UInteger, ByVal state() As UInteger) As UInteger()
            Dim h(0 To 79) As UInteger
            For i = 0 To 15
                h(i) = ReverseByteEndian(data(i))
            Next i
            For i = 16 To 79
                Dim x = h(i - 3) Xor h(i - 8) Xor h(i - 14) Xor h(i - 16)
                h(i) = ShiftRotateLeft(x, 1)
            Next i

            Dim a = state(0)
            Dim b = state(1)
            Dim c = state(2)
            Dim d = state(3)
            Dim e = state(4)
            For i = 0 To 79
                Dim f As UInteger
                Dim k As UInteger
                Select Case i
                    Case 0 To 19
                        f = (b And c) Or (d And Not b)
                        k = &H5A827999
                    Case 20 To 39
                        f = d Xor c Xor b
                        k = &H6ED9EBA1
                    Case 40 To 59
                        f = (c And b) Or (d And c) Or (d And b)
                        k = &H8F1BBCDCL
                    Case 60 To 79
                        f = d Xor c Xor b
                        k = &HCA62C1D6L
                End Select
                Dim T = uSum(ShiftRotateLeft(a, 5), f, e, k, h(i))
                e = d
                d = c
                c = ShiftRotateLeft(b, 30)
                b = a
                a = T
            Next i

            Dim new_state(0 To 4) As UInteger
            new_state(0) = uAdd(state(0), a)
            new_state(1) = uAdd(state(1), b)
            new_state(2) = uAdd(state(2), c)
            new_state(3) = uAdd(state(3), d)
            new_state(4) = uAdd(state(4), e)
            Return new_state
        End Function
    End Class
End Namespace