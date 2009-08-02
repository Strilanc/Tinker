'''<summary>Forwards all IO.Stream calls to a substream by default</summary>
Public MustInherit Class StreamWrapper
    Inherits IO.Stream
    Protected ReadOnly substream As IO.Stream
    Public Sub New(ByVal substream As IO.Stream)
        Contract.Requires(substream IsNot Nothing)
        Me.substream = substream
    End Sub
    Public Overrides ReadOnly Property CanRead() As Boolean
        Get
            Return substream.CanRead()
        End Get
    End Property
    Public Overrides ReadOnly Property CanSeek() As Boolean
        Get
            Return substream.CanSeek()
        End Get
    End Property
    Public Overrides ReadOnly Property CanWrite() As Boolean
        Get
            Return substream.CanWrite()
        End Get
    End Property
    Public Overrides Sub Flush()
        substream.Flush()
    End Sub
    Public Overrides ReadOnly Property Length() As Long
        Get
            Return substream.Length
        End Get
    End Property
    Public Overrides Property Position() As Long
        Get
            Return substream.Position
        End Get
        Set(ByVal value As Long)
            substream.Position = value
        End Set
    End Property
    Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        Return substream.Read(buffer, offset, count)
    End Function
    Public Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
        Return substream.Seek(offset, origin)
    End Function
    Public Overrides Sub SetLength(ByVal value As Long)
        substream.SetLength(value)
    End Sub
    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        substream.Write(buffer, offset, count)
    End Sub
    Public Overrides Sub Close()
        substream.Close()
        MyBase.Close()
    End Sub
    Public Overrides Function BeginRead(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal callback As System.AsyncCallback, ByVal state As Object) As System.IAsyncResult
        Return substream.BeginRead(buffer, offset, count, callback, state)
    End Function
    Public Overrides Function BeginWrite(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal callback As System.AsyncCallback, ByVal state As Object) As System.IAsyncResult
        Return substream.BeginWrite(buffer, offset, count, callback, state)
    End Function
    Public Overrides Function EndRead(ByVal asyncResult As System.IAsyncResult) As Integer
        Return substream.EndRead(asyncResult)
    End Function
    Public Overrides Sub EndWrite(ByVal asyncResult As System.IAsyncResult)
        substream.EndWrite(asyncResult)
    End Sub
End Class

'''<summary>A stream which can't seek and doesn't have a length.</summary>
Public MustInherit Class ConversionStream
    Inherits IO.Stream
    Public NotOverridable Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
        Throw New NotSupportedException()
    End Function
    Public NotOverridable Overrides Sub SetLength(ByVal value As Long)
        Throw New NotSupportedException()
    End Sub
    Public NotOverridable Overrides ReadOnly Property Length() As Long
        Get
            Throw New NotSupportedException()
        End Get
    End Property
    Public NotOverridable Overrides ReadOnly Property CanSeek() As Boolean
        Get
            Return False
        End Get
    End Property
    Public NotOverridable Overrides Property Position() As Long
        Get
            Throw New NotSupportedException()
        End Get
        Set(ByVal value As Long)
            Throw New NotSupportedException()
        End Set
    End Property
End Class
Public MustInherit Class WrappedConversionStream
    Inherits StreamWrapper
    Public Sub New(ByVal substream As IO.Stream)
        MyBase.New(substream)
        Contract.Requires(substream IsNot Nothing)
    End Sub
    Public NotOverridable Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
        Throw New InvalidOperationException(Me.GetType.Name + " doesn't allow seeking.")
    End Function
    Public NotOverridable Overrides Sub SetLength(ByVal value As Long)
        Throw New InvalidOperationException(Me.GetType.Name + " doesn't allow setting its length.")
    End Sub
    Public NotOverridable Overrides ReadOnly Property Length() As Long
        Get
            Throw New InvalidOperationException(Me.GetType.Name + " doesn't have a length.")
        End Get
    End Property
    Public NotOverridable Overrides ReadOnly Property CanSeek() As Boolean
        Get
            Return False
        End Get
    End Property
    Public NotOverridable Overrides Property Position() As Long
        Get
            Throw New NotSupportedException()
        End Get
        Set(ByVal value As Long)
            Throw New NotSupportedException()
        End Set
    End Property
End Class

'''<summary>A read-only stream which can't seek and doesn't have a length.</summary>
Public MustInherit Class ReadOnlyConversionStream
    Inherits ConversionStream
    Public NotOverridable Overrides ReadOnly Property CanWrite() As Boolean
        Get
            Return False
        End Get
    End Property
    Public NotOverridable Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        Throw New NotSupportedException()
    End Sub
    Public NotOverridable Overrides ReadOnly Property CanRead() As Boolean
        Get
            Return True
        End Get
    End Property
    Public NotOverridable Overrides Sub Flush()
        Throw New NotSupportedException()
    End Sub
End Class
Public MustInherit Class WrappedReadOnlyConversionStream
    Inherits WrappedConversionStream
    Public Sub New(ByVal substream As IO.Stream)
        MyBase.New(substream)
        Contract.Requires(substream IsNot Nothing)
    End Sub
    Public NotOverridable Overrides ReadOnly Property CanWrite() As Boolean
        Get
            Return False
        End Get
    End Property
    Public NotOverridable Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        Throw New InvalidOperationException(Me.GetType.Name + " doesn't allow writing.")
    End Sub
    Public NotOverridable Overrides ReadOnly Property CanRead() As Boolean
        Get
            Return True
        End Get
    End Property
    Public NotOverridable Overrides Sub Flush()
        Throw New NotSupportedException()
    End Sub
End Class

'''<summary>A write-only stream which can't seek and doesn't have a length.</summary>
Public MustInherit Class WrappedWriteOnlyConversionStream
    Inherits WrappedConversionStream
    Public Sub New(ByVal substream As IO.Stream)
        MyBase.New(substream)
        Contract.Requires(substream IsNot Nothing)
    End Sub
    Public NotOverridable Overrides ReadOnly Property CanRead() As Boolean
        Get
            Return False
        End Get
    End Property
    Public NotOverridable Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        Throw New NotSupportedException()
    End Function
End Class

Public Class ProducerConsumerStream
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

Public Class ZLibStream
    Inherits StreamWrapper

    Private Shared Function wrap(ByVal stream As IO.Stream,
                                 ByVal mode As IO.Compression.CompressionMode,
                                 ByVal leaveOpen As Boolean) As IO.Stream
        Contract.Requires(stream IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
        Select Case mode
            Case IO.Compression.CompressionMode.Decompress
                stream.ReadByte()
                stream.ReadByte()
            Case IO.Compression.CompressionMode.Compress
                stream.WriteByte(120)
                stream.WriteByte(156)
            Case Else
                Throw New UnreachableException()
        End Select
        Return New IO.Compression.DeflateStream(stream, mode, leaveOpen)
    End Function
    Public Sub New(ByVal substream As IO.Stream,
                   ByVal mode As IO.Compression.CompressionMode,
                   Optional ByVal leaveOpen As Boolean = False)
        MyBase.New(wrap(substream, mode, leaveOpen))
        Contract.Requires(substream IsNot Nothing)
    End Sub
End Class