Imports Tinker.Pickling

Namespace WC3.Download
    Public Class Manager
        Inherits DisposableWithTask

        Public Shared ReadOnly ForceSteadyPeriod As TimeSpan = 5.Seconds
        Public Shared ReadOnly FreezePeriod As TimeSpan = 10.Seconds
        Public Shared ReadOnly MinSwitchPeriod As TimeSpan = 10.Seconds
        Public Shared ReadOnly SwitchPenaltyPeriod As TimeSpan = 5.Seconds
        Public Shared ReadOnly SwitchPenaltyFactor As Double = 1.2
        Public Shared ReadOnly DefaultBandwidthPerSecond As Double = 32000
        Public Shared ReadOnly MaxBufferedMapSize As Integer = Protocol.Packets.MaxFileDataSize * 8
        Public Shared ReadOnly UpdatePeriod As TimeSpan = 1.Seconds

        Private ReadOnly _map As Map
        Private ReadOnly _logger As Logger
        Private ReadOnly _hooks As New List(Of Task(Of IDisposable))()
        Private ReadOnly inQueue As CallQueue = New TaskedCallQueue()
        Private ReadOnly _allowDownloads As Boolean
        Private ReadOnly _allowUploads As Boolean
        Private ReadOnly _playerClients As New Dictionary(Of IPlayerDownloadAspect, TransferClient)
        Private ReadOnly _clock As IClock
        Private _mapPieceSender As Action(Of IPlayerDownloadAspect, UInt32)
        Private _defaultClient As TransferClient
        Private ReadOnly _started As New OnetimeLock

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_started IsNot Nothing)
            Contract.Invariant(_playerClients IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_map IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            'Contract.Invariant((_started.State = OnetimeLockState.Acquired) = (_mapPieceSender IsNot Nothing))
        End Sub

        Public Sub New(ByVal clock As IClock,
                       ByVal map As Map,
                       ByVal logger As Logger,
                       ByVal allowDownloads As Boolean,
                       ByVal allowUploads As Boolean)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Me._clock = clock
            Me._map = map
            Me._logger = logger
            Me._allowDownloads = allowDownloads
            Me._allowUploads = allowUploads
        End Sub

        Public Sub Start(ByVal startPlayerHoldPoint As IHoldPoint(Of IPlayerDownloadAspect),
                         ByVal mapPieceSender As Action(Of IPlayerDownloadAspect, UInt32))
            Contract.Requires(startPlayerHoldPoint IsNot Nothing)
            Contract.Requires(mapPieceSender IsNot Nothing)
            If Me.IsDisposed Then Throw New ObjectDisposedException(Me.GetType.Name)
            If Not _started.TryAcquire() Then Throw New InvalidOperationException("Already started.")

            Me._mapPieceSender = mapPieceSender

            If _allowUploads Then
                _defaultClient = New TransferClient(Nothing, _map, _clock, {})
                _defaultClient.MarkReported()
                _defaultClient.ReportedPosition = _map.FileSize
            End If

            _hooks.Add(_clock.AsyncRepeat(UpdatePeriod, Sub() inQueue.QueueAction(AddressOf OnTick)).AsTask)

            _hooks.Add(startPlayerHoldPoint.IncludeFutureHandler(Function(arg) inQueue.QueueFunc(
                                    Function() OnGameStartPlayerHold(arg)).Unwrap).AsTask)
        End Sub

        '''<summary>Enumates the player clients as well as the default client.</summary>
        Private ReadOnly Property AllClients As IEnumerable(Of TransferClient)
            Get
                Return _playerClients.Values.Concat(If(_defaultClient Is Nothing, {}, {_defaultClient}))
            End Get
        End Property

        Private Sub SendMapFileData(ByVal client As TransferClient, ByVal reportedPosition As UInt32)
            Contract.Requires(client IsNot Nothing)
            Contract.Requires(client.Transfer IsNot Nothing)
            Contract.Requires(client.Transfer.Uploader Is _defaultClient)
            Contract.Assume(client.Player IsNot Nothing)
            Contract.Assume(_mapPieceSender IsNot Nothing)
            client.LastSendPosition = Math.Max(client.LastSendPosition, reportedPosition)
            While client.LastSendPosition < reportedPosition + MaxBufferedMapSize AndAlso client.LastSendPosition < FileSize
                Call _mapPieceSender(client.Player, client.LastSendPosition)
                client.LastSendPosition += Protocol.Packets.MaxFileDataSize
            End While
        End Sub

#Region "Public View"
        Private ReadOnly Property ClientLatencyDescription(ByVal player As IPlayerDownloadAspect, ByVal latencyDescription As String) As String
            Get
                Contract.Requires(player IsNot Nothing)
                Contract.Requires(latencyDescription IsNot Nothing)
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)

                If Not _playerClients.ContainsKey(player) Then Return latencyDescription
                Dim client = _playerClients(player)
                Contract.Assume(client IsNot Nothing)

                If Not client.HasReported Then Return latencyDescription
                If client.Transfer Is Nothing Then
                    If client.IsSteady Then Return latencyDescription
                    Return If(client.ReportedHasFile, "(ul>>)", "(dl>>)")
                Else
                    If client.Transfer.Uploader Is _defaultClient Then Return "(dl:H)"
                    Contract.Assume(client.Transfer.Downloader.Player IsNot Nothing)
                    Contract.Assume(client.Transfer.Uploader.Player IsNot Nothing)
                    If client.IsSteady Then Return If(client.ReportedHasFile,
                                                      "(ul:{0})".Frmt(client.Transfer.Downloader.Player.Id),
                                                      "(dl:{0})".Frmt(client.Transfer.Uploader.Player.Id))
                    Return If(client.ReportedHasFile, "(>>ul)", "(>>dl)")
                End If
            End Get
        End Property
        Public Function QueueGetClientLatencyDescription(ByVal player As IPlayerDownloadAspect, ByVal latencyDescription As String) As Task(Of String)
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(latencyDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() ClientLatencyDescription(player, latencyDescription))
        End Function

        Private ReadOnly Property ClientBandwidthDescription(ByVal player As IPlayerDownloadAspect) As String
            Get
                Contract.Requires(player IsNot Nothing)
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)

                If Not _playerClients.ContainsKey(player) Then Return "?"
                Dim client = _playerClients(player)
                Contract.Assume(client IsNot Nothing)
                If client.TotalMeasurementTime < 3.Seconds Then Return "?"

                Dim d = client.EstimatedBandwidthPerSecond
                Dim f = 1.0
                For Each s In {"B", "KiB", "MiB", "GiB", "TiB", "PiB"}
                    Dim f2 = f * 1024
                    If d < f2 Then Return "{0:0.0} {1}/s".Frmt(d / f, s)
                    f = f2
                Next s

                Return ">HiB/s" '... What? It could happen...
            End Get
        End Property
        Public Function QueueGetClientBandwidthDescription(ByVal player As IPlayerDownloadAspect) As Task(Of String)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of String))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() ClientBandwidthDescription(player))
        End Function

        Public ReadOnly Property FileSize As UInt32
            Get
                Return _map.FileSize
            End Get
        End Property
#End Region

#Region "Game-Triggered"
        Private Function OnGameStartPlayerHold(ByVal player As IPlayerDownloadAspect) As Task
            Contract.Requires(player IsNot Nothing)

            Dim playerHooks = New List(Of Task(Of IDisposable))() From {
                    player.QueueAddPacketHandler(packetDefinition:=Protocol.Packets.ClientMapInfo,
                                                 handler:=Function(pickle) QueueOnReceiveClientMapInfo(player, pickle)),
                    player.QueueAddPacketHandler(packetDefinition:=Protocol.Packets.PeerConnectionInfo,
                                                 handler:=Function(pickle) QueueOnReceivePeerConnectionInfo(player, pickle))
                }


            Dim client = New TransferClient(player, _map, _clock, playerHooks)
            _playerClients(player) = client
            If _defaultClient IsNot Nothing Then client.Links.Add(_defaultClient)

            player.DisposalTask.QueueContinueWithAction(inQueue,
                Sub()
                    client.Dispose()
                    _playerClients.Remove(player)
                End Sub)

            Return playerHooks.AsAggregateTask
        End Function
#End Region

#Region "Communication-Triggered"
        Private Function QueueOnReceiveClientMapInfo(ByVal player As IPlayerDownloadAspect, ByVal pickle As IPickle(Of NamedValueMap)) As Task
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Dim vals = pickle.Value
            Return inQueue.QueueAction(Sub() OnReceiveClientMapInfo(player:=player,
                                                                    state:=CType(vals("transfer state"), Protocol.MapTransferState),
                                                                    position:=CUInt(vals("total downloaded"))))
        End Function
        Private Function QueueOnReceivePeerConnectionInfo(ByVal player As IPlayerDownloadAspect, ByVal pickle As IPickle(Of UInt16)) As Task
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() OnReceivePeerConnectionInfo(player:=player,
                                                                         flags:=pickle.Value))
        End Function

        Private Sub OnReceivePeerConnectionInfo(ByVal player As IPlayerDownloadAspect, ByVal flags As UInt32)
            Contract.Requires(player IsNot Nothing)
            If Not _playerClients.ContainsKey(player) Then Return

            Dim client = _playerClients(player)
            Contract.Assume(client IsNot Nothing)
            client.Links.Clear()
            If _defaultClient IsNot Nothing Then client.Links.Add(_defaultClient)
            client.Links.AddRange(From peer In _playerClients.Values
                                  Where peer IsNot client
                                  Where flags.HasBitSet(peer.Player.Id.Index - 1)
                                  Select peer)
        End Sub

        Private Sub OnReceiveClientMapInfo(ByVal player As IPlayerDownloadAspect, ByVal state As Protocol.MapTransferState, ByVal position As UInt32)
            Contract.Requires(player IsNot Nothing)
            If Not _playerClients.ContainsKey(player) Then Return

            Dim client = _playerClients(player)
            Contract.Assume(client IsNot Nothing)
            Contract.Assume(client.Player IsNot Nothing)

            If position > _map.FileSize Then
                Throw New IO.InvalidDataException("Moved download position past end of file at {0} to {1}.".Frmt(_map.FileSize, position))
            End If

            If Not client.HasReported Then
                OnFirstMapInfo(client, state, position)
            Else
                OnTypicalMapInfo(client, state, position)
            End If

            client.MarkReported()
            client.ReportedPosition = position
            client.ReportedState = state

            If client.Transfer IsNot Nothing AndAlso client.Transfer.Uploader Is _defaultClient Then
                SendMapFileData(client, position)
            End If
        End Sub
        Private Sub OnFirstMapInfo(ByVal client As TransferClient, ByVal state As Protocol.MapTransferState, ByVal position As UInt32)
            Contract.Requires(client IsNot Nothing)
            Contract.Requires(client.Player IsNot Nothing)
            Contract.Requires(Not client.HasReported)

            If state <> Protocol.MapTransferState.Idle Then
                Throw New IO.InvalidDataException("Invalid initial download transfer state.")
            End If

            If Not _allowDownloads AndAlso position < _map.FileSize Then
                client.Player.QueueDisconnect(expected:=True,
                                              reportedReason:=Protocol.PlayerLeaveReason.Disconnect,
                                              reasonDescription:="Downloads not allowed.")
            End If
        End Sub
        Private Sub OnTypicalMapInfo(ByVal client As TransferClient, ByVal state As Protocol.MapTransferState, ByVal position As UInt32)
            Contract.Requires(client IsNot Nothing)
            Contract.Requires(client.Player IsNot Nothing)
            Contract.Requires(client.HasReported)

            If position < client.ReportedPosition Then
                Throw New IO.InvalidDataException("Moved download position backwards from {0} to {1}.".Frmt(client.ReportedPosition, position))
            ElseIf client.ReportedHasFile AndAlso state = Protocol.MapTransferState.Downloading Then
                Throw New IO.InvalidDataException("Non-downloader reported status as downloading.")
            ElseIf Not client.ReportedHasFile AndAlso state = Protocol.MapTransferState.Uploading Then
                Throw New IO.InvalidDataException("Non-uploader reported status as uploading.")
            End If

            If client.Transfer IsNot Nothing Then
                client.Transfer.Advance(position - client.ReportedPosition)
            End If
            If client.ReportedPosition < _map.FileSize AndAlso position = _map.FileSize Then
                Contract.Assume(client.Player IsNot Nothing)
                If client.Transfer IsNot Nothing Then
                    _logger.Log("{0} finished downloading the map from {1}.".Frmt(client.Player.Name, client.Transfer.Uploader), LogMessageType.Positive)
                    client.Transfer.Dispose() '[nulls client.Transfer]
                Else
                    _logger.Log("{0} finished downloading the map.".Frmt(client.Player.Name), LogMessageType.Positive)
                End If
            End If
        End Sub
#End Region

#Region "Periodic"
        ''' <summary>
        ''' Returns a transfer which has not seen any activity for some time.
        ''' Returns null if there is no such transfer.
        ''' </summary>
        Private Function TryFindFrozenTransfer() As Transfer
            Return (From client In AllClients
                    Where client.Transfer IsNot Nothing
                    Select transfer = client.Transfer
                    Where transfer.TimeSinceLastActivity > FreezePeriod
                   ).FirstOrDefault
        End Function

        ''' <summary>
        ''' Returns a transfer which could be improved by cancelling it and switching the downloader to another uploader.
        ''' Returns null if there is no such transfer.
        ''' </summary>
        Private Function TryFindImprovableTransfer() As Transfer
            For Each transfer In From client In AllClients
                                 Where client.HasReported
                                 Where Not client.ReportedHasFile
                                 Where client.Transfer IsNot Nothing
                                 Order By client.EstimatedBandwidthPerSecond Descending
                                 Select t = client.Transfer
                                 Where t.Duration > MinSwitchPeriod
                                 Where t.ExpectedDurationRemaining > MinSwitchPeriod
                Contract.Assume(transfer IsNot Nothing)
                Dim downloader = transfer.Downloader
                Dim remainingData = _map.FileSize - transfer.ReportedPosition
                Dim curExpectedCost = transfer.ExpectedDurationRemaining.TotalSeconds

                'expected cost of switching and downloading from another uploader
                Dim bestAvailableUploader = downloader.FindBestAvailableUploader()
                If bestAvailableUploader Is Nothing Then Continue For
                Dim newBandwidthPerSecond = Math.Min(downloader.EstimatedBandwidthPerSecond,
                                                     bestAvailableUploader.EstimatedBandwidthPerSecond)
                Dim newExpectedCost = SwitchPenaltyPeriod.TotalSeconds + SwitchPenaltyFactor * remainingData / newBandwidthPerSecond

                'check
                If newExpectedCost < curExpectedCost Then
                    Return transfer
                End If
            Next transfer

            Return Nothing
        End Function

        '''<summary>Starts and stops transfers, trying to minimize the total transfer time.</summary>
        Private Sub OnTick()
            'Start new transfers
            For Each downloader In From client In AllClients
                                   Where client.HasReported
                                   Where Not client.ReportedHasFile
                                   Where client.Transfer Is Nothing
                                   Where client.IsSteady
                Contract.Assume(downloader IsNot Nothing)
                Dim bestAvailableUploader = downloader.FindBestAvailableUploader()
                If bestAvailableUploader Is Nothing Then Continue For

                Dim dler = downloader.Player
                Contract.Assume(dler IsNot Nothing)
                Dim uler = bestAvailableUploader.Player
                Contract.Assume(downloader.HasReported)
                Contract.Assume(downloader.Transfer Is Nothing)
                TransferClient.StartTransfer(downloader, bestAvailableUploader)

                If uler Is Nothing Then
                    _logger.Log("Initiating map upload to {0}.".Frmt(dler.Name), LogMessageType.Positive)
                    Contract.Assume(downloader.Transfer.Uploader Is _defaultClient)
                    SendMapFileData(downloader, downloader.ReportedPosition)
                Else
                    _logger.Log("Initiating peer map transfer from {0} to {1}.".Frmt(uler.Name, dler.Name), LogMessageType.Positive)
                    uler.QueueSendPacket(Protocol.MakeSetUploadTarget(dler.Id, downloader.ReportedPosition))
                    dler.QueueSendPacket(Protocol.MakeSetDownloadSource(uler.Id))
                End If
            Next downloader

            'Find and cancel one frozen or improvable transfer, if there are any
            Dim cancellation = If(TryFindFrozenTransfer(), TryFindImprovableTransfer())
            If cancellation IsNot Nothing Then
                Dim dler = cancellation.Downloader.Player
                Dim uler = cancellation.Uploader.Player
                Contract.Assume(dler IsNot Nothing)
                cancellation.Dispose()

                'Force-cancel the transfer by making the uploader and downloader each think the other rejoined
                If uler IsNot Nothing Then
                    dler.QueueSendPacket(Protocol.MakeOtherPlayerLeft(uler.Id, Protocol.PlayerLeaveReason.Disconnect))
                    uler.QueueSendPacket(Protocol.MakeOtherPlayerLeft(dler.Id, Protocol.PlayerLeaveReason.Disconnect))
                    dler.QueueSendPacket(uler.MakePacketOtherPlayerJoined())
                    uler.QueueSendPacket(dler.MakePacketOtherPlayerJoined())
                End If

                Dim reason = If(cancellation.TimeSinceLastActivity > FreezePeriod, "frozen", "slow")
                If uler Is Nothing Then
                    _logger.Log("Cancelled {0} map upload to {1}.".Frmt(reason, dler.Name), LogMessageType.Negative)
                Else
                    _logger.Log("Cancelled {0} peer map transfer from {1} to {2}.".Frmt(reason, uler.Name, dler.Name), LogMessageType.Negative)
                End If
            End If
        End Sub
#End Region

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return inQueue.QueueFunc(
                Function()
                    Dim results = New List(Of Task)()
                    For Each e In _hooks
                        results.Add(e.ContinueWithAction(Sub(value) value.Dispose()))
                    Next e
                    For Each e In AllClients
                        e.Dispose()
                        results.Add(e.DisposalTask)
                    Next e
                    Return results.AsAggregateTask
                End Function).Unwrap
        End Function
    End Class
End Namespace
