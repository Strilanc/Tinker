Imports Tinker.Pickling

Namespace WC3
    Partial Public NotInheritable Class Player
        Implements Download.IPlayerDownloadAspect

        Public Event SuperficialStateUpdated(ByVal sender As Player)
        Public Event StateUpdated(ByVal sender As Player)
        Private _reportedDownloadPosition As UInt32? = Nothing

        Private Function AddQueuedLocalPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                           ByVal handler As Action(Of IPickle(Of T))) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            packetHandler.AddLogger(packetDefinition.Id, packetDefinition.Jar)
            Return packetHandler.AddHandler(packetDefinition.Id, Function(data) inQueue.QueueAction(Sub() handler(packetDefinition.Jar.Parse(data))))
        End Function

        Private Function AddRemotePacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                      ByVal handler As Func(Of IPickle(Of T), Task)) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            packetHandler.AddLogger(packetDefinition.Id, packetDefinition.Jar)
            Return packetHandler.AddHandler(packetDefinition.Id, Function(data) handler(packetDefinition.Jar.Parse(data)))
        End Function
        Public Function QueueAddPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                    ByVal handler As Func(Of IPickle(Of T), Task)) As Task(Of IDisposable) _
                                                    Implements Download.IPlayerDownloadAspect.QueueAddPacketHandler
            Return inQueue.QueueFunc(Function() AddRemotePacketHandler(packetDefinition, handler))
        End Function

        <Pure()>
        <ContractVerification(False)>
        Public Function MakePacketOtherPlayerJoined() As Protocol.Packet Implements Download.IPlayerDownloadAspect.MakePacketOtherPlayerJoined
            Contract.Ensures(Contract.Result(Of Protocol.Packet)() IsNot Nothing)
            Return Protocol.MakeOtherPlayerJoined(Name, Id, peerKey, PeerData, New Net.IPEndPoint(RemoteEndPoint.Address, ListenPort))
        End Function

        Private Sub LobbyStart()
            state = PlayerState.Lobby
            AddQueuedLocalPacketHandler(Protocol.Packets.PeerConnectionInfo, AddressOf OnReceivePeerConnectionInfo)
            AddQueuedLocalPacketHandler(Protocol.Packets.ClientMapInfo, AddressOf OnReceiveClientMapInfo)
        End Sub

        Private Sub OnReceivePeerConnectionInfo(ByVal flags As IPickle(Of UInt16))
            Contract.Requires(flags IsNot Nothing)
            _numPeerConnections = (From i In 12.Range Where flags.Value.HasBitSet(i)).Count
            Contract.Assume(_numPeerConnections <= 12)
            RaiseEvent SuperficialStateUpdated(Me)
        End Sub
        Private Sub OnReceiveClientMapInfo(ByVal pickle As IPickle(Of NamedValueMap))
            Contract.Requires(pickle IsNot Nothing)
            _reportedDownloadPosition = pickle.Value.ItemAs(Of UInt32)("total downloaded")
            outQueue.QueueAction(Sub() RaiseEvent StateUpdated(Me))
        End Sub
    End Class
End Namespace