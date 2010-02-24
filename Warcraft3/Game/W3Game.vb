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
    Public Enum GameState
        AcceptingPlayers = 0
        PreCounting = 1
        CountingDown = 2
        Loading = 3
        Playing = 4
        Disposed = 5
    End Enum

    Partial Public NotInheritable Class Game
        Inherits FutureDisposable
        Implements IGameDownloadAspect

        Public Shared ReadOnly GuestLobbyCommands As Commands.CommandSet(Of Game) = GameCommands.MakeGuestLobbyCommands()
        Public Shared ReadOnly GuestInGameCommands As Commands.CommandSet(Of Game) = GameCommands.MakeGuestInGameCommands()
        Public Shared ReadOnly HostInGameCommands As Commands.CommandSet(Of Game) = GameCommands.MakeHostInGameCommands()
        Public Shared ReadOnly HostLobbyCommands As Commands.CommandSet(Of Game) = GameCommands.MakeHostLobbyCommands()

        Private ReadOnly _lobby As GameLobby
        Private ReadOnly slotStateUpdateThrottle As New Throttle(cooldown:=250.Milliseconds, clock:=New SystemClock())
        Private ReadOnly updateEventThrottle As New Throttle(cooldown:=100.Milliseconds, clock:=New SystemClock())
        Private ReadOnly _clock As IClock
        Private ReadOnly _name As InvariantString
        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly _logger As Logger
        Private state As GameState = GameState.AcceptingPlayers
        Private flagHasPlayerLeft As Boolean
        Private adminPlayer As Player
        Private ReadOnly _players As New AsyncViewableCollection(Of Player)(outQueue:=outQueue)
        Private ReadOnly _settings As GameSettings
        Private ReadOnly _motor As GameMotor

        Public Event Updated(ByVal sender As Game, ByVal slots As SlotSet)
        Public Event PlayerTalked(ByVal sender As Game, ByVal speaker As Player, ByVal text As String, ByVal receivingGroup As Protocol.ChatGroup?)
        Public Event PlayerLeft(ByVal sender As Game, ByVal state As GameState, ByVal leaver As Player, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String)
        Public Event ChangedState(ByVal sender As Game, ByVal oldState As GameState, ByVal newState As GameState)
        Public Event ReceivedPlayerActions(ByVal sender As Game, ByVal player As Player, ByVal actions As IReadableList(Of Protocol.GameAction))
        Public Event Tick(ByVal sender As Game, ByVal timeSpan As UShort, ByVal actionSets As IReadableList(Of Tuple(Of Player, Protocol.PlayerActionSet)))

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_players IsNot Nothing)

            Contract.Invariant(_settings IsNot Nothing)
            Contract.Invariant(readyPlayers IsNot Nothing)
            Contract.Invariant(unreadyPlayers IsNot Nothing)
            Contract.Invariant(visibleReadyPlayers IsNot Nothing)
            Contract.Invariant(visibleUnreadyPlayers IsNot Nothing)
            Contract.Invariant(fakeTickTimer IsNot Nothing)
            Contract.Invariant(_lobby IsNot Nothing)
            Contract.Invariant(_motor IsNot Nothing)
            Contract.Invariant(updateEventThrottle IsNot Nothing)
            Contract.Invariant(slotStateUpdateThrottle IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal settings As GameSettings,
                       ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(clock IsNot Nothing)
            Contract.Assume(settings IsNot Nothing)

            Me._settings = settings
            Me._clock = clock
            Me._name = name
            Me._logger = If(logger, New Logger)
            Me._motor = New GameMotor(My.Settings.game_speed_factor,
                                      CUInt(My.Settings.game_tick_period).Milliseconds,
                                      CUInt(My.Settings.game_lag_limit).Milliseconds,
                                      Me.inQueue,
                                      Me._players,
                                      Me._clock,
                                      Me._lobby)

            Dim startPlayerholdPoint = New HoldPoint(Of Player)
            Dim downloadManager = New DownloadManager(clock:=_clock,
                                                      Game:=Me,
                                                      startPlayerholdPoint:=startPlayerholdPoint,
                                                      allowDownloads:=settings.AllowDownloads,
                                                      allowUploads:=settings.AllowUpload)
            _lobby = New GameLobby(startPlayerholdPoint, downloadManager, _logger, _players, _clock, settings)
            inQueue.QueueAction(Sub() _lobby.TryRestoreFakeHost())

            _lobby.StartPlayerHoldPoint.IncludeActionhandler(
                Sub(newPlayer)
                    AddHandler newPlayer.Disconnected, Sub(player, expected, reportedReason, reasonDescription) inQueue.QueueAction(Sub() RemovePlayer(player, expected, reportedReason, reasonDescription))
                    AddHandler newPlayer.ReceivedReady, Sub(player) inQueue.QueueAction(Sub() ReceiveReady(player))
                    AddHandler newPlayer.SuperficialStateUpdated, Sub() QueueThrowUpdated()
                    AddHandler newPlayer.StateUpdated, Sub() inQueue.QueueAction(AddressOf _lobby.ThrowChangedPublicState)
                    AddHandler newPlayer.ReceivedNonGameAction, Sub(player, values) inQueue.QueueAction(Sub() ReceiveNonGameAction(player, values))

                    If settings.Greeting <> "" Then
                        SendMessageTo(message:=settings.Greeting, player:=newPlayer, display:=False)
                    End If
                    If settings.AutoElevateUserName IsNot Nothing AndAlso newPlayer.Name = settings.AutoElevateUserName.Value Then
                        ElevatePlayer(newPlayer.Name)
                    End If
                    TryBeginAutoStart()
                End Sub)
            AddHandler _lobby.ChangedPublicState, Sub(sender)
                                                      ThrowUpdated()
                                                      slotStateUpdateThrottle.SetActionToRun(Sub() inQueue.QueueAction(Sub() SendLobbyState(Environment.TickCount)))
                                                  End Sub
            AddHandler _lobby.RemovePlayer, Sub(sender, player, wasExpected, reportedReason, reasonDescription) RemovePlayer(player, wasExpected, reportedReason, reasonDescription)
            AddHandler _motor.RemovePlayer, Sub(sender, player, wasExpected, reportedReason, reasonDescription) RemovePlayer(player, wasExpected, reportedReason, reasonDescription)
            AddHandler _motor.Tick, Sub(sender, timeSpan, actionSets) outQueue.QueueAction(Sub() RaiseEvent Tick(Me, timeSpan, actionSets))
            AddHandler _motor.ReceivedPlayerActions, Sub(sender, player, actions) outQueue.QueueAction(Sub() RaiseEvent ReceivedPlayerActions(Me, player, actions))

            LoadScreenNew()
        End Sub

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            Return outQueue.QueueAction(Sub() inQueue.QueueAction(
                Sub()
                    ChangeState(GameState.Disposed)
                    _lobby.Dispose()
                    _motor.Dispose()
                    For Each player In _players
                        player.Dispose()
                    Next player
                End Sub))
        End Function

        Public ReadOnly Property Logger As Logger Implements IGameDownloadAspect.Logger
            Get
                Return _logger
            End Get
        End Property
        Public ReadOnly Property Map As Map Implements IGameDownloadAspect.Map
            Get
                Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)
                Return _settings.Map
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
            updateEventThrottle.SetActionToRun(Sub() outQueue.QueueAction(Sub() RaiseEvent Updated(Me, slots)))
        End Sub
        Public Function QueueThrowUpdated() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(AddressOf ThrowUpdated)
        End Function

        Private Function CommandProcessText(ByVal bot As Bot.MainBot,
                                            ByVal player As Player,
                                            ByVal argument As String) As IFuture(Of String)
            Contract.Requires(bot IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Dim user = If(player Is Nothing, Nothing, New BotUser(player.Name))
            If player IsNot adminPlayer AndAlso player IsNot Nothing Then
                If state < GameState.Loading Then
                    Return Game.GuestLobbyCommands.Invoke(Me, user, argument)
                Else
                    Return Game.GuestInGameCommands.Invoke(Me, user, argument)
                End If
            ElseIf Settings.IsAdminGame Then
                Return GameCommands.MakeBotAdminCommands(bot).Invoke(Me, Nothing, argument)
            Else
                If state < GameState.Loading Then
                    Return Game.HostLobbyCommands.Invoke(Me, user, argument)
                Else
                    Return Game.HostInGameCommands.Invoke(Me, user, argument)
                End If
            End If
        End Function
        Public Function QueueCommandProcessText(ByVal bot As Bot.MainBot,
                                        ByVal player As Player,
                                        ByVal argument As String) As IFuture(Of String)
            Contract.Requires(bot IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CommandProcessText(bot, player, argument)).Defuturized
        End Function

        Public Function QueueGetAdminPlayer() As IFuture(Of Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() adminPlayer)
        End Function
        Public Function QueueGetFakeHostPlayer() As IFuture(Of Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _lobby.FakeHostPlayer)
        End Function
        Public Function QueueGetPlayers() As IFuture(Of List(Of Player))
            Contract.Ensures(Contract.Result(Of IFuture(Of List(Of Player)))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _players.ToList)
        End Function
        Public Function QueueGetState() As IFuture(Of GameState)
            Contract.Ensures(Contract.Result(Of IFuture(Of GameState))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() Me.state)
        End Function

        Private Sub ChangeState(ByVal newState As GameState)
            Dim oldState = state
            state = newState
            _lobby._acceptingPlayers = state = GameState.AcceptingPlayers
            outQueue.QueueAction(Sub() RaiseEvent ChangedState(Me, oldState, newState))
        End Sub

#Region "Networking"
        '''<summary>Broadcasts a packet to all players. Requires a packer for the packet, and values matching the packer.</summary>
        Private Sub BroadcastPacket(ByVal pk As Protocol.Packet,
                                    Optional ByVal source As Player = Nothing)
            Contract.Requires(pk IsNot Nothing)
            For Each player In (From _player In _players Where _player IsNot source)
                Contract.Assume(player IsNot Nothing)
                player.QueueSendPacket(pk)
            Next player
        End Sub

        '''<summary>Sends text to all players. Uses spoof chat if necessary.</summary>
        Private Sub BroadcastMessage(ByVal message As String,
                                     Optional ByVal playerToAvoid As Player = Nothing,
                                     Optional ByVal messageType As LogMessageType = LogMessageType.Typical)
            Contract.Requires(message IsNot Nothing)
            For Each player In (From _player In _players Where _player IsNot playerToAvoid)
                SendMessageTo(message, player.AssumeNotNull, display:=False)
            Next player
            Logger.Log("{0}: {1}".Frmt(Application.ProductName, message), messageType)
        End Sub
        Public Function QueueBroadcastMessage(ByVal message As String) As IFuture
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() BroadcastMessage(message))
        End Function

        '''<summary>Sends text to the target player. Uses spoof chat if necessary.</summary>
        Private Sub SendMessageTo(ByVal message As String,
                                  ByVal player As Player,
                                  Optional ByVal display As Boolean = True)
            Contract.Requires(message IsNot Nothing)
            Contract.Requires(player IsNot Nothing)

            'Send Text (from fake host or spoofed from receiver)
            Dim prefix = If(_lobby.FakeHostPlayer Is Nothing, "{0}: ".Frmt(Application.ProductName), "")
            Dim chatType = If(state >= GameState.Loading, Protocol.ChatType.Game, Protocol.ChatType.Lobby)
            Dim sender = If(_lobby.FakeHostPlayer, player)
            If Protocol.Packets.MaxChatTextLength - prefix.Length <= 0 Then
                Throw New InvalidStateException("The product name is so long there's no room for text to follow it!")
            End If
            For Each line In SplitText(body:=message, maxLineLength:=Protocol.Packets.MaxChatTextLength - prefix.Length)
                player.QueueSendPacket(Protocol.MakeText(text:=prefix + line,
                                                         chatType:=chatType,
                                                         receivingGroup:=Protocol.ChatGroup.Private,
                                                         receivers:=(From p In _players Select p.PID),
                                                         sender:=sender.PID))
            Next line

            If display Then
                Logger.Log("(Private to {0}): {1}".Frmt(player.Name, message), LogMessageType.Typical)
            End If
        End Sub
        Public Function QueueSendMessageTo(ByVal message As String, ByVal player As Player) As IFuture
            Contract.Requires(message IsNot Nothing)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendMessageTo(message, player))
        End Function

        Private Sub ReceiveChat(ByVal sender As Player,
                                ByVal text As String,
                                ByVal type As Protocol.ChatType,
                                ByVal receivingGroup As Protocol.ChatGroup?,
                                ByVal requestedReceiverIndexes As IReadableList(Of PID))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(requestedReceiverIndexes IsNot Nothing)

            'Log
            Logger.Log("{0}: {1}".Frmt(sender.Name, text), LogMessageType.Typical)
            outQueue.QueueAction(Sub() RaiseEvent PlayerTalked(Me, sender, text, receivingGroup))

            'Forward to requested players
            'visible sender
            Dim visibleSender = _lobby.GetVisiblePlayer(sender)
            If visibleSender IsNot sender Then
                text = visibleSender.Name + ": " + text
            End If
            'packet
            Dim pk = Protocol.MakeText(text, type, receivingGroup, requestedReceiverIndexes, visibleSender.PID)
            'receivers
            For Each receiver In _players
                Contract.Assume(receiver IsNot Nothing)
                Dim visibleReceiver = _lobby.GetVisiblePlayer(receiver)
                If requestedReceiverIndexes.Contains(visibleReceiver.PID) Then
                    receiver.QueueSendPacket(pk)
                ElseIf visibleReceiver Is visibleSender AndAlso sender IsNot receiver Then
                    receiver.QueueSendPacket(pk)
                End If
            Next receiver
        End Sub
        Private Sub ReceiveNonGameAction(ByVal sender As Player, ByVal vals As Dictionary(Of InvariantString, Object))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
            Dim commandType = CType(vals("command type"), Protocol.NonGameAction)

            'Player Chat
            Select Case commandType
                Case Protocol.NonGameAction.GameChat, Protocol.NonGameAction.LobbyChat
                    Dim message = CStr(vals("message")).AssumeNotNull
                    Dim chatType = If(commandType = Protocol.NonGameAction.GameChat, Protocol.ChatType.Game, Protocol.ChatType.Lobby)
                    Dim receivingGroup As Protocol.ChatGroup
                    If chatType = Protocol.ChatType.Game Then
                        receivingGroup = CType(vals("receiving group"), Protocol.ChatGroup)
                    End If
                    Dim receivingPlayerIndexes = CType(vals("receiving player indexes"), IReadableList(Of Byte)).AssumeNotNull
                    Dim receivingPIDs = (From index In receivingPlayerIndexes
                                         Select New PID(index)).ToArray.AsReadableList

                    ReceiveChat(sender,
                                message,
                                chatType,
                                receivingGroup,
                                receivingPIDs)

                Case Protocol.NonGameAction.SetTeam
                    _lobby.OnPlayerSetTeam(sender, CByte(vals("new value")))

                Case Protocol.NonGameAction.SetHandicap
                    _lobby.OnPlayerSetHandicap(sender, CByte(vals("new value")))

                Case Protocol.NonGameAction.SetRace
                    _lobby.OnPlayerSetRace(sender, CType(vals("new value"), Protocol.Races))

                Case Protocol.NonGameAction.SetColor
                    _lobby.OnPlayerSetColor(sender, CType(vals("new value"), Protocol.PlayerColor))

                Case Else
                    RemovePlayer(sender, True, Protocol.PlayerLeaveReason.Disconnect, "Sent unrecognized client command type: {0}".Frmt(commandType))
            End Select
        End Sub
#End Region

#Region "Players"
        '''<summary>Removes the given player from the instance</summary>
        Private Sub RemovePlayer(ByVal player As Player,
                                 ByVal wasExpected As Boolean,
                                 ByVal reportedReason As Protocol.PlayerLeaveReason,
                                 ByVal reasonDescription As String)
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(reasonDescription IsNot Nothing)
            If Not _players.Contains(player) Then
                Return
            End If

            'Clean slot
            Dim slot = _lobby.Slots.TryFindPlayerSlot(player)
            If slot IsNot Nothing Then
                If slot.Contents.EnumPlayers.Contains(player) Then
                    slot = slot.WithContents(slot.Contents.WithoutPlayer(player))
                    _lobby.Slots = _lobby.Slots.WithSlotsReplaced(slot)
                End If
            End If

            'Clean player
            If _lobby.IsPlayerVisible(player) Then
                BroadcastPacket(Protocol.MakeOtherPlayerLeft(player.PID, reportedReason), player)
            End If
            If player Is adminPlayer Then
                adminPlayer = Nothing
            End If
            player.QueueDisconnect(True, reportedReason, reasonDescription)
            _players.Remove(player)
            Select Case state
                Case Is < GameState.Loading
                    _lobby.LobbyCatchRemovedPlayer(player, slot)
                Case GameState.Loading
                    OnLoadScreenRemovedPlayer()
                Case Is > GameState.Loading
            End Select

            'Clean game
            If state >= GameState.Loading AndAlso Not (From x In _players Where Not x.isFake).Any Then
                'the game has started and everyone has left, time to die
                Me.Dispose()
            End If

            'Log
            If player.isFake Then
                Logger.Log("{0} has been removed from the game. ({1})".Frmt(player.Name, reasonDescription), LogMessageType.Negative)
            Else
                flagHasPlayerLeft = True
                If wasExpected Then
                    Logger.Log("{0} has left the game. ({1})".Frmt(player.Name, reasonDescription), LogMessageType.Negative)
                Else
                    Logger.Log("{0} has disconnected. ({1})".Frmt(player.Name, reasonDescription), LogMessageType.Problem)
                End If
                Dim state_ = Me.state
                outQueue.QueueAction(Sub() RaiseEvent PlayerLeft(Me, state_, player, reportedReason, reasonDescription))
            End If
        End Sub

        Private Sub ElevatePlayer(ByVal name As InvariantString,
                                  Optional ByVal password As String = Nothing)
            Dim player = TryFindPlayer(name)
            If player Is Nothing Then Throw New InvalidOperationException("No player found with the name '{0}'.".Frmt(name))
            If adminPlayer IsNot Nothing Then Throw New InvalidOperationException("A player is already the admin.")
            If password IsNot Nothing Then
                player.adminAttemptCount += 1
                If player.adminAttemptCount > 5 Then Throw New InvalidOperationException("Too many tries.")
                If password <> Settings.AdminPassword Then
                    Throw New InvalidOperationException("Incorrect password.")
                End If
            End If

            adminPlayer = player
            SendMessageTo("You are now the admin.", player)
        End Sub
        Public Function QueueElevatePlayer(ByVal name As InvariantString,
                                           Optional ByVal password As String = Nothing) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() ElevatePlayer(name, password))
        End Function

        Private Function TryFindPlayer(ByVal pid As PID) As Player
            Return (From player In _players
                    Where player.AssumeNotNull.PID = pid).
                    FirstOrDefault
        End Function
        Private Function TryFindPlayer(ByVal userName As InvariantString) As Player
            Return (From player In _players
                    Where player.Name = userName
                    ).FirstOrDefault
        End Function
        Public Function QueueTryFindPlayer(ByVal userName As InvariantString) As IFuture(Of Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() TryFindPlayer(userName))
        End Function

        '''<summary>Boots players in the slot with the given index.</summary>
        Private Sub Boot(ByVal slotQuery As InvariantString, ByVal shouldCloseEmptiedSlot As Boolean)
            Dim slot = _lobby.FindMatchingSlot(slotQuery)
            If Not slot.Contents.EnumPlayers.Any Then
                Throw New InvalidOperationException("There is no player to boot in slot '{0}'.".Frmt(slotQuery))
            End If

            Dim target = (From player In slot.Contents.EnumPlayers Where player.Name = slotQuery).FirstOrDefault
            If target IsNot Nothing Then
                _lobby.Slots = _lobby.Slots.WithSlotsReplaced(slot.WithContents(slot.Contents.WithoutPlayer(target)))
                RemovePlayer(target, True, Protocol.PlayerLeaveReason.Disconnect, "Booted")
            Else
                For Each player In slot.Contents.EnumPlayers
                    Contract.Assume(player IsNot Nothing)
                    _lobby.Slots = _lobby.Slots.WithSlotsReplaced(slot.WithContents(slot.Contents.WithoutPlayer(player)))
                    RemovePlayer(player, True, Protocol.PlayerLeaveReason.Disconnect, "Booted")
                Next player
            End If

            If shouldCloseEmptiedSlot AndAlso slot.Contents.ContentType = SlotContents.Type.Empty Then
                _lobby.Slots = _lobby.Slots.WithSlotsReplaced(slot.WithContents(New SlotContentsClosed))
                _lobby.ThrowChangedPublicState()
            End If
        End Sub
        Public Function QueueBoot(ByVal slotQuery As InvariantString, ByVal shouldCloseEmptiedSlot As Boolean) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() Boot(slotQuery, shouldCloseEmptiedSlot))
        End Function
#End Region

        Private Function CreatePlayersAsyncView(ByVal adder As Action(Of Game, Player),
                                                ByVal remover As Action(Of Game, Player)) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _players.BeginSync(adder:=Sub(sender, item) adder(Me, item),
                                      remover:=Sub(sender, item) remover(Me, item))
        End Function
        Public Function QueueCreatePlayersAsyncView(ByVal adder As Action(Of Game, Player),
                                                    ByVal remover As Action(Of Game, Player)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CreatePlayersAsyncView(adder, remover))
        End Function
        Public Function QueueCreatePlayersAsyncView(ByVal adder As Action(Of IGameDownloadAspect, IPlayerDownloadAspect),
                                                    ByVal remover As Action(Of IGameDownloadAspect, IPlayerDownloadAspect)) As IFuture(Of IDisposable) _
                                                    Implements IGameDownloadAspect.QueueCreatePlayersAsyncView
            Return inQueue.QueueFunc(Function() CreatePlayersAsyncView(adder, remover))
        End Function

#Region "Advancing State"
        '''<summary>Autostarts the countdown if autostart is enabled and the game stays full for awhile.</summary>
        Private Function TryBeginAutoStart() As Boolean
            'Sanity check
            If Not Settings.IsAutoStarted Then Return False
            If _lobby.CountFreeSlots() > 0 Then Return False
            If state >= GameState.PreCounting Then Return False
            If (From player In _players Where Not player.isFake AndAlso player.AdvertisedDownloadPercent <> 100).Any Then
                Return False
            End If
            ChangeState(GameState.PreCounting)

            'Give people a few seconds to realize the game is full before continuing
            Call _clock.AsyncWait(3.Seconds).QueueCallWhenReady(inQueue,
                Sub()
                    If state <> GameState.PreCounting Then Return
                    If Not Settings.IsAutoStarted OrElse _lobby.CountFreeSlots() > 0 Then
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
                                        _lobby.TryRestoreFakeHost()
                                        ChangeState(GameState.AcceptingPlayers)
                                        _lobby.ThrowChangedPublicState()
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
                Dim slot = _lobby.Slots.TryFindPlayerSlot(player)
                If slot Is Nothing OrElse slot.Contents.Moveable Then
                    RemovePlayer(player, True, Protocol.PlayerLeaveReason.Disconnect, "Fake players removed before loading")
                End If
            Next player

            _lobby.Slots = _lobby.Slots.WithEncodeHCL(Settings)
            Dim randomSeed As ModInt32 = Environment.TickCount()
            SendLobbyState(randomSeed)

            If Settings.ShouldRecordReplay Then
                Replay.ReplayManager.StartRecordingFrom(Settings.DefaultReplayFileName, Me, _players.ToList, _lobby.Slots, randomSeed)
            End If

            ChangeState(GameState.Loading)
            _lobby.Dispose()
            LoadScreenStart()
        End Sub
#End Region

#Region "Lobby"
        Private Sub SetPlayerVoteToStart(ByVal name As InvariantString, ByVal val As Boolean)
            If Not Settings.IsAutoStarted Then Throw New InvalidOperationException("Game is not set to start automatically.")
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
        Public ReadOnly Property StartPlayerHoldPoint As IHoldPoint(Of Player)
            Get
                Contract.Ensures(Contract.Result(Of IHoldPoint(Of Player))() IsNot Nothing)
                Return _lobby.StartPlayerHoldPoint
            End Get
        End Property
        Public Function QueueAddPlayer(ByVal newPlayer As W3ConnectingPlayer) As IFuture(Of Player)
            Contract.Requires(newPlayer IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _lobby.AddPlayer(newPlayer))
        End Function
        Public Function QueueSendMapPiece(ByVal player As IPlayerDownloadAspect,
                                          ByVal position As UInt32) As IFuture Implements IGameDownloadAspect.QueueSendMapPiece
            Return inQueue.QueueFunc(Function() _lobby.SendMapPiece(player, position)).Defuturized
        End Function

        '''<summary>Broadcasts new game state to players, and throws the updated event.</summary>
        Private Sub SendLobbyState(ByVal randomSeed As ModInt32)
            If state >= GameState.Loading Then Return

            For Each player In _players
                Contract.Assume(player IsNot Nothing)
                player.QueueSendPacket(Protocol.MakeLobbyState(receiver:=player,
                                                               layoutStyle:=Map.LayoutStyle,
                                                               slots:=_lobby.Slots,
                                                               randomSeed:=randomSeed,
                                                               hideSlots:=Settings.IsAdminGame))
            Next player
            TryBeginAutoStart()
        End Sub

        Public Function QueueTrySetTeamSizes(ByVal sizes As IList(Of Integer)) As IFuture
            Contract.Requires(sizes IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.TrySetTeamSizes(sizes))
        End Function

        Public Function QueueOpenSlot(ByVal slotQuery As InvariantString) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.OpenSlot(slotQuery))
        End Function
        Public Function QueueSetSlotCpu(ByVal slotQuery As InvariantString, ByVal newCpuLevel As Protocol.ComputerLevel) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.ComputerizeSlot(slotQuery, newCpuLevel))
        End Function
        Public Function QueueCloseSlot(ByVal slotQuery As InvariantString) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.CloseSlot(slotQuery))
        End Function
        Public Function QueueReserveSlot(ByVal userName As InvariantString,
                                         Optional ByVal slotQuery As InvariantString? = Nothing) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.ReserveSlot(userName, slotQuery))
        End Function
        Public Function QueueSwapSlotContents(ByVal slotQuery1 As InvariantString,
                                              ByVal slotQuery2 As InvariantString) As IFuture
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.SwapSlotContents(slotQuery1, slotQuery2))
        End Function
        Public Function QueueSetSlotColor(ByVal slotQuery As InvariantString, ByVal newColor As Protocol.PlayerColor) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.SetSlotColor(slotQuery, newColor))
        End Function
        Public Function QueueSetSlotRace(ByVal slotQuery As InvariantString, ByVal newRace As Protocol.Races) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.SetSlotRace(slotQuery, newRace))
        End Function
        Public Function QueueSetSlotTeam(ByVal slotQuery As InvariantString, ByVal newTeam As Byte) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.SetSlotTeam(slotQuery, newTeam))
        End Function
        Public Function QueueSetSlotHandicap(ByVal slotQuery As InvariantString, ByVal newHandicap As Byte) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.SetSlotHandicap(slotQuery, newHandicap))
        End Function
        Public Function QueueSetSlotLocked(ByVal slotQuery As InvariantString, ByVal newLockState As Slot.LockState) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.SetSlotLocked(slotQuery, newLockState))
        End Function
        Public Function QueueSetAllSlotsLocked(ByVal newLockState As Slot.LockState) As IFuture
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lobby.SetAllSlotsLocked(newLockState))
        End Function
#End Region
    End Class
End Namespace
