'''<summary>Schedules transfers for sharing a copyable resource among multiple clients.</summary>
Public Class TransferScheduler(Of TClientKey)
    Private ReadOnly typicalRate As Double
    Private ReadOnly clients As New Dictionary(Of TClientKey, Client)
    Private ReadOnly ref As ICallQueue
    Private ReadOnly fileSize As Double
    Private ReadOnly typicalSwitchTime As Double

    Public Event actions(ByVal started As List(Of TransferEndPoints), ByVal stopped As List(Of TransferEndPoints))

    <ContractInvariantMethod()> Protected Sub Invariant()
        Contract.Invariant(clients IsNot Nothing)
        Contract.Invariant(ref IsNot Nothing)
        Contract.Invariant(fileSize > 0)
        Contract.Invariant(Not Double.IsInfinity(fileSize))
        Contract.Invariant(Not Double.IsNaN(fileSize))
        Contract.Invariant(typicalRate > 0)
        Contract.Invariant(Not Double.IsInfinity(typicalRate))
        Contract.Invariant(Not Double.IsNaN(typicalRate))
        Contract.Invariant(typicalSwitchTime >= 0)
        Contract.Invariant(Not Double.IsInfinity(typicalSwitchTime))
        Contract.Invariant(Not Double.IsNaN(typicalSwitchTime))
    End Sub

    Public Class TransferEndPoints
        Public ReadOnly src As TClientKey
        Public ReadOnly dst As TClientKey
        Public Sub New(ByVal src As TClientKey, ByVal dst As TClientKey)
            Me.src = src
            Me.dst = dst
        End Sub
    End Class

    Public Sub New(ByVal typicalRate As Double,
                   ByVal typicalSwitchTime As Double,
                   ByVal fileSize As Double,
                   Optional ByVal pq As ICallQueue = Nothing)
        Contract.Requires(fileSize > 0)
        Contract.Requires(Not Double.IsInfinity(fileSize))
        Contract.Requires(Not Double.IsNaN(fileSize))
        Contract.Requires(typicalRate > 0)
        Contract.Requires(Not Double.IsInfinity(typicalRate))
        Contract.Requires(Not Double.IsNaN(typicalRate))
        Contract.Requires(typicalSwitchTime >= 0)
        Contract.Requires(Not Double.IsInfinity(typicalSwitchTime))
        Contract.Requires(Not Double.IsNaN(typicalSwitchTime))
        Me.ref = If(pq, New ThreadPooledCallQueue)
        Me.typicalRate = typicalRate
        Me.typicalSwitchTime = typicalSwitchTime
        Me.fileSize = fileSize
    End Sub

    '''<summary>Adds a new client to the pool.</summary>
    Public Function AddClient(ByVal clientKey As TClientKey,
                              ByVal completed As Boolean,
                              Optional ByVal expectedRate As Double = 0) As IFuture(Of Outcome)
        Contract.Requires(expectedRate >= 0)
        Contract.Requires(Not Double.IsInfinity(expectedRate))
        Contract.Requires(Not Double.IsNaN(expectedRate))
        Return ref.QueueFunc(
            Function()
                If clients.ContainsKey(clientKey) Then  Return failure("client key already exists")
                clients(clientKey) = New Client(clientKey, completed, If(expectedRate = 0, typicalRate, expectedRate))
                Return success("added")
            End Function
        )
    End Function

    '''<summary>Adds a link between two clients.</summary>
    Public Function SetLink(ByVal clientKey1 As TClientKey,
                            ByVal clientKey2 As TClientKey,
                            ByVal linked As Boolean) As IFuture(Of Outcome)
        Return ref.QueueFunc(
            Function()
                If Not clients.ContainsKey(clientKey1) Then  Return failure("No such client key")
                If Not clients.ContainsKey(clientKey2) Then  Return failure("No such client key")
                Dim client1 = clients(clientKey1)
                Dim client2 = clients(clientKey2)
                If client1.links.Contains(client2) = linked Then  Return success("Already set.")
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
                Return success("Set.")
            End Function
        )
    End Function

    '''<summary>Removes a client from the pool.</summary>
    Public Function RemoveClient(ByVal clientKey As TClientKey) As IFuture(Of Outcome)
        Return ref.QueueFunc(
            Function()
                If Not clients.ContainsKey(clientKey) Then  Return failure("No such client.")
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
                Return success("removed")
            End Function
        )
    End Function

    Public ReadOnly Property GenerateRateDescription(ByVal clientKey As TClientKey) As String
        Get
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            If Not clients.ContainsKey(clientKey) Then Return "?"
            With clients(clientKey)
                If Not .HasRateBeenMeasured AndAlso Not .busy Then Return "?"

                Dim d = .GetCurRateEstimate * 1000
                Dim f = 1
                For Each s In {"B", "KiB", "MiB", "GiB", "TiB", "PiB"}
                    Dim f2 = f * 1024
                    If d < f2 Then
                        Return "{0:0.0} {1}/s".frmt(d / f, s)
                    End If
                    f = f2
                Next s

                Return ">HiB/s" '... It could happen.
            End With
        End Get
    End Property

    '''<summary>Updates the progress of a transfer to or from the given client.</summary>
    Public Function UpdateProgress(ByVal clientKey As TClientKey,
                                   ByVal progress As Double) As IFuture(Of Outcome)
        Contract.Requires(progress >= 0)
        Contract.Requires(Not Double.IsInfinity(progress))
        Contract.Requires(Not Double.IsNaN(progress))
        Return ref.QueueFunc(
            Function()
                If Not clients.ContainsKey(clientKey) Then  Return failure("No such client key.")
                Dim client = clients(clientKey)
                If Not client.busy Then  Return failure("Client isn't transfering.")
                client.UpdateProgress(progress)
                client.other.UpdateProgress(progress)
                Return success("Updated")
            End Function
        )
    End Function

    '''<summary>Stops any transfers to or from the given client.</summary>
    Public Function StopTransfer(ByVal clientKey As TClientKey,
                                 ByVal complete As Boolean) As IFuture(Of Outcome)
        Return ref.QueueFunc(
            Function()
                If Not clients.ContainsKey(clientKey) Then  Return failure("No such client key.")

                Dim client = clients(clientKey)
                If client.busy Then
                    Dim other = client.other
                    client.SetNotTransfering(complete)
                    other.SetNotTransfering(complete)
                End If
                Return success("Stopped")
            End Function
        )
    End Function

    Public Sub Update()
        ref.QueueAction(
            Sub()
                Dim transfers = New List(Of TransferEndPoints)
                Dim breaks = New List(Of TransferEndPoints)

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
                        transfers.Add(New TransferEndPoints(bestUploader.key, downloader.key))
                    ElseIf Math.Min(downloader.GetTransferingTime, curUploader.GetTransferingTime) > Client.MIN_SWITCH_PERIOD Then
                        'Stop transfer if a significantly better uploader is available
                        Dim remaining = (fileSize - downloader.GetLastProgress)
                        Dim curExpectedTime = remaining / downloader.GetCurRateEstimate()
                        Dim newExpectedTime = typicalSwitchTime + 1.25 * remaining / Math.Min(downloader.GetMaxRateEstimate, bestUploader.GetMaxRateEstimate())
                        If curUploader.IsProbablyFrozen OrElse newExpectedTime < curExpectedTime Then
                            breaks.Add(New TransferEndPoints(curUploader.key, downloader.key))
                        End If
                    End If
                Next downloader

                'Stop transfers seen as improvable
                '[Done here instead of in the above loop to avoid potentially giving two commands to clients at the same time]
                For Each e In breaks
                    StopTransfer(e.src, False)
                Next e

                'Report actions to the outside
                If transfers.Any Or breaks.Any Then
                    RaiseEvent actions(transfers, breaks)
                End If
            End Sub
        )
    End Sub

    Private Class Client
        Private maxRateEstimate As Double
        Private curRateEstimate As Double

        Private lastUpdateTime As ModInt32
        Private lastUpdateProgress As Double
        Private lastStartProgress As Double
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
        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(maxRateEstimate > 0)
            Contract.Invariant(Not Double.IsInfinity(maxRateEstimate))
            Contract.Invariant(Not Double.IsNaN(maxRateEstimate))
            Contract.Invariant(curRateEstimate > 0)
            Contract.Invariant(Not Double.IsInfinity(curRateEstimate))
            Contract.Invariant(Not Double.IsNaN(curRateEstimate))
            Contract.Invariant(lastUpdateProgress >= 0)
            Contract.Invariant(Not Double.IsInfinity(lastUpdateProgress))
            Contract.Invariant(Not Double.IsNaN(lastUpdateProgress))
            Contract.Invariant(lastStartProgress >= 0)
            Contract.Invariant(Not Double.IsInfinity(lastStartProgress))
            Contract.Invariant(Not Double.IsNaN(lastStartProgress))
            Contract.Invariant(links IsNot Nothing)
        End Sub

        Public Sub New(ByVal key As TClientKey, ByVal completed As Boolean, ByVal typicalRatePerMillisecond As Double)
            Contract.Requires(typicalRatePerMillisecond > 0)
            Contract.Requires(Not Double.IsInfinity(typicalRatePerMillisecond))
            Contract.Requires(Not Double.IsNaN(typicalRatePerMillisecond))
            Me.key = key
            Me.completed = completed
            Me.maxRateEstimate = typicalRatePerMillisecond
            Me.curRateEstimate = typicalRatePerMillisecond
        End Sub

#Region "Properties"
        Public ReadOnly Property GetTransferingTime() As Double
            Get
                Contract.Ensures(Contract.Result(Of Double)() >= 0)
                Contract.Ensures(Not Double.IsNaN(Contract.Result(Of Double)()))
                Contract.Ensures(Not Double.IsInfinity(Contract.Result(Of Double)()))
                If Not busy Then Return 0
                Return CUInt(Environment.TickCount - lastStartTime)
            End Get
        End Property
        Public ReadOnly Property GetMaxRateEstimate() As Double
            Get
                Contract.Ensures(Contract.Result(Of Double)() > 0)
                Contract.Ensures(Not Double.IsNaN(Contract.Result(Of Double)()))
                Contract.Ensures(Not Double.IsInfinity(Contract.Result(Of Double)()))
                If numMeasurements = 0 Then Return curRateEstimate
                Return maxRateEstimate * (1 + 1 / numMeasurements)
            End Get
        End Property
        Public ReadOnly Property GetCurRateEstimate() As Double
            Get
                Contract.Ensures(Contract.Result(Of Double)() > 0)
                Contract.Ensures(Not Double.IsNaN(Contract.Result(Of Double)()))
                Contract.Ensures(Not Double.IsInfinity(Contract.Result(Of Double)()))
                Return curRateEstimate
            End Get
        End Property
        Public ReadOnly Property HasRateBeenMeasured() As Boolean
            Get
                Return numMeasurements <> 0
            End Get
        End Property

        Public ReadOnly Property GetLastProgress() As Double
            Get
                Contract.Ensures(Contract.Result(Of Double)() >= 0)
                Contract.Ensures(Not Double.IsNaN(Contract.Result(Of Double)()))
                Contract.Ensures(Not Double.IsInfinity(Contract.Result(Of Double)()))
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
        Public Sub SetTransfering(ByVal progress As Double, ByVal other As Client)
            Contract.Requires(progress >= 0)
            Contract.Requires(Not Double.IsInfinity(progress))
            Contract.Requires(Not Double.IsNaN(progress))
            If busy Then Return

            'Update state
            busy = True
            Me.other = other

            'Prep measure
            lastStartProgress = progress
            lastStartTime = Environment.TickCount()
            lastUpdateProgress = lastStartProgress
            lastUpdateTime = lastStartTime
        End Sub

        Public Sub UpdateProgress(ByVal progress As Double)
            Contract.Requires(progress >= 0)
            Contract.Requires(Not Double.IsInfinity(progress))
            Contract.Requires(Not Double.IsNaN(progress))
            If Not busy Then Return

            'Measure cur rate
            Dim time As ModInt32 = Environment.TickCount()
            If time = lastUpdateTime Then Return
            If progress <= lastUpdateProgress Then Return

            Dim dp = progress - lastUpdateProgress
            Dim dt = CUInt(time - lastUpdateTime)
            Dim r = dp / dt
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
                Dim c = If(numMeasurements = 0 OrElse r > maxRateEstimate, 0, 0.99)
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
