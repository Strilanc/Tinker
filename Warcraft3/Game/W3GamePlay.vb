Namespace Warcraft3
    Partial Class W3Game
        Private NotInheritable Class GameTickDatum
            Private ReadOnly _source As W3Player
            Private ReadOnly _data As Byte()
            Public ReadOnly Property Source As W3Player
                Get
                    Contract.Ensures(Contract.Result(Of W3Player)() IsNot Nothing)
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

            Public Sub New(ByVal source As W3Player, ByVal data As Byte())
                Contract.Requires(source IsNot Nothing)
                Contract.Requires(data IsNot Nothing)
                Me._source = source
                Me._data = data
            End Sub
        End Class

        Private ReadOnly tickTimer As New Timers.Timer(My.Settings.game_tick_period)
        Private lastTickTime As ModInt32
        Private laggingPlayers As New List(Of W3Player)
        Private lagStartTime As ModInt32
        Private gameDataQueue As New Queue(Of GameTickDatum)
        Private _gameTime As Integer
        Private gameTimeBuffer As Double
        Public Property SettingSpeedFactor As Double
        Public Property SettingTickPeriod As Double
        Public Property SettingLagLimit As Double

        Public Event PlayerSentData(ByVal game As W3Game, ByVal player As W3Player, ByVal data As Byte())

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
            AddHandler tickTimer.Elapsed, Sub() OnTick()
        End Sub
        Private Sub GameplayStart()
            For Each player In players
                Contract.Assume(player IsNot Nothing)
                player.QueueStartPlaying()
            Next player
            Me.lastTickTime = Environment.TickCount
            Me.tickTimer.Start()
        End Sub
        Private Sub GameplayStop()
            tickTimer.Stop()
        End Sub

        Private Sub e_ThrowPlayerSentData(ByVal player As W3Player, ByVal data As Byte())
            eventRef.QueueAction(Sub() RaiseEvent PlayerSentData(Me, player, data))
        End Sub

#Region "Play"
        '''<summary>Adds data to broadcast to all clients in the next tick</summary>
        Private Sub QueueGameData(ByVal sender As W3Player, ByVal data() As Byte)
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            e_ThrowPlayerSentData(sender, data)
            gameDataQueue.Enqueue(New GameTickDatum(sender, data))
        End Sub
        Public Function QueueReceiveGameAction(ByVal player As W3Player,
                                               ByVal action As W3GameAction) As ifuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(action IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return eventRef.QueueAction(Sub() RaiseEvent PlayerAction(Me, player, action))
        End Function

        '''<summary>Drops the players currently lagging.</summary>
        Private Sub DropLagger()
            For Each player In laggingPlayers
                Contract.Assume(player IsNot Nothing)
                RemovePlayer(player, True, W3PlayerLeaveType.Disconnect, "Lagger dropped")
            Next player
        End Sub

        '''<summary>Advances game time</summary>
        Private Sub OnTick()
            ref.QueueAction(
                Sub()
                    Dim t As ModInt32 = Environment.TickCount
                    Dim dt = CUInt(t - lastTickTime) * Me.SettingSpeedFactor
                    Dim dgt = CUShort(Me.SettingTickPeriod * Me.SettingSpeedFactor)
                    lastTickTime = t

                    'Stop for laggers
                    UpdateLagScreen()
                    If laggingPlayers.Count > 0 Then
                        Return
                    End If

                    'Throttle tick rate
                    gameTimeBuffer += dt - dgt
                    gameTimeBuffer = gameTimeBuffer.Between(-dgt * 10, dgt * 10)
                    tickTimer.Interval = (dgt - gameTimeBuffer).Between(dgt / 2, dgt * 2)

                    'Send
                    SendQueuedGameData(New TickRecord(dgt, _gameTime))
                    _gameTime += dgt
                End Sub
            )
        End Sub
        Private Sub UpdateLagScreen()
            If laggingPlayers.Count > 0 Then
                For Each p In laggingPlayers.ToList
                    Contract.Assume(p IsNot Nothing)
                    If Not players.Contains(p) Then
                        laggingPlayers.Remove(p)
                    ElseIf p.GetTockTime >= _gameTime OrElse p.isFake Then
                        laggingPlayers.Remove(p)
                        Dim p_ = p
                        If IsPlayerVisible(p) OrElse (From q In laggingPlayers
                                                      Where GetVisiblePlayer(q) Is GetVisiblePlayer(p_)).None Then
                            BroadcastPacket(W3Packet.MakeRemovePlayerFromLagScreen(
                                player:=GetVisiblePlayer(p),
                                lagTimeInMilliseconds:=CUInt(lastTickTime - lagStartTime)))
                        End If
                    End If
                Next p
            Else
                laggingPlayers = (From p In players
                                  Where Not p.isFake _
                                  AndAlso p.GetTockTime < _gameTime - Me.SettingLagLimit
                                  ).ToList
                If laggingPlayers.Count > 0 Then
                    BroadcastPacket(W3Packet.MakeShowLagScreen(laggingPlayers), Nothing)
                    lagStartTime = lastTickTime
                End If
            End If
        End Sub
        Private Sub SendQueuedGameData(ByVal record As TickRecord)
            Contract.Requires(record IsNot Nothing)
            'Include all the data we can fit in a packet
            Dim dataLength = 0
            Dim dataList = New List(Of Byte())(gameDataQueue.Count)
            Dim outgoingData = New List(Of GameTickDatum)(gameDataQueue.Count)
            While gameDataQueue.Count > 0 _
                  AndAlso dataLength + gameDataQueue.Peek().Data.Length < PacketSocket.DefaultBufferSize - 20 '[20 includes headers and a small safety margin]
                Dim e = gameDataQueue.Dequeue()
                Contract.Assume(e IsNot Nothing)
                outgoingData.Add(e)

                'append client data to broadcast game data
                Dim data = Concat({GetVisiblePlayer(e.Source).Index},
                                  CUShort(e.Data.Length).Bytes,
                                  e.Data)
                dataList.Add(data)
                dataLength += data.Length
            End While

            'Send data
            Dim normalData = Concat(dataList)
            For Each receiver In players
                Contract.Assume(receiver IsNot Nothing)
                If IsPlayerVisible(receiver) Then
                    receiver.QueueSendTick(record, normalData)
                Else
                    receiver.QueueSendTick(record, CreateTickDataForInvisiblePlayer(receiver, outgoingData))
                End If
            Next receiver
        End Sub
        <Pure()>
        Private Function CreateTickDataForInvisiblePlayer(ByVal receiver As W3Player,
                                                          ByVal data As IEnumerable(Of GameTickDatum)) As Byte()
            Contract.Requires(receiver IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
            Return Concat((From e In data Select Concat({If(e.Source Is receiver, receiver, GetVisiblePlayer(e.Source)).Index},
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
            Return ref.QueueAction(AddressOf DropLagger)
        End Function
        Public Function QueueSendGameData(ByVal sender As W3Player,
                                          ByVal data() As Byte) As IFuture
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Ifuture)() IsNot Nothing)
            Return ref.QueueAction(Sub()
                                       Contract.Assume(sender IsNot Nothing)
                                       Contract.Assume(data IsNot Nothing)
                                       QueueGameData(sender, data)
                                   End Sub)
        End Function
#End Region
    End Class
End Namespace
