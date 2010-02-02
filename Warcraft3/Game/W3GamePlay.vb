Namespace WC3
    Partial Class Game
        Private NotInheritable Class GameTickDatum
            Private ReadOnly _source As Player
            Private ReadOnly _data As Byte()
            Public ReadOnly Property Source As Player
                Get
                    Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)
                    Return _source
                End Get
            End Property
            Public ReadOnly Property Data As Byte()
                Get
                    Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
                    Return _data
                End Get
            End Property

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_source IsNot Nothing)
                Contract.Invariant(_data IsNot Nothing)
            End Sub

            Public Sub New(ByVal source As Player, ByVal data As Byte())
                Contract.Requires(source IsNot Nothing)
                Contract.Requires(data IsNot Nothing)
                Me._source = source
                Me._data = data
            End Sub
        End Class

        Private _gameTickTimer As RelativeClock
        Private laggingPlayers As New List(Of Player)
        Private _lagTimer As RelativeClock 'nullable
        Private gameDataQueue As New Queue(Of GameTickDatum)
        Private _gameTime As Integer
        Private gameTimeBuffer As Double
        Public Property SettingSpeedFactor As Double
        Public Property SettingTickPeriod As Double
        Public Property SettingLagLimit As Double

        Public Event PlayerSentData(ByVal game As Game, ByVal player As Player, ByVal data As Byte())

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
            _gameTickTimer = _clock.AfterReset()
            inQueue.QueueAction(AddressOf OnTick)
        End Sub
        Private Sub GameplayStop()
            _gameTickTimer = Nothing
        End Sub


#Region "Play"
        '''<summary>Adds data to broadcast to all clients in the next tick</summary>
        Private Sub QueueGameData(ByVal sender As Player, ByVal data() As Byte)
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            outQueue.QueueAction(Sub() RaiseEvent PlayerSentData(Me, sender, data))
            inQueue.QueueAction(Sub() gameDataQueue.Enqueue(New GameTickDatum(sender, data)))
        End Sub
        Private _asyncWaitTriggered As Boolean
        Public Function QueueReceiveGameAction(ByVal player As Player,
                                               ByVal action As Protocol.GameAction) As ifuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(action IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            inQueue.QueueAction(
                Sub()
                    If action.id = Protocol.GameActionId.GameCacheSyncInteger Then
                        Dim vals = CType(action.Payload.Value, Dictionary(Of InvariantString, Object))
                        If CStr(vals("filename")) = "HostBot.AsyncLag" AndAlso CStr(vals("mission key")) = "wait" Then
                            _asyncWaitTriggered = True
                        End If
                    End If
                End Sub)
            Return outQueue.QueueAction(Sub() RaiseEvent PlayerAction(Me, player, action))
        End Function

        '''<summary>Drops the players currently lagging.</summary>
        Private Sub DropLagger()
            For Each player In laggingPlayers
                Contract.Assume(player IsNot Nothing)
                RemovePlayer(player, True, PlayerLeaveType.Disconnect, "Lagger dropped")
            Next player
        End Sub

        '''<summary>Advances game time</summary>
        Private Sub OnTick()
            If _gameTickTimer Is Nothing Then Return 'ended

            _gameTickTimer = _gameTickTimer.AfterReset()
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
                    _lagTimer = _clock.AfterReset()
                End If
            End If
        End Sub
        Private Sub SendQueuedGameData(ByVal record As TickRecord)
            Contract.Requires(record IsNot Nothing)
            'Include all the data we can fit in a packet
            Dim dataLength = 0
            Dim dataList = New List(Of Byte())(capacity:=gameDataQueue.Count)
            Dim outgoingData = New List(Of GameTickDatum)(capacity:=gameDataQueue.Count)
            While gameDataQueue.Count > 0
                Dim e = gameDataQueue.Peek()
                Contract.Assume(e IsNot Nothing)
                If dataLength + e.Data.Length >= PacketSocket.DefaultBufferSize - 20 Then '[20 includes headers and a small safety margin]
                    Exit While
                End If

                gameDataQueue.Dequeue()
                outgoingData.Add(e)

                'append client data to broadcast game data
                Dim data = Concat({GetVisiblePlayer(e.Source).PID.Index},
                                   CUShort(e.Data.Length).Bytes,
                                   e.Data)
                dataList.Add(data)
                dataLength += data.Length
            End While

            'Send data
            Dim normalData = Concat(dataList)
            For Each receiver In _players
                Contract.Assume(receiver IsNot Nothing)
                If IsPlayerVisible(receiver) Then
                    receiver.QueueSendTick(record, normalData)
                Else
                    receiver.QueueSendTick(record, CreateTickDataForInvisiblePlayer(receiver, outgoingData))
                End If
            Next receiver
        End Sub
        <Pure()>
        Private Function CreateTickDataForInvisiblePlayer(ByVal receiver As Player,
                                                          ByVal data As IEnumerable(Of GameTickDatum)) As Byte()
            Contract.Requires(receiver IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
            Return Concat((From e In data Select Concat({If(e.Source Is receiver, receiver, GetVisiblePlayer(e.Source)).PID.Index},
                                                        CUShort(e.Data.Length).Bytes,
                                                        e.Data)))
        End Function
#End Region

#Region "Interface"
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
        Public Function QueueSendGameData(ByVal sender As Player,
                                          ByVal data() As Byte) As IFuture
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub()
                                           Contract.Assume(sender IsNot Nothing)
                                           Contract.Assume(data IsNot Nothing)
                                           QueueGameData(sender, data)
                                       End Sub)
        End Function
#End Region
    End Class
End Namespace
