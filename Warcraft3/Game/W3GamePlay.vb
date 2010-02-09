Namespace WC3
    Partial Class Game
        Private _gameTickTimer As RelativeClock
        Private laggingPlayers As New List(Of Player)
        Private _lagTimer As RelativeClock 'nullable
        Private ReadOnly gameDataQueue As New Queue(Of Tuple(Of Player, IReadableList(Of Protocol.GameAction)))
        Private _gameTime As Integer
        Private gameTimeBuffer As Double
        Public Property SettingSpeedFactor As Double
        Public Property SettingTickPeriod As Double
        Public Property SettingLagLimit As Double

        Public Event ReceivedPlayerActions(ByVal sender As Game, ByVal player As Player, ByVal actions As IReadableList(Of Protocol.GameAction))
        Public Event Tick(ByVal sender As Game, ByVal timeSpan As UShort, ByVal actionSets As IReadableList(Of Tuple(Of Player, Protocol.PlayerActionSet)))

        Private Sub GamePlayNew()
            Dim gsf As Double = My.Settings.game_speed_factor
            Dim gtp As Double = My.Settings.game_tick_period
            Dim gll As Double = My.Settings.game_lag_limit
            Contract.Assume(gsf > 0 AndAlso gsf.IsFinite)
            Contract.Assume(gtp > 0 AndAlso gtp.IsFinite)
            Contract.Assume(gll >= 0 AndAlso gll.IsFinite)
            SettingSpeedFactor = gsf
            SettingTickPeriod = gtp
            SettingLagLimit = gll
        End Sub
        Private Sub GameplayStart()
            For Each player In _players
                Contract.Assume(player IsNot Nothing)
                player.QueueStartPlaying()
            Next player
            _gameTickTimer = _clock.Restarted()
            inQueue.QueueAction(AddressOf OnTick)
        End Sub
        Private Sub GameplayStop()
            _gameTickTimer = Nothing
        End Sub


        Private _asyncWaitTriggered As Boolean
        Private Sub OnReceiveGameActions(ByVal sender As Player, ByVal actions As IReadableList(Of Protocol.GameAction))
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(actions IsNot Nothing)
            gameDataQueue.Enqueue(Tuple(sender, actions))
            outQueue.QueueAction(Sub() RaiseEvent ReceivedPlayerActions(Me, sender, actions))

            '[async lag -wait command detection]
            If (From action In actions Where action.id = Protocol.GameActionId.GameCacheSyncInteger
                                       Select vals = CType(action.Payload.Value, Dictionary(Of InvariantString, Object))
                                       Where CStr(vals("filename")) = "HostBot.AsyncLag" AndAlso CStr(vals("mission key")) = "wait").Any Then
                _asyncWaitTriggered = True
            End If
        End Sub

        '''<summary>Drops the players currently lagging.</summary>
        Private Sub DropLagger()
            For Each player In laggingPlayers
                Contract.Assume(player IsNot Nothing)
                RemovePlayer(player, True, Protocol.PlayerLeaveType.Disconnect, "Lagger dropped")
            Next player
        End Sub

        '''<summary>Advances game time</summary>
        Private Sub OnTick()
            If _gameTickTimer Is Nothing Then Return 'ended

            _gameTickTimer = _gameTickTimer.Restarted()
            Dim dt = _gameTickTimer.StartingTimeOnParentClock.TotalMilliseconds * Me.SettingSpeedFactor
            Dim dgt = CUShort(Me.SettingTickPeriod * Me.SettingSpeedFactor)

            'Stop for laggers
            UpdateLagScreen()
            If laggingPlayers.Count > 0 Then
                _clock.AsyncWait(CUInt(Me.SettingTickPeriod).Milliseconds).QueueCallOnSuccess(inQueue, AddressOf OnTick)
                Return
            End If

            'Schedule next tick
            gameTimeBuffer += dt - dgt
            gameTimeBuffer = gameTimeBuffer.Between(-dgt * 10, dgt * 10)
            Dim nextTickTime = CLng(dgt - gameTimeBuffer).Between(dgt \ 2US, dgt * 2US).Milliseconds
            _clock.AsyncWait(nextTickTime).QueueCallOnSuccess(inQueue, AddressOf OnTick)

            'Send
            SendQueuedGameData(New TickRecord(dgt, _gameTime))
            _gameTime += dgt
        End Sub
        Private Sub UpdateLagScreen()
            If laggingPlayers.Count > 0 Then
                For Each p In laggingPlayers.ToList
                    Contract.Assume(p IsNot Nothing)
                    If Not _players.Contains(p) Then
                        laggingPlayers.Remove(p)
                    ElseIf p.GetTockTime >= _gameTime OrElse p.isFake Then

                        laggingPlayers.Remove(p)
                        Dim p_ = p
                        If IsPlayerVisible(p) OrElse (From q In laggingPlayers
                                                      Where GetVisiblePlayer(q) Is GetVisiblePlayer(p_)).None Then
                            Contract.Assume(_lagTimer IsNot Nothing)
                            BroadcastPacket(Protocol.MakeRemovePlayerFromLagScreen(
                                pid:=GetVisiblePlayer(p).PID,
                                lagTimeInMilliseconds:=CUInt(_lagTimer.ElapsedTime.TotalMilliseconds)))
                        End If
                    End If
                Next p
            Else
                laggingPlayers = (From p In _players
                                  Where Not p.isFake _
                                  AndAlso p.GetTockTime < _gameTime - If(_asyncWaitTriggered, 0, Me.SettingLagLimit)
                                  ).ToList
                _asyncWaitTriggered = False
                If laggingPlayers.Count > 0 Then
                    BroadcastPacket(Protocol.MakeShowLagScreen(From p In laggingPlayers Select p.PID), Nothing)
                    _lagTimer = _clock.Restarted()
                End If
            End If
        End Sub
        Private Sub SendQueuedGameData(ByVal record As TickRecord)
            Contract.Requires(record IsNot Nothing)
            'Include all the data we can fit in a packet
            Dim totalDataLength = 0
            Dim outgoingActions = New List(Of Tuple(Of Player, Protocol.PlayerActionSet))(capacity:=gameDataQueue.Count)
            While gameDataQueue.Count > 0
                'peek
                Dim e = gameDataQueue.Peek()
                Contract.Assume(e IsNot Nothing)
                Contract.Assume(e.Item1 IsNot Nothing)
                Contract.Assume(e.Item2 IsNot Nothing)
                Dim actionDataLength = (From action In e.Item2 Select action.Payload.Data.Count).Aggregate(Function(e1, e2) e1 + e2)
                If totalDataLength + actionDataLength >= PacketSocket.DefaultBufferSize - 20 Then '[20 includes headers and a small safety margin]
                    Exit While
                End If

                gameDataQueue.Dequeue()
                outgoingActions.Add(Tuple(e.Item1, New Protocol.PlayerActionSet(GetVisiblePlayer(e.Item1).PID, e.Item2)))
                totalDataLength += actionDataLength
            End While

            'Send data
            For Each player In _players
                Contract.Assume(player IsNot Nothing)
                If IsPlayerVisible(player) Then
                    player.QueueSendTick(record, (From e In outgoingActions Select e.Item2).ToArray.AsReadableList)
                Else
                    Dim player_ = player
                    player.QueueSendTick(record, (From e In outgoingActions
                                                  Let pid = If(e.Item1 Is player_, player_, GetVisiblePlayer(e.Item1)).PID
                                                  Select New Protocol.PlayerActionSet(pid, e.Item2.Actions)
                                                  ).ToList.AsReadableList)
                End If
            Next player

            outQueue.QueueAction(Sub() RaiseEvent Tick(Me, record.length, outgoingActions.AsReadableList))
        End Sub

        Public ReadOnly Property GameTime() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Return _gameTime
            End Get
        End Property
        Public Function QueueDropLagger() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return inQueue.QueueAction(AddressOf DropLagger)
        End Function
    End Class
End Namespace
