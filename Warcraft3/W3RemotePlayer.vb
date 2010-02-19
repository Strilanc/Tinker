Imports Tinker.Pickling
Namespace WC3
    Public NotInheritable Class W3ConnectingPlayer
        Private ReadOnly _name As InvariantString
        Private ReadOnly _peerKey As UInteger
        Private ReadOnly _peerData As IReadableList(Of Byte)
        Private ReadOnly _entryKey As UInteger
        Private ReadOnly _gameId As UInteger
        Private ReadOnly _listenPort As UShort
        Private ReadOnly _remoteEndPoint As Net.IPEndPoint
        Private ReadOnly _socket As W3Socket

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_remoteEndPoint IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_peerData IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal gameId As UInteger,
                       ByVal entryKey As UInteger,
                       ByVal peerKey As UInteger,
                       ByVal peerData As IReadableList(Of Byte),
                       ByVal listenPort As UShort,
                       ByVal remoteEndPoint As Net.IPEndPoint,
                       ByVal socket As W3Socket)
            Contract.Requires(peerData IsNot Nothing)
            Contract.Requires(remoteEndPoint IsNot Nothing)
            Contract.Requires(socket IsNot Nothing)
            Me._name = name
            Me._peerKey = peerKey
            Me._peerData = peerData
            Me._listenPort = listenPort
            Me._remoteEndPoint = remoteEndPoint
            Me._socket = socket
            Me._gameId = gameId
            Me._entryKey = entryKey
        End Sub

#Region "Properties"
        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property PeerKey As UInteger
            Get
                Return _peerKey
            End Get
        End Property
        Public ReadOnly Property PeerData As IReadableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                Return _peerData
            End Get
        End Property
        Public ReadOnly Property EntryKey As UInteger
            Get
                Return _entryKey
            End Get
        End Property
        Public ReadOnly Property GameId As UInteger
            Get
                Return _gameId
            End Get
        End Property
        Public ReadOnly Property ListenPort As UShort
            Get
                Return _listenPort
            End Get
        End Property
        Public ReadOnly Property RemoteEndPoint As Net.IPEndPoint
            Get
                Contract.Ensures(Contract.Result(Of Net.IPEndPoint)() IsNot Nothing)
                Return _remoteEndPoint
            End Get
        End Property
        Public ReadOnly Property Socket As W3Socket
            Get
                Contract.Ensures(Contract.Result(Of W3Socket)() IsNot Nothing)
                Return _socket
            End Get
        End Property
#End Region
    End Class

    Public NotInheritable Class W3ConnectingPeer
        Public ReadOnly socket As W3Socket
        Public ReadOnly receiverPeerKey As Byte
        Public ReadOnly pid As PID
        Public ReadOnly connectionOptions As UShort
        Public Sub New(ByVal socket As W3Socket,
                       ByVal receiverPeerKey As Byte,
                       ByVal pid As PID,
                       ByVal connectionOptions As UShort)
            Me.socket = socket
            Me.receiverPeerKey = receiverPeerKey
            Me.pid = pid
            Me.connectionOptions = connectionOptions
        End Sub
    End Class

    Public NotInheritable Class W3Peer
        Public ReadOnly name As String
        Private ReadOnly _pid As PID
        Public ReadOnly listenPort As UShort
        Public ReadOnly ip As Net.IPAddress
        Public ReadOnly peerKey As UInteger
        Private WithEvents _socket As W3Socket
        Private ReadOnly _packetHandler As Protocol.W3PacketHandler
        Public Event Disconnected(ByVal sender As W3Peer, ByVal expected As Boolean, ByVal reason As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_packetHandler IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal pid As PID,
                       ByVal listenPort As UShort,
                       ByVal ip As Net.IPAddress,
                       ByVal peerKey As UInt32,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(ip IsNot Nothing)
            Me.name = name
            Me._packetHandler = New Protocol.W3PacketHandler(Me.name, logger)
            Me._pid = pid
            Me.listenPort = listenPort
            Me.ip = ip
            Me.peerKey = peerKey
        End Sub

        Public ReadOnly Property PID As PID
            Get
                Return _pid
            End Get
        End Property
        Public ReadOnly Property Socket As W3Socket
            Get
                Return _socket
            End Get
        End Property

        Public Sub SetSocket(ByVal socket As W3Socket)
            Me._socket = socket
            If socket Is Nothing Then Return
            AsyncProduceConsumeUntilError2(
                producer:=AddressOf socket.AsyncReadPacket,
                consumer:=AddressOf _packetHandler.HandlePacket,
                errorHandler:=Sub(exception)
                                  'ignore
                              End Sub)
        End Sub

        Public Function AddPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                               ByVal handler As Func(Of IPickle(Of T), ifuture)) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _packetHandler.AddHandler(packetDefinition.Id, Function(data) handler(packetDefinition.Jar.Parse(data)))
        End Function

        Private Sub OnDisconnected(ByVal sender As WC3.W3Socket, ByVal expected As Boolean, ByVal reason As String) Handles _socket.Disconnected
            RaiseEvent Disconnected(Me, expected, reason)
        End Sub
    End Class
End Namespace