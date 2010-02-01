Imports Tinker.Pickling

Namespace WC3
    <ContractClass(GetType(IGameDownloadAspect.ContractClass))>
    Public Interface IGameDownloadAspect
        Function QueueCreatePlayersAsyncView(ByVal adder As Action(Of IGameDownloadAspect, IPlayerDownloadAspect),
                                             ByVal remover As Action(Of IGameDownloadAspect, IPlayerDownloadAspect)) As IFuture(Of IDisposable)
        Function QueueSendMapPiece(ByVal player As IPlayerDownloadAspect, ByVal position As UInt32) As IFuture
        ReadOnly Property Logger As Logger
        ReadOnly Property Map As Map

        <ContractClassFor(GetType(IGameDownloadAspect))>
        Class ContractClass
            Implements IGameDownloadAspect
            Public Function QueueCreatePlayersAsyncView(ByVal adder As Action(Of IGameDownloadAspect, IPlayerDownloadAspect),
                                                        ByVal remover As Action(Of IGameDownloadAspect, IPlayerDownloadAspect)) As IFuture(Of IDisposable) Implements IGameDownloadAspect.QueueCreatePlayersAsyncView
                Contract.Requires(adder IsNot Nothing)
                Contract.Requires(remover IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public Function QueueSendMapPiece(ByVal player As IPlayerDownloadAspect,
                                              ByVal position As UInteger) As IFuture Implements IGameDownloadAspect.QueueSendMapPiece
                Contract.Requires(player IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public ReadOnly Property Logger As Logger Implements IGameDownloadAspect.Logger
                Get
                    Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
            Public ReadOnly Property Map As Map Implements IGameDownloadAspect.Map
                Get
                    Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)
                    Throw New NotSupportedException
                End Get
            End Property
        End Class
    End Interface

    <ContractClass(GetType(IPlayerDownloadAspect.ContractClass))>
    Public Interface IPlayerDownloadAspect
        ReadOnly Property Name As InvariantString
        ReadOnly Property PID As PID
        Function QueueAddPacketHandler(Of T)(ByVal id As Protocol.PacketId,
                                             ByVal jar As IParseJar(Of T),
                                             ByVal handler As Func(Of IPickle(Of T), IFuture)) As IFuture(Of IDisposable)
        Function MakePacketOtherPlayerJoined() As Protocol.Packet
        Function QueueSendPacket(ByVal packet As Protocol.Packet) As IFuture

        <ContractClassFor(GetType(IPlayerDownloadAspect))>
        Class ContractClass
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
            Public ReadOnly Property PID As PID Implements IPlayerDownloadAspect.PID
                Get
                    Throw New NotSupportedException
                End Get
            End Property
            Public Function QueueAddPacketHandler(Of T)(ByVal id As Protocol.PacketId,
                                                        ByVal jar As IParseJar(Of T),
                                                        ByVal handler As Func(Of IPickle(Of T), IFuture)) As IFuture(Of IDisposable) _
                                                        Implements IPlayerDownloadAspect.QueueAddPacketHandler
                Contract.Requires(jar IsNot Nothing)
                Contract.Requires(handler IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
                Throw New NotSupportedException
            End Function
            Public Function QueueSendPacket(ByVal packet As Protocol.Packet) As IFuture Implements IPlayerDownloadAspect.QueueSendPacket
                Contract.Requires(packet IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
                Throw New NotSupportedException
            End Function
        End Class
    End Interface

    Public Class DownloadManager
        Inherits FutureDisposable

        Public Shared ReadOnly FreezePeriod As TimeSpan = 5.Seconds
        Public Shared ReadOnly MinSwitchPeriod As TimeSpan = 5.Seconds
        Public Shared ReadOnly SwitchPenaltyPeriod As TimeSpan = 1.Seconds
        Public Shared ReadOnly SwitchPenaltyFactor As Double = 1.2
        Public Shared ReadOnly TypicalBandwidthPerSecond As Double = 1024
        Public Shared ReadOnly MaxBufferedMapSize As Integer = 64000
        Public Shared ReadOnly UpdatePeriod As TimeSpan = 1.Seconds

        Private Class Transfer
            Implements IDisposable

            Private ReadOnly _fileSize As UInt32
            Private ReadOnly _downloader As TransferClient
            Private ReadOnly _uploader As TransferClient
            Private ReadOnly _durationTimer As ITimer
            Private ReadOnly _lastActivityTimer As ITimer
            Private ReadOnly _startingPosition As UInt32
            Public Property LastSentPosition As UInt32
            Private _totalProgress As UInt32

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_downloader IsNot Nothing)
                Contract.Invariant(_uploader IsNot Nothing)
                Contract.Invariant(_durationTimer IsNot Nothing)
                Contract.Invariant(_lastActivityTimer IsNot Nothing)
                Contract.Invariant(_startingPosition <= _fileSize)
            End Sub

            Public Sub New(ByVal downloader As TransferClient,
                           ByVal uploader As TransferClient,
                           ByVal startingPosition As UInt32,
                           ByVal filesize As UInt32,
                           ByVal clock As IClock)
                Contract.Requires(downloader IsNot Nothing)
                Contract.Requires(uploader IsNot Nothing)
                Contract.Requires(clock IsNot Nothing)
                Contract.Ensures(Me.Downloader Is downloader)
                Contract.Ensures(Me.Uploader Is uploader)
                Contract.Ensures(Me.StartingPosition = startingPosition)
                Me._downloader = downloader
                Me._uploader = uploader
                Me._fileSize = filesize
                Me._durationTimer = clock.StartTimer()
                Me._lastActivityTimer = clock.StartTimer()
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
#End Region

            Public ReadOnly Property BandwidthPerSecond As Double
                Get
                    Dim dt = Duration.TotalSeconds
                    If dt < 1 Then Return TypicalBandwidthPerSecond
                    Return _totalProgress / dt
                End Get
            End Property

            Public ReadOnly Property ExpectedDurationRemaining As TimeSpan
                Get
                    Return New TimeSpan(ticks:=CLng(TimeSpan.TicksPerSecond * ((_fileSize - LastSentPosition) / BandwidthPerSecond)))
                End Get
            End Property

            Public Sub Advance(ByVal progress As UInt32)
                _totalProgress += progress
                _lastActivityTimer.Reset()
            End Sub

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

            Public Property ReportedPosition As UInt32
            Private _hasReported As Boolean
            Private _reportedState As Protocol.MapTransferState = Protocol.MapTransferState.Idle
            Private _expectedState As Protocol.MapTransferState = Protocol.MapTransferState.Idle
            Private _totalPastProgress As UInt64
            Private _totalPastTransferTime As TimeSpan
            Private ReadOnly _clock As IClock
            Private _transferStartPosition As UInt32
            Private ReadOnly _links As New List(Of TransferClient)
            Private ReadOnly _hooks As IReadableList(Of IFuture(Of IDisposable))

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_map IsNot Nothing)
                Contract.Invariant(_links IsNot Nothing)
                Contract.Invariant(_clock IsNot Nothing)
                Contract.Invariant(_hooks IsNot Nothing)
            End Sub

            Public Sub New(ByVal player As IPlayerDownloadAspect,
                           ByVal map As Map,
                           ByVal clock As IClock,
                           ByVal hooks As IEnumerable(Of IFuture(Of IDisposable)))
                Contract.Requires(map IsNot Nothing)
                Contract.Requires(clock IsNot Nothing)
                Contract.Requires(hooks IsNot Nothing)
                Me._map = map
                Me._player = player
                Me._clock = clock
                Me._hooks = hooks.ToArray.AsReadableList
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
                    Contract.Ensures(Contract.Result(Of IEnumerable(Of TransferClient))() IsNot Nothing)
                    Return _links
                End Get
            End Property
            Public ReadOnly Property ExpectedState As Protocol.MapTransferState
                Get
                    Return _expectedState
                End Get
            End Property
#End Region

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
            Public Property HasReported As Boolean
                Get
                    Return _hasReported
                End Get
                Set(ByVal value As Boolean)
                    Contract.Requires(Not HasReported)
                    Contract.Requires(value)
                    _hasReported = value
                End Set
            End Property
            Public Property ReportedState As Protocol.MapTransferState
                Get
                    Contract.Requires(HasReported)
                    Return _reportedState
                End Get
                Set(ByVal value As Protocol.MapTransferState)
                    _reportedState = value
                End Set
            End Property
            Public ReadOnly Property IsSteady As Boolean
                Get
                    Return HasReported AndAlso ExpectedState = ReportedState
                End Get
            End Property
            Public ReadOnly Property EstimatedBandwidthPerSecond As Double
                Get
                    If Not HasReported Then Return TypicalBandwidthPerSecond

                    Dim dt = TotalMeasurementTime.TotalSeconds
                    Dim dp = TotalProgress
                    If dt < 1 Then Return TypicalBandwidthPerSecond
                    Return dp / dt
                End Get
            End Property
            Public Function FindBestAvailableUploader() As TransferClient
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

            Public Shared Sub StartTransfer(ByVal downloader As TransferClient, ByVal uploader As TransferClient)
                Contract.Requires(downloader IsNot Nothing AndAlso uploader IsNot Nothing)
                Contract.Requires(downloader.HasReported AndAlso uploader.HasReported)
                Contract.Requires(uploader.ReportedHasFile AndAlso Not downloader.ReportedHasFile)
                Contract.Requires(downloader.Transfer Is Nothing AndAlso uploader.Transfer Is Nothing)
                Contract.Requires(downloader.IsSteady AndAlso uploader.IsSteady)

                Contract.Ensures(downloader.Transfer IsNot Nothing)
                Contract.Ensures(uploader.Transfer Is downloader.Transfer)
                Contract.Ensures(downloader.Transfer.Downloader Is downloader AndAlso downloader.Transfer.Uploader Is uploader)

                Dim transfer = New Transfer(downloader:=downloader,
                                            uploader:=uploader,
                                            startingPosition:=downloader.ReportedPosition,
                                            Clock:=downloader._clock,
                                            FileSize:=downloader._map.FileSize)
                downloader._transfer = transfer
                uploader._transfer = transfer
                downloader._expectedState = Protocol.MapTransferState.Downloading
                uploader._expectedState = Protocol.MapTransferState.Uploading
                downloader._lastTransferPartner = uploader
                uploader._lastTransferPartner = downloader
            End Sub
            Public Sub ClearTransfer()
                Contract.Requires(Me.Transfer IsNot Nothing)
                Contract.Ensures(Me.Transfer Is Nothing)
                _totalPastProgress += _transfer.TotalProgress
                _expectedState = Protocol.MapTransferState.Idle
                _transfer = Nothing
            End Sub

            Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
                If _transfer IsNot Nothing Then _transfer.Dispose()
                Contract.Assert(_transfer Is Nothing)
                Return (From hook In _hooks
                        Select hook.CallOnValueSuccess(Sub(value) value.Dispose())
                       ).Defuturized
            End Function

            Public Overrides Function ToString() As String
                Return If(_player IsNot Nothing, "Player: {0}".Frmt(_player.Name), "Host")
            End Function
        End Class

        Private ReadOnly _game As IGameDownloadAspect
        Private ReadOnly _hooks As New List(Of IFuture(Of IDisposable))()
        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue()
        Private ReadOnly _allowDownloads As Boolean
        Private ReadOnly _allowUploads As Boolean
        Private ReadOnly _playerClients As New Dictionary(Of IPlayerDownloadAspect, TransferClient)
        Private ReadOnly _defaultClient As TransferClient
        Private ReadOnly _clock As IClock

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_playerClients IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
        End Sub

        Public Sub New(ByVal clock As IClock,
                       ByVal game As IGameDownloadAspect,
                       ByVal allowDownloads As Boolean,
                       ByVal allowUploads As Boolean)
            Contract.Requires(clock IsNot Nothing)
            Contract.Requires(game IsNot Nothing)
            Me._clock = clock
            Me._game = game
            Me._allowDownloads = allowDownloads
            Me._allowUploads = allowUploads
            If _allowUploads Then
                _defaultClient = New TransferClient(Nothing, game.Map, clock, {})
                _defaultClient.HasReported = True
                _defaultClient.ReportedPosition = game.Map.FileSize
            End If

            _hooks.Add(game.QueueCreatePlayersAsyncView(
                                adder:=Sub(sender, player) inQueue.QueueAction(Sub() OnGameAddedPlayer(player)),
                                remover:=Sub(sender, player) inQueue.QueueAction(Sub() OnGameRemovedPlayer(player))))

            _hooks.Add(clock.AsyncRepeat(UpdatePeriod, Sub() inQueue.QueueAction(AddressOf OnTick)).Futurized)
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
            Dim transfer = client.Transfer
            transfer.LastSentPosition = Math.Max(transfer.LastSentPosition, reportedPosition)
            While transfer.LastSentPosition < reportedPosition + MaxBufferedMapSize AndAlso transfer.LastSentPosition < FileSize
                _game.QueueSendMapPiece(client.Player, transfer.LastSentPosition)
                transfer.LastSentPosition += Protocol.Packets.MaxFileDataSize
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

                If client.Transfer Is Nothing Then
                    If client.IsSteady Then Return latencyDescription
                    Return If(client.ReportedHasFile, "(..ul)", "(..dl)")
                Else
                    If client.IsSteady Then Return If(client.ReportedHasFile, "(ul)", "(dl)")
                    Return If(client.ReportedHasFile, "(ul..)", "(dl..)")
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
                Return _game.Map.FileSize
            End Get
        End Property
#End Region

#Region "Game-Triggered"
        Private Sub OnGameAddedPlayer(ByVal player As IPlayerDownloadAspect)
            Contract.Requires(player IsNot Nothing)

            Dim playerHooks = New List(Of IFuture(Of IDisposable))() From {
                    player.QueueAddPacketHandler(id:=Protocol.PacketId.ClientMapInfo,
                                                 jar:=Protocol.Packets.ClientMapInfo,
                                                 handler:=Function(pickle) QueueOnReceiveClientMapInfo(player, pickle)),
                    player.QueueAddPacketHandler(id:=Protocol.PacketId.MapFileDataReceived,
                                                 jar:=Protocol.Packets.MapFileDataReceived,
                                                 handler:=Function(pickle) QueueOnReceiveMapFileDataReceived(player, pickle)),
                    player.QueueAddPacketHandler(id:=Protocol.PacketId.PeerConnectionInfo,
                                                 jar:=Protocol.Packets.PeerConnectionInfo,
                                                 handler:=Function(pickle) QueueOnReceivePeerConnectionInfo(player, pickle))
                }

            _playerClients(player) = New TransferClient(player, _game.Map, _clock, playerHooks)
            If _defaultClient IsNot Nothing Then _playerClients(player).Links.Add(_defaultClient)
        End Sub
        Private Sub OnGameRemovedPlayer(ByVal player As IPlayerDownloadAspect)
            Contract.Requires(player IsNot Nothing)
            _playerClients(player).Dispose()
            _playerClients.Remove(player)
        End Sub
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
        Private Function QueueOnReceiveMapFileDataReceived(ByVal player As IPlayerDownloadAspect, ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object))) As ifuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Dim vals = pickle.Value
            Return inQueue.QueueAction(Sub() OnReceiveMapFileDataReceived(player:=player,
                                                                          position:=CUInt(vals("total downloaded"))))
        End Function
        Private Function QueueOnReceivePeerConnectionInfo(ByVal player As IPlayerDownloadAspect, ByVal pickle As IPickle(Of Dictionary(Of InvariantString, Object))) As ifuture
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Dim vals = pickle.Value
            Return inQueue.QueueAction(Sub() OnReceivePeerConnectionInfo(player:=player,
                                                                         flags:=CUInt(vals("player bitflags"))))
        End Function

        Private Sub OnReceivePeerConnectionInfo(ByVal player As IPlayerDownloadAspect, ByVal flags As UInt32)
            Contract.Requires(player IsNot Nothing)
            If Not _playerClients.ContainsKey(player) Then Return

            Dim client = _playerClients(player)
            client.Links.Clear()
            If _defaultClient IsNot Nothing Then client.Links.Add(_defaultClient)
            client.Links.AddRange(From peer In AllClients
                                  Where peer IsNot client
                                  Let pidFlag = 1UI << (peer.Player.PID.Index - 1)
                                  Where (flags And pidFlag) <> 0
                                  Select peer)
        End Sub
        Private Sub OnReceiveMapFileDataReceived(ByVal player As IPlayerDownloadAspect, ByVal position As UInt32)
            Contract.Requires(player IsNot Nothing)
            If Not _playerClients.ContainsKey(player) Then Return

            Dim client = _playerClients(player)
            Dim transfer = client.Transfer
            If position > _game.Map.FileSize Then
                Throw New IO.InvalidDataException("Moved download position past end of file at {0} to {1}.".Frmt(_game.Map.FileSize, position))
            ElseIf position < client.ReportedPosition Then
                Throw New IO.InvalidDataException("Moved download position backwards from {0} to {1}.".Frmt(client.ReportedPosition, position))
            End If

            If transfer IsNot Nothing AndAlso transfer.Uploader Is _defaultClient Then
                SendMapFileData(client, position)
            End If
        End Sub
        Private Sub OnReceiveClientMapInfo(ByVal player As IPlayerDownloadAspect, ByVal state As Protocol.MapTransferState, ByVal position As UInt32)
            Contract.Requires(player IsNot Nothing)
            If Not _playerClients.ContainsKey(player) Then Return

            Dim client = _playerClients(player)
            If position > _game.Map.FileSize Then
                Throw New IO.InvalidDataException("Moved download position past end of file at {0} to {1}.".Frmt(_game.Map.FileSize, position))
            ElseIf position < client.ReportedPosition Then
                Throw New IO.InvalidDataException("Moved download position backwards from {0} to {1}.".Frmt(client.ReportedPosition, position))
            ElseIf Not client.HasReported AndAlso state <> Protocol.MapTransferState.Idle Then
                Throw New IO.InvalidDataException("Invalid initial download transfer state.")
            ElseIf client.HasReported AndAlso client.ReportedHasFile AndAlso state = Protocol.MapTransferState.Downloading Then
                Throw New IO.InvalidDataException("Non-downloader reported status as downloading.")
            ElseIf (Not client.HasReported OrElse Not client.ReportedHasFile) AndAlso state = Protocol.MapTransferState.Uploading Then
                Throw New IO.InvalidDataException("Non-uploader reported status as uploading.")
            End If

            If Not client.HasReported Then
                client.HasReported = True
                client.ReportedPosition = position
                client.ReportedState = state
                If Not _allowDownloads AndAlso Not client.ReportedHasFile Then
                    Throw New IO.InvalidDataException("Downloads not allowed.")
                End If
            Else
                If client.ReportedState = Protocol.MapTransferState.Downloading Then
                    client.Transfer.Advance(position - client.ReportedPosition)
                End If
                If state = Protocol.MapTransferState.Idle AndAlso client.Transfer IsNot Nothing Then
                    client.Transfer.Dispose()
                End If

                client.ReportedPosition = position
                client.ReportedState = state
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
                                 Select t = client.Transfer
                                 Where t.Duration > MinSwitchPeriod
                                 Where t.ExpectedDurationRemaining > MinSwitchPeriod
                Dim downloader = transfer.Downloader
                Dim remainingData = _game.Map.FileSize - transfer.LastSentPosition
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
                Dim bestAvailableUploader = downloader.FindBestAvailableUploader()
                If bestAvailableUploader Is Nothing Then Continue For

                Dim dler = downloader.Player
                Dim uler = bestAvailableUploader.Player
                TransferClient.StartTransfer(downloader, bestAvailableUploader)

                If uler IsNot Nothing Then
                    uler.QueueSendPacket(Protocol.MakeSetUploadTarget(dler.PID, downloader.ReportedPosition))
                    dler.QueueSendPacket(Protocol.MakeSetDownloadSource(uler.PID))
                End If

                If uler Is Nothing Then
                    _game.Logger.Log("Initiating map upload to {0}.".Frmt(dler.Name), LogMessageType.Positive)
                    SendMapFileData(downloader, downloader.ReportedPosition)
                Else
                    _game.Logger.Log("Initiating peer map transfer from {0} to {1}.".Frmt(uler.Name, dler.Name), LogMessageType.Positive)
                End If
            Next downloader

            'Find and cancel one frozen or improvable transfer, if there are any
            Dim cancellation = If(TryFindFrozenTransfer(), TryFindImprovableTransfer())
            If cancellation IsNot Nothing Then
                Dim dler = cancellation.Downloader.Player
                Dim uler = cancellation.Uploader.Player
                cancellation.Dispose()

                'Force-cancel the transfer by making the uploader and downloader each think the other rejoined
                If uler IsNot Nothing Then
                    dler.QueueSendPacket(Protocol.MakeOtherPlayerLeft(uler.PID, PlayerLeaveType.Disconnect))
                    uler.QueueSendPacket(Protocol.MakeOtherPlayerLeft(dler.PID, PlayerLeaveType.Disconnect))
                    dler.QueueSendPacket(uler.MakePacketOtherPlayerJoined())
                    uler.QueueSendPacket(dler.MakePacketOtherPlayerJoined())
                End If

                If uler Is Nothing Then
                    _game.Logger.Log("Cancelled map upload to {0}.".Frmt(dler.Name), LogMessageType.Positive)
                Else
                    _game.Logger.Log("Cancelled peer map transfer from {0} to {1}.".Frmt(uler.Name, dler.Name), LogMessageType.Positive)
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
