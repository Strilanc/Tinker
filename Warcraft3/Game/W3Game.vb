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

        Public Shared ReadOnly GameGuestCommandsLobby As New WC3.InstanceGuestSetupCommands
        Public Shared ReadOnly GameGuestCommandsGamePlay As New WC3.InstanceGuestPlayCommands
        Public Shared ReadOnly GameCommandsGamePlay As New WC3.InstancePlayCommands
        Public Shared ReadOnly GameCommandsLobby As New WC3.InstanceSetupCommands

        Private ReadOnly _clock As IClock
        Private ReadOnly _map As Map
        Private ReadOnly _name As InvariantString
        Private ReadOnly slots As New List(Of Slot)
        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly _logger As Logger
        Private state As GameState = GameState.AcceptingPlayers
        Private fakeHostPlayer As Player
        Private flagHasPlayerLeft As Boolean
        Private adminPlayer As Player
        Private ReadOnly _players As New AsyncViewableCollection(Of Player)(outQueue:=outQueue)
        Private ReadOnly indexMap(0 To 12) As Byte
        Private ReadOnly settings As GameSettings

        Public Event PlayerAction(ByVal sender As Game, ByVal player As Player, ByVal action As GameAction)
        Public Event Updated(ByVal sender As Game, ByVal slots As List(Of Slot))
        Public Event PlayerTalked(ByVal sender As Game, ByVal speaker As Player, ByVal text As String)
        Public Event PlayerLeft(ByVal sender As Game, ByVal state As GameState, ByVal leaver As Player, ByVal leaveType As PlayerLeaveType, ByVal reason As String)
        Public Event ChangedState(ByVal sender As Game, ByVal oldState As GameState, ByVal newState As GameState)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_map IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(slots IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_players IsNot Nothing)
            Contract.Invariant(indexMap IsNot Nothing)
            Contract.Invariant(indexMap.Length = 13)

            Contract.Invariant(settings IsNot Nothing)
            Contract.Invariant(freeIndexes IsNot Nothing)
            Contract.Invariant(readyPlayers IsNot Nothing)
            Contract.Invariant(unreadyPlayers IsNot Nothing)
            Contract.Invariant(visibleReadyPlayers IsNot Nothing)
            Contract.Invariant(visibleUnreadyPlayers IsNot Nothing)
            Contract.Invariant(fakeTickTimer IsNot Nothing)
            Contract.Invariant(downloadTimer IsNot Nothing)
            Contract.Invariant(_downloadScheduler IsNot Nothing)
            Contract.Invariant(_gameTime >= 0)
            Contract.Invariant(laggingPlayers IsNot Nothing)
            Contract.Invariant(gameDataQueue IsNot Nothing)
            Contract.Invariant(updateEventThrottle IsNot Nothing)
            Contract.Invariant(slotStateUpdateThrottle IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal settings As GameSettings,
                       ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(map IsNot Nothing)
            'Contract.Requires(name IsNot Nothing)
            Contract.Assume(settings IsNot Nothing)

            Me.settings = settings
            Me._map = settings.Map
            Me._clock = clock
            Me._name = name
            Me._logger = If(logger, New Logger)
            For i = 0 To indexMap.Length - 1
                indexMap(i) = CByte(i)
            Next i

            LobbyNew()
            LoadScreenNew()
            GamePlayNew()
        End Sub

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(
                Sub()
                    ChangeState(GameState.Disposed)
                    For Each player In _players
                        player.Dispose()
                    Next player
                End Sub)
        End Function

        Public ReadOnly Property Logger As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property
        Public ReadOnly Property Map As Map
            Get
                Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)
                Return _map
            End Get
        End Property
        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property

        Private Sub ThrowUpdated()
            Dim slots = (From slot In Me.slots Select slot.Cloned()).ToList()
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
                    Return Game.GameGuestCommandsLobby.Invoke(Me, user, argument)
                Else
                    Return Game.GameGuestCommandsGamePlay.Invoke(Me, user, argument)
                End If
            ElseIf settings.IsAdminGame Then
                Return New WC3.InstanceAdminCommands(bot).Invoke(Me, Nothing, argument)
            Else
                If state < GameState.Loading Then
                    Return Game.GameCommandsLobby.Invoke(Me, user, argument)
                Else
                    Return Game.GameCommandsGamePlay.Invoke(Me, user, argument)
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

#Region "Access"
        Public Function QueueGetAdminPlayer() As IFuture(Of Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() adminPlayer)
        End Function
        Public Function QueueGetFakeHostPlayer() As IFuture(Of Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() fakeHostPlayer)
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
            outQueue.QueueAction(Sub() RaiseEvent ChangedState(Me, oldState, newState))
        End Sub

        '''<summary>Returns the number of slots potentially available for new players.</summary>
        <Pure()>
        Private Function CountFreeSlots() As Integer
            Return (From slot In slots
                    Where slot.AssumeNotNull.Contents.WantPlayer(Nothing) >= SlotContents.WantPlayerPriority.Open
                    ).Count
        End Function

        '''<summary>Returns any slot matching a string. Checks index, color and player name.</summary>
        <Pure()>
        Private Function FindMatchingSlot(ByVal query As InvariantString) As Slot
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Dim bestSlot As Slot = Nothing
            Dim bestMatch = Slot.Match.None * 3 - SlotContentType.Empty
            slots.MaxPair(Function(slot) slot.Matches(query) * 3 - slot.Contents.ContentType, bestSlot, bestMatch)
            If bestMatch = Slot.Match.None Then Throw New OperationFailedException("No matching slot found.")
            Contract.Assume(bestSlot IsNot Nothing)
            Return bestSlot
        End Function

        '''<summary>Returns the slot containing the given player.</summary>
        <Pure()>
        Private Function TryFindPlayerSlot(ByVal player As Player) As Slot
            Contract.Requires(player IsNot Nothing)
            Return (From slot In slots
                    Where (From resident In slot.Contents.EnumPlayers Where resident Is player).Any
                    ).FirstOrDefault
        End Function
#End Region

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
            Dim prefix = If(fakeHostPlayer Is Nothing, "{0}: ".Frmt(Application.ProductName), "")
            Dim chatType = If(state >= GameState.Loading, Protocol.ChatType.Game, Protocol.ChatType.Lobby)
            Dim sender = If(fakeHostPlayer, player)
            If Protocol.Jars.MaxChatTextLength - prefix.Length <= 0 Then
                Throw New InvalidStateException("The product name is so long there's no room for text to follow it!")
            End If
            For Each line In SplitText(body:=message, maxLineLength:=Protocol.Jars.MaxChatTextLength - prefix.Length)
                player.QueueSendPacket(Protocol.MakeText(text:=prefix + line,
                                                         chatType:=chatType,
                                                         receiverType:=Protocol.ChatReceiverType.Private,
                                                         receivingPlayers:=_players,
                                                         sender:=sender))
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
                                ByVal receiverType As Protocol.ChatReceiverType,
                                ByVal requestedReceiverIndexes As IReadableList(Of Byte))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(requestedReceiverIndexes IsNot Nothing)

            'Log
            Logger.Log("{0}: {1}".Frmt(sender.Name, text), LogMessageType.Typical)
            outQueue.QueueAction(Sub() RaiseEvent PlayerTalked(Me, sender, text))

            'Forward to requested players
            'visible sender
            Dim visibleSender = GetVisiblePlayer(sender)
            If visibleSender IsNot sender Then
                text = visibleSender.Name + ": " + text
            End If
            'packet
            Dim pk = Protocol.MakeText(text, type, receiverType, _players, visibleSender)
            'receivers
            For Each receiver In _players
                Contract.Assume(receiver IsNot Nothing)
                Dim visibleReceiver = GetVisiblePlayer(receiver)
                If requestedReceiverIndexes.Contains(visibleReceiver.Index) Then
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
                    Dim receiverType As Protocol.ChatReceiverType
                    If chatType = Protocol.ChatType.Game Then
                        receiverType = CType(vals("receiver type"), Protocol.ChatReceiverType)
                    End If
                    Dim receivingPlayerIndexes = CType(vals("receiving player indexes"), IReadableList(Of Byte)).AssumeNotNull

                    ReceiveChat(sender,
                                message,
                                chatType,
                                receiverType,
                                receivingPlayerIndexes)

                Case Protocol.NonGameAction.SetTeam
                    ReceiveSetTeam(sender, CByte(vals("new value")))

                Case Protocol.NonGameAction.SetHandicap
                    ReceiveSetHandicap(sender, CByte(vals("new value")))

                Case Protocol.NonGameAction.SetRace
                    ReceiveSetRace(sender, CType(vals("new value"), Slot.Races))

                Case Protocol.NonGameAction.SetColor
                    ReceiveSetColor(sender, CType(vals("new value"), Slot.PlayerColor))

                Case Else
                    RemovePlayer(sender, True, PlayerLeaveType.Disconnect, "Sent unrecognized client command type: {0}".Frmt(commandType))
            End Select
        End Sub
        Public Function QueueReceiveNonGameAction(ByVal player As Player,
                                                  ByVal values As Dictionary(Of InvariantString, Object)) As IFuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(values IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() ReceiveNonGameAction(player, values))
        End Function
#End Region

#Region "Players"
        '''<summary>Removes the given player from the instance</summary>
        Private Sub RemovePlayer(ByVal player As Player,
                                 ByVal wasExpected As Boolean,
                                 ByVal leaveType As PlayerLeaveType,
                                 ByVal reason As String)
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            If Not _players.Contains(player) Then
                Return
            End If

            'Clean slot
            Dim slot = TryFindPlayerSlot(player)
            If slot IsNot Nothing Then
                If slot.Contents.EnumPlayers.Contains(player) Then
                    slot.Contents = slot.Contents.RemovePlayer(player)
                End If
            End If

            'Clean player
            If IsPlayerVisible(player) Then
                BroadcastPacket(Protocol.MakeOtherPlayerLeft(player, leaveType), player)
            End If
            If player Is adminPlayer Then
                adminPlayer = Nothing
            End If
            player.QueueDisconnect(True, leaveType, reason)
            _players.Remove(player)
            Select Case state
                Case Is < GameState.Loading
                    LobbyCatchRemovedPlayer(player, slot)
                Case GameState.Loading
                    OnLoadScreenRemovedPlayer()
                Case Is > GameState.Loading
            End Select
            If player Is fakeHostPlayer Then
                fakeHostPlayer = Nothing
            End If

            'Clean game
            If state >= GameState.Loading AndAlso Not (From x In _players Where Not x.isFake).Any Then
                'the game has started and everyone has left, time to die
                Me.Dispose()
            End If

            'Log
            If player.isFake Then
                Logger.Log("{0} has been removed from the game. ({1})".Frmt(player.Name, reason), LogMessageType.Negative)
            Else
                flagHasPlayerLeft = True
                If wasExpected Then
                    Logger.Log("{0} has left the game. ({1})".Frmt(player.Name, reason), LogMessageType.Negative)
                Else
                    Logger.Log("{0} has disconnected. ({1})".Frmt(player.Name, reason), LogMessageType.Problem)
                End If
                Dim state_ = Me.state
                outQueue.QueueAction(Sub() RaiseEvent PlayerLeft(Me, state_, player, leaveType, reason))
            End If
        End Sub
        Public Function QueueRemovePlayer(ByVal player As Player,
                                          ByVal expected As Boolean,
                                          ByVal leaveType As PlayerLeaveType,
                                          ByVal reason As String) As IFuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() RemovePlayer(player, expected, leaveType, reason))
        End Function

        Private Sub ElevatePlayer(ByVal name As InvariantString,
                                  Optional ByVal password As String = Nothing)
            Dim player = TryFindPlayer(name)
            If player Is Nothing Then Throw New InvalidOperationException("No player found with the name '{0}'.".Frmt(name))
            If adminPlayer IsNot Nothing Then Throw New InvalidOperationException("A player is already the admin.")
            If password IsNot Nothing Then
                player.numAdminTries += 1
                If player.numAdminTries > 5 Then Throw New InvalidOperationException("Too many tries.")
                If password <> settings.AdminPassword Then
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

        Private Function TryFindPlayer(ByVal index As Byte) As Player
            Return (From player In _players
                    Where player.AssumeNotNull.Index = index).
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
        Private Sub Boot(ByVal slotQuery As InvariantString)
            Dim slot = FindMatchingSlot(slotQuery)
            If Not slot.Contents.EnumPlayers.Any Then
                Throw New InvalidOperationException("There is no player to boot in slot '{0}'.".Frmt(slotQuery))
            End If

            Dim target = (From player In slot.Contents.EnumPlayers Where player.Name = slotQuery).FirstOrDefault
            If target IsNot Nothing Then
                slot.Contents = slot.Contents.RemovePlayer(target)
                RemovePlayer(target, True, PlayerLeaveType.Disconnect, "Booted")
                Return
            End If

            For Each player In slot.Contents.EnumPlayers
                Contract.Assume(player IsNot Nothing)
                slot.Contents = slot.Contents.RemovePlayer(player)
                RemovePlayer(player, True, PlayerLeaveType.Disconnect, "Booted")
            Next player
        End Sub
        Public Function QueueBoot(ByVal slotQuery As InvariantString) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() Boot(slotQuery))
        End Function
#End Region

#Region "Invisible Players"
        <Pure()>
        Private Function IsPlayerVisible(ByVal player As Player) As Boolean
            Contract.Requires(player IsNot Nothing)
            Return indexMap(player.Index) = player.Index
        End Function
        <Pure()>
        Private Function GetVisiblePlayer(ByVal player As Player) As Player
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)
            If IsPlayerVisible(player) Then Return player
            Dim visibleIndex = indexMap(player.Index)
            Dim visiblePlayer = (From p In _players Where p.Index = visibleIndex).First
            Contract.Assume(visiblePlayer IsNot Nothing)
            Return visiblePlayer
        End Function
        Private Shared Sub SetupCoveredSlot(ByVal coveringSlot As Slot,
                                            ByVal coveredSlot As Slot,
                                            ByVal playerIndex As Byte)
            Contract.Requires(coveringSlot IsNot Nothing)
            Contract.Requires(coveredSlot IsNot Nothing)
            Contract.Requires(playerIndex > 0)
            Contract.Requires(playerIndex <= 12)
            If coveringSlot.Contents.EnumPlayers.Count <> 1 Then Throw New InvalidOperationException()
            If coveredSlot.Contents.EnumPlayers.Any Then Throw New InvalidOperationException()
            Dim player = coveringSlot.Contents.EnumPlayers.First
            Contract.Assume(player IsNot Nothing)
            coveringSlot.Contents = New SlotContentsCovering(coveringSlot, coveredSlot, player)
            coveredSlot.Contents = New SlotContentsCovered(coveredSlot, coveringSlot, playerIndex, coveredSlot.Contents.EnumPlayers)
        End Sub
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
    End Class
End Namespace
