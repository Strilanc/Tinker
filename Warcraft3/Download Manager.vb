Imports Tinker.Pickling

Namespace WC3
    <ContractClass(GetType(IPlayerDownloadAspect.ContractClass))>
    Public Interface IPlayerDownloadAspect
        Inherits IFutureDisposable
        ReadOnly Property Name As InvariantString
        ReadOnly Property PID As PlayerID
        Function QueueAddPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                             ByVal handler As Func(Of IPickle(Of T), IFuture)) As IFuture(Of IDisposable)
        Function MakePacketOtherPlayerJoined() As Protocol.Packet
        Function QueueSendPacket(ByVal packet As Protocol.Packet) As IFuture
        Function QueueDisconnect(ByVal expected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String) As IFuture

        <ContractClassFor(GetType(IPlayerDownloadAspect))>
        NotInheritable Shadows Class ContractClass
            Implements IPlayerDownloadAspect
            Public Function MakePacketOtherPlayerJoined() As Protocol.Packet Implements IPlayerDownloadAspect.MakePacketOtherPlayerJoined
                Contract.Ensures(Contract.Result(Of Protocol.Packet)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public ReadOnly Property Name As InvariantString Implements IPlayerDownloadAspect.Name
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property PID As PlayerID Implements IPlayerDownloadAspect.PID
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public Function QueueAddPacketHandler(Of T)(ByVal packetDefinition As Protocol.Packets.Definition(Of T),
                                                        ByVal handler As Func(Of IPickle(Of T), IFuture)) As IFuture(Of IDisposable) _
                                                        Implements IPlayerDownloadAspect.QueueAddPacketHandler
                Contract.Requires(packetDefinition IsNot Nothing)
                Contract.Requires(handler IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public Function QueueSendPacket(ByVal packet As Protocol.Packet) As IFuture Implements IPlayerDownloadAspect.QueueSendPacket
                Contract.Requires(packet IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public ReadOnly Property FutureDisposed As Strilbrary.Threading.IFuture Implements Strilbrary.Threading.IFutureDisposable.FutureDisposed
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
            Public Function QueueDisconnect(ByVal expected As Boolean, ByVal reportedReason As Protocol.PlayerLeaveReason, ByVal reasonDescription As String) As IFuture Implements IPlayerDownloadAspect.QueueDisconnect
                Contract.Requires(reasonDescription IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Interface

    Public Class DownloadManager
        Inherits FutureDisposable

        Public Shared ReadOnly ForceSteadyPeriod As TimeSpan = 5.Seconds
        Public Shared ReadOnly FreezePeriod As TimeSpan = 10.Seconds
        Public Shared ReadOnly MinSwitchPeriod As TimeSpan = 10.Seconds
        Public Shared ReadOnly SwitchPenaltyPeriod As TimeSpan = 5.Seconds
        Public Shared ReadOnly SwitchPenaltyFactor As Double = 1.2
        Public Shared ReadOnly DefaultBandwidthPerSecond As Double = 32000
        Public Shared ReadOnly MaxBufferedMapSize As Integer = Protocol.Packets.MaxFileDataSize * 8
        Public Shared ReadOnly UpdatePeriod As TimeSpan = 1.Seconds

        Private Class Transfer
            Implements IDisposable

            Private ReadOnly _fileSize As UInt32
            Private ReadOnly _downloader As TransferClient
            Private ReadOnly _uploader As TransferClient
            Private ReadOnly _startingPosition As UInt32
            Private _durationTimer As RelativeClock
            Private _lastActivityTimer As RelativeClock
            Private _totalProgress As UInt32

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_downloader IsNot Nothing)
                Contract.Invariant(_uploader IsNot Nothing)
                Contract.Invariant(_durationTimer IsNot Nothing)
                Contract.Invariant(_lastActivityTimer IsNot Nothing)
                Contract.Invariant(_startingPosition <= _fileSize)
            End Sub

            <ContractVerification(False)>
            Public Sub New(ByVal downloader As TransferClient,
                           ByVal uploader As TransferClient,
                           ByVal startingPosition As UInt32,
                           ByVal filesize As UInt32,
                           ByVal clock As IClock)
                Contract.Requires(downloader IsNot Nothing)
                Contract.Requires(uploader IsNot Nothing)
                Contract.Requires(clock IsNot Nothing)
                Contract.Requires(startingPosition <= filesize)
                Contract.Ensures(Me.Downloader Is downloader)
                Contract.Ensures(Me.Uploader Is uploader)
                Contract.Ensures(Me.StartingPosition = startingPosition)
                Me._downloader = downloader
                Me._uploader = uploader
                Me._fileSize = filesize
                Me._durationTimer = clock.Restarted()
                Me._lastActivityTimer = clock.Restarted()
                Me._startingPosition = startingPosition
            End Sub

#Region "Naive Properties"
            Public ReadOnly Property Downloader As TransferClient
                Get
                    Contract.Ensures(Contract.Result(Of TransferClient)() IsNot Nothing)
                    Return _downloader
                End Get
            End Property
            Public ReadOnly Property Uploader As TransferClient
                Get
                    Contract.Ensures(Contract.Result(Of TransferClient)() IsNot Nothing)
                    Return _uploader
                End Get
            End Property
            Public ReadOnly Property Duration As TimeSpan
                Get
                    Contract.Ensures(Contract.Result(Of TimeSpan)().Ticks >= 0)
                    Return _durationTimer.ElapsedTime
                End Get
            End Property
            Public ReadOnly Property TimeSinceLastActivity As TimeSpan
                Get
                    Contract.Ensures(Contract.Result(Of TimeSpan)().Ticks >= 0)
                    Return _lastActivityTimer.ElapsedTime
                End Get
            End Property
            Public ReadOnly Property StartingPosition As UInt32
                Get
                    Return _startingPosition
                End Get
            End Property
            Public ReadOnly Property TotalProgress As UInt32
                Get
                    Return _totalProgress
                End Get
            End Property
            Public ReadOnly Property ReportedPosition As UInt32
                Get
                    Return StartingPosition + TotalProgress
                End Get
            End Property
#End Region

            Public ReadOnly Property BandwidthPerSecond As Double
                Get
                    Dim dt = Duration.TotalSeconds
                    If dt < 1 Then Return DefaultBandwidthPerSecond
                    Dim expansionFactor = 1 + 1 / (1 + Duration.TotalSeconds)
                    Return expansionFactor * _totalProgress / dt
                End Get
            End Property

            Public ReadOnly Property ExpectedDurationRemaining As TimeSpan
                Get
                    Return New TimeSpan(ticks:=CLng(TimeSpan.TicksPerSecond * ((_fileSize - ReportedPosition) / BandwidthPerSecond)))
                End Get
            End Property

            Public Sub Advance(ByVal progress As UInt32)
                _totalProgress += progress
                _lastActivityTimer = _lastActivityTimer.Restarted()
            End Sub

            <ContractVerification(False)>
            Public Sub Dispose() Implements IDisposable.Dispose
                If Downloader.Transfer Is Me Then Downloader.ClearTransfer()
                If Uploader.Transfer Is Me Then Uploader.ClearTransfer()
            End Sub
        End Class

        <DebuggerDisplay("{ToString}")>
        Private Class TransferClient
            Inherits FutureDisposable

            Private ReadOnly _map As Map
            Private ReadOnly _player As IPlayerDownloadAspect
            Private _transfer As Transfer
            Private _lastTransferPartner As TransferClient

            Public Property LastSendPosition As UInt32
            Private _hasReported As Boolean
            Private _reportedState As Protocol.MapTransferState = Protocol.MapTransferState.Idle
            Private _expectedState As Protocol.MapTransferState = Protocol.MapTransferState.Idle
            Private _totalPastProgress As UInt64
            Private _totalPastTransferTime As TimeSpan
            Private _numTransfers As Integer = 0
            Private ReadOnly _clock As IClock
            Private ReadOnly _links As New List(Of TransferClient)
            Private ReadOnly _hooks As IReadableList(Of IFuture(Of IDisposable))
            Private _lastActivityTimer As RelativeClock
            Private _reportedPosition As UInt32

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_map IsNot Nothing)
                Contract.Invariant(_links IsNot Nothing)
                Contract.Invariant(_clock IsNot Nothing)
                Contract.Invariant(_hooks IsNot Nothing)
                Contract.Invariant(_transfer Is Nothing OrElse HasReported)
                Contract.Invariant(_lastActivityTimer IsNot Nothing)
            End Sub

            Public Sub New(ByVal player As IPlayerDownloadAspect,
                           ByVal map As Map,
                           ByVal clock As IClock,
                           ByVal hooks As IEnumerable(Of IFuture(Of IDisposable)))
                Contract.Requires(map IsNot Nothing)
                Contract.Requires(clock IsNot Nothing)
                Contract.Requires(hooks IsNot Nothing)
                Contract.Ensures(Not Me.HasReported)
                Me._map = map
                Me._player = player
                Me._clock = clock
                Me._hooks = hooks.ToReadableList
                Me._lastActivityTimer = _clock.Restarted
            End Sub

#Region "Naive Properties"
            Public ReadOnly Property Transfer As Transfer
                Get
                    Return _transfer
                End Get
            End Property
            Public ReadOnly Property Map As Map
                Get
                    Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)
                    Return _map
                End Get
            End Property
            Public ReadOnly Property LastTransferPartner As TransferClient
                Get
                    Return _lastTransferPartner
                End Get
            End Property
            Public ReadOnly Property Player As IPlayerDownloadAspect
                Get
                    Return _player
                End Get
            End Property
            Public ReadOnly Property Links As List(Of TransferClient)
                Get
                    Contract.Ensures(Contract.Result(Of List(Of TransferClient))() IsNot Nothing)
                    Return _links
                End Get
            End Property
            Public ReadOnly Property ExpectedState As Protocol.MapTransferState
                Get
                    Return _expectedState
                End Get
            End Property
#End Region

            Public ReadOnly Property TimeSinceLastActivity As TimeSpan
                Get
                    Return _lastActivityTimer.ElapsedTime
                End Get
            End Property
            Public ReadOnly Property LocalState As Protocol.MapTransferState
                Get
                    If _transfer Is Nothing Then Return Protocol.MapTransferState.Idle
                    Return If(ReportedHasFile,
                              Protocol.MapTransferState.Uploading,
                              Protocol.MapTransferState.Downloading)
                End Get
            End Property
            Public ReadOnly Property TotalMeasurementTime As TimeSpan
                Get
                    Return _totalPastTransferTime + If(_transfer IsNot Nothing, _transfer.Duration, 0.Seconds)
                End Get
            End Property
            Private ReadOnly Property TotalProgress As UInt64
                Get
                    Return _totalPastProgress + If(_transfer IsNot Nothing, _transfer.TotalProgress, 0UI)
                End Get
            End Property
            Public ReadOnly Property ReportedHasFile As Boolean
                Get
                    Contract.Requires(HasReported)
                    Return ReportedPosition = Map.FileSize
                End Get
            End Property
            Public ReadOnly Property HasReported As Boolean
                Get
                    Return _hasReported
                End Get
            End Property
            <ContractVerification(False)>
            Public Property ReportedState As Protocol.MapTransferState
                Get
                    Contract.Requires(HasReported)
                    Return _reportedState
                End Get
                <ContractVerification(False)>
                Set(ByVal value As Protocol.MapTransferState)
                    Contract.Requires(HasReported)
                    _reportedState = value
                End Set
            End Property
            Public Property ReportedPosition As UInt32
                Get
                    Contract.Requires(HasReported)
                    Return _reportedPosition
                End Get
                <ContractVerification(False)>
                Set(ByVal value As UInt32)
                    Contract.Requires(HasReported)
                    _reportedPosition = value
                End Set
            End Property
            Public ReadOnly Property IsSteady As Boolean
                Get
                    If TimeSinceLastActivity >= ForceSteadyPeriod Then Return True
                    If _lastTransferPartner IsNot Nothing AndAlso _lastTransferPartner.Player Is Nothing Then Return True
                    Return HasReported AndAlso ExpectedState = ReportedState
                End Get
            End Property
            Public ReadOnly Property EstimatedBandwidthPerSecond As Double
                Get
                    If Not HasReported Then Return DefaultBandwidthPerSecond

                    Dim dt = TotalMeasurementTime.TotalSeconds
                    Dim dp = TotalProgress
                    If dt < 1 Then Return DefaultBandwidthPerSecond
                    Dim expansionFactor = 1 + 1 / (1 + _numTransfers)
                    Return dp / dt * expansionFactor
                End Get
            End Property
            <ContractVerification(False)>
            Public Function FindBestAvailableUploader() As TransferClient
                Contract.Ensures(Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().HasReported)
                Contract.Ensures(Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().Transfer Is Nothing)
                Contract.Ensures(Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().ReportedHasFile)
                Contract.Ensures(Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().IsSteady)
                Dim availableUploaders = From client In Links
                                         Where client.HasReported
                                         Where client.ReportedHasFile
                                         Where client.Transfer Is Nothing
                                         Where client.IsSteady
                Return availableUploaders.Max(
                    Function(e1, e2) (From sign In {If(LastTransferPartner Is e1, 0, 1) - If(LastTransferPartner Is e2, 0, 1),
                                                    Math.Sign(e1.EstimatedBandwidthPerSecond - e2.EstimatedBandwidthPerSecond),
                                                    Math.Sign(e2.Links.Count - e1.Links.Count)}
                                      Where sign <> 0).FirstOrDefault)
            End Function

            <ContractVerification(False)>
            Public Shared Function StartTransfer(ByVal downloader As TransferClient, ByVal uploader As TransferClient) As Transfer
                Contract.Requires(downloader IsNot Nothing)
                Contract.Requires(uploader IsNot Nothing)
                Contract.Requires(downloader.HasReported)
                Contract.Requires(uploader.HasReported)
                Contract.Requires(Not downloader.ReportedHasFile)
                Contract.Requires(uploader.ReportedHasFile)
                Contract.Requires(downloader.Transfer Is Nothing)
                Contract.Requires(uploader.Transfer Is Nothing)
                Contract.Requires(downloader.IsSteady)
                Contract.Requires(uploader.IsSteady)

                Contract.Ensures(Contract.Result(Of Transfer)() IsNot Nothing)
                Contract.Ensures(downloader.Transfer Is Contract.Result(Of Transfer)())
                Contract.Ensures(uploader.Transfer Is Contract.Result(Of Transfer)())
                Contract.Ensures(Contract.Result(Of Transfer)().Downloader Is downloader)
                Contract.Ensures(Contract.Result(Of Transfer)().Uploader Is uploader)

                Dim transfer = New Transfer(downloader:=downloader,
                                            uploader:=uploader,
                                            startingPosition:=downloader.ReportedPosition,
                                            Clock:=downloader._clock,
                                            FileSize:=downloader._map.FileSize)
                downloader._numTransfers += 1
                downloader._transfer = transfer
                downloader._expectedState = Protocol.MapTransferState.Downloading
                downloader._lastTransferPartner = uploader

                uploader._numTransfers += 1
                uploader._transfer = transfer
                uploader._expectedState = Protocol.MapTransferState.Uploading
                uploader._lastTransferPartner = downloader

                Return transfer
            End Function
            Public Sub ClearTransfer()
                Contract.Requires(Me.Transfer IsNot Nothing)
                Contract.Ensures(Me.Transfer Is Nothing)
                Contract.Assume(_transfer IsNot Nothing)
                _totalPastProgress += _transfer.TotalProgress
                _totalPastTransferTime += _transfer.Duration
                _expectedState = Protocol.MapTransferState.Idle
                _transfer = Nothing
            End Sub

            <ContractVerification(False)>
            Public Sub MarkReported()
                Contract.Ensures(HasReported)
                _lastActivityTimer = _lastActivityTimer.Restarted
                _hasReported = True
            End Sub

            Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
                If _transfer IsNot Nothing Then
                    _transfer.Dispose()
                    _transfer = Nothing
                End If
                Return (From hook In _hooks
                        Select hook.CallOnValueSuccess(Sub(value) value.Dispose())
                       ).Defuturized
            End Function

            Public Overrides Function ToString() As String
                Return If(_player IsNot Nothing, "Player: {0}".Frmt(_player.Name), "Host")
            End Function
        End Class

        Private ReadOnly _map As Map
        Private ReadOnly _logger As Logger
        Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))()
        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue()
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
            Contract.Invariant(_mapPieceSender IsNot Nothing)
            Contract.Invariant(_map IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
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
            If FutureDisposed.State <> FutureState.Unknown Then Throw New ObjectDisposedException(Me.GetType.Name)
            If Not _started.TryAcquire() Then Throw New InvalidOperationException("Already started.")

            Me._mapPieceSender = mapPieceSender

            If _allowUploads Then
                _defaultClient = New TransferClient(Nothing, _map, _clock, {})
                _defaultClient.MarkReported()
                _defaultClient.ReportedPosition = _map.FileSize
            End If

            _hooks.Add(_clock.AsyncRepeat(UpdatePeriod, Sub() inQueue.QueueAction(AddressOf OnTick)).Futurized)

            _hooks.Add(startPlayerHoldPoint.IncludeFutureHandler(Function(arg) inQueue.QueueFunc(
                                    Function() OnGameStartPlayerHold(arg)).Defuturized).Futurized)
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
                                                      "(ul:{0})".Frmt(client.Transfer.Downloader.Player.PID),
                                                      "(dl:{0})".Frmt(client.Transfer.Uploader.Player.PID))
                    Return If(client.ReportedHasFile, "(>>ul)", "(>>dl)")
                End If
            End Get
        End Property
        Public Function QueueGetClientLatencyDescription(ByVal player As IPlayerDownloadAspect, ByVal latencyDescription As String) As IFuture(Of String)
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(latencyDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
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
        Public Function QueueGetClientBandwidthDescription(ByVal player As IPlayerDownloadAspect) As IFuture(Of String)
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() ClientBandwidthDescription(player))
        End Function

        Public ReadOnly Property FileSize As UInt32
            Get
                Return _map.FileSize
            End Get
        End Property
#End Region

#Region "Game-Triggered"
        Private Function OnGameStartPlayerHold(ByVal player As IPlayerDownloadAspect) As IFuture
            Contract.Requires(player IsNot Nothing)

            Dim playerHooks = New List(Of IFuture(Of IDisposable))() From {
                    player.QueueAddPacketHandler(packetDefinition:=Protocol.Packets.ClientMapInfo,
                                                 handler:=Function(pickle) QueueOnReceiveClientMapInfo(player, pickle)),
                    player.QueueAddPacketHandler(packetDefinition:=Protocol.Packets.PeerConnectionInfo,
                                                 handler:=Function(pickle) QueueOnReceivePeerConnectionInfo(player, pickle))
                }


            Dim client = New TransferClient(player, _map, _clock, playerHooks)
            _playerClients(player) = client
            If _defaultClient IsNot Nothing Then client.Links.Add(_defaultClient)

            player.FutureDisposed.QueueCallOnSuccess(inQueue,
                Sub()
                    client.Dispose()
                    _playerClients.Remove(player)
                End Sub)

            Return playerHooks.Defuturized
        End Function
#End Region

#Region "Communication-Triggered"
        Private Function QueueOnReceiveClientMapInfo(ByVal player As IPlayerDownloadAspect, ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object))) As ifuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Dim vals = pickle.Value
            Return inQueue.QueueAction(Sub() OnReceiveClientMapInfo(player:=player,
                                                                    state:=CType(vals("transfer state"), Protocol.MapTransferState),
                                                                    position:=CUInt(vals("total downloaded"))))
        End Function
        Private Function QueueOnReceivePeerConnectionInfo(ByVal player As IPlayerDownloadAspect, ByVal pickle As IPickle(Of UInt16)) As ifuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
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
                                  Let pidFlag = 1UI << (peer.Player.PID.Index - 1)
                                  Where (flags And pidFlag) <> 0
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
                    uler.QueueSendPacket(Protocol.MakeSetUploadTarget(dler.PID, downloader.ReportedPosition))
                    dler.QueueSendPacket(Protocol.MakeSetDownloadSource(uler.PID))
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
                    dler.QueueSendPacket(Protocol.MakeOtherPlayerLeft(uler.PID, Protocol.PlayerLeaveReason.Disconnect))
                    uler.QueueSendPacket(Protocol.MakeOtherPlayerLeft(dler.PID, Protocol.PlayerLeaveReason.Disconnect))
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

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            If finalizing Then Return Nothing
            Return inQueue.QueueFunc(
                Function()
                    Dim results = New List(Of IFuture)()
                    For Each e In _hooks
                        results.Add(e.CallOnValueSuccess(Sub(value) value.Dispose()))
                    Next e
                    For Each e In AllClients
                        e.Dispose()
                        results.Add(e.FutureDisposed)
                    Next e
                    Return results.Defuturized
                End Function).Defuturized
        End Function
    End Class
End Namespace
