Imports System.Net.Sockets

Public Class BnetSocket
    Private isReading As Boolean
    Private wantReading As Boolean
    Private expectConnected As Boolean
    Private ReadOnly client As TcpClient
    Public logger As Logger
    Private ReadOnly lock As New Object()
    Private ReadOnly stream As SafeReadPacketStream
    Private ReadOnly _remoteEndPoint As Net.IPEndPoint
    Public ReadOnly Property RemoteEndPoint As Net.IPEndPoint
        Get
            Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address IsNot Nothing)
            Return _remoteEndPoint
        End Get
    End Property

    Public Const DefaultBufferSize As Integer = 1460 '[sending more data in a packet causes wc3 clients to disc; happens to be maximum ethernet header size]
    Public ReadOnly bufferSize As Integer
    Private ReadOnly readBuffer() As Byte
    Public Event ReceivedPacket(ByVal sender As BnetSocket, ByVal flag As Byte, ByVal id As Byte, ByVal data As IViewableList(Of Byte))
    Public Event Disconnected(ByVal sender As BnetSocket, ByVal reason As String)
    Private WithEvents deadManSwitch As DeadManSwitch

    <ContractInvariantMethod()> Protected Sub Invariant()
        Contract.Invariant(stream IsNot Nothing)
        Contract.Invariant(readBuffer IsNot Nothing)
        Contract.Invariant(_remoteEndPoint IsNot Nothing)
        Contract.Invariant(_remoteEndPoint.Address IsNot Nothing)
    End Sub

#Region "Inner"
    Public Property Name() As String
        Get
            Return stream.logDestination
        End Get
        Set(ByVal value As String)
            stream.logDestination = value
        End Set
    End Property
    Private Class SafeReadPacketStream
        Inherits PacketStream
        Private read_exception As Exception
        Public Sub New(ByVal substream As IO.Stream,
                       ByVal numBytesBeforeSize As Integer,
                       ByVal numSizeBytes As Integer,
                       ByVal mode As InterfaceModes,
                       ByVal logger As Logger,
                       ByVal log_destination As String)
            MyBase.New(substream, numBytesBeforeSize, numSizeBytes, mode, logger, log_destination)
            Contract.Requires(substream IsNot Nothing)
            Contract.Requires(numBytesBeforeSize >= 0)
            Contract.Requires(numSizeBytes > 0)
        End Sub
        Public Overrides Function Read(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer) As Integer
            Try
                read_exception = Nothing
                Return MyBase.Read(buffer, offset, count)
            Catch e As Exception
                read_exception = e
                Return 0
            End Try
        End Function
        Public Overrides Function EndRead(ByVal asyncResult As System.IAsyncResult) As Integer
            If read_exception IsNot Nothing Then Throw read_exception
            Return MyBase.EndRead(asyncResult)
        End Function
    End Class
#End Region

#Region "New"
    Public Sub New(ByVal client As TcpClient,
                   ByVal timeout As TimeSpan,
                   Optional ByVal logger As Logger = Nothing,
                   Optional ByVal streamWrapper As Func(Of IO.Stream, IO.Stream) = Nothing,
                   Optional ByVal bufferSize As Integer = DefaultBufferSize)
        Contract.Requires(client IsNot Nothing)

        Dim stream As IO.Stream = client.GetStream
        If streamWrapper IsNot Nothing Then stream = streamWrapper(stream)
        Me.bufferSize = bufferSize
        ReDim readBuffer(0 To BufferSize - 1)
        Me.deadManSwitch = New DeadManSwitch(timeout, initiallyArmed:=True)
        Me.logger = If(logger, New Logger)
        Me.stream = New SafeReadPacketStream(stream, 2, 2, PacketStream.InterfaceModes.IncludeSizeBytes, Me.logger, "")
        Me.client = client
        Name = CType(client.Client.RemoteEndPoint, Net.IPEndPoint).ToString()
        expectConnected = client.Connected
        _remoteEndPoint = CType(client.Client.RemoteEndPoint, Net.IPEndPoint)
        If ArraysEqual(RemoteEndPoint.Address.GetAddressBytes(), GetCachedIpAddressBytes(external:=False)) OrElse
                    ArraysEqual(RemoteEndPoint.Address.GetAddressBytes(), {127, 0, 0, 1}) Then
            _remoteEndPoint = New Net.IPEndPoint(New Net.IPAddress(GetCachedIpAddressBytes(external:=True)), RemoteEndPoint.Port)
        End If
        Contract.Assume(_remoteEndPoint IsNot Nothing)
        Contract.Assume(_remoteEndPoint.Address IsNot Nothing)
    End Sub
#End Region

#Region "Access"
    Public Function GetLocalPort() As UShort
        Return CUShort(CType(client.Client.LocalEndPoint, Net.IPEndPoint).Port)
    End Function
    Public Function GetLocalIP() As Byte()
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Dim bytes = CType(client.Client.LocalEndPoint, Net.IPEndPoint).Address.GetAddressBytes()
        Contract.Assume(bytes IsNot Nothing)
        Return bytes
    End Function
    Public Function IsConnected() As Boolean
        Return client.Connected
    End Function
    Public Sub Disconnect(ByVal reason As String)
        Contract.Requires(reason IsNot Nothing)
        SyncLock lock
            If Not expectConnected Then Return
            expectConnected = False
            client.Close()
            deadManSwitch.Dispose()
            stream.Close()
            isReading = False
            wantReading = False
            ThreadPooledAction(
                Sub()
                    RaiseEvent Disconnected(Me, reason)
                End Sub
            )
        End SyncLock
    End Sub
    Private Sub DeadManSwitch_Triggered(ByVal sender As DeadManSwitch) Handles deadManSwitch.Triggered
        Disconnect("Connection went idle.")
    End Sub
#End Region

#Region "Read"
    Public Sub SetReading(ByVal value As Boolean)
        SyncLock lock
            If wantReading = value Then Return 'no change needed
            wantReading = value

            If Not wantReading Then Return 'don't want to be reading, no need to start
            If isReading Then Return 'already reading, no need to start
            isReading = True
        End SyncLock

        Try
            stream.BeginRead(readBuffer, 0, readBuffer.Length, AddressOf ReadComplete, Nothing)
        Catch e As NotSupportedException
            isReading = False
        End Try
    End Sub

    Private Sub ReadComplete(ByVal ar As IAsyncResult)
        Contract.Requires(ar IsNot Nothing)

        Try
            'read
            If Not stream.CanRead Then
                logger.log("Socket '{0}' Closed.".frmt(Name), LogMessageTypes.DataRaw)
                Disconnect("stream closed")
                Return
            End If
            Dim n = stream.EndRead(ar)
            If n = 0 Then
                logger.log("Socket '{0}' Ended.".frmt(Name), LogMessageTypes.DataRaw)
                Disconnect("stream ended")
                Return
            End If
            If n < 4 Then Throw New InvalidOperationException("Socket's substream did not divide reads into valid packets.")

            'report
            Dim header = readBuffer(0)
            Dim packet_id = readBuffer(1)
            Dim data = readBuffer.SubArray(4, n - 4).ToView()
            deadManSwitch.Reset()
            RaiseEvent ReceivedPacket(Me, header, packet_id, data)

            'keep reading
            Dim continueReading As Boolean
            SyncLock lock
                isReading = wantReading
                continueReading = wantReading
            End SyncLock
            If continueReading Then
                stream.BeginRead(readBuffer, 0, readBuffer.Length, AddressOf ReadComplete, Nothing)
            End If

        Catch e As Exception
            Disconnect("Error receiving data from {0}: {1}".frmt(Name, e.Message))
        End Try
    End Sub
#End Region

#Region "Write"
    Public Function Write(ByVal header() As Byte, ByVal body() As Byte) As Outcome
        Contract.Requires(header IsNot Nothing)
        Contract.Requires(body IsNot Nothing)
        Return WriteWithMode(Concat({header, New Byte() {0, 0}, body}), PacketStream.InterfaceModes.IncludeSizeBytes)
    End Function

    Public Function WriteWithMode(ByVal data() As Byte, ByVal mode As PacketStream.InterfaceModes) As Outcome
        Contract.Requires(data IsNot Nothing)

        SyncLock lock
            Try
                If data.Length > bufferSize Then Throw New ArgumentException("Data exceeded buffer size.")
                stream.WriteWithMode(data, 0, data.Length, mode)
                Return success("Sent data.")
            Catch e As Exception
                Dim msg = "Error sending data to {0}: {1}".frmt(Name, e.Message)
                Disconnect(msg)
                Return failure(msg)
            End Try
        End SyncLock
    End Function
#End Region
End Class
