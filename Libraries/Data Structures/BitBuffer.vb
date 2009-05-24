'''<summary>Stores up to maxBits bits and provides methods to add and extract bits for common types.</summary>
Public Class BitBuffer
    Public Const MAX_OP_BITS As Integer = 64
    Private buf As ULong 'bit storage
    Private num As Integer 'number of stored bits
    Public ReadOnly Property numBits() As Integer
        Get
            Return num
        End Get
    End Property
    Public ReadOnly Property numBytes() As Integer
        Get
            Return num \ 8
        End Get
    End Property

#Region "Base Operations"
    Public Sub queue(ByVal u As ULong, ByVal n As Integer)
        If n < 0 Or num + n > MAX_OP_BITS Then Throw New ArgumentOutOfRangeException()
        buf = buf Or (u << num)
        num += n
    End Sub
    Public Sub stack(ByVal u As ULong, ByVal n As Integer)
        If n < 0 Or num + n > MAX_OP_BITS Then Throw New ArgumentOutOfRangeException()
        buf <<= n
        buf = buf Or u
        num += n
    End Sub
    Public Function take(ByVal n As Integer) As ULong
        take = peek(n)
        buf >>= n
        num -= n
    End Function
    Public Function peek(ByVal n As Integer) As ULong
        If n > num Or n > MAX_OP_BITS Then Throw New ArgumentOutOfRangeException()
        If n < 0 Then Throw New ArgumentOutOfRangeException()
        peek = CULng(buf And ((CULng(1) << n) - CULng(1)))
    End Function

    Public Sub clear()
        num = 0
        buf = 0
    End Sub
#End Region

#Region "Stack Types"
    Public Sub stackBit(ByVal b As Boolean)
        stack(If(b, CULng(1), CULng(0)), 1)
    End Sub
    Public Sub stackByte(ByVal b As Byte)
        stack(b, 8)
    End Sub
    Public Sub stackUShort(ByVal s As UShort)
        stack(s, 16)
    End Sub
    Public Sub stackUInteger(ByVal u As UInteger)
        stack(u, 32)
    End Sub
#End Region

#Region "Queue Types"
    Public Sub queueBit(ByVal b As Boolean)
        queue(If(b, CULng(1), CULng(0)), 1)
    End Sub
    Public Sub queueByte(ByVal b As Byte)
        queue(b, 8)
    End Sub
    Public Sub queueUShort(ByVal s As UShort)
        queue(s, 16)
    End Sub
    Public Sub queueUInteger(ByVal u As UInteger)
        queue(u, 32)
    End Sub
#End Region

#Region "Take Types"
    Public Function takeBit() As Boolean
        Return take(1) <> 0
    End Function
    Public Function takeByte() As Byte
        Return CByte(take(8))
    End Function
    Public Function takeUShort() As UShort
        Return CUShort(take(16))
    End Function
    Public Function takeUInteger() As UInteger
        Return CUInt(take(32))
    End Function
#End Region

#Region "Peek Types"
    Public Function peekBit() As Boolean
        Return peek(1) <> 0
    End Function
    Public Function peekByte() As Byte
        Return CByte(peek(8))
    End Function
    Public Function peekUShort() As UShort
        Return CUShort(peek(16))
    End Function
    Public Function peekUInteger() As UInteger
        Return CUInt(peek(32))
    End Function
#End Region

#Region "UShort to Short"
    Public Sub stackShort(ByVal s As Short)
        stackUShort(uCUShort(s))
    End Sub
    Public Sub queueShort(ByVal s As Short)
        queueUShort(uCUShort(s))
    End Sub
    Public Function takeShort() As Short
        Return uCShort(takeUShort())
    End Function
#End Region
End Class
