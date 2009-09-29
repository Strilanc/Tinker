Namespace Warcraft3
    Partial Public Class W3Player
#Region "Networking"
        Private Sub ReceiveNonGameAction(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
            game.QueueReceiveNonGameAction(Me, vals)
        End Sub

        Private Sub IgnorePacket(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
        End Sub

        Private Sub ReceivePong(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
            Dim lambda = New FiniteDouble(0.9)
            Dim tick As ModInt32 = Environment.TickCount
            Dim salt = CUInt(vals("salt"))

            If pingQueue.Count <= 0 Then
                logger.Log("Banned behavior: {0} responded to a ping which wasn't sent.".Frmt(name), LogMessageType.Problem)
                Disconnect(True, W3PlayerLeaveType.Disconnect, "no pings for pong")
                Return
            End If

            Dim stored = pingQueue.Dequeue()
            If salt <> stored.salt Then
                logger.Log("Banned behavior: {0} responded incorrectly to a ping. {1} was returned instead of {2}.".Frmt(name, salt, stored.salt), LogMessageType.Problem)
                Disconnect(True, W3PlayerLeaveType.Disconnect, "incorrect pong")
                Return
            End If

            latency *= 1 - lambda
            latency += lambda * New FiniteDouble(CUInt(tick - stored.time))
            If latency = 0 Then latency = Double.Epsilon
            Contract.Assume(latency >= 0)
        End Sub

        Private Sub ReceiveLeaving(ByVal packet As W3Packet)
            Contract.Requires(packet IsNot Nothing)
            Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
            Dim leaveType = CType(vals("leave type"), W3PlayerLeaveType)
            Disconnect(True, leaveType, "manually leaving ({0})".Frmt(leaveType))
        End Sub
#End Region

        Public ReadOnly Property GetDownloadPercent() As Byte
            Get
                If state <> W3PlayerState.Lobby Then Return 100
                Dim pos = mapDownloadPosition
                If isFake Then Return 254 'Not a real player, show "|CF"
                If pos = -1 Then Return 255 'Not known yet, show "?"
                If pos >= game.Map.FileSize Then Return 100 'No DL, show nothing
                Return CByte((100 * pos) \ game.Map.FileSize) 'DL running, show % done
            End Get
        End Property

        Public Function Description() As IFuture(Of String)
            Dim base = name.Padded(20) +
                       "Host={0}".Frmt(CanHost()).Padded(12) +
                       "{0}c".Frmt(numPeerConnections).Padded(5) +
                       If(latency = 0, "RTT=?", "RTT={0:0}ms".Frmt(latency)).Padded(12)
            Select Case state
                Case W3PlayerState.Lobby
                    Dim dl = GetDownloadPercent().ToString(CultureInfo.InvariantCulture)
                    Select Case dl
                        Case "255" : dl = "?"
                        Case "254" : dl = "fake"
                        Case Else : dl += "%"
                    End Select
                    Return game.DownloadScheduler.GenerateRateDescription(index).Select(
                        Function(rateDescription)
                            Return base +
                                   Padded("DL={0}".Frmt(dl), 9) +
                                   "EB={0}".Frmt(rateDescription)
                        End Function)
                Case W3PlayerState.Loading
                    Return (base + "Ready={0}".Frmt(Ready)).Futurized
                Case W3PlayerState.Playing
                    Return (base + "DT={0}gms".Frmt(game.GameTime - Me.totalTockTime)).Futurized
                Case Else
                    Throw New UnreachableException
            End Select
        End Function
    End Class
End Namespace
