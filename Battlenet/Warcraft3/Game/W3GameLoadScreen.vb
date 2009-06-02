Namespace Warcraft3
    Partial Public NotInheritable Class W3Game
        Private Class W3GameSoul_Load
            Inherits W3Game.W3GamePart
            Implements IW3GameLoadScreen
            Private ready_players As New HashSet(Of IW3Player)
            Private unready_players As New HashSet(Of IW3Player)
            Private visible_ready_players As New HashSet(Of IW3Player)
            Private visible_unready_players As New HashSet(Of IW3Player)
            Private tick_counts As New Dictionary(Of IW3Player, Integer)
            Private WithEvents tick_timer As New Timers.Timer(New TimeSpan(0, 0, 30).TotalMilliseconds)

            Public Sub New(ByVal body As W3Game)
                MyBase.new(body)
            End Sub

            Public Sub Start()
                For Each player In game.players
                    player.load_screen.f_Start()
                    unready_players.Add(player)
                    If game.IsPlayerVisible(player) Then visible_unready_players.Add(player)
                    tick_counts(player) = 0
                Next player

                'Load
                game.logger.log("Players Loading", LogMessageTypes.Positive)

                'Ready any lingering fake players
                For Each player In (From p In game.players Where p.is_fake _
                                                           AndAlso game.IsPlayerVisible(p) _
                                                           AndAlso game.FindPlayerSlot(p).contents.Moveable)
                    ready_players.Add(player)
                    unready_players.Remove(player)
                    visible_ready_players.Add(player)
                    visible_unready_players.Remove(player)
                    player.load_screen.ready = True
                    game.BroadcastPacket(W3Packet.MakePacket_OTHER_PLAYER_READY(player), Nothing)
                Next player

                If game.parent.settings.load_in_game Then
                    tick_timer.Start()
                End If
            End Sub
            Public Sub [Stop]()
                tick_timer.Stop()
            End Sub

            Public Sub CatchRemovedPlayer(ByVal p As IW3Player, ByVal slot As W3Slot)
                TryLaunch()
            End Sub

            '''<summary>Starts the in-game play if all players are ready</summary>
            Private Function TryLaunch() As Outcome
                If (From x In (From q In game.players Select q.load_screen) Where Not x.ready AndAlso Not x.player.is_fake).Any Then
                    Return failure("Not all players are ready.")
                End If
                Return Launch()
            End Function

            '''<summary>Starts the in-game play</summary>
            Private Function Launch() As Outcome
                If game.parent.settings.load_in_game Then
                    Dim max_ticks = game.players.Max(Function(player) tick_counts(player))
                    For Each player In game.players
                        For i = tick_counts(player) To max_ticks - 1
                            player.f_SendPacket(W3Packet.MakePacket_TICK(0))
                        Next i
                    Next player
                End If

                game.change_state(W3GameStates.Playing)
                game.logger.log("Launching", LogMessageTypes.Positive)

                'start gameplay
                Me.Stop()
                Me.game.gameplay.Start()
                Return success(My.Resources.Instance_Launched_f0name.frmt(game.name))
            End Function

            Private Sub receivePacket_READY(ByVal sending_player As IW3Player, ByVal vals As Dictionary(Of String, Object))
                Dim visible_readied_player As IW3Player = Nothing
                If game.IsPlayerVisible(sending_player) Then
                    visible_readied_player = sending_player
                Else
                    sending_player.f_SendPacket(W3Packet.MakePacket_OTHER_PLAYER_READY(sending_player))
                    ready_players.Add(sending_player)
                    unready_players.Remove(sending_player)

                    Dim slot = game.FindPlayerSlot(sending_player)
                    If (From x In slot.contents.EnumPlayers Where Not (x.ready_to_play Or x.is_fake)).None Then
                        visible_readied_player = slot.contents.EnumPlayers.First
                    End If
                End If

                If visible_readied_player IsNot Nothing Then
                    If Not game.parent.settings.load_in_game Then
                        game.BroadcastPacket(W3Packet.MakePacket_OTHER_PLAYER_READY(visible_readied_player), Nothing)
                    End If
                    ready_players.Add(visible_readied_player)
                    unready_players.Remove(visible_readied_player)
                    visible_ready_players.Add(visible_readied_player)
                    visible_unready_players.Remove(visible_readied_player)
                End If

                If game.parent.settings.load_in_game Then
                    'Inform readied player
                    For Each player In visible_ready_players.Concat(visible_unready_players)
                        sending_player.f_SendPacket(W3Packet.MakePacket_OTHER_PLAYER_READY(player))
                    Next player
                    If visible_unready_players.Any Then
                        game.SendMessageTo("PLAYERS STILL LOADING", sending_player)
                        sending_player.f_SendPacket(W3Packet.MakePacket_START_LAG(visible_unready_players))
                    End If

                    'Inform other players
                    For Each player In ready_players
                        If player IsNot sending_player Then
                            game.SendMessageTo(sending_player.name + " is ready.", player)
                        End If
                    Next player
                    If visible_readied_player IsNot Nothing Then
                        For Each player In ready_players
                            If player IsNot sending_player Then
                                player.f_SendPacket(W3Packet.MakePacket_END_LAG(visible_readied_player, 0))
                            End If
                        Next player
                    End If
                End If

                TryLaunch()
            End Sub

            Private Sub c_fake_tick() Handles tick_timer.Elapsed
                game.ref.QueueAction(AddressOf _c_fake_tick)
            End Sub
            Private Sub _c_fake_tick()
                If game.state > W3GameStates.Loading Then Return

                For Each receiver In ready_players
                    If Not receiver.is_fake Then
                        tick_counts(receiver) += 1
                        For Each player In visible_unready_players
                            receiver.f_SendPacket(W3Packet.MakePacket_END_LAG(player, 0))
                        Next player
                        receiver.f_SendPacket(W3Packet.MakePacket_TICK(0))
                        receiver.f_SendPacket(W3Packet.MakePacket_START_LAG(visible_unready_players))
                    End If
                Next receiver
            End Sub

            Private Function _receivePacket_READY(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture Implements IW3GameLoadScreen.f_ReceivePacket_READY
                Return game.ref.QueueAction(Sub() receivePacket_READY(player, vals))
            End Function
        End Class
    End Class
End Namespace