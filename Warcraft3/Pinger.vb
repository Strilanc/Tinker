Public NotInheritable Class Pinger
    Implements IDisposable

    Private _latency As Double
    Private ReadOnly _timeoutCount As Integer
    Private ReadOnly _pingQueue As New Queue(Of Tuple(Of UInt32, IClock))
    Private ReadOnly _rng As New Random()
    Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue()
    Private ReadOnly _clock As IClock
    Private ReadOnly _ticker As IDisposable

    Public Event SendPing(ByVal sender As Pinger, ByVal salt As UInteger)
    Public Event Timeout(ByVal sender As Pinger)

    <ContractInvariantMethod()>
    Private Sub ObjectInvariant()
        Contract.Invariant(_rng IsNot Nothing)
        Contract.Invariant(_latency >= 0)
        Contract.Invariant(_ticker IsNot Nothing)
        Contract.Invariant(_clock IsNot Nothing)
        Contract.Invariant(_pingQueue IsNot Nothing)
        Contract.Invariant(inQueue IsNot Nothing)
        Contract.Invariant(_timeoutCount > 0)
    End Sub

    Public Sub New(ByVal period As TimeSpan, ByVal timeoutCount As Integer, ByVal clock As IClock)
        Contract.Assume(period.Ticks > 0)
        Contract.Assume(timeoutCount > 0)
        Contract.Assume(clock IsNot Nothing)
        Me._timeoutCount = timeoutCount
        Me._clock = clock
        Me._ticker = clock.AsyncRepeat(period, Sub() inQueue.QueueAction(AddressOf OnTick))
    End Sub

    Public Function QueueGetLatency() As Task(Of Double)
        Contract.Ensures(Contract.Result(Of Task(Of Double))() IsNot Nothing)
        Return inQueue.QueueFunc(Function() _latency)
    End Function

    Private Sub OnTick()
        If _pingQueue.Count >= _timeoutCount Then
            RaiseEvent Timeout(Me)
        Else
            Dim record = Tuple.Create(CUInt(_rng.Next()), DirectCast(_clock.Restarted(), IClock))
            _pingQueue.Enqueue(record)
            RaiseEvent SendPing(Me, record.Item1)
        End If
    End Sub

    Private Sub ReceivedPong(ByVal salt As UInteger)
        If _pingQueue.Count <= 0 Then
            Throw New InvalidOperationException("Pong received before ping sent.")
        End If

        Dim stored = _pingQueue.Dequeue()
        Contract.Assume(stored IsNot Nothing)
        Contract.Assume(stored.Item2 IsNot Nothing)
        If salt <> stored.Item1 Then
            Throw New InvalidOperationException("Pong didn't match ping.")
        End If

        'Measure
        Dim lambda = 0.5
        _latency *= 1 - lambda
        _latency += lambda * stored.Item2.ElapsedTime.TotalMilliseconds
        If _latency <= 0 Then _latency = Double.Epsilon
    End Sub
    Public Function QueueReceivedPong(ByVal salt As UInteger) As Task
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Return inQueue.QueueAction(Sub() ReceivedPong(salt))
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        _ticker.Dispose()
    End Sub
End Class
