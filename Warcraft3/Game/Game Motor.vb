Namespace WC3
    Public Class GameMotor
        Inherits FutureDisposable

        Private ReadOnly inQueue As ICallQueue
        Private ReadOnly _players As AsyncViewableCollection(Of Player)
        Private ReadOnly _clock As IClock
        Private ReadOnly _lobby As GameLobby
        Private ReadOnly _gameDataQueue As New Queue(Of Tuple(Of Player, IReadableList(Of Protocol.GameAction)))

        Private _laggingPlayers As New List(Of Player)
        Private _gameTime As Integer
        Private _leftoverGameTime As Double
        Private _speedFactor As Double
        Private _tickPeriod As TimeSpan
        Private _lagLimit As TimeSpan
        Private _tickClock As RelativeClock
        Private _lagClock As RelativeClock
        Private ReadOnly _init As New OnetimeLock

        Public Event ReceivedPlayerActions(ByVal sender As GameMotor, ByVal player As Player, ByVal actions As IReadableList(Of Protocol.GameAction))
        Public Event Tick(ByVal sender As GameMotor, ByVal timeSpan As UShort, ByVal actionSets As IReadableList(Of Tuple(Of Player, Protocol.PlayerActionSet)))
        Public Event RemovePlayer(ByVal sender As GameMotor, ByVal player As Player, ByVal wasExpected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_laggingPlayers IsNot Nothing)
            Contract.Invariant(_speedFactor > 0)
            Contract.Invariant(_init isnot nothing)
            Contract.Invariant(_tickPeriod.Ticks > 0)
            Contract.Invariant(_lagLimit.Ticks >= 0)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_players IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_lobby IsNot Nothing)
            Contract.Invariant(_gameTime >= 0)
            Contract.Invariant(_gameDataQueue IsNot Nothing)
        End Sub

        Public Sub New(ByVal defaultSpeedFactor As Double,
                       ByVal defaultTickPeriod As TimeSpan,
                       ByVal defaultLagLimit As TimeSpan,
                       ByVal inQueue As ICallQueue,
                       ByVal players As AsyncViewableCollection(Of Player),
                       ByVal clock As IClock,
                       ByVal lobby As GameLobby)
            Contract.Requires(defaultSpeedFactor > 0)
            Contract.Requires(defaultTickPeriod.Ticks > 0)
            Contract.Requires(defaultLagLimit.Ticks >= 0)
            Contract.Requires(inQueue IsNot Nothing)
            Contract.Requires(players IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Contract.Requires(lobby IsNot Nothing)
            Me._speedFactor = defaultSpeedFactor
            Me._tickPeriod = defaultTickPeriod
            Me._lagLimit = defaultLagLimit
            Me.inQueue = inQueue
            Me._players = players
            Me._clock = clock
            Me._lobby = lobby
        End Sub
        Public Sub Init()
            If FutureDisposed.State <> FutureState.Unknown Then Throw New ObjectDisposedException(Me.GetType.Name)
            If Not _init.TryAcquire Then Throw New InvalidOperationException("Already initialized.")

            _lobby.StartPlayerHoldPoint.IncludeActionHandler(
                Sub(newPlayer)
                    AddHandler newPlayer.ReceivedRequestDropLaggers, Sub() inQueue.QueueAction(Sub() OnDropLagger(newPlayer))
                    AddHandler newPlayer.ReceivedGameActions, Sub(sender, actions) inQueue.QueueAction(Sub() OnReceiveGameActions(sender, actions))
                End Sub)
        End Sub

        Private Sub Start()
            For Each player In _players
                Contract.Assume(player IsNot Nothing)
                player.QueueStartPlaying()
            Next player
            _tickClock = _clock.Restarted()
            OnTick()
        End Sub
        Public Function QueueStart() As IFuture
            Return inQueue.QueueAction(AddressOf Start)
        End Function
        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(Sub() _tickClock = Nothing)
        End Function

        Private _asyncWaitTriggered As Boolean
        Private Sub OnReceiveGameActions(ByVal sender As Player, ByVal actions As IReadableList(Of Protocol.GameAction))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(actions IsNot Nothing)
            _gameDataQueue.Enqueue(Tuple(sender, actions))
            RaiseEvent ReceivedPlayerActions(Me, sender, actions)

            '[async lag -wait command detection]
            If (From action In actions Where action.Id = Protocol.GameActionId.GameCacheSyncInteger
                                       Select vals = CType(action.Payload, Pickling.IPickle(Of Dictionary(Of InvariantString, Object))).Value
                                       Where CStr(vals("filename")) = "HostBot.AsyncLag" AndAlso CStr(vals("mission key")) = "wait").Any Then
                _asyncWaitTriggered = True
            End If
        End Sub

        Private Sub OnDropLagger(ByVal sender As Player)
            For Each player In _laggingPlayers
                Contract.Assume(player IsNot Nothing)
                RaiseEvent RemovePlayer(Me, player, True, Protocol.PlayerLeaveReason.Disconnect, "Lagger dropped")
            Next player
        End Sub

        Private Function BroadcastPacket(ByVal packet As Protocol.Packet) As ifuture
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return (From player In _players Select player.QueueSendPacket(packet)).Defuturized
        End Function

        '''<summary>Advances game time</summary>
        Private Sub OnTick()
            If _tickClock Is Nothing Then Return 'stopped

            _tickClock = _tickClock.Restarted()
            Dim dt = _tickClock.StartingTimeOnParentClock.TotalMilliseconds * _speedFactor
            Dim dgt = CUShort(_tickPeriod.TotalMilliseconds * _speedFactor).KeepAtOrAbove(1)

            'Stop for laggers
            UpdateLagScreen()
            If _laggingPlayers.Count > 0 Then
                _clock.AsyncWait(_tickPeriod).QueueCallOnSuccess(inQueue, AddressOf OnTick)
                Return
            End If

            'Schedule next tick
            _leftoverGameTime += dt - dgt
            _leftoverGameTime = _leftoverGameTime.Between(-dgt * 10, dgt * 10)
            Dim nextTickTime = CLng(dgt - _leftoverGameTime).Between(dgt \ 2US, dgt * 2US).Milliseconds
            _clock.AsyncWait(nextTickTime).QueueCallOnSuccess(inQueue, AddressOf OnTick)

            'Send
            SendQueuedGameData(New TickRecord(dgt, _gameTime))
            _gameTime += dgt
        End Sub
        Private Sub UpdateLagScreen()
            If _laggingPlayers.Count > 0 Then
                For Each p In _laggingPlayers.ToList
                    Contract.Assume(p IsNot Nothing)
                    If Not _players.Contains(p) Then
                        _laggingPlayers.Remove(p)
                    ElseIf p.GetTockTime >= _gameTime OrElse p.isFake Then

                        _laggingPlayers.Remove(p)
                        Dim p_ = p
                        If _lobby.IsPlayerVisible(p) OrElse (From q In _laggingPlayers
                                                             Where _lobby.GetVisiblePlayer(q) Is _lobby.GetVisiblePlayer(p_)).None Then
                            Contract.Assume(_lagClock IsNot Nothing)
                            BroadcastPacket(Protocol.MakeRemovePlayerFromLagScreen(
                                lagger:=_lobby.GetVisiblePlayer(p).Id,
                                lagTimeInMilliseconds:=CUInt(_lagClock.ElapsedTime.TotalMilliseconds))).SetHandled()
                        End If
                    End If
                Next p
            Else
                _laggingPlayers = (From p In _players
                                   Where Not p.isFake _
                                   AndAlso p.GetTockTime < _gameTime - If(_asyncWaitTriggered, 0, _lagLimit.TotalMilliseconds)
                                   ).ToList
                _asyncWaitTriggered = False
                If _laggingPlayers.Count > 0 Then
                    BroadcastPacket(Protocol.MakeShowLagScreen(From p In _laggingPlayers Select p.Id)).SetHandled()
                    _lagClock = _clock.Restarted()
                End If
            End If
        End Sub
        <ContractVerification(False)>
        Private Sub SendQueuedGameData(ByVal record As TickRecord)
            Contract.Requires(record IsNot Nothing)
            'Include all the data we can fit in a packet
            Dim totalDataLength = 0
            Dim outgoingActions = New List(Of Tuple(Of Player, Protocol.PlayerActionSet))(capacity:=_gameDataQueue.Count)
            While _gameDataQueue.Count > 0
                'peek
                Dim e = _gameDataQueue.Peek()
                Contract.Assume(e IsNot Nothing)
                Contract.Assume(e.Item1 IsNot Nothing)
                Contract.Assume(e.Item2 IsNot Nothing)
                Dim actionDataLength = (From action In e.Item2 Select action.Payload.Data.Count).Aggregate(Function(e1, e2) e1 + e2)
                If totalDataLength + actionDataLength >= PacketSocket.DefaultBufferSize - 20 Then '[20 includes headers and a small safety margin]
                    Exit While
                End If

                _gameDataQueue.Dequeue()
                outgoingActions.Add(Tuple(e.Item1, New Protocol.PlayerActionSet(_lobby.GetVisiblePlayer(e.Item1).Id, e.Item2)))
                totalDataLength += actionDataLength
            End While

            'Send data
            For Each player In _players
                Contract.Assume(player IsNot Nothing)
                If _lobby.IsPlayerVisible(player) Then
                    player.QueueSendTick(record, (From e In outgoingActions Select e.Item2).ToReadableList)
                Else
                    Dim player_ = player
                    player.QueueSendTick(record, (From e In outgoingActions
                                                  Let pid = If(e.Item1 Is player_, player_, _lobby.GetVisiblePlayer(e.Item1)).Id
                                                  Select New Protocol.PlayerActionSet(pid, e.Item2.Actions)
                                                  ).ToReadableList)
                End If
            Next player

            RaiseEvent Tick(Me, record.length, outgoingActions.AsReadableList)
        End Sub

        Public Function QueueGetTickPeriod() As IFuture(Of TimeSpan)
            Contract.Ensures(Contract.Result(Of IFuture(Of TimeSpan))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _tickPeriod)
        End Function
        Public Function QueueGetSpeedFactor() As IFuture(Of Double)
            Contract.Ensures(Contract.Result(Of IFuture(Of Double))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _speedFactor)
        End Function
        Public Function QueueGetLagLimit() As IFuture(Of TimeSpan)
            Contract.Ensures(Contract.Result(Of IFuture(Of TimeSpan))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _lagLimit)
        End Function

        Public Function QueueSetTickPeriod(ByVal value As TimeSpan) As IFuture
            Contract.Requires(value.Ticks > 0)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _tickPeriod = value)
        End Function
        Public Function QueueSetSpeedFactor(ByVal value As Double) As IFuture
            Contract.Requires(value > 0)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _speedFactor = value)
        End Function
        Public Function QueueSetLagLimit(ByVal value As TimeSpan) As IFuture
            Contract.Requires(value.Ticks >= 0)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() _lagLimit = value)
        End Function
    End Class
End Namespace
