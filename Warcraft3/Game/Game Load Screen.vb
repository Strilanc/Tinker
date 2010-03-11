Namespace WC3
    Public NotInheritable Class GameLoadScreen
        Private Shared ReadOnly LoadInGameTickPeriod As TimeSpan = 30.Seconds

        Private ReadOnly _readyPlayers As New HashSet(Of Player)
        Private ReadOnly _kernel As GameKernel
        Private ReadOnly _startLock As New OnetimeLock
        Private ReadOnly _lobby As GameLobby
        Private ReadOnly _logger As Logger
        Private ReadOnly _settings As GameSettings

        Private _loadInGameTickCount As Integer
        Private _loadInGameTicker As IDisposable

        Public Event RecordGameStarted(ByVal sender As GameLoadScreen)
        Public Event EmptyTick(ByVal sender As GameLoadScreen)
        Public Event Finished(ByVal sender As GameLoadScreen)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_settings IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_kernel IsNot Nothing)
            Contract.Invariant(_startLock IsNot Nothing)
            Contract.Invariant(_lobby IsNot Nothing)
            Contract.Invariant(_readyPlayers IsNot Nothing)
            Contract.Invariant(UnreadyPlayers IsNot Nothing)
            Contract.Invariant(_loadInGameTickCount >= 0)
        End Sub

        Public Sub New(ByVal kernel As GameKernel,
                       ByVal lobby As GameLobby,
                       ByVal logger As Logger,
                       ByVal settings As GameSettings)
            Contract.Assume(kernel IsNot Nothing)
            Contract.Assume(lobby IsNot Nothing)
            Contract.Assume(logger IsNot Nothing)
            Contract.Assume(settings IsNot Nothing)
            Me._kernel = kernel
            Me._lobby = lobby
            Me._logger = logger
            Me._settings = settings
        End Sub

        Public Sub Start()
            If Not _startLock.TryAcquire Then Throw New InvalidOperationException("Already started.")

            For Each player In _kernel.Players
                Contract.Assume(player IsNot Nothing)
                AddHandler player.ReceivedReady, Sub(sender) _kernel.InQueue.QueueAction(Sub() OnReceiveReady(sender))
                player.QueueStartLoading()
            Next player

            'Load
            _logger.Log("Players Loading", LogMessageType.Positive)

            'Ready any lingering fake players
            For Each player In From p In _kernel.Players
                               Where p.isFake
                               Where _lobby.IsPlayerVisible(p)
                               Where _lobby.Slots.TryFindPlayerSlot(p).Contents.Moveable
                Contract.Assume(player IsNot Nothing)
                _readyPlayers.Add(player)
                _lobby.BroadcastPacket(Protocol.MakeOtherPlayerReady(player.Id), Nothing)
            Next player

            If _settings.UseLoadInGame Then
                RaiseEvent RecordGameStarted(Me)
                _loadInGameTicker = _kernel.Clock.AsyncRepeat(LoadInGameTickPeriod, Sub() _kernel.InQueue.QueueAction(AddressOf OnLoadInGameTick))
            End If
        End Sub

        Private ReadOnly Property UnreadyPlayers As IEnumerable(Of Player)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of Player))() IsNot Nothing)
                Return From player In _kernel.Players Where Not _readyPlayers.Contains(player)
            End Get
        End Property
        Private ReadOnly Property VisibleUnreadyPlayers As IEnumerable(Of Player)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of Player))() IsNot Nothing)
                Return From player In unreadyPlayers Where _lobby.IsPlayerVisible(player)
            End Get
        End Property
        Private ReadOnly Property VisibleReadyPlayers As IEnumerable(Of Player)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of Player))() IsNot Nothing)
                Return From player In _readyPlayers Where _lobby.IsPlayerVisible(player)
            End Get
        End Property

        Public Sub OnRemovedPlayer()
            TryLaunch()
        End Sub

        '''<summary>Starts the in-game play if all players are ready</summary>
        Private Function TryLaunch() As Boolean
            If (From p In _kernel.Players Where Not p.IsReady).Any Then Return False

            _kernel.State = GameState.Playing
            _logger.Log("Launching", LogMessageType.Positive)

            'start gameplay
            If _loadInGameTicker IsNot Nothing Then
                _loadInGameTicker.Dispose()
                _loadInGameTicker = Nothing
            End If
            If Not _settings.UseLoadInGame Then
                RaiseEvent RecordGameStarted(Me)
            End If
            RaiseEvent Finished(Me)
            Return True
        End Function

        '''<summary>Starts the in-game play</summary>
        Private Sub Launch()
            If Not TryLaunch() Then
                Throw New InvalidOperationException("Not all players are ready.")
            End If
        End Sub

        Private Sub OnReceiveReady(ByVal sendingPlayer As Player)
            Contract.Requires(sendingPlayer IsNot Nothing)
            If Not unreadyPlayers.Contains(sendingPlayer) Then Return

            'Make the sending player ready
            _readyPlayers.Add(sendingPlayer)
            If Not _lobby.IsPlayerVisible(sendingPlayer) Then
                sendingPlayer.QueueSendPacket(Protocol.MakeOtherPlayerReady(sendingPlayer.Id))
            End If

            'Make the visible player ready once all covered players are ready
            Dim visibleSender = _lobby.GetVisiblePlayer(sendingPlayer)
            Dim visibleSenderReady = (From p In _kernel.Players
                                      Where p IsNot visibleSender
                                      Where Not p.IsReady
                                      Where _lobby.GetVisiblePlayer(p) Is visibleSender
                                      ).None
            If visibleSenderReady Then
                _readyPlayers.Add(visibleSender)
            End If

            'Inform players of current ready-state
            If _settings.UseLoadInGame Then
                'Pretend to sender that all players are ready
                For Each player In _kernel.Players
                    Contract.Assume(player IsNot Nothing)
                    If _lobby.IsPlayerVisible(player) Then
                        sendingPlayer.QueueSendPacket(Protocol.MakeOtherPlayerReady(player.Id))
                    End If
                Next player
                'Bring sender up to the correct number of ticks
                For Each repeat In _loadInGameTickCount.Range
                    sendingPlayer.QueueSendPacket(Protocol.MakeTick(0))
                Next repeat

                'Show lag screen with remaining loaders to sender
                If VisibleUnreadyPlayers.Any Then
                    sendingPlayer.QueueSendPacket(Protocol.MakeShowLagScreen(From p In VisibleUnreadyPlayers Select p.Id))
                End If
                'Inform sender that load-in-game is occuring
                If unreadyPlayers.Any Then
                    _lobby.SendMessageTo("{0} players still loading.".Frmt(unreadyPlayers.Count), sendingPlayer)
                End If
                'Inform others that sender is ready
                For Each player In From p In _readyPlayers
                                   Where p IsNot sendingPlayer
                    Contract.Assume(player IsNot Nothing)
                    _lobby.SendMessageTo("{0} finished loading.".Frmt(sendingPlayer.Name), player)
                    If visibleSenderReady Then
                        player.QueueSendPacket(Protocol.MakeRemovePlayerFromLagScreen(visibleSender.Id, 0))
                    End If
                Next player
            Else
                'Inform everyone that the sender is ready
                If visibleSenderReady Then
                    _lobby.BroadcastPacket(Protocol.MakeOtherPlayerReady(visibleSender.Id), Nothing)
                End If
            End If

            TryLaunch()
        End Sub

        Private Sub OnLoadInGameTick()
            If _kernel.State > GameState.Loading Then Return 'dangling call
            If _readyPlayers.None Then Return 'no danger of tick timeouts during load
            Contract.Assume(_loadInGameTicker IsNot Nothing)

            'Send an empty tick (refreshing the lag screen) to avoid timeouts
            _loadInGameTickCount += 1
            For Each player In _readyPlayers
                Contract.Assume(player IsNot Nothing)
                'Hide lag screen for tick
                For Each other In visibleUnreadyPlayers
                    Contract.Assume(other IsNot Nothing)
                    player.QueueSendPacket(Protocol.MakeRemovePlayerFromLagScreen(other.Id, 0))
                Next other
                'Empty tick
                player.QueueSendPacket(Protocol.MakeTick(0))
                'Reshow lag screen
                player.QueueSendPacket(Protocol.MakeShowLagScreen(From p In visibleUnreadyPlayers Select p.Id))
            Next player

            RaiseEvent EmptyTick(Me)
        End Sub
    End Class
End Namespace