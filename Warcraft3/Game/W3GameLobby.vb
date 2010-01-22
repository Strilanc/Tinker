Namespace WC3
    Partial Public NotInheritable Class Game
        Public Const LocalTransferClientKey As Byte = 255

        Private _downloadScheduler As TransferScheduler(Of Byte)
        Private ReadOnly downloadTimer As New Timers.Timer(interval:=250)
        Private ReadOnly freeIndexes As New List(Of PID)
        Private ReadOnly slotStateUpdateThrottle As New Throttle(cooldown:=250.Milliseconds, clock:=New SystemClock())
        Private ReadOnly updateEventThrottle As New Throttle(cooldown:=100.Milliseconds, clock:=New SystemClock())

        Private Sub LobbyNew()
            Contract.Ensures(DownloadScheduler IsNot Nothing)

            Dim rate As FiniteDouble = 10 * 1024 / 1000
            Dim switchTime As FiniteDouble = 3000
            Dim size As FiniteDouble = New FiniteDouble(Map.FileSize)
            Me._downloadScheduler = New TransferScheduler(Of Byte)(typicalRate:=rate,
                                                                   typicalSwitchTime:=3.Seconds,
                                                                   filesize:=size,
                                                                   clock:=_clock)
            AddHandler DownloadScheduler.Actions, Sub(started, stopped) inQueue.QueueAction(Sub() OnDownloadSchedulerActions(started, stopped))
            AddHandler downloadTimer.Elapsed, Sub() DownloadScheduler.Update()

            InitCreateSlots()
            InitProcessArguments()
            InitDownloads()
            downloadTimer.Start()
            inQueue.QueueAction(Sub() TryRestoreFakeHost())
        End Sub
        Private Sub InitCreateSlots()
            'create player slots
            For i = 0 To map.slots.Count - 1
                Dim baseSlot = Map.Slots(i)
                Contract.Assume(baseSlot IsNot Nothing)
                Dim slot = New Slot(CByte(i), Map.IsMelee)
                slot.Contents = baseSlot.Contents.Clone(slot)
                slot.color = baseSlot.color
                slot.race = baseSlot.race
                slot.team = baseSlot.team
                slot.locked = settings.defaultSlotLockState
                slots.Add(slot)
                freeIndexes.Add(New PID(CByte(i + 1)))
            Next i

            'create observer slots
            Select Case settings.GameDescription.GameStats.observers
                Case GameObserverOption.FullObservers, GameObserverOption.Referees
                    For i = Map.NumPlayerSlots To 12 - 1
                        Dim slot = New Slot(CByte(i), Map.IsMelee)
                        slot.color = CType(slot.ObserverTeamIndex, Slot.PlayerColor)
                        slot.Team = slot.ObserverTeamIndex
                        slot.race = slot.Races.Random
                        slots.Add(slot)
                        freeIndexes.Add(New PID(CByte(i + 1)))
                    Next i
            End Select
        End Sub
        Private Sub InitProcessArguments()
            If settings.useMultiObs Then
                Contract.Assume(slots.Count = 12)
                Contract.Assume(freeIndexes.Count > 0)
                If Map.NumPlayerSlots <= 10 Then
                    For i = Map.NumPlayerSlots To 10 - 1
                        Contract.Assume(slots(i) IsNot Nothing)
                        slots(i).Contents = New SlotContentsClosed(slots(i))
                    Next i
                    Dim playerIndex = freeIndexes(0)
                    freeIndexes.Remove(playerIndex)
                    Contract.Assume(slots(10) IsNot Nothing)
                    Contract.Assume(slots(11) IsNot Nothing)
                    AddFakePlayer("# multi obs", slots(10))
                    SetupCoveredSlot(slots(10), slots(11), playerIndex)
                End If
            End If
            TrySetTeamSizes(settings.TeamSizes)
            For Each reservation In settings.reservations
                ReserveSlot(reservation)
            Next reservation
            If settings.ObserverCount > 0 Then
                Dim n = settings.ObserverCount
                For Each slot In Me.slots
                    If slot.Team = slot.ObserverTeamIndex Then
                        If n <= 0 Then CloseSlot(slot.MatchableId)
                        n -= 1
                    End If
                Next slot
            ElseIf settings.ObserverReservations.Count > 0 Then
                For Each reservation In settings.ObserverReservations
                    ReserveSlot(reservation, "obs")
                Next reservation
                For Each slot In Me.slots
                    If slot.Team = slot.ObserverTeamIndex AndAlso slot.Contents.ContentType <> SlotContentType.Player Then
                        CloseSlot(slot.MatchableId)
                    End If
                Next slot
            End If
        End Sub
        Private Sub InitDownloads()
            If settings.allowUpload AndAlso Map.fileAvailable Then
                Me.DownloadScheduler.AddClient(LocalTransferClientKey, completed:=True)
            End If
        End Sub

        Public ReadOnly Property DownloadScheduler() As TransferScheduler(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of TransferScheduler(Of Byte))() IsNot Nothing)
                Return _downloadScheduler
            End Get
        End Property

#Region "Advancing State"
        '''<summary>Autostarts the countdown if autostart is enabled and the game stays full for awhile.</summary>
        Private Function TryBeginAutoStart() As Boolean
            'Sanity check
            If Not settings.IsAutoStarted Then Return False
            If CountFreeSlots() > 0 Then Return False
            If state >= GameState.PreCounting Then Return False
            If (From player In _players Where Not player.isFake And player.AdvertisedDownloadPercent <> 100).Any Then
                Return False
            End If
            ChangeState(GameState.PreCounting)

            'Give people a few seconds to realize the game is full before continuing
            Call _clock.AsyncWait(3.Seconds).QueueCallWhenReady(inQueue,
                Sub()
                    If state <> GameState.PreCounting Then Return
                    If Not settings.IsAutoStarted OrElse CountFreeSlots() > 0 Then
                        ChangeState(GameState.AcceptingPlayers)
                    Else
                        TryStartCountdown()
                    End If
                End Sub
            )
            Return True
        End Function

        '''<summary>Starts the countdown to launch.</summary>
        Private Function TryStartCountdown() As Boolean
            If state >= GameState.CountingDown Then Return False
            If (From p In _players Where Not p.isFake AndAlso p.AdvertisedDownloadPercent <> 100).Any Then
                Return False
            End If

            ChangeState(GameState.CountingDown)
            flagHasPlayerLeft = False

            'Perform countdown
            Dim continueCountdown As Action(Of Integer)
            continueCountdown = Sub(ticksLeft)
                                    If state <> GameState.CountingDown Then Return

                                    If flagHasPlayerLeft Then 'abort countdown
                                        BroadcastMessage("Countdown Aborted", messageType:=LogMessageType.Negative)
                                        TryRestoreFakeHost()
                                        ChangeState(GameState.AcceptingPlayers)
                                        ChangedLobbyState()
                                    ElseIf ticksLeft > 0 Then 'continue ticking
                                        BroadcastMessage("Starting in {0}...".Frmt(ticksLeft), messageType:=LogMessageType.Positive)
                                        _clock.AsyncWait(1.Seconds).QueueCallWhenReady(inQueue, Sub() continueCountdown(ticksLeft - 1))
                                    Else 'start
                                        StartLoading()
                                    End If
                                End Sub
            Call _clock.AsyncWait(1.Seconds).QueueCallWhenReady(inQueue, Sub() continueCountdown(5))

            Return True
        End Function
        Public Function QueueStartCountdown() As IFuture(Of Boolean)
            Contract.Ensures(Contract.Result(Of IFuture(Of Boolean))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() TryStartCountdown())
        End Function

        '''<summary>Launches the game, sending players to the loading screen.</summary>
        Private Sub StartLoading()
            If state >= GameState.Loading Then Return

            'Remove fake players
            For Each player In (From p In _players.ToList Where p.isFake)
                Contract.Assume(player IsNot Nothing)
                Dim slot = TryFindPlayerSlot(player)
                If slot Is Nothing OrElse slot.Contents.Moveable Then
                    RemovePlayer(player, True, PlayerLeaveType.Disconnect, "Fake players removed before loading")
                End If
            Next player

            'Encode HCL data
            Dim useableSlots = (From slot In slots Where slot.Contents.Moveable AndAlso slot.Contents.ContentType <> SlotContentType.Empty).ToArray
            Dim encodedHandicaps = settings.EncodedHCLMode((From slot In useableSlots Select slot.handicap).ToArray)
            For i = 0 To encodedHandicaps.Length - 1
                Contract.Assume(useableSlots(i) IsNot Nothing)
                useableSlots(i).handicap = encodedHandicaps(i)
            Next i
            SendLobbyState()

            ChangeState(GameState.Loading)
            downloadTimer.Stop()
            LoadScreenStart()
        End Sub
#End Region

#Region "Players"
        Private Sub SetPlayerVoteToStart(ByVal name As InvariantString, ByVal val As Boolean)
            If Not settings.IsAutoStarted Then Throw New InvalidOperationException("Game is not set to start automatically.")
            Dim p = TryFindPlayer(name)
            If p Is Nothing Then Throw New InvalidOperationException("No player found with the name '{0}'.".Frmt(name))
            p.hasVotedToStart = val
            If Not val Then Return

            Dim numPlayers = (From q In _players Where Not q.isFake).Count
            Dim numInFavor = (From q In _players Where Not q.isFake AndAlso q.hasVotedToStart).Count
            If numPlayers >= 2 And numInFavor * 3 >= numPlayers * 2 Then
                TryStartCountdown()
            End If
        End Sub
        Public Function QueueSetPlayerVoteToStart(ByVal name As InvariantString,
                                                  ByVal wantsToStart As Boolean) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SetPlayerVoteToStart(name, wantsToStart))
        End Function

        Private Function AddFakePlayer(ByVal name As InvariantString,
                                       Optional ByVal newSlot As Slot = Nothing) As Player
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            If state > GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("No longer accepting players.")
            ElseIf freeIndexes.Count <= 0 Then
                If fakeHostPlayer IsNot Nothing Then
                    RemovePlayer(fakeHostPlayer, True, PlayerLeaveType.Disconnect, "Need player index for new fake player")
                Else
                    Throw New InvalidOperationException("No space available for fake player.")
                End If
            End If
            Contract.Assume(freeIndexes.Count > 0)

            'Assign index
            Dim index = freeIndexes(0)
            freeIndexes.Remove(index)

            'Make player
            Dim newPlayer As Player = New Player(index, settings, _downloadScheduler, name, Logger)
            If newSlot IsNot Nothing Then
                newSlot.Contents = New SlotContentsPlayer(newSlot, newPlayer)
            End If
            _players.Add(newPlayer)

            'Inform other players
            For Each player In _players
                Contract.Assume(player IsNot Nothing)
                player.QueueSendPacket(Protocol.MakeOtherPlayerJoined(newPlayer))
            Next player

            'Inform bot
            Logger.Log("{0} has been placed in the game.".Frmt(newPlayer.Name), LogMessageType.Positive)

            'Update state
            ChangedLobbyState()
            Return newPlayer
        End Function
        Private Function TryRestoreFakeHost() As Player
            If fakeHostPlayer IsNot Nothing Then  Return Nothing
            If state > GameState.AcceptingPlayers Then  Return Nothing

            Dim name = My.Settings.ingame_name
            Contract.Assume(name IsNot Nothing)
            Try
                fakeHostPlayer = AddFakePlayer(name)
                Return fakeHostPlayer
            Catch ex As InvalidOperationException
                Return Nothing
            End Try
        End Function

        Private Function AddPlayer(ByVal connectingPlayer As W3ConnectingPlayer) As Player
            Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            If state > GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("No longer accepting players.")
            ElseIf Not connectingPlayer.Socket.Connected Then
                Throw New InvalidOperationException("Player isn't connected.")
            End If

            'Assign slot
            Dim bestSlot As Slot = Nothing
            Dim bestMatch = SlotContents.WantPlayerPriority.Filled
            slots.MaxPair(Function(slot) slot.Contents.WantPlayer(connectingPlayer.Name),
                          outElement:=bestSlot,
                          outImage:=bestMatch)
            If bestMatch < SlotContents.WantPlayerPriority.Open Then
                Throw New InvalidOperationException("No slot available for player.")
            End If
            Contract.Assume(bestSlot IsNot Nothing)

            'Assign index
            Dim pid As PID
            If bestMatch = SlotContents.WantPlayerPriority.Reserved Then
                'the player has a reserved slot and index
                pid = bestSlot.Contents.PlayerIndex.Value
                For Each player In bestSlot.Contents.EnumPlayers
                    Contract.Assume(player IsNot Nothing)
                    RemovePlayer(player, wasExpected:=True, leaveType:=PlayerLeaveType.Disconnect, reason:="Reservation fulfilled")
                Next player
                If fakeHostPlayer IsNot Nothing AndAlso fakeHostPlayer.PID = pid Then
                    RemovePlayer(fakeHostPlayer, True, PlayerLeaveType.Disconnect, "Need player index for joining player.")
                End If
                Contract.Assume(freeIndexes.Contains(pid))
            ElseIf bestSlot.Contents.PlayerIndex IsNot Nothing Then
                'the slot requires the player to take a specific index
                pid = bestSlot.Contents.PlayerIndex.Value
            ElseIf freeIndexes.Count > 0 Then
                'there is a player index available
                pid = freeIndexes(0)
            ElseIf fakeHostPlayer IsNot Nothing Then
                'the only player index left belongs to the fake host
                pid = fakeHostPlayer.PID
                RemovePlayer(fakeHostPlayer, True, PlayerLeaveType.Disconnect, "Need player index for joining player.")
                Contract.Assume(freeIndexes.Contains(pid))
            Else
                'no indexes left, go away
                Throw New InvalidOperationException("No index space available for player.")
            End If
            freeIndexes.Remove(pid)

            'Create player object
            Dim newPlayer = New Player(pid, settings, _downloadScheduler, connectingPlayer, _clock, Logger)
            bestSlot.Contents = bestSlot.Contents.TakePlayer(newPlayer)
            _players.Add(newPlayer)

            'Greet new player
            newPlayer.QueueSendPacket(Protocol.MakeGreet(newPlayer.RemoteEndPoint, newPlayer.PID))
            For Each player In (From p In _players Where p IsNot newPlayer AndAlso IsPlayerVisible(p))
                newPlayer.QueueSendPacket(Protocol.MakeOtherPlayerJoined(player))
            Next player
            newPlayer.QueueSendPacket(Protocol.MakeHostMapInfo(Map))

            'Inform other players
            If IsPlayerVisible(newPlayer) Then
                For Each player In (From p In _players Where p IsNot newPlayer)
                    player.QueueSendPacket(Protocol.MakeOtherPlayerJoined(newPlayer))
                Next player
            End If

            'Inform bot
            Logger.Log("{0} has entered the game.".Frmt(newPlayer.Name), LogMessageType.Positive)

            'Update state
            ChangedLobbyState()
            TryBeginAutoStart()
            If settings.AutoElevateUserName IsNot Nothing Then
                If newPlayer.Name = settings.AutoElevateUserName.Value Then
                    ElevatePlayer(newPlayer.Name)
                End If
            End If
            If settings.Greeting <> "" Then
                Logger.Log("Greeted {0}".Frmt(newPlayer.Name), LogMessageType.Positive)
                SendMessageTo(message:=settings.Greeting, player:=newPlayer, display:=False)
            End If

            AddHandler newPlayer.ReceivedRequestDropLaggers, Sub() QueueDropLagger()
            AddHandler newPlayer.ReceivedGameAction, AddressOf QueueReceiveGameAction
            AddHandler newPlayer.ReceivedGameData, AddressOf QueueGameData
            AddHandler newPlayer.Disconnected, AddressOf QueueRemovePlayer
            AddHandler newPlayer.ReceivedReady, AddressOf QueueReceiveReady
            AddHandler newPlayer.SuperficialStateUpdated, Sub() QueueThrowUpdated()
            AddHandler newPlayer.StateUpdated, Sub() inQueue.QueueAction(AddressOf ChangedLobbyState)
            AddHandler newPlayer.ReceivedNonGameAction, AddressOf QueueReceiveNonGameAction
            AddHandler newPlayer.WantMapSender, Sub(sender) QueueGetFakeHostPlayer.CallOnValueSuccess(
                                                        Sub(value) sender.GiveMapSender(If(value Is Nothing, Nothing, value.PID))
                                                    )

            Return newPlayer
        End Function
        Public Function QueueTryAddPlayer(ByVal newPlayer As W3ConnectingPlayer) As IFuture(Of Player)
            Contract.Requires(newPlayer IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AddPlayer(newPlayer))
        End Function
#End Region

#Region "Events"
        Private Sub OnDownloadSchedulerActions(ByVal started As List(Of TransferScheduler(Of Byte).TransferEndpoints),
                                               ByVal stopped As List(Of TransferScheduler(Of Byte).TransferEndpoints))
            Contract.Requires(started IsNot Nothing)
            Contract.Requires(stopped IsNot Nothing)
            'Start transfers
            For Each e In started
                Contract.Assume(e IsNot Nothing)
                Contract.Assume(e.destination > 0)
                Contract.Assume(e.destination <= 12)

                Dim dst = TryFindPlayer(New PID(e.destination))
                If dst Is Nothing Then Continue For
                If e.source = LocalTransferClientKey Then
                    Logger.Log("Initiating map upload to {0}.".Frmt(dst.Name), LogMessageType.Positive)
                    dst.IsGettingMapFromBot = True
                    dst.QueueBufferMap()
                Else
                    Contract.Assume(e.source > 0)
                    Contract.Assume(e.source <= 12)
                    Dim src = TryFindPlayer(New PID(e.source))
                    If src Is Nothing Then Continue For
                    Logger.Log("Initiating peer map transfer from {0} to {1}.".Frmt(src.Name, dst.Name), LogMessageType.Positive)
                    src.QueueSendPacket(Protocol.MakeSetUploadTarget(dst.PID, CUInt(Math.Max(0, dst.GetMapDownloadPosition))))
                    dst.QueueSendPacket(Protocol.MakeSetDownloadSource(src.PID))
                End If
            Next e

            'Stop transfers
            For Each e In stopped
                Contract.Assume(e IsNot Nothing)
                Contract.Assume(e.destination > 0)
                Contract.Assume(e.destination <= 12)

                Dim dst = TryFindPlayer(New PID(e.destination))
                If dst Is Nothing Then Continue For
                If e.source = LocalTransferClientKey Then
                    Logger.Log("Stopping map upload to {0}.".Frmt(dst.Name), LogMessageType.Positive)
                    dst.IsGettingMapFromBot = False
                Else
                    Contract.Assume(e.source > 0)
                    Contract.Assume(e.source <= 12)
                    Dim src = TryFindPlayer(New PID(e.source))
                    If src Is Nothing Then Continue For
                    src.QueueSendPacket(Protocol.MakeOtherPlayerJoined(dst))
                    dst.QueueSendPacket(Protocol.MakeOtherPlayerLeft(src.PID, PlayerLeaveType.Disconnect))
                    dst.QueueSendPacket(Protocol.MakeOtherPlayerJoined(src))
                End If
            Next e
        End Sub

        Public Sub LobbyCatchRemovedPlayer(ByVal player As Player, ByVal slot As Slot)
            Contract.Requires(player IsNot Nothing)

            If slot Is Nothing OrElse slot.Contents.PlayerIndex Is Nothing Then
                freeIndexes.Add(player.PID)
            End If
            DownloadScheduler.RemoveClient(player.PID.Index).SetHandled()
            If player IsNot fakeHostPlayer Then TryRestoreFakeHost()
            ChangedLobbyState()
        End Sub
#End Region

#Region "Slots"
        '''<summary>Broadcasts new game state to players, and throws the updated event.</summary>
        Private Sub ChangedLobbyState()
            ThrowUpdated()

            'Don't let update rate to clients become too high
            slotStateUpdateThrottle.SetActionToRun(Sub() inQueue.QueueAction(AddressOf SendLobbyState))
        End Sub
        Private Sub SendLobbyState()
            If state >= GameState.Loading Then Return

            Dim randomSeed As ModInt32 = Environment.TickCount()
            For Each player In _players
                Contract.Assume(player IsNot Nothing)
                player.QueueSendPacket(Protocol.MakeLobbyState(player, Map, slots, randomSeed, settings.IsAdminGame))
            Next player
            TryBeginAutoStart()
        End Sub

        '''<summary>Opens slots, closes slots and moves players around to try to match the desired team sizes.</summary>
        Private Sub TrySetTeamSizes(ByVal desiredTeamSizes As IEnumerable(Of Integer))
            Contract.Requires(desiredTeamSizes IsNot Nothing)
            If state > GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("Can't change team sizes after launch.")
            End If

            For repeat = 1 To 2
                Dim availableWellPlacedSlots = New List(Of Slot)
                Dim misplacedPlayerSlots = New List(Of Slot)
                Dim teamSizesLeft = desiredTeamSizes.ToArray()
                For Each slot In slots
                    Contract.Assume(slot IsNot Nothing)
                    If slot.Team >= teamSizesLeft.Count Then Continue For

                    Select Case slot.Contents.ContentType
                        Case SlotContentType.Computer
                            'computers slots shouldn't be affected

                        Case SlotContentType.Empty
                            If teamSizesLeft(slot.Team) > 0 Then
                                teamSizesLeft(slot.Team) -= 1
                                availableWellPlacedSlots.Add(slot)
                                slot.Contents = New SlotContentsOpen(slot)
                            Else
                                slot.Contents = New SlotContentsClosed(slot)
                            End If

                        Case SlotContentType.Player
                            If teamSizesLeft(slot.Team) > 0 Then
                                teamSizesLeft(slot.Team) -= 1
                            Else
                                misplacedPlayerSlots.Add(slot)
                            End If

                        Case Else
                            Throw slot.Contents.ContentType.MakeImpossibleValueException
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
        Public Function QueueTrySetTeamSizes(ByVal sizes As IList(Of Integer)) As IFuture
            Contract.Requires(sizes IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() TrySetTeamSizes(sizes))
        End Function

        Private Sub ModifySlotContents(ByVal slotQuery As InvariantString,
                                       ByVal action As Action(Of Slot),
                                       Optional ByVal avoidPlayers As Boolean = False)
            Contract.Requires(action IsNot Nothing)

            If state >= GameState.CountingDown Then
                Throw New InvalidOperationException("Can't modify slots during launch.")
            End If

            Dim slot = FindMatchingSlot(slotQuery)
            If avoidPlayers AndAlso slot.Contents.ContentType = SlotContentType.Player Then
                Throw New InvalidOperationException("Slot '{0}' contains a player.".Frmt(slotQuery))
            End If

            Call action(slot)
            ChangedLobbyState()
        End Sub
#End Region
#Region "Slot Contents"
        '''<summary>Opens the slot with the given index, unless the slot contains a player.</summary>
        Private Sub OpenSlot(ByVal slotQuery As InvariantString)
            ModifySlotContents(slotQuery,
                               Sub(slot) slot.Contents = New SlotContentsOpen(slot),
                               avoidPlayers:=True)
        End Sub
        Public Function QueueOpenSlot(ByVal slotQuery As InvariantString) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() OpenSlot(slotQuery))
        End Function

        '''<summary>Places a computer with the given difficulty in the slot with the given index, unless the slot contains a player.</summary>
        Private Sub ComputerizeSlot(ByVal slotQuery As InvariantString, ByVal cpu As Slot.ComputerLevel)
            ModifySlotContents(slotQuery,
                               Sub(slot) slot.Contents = New SlotContentsComputer(slot, cpu),
                               avoidPlayers:=True)
        End Sub
        Public Function QueueSetSlotCpu(ByVal slotQuery As InvariantString, ByVal newCpuLevel As Slot.ComputerLevel) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() ComputerizeSlot(slotQuery, newCpuLevel))
        End Function

        '''<summary>Closes the slot with the given index, unless the slot contains a player.</summary>
        Private Sub CloseSlot(ByVal slotQuery As InvariantString)
            ModifySlotContents(slotQuery,
                               Sub(slot) slot.Contents = New SlotContentsClosed(slot),
                               avoidPlayers:=True)
        End Sub
        Public Function QueueCloseSlot(ByVal slotQuery As InvariantString) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() CloseSlot(slotQuery))
        End Function

        '''<summary>Reserves a slot for a player.</summary>
        Private Function ReserveSlot(ByVal userName As InvariantString,
                                     Optional ByVal slotQuery As InvariantString? = Nothing) As Player
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)
            If state >= GameState.CountingDown Then
                Throw New InvalidOperationException("Can't reserve slots after launch.")
            End If
            Dim slot As Slot
            If slotQuery Is Nothing Then
                slot = (From s In slots Where s.Contents.WantPlayer(Nothing) = SlotContents.WantPlayerPriority.Open).FirstOrDefault
                If slot Is Nothing Then Throw New InvalidOperationException("No available slot.")
            Else
                slot = FindMatchingSlot(slotQuery.Value)
            End If
            If slot.Contents.ContentType = SlotContentType.Player Then
                Throw New InvalidOperationException("Slot '{0}' can't be reserved because it already contains a player.".Frmt(slotQuery))
            Else
                Return AddFakePlayer(userName, slot)
            End If
        End Function
        Public Function QueueReserveSlot(ByVal userName As InvariantString,
                                         Optional ByVal slotQuery As InvariantString? = Nothing) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() ReserveSlot(userName, slotQuery))
        End Function

        Private Sub SwapSlotContents(ByVal slotQuery1 As InvariantString,
                                     ByVal slotQuery2 As InvariantString)
            If state > GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("Can't swap slots after launch.")
            End If
            Dim slot1 = FindMatchingSlot(slotQuery1)
            Dim slot2 = FindMatchingSlot(slotQuery2)
            If slot1 Is slot2 Then Throw New InvalidOperationException("Slot {0} is slot '{1}'.".Frmt(slotQuery1, slotQuery2))
            SwapSlotContents(slot1, slot2)
            ChangedLobbyState()
        End Sub
        Private Sub SwapSlotContents(ByVal slot1 As Slot, ByVal slot2 As Slot)
            Contract.Requires(slot1 IsNot Nothing)
            Contract.Requires(slot2 IsNot Nothing)
            Dim t = slot1.Contents.Clone(slot2)
            slot1.Contents = slot2.Contents.Clone(slot1)
            slot2.Contents = t
            ChangedLobbyState()
        End Sub
        Public Function QueueSwapSlotContents(ByVal slotQuery1 As InvariantString,
                                              ByVal slotQuery2 As InvariantString) As IFuture
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SwapSlotContents(slotQuery1, slotQuery2))
        End Function
#End Region
#Region "Slot States"
        Private Sub SetSlotColor(ByVal slotQuery As InvariantString, ByVal color As Slot.PlayerColor)
            If state > GameState.CountingDown Then
                Throw New InvalidOperationException("Can't change slot settings after launch.")
            End If

            Dim slot = FindMatchingSlot(slotQuery)
            Dim swapColorSlot = (From x In slots Where x.color = color).FirstOrDefault
            If swapColorSlot IsNot Nothing Then swapColorSlot.color = slot.color
            slot.color = color

            ChangedLobbyState()
        End Sub
        Public Function QueueSetSlotColor(ByVal slotQuery As InvariantString, ByVal newColor As Slot.PlayerColor) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SetSlotColor(slotQuery, newColor))
        End Function

        Private Sub SetSlotRace(ByVal slotQuery As InvariantString, ByVal race As Slot.Races)
            ModifySlotContents(slotQuery, Sub(slot) slot.race = race)
        End Sub
        Public Function QueueSetSlotRace(ByVal slotQuery As InvariantString, ByVal newRace As Slot.Races) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SetSlotRace(slotQuery, newRace))
        End Function

        Private Sub SetSlotTeam(ByVal slotQuery As InvariantString, ByVal team As Byte)
            ModifySlotContents(slotQuery, Sub(slot) slot.Team = team)
        End Sub
        Public Function QueueSetSlotTeam(ByVal slotQuery As InvariantString, ByVal newTeam As Byte) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SetSlotTeam(slotQuery, newTeam))
        End Function

        Private Sub SetSlotHandicap(ByVal slotQuery As InvariantString, ByVal handicap As Byte)
            ModifySlotContents(slotQuery, Sub(slot) slot.handicap = handicap)
        End Sub
        Public Function QueueSetSlotHandicap(ByVal slotQuery As InvariantString, ByVal newHandicap As Byte) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SetSlotHandicap(slotQuery, newHandicap))
        End Function

        Private Sub SetSlotLocked(ByVal slotQuery As InvariantString, ByVal locked As Slot.Lock)
            ModifySlotContents(slotQuery, Sub(slot) slot.locked = locked)
        End Sub
        Public Function QueueSetSlotLocked(ByVal slotQuery As InvariantString, ByVal newLockState As Slot.Lock) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SetSlotLocked(slotQuery, newLockState))
        End Function

        Private Sub SetAllSlotsLocked(ByVal locked As Slot.Lock)
            If state > GameState.AcceptingPlayers Then
                Throw New InvalidOperationException("Can't lock slots after launch.")
            End If
            For Each slot In slots.ToList
                Contract.Assume(slot IsNot Nothing)
                slot.locked = locked
            Next slot
        End Sub
        Public Function QueueSetAllSlotsLocked(ByVal newLockState As Slot.Lock) As IFuture
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SetAllSlotsLocked(newLockState))
        End Function
#End Region

#Region "Networking"
        Private Sub ReceiveSetColor(ByVal player As Player, ByVal newColor As Slot.PlayerColor)
            Contract.Requires(player IsNot Nothing)
            Dim slot = TryFindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = slot.Lock.Frozen Then Return '[no changes allowed]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If state >= GameState.Loading Then Return '[too late]
            If Not newColor.EnumValueIsDefined Then Return '[not a valid color]

            'check for duplicates
            For Each otherSlot In slots.ToList()
                Contract.Assume(otherSlot IsNot Nothing)
                If otherSlot.color = newColor Then
                    If Not Map.IsMelee Then Return
                    If Not otherSlot.Contents.ContentType = SlotContentType.Empty Then Return
                    otherSlot.color = slot.color
                    Exit For
                End If
            Next otherSlot

            'change color
            slot.color = newColor
            ChangedLobbyState()
        End Sub
        Private Sub ReceiveSetRace(ByVal player As Player, ByVal newRace As Slot.Races)
            Contract.Requires(player IsNot Nothing)
            Dim slot = TryFindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = slot.Lock.Frozen Then Return '[no changes allowed]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If state >= GameState.Loading Then Return '[too late]
            If Not newRace.EnumValueIsDefined OrElse newRace = slot.Races.Unlocked Then Return '[not a valid race]

            'Perform
            slot.race = newRace
            ChangedLobbyState()
        End Sub
        Private Sub ReceiveSetHandicap(ByVal player As Player, ByVal new_handicap As Byte)
            Contract.Requires(player IsNot Nothing)
            Dim slot = TryFindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = slot.Lock.Frozen Then Return '[no changes allowed]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If state >= GameState.CountingDown Then Return '[too late]

            'Perform
            Select Case new_handicap
                Case 50, 60, 70, 80, 90, 100
                    slot.handicap = new_handicap
                Case Else
                    Return '[invalid handicap]
            End Select

            ChangedLobbyState()
        End Sub
        Private Sub ReceiveSetTeam(ByVal player As Player, ByVal newTeam As Byte)
            Contract.Requires(player IsNot Nothing)
            Dim slot = TryFindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked <> slot.Lock.Unlocked Then Return '[no teams changes allowed]
            If newTeam > slot.ObserverTeamIndex Then Return '[invalid value]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If state >= GameState.Loading Then Return '[too late]
            If newTeam = slot.ObserverTeamIndex Then
                Select Case settings.GameDescription.GameStats.observers
                    Case GameObserverOption.FullObservers, GameObserverOption.Referees
                        '[fine; continue]
                    Case Else
                        Return '[obs not enabled; invalid value]
                End Select
            ElseIf Map.IsMelee And newTeam >= Map.NumPlayerSlots Then
                Return '[invalid team]
            End If

            'Perform
            If Map.IsMelee Then
                'set slot to target team
                slot.Team = newTeam
            Else
                'swap with next open slot from target team
                For offset_mod = 0 To slots.Count - 1
                    Dim nextIndex = (slot.index + offset_mod) Mod slots.Count
                    Contract.Assume(nextIndex >= 0)
                    Dim nextSlot = slots(nextIndex)
                    Contract.Assume(nextSlot IsNot Nothing)
                    If nextSlot.Team = newTeam AndAlso nextSlot.Contents.WantPlayer(player.Name) >= SlotContents.WantPlayerPriority.Open Then
                        SwapSlotContents(nextSlot, slot)
                        Exit For
                    End If
                Next offset_mod
            End If

            ChangedLobbyState()
        End Sub
#End Region
    End Class
End Namespace
