Namespace Warcraft3
    Partial Public Class W3Game
        Implements IW3Game

        Public Const SELF_DOWNLOAD_ID As Byte = 255
        Private Const DOWNLOAD_UPDATE_PERIOD As UShort = 3000

        Private downloadScheduler As TransferScheduler(Of Byte)
        Private ReadOnly downloadTimer As New Timers.Timer(DOWNLOAD_UPDATE_PERIOD)
        Private ReadOnly freeIndexes As New List(Of Byte)
        Private ReadOnly slotStateUpdateThrottle As New Throttle(250.MilliSeconds)
        Private ReadOnly updateEventThrottle As New Throttle(100.MilliSeconds)

        Private Event PlayerEntered(ByVal sender As IW3Game, ByVal player As IW3Player) Implements IW3Game.PlayerEntered

#Region "Life"
        Private Sub LobbyNew(ByVal arguments As IEnumerable(Of String))
            Contract.Ensures(downloadScheduler IsNot Nothing)

            Dim rate As Double = 10 * 1024 / 1000
            Dim switchTime As Double = 3000
            Dim size As Double = map.FileSize
            Contract.Assume(Not Double.IsNaN(rate) AndAlso Not Double.IsInfinity(rate))
            Contract.Assume(Not Double.IsNaN(switchTime) AndAlso Not Double.IsInfinity(switchTime))
            Contract.Assume(Not Double.IsNaN(size) AndAlso Not Double.IsInfinity(size))
            Me.downloadScheduler = New TransferScheduler(Of Byte)(typicalRate:=rate,
                                                                  typicalSwitchTime:=3000,
                                                                  filesize:=size)
            AddHandler downloadScheduler.actions, AddressOf CatchDownloadSchedulerActions
            AddHandler downloadTimer.Elapsed, Sub() c_ScheduleDownloads()

            InitCreateSlots()
            InitProcessArguments(arguments)
            InitDownloads()
            downloadTimer.Start()
            ref.QueueAction(Sub() TryRestoreFakeHost())
        End Sub
        Private Sub InitCreateSlots()
            'create player slots
            For i = 0 To map.slots.Count - 1
                With map.slots(i)
                    Dim slot = New W3Slot(Me, CByte(i))
                    slot.contents = .contents.Clone(slot)
                    slot.color = .color
                    slot.race = .race
                    slot.team = .team
                    slot.locked = server.settings.defaultSlotLockState
                    slots.Add(slot)
                    freeIndexes.Add(CByte(i + 1))
                End With
            Next i

            'create observer slots
            If server.settings.header.Map.observers = GameObserverOption.FullObservers OrElse
                                    server.settings.header.Map.observers = GameObserverOption.Referees Then
                For i = map.NumPlayerSlots To 12 - 1
                    Dim slot As W3Slot = New W3Slot(Me, CByte(i))
                    slot.color = CType(W3Slot.OBS_TEAM, W3Slot.PlayerColor)
                    slot.team = W3Slot.OBS_TEAM
                    slot.race = W3Slot.RaceFlags.Random
                    slots.Add(slot)
                    freeIndexes.Add(CByte(i + 1))
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
                        Contract.Assume(map.NumPlayerSlots > 0)
                        Contract.Assume(slots.Count = 12)
                        Contract.Assume(freeIndexes.Count > 0)
                        If map.NumPlayerSlots <= 10 Then
                            For i = map.NumPlayerSlots To 10 - 1
                                Contract.Assume(slots(i) IsNot Nothing)
                                slots(i).contents = New W3SlotContentsClosed(slots(i))
                            Next i
                            Dim playerIndex = freeIndexes(0)
                            freeIndexes.Remove(playerIndex)
                            Contract.Assume(slots(10) IsNot Nothing)
                            Contract.Assume(slots(11) IsNot Nothing)
                            Contract.Assume(playerIndex > 0)
                            Contract.Assume(playerIndex <= 12)
                            TryAddFakePlayer("# multi obs", slots(10))
                            SetupCoveredSlot(slots(10), slots(11), playerIndex)
                        End If
                    Case "-teams=", "-t="
                        Dim out = XvX(arg2)
                        If out.succeeded Then
                            Contract.Assume(out.Value IsNot Nothing)
                            TrySetTeamSizes(out.Value)
                        End If
                    Case "-reserve=", "-r="
                        Dim slot = (From s In slots _
                                    Where s.contents.WantPlayer(Nothing) >= W3SlotContents.WantPlayerPriority.Accept _
                                    ).FirstOrDefault
                        If slot IsNot Nothing Then
                            ReserveSlot(slot.MatchableId, arg2)
                        End If
                End Select
            Next arg
        End Sub
        Private Sub InitDownloads()
            If server.settings.allowUpload AndAlso map.fileAvailable Then
                Me.downloadScheduler.AddClient(SELF_DOWNLOAD_ID, True)
            End If

            If server.settings.grabMap Then
                Dim server_port = server.settings.default_listen_ports.FirstOrDefault
                If server_port = 0 Then
                    Throw New InvalidOperationException("Server has no port for Grab player to connect on.")
                End If

                Dim grabPort = server.parent.portPool.TryTakePortFromPool()
                If grabPort Is Nothing Then
                    Throw New InvalidOperationException("Failed to get port from pool for Grab player to listen on.")
                End If

                Dim p = New W3DummyPlayer("Grab", grabPort, logger)
                p.QueueConnect("localhost", server_port)
            End If
        End Sub

        Private Sub LobbyStart()
        End Sub
        Private Sub LobbyStop()
            downloadTimer.Stop()
        End Sub
#End Region

#Region "Advancing State"
        '''<summary>Autostarts the countdown if the game stays full for awhile.</summary>
        Private Function TryBeginAutoStart() As Outcome
            'Sanity check
            If Not server.settings.autostarted Then
                Return failure("Autostart is turned off.")
            ElseIf CountFreeSlots() > 0 Then
                Return failure("Game isn't full yet.")
            ElseIf state >= W3GameStates.PreCounting Then
                Return failure("Game is already autostarting.")
            ElseIf (From player In players Where Not player.IsFake And player.GetDownloadPercent <> 100).Any Then
                Return failure("Downloads haven't finished yet.")
            End If

            ChangeState(W3GameStates.PreCounting)
            'Give people a few seconds to realize the game is full before continuing
            FutureWait(3.Seconds).CallWhenReady(Sub() ref.QueueAction(
                Sub()
                    If state <> W3GameStates.PreCounting Then  Return
                    If Not server.settings.autostarted OrElse CountFreeSlots() > 0 Then
                        ChangeState(W3GameStates.AcceptingPlayers)
                        Return
                    End If

                    'Inform players autostart has begun
                    logger.log("Preparing to launch", LogMessageTypes.Positive)
                    BroadcastMessage("Game is Full. Waiting 5 seconds for stability.")

                    'Give jittery players a few seconds to leave
                    FutureWait(5.Seconds).CallWhenReady(Sub() ref.QueueAction(
                        Sub()
                            If state <> W3GameStates.PreCounting Then  Return
                            If Not server.settings.autostarted OrElse CountFreeSlots() > 0 Then
                                ChangeState(W3GameStates.AcceptingPlayers)
                                Return
                            End If

                            'Start the countdown
                            logger.log("Starting Countdown", LogMessageTypes.Positive)
                            TryStartCountdown()
                        End Sub
                    ))
                End Sub
            ))
            Return success("Autostart has begun")
        End Function

        '''<summary>Starts the countdown to launch.</summary>
        Private Function TryStartCountdown() As Outcome
            If state = W3GameStates.CountingDown Then
                Return failure("Countdown is already started.")
            ElseIf state > W3GameStates.CountingDown Then
                Return failure("Countdown has already run.")
            ElseIf (From p In players Where Not p.IsFake AndAlso p.GetDownloadPercent <> 100).Any Then
                Return failure("Downloads haven't finished yet.")
            End If

            ChangeState(W3GameStates.CountingDown)
            flagHasPlayerLeft = False

            For Each player In players
                player.QueueStartCountdown()
            Next player

            FutureWait(1.Seconds).CallWhenReady(Sub() ref.QueueAction(Sub() _TryContinueCountdown(5)))
            Return success("Countdown started.")
        End Function
        Private Sub _TryContinueCountdown(ByVal ticks_left As Integer)
            If state <> W3GameStates.CountingDown Then
                Return
            End If

            'Abort if a player left
            If flagHasPlayerLeft Then
                logger.log("Countdown Aborted", LogMessageTypes.Negative)
                TryRestoreFakeHost()
                BroadcastMessage("===============================================")
                BroadcastMessage("A player left. Launch is held.")
                BroadcastMessage("Waiting for more players...")
                BroadcastMessage("Use {0}leave if you need to leave.".frmt(My.Settings.commandPrefix))
                BroadcastMessage("===============================================")
                ChangeState(W3GameStates.AcceptingPlayers)
                flagHasPlayerLeft = False
                ChangedLobbyState()
                Return
            End If

            If ticks_left > 0 Then
                'Next tick
                logger.log("Game starting in {0}".frmt(ticks_left), LogMessageTypes.Positive)
                For Each player In (From p In players Where p.overcounted)
                    Contract.Assume(player IsNot Nothing)
                    SendMessageTo("Game starting in {0}...".frmt(ticks_left), player, display:=False)
                Next player

                FutureWait(1.Seconds).CallWhenReady(Sub() ref.QueueAction(Sub() _TryContinueCountdown(ticks_left - 1)))
                Return
            End If

            StartLoading()
        End Sub

        '''<summary>Launches the game, sending players to the loading screen.</summary>
        Private Sub StartLoading()
            If state >= W3GameStates.Loading Then Return

            'Remove fake players
            For Each player In (From p In players.ToList Where p.IsFake)
                Contract.Assume(player IsNot Nothing)
                Dim slot = FindPlayerSlot(player)
                If slot Is Nothing OrElse slot.contents.Moveable Then
                    Contract.Assume(player IsNot Nothing)
                    RemovePlayer(player, True, W3PlayerLeaveTypes.Disconnect, "Fake players removed before loading")
                End If
            Next player

            'Encode HCL data
            Dim useableSlots = (From slot In slots Where slot.contents.Moveable AndAlso slot.contents.Type <> W3SlotContents.ContentType.Empty).ToArray
            Dim encodedHandicaps = server.settings.EncodedHCLMode((From slot In useableSlots Select slot.handicap).ToArray)
            For i = 0 To encodedHandicaps.Length - 1
                useableSlots(i).handicap = encodedHandicaps(i)
            Next i
            SendLobbyState()

            ChangeState(W3GameStates.Loading)
            Me.LobbyStop()
            LoadScreenStart()
        End Sub
#End Region

#Region "Players"
        Private Function PlayerVoteToStart(ByVal name As String, ByVal val As Boolean) As Outcome
            Contract.Requires(name IsNot Nothing)
            If Not server.settings.autostarted Then Return failure("Game is not set to autostart.")
            Dim p = FindPlayer(name)
            If p Is Nothing Then Return failure("No player found with the name '{0}'.".frmt(name))
            p.HasVotedToStart = val
            If Not val Then Return success("Removed vote to start.")

            Dim players_ = From q In players Where Not q.IsFake
            Dim voters_ = From q In players Where Not q.IsFake AndAlso q.HasVotedToStart
            Contract.Assume(players_ IsNot Nothing)
            Contract.Assume(voters_ IsNot Nothing)
            Dim numPlayers = (players_).Count()
            Dim numInFavor = (voters_).Count()
            If numPlayers >= 2 And numInFavor * 3 >= numPlayers * 2 Then
                TryStartCountdown()
            End If
            Return success("Voted to start.")
        End Function

        Private Function TryAddFakePlayer(ByVal name As String,
                                          Optional ByVal newSlot As W3Slot = Nothing) As Outcome(Of IW3Player)
            Contract.Requires(name IsNot Nothing)
            If state > W3GameStates.AcceptingPlayers Then
                Return failure("No longer accepting players.")
            ElseIf freeIndexes.Count <= 0 And fakeHostPlayer Is Nothing Then
                Return failure("No space available for fake player.")
            End If

            'Assign index
            If freeIndexes.Count = 0 Then RemovePlayer(fakeHostPlayer, True, W3PlayerLeaveTypes.Disconnect, "Need player index for new fake player")
            Dim index = freeIndexes(0)
            freeIndexes.Remove(index)

            'Make player
            Contract.Assume(index > 0)
            Contract.Assume(index <= 12)
            Dim newPlayer As IW3Player = New W3Player(index, Me, name, logger)
            If newSlot IsNot Nothing Then
                newSlot.contents = New W3SlotContentsPlayer(newSlot, newPlayer)
            End If
            players.Add(newPlayer)

            'Inform other players
            For Each player In players
                player.QueueSendPacket(W3Packet.MakeOtherPlayerJoined(newPlayer, index))
            Next player

            'Inform bot
            ThrowPlayerEntered(newPlayer)
            logger.log(newPlayer.name + " has been placed in the game.", LogMessageTypes.Positive)

            'Update state
            ChangedLobbyState()
            Return Success(newPlayer, "Added fake player '{0}'.".Frmt(newPlayer.name))
        End Function
        Private Function TryRestoreFakeHost() As Outcome
            If fakeHostPlayer IsNot Nothing Then
                Return failure("Fake host already exists.")
            ElseIf state >= W3GameStates.CountingDown Then
                Return failure("Fake host can't join after countdown has started.")
            End If

            Dim pname = My.Settings.ingame_name
            Contract.Assume(pname IsNot Nothing)
            Dim out = TryAddFakePlayer(pname)
            If out.succeeded Then
                fakeHostPlayer = out.Value
            End If
            Return out
        End Function

        Private Function TryAddPlayer(ByVal connecting_player As W3ConnectingPlayer) As Outcome
            If state > W3GameStates.AcceptingPlayers Then
                Return failure("No longer accepting players.")
            ElseIf Not connecting_player.Socket.connected Then
                Return failure("Player isn't connected.")
            End If

            'Assign slot
            Dim bestSlot As W3Slot = Nothing
            Dim bestMatch = W3SlotContents.WantPlayerPriority.Reject
            slots.MaxPair(Function(slot) slot.contents.WantPlayer(connecting_player.Name),
                               bestSlot,
                               bestMatch)
            If bestMatch <= W3SlotContents.WantPlayerPriority.Reluctant Then
                Return failure("No slot available for player.")
            End If

            'Assign index
            Dim index As Byte = 0
            If bestSlot.contents.PlayerIndex <> 0 And bestMatch <> W3SlotContents.WantPlayerPriority.AcceptReservation Then
                'the slot requires the player to take a specific index
                index = bestSlot.contents.PlayerIndex
            ElseIf freeIndexes.Count > 0 Then
                'there is a player index available
                index = freeIndexes(0)
            ElseIf fakeHostPlayer IsNot Nothing Then
                'the only player index left belongs to the fake host
                index = fakeHostPlayer.index
                RemovePlayer(fakeHostPlayer, True, W3PlayerLeaveTypes.Disconnect, "Need player index for joining player.")
            Else
                'no indexes left, go away
                Return failure("No index space available for player.")
            End If
            freeIndexes.Remove(index)
            Contract.Assume(index > 0)
            Contract.Assume(index <= 12)

            'Reservation
            If bestMatch = W3SlotContents.WantPlayerPriority.AcceptReservation Then
                For Each player In bestSlot.contents.EnumPlayers
                    Contract.Assume(player IsNot Nothing)
                    RemovePlayer(player, True, W3PlayerLeaveTypes.Disconnect, "Reservation fulfilled")
                Next player
            End If

            'Create player object
            Dim newPlayer As IW3Player = New W3Player(index, Me, connecting_player, logger)
            bestSlot.contents = bestSlot.contents.TakePlayer(newPlayer)
            players.Add(newPlayer)

            'Greet new player
            newPlayer.QueueSendPacket(W3Packet.MakeGreet(newPlayer, index, map))
            For Each player In (From p In players Where p IsNot newPlayer AndAlso IsPlayerVisible(p))
                newPlayer.QueueSendPacket(W3Packet.MakeOtherPlayerJoined(player))
            Next player
            newPlayer.QueueSendPacket(W3Packet.MakeHostMapInfo(map))
            If My.Resources.ProgramShowoff_f0name <> "" Then
                SendMessageTo(My.Resources.ProgramShowoff_f0name.frmt(My.Resources.ProgramName), newPlayer)
            End If

            'Inform other players
            If IsPlayerVisible(newPlayer) Then
                For Each player In (From p In players Where p IsNot newPlayer)
                    player.QueueSendPacket(W3Packet.MakeOtherPlayerJoined(newPlayer, index))
                Next player
            End If

            'Inform bot
            ThrowPlayerEntered(newPlayer)
            logger.log("{0} has entered the game.".frmt(newPlayer.name), LogMessageTypes.Positive)

            'Update state
            ChangedLobbyState()
            TryBeginAutoStart()
            If server.settings.auto_elevate_username IsNot Nothing Then
                If newPlayer.name.ToLower() = server.settings.auto_elevate_username.ToLower() Then
                    TryElevatePlayer(newPlayer.name)
                End If
            End If
            If server.settings.autostarted Then
                SendMessageTo("This is an automated game. {0}help for a list of commands.".frmt(My.Settings.commandPrefix), newPlayer)
            End If

            Return success("Added player '{0}' to instance '{1}'.".frmt(newPlayer.name, name))
        End Function
#End Region

#Region "Events"
        Private Sub ThrowPlayerEntered(ByVal new_player As IW3Player)
            eventRef.QueueAction(Sub()
                                     RaiseEvent PlayerEntered(Me, new_player)
                                 End Sub)
        End Sub

        Private Sub CatchDownloadSchedulerActions(ByVal started As List(Of TransferScheduler(Of Byte).TransferEndPoints),
                                                  ByVal stopped As List(Of TransferScheduler(Of Byte).TransferEndPoints))
            ref.QueueAction(
                Sub()
                    'Start transfers
                    For Each e In started
                        'Find matching players
                        Dim src = FindPlayer(e.src)
                        Dim dst = FindPlayer(e.dst)
                        If dst Is Nothing Then  Continue For

                        'Apply
                        If e.src = SELF_DOWNLOAD_ID Then
                            logger.log("Initiating map upload to {0}.".frmt(dst.name), LogMessageTypes.Positive)
                            dst.IsGettingMapFromBot = True
                            dst.QueueBufferMap()
                        ElseIf src IsNot Nothing Then
                            logger.log("Initiating peer map transfer from {0} to {1}.".frmt(src.name, dst.name), LogMessageTypes.Positive)
                            src.QueueSendPacket(W3Packet.MakeSetUploadTarget(dst.index, CUInt(Math.Max(0, dst.MapDownloadPosition))))
                            dst.QueueSendPacket(W3Packet.MakeSetDownloadSource(src.index))
                        End If
                    Next e

                    'Stop transfers
                    For Each e In stopped
                        'Find matching players
                        Dim src = FindPlayer(e.src)
                        Dim dst = FindPlayer(e.dst)
                        If dst Is Nothing Then  Continue For

                        'Apply
                        If e.src = SELF_DOWNLOAD_ID Then
                            logger.log("Stopping map upload to {0}.".frmt(dst.name), LogMessageTypes.Positive)
                            dst.IsGettingMapFromBot = False
                        ElseIf src IsNot Nothing Then
                            logger.log("Stopping peer map transfer from {0} to {1}.".frmt(src.name, dst.name), LogMessageTypes.Positive)
                            src.QueueSendPacket(W3Packet.MakeOtherPlayerLeft(dst, W3PlayerLeaveTypes.Disconnect))
                            src.QueueSendPacket(W3Packet.MakeOtherPlayerJoined(dst))
                            dst.QueueSendPacket(W3Packet.MakeOtherPlayerLeft(src, W3PlayerLeaveTypes.Disconnect))
                            dst.QueueSendPacket(W3Packet.MakeOtherPlayerJoined(src))
                        End If
                    Next e
                End Sub
            )
        End Sub

        Private Sub c_ScheduleDownloads()
            downloadScheduler.Update()
        End Sub

        Public Sub LobbyCatchRemovedPlayer(ByVal p As IW3Player, ByVal slot As W3Slot)
            If slot Is Nothing OrElse slot.contents.PlayerIndex <> p.index Then
                freeIndexes.Add(p.index)
            End If
            downloadScheduler.RemoveClient(p.index)
            If p IsNot fakeHostPlayer Then TryRestoreFakeHost()
            ChangedLobbyState()
        End Sub
#End Region

#Region "Slots"
        '''<summary>Broadcasts new game state to players, and throws the updated event.</summary>
        Private Sub ChangedLobbyState()
            ThrowUpdated()

            'Don't let update rate to clients become too high
            slotStateUpdateThrottle.SetActionToRun(Sub() ref.QueueAction(AddressOf SendLobbyState))
        End Sub
        Private Sub SendLobbyState()
            If state >= W3GameStates.Loading Then Return

            Dim time As ModInt32 = Environment.TickCount()
            For Each player In players
                Contract.Assume(player IsNot Nothing)
                Dim pk = W3Packet.MakeLobbyState(player, map, slots, time, server.settings.isAdminGame)
                player.QueueSendPacket(pk)
            Next player
            TryBeginAutoStart()
        End Sub

        ''' <summary>Opens slots, closes slots and moves players around to try to match the desired team sizes.</summary>
        Private Function TrySetTeamSizes(ByVal desiredTeamSizes As IList(Of Integer)) As Outcome
            Contract.Requires(desiredTeamSizes IsNot Nothing)
            If state > W3GameStates.AcceptingPlayers Then
                Return failure("Can't change team sizes after launch.")
            End If

            Dim availableWellPlacedSlots = New List(Of W3Slot)
            Dim misplacedPlayerSlots = New List(Of W3Slot)
            Dim teamSizesLeft = desiredTeamSizes.ToArray()
            For Each slot In slots
                If slot.team >= teamSizesLeft.Count Then Continue For

                Select Case slot.contents.Type
                    Case W3SlotContents.ContentType.Computer
                        'computers slots shouldn't be affected

                    Case W3SlotContents.ContentType.Empty
                        If teamSizesLeft(slot.team) > 0 Then
                            teamSizesLeft(slot.team) -= 1
                            availableWellPlacedSlots.Add(slot)
                            slot.contents = New W3SlotContentsOpen(slot)
                        Else
                            slot.contents = New W3SlotContentsClosed(slot)
                        End If

                    Case W3SlotContents.ContentType.Player
                        If teamSizesLeft(slot.team) > 0 Then
                            teamSizesLeft(slot.team) -= 1
                        Else
                            misplacedPlayerSlots.Add(slot)
                        End If

                    Case Else
                        Throw New UnreachableException
                End Select
            Next slot

            'Swap misplaced players to wellplaced slots
            For i = 0 To Math.Min(availableWellPlacedSlots.Count, misplacedPlayerSlots.Count) - 1
                Contract.Assume(availableWellPlacedSlots(i) IsNot Nothing)
                Contract.Assume(misplacedPlayerSlots(i) IsNot Nothing)
                SwapSlotContents(availableWellPlacedSlots(i), misplacedPlayerSlots(i))
            Next i

            ChangedLobbyState()
            Return success("Set team sizes.")
        End Function

        Private Function ModifySlotContents(ByVal slotQuery As String,
                                            ByVal action As Action(Of W3Slot),
                                            ByVal successDescription As String,
                                            Optional ByVal avoidPlayers As Boolean = False) As Outcome
            Contract.Requires(slotQuery IsNot Nothing)
            Contract.Requires(action IsNot Nothing)
            Contract.Requires(successDescription IsNot Nothing)

            If state >= W3GameStates.CountingDown Then
                Return failure("Can't modify slots during launch.")
            End If

            Dim found_slot = FindMatchingSlot(slotQuery)
            If Not found_slot.succeeded Then
                Return found_slot
            End If

            Dim slot = found_slot.Value
            If avoidPlayers AndAlso slot.contents.Type = W3SlotContents.ContentType.Player Then
                Return failure("Slot '{0}' contains a player.".frmt(slotQuery))
            End If

            Call action(slot)
            ChangedLobbyState()
            Return success(successDescription)
        End Function
#End Region
#Region "Slot Contents"
        '''<summary>Opens the slot with the given index, unless the slot contains a player.</summary>
        Private Function OpenSlot(ByVal slotid As String) As Outcome
            Contract.Requires(slotid IsNot Nothing)
            Return ModifySlotContents(slotid,
                                      Sub(slot)
                                          Contract.Assume(slot IsNot Nothing)
                                          slot.contents = New W3SlotContentsOpen(slot)
                                      End Sub,
                                      "Slot '{0}' opened.".frmt(slotid),
                                      avoidPlayers:=True)
        End Function

        '''<summary>Places a computer with the given difficulty in the slot with the given index, unless the slot contains a player.</summary>
        Private Function ComputerizeSlot(ByVal slotid As String, ByVal cpu As W3Slot.ComputerLevel) As outcome
            Contract.Requires(slotid IsNot Nothing)
            Return ModifySlotContents(slotid,
                                      Sub(slot)
                                          Contract.Assume(slot IsNot Nothing)
                                          slot.contents = New W3SlotContentsComputer(slot, cpu)
                                      End Sub,
                                      "Slot '{0}' computerized.".frmt(slotid),
                                      avoidPlayers:=True)
        End Function

        '''<summary>Closes the slot with the given index, unless the slot contains a player.</summary>
        Private Function CloseSlot(ByVal slotid As String) As Outcome
            Contract.Requires(slotid IsNot Nothing)
            Return ModifySlotContents(slotid,
                                      Sub(slot)
                                          Contract.Assume(slot IsNot Nothing)
                                          slot.contents = New W3SlotContentsClosed(slot)
                                      End Sub,
                                      "Slot '{0}' closed.".frmt(slotid),
                                      avoidPlayers:=True)
        End Function

        '''<summary>Reserves a slot for a player.</summary>
        Private Function ReserveSlot(ByVal slotid As String, ByVal username As String) As Outcome
            Contract.Requires(slotid IsNot Nothing)
            Contract.Requires(username IsNot Nothing)
            If state >= W3GameStates.CountingDown Then
                Return failure("Can't reserve slots after launch.")
            End If
            Dim slot_out = FindMatchingSlot(slotid)
            If Not slot_out.succeeded Then Return slot_out
            Dim slot = slot_out.Value
            If slot.contents.Type = W3SlotContents.ContentType.Player Then
                Return failure("Slot '{0}' can't be reserved because it already contains a player.".frmt(slotid))
            Else
                Return TryAddFakePlayer(username, slot)
            End If
        End Function

        Private Function SwapSlotContents(ByVal query1 As String, ByVal query2 As String) As Outcome
            Contract.Requires(query1 IsNot Nothing)
            Contract.Requires(query2 IsNot Nothing)
            If state > W3GameStates.AcceptingPlayers Then
                Return failure("Can't swap slots after launch.")
            End If
            Dim slot_out1 = FindMatchingSlot(query1)
            Dim slot_out2 = FindMatchingSlot(query2)
            If Not slot_out1.succeeded Then Return slot_out1
            If Not slot_out2.succeeded Then Return slot_out2
            Dim slot1 = slot_out1.Value
            Dim slot2 = slot_out2.Value
            If slot1 Is Nothing Then
                Return failure("No slot matching '{0}'.".frmt(query1))
            ElseIf slot2 Is Nothing Then
                Return failure("No slot matching '{0}'.".frmt(query2))
            ElseIf slot1 Is slot2 Then
                Return failure("Slot {0} is slot '{1}'.".frmt(query1, query2))
            End If
            SwapSlotContents(slot1, slot2)
            ChangedLobbyState()
            Return success("Slot '{0}' swapped with slot '{1}'.".frmt(query1, query2))
        End Function
        Private Sub SwapSlotContents(ByVal slot1 As W3Slot, ByVal slot2 As W3Slot)
            Contract.Requires(slot1 IsNot Nothing)
            Contract.Requires(slot2 IsNot Nothing)
            Dim t = slot1.contents.Clone(slot2)
            slot1.contents = slot2.contents.Clone(slot1)
            slot2.contents = t
            ChangedLobbyState()
        End Sub
#End Region
#Region "Slot States"
        Private Function SetSlotColor(ByVal slotid As String, ByVal color As W3Slot.PlayerColor) As Outcome
            Contract.Requires(slotid IsNot Nothing)
            If state > W3GameStates.CountingDown Then
                Return failure("Can't change slot settings after launch.")
            End If

            Dim foundSlot = FindMatchingSlot(slotid)
            If Not foundSlot.succeeded Then Return foundSlot

            Dim swap_color_slot = (From x In slots Where x.color = color).FirstOrDefault
            If swap_color_slot IsNot Nothing Then swap_color_slot.color = foundSlot.Value.color
            foundSlot.Value.color = color

            ChangedLobbyState()
            Return success("Slot '{0}' color set to {1}.".frmt(slotid, color))
        End Function

        Private Function SetSlotRace(ByVal slotid As String, ByVal race As W3Slot.RaceFlags) As Outcome
            Contract.Requires(slotid IsNot Nothing)
            Return ModifySlotContents(slotid,
                                      Sub(slot)
                                          slot.race = race
                                      End Sub,
                                      "Slot '{0}' race set to {1}.".frmt(slotid, race))
        End Function

        Private Function SetSlotTeam(ByVal slotid As String, ByVal team As Byte) As Outcome
            Contract.Requires(slotid IsNot Nothing)
            Return ModifySlotContents(slotid,
                                      Sub(slot)
                                          slot.team = team
                                      End Sub,
                                      "Slot '{0}' team set to {1}.".frmt(slotid, team))
        End Function

        Private Function SetSlotHandicap(ByVal slotid As String, ByVal handicap As Byte) As Outcome
            Contract.Requires(slotid IsNot Nothing)
            Return ModifySlotContents(slotid,
                                      Sub(slot)
                                          slot.handicap = handicap
                                      End Sub,
                                      "Slot '{0}' handicap set to {1}.".frmt(slotid, handicap))
        End Function

        Private Function SetSlotLocked(ByVal slotid As String, ByVal locked As W3Slot.Lock) As Outcome
            Contract.Requires(slotid IsNot Nothing)
            Return ModifySlotContents(slotid,
                                      Sub(slot)
                                          slot.locked = locked
                                      End Sub,
                                      "Slot '{0}' lock state set to {1}.".frmt(slotid, locked))
        End Function

        Private Function SetAllSlotsLocked(ByVal locked As W3Slot.Lock) As Outcome
            If state > W3GameStates.AcceptingPlayers Then
                Return failure("Can't lock slots after launch.")
            End If
            For Each slot In slots.ToList
                slot.locked = locked
            Next slot
            Return success("All slots' lock state set to {0}.".frmt(locked))
        End Function
#End Region

#Region "Networking"
        Private Sub ReceiveSetColor(ByVal player As IW3Player, ByVal newColor As W3Slot.PlayerColor)
            Contract.Requires(player IsNot Nothing)
            Dim slot = FindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = W3Slot.Lock.frozen Then Return '[no changes allowed]
            If Not slot.contents.Moveable Then Return '[slot is weird]
            If state >= W3GameStates.Loading Then Return '[too late]
            If Not newColor.EnumValueIsDefined Then Return '[not a valid color]

            'check for duplicates
            For Each other_slot In slots.ToList()
                If other_slot.color = newColor Then
                    If Not map.isMelee Then Return
                    If Not other_slot.contents.Type = W3SlotContents.ContentType.Empty Then Return
                    other_slot.color = slot.color
                    Exit For
                End If
            Next other_slot

            'change color
            slot.color = newColor
            ChangedLobbyState()
        End Sub
        Private Sub ReceiveSetRace(ByVal player As IW3Player, ByVal newRace As W3Slot.RaceFlags)
            Contract.Requires(player IsNot Nothing)
            Dim slot = FindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = W3Slot.Lock.frozen Then Return '[no changes allowed]
            If Not slot.contents.Moveable Then Return '[slot is weird]
            If state >= W3GameStates.Loading Then Return '[too late]
            If Not newRace.EnumValueIsDefined OrElse newRace = W3Slot.RaceFlags.Unlocked Then Return '[not a valid race]

            'Perform
            slot.race = newRace
            ChangedLobbyState()
        End Sub
        Private Sub ReceiveSetHandicap(ByVal player As IW3Player, ByVal new_handicap As Byte)
            Contract.Requires(player IsNot Nothing)
            Dim slot = FindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = W3Slot.Lock.frozen Then Return '[no changes allowed]
            If Not slot.contents.Moveable Then Return '[slot is weird]
            If state >= W3GameStates.CountingDown Then Return '[too late]

            'Perform
            Select Case new_handicap
                Case 50, 60, 70, 80, 90, 100
                    slot.handicap = new_handicap
                Case Else
                    Return '[invalid handicap]
            End Select

            ChangedLobbyState()
        End Sub
        Private Sub ReceiveSetTeam(ByVal player As IW3Player, ByVal new_team As Byte)
            Contract.Requires(player IsNot Nothing)
            Dim slot = FindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked <> W3Slot.Lock.unlocked Then Return '[no teams changes allowed]
            If new_team > W3Slot.OBS_TEAM Then Return '[invalid value]
            If Not slot.contents.Moveable Then Return '[slot is weird]
            If state >= W3GameStates.Loading Then Return '[too late]
            If new_team = W3Slot.OBS_TEAM Then
                Select Case server.settings.header.Map.observers
                    Case GameObserverOption.FullObservers, GameObserverOption.Referees
                        '[fine; continue]
                    Case Else
                        Return '[obs not enabled; invalid value]
                End Select
            ElseIf Not map.isMelee And new_team >= map.numForces Then
                Return '[invalid team]
            ElseIf map.isMelee And new_team >= map.NumPlayerSlots Then
                Return '[invalid team]
            End If

            'Perform
            If map.isMelee Then
                'set slot to target team
                slot.team = new_team
            Else
                'swap with next open slot from target team
                For offset_mod = 0 To slots.Count - 1
                    Dim nextIndex = (slot.index + offset_mod) Mod slots.Count
                    Contract.Assume(nextIndex >= 0)
                    Dim nextSlot = slots(nextIndex)
                    Contract.Assume(nextSlot IsNot Nothing)
                    If nextSlot.team = new_team AndAlso nextSlot.contents.WantPlayer(player.name) > W3SlotContents.WantPlayerPriority.Accept Then
                        SwapSlotContents(nextSlot, slot)
                        Exit For
                    End If
                Next offset_mod
            End If

            ChangedLobbyState()
        End Sub
#End Region

#Region "Interface"
        Private ReadOnly Property _DownloadScheduler() As TransferScheduler(Of Byte) Implements IW3Game.DownloadScheduler
            Get
                Return downloadScheduler
            End Get
        End Property

        Private Function _QueueUpdatedGameState() As IFuture Implements IW3Game.QueueUpdatedGameState
            Return ref.QueueAction(AddressOf ChangedLobbyState)
        End Function

        Private Function _QueueOpenSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3Game.QueueOpenSlot
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query IsNot Nothing)
                                     Return OpenSlot(query)
                                 End Function)
        End Function
        Private Function _QueueCloseSlot(ByVal query As String) As IFuture(Of Outcome) Implements IW3Game.QueueCloseSlot
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query IsNot Nothing)
                                     Return CloseSlot(query)
                                 End Function)
        End Function
        Private Function _QueueReserveSlot(ByVal query As String, ByVal username As String) As IFuture(Of Outcome) Implements IW3Game.QueueReserveSlot
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query IsNot Nothing)
                                     Contract.Assume(username IsNot Nothing)
                                     Return ReserveSlot(query, username)
                                 End Function)
        End Function
        Private Function _QueueSwapSlotContents(ByVal query1 As String, ByVal query2 As String) As IFuture(Of Outcome) Implements IW3Game.QueueSwapSlotContents
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query1 IsNot Nothing)
                                     Contract.Assume(query2 IsNot Nothing)
                                     Return SwapSlotContents(query1, query2)
                                 End Function)
        End Function

        Private Function _QueueSetSlotCpu(ByVal query As String, ByVal newCpuLevel As W3Slot.ComputerLevel) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotCpu
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query IsNot Nothing)
                                     Return ComputerizeSlot(query, newCpuLevel)
                                 End Function)
        End Function
        Private Function _QueueSetSlotLocked(ByVal query As String, ByVal newLockState As W3Slot.Lock) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotLocked
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query IsNot Nothing)
                                     Return SetSlotLocked(query, newLockState)
                                 End Function)
        End Function
        Private Function _QueueSetAllSlotsLocked(ByVal newLockState As W3Slot.Lock) As IFuture(Of Outcome) Implements IW3Game.QueueSetAllSlotsLocked
            Return ref.QueueFunc(Function() SetAllSlotsLocked(newLockState))
        End Function
        Private Function _QueueSetSlotHandicap(ByVal query As String, ByVal newHandicap As Byte) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotHandicap
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query IsNot Nothing)
                                     Return SetSlotHandicap(query, newHandicap)
                                 End Function)
        End Function
        Private Function _QueueSetSlotTeam(ByVal query As String, ByVal new_team As Byte) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotTeam
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query IsNot Nothing)
                                     Return SetSlotTeam(query, new_team)
                                 End Function)
        End Function
        Private Function _QueueSetSlotRace(ByVal query As String, ByVal newRace As W3Slot.RaceFlags) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotRace
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query IsNot Nothing)
                                     Return SetSlotRace(query, newRace)
                                 End Function)
        End Function
        Private Function _QueueSetSlotColor(ByVal query As String, ByVal newColor As W3Slot.PlayerColor) As IFuture(Of Outcome) Implements IW3Game.QueueSetSlotColor
            Return ref.QueueFunc(Function()
                                     Contract.Assume(query IsNot Nothing)
                                     Return SetSlotColor(query, newColor)
                                 End Function)
        End Function

        Private Function _QueueTryAddPlayer(ByVal newPlayer As W3ConnectingPlayer) As IFuture(Of Outcome) Implements IW3Game.QueueTryAddPlayer
            Return ref.QueueFunc(Function() TryAddPlayer(newPlayer))
        End Function
        Private Function _QueuePlayerVoteToStart(ByVal name As String, ByVal val As Boolean) As IFuture(Of Outcome) Implements IW3Game.QueuePlayerVoteToStart
            Return ref.QueueFunc(Function()
                                     Contract.Assume(name IsNot Nothing)
                                     Return PlayerVoteToStart(name, val)
                                 End Function)
        End Function
        Private Function _QueueStartCountdown() As IFuture(Of Outcome) Implements IW3Game.QueueStartCountdown
            Return ref.QueueFunc(Function() TryStartCountdown())
        End Function
        Private Function _QueueTrySetTeamSizes(ByVal sizes As IList(Of Integer)) As IFuture(Of Outcome) Implements IW3Game.QueueTrySetTeamSizes
            Return ref.QueueFunc(Function()
                                     Contract.Assume(sizes IsNot Nothing)
                                     Return TrySetTeamSizes(sizes)
                                 End Function)
        End Function
#End Region
    End Class
End Namespace
