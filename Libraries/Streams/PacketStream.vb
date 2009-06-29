'''<summary>
'''A stream wrapper which causes reads to return data in the same chunks it was written with.
'''Adds a size parameter to data when writing, includes size parameter in data when reading.
'''</summary>
Public Class PacketStream
    Inherits StreamWrapper

#Region "Variables"
    Public ReadOnly logger As Logger
    Public logDestination As String

    Private ReadOnly numBytesBeforeSize As Integer
    Private ReadOnly numSizeBytes As Integer
    Private ReadOnly headerSize As Integer

    Private ReadOnly read_header_buffer As Byte()

    Public ReadOnly defaultMode As InterfaceModes
    Public Enum InterfaceModes
        IncludeSizeBytes
        HideSizeBytes
        RawBytes
    End Enum
#End Region

#Region "New"
    Public Sub New(ByVal substream As IO.Stream,
                   ByVal numBytesBeforeSize As Integer,
                   ByVal numSizeBytes As Integer,
                   ByVal defaultMode As InterfaceModes,
                   ByVal logger As Logger,
                   ByVal log_destination As String)
        MyBase.New(substream)
        Contract.Requires(substream IsNot Nothing)
        Contract.Requires(numBytesBeforeSize >= 0)
        Contract.Requires(numSizeBytes > 0)

        Me.numSizeBytes = numSizeBytes
        Me.numBytesBeforeSize = numBytesBeforeSize
        Me.headerSize = numSizeBytes + numBytesBeforeSize
        Me.defaultMode = defaultMode
        Me.logger = If(logger, New Logger)
        Me.logDestination = log_destination
        ReDim read_header_buffer(0 To numBytesBeforeSize + numSizeBytes - 1)
    End Sub
#End Region

#Region "Read/Write"
    Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        Return ReadWithMode(buffer, offset, count, defaultMode)
    End Function
    Public Function ReadWithMode(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal mode As InterfaceModes) As Integer
        Contract.Requires(buffer IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Requires(count >= 0)
        Contract.Requires(offset + count <= buffer.Length)
        Contract.Ensures(Contract.Result(Of Integer)() >= 0)
        Contract.Ensures(Contract.Result(Of Integer)() <= count)

        Dim read_size = 0
        Dim total_size = 0

        'Read header
        If mode <> InterfaceModes.RawBytes Then
            If count < headerSize Then
                Throw New IO.IOException("Header exceeds buffer size.")
            End If
            Do
                Dim n = substream.Read(read_header_buffer, read_size, headerSize - read_size)
                If n = 0 Then Exit Do
                read_size += n
            Loop While read_size < headerSize
            If read_size = 0 Then
                Return 0 'no data
            ElseIf read_size < headerSize Then
                Throw New IO.IOException("Fragmented packet header.")
            End If

            'Parse header
            For i = headerSize - 1 To numBytesBeforeSize Step -1
                total_size <<= 8
                total_size += read_header_buffer(i)
            Next i
            If total_size > count Then
                Throw New IO.IOException("Data exceeded buffer size.")
            End If
        End If

        'Transfer header to buffer
        Select Case mode
            Case InterfaceModes.HideSizeBytes
                For i = 0 To numBytesBeforeSize - 1
                    buffer(i + offset) = read_header_buffer(i)
                Next i
                read_size = numBytesBeforeSize
            Case InterfaceModes.IncludeSizeBytes
                For i = 0 To headerSize - 1
                    buffer(i + offset) = read_header_buffer(i)
                Next i
                read_size = headerSize
            Case InterfaceModes.RawBytes
                total_size = count
            Case Else
                Throw New NotSupportedException("Unrecognized interface mode.")
        End Select

        'Read body into buffer
        While read_size < total_size
            Dim n = substream.Read(buffer, offset + read_size, total_size - read_size)
            If n = 0 Then
                Throw New IO.IOException("Fragmented packet body.")
            End If
            read_size += n
        End While

        logger.log(Function() "Received from {0}: {1}".frmt(logDestination, buffer.SubArray(offset, total_size).ToHexString), LogMessageTypes.DataRaw)
        Return total_size
    End Function

    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        WriteWithMode(buffer, offset, count, defaultMode)
    End Sub
    Public Sub WriteWithMode(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal mode As InterfaceModes)
        Contract.Requires(buffer IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Requires(count >= 0)
        Contract.Requires(offset + count <= buffer.Length)

        'Convert data format
        Select Case mode
            Case InterfaceModes.HideSizeBytes
                If count < numBytesBeforeSize Then Throw New ArgumentException("Data didn't include header data.")
                Dim data(0 To count + numSizeBytes - 1) As Byte
                For i = 0 To numBytesBeforeSize - 1
                    data(i) = buffer(offset + i)
                Next i
                For i = numBytesBeforeSize To count - 1
                    data(i + numSizeBytes) = buffer(offset + i)
                Next i
                buffer = data
                offset = 0
                count = data.Length
                EncodeSize(count, buffer, offset + numBytesBeforeSize)

            Case InterfaceModes.IncludeSizeBytes
                If count < headerSize Then Throw New ArgumentException("Data didn't include header data.")
                EncodeSize(count, buffer, offset + numBytesBeforeSize)

            Case InterfaceModes.RawBytes
                'no changes needed

            Case Else
                Throw New NotSupportedException("Unrecognized interface mode.")
        End Select

        logger.log(Function() "Sending to {0}: {1}".frmt(logDestination, buffer.SubArray(offset, count).ToHexString), LogMessageTypes.DataRaw)
        substream.Write(buffer, offset, count)
    End Sub

    Private Sub EncodeSize(ByVal size As Integer, ByVal buffer() As Byte, ByVal offset As Integer)
        If size < headerSize Then
            Throw New ArgumentException("Packet size must include header.")
        End If
        For i = 0 To numSizeBytes - 1
            buffer(i + offset) = CByte(size And &HFF)
            size >>= 8
        Next i
        If size > 0 Then
            Throw New ArgumentException("Size exceeded encodable values.")
        End If
    End Sub
#End Region
End Class
