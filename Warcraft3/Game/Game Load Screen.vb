Namespace WC3
    Public NotInheritable Class GameLoadScreen
        Private ReadOnly inQueue As CallQueue
        Private ReadOnly readyPlayers As New HashSet(Of Player)
        Private ReadOnly unreadyPlayers As New HashSet(Of Player)
        Private ReadOnly visibleReadyPlayers As New HashSet(Of Player)
        Private ReadOnly visibleUnreadyPlayers As New HashSet(Of Player)
        Private numFakeTicks As Integer
        Private fakeTickTimer As Timers.Timer
        Private ReadOnly _kernel As GameKernel
        Private ReadOnly _initialized As New OneTimeLock
        Private ReadOnly _lobby As GameLobby
        Private ReadOnly _logger As Logger
        Private ReadOnly _settings As GameSettings
        Private ReadOnly _motor As GameMotor

        Public Event Launched(ByVal sender As GameLoadScreen, ByVal usingLoadInGame As Boolean)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_settings IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_kernel IsNot Nothing)
            Contract.Invariant(_initialized IsNot Nothing)
            Contract.Invariant(_lobby IsNot Nothing)
            Contract.Invariant(_motor IsNot Nothing)
            Contract.Invariant(readyPlayers IsNot Nothing)
            Contract.Invariant(unreadyPlayers IsNot Nothing)
            Contract.Invariant(visibleReadyPlayers IsNot Nothing)
            Contract.Invariant(visibleUnreadyPlayers IsNot Nothing)
        End Sub

        Public Sub New(ByVal kernel As GameKernel,
                       ByVal inQueue As CallQueue,
                       ByVal clock As IClock,
                       ByVal lobby As GameLobby,
                       ByVal logger As Logger,
                       ByVal settings As GameSettings,
                       ByVal motor As GameMotor)
            Contract.Assume(kernel IsNot Nothing)
            Contract.Assume(lobby IsNot Nothing)
            Contract.Assume(logger IsNot Nothing)
            Contract.Assume(settings IsNot Nothing)
            Contract.Assume(clock IsNot Nothing)
            Contract.Assume(motor IsNot Nothing)
            Contract.Assume(inQueue IsNot Nothing)
            Me._kernel = kernel
            Me._lobby = lobby
            Me._logger = logger
            Me._settings = settings
            Me._motor = motor
            Me.inQueue = inQueue

            Me.fakeTickTimer = New Timers.Timer(30.Seconds.TotalMilliseconds)
            AddHandler fakeTickTimer.Elapsed, Sub() OnFakeTick()
        End Sub

        Public Sub Start()
            For Each player In _kernel.Players
                Contract.Assume(player IsNot Nothing)
                player.QueueStartLoading()
                unreadyPlayers.Add(player)
                If _lobby.IsPlayerVisible(player) Then visibleUnreadyPlayers.Add(player)
            Next player

            'Load
            _logger.Log("Players Loading", LogMessageType.Positive)

            'Ready any lingering fake players
            For Each player In From p In _kernel.Players
                               Where p.isFake
                               Where _lobby.IsPlayerVisible(p)
                               Where _lobby.Slots.TryFindPlayerSlot(p).Contents.Moveable
                Contract.Assume(player IsNot Nothing)
                readyPlayers.Add(player)
                unreadyPlayers.Remove(player)
                visibleReadyPlayers.Add(player)
                visibleUnreadyPlayers.Remove(player)
                player.Ready = True
                _lobby.BroadcastPacket(Protocol.MakeOtherPlayerReady(player.Id), Nothing)
            Next player

            If _settings.UseLoadInGame Then
                RaiseEvent Launched(Me, usingloadInGame:=True)
                Contract.Assume(fakeTickTimer IsNot Nothing)
                fakeTickTimer.Start()
            End If
        End Sub
        Private Sub LoadScreenStop()
            Contract.Assume(fakeTickTimer IsNot Nothing)
            fakeTickTimer.Stop()
        End Sub

        Public Sub OnRemovedPlayer()
            TryLaunch()
        End Sub

        '''<summary>Starts the in-game play if all players are ready</summary>
        Private Function TryLaunch() As Boolean
            If (From x In _kernel.Players Where Not x.Ready AndAlso Not x.isFake).Any Then
                Return False
            End If
            _kernel.State = GameState.Playing
            _logger.Log("Launching", LogMessageType.Positive)

            'start gameplay
            Me.LoadScreenStop()
            _Motor.QueueStart()
            If Not _settings.UseLoadInGame Then
                RaiseEvent Launched(Me, usingloadInGame:=False)
            End If
            Return True
        End Function

        '''<summary>Starts the in-game play</summary>
        Private Sub Launch()
            If Not TryLaunch() Then
                Throw New InvalidOperationException("Not all players are ready.")
            End If
        End Sub

        Public Sub OnReceiveReady(ByVal sendingPlayer As Player)
            Contract.Requires(sendingPlayer IsNot Nothing)

            'Get if there is a visible readied player
            Dim visibleReadiedPlayer As Player = Nothing
            If _lobby.IsPlayerVisible(sendingPlayer) Then
                visibleReadiedPlayer = sendingPlayer
            Else
                sendingPlayer.QueueSendPacket(Protocol.MakeOtherPlayerReady(sendingPlayer.Id))
                readyPlayers.Add(sendingPlayer)
                unreadyPlayers.Remove(sendingPlayer)

                Dim slot = _lobby.Slots.TryFindPlayerSlot(sendingPlayer)
                Contract.Assume(slot IsNot Nothing)
                If (From x In slot.Contents.EnumPlayers Where Not (x.Ready Or x.isFake)).None Then
                    visibleReadiedPlayer = slot.Contents.EnumPlayers.First
                End If
            End If

            If visibleReadiedPlayer IsNot Nothing Then
                readyPlayers.Add(visibleReadiedPlayer)
                unreadyPlayers.Remove(visibleReadiedPlayer)
                visibleReadyPlayers.Add(visibleReadiedPlayer)
                visibleUnreadyPlayers.Remove(visibleReadiedPlayer)
            End If

            If _settings.UseLoadInGame Then
                For Each player In _kernel.Players
                    Contract.Assume(player IsNot Nothing)
                    If _lobby.IsPlayerVisible(player) Then
                        sendingPlayer.QueueSendPacket(Protocol.MakeOtherPlayerReady(player.Id))
                    End If
                Next player
                For i = 1 To numFakeTicks
                    sendingPlayer.QueueSendPacket(Protocol.MakeTick(0))
                Next i

                If unreadyPlayers.Any Then
                    _lobby.SendMessageTo("{0} players still loading.".Frmt(unreadyPlayers.Count), sendingPlayer)
                End If
                For Each player In readyPlayers
                    Contract.Assume(player IsNot Nothing)
                    If player IsNot sendingPlayer Then
                        _lobby.SendMessageTo("{0} is ready.".Frmt(sendingPlayer.Name), player)
                        If visibleReadiedPlayer IsNot Nothing Then
                            player.QueueSendPacket(Protocol.MakeRemovePlayerFromLagScreen(visibleReadiedPlayer.Id, 0))
                        End If
                    End If
                Next player

                If visibleUnreadyPlayers.Count > 0 Then
                    sendingPlayer.QueueSendPacket(Protocol.MakeShowLagScreen(From p In visibleUnreadyPlayers Select p.Id))
                End If
            Else
                If visibleReadiedPlayer IsNot Nothing Then
                    _lobby.BroadcastPacket(Protocol.MakeOtherPlayerReady(visibleReadiedPlayer.Id), Nothing)
                End If
            End If

            TryLaunch()
        End Sub

        Private Sub OnFakeTick()
            inQueue.QueueAction(
                Sub()
                    If _kernel.State > GameState.Loading Then Return
                    If readyPlayers.Count = 0 Then Return

                    numFakeTicks += 1
                    For Each player In readyPlayers
                        Contract.Assume(player IsNot Nothing)
                        For Each other In visibleUnreadyPlayers
                            Contract.Assume(other IsNot Nothing)
                            player.QueueSendPacket(Protocol.MakeRemovePlayerFromLagScreen(other.Id, 0))
                        Next other
                        player.QueueSendPacket(Protocol.MakeTick(0))
                        player.QueueSendPacket(Protocol.MakeShowLagScreen(From p In visibleUnreadyPlayers Select p.Id))
                    Next player
                End Sub
            )
        End Sub
    End Class
End Namespace