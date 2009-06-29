Imports HostBot.Warcraft3.W3PacketId
Imports System.Runtime.CompilerServices

Namespace Warcraft3
    Public Class W3Socket
        Private WithEvents socket As BnetSocket
        Public Event ReceivedPacket(ByVal sender As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
        Public Event Disconnected(ByVal sender As W3Socket, ByVal reason As String)

        Public Sub New(ByVal socket As BnetSocket)
            If socket Is Nothing Then Throw New ArgumentNullException("socket")
            Me.socket = socket
        End Sub

#Region "Properties"
        Public Property logger() As Logger
            Get
                Return socket.logger
            End Get
            Set(ByVal value As Logger)
                socket.logger = value
            End Set
        End Property
        Public Property name() As String
            Get
                Return socket.Name
            End Get
            Set(ByVal value As String)
                socket.Name = value
            End Set
        End Property
#End Region

#Region "Socket"
        Public Sub SetReading(ByVal value As Boolean)
            socket.SetReading(value)
        End Sub
        Public Function getLocalPort() As UShort
            Return socket.GetLocalPort()
        End Function
        Public ReadOnly Property RemoteEndPoint As Net.IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)().Address IsNot Nothing)
                Return socket.RemoteEndPoint
            End Get
        End Property
        Public Function getLocalIp() As Byte()
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
            Return socket.GetLocalIP()
        End Function
        Public Function connected() As Boolean
            Return socket.IsConnected
        End Function

        Private Sub c_Disconnected(ByVal sender As BnetSocket, ByVal reason As String) Handles socket.Disconnected
            RaiseEvent Disconnected(Me, reason)
        End Sub
        Public Sub disconnect(ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            socket.Disconnect(reason)
        End Sub

        Public Function SendPacket(ByVal pk As W3Packet) As Outcome
            Contract.Requires(pk IsNot Nothing)

            Try
                'Validate
                If socket Is Nothing OrElse Not socket.IsConnected OrElse pk Is Nothing Then
                    Return failure("Socket is not connected")
                End If

                'Log
                logger.log(Function() "Sending {0} to {1}".frmt(pk.id, name), LogMessageTypes.DataEvent)
                logger.log(pk.payload.Description, LogMessageTypes.DataParsed)

                'Send
                Return socket.Write(New Byte() {W3Packet.PACKET_PREFIX, pk.id}, pk.payload.Data.ToArray)

            Catch e As Pickling.PicklingException
                Dim msg = "Error packing {0} for {1}: {2}".frmt(pk.id, name, e.Message)
                logger.log(msg, LogMessageTypes.Problem)
                Return failure(msg)
            Catch e As Exception
                Dim msg = "Error sending {0} to {1}: {2}".frmt(pk.id, name, e.Message)
                Logging.LogUnexpectedException(msg, e)
                logger.log(msg, LogMessageTypes.Problem)
                Return failure(msg)
            End Try
        End Function

        Private Sub receivePacket(ByVal sender As BnetSocket, ByVal flag As Byte, ByVal id_ As Byte, ByVal data As IViewableList(Of Byte)) Handles socket.ReceivedPacket
            Dim id = CType(id_, W3PacketId)
            Try
                'Validate
                If flag <> W3Packet.PACKET_PREFIX Then
                    disconnect("Invalid packet prefix")
                    Throw New IO.IOException("Invalid packet prefix")
                End If

                'Log Event
                logger.log(Function() "Received {0} from {1}".frmt(id, name), LogMessageTypes.DataEvent)

                'Parse
                Dim p = W3Packet.FromData(id, data).payload
                If p.Data.Length <> data.Length Then
                    Throw New Pickling.PicklingException("Data left over after parsing.")
                End If
                Dim d = CType(p.Value, Dictionary(Of String, Object))

                'Log Parsed Data
                logger.log(p.Description, LogMessageTypes.DataParsed)

                'Handle
                RaiseEvent ReceivedPacket(Me, id, d)

            Catch e As Pickling.PicklingException
                Dim msg = "(Ignored) Error parsing {0} from {1}: {2}".frmt(id, name, e.Message)
                logger.log(msg, LogMessageTypes.Negative)

            Catch e As Exception
                Dim msg = "(Ignored) Error receiving {0} from {1}: {2}".frmt(id, name, e.Message)
                logger.log(msg, LogMessageTypes.Problem)
                Logging.LogUnexpectedException(msg, e)
            End Try
        End Sub
#End Region
    End Class
End Namespace