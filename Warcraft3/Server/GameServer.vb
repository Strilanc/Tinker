Imports Tinker.Pickling

Namespace WC3
    Public NotInheritable Class GameServer
        Inherits DisposableWithTask

        Private Shared ReadOnly InitialConnectionTimeout As TimeSpan = 5.Seconds

        Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue
        Private ReadOnly outQueue As CallQueue = MakeTaskedCallQueue

        Private ReadOnly _clock As IClock
        Private ReadOnly _logger As Logger
        Private ReadOnly _gameSets As New Dictionary(Of UInt32, GameSet)()
        Private ReadOnly _viewGameSets As New ObservableCollection(Of GameSet)(outQueue:=outQueue)
        Private ReadOnly _viewActiveGameSets As New ObservableCollection(Of GameSet)(outQueue:=outQueue)
        Private ReadOnly _viewGames As New ObservableCollection(Of Tuple(Of GameSet, Game))(outQueue:=outQueue)
        Private ReadOnly _viewPlayers As New ObservableCollection(Of Tuple(Of GameSet, Game, Player))(outQueue:=outQueue)

        Public Event PlayerTalked(sender As GameServer, game As Game, player As Player, text As String)
        Public Event PlayerLeft(sender As GameServer, game As Game, gameState As GameState, player As Player, reportedResult As Protocol.PlayerLeaveReason, reasonDescription As String)
        Public Event PlayerSentData(sever As GameServer, game As Game, player As Player, data As Byte())
        Public Event PlayerEntered(sender As GameServer, game As Game, player As Player)

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

        Public Sub New(clock As IClock,
                       Optional logger As Logger = Nothing)
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
        Private Async Sub AcceptSocket(socket As W3Socket)
            Contract.Assume(socket IsNot Nothing)

            _logger.Log("Connection from {0}.".Frmt(socket.Name), LogMessageType.Positive)
            Dim socketHandled = New OnetimeLock()

            'Setup initial timeout
            Call Async Sub()
                     Await _clock.AsyncWait(InitialConnectionTimeout)
                     If Not socketHandled.TryAcquire Then Return
                     socket.Disconnect(expected:=True, reason:="Timeout")
                 End Sub

            'Try to read Knock packet
            Try
                Dim data = Await socket.AsyncReadPacket()
                If Not socketHandled.TryAcquire Then Return
                HandleFirstPacket(socket, data)
            Catch ex As Exception
                socket.Disconnect(expected:=False, reason:=ex.Summarize)
            End Try
        End Sub
        Public Function QueueAcceptSocket(socket As W3Socket) As Task
            Contract.Requires(socket IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() AcceptSocket(socket))
        End Function
        Private Sub HandleFirstPacket(socket As W3Socket, data As IRist(Of Byte))
            Contract.Requires(socket IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            If data.Count < 4 OrElse data(0) <> Protocol.Packets.PacketPrefix OrElse data(1) <> Protocol.PacketId.Knock Then
                Throw New IO.InvalidDataException("{0} was not a warcraft 3 player connection.".Frmt(socket.Name))
            End If

            'Parse
            Dim pickle = Protocol.ClientPackets.Knock.Jar.ParsePickle(data.SkipExact(4))
            Dim knockData = pickle.Value

            'Handle
            Dim oldSocketName = socket.Name
            _logger.Log(Function() "{0} self-identified as {1} and wants to join game with id = {2}".Frmt(oldSocketName,
                                                                                                          knockData.Name,
                                                                                                          knockData.GameId), LogMessageType.Positive)
            socket.Name = knockData.Name
            _logger.Log(Function() "Received {0} from {1}".Frmt(Protocol.PacketId.Knock, oldSocketName), LogMessageType.DataEvent)
            _logger.Log(Function() "Received {0} from {1}: {2}".Frmt(Protocol.PacketId.Knock, oldSocketName, pickle.Description), LogMessageType.DataParsed)
            inQueue.QueueAction(Sub() OnPlayerIntroduction(knockData, socket))
        End Sub

        '''<summary>Handles connecting players that have sent their Knock packet.</summary>
        Private Async Sub OnPlayerIntroduction(knockData As Protocol.KnockData, socket As W3Socket)
            Contract.Assume(knockData IsNot Nothing)
            Contract.Assume(socket IsNot Nothing)

            'Get player's desired game set
            If Not _gameSets.ContainsKey(knockData.GameId) Then
                _logger.Log("{0} specified an invalid game id ({1})".Frmt(knockData.Name, knockData.GameId), LogMessageType.Negative)
                socket.SendPacket(Protocol.MakeReject(Protocol.RejectReason.GameNotFound))
                socket.Disconnect(expected:=False, reason:="Invalid game id")
                Return
            End If
            Dim entry = _gameSets(knockData.GameId)
            Contract.Assume(entry IsNot Nothing)

            'Send player to game set
            Try
                Dim game = Await entry.QueueTryAcceptPlayer(knockData, socket)
                _logger.Log("{0} entered {1}.".Frmt(knockData.Name, game.Name), LogMessageType.Positive)
            Catch ex As Exception
                _logger.Log("A game could not be found for {0}.".Frmt(knockData.Name), LogMessageType.Negative)
                socket.SendPacket(Protocol.MakeReject(Protocol.RejectReason.GameFull))
                socket.Disconnect(expected:=True, reason:="A game could not be found for {0}.".Frmt(knockData.Name))
            End Try
        End Sub

        Private Function AddGameSet(gameSettings As GameSettings) As GameSet
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

            Dim gameLink = gameSet.ObserveGames(
                    adder:=Sub(game) inQueue.QueueAction(Sub() _viewGames.Add(Tuple.Create(gameSet, game))),
                    remover:=Sub(game) inQueue.QueueAction(Sub() _viewGames.Remove(Tuple.Create(gameSet, game))))
            Dim playerLink = gameSet.ObservePlayers(
                    adder:=Sub(game, player) inQueue.QueueAction(Sub() _viewPlayers.Add(Tuple.Create(gameSet, game, player))),
                    remover:=Sub(game, player) inQueue.QueueAction(Sub() _viewPlayers.Remove(Tuple.Create(gameSet, game, player))))

            'Automatic removal
            Call Async Sub()
                     Await gameSet.DisposalTask
                     _gameSets.Remove(id)
                     _viewGameSets.Remove(gameSet)
                     _viewActiveGameSets.Remove(gameSet)
                     gameLink.DisposeAsync()
                     playerLink.DisposeAsync()
                     RemoveHandler gameSet.StateChanged, activeAdder
                 End Sub

            Return gameSet
        End Function
        Public Function QueueAddGameSet(gameSettings As GameSettings) As Task(Of GameSet)
            Contract.Requires(gameSettings IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of GameSet))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AddGameSet(gameSettings))
        End Function

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(
                Sub()
                    For Each entry In _gameSets.Values
                        entry.Dispose()
                    Next entry
                End Sub)
        End Function

        Private Async Function AsyncFindPlayer(username As String) As Task(Of Player)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of Player))() IsNot Nothing)
            Dim findResults = Await TaskEx.WhenAll(From entry In _gameSets.Values Select entry.QueueTryFindPlayer(username))
            Return (From player In findResults Where player IsNot Nothing).FirstOrDefault
        End Function
        Public Function QueueFindPlayer(userName As String) As Task(Of Player)
            Contract.Ensures(Contract.Result(Of Task(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncFindPlayer(userName)).Unwrap.AssumeNotNull
        End Function

        Private Async Function AsyncFindPlayerGame(username As String) As Task(Of Game)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of Game))() IsNot Nothing)
            Dim findResults = Await TaskEx.WhenAll(From entry In _gameSets.Values Select entry.QueueTryFindPlayerGame(username))
            Return (From game In findResults Where game IsNot Nothing).FirstOrDefault
        End Function
        Public Function QueueFindPlayerGame(userName As String) As Task(Of Game)
            Contract.Ensures(Contract.Result(Of Task(Of Game))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncFindPlayerGame(userName)).Unwrap.AssumeNotNull
        End Function

        Public Function ObserveGameSets(adder As Action(Of GameSet),
                                        remover As Action(Of GameSet)) As Task(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _viewGameSets.Observe(adder, remover))
        End Function

        Public Function ObserveActiveGameSets(adder As Action(Of GameSet),
                                              remover As Action(Of GameSet)) As Task(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _viewActiveGameSets.Observe(adder, remover))
        End Function

        Public Function ObserveGames(adder As Action(Of GameSet, Game),
                                     remover As Action(Of GameSet, Game)) As Task(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _viewGames.Observe(
                adder:=Sub(item) adder(item.Item1, item.Item2),
                remover:=Sub(item) remover(item.Item1, item.Item2)))
        End Function

        Public Function ObservePlayers(adder As Action(Of GameSet, Game, Player),
                                       remover As Action(Of GameSet, Game, Player)) As Task(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _viewPlayers.Observe(
                adder:=Sub(item) adder(item.Item1, item.Item2, item.Item3),
                remover:=Sub(item) remover(item.Item1, item.Item2, item.Item3)))
        End Function
    End Class
End Namespace
