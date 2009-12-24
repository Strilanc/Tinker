Imports Tinker.Links
Imports Tinker.Pickling

Namespace WC3
    Public NotInheritable Class GameServer
        Inherits FutureDisposable

        Private Shared ReadOnly InitialConnectionTimeout As TimeSpan = 5.Seconds

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue

        Private ReadOnly _logger As Logger
        Private ReadOnly _gameSets As New Dictionary(Of UInt32, GameSet)()
        Private ReadOnly _viewGameSets As New AsyncViewableCollection(Of GameSet)(outQueue:=outQueue)
        Private ReadOnly _viewGames As New AsyncViewableCollection(Of Tuple(Of GameSet, Game))(outQueue:=outQueue)
        Private ReadOnly _viewPlayers As New AsyncViewableCollection(Of Tuple(Of GameSet, Game, Player))(outQueue:=outQueue)

        Public Event PlayerTalked(ByVal sender As GameServer, ByVal game As Game, ByVal player As Player, ByVal text As String)
        Public Event PlayerLeft(ByVal sender As GameServer, ByVal game As Game, ByVal gameState As GameState, ByVal player As Player, ByVal leaveType As PlayerLeaveType, ByVal reason As String)
        Public Event PlayerSentData(ByVal sever As GameServer, ByVal game As Game, ByVal player As Player, ByVal data As Byte())
        Public Event PlayerEntered(ByVal sender As GameServer, ByVal game As Game, ByVal player As Player)

        Private instanceCreationCount As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_viewGames IsNot Nothing)
            Contract.Invariant(_viewGameSets IsNot Nothing)
            Contract.Invariant(_gameSets IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
        End Sub

        Public Sub New(Optional ByVal logger As Logger = Nothing)
            Me._logger = If(logger, New Logger)

            'For i = 1 To Me.Settings.instances
            'CreateGame()
            'Next i
            'Me.Parent.logger.Log("Server started for map {0}.".Frmt(Me.Settings.Map.RelativePath), LogMessageType.Positive)

            'If Me.Settings.grabMap Then
            'Dim serverPort = Me.Settings.defaultListenPorts.FirstOrDefault
            'If serverPort = 0 Then
            'Throw New InvalidOperationException("Server has no port for Grab player to connect on.")
            'End If

            'Dim grabPort = parent.PortPool.TryAcquireAnyPort()
            'If grabPort Is Nothing Then
            'Throw New InvalidOperationException("Failed to get port from pool for Grab player to listen on.")
            'End If

            'FutureWait(3.Seconds).CallWhenReady(
            'Sub()
            'Dim p = New W3DummyPlayer("Grab", grabPort, logger)
            'p.QueueConnect("localhost", serverPort)
            'End Sub
            ')
            'End If

            'If Me.Settings.testFakePlayers AndAlso Me.Settings.defaultListenPorts.Any Then
            'FutureWait(3.Seconds).CallWhenReady(
            'Sub()
            'For i = 1 To 3
            'Dim receivedPort = Me.Parent.PortPool.TryAcquireAnyPort()
            'If receivedPort Is Nothing Then
            'logger.Log("Failed to get port for fake player.", LogMessageType.Negative)
            'Exit For
            'End If

            'Dim p = New W3DummyPlayer("Wait {0}min".Frmt(i), receivedPort, logger, DummyPlayerMode.EnterGame)
            'p.readyDelay = i.Minutes
            'Dim i_ = i
            'p.QueueConnect("localhost", Me.Settings.defaultListenPorts.FirstOrDefault).CallWhenReady(
            'Sub(exception)
            'If exception Is Nothing Then
            'Me.logger.Log("Fake player {0} Connected", LogMessageType.Positive)
            'Else
            'Me.logger.Log("Fake player {0}: {1}".Frmt(i_, exception.Message), LogMessageType.Negative)
            'End If
            'End Sub)
            'Next i
            'End Sub)
            'End If

            'If Me.Settings.grabMap Then
            'Dim server_port = Me.Settings.defaultListenPorts.FirstOrDefault
            'If server_port = 0 Then
            'Throw New InvalidOperationException("Server has no port for Grab player to connect on.")
            'End If

            'Dim grabPort = Me.Parent.PortPool.TryAcquireAnyPort()
            'If grabPort Is Nothing Then
            'Throw New InvalidOperationException("Failed to get port from pool for Grab player to listen on.")
            'End If

            'FutureWait(3.Seconds).CallWhenReady(
            'Sub()
            'Dim p = New W3DummyPlayer("Grab", grabPort, logger)
            'p.QueueConnect("localhost", server_port)
            'End Sub)
            'End If
        End Sub

        Public ReadOnly Property Logger As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property

#Region "Events"
        'Private Sub e_ThrowStateChanged(ByVal old_state As ServerState, ByVal new_state As ServerState)
        'outQueue.QueueAction(
        'Sub()
        'RaiseEvent ChangedState(Me, old_state, new_state)
        'End Sub
        ')
        'End Sub
        'Private Sub e_ThrowAddedGame(ByVal game As Game)
        'outQueue.QueueAction(
        'Sub()
        'RaiseEvent AddedGame(Me, game)
        'End Sub
        ')
        'End Sub
        'Private Sub e_ThrowRemovedGame(ByVal game As Game)
        'outQueue.QueueAction(
        'Sub()
        'RaiseEvent RemovedGame(Me, game)
        'End Sub
        ')
        'End Sub

        'Private Sub c_PlayerTalked(ByVal game As Game, ByVal player As Player, ByVal text As String)
        'RaiseEvent PlayerTalked(Me, game, player, text)
        'End Sub
        'Private Sub c_PlayerSentData(ByVal game As Game, ByVal player As Player, ByVal data As Byte())
        'RaiseEvent PlayerSentData(Me, game, player, data)
        'End Sub
        'Private Sub c_PlayerLeft(ByVal game As Game, ByVal game_state As GameState, ByVal player As Player, ByVal leaveType As PlayerLeaveType, ByVal reason As String)
        'logger.Log("{0} left game {1}. ({2})".Frmt(player.Name, game.Name, reason), LogMessageType.Negative)
        'RaiseEvent PlayerLeft(Me, game, game_state, player, leaveType, reason)
        'End Sub
        'Private Sub c_PlayerEntered(ByVal game As Game, ByVal player As Player)
        'RaiseEvent PlayerEntered(Me, game, player)
        'End Sub
        'Private Sub c_GameStateChanged(ByVal sender As Game, ByVal old_state As GameState, ByVal new_state As GameState)
        'inQueue.QueueAction(
        'Sub()
        ''If Not games_all.Contains(sender) Then Return

        'Select Case new_state
        'Case GameState.Loading
        'Logger.Log(sender.Name + " has begun loading.", LogMessageType.Positive)
        '' games_lobby.Remove(sender)
        '' games_load_screen.Add(sender)
        'Case GameState.Playing
        'Logger.Log(sender.Name + " has started play.", LogMessageType.Positive)
        '' games_load_screen.Remove(sender)
        '' games_gameplay.Add(sender)
        'Case GameState.Closed
        'Logger.Log(sender.Name + " has closed.", LogMessageType.Negative)
        'RemoveGame(sender.Name)
        'End Select

        ''Advance from only_accepting if there is a game started
        ''If state = ServerState.OnlyAcceptingPlayers Then
        ''If games_all.Count > games_lobby.Count Then
        ''change_state(ServerState.AcceptingPlayersAndPlayingGames)
        ''End If
        ''End If

        ''Advance from accepting_and_playing if there are no more games accepting players
        ''If state = ServerState.AcceptingPlayersAndPlayingGames AndAlso Settings.instances > 0 Then
        ''If games_lobby.Count = 0 Then
        ''If Settings.permanent Then
        ''SetAdvertiserOptions(True)
        ''Else
        ''StopAcceptingPlayers()
        ''End If
        ''End If
        ''End If

        ''Advance from only_playing_out if there are no more games being played
        ''If state = ServerState.OnlyPlayingGames Then
        ''If games_all.Count = 0 Then
        ''Kill()
        ''End If
        ''End If
        'End Sub
        ')
        'End Sub
#End Region

        'Dim socket = New W3Socket(New PacketSocket(stream:=TcpClient.GetStream,
        'localendpoint:=CType(TcpClient.Client.LocalEndPoint, Net.IPEndPoint),
        'remoteendpoint:=CType(TcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
        'timeout:=60.Seconds,
        'Logger:=_logger))

        '''<summary>Handles new connections to the server.</summary>
        Private Sub AcceptSocket(ByVal socket As W3Socket)
            Contract.Requires(socket IsNot Nothing)

            _logger.Log("Connection from {0}.".Frmt(socket.Name), LogMessageType.Positive)
            Dim socketHandled = New OnetimeLock()

            'Setup initial timeout
            InitialConnectionTimeout.AsyncWait().CallWhenReady(
                Sub()
                    If Not socketHandled.TryAcquire Then Return
                    socket.Disconnect(expected:=True, reason:="Timeout")
                End Sub)

            'Try to read Knock packet
            socket.AsyncReadPacket().CallOnValueSuccess(
                Sub(data)
                    If Not socketHandled.TryAcquire Then Return
                    If data.Count < 4 OrElse data(0) <> Packet.PacketPrefixValue OrElse data(1) <> PacketId.Knock Then
                        Throw New IO.InvalidDataException("{0} was not a warcraft 3 player connection.".Frmt(socket.Name))
                    End If

                    'Parse
                    Dim pickle = Packet.Jars.Knock.Parse(data.SubView(4))
                    Dim vals = pickle.Value
                    Dim player = New W3ConnectingPlayer(Name:=CStr(vals("name")).AssumeNotNull,
                                                        gameid:=CUInt(vals("game id")),
                                                        entrykey:=CUInt(vals("entry key")),
                                                        peerkey:=CUInt(vals("peer key")),
                                                        listenport:=CUShort(vals("listen port")),
                                                        remoteendpoint:=CType(vals("internal address"), Net.IPEndPoint).AssumeNotNull,
                                                        socket:=socket)

                    'Handle
                    Dim oldSocketName = socket.Name
                    _logger.Log(Function() "{0} self-identified as {1} and wants to join game with id = {2}".Frmt(oldSocketName, player.Name, player.GameId), LogMessageType.Positive)
                    socket.Name = player.Name
                    _logger.Log(Function() "Received {0}".Frmt(PacketId.Knock), LogMessageType.DataEvent)
                    _logger.Log(Function() "{0}: {1}".Frmt(PacketId.Knock, pickle.Description.Value), LogMessageType.DataParsed)
                    inQueue.QueueAction(Sub() OnPlayerIntroduction(player))
                End Sub
            ).Catch(
                Sub(exception)
                    socket.Disconnect(expected:=False, reason:=exception.Message)
                End Sub
            )
        End Sub
        Public Function QueueAcceptSocket(ByVal socket As W3Socket) As ifuture
            Contract.Requires(socket IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() AcceptSocket(socket))
        End Function

        '''<summary>Handles connecting players that have sent their Knock packet.</summary>
        Private Sub OnPlayerIntroduction(ByVal player As W3ConnectingPlayer)
            Contract.Requires(player IsNot Nothing)

            'Get player's desired game set
            If Not _gameSets.ContainsKey(player.GameId) Then
                _logger.Log("{0} specified an invalid game id ({1})".Frmt(player.Name, player.GameId), LogMessageType.Negative)
                player.Socket.Disconnect(expected:=False, reason:="Invalid game id")
                Return
            End If
            Dim entry = _gameSets(player.GameId)

            'Send player to game set
            entry.QueueTryAcceptPlayer(player).CallWhenValueReady(
                Sub(game, exception)
                    If exception IsNot Nothing Then
                        _logger.Log("A game could not be found for {0}.".Frmt(player.Name), LogMessageType.Negative)
                    Else
                        _logger.Log("{0} entered {1}.".Frmt(player.Name, game.Name), LogMessageType.Positive)
                    End If
                End Sub)
        End Sub

        Private Function AddGameSet(ByVal gameSettings As GameSettings) As GameSet
            Contract.Requires(gameSettings IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameSet)() IsNot Nothing)

            Dim id = gameSettings.GameDescription.GameId
            If _gameSets.ContainsKey(id) Then Throw New InvalidOperationException("There is already a server entry with that game id.")
            Dim gameSet = New GameSet(gameSettings)
            _gameSets(id) = gameSet
            _viewGameSets.Add(gameSet)
            Dim gameLink = gameSet.QueueCreateGamesAsyncView(
                    adder:=Sub(sender, game) inQueue.QueueAction(Sub() _viewGames.Add(New Tuple(Of GameSet, Game)(gameSet, game))),
                    remover:=Sub(sender, game) inQueue.QueueAction(Sub() _viewGames.Remove(New Tuple(Of GameSet, Game)(gameSet, game))))
            Dim playerLink = gameSet.QueueCreatePlayersAsyncView(
                    adder:=Sub(sender, game, player) inQueue.QueueAction(Sub() _viewPlayers.Add(New Tuple(Of GameSet, Game, Player)(gameSet, game, player))),
                    remover:=Sub(sender, game, player) inQueue.QueueAction(Sub() _viewPlayers.Remove(New Tuple(Of GameSet, Game, Player)(gameSet, game, player))))

            'Automatic removal
            _gameSets(id).FutureDisposed.QueueCallWhenReady(inQueue,
                Sub()
                    _gameSets.Remove(id)
                    _viewGameSets.Remove(gameSet)
                    gameLink.CallOnValueSuccess(Sub(link) link.Dispose()).SetHandled()
                    playerLink.CallOnValueSuccess(Sub(link) link.Dispose()).SetHandled()
                End Sub)

            Return gameSet
        End Function
        Public Function QueueAddGameSet(ByVal gameSettings As GameSettings) As IFuture(Of GameSet)
            Contract.Requires(gameSettings IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of GameSet))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AddGameSet(gameSettings))
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(
                Sub()
                    For Each entry In _gameSets.Values
                        entry.Dispose()
                    Next entry

                    'For Each adv In linkedAdvertisers
                    'adv.RemoveGame(Me.Settings.Header, "Server killed.")
                    'Next adv

                    'change_state(ServerState.Disposed)
                    'Parent.QueueRemoveServer(Me.Name)
                End Sub)
        End Function


        '''''<summary>Adds a game to the server.</summary>
        ''Private Function CreateGame(Optional ByVal gameName As String = Nothing,
        ''Optional ByVal arguments As IEnumerable(Of String) = Nothing) As Game
        ''gameName = If(gameName, instanceCreationCount.ToString(CultureInfo.InvariantCulture))
        ''If state > ServerState.AcceptingPlayersAndPlayingGames Then
        ''Throw New InvalidOperationException("No longer accepting players. Can't create new instances.")
        ''End If
        ''Dim game = FindGame(gameName)
        ''If game IsNot Nothing Then
        ''Throw New InvalidOperationException("A game called '{0}' already exists.".Frmt(gameName))
        ''End If

        ''game = New Game(gameName, Settings.Map, Settings)
        ''Logger.Log(game.Name + " opened.", LogMessageType.Positive)
        ''instanceCreationCount += 1
        ''games_all.Add(game)
        ''games_lobby.Add(game)

        ''AddHandler game.PlayerTalked, AddressOf c_PlayerTalked
        ''AddHandler game.PlayerLeft, AddressOf c_PlayerLeft
        ''AddHandler game.ChangedState, AddressOf c_GameStateChanged
        ''AddHandler game.PlayerEntered, AddressOf c_PlayerEntered
        ''AddHandler game.PlayerSentData, AddressOf c_PlayerSentData

        ''SetAdvertiserOptions(private:=False)
        ''e_ThrowAddedGame(game)
        ''Return game
        ''End Function

        Private Function AsyncFindPlayer(ByVal username As String) As IFuture(Of Player)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return From futureFindResults In (From entry In _gameSets.Values Select entry.QueueTryFindPlayer(username)).ToList.Defuturized
                   Select (From player In futureFindResults Where player IsNot Nothing).FirstOrDefault
        End Function
        Public Function QueueFindPlayer(ByVal userName As String) As IFuture(Of Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncFindPlayer(userName)).Defuturized
        End Function

        Private Function AsyncFindPlayerGame(ByVal username As String) As IFuture(Of Game)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
            Return From futureFindResults In (From entry In _gameSets.Values Select entry.QueueTryFindPlayerGame(username)).ToList.Defuturized
                   Select (From game In futureFindResults Where game IsNot Nothing).FirstOrDefault
        End Function
        Public Function QueueFindPlayerGame(ByVal userName As String) As IFuture(Of Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncFindPlayerGame(userName)).Defuturized
        End Function

        ''''<summary>Removes a game with the given name.</summary>
        'Private Sub RemoveGame(ByVal gameName As String,
        'Optional ByVal ignorePermanent As Boolean = False)
        'Dim game = FindGame(gameName)
        'If game Is Nothing Then Throw New InvalidOperationException("No game with that name.")

        'RemoveHandler game.PlayerTalked, AddressOf c_PlayerTalked
        'RemoveHandler game.PlayerLeft, AddressOf c_PlayerLeft
        'RemoveHandler game.ChangedState, AddressOf c_GameStateChanged
        'RemoveHandler game.PlayerEntered, AddressOf c_PlayerEntered
        'RemoveHandler game.PlayerSentData, AddressOf c_PlayerSentData

        'games_all.Remove(game)
        'games_lobby.Remove(game)
        'games_load_screen.Remove(game)
        'games_gameplay.Remove(game)
        'game.QueueClose()
        'e_ThrowRemovedGame(game)

        'If Not ignorePermanent AndAlso Settings.permanent AndAlso
        'Settings.instances > 0 AndAlso
        'state < ServerState.OnlyPlayingGames Then
        'CreateGame()
        'End If
        'End Sub

        'Private ReadOnly linkedAdvertisers As New HashSet(Of IGameSourceSink)
        'Private Sub AddAdvertiser(ByVal m As IGameSourceSink)
        'If state > ServerState.AcceptingPlayersAndPlayingGames Then Throw New InvalidOperationException("Not accepting players anymore.")
        'If linkedAdvertisers.Contains(m) Then Throw New InvalidOperationException("Already have that advertiser.")
        'AddHandler m.RemovedGame, AddressOf c_AdvertiserRemovedGame
        'linkedAdvertisers.Add(m)
        'End Sub
        'Private Sub SetAdvertiserOptions(ByVal [private] As Boolean)
        'For Each m In linkedAdvertisers
        'm.SetAdvertisingOptions([private])
        'Next m
        'End Sub

        'Private Sub c_AdvertiserRemovedGame(ByVal _m As IGameSource, ByVal header As GameDescription, ByVal reason As String)
        'If header IsNot Settings.Header Then Return
        'Dim m = CType(_m, IGameSourceSink)
        'inQueue.QueueAction(
        'Sub()
        'If Not linkedAdvertisers.Contains(m) Then Return
        'RemoveHandler m.RemovedGame, AddressOf c_AdvertiserRemovedGame
        'linkedAdvertisers.Remove(m)
        'End Sub
        ')
        'End Sub
        'Private NotInheritable Class AdvertisingDependency
        'Inherits FutureDisposable
        'Private WithEvents server As GameServer

        'Public Sub New(ByVal server As GameServer)
        ''contract bug wrt interface event implementation requires this:
        ''Contract.Requires(server IsNot Nothing)
        'Contract.Assume(server IsNot Nothing)
        'Me.server = server
        'End Sub

        'Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
        'If Not finalizing Then
        'server.QueueStopAcceptingPlayers()
        'server = Nothing
        'End If
        'End Sub

        'Private Sub c_ServerStateChanged(ByVal sender As GameServer,
        'ByVal oldState As ServerState,
        'ByVal newState As ServerState) Handles server.ChangedState
        'If oldState <= ServerState.AcceptingPlayersAndPlayingGames And newState > ServerState.AcceptingPlayersAndPlayingGames Then
        'Dispose()
        'End If
        'End Sub
        'End Class

        'Public Function QueueFindGame(ByVal gameName As String) As IFuture(Of Game)
        'Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
        'Return inQueue.QueueFunc(Function() asyncFindGame(gameName))
        'End Function

        'Public Function QueueCreateGame(Optional ByVal gameName As String = Nothing) As IFuture(Of Game)
        'Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
        'Return inQueue.QueueFunc(Function() CreateGame(gameName))
        'End Function
        'Public Function QueueRemoveGame(ByVal gameName As String, Optional ByVal ignorePermanent As Boolean = False) As IFuture
        'Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        'Return inQueue.QueueAction(Sub() RemoveGame(gameName, ignorePermanent))
        'End Function
        Public Function QueueAddAdvertiser(ByVal advertiser As IGameSourceSink) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Throw New NotImplementedException
            'Return inQueue.QueueAction(Sub() AddAdvertiser(advertiser))
        End Function
        Public Function CreateAdvertisingDependency() As FutureDisposable
            Contract.Ensures(Contract.Result(Of FutureDisposable)() IsNot Nothing)
            'Return New AdvertisingDependency(Me)
            Throw New NotImplementedException
        End Function

        Private Function CreateGameSetsAsyncView(ByVal adder As Action(Of GameServer, GameSet),
                                                 ByVal remover As Action(Of GameServer, GameSet)) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _viewGameSets.BeginSync(adder:=Sub(sender, item) adder(Me, item),
                                                 remover:=Sub(sender, item) remover(Me, item))
        End Function
        Public Function QueueCreateGameSetsAsyncView(ByVal adder As Action(Of GameServer, GameSet),
                                                     ByVal remover As Action(Of GameServer, GameSet)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CreateGameSetsAsyncView(adder, remover))
        End Function

        Private Function CreateGamesAsyncView(ByVal adder As Action(Of GameServer, GameSet, Game),
                                              ByVal remover As Action(Of GameServer, GameSet, Game)) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _viewGames.BeginSync(adder:=Sub(sender, item) adder(Me, item.Item1, item.Item2),
                                        remover:=Sub(sender, item) remover(Me, item.Item1, item.Item2))
        End Function
        Public Function QueueCreateGamesAsyncView(ByVal adder As Action(Of GameServer, GameSet, Game),
                                                  ByVal remover As Action(Of GameServer, GameSet, Game)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CreateGamesAsyncView(adder, remover))
        End Function

        Private Function CreatePlayersAsyncView(ByVal adder As Action(Of GameServer, GameSet, Game, Player),
                                            ByVal remover As Action(Of GameServer, GameSet, Game, Player)) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _viewPlayers.BeginSync(adder:=Sub(sender, item) adder(Me, item.Item1, item.Item2, item.Item3),
                                          remover:=Sub(sender, item) remover(Me, item.Item1, item.Item2, item.Item3))
        End Function
        Public Function QueueCreatePlayersAsyncView(ByVal adder As Action(Of GameServer, GameSet, Game, Player),
                                                    ByVal remover As Action(Of GameServer, GameSet, Game, Player)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CreatePlayersAsyncView(adder, remover))
        End Function
    End Class
End Namespace
