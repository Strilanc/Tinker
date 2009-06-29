Namespace Warcraft3
    Partial Public Class W3Player
        Implements IW3Player

        Private knowMapState As Boolean
        Private mapDownloadPosition As Integer = -1
        Private gettingMapFromBot As Boolean
        Private mapUploadPosition As Integer
        Private countdowns As Integer
        Private Const MAX_BUFFERED_MAP_SIZE As UInteger = 64000
        Private ReadOnly handlers(0 To 255) As Action(Of Dictionary(Of String, Object))

        Private Sub LobbyStart()
            state = W3PlayerStates.Lobby
            handlers(W3PacketId.ClientMapInfo) = AddressOf ReceiveClientMapInfo
            handlers(W3PacketId.PeerConnectionInfo) = AddressOf ReceivePeerConnectionInfo
        End Sub
        Private Sub LobbyStop()
            handlers(W3PacketId.ClientMapInfo) = Nothing
        End Sub

#Region "Networking"
        Private Sub ReceivePeerConnectionInfo(ByVal vals As Dictionary(Of String, Object))
            Dim dword = CUInt(vals("player bitflags"))
            Dim flags = From i In enumerable.Range(0, 12)
                        Select connected = ((dword >> i) And &H1) <> 0,
                               pid = CByte(i + 1)
            numPeerConnections = (From flag In flags Where flag.connected).Count

            If state = W3PlayerStates.Lobby Then
                For Each flag In flags
                    game.DownloadScheduler.SetLink(Me.index, flag.pid, flag.connected)
                Next flag
            End If
            game.f_ThrowUpdated()
        End Sub
        Private Sub ReceiveClientMapInfo(ByVal vals As Dictionary(Of String, Object))
            Dim newMapDownloadPosition = CInt(CUInt(vals("total downloaded")))
            Dim delta = newMapDownloadPosition - mapDownloadPosition
            If delta < 0 Then
                Disconnect(True, W3PlayerLeaveTypes.Disconnect, "auto-booted: moved download position backwards from {1} to {2}.".frmt(mapDownloadPosition, newMapDownloadPosition))
                Return
            ElseIf newMapDownloadPosition > game.map.FileSize Then
                Disconnect(True, W3PlayerLeaveTypes.Disconnect, "auto-booted: moved download position past file size")
                Return
            ElseIf mapDownloadPosition = game.map.FileSize Then
                '[previously finished download]
                Return
            End If

            mapDownloadPosition = newMapDownloadPosition
            mapUploadPosition = Math.Max(mapDownloadPosition, mapUploadPosition)
            If Not knowMapState Then
                Dim hasMap = mapDownloadPosition = game.map.FileSize
                If Not hasMap AndAlso Not game.server.settings.allowDownloads Then
                    Disconnect(True, W3PlayerLeaveTypes.Disconnect, "no dls allowed")
                    Return
                End If

                Dim d As Double = 0
                Contract.Assume(d >= 0)
                Contract.Assume(Not Double.IsInfinity(d))
                Contract.Assume(Not Double.IsNaN(d))
                game.DownloadScheduler.AddClient(index, hasMap, d)
                game.DownloadScheduler.SetLink(index, W3Game.SELF_DOWNLOAD_ID, True)
                knowMapState = True
            ElseIf mapDownloadPosition = game.map.FileSize Then
                logger.log("{0} finished downloading the map.".frmt(name), LogMessageTypes.Positive)
                game.DownloadScheduler.StopTransfer(index, True)
            Else
                Dim d = CDbl(mapDownloadPosition)
                Contract.Assume(d >= 0)
                Contract.Assume(Not Double.IsNaN(d))
                Contract.Assume(Not Double.IsInfinity(d))
                game.DownloadScheduler.UpdateProgress(index, d)
                If gettingMapFromBot Then
                    BufferMap()
                End If
            End If

            game.f_UpdatedGameState()
        End Sub
#End Region

#Region "Interface"
        Private ReadOnly Property _MapDownloadPosition() As Integer Implements IW3Player.MapDownloadPosition
            Get
                Return mapDownloadPosition
            End Get
        End Property
        Private ReadOnly Property _overcounted() As Boolean Implements IW3Player.overcounted
            Get
                Return countdowns > 1
            End Get
        End Property
        Private Property _IsGettingMapFromBot() As Boolean Implements IW3Player.IsGettingMapFromBot
            Get
                Return gettingMapFromBot
            End Get
            Set(ByVal value As Boolean)
                gettingMapFromBot = value
            End Set
        End Property
        Private Function _f_BufferMap() As IFuture Implements IW3Player.f_BufferMap
            Return ref.QueueAction(AddressOf BufferMap)
        End Function
        Private Function _f_StartCountdown() As IFuture Implements IW3Player.f_StartCountdown
            Return ref.QueueAction(AddressOf StartCountdown)
        End Function
#End Region

#Region "Misc"
        Private Sub StartCountdown()
            countdowns += 1
            If countdowns > 1 Then Return
            SendPacket(W3Packet.MakeStartCountdown())
        End Sub

        Private Sub BufferMap()
            Dim f_host = game.f_FakeHostPlayer
            Dim f_index = f_host.EvalWhenValueReady(Function(player) If(player Is Nothing, CByte(0), player.index))
            f_index.CallWhenValueReady(
                Sub(senderIndex)
                                           ref.QueueAction(
                                               Sub()
                                                   While mapUploadPosition < Math.Min(game.map.FileSize, mapDownloadPosition + MAX_BUFFERED_MAP_SIZE)
                                                       Dim out_DataSize = 0
                                                       Dim pk = W3Packet.MakeMapFileData(game.map, index, mapUploadPosition, out_DataSize, senderIndex)
                                                       mapUploadPosition += out_DataSize
                                                       SendPacket(pk)
                                                   End While
                                               End Sub
                                           )
                                       End Sub
            )
        End Sub
#End Region
    End Class
End Namespace