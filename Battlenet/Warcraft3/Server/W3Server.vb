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
        Private ReadOnly logger As MultiLogger

        Private ReadOnly ref As ICallQueue
        Private ReadOnly eref As ICallQueue

        Private Event ChangedState(ByVal sender As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates) Implements IW3Server.ChangedState
        Private Event AddedGame(ByVal sender As IW3Server, ByVal game As IW3Game) Implements IW3Server.AddedGame
        Private Event RemovedGame(ByVal sender As IW3Server, ByVal game As IW3Game) Implements IW3Server.RemovedGame
        Private Event PlayerTalked(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player, ByVal text As String) Implements IW3Server.PlayerTalked
        Private Event PlayerLeft(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal game_state As W3GameStates, ByVal player As IW3Player, ByVal reason As W3PlayerLeaveTypes) Implements IW3Server.PlayerLeft
        Private Event PlayerSentData(ByVal sever As IW3Server, ByVal game As IW3GamePlay, ByVal player As IW3PlayerGameplay, ByVal data As Byte()) Implements IW3Server.PlayerSentData
        Private Event PlayerEntered(ByVal sender As IW3Server, ByVal game As IW3GameLobby, ByVal player As IW3PlayerLobby) Implements IW3Server.PlayerEntered
        Private Event Closed() Implements Links.IDependencyLinkMaster.Closed

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

#Region "New"
        Public Sub New(ByVal name As String, _
                       ByVal parent As MainBot, _
                       ByVal settings As ServerSettings, _
                       Optional ByVal suffix As String = "", _
                       Optional ByVal logger As MultiLogger = Nothing)
            Try
                Me.settings = ContractNotNull(settings, "settings")
                Me.parent = ContractNotNull(parent, "parent")
                Me.name = ContractNotNull(name, "name")
                Me.suffix = suffix
                Me.logger = If(logger, New MultiLogger)
                Me.door = New W3ServerDoor(Me, Me.logger)
                Me.eref = New ThreadedCallQueue("{0} {1} eref".frmt(Me.GetType.Name, name))
                Me.ref = New ThreadedCallQueue("{0} {1} ref".frmt(Me.GetType.Name, name))

                For Each port In settings.default_listen_ports
                    Dim out = door.accepter.accepter.OpenPort(port)
                    If out.outcome = Outcomes.failed Then Throw New InvalidOperationException(out.message)
                Next port
                For i = 1 To settings.instances
                    CreateGame()
                Next i
                parent.logger.log("Server started for map {0}.".frmt(settings.map.relative_path), LogMessageTypes.PositiveEvent)
            Catch e As Exception
                door.Reset()
                Throw
            End Try
        End Sub
#End Region

#Region "Events"
        Private Sub e_ThrowStateChanged(ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
            eref.enqueue(Function() eval(AddressOf _e_ThrowStateChanged, old_state, new_state))
        End Sub
        Private Sub e_ThrowAddedGame(ByVal game As IW3Game)
            eref.enqueue(Function() eval(AddressOf _e_ThrowAddedGame, game))
        End Sub
        Private Sub e_ThrowRemovedGame(ByVal game As IW3Game)
            eref.enqueue(Function() eval(AddressOf _e_ThrowRemovedGame, game))
        End Sub
        Private Sub _e_ThrowStateChanged(ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
            RaiseEvent ChangedState(Me, old_state, new_state)
        End Sub
        Private Sub _e_ThrowAddedGame(ByVal game As IW3Game)
            RaiseEvent AddedGame(Me, game)
        End Sub
        Private Sub _e_ThrowRemovedGame(ByVal game As IW3Game)
            RaiseEvent RemovedGame(Me, game)
        End Sub

        Private Sub c_PlayerTalked(ByVal game As IW3Game, ByVal player As IW3Player, ByVal text As String)
            RaiseEvent PlayerTalked(Me, game, player, text)
        End Sub
        Private Sub c_PlayerSentData(ByVal game As IW3GamePlay, ByVal player As IW3PlayerGameplay, ByVal data As Byte())
            RaiseEvent PlayerSentData(Me, game, player, data)
        End Sub
        Private Sub c_PlayerLeft(ByVal game As IW3Game, ByVal game_state As W3GameStates, ByVal player As IW3Player, ByVal reason As W3PlayerLeaveTypes)
            logger.log("{0} left game {1}".frmt(player.name, game.name), LogMessageTypes.NegativeEvent)
            RaiseEvent PlayerLeft(Me, game, game_state, player, reason)
        End Sub
        Private Sub c_PlayerEntered(ByVal game As IW3GameLobby, ByVal player As IW3PlayerLobby)
            RaiseEvent PlayerEntered(Me, game, player)
        End Sub
        Private Sub c_GameStateChanged(ByVal sender As IW3Game, ByVal old_state As W3GameStates, ByVal new_state As W3GameStates)
            ref.enqueue(Function() eval(AddressOf _c_GameStateChanged, sender, old_state, new_state))
        End Sub

        Private Sub _c_GameStateChanged(ByVal sender As IW3Game, ByVal old_state As W3GameStates, ByVal new_state As W3GameStates)
            If Not games_all.Contains(sender) Then Return

            Select Case new_state
                Case W3GameStates.Loading
                    logger.log(sender.name + " has begun loading.", LogMessageTypes.PositiveEvent)
                    games_lobby.Remove(sender)
                    games_load_screen.Add(sender)
                Case W3GameStates.Playing
                    logger.log(sender.name + " has started play.", LogMessageTypes.PositiveEvent)
                    games_load_screen.Remove(sender)
                    games_gameplay.Add(sender)
                Case W3GameStates.Closed
                    logger.log(sender.name + " has closed.", LogMessageTypes.NegativeEvent)
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
                        set_advertiser_options(True)
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
                For Each adv In link_advertisers
                    adv.stop_advertising("Server no longer accepting players.")
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

            For Each adv In link_advertisers
                adv.stop_advertising("Server killed.")
            Next adv

            change_state(W3ServerStates.killed)
            RaiseEvent Closed()
            parent.remove_server_R(Me.name)
            Return success("Server killed.")
        End Function
#End Region

#Region "Games"
        '''<summary>Adds a game to the server.</summary>
        Private Function CreateGame(Optional ByVal game_name As String = Nothing, _
                                    Optional ByVal arguments As IEnumerable(Of String) = Nothing) As Outcome(Of IW3Game)
            If game_name Is Nothing Then game_name = total_instances_created_P.ToString()
            If arguments Is Nothing Then arguments = settings.arguments
            If state > W3ServerStates.accepting_and_playing Then
                Return failure("No longer accepting players. Can't create new instances.")
            End If
            Dim game = FindGame(game_name)
            If game IsNot Nothing Then
                Return failure("A game called '{0}' already exists.".frmt(game_name))
            End If

            game = New W3Game(Me, game_name, settings.map, arguments)
            logger.log(game.name + " opened.", LogMessageTypes.PositiveEvent)
            total_instances_created_P += 1
            games_all.Add(game)
            games_lobby.Add(game)

            AddHandler game.PlayerTalked, AddressOf c_PlayerTalked
            AddHandler game.PlayerLeft, AddressOf c_PlayerLeft
            AddHandler game.ChangedState, AddressOf c_GameStateChanged
            AddHandler game.lobby.PlayerEntered, AddressOf c_PlayerEntered
            AddHandler game.gameplay.PlayerSentData, AddressOf c_PlayerSentData

            e_ThrowAddedGame(game)
            Return successVal(game, "Succesfully created instance '{0}'.".frmt(game_name))
        End Function

        '''<summary>Finds a game with the given name.</summary>
        Private Function FindGame(ByVal game_name As String) As IW3Game
            Return (From game In games_all Where game.name.ToLower() = game_name.ToLower()).FirstOrDefault
        End Function

        '''<summary>Finds a player with the given name in any of the server's games.</summary>
        Private Function f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player)
            Return futureSelect(futureMap(futurize(games_all.ToList), _
                                          Function(game) game.f_FindPlayer(username)), _
                                Function(player) futurize(player IsNot Nothing))
        End Function

        '''<summary>Finds a game containing a player with the given name.</summary>
        Private Function f_FindPlayerGame(ByVal username As String) As IFuture(Of IW3Game)
            Return futureSelect(futurize(games_all.ToList), _
                                Function(game) FutureFunc(Of Boolean).frun(Function(player) player IsNot Nothing, _
                                                                           game.f_FindPlayer(username)))
        End Function

        '''<summary>Removes a game with the given name.</summary>
        Private Function RemoveGame(ByVal game_name As String) As Outcome
            Dim game = FindGame(game_name)
            If game Is Nothing Then Return failure("No game with that name.")

            RemoveHandler game.PlayerTalked, AddressOf c_PlayerTalked
            RemoveHandler game.PlayerLeft, AddressOf c_PlayerLeft
            RemoveHandler game.ChangedState, AddressOf c_GameStateChanged
            RemoveHandler game.lobby.PlayerEntered, AddressOf c_PlayerEntered
            RemoveHandler game.gameplay.PlayerSentData, AddressOf c_PlayerSentData

            games_all.Remove(game)
            games_lobby.Remove(game)
            games_load_screen.Remove(game)
            games_gameplay.Remove(game)
            game.f_Close()
            e_ThrowRemovedGame(game)

            If settings.permanent And settings.instances > 0 And state < W3ServerStates.only_playing_out Then
                CreateGame()
                set_advertiser_options(False)
            End If

            Return success("Game '{0}' removed from server '{1}'.".frmt(game.name, game_name))
        End Function
#End Region

#Region "Link"
        Private link_advertisers As New List(Of IAdvertisingLinkMember)
        Private Function add_advertiser_L(ByVal m As IAdvertisingLinkMember) As outcome
            If state > W3ServerStates.accepting_and_playing Then Return failure("Not accepting players anymore.")
            If link_advertisers.Contains(m) Then Return success("Already have that advertiser.")
            AddHandler m.stopped_advertising, AddressOf remove_stalled_advertised_R
            link_advertisers.Add(m)
            Return success("Added advertiser.")
        End Function
        Private Sub set_advertiser_options(ByVal [private] As Boolean)
            For Each m In link_advertisers
                m.set_advertising_options([private])
            Next m
        End Sub

        Private Sub remove_stalled_advertised_R(ByVal m As IAdvertisingLinkMember, ByVal reason As String)
            ref.enqueue(Function() eval(AddressOf remove_stalled_advertised_L, m, reason))
        End Sub
        Private Sub remove_stalled_advertised_L(ByVal m As IAdvertisingLinkMember, ByVal reason As String)
            If Not link_advertisers.Contains(m) Then Return
            RemoveHandler m.stopped_advertising, AddressOf remove_stalled_advertised_R
            link_advertisers.Remove(m)
        End Sub
        Private Class AdvertisingDependency
            Implements IDependencyLinkServant
            Private WithEvents server As IW3Server
            Private cleaned As Boolean = False
            Private ReadOnly lock As New Object()
            Public Event closed() Implements Links.IDependencyLinkMaster.Closed

            Public Sub New(ByVal server As IW3Server)
                Me.server = ContractNotNull(server, "server")
            End Sub

            Public Sub close() Implements Links.IDependencyLinkServant.close
                SyncLock lock
                    If cleaned Then Return
                    cleaned = True
                End SyncLock

                server.f_StopAcceptingPlayers()
                server = Nothing
                RaiseEvent closed()
            End Sub

            Private Sub server_state_changed(ByVal sender As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates) Handles server.ChangedState
                If old_state <= W3ServerStates.accepting_and_playing And new_state > W3ServerStates.accepting_and_playing Then
                    close()
                End If
            End Sub
        End Class
#End Region

#Region "Interface"
        Private Function _f_FindGame(ByVal game_name As String) As IFuture(Of IW3Game) Implements IW3Server.f_FindGame
            Return ref.enqueue(Function() FindGame(game_name))
        End Function
        Private Function _f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player) Implements IW3Server.f_FindPlayer
            Return futurefuture(ref.enqueue(Function() f_FindPlayer(username)))
        End Function
        Private Function _f_FindPlayerGame(ByVal username As String) As IFuture(Of IW3Game) Implements IW3Server.f_FindPlayerGame
            Return futurefuture(ref.enqueue(Function() f_FindPlayerGame(username)))
        End Function
        Private Function _f_EnumGames() As IFuture(Of IEnumerable(Of IW3Game)) Implements IW3Server.f_EnumGames
            Return ref.enqueue(Function() CType(games_all.ToList, IEnumerable(Of IW3Game)))
        End Function
        Private Function _f_CreateGame(Optional ByVal instance_name As String = Nothing) As IFuture(Of Outcome(Of IW3Game)) Implements IW3Server.f_CreateGame
            Return ref.enqueue(Function() CreateGame(instance_name))
        End Function
        Private Function _f_RemoveGame(ByVal instance_name As String) As IFuture(Of Outcome) Implements IW3Server.f_RemoveGame
            Return ref.enqueue(Function() RemoveGame(instance_name))
        End Function
        Private Function _f_ClosePort(ByVal port As UShort) As IFuture(Of Outcome) Implements IW3Server.f_ClosePort
            Return ref.enqueue(Function() door.accepter.accepter.ClosePort(port))
        End Function
        Private Function _f_OpenPort(ByVal port As UShort) As IFuture(Of Outcome) Implements IW3Server.f_OpenPort
            Return ref.enqueue(Function() door.accepter.accepter.OpenPort(port))
        End Function
        Private Function _f_CloseAllPorts() As IFuture(Of Outcome) Implements IW3Server.f_CloseAllPorts
            Return ref.enqueue(Function() door.accepter.accepter.CloseAllPorts())
        End Function
        Private Function _f_StopAcceptingPlayers() As IFuture(Of Outcome) Implements IW3Server.f_StopAcceptingPlayers
            Return ref.enqueue(Function() StopAcceptingPlayers())
        End Function
        Private Sub _servant_close() Implements Links.IDependencyLinkServant.close
            ref.enqueue(Function() Kill())
        End Sub
        Private Function _f_Kill() As IFuture(Of Outcome) Implements IW3Server.f_Kill
            Return ref.enqueue(Function() Kill())
        End Function
        Private Function _add_advertiser_R(ByVal m As IAdvertisingLinkMember) As IFuture(Of outcome) Implements IW3Server.f_AddAvertiser
            Return ref.enqueue(Function() add_advertiser_L(m))
        End Function
        Private Function _advertising_dep() As IDependencyLinkServant Implements IW3Server.advertising_dep
            Return New AdvertisingDependency(Me)
        End Function
        Private ReadOnly Property _logger() As MultiLogger Implements IW3Server.logger
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
