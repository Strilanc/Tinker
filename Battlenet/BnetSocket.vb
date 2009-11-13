Imports System.Net
Imports System.Net.Sockets

Public NotInheritable Class BnetSocket
    Private WithEvents _socket As PacketSocket
    Public Event Disconnected(ByVal sender As BnetSocket, ByVal expected As Boolean, ByVal reason As String)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_socket IsNot Nothing)
    End Sub

    Public Sub New(ByVal socket As PacketSocket)
        Contract.Assume(socket IsNot Nothing)
        Me._socket = socket
    End Sub

    Public Property Logger() As Logger
        Get
            Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
            Return _socket.Logger
        End Get
        Set(ByVal value As Logger)
            Contract.Requires(value IsNot Nothing)
            _socket.Logger = value
        End Set
    End Property
    Public Property Name() As String
        Get
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return _socket.Name
        End Get
        Set(ByVal value As String)
            Contract.Requires(value IsNot Nothing)
            _socket.Name = value
        End Set
    End Property

    Public ReadOnly Property LocalEndPoint As IPEndPoint
        Get
            Contract.Ensures(Contract.Result(Of IPEndPoint)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPEndPoint)().Address IsNot Nothing)
            Return _socket.LocalEndPoint
        End Get
    End Property
    Public ReadOnly Property RemoteEndPoint As IPEndPoint
        Get
            Contract.Ensures(Contract.Result(Of IPEndPoint)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPEndPoint)().Address IsNot Nothing)
            Return _socket.RemoteEndPoint
        End Get
    End Property
    Public Function IsConnected() As Boolean
        Return _socket.IsConnected
    End Function

    Private Sub CatchDisconnected(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String) Handles _socket.Disconnected
        Contract.Requires(sender IsNot Nothing)
        Contract.Requires(reason IsNot Nothing)
        RaiseEvent Disconnected(Me, expected, reason)
    End Sub
    Public Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
        Contract.Requires(reason IsNot Nothing)
        _socket.Disconnect(expected, reason)
    End Sub

    Public Sub SendPacket(ByVal packet As Bnet.Packet)
        Contract.Requires(packet IsNot Nothing)

        'Validate
        If Not _socket.IsConnected Then
            Throw New InvalidOperationException("Socket is not connected")
        End If

        Try
            'Log
            Logger.Log(Function() "Sending {0} to {1}".Frmt(packet.id, Name), LogMessageType.DataEvent)
            Logger.Log(packet.Payload.Description, LogMessageType.DataParsed)

            'Send
            _socket.WritePacket(Concat({Bnet.Packet.PacketPrefixValue, packet.id, 0, 0},
                                      packet.Payload.Data.ToArray))

        Catch e As Pickling.PicklingException
            Dim msg = "Error packing {0} for {1}: {2}".Frmt(packet.id, Name, e)
            Logger.Log(msg, LogMessageType.Problem)
            Throw
        Catch e As Exception
            Dim msg = "Error sending {0} to {1}: {2}".Frmt(packet.id, Name, e)
            e.RaiseAsUnexpected(msg)
            Logger.Log(msg, LogMessageType.Problem)
            Throw
        End Try
    End Sub

    Public Function FutureReadPacket() As IFuture(Of Bnet.Packet)
        Contract.Ensures(Contract.Result(Of IFuture(Of Bnet.Packet))() IsNot Nothing)
        Return _socket.FutureReadPacket().Select(
            Function(data)
                Contract.Assume(data IsNot Nothing)
                If data.Length < 4 Then
                    Disconnect(expected:=False, reason:="Packer didn't include a header.")
                    Throw New IO.InvalidDataException("Invalid packet prefix")
                ElseIf data(0) <> Bnet.Packet.PacketPrefixValue Then
                    Disconnect(expected:=False, reason:="Invalid packet prefix")
                    Throw New IO.InvalidDataException("Invalid packet prefix")
                End If
                Dim id = CType(data(1), Bnet.PacketId)
                data = data.SubView(4)

                Try
                    'Handle
                    Logger.Log(Function() "Received {0} from {1}".Frmt(id, Name), LogMessageType.DataEvent)
                    Dim pk = Bnet.Packet.FromData(id, data)
                    If pk.Payload.Data.Length <> data.Length Then
                        Throw New Pickling.PicklingException("Data left over after parsing.")
                    End If
                    Logger.Log(pk.Payload.Description, LogMessageType.DataParsed)
                    Return pk

                Catch e As Pickling.PicklingException
                    Dim msg = "(Ignored) Error parsing {0} from {1}: {2}".Frmt(id, Name, e)
                    Logger.Log(msg, LogMessageType.Negative)
                    Throw

                Catch e As Exception
                    Dim msg = "(Ignored) Error receiving {0} from {1}: {2}".Frmt(id, Name, e)
                    Logger.Log(msg, LogMessageType.Problem)
                    e.RaiseAsUnexpected(msg)
                    Throw
                End Try
            End Function
        )
    End Function

    Public ReadOnly Property Socket As PacketSocket
        Get
            Contract.Ensures(Contract.Result(Of PacketSocket)() IsNot Nothing)
            Return _socket
        End Get
    End Property
End Class

Public NotInheritable Class PacketSocket
    Private expectConnected As Boolean
    Private ReadOnly client As TcpClient
    Private ReadOnly _subStream As IO.Stream
    Private ReadOnly packetStreamer As PacketStreamer
    Private ReadOnly _remoteEndPoint As IPEndPoint
    Private _logger As Logger
    Private _name As String

    Public Const DefaultBufferSize As Integer = 1460 '[sending more data in a packet causes wc3 clients to disc; happens to be maximum ethernet header size]
    Public ReadOnly bufferSize As Integer
    Public Event Disconnected(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String)
    Private WithEvents deadManSwitch As DeadManSwitch

    Public Property Name As String
        Get
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return Me._name
        End Get
        Set(ByVal value As String)
            Contract.Requires(value IsNot Nothing)
            Me._name = value
        End Set
    End Property
    Public Property Logger As Logger
        Get
            Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
            Return Me._logger
        End Get
        Set(ByVal value As Logger)
            Contract.Requires(value IsNot Nothing)
            Me._logger = value
        End Set
    End Property

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_subStream IsNot Nothing)
        Contract.Invariant(packetStreamer IsNot Nothing)
        Contract.Invariant(deadManSwitch IsNot Nothing)
        Contract.Invariant(_remoteEndPoint IsNot Nothing)
        Contract.Invariant(_remoteEndPoint.Address IsNot Nothing)
        Contract.Invariant(_logger IsNot Nothing)
        Contract.Invariant(_name IsNot Nothing)
        Contract.Invariant(client IsNot Nothing)
    End Sub

    Public Sub New(ByVal client As TcpClient,
                   Optional ByVal timeout As TimeSpan? = Nothing,
                   Optional ByVal logger As Logger = Nothing,
                   Optional ByVal streamWrapper As Func(Of IO.Stream, IO.Stream) = Nothing,
                   Optional ByVal bufferSize As Integer = DefaultBufferSize,
                   Optional ByVal numBytesBeforeSize As Integer = 2,
                   Optional ByVal numSizeBytes As Integer = 2,
                   Optional ByVal name As String = Nothing)
        Contract.Assume(client IsNot Nothing) 'bug in contracts required not using requires here

        Me._subStream = client.GetStream
        If streamWrapper IsNot Nothing Then Me._subStream = streamWrapper(Me._subStream)
        Me.bufferSize = bufferSize
        If timeout IsNot Nothing Then
            Me.deadManSwitch = New DeadManSwitch(timeout.Value, initiallyArmed:=True)
        End If
        Me._logger = If(logger, New Logger)
        Me.client = client
        Me.packetStreamer = New PacketStreamer(Me._subStream, numBytesBeforeSize, numSizeBytes, maxPacketSize:=bufferSize)
        Me.expectConnected = client.Connected
        _remoteEndPoint = CType(client.Client.RemoteEndPoint, Net.IPEndPoint)
        If RemoteEndPoint.Address.GetAddressBytes().HasSameItemsAs(GetCachedIPAddressBytes(external:=False)) OrElse
           RemoteEndPoint.Address.GetAddressBytes().HasSameItemsAs({127, 0, 0, 1}) Then
            _remoteEndPoint = New Net.IPEndPoint(New Net.IPAddress(GetCachedIPAddressBytes(external:=True)), RemoteEndPoint.Port)
        End If
        Contract.Assume(_remoteEndPoint IsNot Nothing)
        Contract.Assume(_remoteEndPoint.Address IsNot Nothing)
        Me._name = If(name, Me.RemoteEndPoint.ToString)
    End Sub

    Public ReadOnly Property RemoteEndPoint As IPEndPoint
        Get
            Contract.Ensures(Contract.Result(Of IPEndPoint)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPEndPoint)().Address IsNot Nothing)
            Return _remoteEndPoint
        End Get
    End Property
    Public ReadOnly Property LocalEndPoint As IPEndPoint
        Get
            Contract.Ensures(Contract.Result(Of IPEndPoint)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPEndPoint)().Address IsNot Nothing)
            Contract.Assume(client.Client IsNot Nothing)
            Dim result = CType(client.Client.LocalEndPoint, Net.IPEndPoint)
            Contract.Assume(result IsNot Nothing)
            Contract.Assume(result.Address IsNot Nothing)
            Return result
        End Get
    End Property
    Public Function IsConnected() As Boolean
        Return client.Connected
    End Function
    Public Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
        Contract.Requires(reason IsNot Nothing)
        If Not expectConnected Then Return
        expectConnected = False
        client.Close()
        If deadManSwitch IsNot Nothing Then deadManSwitch.Dispose()
        _subStream.Close()
        ThreadPooledAction(Sub()
                               RaiseEvent Disconnected(Me, expected, reason)
                           End Sub)
    End Sub
    Private Sub DeadManSwitch_Triggered(ByVal sender As DeadManSwitch) Handles deadManSwitch.Triggered
        Disconnect(expected:=False, reason:="Connection went idle.")
    End Sub

    Public Function FutureReadPacket() As IFuture(Of ViewableList(Of Byte))
        Contract.Ensures(Contract.Result(Of IFuture(Of ViewableList(Of Byte)))() IsNot Nothing)
        'Async read packet
        Dim result = packetStreamer.FutureReadPacket()
        'Async handle receiving packet
        result.CallWhenValueReady(
            Sub(data, dataException)
                If deadManSwitch IsNot Nothing Then deadManSwitch.Reset()
                If dataException IsNot Nothing Then
                    Disconnect(expected:=False, reason:=dataException.ToString)
                Else
                    Logger.Log(Function() "Received from {0}: {1}".Frmt(Name, data.AssumeNotNull.ToHexString), LogMessageType.DataRaw)
                End If
            End Sub)
        Return result
    End Function

    Public Sub WritePacket(ByVal data() As Byte)
        Contract.Requires(data IsNot Nothing)
        packetStreamer.WritePacket(data)
        Logger.Log(Function() "Sending to {0}: {1}".Frmt(Name, data.AssumeNotNull.ToHexString), LogMessageType.DataRaw)
    End Sub

    Public Sub WriteRawData(ByVal data() As Byte)
        Contract.Requires(data IsNot Nothing)
        _subStream.Write(data, 0, data.Length)
        Logger.Log(Function() "Sending to {0}: {1}".Frmt(Name, data.AssumeNotNull.ToHexString), LogMessageType.DataRaw)
    End Sub

    Public ReadOnly Property SubStream As IO.Stream
        Get
            Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
            Return _subStream
        End Get
    End Property
End Class
