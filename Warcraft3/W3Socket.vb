Imports System.Net
Imports System.Net.Sockets
Imports Tinker.Pickling

Namespace WC3
    Public NotInheritable Class W3Socket
        Private WithEvents _socket As PacketSocket
        Public Event Disconnected(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reason As String)

        Public Sub New(ByVal socket As PacketSocket)
            Contract.Assume(socket IsNot Nothing) 'bug in contracts required not using requires here
            Me._socket = socket
        End Sub

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_socket IsNot Nothing)
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
        Public Function Connected() As Boolean
            Return _socket.IsConnected
        End Function

        Private Sub OnSocketDisconnect(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String) Handles _socket.Disconnected
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            RaiseEvent Disconnected(Me, expected, reason)
        End Sub
        Public Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            _socket.QueueDisconnect(expected, reason)
        End Sub

        Public Sub SendPacket(ByVal packet As Protocol.Packet)
            Contract.Requires(packet IsNot Nothing)

            Try
                'Validate
                If Not _socket.IsConnected Then
                    Throw New InvalidOperationException("Socket is not connected")
                End If

                'Log
                Logger.Log(Function() "Sending {0} to {1}".Frmt(packet.id, Name), LogMessageType.DataEvent)
                Logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(packet.Id, Name, packet.Payload.Description), LogMessageType.DataParsed)

                'Send
                _socket.WritePacket({Protocol.Packets.PacketPrefix, packet.Id}, packet.Payload.Data)

            Catch ex As Exception When TypeOf ex Is SocketException OrElse
                                       TypeOf ex Is ObjectDisposedException OrElse
                                       TypeOf ex Is InvalidOperationException OrElse
                                       TypeOf ex Is IO.IOException
                _socket.QueueDisconnect(expected:=False, reason:="Error sending {0} to {1}: {2}".Frmt(packet.Id, Name, ex))
            End Try
        End Sub

        Public Function AsyncReadPacket() As Task(Of IRist(Of Byte))
            Contract.Ensures(Contract.Result(Of Task(Of IRist(Of Byte)))() IsNot Nothing)
            Return _socket.AsyncReadPacket()
        End Function

        Public ReadOnly Property Socket As PacketSocket
            Get
                Contract.Ensures(Contract.Result(Of PacketSocket)() IsNot Nothing)
                Return _socket
            End Get
        End Property
    End Class
End Namespace
