Imports Tinker.Pickling

Namespace WC3
    Partial Public NotInheritable Class Player
        Public Event ReceivedNonGameAction(ByVal sender As Player, ByVal vals As Dictionary(Of InvariantString, Object))
#Region "Networking"
        Private Sub ReceiveNonGameAction(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(pickle IsNot Nothing)
            Dim vals = CType(pickle.Value, Dictionary(Of InvariantString, Object))
            RaiseEvent ReceivedNonGameAction(Me, vals)
        End Sub

        Private Sub IgnorePacket(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
        End Sub

        Private Sub ReceiveLeaving(ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object)))
            Contract.Requires(pickle IsNot Nothing)
            Dim vals = CType(pickle.Value, Dictionary(Of InvariantString, Object))
            Dim leaveType = CType(vals("leave type"), PlayerLeaveType)
            Disconnect(True, leaveType, "Controlled exit with reported result: {0}".Frmt(leaveType))
        End Sub
#End Region

        Public ReadOnly Property AdvertisedDownloadPercent() As Byte
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
            Return QueueGetLatencyDescription.Select(
                Function(latencyDesc)
                    Dim contextInfo As IFuture(Of String)
                    Select Case state
                        Case PlayerState.Lobby
                            Dim p = AdvertisedDownloadPercent
                            Dim dlText As String
                            Select Case p
                                Case 255 : dlText = "?"
                                Case 254 : dlText = "fake"
                                Case Else : dlText = "{0}%".Frmt(p)
                            End Select
                            contextInfo = From rateDescription In scheduler.GenerateRateDescription(Me.PID.Index)
                                           Select "DL={0}".Frmt(dlText).Padded(9) + "EB={0}".Frmt(rateDescription)
                        Case PlayerState.Loading
                            contextInfo = "Ready={0}".Frmt(Ready).Futurized
                        Case PlayerState.Playing
                            contextInfo = "DT={0}gms".Frmt(Me.maxTockTime - Me.totalTockTime).Futurized
                        Case Else
                            Throw state.MakeImpossibleValueException
                    End Select
                    Contract.Assert(contextInfo IsNot Nothing)

                    Return From text In contextInfo
                           Select Name.Value.Padded(20) +
                                  "pid={0}".Frmt(Me.PID).Padded(6) +
                                  "Host={0}".Frmt(CanHost()).Padded(12) +
                                  "{0}c".Frmt(_numPeerConnections).Padded(5) +
                                  latencyDesc.Padded(12) +
                                  text
                End Function).Defuturized
        End Function
    End Class
End Namespace
