''' <summary>
''' Wraps streams so that reads and writes force data into chunks with headers including their size.
''' </summary>
Public NotInheritable Class PacketStreamer
    Private ReadOnly subStream As IO.Stream
    Private ReadOnly headerBytesBeforeSizeCount As Integer
    Private ReadOnly headerValueSizeByteCount As Integer
    Private ReadOnly maxPacketSize As Integer

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(headerBytesBeforeSizeCount >= 0)
        Contract.Invariant(headerValueSizeByteCount > 0)
        Contract.Invariant(maxPacketSize > 0)
        Contract.Invariant(subStream IsNot Nothing)
    End Sub

    Private ReadOnly Property HeaderSize As Integer
        Get
            Return headerValueSizeByteCount + headerBytesBeforeSizeCount
        End Get
    End Property

    Public Sub New(ByVal subStream As IO.Stream,
                   ByVal headerBytesBeforeSizeCount As Integer,
                   ByVal headerValueSizeByteCount As Integer,
                   ByVal maxPacketSize As Integer)
        Contract.Requires(subStream IsNot Nothing)
        Contract.Requires(headerBytesBeforeSizeCount >= 0)
        Contract.Requires(headerValueSizeByteCount > 0)
        Contract.Requires(maxPacketSize >= headerBytesBeforeSizeCount + headerValueSizeByteCount)

        Me.maxPacketSize = maxPacketSize
        Me.subStream = subStream
        Me.headerValueSizeByteCount = headerValueSizeByteCount
        Me.headerBytesBeforeSizeCount = headerBytesBeforeSizeCount
    End Sub

    Public Function FutureReadPacket() As IFuture(Of ViewableList(Of Byte))
        Contract.Ensures(Contract.Result(Of IFuture(Of ViewableList(Of Byte)))() IsNot Nothing)
        Dim readSize = 0
        Dim totalSize = 0
        Dim packetData(0 To HeaderSize - 1) As Byte
        Dim result = New FutureFunction(Of ViewableList(Of Byte))

        FutureIterate(Function() subStream.FutureRead(packetData, readSize, packetData.Length - readSize),
            Function(numBytesRead, readException)
                'Check result
                If readException IsNot Nothing Then 'read failed
                    result.SetFailed(readException)
                    Return False.Futurized
                ElseIf numBytesRead <= 0 Then 'subStream ended
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
                If readSize = HeaderSize Then
                    totalSize = CInt(packetData.SubArray(headerBytesBeforeSizeCount, headerValueSizeByteCount).ToUInt32())
                    If totalSize < HeaderSize Then
                        'too small
                        result.SetFailed(New IO.InvalidDataException("Invalid packet size (less than header size)."))
                        Return False.Futurized
                    ElseIf totalSize > maxPacketSize Then
                        'too large
                        result.SetFailed(New IO.InvalidDataException("Packet exceeded maximum size."))
                        Return False.Futurized
                    ElseIf totalSize > HeaderSize Then
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

    Public Sub WritePacket(ByVal packetData As Byte())
        Contract.Requires(packetData IsNot Nothing)
        If packetData.Length < HeaderSize Then Throw New ArgumentException("Data didn't include header data.")

        'Encode size
        Dim sizeBytes = CULng(packetData.Length).Bytes(size:=headerValueSizeByteCount)
        System.Array.Copy(sourceArray:=sizeBytes,
                          sourceIndex:=0,
                          destinationArray:=packetData,
                          destinationIndex:=headerBytesBeforeSizeCount,
                          length:=headerValueSizeByteCount)

        subStream.Write(packetData, 0, packetData.Length)
    End Sub
End Class
