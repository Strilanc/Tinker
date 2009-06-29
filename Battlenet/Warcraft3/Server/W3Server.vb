Imports System.Net.Sockets
Imports HostBot.Bnet
Imports HostBot.Warcraft3.W3Server
Imports HostBot.Links

Namespace Warcraft3
    Public NotInheritable Class W3Server
        Implements IW3Server
#Region "Properties"
        Private ReadOnly parent As MainBot
        Private ReadOnly name As String
        Private ReadOnly settings As ServerSettings

        Private ReadOnly games_all As New List(Of IW3Game)
        Private ReadOnly games_lobby As New List(Of IW3Game)
        Private ReadOnly games_load_screen As New List(Of IW3Game)
        Private ReadOnly games_gameplay As New List(Of IW3Game)

        Private ReadOnly door As W3ServerDoor
        Private ReadOnly logger As Logger

        Private ReadOnly ref As ICallQueue
        Private ReadOnly eref As ICallQueue

        Private Event ChangedState(ByVal sender As IW3Server, ByVal oldState As W3ServerStates, ByVal newState As W3ServerStates) Implements IW3Server.ChangedState
        Private Event AddedGame(ByVal sender As IW3Server, ByVal game As IW3Game) Implements IW3Server.AddedGame
        Private Event RemovedGame(ByVal sender As IW3Server, ByVal game As IW3Game) Implements IW3Server.RemovedGame
        Private Event PlayerTalked(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player, ByVal text As String) Implements IW3Server.PlayerTalked
        Private Event PlayerLeft(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal game_state As W3GameStates, ByVal player As IW3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) Implements IW3Server.PlayerLeft
        Private Event PlayerSentData(ByVal sever As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player, ByVal data As Byte()) Implements IW3Server.PlayerSentData
        Private Event PlayerEntered(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player) Implements IW3Server.PlayerEntered
        Private Event Closed() Implements INotifyingDisposable.Disposed

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

        <ContractInvariantMethod()> Protected Sub Invariant()
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
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(parent IsNot Nothing)
            Contract.Requires(settings IsNot Nothing)
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
                    Dim out = door.accepter.accepter.OpenPort(port)
                    If Not out.succeeded Then Throw New InvalidOperationException(out.Message)
                Next port
                For i = 1 To settings.instances
                    CreateGame()
                Next i
                parent.logger.log("Server started for map {0}.".frmt(settings.map.RelativePath), LogMessageTypes.Positive)
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

        Private Sub c_PlayerTalked(ByVal game As IW3Game, ByVal player As IW3Player, ByVal text As String)
            RaiseEvent PlayerTalked(Me, game, player, text)
        End Sub
        Private Sub c_PlayerSentData(ByVal game As IW3Game, ByVal player As IW3Player, ByVal data As Byte())
            RaiseEvent PlayerSentData(Me, game, player, data)
        End Sub
        Private Sub c_PlayerLeft(ByVal game As IW3Game, ByVal game_state As W3GameStates, ByVal player As IW3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String)
            logger.log("{0} left game {1}. ({2})".frmt(player.name, game.name, reason), LogMessageTypes.Negative)
            RaiseEvent PlayerLeft(Me, game, game_state, player, leaveType, reason)
        End Sub
        Private Sub c_PlayerEntered(ByVal game As IW3Game, ByVal player As IW3Player)
            RaiseEvent PlayerEntered(Me, game, player)
        End Sub
        Private Sub c_GameStateChanged(ByVal sender As IW3Game, ByVal old_state As W3GameStates, ByVal new_state As W3GameStates)
            ref.QueueAction(
                Sub()
                    If Not games_all.Contains(sender) Then  Return

                    Select Case new_state
                        Case W3GameStates.Loading
                            logger.log(sender.name + " has begun loading.", LogMessageTypes.Positive)
                            games_lobby.Remove(sender)
                            games_load_screen.Add(sender)
                        Case W3GameStates.Playing
                            logger.log(sender.name + " has started play.", LogMessageTypes.Positive)
                            games_load_screen.Remove(sender)
                            games_gameplay.Add(sender)
                        Case W3GameStates.Closed
                            logger.log(sender.name + " has closed.", LogMessageTypes.Negative)
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
        Private Function StopAcceptingPlayers() As Outcome
            If state > W3ServerStates.accepting_and_playing Then Return success("Already not accepting players.")
            change_state(W3ServerStates.only_playing_out)

            door.Reset()
            For Each game In games_lobby.ToList
                RemoveGame(game.name)
            Next game

            If games_all.Any Then
                For Each adv In linkedAdvertisers
                    adv.RemoveGame(Me.settings.header, "Server no longer accepting players.")
                Next adv
                Return success("No longer accepting players.")
            Else
                Return Kill()
            End If
        End Function

        '''<summary>Stops listening for connections, kills all instances, and shuts down the server.</summary>
        Private Function Kill() As Outcome
            If state >= W3ServerStates.killed Then
                Return success("Server is already killed.")
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
            RaiseEvent Closed()
            parent.f_RemoveServer(Me.name)
            Return success("Server killed.")
        End Function
        Public ReadOnly Property IsDisposed As Boolean Implements INotifyingDisposable.IsDisposed
            Get
                Return state >= W3ServerStates.killed
            End Get
        End Property
#End Region

#Region "Games"
        '''<summary>Adds a game to the server.</summary>
        Private Function CreateGame(Optional ByVal game_name As String = Nothing,
                                    Optional ByVal arguments As IEnumerable(Of String) = Nothing) As Outcome(Of IW3Game)
            game_name = If(game_name, total_instances_created_P.ToString())
            If state > W3ServerStates.accepting_and_playing Then
                Return failure("No longer accepting players. Can't create new instances.")
            End If
            Dim game = FindGame(game_name)
            If game IsNot Nothing Then
                Return failure("A game called '{0}' already exists.".frmt(game_name))
            End If

            game = New W3Game(Me, game_name, settings.map, If(arguments, settings.header.options))
            logger.log(game.name + " opened.", LogMessageTypes.Positive)
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
            Return successVal(game, "Succesfully created instance '{0}'.".frmt(game_name))
        End Function

        '''<summary>Finds a game with the given name.</summary>
        Private Function FindGame(ByVal game_name As String) As IW3Game
            Return (From game In games_all Where game.name.ToLower() = game_name.ToLower()).FirstOrDefault
        End Function

        '''<summary>Finds a player with the given name in any of the server's games.</summary>
        Private Function f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player)
            Return games_all.ToList.FutureMap(Function(game) game.f_FindPlayer(username)).EvalWhenValueReady(
                                   Function(players) players.FirstOrDefault)
        End Function

        '''<summary>Finds a game containing a player with the given name.</summary>
        Private Function f_FindPlayerGame(ByVal username As String) As IFuture(Of Outcome(Of IW3Game))
            Return FutureSelect(games_lobby.ToList,
                                Function(game) game.f_FindPlayer(username).EvalWhenValueReady(
                                                               Function(player) player IsNot Nothing))
        End Function

        '''<summary>Removes a game with the given name.</summary>
        Private Function RemoveGame(ByVal game_name As String, Optional ByVal ignorePermanent As Boolean = False) As Outcome
            Dim game = FindGame(game_name)
            If game Is Nothing Then Return failure("No game with that name.")

            RemoveHandler game.PlayerTalked, AddressOf c_PlayerTalked
            RemoveHandler game.PlayerLeft, AddressOf c_PlayerLeft
            RemoveHandler game.ChangedState, AddressOf c_GameStateChanged
            RemoveHandler game.PlayerEntered, AddressOf c_PlayerEntered
            RemoveHandler game.PlayerSentData, AddressOf c_PlayerSentData

            games_all.Remove(game)
            games_lobby.Remove(game)
            games_load_screen.Remove(game)
            games_gameplay.Remove(game)
            game.f_Close()
            e_ThrowRemovedGame(game)

            If Not ignorePermanent AndAlso settings.permanent AndAlso
                                           settings.instances > 0 AndAlso
                                           state < W3ServerStates.only_playing_out Then
                CreateGame()
            End If

            Return success("Game '{0}' removed from server '{1}'.".frmt(game.name, game_name))
        End Function
#End Region

#Region "Link"
        Private ReadOnly linkedAdvertisers As New HashSet(Of IGameSourceSink)
        Private Function AddAdvertiser(ByVal m As IGameSourceSink) As outcome
            If state > W3ServerStates.accepting_and_playing Then Return failure("Not accepting players anymore.")
            If linkedAdvertisers.Contains(m) Then Return success("Already have that advertiser.")
            AddHandler m.RemovedGame, AddressOf c_AdvertiserRemovedGame
            linkedAdvertisers.Add(m)
            Return success("Added advertiser.")
        End Function
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
            Inherits NotifyingDisposable
            Private WithEvents server As IW3Server

            Public Sub New(ByVal server As IW3Server)
                Contract.Requires(server IsNot Nothing)
                Me.server = server
            End Sub

            Protected Overrides Sub PerformDispose()
                server.f_StopAcceptingPlayers()
                server = Nothing
            End Sub

            Private Sub c_ServerStateChanged(ByVal sender As IW3Server,
                                             ByVal oldState As W3ServerStates,
                                             ByVal newState As W3ServerStates) Handles server.ChangedState
                If oldState <= W3ServerStates.accepting_and_playing And newState > W3ServerStates.accepting_and_playing Then
                    Dispose()
                End If
            End Sub
        End Class
#End Region

#Region "Interface"
        Private Function _f_FindGame(ByVal gameName As String) As IFuture(Of IW3Game) Implements IW3Server.f_FindGame
            Return ref.QueueFunc(Function() FindGame(gameName))
        End Function
        Private Function _f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player) Implements IW3Server.f_FindPlayer
            Return ref.QueueFunc(Function() f_FindPlayer(username)).Defuturize
        End Function
        Private Function _f_FindPlayerGame(ByVal username As String) As IFuture(Of Outcome(Of IW3Game)) Implements IW3Server.f_FindPlayerGame
            Return ref.QueueFunc(Function() f_FindPlayerGame(username)).Defuturize
        End Function
        Private Function _f_EnumGames() As IFuture(Of IEnumerable(Of IW3Game)) Implements IW3Server.f_EnumGames
            Return ref.QueueFunc(Function() CType(games_all.ToList, IEnumerable(Of IW3Game)))
        End Function
        Private Function _f_CreateGame(Optional ByVal gameName As String = Nothing) As IFuture(Of Outcome(Of IW3Game)) Implements IW3Server.f_CreateGame
            Return ref.QueueFunc(Function() CreateGame(gameName))
        End Function
        Private Function _f_RemoveGame(ByVal gameName As String, Optional ByVal ignorePermanent As Boolean = False) As IFuture(Of Outcome) Implements IW3Server.f_RemoveGame
            Return ref.QueueFunc(Function() RemoveGame(gameName, ignorePermanent))
        End Function
        Private Function _f_ClosePort(ByVal port As UShort) As IFuture(Of Outcome) Implements IW3Server.f_ClosePort
            Return ref.QueueFunc(Function() door.accepter.accepter.ClosePort(port))
        End Function
        Private Function _f_OpenPort(ByVal port As UShort) As IFuture(Of Outcome) Implements IW3Server.f_OpenPort
            Return ref.QueueFunc(Function() door.accepter.accepter.OpenPort(port))
        End Function
        Private Function _f_CloseAllPorts() As IFuture(Of Outcome) Implements IW3Server.f_CloseAllPorts
            Return ref.QueueFunc(Function() door.accepter.accepter.CloseAllPorts())
        End Function
        Private Function _f_StopAcceptingPlayers() As IFuture(Of Outcome) Implements IW3Server.f_StopAcceptingPlayers
            Return ref.QueueFunc(Function() StopAcceptingPlayers())
        End Function
        Private Sub _servant_close() Implements INotifyingDisposable.Dispose
            ref.QueueAction(Function() Kill())
        End Sub
        Private Function _f_Kill() As IFuture(Of Outcome) Implements IW3Server.f_Kill
            Return ref.QueueFunc(Function() Kill())
        End Function
        Private Function _add_advertiser_R(ByVal m As IGameSourceSink) As IFuture(Of outcome) Implements IW3Server.f_AddAvertiser
            Return ref.QueueFunc(Function() AddAdvertiser(m))
        End Function
        Private Function _advertising_dep() As INotifyingDisposable Implements IW3Server.CreateAdvertisingDependency
            Return New AdvertisingDependency(Me)
        End Function
        Private ReadOnly Property _logger() As Logger Implements IW3Server.logger
            Get
                Return logger
            End Get
        End Property
        Private ReadOnly Property _name() As String Implements IW3Server.name
            Get
                Return name
            End Get
        End Property
        Private ReadOnly Property _parent() As MainBot Implements IW3Server.parent
            Get
                Return parent
            End Get
        End Property
        Private ReadOnly Property _settings() As ServerSettings Implements IW3Server.settings
            Get
                Return settings
            End Get
        End Property
        Private ReadOnly Property _suffix() As String Implements IW3Server.suffix
            Get
                Return suffix
            End Get
        End Property
#End Region
    End Class
End Namespace
