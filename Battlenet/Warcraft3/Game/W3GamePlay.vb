Namespace Warcraft3
    Partial Class W3Game
        Private Class W3GamePlay
            Inherits W3GamePart
            Implements IW3GamePlay
            Private Class GameTickDatum
                Public ReadOnly source As IW3PlayerGameplay
                Public ReadOnly data As Byte()
                Public Sub New(ByVal source As IW3PlayerGameplay, ByVal data As Byte())
                    Me.source = source
                    Me.data = data
                End Sub
            End Class

            Private Event PlayerSentData(ByVal game As IW3GamePlay, ByVal player As IW3PlayerGameplay, ByVal data As Byte()) Implements IW3GamePlay.PlayerSentData
            Private WithEvents tick_timer As New Timers.Timer(My.Settings.game_tick_period)
            Private last_tick_time As Integer
            Private lagging_players As New List(Of IW3Player)
            Private lag_start_time As Integer
            Private game_data_queue As New Queue(Of GameTickDatum)
            Private game_time As Integer = 0
            Private game_time_buffer As Double = 0

            Public Sub New(ByVal body As W3Game)
                MyBase.New(body)
            End Sub
            Public Sub Start()
                For Each player In game.players
                    player.gameplay.f_Start()
                Next player
                Me.last_tick_time = Environment.TickCount
                Me.tick_timer.Start()
            End Sub
            Public Sub [Stop]()
                For Each player In game.players
                    player.gameplay.f_Stop()
                Next player
                tick_timer.Stop()
            End Sub

            Private Sub e_ThrowPlayerSentData(ByVal p As IW3PlayerGameplay, ByVal data As Byte())
                game.eventRef.enqueue(Function() eval(AddressOf _e_ThrowPlayerSentData, p, data))
            End Sub
            Private Sub _e_ThrowPlayerSentData(ByVal p As IW3PlayerGameplay, ByVal data As Byte())
                RaiseEvent PlayerSentData(Me, p, data)
            End Sub

#Region "Play"
            '''<summary>Adds data to broadcast to all clients in the next tick</summary>
            Private Sub QueueGameData(ByVal sender As IW3PlayerGameplay, ByVal data() As Byte)
                e_ThrowPlayerSentData(sender, data)
                game_data_queue.Enqueue(New GameTickDatum(sender, data))
            End Sub

            '''<summary>Drops the players currently lagging.</summary>
            Private Sub DropLagger()
                For Each player In lagging_players
                    game.RemovePlayer(player, True, W3PlayerLeaveTypes.disc)
                Next player
            End Sub

            '''<summary>Advances game time</summary>
            Private Sub c_Tick(ByVal sender As Object, ByVal e As Timers.ElapsedEventArgs) Handles tick_timer.Elapsed
                game.ref.enqueue(AddressOf _c_Tick)
            End Sub
            Private Sub _c_Tick()
                Dim t = Environment.TickCount
                Dim dt = TickCountDelta(t, last_tick_time) * My.Settings.game_speed_factor
                Dim dgt = CUShort(My.Settings.game_tick_period * My.Settings.game_speed_factor)
                last_tick_time = t

                'Stop for laggers
                UpdateLagScreen()
                If lagging_players.Count > 0 Then
                    Return
                End If

                'Throttle tick rate
                game_time_buffer += dt - dgt
                game_time_buffer = game_time_buffer.between(-dgt * 10, dgt * 10)
                tick_timer.Interval = (dgt - game_time_buffer).between(dgt / 2, dgt * 2)

                'Send
                SendQueuedGameData(New TickRecord(dgt, game_time))
                game_time += dgt
            End Sub
            Private Sub UpdateLagScreen()
                If lagging_players.Count > 0 Then
                    For Each p In lagging_players.ToList()
                        If Not game.players.Contains(p) Then
                            lagging_players.Remove(p)
                        ElseIf p.gameplay.tock_time >= game_time OrElse p.is_fake Then
                            lagging_players.Remove(p)
                            Dim p_ = p
                            If game.IsPlayerVisible(p) OrElse (From q In lagging_players _
                                                               Where game.GetVisiblePlayer(q) Is game.GetVisiblePlayer(p_)).None Then
                                game.BroadcastPacket( _
                                        W3Packet.MakePacket_END_LAG( _
                                                game.GetVisiblePlayer(p), _
                                                CUInt(last_tick_time - lag_start_time)))
                            End If
                        End If
                    Next p
                Else
                    lagging_players = (From p In game.players _
                                      Where Not p.is_fake _
                                      AndAlso p.gameplay.tock_time < game_time - My.Settings.game_lag_limit).ToList()
                    If lagging_players.Count > 0 Then
                        game.BroadcastPacket(W3Packet.MakePacket_START_LAG(lagging_players), Nothing)
                        lag_start_time = last_tick_time
                    End If
                End If
            End Sub
            Private Sub SendQueuedGameData(ByVal record As TickRecord)
                Dim dgt = record.length
                For Each player In game.players
                    player.gameplay.f_QueueTick(record)
                Next player

                'Include all the data we can fit in a packet
                Dim data_length = 0
                Dim data_list = New List(Of Byte())(game_data_queue.Count)
                Dim outgoing_data = New List(Of GameTickDatum)(game_data_queue.Count)
                While game_data_queue.Count > 0 _
                      AndAlso data_length + game_data_queue.Peek().data.Length < BnetSocket.BUFFER_SIZE - 20 '[20 includes headers and a small safety margin]
                    Dim e = game_data_queue.Dequeue()
                    outgoing_data.Add(e)

                    'append client data to broadcast game data
                    Dim data = concat(New Byte() {game.GetVisiblePlayer(e.source.player).index}, _
                                                  CUShort(e.data.Length).bytes(), _
                                                  e.data)
                    data_list.Add(data)
                    data_length += data.Length
                End While

                'Send data
                Dim normal_packet = W3Packet.MakePacket_TICK(dgt, concat(data_list))
                For Each receiver In game.players
                    If game.IsPlayerVisible(receiver) Then
                        receiver.f_SendPacket(normal_packet)
                    Else
                        receiver.f_SendPacket(CreatePacketForInvisiblePlayer(receiver, dgt, outgoing_data))
                    End If
                Next receiver
            End Sub
            Private Function CreatePacketForInvisiblePlayer(ByVal receiver As IW3Player, _
                                                            ByVal dgt As UShort, _
                                                            ByVal data As IEnumerable(Of GameTickDatum)) As W3Packet
                Return W3Packet.MakePacket_TICK(dgt, _
                        concat((From e In data _
                                Select concat( _
                                              New Byte() {If(e.source Is receiver, _
                                                             receiver, _
                                                             game.GetVisiblePlayer(e.source.player)).index}, _
                                              CUShort(e.data.Length).bytes(), _
                                              e.data))))
            End Function
#End Region

#Region "Interface"
            Private ReadOnly Property _game_time() As Integer Implements IW3GamePlay.game_time
                Get
                    Return game_time
                End Get
            End Property
            Private Function _f_DropLagger() As IFuture Implements IW3GamePlay.f_DropLagger
                Return game.ref.enqueue(AddressOf DropLagger)
            End Function
            Private Function _f_QueueGameData(ByVal sender As IW3PlayerGameplay, ByVal data() As Byte) As IFuture Implements IW3GamePlay.f_QueueGameData
                Return game.ref.enqueue(Function() eval(AddressOf QueueGameData, sender, data))
            End Function
#End Region
        End Class
    End Class
End Namespace
