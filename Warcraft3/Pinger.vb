Public Class Pinger
    Implements IDisposable

    Private _latency As FiniteDouble
    Private ReadOnly timer As Timers.Timer
    Private ReadOnly timeoutCount As Integer
    Private ReadOnly pingQueue As New Queue(Of Tuple(Of UInt32, ModInt32))
    Private ReadOnly rng As New Random()
    Private ReadOnly ref As New TaskedCallQueue()

    Public Event SendPing(ByVal sender As Pinger, ByVal salt As UInteger)
    Public Event Timeout(ByVal sender As Pinger)

    <ContractInvariantMethod()>
    Private Sub ObjectInvariant()
        Contract.Invariant(timer IsNot Nothing)
        Contract.Invariant(rng IsNot Nothing)
        Contract.Invariant(_latency >= 0)
        Contract.Invariant(pingQueue IsNot Nothing)
        Contract.Invariant(ref IsNot Nothing)
        Contract.Invariant(timeoutCount > 0)
    End Sub

    Public Sub New(ByVal period As TimeSpan, ByVal timeoutCount As Integer)
        Contract.Requires(period.Ticks > 0)
        Contract.Requires(timeoutCount > 0)
        Me.timeoutCount = timeoutCount
        Me.timer = New Timers.Timer(period.TotalMilliseconds)
        AddHandler Me.timer.Elapsed, Sub() OnTick()
        Me.timer.Start()
        Contract.Assume(_latency >= 0)
    End Sub

    Public Function QueueGetLatency() As IFuture(Of FiniteDouble)
        Contract.Ensures(Contract.Result(Of IFuture(Of FiniteDouble))() IsNot Nothing)
        Return ref.QueueFunc(Function() _latency)
    End Function

    Private Sub OnTick()
        ref.QueueAction(
            Sub()
                If pingQueue.Count >= timeoutCount Then
                    RaiseEvent Timeout(Me)
                Else
                    Dim record = New Tuple(Of UInt32, ModInt32)(CUInt(rng.Next()), Environment.TickCount)
                    pingQueue.Enqueue(record)
                    RaiseEvent SendPing(Me, record.Item1)
                End If
            End Sub)
    End Sub

    Public Function QueueReceivedPong(ByVal salt As UInteger) As IFuture
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(
            Sub()
                If pingQueue.Count <= 0 Then
                    Throw New InvalidOperationException("Pong received before ping sent.")
                End If

                Dim stored = pingQueue.Dequeue()
                If salt <> stored.Item1 Then
                    Throw New InvalidOperationException("Pong didn't match ping.")
                End If

                'Measure
                Dim lambda = New FiniteDouble(0.5)
                Dim tick As ModInt32 = Environment.TickCount
                _latency *= 1 - lambda
                _latency += lambda * New FiniteDouble(CUInt(tick - stored.Item2))
                If _latency <= 0 Then  _latency = Double.Epsilon
            End Sub)
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        timer.Dispose()
    End Sub
End Class
