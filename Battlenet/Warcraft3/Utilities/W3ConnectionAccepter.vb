Namespace Warcraft3
    Public MustInherit Class W3ConnectionAccepterBase
        Private Shared ReadOnly EXPIRY_PERIOD As TimeSpan = 10.Seconds

        Private ReadOnly _accepter As New ConnectionAccepter
        Private ReadOnly logger As Logger
        Private ReadOnly sockets As New HashSet(Of W3Socket)
        Private ReadOnly lock As New Object()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_accepter IsNot Nothing)
        End Sub

        Protected Sub New(Optional ByVal logger As Logger = Nothing)
            Me.logger = If(logger, New Logger)
            AddHandler _accepter.AcceptedConnection, AddressOf OnAcceptConnection
        End Sub

        ''' <summary>
        ''' Clears pending connections and stops listening on all ports.
        ''' WARNING: Does not guarantee no more Connection events!
        ''' For example catch_knocked might be half-finished, resulting in an event after the reset.
        ''' </summary>
        Public Sub Reset()
            SyncLock lock
                Accepter.CloseAllPorts()
                For Each socket In sockets
                    socket.Disconnect(expected:=True, reason:="Accepter Reset")
                Next socket
                sockets.Clear()
            End SyncLock
        End Sub

        '''<summary>Provides public access to the underlying accepter.</summary>
        Public ReadOnly Property Accepter() As ConnectionAccepter
            Get
                Contract.Ensures(Contract.Result(Of ConnectionAccepter)() IsNot Nothing)
                Return _accepter
            End Get
        End Property

        '''<summary>Handles new connections.</summary>
        Private Sub OnAcceptConnection(ByVal sender As ConnectionAccepter, ByVal client As Net.Sockets.TcpClient)
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(client IsNot Nothing)
            Try
                Dim socket = New W3Socket(New PacketSocket(client, 60.Seconds, logger))
                logger.log("New player connecting from {0}.".frmt(socket.Name), LogMessageType.Positive)

                SyncLock lock
                    sockets.Add(socket)
                End SyncLock
                socket.FutureReadPacket().CallWhenValueReady(
                    Sub(packet, packetException)
                        If Not TryRemoveSocket(socket) Then  Return
                        If packetException IsNot Nothing Then
                            socket.Disconnect(expected:=False, reason:=packetException.ToString)
                            logger.Log(packetException.ToString, LogMessageType.Problem)
                            Return
                        End If

                        Try
                            GetConnectingPlayer(socket, packet)
                        Catch e As Exception
                            Dim msg = "Error receiving {0} from {1}: {2}".Frmt(packet.id, socket.Name, e)
                            logger.Log(msg, LogMessageType.Problem)
                            socket.Disconnect(expected:=False, reason:=msg)
                        End Try
                    End Sub
                )
                FutureWait(EXPIRY_PERIOD).CallWhenReady(
                    Sub()
                        If Not TryRemoveSocket(socket) Then  Return
                        socket.Disconnect(expected:=False, reason:="Idle")
                    End Sub
                )
            Catch e As Exception
                logger.log("Error accepting connection: " + e.ToString, LogMessageType.Problem)
            End Try
        End Sub

        '''<summary>Atomically checks if a socket has already been processed, and removes it if not.</summary>
        Private Function TryRemoveSocket(ByVal socket As W3Socket) As Boolean
            SyncLock lock
                If Not sockets.Contains(socket) Then Return False
                sockets.Remove(socket)
            End SyncLock
            Return True
        End Function

        Protected MustOverride Sub GetConnectingPlayer(ByVal socket As W3Socket, ByVal packet As W3Packet)
    End Class

    Public NotInheritable Class W3ConnectionAccepter
        Inherits W3ConnectionAccepterBase

        Public Event Connection(ByVal sender As W3ConnectionAccepter, ByVal player As W3ConnectingPlayer)

        Public Sub New(Optional ByVal logger As Logger = Nothing)
            MyBase.New(logger)
        End Sub

        Protected Overrides Sub GetConnectingPlayer(ByVal socket As W3Socket, ByVal packet As W3Packet)
            If packet.id <> W3PacketId.Knock Then
                Throw New IO.IOException("{0} was not a warcraft 3 player.".Frmt(socket.Name))
            End If

            Dim vals = CType(packet.Payload.Value, Dictionary(Of String, Object))
            Dim name = CStr(vals("name"))
            Dim internalAddress = CType(vals("internal address"), Dictionary(Of String, Object))
            Contract.Assume(name IsNot Nothing)
            Contract.Assume(internalAddress IsNot Nothing)
            Contract.Assume(socket IsNot Nothing)
            Dim player = New W3ConnectingPlayer(name,
                                                CUInt(vals("game id")),
                                                CUInt(vals("entry key")),
                                                CUInt(vals("peer key")),
                                                CUShort(vals("listen port")),
                                                AddressJar.ExtractIPEndpoint(internalAddress),
                                                socket)

            socket.Name = player.Name
            RaiseEvent Connection(Me, player)
        End Sub
    End Class

    Public NotInheritable Class W3PeerConnectionAccepter
        Inherits W3ConnectionAccepterBase

        Public Event Connection(ByVal sender As W3PeerConnectionAccepter, ByVal player As W3ConnectingPeer)

        Public Sub New(Optional ByVal logger As Logger = Nothing)
            MyBase.New(logger)
        End Sub

        Protected Overrides Sub GetConnectingPlayer(ByVal socket As W3Socket, ByVal packet As W3Packet)
            If packet.id <> W3PacketId.PeerKnock Then
                Throw New IO.IOException("{0} was not a warcraft 3 peer connection.".frmt(socket.Name))
            End If
            Dim vals = CType(packet.payload, Dictionary(Of String, Object))
            Dim player = New W3ConnectingPeer(socket,
                                              CByte(vals("receiver peer key")),
                                              CByte(vals("sender player id")),
                                              CUShort(vals("sender peer connection flags")))
            RaiseEvent Connection(Me, player)
        End Sub
    End Class
End Namespace