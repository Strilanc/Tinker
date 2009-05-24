'''<summary>
'''A stream wrapper which causes reads to return data in the same chunks it was written with.
'''Adds a size parameter to data when writing, includes size parameter in data when reading.
'''</summary>
Public Class PacketStream
    Inherits StreamWrapper

#Region "Variables"
    Public ReadOnly logger As MultiLogger
    Public log_destination As String

    Private ReadOnly pre_size As Integer
    Private ReadOnly size_size As Integer
    Private ReadOnly header_size As Integer

    Private ReadOnly read_header_buffer As Byte()

    Public ReadOnly default_mode As InterfaceModes
    Public Enum InterfaceModes
        IncludeSizeBytes
        HideSizeBytes
        RawBytes
    End Enum
#End Region

#Region "New"
    Public Sub New(ByVal substream As IO.Stream, _
                   ByVal num_bytes_before_size As Integer, _
                   ByVal num_size_bytes As Integer, _
                   ByVal interface_mode As InterfaceModes, _
                   ByVal logger As MultiLogger, _
                   ByVal log_destination As String)
        MyBase.New(substream)
        ContractNonNegative(num_bytes_before_size, "num_bytes_before_size")
        ContractPositive(num_size_bytes, "num_size_bytes")

        Me.size_size = num_size_bytes
        Me.pre_size = num_bytes_before_size
        Me.header_size = num_size_bytes + num_bytes_before_size
        Me.default_mode = interface_mode
        Me.logger = If(logger, New MultiLogger)
        Me.log_destination = log_destination
        ReDim read_header_buffer(0 To num_bytes_before_size + num_size_bytes - 1)
    End Sub
#End Region

#Region "Read/Write"
    Public Overrides Function read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
        Return ReadMode(buffer, offset, count, default_mode)
    End Function
    Public Function ReadMode(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal mode As InterfaceModes) As Integer
        If Not (buffer IsNot Nothing) Then Throw New ArgumentException()
        If Not (offset >= 0) Then Throw New ArgumentException()
        If Not (count >= 0) Then Throw New ArgumentException()
        If Not (offset + count <= buffer.Length) Then Throw New ArgumentException()

        Dim read_size = 0
        Dim total_size = 0

        'Read header
        If mode <> InterfaceModes.RawBytes Then
            If count < header_size Then
                Throw New IO.IOException("Header exceeds buffer size.")
            End If
            Do
                Dim n = substream.Read(read_header_buffer, read_size, header_size - read_size)
                If n = 0 Then Exit Do
                read_size += n
            Loop While read_size < header_size
            If read_size = 0 Then
                Return 0 'no data
            ElseIf read_size < header_size Then
                Throw New IO.IOException("Fragmented packet header.")
            End If

            'Parse header
            For i = header_size - 1 To pre_size Step -1
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
                For i = 0 To pre_size - 1
                    buffer(i + offset) = read_header_buffer(i)
                Next i
                read_size = pre_size
            Case InterfaceModes.IncludeSizeBytes
                For i = 0 To header_size - 1
                    buffer(i + offset) = read_header_buffer(i)
                Next i
                read_size = header_size
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

        logger.log(Function() "Received from {0}: {1}".frmt(log_destination, unpackHexString(subArray(buffer, offset, total_size))), LogMessageTypes.RawData)
        Return total_size
    End Function

    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        WriteMode(buffer, offset, count, default_mode)
    End Sub
    Public Sub WriteMode(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal mode As InterfaceModes)
        If buffer Is Nothing Then Throw New ArgumentNullException("buffer")
        If offset < 0 Then Throw New ArgumentOutOfRangeException("offset")
        If count < 0 Then Throw New ArgumentOutOfRangeException("count")
        If offset + count > buffer.Length Then Throw New ArgumentOutOfRangeException("offset+count")

        'Convert data format
        Select Case mode
            Case InterfaceModes.HideSizeBytes
                If count < pre_size Then Throw New ArgumentException("Data didn't include header data.")
                Dim data(0 To count + size_size - 1) As Byte
                For i = 0 To pre_size - 1
                    data(i) = buffer(offset + i)
                Next i
                For i = pre_size To count - 1
                    data(i + size_size) = buffer(offset + i)
                Next i
                buffer = data
                offset = 0
                count = data.Length
                encode_size(count, buffer, offset + pre_size)

            Case InterfaceModes.IncludeSizeBytes
                If count < header_size Then Throw New ArgumentException("Data didn't include header data.")
                encode_size(count, buffer, offset + pre_size)

            Case InterfaceModes.RawBytes
                'no changes needed

            Case Else
                Throw New NotSupportedException("Unrecognized interface mode.")
        End Select

        logger.log(Function() "Sending to {0}: {1}".frmt(log_destination, unpackHexString(subArray(buffer, offset, count))), LogMessageTypes.RawData)
        substream.Write(buffer, offset, count)
    End Sub

    Private Sub encode_size(ByVal size As Integer, ByVal buffer() As Byte, ByVal offset As Integer)
        If size < header_size Then
            Throw New ArgumentException("Packet size must include header.")
        End If
        For i = 0 To size_size - 1
            buffer(i + offset) = CByte(size And &HFF)
            size >>= 8
        Next i
        If size > 0 Then
            Throw New ArgumentException("Size exceeded encodable values.")
        End If
    End Sub
#End Region
End Class
