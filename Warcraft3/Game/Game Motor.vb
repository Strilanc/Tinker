Namespace WC3
    Public NotInheritable Class TickRecord
        Public ReadOnly length As UShort
        Public ReadOnly startTime As Integer
        Public ReadOnly Property EndTime() As Integer
            Get
                Return length + startTime
            End Get
        End Property

        Public Sub New(ByVal length As UShort, ByVal startTime As Integer)
            Me.length = length
            Me.startTime = startTime
        End Sub
    End Class

    Public Class GameMotor
        Inherits DisposableWithTask

        Private ReadOnly _kernel As GameKernel
        Private ReadOnly _lobby As GameLobby
        Private ReadOnly _actionsForNextTick As New List(Of Protocol.SpecificPlayerActionSet)

        Private _laggingPlayers As New List(Of Player)
        Private _gameTime As Integer
        Private _leftoverTickDurations As TimeSpan
        Private _speedFactor As Double
        Private _tickPeriod As TimeSpan
        Private _lagLimit As TimeSpan
        Private _tickClock As RelativeClock
        Private _lagClock As RelativeClock
        Private ReadOnly _init As New OnetimeLock

        Public Event ReceivedPlayerActions(ByVal sender As GameMotor, ByVal player As Player, ByVal actions As IRist(Of Protocol.GameAction))
        Public Event Tick(ByVal sender As GameMotor,
                          ByVal timeSpan As UShort,
                          ByVal actualActionStreaks As IRist(Of IRist(Of Protocol.SpecificPlayerActionSet)),
                          ByVal visibleActionStreaks As IRist(Of IRist(Of Protocol.PlayerActionSet)))
        Public Event RemovePlayer(ByVal sender As GameMotor, ByVal player As Player, ByVal wasExpected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_laggingPlayers IsNot Nothing)
            Contract.Invariant(_speedFactor > 0)
            Contract.Invariant(_init IsNot Nothing)
            Contract.Invariant(_tickPeriod.Ticks > 0)
            Contract.Invariant(_lagLimit.Ticks >= 0)
            Contract.Invariant(_kernel IsNot Nothing)
            Contract.Invariant(_lobby IsNot Nothing)
            Contract.Invariant(_gameTime >= 0)
            Contract.Invariant(_actionsForNextTick IsNot Nothing)
        End Sub

        Public Sub New(ByVal defaultSpeedFactor As Double,
                       ByVal defaultTickPeriod As TimeSpan,
                       ByVal defaultLagLimit As TimeSpan,
                       ByVal kernel As GameKernel,
                       ByVal lobby As GameLobby)
            Contract.Assume(defaultSpeedFactor > 0)
            Contract.Assume(defaultTickPeriod.Ticks > 0)
            Contract.Assume(defaultLagLimit.Ticks >= 0)
            Contract.Assume(kernel IsNot Nothing)
            Contract.Assume(lobby IsNot Nothing)
            Me._speedFactor = defaultSpeedFactor
            Me._tickPeriod = defaultTickPeriod
            Me._lagLimit = defaultLagLimit
            Me._kernel = kernel
            Me._lobby = lobby
        End Sub
        Public Sub Init()
            If Me.IsDisposed Then Throw New ObjectDisposedException(Me.GetType.Name)
            If Not _init.TryAcquire Then Throw New InvalidOperationException("Already initialized.")

            _lobby.StartPlayerHoldPoint.IncludeActionHandler(
                Sub(newPlayer)
                    newPlayer.QueueAddPacketHandler(Protocol.ClientPackets.RequestDropLaggers,
                                                    handler:=Function() _kernel.InQueue.QueueAction(Sub() OnReceiveRequestDropLagger(newPlayer)))
                    newPlayer.QueueAddPacketHandler(Protocol.ClientPackets.GameAction,
                                                    handler:=Function(value) _kernel.InQueue.QueueAction(Sub() OnReceiveGameActions(newPlayer, value)))
                End Sub)
        End Sub

        Private Sub Start()
            For Each player In _kernel.Players
                Contract.Assume(player IsNot Nothing)
                player.QueueStartPlaying()
            Next player
            _tickClock = _kernel.Clock.Restarted()
            OnTick()
        End Sub
        Public Function QueueStart() As Task
            Return _kernel.InQueue.QueueAction(AddressOf Start)
        End Function
        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return _kernel.InQueue.QueueAction(Sub() _tickClock = Nothing)
        End Function

        Private _asyncWaitTriggered As Boolean
        Private Sub OnReceiveGameActions(ByVal sender As Player, ByVal actions As IRist(Of Protocol.GameAction))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(actions IsNot Nothing)
            _actionsForNextTick.Add(New Protocol.SpecificPlayerActionSet(sender, actions))
            RaiseEvent ReceivedPlayerActions(Me, sender, actions)

            '[async lag -wait command detection]
            If (From action In actions Where action.Definition Is Protocol.GameActions.GameCacheSyncInteger
                                       Select vals = DirectCast(action.Payload, NamedValueMap)
                                       Where vals.ItemAs(Of String)("filename") = "HostBot.AsyncLag" AndAlso vals.ItemAs(Of String)("mission key") = "wait").Any Then
                _asyncWaitTriggered = True
            End If
        End Sub

        <CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId:="sender")>
        Private Sub OnReceiveRequestDropLagger(ByVal sender As Player)
            Contract.Requires(sender IsNot Nothing)
            For Each player In _laggingPlayers
                Contract.Assume(player IsNot Nothing)
                RaiseEvent RemovePlayer(Me, player, True, Protocol.PlayerLeaveReason.Disconnect, "Lagger dropped")
            Next player
        End Sub

        Private Function BroadcastPacket(ByVal packet As Protocol.Packet) As Task
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return (From player In _kernel.Players Select player.QueueSendPacket(packet)).AsAggregateTask
        End Function

        '''<summary>Advances game time</summary>
        Private Sub OnTick()
            If _tickClock Is Nothing Then Return 'stopped

            _tickClock = _tickClock.Restarted()

            'Stop for laggers
            UpdateLagScreen()
            If _laggingPlayers.Count > 0 Then
                _kernel.Clock.AsyncWait(_tickPeriod).QueueContinueWithAction(_kernel.InQueue, AddressOf OnTick)
                Return
            End If

            'Schedule next tick
            Contract.Assume(_tickClock IsNot Nothing)
            Dim measuredTickDuration = _tickClock.StartingTimeOnParentClock
            Dim expectedTickDuration = _tickPeriod
            _leftoverTickDurations += measuredTickDuration - expectedTickDuration
            _leftoverTickDurations = _leftoverTickDurations.Between(-5.Seconds, 5.Seconds)
            Dim nextTickDuration = (_tickPeriod - _leftoverTickDurations).Between(expectedTickDuration.Times(0.5), expectedTickDuration.Times(2))
            _kernel.Clock.AsyncWait(nextTickDuration).QueueContinueWithAction(_kernel.InQueue, AddressOf OnTick)

            'Send tick
            Dim gameTickDuration = CUShort(nextTickDuration.TotalMilliseconds * _speedFactor)
            SendQueuedGameData(New TickRecord(gameTickDuration, _gameTime))
            _gameTime += gameTickDuration
        End Sub
        Private Sub UpdateLagScreen()
            If _laggingPlayers.Count > 0 Then
                For Each p In _laggingPlayers.ToList
                    Contract.Assume(p IsNot Nothing)
                    If Not _kernel.Players.Contains(p) Then
                        _laggingPlayers.Remove(p)
                    ElseIf p.TockTime >= _gameTime OrElse p.IsFake Then

                        _laggingPlayers.Remove(p)
                        Dim p_ = p
                        If _lobby.IsPlayerVisible(p) OrElse (From q In _laggingPlayers
                                                             Where _lobby.GetVisiblePlayer(q) Is _lobby.GetVisiblePlayer(p_)).None Then
                            Contract.Assume(_lagClock IsNot Nothing)
                            BroadcastPacket(Protocol.MakePlayerStoppedLagging(
                                lagger:=_lobby.GetVisiblePlayer(p).Id,
                                lagTimeInMilliseconds:=CUInt(_lagClock.ElapsedTime.TotalMilliseconds))).IgnoreExceptions()
                        End If
                    End If
                Next p
            Else
                _laggingPlayers = (From p In _kernel.Players
                                   Where Not p.IsFake _
                                   AndAlso p.TockTime < _gameTime - If(_asyncWaitTriggered, 0, _lagLimit.TotalMilliseconds)
                                   ).ToList
                _asyncWaitTriggered = False
                If _laggingPlayers.Count > 0 Then
                    BroadcastPacket(Protocol.MakePlayersLagging(From p In _laggingPlayers Select p.Id)).IgnoreExceptions()
                    _lagClock = _kernel.Clock.Restarted()
                End If
            End If
        End Sub

        <CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", justification:="The analyzer doesn't see the maxDataSize parameter from within the lambda.")>
        Private Function SplitSequenceByDataSize(Of TValue)(ByVal sequence As IEnumerable(Of TValue),
                                                            ByVal measure As Func(Of TValue, Int32),
                                                            ByVal maxDataSize As Int32) As IRist(Of IRist(Of TValue))
            Contract.Requires(sequence IsNot Nothing)
            Contract.Requires(maxDataSize > 0)
            Contract.Requires(measure IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of IRist(Of TValue)))() IsNot Nothing)

            Dim result = sequence.ZipWithPartialAggregates(
                    seed:=New With {.sequenceDataCount = 0,
                                    .sequenceIndex = 0},
                    func:=Function(acc, e)
                              Dim itemDataCount = measure(e)
                              If itemDataCount > maxDataSize Then
                                  Throw New ArgumentException("Unable to fit an item within the max data size.", "maxDataSize")
                              ElseIf acc.sequenceDataCount + itemDataCount > maxDataSize Then
                                  Return New With {.sequenceDataCount = itemDataCount,
                                                   .sequenceIndex = acc.sequenceIndex + 1}
                              Else
                                  Return New With {.sequenceDataCount = acc.sequenceDataCount + itemDataCount,
                                                   .sequenceIndex = acc.sequenceIndex}
                              End If
                          End Function
                ).GroupBy(keySelector:=Function(e) e.Item2.sequenceIndex,
                          elementSelector:=Function(e) e.Item1)

            Return (From subSequence In result
                    Select subSequence.ToRist
                    ).ToRist
        End Function
        Private Sub SendQueuedGameData(ByVal record As TickRecord)
            Contract.Requires(record IsNot Nothing)
            'Include all the data we can fit in a packet
            Dim totalDataLength = 0

            '[20 includes headers and a small safety margin]
            Dim jar = New Protocol.PlayerActionSetJar()
            Dim actualActions = SplitSequenceByDataSize(_actionsForNextTick,
                                                        Function(e) jar.Pack(e).Count,
                                                        PacketSocket.DefaultBufferSize - 20)
            _actionsForNextTick.Clear()

            'Adjust actions so they always appear to come from visible players
            Dim visibleActions = (From subSequence In actualActions
                                  Select (From actionSet In subSequence
                                          Let id = _lobby.GetVisiblePlayer(actionSet.Player).Id
                                          Select New Protocol.PlayerActionSet(id, actionSet.Actions)
                                          ).ToRist
                                  ).ToRist

            For Each player In _kernel.Players
                Contract.Assume(player IsNot Nothing)
                player.QueueSendTick(record, visibleActions)
            Next player

            RaiseEvent Tick(Me, record.length, actualActions, visibleActions)
        End Sub

        Public Function QueueGetTickPeriod() As Task(Of TimeSpan)
            Contract.Ensures(Contract.Result(Of Task(Of TimeSpan))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() _tickPeriod)
        End Function
        Public Function QueueGetSpeedFactor() As Task(Of Double)
            Contract.Ensures(Contract.Result(Of Task(Of Double))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() _speedFactor)
        End Function
        Public Function QueueGetLagLimit() As Task(Of TimeSpan)
            Contract.Ensures(Contract.Result(Of Task(Of TimeSpan))() IsNot Nothing)
            Return _kernel.InQueue.QueueFunc(Function() _lagLimit)
        End Function

        Public Function QueueSetTickPeriod(ByVal value As TimeSpan) As Task
            Contract.Requires(value.Ticks > 0)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _tickPeriod = value)
        End Function
        Public Function QueueSetSpeedFactor(ByVal value As Double) As Task
            Contract.Requires(value > 0)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _speedFactor = value)
        End Function
        Public Function QueueSetLagLimit(ByVal value As TimeSpan) As Task
            Contract.Requires(value.Ticks >= 0)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return _kernel.InQueue.QueueAction(Sub() _lagLimit = value)
        End Function
    End Class
End Namespace
