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

        <ContractInvariantMethod()> Protected Sub Invariant()
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

    Public Class W3P2PConnectingPlayer
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

    Public Class W3P2PPlayer
        Public ReadOnly name As String
        Public ReadOnly index As Byte
        Public ReadOnly listenPort As UShort
        Public ReadOnly ip As Byte()
        Public ReadOnly p2pKey As UInteger
        Public WithEvents socket As W3Socket
        Public Event ReceivedPacket(ByVal sender As W3P2PPlayer, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
        Public Event Disconnected(ByVal sender As W3P2PPlayer, ByVal reason As String)

        Public Sub New(ByVal name As String, ByVal index As Byte, ByVal listen_port As UShort, ByVal ip As Byte(), ByVal p2p_key As UInteger)
            Me.name = name
            Me.index = index
            Me.listenPort = listen_port
            Me.ip = ip
            Me.p2pKey = p2p_key
        End Sub

        Private Sub socket_Disconnected(ByVal sender As Warcraft3.W3Socket, ByVal reason As String) Handles socket.Disconnected
            RaiseEvent Disconnected(Me, reason)
        End Sub

        Private Sub socket_ReceivedPacket(ByVal sender As Warcraft3.W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object)) Handles socket.ReceivedPacket
            RaiseEvent ReceivedPacket(Me, id, vals)
        End Sub
    End Class
End Namespace