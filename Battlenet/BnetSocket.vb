Imports System.Net
Imports System.Net.Sockets

Public Class BnetSocket
    Private WithEvents socket As PacketSocket
    Public Event Disconnected(ByVal sender As BnetSocket, ByVal reason As String)

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

    Private Sub CatchDisconnected(ByVal sender As PacketSocket, ByVal reason As String) Handles socket.Disconnected
        RaiseEvent Disconnected(Me, reason)
    End Sub
    Public Sub disconnect(ByVal reason As String)
        Contract.Requires(reason IsNot Nothing)
        socket.Disconnect(reason)
    End Sub

    Public Function SendPacket(ByVal pk As Bnet.BnetPacket) As Outcome
        Contract.Requires(pk IsNot Nothing)

        Try
            'Validate
            If socket Is Nothing OrElse Not socket.IsConnected OrElse pk Is Nothing Then
                Return failure("Socket is not connected")
            End If

            'Log
            Dim pk_ = pk
            Logger.log(Function() "Sending {0} to {1}".frmt(pk_.id, Name), LogMessageTypes.DataEvent)
            Logger.log(pk.payload.Description, LogMessageTypes.DataParsed)

            'Send
            socket.WritePacket(Concat({New Byte() {Bnet.BnetPacket.PACKET_PREFIX, pk.id, 0, 0}, pk.payload.Data.ToArray}))
            Return success("Sent")

        Catch e As Pickling.PicklingException
            Dim msg = "Error packing {0} for {1}: {2}".frmt(pk.id, Name, e)
            Logger.log(msg, LogMessageTypes.Problem)
            Return failure(msg)
        Catch e As Exception
            Dim msg = "Error sending {0} to {1}: {2}".frmt(pk.id, Name, e)
            Logging.LogUnexpectedException(msg, e)
            Logger.log(msg, LogMessageTypes.Problem)
            Return failure(msg)
        End Try
    End Function

    Public Function FutureReadPacket() As IFuture(Of PossibleException(Of Bnet.BnetPacket, Exception))
        Dim f = New Future(Of PossibleException(Of Bnet.BnetPacket, Exception))
        socket.FutureReadPacket().CallWhenValueReady(
            Sub(result)
                If result.Exception IsNot Nothing Then
                    f.SetValue(result.Exception)
                    Return
                End If

                Dim data = result.Value
                If data(0) <> Bnet.BnetPacket.PACKET_PREFIX Then
                    disconnect("Invalid packet prefix")
                    Throw New IO.IOException("Invalid packet prefix")
                End If
                Dim id = CType(data(1), Bnet.BnetPacketID)
                data = data.SubView(4)

                Try
                    'Handle
                    Logger.log(Function() "Received {0} from {1}".frmt(id, Name), LogMessageTypes.DataEvent)
                    Dim pk = Bnet.BnetPacket.FromData(id, data)
                    If pk.payload.Data.Length <> data.Length Then
                        Throw New Pickling.PicklingException("Data left over after parsing.")
                    End If
                    Logger.log(pk.payload.Description, LogMessageTypes.DataParsed)
                    f.SetValue(pk)

                Catch e As Pickling.PicklingException
                    Dim msg = "(Ignored) Error parsing {0} from {1}: {2}".frmt(id, Name, e)
                    Logger.log(msg, LogMessageTypes.Negative)
                    f.SetValue(e)

                Catch e As Exception
                    Dim msg = "(Ignored) Error receiving {0} from {1}: {2}".frmt(id, Name, e)
                    Logger.log(msg, LogMessageTypes.Problem)
                    Logging.LogUnexpectedException(msg, e)
                    f.SetValue(e)
                End Try
            End Sub
        )
        Return f
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
    Public Event Disconnected(ByVal sender As PacketSocket, ByVal reason As String)
    Private WithEvents deadManSwitch As DeadManSwitch

    <ContractInvariantMethod()> Protected Sub Invariant()
        Contract.Invariant(substream IsNot Nothing)
        Contract.Invariant(packetStreamer IsNot Nothing)
        Contract.Invariant(_remoteEndPoint IsNot Nothing)
        Contract.Invariant(_remoteEndPoint.Address IsNot Nothing)
    End Sub

    Public Sub New(ByVal client As TcpClient,
                   ByVal timeout As TimeSpan,
                   Optional ByVal logger As Logger = Nothing,
                   Optional ByVal streamWrapper As Func(Of IO.Stream, IO.Stream) = Nothing,
                   Optional ByVal bufferSize As Integer = DefaultBufferSize)
        'contract bug wrt interface event implementation requires this:
        'Contract.Requires(client IsNot Nothing)
        Contract.Assume(client IsNot Nothing)

        Me.substream = client.GetStream
        If streamWrapper IsNot Nothing Then Me.substream = streamWrapper(Me.substream)
        Me.bufferSize = bufferSize
        Me.deadManSwitch = New DeadManSwitch(timeout, initiallyArmed:=True)
        Me.logger = If(logger, New Logger)
        Me.client = client
        Me.packetStreamer = New PacketStreamer(Me.substream, numBytesBeforeSize:=2, numSizeBytes:=2, maxPacketSize:=bufferSize)
        Me.expectConnected = client.Connected
        _remoteEndPoint = CType(client.Client.RemoteEndPoint, Net.IPEndPoint)
        If ArraysEqual(RemoteEndPoint.Address.GetAddressBytes(), GetCachedIpAddressBytes(external:=False)) OrElse
           ArraysEqual(RemoteEndPoint.Address.GetAddressBytes(), {127, 0, 0, 1}) Then
            _remoteEndPoint = New Net.IPEndPoint(New Net.IPAddress(GetCachedIpAddressBytes(external:=True)), RemoteEndPoint.Port)
        End If
        Contract.Assume(_remoteEndPoint IsNot Nothing)
        Contract.Assume(_remoteEndPoint.Address IsNot Nothing)
        Me.Name = Me.RemoteEndPoint.ToString
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
    Public Sub Disconnect(ByVal reason As String)
        Contract.Requires(reason IsNot Nothing)
        Dim _reason = reason  'avoid problems with contract verification on hoisted arguments
        If Not expectConnected Then Return
        expectConnected = False
        client.Close()
        deadManSwitch.Dispose()
        substream.Close()
        ThreadPooledAction(
            Sub()
                RaiseEvent Disconnected(Me, _reason)
            End Sub
        )
    End Sub
    Private Sub DeadManSwitch_Triggered(ByVal sender As DeadManSwitch) Handles deadManSwitch.Triggered
        Disconnect("Connection went idle.")
    End Sub

    Public Function FutureReadPacket() As IFuture(Of PossibleException(Of ViewableList(Of Byte), Exception))
        Dim f = packetStreamer.FutureReadPacket()
        f.CallWhenValueReady(Sub(result)
                                 deadManSwitch.Reset()
                                 If result.Exception IsNot Nothing Then
                                     Disconnect(result.Exception.ToString)
                                 ElseIf result.Value IsNot Nothing Then
                                     logger.log(Function() "Received from {0}: {1}".frmt(Name, result.Value.ToHexString), LogMessageTypes.DataRaw)
                                 End If
                             End Sub)
        Return f
    End Function

    Public Sub WritePacket(ByVal data() As Byte)
        Contract.Requires(data IsNot Nothing)
        packetStreamer.WritePacket(data)
        Dim data_ = data
        logger.log(Function() "Sending to {0}: {1}".frmt(Name, data_.ToHexString), LogMessageTypes.DataRaw)
    End Sub

    Public Sub WriteRawData(ByVal data() As Byte)
        Contract.Requires(data IsNot Nothing)
        substream.Write(data, 0, data.Length)
        Dim data_ = data
        logger.log(Function() "Sending to {0}: {1}".frmt(Name, data_.ToHexString), LogMessageTypes.DataRaw)
    End Sub
End Class
