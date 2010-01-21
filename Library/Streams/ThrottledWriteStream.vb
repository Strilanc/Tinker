'''<summary>Wraps a substream so that it has asynchronously throttled writes.</summary>
Public NotInheritable Class ThrottledWriteStream
    Inherits WrappedStream

    Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
    Private ReadOnly _clock As IClock
    Private ReadOnly _timer As ITimer
    Private ReadOnly _queuedWrites As New Queue(Of Byte())
    Private ReadOnly _costEstimator As Func(Of Byte(), Integer)
    Private ReadOnly _costLimit As Double
    Private ReadOnly _recoveryRatePerMillisecond As Double

    Private _availableSlack As Double
    Private _usedCost As Double
    Private _throttled As Boolean

    <ContractInvariantMethod()> Private Shadows Sub ObjectInvariant()
        Contract.Invariant(_queuedWrites IsNot Nothing)
        Contract.Invariant(_costEstimator IsNot Nothing)
        Contract.Invariant(inQueue IsNot Nothing)
        Contract.Invariant(_availableSlack >= 0)
        Contract.Invariant(_usedCost >= 0)
        Contract.Invariant(_costLimit >= 0)
        Contract.Invariant(_recoveryRatePerMillisecond > 0)
        Contract.Invariant(_clock IsNot Nothing)
        Contract.Invariant(_timer IsNot Nothing)
    End Sub

    Public Sub New(ByVal subStream As IO.Stream,
                   ByVal costEstimator As Func(Of Byte(), Integer),
                   ByVal clock As IClock,
                   Optional ByVal initialSlack As Double = 0,
                   Optional ByVal costLimit As Double = 0,
                   Optional ByVal costRecoveredPerMillisecond As Double = 1)
        MyBase.New(subStream)
        Contract.Requires(clock IsNot Nothing)
        Contract.Requires(subStream IsNot Nothing)
        Contract.Requires(initialSlack >= 0)
        Contract.Requires(costEstimator IsNot Nothing)
        Contract.Requires(costLimit >= 0)
        Contract.Requires(costRecoveredPerMillisecond > 0)

        Me._clock = clock
        Me._timer = clock.StartTimer
        Me._availableSlack = initialSlack
        Me._costEstimator = costEstimator
        Me._costLimit = costLimit
        Me._recoveryRatePerMillisecond = costRecoveredPerMillisecond
    End Sub

    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        Dim data = buffer.SubArray(offset, count)
        inQueue.QueueAction(Sub()
                                _queuedWrites.Enqueue(data)
                                PerformWrites(isWaitCallback:=False)
                            End Sub)
    End Sub
    Private Sub PerformWrites(ByVal isWaitCallback As Boolean)
        If _throttled AndAlso Not isWaitCallback Then Return
        _throttled = False

        While _queuedWrites.Count > 0
            'Recover over time
            Dim dt = _timer.Reset()
            _usedCost -= dt.TotalMilliseconds * _recoveryRatePerMillisecond
            If _usedCost < 0 Then _usedCost = 0
            'Recover using slack
            If _availableSlack > 0 Then
                Dim slack = Math.Min(_usedCost, _availableSlack)
                _availableSlack -= slack
                _usedCost -= slack
            End If

            'Wait if necessary
            If _usedCost > _costLimit Then
                Dim delay = New TimeSpan(ticks:=CLng((_usedCost - _costLimit) / _recoveryRatePerMillisecond * TimeSpan.TicksPerMillisecond))
                _clock.AsyncWait(delay).QueueCallOnSuccess(inQueue, Sub() PerformWrites(isWaitCallback:=True))
                _throttled = True
                Return
            End If

            'Perform write
            Dim data = _queuedWrites.Dequeue()
            Contract.Assume(data IsNot Nothing)
            _usedCost += _costEstimator(data)
            substream.Write(data, 0, data.Length)
        End While
        Contract.Assume(_usedCost >= 0)
    End Sub

    Public Overrides Function BeginRead(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal callback As System.AsyncCallback, ByVal state As Object) As System.IAsyncResult
        Return substream.BeginRead(buffer, offset, count, callback, state)
    End Function
    Public Overrides Function EndRead(ByVal asyncResult As System.IAsyncResult) As Integer
        Return substream.EndRead(asyncResult)
    End Function

#Region "Not Supported"
    Public Overrides Function BeginWrite(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal callback As System.AsyncCallback, ByVal state As Object) As System.IAsyncResult
        Throw New NotSupportedException
    End Function
    Public Overrides ReadOnly Property CanSeek() As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides Property Position() As Long
        Get
            Throw New NotSupportedException
        End Get
        Set(ByVal value As Long)
            Throw New NotSupportedException
        End Set
    End Property
    Public Overrides Function Seek(ByVal offset As Long, ByVal origin As System.IO.SeekOrigin) As Long
        Throw New NotSupportedException
    End Function
    Public Overrides Sub SetLength(ByVal value As Long)
        Throw New NotSupportedException
    End Sub
#End Region
End Class
