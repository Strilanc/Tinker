Namespace Warcraft3.Warden
    '''<summary>Generates random data from a seed.</summary>
    Public Class WardenPRNG
        Inherits ReadOnlyConversionStream
        Private hash_state(0 To 59) As Byte
        Private out_buffer As Byte()
        Private num_buffered As Integer

        Public Sub New(ByVal seed_ As UInteger)
            Dim seed = seed_.bytes()
            Dim hash_left = BSha1Processor.process(New IO.MemoryStream(subArray(seed, 0, 2)))
            Dim hash_right = BSha1Processor.process(New IO.MemoryStream(subArray(seed, 2, 2)))
            For i = 0 To 19
                hash_state(i) = hash_left(i)
                hash_state(i + 40) = hash_right(i)
            Next i
        End Sub

        Private Sub refill()
            out_buffer = BSha1Processor.process(New IO.MemoryStream(hash_state))
            For i = 0 To 19
                hash_state(i + 20) = out_buffer(i)
            Next i
            num_buffered = 20
        End Sub

        Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
            For i = offset To offset + count - 1
                If num_buffered <= 0 Then refill()
                buffer(i) = out_buffer(20 - num_buffered)
                num_buffered -= 1
            Next i
            Return count
        End Function
    End Class
End Namespace
