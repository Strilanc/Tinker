Namespace Warcraft3
    Partial Public Class W3Game
        Public Const LocalTransferClientKey As Byte = 255

        Private _downloadScheduler As TransferScheduler(Of Byte)
        Private ReadOnly downloadTimer As New Timers.Timer(1000.Milliseconds.TotalSeconds)
        Private ReadOnly freeIndexes As New List(Of Byte)
        Private ReadOnly slotStateUpdateThrottle As New Throttle(250.MilliSeconds)
        Private ReadOnly updateEventThrottle As New Throttle(100.MilliSeconds)

        Public Event PlayerEntered(ByVal sender As W3Game, ByVal player As W3Player)

#Region "Life"
        Private Sub LobbyNew(ByVal arguments As IEnumerable(Of String))
            Contract.Ensures(downloadScheduler IsNot Nothing)

            Dim rate As FiniteDouble = 10 * 1024 / 1000
            Dim switchTime As FiniteDouble = 3000
            Dim size As FiniteDouble = New FiniteDouble(Map.FileSize)
            Me._downloadScheduler = New TransferScheduler(Of Byte)(typicalRate:=rate,
                                                                   typicalSwitchTime:=3000,
                                                                   filesize:=size)
            AddHandler downloadScheduler.Actions, AddressOf OnDownloadSchedulerActions
            AddHandler downloadTimer.Elapsed, Sub() OnScheduleDownloadsTick()

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
                    slot.color = CType(W3Slot.ObserverTeamIndex, W3Slot.PlayerColor)
                    slot.team = W3Slot.ObserverTeamIndex
                    slot.race = W3Slot.Races.Random
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

                Select Case arg.ToUpperInvariant
                    Case "-MULTIOBS", "-MO"
                        Contract.Assume(Map.NumPlayerSlots > 0)
                        Contract.Assume(slots.Count = 12)
                        Contract.Assume(freeIndexes.Count > 0)
                        If Map.NumPlayerSlots <= 10 Then
                            For i = Map.NumPlayerSlots To 10 - 1
                                Contract.Assume(slots(i) IsNot Nothing)
                                slots(i).contents = New W3SlotContentsClosed(slots(i))
                            Next i
                            Dim playerIndex = freeIndexes(0)
                            freeIndexes.Remove(playerIndex)
                            Contract.Assume(slots(10) IsNot Nothing)
                            Contract.Assume(slots(11) IsNot Nothing)
                            Contract.Assume(playerIndex > 0)
                            Contract.Assume(playerIndex <= 12)
                            AddFakePlayer("# multi obs", slots(10))
                            SetupCoveredSlot(slots(10), slots(11), playerIndex)
                        End If
                    Case "-TEAMS=", "-T="
                        TrySetTeamSizes(TeamVersusStringToTeamSizes(arg2))
                    Case "-RESERVE=", "-R="
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
                Me.downloadScheduler.AddClient(LocalTransferClientKey, True)
            End If

            If server.settings.grabMap Then
                Dim server_port = server.settings.default_listen_ports.FirstOrDefault
                If server_port = 0 Then
                    Throw New InvalidOperationException("Server has no port for Grab player to connect on.")
                End If

                Dim grabPort = server.parent.portPool.TryAcquireAnyPort()
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
        '''<summary>Autostarts the countdown if autostart is enabled and the game stays full for awhile.</summary>
        Private Function TryBeginAutoStart() As Boolean
            'Sanity check
            If Not server.settings.isAutoStarted Then
                Return False
            ElseIf CountFreeSlots() > 0 Then
                Return False
            ElseIf state >= W3GameState.PreCounting Then
                Return False
            ElseIf (From player In players Where Not player.isFake And player.GetDownloadPercent <> 100).Any Then
                Return False
            End If
            ChangeState(W3GameState.PreCounting)

            'Give people a few seconds to realize the game is full before continuing
            FutureWait(3.Seconds).QueueCallWhenReady(ref,
                Sub()
                    If state <> W3GameState.PreCounting Then  Return
                    If Not server.settings.isAutoStarted OrElse CountFreeSlots() > 0 Then
                        ChangeState(W3GameState.AcceptingPlayers)
                        Return
                    End If

                    'Inform players autostart has begun
                    logger.Log("Preparing to launch", LogMessageType.Positive)
                    BroadcastMessage("Game is Full. Waiting 5 seconds for stability.")

                    'Give jittery players a few seconds to leave
                    FutureWait(5.Seconds).QueueCallWhenReady(ref,
                        Sub()
                            If state <> W3GameState.PreCounting Then  Return
                            If Not server.settings.isAutoStarted OrElse CountFreeSlots() > 0 Then
                                ChangeState(W3GameState.AcceptingPlayers)
                                Return
                            End If

                            TryStartCountdown()
                        End Sub
                    )
                End Sub
            )
            Return True
        End Function

        '''<summary>Starts the countdown to launch.</summary>
        Private Function TryStartCountdown() As Boolean
            If state = W3GameState.CountingDown Then
                Return False
            ElseIf state > W3GameState.CountingDown Then
                Return False
            ElseIf (From p In players Where Not p.isFake AndAlso p.GetDownloadPercent <> 100).Any Then
                Return False
            End If

            ChangeState(W3GameState.CountingDown)
            flagHasPlayerLeft = False

            For Each player In players
                player.QueueStartCountdown()
            Next player

            logger.Log("Starting Countdown", LogMessageType.Positive)
            FutureWait(1.Seconds).QueueCallWhenReady(ref, Sub() _TryContinueCountdown(5))
            Return True
        End Function
        Private Sub _TryContinueCountdown(ByVal ticksLeft As Integer)
            If state <> W3GameState.CountingDown Then
                Return
            End If

            'Abort if a player left
            If flagHasPlayerLeft Then
                logger.log("Countdown Aborted", LogMessageType.Negative)
                TryRestoreFakeHost()
                BroadcastMessage("===============================================")
                BroadcastMessage("A player left. Launch is held.")
                BroadcastMessage("Waiting for more players...")
                BroadcastMessage("Use {0}leave if you need to leave.".frmt(My.Settings.commandPrefix))
                BroadcastMessage("===============================================")
                ChangeState(W3GameState.AcceptingPlayers)
                flagHasPlayerLeft = False
                ChangedLobbyState()
                Return
            End If

            If ticksLeft > 0 Then
                'Next tick
                logger.log("Game starting in {0}".frmt(ticksLeft), LogMessageType.Positive)
                For Each player In (From p In players Where p.IsOverCounted)
                    Contract.Assume(player IsNot Nothing)
                    SendMessageTo("Game starting in {0}...".Frmt(ticksLeft), player, display:=False)
                Next player

                FutureWait(1.Seconds).QueueCallWhenReady(ref, Sub() _TryContinueCountdown(ticksLeft - 1))
                Return
            End If

            StartLoading()
        End Sub

        '''<summary>Launches the game, sending players to the loading screen.</summary>
        Private Sub StartLoading()
            If state >= W3GameState.Loading Then Return

            'Remove fake players
            For Each player In (From p In players.ToList Where p.IsFake)
                Contract.Assume(player IsNot Nothing)
                Dim slot = FindPlayerSlot(player)
                If slot Is Nothing OrElse slot.contents.Moveable Then
                    Contract.Assume(player IsNot Nothing)
                    RemovePlayer(player, True, W3PlayerLeaveType.Disconnect, "Fake players removed before loading")
                End If
            Next player

            'Encode HCL data
            Dim useableSlots = (From slot In slots Where slot.contents.Moveable AndAlso slot.contents.ContentType <> SlotContentType.Empty).ToArray
            Dim encodedHandicaps = Server.settings.EncodedHCLMode((From slot In useableSlots Select slot.handicap).ToArray)
            For i = 0 To encodedHandicaps.Length - 1
                useableSlots(i).handicap = encodedHandicaps(i)
            Next i
            SendLobbyState()

            ChangeState(W3GameState.Loading)
            Me.LobbyStop()
            LoadScreenStart()
        End Sub
#End Region

#Region "Players"
        Private Sub SetPlayerVoteToStart(ByVal name As String, ByVal val As Boolean)
            Contract.Requires(name IsNot Nothing)
            If Not Server.settings.isAutoStarted Then Throw New InvalidOperationException("Game is not set to start automatically.")
            Dim p = FindPlayer(name)
            If p Is Nothing Then Throw New InvalidOperationException("No player found with the name '{0}'.".Frmt(name))
            p.hasVotedToStart = val
            If Not val Then Return

            Dim numPlayers = (From q In players Where Not q.isFake).Count
            Dim numInFavor = (From q In players Where Not q.isFake AndAlso q.hasVotedToStart).Count
            If numPlayers >= 2 And numInFavor * 3 >= numPlayers * 2 Then
                TryStartCountdown()
            End If
        End Sub

        Private Function AddFakePlayer(ByVal name As String,
                                       Optional ByVal newSlot As W3Slot = Nothing) As W3Player
            Contract.Requires(name IsNot Nothing)
            If state > W3GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("No longer accepting players.")
            ElseIf freeIndexes.Count <= 0 And fakeHostPlayer Is Nothing Then
                Throw New InvalidOperationException("No space available for fake player.")
            End If

            'Assign index
            If freeIndexes.Count = 0 Then RemovePlayer(fakeHostPlayer, True, W3PlayerLeaveType.Disconnect, "Need player index for new fake player")
            Dim index = freeIndexes(0)
            freeIndexes.Remove(index)

            'Make player
            Contract.Assume(index > 0)
            Contract.Assume(index <= 12)
            Dim newPlayer As W3Player = New W3Player(index, Me, name, Logger)
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
            Logger.Log(newPlayer.name + " has been placed in the game.", LogMessageType.Positive)

            'Update state
            ChangedLobbyState()
            Return newPlayer
        End Function
        Private Function TryRestoreFakeHost() As W3Player
            If fakeHostPlayer IsNot Nothing Then
                Return Nothing
            ElseIf state > W3GameState.AcceptingPlayers Then
                Return Nothing
            End If

            Dim pname = My.Settings.ingame_name
            Contract.Assume(pname IsNot Nothing)
            Try
                fakeHostPlayer = AddFakePlayer(pname)
                Return fakeHostPlayer
            Catch ex As InvalidOperationException
                Return Nothing
            End Try
        End Function

        Private Function AddPlayer(ByVal connectingPlayer As W3ConnectingPlayer) As W3Player
            If state > W3GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("No longer accepting players.")
            ElseIf Not connectingPlayer.Socket.connected Then
                Throw New InvalidOperationException("Player isn't connected.")
            End If

            'Assign slot
            Dim bestSlot As W3Slot = Nothing
            Dim bestMatch = W3SlotContents.WantPlayerPriority.Reject
            slots.MaxPair(Function(slot) slot.contents.WantPlayer(connectingPlayer.Name),
                               bestSlot,
                               bestMatch)
            If bestMatch < W3SlotContents.WantPlayerPriority.Accept Then
                Throw New InvalidOperationException("No slot available for player.")
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
                RemovePlayer(fakeHostPlayer, True, W3PlayerLeaveType.Disconnect, "Need player index for joining player.")
            Else
                'no indexes left, go away
                Throw New InvalidOperationException("No index space available for player.")
            End If
            freeIndexes.Remove(index)
            Contract.Assume(index > 0)
            Contract.Assume(index <= 12)

            'Reservation
            If bestMatch = W3SlotContents.WantPlayerPriority.AcceptReservation Then
                For Each player In bestSlot.contents.EnumPlayers
                    Contract.Assume(player IsNot Nothing)
                    RemovePlayer(player, True, W3PlayerLeaveType.Disconnect, "Reservation fulfilled")
                Next player
            End If

            'Create player object
            Dim newPlayer As W3Player = New W3Player(index, Me, connectingPlayer, Logger)
            bestSlot.contents = bestSlot.contents.TakePlayer(newPlayer)
            players.Add(newPlayer)

            'Greet new player
            newPlayer.QueueSendPacket(W3Packet.MakeGreet(newPlayer, index))
            For Each player In (From p In players Where p IsNot newPlayer AndAlso IsPlayerVisible(p))
                newPlayer.QueueSendPacket(W3Packet.MakeOtherPlayerJoined(player))
            Next player
            newPlayer.QueueSendPacket(W3Packet.MakeHostMapInfo(Map))
            If My.Resources.ProgramShowoff_f0name <> "" Then
                SendMessageTo(My.Resources.ProgramShowoff_f0name.Frmt(My.Resources.ProgramName), newPlayer)
            End If

            'Inform other players
            If IsPlayerVisible(newPlayer) Then
                For Each player In (From p In players Where p IsNot newPlayer)
                    player.QueueSendPacket(W3Packet.MakeOtherPlayerJoined(newPlayer, index))
                Next player
            End If

            'Inform bot
            ThrowPlayerEntered(newPlayer)
            Logger.Log("{0} has entered the game.".Frmt(newPlayer.name), LogMessageType.Positive)

            'Update state
            ChangedLobbyState()
            TryBeginAutoStart()
            If Server.settings.autoElevateUserName IsNot Nothing Then
                If newPlayer.name.ToUpperInvariant = Server.settings.autoElevateUserName.ToUpperInvariant Then
                    TryElevatePlayer(newPlayer.name)
                End If
            End If
            If Server.settings.isAutoStarted Then
                SendMessageTo("This is an automated game. {0}help for a list of commands.".Frmt(My.Settings.commandPrefix), newPlayer)
            End If

            Return newPlayer
        End Function
#End Region

#Region "Events"
        Private Sub ThrowPlayerEntered(ByVal new_player As W3Player)
            eventRef.QueueAction(Sub()
                                     RaiseEvent PlayerEntered(Me, new_player)
                                 End Sub)
        End Sub

        Private Sub OnDownloadSchedulerActions(ByVal started As List(Of TransferScheduler(Of Byte).TransferEndpoints),
                                               ByVal stopped As List(Of TransferScheduler(Of Byte).TransferEndpoints))
            ref.QueueAction(
                Sub()
                    'Start transfers
                    For Each e In started
                        'Find matching players
                        Dim src = FindPlayer(e.source)
                        Dim dst = FindPlayer(e.destination)
                        If dst Is Nothing Then  Continue For

                        'Apply
                        If e.source = LocalTransferClientKey Then
                            Logger.Log("Initiating map upload to {0}.".Frmt(dst.name), LogMessageType.Positive)
                            dst.IsGettingMapFromBot = True
                            dst.QueueBufferMap()
                        ElseIf src IsNot Nothing Then
                            Logger.Log("Initiating peer map transfer from {0} to {1}.".Frmt(src.name, dst.name), LogMessageType.Positive)
                            src.QueueSendPacket(W3Packet.MakeSetUploadTarget(dst.index, CUInt(Math.Max(0, dst.GetMapDownloadPosition))))
                            dst.QueueSendPacket(W3Packet.MakeSetDownloadSource(src.index))
                        End If
                    Next e

                    'Stop transfers
                    For Each e In stopped
                        'Find matching players
                        Dim src = FindPlayer(e.source)
                        Dim dst = FindPlayer(e.destination)
                        If dst Is Nothing Then  Continue For

                        'Apply
                        If e.source = LocalTransferClientKey Then
                            Logger.Log("Stopping map upload to {0}.".Frmt(dst.name), LogMessageType.Positive)
                            dst.IsGettingMapFromBot = False
                        ElseIf src IsNot Nothing Then
                            Logger.Log("Stopping peer map transfer from {0} to {1}.".Frmt(src.name, dst.name), LogMessageType.Positive)
                            src.QueueSendPacket(W3Packet.MakeOtherPlayerLeft(dst, W3PlayerLeaveType.Disconnect))
                            src.QueueSendPacket(W3Packet.MakeOtherPlayerJoined(dst))
                            dst.QueueSendPacket(W3Packet.MakeOtherPlayerLeft(src, W3PlayerLeaveType.Disconnect))
                            dst.QueueSendPacket(W3Packet.MakeOtherPlayerJoined(src))
                        End If
                    Next e
                End Sub
            )
        End Sub

        Private Sub OnScheduleDownloadsTick()
            DownloadScheduler.Update()
        End Sub

        Public Sub LobbyCatchRemovedPlayer(ByVal player As W3Player, ByVal slot As W3Slot)
            If slot Is Nothing OrElse slot.contents.PlayerIndex <> player.index Then
                freeIndexes.Add(player.index)
            End If
            DownloadScheduler.RemoveClient(player.index).MarkAnyExceptionAsHandled()
            If player IsNot fakeHostPlayer Then TryRestoreFakeHost()
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
            If state >= W3GameState.Loading Then Return

            Dim time As ModInt32 = Environment.TickCount()
            For Each player In players
                Contract.Assume(player IsNot Nothing)
                Dim pk = W3Packet.MakeLobbyState(player, Map, slots, time, Server.settings.isAdminGame)
                player.QueueSendPacket(pk)
            Next player
            TryBeginAutoStart()
        End Sub

        ''' <summary>Opens slots, closes slots and moves players around to try to match the desired team sizes.</summary>
        Private Sub TrySetTeamSizes(ByVal desiredTeamSizes As IList(Of Integer))
            Contract.Requires(desiredTeamSizes IsNot Nothing)
            If state > W3GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("Can't change team sizes after launch.")
            End If

            For repeat = 1 To 2
                Dim availableWellPlacedSlots = New List(Of W3Slot)
                Dim misplacedPlayerSlots = New List(Of W3Slot)
                Dim teamSizesLeft = desiredTeamSizes.ToArray()
                For Each slot In slots
                    If slot.team >= teamSizesLeft.Count Then Continue For

                    Select Case slot.contents.ContentType
                        Case SlotContentType.Computer
                            'computers slots shouldn't be affected

                        Case SlotContentType.Empty
                            If teamSizesLeft(slot.team) > 0 Then
                                teamSizesLeft(slot.team) -= 1
                                availableWellPlacedSlots.Add(slot)
                                slot.contents = New W3SlotContentsOpen(slot)
                            Else
                                slot.contents = New W3SlotContentsClosed(slot)
                            End If

                        Case SlotContentType.Player
                            If teamSizesLeft(slot.team) > 0 Then
                                teamSizesLeft(slot.team) -= 1
                            Else
                                misplacedPlayerSlots.Add(slot)
                            End If

                        Case Else
                            Throw slot.contents.ContentType.MakeImpossibleValueException
                    End Select
                Next slot

                'Swap misplaced players to wellplaced slots
                For i = 0 To Math.Min(availableWellPlacedSlots.Count, misplacedPlayerSlots.Count) - 1
                    Contract.Assume(availableWellPlacedSlots(i) IsNot Nothing)
                    Contract.Assume(misplacedPlayerSlots(i) IsNot Nothing)
                    SwapSlotContents(availableWellPlacedSlots(i), misplacedPlayerSlots(i))
                Next i
            Next repeat

            ChangedLobbyState()
        End Sub

        Private Sub ModifySlotContents(ByVal slotQuery As String,
                                       ByVal action As Action(Of W3Slot),
                                       Optional ByVal avoidPlayers As Boolean = False)
            Contract.Requires(slotQuery IsNot Nothing)
            Contract.Requires(action IsNot Nothing)

            If state >= W3GameState.CountingDown Then
                Throw New InvalidOperationException("Can't modify slots during launch.")
            End If

            Dim slot = FindMatchingSlot(slotQuery)
            If slot Is Nothing Then
                Throw New InvalidOperationException("No slot matching {0}".Frmt(slotQuery))
            End If

            If avoidPlayers AndAlso slot.contents.ContentType = SlotContentType.Player Then
                Throw New InvalidOperationException("Slot '{0}' contains a player.".Frmt(slotQuery))
            End If

            Call action(slot)
            ChangedLobbyState()
        End Sub
#End Region
#Region "Slot Contents"
        '''<summary>Opens the slot with the given index, unless the slot contains a player.</summary>
        Private Sub OpenSlot(ByVal slotid As String)
            Contract.Requires(slotid IsNot Nothing)
            ModifySlotContents(slotid,
                               Sub(slot)
                                   Contract.Assume(slot IsNot Nothing)
                                   slot.contents = New W3SlotContentsOpen(slot)
                               End Sub,
                               avoidPlayers:=True)
        End Sub

        '''<summary>Places a computer with the given difficulty in the slot with the given index, unless the slot contains a player.</summary>
        Private Sub ComputerizeSlot(ByVal slotid As String, ByVal cpu As W3Slot.ComputerLevel)
            Contract.Requires(slotid IsNot Nothing)
            ModifySlotContents(slotid,
                               Sub(slot)
                                   Contract.Assume(slot IsNot Nothing)
                                   slot.contents = New W3SlotContentsComputer(slot, cpu)
                               End Sub,
                               avoidPlayers:=True)
        End Sub

        '''<summary>Closes the slot with the given index, unless the slot contains a player.</summary>
        Private Sub CloseSlot(ByVal slotid As String)
            Contract.Requires(slotid IsNot Nothing)
            ModifySlotContents(slotid,
                               Sub(slot)
                                   Contract.Assume(slot IsNot Nothing)
                                   slot.contents = New W3SlotContentsClosed(slot)
                               End Sub,
                               avoidPlayers:=True)
        End Sub

        '''<summary>Reserves a slot for a player.</summary>
        Private Function ReserveSlot(ByVal slotid As String, ByVal username As String) As W3Player
            Contract.Requires(slotid IsNot Nothing)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of W3Player)() IsNot Nothing)
            If state >= W3GameState.CountingDown Then
                Throw New InvalidOperationException("Can't reserve slots after launch.")
            End If
            Dim slot = FindMatchingSlot(slotid)
            If slot Is Nothing Then Throw New InvalidOperationException("No slot matching {0}".Frmt(slotid))
            If slot.contents.ContentType = SlotContentType.Player Then
                Throw New InvalidOperationException("Slot '{0}' can't be reserved because it already contains a player.".Frmt(slotid))
            Else
                Return AddFakePlayer(username, slot)
            End If
        End Function

        Private Sub SwapSlotContents(ByVal query1 As String, ByVal query2 As String)
            Contract.Requires(query1 IsNot Nothing)
            Contract.Requires(query2 IsNot Nothing)
            If state > W3GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("Can't swap slots after launch.")
            End If
            Dim slot1 = FindMatchingSlot(query1)
            Dim slot2 = FindMatchingSlot(query2)
            If slot1 Is Nothing Then
                Throw New InvalidOperationException("No slot matching '{0}'.".Frmt(query1))
            ElseIf slot2 Is Nothing Then
                Throw New InvalidOperationException("No slot matching '{0}'.".Frmt(query2))
            ElseIf slot1 Is slot2 Then
                Throw New InvalidOperationException("Slot {0} is slot '{1}'.".Frmt(query1, query2))
            End If
            SwapSlotContents(slot1, slot2)
            ChangedLobbyState()
        End Sub
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
        Private Sub SetSlotColor(ByVal slotid As String, ByVal color As W3Slot.PlayerColor)
            Contract.Requires(slotid IsNot Nothing)
            If state > W3GameState.CountingDown Then
                Throw New InvalidOperationException("Can't change slot settings after launch.")
            End If

            Dim foundSlot = FindMatchingSlot(slotid)
            If foundSlot Is Nothing Then Throw New InvalidOperationException("No slot {0}".Frmt(slotid))

            Dim swapColorSlot = (From x In slots Where x.color = color).FirstOrDefault
            If swapColorSlot IsNot Nothing Then swapColorSlot.color = foundSlot.color
            foundSlot.color = color

            ChangedLobbyState()
        End Sub

        Private Sub SetSlotRace(ByVal slotid As String, ByVal race As W3Slot.Races)
            Contract.Requires(slotid IsNot Nothing)
            ModifySlotContents(slotid,
                               Sub(slot)
                                   slot.race = race
                               End Sub)
        End Sub

        Private Sub SetSlotTeam(ByVal slotid As String, ByVal team As Byte)
            Contract.Requires(slotid IsNot Nothing)
            ModifySlotContents(slotid,
                               Sub(slot)
                                   slot.team = team
                               End Sub)
        End Sub

        Private Sub SetSlotHandicap(ByVal slotid As String, ByVal handicap As Byte)
            Contract.Requires(slotid IsNot Nothing)
            ModifySlotContents(slotid,
                               Sub(slot)
                                   slot.handicap = handicap
                               End Sub)
        End Sub

        Private Sub SetSlotLocked(ByVal slotid As String, ByVal locked As W3Slot.Lock)
            Contract.Requires(slotid IsNot Nothing)
            ModifySlotContents(slotid,
                               Sub(slot)
                                   slot.locked = locked
                               End Sub)
        End Sub

        Private Sub SetAllSlotsLocked(ByVal locked As W3Slot.Lock)
            If state > W3GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("Can't lock slots after launch.")
            End If
            For Each slot In slots.ToList
                slot.locked = locked
            Next slot
        End Sub
#End Region

#Region "Networking"
        Private Sub ReceiveSetColor(ByVal player As W3Player, ByVal newColor As W3Slot.PlayerColor)
            Contract.Requires(player IsNot Nothing)
            Dim slot = FindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = W3Slot.Lock.Frozen Then Return '[no changes allowed]
            If Not slot.contents.Moveable Then Return '[slot is weird]
            If state >= W3GameState.Loading Then Return '[too late]
            If Not newColor.EnumValueIsDefined Then Return '[not a valid color]

            'check for duplicates
            For Each other_slot In slots.ToList()
                If other_slot.color = newColor Then
                    If Not Map.isMelee Then Return
                    If Not other_slot.contents.ContentType = SlotContentType.Empty Then Return
                    other_slot.color = slot.color
                    Exit For
                End If
            Next other_slot

            'change color
            slot.color = newColor
            ChangedLobbyState()
        End Sub
        Private Sub ReceiveSetRace(ByVal player As W3Player, ByVal newRace As W3Slot.Races)
            Contract.Requires(player IsNot Nothing)
            Dim slot = FindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = W3Slot.Lock.Frozen Then Return '[no changes allowed]
            If Not slot.contents.Moveable Then Return '[slot is weird]
            If state >= W3GameState.Loading Then Return '[too late]
            If Not newRace.EnumValueIsDefined OrElse newRace = W3Slot.Races.Unlocked Then Return '[not a valid race]

            'Perform
            slot.race = newRace
            ChangedLobbyState()
        End Sub
        Private Sub ReceiveSetHandicap(ByVal player As W3Player, ByVal new_handicap As Byte)
            Contract.Requires(player IsNot Nothing)
            Dim slot = FindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = W3Slot.Lock.Frozen Then Return '[no changes allowed]
            If Not slot.contents.Moveable Then Return '[slot is weird]
            If state >= W3GameState.CountingDown Then Return '[too late]

            'Perform
            Select Case new_handicap
                Case 50, 60, 70, 80, 90, 100
                    slot.handicap = new_handicap
                Case Else
                    Return '[invalid handicap]
            End Select

            ChangedLobbyState()
        End Sub
        Private Sub ReceiveSetTeam(ByVal player As W3Player, ByVal new_team As Byte)
            Contract.Requires(player IsNot Nothing)
            Dim slot = FindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked <> W3Slot.Lock.Unlocked Then Return '[no teams changes allowed]
            If new_team > W3Slot.ObserverTeamIndex Then Return '[invalid value]
            If Not slot.contents.Moveable Then Return '[slot is weird]
            If state >= W3GameState.Loading Then Return '[too late]
            If new_team = W3Slot.ObserverTeamIndex Then
                Select Case Server.settings.header.Map.observers
                    Case GameObserverOption.FullObservers, GameObserverOption.Referees
                        '[fine; continue]
                    Case Else
                        Return '[obs not enabled; invalid value]
                End Select
            ElseIf Not Map.isMelee And new_team >= Map.numForces Then
                Return '[invalid team]
            ElseIf Map.isMelee And new_team >= Map.NumPlayerSlots Then
                Return '[invalid team]
            End If

            'Perform
            If Map.isMelee Then
                'set slot to target team
                slot.team = new_team
            Else
                'swap with next open slot from target team
                For offset_mod = 0 To slots.Count - 1
                    Dim nextIndex = (slot.index + offset_mod) Mod slots.Count
                    Contract.Assume(nextIndex >= 0)
                    Dim nextSlot = slots(nextIndex)
                    Contract.Assume(nextSlot IsNot Nothing)
                    If nextSlot.team = new_team AndAlso nextSlot.contents.WantPlayer(player.name) >= W3SlotContents.WantPlayerPriority.Accept Then
                        SwapSlotContents(nextSlot, slot)
                        Exit For
                    End If
                Next offset_mod
            End If

            ChangedLobbyState()
        End Sub
#End Region

#Region "Interface"
        Public ReadOnly Property DownloadScheduler() As TransferScheduler(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of TransferScheduler(Of Byte))() IsNot Nothing)
                Return _downloadScheduler
            End Get
        End Property

        Public Function QueueUpdatedGameState() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(AddressOf ChangedLobbyState)
        End Function

        Public Function QueueOpenSlot(ByVal query As String) As IFuture
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query IsNot Nothing)
                                       OpenSlot(query)
                                   End Sub)
        End Function
        Public Function QueueCloseSlot(ByVal query As String) As IFuture
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query IsNot Nothing)
                                       CloseSlot(query)
                                   End Sub)
        End Function
        Public Function QueueReserveSlot(ByVal query As String,
                                         ByVal userName As String) As IFuture
            Contract.Requires(query IsNot Nothing)
            Contract.Requires(userName IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query IsNot Nothing)
                                       Contract.Assume(userName IsNot Nothing)
                                       ReserveSlot(query, userName)
                                   End Sub)
        End Function
        Public Function QueueSwapSlotContents(ByVal query1 As String, ByVal query2 As String) As IFuture
            Contract.Requires(query1 IsNot Nothing)
            Contract.Requires(query2 IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query1 IsNot Nothing)
                                       Contract.Assume(query2 IsNot Nothing)
                                       SwapSlotContents(query1, query2)
                                   End Sub)
        End Function

        Public Function QueueSetSlotCpu(ByVal query As String, ByVal newCpuLevel As W3Slot.ComputerLevel) As IFuture
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query IsNot Nothing)
                                       ComputerizeSlot(query, newCpuLevel)
                                   End Sub)
        End Function
        Public Function QueueSetSlotLocked(ByVal query As String, ByVal newLockState As W3Slot.Lock) As IFuture
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query IsNot Nothing)
                                       SetSlotLocked(query, newLockState)
                                   End Sub)
        End Function
        Public Function QueueSetAllSlotsLocked(ByVal newLockState As W3Slot.Lock) As IFuture
            Return ref.QueueAction(Sub() SetAllSlotsLocked(newLockState))
        End Function
        Public Function QueueSetSlotHandicap(ByVal query As String, ByVal newHandicap As Byte) As IFuture
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query IsNot Nothing)
                                       SetSlotHandicap(query, newHandicap)
                                   End Sub)
        End Function
        Public Function QueueSetSlotTeam(ByVal query As String, ByVal newTeam As Byte) As IFuture
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query IsNot Nothing)
                                       SetSlotTeam(query, newTeam)
                                   End Sub)
        End Function
        Public Function QueueSetSlotRace(ByVal query As String, ByVal newRace As W3Slot.Races) As IFuture
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query IsNot Nothing)
                                       SetSlotRace(query, newRace)
                                   End Sub)
        End Function
        Public Function QueueSetSlotColor(ByVal query As String, ByVal newColor As W3Slot.PlayerColor) As IFuture
            Contract.Requires(query IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(query IsNot Nothing)
                                       SetSlotColor(query, newColor)
                                   End Sub)
        End Function

        Public Function QueueTryAddPlayer(ByVal newPlayer As W3ConnectingPlayer) As IFuture(Of W3Player)
            Contract.Requires(newPlayer IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Player))() IsNot Nothing)
            Return ref.QueueFunc(Function() AddPlayer(newPlayer))
        End Function
        Public Function QueueSetPlayerVoteToStart(ByVal name As String,
                                                  ByVal wantsToStart As Boolean) As IFuture
            Contract.Requires(name IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(name IsNot Nothing)
                                       SetPlayerVoteToStart(name, wantsToStart)
                                   End Sub)
        End Function
        Public Function QueueStartCountdown() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Function() TryStartCountdown())
        End Function
        Public Function QueueTrySetTeamSizes(ByVal sizes As IList(Of Integer)) As IFuture
            Contract.Requires(sizes IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(sizes IsNot Nothing)
                                       TrySetTeamSizes(sizes)
                                   End Sub)
        End Function
#End Region
    End Class
End Namespace
