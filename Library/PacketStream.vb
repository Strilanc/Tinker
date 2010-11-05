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

    Public Async Function AsyncReadPacket() As Task(Of IReadableList(Of Byte))
        Contract.Ensures(Contract.Result(Of Task(Of IReadableList(Of Byte)))() IsNot Nothing)

        Dim header = Await _subStream.ReadExactAsync(FullHeaderSize)
        Dim totalSize = CInt(header.Skip(_preheaderLength).Take(_sizeHeaderLength).ToUValue)
        If totalSize < FullHeaderSize Then Throw New IO.InvalidDataException("Invalid packet size (less than header size).")
        If totalSize > _maxPacketSize Then Throw New IO.InvalidDataException("Packet exceeded maximum size.")
        Dim body = Await _subStream.ReadExactAsync(totalSize - FullHeaderSize)

        Return Concat(header, body).ToReadableList()
    End Function

    'verification disabled due to stupid verifier (1.2.30118.5)
    <ContractVerification(False)>
    Public Function WritePacket(ByVal preheader As IEnumerable(Of Byte), ByVal payload As IEnumerable(Of Byte)) As IEnumerable(Of Byte)
        Contract.Requires(preheader IsNot Nothing)
        Contract.Requires(payload IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
        If preheader.Count <> _preheaderLength Then Throw New ArgumentException("Preheader was not of the correct size.", "preheader")

        Dim size = CULng(FullHeaderSize + payload.Count)
        If size >> (_sizeHeaderLength * 8) <> 0 Then Throw New ArgumentException("Too must data to count in size header.", "payload")
        Dim data = Concat(preheader, size.Bytes.Take(_sizeHeaderLength), payload).ToArray
        _subStream.Write(data, 0, data.Length)
        Return data
    End Function
End Class
