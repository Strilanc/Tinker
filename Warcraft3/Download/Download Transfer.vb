Imports Tinker.Pickling

Namespace WC3.Download
    Partial Public Class Manager
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
    End Class
End Namespace
