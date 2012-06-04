Imports Tinker.Pickling

Namespace WC3.Download
    <DebuggerDisplay("{ToString()}")>
    Friend Class TransferClient
        Inherits DisposableWithTask

        Private ReadOnly _map As Map
        Private ReadOnly _player As IPlayerDownloadAspect
        Private ReadOnly _clock As IClock
        Private ReadOnly _links As New List(Of TransferClient)
        Private ReadOnly _hooks As IRist(Of Task(Of IDisposable))

        Private _hasReported As Boolean
        Private _reportedPosition As UInt32
        Private _reportedState As Protocol.MapTransferState = Protocol.MapTransferState.Idle

        Private _transfer As Transfer
        Private _expectedState As Protocol.MapTransferState = Protocol.MapTransferState.Idle
        Private _lastActivityClock As ClockTimer
        Public Property LastSendPosition As UInt32

        Private _lastTransferPartner As TransferClient
        Private _totalPastProgress As UInt64
        Private _totalPastTransferTime As TimeSpan
        Private _pastTransferCount As Integer = 0

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_map IsNot Nothing)
            Contract.Invariant(_links IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(_transfer Is Nothing OrElse HasReported)
            Contract.Invariant(_lastActivityClock IsNot Nothing)
            Contract.Invariant(_reportedPosition <= _map.FileSize)
        End Sub

        Public Sub New(player As IPlayerDownloadAspect,
                       map As Map,
                       clock As IClock,
                       hooks As IEnumerable(Of Task(Of IDisposable)))
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Contract.Requires(hooks IsNot Nothing)
            Contract.Ensures(Not Me.HasReported)
            Me._map = map
            Me._player = player
            Me._clock = clock
            Me._hooks = hooks.ToRist
            Me._lastActivityClock = _clock.StartTimer()
        End Sub

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

        Public ReadOnly Property TimeSinceLastActivity As TimeSpan
            Get
                Return _lastActivityClock.ElapsedTime
            End Get
        End Property
        <SuppressMessage("Microsoft.Contracts", "Requires-6-11")>
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
        Public Property ReportedState As Protocol.MapTransferState
            Get
                Contract.Requires(HasReported)
                Return _reportedState
            End Get
            Set(value As Protocol.MapTransferState)
                Contract.Requires(HasReported)
                _reportedState = value
            End Set
        End Property
        Public Property ReportedPosition As UInt32
            Get
                Contract.Requires(HasReported)
                Contract.Ensures(Contract.Result(Of UInt32)() <= Map.FileSize)
                Contract.Assume(_reportedPosition <= Map.FileSize)
                Return _reportedPosition
            End Get
            Set(value As UInt32)
                Contract.Requires(HasReported)
                Contract.Requires(value <= Map.FileSize)
                Contract.Assume(value <= _map.FileSize)
                _reportedPosition = value
            End Set
        End Property
        Public ReadOnly Property IsSteady As Boolean
            Get
                If TimeSinceLastActivity >= Manager.ForceSteadyPeriod Then Return True
                If _lastTransferPartner IsNot Nothing AndAlso _lastTransferPartner.Player Is Nothing Then Return True
                Return HasReported AndAlso ExpectedState = ReportedState
            End Get
        End Property
        Public ReadOnly Property EstimatedBandwidthPerSecond As Double
            Get
                If Not HasReported Then Return Manager.DefaultBandwidthPerSecond

                Dim dt = TotalMeasurementTime.TotalSeconds
                Dim dp = TotalProgress
                If dt < 1 Then Return Manager.DefaultBandwidthPerSecond
                Dim expansionFactor = 1 + 1 / (1 + _pastTransferCount)
                Return dp / dt * expansionFactor
            End Get
        End Property
        <Pure()>
        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().HasReported")>
        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().Transfer Is Nothing")>
        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().ReportedHasFile")>
        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().IsSteady")>
        Public Function TryFindBestAvailableUploader() As TransferClient
            Contract.Ensures(Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().HasReported)
            Contract.Ensures(Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().Transfer Is Nothing)
            Contract.Ensures(Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().ReportedHasFile)
            Contract.Ensures(Contract.Result(Of TransferClient)() Is Nothing OrElse Contract.Result(Of TransferClient)().IsSteady)
            Dim availableUploaders = From client In Links
                                     Where client.HasReported
                                     Where client.ReportedHasFile
                                     Where client.Transfer Is Nothing
                                     Where client.IsSteady
            If availableUploaders.None Then Return Nothing
            Contract.Assume(availableUploaders.Any())
            Return availableUploaders.Max(
                Function(e1, e2) (From sign In {If(LastTransferPartner Is e1, 0, 1) - If(LastTransferPartner Is e2, 0, 1),
                                                Math.Sign(e1.EstimatedBandwidthPerSecond - e2.EstimatedBandwidthPerSecond),
                                                Math.Sign(e2.Links.Count - e1.Links.Count)}
                                  Where sign <> 0).FirstOrDefault)
        End Function

        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of Transfer)().Downloader Is downloader")>
        <SuppressMessage("Microsoft.Contracts", "EnsuresInMethod-Contract.Result(Of Transfer)().Uploader Is uploader")>
        Public Shared Function StartTransfer(downloader As TransferClient, uploader As TransferClient) As Transfer
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
                                        Clock:=downloader._clock.AssumeNotNull(),
                                        FileSize:=downloader.Map.FileSize)
            downloader._pastTransferCount += 1
            downloader._transfer = transfer
            downloader._expectedState = Protocol.MapTransferState.Downloading
            downloader._lastTransferPartner = uploader

            uploader._pastTransferCount += 1
            uploader._transfer = transfer
            uploader._expectedState = Protocol.MapTransferState.Uploading
            uploader._lastTransferPartner = downloader

            Return transfer
        End Function
        Public Sub ClearTransfer()
            Contract.Requires(Me.Transfer IsNot Nothing)
            Contract.Ensures(Me.Transfer Is Nothing)
            _totalPastProgress += _transfer.TotalProgress
            _totalPastTransferTime += _transfer.Duration
            _expectedState = Protocol.MapTransferState.Idle
            _transfer = Nothing
        End Sub

        Public Sub MarkReported()
            Contract.Ensures(HasReported)
            _lastActivityClock = _lastActivityClock.Restarted
            _hasReported = True
        End Sub

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If _transfer IsNot Nothing Then
                _transfer.Dispose()
                _transfer = Nothing
            End If
            Return _hooks.DisposeAllAsync()
        End Function

        Public Overrides Function ToString() As String
            Return If(_player IsNot Nothing, "Player: {0}".Frmt(_player.Name), "Host")
        End Function
    End Class
End Namespace
