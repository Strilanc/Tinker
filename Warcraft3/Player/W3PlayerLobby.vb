Imports Tinker.Pickling

Namespace WC3
    Partial Public NotInheritable Class Player
        Private knowMapState As Boolean
        Private mapDownloadPosition As Integer = -1
        Public IsGettingMapFromBot As Boolean
        Private mapUploadPosition As Integer
        Private Const MAX_BUFFERED_MAP_SIZE As UInteger = 64000

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
        Private Sub LobbyStart()
            state = PlayerState.Lobby
            AddQueuedPacketHandler(Protocol.Packets.ClientMapInfo, AddressOf ReceiveClientMapInfo)
            AddQueuedPacketHandler(Protocol.Packets.PeerConnectionInfo, AddressOf ReceivePeerConnectionInfo)
        End Sub

#Region "Networking"
        Public Event SuperficialStateUpdated(ByVal sender As Player)
        Public Event StateUpdated(ByVal sender As Player)
        Private Sub ReceivePeerConnectionInfo(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(pickle IsNot Nothing)
            Dim vals = CType(pickle.Value, Dictionary(Of InvariantString, Object))
            Dim dword = CUInt(vals("player bitflags"))
            Dim flags = From i In enumerable.Range(0, 12)
                        Select connected = ((dword >> i) And &H1) <> 0,
                               pid = CByte(i + 1)
            _numPeerConnections = (From flag In flags Where flag.connected).Count
            Contract.Assume(_numPeerConnections <= 12)

            If state = PlayerState.Lobby Then
                For Each flag In flags
                    Contract.Assume(flag IsNot Nothing)
                    scheduler.SetLink(Me.Index, flag.pid, flag.connected).SetHandled()
                Next flag
            End If
            RaiseEvent SuperficialStateUpdated(Me)
        End Sub
        Private Sub ReceiveClientMapInfo(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(pickle IsNot Nothing)
            Dim vals = CType(pickle.Value, Dictionary(Of InvariantString, Object))
            Dim newMapDownloadPosition = CInt(CUInt(vals("total downloaded")))
            Dim delta = newMapDownloadPosition - mapDownloadPosition
            If delta < 0 Then
                Disconnect(True, PlayerLeaveType.Disconnect, "auto-booted: moved download position backwards from {1} to {2}.".Frmt(mapDownloadPosition, newMapDownloadPosition))
                Return
            ElseIf newMapDownloadPosition > settings.Map.FileSize Then
                Disconnect(True, PlayerLeaveType.Disconnect, "auto-booted: moved download position past file size")
                Return
            ElseIf mapDownloadPosition = settings.Map.FileSize Then
                '[previously finished download]
                Return
            End If

            mapDownloadPosition = newMapDownloadPosition
            mapUploadPosition = Math.Max(mapDownloadPosition, mapUploadPosition)
            If Not knowMapState Then
                Dim hasMap = mapDownloadPosition = settings.Map.FileSize
                If Not hasMap AndAlso Not settings.AllowDownloads Then
                    Disconnect(True, PlayerLeaveType.Disconnect, "no dls allowed")
                    Return
                End If

                scheduler.AddClient(Index, hasMap).SetHandled()
                scheduler.SetLink(Index, Game.LocalTransferClientKey, linked:=True).SetHandled()
                knowMapState = True
            ElseIf mapDownloadPosition = settings.Map.FileSize Then
                logger.Log("{0} finished downloading the map.".Frmt(Name), LogMessageType.Positive)
                scheduler.SetNotTransfering(Index, completed:=True).SetHandled()
            Else
                Dim progress = New FiniteDouble(mapDownloadPosition)
                Contract.Assume(progress >= 0)
                scheduler.UpdateProgress(Index, progress).SetHandled()
                If IsGettingMapFromBot Then
                    BufferMap()
                End If
            End If

            RaiseEvent StateUpdated(Me)
        End Sub
#End Region

#Region "Interface"
        Public ReadOnly Property GetMapDownloadPosition() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Return mapDownloadPosition
            End Get
        End Property
        Public Function QueueBufferMap() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(AddressOf BufferMap)
        End Function
#End Region

#Region "Misc"
        Public Event WantMapSender(ByVal sender As Player)
        Public Sub GiveMapSender(ByVal senderIndex As Byte)
            Contract.Requires(senderIndex >= 0)
            Contract.Requires(senderIndex <= 12)
            inQueue.QueueAction(
                Sub()
                    While mapUploadPosition < Math.Min(settings.Map.FileSize, mapDownloadPosition + MAX_BUFFERED_MAP_SIZE)
                        Dim out_DataSize = 0
                        Contract.Assume(senderIndex >= 0)
                        Contract.Assume(senderIndex <= 12)
                        Dim pk = Protocol.MakeMapFileData(settings.Map, Index, mapUploadPosition, out_DataSize, senderIndex)
                        mapUploadPosition += out_DataSize
                        Try
                            SendPacket(pk)
                        Catch e As Exception '[check this more thoroughly]
                            Exit While
                        End Try
                    End While
                End Sub)
        End Sub
        Private Sub BufferMap()
            RaiseEvent WantMapSender(Me)
        End Sub
#End Region
    End Class
End Namespace