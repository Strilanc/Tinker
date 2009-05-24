Namespace Warcraft3
    Partial Public Class W3Game
        Private Class W3GameSoul_Lobby
            Inherits W3GamePart
            Implements IW3GameLobby

            Private Event player_entered(ByVal sender As IW3GameLobby, ByVal player As IW3PlayerLobby) Implements IW3GameLobby.PlayerEntered
            Private WithEvents download_scheduler As TransferScheduler(Of Byte)
            Private Const MIN_SLOT_LAYOUT_UPDATE_TIME As Integer = 750
            Private WithEvents slot_layout_timer As New Timers.Timer(MIN_SLOT_LAYOUT_UPDATE_TIME \ 2)
            Private Const DOWNLOAD_UPDATE_PERIOD As UShort = 5000
            Private WithEvents download_timer As New Timers.Timer(DOWNLOAD_UPDATE_PERIOD)
            Private slot_layout_timer_running As Boolean = False
            Private last_slot_layout As Date = DateTime.Now()
            Private ReadOnly free_indexes As New List(Of Byte)
            Private Const SELF_DOWNLOAD_ID As Byte = 255

#Region "Life"
            Public Sub New(ByVal parent As W3Game, ByVal arguments As IEnumerable(Of String))
                MyBase.New(parent)
                InitCreateSlots()
                InitProcessArguments(arguments)
                InitDownloads()
                TryRestoreFakeHost()
                Start()
            End Sub
            Private Sub InitCreateSlots()
                'create player slots
                For i = 0 To game.map.slots.Count - 1
                    With game.map.slots(i)
                        Dim slot As New W3Slot(game, CByte(i))
                        slot.contents = .contents.Clone(slot)
                        slot.color = .color
                        slot.race = .race
                        slot.team = .team
                        slot.locked = game.parent.settings.defaultSlotLockState
                        game.slots.Add(slot)
                        free_indexes.Add(CByte(i + 1))
                    End With
                Next i

                'create observer slots
                If game.parent.settings.map_settings.observers = W3Map.OBS.FULL_OBSERVERS OrElse game.parent.settings.map_settings.observers = W3Map.OBS.REFEREES Then
                    For i = game.map.numPlayerSlots To 12 - 1
                        Dim slot As W3Slot = New W3Slot(game, CByte(i))
                        slot.color = CType(W3Slot.OBS_TEAM, W3Slot.PlayerColor)
                        slot.team = W3Slot.OBS_TEAM
                        slot.race = W3Slot.RaceFlags.Random
                        game.slots.Add(slot)
                        free_indexes.Add(CByte(i + 1))
                    Next i
                End If
            End Sub
            Private Sub InitProcessArguments(ByVal arguments As IEnumerable(Of String))
                If arguments Is Nothing Then Return

                For Each arg In arguments
                    Dim arg2 = ""
                    If arg Like "-*=*" AndAlso Not arg Like "-*=*=*" Then
                        Dim n = arg.IndexOf("="c)
                        arg2 = arg.Substring(n + 1)
                        arg = arg.Substring(0, n + 1)
                    End If

                    Select Case arg.ToLower()
                        Case "-multiobs", "-mo"
                            If game.map.numPlayerSlots <= 10 Then
                                For i = game.map.numPlayerSlots To 10 - 1
                                    game.slots(i).contents = New W3SlotContentsClosed(game.slots(i))
                                Next i
                                Dim player_index = free_indexes(0)
                                free_indexes.Remove(player_index)
                                TryAddFakePlayer("# multi obs", game.slots(10))
                                SetupCoveredSlot(game.slots(10), game.slots(11), player_index)
                            End If
                        Case "-teams=", "-t="
                            Dim out = XvX(arg2)
                            If out.outcome = Outcomes.succeeded Then
                                TrySetTeamSizes(out.val)
                            End If
                        Case "-reserve=", "-r="
                            Dim slot = (From s In game.slots _
                                        Where s.contents.WantPlayer(Nothing) >= W3SlotContents.WantPlayerPriority.Accept_low _
                                        ).FirstOrDefault
                            If slot IsNot Nothing Then
                                ReserveSlot(slot.MatchableId, arg2)
                            End If
                    End Select
                Next arg
            End Sub
            Private Sub InitDownloads()
                Me.download_scheduler = New TransferScheduler(Of Byte)(10 * 1024 / 1000) 'bytes/ms
                If game.parent.settings.allowUpload And game.map.file_available Then
                    Me.download_scheduler.add(SELF_DOWNLOAD_ID, True)
                End If

                If game.parent.settings.grab_map Then
                    Dim server_port = game.parent.settings.default_listen_ports.FirstOrDefault
                    If server_port = 0 Then
                        Throw New InvalidOperationException("Server has no port for Grab player to connect on.")
                    End If

                    Dim grab_port = game.parent.parent.port_pool.TryTakePortFromPool()
                    If grab_port.outcome = Outcomes.failed Then
                        Throw New InvalidOperationException("Failed to get port from pool for Grab player to listen on.")
                    End If

                    Dim p = New W3DummyPlayer("Grab", grab_port.val, Me.game.logger)
                    p.f_Connect("localhost", server_port)
                End If

                'Dim q = New W3DummyPlayer("Wait 1min", game.parent.parent.port_pool.TryTakePortFromPool().val, Me.game.logger, W3DummyPlayer.Modes.EnterGame)
                'q.ready_delay = New TimeSpan(0, 1, 0)
                'q.f_Connect("localhost", game.parent.settings.default_listen_ports.FirstOrDefault)
                'q = New W3DummyPlayer("Wait 2min", game.parent.parent.port_pool.TryTakePortFromPool().val, Me.game.logger, W3DummyPlayer.Modes.EnterGame)
                'q.ready_delay = New TimeSpan(0, 2, 0)
                'q.f_Connect("localhost", game.parent.settings.default_listen_ports.FirstOrDefault)
                'q = New W3DummyPlayer("Wait 3min", game.parent.parent.port_pool.TryTakePortFromPool().val, Me.game.logger, W3DummyPlayer.Modes.EnterGame)
                'q.ready_delay = New TimeSpan(0, 3, 0)
                'q.f_Connect("localhost", game.parent.settings.default_listen_ports.FirstOrDefault)
            End Sub

            Public Sub Start()
                slot_layout_timer.Start()
                download_timer.Start()
            End Sub
            Public Sub [Stop]()
                slot_layout_timer.Stop()
                download_timer.Stop()
            End Sub
#End Region

#Region "Advancing State"
            '''<summary>Autostarts the countdown if the game stays full for awhile.</summary>
            Private Function TryBeginAutoStart() As Outcome
                'Sanity check
                If Not game.parent.settings.autostarted Then
                    Return failure("Autostart is turned off.")
                ElseIf game.CountFreeSlots() > 0 Then
                    Return failure("Game isn't full yet.")
                ElseIf game.state >= W3GameStates.PreCounting Then
                    Return failure("Game is already autostarting.")
                ElseIf (From player In game.players Where Not player.is_fake And player.soul.get_percent_dl <> 100).Any Then
                    Return failure("Downloads haven't finished yet.")
                End If

                game.change_state(W3GameStates.PreCounting)
                'Give people a few seconds to realize the game is full before continuing
                FutureSub.schedule( _
                        AddressOf r_TryContinueAutoStart1, _
                        futurewait(New TimeSpan(0, 0, 3)))
                Return success("Autostart has begun")
            End Function
            Private Sub r_TryContinueAutoStart1()
                game.ref.enqueue(AddressOf _r_TryContinueAutoStart1)
            End Sub
            Private Sub _r_TryContinueAutoStart1()
                If game.state <> W3GameStates.PreCounting Then Return
                If Not game.parent.settings.autostarted Then
                    game.change_state(W3GameStates.AcceptingPlayers)
                    Return
                End If

                'Cancel if game is no longer full
                If game.CountFreeSlots() > 0 Then
                    game.change_state(W3GameStates.AcceptingPlayers)
                    Return
                End If

                'Inform players autostart has begun
                game.logger.log("Preparing to launch", LogMessageTypes.PositiveEvent)
                game.BroadcastMessage("Game is Full. Waiting 5 seconds for stability.")

                'Give jittery players a few seconds to leave
                FutureSub.schedule( _
                        AddressOf r_TryContinueAutoStart2, _
                        futurewait(New TimeSpan(0, 0, 5)))
            End Sub
            Private Sub r_TryContinueAutoStart2()
                game.ref.enqueue(AddressOf _r_TryContinueAutoStart2)
            End Sub
            Private Sub _r_TryContinueAutoStart2()
                If game.state <> W3GameStates.PreCounting Then Return
                If Not game.parent.settings.autostarted Then
                    game.change_state(W3GameStates.AcceptingPlayers)
                    Return
                End If

                'Cancel if game is no longer full
                If game.CountFreeSlots() > 0 Then
                    game.change_state(W3GameStates.AcceptingPlayers)
                    Return
                End If

                'Start the countdown
                game.logger.log("Starting Countdown", LogMessageTypes.PositiveEvent)
                TryStartCountdown()
            End Sub

            '''<summary>Starts the countdown to launch.</summary>
            Private Function TryStartCountdown() As Outcome
                If game.state = W3GameStates.CountingDown Then
                    Return failure("Countdown is already started.")
                ElseIf game.state > W3GameStates.CountingDown Then
                    Return failure("Countdown has already run.")
                ElseIf (From p In game.players Where Not p.is_fake AndAlso p.lobby.get_percent_dl <> 100).Any Then
                    Return failure("Downloads haven't finished yet.")
                End If

                game.change_state(W3GameStates.CountingDown)
                game.flag_player_left = False

                For Each player In game.players
                    player.lobby.f_StartCountdown()
                Next player

                FutureSub.schedule( _
                            Function() eval(AddressOf r_ContinueCountdown, 5), _
                            futurewait(New TimeSpan(0, 0, 1)))
                Return success("Countdown started.")
            End Function
            Private Sub r_ContinueCountdown(ByVal ticks_left As Integer)
                game.ref.enqueue(Function() eval(AddressOf _r_ContinueCountdown, ticks_left))
            End Sub
            Private Sub _r_ContinueCountdown(ByVal ticks_left As Integer)
                If game.state <> W3GameStates.CountingDown Then
                    Return
                End If

                'Abort if a player left
                If game.flag_player_left Then
                    game.logger.log("Countdown Aborted", LogMessageTypes.NegativeEvent)
                    TryRestoreFakeHost()
                    game.BroadcastMessage("===============================================")
                    game.BroadcastMessage("A player left. Launch is held.")
                    game.BroadcastMessage("Waiting for more players...")
                    game.BroadcastMessage("Use {0}leave if you need to leave.".frmt(My.Settings.commandPrefix))
                    game.BroadcastMessage("===============================================")
                    game.change_state(W3GameStates.AcceptingPlayers)
                    game.flag_player_left = False
                    ChangedSlotState()
                    Return
                End If

                If ticks_left > 0 Then
                    'Next tick
                    game.logger.log("Game starting in " + ticks_left.ToString(), LogMessageTypes.PositiveEvent)
                    For Each player In (From p In game.players Where p.lobby.overcounted)
                        game.SendMessageTo("Game starting in {0}...".frmt(ticks_left), player)
                    Next player

                    FutureSub.schedule( _
                                Function() eval(AddressOf r_ContinueCountdown, ticks_left - 1), _
                                futurewait(New TimeSpan(0, 0, 1)))
                    Return
                End If

                StartLoading()
            End Sub

            '''<summary>Launches the game, sending players to the loading screen.</summary>
            Private Sub StartLoading()
                If game.state >= W3GameStates.Loading Then Return
                game.change_state(W3GameStates.Loading)

                'Remove fake players
                For Each player In (From p In game.players.ToList Where p.is_fake)
                    Dim slot = game.FindPlayerSlot(player)
                    If slot Is Nothing OrElse slot.contents.Moveable Then
                        game.RemovePlayer(player, True, W3PlayerLeaveTypes.disc)
                    End If
                Next player

                Me.Stop()
                game.load_screen.Start()
            End Sub
#End Region

            Private Function player_vote_to_start_L(ByVal name As String, ByVal val As Boolean) As Outcome
                If Not game.parent.settings.autostarted Then Return failure("Game is not set to autostart.")
                Dim p = game.FindPlayer(name)
                If p Is Nothing Then Return failure("No player found with the name '{0}'.".frmt(name))
                p.voted_to_start = val
                If Not val Then Return success("Removed vote to start.")

                Dim num_players = (From q In game.players Where Not q.is_fake).Count()
                Dim num_in_favor = (From q In game.players Where Not q.is_fake AndAlso q.voted_to_start).Count()
                If num_players >= 2 And num_in_favor * 3 >= num_players * 2 Then
                    TryStartCountdown()
                End If
                Return success("Voted to start.")
            End Function

#Region "Players"
            Private Function TryAddFakePlayer(ByVal name As String, ByVal new_slot As W3Slot) As Outcome(Of IW3Player)
                If game.state > W3GameStates.AcceptingPlayers Then
                    Return failure("No longer accepting players.")
                ElseIf free_indexes.Count <= 0 And game.fake_host_player Is Nothing Then
                    Return failure("No space available for fake player.")
                End If

                'Assign index
                If free_indexes.Count = 0 Then game.RemovePlayer(game.fake_host_player, True, W3PlayerLeaveTypes.disc)
                Dim index = free_indexes(0)
                free_indexes.Remove(index)

                'Make player
                Dim new_player As IW3Player = New W3Player(game.ref, index, Me.game, name, game.logger)
                If new_slot IsNot Nothing Then
                    new_slot.contents = New W3SlotContentsPlayer(new_slot, new_player)
                End If
                game.players.Add(new_player)

                'Inform other players
                For Each player In game.players
                    player.f_SendPacket(W3Packet.MakePacket_OTHER_PLAYER_JOINED(new_player, index))
                Next player

                'Inform bot
                e_ThrowPlayerEntered(new_player.lobby)
                game.logger.log(new_player.name + " has been placed in the game.", LogMessageTypes.PositiveEvent)

                'Update state
                ChangedSlotState()
                Return successVal(new_player, "Added fake player '{0}'.".frmt(new_player.name))
            End Function
            Private Function TryRestoreFakeHost() As Outcome
                If game.fake_host_player IsNot Nothing Then
                    Return failure("Fake host already exists.")
                ElseIf game.state >= W3GameStates.CountingDown Then
                    Return failure("Fake host can't join after countdown has started.")
                End If

                Dim out = TryAddFakePlayer(My.Settings.ingame_name, Nothing)
                If out.outcome = Outcomes.succeeded Then
                    game.fake_host_player = out.val
                End If
                Return out
            End Function

            Private Function TryAddPlayer(ByVal connecting_player As W3ConnectingPlayer) As Outcome
                If game.state > W3GameStates.AcceptingPlayers Then
                    Return failure("No longer accepting players.")
                ElseIf Not connecting_player.socket.connected Then
                    Return failure("Player isn't connected.")
                End If

                'Assign slot
                Dim best_slot As W3Slot = Nothing
                Dim best_match = W3SlotContents.WantPlayerPriority.Reject
                game.slots.MaxPair(Function(slot) slot.contents.WantPlayer(connecting_player.name), _
                                   best_slot, _
                                   best_match)
                If best_match <= W3SlotContents.WantPlayerPriority.Reluctant Then
                    Return failure("No slot available for player.")
                End If

                'Assign index
                Dim index As Byte = 0
                If best_slot.contents.PlayerIndex <> 0 And best_match <> W3SlotContents.WantPlayerPriority.Accept_reservation Then
                    'the slot requires the player to take a specific index
                    index = best_slot.contents.PlayerIndex
                ElseIf free_indexes.Count > 0 Then
                    'there is a player index available
                    index = free_indexes(0)
                ElseIf game.fake_host_player IsNot Nothing Then
                    'the only player index left belongs to the fake host
                    index = game.fake_host_player.index
                    game.RemovePlayer(game.fake_host_player, True, W3PlayerLeaveTypes.disc)
                Else
                    'no indexes left, go away
                    Return failure("No index space available for player.")
                End If
                free_indexes.Remove(index)

                'Reservation
                If best_match = W3SlotContents.WantPlayerPriority.Accept_reservation Then
                    For Each player In best_slot.contents.EnumPlayers
                        game.RemovePlayer(player, True, W3PlayerLeaveTypes.disc)
                    Next player
                End If

                'Create player object
                Dim new_player As IW3Player = New W3Player(game.ref, index, Me.game, connecting_player, game.logger)
                best_slot.contents = best_slot.contents.TakePlayer(new_player)
                game.players.Add(new_player)

                'Greet new player
                new_player.f_SendPacket(W3Packet.MakePacket_GREET(new_player, index, game.map, game.slots))
                For Each player In (From p In game.players Where p IsNot new_player AndAlso game.IsPlayerVisible(p))
                    new_player.f_SendPacket(W3Packet.MakePacket_OTHER_PLAYER_JOINED(player))
                Next player
                new_player.f_SendPacket(W3Packet.MakePacket_MAP_INFO(game.map))
                If My.Resources.ProgramShowoff_f0name <> "" Then
                    game.SendMessageTo(My.Resources.ProgramShowoff_f0name.frmt(My.Resources.ProgramName), new_player)
                End If

                'Inform other players
                If game.IsPlayerVisible(new_player) Then
                    For Each player In (From p In game.players Where p IsNot new_player)
                        player.f_SendPacket(W3Packet.MakePacket_OTHER_PLAYER_JOINED(new_player, index))
                    Next player
                End If

                'Inform bot
                e_ThrowPlayerEntered(new_player.lobby)
                game.logger.log("{0} has entered the game.".frmt(new_player.name), LogMessageTypes.PositiveEvent)

                'Update state
                ChangedSlotState()
                TryBeginAutoStart()
                If game.parent.settings.auto_elevate_username IsNot Nothing Then
                    If new_player.name.ToLower() = game.parent.settings.auto_elevate_username.ToLower() Then
                        game.TryElevatePlayer(new_player.name)
                    End If
                End If
                If game.parent.settings.autostarted Then
                    game.SendMessageTo("This is an automated game. {0}help for a list of commands.".frmt(My.Settings.commandPrefix), new_player)
                End If

                Return success("Added player '{0}' to instance '{1}'.".frmt(new_player.name, game.name))
            End Function
#End Region

#Region "Events"
            Private Sub e_ThrowPlayerEntered(ByVal new_player As IW3PlayerLobby)
                game.eventRef.enqueue(Function() eval(AddressOf _e_ThrowPlayerEntered, new_player))
            End Sub
            Private Sub _e_ThrowPlayerEntered(ByVal new_player As IW3PlayerLobby)
                RaiseEvent player_entered(Me, new_player)
            End Sub

            Private Sub c_DownloadSchedulerActions(ByVal started As List(Of TransferScheduler(Of Byte).TransferEndPoint), _
                                                   ByVal stopped As List(Of TransferScheduler(Of Byte).TransferEndPoint)) Handles download_scheduler.actions
                game.ref.enqueue(Function() eval(AddressOf _c_DownloadSchedulerActions, started, stopped))
            End Sub
            Private Sub _c_DownloadSchedulerActions(ByVal started As List(Of TransferScheduler(Of Byte).TransferEndPoint), _
                                                    ByVal stopped As List(Of TransferScheduler(Of Byte).TransferEndPoint))
                'Start transfers
                For Each e In started
                    'Find matching players
                    Dim src = game.FindPlayer(e.src)
                    Dim dst = game.FindPlayer(e.dst)
                    If dst Is Nothing Then Continue For

                    'Apply
                    If e.src = SELF_DOWNLOAD_ID Then
                        game.logger.log("Initiating map upload to {0}.".frmt(dst.name), LogMessageTypes.PositiveEvent)
                        dst.lobby.getting_map_from_bot = True
                        dst.lobby.f_BufferMap()
                    ElseIf src IsNot Nothing Then
                        game.logger.log("Initiating p2p map transfer from {0} to {1}.".frmt(src.name, dst.name), LogMessageTypes.PositiveEvent)
                        src.f_SendPacket(W3Packet.MakePacket_UL_START(e.dst, CUInt(Math.Max(0, dst.lobby.downloaded_map_size_P))))
                        dst.f_SendPacket(W3Packet.MakePacket_DL_START(e.src))
                    End If
                Next e

                'Stop transfers
                For Each e In stopped
                    'Find matching players
                    Dim src = game.FindPlayer(e.src)
                    Dim dst = game.FindPlayer(e.dst)
                    If dst Is Nothing Then Continue For

                    'Apply
                    If e.src = SELF_DOWNLOAD_ID Then
                        game.logger.log("Stopping map upload to {0}.".frmt(dst.name), LogMessageTypes.PositiveEvent)
                        dst.lobby.getting_map_from_bot = False
                    ElseIf src IsNot Nothing Then
                        game.logger.log("Stopping p2p map transfer from {0} to {1}.".frmt(src.name, dst.name), LogMessageTypes.PositiveEvent)
                        src.f_SendPacket(W3Packet.MakePacket_OTHER_PLAYER_LEFT(dst, W3PlayerLeaveTypes.disc))
                        src.f_SendPacket(W3Packet.MakePacket_OTHER_PLAYER_JOINED(dst))
                        dst.f_SendPacket(W3Packet.MakePacket_OTHER_PLAYER_LEFT(src, W3PlayerLeaveTypes.disc))
                        dst.f_SendPacket(W3Packet.MakePacket_OTHER_PLAYER_JOINED(src))
                    End If
                Next e
            End Sub
#End Region

#Region "Over"
            Private Sub c_ScheduleDownloads() Handles download_timer.Elapsed
                download_scheduler.update()
            End Sub

            Public Sub CatchRemovedPlayer(ByVal p As IW3Player, ByVal slot As W3Slot)
                If slot Is Nothing OrElse slot.contents.PlayerIndex <> p.index Then
                    free_indexes.Add(p.index)
                End If
                download_scheduler.remove(p.index)
                TryRestoreFakeHost()
                ChangedSlotState()
            End Sub
#End Region

#Region "Slots"
            Private Sub c_ChangedSlotState() Handles slot_layout_timer.Elapsed
                game.ref.enqueue(AddressOf ChangedSlotState)
            End Sub
            '''<summary>Broadcasts new game state to players, and throws the updated event.</summary>
            Private Sub ChangedSlotState()
                game.e_ThrowUpdated()

                'Don't let update rate to clients become too high
                If (DateTime.Now() - last_slot_layout).TotalMilliseconds < MIN_SLOT_LAYOUT_UPDATE_TIME Then
                    If Not slot_layout_timer_running Then slot_layout_timer.Start()
                    slot_layout_timer_running = True
                    Return
                End If

                'Send layout to clients
                Dim time = uCUInt(Environment.TickCount())
                For Each player In game.players
                    player.f_SendPacket(W3Packet.MakePacket_SLOT_LAYOUT(player, game.map, game.slots, time, game.parent.settings.is_admin_game))
                Next player

                'Finish
                last_slot_layout = DateTime.Now()
                If slot_layout_timer_running Then
                    slot_layout_timer.Stop()
                    slot_layout_timer_running = False
                End If
                TryBeginAutoStart()
            End Sub

            ''' <summary>Opens slots, closes slots and moves players around to try to match the desired team sizes.</summary>
            Private Function TrySetTeamSizes(ByVal desired_team_sizes As IList(Of Integer)) As Outcome
                If game.state > W3GameStates.AcceptingPlayers Then
                    Return failure("Can't change team sizes after launch.")
                End If

                Dim available_wellplaced_slots = New List(Of W3Slot)
                Dim misplaced_player_slots = New List(Of W3Slot)
                Dim team_sizes_left = desired_team_sizes.ToArray()
                For Each slot In game.slots
                    If slot.team >= team_sizes_left.Count Then Continue For

                    Select Case slot.contents.Type
                        Case W3SlotContents.ContentType.Computer
                            'computers slots shouldn't be affected

                        Case W3SlotContents.ContentType.Empty
                            If team_sizes_left(slot.team) > 0 Then
                                team_sizes_left(slot.team) -= 1
                                available_wellplaced_slots.Add(slot)
                                slot.contents = New W3SlotContentsOpen(slot)
                            Else
                                slot.contents = New W3SlotContentsClosed(slot)
                            End If

                        Case W3SlotContents.ContentType.Player
                            If team_sizes_left(slot.team) > 0 Then
                                team_sizes_left(slot.team) -= 1
                            Else
                                misplaced_player_slots.Add(slot)
                            End If

                        Case Else
                            Throw New UnreachableStateException
                    End Select
                Next slot

                'Swap misplaced players to wellplaced slots
                For i = 0 To Math.Min(available_wellplaced_slots.Count, misplaced_player_slots.Count) - 1
                    SwapSlotContents(available_wellplaced_slots(i), misplaced_player_slots(i))
                Next i

                ChangedSlotState()
                Return success("Set team sizes.")
            End Function

            Private Function ModifySlotContents(ByVal slot_query As String, _
                                                  ByVal action As Action(Of W3Slot), _
                                                  ByVal success_description As String, _
                                                  Optional ByVal avoid_players As Boolean = False) As Outcome
                If game.state >= W3GameStates.CountingDown Then
                    Return failure("Can't modify slots during launch.")
                End If

                Dim found_slot = game.FindMatchingSlot(slot_query)
                If found_slot.outcome <> Outcomes.succeeded Then
                    Return found_slot
                End If

                Dim slot = found_slot.val
                If avoid_players AndAlso slot.contents.Type = W3SlotContents.ContentType.Player Then
                    Return failure("Slot '{0}' contains a player.".frmt(slot_query))
                End If

                Call action(slot)
                ChangedSlotState()
                Return success(success_description)
            End Function
#End Region
#Region "Slot Contents"
            '''<summary>Opens the slot with the given index, unless the slot contains a player.</summary>
            Private Function OpenSlot(ByVal slotid As String) As Outcome
                Return ModifySlotContents(slotid, _
                                            Function(slot) Assign(slot.contents, New W3SlotContentsOpen(slot)), _
                                            "Slot '{0}' opened.".frmt(slotid), _
                                            avoid_players:=True)
            End Function

            '''<summary>Places a computer with the given difficulty in the slot with the given index, unless the slot contains a player.</summary>
            Private Function ComputerizeSlot(ByVal slotid As String, ByVal cpu As W3Slot.ComputerLevel) As outcome
                Return ModifySlotContents(slotid, _
                                            Function(slot) Assign(slot.contents, New W3SlotContentsComputer(slot, cpu)), _
                                            "Slot '{0}' computerized.".frmt(slotid), _
                                            avoid_players:=True)
            End Function

            '''<summary>Closes the slot with the given index, unless the slot contains a player.</summary>
            Private Function CloseSlot(ByVal slotid As String) As Outcome
                Return ModifySlotContents(slotid, _
                                            Function(slot) Assign(slot.contents, New W3SlotContentsClosed(slot)), _
                                            "Slot '{0}' closed.".frmt(slotid), _
                                            avoid_players:=True)
            End Function

            '''<summary>Reserves a slot for a player.</summary>
            Private Function ReserveSlot(ByVal slotid As String, ByVal username As String) As Outcome
                If game.state >= W3GameStates.CountingDown Then
                    Return failure("Can't reserve slots after launch.")
                End If
                Dim slot_out = game.FindMatchingSlot(slotid)
                If slot_out.outcome <> Outcomes.succeeded Then Return slot_out
                Dim slot = slot_out.val
                If slot.contents.Type = W3SlotContents.ContentType.Player Then
                    Return failure("Slot '{0}' can't be reserved because it already contains a player.".frmt(slotid))
                Else
                    Return TryAddFakePlayer(username, slot)
                End If
            End Function

            Private Function SwapSlotContents(ByVal query1 As String, ByVal query2 As String) As Outcome
                If game.state > W3GameStates.AcceptingPlayers Then
                    Return failure("Can't swap slots after launch.")
                End If
                Dim slot_out1 = game.FindMatchingSlot(query1)
                Dim slot_out2 = game.FindMatchingSlot(query2)
                If slot_out1.outcome <> Outcomes.succeeded Then Return slot_out1
                If slot_out2.outcome <> Outcomes.succeeded Then Return slot_out2
                Dim slot1 = slot_out1.val
                Dim slot2 = slot_out2.val
                If slot1 Is Nothing Then
                    Return failure("No slot matching '{0}'.".frmt(query1))
                ElseIf slot2 Is Nothing Then
                    Return failure("No slot matching '{0}'.".frmt(query2))
                ElseIf slot1 Is slot2 Then
                    Return failure("Slot {0} is slot '{1}'.".frmt(query1, query2))
                End If
                SwapSlotContents(slot1, slot2)
                ChangedSlotState()
                Return success("Slot '{0}' swapped with slot '{1}'.".frmt(query1, query2))
            End Function
            Private Sub SwapSlotContents(ByVal slot1 As W3Slot, ByVal slot2 As W3Slot)
                Dim t = slot1.contents.Clone(slot2)
                slot1.contents = slot2.contents.Clone(slot1)
                slot2.contents = t
                ChangedSlotState()
            End Sub
#End Region
#Region "Slot States"
            Private Function SetSlotColor(ByVal slotid As String, ByVal color As W3Slot.PlayerColor) As Outcome
                If game.state > W3GameStates.CountingDown Then
                    Return failure("Can't change slot settings after launch.")
                End If

                Dim slot_out = game.FindMatchingSlot(slotid)
                If slot_out.outcome <> Outcomes.succeeded Then Return slot_out

                Dim swap_color_slot = (From x In game.slots Where x.color = color).FirstOrDefault
                If swap_color_slot IsNot Nothing Then swap_color_slot.color = slot_out.val.color
                slot_out.val.color = color

                ChangedSlotState()
                Return success("Slot '{0}' color set to {1}.".frmt(slotid, color))
            End Function

            Private Function SetSlotRace(ByVal slotid As String, ByVal race As W3Slot.RaceFlags) As Outcome
                Return ModifySlotContents(slotid, _
                                            Function(slot) Assign(slot.race, race), _
                                            "Slot '{0}' race set to {1}.".frmt(slotid, race))
            End Function

            Private Function SetSlotTeam(ByVal slotid As String, ByVal team As Byte) As Outcome
                Return ModifySlotContents(slotid, _
                                            Function(slot) Assign(slot.team, team), _
                                            "Slot '{0}' team set to {1}.".frmt(slotid, team))
            End Function

            Private Function SetSlotHandicap(ByVal slotid As String, ByVal handicap As Byte) As Outcome
                Return ModifySlotContents(slotid, _
                                            Function(slot) Assign(slot.handicap, handicap), _
                                            "Slot '{0}' handicap set to {1}.".frmt(slotid, handicap))
            End Function

            Private Function SetSlotLocked(ByVal slotid As String, ByVal locked As W3Slot.Lock) As Outcome
                Return ModifySlotContents(slotid, _
                                            Function(slot) Assign(slot.locked, locked), _
                                            "Slot '{0}' lock state set to {1}.".frmt(slotid, locked))
            End Function

            Private Function SetAllSlotsLocked(ByVal locked As W3Slot.Lock) As Outcome
                If game.state > W3GameStates.AcceptingPlayers Then
                    Return failure("Can't lock slots after launch.")
                End If
                For Each slot In game.slots.ToList
                    slot.locked = locked
                Next slot
                Return success("All slots' lock state set to {0}.".frmt(locked))
            End Function
#End Region

#Region "Networking"
            Public Sub ReceiveSetColor(ByVal player As IW3Player, ByVal new_color As W3Slot.PlayerColor)
                Dim slot = game.FindPlayerSlot(player)

                'Validate
                If slot Is Nothing Then Return
                If slot.locked = W3Slot.Lock.frozen Then Return '[no changes allowed]
                If Not slot.contents.Moveable Then Return '[slot is weird]
                If game.state >= W3GameStates.Loading Then Return '[too late]
                If Not IsEnumValid(Of W3Slot.PlayerColor)(new_color) Then Return '[not a valid color]

                'check for duplicates
                For Each other_slot In game.slots.ToList()
                    If other_slot.color = new_color Then
                        If Not game.map.isMelee Then Return
                        If Not other_slot.contents.Type = W3SlotContents.ContentType.Empty Then Return
                        other_slot.color = slot.color
                        Exit For
                    End If
                Next other_slot

                'change color
                slot.color = new_color
                ChangedSlotState()
            End Sub
            Public Sub ReceiveSetRace(ByVal player As IW3Player, ByVal new_race As W3Slot.RaceFlags)
                Dim slot = game.FindPlayerSlot(player)

                'Validate
                If slot Is Nothing Then Return
                If slot.locked = W3Slot.Lock.frozen Then Return '[no changes allowed]
                If Not slot.contents.Moveable Then Return '[slot is weird]
                If game.state >= W3GameStates.Loading Then Return '[too late]
                If Not IsEnumValid(Of W3Slot.RaceFlags)(new_race) OrElse new_race = W3Slot.RaceFlags.Unlocked Then Return '[not a valid race]

                'Perform
                slot.race = new_race
                ChangedSlotState()
            End Sub
            Public Sub ReceiveSetHandicap(ByVal player As IW3Player, ByVal new_handicap As Byte)
                Dim slot = game.FindPlayerSlot(player)

                'Validate
                If slot Is Nothing Then Return
                If slot.locked = W3Slot.Lock.frozen Then Return '[no changes allowed]
                If Not slot.contents.Moveable Then Return '[slot is weird]
                If game.state >= W3GameStates.CountingDown Then Return '[too late]

                'Perform
                Select Case new_handicap
                    Case 50, 60, 70, 80, 90, 100
                        slot.handicap = new_handicap
                    Case Else
                        Return '[invalid handicap]
                End Select

                ChangedSlotState()
            End Sub
            Public Sub ReceiveSetTeam(ByVal player As IW3Player, ByVal new_team As Byte)
                Dim slot = game.FindPlayerSlot(player)

                'Validate
                If slot Is Nothing Then Return
                If slot.locked <> W3Slot.Lock.unlocked Then Return '[no teams changes allowed]
                If new_team > W3Slot.OBS_TEAM Then Return '[invalid value]
                If Not slot.contents.Moveable Then Return '[slot is weird]
                If game.state >= W3GameStates.Loading Then Return '[too late]
                If new_team = W3Slot.OBS_TEAM Then
                    Select Case game.parent.settings.map_settings.observers
                        Case W3Map.OBS.FULL_OBSERVERS, W3Map.OBS.REFEREES
                            '[fine; continue]
                        Case Else
                            Return '[obs not enabled; invalid value]
                    End Select
                ElseIf Not game.map.isMelee And new_team >= game.map.numForces Then
                    Return '[invalid team]
                ElseIf game.map.isMelee And new_team >= game.map.numPlayerSlots Then
                    Return '[invalid team]
                End If

                'Perform
                If game.map.isMelee Then
                    'set slot to target team
                    slot.team = new_team
                Else
                    'swap with next open slot from target team
                    For offset_mod = 0 To game.slots.Count - 1
                        Dim next_slot = game.slots((slot.index + offset_mod) Mod game.slots.Count)
                        If next_slot.team = new_team AndAlso next_slot.contents.WantPlayer(player.name) > W3SlotContents.WantPlayerPriority.Accept_low Then
                            SwapSlotContents(next_slot, slot)
                            Exit For
                        End If
                    Next offset_mod
                End If

                ChangedSlotState()
            End Sub
#End Region

#Region "Interface"
            Private ReadOnly Property __download_scheduler() As TransferScheduler(Of Byte) Implements IW3GameLobby.download_scheduler
                Get
                    Return download_scheduler
                End Get
            End Property

            Private Function _f_UpdatedGameState() As IFuture Implements IW3GameLobby.f_UpdatedGameState
                Return game.ref.enqueue(AddressOf ChangedSlotState)
            End Function

            Private Function _f_OpenSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3GameLobby.f_OpenSlot
                Return game.ref.enqueue(Function() OpenSlot(query))
            End Function
            Private Function _f_CloseSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3GameLobby.f_CloseSlot
                Return game.ref.enqueue(Function() CloseSlot(query))
            End Function
            Private Function _f_ReserveSlot(ByVal query As String, ByVal username As String) As IFuture(Of Outcome) Implements IW3GameLobby.f_ReserveSlot
                Return game.ref.enqueue(Function() ReserveSlot(query, username))
            End Function
            Private Function _f_SwapSlotContents(ByVal query1 As String, ByVal query2 As String) As IFuture(Of Outcome) Implements IW3GameLobby.f_SwapSlotContents
                Return game.ref.enqueue(Function() SwapSlotContents(query1, query2))
            End Function

            Private Function _f_SetSlotCpu(ByVal query As String, ByVal c As W3Slot.ComputerLevel) As IFuture(Of Outcome) Implements IW3GameLobby.f_SetSlotCpu
                Return game.ref.enqueue(Function() ComputerizeSlot(query, c))
            End Function
            Private Function _f_SetSlotLocked(ByVal query As String, ByVal new_lock_state As W3Slot.Lock) As IFuture(Of Outcome) Implements IW3GameLobby.f_SetSlotLocked
                Return game.ref.enqueue(Function() SetSlotLocked(query, new_lock_state))
            End Function
            Private Function _f_SetAllSlotsLocked(ByVal new_lock_state As W3Slot.Lock) As IFuture(Of Outcome) Implements IW3GameLobby.f_SetAllSlotsLocked
                Return game.ref.enqueue(Function() SetAllSlotsLocked(new_lock_state))
            End Function
            Private Function _f_SetSlotHandicap(ByVal query As String, ByVal new_handicap As Byte) As IFuture(Of Outcome) Implements IW3GameLobby.f_SetSlotHandicap
                Return game.ref.enqueue(Function() SetSlotHandicap(query, new_handicap))
            End Function
            Private Function _f_SetSlotTeam(ByVal query As String, ByVal new_team As Byte) As IFuture(Of Outcome) Implements IW3GameLobby.f_SetSlotTeam
                Return game.ref.enqueue(Function() SetSlotTeam(query, new_team))
            End Function
            Private Function _f_SetSlotRace(ByVal query As String, ByVal new_race As W3Slot.RaceFlags) As IFuture(Of Outcome) Implements IW3GameLobby.f_SetSlotRace
                Return game.ref.enqueue(Function() SetSlotRace(query, new_race))
            End Function
            Private Function _f_SetSlotColor(ByVal query As String, ByVal new_color As W3Slot.PlayerColor) As IFuture(Of Outcome) Implements IW3GameLobby.f_SetSlotColor
                Return game.ref.enqueue(Function() SetSlotColor(query, new_color))
            End Function

            Private Function _f_TryAddPlayer(ByVal new_player As W3ConnectingPlayer) As IFuture(Of Outcome) Implements IW3GameLobby.f_TryAddPlayer
                Return game.ref.enqueue(Function() TryAddPlayer(new_player))
            End Function
            Private Function _f_PlayerVoteToStart(ByVal name As String, ByVal val As Boolean) As IFuture(Of Outcome) Implements IW3GameLobby.f_PlayerVoteToStart
                Return game.ref.enqueue(Function() player_vote_to_start_L(name, val))
            End Function
            Private Function _f_StartCountdown() As IFuture(Of Outcome) Implements IW3GameLobby.f_StartCountdown
                Return game.ref.enqueue(Function() TryStartCountdown())
            End Function
            Private Function _f_TrySetTeamSizes(ByVal sizes As IList(Of Integer)) As IFuture(Of Outcome) Implements IW3GameLobby.f_TrySetTeamSizes
                Return game.ref.enqueue(Function() TrySetTeamSizes(sizes))
            End Function
#End Region
        End Class
    End Class
End Namespace
