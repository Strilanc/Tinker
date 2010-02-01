Imports System.Net

Public NotInheritable Class PacketSocket
    Private _isConnected As Boolean
    Private ReadOnly _stream As IO.Stream
    Private ReadOnly packetStreamer As PacketStreamer
    Private ReadOnly _remoteEndPoint As IPEndPoint
    Private ReadOnly _localEndPoint As IPEndPoint
    Private _logger As Logger
    Private _name As InvariantString

    Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue()
    Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue()

    Public Const DefaultBufferSize As Integer = 1460 '[sending more data in a packet causes wc3 clients to disc; happens to be maximum ethernet header size]
    Public ReadOnly bufferSize As Integer
    Public Event Disconnected(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String)
    Private WithEvents deadManSwitch As DeadManSwitch

    Public Property Name As InvariantString
        Get
            Return Me._name
        End Get
        Set(ByVal value As InvariantString)
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
        Contract.Invariant(inQueue IsNot Nothing)
        Contract.Invariant(outQueue IsNot Nothing)
        Contract.Invariant(_stream IsNot Nothing)
        Contract.Invariant(packetStreamer IsNot Nothing)
        Contract.Invariant(_localEndPoint IsNot Nothing)
        Contract.Invariant(_localEndPoint.Address IsNot Nothing)
        Contract.Invariant(_localEndPoint.Port >= UInt16.MinValue)
        Contract.Invariant(_localEndPoint.Port <= UInt16.MaxValue)
        Contract.Invariant(_remoteEndPoint IsNot Nothing)
        Contract.Invariant(_remoteEndPoint.Address IsNot Nothing)
        Contract.Invariant(_remoteEndPoint.Port >= UInt16.MinValue)
        Contract.Invariant(_remoteEndPoint.Port <= UInt16.MaxValue)
        Contract.Invariant(_logger IsNot Nothing)
    End Sub

    Public Sub New(ByVal stream As IO.Stream,
                   ByVal localEndPoint As Net.IPEndPoint,
                   ByVal remoteEndPoint As Net.IPEndPoint,
                   ByVal clock As IClock,
                   Optional ByVal timeout As TimeSpan? = Nothing,
                   Optional ByVal logger As Logger = Nothing,
                   Optional ByVal bufferSize As Integer = DefaultBufferSize,
                   Optional ByVal numBytesBeforeSize As Integer = 2,
                   Optional ByVal numSizeBytes As Integer = 2,
                   Optional ByVal name As InvariantString? = Nothing)
        Contract.Assume(clock IsNot Nothing)
        Contract.Assume(stream IsNot Nothing)
        Contract.Assume(localEndPoint IsNot Nothing)
        Contract.Assume(remoteEndPoint IsNot Nothing)
        Contract.Assume(localEndPoint.Address IsNot Nothing)
        Contract.Assume(remoteEndPoint.Address IsNot Nothing)
        Contract.Assume(localEndPoint.Port >= UInt16.MinValue)
        Contract.Assume(localEndPoint.Port <= UInt16.MaxValue)
        Contract.Assume(remoteEndPoint.Port >= UInt16.MinValue)
        Contract.Assume(remoteEndPoint.Port <= UInt16.MaxValue)
        Contract.Assume(numBytesBeforeSize >= 0)
        Contract.Assume(numSizeBytes > 0)
        Contract.Assume(bufferSize >= numBytesBeforeSize + numSizeBytes)
        Contract.Assume(timeout Is Nothing OrElse timeout.Value.Ticks > 0)

        Me._stream = stream
        Me.bufferSize = bufferSize
        If timeout IsNot Nothing Then
            Me.deadManSwitch = New DeadManSwitch(timeout.Value, clock)
            Me.deadManSwitch.Arm()
        End If
        Me._logger = If(logger, New Logger)
        Me.packetStreamer = New PacketStreamer(Me._stream, numBytesBeforeSize, numSizeBytes, maxPacketSize:=bufferSize)
        Me._isConnected = True
        Me._remoteEndPoint = remoteEndPoint
        Me._localEndPoint = localEndPoint

        Dim addrBytes = remoteEndPoint.Address.GetAddressBytes
        If addrBytes.SequenceEqual(GetCachedIPAddressBytes(external:=False)) OrElse addrBytes.SequenceEqual({127, 0, 0, 1}) Then
            _remoteEndPoint = New Net.IPEndPoint(New Net.IPAddress(GetCachedIPAddressBytes(external:=True)), remoteEndPoint.Port)
            Contract.Assume(_remoteEndPoint.Address IsNot Nothing)
            Contract.Assume(_remoteEndPoint.Port >= UShort.MinValue)
            Contract.Assume(_remoteEndPoint.Port <= UShort.MaxValue)
        End If
        Me._name = If(name, New InvariantString(Me.RemoteEndPoint.ToString))
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
            Return _localEndPoint
        End Get
    End Property
    Public ReadOnly Property IsConnected() As Boolean
        Get
            Return _isConnected
        End Get
    End Property

    Public Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
        Contract.Requires(reason IsNot Nothing)
        inQueue.QueueAction(
            Sub()
                If Not _isConnected Then Return
                _isConnected = False
                If deadManSwitch IsNot Nothing Then deadManSwitch.Disarm()
                _stream.Close()
                outQueue.QueueAction(Sub() RaiseEvent Disconnected(Me, expected, reason))
            End Sub)
    End Sub
    Private Sub DeadManSwitch_Triggered(ByVal sender As DeadManSwitch) Handles deadManSwitch.Triggered
        Disconnect(expected:=False, reason:="Connection went idle.")
    End Sub

    Public Function AsyncReadPacket() As IFuture(Of IReadableList(Of Byte))
        Contract.Ensures(Contract.Result(Of IFuture(Of IReadableList(Of Byte)))() IsNot Nothing)
        'Read
        Dim result = packetStreamer.AsyncReadPacket()
        'Handle
        result.CallWhenValueReady(
            Sub(data, dataException)
                If deadManSwitch IsNot Nothing Then deadManSwitch.Reset()
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
        packetStreamer.WritePacket(data) '[modifies size bytes, do not move after log statement]
        Logger.Log(Function() "Sending to {0}: {1}".Frmt(Name, data.ToHexString), LogMessageType.DataRaw)
    End Sub

    Public Sub WriteRawData(ByVal data() As Byte)
        Contract.Requires(data IsNot Nothing)
        _stream.Write(data, 0, data.Length)
        Logger.Log(Function() "Sending to {0}: {1}".Frmt(Name, data.ToHexString), LogMessageType.DataRaw)
    End Sub

    Public ReadOnly Property SubStream As IO.Stream
        Get
            Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
            Return _stream
        End Get
    End Property
End Class
