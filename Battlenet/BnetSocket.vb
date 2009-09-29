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
            Return socket.Logger
        End Get
        Set(ByVal value As Logger)
            socket.Logger = value
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
    Public Function IsConnected() As Boolean
        Return socket.IsConnected
    End Function

    Private Sub CatchDisconnected(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String) Handles socket.Disconnected
        RaiseEvent Disconnected(Me, expected, reason)
    End Sub
    Public Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
        Contract.Requires(reason IsNot Nothing)
        socket.Disconnect(expected, reason)
    End Sub

    Public Sub SendPacket(ByVal packet As Bnet.BnetPacket)
        Contract.Requires(packet IsNot Nothing)

        'Validate
        If socket Is Nothing OrElse Not socket.IsConnected OrElse packet Is Nothing Then
            Throw New InvalidOperationException("Socket is not connected")
        End If

        Try
            'Log
            Logger.Log(Function() "Sending {0} to {1}".Frmt(packet.id, Name), LogMessageType.DataEvent)
            Logger.Log(packet.payload.Description, LogMessageType.DataParsed)

            'Send
            socket.WritePacket(Concat({Bnet.BnetPacket.PacketPrefixValue, packet.id, 0, 0},
                                      packet.payload.Data.ToArray))

        Catch e As Pickling.PicklingException
            Dim msg = "Error packing {0} for {1}: {2}".Frmt(packet.id, Name, e)
            Logger.Log(msg, LogMessageType.Problem)
            Throw
        Catch e As Exception
            Dim msg = "Error sending {0} to {1}: {2}".Frmt(packet.id, Name, e)
            LogUnexpectedException(msg, e)
            Logger.Log(msg, LogMessageType.Problem)
            Throw
        End Try
    End Sub

    Public Function FutureReadPacket() As IFuture(Of Bnet.BnetPacket)
        Contract.Ensures(Contract.Result(Of IFuture(Of Bnet.BnetPacket))() IsNot Nothing)
        Return socket.FutureReadPacket().Select(
            Function(data)
                Contract.Assume(data IsNot Nothing)
                If data.Length < 4 Then
                    Disconnect(expected:=False, reason:="Packer didn't include a header.")
                    Throw New IO.IOException("Invalid packet prefix")
                ElseIf data(0) <> Bnet.BnetPacket.PacketPrefixValue Then
                    Disconnect(expected:=False, reason:="Invalid packet prefix")
                    Throw New IO.IOException("Invalid packet prefix")
                End If
                Dim id = CType(data(1), Bnet.BnetPacketId)
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
    Private ReadOnly subStream As IO.Stream
    Private ReadOnly packetStreamer As PacketStreamer
    Private ReadOnly _remoteEndPoint As IPEndPoint
    Public Property Logger As Logger
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
                   Optional ByVal streamWrapper As Func(Of IO.Stream, IO.Stream) = Nothing,
                   Optional ByVal bufferSize As Integer = DefaultBufferSize,
                   Optional ByVal numBytesBeforeSize As Integer = 2,
                   Optional ByVal numSizeBytes As Integer = 2,
                   Optional ByVal name As String = Nothing)
        Contract.Assume(client IsNot Nothing) 'bug in contracts required not using requires here

        Me.subStream = client.GetStream
        If streamWrapper IsNot Nothing Then Me.subStream = streamWrapper(Me.subStream)
        Me.bufferSize = bufferSize
        Me.deadManSwitch = New DeadManSwitch(timeout, initiallyArmed:=True)
        Me.Logger = If(logger, New Logger)
        Me.client = client
        Me.packetStreamer = New PacketStreamer(Me.subStream, numBytesBeforeSize, numSizeBytes, maxPacketSize:=bufferSize)
        Me.expectConnected = client.Connected
        _remoteEndPoint = CType(client.Client.RemoteEndPoint, Net.IPEndPoint)
        If RemoteEndPoint.Address.GetAddressBytes().HasSameItemsAs(GetCachedIPAddressBytes(external:=False)) OrElse
           RemoteEndPoint.Address.GetAddressBytes().HasSameItemsAs({127, 0, 0, 1}) Then
            _remoteEndPoint = New Net.IPEndPoint(New Net.IPAddress(GetCachedIPAddressBytes(external:=True)), RemoteEndPoint.Port)
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
                    Logger.Log(Function() "Received from {0}: {1}".Frmt(Name, data.ToHexString), LogMessageType.DataRaw)
                End If
            End Sub)
        Return result
    End Function

    Public Sub WritePacket(ByVal data() As Byte)
        Contract.Requires(data IsNot Nothing)
        packetStreamer.WritePacket(data)
        Logger.Log(Function() "Sending to {0}: {1}".Frmt(Name, data.ToHexString), LogMessageType.DataRaw)
    End Sub

    Public Sub WriteRawData(ByVal data() As Byte)
        Contract.Requires(data IsNot Nothing)
        substream.Write(data, 0, data.Length)
        Logger.Log(Function() "Sending to {0}: {1}".Frmt(Name, data.ToHexString), LogMessageType.DataRaw)
    End Sub
End Class
