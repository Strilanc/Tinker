Namespace Warcraft3
    Public Class W3ServerDoor
        Public ReadOnly server As IW3Server
        Public ReadOnly logger As Logger
        Private WithEvents _accepter As W3ConnectionAccepter
        Private connecting_players As New List(Of W3ConnectingPlayer)
        Private ReadOnly lock As New Object()

        Public Sub New(ByVal server As IW3Server, Optional ByVal logger As Logger = Nothing)
            Me.logger = If(logger, New Logger())
            Me.server = server
            Me._accepter = New W3ConnectionAccepter(Me.logger)
        End Sub

        Public ReadOnly Property accepter() As W3ConnectionAccepter
            Get
                Return _accepter
            End Get
        End Property

        ''' <summary>
        ''' Clears pending connections and stops listening on all ports.
        ''' WARNING: Doesn't guarantee no more players entering the server!
        ''' For example find_game_for_player might be half-finished, resulting in a player joining a game after the reset.
        ''' </summary>
        Public Sub Reset()
            SyncLock lock
                accepter.Reset()
                For Each player In connecting_players
                    player.socket.disconnect()
                Next player
                connecting_players.Clear()
            End SyncLock
        End Sub

        Private Sub accept_connection(ByVal sender As W3ConnectionAccepter, ByVal player As W3ConnectingPlayer) Handles _accepter.Connection
            SyncLock lock
                connecting_players.Add(player)
            End SyncLock
            find_game_for_player(player)
        End Sub

        Private Sub find_game_for_player(ByVal player As W3ConnectingPlayer)
            Dim success_filter As Func(Of Outcome, Boolean) = _
                  Function(outcome) outcome.succeeded

            Dim added_player_filter As Func(Of IW3Game, IFuture(Of Boolean)) = _
                  Function(game) FutureFunc.Call(game.lobby.f_TryAddPlayer(player), success_filter)

            Dim future_selected_game = _
                  FutureSelect(server.f_EnumGames, added_player_filter)

            FutureSub.Call(future_selected_game, Sub(g) finished_selecting_game(player, g))
        End Sub

        Private Sub finished_selecting_game(ByVal player As W3ConnectingPlayer, ByVal game As IW3Game)
            SyncLock lock
                If server.settings.instances = 0 AndAlso game Is Nothing Then
                    FutureSub.Call({server.f_CreateGame}, Sub() find_game_for_player(player))
                Else
                    connecting_players.Remove(player)
                    If game Is Nothing Then
                        logger.log("Couldn't find a game for player {0}.".frmt(player.name), LogMessageTypes.Negative)
                        player.socket.SendPacket(W3Packet.MakePacket_REJECT(W3Packet.RejectReason.GameFull))
                        player.socket.disconnect()
                    Else
                        logger.log("Player {0} entered game {1}.".frmt(player.name, game.name), LogMessageTypes.Positive)
                    End If
                End If
            End SyncLock
        End Sub
    End Class
End Namespace