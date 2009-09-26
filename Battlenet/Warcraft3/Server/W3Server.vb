Imports System.Net.Sockets
Imports HostBot.Bnet
Imports HostBot.Warcraft3.W3Server
Imports HostBot.Links

Namespace Warcraft3
    Public NotInheritable Class W3Server
        Inherits FutureDisposable
#Region "Properties"
        Public ReadOnly parent As MainBot
        Public ReadOnly name As String
        Public ReadOnly settings As ServerSettings

        Private ReadOnly games_all As New List(Of IW3Game)
        Private ReadOnly games_lobby As New List(Of IW3Game)
        Private ReadOnly games_load_screen As New List(Of IW3Game)
        Private ReadOnly games_gameplay As New List(Of IW3Game)

        Private ReadOnly door As W3ServerDoor
        Public ReadOnly logger As Logger

        Private ReadOnly ref As ICallQueue
        Private ReadOnly eref As ICallQueue

        Public Event ChangedState(ByVal sender As W3Server, ByVal oldState As W3ServerStates, ByVal newState As W3ServerStates)
        Public Event AddedGame(ByVal sender As W3Server, ByVal game As IW3Game)
        Public Event RemovedGame(ByVal sender As W3Server, ByVal game As IW3Game)
        Public Event PlayerTalked(ByVal sender As W3Server, ByVal game As IW3Game, ByVal player As W3Player, ByVal text As String)
        Public Event PlayerLeft(ByVal sender As W3Server, ByVal game As IW3Game, ByVal game_state As W3GameStates, ByVal player As W3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String)
        Public Event PlayerSentData(ByVal sever As W3Server, ByVal game As IW3Game, ByVal player As W3Player, ByVal data As Byte())
        Public Event PlayerEntered(ByVal sender As W3Server, ByVal game As IW3Game, ByVal player As W3Player)

        Private total_instances_created_P As Integer = 0
        Private suffix As String
        Private _state As W3ServerStates = W3ServerStates.only_accepting
        Private ReadOnly Property state() As W3ServerStates
            Get
                Return _state
            End Get
        End Property
        Private Sub change_state(ByVal new_state As W3ServerStates)
            Dim old_state = _state
            _state = new_state
            suffix = "[" + new_state.ToString() + "]"
            e_ThrowStateChanged(old_state, new_state)
        End Sub
#End Region

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(parent IsNot Nothing)
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(settings IsNot Nothing)
            Contract.Invariant(games_all IsNot Nothing)
            Contract.Invariant(games_lobby IsNot Nothing)
            Contract.Invariant(games_load_screen IsNot Nothing)
            Contract.Invariant(games_gameplay IsNot Nothing)
            Contract.Invariant(door IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eref IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String,
                       ByVal parent As MainBot,
                       ByVal settings As ServerSettings,
                       Optional ByVal suffix As String = "",
                       Optional ByVal logger As Logger = Nothing)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(name IsNot Nothing)
            'Contract.Requires(parent IsNot Nothing)
            'Contract.Requires(settings IsNot Nothing)
            Contract.Assume(name IsNot Nothing)
            Contract.Assume(parent IsNot Nothing)
            Contract.Assume(settings IsNot Nothing)
            Try
                Me.settings = settings
                Me.parent = parent
                Me.name = name
                Me.suffix = suffix
                Me.logger = If(logger, New Logger)
                Me.door = New W3ServerDoor(Me, Me.logger)
                Me.eref = New ThreadPooledCallQueue
                Me.ref = New ThreadPooledCallQueue

                For Each port In settings.default_listen_ports
                    door.accepter.Accepter.OpenPort(port)
                Next port
                For i = 1 To settings.instances
                    CreateGame()
                Next i
                parent.logger.Log("Server started for map {0}.".Frmt(settings.map.RelativePath), LogMessageType.Positive)

                If settings.testFakePlayers AndAlso settings.default_listen_ports.Any Then
                    FutureWait(3.Seconds).CallWhenReady(
                        Sub()
                            For i = 1 To 3
                                Dim receivedPort = parent.portPool.TryAcquireAnyPort()
                                If receivedPort Is Nothing Then
                                    logger.Log("Failed to get port for fake player.", LogMessageType.Negative)
                                    Exit For
                                End If

                                Dim p = New W3DummyPlayer("Wait {0}min".Frmt(i), receivedPort, logger, W3DummyPlayer.Modes.EnterGame)
                                p.readyDelay = i.Minutes
                                Dim i_ = i
                                p.QueueConnect("localhost", settings.default_listen_ports.FirstOrDefault).CallWhenReady(
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

                If settings.grabMap Then
                    Dim server_port = settings.default_listen_ports.FirstOrDefault
                    If server_port = 0 Then
                        Throw New InvalidOperationException("Server has no port for Grab player to connect on.")
                    End If

                    Dim grabPort = parent.portPool.TryAcquireAnyPort()
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
        Private Sub e_ThrowStateChanged(ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
            eref.QueueAction(
                Sub()
                    RaiseEvent ChangedState(Me, old_state, new_state)
                End Sub
            )
        End Sub
        Private Sub e_ThrowAddedGame(ByVal game As IW3Game)
            eref.QueueAction(
                Sub()
                    RaiseEvent AddedGame(Me, game)
                End Sub
            )
        End Sub
        Private Sub e_ThrowRemovedGame(ByVal game As IW3Game)
            eref.QueueAction(
                Sub()
                    RaiseEvent RemovedGame(Me, game)
                End Sub
            )
        End Sub

        Private Sub c_PlayerTalked(ByVal game As IW3Game, ByVal player As W3Player, ByVal text As String)
            RaiseEvent PlayerTalked(Me, game, player, text)
        End Sub
        Private Sub c_PlayerSentData(ByVal game As IW3Game, ByVal player As W3Player, ByVal data As Byte())
            RaiseEvent PlayerSentData(Me, game, player, data)
        End Sub
        Private Sub c_PlayerLeft(ByVal game As IW3Game, ByVal game_state As W3GameStates, ByVal player As W3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String)
            logger.Log("{0} left game {1}. ({2})".Frmt(player.name, game.name, reason), LogMessageType.Negative)
            RaiseEvent PlayerLeft(Me, game, game_state, player, leaveType, reason)
        End Sub
        Private Sub c_PlayerEntered(ByVal game As IW3Game, ByVal player As W3Player)
            RaiseEvent PlayerEntered(Me, game, player)
        End Sub
        Private Sub c_GameStateChanged(ByVal sender As IW3Game, ByVal old_state As W3GameStates, ByVal new_state As W3GameStates)
            ref.QueueAction(
                Sub()
                    If Not games_all.Contains(sender) Then  Return

                    Select Case new_state
                        Case W3GameStates.Loading
                            logger.Log(sender.name + " has begun loading.", LogMessageType.Positive)
                            games_lobby.Remove(sender)
                            games_load_screen.Add(sender)
                        Case W3GameStates.Playing
                            logger.Log(sender.name + " has started play.", LogMessageType.Positive)
                            games_load_screen.Remove(sender)
                            games_gameplay.Add(sender)
                        Case W3GameStates.Closed
                            logger.Log(sender.name + " has closed.", LogMessageType.Negative)
                            RemoveGame(sender.name)
                    End Select

                    'Advance from only_accepting if there is a game started
                    If state = W3ServerStates.only_accepting Then
                        If games_all.Count > games_lobby.Count Then
                            change_state(W3ServerStates.accepting_and_playing)
                        End If
                    End If

                    'Advance from accepting_and_playing if there are no more games accepting players
                    If state = W3ServerStates.accepting_and_playing AndAlso settings.instances > 0 Then
                        If games_lobby.Count = 0 Then
                            If settings.permanent Then
                                SetAdvertiserOptions(True)
                            Else
                                StopAcceptingPlayers()
                            End If
                        End If
                    End If

                    'Advance from only_playing_out if there are no more games being played
                    If state = W3ServerStates.only_playing_out Then
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
            If state > W3ServerStates.accepting_and_playing Then Return
            change_state(W3ServerStates.only_playing_out)

            door.Reset()
            For Each game In games_lobby.ToList
                RemoveGame(game.name)
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
            If state >= W3ServerStates.killed Then
                Return
            End If

            For Each game In games_all.ToList
                RemoveGame(game.name)
            Next game
            games_all.Clear()
            door.Reset()

            For Each adv In linkedAdvertisers
                adv.RemoveGame(Me.settings.header, "Server killed.")
            Next adv

            change_state(W3ServerStates.killed)
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
                                    Optional ByVal arguments As IEnumerable(Of String) = Nothing) As IW3Game
            gameName = If(gameName, total_instances_created_P.ToString())
            If state > W3ServerStates.accepting_and_playing Then
                Throw New InvalidOperationException("No longer accepting players. Can't create new instances.")
            End If
            Dim game = FindGame(gameName)
            If game IsNot Nothing Then
                Throw New InvalidOperationException("A game called '{0}' already exists.".Frmt(gameName))
            End If

            game = New W3Game(Me, gameName, settings.map, If(arguments, settings.header.Options))
            logger.Log(game.name + " opened.", LogMessageType.Positive)
            total_instances_created_P += 1
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
        Private Function FindGame(ByVal game_name As String) As IW3Game
            Return (From game In games_all Where game.name.ToLower() = game_name.ToLower()).FirstOrDefault
        End Function

        '''<summary>Finds a player with the given name in any of the server's games.</summary>
        Private Function f_FindPlayer(ByVal username As String) As IFuture(Of W3Player)
            Dim futureFoundPlayers = (From game In games_all Select game.QueueFindPlayer(username)).ToList.Defuturized
            Return futureFoundPlayers.Select(
                Function(foundPlayers)
                    Return (From player In foundPlayers Where player IsNot Nothing).FirstOrDefault
                End Function
            )
        End Function

        '''<summary>Finds a game containing a player with the given name.</summary>
        Private Function f_FindPlayerGame(ByVal username As String) As IFuture(Of IW3Game)
            Return games_lobby.ToList.
                   FutureSelect(Function(game) game.QueueFindPlayer(username).Select(Function(player) player IsNot Nothing))
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
                                           state < W3ServerStates.only_playing_out Then
                CreateGame()
            End If
        End Sub
#End Region

#Region "Link"
        Private ReadOnly linkedAdvertisers As New HashSet(Of IGameSourceSink)
        Private Sub AddAdvertiser(ByVal m As IGameSourceSink)
            If state > W3ServerStates.accepting_and_playing Then Throw New InvalidOperationException("Not accepting players anymore.")
            If linkedAdvertisers.Contains(m) Then Throw New InvalidOperationException("Already have that advertiser.")
            AddHandler m.RemovedGame, AddressOf c_AdvertiserRemovedGame
            linkedAdvertisers.Add(m)
        End Sub
        Private Sub SetAdvertiserOptions(ByVal [private] As Boolean)
            For Each m In linkedAdvertisers
                m.SetAdvertisingOptions([private])
            Next m
        End Sub

        Private Sub c_AdvertiserRemovedGame(ByVal _m As IGameSource, ByVal header As W3GameHeader, ByVal reason As String)
            If header IsNot settings.header Then Return
            Dim m = CType(_m, IGameSourceSink)
            ref.QueueAction(
                Sub()
                    If Not linkedAdvertisers.Contains(m) Then  Return
                    RemoveHandler m.RemovedGame, AddressOf c_AdvertiserRemovedGame
                    linkedAdvertisers.Remove(m)
                End Sub
            )
        End Sub
        Private Class AdvertisingDependency
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
                                             ByVal oldState As W3ServerStates,
                                             ByVal newState As W3ServerStates) Handles server.ChangedState
                If oldState <= W3ServerStates.accepting_and_playing And newState > W3ServerStates.accepting_and_playing Then
                    Dispose()
                End If
            End Sub
        End Class
#End Region

#Region "Interface"
        Public Function QueueFindGame(ByVal gameName As String) As IFuture(Of IW3Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Game))() IsNot Nothing)
            Return ref.QueueFunc(Function() FindGame(gameName))
        End Function
        Public Function QueueFindPlayer(ByVal username As String) As IFuture(Of W3Player)
            Contract.Ensures(Contract.Result(Of IFuture(Of W3Player))() IsNot Nothing)
            Return ref.QueueFunc(Function() f_FindPlayer(username)).Defuturized
        End Function
        Public Function QueueFindPlayerGame(ByVal username As String) As IFuture(Of IW3Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Game))() IsNot Nothing)
            Return ref.QueueFunc(Function() f_FindPlayerGame(username)).Defuturized
        End Function
        Public Function QueueGetGames() As IFuture(Of IEnumerable(Of IW3Game))
            Contract.Ensures(Contract.Result(Of IFuture(Of IEnumerable(Of IW3Game)))() IsNot Nothing)
            Return ref.QueueFunc(Function() CType(games_all.ToList, IEnumerable(Of IW3Game)))
        End Function
        Public Function QueueCreateGame(Optional ByVal gameName As String = Nothing) As IFuture(Of IW3Game)
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Game))() IsNot Nothing)
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
        Public Function QueueAddAvertiser(ByVal m As IGameSourceSink) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Return ref.QueueAction(Sub() AddAdvertiser(m))
        End Function
        Public Function CreateAdvertisingDependency() As FutureDisposable
            Contract.Ensures(Contract.Result(Of FutureDisposable)() IsNot Nothing)
            Return New AdvertisingDependency(Me)
        End Function
        Public ReadOnly Property GetSuffix As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return suffix
            End Get
        End Property
#End Region
    End Class
End Namespace
