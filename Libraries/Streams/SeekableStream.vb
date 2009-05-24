Public Class SeekableStream
    Inherits StreamWrapper
    Private m As New IO.MemoryStream
    Private pos As Long
    Private mback As Integer
    Private ReadOnly memLimit As Integer

    Public Sub New(ByVal substream As IO.Stream, Optional ByVal min_memory As Integer = 4096)
        MyBase.New(substream)
        Me.memLimit = min_memory
    End Sub

    Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        Dim numRead = 0

        'Read from seek-back memory
        If mback > 0 Then
            Dim n = m.Read(buffer, offset, count)
            mback -= n
            numRead += n
            count -= n
            offset += n
        End If

        'Read from substream
        If count > 0 Then
            Dim n = substream.Read(buffer, offset, count)
            numRead += n

            'Store in seek-back memory, but limit memory size
            m.Write(buffer, offset, n)
            If m.Length >= memLimit * 2 Then
                Dim bb(0 To memLimit - 1) As Byte
                m.Seek(-memLimit, IO.SeekOrigin.End)
                m.Read(bb, 0, memLimit)
                m.Dispose()
                m = New IO.MemoryStream()
                m.Write(bb, 0, memLimit)
            End If
        End If

        pos += numRead
        Return numRead
    End Function

    Public Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
        'Convert to relative offset
        If origin = IO.SeekOrigin.Begin Then offset -= pos
        If origin = IO.SeekOrigin.End Then offset += MyBase.Length - pos

        If offset > 0 Then
            'read ahead
            Dim bb(0 To CInt(offset) - 1) As Byte
            Me.Read(bb, 0, CInt(offset))
        ElseIf offset < 0 Then
            'move into seek-back memory
            offset = -offset
            If offset > Math.Min(m.Length, memLimit) - mback Then
                Throw New InvalidOperationException("Seeked past memory wall.")
            End If
            mback += CInt(offset)
            m.Seek(-offset, IO.SeekOrigin.Current)
        End If
        pos += offset

        Return pos
    End Function

    Public Overrides Property Position() As Long
        Get
            Return pos
        End Get
        Set(ByVal value As Long)
            Seek(value, IO.SeekOrigin.Begin)
        End Set
    End Property
    Public Overrides ReadOnly Property CanSeek() As Boolean
        Get
            Return True
        End Get
    End Property
    Public Overrides ReadOnly Property CanWrite() As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        Throw New NotSupportedException("Can't write to a SeekableStream.")
    End Sub
End Class
