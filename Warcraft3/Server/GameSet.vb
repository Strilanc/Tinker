Namespace WC3
    Public Class GameSet
        Inherits FutureDisposable

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly _logger As Logger
        Private ReadOnly _gameSettings As GameSettings
        Private ReadOnly _games As New AsyncViewableCollection(Of Game)(outQueue:=outQueue)
        Private allocId As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_gameSettings IsNot Nothing)
            Contract.Invariant(_games IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
        End Sub

        Public Sub New(ByVal gameSettings As GameSettings,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Requires(gameSettings IsNot Nothing)
            Me._gameSettings = gameSettings
            Me._logger = If(logger, New Logger)
            For i = 0 To gameSettings.NumInstances - 1
                AddInstance()
            Next i
        End Sub

        Public ReadOnly Property GameSettings As GameSettings
            Get
                Contract.Ensures(Contract.Result(Of GameSettings)() IsNot Nothing)
                Return _gameSettings
            End Get
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

        Private Function TryFindGame(ByVal name As InvariantString) As Game
            Return (From game In _games Where game.Name = name).FirstOrDefault
        End Function
        Private Function AddInstance() As Game
            Contract.Ensures(Contract.Result(Of Game)() IsNot Nothing)

            Dim name = allocId.ToString
            allocId += 1

            Dim game = New Game(name, _gameSettings)
            _logger.Log("{0} opened.".Frmt(name), LogMessageType.Positive)
            _games.Add(game)

            'AddHandler game.PlayerTalked, AddressOf c_PlayerTalked
            'AddHandler game.PlayerLeft, AddressOf c_PlayerLeft
            AddHandler game.ChangedState, Sub(sender, newState, oldState) inQueue.QueueAction(
                    Sub()
                        If newState = GameState.Closed Then
                            _games.Remove(sender)
                        End If
                    End Sub)
            'AddHandler game.PlayerEntered, AddressOf c_PlayerEntered
            'AddHandler game.PlayerSentData, AddressOf c_PlayerSentData

            'SetAdvertiserOptions(private:=False)
            'e_ThrowAddedGame(game)
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

        Private Function AsyncTryFindPlayer(ByVal username As String) As IFuture(Of Player)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return From futureFindResults In (From game In _games Select game.QueueFindPlayer(username)).ToList.Defuturized
                   Select (From player In futureFindResults Where player IsNot Nothing).FirstOrDefault
        End Function

        Private Function AsyncTryFindPlayerGame(ByVal username As String) As IFuture(Of Game)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
            Return _games.ToList.FutureSelect(Function(game) game.QueueFindPlayer(username).Select(Function(player) player IsNot Nothing))
        End Function

        Public Function QueueTryAcceptPlayer(ByVal player As W3ConnectingPlayer) As IFuture(Of Game)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncTryAcceptPlayer(player)).Defuturized
        End Function
        Public Function QueueTryFindPlayerGame(ByVal username As String) As IFuture(Of Game)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Game))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncTryFindPlayerGame(username)).Defuturized
        End Function
        Public Function QueueTryFindPlayer(ByVal username As String) As IFuture(Of Player)
            Contract.Requires(username IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Player))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncTryFindPlayer(username)).Defuturized
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
    End Class
End Namespace
