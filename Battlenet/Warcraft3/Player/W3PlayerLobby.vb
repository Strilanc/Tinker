Namespace Warcraft3
    Partial Public NotInheritable Class W3Player
        Private knowMapState As Boolean
        Private mapDownloadPosition As Integer = -1
        Public IsGettingMapFromBot As Boolean
        Private mapUploadPosition As Integer
        Private countdowns As Integer
        Private Const MAX_BUFFERED_MAP_SIZE As UInteger = 64000
        Private ReadOnly packetHandlers As New Dictionary(Of W3PacketId, Action(Of W3Packet))

        Private Sub LobbyStart()
            state = W3PlayerState.Lobby
            packetHandlers(W3PacketId.ClientMapInfo) = AddressOf ReceiveClientMapInfo
            packetHandlers(W3PacketId.PeerConnectionInfo) = AddressOf ReceivePeerConnectionInfo
        End Sub
        Private Sub LobbyStop()
            packetHandlers.Remove(W3PacketId.ClientMapInfo)
        End Sub

#Region "Networking"
        Public Event GameStateUpdated1(ByVal sender As W3Player)
        Public Event GameStateUpdated2(ByVal sender As W3Player)
        Private Sub ReceivePeerConnectionInfo(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.Payload.Value, Dictionary(Of String, Object))
            Contract.Assume(vals IsNot Nothing)
            Dim dword = CUInt(vals("player bitflags"))
            Dim flags = From i In enumerable.Range(0, 12)
                        Select connected = ((dword >> i) And &H1) <> 0,
                               pid = CByte(i + 1)
            numPeerConnections = (From flag In flags Where flag.connected).Count
            Contract.Assume(numPeerConnections >= 0)
            Contract.Assume(numPeerConnections <= 12)

            If state = W3PlayerState.Lobby Then
                For Each flag In flags
                    scheduler.SetLink(Me.Index, flag.pid, flag.connected).MarkAnyExceptionAsHandled()
                Next flag
            End If
            RaiseEvent GameStateUpdated1(Me)
        End Sub
        Private Sub ReceiveClientMapInfo(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.Payload.Value, Dictionary(Of String, Object))
            Contract.Assume(vals IsNot Nothing)
            Dim newMapDownloadPosition = CInt(CUInt(vals("total downloaded")))
            Dim delta = newMapDownloadPosition - mapDownloadPosition
            If delta < 0 Then
                Disconnect(True, W3PlayerLeaveType.Disconnect, "auto-booted: moved download position backwards from {1} to {2}.".Frmt(mapDownloadPosition, newMapDownloadPosition))
                Return
            ElseIf newMapDownloadPosition > settings.Map.FileSize Then
                Disconnect(True, W3PlayerLeaveType.Disconnect, "auto-booted: moved download position past file size")
                Return
            ElseIf mapDownloadPosition = settings.Map.FileSize Then
                '[previously finished download]
                Return
            End If

            mapDownloadPosition = newMapDownloadPosition
            mapUploadPosition = Math.Max(mapDownloadPosition, mapUploadPosition)
            If Not knowMapState Then
                Dim hasMap = mapDownloadPosition = settings.Map.FileSize
                If Not hasMap AndAlso Not settings.allowDownloads Then
                    Disconnect(True, W3PlayerLeaveType.Disconnect, "no dls allowed")
                    Return
                End If

                scheduler.AddClient(Index, hasMap).MarkAnyExceptionAsHandled()
                scheduler.SetLink(Index, W3Game.LocalTransferClientKey, linked:=True).MarkAnyExceptionAsHandled()
                knowMapState = True
            ElseIf mapDownloadPosition = settings.Map.FileSize Then
                logger.Log("{0} finished downloading the map.".Frmt(name), LogMessageType.Positive)
                scheduler.SetNotTransfering(Index, completed:=True).MarkAnyExceptionAsHandled()
            Else
                scheduler.UpdateProgress(Index, New FiniteDouble(mapDownloadPosition)).MarkAnyExceptionAsHandled()
                If IsGettingMapFromBot Then
                    BufferMap()
                End If
            End If

            RaiseEvent GameStateUpdated2(Me)
        End Sub
#End Region

#Region "Interface"
        Public ReadOnly Property GetMapDownloadPosition() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Return mapDownloadPosition
            End Get
        End Property
        Public ReadOnly Property IsOverCounted() As Boolean
            Get
                Return countdowns > 1
            End Get
        End Property
        Public Function QueueBufferMap() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(AddressOf BufferMap)
        End Function
        Public Function QueueStartCountdown() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(AddressOf StartCountdown)
        End Function
#End Region

#Region "Misc"
        Private Sub StartCountdown()
            countdowns += 1
            If countdowns > 1 Then Return
            SendPacket(W3Packet.MakeStartCountdown())
        End Sub

        Public Event WantMapSender(ByVal sender As W3Player)
        Public Sub GiveMapSender(ByVal senderIndex As Byte)
            Contract.Requires(senderIndex >= 0)
            Contract.Requires(senderIndex <= 12)
            ref.QueueAction(
                Sub()
                    While mapUploadPosition < Math.Min(settings.Map.FileSize, mapDownloadPosition + MAX_BUFFERED_MAP_SIZE)
                        Dim out_DataSize = 0
                        Contract.Assume(senderIndex >= 0)
                        Contract.Assume(senderIndex <= 12)
                        Dim pk = W3Packet.MakeMapFileData(settings.Map, Index, mapUploadPosition, out_DataSize, senderIndex)
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