Namespace Warcraft3.Warden
    '''<summary>Encrypts and decrypts data using RC4.</summary>
    Public Class RC4Converter
        Implements IBlockConverter
        Private ReadOnly rc4 As RC4XorStream
        Public Sub New(ByVal seed() As Byte)
            Me.rc4 = New RC4XorStream(seed)
        End Sub
        Public Sub New(ByVal state As Warden_Module_Lib.RC4State)
            Me.rc4 = New RC4XorStream(state)
        End Sub

        Public Sub convert(ByVal ReadView As ReadOnlyArrayView(Of Byte), _
                           ByVal WriteView As ArrayView(Of Byte), _
                           ByRef OutReadCount As Integer, _
                           ByRef OutWriteCount As Integer) _
                           Implements IBlockConverter.convert
            Dim n = Math.Min(ReadView.length, WriteView.length)
            For i = 0 To n - 1
                WriteView(i) = ReadView(i) Xor rc4.ReadByte()
            Next i
            OutReadCount = n
            OutWriteCount = n
        End Sub

        Public Function needs(ByVal outputSize As Integer) As Integer Implements IBlockConverter.needs
            Return outputSize
        End Function
    End Class

    '''<summary>Generates the stream of bytes which data is XORed against to encrypt and decrypt via RC4.</summary>
    Public Class RC4XorStream
        Inherits ReadOnlyConversionStream
        Private key(0 To Byte.MaxValue) As Byte
        Private k1 As Byte
        Private k2 As Byte

        Public Sub New(ByVal seed As Byte())
            'prep key
            For i = 0 To Byte.MaxValue
                key(i) = CByte(i)
            Next i
            'shuffle key
            Dim p = 0
            For i = 0 To Byte.MaxValue
                p = (p + key(i) + seed(i Mod seed.Length)) And &HFF
                Swap(key(i), key(p))
            Next i
        End Sub
        Public Sub New(ByVal state As Warden_Module_Lib.RC4State)
            k1 = state.k1
            k2 = state.k2
            For i = 0 To Byte.MaxValue
                key(i) = state.state(CByte(i))
            Next i
        End Sub

        '''<summary>Generates the next XOR byte.</summary>
        Public Shadows Function ReadByte() As Byte
            k1 = uCByte(k1 + 1)
            k2 = uCByte(CInt(k2) + CInt(key(k1)))
            Swap(key(k1), key(k2))
            Return key(uCByte(CInt(key(k1)) + CInt(key(k2))))
        End Function

        '''<summary>Reads a block of XOR bytes.</summary>
        Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
            For i = offset To offset + count - 1
                buffer(i) = Me.ReadByte()
            Next i
            Return count
        End Function
    End Class
End Namespace
