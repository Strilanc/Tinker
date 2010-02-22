Namespace WC3
    Public NotInheritable Class GameLobby
        Inherits FutureDisposable

        Private ReadOnly _downloadManager As DownloadManager
        Private ReadOnly _startPlayerHoldPoint As HoldPoint(Of Player)
        Private ReadOnly _freeIndexes As List(Of PID)
        Private ReadOnly _logger As Logger
        Private ReadOnly _clock As IClock
        Private ReadOnly _players As AsyncViewableCollection(Of Player)
        Private ReadOnly _pidVisiblityMap As New Dictionary(Of PID, PID)()
        Private ReadOnly _settings As GameSettings
        Private _fakeHostPlayer As Player
        Public Property _acceptingPlayers As Boolean = True
        Private _slots As SlotSet

        Public Event ChangedPublicState(ByVal sender As GameLobby)
        Public Event RemovePlayer(ByVal sender As GameLobby, ByVal player As Player, ByVal wasExpected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_downloadManager IsNot Nothing)
            Contract.Invariant(_startPlayerHoldPoint IsNot Nothing)
            Contract.Invariant(_freeIndexes IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_slots IsNot Nothing)
            Contract.Invariant(_players IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_pidVisiblityMap IsNot Nothing)
            Contract.Invariant(_settings IsNot Nothing)
        End Sub

        Public Sub New(ByVal startPlayerholdPoint As HoldPoint(Of Player),
                       ByVal downloadManager As DownloadManager,
                       ByVal logger As Logger,
                       ByVal players As AsyncViewableCollection(Of Player),
                       ByVal clock As IClock,
                       ByVal settings As GameSettings)
            Contract.Requires(startPlayerholdPoint IsNot Nothing)
            Contract.Requires(downloadManager IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Me._startPlayerHoldPoint = startPlayerholdPoint
            Me._downloadManager = downloadManager
            Me._slots = New SlotSet(InitCreateSlots(settings))
            Me._logger = logger
            Me._players = players
            Me._clock = clock
            Me._settings = settings
            Dim pidCount = _slots.Slots.Count
            Me._freeIndexes = (From i In Enumerable.Range(1, pidCount) Select New PID(CByte(i))).ToList
            For i As Byte = 1 To 12
                _pidVisiblityMap(New PID(i)) = New PID(i)
            Next i

            If _settings.UseMultiObs Then
                If _settings.Map.Slots.Count <= 10 Then
                    For i = _settings.Map.Slots.Count To 10 - 1
                        CloseSlot(_slots.Slots(i).MatchableId)
                    Next i
                    Dim playerIndex = FreeIndexes(0)
                    FreeIndexes.Remove(playerIndex)
                    AddFakePlayer("# multi obs", slot:=_slots.Slots(10))
                    _slots = GameLobby.SetupCoveredSlot(_slots, _slots.Slots(10), _slots.Slots(11), playerIndex)
                End If
            End If
            TrySetTeamSizes(_settings.TeamSizes)
            For Each reservation In _settings.Reservations
                ReserveSlot(reservation)
            Next reservation
            If _settings.ObserverCount > 0 Then
                Dim n = _settings.ObserverCount
                For Each slot In _slots.Slots
                    If slot.Team = slot.ObserverTeamIndex Then
                        If n <= 0 Then CloseSlot(slot.MatchableId)
                        n -= 1
                    End If
                Next slot
            ElseIf _settings.ObserverReservations.Count > 0 Then
                For Each reservation In _settings.ObserverReservations
                    ReserveSlot(reservation, "obs")
                Next reservation
                For Each remainingObsSlot In From slot In _slots.Slots
                                             Where slot.Team = slot.ObserverTeamIndex
                                             Where slot.Contents.ContentType <> SlotContents.Type.Player
                    CloseSlot(remainingObsSlot.MatchableId)
                Next remainingObsSlot
            End If
        End Sub
        <ContractVerification(False)>
        Private Shared Function InitCreateSlots(ByVal settings As GameSettings) As IReadableList(Of Slot)
            Contract.Requires(settings IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))() IsNot Nothing)
            Dim result = New List(Of Slot)

            'Create player slots
            result.AddRange(From slot In settings.Map.Slots
                            Select slot.WithLock(settings.DefaultSlotLockState))

            'Create observer slots
            Select Case settings.GameDescription.GameStats.Observers
                Case GameObserverOption.FullObservers, GameObserverOption.Referees
                    result.AddRange(From i In Enumerable.Range(result.Count, 12 - result.Count)
                                    Select New Slot(index:=CByte(i),
                                                    Color:=CType(Slot.ObserverTeamIndex, Protocol.PlayerColor),
                                                    team:=Slot.ObserverTeamIndex,
                                                    contents:=New SlotContentsOpen,
                                                    locked:=settings.DefaultSlotLockState,
                                                    raceUnlocked:=False))
            End Select

            Return result.AsReadableList
        End Function

        Public Property Slots As SlotSet
            Get
                Return _slots
            End Get
            Set(ByVal value As SlotSet)
                _slots = value
            End Set
        End Property
        Public ReadOnly Property Logger As Logger
            Get
                Return _logger
            End Get
        End Property
        Public ReadOnly Property FakeHostPlayer As Player
            Get
                Return _fakeHostPlayer
            End Get
        End Property
        Public ReadOnly Property FreeIndexes As IList(Of PID)
            Get
                Return _freeIndexes
            End Get
        End Property
        Public ReadOnly Property StartPlayerHoldPoint As IHoldPoint(Of Player)
            Get
                Return _startPlayerHoldPoint
            End Get
        End Property
        Public ReadOnly Property DownloadManager As DownloadManager
            Get
                Return _downloadManager
            End Get
        End Property

        Public Sub LobbyCatchRemovedPlayer(ByVal player As Player,
                                           ByVal slot As Slot)
            Contract.Requires(player IsNot Nothing)

            If slot Is Nothing OrElse slot.Contents.PlayerIndex Is Nothing Then
                _freeIndexes.Add(player.PID)
            End If
            If player Is _fakeHostPlayer Then
                _fakeHostPlayer = Nothing
            Else
                TryRestoreFakeHost()
            End If
            RaiseEvent ChangedPublicState(Me)
        End Sub

        Public Function TryRestoreFakeHost() As Player
            If _fakeHostPlayer IsNot Nothing Then Return Nothing
            If Not _acceptingPlayers Then Return Nothing

            Dim name = My.Settings.ingame_name
            Contract.Assume(name IsNot Nothing)
            Try
                _fakeHostPlayer = AddFakePlayer(name)
                Return _fakeHostPlayer
            Catch ex As InvalidOperationException
                Return Nothing
            End Try
        End Function

        Public Function AddFakePlayer(ByVal name As InvariantString,
                                      Optional ByVal slot As Slot = Nothing) As Player
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            If Not _acceptingPlayers Then
                Throw New InvalidOperationException("No longer accepting players.")
            ElseIf FreeIndexes.Count <= 0 Then
                If FakeHostPlayer IsNot Nothing Then
                    RaiseEvent RemovePlayer(Me, FakeHostPlayer, True, Protocol.PlayerLeaveReason.Disconnect, "Need player index for new fake player")
                Else
                    Throw New InvalidOperationException("No space available for fake player.")
                End If
            End If
            Contract.Assume(FreeIndexes.Count > 0)

            'Assign index
            Dim index = FreeIndexes(0)
            FreeIndexes.Remove(index)

            'Make player
            Dim newPlayer = New Player(index, name, Logger)
            If slot IsNot Nothing Then
                _slots = _slots.WithSlotsReplaced(slot.WithContents(New SlotContentsPlayer(newPlayer)))
            End If
            _players.Add(newPlayer)

            'Inform other players
            For Each player In _players
                Contract.Assume(player IsNot Nothing)
                player.QueueSendPacket(newPlayer.MakePacketOtherPlayerJoined())
            Next player

            'Inform bot
            Logger.Log("{0} has been placed in the game.".Frmt(newPlayer.Name), LogMessageType.Positive)

            'Update state
            RaiseEvent ChangedPublicState(Me)
            Return newPlayer
        End Function

        Public Sub ThrowChangedPublicState()
            RaiseEvent ChangedPublicState(Me)
        End Sub

        <Pure()>
        Private Function ChooseSlotForNewPlayer(ByVal connectingPlayer As W3ConnectingPlayer) As Slot
            Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)

            Return (From slot In _slots.Slots
                    Let match = slot.Contents.WantPlayer(connectingPlayer.Name)
                    ).Max(comparator:=Function(e1, e2) e1.match - e2.match).slot
        End Function
        Private Function AllocatePIDForNewPlayer(ByVal connectingPlayer As W3ConnectingPlayer, ByVal slot As Slot) As PID
            Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Requires(slot IsNot Nothing)
            Contract.Ensures(Not FreeIndexes.Contains(Contract.Result(Of PID)()))

            Dim result As PID
            If slot.Contents.WantPlayer(connectingPlayer.Name) = SlotContents.WantPlayerPriority.ReservationForPlayer Then
                result = slot.Contents.PlayerIndex.Value
                For Each player In slot.Contents.EnumPlayers
                    Contract.Assume(player IsNot Nothing)
                    RaiseEvent RemovePlayer(Me, player, wasExpected:=True, reportedReason:=Protocol.PlayerLeaveReason.Disconnect, reasonDescription:="Reservation fulfilled")
                Next player
                Contract.Assume(FreeIndexes.Contains(result))
            ElseIf slot.Contents.PlayerIndex IsNot Nothing Then '[slot forces the pid]
                result = slot.Contents.PlayerIndex.Value
            ElseIf FreeIndexes.Count > 0 Then '[pid available]
                result = FreeIndexes(0)
            ElseIf FakeHostPlayer IsNot Nothing Then '[take fake host's pid]
                result = FakeHostPlayer.PID
                RaiseEvent RemovePlayer(Me, FakeHostPlayer, True, Protocol.PlayerLeaveReason.Disconnect, "Need player index for joining player.")
            Else
                Throw New InvalidOperationException("No available pids.")
            End If
            FreeIndexes.Remove(result)
            Return result
        End Function
        Private Function AddPlayer(ByVal connectingPlayer As W3ConnectingPlayer, ByVal slot As Slot, ByVal pid As PID) As Player
            Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Requires(slot IsNot Nothing)

            'Add
            Dim newPlayer = New Player(pid, connectingPlayer, _clock, _downloadManager, Logger)
            _slots = _slots.WithSlotsReplaced(slot.WithContents(slot.Contents.WithPlayer(newPlayer)))
            _players.Add(newPlayer)
            Logger.Log("{0} has entered the game.".Frmt(newPlayer.Name), LogMessageType.Positive)

            'Greet
            newPlayer.QueueSendPacket(Protocol.MakeGreet(newPlayer.RemoteEndPoint, newPlayer.PID))
            newPlayer.QueueSendPacket(Protocol.MakeHostMapInfo(_settings.Map))
            For Each visibleOtherPlayer In From p In _players Where p IsNot newPlayer AndAlso IsPlayerVisible(p)
                newPlayer.QueueSendPacket(visibleOtherPlayer.MakePacketOtherPlayerJoined())
            Next visibleOtherPlayer

            'Inform others
            If IsPlayerVisible(newPlayer) Then
                For Each otherPlayer In From p In _players Where p IsNot newPlayer
                    otherPlayer.QueueSendPacket(newPlayer.MakePacketOtherPlayerJoined())
                Next otherPlayer
            End If

            'Effects
            RaiseEvent ChangedPublicState(Me)
            _startPlayerHoldPoint.Hold(newPlayer).CallWhenReady(Sub() newPlayer.QueueStart()).SetHandled()

            Return newPlayer
        End Function
        Public Function AddPlayer(ByVal connectingPlayer As W3ConnectingPlayer) As Player
            Contract.Requires(connectingPlayer IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            If Not _acceptingPlayers Then
                Throw New InvalidOperationException("No longer accepting players.")
            ElseIf Not connectingPlayer.Socket.Connected Then
                Throw New InvalidOperationException("Player isn't connected.")
            End If

            'Alllocate space
            Dim slot = ChooseSlotForNewPlayer(connectingPlayer)
            Dim match = slot.Contents.WantPlayer(connectingPlayer.Name)
            If match < SlotContents.WantPlayerPriority.Open Then
                Throw New InvalidOperationException("No slot available for player.")
            End If
            Dim pid = AllocatePIDForNewPlayer(connectingPlayer, slot)

            Return AddPlayer(connectingPlayer, slot, pid)
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Strilbrary.Threading.IFuture
            If finalizing Then Return Nothing
            _downloadManager.Dispose()
            Return Nothing
        End Function

        <Pure()>
        Public Function IsPlayerVisible(ByVal player As Player) As Boolean
            Contract.Requires(player IsNot Nothing)
            Return _pidVisiblityMap(player.PID) = player.PID
        End Function
        <Pure()>
        Public Function GetVisiblePlayer(ByVal player As Player) As Player
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)
            If IsPlayerVisible(player) Then Return player
            Dim visibleIndex = _pidVisiblityMap(player.PID)
            Dim visiblePlayer = (From p In _players Where p.PID = visibleIndex).First
            Contract.Assume(visiblePlayer IsNot Nothing)
            Return visiblePlayer
        End Function
        Public Shared Function SetupCoveredSlot(ByVal slots As SlotSet,
                                                ByVal coveringSlot As Slot,
                                                ByVal coveredSlot As Slot,
                                                ByVal playerIndex As PID) As SlotSet
            Contract.Requires(slots IsNot Nothing)
            Contract.Requires(coveringSlot IsNot Nothing)
            Contract.Requires(coveredSlot IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)
            If coveringSlot.Contents.EnumPlayers.Count <> 1 Then Throw New InvalidOperationException()
            If coveredSlot.Contents.EnumPlayers.Any Then Throw New InvalidOperationException()

            Return slots.WithSlotsReplaced(coveringSlot.WithContents(New SlotContentsCovering(coveredSlot.MatchableId, coveringSlot.Contents.EnumPlayers.First)),
                                           coveredSlot.WithContents(New SlotContentsCovered(coveringSlot.MatchableId, playerIndex, coveredSlot.Contents.EnumPlayers)))
        End Function

        Public Function SendMapPiece(ByVal player As IPlayerDownloadAspect,
                                      ByVal position As UInt32) As ifuture
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Dim sender = If(FakeHostPlayer, (From p In _players Where p IsNot player).First)
            Contract.Assume(sender IsNot Nothing)
            Dim filedata = _settings.Map.ReadChunk(position, Protocol.Packets.MaxFileDataSize)
            Dim pk = Protocol.MakeMapFileData(position, filedata, player.PID, sender.PID)
            Return player.QueueSendPacket(pk)
        End Function

        '''<summary>Returns any slot matching a string. Checks index, color and player name.</summary>
        <Pure()>
        Public Function FindMatchingSlot(ByVal query As InvariantString) As Slot
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Dim best = (From slot In _slots.Slots
                        Let match = slot.Matches(query)
                        Let content = slot.Contents.ContentType
                        ).MaxProjection(Function(item) item.match * 10 - item.content).Item1
            If best.match = Slot.Match.None Then Throw New OperationFailedException("No matching slot found.")
            Contract.Assume(best.slot IsNot Nothing)
            Return best.slot
        End Function

        Public Sub OnPlayerSetColor(ByVal player As Player, ByVal newColor As Protocol.PlayerColor)
            Contract.Requires(player IsNot Nothing)
            Dim slot = _slots.TryFindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.Color = newColor Then Return
            If slot.Locked = slot.LockState.Frozen Then Return '[no changes allowed]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If Not _acceptingPlayers Then Return '[too late]
            If Not newColor.EnumValueIsDefined Then Return '[not a valid color]

            'check for duplicates
            Dim colorOwner = (From otherSlot In _slots.Slots Where otherSlot.Color = newColor).FirstOrDefault
            If colorOwner IsNot Nothing Then
                If Not _settings.Map.UsesFixedPlayerSettings AndAlso colorOwner.Contents.ContentType = SlotContents.Type.Empty Then
                    _slots = _slots.WithSlotsReplaced(colorOwner, colorOwner.WithColor(slot.Color))
                End If
            End If

            'change color
            _slots = _slots.WithSlotsReplaced(slot.WithColor(newColor))
            RaiseEvent ChangedPublicState(Me)
        End Sub
        Public Sub OnPlayerSetRace(ByVal player As Player, ByVal newRace As Protocol.Races)
            Contract.Requires(player IsNot Nothing)
            Dim slot = _slots.TryFindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.Locked = slot.LockState.Frozen Then Return '[no changes allowed]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If Not _acceptingPlayers Then Return '[too late]
            If Not newRace.EnumValueIsDefined OrElse newRace = Protocol.Races.Unlocked Then Return '[not a valid race]

            'Perform
            _slots = _slots.WithSlotsReplaced(slot.withRace(newRace))
            RaiseEvent ChangedPublicState(Me)
        End Sub
        Public Sub OnPlayerSetHandicap(ByVal player As Player, ByVal new_handicap As Byte)
            Contract.Requires(player IsNot Nothing)
            Dim slot = _slots.TryFindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked = slot.LockState.Frozen Then Return '[no changes allowed]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If Not _acceptingPlayers Then Return '[too late]

            'Perform
            Select Case new_handicap
                Case 50, 60, 70, 80, 90, 100
                    _slots = _slots.WithSlotsReplaced(slot.WithHandicap(new_handicap))
                Case Else
                    Return '[invalid handicap]
            End Select

            RaiseEvent ChangedPublicState(Me)
        End Sub
        Public Sub OnPlayerSetTeam(ByVal player As Player, ByVal newTeam As Byte)
            Contract.Requires(player IsNot Nothing)
            Dim slot = _slots.TryFindPlayerSlot(player)

            'Validate
            If slot Is Nothing Then Return
            If slot.locked <> slot.LockState.Unlocked Then Return '[no teams changes allowed]
            If newTeam > slot.ObserverTeamIndex Then Return '[invalid value]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If Not _acceptingPlayers Then Return '[too late]
            If newTeam = slot.ObserverTeamIndex Then
                Select Case _settings.GameDescription.GameStats.Observers
                    Case GameObserverOption.FullObservers, GameObserverOption.Referees
                        '[fine; continue]
                    Case Else
                        Return '[obs not enabled; invalid value]
                End Select
            End If

            'Perform
            If Not _settings.Map.UsesCustomForces Then
                If slot.Team <> newTeam AndAlso slot.Team = WC3.Slot.ObserverTeamIndex Then
                    If (From s In _slots.Slots Where s.Team <> WC3.Slot.ObserverTeamIndex).Count >= _settings.Map.Slots.Count Then
                        Dim partner = (From s In _slots.Slots
                                       Where s.Team <> WC3.Slot.ObserverTeamIndex
                                       Where s.Contents.ContentType = SlotContents.Type.Empty
                                       ).FirstOrDefault
                        If partner Is Nothing Then Return 'would exceed max player slots
                        _slots = _slots.WithSlotsReplaced(partner.WithTeam(slot.Team))
                    End If
                End If
                'set slot to target team
                _slots = _slots.WithSlotsReplaced(slot.WithTeam(newTeam))
            Else
                'swap with next open slot from target team
                For offset_mod = 0 To _slots.Slots.Count - 1
                    Dim nextIndex = (slot.Index + offset_mod) Mod _slots.Slots.Count
                    Contract.Assume(nextIndex >= 0)
                    Dim nextSlot = _slots.Slots(nextIndex)
                    Contract.Assume(nextSlot IsNot Nothing)
                    If nextSlot.Team = newTeam AndAlso nextSlot.Contents.WantPlayer(player.Name) >= SlotContents.WantPlayerPriority.Open Then
                        SwapSlotContents(nextSlot, slot)
                        Exit For
                    End If
                Next offset_mod
            End If

            RaiseEvent ChangedPublicState(Me)
        End Sub

        '''<summary>Reserves a slot for a player.</summary>
        Public Function ReserveSlot(ByVal userName As InvariantString,
                                    Optional ByVal slotQuery As InvariantString? = Nothing) As Player
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)
            If Not _acceptingPlayers Then Throw New InvalidOperationException("Can't reserve slots after launch.")
            Dim slot As Slot
            If slotQuery Is Nothing Then
                slot = (From s In _slots.Slots Where s.Contents.WantPlayer() = SlotContents.WantPlayerPriority.Open).FirstOrDefault
                If slot Is Nothing Then Throw New InvalidOperationException("No available slot.")
            Else
                slot = FindMatchingSlot(slotQuery.Value)
            End If
            If slot.Contents.ContentType = SlotContents.Type.Player Then
                Throw New InvalidOperationException("Slot '{0}' can't be reserved because it already contains a player.".Frmt(slotQuery))
            End If
            Return AddFakePlayer(userName, slot:=slot)
        End Function

        Private Sub ModifySlot(ByVal slotQuery As InvariantString,
                               ByVal projection As Func(Of Slot, Slot),
                               Optional ByVal avoidPlayers As Boolean = False)
            Contract.Requires(projection IsNot Nothing)

            If Not _acceptingPlayers Then Throw New InvalidOperationException("Can't modify slots during launch.")

            Dim slot = FindMatchingSlot(slotQuery)
            If avoidPlayers AndAlso slot.Contents.ContentType = SlotContents.Type.Player Then
                Throw New InvalidOperationException("Slot '{0}' contains a player.".Frmt(slotQuery))
            End If

            _slots = _slots.WithSlotsReplaced(projection(slot))
            RaiseEvent ChangedPublicState(Me)
        End Sub

        '''<summary>Opens the slot with the given index, unless the slot contains a player.</summary>
        Public Sub OpenSlot(ByVal slotQuery As InvariantString)
            ModifySlot(slotQuery,
                       Function(slot) slot.WithContents(New SlotContentsOpen),
                       avoidPlayers:=True)
        End Sub

        '''<summary>Places a computer with the given difficulty in the slot with the given index, unless the slot contains a player.</summary>
        Public Sub ComputerizeSlot(ByVal slotQuery As InvariantString, ByVal cpu As Protocol.ComputerLevel)
            ModifySlot(slotQuery,
                       Function(slot) slot.WithContents(New SlotContentsComputer(cpu)),
                       avoidPlayers:=True)
        End Sub

        '''<summary>Closes the slot with the given index, unless the slot contains a player.</summary>
        Public Sub CloseSlot(ByVal slotQuery As InvariantString)
            ModifySlot(slotQuery,
                       Function(slot) slot.WithContents(New SlotContentsClosed),
                       avoidPlayers:=True)
        End Sub

        Public Sub SwapSlotContents(ByVal slotQuery1 As InvariantString,
                                    ByVal slotQuery2 As InvariantString)
            If Not _acceptingPlayers Then Throw New InvalidOperationException("Can't swap slots after launch.")
            Dim slot1 = FindMatchingSlot(slotQuery1)
            Dim slot2 = FindMatchingSlot(slotQuery2)
            If slot1 Is slot2 Then Throw New InvalidOperationException("Slot {0} is slot '{1}'.".Frmt(slotQuery1, slotQuery2))
            SwapSlotContents(slot1, slot2)
            RaiseEvent ChangedPublicState(Me)
        End Sub
        Public Sub SwapSlotContents(ByVal slot1 As Slot, ByVal slot2 As Slot)
            Contract.Requires(slot1 IsNot Nothing)
            Contract.Requires(slot2 IsNot Nothing)
            _slots = _slots.WithSlotsReplaced(slot1.WithContents(slot2.Contents), slot2.WithContents(slot1.Contents))
            RaiseEvent ChangedPublicState(Me)
        End Sub

        Public Sub SetSlotColor(ByVal slotQuery As InvariantString, ByVal color As Protocol.PlayerColor)
            If _settings.Map.UsesFixedPlayerSettings Then
                Throw New InvalidOperationException("The map says that slot's color is locked.")
            ElseIf Not _acceptingPlayers Then
                Throw New InvalidOperationException("Can't change slot settings after launch.")
            End If

            Dim slot = FindMatchingSlot(slotQuery)
            Dim swapColorSlot = (From x In _slots.Slots Where x.color = color).FirstOrDefault
            If swapColorSlot IsNot Nothing Then _slots = _slots.WithSlotsReplaced(swapColorSlot, swapColorSlot.WithColor(slot.color))
            _slots = _slots.WithSlotsReplaced(slot.WithColor(color))

            RaiseEvent ChangedPublicState(Me)
        End Sub

        Public Sub SetSlotRace(ByVal slotQuery As InvariantString, ByVal race As Protocol.Races)
            ModifySlot(slotQuery, Function(slot)
                                      If Not slot.RaceUnlocked Then Throw New InvalidOperationException("The map says that slot's race is locked.")
                                      Return slot.WithRace(race)
                                  End Function)
        End Sub

        Public Sub SetSlotTeam(ByVal slotQuery As InvariantString, ByVal team As Byte)
            If _settings.Map.UsesCustomForces Then Throw New InvalidOperationException("The map says that all teams are locked.")
            ModifySlot(slotQuery, Function(slot)
                                      If slot.Team <> team AndAlso slot.Team = WC3.Slot.ObserverTeamIndex Then
                                          If (From s In _slots.Slots Where s.Team <> WC3.Slot.ObserverTeamIndex).Count >= _settings.Map.Slots.Count Then
                                              Throw New InvalidOperationException("You can only have {0} non-obs slots.".Frmt(_settings.Map.Slots.Count))
                                          End If
                                      End If
                                      Return slot.WithTeam(team)
                                  End Function)
        End Sub

        Public Sub SetSlotHandicap(ByVal slotQuery As InvariantString, ByVal handicap As Byte)
            ModifySlot(slotQuery, Function(slot) slot.WithHandicap(handicap))
        End Sub

        Public Sub SetSlotLocked(ByVal slotQuery As InvariantString, ByVal locked As Slot.LockState)
            ModifySlot(slotQuery, Function(slot) slot.WithLock(locked))
        End Sub

        Public Sub SetAllSlotsLocked(ByVal locked As Slot.LockState)
            _slots = _slots.WithSlotsReplaced(From slot In _slots.Slots Select slot.WithLock(locked))
        End Sub

        '''<summary>Returns the number of slots potentially available for new players.</summary>
        <Pure()>
        Public Function CountFreeSlots() As Integer
            Return (From slot In _slots.Slots
                    Where slot.Contents.WantPlayer() >= SlotContents.WantPlayerPriority.Open
                    ).Count
        End Function

        '''<summary>Opens slots, closes slots and moves players around to try to match the desired team sizes.</summary>
        Public Sub TrySetTeamSizes(ByVal desiredTeamSizes As IEnumerable(Of Integer))
            Contract.Requires(desiredTeamSizes IsNot Nothing)
            If Not _acceptingPlayers Then
                Throw New InvalidOperationException("Can't change team sizes after launch.")
            End If
            If desiredTeamSizes.Count = 0 Then Return

            If _settings.Map.UsesCustomForces Then
                'Group affected slots by team
                Dim teamSlotSets = (From team In Enumerable.Range(0, desiredTeamSizes.Count)
                                    Let size = desiredTeamSizes(team)
                                    Let slots = (From slot In _slots.Slots
                                                 Where slot.Team = team
                                                 Where slot.Contents.ContentType <> SlotContents.Type.Computer
                                                 Where slot.Contents.Moveable)
                                    ).ToArray '[cache results to avoid closure problems due to _slots changing later]

                'Group slots by availability
                Dim availableSlots = From teamSlotSet In teamSlotSets
                                     From slot In teamSlotSet.slots.Take(teamSlotSet.size)
                                     Where slot.Contents.ContentType = SlotContents.Type.Empty
                                     Select slot
                Dim blockedEmptySlots = From teamSlotSet In teamSlotSets
                                        From slot In teamSlotSet.slots.Skip(teamSlotSet.size)
                                        Where slot.Contents.ContentType = SlotContents.Type.Empty
                                        Select slot
                Dim blockedPlayerSlots = From teamSlotSet In teamSlotSets
                                         From slot In teamSlotSet.slots.Skip(teamSlotSet.size)
                                         Where slot.Contents.ContentType = SlotContents.Type.Player
                                         Select slot

                'Open available slots and close blocked slots
                For Each slot In From s In availableSlots.Skip(blockedPlayerSlots.Count)
                    _slots = _slots.WithSlotsReplaced(slot.WithContents(New SlotContentsOpen))
                Next slot
                For Each slot In From s In blockedEmptySlots
                    _slots = _slots.WithSlotsReplaced(slot.WithContents(New SlotContentsClosed))
                Next slot

                'Swap players from blocked slots to available slots (closing the now-empty blocked slot)
                For Each slotPair In availableSlots.Zip(blockedPlayerSlots)
                    Dim availableSlot = slotPair.Item1
                    Dim blockedPlayerSlot = slotPair.Item2
                    _slots = _slots.WithSlotsReplaced(availableSlot.WithContents(blockedPlayerSlot.Contents),
                                                      blockedPlayerSlot.WithContents(New SlotContentsClosed))
                Next slotPair
            Else
                Dim moveableSlots = Function() From slot In _slots.Slots
                                               Where slot.Contents.Moveable
                                               Where slot.Contents.ContentType <> SlotContents.Type.Computer

                'Move players up
                For Each slotPair In moveableSlots().Zip(From slot In moveableSlots()
                                                         Where slot.Contents.ContentType = SlotContents.Type.Player
                                                         Where slot.Team <> slot.ObserverTeamIndex).ToArray
                    _slots = _slots.WithSlotsReplaced(slotPair.Item1.WithContents(slotPair.Item2.Contents),
                                                      slotPair.Item2.WithContents(slotPair.Item1.Contents))
                Next slotPair

                'Move observers down
                For Each slotPair In moveableSlots().Reverse.Zip(From slot In _slots.Slots.Reverse
                                                                 Where slot.Team = slot.ObserverTeamIndex).ToArray
                    _slots = _slots.WithSlotsReplaced(slotPair.Item1.WithContents(slotPair.Item2.Contents).WithTeam(slotPair.Item2.Team),
                                                      slotPair.Item2.WithContents(slotPair.Item1.Contents).WithTeam(slotPair.Item1.Team))
                Next slotPair

                'Set teams
                For Each slotTeam In moveableSlots().Zip(From i In Enumerable.Range(0, desiredTeamSizes.Count)
                                                         From e In Enumerable.Repeat(CByte(i), desiredTeamSizes(i))
                                                         Select e).ToArray
                    _slots = _slots.WithSlotsReplaced(slotTeam.Item1.WithTeam(slotTeam.Item2))
                Next slotTeam
                'Open Available
                For Each slot In From s In moveableSlots().Take(desiredTeamSizes.Sum)
                                 Where s.Contents.ContentType = SlotContents.Type.Empty
                    _slots = _slots.WithSlotsReplaced(slot.WithContents(New SlotContentsOpen))
                Next slot
                'Close remainder
                For Each slot In From s In moveableSlots().Skip(desiredTeamSizes.Sum)
                                 Where s.Contents.ContentType = SlotContents.Type.Empty
                                 Where s.Team <> WC3.Slot.ObserverTeamIndex
                    _slots = _slots.WithSlotsReplaced(slot.WithContents(New SlotContentsClosed))
                Next slot
            End If

            RaiseEvent ChangedPublicState(Me)
        End Sub
    End Class
End Namespace
