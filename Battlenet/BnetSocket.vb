Imports System.Net
Imports System.Net.Sockets

Public NotInheritable Class BnetSocket
    Private WithEvents _socket As PacketSocket
    Public Event Disconnected(ByVal sender As BnetSocket, ByVal expected As Boolean, ByVal reason As String)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_socket IsNot Nothing)
    End Sub

    Public Sub New(ByVal socket As PacketSocket)
        Contract.Assume(socket IsNot Nothing)
        Me._socket = socket
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
    Public Function IsConnected() As Boolean
        Return _socket.IsConnected
    End Function

    Private Sub CatchDisconnected(ByVal sender As PacketSocket, ByVal expected As Boolean, ByVal reason As String) Handles _socket.Disconnected
        Contract.Requires(sender IsNot Nothing)
        Contract.Requires(reason IsNot Nothing)
        RaiseEvent Disconnected(Me, expected, reason)
    End Sub
    Public Sub Disconnect(ByVal expected As Boolean, ByVal reason As String)
        Contract.Requires(reason IsNot Nothing)
        _socket.Disconnect(expected, reason)
    End Sub

    Public Sub SendPacket(ByVal packet As Bnet.Packet)
        Contract.Requires(packet IsNot Nothing)

        'Validate
        If Not _socket.IsConnected Then
            Throw New InvalidOperationException("Socket is not connected")
        End If

        Try
            'Log
            Logger.Log(Function() "Sending {0} to {1}".Frmt(packet.id, Name), LogMessageType.DataEvent)
            Logger.Log(packet.Payload.Description, LogMessageType.DataParsed)

            'Send
            _socket.WritePacket(Concat({Bnet.Packet.PacketPrefixValue, packet.id, 0, 0},
                                       packet.Payload.Data.ToArray))

        Catch e As Pickling.PicklingException
            Dim msg = "Error packing {0} for {1}: {2}".Frmt(packet.id, Name, e)
            Logger.Log(msg, LogMessageType.Problem)
            Throw
        Catch e As Exception
            Dim msg = "Error sending {0} to {1}: {2}".Frmt(packet.id, Name, e)
            e.RaiseAsUnexpected(msg)
            Logger.Log(msg, LogMessageType.Problem)
            Throw
        End Try
    End Sub

    Public Function FutureReadPacket() As IFuture(Of ViewableList(Of Byte))
        Contract.Ensures(Contract.Result(Of IFuture(Of ViewableList(Of Byte)))() IsNot Nothing)
        Return _socket.AsyncReadPacket()
    End Function

    Public ReadOnly Property Socket As PacketSocket
        Get
            Contract.Ensures(Contract.Result(Of PacketSocket)() IsNot Nothing)
            Return _socket
        End Get
    End Property
End Class
