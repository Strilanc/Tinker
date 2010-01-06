Namespace WC3
    Public Class GameSet
        Inherits FutureDisposable

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly _logger As Logger
        Private ReadOnly _gameSettings As GameSettings
        Private ReadOnly _games As New AsyncViewableCollection(Of Game)(outQueue:=outQueue)
        Private ReadOnly _viewPlayers As New AsyncViewableCollection(Of Tuple(Of Game, Player))(outQueue:=outQueue)
        Private allocId As Integer
        Public Event StateChanged(ByVal sender As GameSet, ByVal acceptingPlayers As Boolean)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_gameSettings IsNot Nothing)
            Contract.Invariant(_games IsNot Nothing)
            Contract.Invariant(_viewPlayers IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
        End Sub

        Public Sub New(ByVal gameSettings As GameSettings,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(gameSettings IsNot Nothing)
            Me._gameSettings = gameSettings
            Me._logger = If(logger, New Logger)
            _activeGameCount = 1
            For i = 0 To gameSettings.NumInstances - 1
                AddInstance()
            Next i
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
            Set(ByVal value As Integer)
                If (value = 0) <> (_activeGameCount = 0) Then
                    outQueue.QueueAction(Sub() RaiseEvent StateChanged(Me, acceptingPlayers:=value <> 0))
                End If
                _activeGameCount = value
            End Set
        End Property

        Private Function AsyncTryAcceptPlayer(ByVal player As W3ConnectingPlayer) As IFuture(Of Game)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
            Dim result = New FutureFunction(Of Game)()

            Dim futureSelectGame = _games.FutureSelect(
                filterFunction:=Function(game) game.QueueTryAddPlayer(player).EvalWhenValueReady(
                                        Function(addedPlayer, playerException) addedPlayer IsNot Nothing
                                    ))

            futureSelectGame.CallOnValueSuccess(
                Sub(game) result.SetSucceeded(game)
            )
            futureSelectGame.Catch(
                Sub(exception)
                    If _gameSettings.UseInstanceOnDemand Then
                        result.SetSucceeded(AddInstance())
                    Else
                        result.SetFailed(New OperationFailedException("Failed to find game for player (eg. {0}).".Frmt(exception.Message)))
                    End If
                End Sub
            )
            Return result
        End Function
        Public Function QueueTryAcceptPlayer(ByVal player As W3ConnectingPlayer) As IFuture(Of Game)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncTryAcceptPlayer(player)).Defuturized
        End Function

        Private Function TryFindGame(ByVal name As InvariantString) As Game
            Return (From game In _games Where game.Name = name).FirstOrDefault
        End Function
        Private Function AddInstance() As Game
            Contract.Ensures(Contract.Result(Of Game)() IsNot Nothing)

            Dim name = allocId.ToString(CultureInfo.InvariantCulture)
            allocId += 1

            Dim game = New Game(name, _gameSettings)
            _logger.Log("{0} opened.".Frmt(name), LogMessageType.Positive)
            _games.Add(game)

            'AddHandler game.PlayerTalked, AddressOf c_PlayerTalked
            'AddHandler game.PlayerLeft, AddressOf c_PlayerLeft
            AddHandler game.ChangedState, Sub(sender, oldState, newState) inQueue.QueueAction(
                    Sub()
                        If oldState < GameState.Loading AndAlso newState >= GameState.Loading Then
                            ActiveGameCount -= 1
                        End If
                    End Sub)
            'AddHandler game.PlayerEntered, AddressOf c_PlayerEntered
            'AddHandler game.PlayerSentData, AddressOf c_PlayerSentData

            'SetAdvertiserOptions(private:=False)
            'e_ThrowAddedGame(game)
            Dim playerLink = game.QueueCreatePlayersAsyncView(
                    adder:=Sub(sender, player) inQueue.QueueAction(Sub() _viewPlayers.Add(New Tuple(Of Game, Player)(game, player))),
                    remover:=Sub(sender, player) inQueue.QueueAction(Sub() _viewPlayers.Remove(New Tuple(Of Game, Player)(game, player))))

            'Automatic removal
            game.FutureDisposed.QueueCallWhenReady(inQueue,
                Sub()
                    _games.Remove(game)
                    playerLink.CallOnValueSuccess(Sub(link) link.Dispose()).SetHandled()
                    If _gameSettings.UsePermanent Then AddInstance()
                    If _games.Count = 0 AndAlso Not _gameSettings.UseInstanceOnDemand Then
                        Me.Dispose()
                    End If
                End Sub)

            ActiveGameCount += 1
            Return game
        End Function

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(
                Sub()
                    For Each game In _games
                        game.Dispose()
                    Next game
                End Sub)
        End Function

        Private Function AsyncTryFindPlayer(ByVal userName As InvariantString) As IFuture(Of Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return From futureFindResults In (From game In _games Select game.QueueTryFindPlayer(userName)).ToList.Defuturized
                   Select (From player In futureFindResults Where player IsNot Nothing).FirstOrDefault
        End Function
        Public Function QueueTryFindPlayer(ByVal userName As InvariantString) As IFuture(Of Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncTryFindPlayer(userName)).Defuturized
        End Function

        Private Function AsyncTryFindPlayerGame(ByVal userName As InvariantString) As IFuture(Of Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
            Return _games.ToList.FutureSelect(Function(game) game.QueueTryFindPlayer(userName).Select(Function(player) player IsNot Nothing))
        End Function
        Public Function QueueTryFindPlayerGame(ByVal userName As InvariantString) As IFuture(Of Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncTryFindPlayerGame(userName)).Defuturized
        End Function

        Private Function CreateGameAsyncView(ByVal adder As Action(Of GameSet, Game),
                                             ByVal remover As Action(Of GameSet, Game)) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _games.BeginSync(adder:=Sub(sender, item) adder(Me, item),
                                    remover:=Sub(sender, item) remover(Me, item))
        End Function
        Public Function QueueCreateGamesAsyncView(ByVal adder As Action(Of GameSet, Game),
                                                  ByVal remover As Action(Of GameSet, Game)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CreateGameAsyncView(adder, remover))
        End Function
        Public Function QueueTryFindGame(ByVal gameName As InvariantString) As IFuture(Of Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() (From game In _games Where game.Name = gameName).FirstOrDefault)
        End Function

        Private Function CreatePlayersAsyncView(ByVal adder As Action(Of GameSet, Game, Player),
                                                ByVal remover As Action(Of GameSet, Game, Player)) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _viewPlayers.BeginSync(adder:=Sub(sender, item) adder(Me, item.Item1, item.Item2),
                                          remover:=Sub(sender, item) remover(Me, item.Item1, item.Item2))
        End Function
        Public Function QueueCreatePlayersAsyncView(ByVal adder As Action(Of GameSet, Game, Player),
                                                    ByVal remover As Action(Of GameSet, Game, Player)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CreatePlayersAsyncView(adder, remover))
        End Function
    End Class
End Namespace
