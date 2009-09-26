Imports HostBot.Warcraft3.W3PacketId
Imports HostBot.Warcraft3

Namespace Warcraft3
    Public Class W3ConnectingPlayer
        Private ReadOnly _name As String
        Private ReadOnly _peerKey As UInteger
        Private ReadOnly _entryKey As UInteger
        Private ReadOnly _gameId As UInteger
        Private ReadOnly _listenPort As UShort
        Private ReadOnly _remoteEndPoint As Net.IPEndPoint
        Private ReadOnly _socket As W3Socket

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_name IsNot Nothing)
            Contract.Invariant(_remoteEndPoint IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
        End Sub

        Public ReadOnly Property Name As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property
        Public ReadOnly Property PeerKey As UInteger
            Get
                Return _peerKey
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

        Public Sub New(ByVal name As String,
                       ByVal gameId As UInteger,
                       ByVal entrykey As UInteger,
                       ByVal peerKey As UInteger,
                       ByVal listenPort As UShort,
                       ByVal remoteEndPoint As Net.IPEndPoint,
                       ByVal socket As W3Socket)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(remoteEndPoint IsNot Nothing)
            Contract.Requires(socket IsNot Nothing)
            Me._name = name
            Me._peerKey = peerKey
            Me._listenPort = listenPort
            Me._remoteEndPoint = remoteEndPoint
            Me._socket = socket
            Me._gameId = gameId
            Me._entryKey = entrykey
        End Sub
    End Class

    Public Class W3ConnectingPeer
        Public ReadOnly socket As W3Socket
        Public ReadOnly receiverPeerKey As Byte
        Public ReadOnly index As Byte
        Public ReadOnly connectionFlags As UShort
        Public Sub New(ByVal socket As W3Socket,
                       ByVal receiverPeerKey As Byte,
                       ByVal index As Byte,
                       ByVal connectionFlags As UShort)
            Me.socket = socket
            Me.receiverPeerKey = receiverPeerKey
            Me.index = index
            Me.connectionFlags = connectionFlags
        End Sub
    End Class

    Public Class W3Peer
        Public ReadOnly name As String
        Public ReadOnly index As Byte
        Public ReadOnly listenPort As UShort
        Public ReadOnly ip As Net.IPAddress
        Public ReadOnly peerKey As UInteger
        Private WithEvents _socket As W3Socket
        Public Event ReceivedPacket(ByVal sender As W3Peer, ByVal packet As W3Packet)
        Public Event Disconnected(ByVal sender As W3Peer, ByVal expected As Boolean, ByVal reason As String)

        Public Sub New(ByVal name As String,
                       ByVal index As Byte,
                       ByVal listenPort As UShort,
                       ByVal ip As Net.IPAddress, ByVal peerKey As UInteger)
            Me.name = name
            Me.index = index
            Me.listenPort = listenPort
            Me.ip = ip
            Me.peerKey = peerKey
        End Sub

        Public ReadOnly Property Socket As W3Socket
            Get
                Return _socket
            End Get
        End Property
        Public Sub SetSocket(ByVal socket As W3Socket)
            Me._socket = socket
            If socket Is Nothing Then Return
            FutureIterateExcept(AddressOf socket.FutureReadPacket,
                Sub(packet)
                                                                       RaiseEvent ReceivedPacket(Me, packet)
                                                                   End Sub)
        End Sub

        Private Sub socket_Disconnected(ByVal sender As Warcraft3.W3Socket, ByVal expected As Boolean, ByVal reason As String) Handles _socket.Disconnected
            RaiseEvent Disconnected(Me, expected, reason)
        End Sub
    End Class
End Namespace