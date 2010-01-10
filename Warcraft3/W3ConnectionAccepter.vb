Imports Tinker.Pickling

Namespace WC3
    <ContractClass(GetType(W3ConnectionAccepterBase.ContractClass))>
    Public MustInherit Class W3ConnectionAccepterBase
        Private Shared ReadOnly FirstPacketTimeout As TimeSpan = 10.Seconds

        Private ReadOnly _clock As IClock
        Private ReadOnly _accepter As New ConnectionAccepter
        Private ReadOnly logger As Logger
        Private ReadOnly sockets As New HashSet(Of W3Socket)
        Private ReadOnly lock As New Object()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_accepter IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(sockets IsNot Nothing)
            Contract.Invariant(lock IsNot Nothing)
        End Sub

        Protected Sub New(ByVal clock As IClock,
                          Optional ByVal logger As Logger = Nothing)
            Contract.Assume(clock IsNot Nothing)
            Me.logger = If(logger, New Logger)
            Me._clock = clock
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
                    Contract.Assume(socket IsNot Nothing)
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
                Dim socket = New W3Socket(New PacketSocket(stream:=client.GetStream,
                                                           localendpoint:=CType(client.Client.LocalEndPoint, Net.IPEndPoint),
                                                           remoteendpoint:=CType(client.Client.RemoteEndPoint, Net.IPEndPoint),
                                                           timeout:=60.Seconds,
                                                           logger:=logger,
                                                           clock:=_clock))
                logger.Log("New player connecting from {0}.".Frmt(socket.Name), LogMessageType.Positive)

                SyncLock lock
                    sockets.Add(socket)
                End SyncLock
                socket.AsyncReadPacket().CallWhenValueReady(
                    Sub(packetData, readException)
                        If Not TryRemoveSocket(socket) Then Return
                        If readException IsNot Nothing Then
                            socket.Disconnect(expected:=False, reason:=readException.Message)
                            Return
                        End If

                        Try
                            Dim id = CType(packetData(1), PacketId)
                            Dim pickle = ProcessConnectingPlayer(socket, packetData)
                            logger.Log(Function() "Received {0}".Frmt(id), LogMessageType.DataEvent)
                            logger.Log(Function() "{0} = {1}".Frmt(id, pickle.Description.Value), LogMessageType.DataParsed)
                        Catch e As Exception
                            socket.Disconnect(expected:=False, reason:=e.Message)
                        End Try
                    End Sub
                )
                _clock.AsyncWait(FirstPacketTimeout).CallWhenReady(
                    Sub()
                        If Not TryRemoveSocket(socket) Then Return
                        logger.Log("Connection from {0} timed out.".Frmt(socket.Name), LogMessageType.Negative)
                        socket.Disconnect(expected:=False, reason:="Idle")
                    End Sub
                )
            Catch e As Exception
                logger.Log("Error accepting connection: {0}".Frmt(e.Message), LogMessageType.Problem)
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

        Protected MustOverride Function ProcessConnectingPlayer(ByVal socket As W3Socket, ByVal packetData As IReadableList(Of Byte)) As IPickle
        <ContractClassFor(GetType(W3ConnectionAccepterBase))>
        Public MustInherit Class ContractClass
            Inherits W3ConnectionAccepterBase
            Protected Sub New()
                MyBase.New(Nothing, Nothing)
                Throw New NotSupportedException
            End Sub
            Protected Overrides Function ProcessConnectingPlayer(ByVal socket As W3Socket, ByVal packetData As IReadableList(Of Byte)) As IPickle
                Contract.Requires(socket IsNot Nothing)
                Contract.Requires(packetData IsNot Nothing)
                Contract.Requires(packetData.Count >= 4)
                Contract.Ensures(Contract.Result(Of IPickle)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Class

    Public NotInheritable Class W3ConnectionAccepter
        Inherits W3ConnectionAccepterBase

        Public Event Connection(ByVal sender As W3ConnectionAccepter, ByVal player As W3ConnectingPlayer)

        Public Sub New(ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            MyBase.New(clock, logger)
            Contract.Assume(clock IsNot Nothing)
        End Sub

        Protected Overrides Function ProcessConnectingPlayer(ByVal socket As W3Socket, ByVal packetData As IReadableList(Of Byte)) As IPickle
            If packetData(1) <> PacketId.Knock Then
                Throw New IO.InvalidDataException("{0} was not a warcraft 3 player.".Frmt(socket.Name))
            End If

            Dim pickle = Packet.Jars.Knock.Parse(packetData.SubView(4))
            Dim vals = pickle.Value.AssumeNotNull
            Dim player = New W3ConnectingPlayer(CStr(vals("name")).AssumeNotNull,
                                                CUInt(vals("game id")),
                                                CUInt(vals("entry key")),
                                                CUInt(vals("peer key")),
                                                CUShort(vals("listen port")),
                                                CType(vals("internal address"), Net.IPEndPoint).AssumeNotNull,
                                                socket)

            socket.Name = player.Name
            RaiseEvent Connection(Me, player)
            Return pickle
        End Function
    End Class

    Public NotInheritable Class W3PeerConnectionAccepter
        Inherits W3ConnectionAccepterBase

        Public Event Connection(ByVal sender As W3PeerConnectionAccepter, ByVal player As W3ConnectingPeer)

        Public Sub New(ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            MyBase.New(clock, logger)
            Contract.Assume(clock IsNot Nothing)
        End Sub

        Protected Overrides Function ProcessConnectingPlayer(ByVal socket As W3Socket, ByVal packetData As IReadableList(Of Byte)) As IPickle
            If packetData(1) <> PacketId.PeerKnock Then
                Throw New IO.InvalidDataException("{0} was not a warcraft 3 peer connection.".Frmt(socket.Name))
            End If
            Dim pickle = Packet.Jars.PeerKnock.Parse(packetData.SubView(4))
            Dim vals = pickle.Value.AssumeNotNull
            Dim player = New W3ConnectingPeer(socket,
                                              CByte(vals("receiver peer key")),
                                              CByte(vals("sender player id")),
                                              CUShort(vals("sender peer connection flags")))
            RaiseEvent Connection(Me, player)
            Return pickle
        End Function
    End Class
End Namespace