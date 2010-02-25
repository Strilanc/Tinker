Namespace WC3
    Partial Public NotInheritable Class Game
        Private ReadOnly readyPlayers As New HashSet(Of Player)
        Private ReadOnly unreadyPlayers As New HashSet(Of Player)
        Private ReadOnly visibleReadyPlayers As New HashSet(Of Player)
        Private ReadOnly visibleUnreadyPlayers As New HashSet(Of Player)
        Private numFakeTicks As Integer
        Private fakeTickTimer As Timers.Timer

        Public Event Launched(ByVal sender As Game, ByVal usingLoadInGame As Boolean)

        Private Sub LoadScreenNew()
            fakeTickTimer = New Timers.Timer(30.Seconds.TotalMilliseconds)
            AddHandler fakeTickTimer.Elapsed, Sub() OnFakeTick()
        End Sub
        Private Sub LoadScreenStart()
            For Each player In _players
                Contract.Assume(player IsNot Nothing)
                player.QueueStartLoading()
                unreadyPlayers.Add(player)
                If _lobby.IsPlayerVisible(player) Then visibleUnreadyPlayers.Add(player)
            Next player

            'Load
            logger.log("Players Loading", LogMessageType.Positive)

            'Ready any lingering fake players
            For Each player In From p In _players Where p.isFake _
                                                  AndAlso _lobby.IsPlayerVisible(p) _
                                                  AndAlso _lobby.Slots.TryFindPlayerSlot(p).Contents.Moveable
                Contract.Assume(player IsNot Nothing)
                readyPlayers.Add(player)
                unreadyPlayers.Remove(player)
                visibleReadyPlayers.Add(player)
                visibleUnreadyPlayers.Remove(player)
                player.Ready = True
                BroadcastPacket(Protocol.MakeOtherPlayerReady(player.Id), Nothing)
            Next player

            If settings.UseLoadInGame Then
                outQueue.QueueAction(Sub() RaiseEvent Launched(Me, usingloadInGame:=True))
                fakeTickTimer.Start()
            End If
        End Sub
        Private Sub LoadScreenStop()
            fakeTickTimer.Stop()
        End Sub

        Private Sub OnLoadScreenRemovedPlayer()
            TryLaunch()
        End Sub

        '''<summary>Starts the in-game play if all players are ready</summary>
        Private Function TryLaunch() As Boolean
            If (From x In _players Where Not x.Ready AndAlso Not x.isFake).Any Then
                Return False
            End If
            ChangeState(GameState.Playing)
            Logger.Log("Launching", LogMessageType.Positive)

            'start gameplay
            Me.LoadScreenStop()
            Motor.QueueStart()
            If Not settings.UseLoadInGame Then
                outQueue.QueueAction(Sub() RaiseEvent Launched(Me, usingloadInGame:=False))
            End If
            Return True
        End Function

        '''<summary>Starts the in-game play</summary>
        Private Sub Launch()
            If Not TryLaunch() Then
                Throw New InvalidOperationException("Not all players are ready.")
            End If
        End Sub

        Private Sub ReceiveReady(ByVal sendingPlayer As Player)
            Contract.Requires(sendingPlayer IsNot Nothing)

            'Get if there is a visible readied player
            Dim visibleReadiedPlayer As Player = Nothing
            If _lobby.IsPlayerVisible(sendingPlayer) Then
                visibleReadiedPlayer = sendingPlayer
            Else
                sendingPlayer.QueueSendPacket(Protocol.MakeOtherPlayerReady(sendingPlayer.Id))
                readyPlayers.Add(sendingPlayer)
                unreadyPlayers.Remove(sendingPlayer)

                Dim slot = _lobby.Slots.TryFindPlayerSlot(sendingPlayer)
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

            If settings.useLoadInGame Then
                For Each player In _players
                    Contract.Assume(player IsNot Nothing)
                    If _lobby.IsPlayerVisible(player) Then
                        sendingPlayer.QueueSendPacket(Protocol.MakeOtherPlayerReady(player.Id))
                    End If
                Next player
                For i = 1 To numFakeTicks
                    sendingPlayer.QueueSendPacket(Protocol.MakeTick(0))
                Next i

                If unreadyPlayers.Any Then
                    SendMessageTo("{0} players still loading.".Frmt(unreadyPlayers.Count), sendingPlayer)
                End If
                For Each player In readyPlayers
                    Contract.Assume(player IsNot Nothing)
                    If player IsNot sendingPlayer Then
                        SendMessageTo("{0} is ready.".Frmt(sendingPlayer.Name), player)
                        If visibleReadiedPlayer IsNot Nothing Then
                            player.QueueSendPacket(Protocol.MakeRemovePlayerFromLagScreen(visibleReadiedPlayer.Id, 0))
                        End If
                    End If
                Next player

                If visibleUnreadyPlayers.Count > 0 Then
                    sendingPlayer.QueueSendPacket(Protocol.MakeShowLagScreen(From p In visibleUnreadyPlayers Select p.Id))
                End If
            Else
                If visibleReadiedPlayer IsNot Nothing Then
                    BroadcastPacket(Protocol.MakeOtherPlayerReady(visibleReadiedPlayer.Id), Nothing)
                End If
            End If

            TryLaunch()
        End Sub

        Private Sub OnFakeTick()
            inQueue.QueueAction(
                Sub()
                    If state > GameState.Loading Then Return
                    If readyPlayers.Count = 0 Then Return

                    numFakeTicks += 1
                    For Each player In readyPlayers
                        Contract.Assume(player IsNot Nothing)
                        For Each other In visibleUnreadyPlayers
                            Contract.Assume(other IsNot Nothing)
                            player.QueueSendPacket(Protocol.MakeRemovePlayerFromLagScreen(other.Id, 0))
                        Next other
                        player.QueueSendPacket(Protocol.MakeTick(0))
                        player.QueueSendPacket(Protocol.MakeShowLagScreen(From p In visibleUnreadyPlayers Select p.Id))
                    Next player
                End Sub
            )
        End Sub
    End Class
End Namespace