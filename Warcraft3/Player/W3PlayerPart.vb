Namespace Warcraft3
    Partial Public NotInheritable Class W3Player
        Public Event ReceivedNonGameAction(ByVal sender As W3Player, ByVal vals As Dictionary(Of String, Object))
#Region "Networking"
        Private Sub ReceiveNonGameAction(ByVal pickle As IPickle(Of Dictionary(Of String, Object)))
            Contract.Requires(Pickle IsNot Nothing)
            Dim vals = CType(Pickle.Value, Dictionary(Of String, Object))
            RaiseEvent ReceivedNonGameAction(Me, vals)
        End Sub

        Private Sub IgnorePacket(ByVal pickle As IPickle(Of Dictionary(Of String, Object)))
        End Sub

        Private Sub ReceiveLeaving(ByVal pickle As IPickle(Of Dictionary(Of String, Object)))
            Contract.Requires(Pickle IsNot Nothing)
            Dim vals = CType(Pickle.Value, Dictionary(Of String, Object))
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
                If pos >= settings.Map.FileSize Then Return 100 'No DL, show nothing
                Return CByte((100 * pos) \ settings.Map.FileSize) 'DL running, show % done
            End Get
        End Property

        Public Function Description() As IFuture(Of String)
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
            Return GetLatencyDescription.Select(
                Function(latencyDesc)
                    Dim base = Name.Padded(20) +
                               "Host={0}".Frmt(CanHost()).Padded(12) +
                               "{0}c".Frmt(_numPeerConnections).Padded(5) +
                               latencyDesc.Padded(12)
                    Select Case state
                        Case W3PlayerState.Lobby
                            Dim dl = GetDownloadPercent().ToString(CultureInfo.InvariantCulture)
                            Select Case dl
                                Case "255" : dl = "?"
                                Case "254" : dl = "fake"
                                Case Else : dl += "%"
                            End Select
                            Return scheduler.GenerateRateDescription(Index).Select(
                                Function(rateDescription)
                                    Return base +
                                           Padded("DL={0}".Frmt(dl), 9) +
                                           "EB={0}".Frmt(rateDescription)
                                End Function)
                                        Case W3PlayerState.Loading
                                            Return (base + "Ready={0}".Frmt(Ready)).Futurized
                                        Case W3PlayerState.Playing
                                            Return (base + "DT={0}gms".Frmt(Me.maxTockTime - Me.totalTockTime)).Futurized
                                        Case Else
                                            Throw state.MakeImpossibleValueException
                                    End Select
                                End Function
            ).Defuturized
        End Function
    End Class
End Namespace
