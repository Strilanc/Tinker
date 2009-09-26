Namespace Warcraft3
    Public Class W3ServerDoor
        Public ReadOnly server As W3Server
        Public ReadOnly logger As Logger
        Private WithEvents _accepter As W3ConnectionAccepter
        Private connectingPlayers As New List(Of W3ConnectingPlayer)
        Private ReadOnly lock As New Object()

        Public Sub New(ByVal server As W3Server,
                       Optional ByVal logger As Logger = Nothing)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(server IsNot Nothing)
            Contract.Assume(server IsNot Nothing)
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
                For Each player In connectingPlayers
                    player.Socket.Disconnect(expected:=True, reason:="Reset Server Door")
                Next player
                connectingPlayers.Clear()
            End SyncLock
        End Sub

        Private Sub c_Connection(ByVal sender As W3ConnectionAccepter,
                                 ByVal player As W3ConnectingPlayer) Handles _accepter.Connection
            SyncLock lock
                connectingPlayers.Add(player)
            End SyncLock
            FindGameForPlayer(player)
        End Sub

        Private Sub FindGameForPlayer(ByVal player As W3ConnectingPlayer)
            Dim addedPlayerFilter = Function(game As IW3Game)
                                        Return game.QueueTryAddPlayer(player).EvalWhenValueReady(
                                            Function(addedPlayer, playerException)
                                                Return addedPlayer IsNot Nothing
                                            End Function
                                        )
                                    End Function

            Dim futureSelectedGame = server.QueueGetGames.Select(
                                                     Function(games) games.FutureSelect(addedPlayerFilter)).Defuturized()

            futureSelectedGame.CallWhenValueReady(
                Sub(game, gameException)
                    If server.settings.instances = 0 AndAlso gameException IsNot Nothing Then
                        server.QueueCreateGame.CallWhenReady(
                            Sub(createdException)
                                If createdException Is Nothing Then
                                    FindGameForPlayer(player)
                                Else
                                    FailConnectingPlayer(player)
                                End If
                            End Sub
                        )
                    Else
                        If gameException IsNot Nothing Then
                            FailConnectingPlayer(player)
                        Else
                            SyncLock lock
                                connectingPlayers.Remove(player)
                            End SyncLock
                            logger.Log("Player {0} entered game {1}.".Frmt(player.Name, game.name), LogMessageType.Positive)
                        End If
                    End If
                End Sub
            )
        End Sub
        Private Sub FailConnectingPlayer(ByVal player As W3ConnectingPlayer)
            SyncLock lock
                connectingPlayers.Remove(player)
            End SyncLock
            logger.log("Couldn't find a game for player {0}.".frmt(player.Name), LogMessageType.Negative)
            player.Socket.SendPacket(W3Packet.MakeReject(W3Packet.RejectReason.GameFull))
            player.Socket.Disconnect(expected:=True, reason:="No Game")
        End Sub
    End Class
End Namespace