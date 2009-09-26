'''<summary>
'''A stream wrapper which causes reads to return data in the same chunks it was written with.
'''Adds a size parameter to data when writing, includes size parameter in data when reading.
'''</summary>
Public Class PacketStream
    Inherits WrappedStream

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
        Contract.Assume(buffer IsNot Nothing)
        Contract.Assume(offset >= 0)
        Contract.Assume(count >= 0)
        Contract.Assume(offset + count <= buffer.Length)
        Return ReadWithMode(buffer, offset, count, defaultMode)
    End Function
    Public Function ReadWithMode(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal mode As InterfaceModes) As Integer
        Contract.Requires(buffer IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Requires(count >= 0)
        Contract.Requires(offset + count <= buffer.Length)
        Contract.Ensures(Contract.Result(Of Integer)() >= 0)
        Contract.Ensures(Contract.Result(Of Integer)() <= count)

        Dim readSize = 0
        Dim totalSize = 0

        'Read header
        If mode <> InterfaceModes.RawBytes Then
            If count < headerSize Then
                Throw New IO.IOException("Header exceeds buffer size.")
            End If
            Do
                Dim n = substream.Read(read_header_buffer, readSize, headerSize - readSize)
                If n = 0 Then Exit Do
                readSize += n
            Loop While readSize < headerSize
            If readSize = 0 Then
                Return 0 'no data
            ElseIf readSize < headerSize Then
                Throw New IO.IOException("Fragmented packet header.")
            End If

            'Parse header
            For i = headerSize - 1 To numBytesBeforeSize Step -1
                totalSize <<= 8
                totalSize += read_header_buffer(i)
            Next i
            If totalSize > count Then
                Throw New IO.IOException("Data exceeded buffer size.")
            End If
        End If

        'Transfer header to buffer
        Select Case mode
            Case InterfaceModes.HideSizeBytes
                For i = 0 To numBytesBeforeSize - 1
                    buffer(i + offset) = read_header_buffer(i)
                Next i
                readSize = numBytesBeforeSize
            Case InterfaceModes.IncludeSizeBytes
                For i = 0 To headerSize - 1
                    buffer(i + offset) = read_header_buffer(i)
                Next i
                readSize = headerSize
            Case InterfaceModes.RawBytes
                totalSize = count
            Case Else
                Throw New NotSupportedException("Unrecognized interface mode.")
        End Select

        'Read body into buffer
        While readSize < totalSize
            Dim n = substream.Read(buffer, offset + readSize, totalSize - readSize)
            If n = 0 Then
                Throw New IO.IOException("Fragmented packet body.")
            End If
            readSize += n
        End While

        Dim buffer_ = buffer 'avoids problems with contract verification on hoisted arguments
        Dim offset_ = offset
        logger.log(Function() "Received from {0}: {1}".frmt(logDestination, buffer_.SubArray(offset_, totalSize).ToHexString), LogMessageType.DataRaw)
        Return totalSize
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

        Dim buffer_ = buffer 'fixes contract verification error due to hoisting
        Dim offset_ = offset
        Dim count_ = count
        logger.log(Function() "Sending to {0}: {1}".frmt(logDestination, buffer_.SubArray(offset_, count_).ToHexString), LogMessageType.DataRaw)
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

Public Class PacketStreamer
    Private ReadOnly substream As IO.Stream
    Private ReadOnly numBytesBeforeSize As Integer
    Private ReadOnly numSizeBytes As Integer
    Private ReadOnly headerSize As Integer
    Private ReadOnly maxPacketSize As Integer

    Public Sub New(ByVal substream As IO.Stream,
                   ByVal numBytesBeforeSize As Integer,
                   ByVal numSizeBytes As Integer,
                   ByVal maxPacketSize As Integer)
        Contract.Requires(substream IsNot Nothing)
        Contract.Requires(numBytesBeforeSize >= 0)
        Contract.Requires(numSizeBytes > 0)
        Contract.Requires(maxPacketSize >= numBytesBeforeSize + numSizeBytes)

        Me.maxPacketSize = maxPacketSize
        Me.substream = substream
        Me.numSizeBytes = numSizeBytes
        Me.numBytesBeforeSize = numBytesBeforeSize
        Me.headerSize = numSizeBytes + numBytesBeforeSize
    End Sub

    Public Function FutureReadPacket() As IFuture(Of ViewableList(Of Byte))
        Dim readSize = 0
        Dim totalSize = 0
        Dim packetData(0 To headerSize - 1) As Byte
        Dim result = New FutureFunction(Of ViewableList(Of Byte))

        FutureIterate(Function() substream.FutureRead(packetData, readSize, packetData.Length - readSize),
            Function(numBytesRead, readException)
                'Check result
                If readException IsNot Nothing Then 'read failed
                    result.SetFailed(readException)
                    Return False.Futurized
                ElseIf numBytesRead <= 0 Then 'substream ended
                    If readSize = 0 Then
                        result.SetFailed(New IO.IOException("End of stream."))
                    Else
                        result.SetFailed(New IO.IOException("Fragmented packet (stream ended in the middle of a packet)."))
                    End If
                    Return False.Futurized
                End If

                'Read until whole header or whole body has arrived
                readSize += numBytesRead
                If readSize < packetData.Length Then
                    Return True.Futurized
                End If

                'Parse header
                If readSize = headerSize Then
                    totalSize = CInt(packetData.SubArray(numBytesBeforeSize, numSizeBytes).ToUInt32())
                    If totalSize < headerSize Then
                        'too small
                        result.SetFailed(New IO.IOException("Invalid packet size (less than header size)."))
                        Return False.Futurized
                    ElseIf totalSize > maxPacketSize Then
                        'too large
                        result.SetFailed(New IO.IOException("Packet exceeded maximum size."))
                        Return False.Futurized
                    ElseIf totalSize > headerSize Then
                        'begin reading packet body
                        ReDim Preserve packetData(0 To totalSize - 1)
                        Return True.Futurized
                    End If
                End If

                'Finished reading
                result.SetSucceeded(packetData.ToView)
                Return False.Futurized
            End Function
        )

        Return result
    End Function
    Public Function ReadPacket() As ViewableList(Of Byte)
        Dim readSize = 0
        Dim totalSize = 0
        Dim packetData(0 To headerSize - 1) As Byte

        'Read header
        Do
            Dim n = substream.Read(packetData, readSize, headerSize - readSize)
            If n = 0 Then Exit Do
            readSize += n
        Loop While readSize < headerSize
        If readSize = 0 Then
            Return Nothing 'no data
        ElseIf readSize < headerSize Then
            Throw New IO.IOException("Fragmented packet (stream ended in the middle of a packet header).")
        End If

        'Parse header
        For i = headerSize - 1 To numBytesBeforeSize Step -1
            totalSize <<= 8
            totalSize += packetData(i)
        Next i
        If totalSize < headerSize Then
            Throw New IO.IOException("Invalid packet size (less than header size).")
        ElseIf totalSize > maxPacketSize Then
            Throw New IO.IOException("Packet exceeded maximum size.")
        End If

        'Transfer header to buffer
        ReDim Preserve packetData(0 To totalSize - 1)

        'Read body into buffer
        While readSize < totalSize
            Dim n = substream.Read(packetData, readSize, totalSize - readSize)
            If n = 0 Then
                Throw New IO.IOException("Fragmented packet (stream ended in the middle of a packet body).")
            End If
            readSize += n
        End While

        Return packetData.ToView
    End Function

    Public Sub WritePacket(ByVal packetData As Byte())
        Contract.Requires(packetData IsNot Nothing)
        If packetData.Length < headerSize Then Throw New ArgumentException("Data didn't include header data.")

        'Encode size
        System.Array.Copy(CULng(packetData.Length).Bytes(size:=numSizeBytes), 0, packetData, numBytesBeforeSize, numSizeBytes)

        substream.Write(packetData, 0, packetData.Length)
    End Sub
End Class
