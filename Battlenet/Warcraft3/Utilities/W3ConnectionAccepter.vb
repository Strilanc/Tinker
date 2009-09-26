Namespace Warcraft3
    Public MustInherit Class W3ConnectionAccepterBase
        Private Shared ReadOnly EXPIRY_PERIOD As TimeSpan = 10.Seconds

        Private ReadOnly _accepter As New ConnectionAccepter
        Private ReadOnly logger As Logger
        Private ReadOnly sockets As New HashSet(Of W3Socket)
        Private ReadOnly lock As New Object()
        Private ReadOnly costPerPacket As Double
        Private ReadOnly costPerPacketData As Double
        Private ReadOnly costPerNonGameAction As Double
        Private ReadOnly costPerNonGameActionData As Double
        Private ReadOnly costLimit As Double
        Private ReadOnly costRecoveredPerSecond As Double
        Private ReadOnly initialSlack As Double

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(initialSlack >= 0)
            Contract.Invariant(costPerPacket >= 0)
            Contract.Invariant(costPerPacketData >= 0)
            Contract.Invariant(costPerNonGameAction >= 0)
            Contract.Invariant(costPerNonGameActionData >= 0)
            Contract.Invariant(costLimit >= 0)
            Contract.Invariant(costRecoveredPerSecond > 0)
            Contract.Invariant(_accepter IsNot Nothing)
        End Sub

        Public Sub New(Optional ByVal logger As Logger = Nothing,
                       Optional ByVal initialSlack As Double = 0,
                       Optional ByVal costPerPacket As Double = 0,
                       Optional ByVal costPerPacketData As Double = 0,
                       Optional ByVal costPerNonGameAction As Double = 0,
                       Optional ByVal costPerNonGameActionData As Double = 0,
                       Optional ByVal costLimit As Double = 0,
                       Optional ByVal costRecoveredPerSecond As Double = 1)
            Contract.Requires(initialSlack >= 0)
            Contract.Requires(costPerPacket >= 0)
            Contract.Requires(costPerPacketData >= 0)
            Contract.Requires(costPerNonGameAction >= 0)
            Contract.Requires(costPerNonGameActionData >= 0)
            Contract.Requires(costLimit >= 0)
            Contract.Requires(costRecoveredPerSecond > 0)
            Me.logger = If(logger, New Logger)
            Me.initialSlack = initialSlack
            Me.costPerNonGameAction = costPerNonGameActionData
            Me.costPerNonGameActionData = costPerNonGameActionData
            Me.costPerPacket = costPerPacket
            Me.costPerPacketData = costPerPacketData
            Me.costLimit = costLimit
            Me.costRecoveredPerSecond = costRecoveredPerSecond
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
                Dim socket = New W3Socket(New PacketSocket(client, 60.Seconds, logger),
                                          initialSlack,
                                          costPerPacket,
                                          costPerPacketData,
                                          costPerNonGameAction,
                                          costPerNonGameActionData,
                                          costLimit,
                                          costRecoveredPerSecond)
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

    Public Class W3ConnectionAccepter
        Inherits W3ConnectionAccepterBase

        Public Event Connection(ByVal sender As W3ConnectionAccepter, ByVal player As W3ConnectingPlayer)

        Public Sub New(Optional ByVal logger As Logger = Nothing)
            MyBase.New(logger,
                       costLimit:=100,
                       costRecoveredPerSecond:=20,
                       costPerNonGameAction:=1,
                       costPerNonGameActionData:=0.1,
                       costPerPacket:=0,
                       costperpacketdata:=0,
                       initialSlack:=0)
        End Sub

        Protected Overrides Sub GetConnectingPlayer(ByVal socket As W3Socket, ByVal packet As W3Packet)
            If packet.id <> W3PacketId.Knock Then
                Throw New IO.IOException("{0} was not a warcraft 3 player.".frmt(socket.Name))
            End If

            Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
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

    Public Class W3PeerConnectionAccepter
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