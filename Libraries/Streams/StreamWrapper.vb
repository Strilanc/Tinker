'''<summary>Forwards all IO.Stream calls to a substream by default</summary>
Public MustInherit Class StreamWrapper
    Inherits IO.Stream
    Protected ReadOnly substream As IO.Stream
    Public Sub New(ByVal substream As IO.Stream)
        If Not (substream IsNot Nothing) Then Throw New ArgumentException()
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
    Private read_pos As Integer
    Private write_pos As Integer
    Private size As Integer

    Public Function Peek(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        Dim n = Math.Min(count, size)
        For i = 0 To n - 1
            buffer(offset + i) = data((read_pos + i) Mod data.Length)
        Next i
        Return n
    End Function

    Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        Dim n = Peek(buffer, offset, count)
        size -= n
        read_pos = (read_pos + n) Mod data.Length
        Return n
    End Function

    Private Sub resize(ByVal new_size As Integer)
        Dim new_data(0 To new_size - 1) As Byte
        Dim count = size
        Read(new_data, 0, count)
        size = count
        data = new_data
        read_pos = 0
        write_pos = count
    End Sub

    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        If size + count > data.Length Then resize((size + count) * 2)
        For i = 0 To count - 1
            data(write_pos) = buffer(offset + i)
            write_pos = (write_pos + 1) Mod data.Length
        Next i
        size += count
    End Sub

    Public Overrides ReadOnly Property Length() As Long
        Get
            Return size
        End Get
    End Property

    Public Sub Clear()
        read_pos = 0
        write_pos = 0
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