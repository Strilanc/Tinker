Namespace WC3
    Public Class GameSet
        Inherits DisposableWithTask

        Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue
        Private ReadOnly outQueue As CallQueue = MakeTaskedCallQueue
        Private ReadOnly _logger As Logger
        Private ReadOnly _gameSettings As GameSettings
        Private ReadOnly _games As New ObservableCollection(Of Game)(outQueue:=outQueue)
        Private ReadOnly _viewPlayers As New ObservableCollection(Of Tuple(Of Game, Player))(outQueue:=outQueue)
        Private ReadOnly _clock As IClock
        Private allocId As Integer
        Public Event StateChanged(sender As GameSet, acceptingPlayers As Boolean)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_gameSettings IsNot Nothing)
            Contract.Invariant(_games IsNot Nothing)
            Contract.Invariant(_viewPlayers IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
        End Sub

        Public Sub New(gameSettings As GameSettings,
                       clock As IClock,
                       Optional logger As Logger = Nothing)
            Contract.Assume(gameSettings IsNot Nothing)
            Contract.Assume(clock IsNot Nothing)
            Me._gameSettings = gameSettings
            Me._clock = clock
            Me._logger = If(logger, New Logger)
            _activeGameCount = 1
            For Each repeat In gameSettings.InitialInstanceCount.Range
                AddInstance()
            Next repeat
            If Not gameSettings.UseInstanceOnDemand Then _activeGameCount -= 1
        End Sub

        Public ReadOnly Property GameSettings As GameSettings
            Get
                Contract.Ensures(Contract.Result(Of GameSettings)() IsNot Nothing)
                Return _gameSettings
            End Get
        End Property

        Private _activeGameCount As Integer
        Private Property ActiveGameCount As Integer
            Get
                Return _activeGameCount
            End Get
            Set(value As Integer)
                If (value = 0) <> (_activeGameCount = 0) Then
                    outQueue.QueueAction(Sub() RaiseEvent StateChanged(Me, acceptingPlayers:=value <> 0))
                End If
                _activeGameCount = value
            End Set
        End Property

        Private Async Function AsyncTryAcceptPlayer(knockData As Protocol.KnockData,
                                                    socket As W3Socket) As Task(Of Game)
            Contract.Assume(knockData IsNot Nothing)
            Contract.Assume(socket IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of Task(Of Game))() IsNot Nothing)

            Try
                Return Await _games.FirstMatchAsync(
                    Async Function(g)
                        Dim player = Await g.QueueAddPlayer(knockData, socket)
                        Return player IsNot Nothing
                    End Function)
            Catch ex As OperationFailedException
                If _gameSettings.UseInstanceOnDemand Then Return AddInstance()
                Throw New OperationFailedException("Failed to find game for player.", ex)
            End Try
        End Function
        Public Function QueueTryAcceptPlayer(knockData As Protocol.KnockData,
                                             socket As W3Socket) As Task(Of Game)
            Contract.Requires(knockData IsNot Nothing)
            Contract.Requires(socket IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of Game))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncTryAcceptPlayer(knockData, socket)).Unwrap.AssumeNotNull
        End Function

        Private Function TryFindGame(name As InvariantString) As Game
            Return (From game In _games Where game.Name = name).FirstOrDefault
        End Function
        Private Function AddInstance() As Game
            Contract.Ensures(Contract.Result(Of Game)() IsNot Nothing)

            Dim name = allocId.ToString(CultureInfo.InvariantCulture)
            allocId += 1

            Dim game = WC3.Game.FromSettings(_gameSettings, name, _clock, New Logger)
            _logger.Log("{0} opened.".Frmt(name), LogMessageType.Positive)
            _games.Add(game)

            AddHandler game.ChangedState, Sub(sender, oldState, newState) inQueue.QueueAction(
                    Sub()
                        If oldState < GameState.Loading AndAlso newState >= GameState.Loading Then
                            ActiveGameCount -= 1
                        End If
                    End Sub)

            'SetAdvertiserOptions(private:=False)
            Dim playerLink = game.ObservePlayers(
                    adder:=Sub(player) inQueue.QueueAction(Sub() _viewPlayers.Add(Tuple.Create(game, player))),
                    remover:=Sub(player) inQueue.QueueAction(Sub() _viewPlayers.Remove(Tuple.Create(game, player))))

            'Automatic removal
            Call Async Sub()
                     Await game.DisposalTask
                     _games.Remove(game)
                     Call Async Sub() Await playerLink.DisposeAsync()
                     If _gameSettings.UsePermanent Then AddInstance()
                     If _games.Count = 0 AndAlso Not _gameSettings.UseInstanceOnDemand Then
                         Me.Dispose()
                     End If
                 End Sub

            ActiveGameCount += 1
            game.Start()
            Return game
        End Function

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(
                Sub()
                    For Each game In _games
                        game.Dispose()
                    Next game
                End Sub)
        End Function

        Private Async Function AsyncTryFindPlayer(userName As InvariantString) As Task(Of Player)
            'Contract.Ensures(Contract.Result(Of Task(Of Player))() IsNot Nothing)
            Dim findResults = Await Task.WhenAll(From game In _games Select game.QueueTryFindPlayer(userName))
            Return (From player In findResults Where player IsNot Nothing).FirstOrDefault
        End Function
        Public Function QueueTryFindPlayer(userName As InvariantString) As Task(Of Player)
            Contract.Ensures(Contract.Result(Of Task(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncTryFindPlayer(userName)).Unwrap.AssumeNotNull
        End Function

        Private Function AsyncTryFindPlayerGame(userName As InvariantString) As Task(Of Game)
            'Contract.Ensures(Contract.Result(Of Task(Of Game))() IsNot Nothing)
            Return _games.ToList.FirstMatchAsync(
                Async Function(game)
                    Dim player = Await game.QueueTryFindPlayer(userName)
                    Return player IsNot Nothing
                End Function)
        End Function
        Public Function QueueTryFindPlayerGame(userName As InvariantString) As Task(Of Game)
            Contract.Ensures(Contract.Result(Of Task(Of Game))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncTryFindPlayerGame(userName)).Unwrap.AssumeNotNull
        End Function

        Public Function ObserveGames(adder As Action(Of Game),
                                     remover As Action(Of Game)) As Task(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _games.Observe(adder, remover))
        End Function
        Public Function QueueTryFindGame(gameName As InvariantString) As Task(Of Game)
            Contract.Ensures(Contract.Result(Of Task(Of Game))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() (From game In _games Where game.Name = gameName).FirstOrDefault)
        End Function

        Public Function ObservePlayers(adder As Action(Of Game, Player),
                                       remover As Action(Of Game, Player)) As Task(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _viewPlayers.Observe(
                adder:=Sub(item) adder(item.Item1, item.Item2),
                remover:=Sub(item) remover(item.Item1, item.Item2)))
        End Function
    End Class
End Namespace
