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
Imports HostBot.Commands

Namespace Warcraft3
    Public Enum W3GameState
        AcceptingPlayers = 0
        PreCounting = 1
        CountingDown = 2
        Loading = 3
        Playing = 4
        Closed = 5
    End Enum

    Partial Public NotInheritable Class W3Game
        Private ReadOnly _server As W3Server
        Private ReadOnly _map As W3Map
        Private ReadOnly _name As String
        Private ReadOnly rand As New Random()
        Private ReadOnly slots As New List(Of W3Slot)
        Private ReadOnly ref As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly eventRef As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly _logger As Logger
        Private Const PING_PERIOD As UShort = 5000
        Private ReadOnly pingTimer As New Timers.Timer(PING_PERIOD)
        Private state As W3GameState = W3GameState.AcceptingPlayers
        Private fakeHostPlayer As W3Player
        Private flagHasPlayerLeft As Boolean
        Private adminPlayer As W3Player
        Private ReadOnly players As New List(Of W3Player)
        Private ReadOnly index_map(0 To 12) As Byte

        Public Event PlayerAction(ByVal sender As W3Game, ByVal player As W3Player, ByVal action As W3GameAction)
        Public Event Updated(ByVal sender As W3Game, ByVal slots As List(Of W3Slot))
        Public Event PlayerTalked(ByVal sender As W3Game, ByVal speaker As W3Player, ByVal text As String)
        Public Event PlayerLeft(ByVal sender As W3Game, ByVal state As W3GameState, ByVal leaver As W3Player, ByVal leaveType As W3PlayerLeaveType, ByVal reason As String)
        Public Event ChangedState(ByVal sender As W3Game, ByVal oldState As W3GameState, ByVal newState As W3GameState)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(Server IsNot Nothing)
            Contract.Invariant(Map IsNot Nothing)
            Contract.Invariant(Name IsNot Nothing)
            Contract.Invariant(slots IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eventRef IsNot Nothing)
            Contract.Invariant(Logger IsNot Nothing)
            Contract.Invariant(pingTimer IsNot Nothing)
            Contract.Invariant(players IsNot Nothing)
            Contract.Invariant(index_map IsNot Nothing)

            Contract.Invariant(readyPlayers IsNot Nothing)
            Contract.Invariant(unreadyPlayers IsNot Nothing)
            Contract.Invariant(visibleReadyPlayers IsNot Nothing)
            Contract.Invariant(visibleUnreadyPlayers IsNot Nothing)
            Contract.Invariant(fakeTickTimer IsNot Nothing)
            Contract.Invariant(DownloadScheduler IsNot Nothing)
            Contract.Invariant(_gameTime >= 0)
        End Sub

#Region "Commands"
        Private Sub CommandProcessLocalText(ByVal text As String, ByVal logger As Logger)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Select Case state
                Case Is < W3GameState.Loading
                    Server.parent.GameCommandsLobby.ProcessLocalText(Me, text, logger)
                Case W3GameState.Loading
                    Server.parent.GameCommandsLoadScreen.ProcessLocalText(Me, text, logger)
                Case Is > W3GameState.Loading
                    Server.parent.GameCommandsGamePlay.ProcessLocalText(Me, text, logger)
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
                    Case Is < W3GameState.Loading
                        Return Server.parent.GameGuestCommandsLobby.ProcessCommand(Me, user, arguments)
                    Case W3GameState.Loading
                        Return Server.parent.GameGuestCommandsLoadScreen.ProcessCommand(Me, user, arguments)
                    Case Is > W3GameState.Loading
                        Return Server.parent.GameGuestCommandsGamePlay.ProcessCommand(Me, user, arguments)
                    Case Else
                        Throw New UnreachableException()
                End Select
            ElseIf Server.settings.isAdminGame Then
                Return Server.parent.GameCommandsAdmin.ProcessCommand(Me, Nothing, arguments)
            Else
                Select Case state
                    Case Is < W3GameState.Loading
                        Return Server.parent.GameCommandsLobby.ProcessCommand(Me, user, arguments)
                    Case W3GameState.Loading
                        Return Server.parent.GameCommandsLoadScreen.ProcessCommand(Me, user, arguments)
                    Case Is > W3GameState.Loading
                        Return Server.parent.GameCommandsGamePlay.ProcessCommand(Me, user, arguments)
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
            Logger.Log(sender.name + ": " + text, LogMessageType.Typical)
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
                    ReceiveSetRace(sender, CType(vals("new value"), W3Slot.Races))

                Case W3Packet.NonGameAction.SetColor
                    ReceiveSetColor(sender, CType(vals("new value"), W3Slot.PlayerColor))

                Case Else
                    RemovePlayer(sender, True, W3PlayerLeaveType.Disconnect, "Sent unrecognized client command type: {0}".Frmt(commandType))
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

            Me._map = map
            Me._server = parent
            Me._name = name
            Me._logger = If(logger, New Logger)
            For i = 0 To index_map.Length - 1
                index_map(i) = CByte(i)
            Next i

            LobbyNew(arguments)
            LoadScreenNew()
            GamePlayNew()

            AddHandler pingTimer.Elapsed, Sub() CatchPing()
            Me.pingTimer.Start()

            Contract.Assume(DownloadScheduler IsNot Nothing)
            Contract.Assume(fakeTickTimer IsNot Nothing)
            Contract.Assume(visibleUnreadyPlayers IsNot Nothing)
            Contract.Assume(visibleReadyPlayers IsNot Nothing)
            Contract.Assume(unreadyPlayers IsNot Nothing)
            Contract.Assume(readyPlayers IsNot Nothing)
        End Sub

        '''<summary>Disconnects from all players and kills the instance. Passes hosting to a player if possible.</summary>
        Private Sub Close()
            If state >= W3GameState.Closed Then
                Return
            End If
            pingTimer.Stop()

            'Pass hosting duty to another player if possible
            If state = W3GameState.Playing AndAlso players.Count > 1 Then
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
                    Logger.Log(Name + " has handed off hosting to " + host.name, LogMessageType.Positive)
                Else
                    Logger.Log(Name + " has failed to hand off hosting", LogMessageType.Negative)
                End If
            End If

            'disconnect from all players
            For Each p In players.ToList
                Contract.Assume(p IsNot Nothing)
                RemovePlayer(p, True, W3PlayerLeaveType.Disconnect, "Closing game")
            Next p

            ChangeState(W3GameState.Closed)
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
        Private Sub ThrowPlayerLeft(ByVal leaver As W3Player, ByVal leaveType As W3PlayerLeaveType, ByVal reason As String)
            Dim state = Me.state
            eventRef.QueueAction(Sub()
                                     RaiseEvent PlayerLeft(Me, state, leaver, leaveType, reason)
                                 End Sub)
        End Sub
        Private Sub ThrowStateChanged(ByVal old_state As W3GameState, ByVal new_state As W3GameState)
            eventRef.QueueAction(Sub()
                                     RaiseEvent ChangedState(Me, old_state, new_state)
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
        '''<summary>Converts versus strings to a list of the team sizes (eg. 1v3v2 -> {1,3,2}).</summary>
        Public Shared Function TeamVersusStringToTeamSizes(ByVal value As String) As IList(Of Integer)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IList(Of Integer))() IsNot Nothing)

            'parse numbers between 'v's
            Dim vals = value.ToUpperInvariant.Split("V"c)
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

        Private Sub ChangeState(ByVal newState As W3GameState)
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
            Logger.Log("{0}: {1}".Frmt(My.Resources.ProgramName, message), LogMessageType.Typical)
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
                                                     chatType:=If(state >= W3GameState.Loading, W3Packet.ChatType.Game, W3Packet.ChatType.Lobby),
                                                     receiverType:=W3Packet.ChatReceiverType.AllPlayers,
                                                     receivingPlayers:=players,
                                                     sender:=If(fakeHostPlayer, player)))

            If display Then
                Logger.Log("(Private to {0}): {1}".Frmt(player.name, message), LogMessageType.Typical)
            End If
        End Sub
#End Region

#Region "Players"
        '''<summary>Removes the given player from the instance</summary>
        Private Sub RemovePlayer(ByVal player As W3Player,
                                 ByVal wasExpected As Boolean,
                                 ByVal leaveType As W3PlayerLeaveType,
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
                Case Is < W3GameState.Loading
                    LobbyCatchRemovedPlayer(player, slot)
                Case W3GameState.Loading
                    OnLoadScreenRemovedPlayer(player, slot)
                Case Is > W3GameState.Loading
            End Select
            If player Is fakeHostPlayer Then
                fakeHostPlayer = Nothing
            End If

            'Clean game
            If state >= W3GameState.Loading AndAlso Not (From x In players Where Not x.isFake).Any Then
                'the game has started and everyone has left, time to die
                Close()
            End If

            'Log
            If player.isFake Then
                Logger.Log("{0} has been removed from the game. ({1})".Frmt(player.name, reason), LogMessageType.Negative)
            Else
                flagHasPlayerLeft = True
                If wasExpected Then
                    Logger.Log("{0} has left the game. ({1})".Frmt(player.name, reason), LogMessageType.Negative)
                Else
                    Logger.Log("{0} has disconnected. ({1})".Frmt(player.name, reason), LogMessageType.Problem)
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
                If password.ToUpperInvariant <> Server.settings.adminPassword.ToUpperInvariant Then
                    Throw New InvalidOperationException("Incorrect password.")
                End If
            End If

            adminPlayer = player
            SendMessageTo("You are now the admin.", player)
        End Sub

        Private Function FindPlayer(ByVal username As String) As W3Player
            Contract.Requires(username IsNot Nothing)
            Return (From x In players Where x.name.ToUpperInvariant = username.ToUpperInvariant).FirstOrDefault
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

            Dim target = (From player In slot.contents.EnumPlayers
                          Where player.name.ToUpperInvariant = slotQuery.ToUpperInvariant).FirstOrDefault
            If target IsNot Nothing Then
                slot.contents = slot.contents.RemovePlayer(target)
                RemovePlayer(target, True, W3PlayerLeaveType.Disconnect, "Booted")
                Return
            End If

            For Each player In slot.contents.EnumPlayers
                Contract.Assume(player IsNot Nothing)
                slot.contents = slot.contents.RemovePlayer(player)
                RemovePlayer(player, True, W3PlayerLeaveType.Disconnect, "Booted")
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
        Public ReadOnly Property Logger As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property

        Public ReadOnly Property Map As W3Map
            Get
                Contract.Ensures(Contract.Result(Of W3Map)() IsNot Nothing)
                Return _map
            End Get
        End Property

        Public ReadOnly Property Name As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property

        Public ReadOnly Property Server As W3Server
            Get
                Contract.Ensures(Contract.Result(Of W3Server)() IsNot Nothing)
                Return _server
            End Get
        End Property

        Public Function QueueGetAdminPlayer() As IFuture(Of W3Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Player))() IsNot Nothing)
            Return ref.QueueFunc(Function() adminPlayer)
        End Function
        Public Function QueueGetFakeHostPlayer() As IFuture(Of W3Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Player))() IsNot Nothing)
            Return ref.QueueFunc(Function() fakeHostPlayer)
        End Function
        Public Function QueueCommandProcessLocalText(ByVal text As String, ByVal logger As Logger) As IFuture
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(text IsNot Nothing)
                                       Contract.Assume(logger IsNot Nothing)
                                       CommandProcessLocalText(text, logger)
                                   End Sub)
        End Function
        Public Function QueueCommandProcessText(ByVal player As W3Player,
                                                ByVal arguments As IList(Of String)) As IFuture(Of String)
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(arguments IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueFunc(Function()
                                     Contract.Assume(player IsNot Nothing)
                                     Contract.Assume(arguments IsNot Nothing)
                                     Return CommandProcessText(player, arguments)
                                 End Function).Defuturized
        End Function
        Public Function QueueTryElevatePlayer(ByVal name As String,
                                              Optional ByVal password As String = Nothing) As IFuture
            Contract.Requires(name IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(name IsNot Nothing)
                                       Contract.Assume(password IsNot Nothing)
                                       TryElevatePlayer(name, password)
                                   End Sub)
        End Function
        Public Function QueueFindPlayer(ByVal userName As String) As IFuture(Of W3Player)
            Contract.Requires(userName IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Player))() IsNot Nothing)
            Return ref.QueueFunc(Function()
                                     Contract.Assume(userName IsNot Nothing)
                                     Return FindPlayer(userName)
                                 End Function)
        End Function
        Public Function QueueRemovePlayer(ByVal player As W3Player,
                                          ByVal expected As Boolean,
                                          ByVal leaveType As W3PlayerLeaveType,
                                          ByVal reason As String) As IFuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(player IsNot Nothing)
                                       Contract.Assume(reason IsNot Nothing)
                                       RemovePlayer(player, expected, leaveType, reason)
                                   End Sub)
        End Function
        Public Function QueueGetPlayers() As IFuture(Of List(Of W3Player))
            Contract.Ensures(Contract.Result(Of IFuture(Of List(Of W3Player)))() IsNot Nothing)
            Return ref.QueueFunc(Function() players.ToList)
        End Function
        Public Function QueueThrowUpdated() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(AddressOf ThrowUpdated)
        End Function
        Public Function QueueClose() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() Close())
        End Function
        Public Function QueueBroadcastMessage(ByVal message As String) As IFuture
            Contract.Requires(message IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(message IsNot Nothing)
                                       BroadcastMessage(message)
                                   End Sub)
        End Function
        Public Function QueueSendMessageTo(ByVal message As String, ByVal player As W3Player) As IFuture
            Contract.Requires(message IsNot Nothing)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(message IsNot Nothing)
                                       Contract.Assume(player IsNot Nothing)
                                       SendMessageTo(message, player)
                                   End Sub)
        End Function
        Public Function QueueBootSlot(ByVal slotQuery As String) As IFuture
            Contract.Requires(slotQuery IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(slotQuery IsNot Nothing)
                                       Boot(slotQuery)
                                   End Sub)
        End Function
        Public Function QueueGetState() As IFuture(Of W3GameState)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3GameState))() IsNot Nothing)
            Return ref.QueueFunc(Function() Me.state)
        End Function
        Public Function QueueReceiveNonGameAction(ByVal player As W3Player,
                                                  ByVal values As Dictionary(Of String, Object)) As IFuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(values IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(player IsNot Nothing)
                                       Contract.Assume(values IsNot Nothing)
                                       ReceiveNonGameAction(player, values)
                                   End Sub)
        End Function
#End Region
    End Class
End Namespace
