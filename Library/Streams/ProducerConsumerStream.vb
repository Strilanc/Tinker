Public NotInheritable Class ProducerConsumerStream
    Inherits IO.Stream
    Private data(0 To 15) As Byte
    Private readPosition As Integer
    Private writePosition As Integer
    Private size As Integer

    Public Function Peek(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        If buffer Is Nothing Then Throw New ArgumentNullException("buffer")
        If count <= 0 Then Return 0
        Dim n = Math.Min(count, size)
        For i = 0 To n - 1
            buffer(offset + i) = data((readPosition + i) Mod data.Length)
        Next i
        Return n
    End Function

    Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        If buffer Is Nothing Then Throw New ArgumentNullException("buffer")
        Dim n = Peek(buffer, offset, count)
        size -= n
        readPosition = (readPosition + n) Mod data.Length
        Return n
    End Function

    Private Sub resize(ByVal newSize As Integer)
        Contract.Requires(newSize >= 0)
        Dim newData(0 To newSize - 1) As Byte
        Dim count = size
        Read(newData, 0, count)
        size = count
        data = newData
        readPosition = 0
        writePosition = count
    End Sub

    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        If buffer Is Nothing Then Throw New ArgumentNullException("buffer")
        If count <= 0 Then Return
        If size + count > data.Length Then resize((size + count) * 2)
        For i = 0 To count - 1
            data(writePosition) = buffer(offset + i)
            writePosition = (writePosition + 1) Mod data.Length
        Next i
        size += count
    End Sub

    Public Overrides ReadOnly Property Length() As Long
        Get
            Return size
        End Get
    End Property

    Public Sub Clear()
        readPosition = 0
        writePosition = 0
        size = 0
    End Sub

    Public Overrides Sub Flush()
    End Sub

#Region "Capabilities"
    Public Overrides ReadOnly Property CanRead() As Boolean
        Get
            Return True
        End Get
    End Property
    Public Overrides ReadOnly Property CanSeek() As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides ReadOnly Property CanWrite() As Boolean
        Get
            Return True
        End Get
    End Property
#End Region

#Region "Not Supported"
    Public Overrides Property Position() As Long
        Get
            Throw New NotSupportedException()
        End Get
        Set(ByVal value As Long)
            Throw New NotSupportedException()
        End Set
    End Property
    Public Overrides Sub SetLength(ByVal value As Long)
        Throw New NotSupportedException()
    End Sub
    Public Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
        Throw New NotSupportedException()
    End Function
#End Region
End Class
