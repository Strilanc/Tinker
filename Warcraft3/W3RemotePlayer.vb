Imports Tinker.Pickling
Namespace WC3
    Public NotInheritable Class W3ConnectingPeer
        Public ReadOnly socket As W3Socket
        Public ReadOnly receiverPeerKey As Byte
        Public ReadOnly id As PlayerId
        Public ReadOnly connectionOptions As UInt32
        Public Sub New(ByVal socket As W3Socket,
                       ByVal receiverPeerKey As Byte,
                       ByVal id As PlayerId,
                       ByVal connectionOptions As UInt32)
            Me.socket = socket
            Me.receiverPeerKey = receiverPeerKey
            Me.id = id
            Me.connectionOptions = connectionOptions
        End Sub
    End Class

    Public NotInheritable Class W3Peer
        Public ReadOnly name As String
        Private ReadOnly _id As PlayerId
        Public ReadOnly listenPort As UShort
        Public ReadOnly ip As Net.IPAddress
        Public ReadOnly peerKey As UInteger
        Private WithEvents _socket As W3Socket
        Private ReadOnly _packetHandlerLogger As PacketHandlerLogger(Of Protocol.PacketId)
        Public Event Disconnected(ByVal sender As W3Peer, ByVal expected As Boolean, ByVal reason As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_packetHandlerLogger IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal id As PlayerId,
                       ByVal listenPort As UShort,
                       ByVal ip As Net.IPAddress,
                       ByVal peerKey As UInt32,
                       ByVal logger As Logger)
            Contract.Assume(ip IsNot Nothing)
            Contract.Assume(logger IsNot Nothing)
            Me.name = name
            Me._packetHandlerLogger = Protocol.MakeW3PacketHandlerLogger(Me.name, logger)
            Me._id = id
            Me.listenPort = listenPort
            Me.ip = ip
            Me.peerKey = peerKey
        End Sub

        Public ReadOnly Property Id As PlayerId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Socket As W3Socket
            Get
                Return _socket
            End Get
        End Property

        Public Async Sub SetSocket(ByVal socket As W3Socket)
            Me._socket = socket
            If socket Is Nothing Then Return
            Try
                Do
                    Dim data = Await socket.AsyncReadPacket()
                    Await _packetHandlerLogger.HandlePacket(data)
                Loop
            Catch ex As Exception
                'ignore (to match old behavior, should fix)
            End Try
        End Sub

        Public Function AddPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                               ByVal handler As Func(Of IPickle(Of T), Task)) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _packetHandlerLogger.IncludeHandler(packetDefinition.Id, packetDefinition.Jar, handler)
        End Function

        Private Sub OnDisconnected(ByVal sender As WC3.W3Socket, ByVal expected As Boolean, ByVal reason As String) Handles _socket.Disconnected
            RaiseEvent Disconnected(Me, expected, reason)
        End Sub
    End Class
End Namespace