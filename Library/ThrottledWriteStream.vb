'''<summary>Wraps a substream so that it has asynchronously throttled writes.</summary>
Public NotInheritable Class ThrottledWriteStream
    Inherits IO.Stream

    Private ReadOnly _substream As IO.Stream
    Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue
    Private ReadOnly _queuedWrites As New Queue(Of Byte())
    Private ReadOnly _costEstimator As Func(Of Byte(), Integer)
    Private ReadOnly _costLimit As Double
    Private ReadOnly _recoveryRatePerMillisecond As Double

    Private _timer As RelativeClock
    Private _availableSlack As Double
    Private _usedCost As Double
    Private _writing As Boolean

    <ContractInvariantMethod()> Private Shadows Sub ObjectInvariant()
        Contract.Invariant(_substream IsNot Nothing)
        Contract.Invariant(_queuedWrites IsNot Nothing)
        Contract.Invariant(_costEstimator IsNot Nothing)
        Contract.Invariant(inQueue IsNot Nothing)
        Contract.Invariant(_availableSlack >= 0)
        Contract.Invariant(_usedCost >= 0)
        Contract.Invariant(_costLimit >= 0)
        Contract.Invariant(_recoveryRatePerMillisecond > 0)
        Contract.Invariant(_timer IsNot Nothing)
    End Sub

    Public Sub New(subStream As IO.Stream,
                   costEstimator As Func(Of Byte(), Integer),
                   clock As IClock,
                   Optional initialSlack As Double = 0,
                   Optional costLimit As Double = 0,
                   Optional costRecoveredPerMillisecond As Double = 1)
        Contract.Requires(clock IsNot Nothing)
        Contract.Requires(subStream IsNot Nothing)
        Contract.Requires(initialSlack >= 0)
        Contract.Requires(costEstimator IsNot Nothing)
        Contract.Requires(costLimit >= 0)
        Contract.Requires(costRecoveredPerMillisecond > 0)

        Me._substream = subStream
        Me._timer = clock.Restarted
        Me._availableSlack = initialSlack
        Me._costEstimator = costEstimator
        Me._costLimit = costLimit
        Me._recoveryRatePerMillisecond = costRecoveredPerMillisecond
    End Sub

    Public Overrides Sub Write(buffer() As Byte, offset As Integer, count As Integer)
        Dim data = buffer.Skip(offset).Take(count).ToArray
        inQueue.QueueAction(Sub()
                                _queuedWrites.Enqueue(data)
                                PerformWrites()
                            End Sub)
    End Sub
    Private Async Sub PerformWrites()
        If _writing Then Return
        _writing = True

        Try
            While _queuedWrites.Count > 0
                'Recover over time
                _timer = _timer.Restarted
                Dim dt = _timer.StartingTimeOnParentClock
                _usedCost -= dt.TotalMilliseconds * _recoveryRatePerMillisecond
                If _usedCost < 0 Then _usedCost = 0
                'Recover using slack
                If _availableSlack > 0 Then
                    Dim slack = Math.Min(_usedCost, _availableSlack)
                    _availableSlack -= slack
                    _usedCost -= slack
                End If

                'throttle
                Await _timer.AsyncWait(((_usedCost - _costLimit) / _recoveryRatePerMillisecond).Milliseconds)

                'Perform write
                Dim data = _queuedWrites.Dequeue()
                Contract.Assume(data IsNot Nothing)
                _usedCost += _costEstimator(data)
                _substream.Write(data, 0, data.Length)
            End While
        Finally
            _writing = False
        End Try
        Contract.Assume(_availableSlack >= 0)
        Contract.Assume(_usedCost >= 0)
    End Sub

    Public Overrides Function BeginRead(buffer() As Byte, offset As Integer, count As Integer, callback As System.AsyncCallback, state As Object) As IAsyncResult
        Return _substream.BeginRead(buffer, offset, count, callback, state)
    End Function
    Public Overrides Function EndRead(asyncResult As IAsyncResult) As Integer
        Return _substream.EndRead(asyncResult)
    End Function

#Region "Not Supported"
    Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
        Return _substream.Read(buffer, offset, count)
    End Function
    Public Overrides Sub Flush()
        _substream.Flush()
    End Sub
    Public Overrides ReadOnly Property Length As Long
        Get
            Return _substream.Length
        End Get
    End Property
    Public Overrides ReadOnly Property CanWrite As Boolean
        Get
            Return _substream.CanWrite
        End Get
    End Property
    Public Overrides ReadOnly Property CanRead As Boolean
        Get
            Return _substream.CanRead
        End Get
    End Property
    Public Overrides ReadOnly Property CanSeek() As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides Property Position() As Long
        Get
            Throw New NotSupportedException
        End Get
        Set(value As Long)
            Throw New NotSupportedException
        End Set
    End Property
    Public Overrides Function Seek(offset As Long, origin As System.IO.SeekOrigin) As Long
        Throw New NotSupportedException
    End Function
    Public Overrides Sub SetLength(value As Long)
        Throw New NotSupportedException
    End Sub
    Public Overrides Function BeginWrite(buffer() As Byte, offset As Integer, count As Integer, callback As System.AsyncCallback, state As Object) As System.IAsyncResult
        Throw New NotSupportedException
    End Function
#End Region
End Class
