Namespace Warcraft3
    Partial Public Class W3Player
        Implements IW3Player

#Region "Networking"
        Private Sub ReceiveNonGameAction(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            game.QueueReceiveNonGameAction(Me, vals)
        End Sub

        Private Sub IgnorePacket(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
        End Sub

        Private Sub ReceivePong(ByVal vals As Dictionary(Of String, Object))
            Contract.Requires(vals IsNot Nothing)
            Dim lambda = 0.5
            Dim tick As ModInt32 = Environment.TickCount
            Dim salt = CUInt(vals("salt"))

            If pingQueue.Count <= 0 Then
                logger.log("Banned behavior: {0} responded to a ping which wasn't sent.".frmt(name), LogMessageTypes.Problem)
                Disconnect(True, W3PlayerLeaveTypes.Disconnect, "no pings for pong")
                Return
            End If

            Dim stored = pingQueue.Dequeue()
            If salt <> stored.salt Then
                logger.log("Banned behavior: {0} responded incorrectly to a ping. {1} was returned instead of {2}.".frmt(name, salt, stored.salt), LogMessageTypes.Problem)
                Disconnect(True, W3PlayerLeaveTypes.Disconnect, "incorrect pong")
                Return
            End If

            latency *= 1 - lambda
            latency += lambda * CUInt(tick - stored.time)
            Contract.Assume(latency >= 0)
            Contract.Assume(Not Double.IsNaN(latency))
            Contract.Assume(Not Double.IsInfinity(latency))
        End Sub

        Private Sub ReceiveLeaving(ByVal vals As Dictionary(Of String, Object))
            Dim leaveType = CType(vals("leave type"), W3PlayerLeaveTypes)
            Disconnect(True, leaveType, "manually leaving ({0})".frmt(leaveType))
        End Sub
#End Region

        Protected Overridable ReadOnly Property GetPercentDl() As Byte Implements IW3Player.GetDownloadPercent
            Get
                If state <> W3PlayerStates.Lobby Then Return 100
                Dim pos = mapDownloadPosition
                If isFake Then Return 254 'Not a real player, show "|CF"
                If pos = -1 Then Return 255 'Not known yet, show "?"
                If pos >= game.map.FileSize Then Return 100 'No DL, show nothing
                Return CByte((100 * pos) \ game.map.FileSize) 'DL running, show % done
            End Get
        End Property

        Public Overridable Function Description() As String Implements IW3Player.Description
            Dim base = name.Padded(20) +
                       "Host={0}".frmt(CanHost()).Padded(12) +
                       "{0}c".frmt(numPeerConnections).Padded(5) +
                       "RTT={0:0}ms".frmt(latency).Padded(12)
            Select Case state
                Case W3PlayerStates.Lobby
                    Dim dl = GetPercentDl().ToString
                    Select Case dl
                        Case "255"
                            dl = "?"
                        Case "254"
                            dl = "fake"
                        Case Else
                            dl += "%"
                    End Select
                    Return base +
                           Padded("DL={0}".frmt(dl), 9) +
                           "EB={0}".frmt(game.DownloadScheduler.GenerateRateDescription(index))
                Case W3PlayerStates.Loading
                    Return base + "Ready={0}".frmt(ready)
                Case W3PlayerStates.Playing
                    Return base + "DT={0}gms".frmt(game.GameTime - Me.totalTockTime)
                Case Else
                    Throw New UnreachableException
            End Select
        End Function
    End Class
End Namespace
