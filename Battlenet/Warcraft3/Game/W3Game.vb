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

Imports HostBot.Warcraft3
Imports System.Runtime.CompilerServices

Namespace Warcraft3
    Partial Public NotInheritable Class W3Game
        Implements IW3Game

        Private ReadOnly server As W3Server
        Private ReadOnly map As W3Map
        Private ReadOnly name As String
        Private ReadOnly rand As New Random()
        Private ReadOnly slots As New List(Of W3Slot)
        Private ReadOnly ref As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly eventRef As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly logger As Logger
        Private Const PING_PERIOD As UShort = 5000
        Private ReadOnly pingTimer As New Timers.Timer(PING_PERIOD)
        Private state As W3GameStates = W3GameStates.AcceptingPlayers
        Private fakeHostPlayer As W3Player
        Private flagHasPlayerLeft As Boolean
        Private adminPlayer As W3Player
        Private ReadOnly players As New List(Of W3Player)
        Private ReadOnly index_map(0 To 12) As Byte

        Private Event PlayerAction(ByVal sender As IW3Game, ByVal player As W3Player, ByVal action As W3GameAction) Implements IW3Game.PlayerAction
        Private Event Updated(ByVal sender As IW3Game, ByVal slots As List(Of W3Slot)) Implements IW3Game.Updated
        Private Event PlayerTalked(ByVal sender As IW3Game, ByVal speaker As W3Player, ByVal text As String) Implements IW3Game.PlayerTalked
        Private Event PlayerLeft(ByVal sender As IW3Game, ByVal state As W3GameStates, ByVal leaver As W3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) Implements IW3Game.PlayerLeft
        Private Event StateChanged(ByVal sender As IW3Game, ByVal old_state As W3GameStates, ByVal new_state As W3GameStates) Implements IW3Game.ChangedState

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(server IsNot Nothing)
            Contract.Invariant(map IsNot Nothing)
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(slots IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eventRef IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(pingTimer IsNot Nothing)
            Contract.Invariant(players IsNot Nothing)
            Contract.Invariant(index_map IsNot Nothing)

            Contract.Invariant(readyPlayers IsNot Nothing)
            Contract.Invariant(unreadyPlayers IsNot Nothing)
            Contract.Invariant(visibleReadyPlayers IsNot Nothing)
            Contract.Invariant(visibleUnreadyPlayers IsNot Nothing)
            Contract.Invariant(fakeTickTimer IsNot Nothing)
            Contract.Invariant(downloadScheduler IsNot Nothing)
            Contract.Invariant(gameTime >= 0)
        End Sub

#Region "Commands"
        Private Sub CommandProcessLocalText(ByVal text As String, ByVal logger As Logger)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Select Case state
                Case Is < W3GameStates.Loading
                    server.parent.GameCommandsLobby.ProcessLocalText(Me, text, logger)
                Case W3GameStates.Loading
                    server.parent.GameCommandsLoadScreen.ProcessLocalText(Me, text, logger)
                Case Is > W3GameStates.Loading
                    server.parent.GameCommandsGamePlay.ProcessLocalText(Me, text, logger)
                Case Else
                    Throw New UnreachableException()
            End Select
        End Sub
        Private Function CommandProcessText(ByVal player As W3Player, ByVal arguments As IList(Of String)) As IFuture(Of String)
            Contract.Requires(arguments IsNot Nothing)
            Contract.Requires(player IsNot Nothing)
            Dim user = New BotUser(player.name)
            If player IsNot adminPlayer Then
                Select Case state
                    Case Is < W3GameStates.Loading
                        Return server.parent.GameGuestCommandsLobby.ProcessCommand(Me, user, arguments)
                    Case W3GameStates.Loading
                        Return server.parent.GameGuestCommandsLoadScreen.ProcessCommand(Me, user, arguments)
                    Case Is > W3GameStates.Loading
                        Return server.parent.GameGuestCommandsGamePlay.ProcessCommand(Me, user, arguments)
                    Case Else
                        Throw New UnreachableException()
                End Select
            ElseIf server.settings.isAdminGame Then
                Return server.parent.GameCommandsAdmin.ProcessCommand(Me, Nothing, arguments)
            Else
                Select Case state
                    Case Is < W3GameStates.Loading
                        Return server.parent.GameCommandsLobby.ProcessCommand(Me, user, arguments)
                    Case W3GameStates.Loading
                        Return server.parent.GameCommandsLoadScreen.ProcessCommand(Me, user, arguments)
                    Case Is > W3GameStates.Loading
                        Return server.parent.GameCommandsGamePlay.ProcessCommand(Me, user, arguments)
                    Case Else
                        Throw New UnreachableException()
                End Select
            End If
        End Function

        Private Sub ReceiveChat(ByVal sender As W3Player,
                                ByVal text As String,
                                ByVal type As W3Packet.ChatType,
                                ByVal receiverType As W3Packet.ChatReceiverType,
                                ByVal requestedReceiverIndexes As IList(Of Byte))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(requestedReceiverIndexes IsNot Nothing)

            'Log
            logger.Log(sender.name + ": " + text, LogMessageType.Typical)
            ThrowPlayerTalked(sender, text)

            'Forward to requested players
            'visible sender
            Dim visibleSender = GetVisiblePlayer(sender)
            If visibleSender IsNot sender Then
                text = visibleSender.name + ": " + text
            End If
            'packet
            Dim pk = W3Packet.MakeText(text, type, receiverType, players, visibleSender)
            'receivers
            For Each receiver In players
                Contract.Assume(receiver IsNot Nothing)
                Dim visibleReceiver = GetVisiblePlayer(receiver)
                If requestedReceiverIndexes.Contains(visibleReceiver.index) Then
                    receiver.QueueSendPacket(pk)
                ElseIf visibleReceiver Is visibleSender AndAlso sender IsNot receiver Then
                    receiver.QueueSendPacket(pk)
                End If
            Next receiver
        End Sub
        Private Sub ReceiveNonGameAction(ByVal sender As W3Player, ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
            Dim commandType = CType(vals("command type"), W3Packet.NonGameAction)

            'Player Chat
            Select Case commandType
                Case W3Packet.NonGameAction.GameChat, W3Packet.NonGameAction.LobbyChat
                    Dim message = CStr(vals("message"))
                    Dim chatType = If(commandType = W3Packet.NonGameAction.GameChat, W3Packet.ChatType.Game, W3Packet.ChatType.Lobby)
                    Dim receiverType As W3Packet.ChatReceiverType
                    If chatType = W3Packet.ChatType.Game Then
                        receiverType = CType(vals("receiver type"), W3Packet.ChatReceiverType)
                    End If
                    Dim receivingPlayerIndexes = CType(vals("receiving player indexes"), IList(Of Byte))

                    Contract.Assume(message IsNot Nothing)
                    Contract.Assume(receivingPlayerIndexes IsNot Nothing)

                    ReceiveChat(sender,
                                message,
                                chatType,
                                receiverType,
                                receivingPlayerIndexes)

                Case W3Packet.NonGameAction.SetTeam
                    ReceiveSetTeam(sender, CByte(vals("new value")))

                Case W3Packet.NonGameAction.SetHandicap
                    ReceiveSetHandicap(sender, CByte(vals("new value")))

                Case W3Packet.NonGameAction.SetRace
                    ReceiveSetRace(sender, CType(vals("new value"), W3Slot.RaceFlags))

                Case W3Packet.NonGameAction.SetColor
                    ReceiveSetColor(sender, CType(vals("new value"), W3Slot.PlayerColor))

                Case Else
                    RemovePlayer(sender, True, W3PlayerLeaveTypes.Disconnect, "Sent unrecognized client command type: {0}".Frmt(commandType))
            End Select
        End Sub
#End Region

#Region "Life"
        Public Sub New(ByVal parent As W3Server,
                       ByVal name As String,
                       ByVal map As W3Map,
                       ByVal arguments As IEnumerable(Of String),
                       Optional ByVal logger As Logger = Nothing)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(map IsNot Nothing)
            'Contract.Requires(parent IsNot Nothing)
            'Contract.Requires(name IsNot Nothing)
            Contract.Assume(map IsNot Nothing)
            Contract.Assume(parent IsNot Nothing)
            Contract.Assume(name IsNot Nothing)

            Me.map = map
            Me.server = parent
            Me.name = name
            Me.logger = If(logger, New Logger)
            For i = 0 To index_map.Length - 1
                index_map(i) = CByte(i)
            Next i

            LobbyNew(arguments)
            LoadScreenNew()
            GamePlayNew()

            AddHandler pingTimer.Elapsed, Sub() CatchPing()
            Me.pingTimer.Start()

            Contract.Assume(downloadScheduler IsNot Nothing)
            Contract.Assume(fakeTickTimer IsNot Nothing)
            Contract.Assume(visibleUnreadyPlayers IsNot Nothing)
            Contract.Assume(visibleReadyPlayers IsNot Nothing)
            Contract.Assume(unreadyPlayers IsNot Nothing)
            Contract.Assume(readyPlayers IsNot Nothing)
        End Sub

        '''<summary>Disconnects from all players and kills the instance. Passes hosting to a player if possible.</summary>
        Private Sub Close()
            If state >= W3GameStates.Closed Then
                Return
            End If
            pingTimer.Stop()

            'Pass hosting duty to another player if possible
            If state = W3GameStates.Playing AndAlso players.Count > 1 Then
                Dim host = players.Max(Function(p1, p2)
                                           If p1 Is Nothing Then  Return -1
                                           If p2 Is Nothing Then  Return 1
                                           If p1.isFake Then  Return -1
                                           If p2.isFake Then  Return 1

                                           'Can they host? Are they connected to more people? Are they on a better connection?
                                           Dim signs = {Math.Sign(p1.CanHost - p2.CanHost),
                                                        Math.Sign(p1.GetNumPeerConnections - p2.GetNumPeerConnections),
                                                        Math.Sign(p1.GetLatency - p2.GetLatency)}
                                           Return (From sign In signs Where sign <> 0).FirstOrDefault
                                       End Function)

                If host IsNot Nothing AndAlso Not host.isFake Then
                    BroadcastPacket(W3Packet.MakeSetHost(host.index), Nothing)
                    logger.Log(name + " has handed off hosting to " + host.name, LogMessageType.Positive)
                Else
                    logger.Log(name + " has failed to hand off hosting", LogMessageType.Negative)
                End If
            End If

            'disconnect from all players
            For Each p In players.ToList
                Contract.Assume(p IsNot Nothing)
                RemovePlayer(p, True, W3PlayerLeaveTypes.Disconnect, "Closing game")
            Next p

            ChangeState(W3GameStates.Closed)
        End Sub
#End Region

#Region "Events"
        Private Sub ThrowUpdated()
            Dim slots = (From slot In Me.slots Select slot.Cloned()).ToList()
            updateEventThrottle.SetActionToRun(Sub() eventRef.QueueAction(Sub()
                                                                              RaiseEvent Updated(Me, slots)
                                                                          End Sub))
        End Sub
        Private Sub ThrowPlayerTalked(ByVal speaker As W3Player, ByVal text As String)
            eventRef.QueueAction(Sub()
                                     RaiseEvent PlayerTalked(Me, speaker, text)
                                 End Sub)
        End Sub
        Private Sub ThrowPlayerLeft(ByVal leaver As W3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String)
            Dim state = Me.state
            eventRef.QueueAction(Sub()
                                     RaiseEvent PlayerLeft(Me, state, leaver, leaveType, reason)
                                 End Sub)
        End Sub
        Private Sub ThrowStateChanged(ByVal old_state As W3GameStates, ByVal new_state As W3GameStates)
            eventRef.QueueAction(Sub()
                                     RaiseEvent StateChanged(Me, old_state, new_state)
                                 End Sub)
        End Sub

        Private Sub CatchPing()
            ref.QueueAction(
                Sub()
                    Dim salt = CUInt(rand.Next(0, Integer.MaxValue))
                    Dim tick As ModInt32 = Environment.TickCount
                    Dim record = New W3PlayerPingRecord(salt, tick)
                    For Each player In players
                        player.QueuePing(record)
                    Next player
                    ThrowUpdated()
                End Sub
            )
        End Sub
#End Region

#Region "Access"
        Public Shared Function XvX(ByVal s As String) As IList(Of Integer)
            Contract.Requires(s IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IList(Of Integer))() IsNot Nothing)

            'parse numbers between 'v's
            Dim vals = s.ToLower().Split("v"c)
            Dim nums = New List(Of Integer)
            For Each e In vals
                Dim b As Byte
                Contract.Assume(e IsNot Nothing)
                If Not Byte.TryParse(e, b) Then
                    Throw New InvalidOperationException("Non-numeric team limit '{0}'.".Frmt(e))
                End If
                nums.Add(b)
            Next e
            Return nums
        End Function

        Private Sub ChangeState(ByVal newState As W3GameStates)
            Dim oldState = state
            state = newState
            ThrowStateChanged(oldState, newState)
        End Sub

        '''<summary>Broadcasts a packet to all players. Requires a packer for the packet, and values matching the packer.</summary>
        Private Sub BroadcastPacket(ByVal pk As W3Packet,
                                    Optional ByVal source As W3Player = Nothing)
            Contract.Requires(pk IsNot Nothing)
            For Each player In (From _player In players
                                Where _player IsNot source)
                player.QueueSendPacket(pk)
            Next player
        End Sub

        '''<summary>Returns the number of slots potentially available for new players.</summary>
        <Pure()> Private Function CountFreeSlots() As Integer
            Dim freeSlots = From slot In slots Where slot.contents.WantPlayer(Nothing) >= W3SlotContents.WantPlayerPriority.Accept
            Contract.Assume(freeSlots IsNot Nothing)
            Return (freeSlots).Count
        End Function

        '''<summary>Returns any slot matching a string. Checks index, color and player name.</summary>
        <Pure()>
        Private Function FindMatchingSlot(ByVal query As String) As W3Slot
            Contract.Requires(query IsNot Nothing)

            Dim bestSlot As W3Slot = Nothing
            Dim bestMatch = W3Slot.Match.None
            slots.MaxPair(Function(slot) slot.Matches(query), bestSlot, bestMatch)

            If bestMatch = W3Slot.Match.None Then
                Return Nothing
            Else
                Return bestSlot
            End If
        End Function

        '''<summary>Returns the slot containing the given player.</summary>
        <Pure()> Private Function FindPlayerSlot(ByVal player As W3Player) As W3Slot
            Contract.Requires(player IsNot Nothing)
            Return (From slot In slots
                    Where (From resident In slot.contents.EnumPlayers Where resident Is player).Any
                   ).FirstOrDefault
        End Function
#End Region

#Region "Messaging"
        '''<summary>Sends text to all players. Uses spoof chat if necessary.</summary>
        Private Sub BroadcastMessage(ByVal message As String,
                                     Optional ByVal playerToAvoid As W3Player = Nothing)
            Contract.Requires(message IsNot Nothing)
            For Each player In (From _player In players Where _player IsNot playerToAvoid)
                Contract.Assume(player IsNot Nothing)
                SendMessageTo(message, player, display:=False)
            Next player
            logger.Log("{0}: {1}".Frmt(My.Resources.ProgramName, message), LogMessageType.Typical)
        End Sub

        '''<summary>Sends text to the target player. Uses spoof chat if necessary.</summary>
        Private Sub SendMessageTo(ByVal message As String,
                                  ByVal player As W3Player,
                                  Optional ByVal display As Boolean = True)
            Contract.Requires(message IsNot Nothing)
            Contract.Requires(player IsNot Nothing)

            'Send Text
            'text comes from fake host or spoofs from the receiving player
            player.QueueSendPacket(W3Packet.MakeText(text:=If(fakeHostPlayer Is Nothing, My.Resources.ProgramName + ": ", "") + message,
                                                     chatType:=If(state >= W3GameStates.Loading, W3Packet.ChatType.Game, W3Packet.ChatType.Lobby),
                                                     receiverType:=W3Packet.ChatReceiverType.AllPlayers,
                                                     receivingPlayers:=players,
                                                     sender:=If(fakeHostPlayer, player)))

            If display Then
                logger.Log("(Private to {0}): {1}".Frmt(player.name, message), LogMessageType.Typical)
            End If
        End Sub
#End Region

#Region "Players"
        '''<summary>Removes the given player from the instance</summary>
        Private Sub RemovePlayer(ByVal player As W3Player,
                                 ByVal wasExpected As Boolean,
                                 ByVal leaveType As W3PlayerLeaveTypes,
                                 ByVal reason As String)
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            If Not players.Contains(player) Then
                Throw New InvalidOperationException("Player is not in the game.")
            End If

            'Clean slot
            Dim slot = FindPlayerSlot(player)
            If slot IsNot Nothing Then
                If slot.contents.EnumPlayers.Contains(player) Then
                    slot.contents = slot.contents.RemovePlayer(player)
                End If
            End If

            'Clean player
            If IsPlayerVisible(player) Then
                BroadcastPacket(W3Packet.MakeOtherPlayerLeft(player, leaveType), player)
            End If
            If player Is adminPlayer Then
                adminPlayer = Nothing
            End If
            player.QueueDisconnect(True, leaveType, reason)
            players.Remove(player)
            Select Case state
                Case Is < W3GameStates.Loading
                    LobbyCatchRemovedPlayer(player, slot)
                Case W3GameStates.Loading
                    LoadScreenCatchRemovedPlayer(player, slot)
                Case Is > W3GameStates.Loading
            End Select
            If player Is fakeHostPlayer Then
                fakeHostPlayer = Nothing
            End If

            'Clean game
            If state >= W3GameStates.Loading AndAlso Not (From x In players Where Not x.isFake).Any Then
                'the game has started and everyone has left, time to die
                Close()
            End If

            'Log
            If player.isFake Then
                logger.Log("{0} has been removed from the game. ({1})".Frmt(player.name, reason), LogMessageType.Negative)
            Else
                flagHasPlayerLeft = True
                If wasExpected Then
                    logger.Log("{0} has left the game. ({1})".Frmt(player.name, reason), LogMessageType.Negative)
                Else
                    logger.Log("{0} has disconnected. ({1})".Frmt(player.name, reason), LogMessageType.Problem)
                End If
                ThrowPlayerLeft(player, leaveType, reason)
            End If
        End Sub

        Private Sub TryElevatePlayer(ByVal name As String,
                                          Optional ByVal password As String = Nothing)
            Contract.Requires(name IsNot Nothing)

            Dim player = FindPlayer(name)
            If player Is Nothing Then Throw New InvalidOperationException("No player found with the name '{0}'.".Frmt(name))
            If adminPlayer IsNot Nothing Then Throw New InvalidOperationException("A player is already the admin.")
            If password IsNot Nothing Then
                player.numAdminTries += 1
                If player.numAdminTries > 5 Then Throw New InvalidOperationException("Too many tries.")
                If password.ToLower() <> server.settings.adminPassword.ToLower() Then Throw New InvalidOperationException("Incorrect password.")
            End If

            adminPlayer = player
            SendMessageTo("You are now the admin.", player)
        End Sub

        Private Function FindPlayer(ByVal username As String) As W3Player
            Contract.Requires(username IsNot Nothing)
            Dim username_ = username 'avoids contract verification problem on hoisted arguments
            Return (From x In players Where x.name.ToLower = username_.ToLower).FirstOrDefault
        End Function
        Private Function FindPlayer(ByVal index As Byte) As W3Player
            Return (From x In players Where x.index = index).FirstOrDefault
        End Function

        '''<summary>Boots players in the slot with the given index.</summary>
        Private Sub Boot(ByVal slotQuery As String)
            Contract.Requires(slotQuery IsNot Nothing)
            Dim slot = FindMatchingSlot(slotQuery)
            If slot IsNot Nothing Then Throw New InvalidOperationException("No slot {0}".Frmt(slotQuery))
            If Not slot.contents.EnumPlayers.Any Then
                Throw New InvalidOperationException("There is no player to boot in slot '{0}'.".Frmt(slotQuery))
            End If

            Dim slotQuery_ = slotQuery
            Dim target = (From player In slot.contents.EnumPlayers
                          Where player.name.ToLower() = slotQuery_.ToLower()).FirstOrDefault
            If target IsNot Nothing Then
                slot.contents = slot.contents.RemovePlayer(target)
                RemovePlayer(target, True, W3PlayerLeaveTypes.Disconnect, "Booted")
                Return
            End If

            For Each player In slot.contents.EnumPlayers
                Contract.Assume(player IsNot Nothing)
                slot.contents = slot.contents.RemovePlayer(player)
                RemovePlayer(player, True, W3PlayerLeaveTypes.Disconnect, "Booted")
            Next player
        End Sub
#End Region

#Region "Invisible Players"
        <Pure()> Private Function IsPlayerVisible(ByVal player As W3Player) As Boolean
            Contract.Requires(player IsNot Nothing)
            Return index_map(player.index) = player.index
        End Function
        <Pure()> Private Function GetVisiblePlayer(ByVal player As W3Player) As W3Player
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of W3Player)() IsNot Nothing)
            If IsPlayerVisible(player) Then Return player
            Dim visibleIndex = index_map(player.index)
            Dim visiblePlayer = (From p In players Where p.index = visibleIndex).First
            Contract.Assume(visiblePlayer IsNot Nothing)
            Return visiblePlayer
        End Function
        Private Shared Sub SetupCoveredSlot(ByVal coveringSlot As W3Slot,
                                            ByVal coveredSlot As W3Slot,
                                            ByVal playerIndex As Byte)
            Contract.Requires(coveringSlot IsNot Nothing)
            Contract.Requires(coveredSlot IsNot Nothing)
            Contract.Requires(playerIndex > 0)
            Contract.Requires(playerIndex <= 12)
            If coveringSlot.contents.EnumPlayers.Count <> 1 Then Throw New InvalidOperationException()
            If coveredSlot.contents.EnumPlayers.Any Then Throw New InvalidOperationException()
            Dim player = coveringSlot.contents.EnumPlayers.First
            Contract.Assume(player IsNot Nothing)
            coveringSlot.contents = New W3SlotContentsCovering(coveringSlot, coveredSlot, player)
            coveredSlot.contents = New W3SlotContentsCovered(coveredSlot, coveringSlot, playerIndex, coveredSlot.contents.EnumPlayers)
        End Sub
#End Region

#Region "Interface"
        Private ReadOnly Property _parent() As W3Server Implements IW3Game.server
            Get
                Return server
            End Get
        End Property
        Private ReadOnly Property _map() As W3Map Implements IW3Game.map
            Get
                Return map
            End Get
        End Property
        Private ReadOnly Property _name() As String Implements IW3Game.name
            Get
                Return name
            End Get
        End Property
        Private ReadOnly Property _logger() As Logger Implements IW3Game.logger
            Get
                Return logger
            End Get
        End Property

        Private Function _f_AdminPlayer() As IFuture(Of W3Player) Implements IW3Game.QueueGetAdminPlayer
            Return ref.QueueFunc(Function() adminPlayer)
        End Function
        Private Function _f_FakeHostPlayer() As IFuture(Of W3Player) Implements IW3Game.QueueGetFakeHostPlayer
            Return ref.QueueFunc(Function() fakeHostPlayer)
        End Function
        Private Function QueueCommandProcessLocalText(ByVal text As String, ByVal logger As Logger) As IFuture Implements IW3Game.QueueCommandProcessLocalText
            Return ref.QueueAction(Sub()
                                       Contract.Assume(text IsNot Nothing)
                                       Contract.Assume(logger IsNot Nothing)
                                       CommandProcessLocalText(text, logger)
                                   End Sub)
        End Function
        Private Function _QueueCommandProcessText(ByVal player As W3Player, ByVal arguments As IList(Of String)) As IFuture(Of String) Implements IW3Game.QueueProcessCommand
            Return ref.QueueFunc(Function()
                                     Contract.Assume(player IsNot Nothing)
                                     Contract.Assume(arguments IsNot Nothing)
                                     Return CommandProcessText(player, arguments)
                                 End Function).Defuturized
        End Function
        Private Function _QueueTryElevatePlayer(ByVal name As String, Optional ByVal password As String = Nothing) As IFuture Implements IW3Game.QueueTryElevatePlayer
            Return ref.QueueAction(Sub()
                                       Contract.Assume(name IsNot Nothing)
                                       Contract.Assume(password IsNot Nothing)
                                       TryElevatePlayer(name, password)
                                   End Sub)
        End Function
        Private Function _QueueFindPlayer(ByVal username As String) As IFuture(Of W3Player) Implements IW3Game.QueueFindPlayer
            Return ref.QueueFunc(Function()
                                     Contract.Assume(username IsNot Nothing)
                                     Return FindPlayer(username)
                                 End Function)
        End Function
        Private Function _QueueRemovePlayer(ByVal player As W3Player,
                                         ByVal expected As Boolean,
                                         ByVal leaveType As W3PlayerLeaveTypes,
                                         ByVal reason As String) As IFuture Implements IW3Game.QueueRemovePlayer
            Return ref.QueueAction(Sub()
                                       Contract.Assume(player IsNot Nothing)
                                       Contract.Assume(reason IsNot Nothing)
                                       RemovePlayer(player, expected, leaveType, reason)
                                   End Sub)
        End Function
        Private Function _QueueEnumPlayers() As IFuture(Of List(Of W3Player)) Implements IW3Game.QueueGetPlayers
            Return ref.QueueFunc(Function() players.ToList)
        End Function
        Private Function _QueueThrowUpdated() As IFuture Implements IW3Game.QueueThrowUpdated
            Return ref.QueueAction(AddressOf ThrowUpdated)
        End Function
        Private Function _QueueClose() As IFuture Implements IW3Game.QueueClose
            Return ref.QueueAction(Sub() Close())
        End Function
        Private Function _QueueBroadcastMessage(ByVal message As String) As IFuture Implements IW3Game.QueueBroadcastMessage
            Return ref.QueueAction(Sub()
                                       Contract.Assume(message IsNot Nothing)
                                       BroadcastMessage(message)
                                   End Sub)
        End Function
        Private Function _QueueSendMessageTo(ByVal message As String, ByVal player As W3Player) As IFuture Implements IW3Game.QueueSendMessageTo
            Return ref.QueueAction(Sub()
                                       Contract.Assume(message IsNot Nothing)
                                       Contract.Assume(player IsNot Nothing)
                                       SendMessageTo(message, player)
                                   End Sub)
        End Function
        Private Function _QueueBootSlot(ByVal slotQuery As String) As IFuture Implements IW3Game.QueueBootSlot
            Return ref.QueueAction(Sub()
                                       Contract.Assume(slotQuery IsNot Nothing)
                                       Boot(slotQuery)
                                   End Sub)
        End Function
        Private Function _QueueGetState() As IFuture(Of W3GameStates) Implements IW3Game.QueueGetState
            Return ref.QueueFunc(Function() Me.state)
        End Function
        Private Function _f_ReceiveNonGameAction(ByVal player As W3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture Implements IW3Game.QueueReceiveNonGameAction
            Return ref.QueueAction(Sub()
                                       Contract.Assume(player IsNot Nothing)
                                       Contract.Assume(vals IsNot Nothing)
                                       ReceiveNonGameAction(player, vals)
                                   End Sub)
        End Function
#End Region
    End Class
End Namespace
