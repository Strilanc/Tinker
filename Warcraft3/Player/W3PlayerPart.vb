Namespace WC3
    Partial Public NotInheritable Class Player
        Public Event ReceivedNonGameAction(ByVal sender As Player, ByVal vals As Dictionary(Of InvariantString, Object))
#Region "Networking"
        Private Sub ReceiveNonGameAction(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(Pickle IsNot Nothing)
            Dim vals = CType(Pickle.Value, Dictionary(Of InvariantString, Object))
            RaiseEvent ReceivedNonGameAction(Me, vals)
        End Sub

        Private Sub IgnorePacket(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
        End Sub

        Private Sub ReceiveLeaving(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(Pickle IsNot Nothing)
            Dim vals = CType(Pickle.Value, Dictionary(Of InvariantString, Object))
            Dim leaveType = CType(vals("leave type"), PlayerLeaveType)
            Disconnect(True, leaveType, "Controlled exit with reported result: {0}".Frmt(leaveType))
        End Sub
#End Region

        Public ReadOnly Property GetDownloadPercent() As Byte
            Get
                If state <> PlayerState.Lobby Then Return 100
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
                    Dim base = Name.Value.Padded(20) +
                               "Host={0}".Frmt(CanHost()).Padded(12) +
                               "{0}c".Frmt(_numPeerConnections).Padded(5) +
                               latencyDesc.Padded(12)
                    Select Case state
                        Case PlayerState.Lobby
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
                                        Case PlayerState.Loading
                                            Return (base + "Ready={0}".Frmt(Ready)).Futurized
                                        Case PlayerState.Playing
                                            Return (base + "DT={0}gms".Frmt(Me.maxTockTime - Me.totalTockTime)).Futurized
                                        Case Else
                                            Throw state.MakeImpossibleValueException
                                    End Select
                                End Function
            ).Defuturized
        End Function
    End Class
End Namespace
