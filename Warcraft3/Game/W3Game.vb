''HostBot - Warcraft 3 game hosting bot
''Copyright (C) 2008 Craig Gidney
''
''This program is free software: you can redistribute it and/or modify
''it under the terms of the GNU General Public License as published by
''the Free Software Foundation, either version 3 of the License, or
''(at your option) any later version.
''
''This program is distributed in the hope that it will be useful,
''but WITHOUT ANY WARRANTY; without even the implied warranty of
''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''GNU General Public License for more details.
''You should have received a copy of the GNU General Public License
''along with this program.  If not, see http://www.gnu.org/licenses/

Namespace WC3
    Public NotInheritable Class Game
        Inherits DisposableWithTask

        Public Shared ReadOnly GuestLobbyCommands As Commands.CommandSet(Of GameManager) = GameCommands.MakeGuestLobbyCommands()
        Public Shared ReadOnly GuestInGameCommands As Commands.CommandSet(Of GameManager) = GameCommands.MakeGuestInGameCommands()
        Public Shared ReadOnly HostInGameCommands As Commands.CommandSet(Of GameManager) = GameCommands.MakeHostInGameCommands()
        Public Shared ReadOnly HostLobbyCommands As Commands.CommandSet(Of GameManager) = GameCommands.MakeHostLobbyCommands()
        Public Shared ReadOnly AdminCommands As Commands.CommandSet(Of GameManager) = GameCommands.MakeBotAdminCommands()

        Private ReadOnly _name As InvariantString
        Private ReadOnly _started As New OnetimeLock
        Private ReadOnly _settings As GameSettings
        Private ReadOnly _updateEventThrottle As New Throttle(cooldown:=100.Milliseconds, clock:=New SystemClock())
        Private ReadOnly _slotStateUpdateThrottle As New Throttle(cooldown:=250.Milliseconds, clock:=New SystemClock())

        Private ReadOnly _lobby As GameLobby
        Private ReadOnly _motor As GameMotor
        Private ReadOnly _kernel As GameKernel
        Private ReadOnly _loadScreen As GameLoadScreen

        Public Property HackManager As GameManager 'eventually remove this hacked value. Only inserted to prevent commit from ballooning.

        Private _adminPlayer As Player
        Private _flagHasPlayerLeft As Boolean

        Public Event Tick(ByVal sender As Game,
                          ByVal timeSpan As UShort,
                          ByVal actualActionStreaks As IReadableList(Of IReadableList(Of Protocol.SpecificPlayerActionSet)),
                          ByVal visibleActionStreaks As IReadableList(Of IReadableList(Of Protocol.PlayerActionSet)))
        Public Event Updated(ByVal sender As Game,
                             ByVal slots As SlotSet)
        Public Event PlayerLeft(ByVal sender As Game,
                                ByVal state As GameState,
                                ByVal leaver As Player,
                                ByVal reportedReason As Protocol.PlayerLeaveReason,
                                ByVal reasonDescription As String)
        Public Event ChangedState(ByVal sender As Game,
                                  ByVal oldState As GameState,
                                  ByVal newState As GameState)
        Public Event PlayerTalked(ByVal sender As Game,
                                  ByVal speaker As Player,
                                  ByVal text As String,
                                  ByVal receivingGroup As Protocol.ChatGroup?)
        Public Event RecordGameStarted(ByVal sender As Game)
        Public Event ReceivedPlayerActions(ByVal sender As Game,
                                           ByVal player As Player,
                                           ByVal actions As IReadableList(Of Protocol.GameAction))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_kernel IsNot Nothing)
            Contract.Invariant(_started IsNot Nothing)
            Contract.Invariant(_settings IsNot Nothing)
            Contract.Invariant(_lobby IsNot Nothing)
            Contract.Invariant(_motor IsNot Nothing)
            Contract.Invariant(_loadScreen IsNot Nothing)
            Contract.Invariant(_updateEventThrottle IsNot Nothing)
            Contract.Invariant(_slotStateUpdateThrottle IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal settings As GameSettings,
                       ByVal lobby As GameLobby,
                       ByVal motor As GameMotor,
                       ByVal loadScreen As GameLoadScreen,
                       ByVal kernel As GameKernel)
            Contract.Assume(settings IsNot Nothing)
            Contract.Assume(lobby IsNot Nothing)
            Contract.Assume(motor IsNot Nothing)
            Contract.Assume(kernel IsNot Nothing)
            Contract.Assume(loadScreen IsNot Nothing)
            Me._settings = settings
            Me._name = name
            Me._lobby = lobby
            Me._motor = motor
            Me._kernel = kernel
            Me._loadScreen = loadScreen
        End Sub

        Public Shared Function FromSettings(ByVal settings As GameSettings,
                                            ByVal name As InvariantString,
                                            ByVal clock As IClock,
                                            ByVal logger As Logger) As Game
            Contract.Requires(clock IsNot Nothing)
            Contract.Requires(settings IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Game)() IsNot Nothing)

            Dim inQueue = New TaskedCallQueue
            Dim outQueue = New TaskedCallQueue
            Dim kernel = New GameKernel(clock, inQueue, outQueue, logger)
            Dim startPlayerHoldPoint = New HoldPoint(Of Player)
            Dim downloadManager = New Download.Manager(clock:=clock,
                                                       Map:=settings.Map,
                                                       logger:=logger,
                                                       allowDownloads:=settings.AllowDownloads,
                                                       allowUploads:=settings.AllowUpload)
            Dim lobby = New GameLobby(startPlayerHoldPoint:=startPlayerHoldPoint,
                                      downloadManager:=downloadManager,
                                      kernel:=kernel,
                                      settings:=settings)
            Dim speedFactor = My.Settings.game_speed_factor
            Dim tickPeriod = CUInt(My.Settings.game_tick_period).Milliseconds
            Dim lagLimit = CUInt(My.Settings.game_lag_limit).Milliseconds
            If speedFactor <= 0 Then Throw New InvalidStateException("Non-positive speed factor.")
            If tickPeriod.Ticks <= 0 Then Throw New InvalidStateException("Non-positive tick period.")
            Dim motor = New GameMotor(defaultSpeedFactor:=speedFactor,
                                      defaultTickPeriod:=tickPeriod,
                                      defaultLagLimit:=lagLimit,
                                      kernel:=kernel,
                                      lobby:=lobby)
            Dim loadScreen = New GameLoadScreen(kernel:=kernel,
                                                lobby:=lobby,
                                                settings:=settings)

            Return New Game(name:=name,
                            settings:=settings,
                            lobby:=lobby,
                            motor:=motor,
                            loadScreen:=loadScreen,
                            kernel:=kernel)
        End Function

        Public Sub Start()
            If Not _started.TryAcquire Then Throw New InvalidOperationException("Already started.")
            If Me.IsDisposed Then Throw New ObjectDisposedException(Me.GetType.Name)

            InitLobby()
            InitLoadScreen()
            InitMotor()

            AddHandler _kernel.ChangedState, Sub(sender, oldState, newState) _kernel.OutQueue.QueueAction(
                                                 Sub() RaiseEvent ChangedState(Me, oldState, newState))
            _kernel.InQueue.QueueAction(Sub() _lobby.TryRestoreFakeHost())
        End Sub
        Private Sub InitLobby()
            _lobby.DownloadManager.Start(
                    startPlayerHoldPoint:=StartPlayerHoldPoint,
                    mapPieceSender:=Sub(receiver, position) _kernel.InQueue.QueueAction(
                                        Sub() _lobby.SendMapPiece(receiver, position)))

            _lobby.StartPlayerHoldPoint.IncludeActionHandler(
                Sub(newPlayer)
                    AddHandler newPlayer.Disconnected, Sub(player, expected, reportedReason, reasonDescription) _kernel.InQueue.QueueAction(
                                                           Sub() RemovePlayer(player, expected, reportedReason, reasonDescription))
                    AddHandler newPlayer.SuperficialStateUpdated, Sub() QueueThrowUpdated()
                    AddHandler newPlayer.StateUpdated, Sub() _kernel.InQueue.QueueAction(AddressOf _lobby.ThrowChangedPublicState)
                    newPlayer.QueueAddPacketHandler(Protocol.ClientPackets.NonGameAction,
                                                    Function(vals) _kernel.InQueue.QueueAction(Sub() OnReceiveNonGameAction(newPlayer, vals)))

                    If Settings.Greeting <> "" Then
                        _lobby.SendMessageTo(message:=Settings.Greeting, player:=newPlayer, display:=False)
                    End If
                    If Settings.AutoElevateUserName IsNot Nothing AndAlso newPlayer.Name = Settings.AutoElevateUserName.Value Then
                        ElevatePlayer(newPlayer.Name)
                    End If
                    TryBeginAutoStart()
                End Sub)

            AddHandler _lobby.ChangedPublicState, Sub(sender)
                                                      ThrowUpdated()
                                                      _slotStateUpdateThrottle.SetActionToRun(
                                                          Sub() _kernel.InQueue.QueueAction(
                                                              Sub() SendLobbyState(New ModInt32(Environment.TickCount).UnsignedValue)))
                                                  End Sub

            AddHandler _lobby.RemovePlayer, Sub(sender, player, wasExpected, reportedReason, reasonDescription)
                                                RemovePlayer(player, wasExpected, reportedReason, reasonDescription)
                                            End Sub
        End Sub
        Private Sub InitLoadScreen()
            AddHandler _loadScreen.RecordGameStarted, Sub(sender) _kernel.OutQueue.QueueAction(Sub() RaiseEvent RecordGameStarted(Me))
            AddHandler _loadScreen.EmptyTick, Sub(sender) _kernel.OutQueue.QueueAction(
                Sub() RaiseEvent Tick(sender:=Me,
                                      TimeSpan:=0,
                                      actualActionStreaks:=New IReadableList(Of Protocol.SpecificPlayerActionSet)() {}.AsReadableList,
                                      visibleActionStreaks:=New IReadableList(Of Protocol.PlayerActionSet)() {}.AsReadableList))
            AddHandler _loadScreen.Finished, Sub(sender) _motor.QueueStart()
        End Sub
        Private Sub InitMotor()
            _motor.Init()
            AddHandler _motor.RemovePlayer, Sub(sender, player, wasExpected, reportedReason, reasonDescription) RemovePlayer(player, wasExpected, reportedReason, reasonDescription)
            AddHandler _motor.Tick, Sub(sender, timeSpan, actualActions, visibleActions) _kernel.OutQueue.QueueAction(Sub() RaiseEvent Tick(Me, timeSpan, actualActions, visibleActions))
            AddHandler _motor.ReceivedPlayerActions, Sub(sender, player, actions) _kernel.OutQueue.QueueAction(Sub() RaiseEvent ReceivedPlayerActions(Me, player, actions))
        End Sub

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return _kernel.OutQueue.QueueAction(Sub() _kernel.InQueue.QueueAction(
                Sub()
                    _kernel.State = GameState.Disposed
                    _lobby.Dispose()
                    _motor.Dispose()
                    For Each player In _kernel.Players
                        player.Dispose()
                    Next player
                End Sub))
        End Function

        Public ReadOnly Property Logger As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _kernel.Logger
            End Get
        End Property
        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Settings As GameSettings
            Get
                Contract.Ensures(Contract.Result(Of GameSettings)() IsNot Nothing)
                Return _settings
            End Get
        End Property
        Public ReadOnly Property Motor As GameMotor
            Get
                Contract.Ensures(Contract.Result(Of GameMotor)() IsNot Nothing)
                Return _motor
            End Get
        End Property

        Private Sub ThrowUpdated()
            Dim slots = _lobby.Slots
            _updateEventThrottle.SetActionToRun(Sub() _kernel.OutQueue.QueueAction(Sub() RaiseEvent Updated(Me, slots)))
        End Sub
        Public Function QueueThrowUpdated() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(AddressOf ThrowUpdated)
        End Function

        Private Function CommandProcessText(ByVal manager As GameManager,
                                            ByVal player As Player,
                                            ByVal argument As String) As Task(Of String)
            Contract.Requires(manager IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)

            Dim isAdmin = player Is _adminPlayer AndAlso player IsNot Nothing
            If Settings.IsAdminGame AndAlso isAdmin Then
                Return Game.AdminCommands.Invoke(manager, Nothing, argument)
            End If

            Dim inLobby = _kernel.State < GameState.Loading
            Dim commands As Commands.CommandSet(Of GameManager)
            If isAdmin Then
                If inLobby Then
                    commands = Game.GuestLobbyCommands
                Else
                    commands = Game.GuestInGameCommands
                End If
            Else
                If inLobby Then
                    commands = Game.HostLobbyCommands
                Else
                    commands = Game.HostInGameCommands
                End If
            End If

            Dim user = If(player Is Nothing, Nothing, New BotUser(player.Name))
            Return commands.Invoke(manager, user, argument)
        End Function
        Public Function QueueCommandProcessText(ByVal manager As GameManager,
                                                ByVal player As Player,
                                                ByVal argument As String) As Task(Of String)
            Contract.Requires(manager IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() CommandProcessText(manager, player, argument)).Unwrap.AssumeNotNull
        End Function

        Public Function QueueGetAdminPlayer() As Task(Of Player)
            Contract.Ensures(Contract.Result(Of Task(Of Player))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() _adminPlayer)
        End Function
        Public Function QueueGetFakeHostPlayer() As Task(Of Player)
            Contract.Ensures(Contract.Result(Of Task(Of Player))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() _lobby.FakeHostPlayer)
        End Function
        Public Function QueueGetPlayers() As Task(Of IReadableList(Of Player))
            Contract.Ensures(Contract.Result(Of Task(Of IReadableList(Of Player)))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() _kernel.Players.ToReadableList)
        End Function
        Public Function QueueGetState() As Task(Of GameState)
            Contract.Ensures(Contract.Result(Of Task(Of GameState))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() _kernel.State)
        End Function

        Public Function QueueBroadcastMessage(ByVal message As String) As Task
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.BroadcastMessage(message))
        End Function

        Public Function QueueSendMessageTo(ByVal message As String, ByVal player As Player) As Task
            Contract.Requires(message IsNot Nothing)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.SendMessageTo(message, player))
        End Function

        Private Sub OnReceiveChat(ByVal sender As Player,
                                  ByVal text As String,
                                  ByVal type As Protocol.ChatType,
                                  ByVal receivingGroup As Protocol.ChatGroup?,
                                  ByVal requestedReceivers As IReadableList(Of PlayerId))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(requestedReceivers IsNot Nothing)

            'Log
            Logger.Log("{0}: {1}".Frmt(sender.Name, text), LogMessageType.Typical)
            _kernel.OutQueue.QueueAction(Sub() RaiseEvent PlayerTalked(Me, sender, text, receivingGroup))

            'Forward to requested players
            'visible sender
            Dim visibleSender = _lobby.GetVisiblePlayer(sender)
            If visibleSender IsNot sender Then
                text = visibleSender.Name + ": " + text
            End If
            'packet
            Dim pk = Protocol.MakeText(text, type, receivingGroup, requestedReceivers, visibleSender.Id)
            'receivers
            For Each receiver In _kernel.Players
                Contract.Assume(receiver IsNot Nothing)
                Dim visibleReceiver = _lobby.GetVisiblePlayer(receiver)
                If requestedReceivers.Contains(visibleReceiver.Id) Then
                    receiver.QueueSendPacket(pk)
                ElseIf visibleReceiver Is visibleSender AndAlso sender IsNot receiver Then
                    receiver.QueueSendPacket(pk)
                End If
            Next receiver
        End Sub
        Private Sub OnReceiveNonGameAction(ByVal sender As Player, ByVal vals As NamedValueMap)
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)

            Dim keyValue = vals.ItemAs(Of KeyValuePair(Of Protocol.NonGameActionType, Object))("value")
            Contract.Assume(keyValue.Value IsNot Nothing)
            Select Case keyValue.Key
                Case Protocol.NonGameActionType.GameChat
                    Dim value = DirectCast(keyValue.Value, NamedValueMap).AssumeNotNull
                    Dim message = value.ItemAs(Of String)("message")
                    Dim chatType = Protocol.ChatType.Game
                    Dim receivingGroup = value.ItemAs(Of Protocol.ChatGroup)("receiving group")
                    Dim requestedReceivers = vals.ItemAs(Of IReadableList(Of PlayerId))("requested receivers")
                    OnReceiveChat(sender, message, chatType, receivingGroup, requestedReceivers)

                Case Protocol.NonGameActionType.LobbyChat
                    Dim value = DirectCast(keyValue.Value, NamedValueMap).AssumeNotNull
                    Dim message = value.ItemAs(Of String)("message")
                    Dim chatType = Protocol.ChatType.Lobby
                    Dim receivingGroup = [Default](Of Protocol.ChatGroup?)()
                    Dim requestedReceivers = vals.ItemAs(Of IReadableList(Of PlayerId))("requested receivers")
                    OnReceiveChat(sender, message, chatType, receivingGroup, requestedReceivers)

                Case Protocol.NonGameActionType.SetTeam
                    _lobby.OnPlayerSetTeam(sender, DirectCast(keyValue.Value, Byte))

                Case Protocol.NonGameActionType.SetHandicap
                    _lobby.OnPlayerSetHandicap(sender, DirectCast(keyValue.Value, Byte))

                Case Protocol.NonGameActionType.SetRace
                    _lobby.OnPlayerSetRace(sender, DirectCast(keyValue.Value, Protocol.Races))

                Case Protocol.NonGameActionType.SetColor
                    _lobby.OnPlayerSetColor(sender, DirectCast(keyValue.Value, Protocol.PlayerColor))

                Case Else
                    RemovePlayer(sender, True, Protocol.PlayerLeaveReason.Disconnect, "Sent unrecognized client command type: {0}".Frmt(keyValue.Key))
            End Select
        End Sub

#Region "Players"
        '''<summary>Removes the given player from the instance</summary>
        Private Sub RemovePlayer(ByVal player As Player,
                                 ByVal wasExpected As Boolean,
                                 ByVal reportedReason As Protocol.PlayerLeaveReason,
                                 ByVal reasonDescription As String)
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(reasonDescription IsNot Nothing)
            If Not _kernel.Players.Contains(player) Then
                Return
            End If

            'Clean slot
            Dim slot = _lobby.Slots.TryFindPlayerSlot(player)
            If slot IsNot Nothing Then
                If slot.Value.Contents.EnumPlayers.Contains(player) Then
                    slot = slot.Value.With(contents:=slot.Value.Contents.WithoutPlayer(player))
                    _lobby.Slots = _lobby.Slots.WithSlotsReplaced(slot.Value)
                End If
            End If

            'Clean player
            If _lobby.IsPlayerVisible(player) Then
                _lobby.BroadcastPacket(Protocol.MakeOtherPlayerLeft(player.Id, reportedReason), player)
            End If
            If player Is _adminPlayer Then
                _adminPlayer = Nothing
            End If
            player.QueueDisconnect(True, reportedReason, reasonDescription)
            _kernel.Players.Remove(player)
            Select Case _kernel.State
                Case Is < GameState.Loading
                    _lobby.LobbyCatchRemovedPlayer(player, slot)
                Case GameState.Loading
                    _loadScreen.OnRemovedPlayer()
                Case Is > GameState.Loading
            End Select

            'Clean game
            If _kernel.State >= GameState.Loading AndAlso Not (From x In _kernel.Players Where Not x.IsFake).Any Then
                'the game has started and everyone has left, time to die
                Me.Dispose()
            End If

            'Log
            If player.IsFake Then
                Logger.Log("{0} has been removed from the game. ({1})".Frmt(player.Name, reasonDescription), LogMessageType.Negative)
            Else
                _flagHasPlayerLeft = True
                If wasExpected Then
                    Logger.Log("{0} has left the game. ({1})".Frmt(player.Name, reasonDescription), LogMessageType.Negative)
                Else
                    Logger.Log("{0} has disconnected. ({1})".Frmt(player.Name, reasonDescription), LogMessageType.Problem)
                End If
                Dim state_ = _kernel.State
                _kernel.OutQueue.QueueAction(Sub() RaiseEvent PlayerLeft(Me, state_, player, reportedReason, reasonDescription))
            End If
        End Sub

        Private Sub ElevatePlayer(ByVal name As InvariantString,
                                  Optional ByVal password As String = Nothing)
            Dim player = TryFindPlayer(name)
            If player Is Nothing Then Throw New InvalidOperationException("No player found with the name '{0}'.".Frmt(name))
            If _adminPlayer IsNot Nothing Then Throw New InvalidOperationException("A player is already the admin.")
            If password IsNot Nothing Then
                player.AdminAttemptCount += 1
                If player.AdminAttemptCount > 5 Then Throw New InvalidOperationException("Too many tries.")
                If password <> Settings.AdminPassword Then
                    Throw New InvalidOperationException("Incorrect password.")
                End If
            End If

            _adminPlayer = player
            _lobby.SendMessageTo("You are now the admin.", player)
        End Sub
        Public Function QueueElevatePlayer(ByVal name As InvariantString,
                                           Optional ByVal password As String = Nothing) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() ElevatePlayer(name, password))
        End Function

        Private Function TryFindPlayer(ByVal id As PlayerId) As Player
            Return (From player In _kernel.Players Where player.Id = id).FirstOrDefault
        End Function
        Private Function TryFindPlayer(ByVal userName As InvariantString) As Player
            Return (From player In _kernel.Players Where player.Name = userName).FirstOrDefault
        End Function
        Public Function QueueTryFindPlayer(ByVal userName As InvariantString) As Task(Of Player)
            Contract.Ensures(Contract.Result(Of Task(Of Player))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() TryFindPlayer(userName))
        End Function

        '''<summary>Boots players in the slot with the given index.</summary>
        Private Sub Boot(ByVal slotQuery As InvariantString, ByVal shouldCloseEmptiedSlot As Boolean)
            Dim slot = _lobby.FindMatchingSlot(slotQuery)
            If Not slot.Contents.EnumPlayers.Any Then
                Throw New InvalidOperationException("There is no player to boot in slot '{0}'.".Frmt(slotQuery))
            End If

            Dim target = (From player In slot.Contents.EnumPlayers Where player.Name = slotQuery).FirstOrDefault
            If target IsNot Nothing Then
                _lobby.Slots = _lobby.Slots.WithSlotsReplaced(slot.With(contents:=slot.Contents.WithoutPlayer(target)))
                RemovePlayer(target, True, Protocol.PlayerLeaveReason.Disconnect, "Booted")
            Else
                For Each player In slot.Contents.EnumPlayers
                    Contract.Assume(player IsNot Nothing)
                    _lobby.Slots = _lobby.Slots.WithSlotsReplaced(slot.With(contents:=slot.Contents.WithoutPlayer(player)))
                    RemovePlayer(player, True, Protocol.PlayerLeaveReason.Disconnect, "Booted")
                Next player
            End If

            If shouldCloseEmptiedSlot AndAlso slot.Contents.ContentType = SlotContents.Type.Empty Then
                _lobby.Slots = _lobby.Slots.WithSlotsReplaced(slot.With(contents:=New SlotContentsClosed))
                _lobby.ThrowChangedPublicState()
            End If
        End Sub
        Public Function QueueBoot(ByVal slotQuery As InvariantString, ByVal shouldCloseEmptiedSlot As Boolean) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() Boot(slotQuery, shouldCloseEmptiedSlot))
        End Function
#End Region

        Public Function ObservePlayers(ByVal adder As Action(Of Game, Player),
                                       ByVal remover As Action(Of Game, Player)) As Task(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() _kernel.Players.Observe(
                adder:=Sub(sender, item) adder(Me, item),
                remover:=Sub(sender, item) remover(Me, item)))
        End Function

#Region "Advancing State"
        '''<summary>Autostarts the countdown if autostart is enabled and the game stays full for awhile.</summary>
        Private Function TryBeginAutoStart() As Boolean
            'Sanity check
            If Not Settings.IsAutoStarted Then Return False
            If _lobby.CountFreeSlots() > 0 Then Return False
            If _kernel.State >= GameState.PreCounting Then Return False
            If (From player In _kernel.Players Where Not player.IsFake AndAlso player.AdvertisedDownloadPercent <> 100).Any Then
                Return False
            End If
            _kernel.State = GameState.PreCounting

            'Give people a few seconds to realize the game is full before continuing
            Call _kernel.Clock.AsyncWait(3.Seconds).QueueContinueWithAction(_kernel.InQueue,
                Sub()
                    If _kernel.State <> GameState.PreCounting Then Return
                    If Not Settings.IsAutoStarted OrElse _lobby.CountFreeSlots() > 0 Then
                        _kernel.State = GameState.AcceptingPlayers
                    Else
                        TryStartCountdown()
                    End If
                End Sub
            )
            Return True
        End Function

        '''<summary>Starts the countdown to launch.</summary>
        Private Function TryStartCountdown() As Boolean
            If _kernel.State >= GameState.CountingDown Then Return False
            If (From p In _kernel.Players Where Not p.IsFake AndAlso p.AdvertisedDownloadPercent <> 100).Any Then
                Return False
            End If

            _kernel.State = GameState.CountingDown
            _flagHasPlayerLeft = False

            'Perform countdown
            Dim continueCountdown As Action(Of Integer)
            continueCountdown = Sub(ticksLeft)
                                    If _kernel.State <> GameState.CountingDown Then Return

                                    If _flagHasPlayerLeft Then 'abort countdown
                                        _lobby.BroadcastMessage("Countdown Aborted", messageType:=LogMessageType.Negative)
                                        _lobby.TryRestoreFakeHost()
                                        _kernel.State = GameState.AcceptingPlayers
                                        _lobby.ThrowChangedPublicState()
                                    ElseIf ticksLeft > 0 Then 'continue ticking
                                        _lobby.BroadcastMessage("Starting in {0}...".Frmt(ticksLeft), messageType:=LogMessageType.Positive)
                                        _kernel.Clock.AsyncWait(1.Seconds).QueueContinueWithAction(_kernel.InQueue, Sub() continueCountdown(ticksLeft - 1))
                                    Else 'start
                                        StartLoading()
                                    End If
                                End Sub
            Call _kernel.Clock.AsyncWait(1.Seconds).QueueContinueWithAction(_kernel.InQueue, Sub() continueCountdown(5))

            Return True
        End Function
        Public Function QueueStartCountdown() As Task(Of Boolean)
            Contract.Ensures(Contract.Result(Of Task(Of Boolean))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() TryStartCountdown())
        End Function

        '''<summary>Launches the game, sending players to the loading screen.</summary>
        Private Sub StartLoading()
            If _kernel.State >= GameState.Loading Then Return

            'Remove fake players
            For Each player In From p In _kernel.Players.Cache Where p.IsFake
                Contract.Assume(player IsNot Nothing)
                Dim slot = _lobby.Slots.TryFindPlayerSlot(player)
                If slot Is Nothing OrElse slot.Value.Contents.Moveable Then
                    RemovePlayer(player, True, Protocol.PlayerLeaveReason.Disconnect, "Fake players removed before loading")
                End If
            Next player

            _lobby.Slots = _lobby.Slots.WithEncodeHCL(Settings)
            Dim randomSeed As ModInt32 = Environment.TickCount()
            SendLobbyState(randomSeed.UnsignedValue)

            If Settings.ShouldRecordReplay Then
                Replay.ReplayManager.StartRecordingFrom(
                    defaultFileName:=Settings.DefaultReplayFileName,
                    game:=Me,
                    players:=_kernel.Players.Cache,
                    slots:=_lobby.Slots,
                    randomSeed:=randomSeed.UnsignedValue,
                    infoProvider:=New CachedWC3InfoProvider())
            End If

            _kernel.State = GameState.Loading
            _lobby.Dispose()
            _loadScreen.Start()
        End Sub
#End Region

#Region "Lobby"
        Private Sub SetPlayerVoteToStart(ByVal name As InvariantString, ByVal val As Boolean)
            If Not Settings.IsAutoStarted Then Throw New InvalidOperationException("Game is not set to start automatically.")
            Dim p = TryFindPlayer(name)
            If p Is Nothing Then Throw New InvalidOperationException("No player found with the name '{0}'.".Frmt(name))
            p.HasVotedToStart = val
            If Not val Then Return

            Dim numPlayers = (From q In _kernel.Players Where Not q.IsFake).Count
            Dim numInFavor = (From q In _kernel.Players Where Not q.IsFake AndAlso q.HasVotedToStart).Count
            If numPlayers >= 2 AndAlso numInFavor * 3 >= numPlayers * 2 Then
                TryStartCountdown()
            End If
        End Sub
        Public Function QueueSetPlayerVoteToStart(ByVal name As InvariantString,
                                                  ByVal wantsToStart As Boolean) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() SetPlayerVoteToStart(name, wantsToStart))
        End Function
        Public ReadOnly Property StartPlayerHoldPoint As IHoldPoint(Of Player)
            Get
                Contract.Ensures(Contract.Result(Of IHoldPoint(Of Player))() IsNot Nothing)
                Return _lobby.StartPlayerHoldPoint
            End Get
        End Property
        Public Function QueueAddPlayer(ByVal knockData As Protocol.KnockData,
                                       ByVal socket As W3Socket) As Task(Of Player)
            Contract.Requires(knockData IsNot Nothing)
            Contract.Requires(socket IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of Player))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() _lobby.AddPlayer(knockData, socket))
        End Function
        Public Function QueueSendMapPiece(ByVal player As Download.IPlayerDownloadAspect,
                                          ByVal position As UInt32) As Task
            Return _kernel.InQueue.QueueFunc(Function() _lobby.SendMapPiece(player, position)).Unwrap
        End Function

        '''<summary>Broadcasts new game state to players, and throws the updated event.</summary>
        Private Sub SendLobbyState(ByVal randomSeed As UInt32)
            If _kernel.State >= GameState.Loading Then Return

            For Each player In _kernel.Players
                Contract.Assume(player IsNot Nothing)
                player.QueueSendPacket(Protocol.MakeLobbyState(receiver:=player,
                                                               layoutStyle:=_settings.Map.LayoutStyle,
                                                               slots:=_lobby.Slots,
                                                               randomSeed:=randomSeed,
                                                               hideSlots:=Settings.IsAdminGame))
            Next player
            TryBeginAutoStart()
        End Sub

        Public Function QueueTrySetTeamSizes(ByVal sizes As IList(Of Integer)) As Task
            Contract.Requires(sizes IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.TrySetTeamSizes(sizes))
        End Function

        Public Function QueueOpenSlot(ByVal slotQuery As InvariantString) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.OpenSlot(slotQuery))
        End Function
        Public Function QueueSetSlotCpu(ByVal slotQuery As InvariantString, ByVal newCpuLevel As Protocol.ComputerLevel) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.ComputerizeSlot(slotQuery, newCpuLevel))
        End Function
        Public Function QueueCloseSlot(ByVal slotQuery As InvariantString) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.CloseSlot(slotQuery))
        End Function
        Public Function QueueReserveSlot(ByVal userName As InvariantString,
                                         Optional ByVal slotQuery As InvariantString? = Nothing) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.ReserveSlot(userName, slotQuery))
        End Function
        Public Function QueueSwapSlotContents(ByVal slotQuery1 As InvariantString,
                                              ByVal slotQuery2 As InvariantString) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.SwapSlotContents(slotQuery1, slotQuery2))
        End Function
        Public Function QueueSetSlotColor(ByVal slotQuery As InvariantString, ByVal newColor As Protocol.PlayerColor) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.SetSlotColor(slotQuery, newColor))
        End Function
        Public Function QueueSetSlotRace(ByVal slotQuery As InvariantString, ByVal newRace As Protocol.Races) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.SetSlotRace(slotQuery, newRace))
        End Function
        Public Function QueueSetSlotTeam(ByVal slotQuery As InvariantString, ByVal newTeam As Byte) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.SetSlotTeam(slotQuery, newTeam))
        End Function
        Public Function QueueSetSlotHandicap(ByVal slotQuery As InvariantString, ByVal newHandicap As Byte) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.SetSlotHandicap(slotQuery, newHandicap))
        End Function
        Public Function QueueSetSlotLocked(ByVal slotQuery As InvariantString, ByVal newLockState As Slot.LockState) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.SetSlotLocked(slotQuery, newLockState))
        End Function
        Public Function QueueSetAllSlotsLocked(ByVal newLockState As Slot.LockState) As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lobby.SetAllSlotsLocked(newLockState))
        End Function
#End Region
    End Class
End Namespace
