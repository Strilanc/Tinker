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

        Private ReadOnly server As IW3Server
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
        Private fakeHostPlayer As IW3Player
        Private flagHasPlayerLeft As Boolean
        Private adminPlayer As IW3Player
        Private ReadOnly players As New List(Of IW3Player)
        Private ReadOnly index_map(0 To 12) As Byte
        Private ReadOnly updateEventThrottle As Throttle

        Private Event Updated(ByVal sender As IW3Game, ByVal slots As List(Of W3Slot)) Implements IW3Game.Updated
        Private Event PlayerTalked(ByVal sender As IW3Game, ByVal speaker As IW3Player, ByVal text As String) Implements IW3Game.PlayerTalked
        Private Event PlayerLeft(ByVal sender As IW3Game, ByVal state As W3GameStates, ByVal leaver As IW3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) Implements IW3Game.PlayerLeft
        Private Event StateChanged(ByVal sender As IW3Game, ByVal old_state As W3GameStates, ByVal new_state As W3GameStates) Implements IW3Game.ChangedState

        <ContractInvariantMethod()> Protected Sub Invariant()
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
            Contract.Invariant(tickCounts IsNot Nothing)
            Contract.Invariant(downloadScheduler IsNot Nothing)
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
        Private Function CommandProcessText(ByVal player As IW3Player, ByVal text As String) As IFuture(Of Functional.Outcome)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(player IsNot Nothing)
            Dim user = New BotUser(player.name)
            If player IsNot adminPlayer Then
                Select Case state
                    Case Is < W3GameStates.Loading
                        Return server.parent.GameGuestCommandsLobby.ProcessText(Me, user, text)
                    Case W3GameStates.Loading
                        Return server.parent.GameGuestCommandsLoadScreen.ProcessText(Me, user, text)
                    Case Is > W3GameStates.Loading
                        Return server.parent.GameGuestCommandsGamePlay.ProcessText(Me, user, text)
                    Case Else
                        Throw New UnreachableException()
                End Select
            ElseIf server.settings.isAdminGame Then
                Return server.parent.GameCommandsAdmin.ProcessText(Me, Nothing, text)
            Else
                Select Case state
                    Case Is < W3GameStates.Loading
                        Return server.parent.GameCommandsLobby.ProcessText(Me, user, text)
                    Case W3GameStates.Loading
                        Return server.parent.GameCommandsLoadScreen.ProcessText(Me, user, text)
                    Case Is > W3GameStates.Loading
                        Return server.parent.GameCommandsGamePlay.ProcessText(Me, user, text)
                    Case Else
                        Throw New UnreachableException()
                End Select
            End If
        End Function

        Private Sub ReceiveChat(ByVal sender As IW3Player,
                                ByVal text As String,
                                ByVal flags() As Byte,
                                ByVal requestedReceiverIndexes As IList(Of Byte))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(requestedReceiverIndexes IsNot Nothing)

            'Log
            logger.log(sender.name + ": " + text, LogMessageTypes.Typical)
            e_ThrowPlayerTalked(sender, text)

            'Forward to requested players
            'visible sender
            Dim visibleSender = GetVisiblePlayer(sender)
            If visibleSender IsNot sender Then
                text = visibleSender.name + ": " + text
            End If
            'packet
            Dim pk = W3Packet.MakeText(text, flags, players, visibleSender)
            'receivers
            For Each receiver In players
                Dim visibleReceiver = GetVisiblePlayer(receiver)
                If requestedReceiverIndexes.Contains(visibleReceiver.index) Then
                    receiver.f_SendPacket(pk)
                ElseIf visibleReceiver Is visibleSender AndAlso sender IsNot receiver Then
                    receiver.f_SendPacket(pk)
                End If
            Next receiver
        End Sub
        Private Sub ReceiveNonGameAction(ByVal sender As IW3Player, ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
            Dim command_type = CType(vals("command type"), W3Packet.NonGameAction)

            'Player Chat
            Select Case command_type
                Case W3Packet.NonGameAction.GameChat, W3Packet.NonGameAction.LobbyChat
                    Dim command_vals = CType(vals("command val"), Dictionary(Of String, Object))
                    Dim message = CStr(command_vals("message"))
                    Dim flags = CType(command_vals("flags"), Byte())
                    Dim receivePlayerIndexes = CType(vals("receiving player indexes"), IList(Of Byte))
                    Contract.Assume(message IsNot Nothing)
                    Contract.Assume(receivePlayerIndexes IsNot Nothing)
                    ReceiveChat(sender,
                                message,
                                flags,
                                receivePlayerIndexes)

                Case W3Packet.NonGameAction.SetTeam
                    ReceiveSetTeam(sender, CByte(vals("command val")))

                Case W3Packet.NonGameAction.SetHandicap
                    ReceiveSetHandicap(sender, CByte(vals("command val")))

                Case W3Packet.NonGameAction.SetRace
                    ReceiveSetRace(sender, CType(vals("command val"), W3Slot.RaceFlags))

                Case W3Packet.NonGameAction.SetColor
                    ReceiveSetColor(sender, CType(vals("command val"), W3Slot.PlayerColor))

                Case Else
                    Dim msg = "{0} sent unrecognized client command type: {1}".frmt(sender.name, command_type)
                    logger.log(msg, LogMessageTypes.Negative)
                    RemovePlayer(sender, True, W3PlayerLeaveTypes.Disconnect, msg)
            End Select
        End Sub
#End Region

#Region "Life"
        Public Sub New(ByVal parent As IW3Server,
                       ByVal name As String,
                       ByVal map As W3Map,
                       ByVal arguments As IEnumerable(Of String),
                       Optional ByVal logger As Logger = Nothing)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(parent IsNot Nothing)
            Contract.Requires(name IsNot Nothing)

            Me.map = map
            Me.server = parent
            Me.name = name
            Me.logger = If(logger, New Logger)
            For i = 0 To index_map.Length - 1
                index_map(i) = CByte(i)
            Next i

            updateEventThrottle = New Throttle(New TimeSpan(0, 0, 0, 0, milliseconds:=250))
            LobbyNew(arguments)
            LoadScreenNew()
            GamePlayNew()

            LobbyStart()
            AddHandler pingTimer.Elapsed, Sub() c_Ping()
            Me.pingTimer.Start()

            Contract.Assume(downloadScheduler IsNot Nothing)
            Contract.Assume(fakeTickTimer IsNot Nothing)
            Contract.Assume(visibleUnreadyPlayers IsNot Nothing)
            Contract.Assume(visibleReadyPlayers IsNot Nothing)
            Contract.Assume(unreadyPlayers IsNot Nothing)
            Contract.Assume(readyPlayers IsNot Nothing)
        End Sub

        '''<summary>Disconnects from all players and kills the instance. Passes hosting to a player if possible.</summary>
        Private Function Close() As outcome
            If state >= W3GameStates.Closed Then
                Return success(My.Resources.Instance_AlreadyClosed_f0name.frmt(Me.name))
            End If
            pingTimer.Stop()

            'Pass hosting duty to another player if possible
            If state = W3GameStates.Playing AndAlso players.Count > 1 Then
                Dim host = players.Max(Function(p1, p2)
                                           If p1 Is Nothing Then  Return -1
                                           If p2 Is Nothing Then  Return 1
                                           If p1.IsFake Then  Return -1
                                           If p2.IsFake Then  Return 1

                                           'Can they host? Are they connected to more people? Are they on a better connection?
                                           Dim signs = {Math.Sign(p1.canHost - p2.canHost),
                                                        Math.Sign(p1.numPeerConnections - p2.numPeerConnections),
                                                        Math.Sign(p1.latency() - p2.latency())}
                                           Return (From sign In signs Where sign <> 0).FirstOrDefault
                                       End Function)

                If host IsNot Nothing AndAlso Not host.IsFake Then
                    BroadcastPacket(W3Packet.MakeSetHost(host.index), Nothing)
                    logger.log(name + " has handed off hosting to " + host.name, LogMessageTypes.Positive)
                Else
                    logger.log(name + " has failed to hand off hosting", LogMessageTypes.Negative)
                End If
            End If

            'disconnect from all players
            For Each p In players.ToList
                Contract.Assume(p IsNot Nothing)
                RemovePlayer(p, True, W3PlayerLeaveTypes.Disconnect, "Closing game")
            Next p

            ChangeState(W3GameStates.Closed)

            Return success(My.Resources.Instance_Closed_f0name.frmt(Me.name))
        End Function
#End Region

#Region "Events"
        Private Sub e_ThrowUpdated()
            Dim slots = (From slot In Me.slots Select slot.Cloned(Me)).ToList()
            updateEventThrottle.SetActionToRun(Sub() eventRef.QueueAction(Sub()
                                                                              RaiseEvent Updated(Me, slots)
                                                                          End Sub))
        End Sub
        Private Sub e_ThrowPlayerTalked(ByVal speaker As IW3Player, ByVal text As String)
            eventRef.QueueAction(Sub()
                                     RaiseEvent PlayerTalked(Me, speaker, text)
                                 End Sub)
        End Sub
        Private Sub e_ThrowPlayerLeft(ByVal leaver As IW3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String)
            Dim state = Me.state
            eventRef.QueueAction(Sub()
                                     RaiseEvent PlayerLeft(Me, state, leaver, leaveType, reason)
                                 End Sub)
        End Sub
        Private Sub e_ThrowStateChanged(ByVal old_state As W3GameStates, ByVal new_state As W3GameStates)
            eventRef.QueueAction(Sub()
                                     RaiseEvent StateChanged(Me, old_state, new_state)
                                 End Sub)
        End Sub

        Private Sub c_Ping()
            ref.QueueAction(
                Sub()
                    Dim salt = CUInt(rand.Next(0, Integer.MaxValue))
                    Dim tick As ModInt32 = Environment.TickCount
                    Dim record = New W3PlayerPingRecord(salt, tick)
                    For Each player In players
                        player.f_QueuePing(record)
                    Next player
                    BroadcastPacket(W3Packet.MakePing(salt), Nothing)
                    e_ThrowUpdated()
                End Sub
            )
        End Sub
#End Region

#Region "Access"
        Public Shared Function XvX(ByVal s As String) As Outcome(Of IList(Of Integer))
            Contract.Requires(s IsNot Nothing)
            'parse numbers between 'v's
            Dim vals = s.ToLower().Split("v"c)
            Dim nums = New List(Of Integer)
            For Each e In vals
                Dim b As Byte
                Contract.Assume(e IsNot Nothing)
                If Not Byte.TryParse(e, b) Then
                    Return failure("Non-numeric team limit '{0}'.".frmt(e))
                End If
                nums.Add(b)
            Next e
            Return successVal(CType(nums, IList(Of Integer)), "Converted to numbers.")
        End Function

        Private Sub ChangeState(ByVal newState As W3GameStates)
            Dim oldState = state
            state = newState
            e_ThrowStateChanged(oldState, newState)
        End Sub

        '''<summary>Broadcasts a packet to all players. Requires a packer for the packet, and values matching the packer.</summary>
        Private Sub BroadcastPacket(ByVal pk As W3Packet,
                                    Optional ByVal source As IW3Player = Nothing)
            Contract.Requires(pk IsNot Nothing)
            For Each player In (From _player In players
                                Where _player IsNot source)
                player.f_SendPacket(pk)
            Next player
        End Sub

        '''<summary>Returns the number of slots potentially available for new players.</summary>
        <Pure()> Private Function CountFreeSlots() As Integer
            Dim freeSlots = From slot In slots Where slot.contents.WantPlayer(Nothing) >= W3SlotContents.WantPlayerPriority.Accept
            Contract.Assume(freeSlots IsNot Nothing)
            Return (freeSlots).Count
        End Function

        '''<summary>Returns any slot matching a string. Checks index, color and player name.</summary>
        <Pure()> Private Function FindMatchingSlot(ByVal query As String) As Outcome(Of W3Slot)
            Contract.Requires(query IsNot Nothing)

            Dim bestSlot As W3Slot = Nothing
            Dim bestMatch = W3Slot.Match.None
            slots.MaxPair(Function(slot) slot.Matches(query), bestSlot, bestMatch)

            If bestMatch = W3Slot.Match.None Then
                Return failure(My.Resources.Slot_NotMatched_f0pattern.frmt(query))
            Else
                Return successVal(bestSlot, "Matched '{0}' to a slot's {1}.".frmt(query, bestMatch))
            End If
        End Function

        '''<summary>Returns the slot containing the given player.</summary>
        <Pure()> Private Function FindPlayerSlot(ByVal player As IW3Player) As W3Slot
            Contract.Requires(player IsNot Nothing)
            Return (From slot In slots
                    Where (From resident In slot.contents.EnumPlayers Where resident Is player).Any
                   ).FirstOrDefault
        End Function
#End Region

#Region "Messaging"
        '''<summary>Sends text to all players. Uses spoof chat if necessary.</summary>
        Private Sub BroadcastMessage(ByVal message As String,
                                     Optional ByVal playerToAvoid As IW3Player = Nothing)
            Contract.Requires(message IsNot Nothing)
            For Each player In (From _player In players Where _player IsNot playerToAvoid)
                Contract.Assume(player IsNot Nothing)
                SendMessageTo(message, player, display:=False)
            Next player
            logger.log("{0}: {1}".frmt(My.Resources.ProgramName, message), LogMessageTypes.Typical)
        End Sub

        '''<summary>Sends text to the target player. Uses spoof chat if necessary.</summary>
        Private Sub SendMessageTo(ByVal message As String,
                                  ByVal player As IW3Player,
                                  Optional ByVal display As Boolean = True)
            Contract.Requires(message IsNot Nothing)
            Contract.Requires(player IsNot Nothing)

            'Prep
            Dim flags() As Byte
            If state >= W3GameStates.Loading Then
                'public game chat
                flags = New Byte() {0, 0, 0, 0}
            Else
                'lobby chat
                flags = Nothing
            End If

            'Send Text
            If fakeHostPlayer IsNot Nothing Then
                'text comes from fake host
                player.f_SendPacket(W3Packet.MakeText(message, flags, players, fakeHostPlayer))
            Else
                'text spoofs from receiving player
                player.f_SendPacket(W3Packet.MakeText(My.Resources.ProgramName + ": " + message, flags, players, player))
            End If

            If display Then
                logger.log("(Private to {0}): {1}".frmt(player.name, message), LogMessageTypes.Typical)
            End If
        End Sub
#End Region

#Region "Players"
        '''<summary>Removes the given player from the instance</summary>
        Private Function RemovePlayer(ByVal player As IW3Player,
                                      ByVal wasExpected As Boolean,
                                      ByVal leaveType As W3PlayerLeaveTypes,
                                      ByVal reason As String) As Outcome
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            If Not players.Contains(player) Then
                Return success("Player already not in game.")
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
            player.Disconnect(True, leaveType, reason)
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
            If state >= W3GameStates.Loading AndAlso Not (From x In players Where Not x.IsFake).Any Then
                'the game has started and everyone has left, time to die
                Close()
            End If

            'Log
            If player.IsFake Then
                logger.log("{0} has been removed from the game. ({1})".frmt(player.name, reason), LogMessageTypes.Negative)
            Else
                flagHasPlayerLeft = True
                If wasExpected Then
                    logger.log("{0} has left the game. ({1})".frmt(player.name, reason), LogMessageTypes.Negative)
                Else
                    logger.log("{0} has disconnected. ({1})".frmt(player.name, reason), LogMessageTypes.Problem)
                End If
                e_ThrowPlayerLeft(player, leaveType, reason)
            End If

            Return success("Removed player {0} from the game.".frmt(player.name))
        End Function

        Private Function TryElevatePlayer(ByVal name As String,
                                          Optional ByVal password As String = Nothing) As Outcome
            Contract.Requires(name IsNot Nothing)

            Dim player = FindPlayer(name)
            If player Is Nothing Then Return failure("No player found with the name '{0}'.".frmt(name))
            If adminPlayer IsNot Nothing Then Return failure("A player is already the admin.")
            If password IsNot Nothing Then
                player.NumAdminTries += 1
                If player.NumAdminTries > 5 Then Return failure("Too many tries.")
                If password.ToLower() <> server.settings.adminPassword.ToLower() Then Return failure("Incorrect password.")
            End If

            adminPlayer = player
            SendMessageTo("You are now the admin.", player)
            Return success("'{0}' is now the admin.".frmt(name))
        End Function

        Private Function FindPlayer(ByVal username As String) As IW3Player
            Contract.Requires(username IsNot Nothing)
            Return (From x In players Where x.name.ToLower = username.ToLower).FirstOrDefault
        End Function
        Private Function FindPlayer(ByVal index As Byte) As IW3Player
            Contract.Requires(index > 0)
            Contract.Requires(index <= 12)
            Return (From x In players Where x.index = index).FirstOrDefault
        End Function

        '''<summary>Boots players in the slot with the given index.</summary>
        Private Function Boot(ByVal slotQuery As String) As Outcome
            Contract.Requires(slotQuery IsNot Nothing)
            Dim foundSlot = FindMatchingSlot(slotQuery)
            If Not foundSlot.succeeded Then Return foundSlot

            Dim slot = foundSlot.val
            If Not slot.contents.EnumPlayers.Any Then
                Return failure("There is no player to boot in slot '{0}'.".frmt(slotQuery))
            End If

            Dim target = (From player In slot.contents.EnumPlayers
                          Where player.name.ToLower() = slotQuery.ToLower()).FirstOrDefault
            If target IsNot Nothing Then
                slot.contents = slot.contents.RemovePlayer(target)
                RemovePlayer(target, True, W3PlayerLeaveTypes.Disconnect, "Booted")
                Return success("Booting player '{0}'.".frmt(slotQuery))
            End If

            For Each player In slot.contents.EnumPlayers
                Contract.Assume(player IsNot Nothing)
                slot.contents = slot.contents.RemovePlayer(player)
                RemovePlayer(player, True, W3PlayerLeaveTypes.Disconnect, "Booted")
            Next player
            Return success("Booting from slot '{0}'.".frmt(slotQuery))
        End Function
#End Region

#Region "Invisible Players"
        <Pure()> Private Function IsPlayerVisible(ByVal player As IW3Player) As Boolean
            Contract.Requires(player IsNot Nothing)
            Return index_map(player.index) = player.index
        End Function
        <Pure()> Private Function GetVisiblePlayer(ByVal player As IW3Player) As IW3Player
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IW3Player)() IsNot Nothing)
            If IsPlayerVisible(player) Then Return player
            Dim visibleIndex = index_map(player.index)
            Return (From p In players Where p.index = visibleIndex).First
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
        Private ReadOnly Property _parent() As IW3Server Implements IW3Game.server
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

        Private Function _f_AdminPlayer() As IFuture(Of IW3Player) Implements IW3Game.f_AdminPlayer
            Return ref.QueueFunc(Function() adminPlayer)
        End Function
        Private Function _f_FakeHostPlayer() As IFuture(Of IW3Player) Implements IW3Game.f_FakeHostPlayer
            Return ref.QueueFunc(Function() fakeHostPlayer)
        End Function
        Private Function _f_CommandProcessLocalText(ByVal text As String, ByVal logger As Logger) As IFuture Implements IW3Game.f_CommandProcessLocalText
            Return ref.QueueAction(Sub()
                                       Contract.Assume(text IsNot Nothing)
                                       Contract.Assume(logger IsNot Nothing)
                                       CommandProcessLocalText(text, logger)
                                   End Sub)
        End Function
        Private Function _f_CommandProcessText(ByVal player As IW3Player, ByVal text As String) As IFuture(Of Functional.Outcome) Implements IW3Game.f_CommandProcessText
            Return ref.QueueFunc(Function()
                                     Contract.Assume(player IsNot Nothing)
                                     Contract.Assume(text IsNot Nothing)
                                     Return CommandProcessText(player, text)
                                 End Function).Defuturize
        End Function
        Private Function _f_TryElevatePlayer(ByVal name As String, Optional ByVal password As String = Nothing) As IFuture(Of Outcome) Implements IW3Game.f_TryElevatePlayer
            Return ref.QueueFunc(Function() TryElevatePlayer(name, password))
        End Function
        Private Function _f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player) Implements IW3Game.f_FindPlayer
            Return ref.QueueFunc(Function() FindPlayer(username))
        End Function
        Private Function _f_RemovePlayer(ByVal player As IW3Player,
                                         ByVal expected As Boolean,
                                         ByVal leaveType As W3PlayerLeaveTypes,
                                         ByVal reason As String) As IFuture(Of Outcome) Implements IW3Game.f_RemovePlayer
            Return ref.QueueFunc(Function()
                                     Contract.Assume(player IsNot Nothing)
                                     Contract.Assume(reason IsNot Nothing)
                                     Return RemovePlayer(player, expected, leaveType, reason)
                                 End Function)
        End Function
        Private Function _f_EnumPlayers() As IFuture(Of List(Of IW3Player)) Implements IW3Game.f_EnumPlayers
            Return ref.QueueFunc(Function() players.ToList)
        End Function
        Private Function _f_ThrowUpdated() As IFuture Implements IW3Game.f_ThrowUpdated
            Return ref.QueueAction(AddressOf e_ThrowUpdated)
        End Function
        Private Function _f_Close() As IFuture(Of Outcome) Implements IW3Game.f_Close
            Return ref.QueueFunc(Function() Close())
        End Function
        Private Function _f_BroadcastMessage(ByVal message As String) As IFuture Implements IW3Game.f_BroadcastMessage
            Return ref.QueueAction(Sub()
                                       Contract.Assume(message IsNot Nothing)
                                       BroadcastMessage(message)
                                   End Sub)
        End Function
        Private Function _f_SendMessageTo(ByVal message As String, ByVal player As IW3Player) As IFuture Implements IW3Game.f_SendMessageTo
            Return ref.QueueAction(Sub()
                                       Contract.Assume(message IsNot Nothing)
                                       Contract.Assume(player IsNot Nothing)
                                       SendMessageTo(message, player)
                                   End Sub)
        End Function
        Private Function _f_BootSlot(ByVal slotQuery As String) As IFuture(Of Outcome) Implements IW3Game.f_BootSlot
            Return ref.QueueFunc(Function()
                                     Contract.Assume(slotQuery IsNot Nothing)
                                     Return Boot(slotQuery)
                                 End Function)
        End Function
        Private Function _f_State() As IFuture(Of W3GameStates) Implements IW3Game.f_State
            Return ref.QueueFunc(Function() Me.state)
        End Function
        Private Function _f_ReceiveNonGameAction(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture Implements IW3Game.f_ReceiveNonGameAction
            Return ref.QueueAction(Sub()
                                       Contract.Assume(player IsNot Nothing)
                                       Contract.Assume(vals IsNot Nothing)
                                       ReceiveNonGameAction(player, vals)
                                   End Sub)
        End Function
#End Region
    End Class
End Namespace
