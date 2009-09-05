Namespace Warcraft3
    Partial Class W3Game
        Implements IW3Game
        Private Class GameTickDatum
            Private ReadOnly _source As W3Player
            Private ReadOnly _data As Byte()
            <ContractInvariantMethod()> Protected Sub Invariant()
                Contract.Invariant(_source IsNot Nothing)
                Contract.Invariant(_data IsNot Nothing)
            End Sub
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
        Private gameTime As Integer
        Private gameTimeBuffer As Double
        Private Property settingSpeedFactor As Double Implements IW3Game.SettingGameRate
        Private Property settingTickPeriod As Double Implements IW3Game.SettingTickPeriod
        Private Property settingLagLimit As Double Implements IW3Game.SettingLagLimit

        Private Event PlayerSentData(ByVal game As IW3Game, ByVal player As W3Player, ByVal data As Byte()) Implements IW3Game.PlayerSentData

        Private Sub GamePlayNew()
            Dim gsf As Double = My.Settings.game_speed_factor
            Dim gtp As Double = My.Settings.game_tick_period
            Dim gll As Double = My.Settings.game_lag_limit
            Contract.Assume(gsf > 0 AndAlso Not Double.IsNaN(gsf) AndAlso Not Double.IsInfinity(gsf))
            Contract.Assume(gtp > 0 AndAlso Not Double.IsNaN(gtp) AndAlso Not Double.IsInfinity(gtp))
            Contract.Assume(gll >= 0)
            Contract.Assume(Not Double.IsNaN(gll))
            Contract.Assume(Not Double.IsInfinity(gll))
            settingSpeedFactor = gsf
            settingTickPeriod = gtp
            settingLagLimit = gll
            AddHandler tickTimer.Elapsed, Sub() c_Tick()
        End Sub
        Private Sub GameplayStart()
            For Each player In players
                player.QueueStartPlaying()
            Next player
            Me.lastTickTime = Environment.TickCount
            Me.tickTimer.Start()
        End Sub
        Private Sub GameplayStop()
            For Each player In players
                player.QueueStopPlaying()
            Next player
            tickTimer.Stop()
        End Sub

        Private Sub e_ThrowPlayerSentData(ByVal player As W3Player, ByVal data As Byte())
            eventRef.QueueAction(Sub()
                                     RaiseEvent PlayerSentData(Me, player, data)
                                 End Sub)
        End Sub

#Region "Play"
        '''<summary>Adds data to broadcast to all clients in the next tick</summary>
        Private Sub QueueGameData(ByVal sender As W3Player, ByVal data() As Byte)
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            e_ThrowPlayerSentData(sender, data)
            gameDataQueue.Enqueue(New GameTickDatum(sender, data))
        End Sub

        '''<summary>Drops the players currently lagging.</summary>
        Private Sub DropLagger()
            For Each player In laggingPlayers
                Contract.Assume(player IsNot Nothing)
                RemovePlayer(player, True, W3PlayerLeaveTypes.Disconnect, "Lagger dropped")
            Next player
        End Sub

        '''<summary>Advances game time</summary>
        Private Sub c_Tick()
            ref.QueueAction(
                Sub()
                    Dim t As ModInt32 = Environment.TickCount
                    Dim dt = CUInt(t - lastTickTime) * Me.settingSpeedFactor
                    Dim dgt = CUShort(Me.settingTickPeriod * Me.settingSpeedFactor)
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
                    SendQueuedGameData(New TickRecord(dgt, gameTime))
                    gameTime += dgt
                End Sub
            )
        End Sub
        Private Sub UpdateLagScreen()
            If laggingPlayers.Count > 0 Then
                For Each p In laggingPlayers.ToList()
                    If Not players.Contains(p) Then
                        laggingPlayers.Remove(p)
                    ElseIf p.GetTockTime >= gameTime OrElse p.isFake Then
                        laggingPlayers.Remove(p)
                        Contract.Assume(p IsNot Nothing)
                        Dim p_ = p
                        If IsPlayerVisible(p) OrElse (From q In laggingPlayers _
                                                      Where GetVisiblePlayer(q) Is GetVisiblePlayer(p_)).None Then
                            Contract.Assume(p IsNot Nothing)
                            BroadcastPacket(
                                    W3Packet.MakeRemovePlayerFromLagScreen(
                                            GetVisiblePlayer(p),
                                            CUInt(lastTickTime - lagStartTime)))
                        End If
                    End If
                Next p
            Else
                laggingPlayers = (From p In players
                                   Where Not p.isFake _
                                   AndAlso p.GetTockTime < gameTime - Me.settingLagLimit).ToList()
                If laggingPlayers.Count > 0 Then
                    BroadcastPacket(W3Packet.MakeShowLagScreen(laggingPlayers), Nothing)
                    lagStartTime = lastTickTime
                End If
            End If
        End Sub
        Private Sub SendQueuedGameData(ByVal record As TickRecord)
            'Include all the data we can fit in a packet
            Dim dataLength = 0
            Dim dataList = New List(Of Byte())(gameDataQueue.Count)
            Dim outgoingData = New List(Of GameTickDatum)(gameDataQueue.Count)
            While gameDataQueue.Count > 0 _
                  AndAlso dataLength + gameDataQueue.Peek().Data.Length < PacketSocket.DefaultBufferSize - 20 '[20 includes headers and a small safety margin]
                Dim e = gameDataQueue.Dequeue()
                outgoingData.Add(e)

                'append client data to broadcast game data
                Dim data = Concat({GetVisiblePlayer(e.Source).index},
                                  CUShort(e.Data.Length).Bytes(),
                                  e.Data)
                dataList.Add(Data)
                dataLength += Data.Length
            End While

            'Send data
            Contract.Assume(dataList IsNot Nothing) 'remove this once verifier properly understands how List.Add works
            Contract.Assume(outgoingData IsNot Nothing) 'remove this once verifier properly understands how List.Add works
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
            Dim receiver_ = receiver
            Dim data_ = data
            Return Concat((From e In data_
                           Select Concat({If(e.Source Is receiver_, receiver_, GetVisiblePlayer(e.Source)).index},
                                         CUShort(e.Data.Length).Bytes(),
                                         e.Data)))
        End Function
#End Region

#Region "Interface"
        Private ReadOnly Property _game_time() As Integer Implements IW3Game.GameTime
            Get
                Return gameTime
            End Get
        End Property
        Private Function _f_DropLagger() As IFuture Implements IW3Game.QueueDropLagger
            Return ref.QueueAction(AddressOf DropLagger)
        End Function
        Private Function _f_QueueGameData(ByVal sender As W3Player, ByVal data() As Byte) As IFuture Implements IW3Game.QueueSendGameData
            Return ref.QueueAction(Sub()
                                       Contract.Assume(sender IsNot Nothing)
                                       Contract.Assume(data IsNot Nothing)
                                       QueueGameData(sender, data)
                                   End Sub)
        End Function
#End Region
    End Class
End Namespace
