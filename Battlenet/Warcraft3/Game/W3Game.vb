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

        Private ReadOnly lobby As W3GameSoul_Lobby
        Private ReadOnly load_screen As W3GameSoul_Load
        Private ReadOnly gameplay As W3GamePlay
        Private ReadOnly parent As IW3Server
        Private ReadOnly map As W3Map
        Private ReadOnly name As String
        Private ReadOnly rand As New Random()
        Private ReadOnly slots As New List(Of W3Slot)
        Private ReadOnly ref As ICallQueue
        Private ReadOnly eventRef As ICallQueue
        Private ReadOnly logger As MultiLogger
        Private Const PING_PERIOD As UShort = 5000
        Private WithEvents ping_timer As New Timers.Timer(PING_PERIOD)
        Private state As W3GameStates = W3GameStates.AcceptingPlayers
        Private fake_host_player As IW3Player
        Private flag_player_left As Boolean
        Private admin_player As IW3Player
        Private ReadOnly players As New List(Of IW3Player)
        Private ReadOnly index_map(0 To 12) As Byte

        Private Event updated(ByVal sender As IW3Game, ByVal slots As List(Of W3Slot)) Implements IW3Game.Updated
        Private Event player_talked(ByVal sender As IW3Game, ByVal speaker As IW3Player, ByVal text As String) Implements IW3Game.PlayerTalked
        Private Event player_left(ByVal sender As IW3Game, ByVal state As W3GameStates, ByVal leaver As IW3Player, ByVal reason As W3PlayerLeaveTypes) Implements IW3Game.PlayerLeft
        Private Event state_changed(ByVal sender As IW3Game, ByVal old_state As W3GameStates, ByVal new_state As W3GameStates) Implements IW3Game.ChangedState

#Region "Commands"
        Private Sub CommandProcessLocalText(ByVal text As String, ByVal logger As MultiLogger)
            Select Case state
                Case Is < W3GameStates.Loading
                    parent.parent.instance_commands_setup.processLocalText(lobby, text, logger)
                Case W3GameStates.Loading
                    parent.parent.instance_commands_load.processLocalText(load_screen, text, logger)
                Case Is > W3GameStates.Loading
                    parent.parent.instance_commands_play.processLocalText(gameplay, text, logger)
                Case Else
                    Throw New UnreachableStateException()
            End Select
        End Sub
        Private Function CommandProcessText(ByVal player As IW3Player, ByVal text As String) As Functional.Futures.IFuture(Of Functional.Outcome)
            If player IsNot admin_player Then
                Select Case state
                    Case Is < W3GameStates.Loading
                        Return parent.parent.instance_commands_guest_setup.processText(lobby, Nothing, text)
                    Case W3GameStates.Loading
                        Return parent.parent.instance_commands_guest_load.processText(load_screen, Nothing, text)
                    Case Is > W3GameStates.Loading
                        Return parent.parent.instance_commands_guest_play.processText(gameplay, Nothing, text)
                    Case Else
                        Throw New UnreachableStateException()
                End Select
            ElseIf parent.settings.is_admin_game Then
                Return parent.parent.instance_commands_admin.processText(Me, Nothing, text)
            Else
                Select Case state
                    Case Is < W3GameStates.Loading
                        Return parent.parent.instance_commands_setup.processText(lobby, Nothing, text)
                    Case W3GameStates.Loading
                        Return parent.parent.instance_commands_load.processText(load_screen, Nothing, text)
                    Case Is > W3GameStates.Loading
                        Return parent.parent.instance_commands_play.processText(gameplay, Nothing, text)
                    Case Else
                        Throw New UnreachableStateException()
                End Select
            End If
        End Function

        Protected Enum ClientActions As Byte
            lobby_chat = &H10
            set_team = &H11
            set_color = &H12
            set_race = &H13
            set_handicap = &H14
            game_chat = &H20
        End Enum
        Private Sub ReceiveChat(ByVal sending_player As IW3Player, _
                                  ByVal text As String, _
                                  ByVal flags() As Byte, _
                                  ByVal requested_receiver_indexes As IList(Of Byte))
            'Log
            logger.log(sending_player.name + ": " + text, LogMessageTypes.NormalEvent)
            e_ThrowPlayerTalked(sending_player, text)

            'Forward to requested players
            'visible sender
            Dim visible_sending_player = GetVisiblePlayer(sending_player)
            If visible_sending_player IsNot sending_player Then
                text = visible_sending_player.name + ": " + text
            End If
            'packet
            Dim pk = W3Packet.MakePacket_TEXT(text, flags, players, visible_sending_player)
            'receivers
            For Each player In players
                Dim visible_receiving_player = GetVisiblePlayer(player)
                If requested_receiver_indexes.Contains(visible_receiving_player.index) Then
                    player.f_SendPacket(pk)
                ElseIf visible_receiving_player Is visible_sending_player AndAlso sending_player IsNot player Then
                    player.f_SendPacket(pk)
                End If
            Next player
        End Sub
        Private Sub ReceivePacket_CLIENT_COMMAND(ByVal sending_player As IW3Player, ByVal vals As Dictionary(Of String, Object))
            Dim command_type = CType(vals("command type"), ClientActions)
            Dim subvals = CType(vals("command value"), Dictionary(Of String, Object))

            'Player Chat
            Select Case command_type
                Case ClientActions.game_chat
                    ReceiveChat(sending_player, _
                                   CStr(subvals("message")), _
                                   CType(subvals("flags"), Byte()), _
                                   CType(vals("receiving player indexes"), IList(Of Byte)))
                Case ClientActions.lobby_chat
                    ReceiveChat(sending_player, _
                                   CStr(subvals("message")), _
                                   Nothing, _
                                   CType(vals("receiving player indexes"), IList(Of Byte)))
                Case ClientActions.set_team
                    lobby.ReceiveSetTeam(sending_player, CByte(subvals("new value")))

                Case ClientActions.set_handicap
                    lobby.ReceiveSetHandicap(sending_player, CByte(subvals("new value")))

                Case ClientActions.set_race
                    lobby.ReceiveSetRace(sending_player, CType(subvals("new value"), W3Slot.RaceFlags))

                Case ClientActions.set_color
                    lobby.ReceiveSetColor(sending_player, CType(subvals("new value"), W3Slot.PlayerColor))

                Case Else
                    logger.log("{0} sent unrecognized client command type: {1}".frmt(sending_player.name, command_type), LogMessageTypes.NegativeEvent)
                    RemovePlayer(sending_player, True, W3PlayerLeaveTypes.disc)
            End Select
        End Sub
#End Region

#Region "Life"
        Public Sub New(ByVal parent As IW3Server, _
                        ByVal name As String, _
                        ByVal map As W3Map, _
                        ByVal arguments As IEnumerable(Of String), _
                        Optional ByRef ref As ICallQueue = Nothing, _
                        Optional ByVal logger As MultiLogger = Nothing)
            Me.map = ContractNotNull(map, "map")
            Me.parent = ContractNotNull(parent, "parent")
            Me.name = ContractNotNull(name, "name")
            Me.eventRef = New ThreadedCallQueue("{0} {1} eventRef".frmt(Me.GetType.Name, name))
            Me.ref = If(ref, New ThreadedCallQueue("{0} {1} ref".frmt(Me.GetType.Name, name)))
            Me.logger = If(logger, New MultiLogger)
            For i = 0 To index_map.Length - 1
                index_map(i) = CByte(i)
            Next i
            Me.lobby = New W3GameSoul_Lobby(Me, arguments)
            Me.load_screen = New W3GameSoul_Load(Me)
            Me.gameplay = New W3GamePlay(Me)
            lobby.Start()
            Me.ping_timer.Start()
        End Sub

        '''<summary>Disconnects from all players and kills the instance. Passes hosting to a player if possible.</summary>
        Private Function Close() As outcome
            If state >= W3GameStates.Closed Then
                Return success(My.Resources.Instance_AlreadyClosed_f0name.frmt(Me.name))
            End If
            ping_timer.Stop()

            'Pass hosting duty to another player if possible
            If state = W3GameStates.Playing And players.Count > 1 Then
                Dim host = reduce(players, AddressOf W3Player.W3PlayerPart.ReduceBetterHost)
                If host IsNot Nothing Then
                    BroadcastPacket(W3Packet.MakePacket_SET_HOST(host.index), Nothing)
                    logger.log(name + " has handed off hosting to " + host.name, LogMessageTypes.PositiveEvent)
                Else
                    logger.log(name + " has failed to hand off hosting", LogMessageTypes.NegativeEvent)
                End If
            End If

            'disconnect from all players
            For Each p In players.ToList
                RemovePlayer(p, True, W3PlayerLeaveTypes.disc)
            Next p

            change_state(W3GameStates.Closed)

            Return success(My.Resources.Instance_Closed_f0name.frmt(Me.name))
        End Function
#End Region

#Region "Events"
        Private Sub e_ThrowUpdated()
            eventRef.enqueue(Function() eval(AddressOf _e_ThrowUpdated, (From slot In slots Select slot.Cloned(Me)).ToList()))
        End Sub
        Private Sub _e_ThrowUpdated(ByVal slots As List(Of W3Slot))
            RaiseEvent updated(Me, slots)
        End Sub

        Private Sub e_ThrowPlayerTalked(ByVal speaker As IW3Player, ByVal text As String)
            eventRef.enqueue(Function() eval(AddressOf _e_ThrowPlayerTalked, speaker, text))
        End Sub
        Private Sub _e_ThrowPlayerTalked(ByVal speaker As IW3Player, ByVal text As String)
            RaiseEvent player_talked(Me, speaker, text)
        End Sub

        Private Sub e_ThrowPlayerLeft(ByVal leaver As IW3Player, ByVal reason As W3PlayerLeaveTypes)
            eventRef.enqueue(Function() eval(AddressOf _e_ThrowPlayerLeft, state, leaver, reason))
        End Sub
        Private Sub _e_ThrowPlayerLeft(ByVal game_state As W3GameStates, ByVal leaver As IW3Player, ByVal reason As W3PlayerLeaveTypes)
            RaiseEvent player_left(Me, game_state, leaver, reason)
        End Sub

        Private Sub e_ThrowStateChanged(ByVal old_state As W3GameStates, ByVal new_state As W3GameStates)
            eventRef.enqueue(Function() eval(AddressOf _e_ThrowStateChanged, old_state, new_state))
        End Sub
        Private Sub _e_ThrowStateChanged(ByVal old_state As W3GameStates, ByVal new_state As W3GameStates)
            RaiseEvent state_changed(Me, old_state, new_state)
        End Sub

        Private Sub c_Ping(ByVal sender As Object, ByVal e As Timers.ElapsedEventArgs) Handles ping_timer.Elapsed
            ref.enqueue(AddressOf _c_Ping)
        End Sub
        Private Sub _c_Ping()
            Dim salt = CUInt(rand.Next(0, Integer.MaxValue))
            Dim tick = Environment.TickCount
            Dim record = New W3PlayerPingRecord(salt, tick)
            For Each player In players
                player.f_QueuePing(record)
            Next player
            BroadcastPacket(W3Packet.MakePacket_PING(salt), Nothing)
            e_ThrowUpdated()
        End Sub
#End Region

#Region "Access"
        Public Shared Function XvX(ByVal s As String) As Outcome(Of IList(Of Integer))
            'parse numbers between 'v's
            Dim vals = s.ToLower().Split("v"c)
            Dim nums = New List(Of Integer)
            For Each e In vals
                Dim b As Byte
                If Not Byte.TryParse(e, b) Then
                    Return failure("Non-numeric team limit '{0}'.".frmt(e))
                End If
                nums.Add(b)
            Next e
            Return successVal(CType(nums, IList(Of Integer)), "Converted to numbers.")
        End Function

        Private Sub change_state(ByVal new_value As W3GameStates)
            Dim old_value As W3GameStates = state
            state = new_value
            e_ThrowStateChanged(old_value, new_value)
        End Sub

        '''<summary>Broadcasts a packet to all players. Requires a packer for the packet, and values matching the packer.</summary>
        Private Sub BroadcastPacket(ByVal pk As W3Packet, Optional ByVal source As IW3Player = Nothing)
            For Each player In players
                If source Is player Then Continue For
                player.f_SendPacket(pk)
            Next player
        End Sub

        '''<summary>Returns the number of slots potentially available for new players.</summary>
        Private Function CountFreeSlots() As Integer
            Return (From slot In slots Where slot.contents.WantPlayer(Nothing) > W3SlotContents.WantPlayerPriority.Reluctant).Count
        End Function

        '''<summary>Returns any slot matching a string. Checks index, color and player name.</summary>
        Private Function FindMatchingSlot(ByVal query As String) As Outcome(Of W3Slot)
            Dim best_slot As W3Slot = Nothing
            Dim best_match = W3Slot.Match.None
            For Each slot In slots
                Dim q = slot.Matches(query)
                If q > best_match Then
                    best_match = q
                    best_slot = slot
                End If
            Next slot
            If best_match = W3Slot.Match.None Then
                Return failure(My.Resources.Slot_NotMatched_f0pattern.frmt(query))
            Else
                Return successVal(best_slot, "Matched '{0}' to a slot's {1}.".frmt(query, best_match))
            End If
        End Function

        '''<summary>Returns the slot containing the given player.</summary>
        Private Function FindPlayerSlot(ByVal player As IW3Player) As W3Slot
            For Each slot In slots
                For Each p In slot.contents.EnumPlayers
                    If p Is player Then Return slot
                Next p
            Next slot
            Return Nothing
        End Function
#End Region

#Region "Messaging"
        '''<summary>Sends text to all players. Uses spoof chat if necessary.</summary>
        Private Sub BroadcastMessage(ByVal message As String, Optional ByVal source_avoid As IW3Player = Nothing)
            For Each player In players
                If player Is source_avoid Then Continue For
                SendMessageTo(message, player)
            Next player
        End Sub

        '''<summary>Sends text to the target player. Uses spoof chat if necessary.</summary>
        Private Sub SendMessageTo(ByVal message As String, ByVal player As IW3Player)
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
            If fake_host_player IsNot Nothing Then
                'text comes from fake host
                player.f_SendPacket(W3Packet.MakePacket_TEXT(message, flags, players, fake_host_player))
            Else
                'text spoofs from receiving player
                player.f_SendPacket(W3Packet.MakePacket_TEXT(My.Resources.ProgramName + ": " + message, flags, players, player))
            End If
        End Sub
#End Region

#Region "Players"
        '''<summary>Removes the given player from the instance</summary>
        Private Function RemovePlayer(ByVal player As IW3Player, ByVal expected As Boolean, ByVal leave_type As W3PlayerLeaveTypes) As Outcome
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
                BroadcastPacket(W3Packet.MakePacket_OTHER_PLAYER_LEFT(player, leave_type), player)
            End If
            If player Is admin_player Then
                admin_player = Nothing
            End If
            If player Is fake_host_player Then
                fake_host_player = Nothing
            End If
            player.disconnect_R(True, leave_type)
            players.Remove(player)
            Select Case state
                Case Is < W3GameStates.Loading
                    lobby.CatchRemovedPlayer(player, slot)
                Case W3GameStates.Loading
                    load_screen.CatchRemovedPlayer(player, slot)
                Case Is > W3GameStates.Loading
            End Select

            'Clean game
            If state >= W3GameStates.Loading AndAlso Not (From x In players Where Not x.is_fake).Any Then
                'the game has started and everyone has left, time to die
                Close()
            End If

            'Log
            If player.is_fake Then
                logger.log(player.name + " has been removed from the game.", LogMessageTypes.NegativeEvent)
            Else
                flag_player_left = True
                If expected Then
                    logger.log(player.name + " has left the game.", LogMessageTypes.NegativeEvent)
                Else
                    logger.log(player.name + " has disconnected.", LogMessageTypes.Problem)
                End If
                e_ThrowPlayerLeft(player, leave_type)
            End If

            Return success("Removed {0} from {1}.".frmt(player.name, name))
        End Function

        Private Function TryElevatePlayer(ByVal name As String, Optional ByVal password As String = Nothing) As Outcome
            Dim p = FindPlayer(name)
            If p Is Nothing Then Return failure("No player found with the name '{0}'.".frmt(name))
            If admin_player IsNot Nothing Then Return failure("A player is already the admin.")
            If password IsNot Nothing Then
                p.admin_tries += 1
                If p.admin_tries > 5 Then Return failure("Too many tries.")
                If password.ToLower() <> parent.settings.admin_password.ToLower() Then failure("Incorrect password.")
            End If

            admin_player = p
            SendMessageTo("You are now the admin.", p)
            Return success("'{0}' is now the admin.".frmt(name))
        End Function

        Private Function FindPlayer(ByVal username As String) As IW3Player
            Return (From x In players Where x.name.ToLower = username.ToLower).FirstOrDefault
        End Function
        Private Function FindPlayer(ByVal index As Byte) As IW3Player
            Return (From x In players Where x.index = index).FirstOrDefault
        End Function

        '''<summary>Boots players in the slot with the given index.</summary>
        Private Function Boot(ByVal slotid As String) As Outcome
            Dim slot_out = FindMatchingSlot(slotid)
            If slot_out.outcome <> Outcomes.succeeded Then Return slot_out
            Dim slot = slot_out.val
            If Not slot.contents.EnumPlayers.Any Then
                Return failure("There is no player to boot in slot '{0}'.".frmt(slotid))
            End If
            For Each player In slot.contents.EnumPlayers
                If player.name.ToLower() = slotid.ToLower() Then
                    logger.log("Booting " + player.name, LogMessageTypes.NegativeEvent)
                    slot.contents = slot.contents.RemovePlayer(player)
                    RemovePlayer(player, True, W3PlayerLeaveTypes.disc)
                    Return success("Booting player '{0}'.".frmt(slotid))
                End If
            Next player
            For Each player In slot.contents.EnumPlayers
                logger.log("Booting " + player.name, LogMessageTypes.NegativeEvent)
                slot.contents = slot.contents.RemovePlayer(player)
                RemovePlayer(player, True, W3PlayerLeaveTypes.disc)
            Next player
            Return success("Booting from slot '{0}'.".frmt(slotid))
        End Function
#End Region

#Region "Invisible Players"
        Private Function IsPlayerVisible(ByVal player As IW3Player) As Boolean
            Return index_map(player.index) = player.index
        End Function
        Private Function GetVisiblePlayer(ByVal player As IW3Player) As IW3Player
            If IsPlayerVisible(player) Then Return player
            Return (From p In players Where p.index = index_map(player.index)).First
        End Function
        Private Shared Sub SetupCoveredSlot(ByVal covering_slot As W3Slot, ByVal covered_slot As W3Slot, ByVal player_index As Byte)
            If Not covering_slot.contents.EnumPlayers.Count = 1 Then Throw New InvalidOperationException()
            If covered_slot.contents.EnumPlayers.Any Then Throw New InvalidOperationException()
            covering_slot.contents = New W3SlotContentsCovering(covering_slot, covered_slot, covering_slot.contents.EnumPlayers.First)
            covered_slot.contents = New W3SlotContentsCovered(covered_slot, covering_slot, player_index, covered_slot.contents.EnumPlayers)
        End Sub
#End Region

#Region "Interface"
        Private ReadOnly Property _parent() As IW3Server Implements IW3Game.parent
            Get
                Return parent
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
        Private ReadOnly Property _logger() As MultiLogger Implements IW3Game.logger
            Get
                Return logger
            End Get
        End Property
        Private ReadOnly Property _lobby() As IW3GameLobby Implements IW3Game.lobby
            Get
                Return lobby
            End Get
        End Property
        Private ReadOnly Property _load_screen() As IW3GameLoadScreen Implements IW3Game.load_screen
            Get
                Return load_screen
            End Get
        End Property
        Private ReadOnly Property _gameplay() As IW3GamePlay Implements IW3Game.gameplay
            Get
                Return gameplay
            End Get
        End Property

        Private Function _f_admin_player() As IFuture(Of IW3Player) Implements IW3Game.f_admin_player
            Return ref.enqueue(Function() admin_player)
        End Function
        Private Function _f_fake_host_player() As IFuture(Of IW3Player) Implements IW3Game.f_fake_host_player
            Return ref.enqueue(Function() fake_host_player)
        End Function
        Private Function _f_CommandProcessLocalText(ByVal text As String, ByVal logger As MultiLogger) As IFuture Implements IW3Game.f_CommandProcessLocalText
            Return ref.enqueue(Function() eval(AddressOf CommandProcessLocalText, text, logger))
        End Function
        Private Function _f_CommandProcessText(ByVal player As IW3Player, ByVal text As String) As Functional.Futures.IFuture(Of Functional.Outcome) Implements IW3Game.f_CommandProcessText
            Return futurefuture(ref.enqueue(Function() CommandProcessText(player, text)))
        End Function
        Private Function _f_TryElevatePlayer(ByVal name As String, Optional ByVal password As String = Nothing) As IFuture(Of Outcome) Implements IW3Game.f_TryElevatePlayer
            Return ref.enqueue(Function() TryElevatePlayer(name, password))
        End Function
        Private Function _f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player) Implements IW3Game.f_FindPlayer
            Return ref.enqueue(Function() FindPlayer(username))
        End Function
        Private Function _f_RemovePlayer(ByVal p As IW3Player, ByVal expected As Boolean, ByVal leave_type As W3PlayerLeaveTypes) As IFuture(Of Outcome) Implements IW3Game.f_RemovePlayer
            Return ref.enqueue(Function() RemovePlayer(p, expected, leave_type))
        End Function
        Private Function _f_EnumPlayers() As IFuture(Of List(Of IW3Player)) Implements IW3Game.f_EnumPlayers
            Return ref.enqueue(Function() players.ToList)
        End Function
        Private Function _f_ThrowUpdated() As IFuture Implements IW3Game.f_ThrowUpdated
            Return ref.enqueue(AddressOf e_ThrowUpdated)
        End Function
        Private Function _f_Close() As IFuture(Of Outcome) Implements IW3Game.f_Close
            Return ref.enqueue(Function() Close())
        End Function
        Private Function _f_BroadcastMessage(ByVal message As String) As IFuture Implements IW3Game.f_BroadcastMessage
            Return ref.enqueue(Function() eval(AddressOf BroadcastMessage, message))
        End Function
        Private Function _f_SendMessageTo(ByVal message As String, ByVal player As IW3Player) As IFuture Implements IW3Game.f_SendMessageTo
            Return ref.enqueue(Function() eval(AddressOf SendMessageTo, message, player))
        End Function
        Private Function _f_BootSlot(ByVal slot_query As String) As IFuture(Of Outcome) Implements IW3Game.f_BootSlot
            Return ref.enqueue(Function() Boot(slot_query))
        End Function
        Private Function _f_State() As IFuture(Of W3GameStates) Implements IW3Game.f_State
            Return ref.enqueue(Function() Me.state)
        End Function
        Private Function _f_ReceivePacket_CLIENT_COMMAND(ByVal player As IW3Player, ByVal vals As Dictionary(Of String, Object)) As IFuture Implements IW3Game.f_ReceivePacket_CLIENT_COMMAND
            Return ref.enqueue(Function() eval(AddressOf ReceivePacket_CLIENT_COMMAND, player, vals))
        End Function
#End Region
    End Class
End Namespace
