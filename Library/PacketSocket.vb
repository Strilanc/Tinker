Imports System.Net

Public NotInheritable Class PacketSocket
    Private _isConnected As Boolean
    Private ReadOnly _stream As IO.Stream
    Private ReadOnly packetStreamer As PacketStreamer
    Private ReadOnly _remoteEndPoint As IPEndPoint
    Private ReadOnly _localEndPoint As IPEndPoint
    Public Property Name As InvariantString
    Public Property Logger As Logger

    Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue()
    Private ReadOnly outQueue As CallQueue = MakeTaskedCallQueue()

    Public Const DefaultBufferSize As Integer = 1460 '[sending more data in a packet causes wc3 clients to disc; happens to be maximum ethernet header size]
    Public ReadOnly bufferSize As Integer
    Public Event Disconnected(sender As PacketSocket, expected As Boolean, reason As String)
    Private WithEvents deadManSwitch As DeadManSwitch

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
        Contract.Invariant(Logger IsNot Nothing)
    End Sub

    Public Sub New(stream As IO.Stream,
                   localEndPoint As Net.IPEndPoint,
                   remoteEndPoint As Net.IPEndPoint,
                   clock As IClock,
                   Optional timeout As TimeSpan? = Nothing,
                   Optional logger As Logger = Nothing,
                   Optional bufferSize As Integer = DefaultBufferSize,
                   Optional preheaderLength As Integer = 2,
                   Optional sizeHeaderLength As Integer = 2,
                   Optional name As InvariantString? = Nothing)
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
        Contract.Assume(preheaderLength >= 0)
        Contract.Assume(sizeHeaderLength > 0)
        Contract.Assume(bufferSize >= preheaderLength + sizeHeaderLength)
        Contract.Assume(timeout Is Nothing OrElse timeout.Value.Ticks > 0)

        Me._stream = stream
        Me.bufferSize = bufferSize
        If timeout IsNot Nothing Then
            Me.deadManSwitch = New DeadManSwitch(timeout.Value, clock)
            Me.deadManSwitch.QueueArm()
        End If
        Me._logger = If(logger, New Logger)
        Me.packetStreamer = New PacketStreamer(Me._stream, preheaderLength, sizeHeaderLength, maxPacketSize:=bufferSize)
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
    Public Shared Async Function AsyncConnect(remoteHost As String,
                                              remotePort As UShort,
                                              clock As IClock,
                                              Optional timeout As TimeSpan? = Nothing,
                                              Optional logger As Logger = Nothing,
                                              Optional bufferSize As Integer = DefaultBufferSize,
                                              Optional preheaderLength As Integer = 2,
                                              Optional sizeHeaderLength As Integer = 2,
                                              Optional name As InvariantString? = Nothing) As Task(Of PacketSocket)
        Contract.Assume(remoteHost IsNot Nothing)
        Contract.Assume(clock IsNot Nothing)
        Contract.Assume(preheaderLength >= 0)
        Contract.Assume(sizeHeaderLength > 0)
        Contract.Assume(bufferSize >= preheaderLength + sizeHeaderLength)
        Contract.Assume(timeout Is Nothing OrElse timeout.Value.Ticks > 0)
        'Contract.Ensures(Contract.Result(Of Task(Of PacketSocket))() IsNot Nothing)
        Dim socket = Await AsyncTcpConnect(remoteHost, remotePort)
        Return New PacketSocket(socket.GetStream,
                                DirectCast(socket.Client.LocalEndPoint, Net.IPEndPoint),
                                DirectCast(socket.Client.RemoteEndPoint, Net.IPEndPoint),
                                clock,
                                timeout,
                                logger,
                                bufferSize,
                                preheaderLength,
                                sizeHeaderLength,
                                name)
    End Function

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
    Public ReadOnly Property SubStream As IO.Stream
        Get
            Contract.Ensures(Contract.Result(Of IO.Stream)() IsNot Nothing)
            Return _stream
        End Get
    End Property
    Public ReadOnly Property IsConnected() As Boolean
        Get
            Return _isConnected
        End Get
    End Property

    Private Sub Disconnect(expected As Boolean, reason As String)
        Contract.Requires(reason IsNot Nothing)
        If Not _isConnected Then Return
        _isConnected = False
        If deadManSwitch IsNot Nothing Then deadManSwitch.QueueDisarm()
        _stream.Close()
        outQueue.QueueAction(Sub() RaiseEvent Disconnected(Me, expected, reason))
    End Sub
    Public Function QueueDisconnect(expected As Boolean, reason As String) As Task
        Contract.Requires(reason IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Return inQueue.QueueAction(Sub() Disconnect(expected, reason))
    End Function
    Private Sub DeadManSwitch_Triggered(sender As DeadManSwitch) Handles DeadManSwitch.Triggered
        Disconnect(expected:=False, reason:="Connection went idle.")
    End Sub

    Public Async Function AsyncReadPacket() As Task(Of IRist(Of Byte))
        'Contract.Ensures(Contract.Result(Of Task(Of IRist(Of Byte)))() IsNot Nothing)
        Try
            Dim data = Await packetStreamer.AsyncReadPacket()
            If deadManSwitch IsNot Nothing Then deadManSwitch.QueueReset()
            Logger.Log(Function() "Received from {0}: {1}".Frmt(Name, data.ToHexString), LogMessageType.DataRaw)
            Return data
        Catch ex As Exception
            If _isConnected Then ex.RaiseAsUnexpected("Receiving packet")
            QueueDisconnect(expected:=False, reason:=ex.Summarize)
            Throw
        End Try
    End Function

    Public Sub WritePacket(preheader As IEnumerable(Of Byte), payload As IEnumerable(Of Byte))
        Contract.Requires(preheader IsNot Nothing)
        Contract.Requires(payload IsNot Nothing)
        Dim writtenData = packetStreamer.WritePacket(preheader, payload)
        Logger.Log(Function() "Sending to {0}: {1}".Frmt(Name, writtenData.ToHexString), LogMessageType.DataRaw)
    End Sub

    Public Sub WriteRawData(data() As Byte)
        Contract.Requires(data IsNot Nothing)
        _stream.Write(data, 0, data.Length)
        Logger.Log(Function() "Sending to {0}: {1}".Frmt(Name, data.ToHexString), LogMessageType.DataRaw)
    End Sub
End Class
