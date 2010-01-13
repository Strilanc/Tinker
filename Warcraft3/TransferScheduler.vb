Public Enum ClientTransferState
    None
    ''' <summary>Doesn't have file but not transfering.</summary>
    Idle
    ''' <summary>Has file but not transfering.</summary>
    Ready
    ''' <summary>Doesn't have file and is transfering.</summary>
    Downloading
    ''' <summary>Has file and is transfering.</summary>
    Uploading
End Enum

'''<summary>Schedules transfers for sharing a copyable resource among multiple clients.</summary>
Public NotInheritable Class TransferScheduler(Of TClientKey)
    Private ReadOnly typicalRate As FiniteDouble
    Private ReadOnly clients As New Dictionary(Of TClientKey, TransferClient)
    Private ReadOnly ref As ICallQueue
    Private ReadOnly fileSize As FiniteDouble
    Private ReadOnly typicalSwitchTime As TimeSpan
    Private ReadOnly minSwithPeriod As TimeSpan
    Private ReadOnly freezePeriod As TimeSpan
    Private Const SwitchRatePenaltyFactor As Double = 1.1
    Private ReadOnly _clock As IClock

    Public Event Actions(ByVal started As List(Of TransferEndpoints), ByVal stopped As List(Of TransferEndpoints))

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(clients IsNot Nothing)
        Contract.Invariant(ref IsNot Nothing)
        Contract.Invariant(_clock IsNot Nothing)
        'Contract.Invariant(fileSize > 0)
        'Contract.Invariant(typicalRate > 0)
        'Contract.Invariant(typicalSwitchTime >= 0)
    End Sub

    Public NotInheritable Class TransferEndpoints
        Public ReadOnly source As TClientKey
        Public ReadOnly destination As TClientKey
        Public Sub New(ByVal source As TClientKey,
                       ByVal destination As TClientKey)
            Me.source = source
            Me.destination = destination
        End Sub
    End Class

    Public Sub New(ByVal typicalRate As FiniteDouble,
                   ByVal typicalSwitchTime As TimeSpan,
                   ByVal fileSize As FiniteDouble,
                   ByVal clock As IClock,
                   Optional ByVal minSwitchPeriodMilliseconds As Integer = 3000,
                   Optional ByVal freezePeriodMilliseconds As Integer = 5000,
                   Optional ByVal pq As ICallQueue = Nothing)
        Contract.Assume(fileSize > 0) 'bug in contracts required not using requires here
        Contract.Assume(typicalRate > 0)
        Contract.Assume(typicalSwitchTime.Ticks >= 0)
        Contract.Assume(clock IsNot Nothing)
        Me.ref = If(pq, New TaskedCallQueue)
        Me.minSwithPeriod = minSwitchPeriodMilliseconds.Milliseconds
        Me.freezePeriod = freezePeriodMilliseconds.Milliseconds
        Me.typicalRate = typicalRate
        Me.typicalSwitchTime = typicalSwitchTime
        Me.fileSize = fileSize
        Me._clock = clock
    End Sub

    '''<summary>Adds a new client to the pool.</summary>
    Public Function AddClient(ByVal clientKey As TClientKey,
                              ByVal completed As Boolean,
                              Optional ByVal expectedRate As FiniteDouble = Nothing) As IFuture
        Contract.Requires(clientKey IsNot Nothing)
        'Contract.Requires(expectedRate = Nothing OrElse expectedRate > 0)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        If expectedRate = Nothing Then expectedRate = typicalRate
        Return ref.QueueAction(
            Sub()
                If clients.ContainsKey(clientKey) Then Throw New InvalidOperationException("client key already exists")
                Contract.Assume(expectedRate > 0)
                clients(clientKey) = New TransferClient(clientKey, completed, expectedRate, _clock)
            End Sub
        )
    End Function

    '''<summary>Adds a link between two clients.</summary>
    Public Function SetLink(ByVal clientKey1 As TClientKey,
                            ByVal clientKey2 As TClientKey,
                            ByVal linked As Boolean) As IFuture
        Contract.Requires(clientKey1 IsNot Nothing)
        Contract.Requires(clientKey2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(
            Sub()
                If Not clients.ContainsKey(clientKey1) Then  Throw New InvalidOperationException("No such client key")
                If Not clients.ContainsKey(clientKey2) Then  Throw New InvalidOperationException("No such client key")
                Dim client1 = clients(clientKey1)
                Dim client2 = clients(clientKey2)
                If client1.links.Contains(client2) = linked Then  Return
                If linked Then
                    client1.links.Add(client2)
                    client2.links.Add(client1)
                Else
                    client1.links.Remove(client2)
                    client2.links.Remove(client1)
                    If client1.Busy AndAlso client1.other Is client2 Then
                        SetNotTransfering(clientKey1, False)
                    End If
                End If
            End Sub
        )
    End Function

    '''<summary>Removes a client from the pool.</summary>
    Public Function RemoveClient(ByVal clientKey As TClientKey) As IFuture
        Contract.Requires(clientKey IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(
            Sub()
                If Not clients.ContainsKey(clientKey) Then  Throw New InvalidOperationException("No such client.")
                Dim client = clients(clientKey)
                If client.Busy Then
                    Dim other = client.other
                    client.SetNotTransfering(False)
                    other.SetNotTransfering(False)
                End If
                clients.Remove(clientKey)
                For Each linkedClient In client.links
                    linkedClient.links.Remove(client)
                Next linkedClient
            End Sub
        )
    End Function

    Public ReadOnly Property GenerateRateDescription(ByVal clientKey As TClientKey) As IFuture(Of String)
        Get
            Contract.Requires(clientKey IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
            Return ref.QueueFunc(
                Function()
                    If Not clients.ContainsKey(clientKey) Then Return "?"
                    With clients(clientKey)
                        If Not .HasRateBeenMeasured AndAlso Not .Busy Then Return "?"

                        Dim d = .GetCurRateEstimate * 1000
                        Dim f = New FiniteDouble(1.0)
                        For Each s In {"B", "KiB", "MiB", "GiB", "TiB", "PiB"}
                            Dim f2 = f * 1024
                            If d < f2 Then
                                Contract.Assume(f <> 0)
                                Return "{0:0.0} {1}/s".Frmt((d / f).Value, s)
                            End If
                            f = f2
                        Next s

                        Return ">HiB/s" '... What? It could happen...
                    End With
                End Function)
        End Get
    End Property

    Public Function GetClientState(ByVal clientKey As TClientKey) As IFuture(Of ClientTransferState)
        Contract.Requires(clientKey IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of ClientTransferState))() IsNot Nothing)
        Return ref.QueueFunc(
            Function()
                If Not clients.ContainsKey(clientKey) Then Return ClientTransferState.None
                Dim client = clients(clientKey)
                If client.Busy Then
                    If client.completed Then
                        Return ClientTransferState.Uploading
                    Else
                        Return ClientTransferState.Downloading
                    End If
                Else
                    If client.completed Then
                        Return ClientTransferState.Ready
                    Else
                        Return ClientTransferState.Idle
                    End If
                End If
            End Function)
    End Function

    '''<summary>Updates the progress of a transfer to or from the given client.</summary>
    Public Function UpdateProgress(ByVal clientKey As TClientKey,
                                   ByVal progress As FiniteDouble) As IFuture
        Contract.Requires(clientKey IsNot Nothing)
        Contract.Requires(progress >= 0)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(
            Sub()
                If Not clients.ContainsKey(clientKey) Then  Throw New InvalidOperationException("No such client key.")
                Dim client = clients(clientKey)
                If Not client.Busy Then  Throw New InvalidOperationException("Client isn't transfering.")
                Contract.Assume(progress >= 0)
                client.UpdateProgress(progress)
                client.other.UpdateProgress(progress)
            End Sub
        )
    End Function

    '''<summary>Stops any transfers to or from the given client. Marks the destination as completed if completed is set.</summary>
    Public Function SetNotTransfering(ByVal clientKey As TClientKey,
                                      ByVal completed As Boolean) As IFuture
        Contract.Requires(clientKey IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(
            Sub()
                If Not clients.ContainsKey(clientKey) Then Throw New InvalidOperationException("No such client key.")

                Dim client = clients(clientKey)
                Dim other = client.other
                If other IsNot Nothing Then
                    other.SetNotTransfering(nowFinished:=completed)
                End If
                client.SetNotTransfering(nowFinished:=completed)
            End Sub
        )
    End Function

    Public Function Update() As ifuture
        Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
        Return ref.QueueAction(
            Sub()
                Dim transfers = New List(Of TransferEndpoints)
                Dim breaks = New List(Of TransferEndpoints)

                'Match downloaders to uploaders
                For Each downloader In (From client In clients.Values Where Not client.completed)
                    Dim curUploader = downloader.other
                    Dim availableUploaders = From e In downloader.links Where e.completed AndAlso Not e.Busy
                    If availableUploaders.None AndAlso curUploader Is Nothing Then Continue For

                    Dim bestUploader = availableUploaders.Max(Function(e1, e2)
                                                                  Dim n = Math.Sign(e1.GetMaxRateEstimate - e2.GetMaxRateEstimate)
                                                                  If n <> 0 Then Return n
                                                                  Return Math.Sign(e2.links.Count - e1.links.Count)
                                                              End Function)

                    If curUploader Is Nothing Then
                        'Start transfer
                        bestUploader.SetTransfering(downloader.GetLastProgress, downloader)
                        downloader.SetTransfering(downloader.GetLastProgress, bestUploader)
                        transfers.Add(New TransferEndpoints(bestUploader.key, downloader.key))
                    ElseIf downloader.TimeSinceLastActivity >= freezePeriod Then
                        'Stop transfer if it appears to have frozen
                        breaks.Add(New TransferEndpoints(curUploader.key, downloader.key))
                    ElseIf bestUploader IsNot Nothing AndAlso downloader.GetTransferingTime >= minSwithPeriod Then
                        'Stop transfer if a significantly better uploader is available
                        Dim remainingData = fileSize - downloader.GetLastProgress
                        Dim curExpectedTime = remainingData / downloader.GetCurRateEstimate
                        Dim newExpectedTime = New FiniteDouble(typicalSwitchTime.TotalMilliseconds) + SwitchRatePenaltyFactor * remainingData / FiniteDouble.Min(downloader.GetMaxRateEstimate, bestUploader.GetMaxRateEstimate)
                        If newExpectedTime < curExpectedTime Then
                            breaks.Add(New TransferEndpoints(curUploader.key, downloader.key))
                        End If
                    End If
                Next downloader

                'Stop transfers seen as improvable
                '[Done here instead of in the above loop to avoid potentially giving two commands to clients at the same time]
                For Each e In breaks
                    clients(e.source).SetNotTransfering(nowFinished:=False)
                    clients(e.destination).SetNotTransfering(nowFinished:=False)
                Next e

                'Report actions to the outside
                If transfers.Any OrElse breaks.Any Then
                    RaiseEvent Actions(transfers, breaks)
                End If
            End Sub
        )
    End Function

    <DebuggerDisplay("{ToString}")>
    Private NotInheritable Class TransferClient
        Private maxRateEstimate As FiniteDouble
        Private curRateEstimate As FiniteDouble

        Private _lastEstimateTimer As ITimer
        Private _lastStartTimer As ITimer
        Private _progressAtLastUpdate As FiniteDouble
        Private _progressAtLastEstimate As FiniteDouble
        Private _progressAtLastStart As FiniteDouble
        Private numMeasurements As Integer

        Public ReadOnly key As TClientKey
        Public other As TransferClient = Nothing
        Private ReadOnly _links As New HashSet(Of TransferClient)
        Public completed As Boolean

        Private Shared ReadOnly LiveEstimateConvergence As New FiniteDouble(0.6)

        Public ReadOnly Property links As HashSet(Of TransferClient)
            Get
                Contract.Ensures(Contract.Result(Of HashSet(Of TransferClient))() IsNot Nothing)
                Return _links
            End Get
        End Property
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            'Contract.Invariant(maxRateEstimate > 0)
            'Contract.Invariant(curRateEstimate > 0)
            'Contract.Invariant(lastUpdateProgress >= 0)
            'Contract.Invariant(lastStartProgress >= 0)
            Contract.Invariant(_lastEstimateTimer IsNot Nothing)
            Contract.Invariant(_lastStartTimer IsNot Nothing)
            Contract.Invariant(_links IsNot Nothing)
            Contract.Invariant(key IsNot Nothing)
        End Sub

        Public Sub New(ByVal key As TClientKey,
                       ByVal completed As Boolean,
                       ByVal typicalRatePerMillisecond As FiniteDouble,
                       ByVal clock As IClock)
            Contract.Requires(key IsNot Nothing)
            Contract.Requires(typicalRatePerMillisecond > 0)
            Contract.Requires(clock IsNot Nothing)
            Me.key = key
            Me.completed = completed
            Me._lastEstimateTimer = clock.StartTimer
            Me._lastStartTimer = clock.StartTimer
            Me.maxRateEstimate = typicalRatePerMillisecond
            Me.curRateEstimate = typicalRatePerMillisecond
            Contract.Assume(_progressAtLastStart >= 0)
            Contract.Assume(_progressAtLastUpdate >= 0)
            Contract.Assume(_progressAtLastEstimate >= 0)
        End Sub

#Region "Properties"
        Public ReadOnly Property Busy As Boolean
            Get
                Return other IsNot Nothing
            End Get
        End Property
        Public ReadOnly Property TimeSinceLastActivity() As TimeSpan
            Get
                Contract.Ensures(Contract.Result(Of TimeSpan)().Ticks >= 0)
                Return _lastEstimateTimer.ElapsedTime
            End Get
        End Property
        Public ReadOnly Property GetTransferingTime() As TimeSpan
            Get
                Contract.Ensures(Contract.Result(Of TimeSpan)().Ticks >= 0)
                If Not Busy Then Return 0.Seconds
                Return _lastStartTimer.ElapsedTime
            End Get
        End Property
        Public ReadOnly Property GetMaxRateEstimate() As FiniteDouble
            Get
                'Contract.Ensures(Contract.Result(Of FiniteDouble)() > 0)
                Dim result = maxRateEstimate * New FiniteDouble(1 + 1 / (1 + numMeasurements))
                If result < curRateEstimate Then result = curRateEstimate
                Contract.Assume(result.Value > 0)
                Return result
            End Get
        End Property
        Public ReadOnly Property GetCurRateEstimate() As FiniteDouble
            Get
                'Contract.Ensures(Contract.Result(Of FiniteDouble)() > 0)
                Return curRateEstimate
            End Get
        End Property
        Public ReadOnly Property HasRateBeenMeasured() As Boolean
            Get
                Return numMeasurements <> 0
            End Get
        End Property

        Public ReadOnly Property GetLastProgress() As FiniteDouble
            Get
                'Contract.Ensures(Contract.Result(Of FiniteDouble)() >= 0)
                Return _progressAtLastUpdate
            End Get
        End Property
#End Region

#Region "State"
        Public Sub SetTransfering(ByVal progress As FiniteDouble, ByVal destinationClient As TransferClient)
            Contract.Requires(destinationClient IsNot Nothing)
            Contract.Requires(progress >= 0)
            If Busy Then Return

            'Update state
            Me.other = destinationClient
            Me.curRateEstimate = FiniteDouble.Min(Me.maxRateEstimate, other.maxRateEstimate)

            'Prep measure
            _progressAtLastStart = progress
            _progressAtLastUpdate = progress
            _progressAtLastEstimate = progress
            _lastStartTimer.Reset()
            _lastEstimateTimer.Reset()
        End Sub

        <ContractVerification(False)>
        Public Sub UpdateProgress(ByVal progress As FiniteDouble)
            Contract.Requires(progress >= 0)
            If Not Busy Then Return

            _progressAtLastUpdate = progress

            'Update estimate
            If _lastEstimateTimer.ElapsedTime > 1.Milliseconds And progress > _progressAtLastEstimate Then
                Dim dt = New FiniteDouble(_lastEstimateTimer.Reset().TotalMilliseconds)
                Dim dp = progress - _progressAtLastEstimate
                Dim r = dp / dt
                Dim dc = dt / 1000
                Dim c = LiveEstimateConvergence ^ dc
                curRateEstimate *= c
                curRateEstimate += r * (1 - c)
                _progressAtLastEstimate = progress
            End If
        End Sub

        Public Sub SetNotTransfering(ByVal nowFinished As Boolean)
            If Not Busy Then
                Me.completed = Me.completed Or nowFinished
                Return
            End If

            'Measure max rate
            Dim dp = _progressAtLastUpdate - _progressAtLastStart
            Dim dt = GetTransferingTime()
            If dt > 1.Milliseconds And dp > 0 Then
                Dim r = dp / New FiniteDouble(dt.TotalMilliseconds)
                Dim c = New FiniteDouble(If(numMeasurements = 0 OrElse r > maxRateEstimate, 0, 0.9))
                maxRateEstimate *= c
                maxRateEstimate += r * (1 - c)
                numMeasurements += 1
            End If

            'Update state
            Me.completed = Me.completed Or nowFinished
            Me.other = Nothing
            Me.curRateEstimate = Me.maxRateEstimate
        End Sub
#End Region

        Public Overrides Function ToString() As String
            If Busy Then
                Return "Client {0}: {1} {2}".Frmt(key, If(completed, "Uploading to", "Downloading from"), other.key)
            Else
                Return "Client {0}: {1}".Frmt(key, If(completed, "Completed", "Not Completed"))
            End If
        End Function
    End Class
End Class
