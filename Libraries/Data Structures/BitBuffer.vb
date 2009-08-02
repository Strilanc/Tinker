'''<summary>Stores up to maxBits bits and provides methods to add and extract bits for common types.</summary>
Public Class BitBuffer
    Public Const MaxBits As Integer = 64
    Private buf As ULong 'bit storage
    Private _numBufferedBits As Integer 'number of stored bits
    Public ReadOnly Property NumBufferedBits() As Integer
        Get
            Return _numBufferedBits
        End Get
    End Property
    Public ReadOnly Property NumBufferedBytes() As Integer
        Get
            Return _numBufferedBits \ 8
        End Get
    End Property

#Region "Base Operations"
    Public Sub Queue(ByVal bits As ULong, ByVal numBits As Integer)
        Contract.Requires(numBits >= 0)
        Contract.Requires(numBits <= MaxBits)
        If numBits > MaxBits - NumBufferedBits Then Throw New InvalidOperationException("Not enough capacity available.")
        buf = buf Or (bits << _numBufferedBits)
        _numBufferedBits += numBits
    End Sub
    Public Sub Stack(ByVal bits As ULong, ByVal numBits As Integer)
        Contract.Requires(numBits >= 0)
        Contract.Requires(numBits <= MaxBits)
        If numBits > MaxBits - NumBufferedBits Then Throw New InvalidOperationException("Not enough capacity available.")
        buf <<= numBits
        buf = buf Or bits
        _numBufferedBits += numBits
    End Sub
    Public Function Take(ByVal numBits As Integer) As ULong
        Contract.Requires(numBits >= 0)
        Contract.Requires(numBits <= MaxBits)
        Take = Peek(numBits)
        buf >>= numBits
        _numBufferedBits -= numBits
    End Function
    Public Function Peek(ByVal numBits As Integer) As ULong
        Contract.Requires(numBits >= 0)
        Contract.Requires(numBits <= MaxBits)
        If numBits > NumBufferedBits Then Throw New InvalidOperationException("Not enough buffered buffered bits available.")
        Peek = CULng(buf And ((1UL << numBits) - 1UL))
    End Function

    Public Sub Clear()
        _numBufferedBits = 0
        buf = 0
    End Sub
#End Region

#Region "Stack Types"
    Public Sub StackBit(ByVal value As Boolean)
        Stack(If(value, 1UL, 0UL), 1)
    End Sub
    Public Sub StackByte(ByVal value As Byte)
        Stack(value, 8)
    End Sub
    Public Sub StackUShort(ByVal value As UShort)
        Stack(value, 16)
    End Sub
    Public Sub StackUInteger(ByVal value As UInteger)
        Stack(value, 32)
    End Sub
#End Region

#Region "Queue Types"
    Public Sub QueueBit(ByVal value As Boolean)
        Queue(If(value, 1UL, 0UL), 1)
    End Sub
    Public Sub QueueByte(ByVal value As Byte)
        Queue(value, 8)
    End Sub
    Public Sub QueueUInt16(ByVal value As UInt16)
        Queue(value, 16)
    End Sub
    Public Sub QueueUInt32(ByVal value As UInt32)
        Queue(value, 32)
    End Sub
#End Region

#Region "Take Types"
    Public Function TakeBit() As Boolean
        Return Take(1) <> 0
    End Function
    Public Function TakeByte() As Byte
        Return CByte(Take(8))
    End Function
    Public Function TakeUInt16() As UInt16
        Return CUShort(Take(16))
    End Function
    Public Function TakeUInt32() As UInt32
        Return CUInt(Take(32))
    End Function
#End Region

#Region "Peek Types"
    Public Function PeekBit() As Boolean
        Return Peek(1) <> 0
    End Function
    Public Function PeekByte() As Byte
        Return CByte(Peek(8))
    End Function
    Public Function PeekUInt16() As UInt16
        Return CUShort(Peek(16))
    End Function
    Public Function PeekUInt32() As UInt32
        Return CUInt(Peek(32))
    End Function
#End Region
End Class

Public Class ByteSequenceBitBuffer
    Private ReadOnly buf As New BitBuffer
    Private ReadOnly sequence As IEnumerator(Of Byte)
    Public Sub New(ByVal sequence As IEnumerator(Of Byte))
        Contract.Requires(sequence IsNot Nothing)
        Me.sequence = sequence
    End Sub
    Public ReadOnly Property NumBufferedBits As Integer
        Get
            Return buf.NumBufferedBits
        End Get
    End Property
    Public Function TryBufferBits(ByVal n As Integer) As Boolean
        While buf.NumBufferedBits < n
            If Not sequence.MoveNext Then Return False
            buf.QueueByte(sequence.Current())
        End While
        Return True
    End Function
    Public Function Take(ByVal n As Integer) As ULong
        If Not TryBufferBits(n) Then Throw New InvalidOperationException("Ran past end of sequence.")
        Return buf.Take(n)
    End Function
    Public Function TakeBit() As Boolean
        Return Take(1) <> 0
    End Function
    Public Function TakeByte() As Byte
        Return CByte(Take(8))
    End Function
    Public Function TakeUShort() As UShort
        Return CUShort(Take(16))
    End Function
    Public Function TakeUInteger() As UInteger
        Return CUInt(Take(32))
    End Function
End Class
