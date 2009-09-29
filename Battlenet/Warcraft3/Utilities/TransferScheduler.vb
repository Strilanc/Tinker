Public Enum ClientTransferState
    Idle
    Downloading
    Uploading
End Enum

'''<summary>Schedules transfers for sharing a copyable resource among multiple clients.</summary>
Public Class TransferScheduler(Of TClientKey)
    Private ReadOnly typicalRate As FiniteDouble
    Private ReadOnly clients As New Dictionary(Of TClientKey, Client)
    Private ReadOnly ref As ICallQueue
    Private ReadOnly fileSize As FiniteDouble
    Private ReadOnly typicalSwitchTime As FiniteDouble

    Public Event Actions(ByVal started As List(Of TransferEndpoints), ByVal stopped As List(Of TransferEndpoints))

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(clients IsNot Nothing)
        Contract.Invariant(ref IsNot Nothing)
        'Contract.Invariant(fileSize > 0)
        'Contract.Invariant(typicalRate > 0)
        'Contract.Invariant(typicalSwitchTime >= 0)
    End Sub

    Public Class TransferEndpoints
        Public ReadOnly source As TClientKey
        Public ReadOnly destination As TClientKey
        Public Sub New(ByVal source As TClientKey,
                       ByVal destination As TClientKey)
            Me.source = source
            Me.destination = destination
        End Sub
    End Class

    Public Sub New(ByVal typicalRate As FiniteDouble,
                   ByVal typicalSwitchTime As FiniteDouble,
                   ByVal fileSize As FiniteDouble,
                   Optional ByVal pq As ICallQueue = Nothing)
        Contract.Assume(fileSize > 0) 'bug in contracts required not using requires here
        Contract.Assume(typicalRate > 0)
        Contract.Assume(typicalSwitchTime >= 0)
        Me.ref = If(pq, New ThreadPooledCallQueue)
        Me.typicalRate = typicalRate
        Me.typicalSwitchTime = typicalSwitchTime
        Me.fileSize = fileSize
    End Sub

    '''<summary>Adds a new client to the pool.</summary>
    Public Function AddClient(ByVal clientKey As TClientKey,
                              ByVal completed As Boolean,
                              Optional ByVal expectedRate As FiniteDouble = Nothing) As IFuture
        'Contract.Requires(expectedRate = Nothing OrElse expectedRate > 0)
        If expectedRate = Nothing Then expectedRate = typicalRate
        Return ref.QueueAction(
            Sub()
                If clients.ContainsKey(clientKey) Then  Throw New InvalidOperationException("client key already exists")
                Contract.Assume(expectedRate > 0)
                clients(clientKey) = New Client(clientKey, completed, expectedRate)
            End Sub
        )
    End Function

    '''<summary>Adds a link between two clients.</summary>
    Public Function SetLink(ByVal clientKey1 As TClientKey,
                            ByVal clientKey2 As TClientKey,
                            ByVal linked As Boolean) As IFuture
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
                    If client1.busy AndAlso client1.other Is client2 Then
                        StopTransfer(clientKey1, False)
                    End If
                End If
            End Sub
        )
    End Function

    '''<summary>Removes a client from the pool.</summary>
    Public Function RemoveClient(ByVal clientKey As TClientKey) As IFuture
        Return ref.QueueAction(
            Sub()
                If Not clients.ContainsKey(clientKey) Then  Throw New InvalidOperationException("No such client.")
                Dim client = clients(clientKey)
                If client.busy Then
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
            Contract.Ensures(Contract.Result(Of IFuture(Of String))() IsNot Nothing)
            Return ref.QueueFunc(
                Function()
                    If Not clients.ContainsKey(clientKey) Then  Return "?"
                    With clients(clientKey)
                        If Not .HasRateBeenMeasured AndAlso Not .busy Then  Return "?"

                        Dim d = .GetCurRateEstimate * 1000
                        Dim f = New FiniteDouble(1.0)
                        For Each s In {"B", "KiB", "MiB", "GiB", "TiB", "PiB"}
                            Dim f2 = f * 1024
                            If d < f2 Then
                                Contract.Assume(f <> 0)
                                Return "{0:0.0} {1}/s".Frmt(d / f, s)
                            End If
                            f = f2
                        Next s

                        Return ">HiB/s" '... It could happen.
                    End With
                End Function)
        End Get
    End Property

    Public Function GetClientState(ByVal clientKey As TClientKey) As IFuture(Of ClientTransferState)
        Return ref.QueueFunc(Function()
                                 If Not clients.ContainsKey(clientKey) Then  Return ClientTransferState.Idle
                                 Dim client = clients(clientKey)
                                 If Not client.busy Then
                                     Return ClientTransferState.Idle
                                 ElseIf client.completed Then
                                     Return ClientTransferState.Uploading
                                 Else
                                     Return ClientTransferState.Downloading
                                 End If
                             End Function)
    End Function

    '''<summary>Updates the progress of a transfer to or from the given client.</summary>
    Public Function UpdateProgress(ByVal clientKey As TClientKey,
                                   ByVal progress As FiniteDouble) As IFuture
        Contract.Requires(progress >= 0)
        Return ref.QueueAction(
            Sub()
                If Not clients.ContainsKey(clientKey) Then  Throw New InvalidOperationException("No such client key.")
                Dim client = clients(clientKey)
                If Not client.busy Then  Throw New InvalidOperationException("Client isn't transfering.")
                Contract.Assume(progress >= 0)
                client.UpdateProgress(progress)
                client.other.UpdateProgress(progress)
            End Sub
        )
    End Function

    '''<summary>Stops any transfers to or from the given client.</summary>
    Public Function StopTransfer(ByVal clientKey As TClientKey,
                                 ByVal complete As Boolean) As IFuture
        Return ref.QueueAction(
            Sub()
                If Not clients.ContainsKey(clientKey) Then  Throw New InvalidOperationException("No such client key.")

                Dim client = clients(clientKey)
                If client.busy Then
                    Dim other = client.other
                    client.SetNotTransfering(nowFinished:=complete)
                    other.SetNotTransfering(nowFinished:=complete)
                End If
            End Sub
        )
    End Function

    Public Sub Update()
        ref.QueueAction(
            Sub()
                Dim transfers = New List(Of TransferEndpoints)
                Dim breaks = New List(Of TransferEndpoints)

                'Match downloaders to uploaders
                For Each downloader In (From client In clients.Values Where Not client.completed)
                    Dim availableUploaders = From e In downloader.links Where e.completed AndAlso Not e.busy
                    If availableUploaders.None Then  Continue For

                    Dim curUploader = downloader.other
                    Dim bestUploader = availableUploaders.Max(Function(e1, e2)
                                                                  Dim n = Math.Sign(e1.GetMaxRateEstimate - e2.GetMaxRateEstimate)
                                                                  If n <> 0 Then  Return n
                                                                  Return Math.Sign(e2.links.Count - e1.links.Count)
                                                              End Function)

                    If curUploader Is Nothing Then
                        'Start transfer
                        bestUploader.SetTransfering(downloader.GetLastProgress, downloader)
                        downloader.SetTransfering(downloader.GetLastProgress, bestUploader)
                        transfers.Add(New TransferEndpoints(bestUploader.key, downloader.key))
                    ElseIf Math.Min(downloader.GetTransferingTime, curUploader.GetTransferingTime) > Client.MIN_SWITCH_PERIOD Then
                        'Stop transfer if a significantly better uploader is available
                        Dim remaining = (fileSize - downloader.GetLastProgress)
                        Dim curExpectedTime = remaining / downloader.GetCurRateEstimate
                        Dim newExpectedTime = typicalSwitchTime + 1.25 * remaining / FiniteDouble.Min(downloader.GetMaxRateEstimate, bestUploader.GetMaxRateEstimate())
                        If curUploader.IsProbablyFrozen OrElse newExpectedTime < curExpectedTime Then
                            breaks.Add(New TransferEndpoints(curUploader.key, downloader.key))
                        End If
                    End If
                Next downloader

                'Stop transfers seen as improvable
                '[Done here instead of in the above loop to avoid potentially giving two commands to clients at the same time]
                For Each e In breaks
                    StopTransfer(e.source, False)
                Next e

                'Report actions to the outside
                If transfers.Any OrElse breaks.Any Then
                    RaiseEvent Actions(transfers, breaks)
                End If
            End Sub
        )
    End Sub

    Private Class Client
        Private maxRateEstimate As FiniteDouble
        Private curRateEstimate As FiniteDouble

        Private lastUpdateTime As ModInt32
        Private lastUpdateProgress As FiniteDouble
        Private lastStartProgress As FiniteDouble
        Private lastStartTime As ModInt32
        Private numMeasurements As Integer

        Public ReadOnly key As TClientKey
        Public other As Client = Nothing
        Private ReadOnly _links As New HashSet(Of Client)
        Public busy As Boolean
        Public completed As Boolean

        Public Const PROBABLY_FROZEN_PERIOD As Integer = 6000
        Public Const MIN_SWITCH_PERIOD As Integer = 3000
        Private Const CUR_ESTIMATE_CONVERGENCE_FACTOR As Double = 0.6

        Public ReadOnly Property links As HashSet(Of Client)
            Get
                Contract.Ensures(Contract.Result(Of HashSet(Of Client))() IsNot Nothing)
                Return _links
            End Get
        End Property
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            'Contract.Invariant(maxRateEstimate > 0)
            'Contract.Invariant(curRateEstimate > 0)
            'Contract.Invariant(lastUpdateProgress >= 0)
            'Contract.Invariant(lastStartProgress >= 0)
            Contract.Invariant(_links IsNot Nothing)
        End Sub

        Public Sub New(ByVal key As TClientKey, ByVal completed As Boolean, ByVal typicalRatePerMillisecond As FiniteDouble)
            Contract.Requires(typicalRatePerMillisecond > 0)
            Me.key = key
            Me.completed = completed
            Me.maxRateEstimate = typicalRatePerMillisecond
            Me.curRateEstimate = typicalRatePerMillisecond
            Contract.Assume(lastUpdateProgress >= 0)
            Contract.Assume(lastStartProgress >= 0)
        End Sub

#Region "Properties"
        Public ReadOnly Property GetTransferingTime() As FiniteDouble
            Get
                'Contract.Ensures(Contract.Result(Of FiniteDouble)() >= 0)
                If Not busy Then Return 0
                Return New FiniteDouble(CUInt(Environment.TickCount - lastStartTime))
            End Get
        End Property
        Public ReadOnly Property GetMaxRateEstimate() As FiniteDouble
            Get
                'Contract.Ensures(Contract.Result(Of FiniteDouble)() > 0)
                If numMeasurements = 0 Then Return curRateEstimate
                Dim result = maxRateEstimate * New FiniteDouble(1 + 1 / numMeasurements)
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
                Return lastUpdateProgress
            End Get
        End Property
        Public ReadOnly Property IsProbablyFrozen() As Boolean
            Get
                Return CUInt(Environment.TickCount - lastUpdateTime) > PROBABLY_FROZEN_PERIOD
            End Get
        End Property
#End Region

#Region "State"
        Public Sub SetTransfering(ByVal progress As FiniteDouble, ByVal destinationClient As Client)
            Contract.Requires(progress >= 0)
            If busy Then Return

            'Update state
            busy = True
            Me.other = destinationClient

            'Prep measure
            lastStartProgress = progress
            lastStartTime = Environment.TickCount()
            lastUpdateProgress = lastStartProgress
            lastUpdateTime = lastStartTime
        End Sub

        Public Sub UpdateProgress(ByVal progress As FiniteDouble)
            Contract.Requires(progress >= 0)
            If Not busy Then Return

            'Measure cur rate
            Dim time As ModInt32 = Environment.TickCount()
            If time = lastUpdateTime Then Return
            If progress <= lastUpdateProgress Then Return

            Dim dp = progress - lastUpdateProgress
            Dim dt = New FiniteDouble(CUInt(time - lastUpdateTime))
            Contract.Assume(dt.Value <> 0)
            Dim r = dp / dt
            Dim dc = dt / 1000
            Contract.Assume(dc.Value <> 0)
            Contract.Assume(CUR_ESTIMATE_CONVERGENCE_FACTOR > 0)
            Dim c = CUR_ESTIMATE_CONVERGENCE_FACTOR ^ (dt / 1000)
            curRateEstimate *= c
            curRateEstimate += r * (1 - c)

            'Update state
            lastUpdateTime = time
            lastUpdateProgress = progress
        End Sub

        Public Sub SetNotTransfering(ByVal nowFinished As Boolean)
            If Not busy Then
                Me.completed = Me.completed Or nowFinished
                Return
            End If

            'Measure max rate
            Dim dp = lastUpdateProgress - lastStartProgress
            Dim dt = GetTransferingTime()
            If dt > 0 And dp > 0 Then
                Dim r = dp / dt
                Dim c = New FiniteDouble(If(numMeasurements = 0 OrElse r > maxRateEstimate, 0, 0.99))
                maxRateEstimate *= c
                maxRateEstimate += r * (1 - c)
                numMeasurements += 1
            End If

            'Update state
            Me.busy = False
            Me.completed = Me.completed Or nowFinished
            Me.other = Nothing
            Me.curRateEstimate = Me.maxRateEstimate
        End Sub
#End Region
    End Class
End Class
