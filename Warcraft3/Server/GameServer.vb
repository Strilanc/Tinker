Imports Tinker.Pickling

Namespace WC3
    Public NotInheritable Class GameServer
        Inherits FutureDisposable

        Private Shared ReadOnly InitialConnectionTimeout As TimeSpan = 5.Seconds

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue

        Private ReadOnly _clock As IClock
        Private ReadOnly _logger As Logger
        Private ReadOnly _gameSets As New Dictionary(Of UInt32, GameSet)()
        Private ReadOnly _viewGameSets As New AsyncViewableCollection(Of GameSet)(outQueue:=outQueue)
        Private ReadOnly _viewActiveGameSets As New AsyncViewableCollection(Of GameSet)(outQueue:=outQueue)
        Private ReadOnly _viewGames As New AsyncViewableCollection(Of Tuple(Of GameSet, Game))(outQueue:=outQueue)
        Private ReadOnly _viewPlayers As New AsyncViewableCollection(Of Tuple(Of GameSet, Game, Player))(outQueue:=outQueue)

        Public Event PlayerTalked(ByVal sender As GameServer, ByVal game As Game, ByVal player As Player, ByVal text As String)
        Public Event PlayerLeft(ByVal sender As GameServer, ByVal game As Game, ByVal gameState As GameState, ByVal player As Player, ByVal reportedResult As Protocol.PlayerLeaveReason, ByVal reasonDescription As String)
        Public Event PlayerSentData(ByVal sever As GameServer, ByVal game As Game, ByVal player As Player, ByVal data As Byte())
        Public Event PlayerEntered(ByVal sender As GameServer, ByVal game As Game, ByVal player As Player)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_viewGames IsNot Nothing)
            Contract.Invariant(_viewGameSets IsNot Nothing)
            Contract.Invariant(_viewActiveGameSets IsNot Nothing)
            Contract.Invariant(_viewPlayers IsNot Nothing)
            Contract.Invariant(_gameSets IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
        End Sub

        Public Sub New(ByVal clock As IClock,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(clock IsNot Nothing)
            Me._logger = If(logger, New Logger)
            Me._clock = clock
        End Sub

        Public ReadOnly Property Logger As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property
        Public ReadOnly Property Clock As IClock
            Get
                Contract.Ensures(Contract.Result(Of IClock)() IsNot Nothing)
                Return _clock
            End Get
        End Property

        '''<summary>Handles new connections to the server.</summary>
        Private Sub AcceptSocket(ByVal socket As W3Socket)
            Contract.Requires(socket IsNot Nothing)

            _logger.Log("Connection from {0}.".Frmt(socket.Name), LogMessageType.Positive)
            Dim socketHandled = New OnetimeLock()

            'Setup initial timeout
            _clock.AsyncWait(InitialConnectionTimeout).CallWhenReady(
                Sub()
                    If Not socketHandled.TryAcquire Then Return
                    socket.Disconnect(expected:=True, reason:="Timeout")
                End Sub)

            'Try to read Knock packet
            socket.AsyncReadPacket().CallOnValueSuccess(
                Sub(data) If socketHandled.TryAcquire Then HandleFirstPacket(socket, data)
            ).Catch(
                Sub(exception) socket.Disconnect(expected:=False, reason:=exception.Message)
            )
        End Sub
        Public Function QueueAcceptSocket(ByVal socket As W3Socket) As ifuture
            Contract.Requires(socket IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() AcceptSocket(socket))
        End Function
        <ContractVerification(False)>
        Private Sub HandleFirstPacket(ByVal socket As W3Socket, ByVal data As IReadableList(Of Byte))
            Contract.Requires(socket IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            If data.Count < 4 OrElse data(0) <> Protocol.Packets.PacketPrefix OrElse data(1) <> Protocol.PacketId.Knock Then
                Throw New IO.InvalidDataException("{0} was not a warcraft 3 player connection.".Frmt(socket.Name))
            End If

            'Parse
            Dim pickle = Protocol.Packets.Knock.Jar.Parse(data.SubView(4))
            Dim vals = pickle.Value
            Dim player = New W3ConnectingPlayer(Name:=CStr(vals("name")).AssumeNotNull,
                                                gameid:=CUInt(vals("game id")),
                                                entrykey:=CUInt(vals("entry key")),
                                                peerkey:=CUInt(vals("peer key")),
                                                peerData:=CType(vals("peer data"), IReadableList(Of Byte)).AssumeNotNull,
                                                listenport:=CUShort(vals("listen port")),
                                                remoteendpoint:=CType(vals("internal address"), Net.IPEndPoint).AssumeNotNull,
                                                socket:=socket)

            'Handle
            Dim oldSocketName = socket.Name
            _logger.Log(Function() "{0} self-identified as {1} and wants to join game with id = {2}".Frmt(oldSocketName, player.Name, player.GameId), LogMessageType.Positive)
            socket.Name = player.Name
            _logger.Log(Function() "Received {0} from {1}".Frmt(Protocol.PacketId.Knock, oldSocketName), LogMessageType.DataEvent)
            _logger.Log(Function() "Received {0} from {1}: {2}".Frmt(Protocol.PacketId.Knock, oldSocketName, pickle.Description.Value), LogMessageType.DataParsed)
            inQueue.QueueAction(Sub() OnPlayerIntroduction(player))
        End Sub

        '''<summary>Handles connecting players that have sent their Knock packet.</summary>
        Private Sub OnPlayerIntroduction(ByVal player As W3ConnectingPlayer)
            Contract.Requires(player IsNot Nothing)

            'Get player's desired game set
            If Not _gameSets.ContainsKey(player.GameId) Then
                _logger.Log("{0} specified an invalid game id ({1})".Frmt(player.Name, player.GameId), LogMessageType.Negative)
                player.Socket.SendPacket(Protocol.MakeReject(Protocol.RejectReason.GameNotFound))
                player.Socket.Disconnect(expected:=False, reason:="Invalid game id")
                Return
            End If
            Dim entry = _gameSets(player.GameId)
            Contract.Assume(entry IsNot Nothing)

            'Send player to game set
            entry.QueueTryAcceptPlayer(player).CallOnValueSuccess(
                Sub(game) _logger.Log("{0} entered {1}.".Frmt(player.Name, game.Name), LogMessageType.Positive)
            ).Catch(
                Sub(exception)
                    _logger.Log("A game could not be found for {0}.".Frmt(player.Name), LogMessageType.Negative)
                    player.Socket.SendPacket(Protocol.MakeReject(Protocol.RejectReason.GameFull))
                    player.Socket.Disconnect(expected:=True, reason:="A game could not be found for {0}.".Frmt(player.Name))
                End Sub
            )
        End Sub

        Private Function AddGameSet(ByVal gameSettings As GameSettings) As GameSet
            Contract.Requires(gameSettings IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameSet)() IsNot Nothing)

            Dim id = gameSettings.GameDescription.GameId
            If _gameSets.ContainsKey(id) Then Throw New InvalidOperationException("There is already a server entry with that game id.")
            Dim gameSet = New GameSet(gameSettings, _clock)
            _gameSets(id) = gameSet
            _viewGameSets.Add(gameSet)
            _viewActiveGameSets.Add(gameSet)
            Dim activeAdder As WC3.GameSet.StateChangedEventHandler = Sub(sender, active) inQueue.QueueAction(
                Sub()
                    If _viewActiveGameSets.Contains(sender) <> active Then
                        If active Then
                            _viewActiveGameSets.Add(sender)
                        Else
                            _viewActiveGameSets.Remove(sender)
                        End If
                    End If
                End Sub)
            AddHandler gameSet.StateChanged, activeAdder

            Dim gameLink = gameSet.QueueCreateGamesAsyncView(
                    adder:=Sub(sender, game) inQueue.QueueAction(Sub() _viewGames.Add(New Tuple(Of GameSet, Game)(gameSet, game))),
                    remover:=Sub(sender, game) inQueue.QueueAction(Sub() _viewGames.Remove(New Tuple(Of GameSet, Game)(gameSet, game))))
            Dim playerLink = gameSet.QueueCreatePlayersAsyncView(
                    adder:=Sub(sender, game, player) inQueue.QueueAction(Sub() _viewPlayers.Add(New Tuple(Of GameSet, Game, Player)(gameSet, game, player))),
                    remover:=Sub(sender, game, player) inQueue.QueueAction(Sub() _viewPlayers.Remove(New Tuple(Of GameSet, Game, Player)(gameSet, game, player))))

            'Automatic removal
            gameSet.FutureDisposed.QueueCallWhenReady(inQueue,
                Sub()
                    _gameSets.Remove(id)
                    _viewGameSets.Remove(gameSet)
                    _viewActiveGameSets.Remove(gameSet)
                    gameLink.CallOnValueSuccess(Sub(link) link.Dispose()).SetHandled()
                    playerLink.CallOnValueSuccess(Sub(link) link.Dispose()).SetHandled()
                    RemoveHandler gameSet.StateChanged, activeAdder
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
                End Sub)
        End Function

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

        Private Function CreateActiveGameSetsAsyncView(ByVal adder As Action(Of GameServer, GameSet),
                                                       ByVal remover As Action(Of GameServer, GameSet)) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _viewActiveGameSets.BeginSync(adder:=Sub(sender, item) adder(Me, item),
                                                 remover:=Sub(sender, item) remover(Me, item))
        End Function
        Public Function QueueCreateActiveGameSetsAsyncView(ByVal adder As Action(Of GameServer, GameSet),
                                                           ByVal remover As Action(Of GameServer, GameSet)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CreateActiveGameSetsAsyncView(adder, remover))
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
