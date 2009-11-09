Imports System.Net.Sockets
Imports HostBot.Bnet
Imports HostBot.Warcraft3.W3Server
Imports HostBot.Links

Namespace Warcraft3
    Public NotInheritable Class W3Server
        Inherits FutureDisposable
#Region "Properties"
        Private ReadOnly _parent As MainBot
        Private ReadOnly _name As String
        Private ReadOnly _settings As ServerSettings

        Private ReadOnly games_all As New List(Of W3Game)
        Private ReadOnly games_lobby As New List(Of W3Game)
        Private ReadOnly games_load_screen As New List(Of W3Game)
        Private ReadOnly games_gameplay As New List(Of W3Game)

        Private ReadOnly door As W3ServerDoor
        Public ReadOnly logger As Logger

        Private ReadOnly ref As ICallQueue
        Private ReadOnly eref As ICallQueue

        Public Event ChangedState(ByVal sender As W3Server, ByVal oldState As W3ServerState, ByVal newState As W3ServerState)
        Public Event AddedGame(ByVal sender As W3Server, ByVal game As W3Game)
        Public Event RemovedGame(ByVal sender As W3Server, ByVal game As W3Game)
        Public Event PlayerTalked(ByVal sender As W3Server, ByVal game As W3Game, ByVal player As W3Player, ByVal text As String)
        Public Event PlayerLeft(ByVal sender As W3Server, ByVal game As W3Game, ByVal gameState As W3GameState, ByVal player As W3Player, ByVal leaveType As W3PlayerLeaveType, ByVal reason As String)
        Public Event PlayerSentData(ByVal sever As W3Server, ByVal game As W3Game, ByVal player As W3Player, ByVal data As Byte())
        Public Event PlayerEntered(ByVal sender As W3Server, ByVal game As W3Game, ByVal player As W3Player)

        Private instanceCreationCount As Integer
        Private _suffix As String
        Private _state As W3ServerState = W3ServerState.OnlyAcceptingPlayers
        Public ReadOnly Property Parent() As MainBot
            Get
                Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                Return _parent
            End Get
        End Property
        Private ReadOnly Property state() As W3ServerState
            Get
                Return _state
            End Get
        End Property
        Public ReadOnly Property Name As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property
        Public ReadOnly Property Settings As ServerSettings
            Get
                Contract.Ensures(Contract.Result(Of ServerSettings)() IsNot Nothing)
                Return _settings
            End Get
        End Property
        Private Sub change_state(ByVal new_state As W3ServerState)
            Dim old_state = _state
            _state = new_state
            _suffix = "[" + new_state.ToString() + "]"
            e_ThrowStateChanged(old_state, new_state)
        End Sub
#End Region

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_parent IsNot Nothing)
            Contract.Invariant(_name IsNot Nothing)
            Contract.Invariant(_settings IsNot Nothing)
            Contract.Invariant(games_all IsNot Nothing)
            Contract.Invariant(games_lobby IsNot Nothing)
            Contract.Invariant(games_load_screen IsNot Nothing)
            Contract.Invariant(games_gameplay IsNot Nothing)
            Contract.Invariant(door IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eref IsNot Nothing)
            Contract.Invariant(_suffix IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String,
                       ByVal parent As MainBot,
                       ByVal settings As ServerSettings,
                       Optional ByVal suffix As String = "",
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(name IsNot Nothing) 'bug in contracts required not using requires here
            Contract.Assume(parent IsNot Nothing)
            Contract.Assume(settings IsNot Nothing)
            Try
                Me._settings = settings
                Me._parent = parent
                Me._name = name
                Me._suffix = suffix
                Me.logger = If(logger, New Logger)
                Me.door = New W3ServerDoor(Me, Me.logger)
                Me.eref = New ThreadPooledCallQueue
                Me.ref = New ThreadPooledCallQueue

                For Each port In Me.settings.defaultListenPorts
                    door.accepter.Accepter.OpenPort(port)
                Next port
                For i = 1 To Me.settings.instances
                    CreateGame()
                Next i
                Me.parent.logger.Log("Server started for map {0}.".Frmt(Me.settings.map.RelativePath), LogMessageType.Positive)

                If Me.Settings.grabMap Then
                    Dim serverPort = Me.Settings.defaultListenPorts.FirstOrDefault
                    If serverPort = 0 Then
                        Throw New InvalidOperationException("Server has no port for Grab player to connect on.")
                    End If

                    Dim grabPort = parent.PortPool.TryAcquireAnyPort()
                    If grabPort Is Nothing Then
                        Throw New InvalidOperationException("Failed to get port from pool for Grab player to listen on.")
                    End If

                    FutureWait(3.Seconds).CallWhenReady(
                        Sub()
                            Dim p = New W3DummyPlayer("Grab", grabPort, logger)
                            p.QueueConnect("localhost", serverPort)
                        End Sub
                    )
                End If

                If Me.Settings.testFakePlayers AndAlso Me.Settings.defaultListenPorts.Any Then
                    FutureWait(3.Seconds).CallWhenReady(
                        Sub()
                            For i = 1 To 3
                                Dim receivedPort = Me.Parent.PortPool.TryAcquireAnyPort()
                                If receivedPort Is Nothing Then
                                    logger.Log("Failed to get port for fake player.", LogMessageType.Negative)
                                    Exit For
                                End If

                                Dim p = New W3DummyPlayer("Wait {0}min".Frmt(i), receivedPort, logger, DummyPlayerMode.EnterGame)
                                p.readyDelay = i.Minutes
                                Dim i_ = i
                                p.QueueConnect("localhost", Me.Settings.defaultListenPorts.FirstOrDefault).CallWhenReady(
                                    Sub(exception)
                                        If exception Is Nothing Then
                                            Me.logger.Log("Fake player {0} Connected", LogMessageType.Positive)
                                        Else
                                            Me.logger.Log("Fake player {0}: {1}".Frmt(i_, exception.Message), LogMessageType.Negative)
                                        End If
                                    End Sub)
                                        Next i
                                    End Sub)
                End If

                If Me.settings.grabMap Then
                    Dim server_port = Me.settings.defaultListenPorts.FirstOrDefault
                    If server_port = 0 Then
                        Throw New InvalidOperationException("Server has no port for Grab player to connect on.")
                    End If

                    Dim grabPort = Me.parent.portPool.TryAcquireAnyPort()
                    If grabPort Is Nothing Then
                        Throw New InvalidOperationException("Failed to get port from pool for Grab player to listen on.")
                    End If

                    FutureWait(3.Seconds).CallWhenReady(
                        Sub()
                            Dim p = New W3DummyPlayer("Grab", grabPort, logger)
                            p.QueueConnect("localhost", server_port)
                        End Sub)
                End If
            Catch e As Exception
                door.Reset()
                Throw
            End Try
        End Sub

#Region "Events"
        Private Sub e_ThrowStateChanged(ByVal old_state As W3ServerState, ByVal new_state As W3ServerState)
            eref.QueueAction(
                Sub()
                    RaiseEvent ChangedState(Me, old_state, new_state)
                End Sub
            )
        End Sub
        Private Sub e_ThrowAddedGame(ByVal game As W3Game)
            eref.QueueAction(
                Sub()
                    RaiseEvent AddedGame(Me, game)
                End Sub
            )
        End Sub
        Private Sub e_ThrowRemovedGame(ByVal game As W3Game)
            eref.QueueAction(
                Sub()
                    RaiseEvent RemovedGame(Me, game)
                End Sub
            )
        End Sub

        Private Sub c_PlayerTalked(ByVal game As W3Game, ByVal player As W3Player, ByVal text As String)
            RaiseEvent PlayerTalked(Me, game, player, text)
        End Sub
        Private Sub c_PlayerSentData(ByVal game As W3Game, ByVal player As W3Player, ByVal data As Byte())
            RaiseEvent PlayerSentData(Me, game, player, data)
        End Sub
        Private Sub c_PlayerLeft(ByVal game As W3Game, ByVal game_state As W3GameState, ByVal player As W3Player, ByVal leaveType As W3PlayerLeaveType, ByVal reason As String)
            logger.Log("{0} left game {1}. ({2})".Frmt(player.name, game.Name, reason), LogMessageType.Negative)
            RaiseEvent PlayerLeft(Me, game, game_state, player, leaveType, reason)
        End Sub
        Private Sub c_PlayerEntered(ByVal game As W3Game, ByVal player As W3Player)
            RaiseEvent PlayerEntered(Me, game, player)
        End Sub
        Private Sub c_GameStateChanged(ByVal sender As W3Game, ByVal old_state As W3GameState, ByVal new_state As W3GameState)
            ref.QueueAction(
                Sub()
                    If Not games_all.Contains(sender) Then Return

                    Select Case new_state
                        Case W3GameState.Loading
                            logger.Log(sender.Name + " has begun loading.", LogMessageType.Positive)
                            games_lobby.Remove(sender)
                            games_load_screen.Add(sender)
                        Case W3GameState.Playing
                            logger.Log(sender.Name + " has started play.", LogMessageType.Positive)
                            games_load_screen.Remove(sender)
                            games_gameplay.Add(sender)
                        Case W3GameState.Closed
                            logger.Log(sender.Name + " has closed.", LogMessageType.Negative)
                            RemoveGame(sender.Name)
                    End Select

                    'Advance from only_accepting if there is a game started
                    If state = W3ServerState.OnlyAcceptingPlayers Then
                        If games_all.Count > games_lobby.Count Then
                            change_state(W3ServerState.AcceptingPlayersAndPlayingGames)
                        End If
                    End If

                    'Advance from accepting_and_playing if there are no more games accepting players
                    If state = W3ServerState.AcceptingPlayersAndPlayingGames AndAlso settings.instances > 0 Then
                        If games_lobby.Count = 0 Then
                            If settings.permanent Then
                                SetAdvertiserOptions(True)
                            Else
                                StopAcceptingPlayers()
                            End If
                        End If
                    End If

                    'Advance from only_playing_out if there are no more games being played
                    If state = W3ServerState.OnlyPlayingGames Then
                        If games_all.Count = 0 Then
                            Kill()
                        End If
                    End If
                End Sub
            )
        End Sub
#End Region

#Region "Access"
        '''<summary>Stops listening for connections and kills all non-started instances.</summary>
        Private Sub StopAcceptingPlayers()
            If state > W3ServerState.AcceptingPlayersAndPlayingGames Then Return
            change_state(W3ServerState.OnlyPlayingGames)

            door.Reset()
            For Each game In games_lobby.ToList
                RemoveGame(game.Name)
            Next game

            If games_all.Any Then
                For Each adv In linkedAdvertisers
                    adv.RemoveGame(Me.settings.header, "Server no longer accepting players.")
                Next adv
            Else
                Kill()
            End If
        End Sub

        '''<summary>Stops listening for connections, kills all instances, and shuts down the server.</summary>
        Private Sub Kill()
            If state >= W3ServerState.Disposed Then
                Return
            End If

            For Each game In games_all.ToList
                RemoveGame(game.Name)
            Next game
            games_all.Clear()
            door.Reset()

            For Each adv In linkedAdvertisers
                adv.RemoveGame(Me.settings.header, "Server killed.")
            Next adv

            change_state(W3ServerState.Disposed)
            Me.Dispose()
            parent.QueueRemoveServer(Me.name)
        End Sub
        Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
            If Not finalizing Then
                ref.QueueAction(Sub() Kill())
            End If
        End Sub
#End Region

#Region "Games"
        '''<summary>Adds a game to the server.</summary>
        Private Function CreateGame(Optional ByVal gameName As String = Nothing,
                                    Optional ByVal arguments As IEnumerable(Of String) = Nothing) As W3Game
            gameName = If(gameName, instanceCreationCount.ToString(CultureInfo.InvariantCulture))
            If state > W3ServerState.AcceptingPlayersAndPlayingGames Then
                Throw New InvalidOperationException("No longer accepting players. Can't create new instances.")
            End If
            Dim game = FindGame(gameName)
            If game IsNot Nothing Then
                Throw New InvalidOperationException("A game called '{0}' already exists.".Frmt(gameName))
            End If

            game = New W3Game(gameName, Settings.Map, Settings)
            logger.Log(game.Name + " opened.", LogMessageType.Positive)
            instanceCreationCount += 1
            games_all.Add(game)
            games_lobby.Add(game)

            AddHandler game.PlayerTalked, AddressOf c_PlayerTalked
            AddHandler game.PlayerLeft, AddressOf c_PlayerLeft
            AddHandler game.ChangedState, AddressOf c_GameStateChanged
            AddHandler game.PlayerEntered, AddressOf c_PlayerEntered
            AddHandler game.PlayerSentData, AddressOf c_PlayerSentData

            SetAdvertiserOptions(private:=False)
            e_ThrowAddedGame(game)
            Return game
        End Function

        '''<summary>Finds a game with the given name.</summary>
        Private Function FindGame(ByVal game_name As String) As W3Game
            Return (From game In games_all Where game.Name.ToUpperInvariant = game_name.ToUpperInvariant).FirstOrDefault
        End Function

        '''<summary>Finds a player with the given name in any of the server's games.</summary>
        Private Function f_FindPlayer(ByVal username As String) As IFuture(Of W3Player)
            Dim futureFoundPlayers = (From game In games_all
                                      Select game.QueueFindPlayer(username)
                                      ).ToList
            Return futureFoundPlayers.Defuturized.Select(
                Function(foundPlayers) (From player In foundPlayers
                                        Where player IsNot Nothing
                                        ).FirstOrDefault
            )
        End Function

        '''<summary>Finds a game containing a player with the given name.</summary>
        Private Function f_FindPlayerGame(ByVal username As String) As IFuture(Of W3Game)
            Return games_lobby.ToList.
                   FutureSelect(Function(game) game.QueueFindPlayer(username).
                                                    Select(Function(player) player IsNot Nothing))
        End Function

        '''<summary>Removes a game with the given name.</summary>
        Private Sub RemoveGame(ByVal gameName As String,
                               Optional ByVal ignorePermanent As Boolean = False)
            Dim game = FindGame(gameName)
            If game Is Nothing Then Throw New InvalidOperationException("No game with that name.")

            RemoveHandler game.PlayerTalked, AddressOf c_PlayerTalked
            RemoveHandler game.PlayerLeft, AddressOf c_PlayerLeft
            RemoveHandler game.ChangedState, AddressOf c_GameStateChanged
            RemoveHandler game.PlayerEntered, AddressOf c_PlayerEntered
            RemoveHandler game.PlayerSentData, AddressOf c_PlayerSentData

            games_all.Remove(game)
            games_lobby.Remove(game)
            games_load_screen.Remove(game)
            games_gameplay.Remove(game)
            game.QueueClose()
            e_ThrowRemovedGame(game)

            If Not ignorePermanent AndAlso settings.permanent AndAlso
                                           settings.instances > 0 AndAlso
                                           state < W3ServerState.OnlyPlayingGames Then
                CreateGame()
            End If
        End Sub
#End Region

#Region "Link"
        Private ReadOnly linkedAdvertisers As New HashSet(Of IGameSourceSink)
        Private Sub AddAdvertiser(ByVal m As IGameSourceSink)
            If state > W3ServerState.AcceptingPlayersAndPlayingGames Then Throw New InvalidOperationException("Not accepting players anymore.")
            If linkedAdvertisers.Contains(m) Then Throw New InvalidOperationException("Already have that advertiser.")
            AddHandler m.RemovedGame, AddressOf c_AdvertiserRemovedGame
            linkedAdvertisers.Add(m)
        End Sub
        Private Sub SetAdvertiserOptions(ByVal [private] As Boolean)
            For Each m In linkedAdvertisers
                m.SetAdvertisingOptions([private])
            Next m
        End Sub

        Private Sub c_AdvertiserRemovedGame(ByVal _m As IGameSource, ByVal header As W3GameDescription, ByVal reason As String)
            If header IsNot settings.header Then Return
            Dim m = CType(_m, IGameSourceSink)
            ref.QueueAction(
                Sub()
                    If Not linkedAdvertisers.Contains(m) Then Return
                    RemoveHandler m.RemovedGame, AddressOf c_AdvertiserRemovedGame
                    linkedAdvertisers.Remove(m)
                End Sub
            )
        End Sub
        Private NotInheritable Class AdvertisingDependency
            Inherits FutureDisposable
            Private WithEvents server As W3Server

            Public Sub New(ByVal server As W3Server)
                'contract bug wrt interface event implementation requires this:
                'Contract.Requires(server IsNot Nothing)
                Contract.Assume(server IsNot Nothing)
                Me.server = server
            End Sub

            Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
                If Not finalizing Then
                    server.QueueStopAcceptingPlayers()
                    server = Nothing
                End If
            End Sub

            Private Sub c_ServerStateChanged(ByVal sender As W3Server,
                                             ByVal oldState As W3ServerState,
                                             ByVal newState As W3ServerState) Handles server.ChangedState
                If oldState <= W3ServerState.AcceptingPlayersAndPlayingGames And newState > W3ServerState.AcceptingPlayersAndPlayingGames Then
                    Dispose()
                End If
            End Sub
        End Class
#End Region

#Region "Interface"
        Public Function QueueFindGame(ByVal gameName As String) As IFuture(Of W3Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Game))() IsNot Nothing)
            Return ref.QueueFunc(Function() FindGame(gameName))
        End Function
        Public Function QueueFindPlayer(ByVal userName As String) As IFuture(Of W3Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Player))() IsNot Nothing)
            Return ref.QueueFunc(Function() f_FindPlayer(userName)).Defuturized
        End Function
        Public Function QueueFindPlayerGame(ByVal username As String) As IFuture(Of W3Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Game))() IsNot Nothing)
            Return ref.QueueFunc(Function() f_FindPlayerGame(username)).Defuturized
        End Function
        Public Function QueueGetGames() As IFuture(Of IEnumerable(Of W3Game))
            Contract.Ensures(Contract.Result(Of IFuture(Of IEnumerable(Of W3Game)))() IsNot Nothing)
            Return ref.QueueFunc(Function() CType(games_all.ToList, IEnumerable(Of W3Game)))
        End Function
        Public Function QueueCreateGame(Optional ByVal gameName As String = Nothing) As IFuture(Of W3Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Game))() IsNot Nothing)
            Return ref.QueueFunc(Function() CreateGame(gameName))
        End Function
        Public Function QueueRemoveGame(ByVal gameName As String, Optional ByVal ignorePermanent As Boolean = False) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() RemoveGame(gameName, ignorePermanent))
        End Function
        Public Function QueueClosePort(ByVal port As UShort) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() door.accepter.Accepter.ClosePort(port))
        End Function
        Public Function QueueOpenPort(ByVal port As UShort) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() door.accepter.Accepter.OpenPort(port))
        End Function
        Public Function QueueCloseAllPorts() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() door.accepter.Accepter.CloseAllPorts())
        End Function
        Public Function QueueStopAcceptingPlayers() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() StopAcceptingPlayers())
        End Function
        Public Function QueueKill() As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() Kill())
        End Function
        Public Function QueueAddAdvertiser(ByVal advertiser As IGameSourceSink) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() AddAdvertiser(advertiser))
        End Function
        Public Function CreateAdvertisingDependency() As FutureDisposable
            Contract.Ensures(Contract.Result(Of FutureDisposable)() IsNot Nothing)
            Return New AdvertisingDependency(Me)
        End Function
        Public ReadOnly Property Suffix As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _suffix
            End Get
        End Property
#End Region
    End Class
End Namespace
