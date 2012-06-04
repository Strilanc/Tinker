Imports Tinker.Pickling

Namespace WC3
    <ContractClass(GetType(W3ConnectionAccepterBase.ContractClass))>
    Public MustInherit Class W3ConnectionAccepterBase
        Private Shared ReadOnly FirstPacketTimeout As TimeSpan = 10.Seconds

        Private ReadOnly _clock As IClock
        Private ReadOnly _accepter As New ConnectionAccepter
        Private ReadOnly _logger As Logger
        Private ReadOnly _sockets As New HashSet(Of W3Socket)
        Private ReadOnly lock As New Object()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_accepter IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_sockets IsNot Nothing)
            Contract.Invariant(lock IsNot Nothing)
        End Sub

        Protected Sub New(clock As IClock,
                          Optional logger As Logger = Nothing)
            Contract.Assume(clock IsNot Nothing)
            Me._logger = If(logger, New Logger)
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
                For Each socket In _sockets
                    Contract.Assume(socket IsNot Nothing)
                    socket.Disconnect(expected:=True, reason:="Accepter Reset")
                Next socket
                _sockets.Clear()
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
        Private Async Sub OnAcceptConnection(sender As ConnectionAccepter, client As Net.Sockets.TcpClient)
            Contract.Assume(sender IsNot Nothing)
            Contract.Assume(client IsNot Nothing)
            Dim socket = New W3Socket(New PacketSocket(stream:=client.GetStream,
                                                       localendpoint:=DirectCast(client.Client.LocalEndPoint, Net.IPEndPoint),
                                                       remoteendpoint:=DirectCast(client.Client.RemoteEndPoint, Net.IPEndPoint),
                                                       timeout:=60.Seconds,
                                                       Logger:=_logger,
                                                       clock:=_clock))
            _logger.Log("New player connecting from {0}.".Frmt(socket.Name), LogMessageType.Positive)

            SyncLock lock
                _sockets.Add(socket)
            End SyncLock

            Call Async Sub()
                     Await _clock.Delay(FirstPacketTimeout)
                     If Not TryRemoveSocket(socket) Then Return
                     _logger.Log("Connection from {0} timed out.".Frmt(socket.Name), LogMessageType.Negative)
                     socket.Disconnect(expected:=False, reason:="Idle")
                 End Sub

            Try
                Dim packetData = Await socket.AsyncReadPacket()
                If Not TryRemoveSocket(socket) Then Return
                Dim id = CType(packetData(1), Protocol.PacketId)
                Dim pickle = ProcessConnectingPlayer(socket, packetData)
                _logger.Log(Function() "Received {0} from {1}".Frmt(id, socket.Name), LogMessageType.DataEvent)
                _logger.Log(Function() "Received {0} from {1}: {2}".Frmt(id, socket.Name, pickle.Description), LogMessageType.DataParsed)
            Catch ex As Exception
                socket.Disconnect(expected:=False, reason:=ex.Summarize)
            End Try
        End Sub

        '''<summary>Atomically checks if a socket has already been processed, and removes it if not.</summary>
        Private Function TryRemoveSocket(socket As W3Socket) As Boolean
            SyncLock lock
                If Not _sockets.Contains(socket) Then Return False
                _sockets.Remove(socket)
            End SyncLock
            Return True
        End Function

        Protected MustOverride Function ProcessConnectingPlayer(socket As W3Socket, packetData As IRist(Of Byte)) As IPickle(Of Object)
        <ContractClassFor(GetType(W3ConnectionAccepterBase))>
        Public MustInherit Class ContractClass
            Inherits W3ConnectionAccepterBase
            Protected Sub New()
                MyBase.New(Nothing, Nothing)
                Throw New NotSupportedException
            End Sub
            Protected Overrides Function ProcessConnectingPlayer(socket As W3Socket, packetData As IRist(Of Byte)) As IPickle(Of Object)
                Contract.Requires(socket IsNot Nothing)
                Contract.Requires(packetData IsNot Nothing)
                Contract.Requires(packetData.Count >= 4)
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Class

    Public NotInheritable Class W3ConnectionAccepter
        Inherits W3ConnectionAccepterBase

        Public Event Connection(sender As W3ConnectionAccepter, knockData As Protocol.KnockData, socket As W3Socket)

        Public Sub New(clock As IClock,
                       Optional logger As Logger = Nothing)
            MyBase.New(clock, logger)
            Contract.Assume(clock IsNot Nothing)
        End Sub

        Protected Overrides Function ProcessConnectingPlayer(socket As W3Socket, packetData As IRist(Of Byte)) As IPickle(Of Object)
            If packetData(1) <> Protocol.PacketId.Knock Then
                Throw New IO.InvalidDataException("{0} was not a warcraft 3 player.".Frmt(socket.Name))
            End If

            Dim pickle = Protocol.ClientPackets.Knock.Jar.ParsePickle(packetData.SkipExact(4))
            Dim knockData = pickle.Value

            socket.Name = knockData.Name
            RaiseEvent Connection(Me, knockData, socket)
            Return pickle
        End Function
    End Class

    Public NotInheritable Class W3PeerConnectionAccepter
        Inherits W3ConnectionAccepterBase

        Public Event Connection(sender As W3PeerConnectionAccepter, player As W3ConnectingPeer)

        Public Sub New(clock As IClock,
                       Optional logger As Logger = Nothing)
            MyBase.New(clock, logger)
            Contract.Assume(clock IsNot Nothing)
        End Sub

        Protected Overrides Function ProcessConnectingPlayer(socket As W3Socket, packetData As IRist(Of Byte)) As IPickle(Of Object)
            If packetData(1) <> Protocol.PacketId.PeerKnock Then
                Throw New IO.InvalidDataException("{0} was not a warcraft 3 peer connection.".Frmt(socket.Name))
            End If
            Dim pickle = Protocol.PeerPackets.PeerKnock.Jar.ParsePickle(packetData.SkipExact(4))
            Dim vals = pickle.Value.AssumeNotNull
            Dim player = New W3ConnectingPeer(socket,
                                              vals.ItemAs(Of Byte)("receiver peer key"),
                                              vals.ItemAs(Of PlayerId)("sender id"),
                                              vals.ItemAs(Of UInt32)("sender peer connection flags"))
            RaiseEvent Connection(Me, player)
            Return pickle
        End Function
    End Class
End Namespace