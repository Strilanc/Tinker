Imports HostBot.Warcraft3.W3PacketId
Imports System.Runtime.CompilerServices
Imports System.Net
Imports System.Net.Sockets

Namespace Warcraft3
    Public NotInheritable Class W3Socket
        Private WithEvents socket As PacketSocket
        Public Event Disconnected(ByVal sender As W3Socket, ByVal expected As Boolean, ByVal reason As String)

        Public Sub New(ByVal socket As PacketSocket)
            Contract.Assume(socket IsNot Nothing) 'bug in contracts required not using requires here
            Me.socket = socket
        End Sub

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(socket IsNot Nothing)
        End Sub

        Public Property Logger() As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return socket.Logger
            End Get
            Set(ByVal value As Logger)
                Contract.Requires(value IsNot Nothing)
                socket.Logger = value
            End Set
        End Property
        Public Property Name() As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return socket.Name
            End Get
            Set(ByVal value As String)
                Contract.Requires(value IsNot Nothing)
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

        Private Sub OnSocketDisconnect(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String) Handles socket.Disconnected
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            RaiseEvent Disconnected(Me, expected, reason)
        End Sub
        Public Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            socket.Disconnect(expected, reason)
        End Sub

        Public Sub SendPacket(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)

            Try
                'Validate
                If Not socket.IsConnected Then
                    Throw New InvalidOperationException("Socket is not connected")
                End If

                'Log
                Logger.Log(Function() "Sending {0} to {1}".Frmt(packet.id, Name), LogMessageType.DataEvent)
                Logger.Log(packet.Payload.Description, LogMessageType.DataParsed)

                'Send
                socket.WritePacket(Concat({W3Packet.PacketPrefixValue, packet.id, 0, 0},
                                           packet.Payload.Data.ToArray))

            Catch e As Pickling.PicklingException
                Dim msg = "Error packing {0} for {1}: {2}".Frmt(packet.id, Name, e)
                Logger.Log(msg, LogMessageType.Problem)
                Throw

            Catch e As Exception
                If Not (TypeOf e Is SocketException OrElse
                        TypeOf e Is ObjectDisposedException OrElse
                        TypeOf e Is IO.IOException) Then
                    e.RaiseAsUnexpected("Error sending {0} to {1}.".Frmt(packet.id, Name))
                End If
                Dim msg = "Error sending {0} to {1}: {2}".Frmt(packet.id, Name, e)
                socket.Disconnect(expected:=False, reason:=msg)
                Throw
            End Try
        End Sub

        Public Function FutureReadPacket() As IFuture(Of W3Packet)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Packet))() IsNot Nothing)
            Return socket.FutureReadPacket().Select(
                Function(data)
                    Contract.Assume(data IsNot Nothing)
                    Contract.Assume(data.Length >= 4)
                    If data(0) <> W3Packet.PacketPrefixValue OrElse data.Length < 4 Then
                        Disconnect(expected:=False, reason:="Invalid packet prefix")
                        Throw New IO.IOException("Invalid packet prefix")
                    End If
                    Dim id = CType(data(1), W3PacketId)
                    data = data.SubView(4)

                    Try
                        'Handle
                        Logger.Log(Function() "Received {0} from {1}".Frmt(id, Name), LogMessageType.DataEvent)
                        Dim pk = W3Packet.FromData(id, data)
                        If pk.Payload.Data.Length <> data.Length Then
                            Throw New Pickling.PicklingException("Data left over after parsing.")
                        End If
                        Logger.Log(pk.Payload.Description, LogMessageType.DataParsed)
                        Return pk

                    Catch e As Pickling.PicklingException
                        Dim msg = "Error parsing {0} from {1}: {2} ({3})".Frmt(id, Name, e, data.ToHexString())
                        Logger.Log(msg, LogMessageType.Negative)
                        Throw

                    Catch e As Exception
                        If Not (TypeOf e Is SocketException OrElse
                                TypeOf e Is ObjectDisposedException OrElse
                                TypeOf e Is IO.IOException) Then
                            e.RaiseAsUnexpected("Error receiving {0} from {1} ({2}).".Frmt(id, Name, data.ToHexString()))
                        End If
                        Dim msg = "Error receiving {0} from {1}: {2} ({3})".Frmt(id, Name, e, data.ToHexString())
                        Logger.Log(msg, LogMessageType.Problem)
                        socket.Disconnect(expected:=False, reason:=msg)
                        Throw
                    End Try
                End Function
            )
        End Function
    End Class
End Namespace