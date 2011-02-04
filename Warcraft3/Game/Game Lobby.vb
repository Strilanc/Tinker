Namespace WC3
    Public NotInheritable Class GameLobby
        Inherits DisposableWithTask

        Private ReadOnly _downloadManager As Download.Manager
        Private ReadOnly _startPlayerHoldPoint As HoldPoint(Of Player)
        Private ReadOnly _freeIndexes As List(Of PlayerId)
        Private ReadOnly _kernel As GameKernel
        Private ReadOnly _pidVisiblityMap As New Dictionary(Of PlayerId, PlayerId)()
        Private ReadOnly _settings As GameSettings
        Private _fakeHostPlayer As Player
        Private _slots As SlotSet

        Public Event ChangedPublicState(ByVal sender As GameLobby)
        Public Event RemovePlayer(ByVal sender As GameLobby, ByVal player As Player, ByVal wasExpected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_downloadManager IsNot Nothing)
            Contract.Invariant(_startPlayerHoldPoint IsNot Nothing)
            Contract.Invariant(_freeIndexes IsNot Nothing)
            Contract.Invariant(_slots IsNot Nothing)
            Contract.Invariant(_kernel IsNot Nothing)
            Contract.Invariant(_pidVisiblityMap IsNot Nothing)
            Contract.Invariant(_settings IsNot Nothing)
        End Sub

        Public Sub New(ByVal startPlayerHoldPoint As HoldPoint(Of Player),
                       ByVal downloadManager As Download.Manager,
                       ByVal kernel As GameKernel,
                       ByVal settings As GameSettings)
            Contract.Assume(startPlayerHoldPoint IsNot Nothing)
            Contract.Assume(downloadManager IsNot Nothing)
            Contract.Assume(settings IsNot Nothing)
            Me._startPlayerHoldPoint = startPlayerHoldPoint
            Me._downloadManager = downloadManager
            Me._slots = New SlotSet(InitCreateSlots(settings))
            Me._kernel = kernel
            Me._settings = settings
            Dim pidCount = _slots.Count
            Me._freeIndexes = (From i In pidCount.Range.OffsetBy(1) Select New PlayerId(CByte(i))).ToList
            For Each pid In From i In 12.Range.OffsetBy(1)
                            Select New PlayerId(CByte(i))
                _pidVisiblityMap.Add(pid, pid)
            Next pid

            If _settings.UseMultiObs Then
                If _settings.Map.LobbySlots.Count <= 10 Then
                    For Each i In 10.Range.Skip(_settings.Map.LobbySlots.Count)
                        CloseSlot(_slots(i).MatchableId)
                    Next i
                    Dim playerIndex = _freeIndexes.First
                    _freeIndexes.Remove(playerIndex)
                    AddFakePlayer("# multi obs", slot:=_slots(10))
                    _slots = GameLobby.SetupCoveredSlot(_slots, _slots(10), _slots(11), playerIndex)
                End If
            End If
            TrySetTeamSizes(_settings.TeamSizes)
            For Each reservation In _settings.Reservations
                ReserveSlot(reservation)
            Next reservation
            If _settings.ObserverCount > 0 Then
                Dim n = _settings.ObserverCount
                For Each slot In _slots
                    If slot.Team = slot.ObserverTeamIndex Then
                        If n <= 0 Then CloseSlot(slot.MatchableId)
                        n -= 1
                    End If
                Next slot
            ElseIf _settings.ObserverReservations.Count > 0 Then
                For Each reservation In _settings.ObserverReservations
                    ReserveSlot(reservation, "obs")
                Next reservation
                For Each remainingObsSlot In From slot In _slots
                                             Where slot.Team = slot.ObserverTeamIndex
                                             Where slot.Contents.ContentType <> SlotContents.Type.Player
                    CloseSlot(remainingObsSlot.MatchableId)
                Next remainingObsSlot
            End If
        End Sub
        Private Shared Function InitCreateSlots(ByVal settings As GameSettings) As IRist(Of Slot)
            Contract.Requires(settings IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Slot))() IsNot Nothing)
            Dim result = New List(Of Slot)

            'Create player slots
            result.AddRange(From slot In settings.Map.LobbySlots
                            Select slot.With(locked:=settings.DefaultSlotLockState))

            'Create observer slots
            Select Case settings.GameDescription.GameStats.Observers
                Case GameObserverOption.FullObservers, GameObserverOption.Referees
                    result.AddRange(From i In 12.Range.Skip(result.Count)
                                    Select New Slot(index:=CByte(i),
                                                    Color:=CType(Slot.ObserverTeamIndex, Protocol.PlayerColor),
                                                    team:=Slot.ObserverTeamIndex,
                                                    contents:=New SlotContentsOpen,
                                                    locked:=settings.DefaultSlotLockState,
                                                    raceUnlocked:=False))
            End Select

            Return result.AsReadableList
        End Function

        Private ReadOnly Property AcceptingPlayers As Boolean
            Get
                Return _kernel.State = GameState.AcceptingPlayers
            End Get
        End Property
        Public Property Slots As SlotSet
            Get
                Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)
                Return _slots
            End Get
            Set(ByVal value As SlotSet)
                Contract.Requires(value IsNot Nothing)
                _slots = value
            End Set
        End Property
        Public ReadOnly Property FakeHostPlayer As Player
            Get
                Return _fakeHostPlayer
            End Get
        End Property
        Public ReadOnly Property StartPlayerHoldPoint As IHoldPoint(Of Player)
            Get
                Contract.Ensures(Contract.Result(Of IHoldPoint(Of Player))() IsNot Nothing)
                Return _startPlayerHoldPoint
            End Get
        End Property
        Public ReadOnly Property DownloadManager As Download.Manager
            Get
                Contract.Ensures(Contract.Result(Of Download.Manager)() IsNot Nothing)
                Return _downloadManager
            End Get
        End Property

        Public Sub LobbyCatchRemovedPlayer(ByVal player As Player,
                                           ByVal slot As Slot?)
            Contract.Requires(player IsNot Nothing)

            If slot Is Nothing OrElse slot.Value.Contents.PlayerIndex Is Nothing Then
                _freeIndexes.Add(player.Id)
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
            If Not AcceptingPlayers Then Return Nothing

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
                                      Optional ByVal slot As Slot? = Nothing) As Player
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            If Not AcceptingPlayers Then
                Throw New InvalidOperationException("No longer accepting players.")
            ElseIf _freeIndexes.Count <= 0 Then
                If _fakeHostPlayer IsNot Nothing Then
                    RaiseEvent RemovePlayer(Me, _fakeHostPlayer, True, Protocol.PlayerLeaveReason.Disconnect, "Need player index for new fake player")
                Else
                    Throw New InvalidOperationException("No space available for fake player.")
                End If
            End If
            Contract.Assume(_freeIndexes.Count > 0)

            'Assign index
            Dim index = _freeIndexes(0)
            _freeIndexes.Remove(index)

            'Make player
            Dim newPlayer = Player.MakeFake(index, name, _kernel.Logger)
            If slot IsNot Nothing Then
                _slots = _slots.WithSlotsUpdatedByIndex(slot.Value.With(contents:=New SlotContentsPlayer(newPlayer)))
            End If
            _kernel.Players.Add(newPlayer)

            'Inform other players
            For Each player In _kernel.Players
                Contract.Assume(player IsNot Nothing)
                player.QueueSendPacket(newPlayer.MakePacketOtherPlayerJoined())
            Next player

            'Inform bot
            _kernel.Logger.Log("{0} has been placed in the game.".Frmt(newPlayer.Name), LogMessageType.Positive)

            'Update state
            RaiseEvent ChangedPublicState(Me)
            Return newPlayer
        End Function

        Public Sub ThrowChangedPublicState()
            RaiseEvent ChangedPublicState(Me)
        End Sub

        Private Function AllocateSpaceForNewPlayer(ByVal name As InvariantString) As Tuple(Of Slot, PlayerId)
            Contract.Ensures(Not _freeIndexes.Contains(Contract.Result(Of Tuple(Of Slot, PlayerId))().Item2))

            'Choose Slot
            Dim slotMatch = (From s In _slots
                             Let match = s.Contents.WantPlayer(name)
                             ).AssumeAny().Max(comparator:=Function(e1, e2) e1.match - e2.match)
            Contract.Assume(slotMatch IsNot Nothing)
            If slotMatch.match < SlotContents.WantPlayerPriority.Open Then
                Throw New InvalidOperationException("No slot available for player.")
            End If
            Dim slot = slotMatch.s

            'Allocate id
            Dim id As PlayerId
            If slot.Contents.WantPlayer(name) = SlotContents.WantPlayerPriority.ReservationForPlayer Then
                Contract.Assume(slot.Contents.PlayerIndex.HasValue)
                id = slot.Contents.PlayerIndex.Value
                For Each player In slot.Contents.EnumPlayers
                    Contract.Assume(player IsNot Nothing)
                    RaiseEvent RemovePlayer(Me, player, wasExpected:=True, reportedReason:=Protocol.PlayerLeaveReason.Disconnect, reasonDescription:="Reservation fulfilled")
                Next player
                slot = (From s In _slots Where s.MatchableId = slot.MatchableId).Single
                Contract.Assume(_freeIndexes.Contains(id))
            ElseIf slot.Contents.PlayerIndex IsNot Nothing Then '[slot forces the pid]
                id = slot.Contents.PlayerIndex.Value
            ElseIf _freeIndexes.Count > 0 Then '[pid available]
                id = _freeIndexes(0)
            ElseIf _fakeHostPlayer IsNot Nothing Then '[take fake host's pid]
                id = _fakeHostPlayer.Id
                RaiseEvent RemovePlayer(Me, _fakeHostPlayer, True, Protocol.PlayerLeaveReason.Disconnect, "Need player index for joining player.")
                slot = (From s In _slots Where s.MatchableId = slot.MatchableId).Single
            Else
                Throw New InvalidOperationException("No available pids.")
            End If
            _freeIndexes.Remove(id)

            Dim r = Tuple.Create(slot, id)
            Contract.Assume(Not _freeIndexes.Contains(r.Item2))
            Return r
        End Function
        Private Function AddPlayer(ByVal newPlayer As Player,
                                   ByVal slot As Slot,
                                   ByVal socketRemoteEndPoint As Net.IPEndPoint) As Player
            Contract.Requires(newPlayer IsNot Nothing)
            Contract.Requires(socketRemoteEndPoint IsNot Nothing)
            Contract.Requires(socketRemoteEndPoint.Address IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            'Add
            _slots = _slots.WithSlotsUpdatedByIndex(slot.With(contents:=slot.Contents.WithPlayer(newPlayer)))
            _kernel.Players.Add(newPlayer)
            _kernel.Logger.Log("{0} has entered the game.".Frmt(newPlayer.Name), LogMessageType.Positive)

            'Greet
            newPlayer.QueueSendPacket(Protocol.MakeGreet(socketRemoteEndPoint, newPlayer.Id))
            newPlayer.QueueSendPacket(Protocol.MakeHostMapInfo(_settings.Map))
            For Each visibleOtherPlayer In From p In _kernel.Players
                                           Where p IsNot newPlayer
                                           Where IsPlayerVisible(p)
                Contract.Assume(visibleOtherPlayer IsNot Nothing)
                newPlayer.QueueSendPacket(visibleOtherPlayer.MakePacketOtherPlayerJoined())
            Next visibleOtherPlayer

            'Inform others
            If IsPlayerVisible(newPlayer) Then
                For Each otherPlayer In From p In _kernel.Players
                                        Where p IsNot newPlayer
                    Contract.Assume(otherPlayer IsNot Nothing)
                    otherPlayer.QueueSendPacket(newPlayer.MakePacketOtherPlayerJoined())
                Next otherPlayer
            End If

            'Effects
            _kernel.OutQueue.QueueAction(Sub() RaiseEvent ChangedPublicState(Me))
            _startPlayerHoldPoint.Hold(newPlayer).ContinueWithAction(Sub() newPlayer.QueueStart()).IgnoreExceptions()

            Return newPlayer
        End Function
        Public Function AddPlayer(ByVal knockData As Protocol.KnockData,
                                  ByVal socket As W3Socket) As Player
            Contract.Requires(knockData IsNot Nothing)
            Contract.Requires(socket IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)

            If Not AcceptingPlayers Then
                Throw New InvalidOperationException("No longer accepting players.")
            ElseIf Not socket.Connected Then
                Throw New InvalidOperationException("Player isn't connected.")
            End If

            Dim space = AllocateSpaceForNewPlayer(knockData.Name)
            Dim newPlayer = Player.MakeRemote(space.Item2, knockData, socket, _kernel.Clock, _downloadManager, _kernel.Logger)
            Return AddPlayer(newPlayer, space.Item1, socket.RemoteEndPoint)
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            _downloadManager.Dispose()
            Return Nothing
        End Function

        <Pure()>
        Public Function IsPlayerVisible(ByVal player As Player) As Boolean
            Contract.Requires(player IsNot Nothing)
            Return _pidVisiblityMap(player.Id) = player.Id
        End Function
        <Pure()>
        Public Function GetVisiblePlayer(ByVal player As Player) As Player
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)
            If IsPlayerVisible(player) Then Return player
            Dim visibleIndex = _pidVisiblityMap(player.Id)
            Dim visiblePlayer = (From p In _kernel.Players Where p.Id = visibleIndex).First
            Contract.Assume(visiblePlayer IsNot Nothing)
            Return visiblePlayer
        End Function
        Public Shared Function SetupCoveredSlot(ByVal slots As SlotSet,
                                                ByVal coveringSlot As Slot,
                                                ByVal coveredSlot As Slot,
                                                ByVal coveredPlayersId As PlayerId) As SlotSet
            Contract.Requires(slots IsNot Nothing)
            Contract.Requires(coveringSlot.Contents.EnumPlayers.None)
            Contract.Requires(coveringSlot.Contents.EnumPlayers.Count = 1)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)

            Dim coveringPlayer = coveringSlot.Contents.EnumPlayers.Single
            Contract.Assume(coveringPlayer IsNot Nothing)
            Return slots.WithSlotsUpdatedByIndex(coveringSlot.With(contents:=New SlotContentsCovering(coveredSlot.MatchableId, coveringPlayer)),
                                                 coveredSlot.With(contents:=New SlotContentsCovered(coveringSlot.MatchableId, coveredPlayersId, {})))
        End Function

        Public Function SendMapPiece(ByVal receiver As Download.IPlayerDownloadAspect,
                                     ByVal position As UInt32) As Task
            Contract.Requires(receiver IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)

            Dim sender = If(_fakeHostPlayer, (From p In _kernel.Players
                                              Where IsPlayerVisible(p)
                                              Where p.Id <> receiver.Id).First)
            Dim filedata = _settings.Map.ReadChunk(position, Protocol.Packets.MaxFileDataSize)
            Contract.Assume(sender IsNot Nothing)

            Contract.Assume(receiver.Id <> sender.Id)
            Dim pk = Protocol.MakeMapFileData(position, filedata, receiver.Id, sender.Id)
            Return receiver.QueueSendPacket(pk)
        End Function

        '''<summary>Returns any slot matching a string. Checks index, color and player name.</summary>
        <Pure()>
        Public Function FindMatchingSlot(ByVal query As InvariantString) As Slot
            Dim best = (From slot In _slots
                        Let match = slot.Matches(query)
                        Let content = slot.Contents.ContentType
                        ).AssumeAny().MaxBy(Function(item) item.match * 10 - item.content)
            Contract.Assume(best IsNot Nothing)
            If best.match = Slot.Match.None Then Throw New OperationFailedException("No matching slot found.")
            Return best.slot
        End Function

        Public Sub OnPlayerSetColor(ByVal sender As Player, ByVal newColor As Protocol.PlayerColor)
            Contract.Requires(sender IsNot Nothing)
            Dim trySlot = _slots.TryFindPlayerSlot(sender)
            If Not trySlot.HasValue Then Return
            Dim slot = trySlot.Value

            'Validate
            If slot.Color = newColor Then Return
            If slot.Locked = slot.LockState.Frozen Then Return '[no changes allowed]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If Not AcceptingPlayers Then Return '[too late]
            If Not newColor.EnumValueIsDefined Then Return '[not a valid color]

            'check for duplicates
            Dim colorOwner = (From otherSlot In _slots Where otherSlot.Color = newColor).FirstOrNullableDefault
            If colorOwner IsNot Nothing Then
                If Not _settings.Map.UsesFixedPlayerSettings AndAlso colorOwner.Value.Contents.ContentType = SlotContents.Type.Empty Then
                    _slots = _slots.WithSlotsUpdatedByIndex(colorOwner.Value, colorOwner.Value.With(color:=slot.Color))
                End If
            End If

            'change color
            _slots = _slots.WithSlotsUpdatedByIndex(slot.With(color:=newColor))
            RaiseEvent ChangedPublicState(Me)
        End Sub
        Public Sub OnPlayerSetRace(ByVal sender As Player, ByVal newRace As Protocol.Races)
            Contract.Requires(sender IsNot Nothing)
            Dim trySlot = _slots.TryFindPlayerSlot(sender)
            If trySlot Is Nothing Then Return
            Dim slot = trySlot.Value

            'Validate
            If slot.Locked = slot.LockState.Frozen Then Return '[no changes allowed]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If Not AcceptingPlayers Then Return '[too late]
            If Not newRace.EnumValueIsDefined OrElse newRace = Protocol.Races.Unlocked Then Return '[not a valid race]

            'Perform
            _slots = _slots.WithSlotsUpdatedByIndex(slot.With(race:=newRace))
            RaiseEvent ChangedPublicState(Me)
        End Sub
        Public Sub OnPlayerSetHandicap(ByVal sender As Player, ByVal newHandicap As Byte)
            Contract.Requires(sender IsNot Nothing)
            Dim trySlot = _slots.TryFindPlayerSlot(sender)
            If trySlot Is Nothing Then Return
            Dim slot = trySlot.Value

            'Validate
            If slot.Locked = slot.LockState.Frozen Then Return '[no changes allowed]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If Not AcceptingPlayers Then Return '[too late]

            'Perform
            Select Case newHandicap
                Case 50, 60, 70, 80, 90, 100
                    _slots = _slots.WithSlotsUpdatedByIndex(slot.With(handicap:=newHandicap))
                Case Else
                    Return '[invalid handicap]
            End Select

            RaiseEvent ChangedPublicState(Me)
        End Sub
        Public Sub OnPlayerSetTeam(ByVal sender As Player, ByVal newTeam As Byte)
            Contract.Requires(sender IsNot Nothing)
            Dim trySlot = _slots.TryFindPlayerSlot(sender)
            If trySlot Is Nothing Then Return
            Dim slot = trySlot.Value

            'Validate
            If slot.Locked <> slot.LockState.Unlocked Then Return '[no teams changes allowed]
            If newTeam > slot.ObserverTeamIndex Then Return '[invalid value]
            If Not slot.Contents.Moveable Then Return '[slot is weird]
            If Not AcceptingPlayers Then Return '[too late]
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
                    If (From s In _slots Where s.Team <> WC3.Slot.ObserverTeamIndex).Count >= _settings.Map.LobbySlots.Count Then
                        Dim partner = (From s In _slots
                                       Where s.Team <> WC3.Slot.ObserverTeamIndex
                                       Where s.Contents.ContentType = SlotContents.Type.Empty
                                       ).FirstOrNullableDefault
                        If partner Is Nothing Then Return 'would exceed max player slots
                        _slots = _slots.WithSlotsUpdatedByIndex(partner.Value.With(team:=slot.Team))
                    End If
                End If
                'set slot to target team
                _slots = _slots.WithSlotsUpdatedByIndex(slot.With(team:=newTeam))
            Else
                'swap with next open slot from target team
                For Each offset_mod In _slots.Count.Range
                    Dim nextIndex = (slot.Index + offset_mod) Mod _slots.Count
                    Contract.Assume(nextIndex >= 0)
                    Dim nextSlot = _slots(nextIndex)
                    If nextSlot.Team = newTeam AndAlso nextSlot.Contents.WantPlayer(sender.Name) >= SlotContents.WantPlayerPriority.Open Then
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
            If Not AcceptingPlayers Then Throw New InvalidOperationException("Can't reserve slots after launch.")
            Dim slot As Slot
            If slotQuery Is Nothing Then
                Dim trySlot = (From s In _slots Where s.Contents.WantPlayer() = SlotContents.WantPlayerPriority.Open).FirstOrNullableDefault
                If trySlot Is Nothing Then Throw New InvalidOperationException("No available slot.")
                slot = trySlot.Value
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

            If Not AcceptingPlayers Then Throw New InvalidOperationException("Can't modify slots during launch.")

            Dim slot = FindMatchingSlot(slotQuery)
            If avoidPlayers AndAlso slot.Contents.ContentType = SlotContents.Type.Player Then
                Throw New InvalidOperationException("Slot '{0}' contains a player.".Frmt(slotQuery))
            End If

            _slots = _slots.WithSlotsUpdatedByIndex(projection(slot))
            RaiseEvent ChangedPublicState(Me)
        End Sub

        '''<summary>Opens the slot with the given index, unless the slot contains a player.</summary>
        Public Sub OpenSlot(ByVal slotQuery As InvariantString)
            ModifySlot(slotQuery,
                       Function(slot) slot.With(contents:=New SlotContentsOpen),
                       avoidPlayers:=True)
        End Sub

        '''<summary>Places a computer with the given difficulty in the slot with the given index, unless the slot contains a player.</summary>
        Public Sub ComputerizeSlot(ByVal slotQuery As InvariantString, ByVal cpu As Protocol.ComputerLevel)
            ModifySlot(slotQuery,
                       Function(slot) slot.With(contents:=New SlotContentsComputer(cpu)),
                       avoidPlayers:=True)
        End Sub

        '''<summary>Closes the slot with the given index, unless the slot contains a player.</summary>
        Public Sub CloseSlot(ByVal slotQuery As InvariantString)
            ModifySlot(slotQuery,
                       Function(slot) slot.With(contents:=New SlotContentsClosed),
                       avoidPlayers:=True)
        End Sub

        Public Sub SwapSlotContents(ByVal slotQuery1 As InvariantString,
                                    ByVal slotQuery2 As InvariantString)
            If Not AcceptingPlayers Then Throw New InvalidOperationException("Can't swap slots after launch.")
            Dim slot1 = FindMatchingSlot(slotQuery1)
            Dim slot2 = FindMatchingSlot(slotQuery2)
            If slot1 = slot2 Then Throw New InvalidOperationException("Slot {0} is slot '{1}'.".Frmt(slotQuery1, slotQuery2))
            SwapSlotContents(slot1, slot2)
            RaiseEvent ChangedPublicState(Me)
        End Sub
        Public Sub SwapSlotContents(ByVal slot1 As Slot, ByVal slot2 As Slot)
            _slots = _slots.WithSlotsUpdatedByIndex(slot1.With(contents:=slot2.Contents),
                                                    slot2.With(contents:=slot1.Contents))
            RaiseEvent ChangedPublicState(Me)
        End Sub

        Public Sub SetSlotColor(ByVal slotQuery As InvariantString, ByVal color As Protocol.PlayerColor)
            If _settings.Map.UsesFixedPlayerSettings Then
                Throw New InvalidOperationException("The map says that slot's color is locked.")
            ElseIf Not AcceptingPlayers Then
                Throw New InvalidOperationException("Can't change slot settings after launch.")
            End If

            Dim slot = FindMatchingSlot(slotQuery)
            Dim swapColorSlot = (From x In _slots
                                 Where x.Color = color
                                 ).FirstOrDefault([default]:=slot)
            _slots = _slots.WithSlotsUpdatedByIndex(slot.With(color:=color),
                                                    swapColorSlot.With(color:=slot.Color))

            RaiseEvent ChangedPublicState(Me)
        End Sub

        Public Sub SetSlotRace(ByVal slotQuery As InvariantString, ByVal race As Protocol.Races)
            ModifySlot(slotQuery, Function(slot)
                                      If Not slot.RaceUnlocked Then Throw New InvalidOperationException("The map says that slot's race is locked.")
                                      Return slot.With(race:=race)
                                  End Function)
        End Sub

        Public Sub SetSlotTeam(ByVal slotQuery As InvariantString, ByVal team As Byte)
            If _settings.Map.UsesCustomForces Then Throw New InvalidOperationException("The map says that all teams are locked.")
            ModifySlot(slotQuery, Function(slot)
                                      If slot.Team <> team AndAlso slot.Team = WC3.Slot.ObserverTeamIndex Then
                                          If (From s In _slots Where s.Team <> WC3.Slot.ObserverTeamIndex).Count >= _settings.Map.LobbySlots.Count Then
                                              Throw New InvalidOperationException("You can only have {0} non-obs slots.".Frmt(_settings.Map.LobbySlots.Count))
                                          End If
                                      End If
                                      Return slot.With(team:=team)
                                  End Function)
        End Sub

        Public Sub SetSlotHandicap(ByVal slotQuery As InvariantString, ByVal handicap As Byte)
            ModifySlot(slotQuery, Function(slot) slot.With(handicap:=handicap))
        End Sub

        Public Sub SetSlotLocked(ByVal slotQuery As InvariantString, ByVal locked As Slot.LockState)
            ModifySlot(slotQuery, Function(slot) slot.With(locked:=locked))
        End Sub

        Public Sub SetAllSlotsLocked(ByVal locked As Slot.LockState)
            _slots = _slots.WithSlotsReplaced(From slot In _slots Select slot.With(locked:=locked))
        End Sub

        '''<summary>Returns the number of slots potentially available for new players.</summary>
        <Pure()>
        Public Function CountFreeSlots() As Integer
            Return (From slot In _slots
                    Where slot.Contents.WantPlayer() >= SlotContents.WantPlayerPriority.Open
                    ).Count
        End Function

        '''<summary>Opens slots, closes slots and moves players around to try to match the desired team sizes.</summary>
        Public Sub TrySetTeamSizes(ByVal desiredTeamSizes As IEnumerable(Of Integer))
            Contract.Requires(desiredTeamSizes IsNot Nothing)
            If Not AcceptingPlayers Then
                Throw New InvalidOperationException("Can't change team sizes after launch.")
            End If

            If _settings.Map.UsesCustomForces Then
                _slots = SetupTeamSizesCustomForces(_slots, desiredTeamSizes)
            Else
                _slots = SetupTeamSizesMeleeForces(_slots, desiredTeamSizes, maxNonObserverSlots:=_settings.Map.LobbySlots.Count)
            End If

            RaiseEvent ChangedPublicState(Me)
        End Sub

        <Pure()>
        Public Shared Function SetupTeamSizesMeleeForces(ByVal slotSet As SlotSet,
                                                         ByVal desiredTeamSizes As IEnumerable(Of Integer),
                                                         ByVal maxNonObserverSlots As Integer) As SlotSet
            Contract.Requires(slotSet IsNot Nothing)
            Contract.Requires(desiredTeamSizes IsNot Nothing)
            Contract.Requires(maxNonObserverSlots > 0)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)

            If desiredTeamSizes.Count = 0 Then Return slotSet

            'Prep
            Dim affectedSlots = From slot In slotSet
                                Where slot.Contents.Moveable
                                Where slot.Contents.ContentType <> SlotContents.Type.Computer
                                Where slot.Team <> WC3.Slot.ObserverTeamIndex OrElse slot.Contents.ContentType = SlotContents.Type.Empty
                                Order By slot.Contents.ContentType Descending, slot.Index Ascending
            Dim teamIndexes = From team In desiredTeamSizes.Count.Range
                              From teamRepeated In CByte(team).Repeated(desiredTeamSizes(team))
                              Select teamRepeated

            'Compute transformed slots
            Dim assignedSlots = From pair In affectedSlots.Take(maxNonObserverSlots).Zip(teamIndexes)
                                Let slot = pair.Item1
                                Let team = pair.Item2
                                Select slot.With(team:=team,
                                                 contents:=If(slot.Contents.ContentType = SlotContents.Type.Empty,
                                                              New SlotContentsOpen,
                                                              slot.Contents))
            Dim closedSlots = From slot In affectedSlots.Skip(assignedSlots.Count)
                              Where slot.Team <> WC3.Slot.ObserverTeamIndex
                              Select slot.With(contents:=New SlotContentsClosed)

            'Transform
            Return slotSet.WithSlotsReplaced(assignedSlots).WithSlotsReplaced(closedSlots)
        End Function
        <Pure()>
        Public Shared Function SetupTeamSizesCustomForces(ByVal slotSet As SlotSet,
                                                          ByVal desiredTeamSizes As IEnumerable(Of Integer)) As SlotSet
            Contract.Requires(slotSet IsNot Nothing)
            Contract.Requires(desiredTeamSizes IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)
            'Contract.Ensures(players before === players after)
            'Contract.Ensures(players only moved if their team is full and another team can be filled)

            'Group affected slots by team
            Dim affectedSlots = From slot In slotSet
                                Where slot.Contents.ContentType <> SlotContents.Type.Computer
                                Where slot.Contents.Moveable
                                Order By slot.Contents.ContentType Descending, slot.Index Ascending
            Dim teamSlotSets = From team In desiredTeamSizes.Count.Range
                               Let size = desiredTeamSizes(team)
                               Let slots = (From slot In affectedSlots Where slot.Team = team)

            'Separate by availability
            Dim availableSlots = From teamSlotSet In teamSlotSets
                                 From slot In teamSlotSet.slots.Take(teamSlotSet.size)
                                 Where slot.Contents.ContentType = SlotContents.Type.Empty
                                 Select slot
            Dim blockedSlots = From teamSlotSet In teamSlotSets
                               From slot In teamSlotSet.slots.Skip(teamSlotSet.size)
                               Select slot

            'Separate by used-for-swapping-ness
            Dim playerBlockedSlots = From slot In blockedSlots
                                     Where slot.Contents.ContentType = SlotContents.Type.Player
            Dim emptyBlockedSlots = From slot In blockedSlots
                                    Where slot.Contents.ContentType = SlotContents.Type.Empty
            Dim usedAvailableSlots = availableSlots.Take(playerBlockedSlots.Count)
            Dim unusedAvailableSlots = availableSlots.Skip(playerBlockedSlots.Count)

            'Perform transformation:
            Dim result = slotSet
            'swap players from blocked slots to available slots (closing the now-empty blocked slot)
            result = result.WithSlotsReplaced(From slotPair In usedAvailableSlots.Zip(playerBlockedSlots)
                                              Let availableSlot = slotPair.Item1
                                              Let blockedSlot = slotPair.Item2
                                              From slot In {availableSlot.With(contents:=blockedSlot.Contents),
                                                            blockedSlot.With(contents:=New SlotContentsClosed)}
                                              Select slot)
            'close empty blocked slots
            result = result.WithSlotsReplaced(From slot In emptyBlockedSlots
                                              Select slot.With(contents:=New SlotContentsClosed))
            'open unused available slots
            result = result.WithSlotsReplaced(From slot In unusedAvailableSlots
                                              Select slot.With(contents:=New SlotContentsOpen))

            Return result
        End Function

        '''<summary>Broadcasts a packet to all players. Requires a packer for the packet, and values matching the packer.</summary>
        Public Sub BroadcastPacket(ByVal packet As Protocol.Packet,
                                   Optional ByVal source As Player = Nothing)
            Contract.Requires(packet IsNot Nothing)
            For Each player In From p In _kernel.Players
                               Where p IsNot source
                Contract.Assume(player IsNot Nothing)
                player.QueueSendPacket(packet)
            Next player
        End Sub

        '''<summary>Sends text to all players. Uses spoof chat if necessary.</summary>
        Public Sub BroadcastMessage(ByVal message As String,
                                    Optional ByVal playerToAvoid As Player = Nothing,
                                    Optional ByVal messageType As LogMessageType = LogMessageType.Typical)
            Contract.Requires(message IsNot Nothing)
            For Each player In From p In _kernel.Players
                               Where p IsNot playerToAvoid
                SendMessageTo(message, player.AssumeNotNull, display:=False)
            Next player
            _kernel.Logger.Log("{0}: {1}".Frmt(Application.ProductName, message), messageType)
        End Sub

        '''<summary>Sends text to the target player. Uses spoof chat if necessary.</summary>
        Public Sub SendMessageTo(ByVal message As String,
                                 ByVal player As Player,
                                 Optional ByVal display As Boolean = True)
            Contract.Requires(message IsNot Nothing)
            Contract.Requires(player IsNot Nothing)

            'Send Text (from fake host or spoofed from receiver)
            Dim prefix = If(_fakeHostPlayer Is Nothing, "{0}: ".Frmt(Application.ProductName), "")
            Dim chatType = If(_kernel.State >= GameState.Loading, Protocol.ChatType.Game, Protocol.ChatType.Lobby)
            Dim sender = If(_fakeHostPlayer, player)
            If Protocol.Packets.MaxChatTextLength - prefix.Length <= 0 Then
                Throw New InvalidStateException("The product name is so long there's no room for text to follow it!")
            End If
            For Each line In SplitText(body:=message, maxLineLength:=Protocol.Packets.MaxChatTextLength - prefix.Length)
                player.QueueSendPacket(Protocol.MakeText(text:=prefix + line,
                                                         chatType:=chatType,
                                                         receivingGroup:=Protocol.ChatGroup.Private,
                                                         receivers:=(From p In _kernel.Players Select p.Id),
                                                         sender:=sender.Id))
            Next line

            If display Then
                _kernel.Logger.Log("(Private to {0}): {1}".Frmt(player.Name, message), LogMessageType.Typical)
            End If
        End Sub
    End Class
End Namespace
