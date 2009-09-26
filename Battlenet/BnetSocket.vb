Imports System.Net
Imports System.Net.Sockets

Public Class BnetSocket
    Private WithEvents socket As PacketSocket
    Public Event Disconnected(ByVal sender As BnetSocket, ByVal expected As Boolean, ByVal reason As String)

    Public Sub New(ByVal socket As PacketSocket)
        Contract.Assume(socket IsNot Nothing)
        Me.socket = socket
    End Sub

    Public Property Logger() As Logger
        Get
            Return socket.logger
        End Get
        Set(ByVal value As Logger)
            socket.logger = value
        End Set
    End Property
    Public Property Name() As String
        Get
            Return socket.Name
        End Get
        Set(ByVal value As String)
            socket.Name = value
        End Set
    End Property

    Public ReadOnly Property LocalEndPoint As IPEndPoint
        Get
            Contract.Ensures(Contract.Result(Of IPEndPoint)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPEndPoint)().Address IsNot Nothing)
            Return socket.LocalEndPoint
        End Get
    End Property
    Public ReadOnly Property RemoteEndPoint As IPEndPoint
        Get
            Contract.Ensures(Contract.Result(Of IPEndPoint)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPEndPoint)().Address IsNot Nothing)
            Return socket.RemoteEndPoint
        End Get
    End Property
    Public Function connected() As Boolean
        Return socket.IsConnected
    End Function

    Private Sub CatchDisconnected(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String) Handles socket.Disconnected
        RaiseEvent Disconnected(Me, expected, reason)
    End Sub
    Public Sub disconnect(ByVal expected As Boolean, ByVal reason As String)
        Contract.Requires(reason IsNot Nothing)
        socket.Disconnect(expected, reason)
    End Sub

    Public Sub SendPacket(ByVal pk As Bnet.BnetPacket)
        Contract.Requires(pk IsNot Nothing)

        'Validate
        If socket Is Nothing OrElse Not socket.IsConnected OrElse pk Is Nothing Then
            Throw New InvalidOperationException("Socket is not connected")
        End If

        Try
            'Log
            Dim pk_ = pk
            Logger.Log(Function() "Sending {0} to {1}".Frmt(pk_.id, Name), LogMessageType.DataEvent)
            Logger.Log(pk.payload.Description, LogMessageType.DataParsed)

            'Send
            socket.WritePacket(Concat({Bnet.BnetPacket.PACKET_PREFIX, pk.id, 0, 0}, pk.payload.Data.ToArray))

        Catch e As Pickling.PicklingException
            Dim msg = "Error packing {0} for {1}: {2}".Frmt(pk.id, Name, e)
            Logger.Log(msg, LogMessageType.Problem)
            Throw
        Catch e As Exception
            Dim msg = "Error sending {0} to {1}: {2}".Frmt(pk.id, Name, e)
            LogUnexpectedException(msg, e)
            Logger.Log(msg, LogMessageType.Problem)
            Throw
        End Try
    End Sub

    Public Function FutureReadPacket() As IFuture(Of Bnet.BnetPacket)
        Return socket.FutureReadPacket().Select(
            Function(data)
                If data(0) <> Bnet.BnetPacket.PACKET_PREFIX Then
                    disconnect(expected:=False, reason:="Invalid packet prefix")
                    Throw New IO.IOException("Invalid packet prefix")
                End If
                Dim id = CType(data(1), Bnet.BnetPacketID)
                data = data.SubView(4)

                Try
                    'Handle
                    Logger.Log(Function() "Received {0} from {1}".Frmt(id, Name), LogMessageType.DataEvent)
                    Dim pk = Bnet.BnetPacket.FromData(id, data)
                    If pk.payload.Data.Length <> data.Length Then
                        Throw New Pickling.PicklingException("Data left over after parsing.")
                    End If
                    Logger.Log(pk.payload.Description, LogMessageType.DataParsed)
                    Return pk

                Catch e As Pickling.PicklingException
                    Dim msg = "(Ignored) Error parsing {0} from {1}: {2}".Frmt(id, Name, e)
                    Logger.Log(msg, LogMessageType.Negative)
                    Throw

                Catch e As Exception
                    Dim msg = "(Ignored) Error receiving {0} from {1}: {2}".Frmt(id, Name, e)
                    Logger.Log(msg, LogMessageType.Problem)
                    LogUnexpectedException(msg, e)
                    Throw
                End Try
            End Function
        )
    End Function
End Class

Public Class PacketSocket
    Private expectConnected As Boolean
    Private ReadOnly client As TcpClient
    Private ReadOnly substream As IO.Stream
    Private ReadOnly packetStreamer As PacketStreamer
    Private ReadOnly _remoteEndPoint As IPEndPoint
    Public Property logger As Logger
    Public Property Name() As String

    Public Const DefaultBufferSize As Integer = 1460 '[sending more data in a packet causes wc3 clients to disc; happens to be maximum ethernet header size]
    Public ReadOnly bufferSize As Integer
    Public Event Disconnected(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String)
    Private WithEvents deadManSwitch As DeadManSwitch

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(substream IsNot Nothing)
        Contract.Invariant(packetStreamer IsNot Nothing)
        Contract.Invariant(_remoteEndPoint IsNot Nothing)
        Contract.Invariant(_remoteEndPoint.Address IsNot Nothing)
    End Sub

    Public Sub New(ByVal client As TcpClient,
                   ByVal timeout As TimeSpan,
                   Optional ByVal logger As Logger = Nothing,
                   Optional ByVal WrappedStream As Func(Of IO.Stream, IO.Stream) = Nothing,
                   Optional ByVal bufferSize As Integer = DefaultBufferSize,
                   Optional ByVal numByteBeforeSize As Integer = 2,
                   Optional ByVal numSizeBytes As Integer = 2,
                   Optional ByVal name As String = Nothing)
        'contract bug wrt interface event implementation requires this:
        'Contract.Requires(client IsNot Nothing)
        Contract.Assume(client IsNot Nothing)

        Me.substream = client.GetStream
        If WrappedStream IsNot Nothing Then Me.substream = WrappedStream(Me.substream)
        Me.bufferSize = bufferSize
        Me.deadManSwitch = New DeadManSwitch(timeout, initiallyArmed:=True)
        Me.logger = If(logger, New Logger)
        Me.client = client
        Me.packetStreamer = New PacketStreamer(Me.substream, numByteBeforeSize, numSizeBytes, maxPacketSize:=bufferSize)
        Me.expectConnected = client.Connected
        _remoteEndPoint = CType(client.Client.RemoteEndPoint, Net.IPEndPoint)
        If RemoteEndPoint.Address.GetAddressBytes().HasSameItemsAs(GetCachedIpAddressBytes(external:=False)) OrElse
           RemoteEndPoint.Address.GetAddressBytes().HasSameItemsAs({127, 0, 0, 1}) Then
            _remoteEndPoint = New Net.IPEndPoint(New Net.IPAddress(GetCachedIpAddressBytes(external:=True)), RemoteEndPoint.Port)
        End If
        Contract.Assume(_remoteEndPoint IsNot Nothing)
        Contract.Assume(_remoteEndPoint.Address IsNot Nothing)
        Me.Name = If(name, Me.RemoteEndPoint.ToString)
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
            Return CType(client.Client.LocalEndPoint, Net.IPEndPoint)
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
        deadManSwitch.Dispose()
        substream.Close()
        ThreadPooledAction(Sub()
                               RaiseEvent Disconnected(Me, expected, reason)
                           End Sub)
    End Sub
    Private Sub DeadManSwitch_Triggered(ByVal sender As DeadManSwitch) Handles deadManSwitch.Triggered
        Disconnect(expected:=False, reason:="Connection went idle.")
    End Sub

    Public Function FutureReadPacket() As IFuture(Of ViewableList(Of Byte))
        'Async read packet
        Dim result = packetStreamer.FutureReadPacket()
        'Async handle receiving packet
        result.CallWhenValueReady(
            Sub(data, dataException)
                deadManSwitch.Reset()
                If dataException IsNot Nothing Then
                    Disconnect(expected:=False, reason:=dataException.ToString)
                Else
                    logger.Log(Function() "Received from {0}: {1}".Frmt(Name, data.ToHexString), LogMessageType.DataRaw)
                End If
            End Sub)
        Return result
    End Function

    Public Sub WritePacket(ByVal data() As Byte)
        Contract.Requires(data IsNot Nothing)
        packetStreamer.WritePacket(data)
        Dim data_ = data
        logger.log(Function() "Sending to {0}: {1}".frmt(Name, data_.ToHexString), LogMessageType.DataRaw)
    End Sub

    Public Sub WriteRawData(ByVal data() As Byte)
        Contract.Requires(data IsNot Nothing)
        substream.Write(data, 0, data.Length)
        Dim data_ = data
        logger.log(Function() "Sending to {0}: {1}".frmt(Name, data_.ToHexString), LogMessageType.DataRaw)
    End Sub
End Class
