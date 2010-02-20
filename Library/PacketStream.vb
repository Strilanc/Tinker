''' <summary>
''' Wraps streams so that reads and writes force data into chunks with headers including their size.
''' </summary>
Public NotInheritable Class PacketStreamer
    Private ReadOnly _subStream As IO.Stream
    Private ReadOnly _preheaderLength As Integer
    Private ReadOnly _sizeHeaderLength As Integer
    Private ReadOnly _maxPacketSize As Integer

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_preheaderLength >= 0)
        Contract.Invariant(_sizeHeaderLength > 0)
        Contract.Invariant(_maxPacketSize > 0)
        Contract.Invariant(_subStream IsNot Nothing)
    End Sub

    Public Sub New(ByVal subStream As IO.Stream,
                   ByVal preheaderLength As Integer,
                   ByVal sizeHeaderLength As Integer,
                   ByVal maxPacketSize As Integer)
        Contract.Requires(subStream IsNot Nothing)
        Contract.Requires(preheaderLength >= 0)
        Contract.Requires(sizeHeaderLength > 0)
        Contract.Requires(maxPacketSize >= preheaderLength + sizeHeaderLength)

        Me._maxPacketSize = maxPacketSize
        Me._subStream = subStream
        Me._sizeHeaderLength = sizeHeaderLength
        Me._preheaderLength = preheaderLength
    End Sub

    Private ReadOnly Property FullHeaderSize As Integer
        Get
            Return _sizeHeaderLength + _preheaderLength
        End Get
    End Property

    Public Function AsyncReadPacket() As IFuture(Of IReadableList(Of Byte))
        Contract.Ensures(Contract.Result(Of IFuture(Of IReadableList(Of Byte)))() IsNot Nothing)
        Dim readSize = 0
        Dim totalSize = 0
        Dim packetData(0 To FullHeaderSize - 1) As Byte
        Dim result = New FutureFunction(Of IReadableList(Of Byte))

        FutureIterate(Function() _subStream.AsyncRead(packetData, readSize, packetData.Length - readSize),
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
                If readSize = FullHeaderSize Then
                    totalSize = CInt(packetData.SubArray(_preheaderLength, _sizeHeaderLength).ToUInt32())
                    If totalSize < FullHeaderSize Then
                        'too small
                        result.SetFailed(New IO.InvalidDataException("Invalid packet size (less than header size)."))
                        Return False.Futurized
                    ElseIf totalSize > _maxPacketSize Then
                        'too large
                        result.SetFailed(New IO.InvalidDataException("Packet exceeded maximum size."))
                        Return False.Futurized
                    ElseIf totalSize > FullHeaderSize Then
                        'begin reading packet body
                        ReDim Preserve packetData(0 To totalSize - 1)
                        Return True.Futurized
                    End If
                End If

                'Finished reading
                result.SetSucceeded(packetData.AsReadableList)
                Return False.Futurized
            End Function
        )

        Return result
    End Function

    'verification disabled due to stupid verifier (1.2.30118.5)
    <ContractVerification(False)>
    Public Sub WritePacket(ByVal packetData As Byte())
        Contract.Requires(packetData IsNot Nothing)
        If packetData.Length < FullHeaderSize Then Throw New ArgumentException("Data didn't include header data.")

        'Encode size
        Dim sizeBytes = CULng(packetData.Length).Bytes.SubArray(0, _sizeHeaderLength)
        System.Array.Copy(sourceArray:=sizeBytes,
                          sourceIndex:=0,
                          destinationArray:=packetData,
                          destinationIndex:=_preheaderLength,
                          length:=_sizeHeaderLength)

        _subStream.Write(packetData, 0, packetData.Length)
    End Sub
End Class
