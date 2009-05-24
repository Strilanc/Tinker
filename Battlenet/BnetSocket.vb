Imports System.Net.Sockets

Public Class BnetSocket
#Region "Inner"
    Private Class SafeReadPacketStream
        Inherits PacketStream
        Private read_exception As Exception
        Public Sub New(ByVal substream As IO.Stream, _
                       ByVal num_bytes_before_size As Integer, _
                       ByVal num_size_bytes As Integer, _
                       ByVal mode As InterfaceModes, _
                       ByVal logger As MultiLogger, _
                       ByVal log_destination As String)
            MyBase.New(substream, num_bytes_before_size, num_size_bytes, mode, logger, log_destination)
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

#Region "Variables"
    Public Property name() As String
        Get
            Return packet_stream.log_destination
        End Get
        Set(ByVal value As String)
            packet_stream.log_destination = value
        End Set
    End Property
    Private is_reading As Boolean = False
    Private want_reading As Boolean = False
    Private expectConnected As Boolean = False
    Private ReadOnly client As TcpClient = Nothing
    Public logger As MultiLogger
    Private ReadOnly lock As New Object()
    Private packet_stream As SafeReadPacketStream

    Public Const BUFFER_SIZE As Integer = 1460 '[sending more data in a packet causes wc3 clients to disc; happens to be maximum ethernet header size]
    Private ReadOnly read_buffer(0 To BUFFER_SIZE - 1) As Byte
    Public Event receivedPacket(ByVal sender As BnetSocket, ByVal flag As Byte, ByVal id As Byte, ByVal data As ImmutableArrayView(Of Byte))
    Public Event disconnected(ByVal sender As BnetSocket)
#End Region

#Region "New"
    Public Sub New(ByVal client As TcpClient, Optional ByVal logger As MultiLogger = Nothing, Optional ByVal stream_wrapper As Func(Of IO.Stream, IO.Stream) = Nothing)
        If client Is Nothing Then Throw New ArgumentNullException("client")
        Dim stream As IO.Stream = client.GetStream
        If stream_wrapper IsNot Nothing Then stream = stream_wrapper(stream)

        Me.logger = If(logger, New MultiLogger)
        Me.packet_stream = New SafeReadPacketStream(stream, 2, 2, PacketStream.InterfaceModes.IncludeSizeBytes, Me.logger, "")
        Me.client = client
        name = CType(client.Client.RemoteEndPoint, Net.IPEndPoint).ToString()
        expectConnected = client.Connected
    End Sub
#End Region

#Region "Access"
    Public Function getRemotePort() As UShort
        Return CUShort(CType(client.Client.RemoteEndPoint, Net.IPEndPoint).Port)
    End Function
    Public Function getLocalPort() As Integer
        Return CType(client.Client.LocalEndPoint, Net.IPEndPoint).Port
    End Function
    Public Function getRemoteIp() As Byte()
        Try
            Dim x = CType(client.Client.RemoteEndPoint, Net.IPEndPoint).Address.GetAddressBytes()
            If ArraysEqual(x, getIpAddress().GetAddressBytes()) OrElse ArraysEqual(x, New Byte() {127, 0, 0, 1}) Then
                x = GetExternalIp()
            End If
            Return x
        Catch e As Exception
            Return New Byte() {0, 0, 0, 0}
        End Try
    End Function
    Public Function getLocalIp() As Byte()
        Return CType(client.Client.LocalEndPoint, Net.IPEndPoint).Address.GetAddressBytes()
    End Function
    Public Function connected() As Boolean
        Return client.Connected
    End Function
    Public Sub disconnect()
        SyncLock lock
            If Not expectConnected Then Return
            expectConnected = False
            client.Close()
            packet_stream.Close()
            is_reading = False
            want_reading = False
            threadedCall(AddressOf throw_disconnected_T, "disconnect socket")
        End SyncLock
    End Sub
    Private Sub throw_disconnected_T()
        RaiseEvent disconnected(Me)
    End Sub
#End Region

#Region "Read"
    Public Sub set_reading(ByVal value As Boolean)
        SyncLock lock
            If want_reading = value Then Return 'no change needed
            want_reading = value

            If Not want_reading Then Return 'don't want to be reading, no need to start
            If is_reading Then Return 'already reading, no need to start
            is_reading = True
        End SyncLock

        Try
            packet_stream.BeginRead(read_buffer, 0, read_buffer.Length, AddressOf read_complete, Nothing)
        Catch e As NotSupportedException
            is_reading = False
        End Try
    End Sub

    Private Sub read_complete(ByVal ar As IAsyncResult)
        If ar Is Nothing Then Throw New ArgumentException()

        Try
            'read
            If Not packet_stream.CanRead Then
                logger.log("Socket '{0}' Closed.".frmt(name), LogMessageTypes.RawData)
                disconnect()
                Return
            End If
            Dim n = packet_stream.EndRead(ar)
            If n = 0 Then
                logger.log("Socket '{0}' Ended.".frmt(name), LogMessageTypes.RawData)
                disconnect()
                Return
            End If

            'report
            Dim header = read_buffer(0)
            Dim packet_id = read_buffer(1)
            Dim data = New ImmutableArrayView(Of Byte)(read_buffer, 4, n - 4)
            RaiseEvent receivedPacket(Me, header, packet_id, data)

            'keep reading
            Dim continue_reading As Boolean
            SyncLock lock
                is_reading = want_reading
                continue_reading = want_reading
            End SyncLock
            If continue_reading Then
                packet_stream.BeginRead(read_buffer, 0, read_buffer.Length, AddressOf read_complete, Nothing)
            End If

        Catch e As Exception
            If expectConnected Then
                logger.log("Error receiving data from {0}: {1}".frmt(name, e.Message), LogMessageTypes.Problem)
            End If
            disconnect()
        End Try
    End Sub
#End Region

#Region "Write"
    Public Function Write(ByVal header() As Byte, ByVal body() As Byte) As Outcome
        Return WriteMode(concat(header, New Byte() {0, 0}, body), PacketStream.InterfaceModes.IncludeSizeBytes)
    End Function

    Public Function WriteMode(ByVal data() As Byte, ByVal mode As PacketStream.InterfaceModes) As Outcome
        SyncLock lock
            Try
                If data.Length > BUFFER_SIZE Then Throw New ArgumentException("Data exceeded buffer size.")
                packet_stream.WriteMode(data, 0, data.Length, mode)
                Return success("Sent data.")
            Catch e As Exception
                Dim msg = "Error sending data to " + name + ": " + e.Message
                If expectConnected Then
                    logger.log(msg, LogMessageTypes.Problem)
                End If
                disconnect()
                Return failure(msg)
            End Try
        End SyncLock
    End Function
#End Region
End Class
