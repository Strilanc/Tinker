Imports Tinker.Pickling

Namespace WC3
    Partial Public NotInheritable Class Player
        Implements IPlayerDownloadAspect

        Public Event SuperficialStateUpdated(ByVal sender As Player)
        Public Event StateUpdated(ByVal sender As Player)
        Private _reportedDownloadPosition As UInt32? = Nothing

        Private Function AddQueuedPacketHandler(ByVal jar As Protocol.Packets.SimpleDefinition,
                                                ByVal handler As Action(Of IPickle(Of Dictionary(Of InvariantString, Object)))) As IDisposable
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return AddQueuedPacketHandler(jar.id, jar, handler)
        End Function
        Private Function AddQueuedPacketHandler(Of T)(ByVal id As Protocol.PacketId,
                                                      ByVal jar As IJar(Of T),
                                                      ByVal handler As Action(Of IPickle(Of T))) As IDisposable
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            packetHandler.AddLogger(id, jar.Weaken)
            Return packetHandler.AddHandler(id, Function(data) inQueue.QueueAction(Sub() handler(jar.Parse(data))))
        End Function

        Public Function QueueAddPacketHandler(ByVal packet As Protocol.Packets.SimpleDefinition,
                                              ByVal handler As Func(Of IPickle(Of Dictionary(Of InvariantString, Object)), ifuture)) As IFuture(Of IDisposable)
            Contract.Requires(packet IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() packetHandler.AddHandler(packet.id, Function(data) handler(packet.Parse(data))))
        End Function
        Public Function QueueAddPacketHandler(ByVal id As Protocol.PacketId,
                                              ByVal handler As Func(Of IReadableList(Of Byte), IFuture)) As IFuture(Of IDisposable)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() packetHandler.AddHandler(id, handler))
        End Function
        Public Function QueueAddPacketHandler(Of T)(ByVal id As Protocol.PacketId,
                                                    ByVal jar As IParseJar(Of T),
                                                    ByVal handler As Func(Of IPickle(Of T), IFuture)) As IFuture(Of IDisposable) _
                                                    Implements IPlayerDownloadAspect.QueueAddPacketHandler
            Return QueueAddPacketHandler(id, Function(data) handler(jar.Parse(data)))
        End Function

        <Pure()>
        <ContractVerification(False)>
        Public Function MakePacketOtherPlayerJoined() As Protocol.Packet Implements IPlayerDownloadAspect.MakePacketOtherPlayerJoined
            Contract.Ensures(Contract.Result(Of Protocol.Packet)() IsNot Nothing)
            Return Protocol.MakeOtherPlayerJoined(Name, PID, peerKey, peerData, New Net.IPEndPoint(RemoteEndPoint.Address, ListenPort))
        End Function

        Private Sub LobbyStart()
            state = PlayerState.Lobby
            AddQueuedPacketHandler(Protocol.PacketId.PeerConnectionInfo,
                                   Protocol.Packets.PeerConnectionInfo,
                                   handler:=AddressOf OnReceivePeerConnectionInfo)
            AddQueuedPacketHandler(Protocol.Packets.ClientMapInfo, AddressOf OnReceiveClientMapInfo)
        End Sub

        Private Sub OnReceivePeerConnectionInfo(ByVal flags As IPickle(Of UInt16))
            Contract.Requires(flags IsNot Nothing)
            _numPeerConnections = (From i In enumerable.Range(0, 12)
                                   Where ((flags.Value >> i) And &H1) <> 0).Count
            Contract.Assume(_numPeerConnections <= 12)
            RaiseEvent SuperficialStateUpdated(Me)
        End Sub
        Private Sub OnReceiveClientMapInfo(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(pickle IsNot Nothing)
            _reportedDownloadPosition = CUInt(pickle.Value("total downloaded"))
            outQueue.QueueAction(Sub() RaiseEvent StateUpdated(Me))
        End Sub
    End Class
End Namespace