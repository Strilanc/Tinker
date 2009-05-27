Imports HostBot.Warcraft3.W3PacketId
Imports System.Runtime.CompilerServices

Namespace Warcraft3
    Public Class W3Socket
        Private WithEvents socket As BnetSocket
        Public Event ReceivedPacket(ByVal sender As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
        Public Event Disconnected(ByVal sender As W3Socket)

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
                Return socket.name
            End Get
            Set(ByVal value As String)
                socket.name = value
            End Set
        End Property
#End Region

#Region "Socket"
        Public Sub set_reading(ByVal value As Boolean)
            socket.set_reading(value)
        End Sub
        Public Function getRemotePort() As UShort
            Return socket.getRemotePort()
        End Function
        Public Function getLocalPort() As Integer
            Return socket.getLocalPort()
        End Function
        Public Function getRemoteIp() As Byte()
            Return socket.getRemoteIp()
        End Function
        Public Function getLocalIp() As Byte()
            Return socket.getLocalIp()
        End Function
        Public Function connected() As Boolean
            Return socket.connected
        End Function

        Private Sub socket_catchDisconnect() Handles socket.disconnected
            RaiseEvent Disconnected(Me)
        End Sub
        Public Sub disconnect()
            socket.disconnect()
        End Sub

        Public Function SendPacket(ByVal pk As W3Packet) As Outcome
            Try
                'Validate
                If socket Is Nothing OrElse Not socket.connected OrElse pk Is Nothing Then
                    Return failure("Socket is not connected")
                End If

                'Log
                logger.log(Function() "Sending {0} to {1}".frmt(pk.id, name), LogMessageTypes.DataEvent)
                logger.log(Function() pk.payload.toString(), LogMessageTypes.DataParsed)

                'Send
                Return socket.Write(New Byte() {W3Packet.PACKET_PREFIX, pk.id}, pk.payload.getData.ToArray)

            Catch e As Pickling.PicklingException
                Dim msg = "Error packing {0} for {1}: {2}".frmt(pk.id, name, e.Message)
                logger.log(msg, LogMessageTypes.Problem)
                Return failure(msg)
            Catch e As Exception
                Dim msg = "Error sending {0} to {1}: {2}".frmt(pk.id, name, e.Message)
                Logging.logUnexpectedException(msg, e)
                logger.log(msg, LogMessageTypes.Problem)
                Return failure(msg)
            End Try
        End Function

        Private Sub receivePacket(ByVal sender As BnetSocket, ByVal flag As Byte, ByVal id_ As Byte, ByVal data As ImmutableArrayView(Of Byte)) Handles socket.receivedPacket
            Dim id = CType(id_, W3PacketId)
            Try
                'Validate
                If flag <> W3Packet.PACKET_PREFIX Then
                    disconnect()
                    Throw New IO.IOException("Invalid packet prefix")
                End If

                'Log Event
                logger.log(Function() "Received {0} from {1}".frmt(id, name), LogMessageTypes.DataEvent)

                'Parse
                Dim p = W3Packet.FromData(id, data).payload
                If p.getData.length <> data.length Then
                    Throw New Pickling.PicklingException("Data left over after parsing.")
                End If
                Dim d = CType(p.getVal(), Dictionary(Of String, Object))

                'Log Parsed Data
                logger.log(Function() p.toString(), LogMessageTypes.DataParsed)

                'Handle
                RaiseEvent ReceivedPacket(Me, id, d)

            Catch e As Pickling.PicklingException
                Dim msg = "(Ignored) Error parsing {0} from {1}: {2}".frmt(id, name, e.Message)
                logger.log(msg, LogMessageTypes.Negative)

            Catch e As Exception
                Dim msg = "(Ignored) Error receiving {0} from {1}: {2}".frmt(id, name, e.Message)
                logger.log(msg, LogMessageTypes.Problem)
                Logging.logUnexpectedException(msg, e)
            End Try
        End Sub
#End Region
    End Class
End Namespace