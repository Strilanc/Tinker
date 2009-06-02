Namespace Warcraft3
    Public MustInherit Class W3ConnectionAccepterBase
        Private Shared ReadOnly EXPIRY_PERIOD As New TimeSpan(0, 0, 10)

        Private WithEvents _accepter As New ConnectionAccepter
        Private ReadOnly logger As Logger
        Private ReadOnly sockets As New HashSet(Of W3Socket)
        Private ReadOnly lock As New Object()

        Public Sub New(Optional ByVal logger As Logger = Nothing)
            Me.logger = If(logger, New Logger)
        End Sub

        ''' <summary>
        ''' Clears pending connections and stops listening on all ports.
        ''' WARNING: Does not guarantee no more Connection events!
        ''' For example catch_knocked might be half-finished, resulting in an event after the reset.
        ''' </summary>
        Public Sub Reset()
            SyncLock lock
                accepter.CloseAllPorts()
                For Each socket In sockets
                    socket.disconnect()
                Next socket
                sockets.Clear()
            End SyncLock
        End Sub

        '''<summary>Provides public access to the underlying accepter.</summary>
        Public ReadOnly Property accepter() As ConnectionAccepter
            Get
                accepter = _accepter
            End Get
        End Property

        '''<summary>Handles new connections.</summary>
        Private Sub catch_connection(ByVal sender As ConnectionAccepter, ByVal client As Net.Sockets.TcpClient) Handles _accepter.accepted_connection
            Try
                Dim socket = New W3Socket(New BnetSocket(client, logger))
                logger.log("New player connecting from {0}.".frmt(socket.name), LogMessageTypes.Positive)

                SyncLock lock
                    sockets.Add(socket)
                End SyncLock
                AddHandler socket.ReceivedPacket, AddressOf catch_knocked
                FutureSub.Call({FutureWait(EXPIRY_PERIOD)}, Sub() catch_expired(socket))

                socket.set_reading(True)
            Catch e As Exception
                logger.log("Error accepting connection: " + e.Message, LogMessageTypes.Problem)
            End Try
        End Sub

        '''<summary>Atomically checks if a socket has already been processed, and removes it if not.</summary>
        Private Function TryRemoveSocket(ByVal socket As W3Socket) As Boolean
            SyncLock lock
                If Not sockets.Contains(socket) Then Return False
                sockets.Remove(socket)
            End SyncLock
            RemoveHandler socket.ReceivedPacket, AddressOf catch_knocked
            Return True
        End Function

        '''<summary>Disconnects sockets which do not send any initial data.</summary>
        Private Sub catch_expired(ByVal socket As W3Socket)
            If Not TryRemoveSocket(socket) Then Return
            socket.disconnect()
        End Sub

        '''<summary>Accepts connecting warcraft 3 players.</summary>
        Private Sub catch_knocked(ByVal socket As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
            If Not TryRemoveSocket(socket) Then Return
            Try
                GetConnectingPlayer(socket, id, vals)
            Catch e As Exception
                logger.log("Error receiving {0} from {1}: {2}".frmt(id, socket.name, e.Message), LogMessageTypes.Problem)
                socket.disconnect()
            End Try
        End Sub

        Protected MustOverride Sub GetConnectingPlayer(ByVal socket As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
    End Class

    Public Class W3ConnectionAccepter
        Inherits W3ConnectionAccepterBase

        Public Event Connection(ByVal sender As W3ConnectionAccepter, ByVal player As W3ConnectingPlayer)

        Public Sub New(Optional ByVal logger As Logger = Nothing)
            MyBase.New(logger)
        End Sub

        Protected Overrides Sub GetConnectingPlayer(ByVal socket As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
            If id <> W3PacketId.KNOCK Then
                Throw New IO.IOException("{0} was not a warcraft 3 player.".frmt(socket.name))
            End If

            Dim addr = CType(vals("internal address"), Dictionary(Of String, Object))
            Dim player = New W3ConnectingPlayer( _
                                CStr(vals("name")),
                                CUInt(vals("connection key")),
                                CUShort(vals("listen port")),
                                CUShort(addr("port")),
                                CType(addr("ip"), Byte()),
                                socket)

            socket.name = player.name
            socket.set_reading(False)
            RaiseEvent Connection(Me, player)
        End Sub
    End Class

    Public Class W3P2PConnectionAccepter
        Inherits W3ConnectionAccepterBase

        Public Event Connection(ByVal sender As W3P2PConnectionAccepter, ByVal player As W3P2PConnectingPlayer)

        Public Sub New(Optional ByVal logger As Logger = Nothing)
            MyBase.New(logger)
        End Sub

        Protected Overrides Sub GetConnectingPlayer(ByVal socket As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
            If id <> W3PacketId.P2P_KNOCK Then
                Throw New IO.IOException("{0} was not a p2p warcraft 3 player connection.".frmt(socket.name))
            End If
            Dim player = New W3P2PConnectingPlayer(socket,
                                              CByte(vals("receiver p2p key")),
                                              CByte(vals("sender player id")),
                                              CUShort(vals("sender p2p flags")))
            socket.set_reading(False)
            RaiseEvent Connection(Me, player)
        End Sub
    End Class
End Namespace