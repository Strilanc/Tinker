Namespace Warcraft3
    Partial Public NotInheritable Class W3Game
        Implements IW3Game
        Private ReadOnly readyPlayers As New HashSet(Of IW3Player)
        Private ReadOnly unreadyPlayers As New HashSet(Of IW3Player)
        Private ReadOnly visibleReadyPlayers As New HashSet(Of IW3Player)
        Private ReadOnly visibleUnreadyPlayers As New HashSet(Of IW3Player)
        Private ReadOnly tickCounts As New Dictionary(Of IW3Player, Integer)
        Private fakeTickTimer As Timers.Timer

        Private Sub LoadScreenNew()
            fakeTickTimer = New Timers.Timer(New TimeSpan(0, 0, seconds:=30).TotalMilliseconds)
            AddHandler fakeTickTimer.Elapsed, Sub() c_FakeTick()
        End Sub
        Private Sub LoadScreenStart()
            For Each player In players
                player.f_StartLoading()
                unreadyPlayers.Add(player)
                If IsPlayerVisible(player) Then visibleUnreadyPlayers.Add(player)
                tickCounts(player) = 0
            Next player

            'Load
            logger.log("Players Loading", LogMessageTypes.Positive)

            'Ready any lingering fake players
            For Each player In (From p In players Where p.IsFake _
                                                  AndAlso IsPlayerVisible(p) _
                                                  AndAlso FindPlayerSlot(p).contents.Moveable)
                readyPlayers.Add(player)
                unreadyPlayers.Remove(player)
                visibleReadyPlayers.Add(player)
                visibleUnreadyPlayers.Remove(player)
                player.Ready = True
                BroadcastPacket(W3Packet.MakeOtherPlayerReady(player), Nothing)
            Next player

            If server.settings.load_in_game Then
                faketickTimer.Start()
            End If
        End Sub
        Private Sub LoadScreenStop()
            faketickTimer.Stop()
        End Sub

        Private Sub LoadScreenCatchRemovedPlayer(ByVal p As IW3Player, ByVal slot As W3Slot)
            TryLaunch()
        End Sub

        '''<summary>Starts the in-game play if all players are ready</summary>
        Private Function TryLaunch() As Outcome
            If (From x In players Where Not x.Ready AndAlso Not x.IsFake).Any Then
                Return failure("Not all players are ready.")
            End If
            Return Launch()
        End Function

        '''<summary>Starts the in-game play</summary>
        Private Function Launch() As Outcome
            If server.settings.load_in_game Then
                Dim max_ticks = players.Max(Function(player) tickCounts(player))
                For Each player In players
                    For i = tickCounts(player) To max_ticks - 1
                        player.f_SendPacket(W3Packet.MakeTick(0))
                    Next i
                Next player
            End If

            ChangeState(W3GameStates.Playing)
            logger.log("Launching", LogMessageTypes.Positive)

            'start gameplay
            Me.LoadScreenStop()
            GameplayStart()
            Return success(My.Resources.Instance_Launched_f0name.frmt(name))
        End Function

        Private Sub ReceiveReady(ByVal sendingPlayer As IW3Player, ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(sendingPlayer IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)

            Dim visibleReadiedPlayer As IW3Player = Nothing
            If IsPlayerVisible(sendingPlayer) Then
                visibleReadiedPlayer = sendingPlayer
            Else
                sendingPlayer.f_SendPacket(W3Packet.MakeOtherPlayerReady(sendingPlayer))
                readyPlayers.Add(sendingPlayer)
                unreadyPlayers.Remove(sendingPlayer)

                Dim slot = FindPlayerSlot(sendingPlayer)
                If (From x In slot.contents.EnumPlayers Where Not (x.Ready Or x.IsFake)).None Then
                    visibleReadiedPlayer = slot.contents.EnumPlayers.First
                End If
            End If

            If visibleReadiedPlayer IsNot Nothing Then
                If Not server.settings.load_in_game Then
                    BroadcastPacket(W3Packet.MakeOtherPlayerReady(visibleReadiedPlayer), Nothing)
                End If
                readyPlayers.Add(visibleReadiedPlayer)
                unreadyPlayers.Remove(visibleReadiedPlayer)
                visibleReadyPlayers.Add(visibleReadiedPlayer)
                visibleUnreadyPlayers.Remove(visibleReadiedPlayer)
            End If

            If server.settings.load_in_game Then
                'Inform readied player
                For Each player In visibleReadyPlayers.Concat(visibleUnreadyPlayers)
                    Contract.Assume(player IsNot Nothing)
                    sendingPlayer.f_SendPacket(W3Packet.MakeOtherPlayerReady(player))
                Next player
                If visibleUnreadyPlayers.Any Then
                    SendMessageTo("PLAYERS STILL LOADING", sendingPlayer)
                    sendingPlayer.f_SendPacket(W3Packet.MakeShowLagScreen(visibleUnreadyPlayers))
                End If

                'Inform other players
                For Each player In readyPlayers
                    Contract.Assume(player IsNot Nothing)
                    If player IsNot sendingPlayer Then
                        SendMessageTo("{0} is ready.".frmt(sendingPlayer.name), player)
                    End If
                Next player
                If visibleReadiedPlayer IsNot Nothing Then
                    For Each player In readyPlayers
                        Contract.Assume(player IsNot Nothing)
                        If player IsNot sendingPlayer Then
                            player.f_SendPacket(W3Packet.MakeRemovePlayerFromLagScreen(visibleReadiedPlayer, 0))
                        End If
                    Next player
                End If
            End If

            TryLaunch()
        End Sub

        Private Sub c_FakeTick()
            ref.QueueAction(
                Sub()
                    If state > W3GameStates.Loading Then  Return

                    For Each receiver In readyPlayers
                        If Not receiver.IsFake Then
                            tickCounts(receiver) += 1
                            For Each player In visibleUnreadyPlayers
                                Contract.Assume(player IsNot Nothing)
                                receiver.f_SendPacket(W3Packet.MakeRemovePlayerFromLagScreen(player, 0))
                            Next player
                            receiver.f_SendPacket(W3Packet.MakeTick(0))
                            receiver.f_SendPacket(W3Packet.MakeShowLagScreen(visibleUnreadyPlayers))
                        End If
                    Next receiver
                End Sub
            )
        End Sub

        Private Function _receivePacket_READY(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture Implements IW3Game.f_ReceiveReady
            Return ref.QueueAction(Sub()
                                       Contract.Assume(player IsNot Nothing)
                                       Contract.Assume(vals IsNot Nothing)
                                       ReceiveReady(player, vals)
                                   End Sub)
        End Function
    End Class
End Namespace