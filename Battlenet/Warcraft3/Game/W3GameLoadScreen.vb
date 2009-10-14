Namespace Warcraft3
    Partial Public NotInheritable Class W3Game
        Private ReadOnly readyPlayers As New HashSet(Of W3Player)
        Private ReadOnly unreadyPlayers As New HashSet(Of W3Player)
        Private ReadOnly visibleReadyPlayers As New HashSet(Of W3Player)
        Private ReadOnly visibleUnreadyPlayers As New HashSet(Of W3Player)
        Private numFakeTicks As Integer
        Private fakeTickTimer As Timers.Timer

        Private Sub LoadScreenNew()
            fakeTickTimer = New Timers.Timer(30.Seconds.TotalMilliseconds)
            AddHandler fakeTickTimer.Elapsed, Sub() c_FakeTick()
        End Sub
        Private Sub LoadScreenStart()
            For Each player In players
                Contract.Assume(player IsNot Nothing)
                player.QueueStartLoading()
                unreadyPlayers.Add(player)
                If IsPlayerVisible(player) Then visibleUnreadyPlayers.Add(player)
            Next player

            'Load
            logger.log("Players Loading", LogMessageType.Positive)

            'Ready any lingering fake players
            For Each player In (From p In players Where p.isFake _
                                                  AndAlso IsPlayerVisible(p) _
                                                  AndAlso FindPlayerSlot(p).Contents.Moveable)
                Contract.Assume(player IsNot Nothing)
                readyPlayers.Add(player)
                unreadyPlayers.Remove(player)
                visibleReadyPlayers.Add(player)
                visibleUnreadyPlayers.Remove(player)
                player.Ready = True
                BroadcastPacket(W3Packet.MakeOtherPlayerReady(player), Nothing)
            Next player

            If settings.loadInGame Then
                fakeTickTimer.Start()
            End If
        End Sub
        Private Sub LoadScreenStop()
            fakeTickTimer.Stop()
        End Sub

        Private Sub OnLoadScreenRemovedPlayer(ByVal player As W3Player, ByVal slot As W3Slot)
            TryLaunch()
        End Sub

        '''<summary>Starts the in-game play if all players are ready</summary>
        Private Function TryLaunch() As Boolean
            If (From x In players Where Not x.Ready AndAlso Not x.isFake).Any Then
                Return False
            End If
            ChangeState(W3GameState.Playing)
            Logger.Log("Launching", LogMessageType.Positive)

            'start gameplay
            Me.LoadScreenStop()
            GameplayStart()
            Return True
        End Function

        '''<summary>Starts the in-game play</summary>
        Private Sub Launch()
            If Not TryLaunch() Then
                Throw New InvalidOperationException("Not all players are ready.")
            End If
        End Sub

        Private Sub ReceiveReady(ByVal sendingPlayer As W3Player)
            Contract.Requires(sendingPlayer IsNot Nothing)

            'Get if there is a visible readied player
            Dim visibleReadiedPlayer As W3Player = Nothing
            If IsPlayerVisible(sendingPlayer) Then
                visibleReadiedPlayer = sendingPlayer
            Else
                sendingPlayer.QueueSendPacket(W3Packet.MakeOtherPlayerReady(sendingPlayer))
                readyPlayers.Add(sendingPlayer)
                unreadyPlayers.Remove(sendingPlayer)

                Dim slot = FindPlayerSlot(sendingPlayer)
                Contract.Assume(slot IsNot Nothing)
                If (From x In slot.Contents.EnumPlayers Where Not (x.Ready Or x.isFake)).None Then
                    visibleReadiedPlayer = slot.Contents.EnumPlayers.First
                End If
            End If

            If visibleReadiedPlayer IsNot Nothing Then
                readyPlayers.Add(visibleReadiedPlayer)
                unreadyPlayers.Remove(visibleReadiedPlayer)
                visibleReadyPlayers.Add(visibleReadiedPlayer)
                visibleUnreadyPlayers.Remove(visibleReadiedPlayer)
            End If

            If settings.loadInGame Then
                For Each player In players
                    If IsPlayerVisible(player) Then
                        sendingPlayer.QueueSendPacket(W3Packet.MakeOtherPlayerReady(player))
                    End If
                Next player
                For i = 1 To numFakeTicks
                    sendingPlayer.QueueSendPacket(W3Packet.MakeTick(0))
                Next i

                If unreadyPlayers.Any Then
                    SendMessageTo("{0} players still loading.".Frmt(unreadyPlayers.Count), sendingPlayer)
                End If
                For Each player In readyPlayers
                    If player IsNot sendingPlayer Then
                        SendMessageTo("{0} is ready.".Frmt(sendingPlayer.name), player)
                        If visibleReadiedPlayer IsNot Nothing Then
                            player.QueueSendPacket(W3Packet.MakeRemovePlayerFromLagScreen(visibleReadiedPlayer, 0))
                        End If
                    End If
                Next player

                If visibleUnreadyPlayers.Count > 0 Then
                    sendingPlayer.QueueSendPacket(W3Packet.MakeShowLagScreen(visibleUnreadyPlayers))
                End If
            Else
                If visibleReadiedPlayer IsNot Nothing Then
                    BroadcastPacket(W3Packet.MakeOtherPlayerReady(visibleReadiedPlayer), Nothing)
                End If
            End If

            TryLaunch()
        End Sub


        Private Sub c_FakeTick()
            ref.QueueAction(
                Sub()
                    If state > W3GameState.Loading Then  Return
                    If readyPlayers.Count = 0 Then  Return

                    numFakeTicks += 1
                    For Each player In readyPlayers
                        Contract.Assume(player IsNot Nothing)
                        For Each other In visibleUnreadyPlayers
                            Contract.Assume(other IsNot Nothing)
                            player.QueueSendPacket(W3Packet.MakeRemovePlayerFromLagScreen(other, 0))
                        Next other
                        player.QueueSendPacket(W3Packet.MakeTick(0))
                        player.QueueSendPacket(W3Packet.MakeShowLagScreen(visibleUnreadyPlayers))
                    Next player
                End Sub
            )
        End Sub

        Public Function QueueReceiveReady(ByVal player As W3Player) As IFuture
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(player IsNot Nothing)
                                       ReceiveReady(player)
                                   End Sub)
        End Function
    End Class
End Namespace