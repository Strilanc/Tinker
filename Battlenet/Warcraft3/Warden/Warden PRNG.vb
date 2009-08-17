Namespace Warcraft3.Warden
    '''<summary>Generates random data from a seed.</summary>
    Public Class WardenPseudoRandomNumberStream
        Inherits ReadOnlyStream
        Private hashState(0 To 59) As Byte
        Private outBuffer As Byte()
        Private numBuffered As Integer

        Public Sub New(ByVal seed_ As ModInt32)
            Dim seed = CUInt(seed_).bytes(ByteOrder.LittleEndian)
            Dim hashLeft = BSha1Processor.process(New IO.MemoryStream(seed.SubArray(0, 2)))
            Dim hashRight = BSha1Processor.process(New IO.MemoryStream(seed.SubArray(2, 2)))
            For i = 0 To 19
                hashState(i) = hashLeft(i)
                hashState(i + 40) = hashRight(i)
            Next i
        End Sub

        Private Sub refill()
            outBuffer = BSha1Processor.process(New IO.MemoryStream(hashState))
            For i = 0 To 19
                hashState(i + 20) = outBuffer(i)
            Next i
            numBuffered = 20
        End Sub

        Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
            For i = offset To offset + count - 1
                If numBuffered <= 0 Then refill()
                buffer(i) = outBuffer(20 - numBuffered)
                numBuffered -= 1
            Next i
            Return count
        End Function
    End Class
End Namespace
