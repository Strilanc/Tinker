''' <summary>
''' A class which raises an event if it is armed and not continuously reset.
''' </summary>
<DebuggerDisplay("{ToString}")>
Public NotInheritable Class DeadManSwitch
    Private ReadOnly _period As TimeSpan
    Private _isArmed As Boolean
    Private _wasReset As Boolean
    Private _timer As ITimer
    Private ReadOnly _clock As IClock
    Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue()

    Public Event Triggered(ByVal sender As DeadManSwitch)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_period.Ticks > 0)
        Contract.Invariant(_clock IsNot Nothing)
        Contract.Invariant(inQueue IsNot Nothing)
        Contract.Invariant(Not _isArmed OrElse _timer IsNot Nothing)
    End Sub

    Public Sub New(ByVal period As TimeSpan, ByVal clock As IClock)
        Contract.Assume(period.Ticks > 0)
        Contract.Assume(clock IsNot Nothing)
        Me._period = period
        Me._clock = clock
    End Sub

    ''' <summary>
    ''' Starts the countdown timer.
    ''' No effect if the timer is already started.
    ''' </summary>
    Public Function Arm() As ifuture
        Return inQueue.QueueAction(
            Sub()
                If _isArmed Then Return
                _isArmed = True
                _timer = _clock.StartTimer
                _wasReset = True
                OnTimeout()
            End Sub)
    End Function
    ''' <summary>
    ''' Resets the countdown timer.
    ''' No effect if the timer is stopped.
    ''' </summary>
    Public Function Reset() As IFuture
        Return inQueue.QueueAction(
            Sub()
                _timer.Reset()
                _wasReset = True
            End Sub)
    End Function
    ''' <summary>
    ''' Cancels the countdown timer.
    ''' No effect if the timer is already stopped.
    ''' </summary>
    Public Function Disarm() As ifuture
        Return inQueue.QueueAction(
            Sub()
                _isArmed = False
            End Sub)
    End Function

    Private Sub OnTimeout()
        If _timer Is Nothing Then Throw New InvalidStateException("OnTimeout called without running timer.")
        If Not _isArmed Then Return

        If _wasReset Then
            _wasReset = False
            Dim dt = _timer.Reset()
            _clock.AsyncWait(_period - dt).QueueCallWhenReady(inQueue, Sub() OnTimeout())
        Else
            _timer = Nothing
            _isArmed = False
            RaiseEvent Triggered(Me)
        End If
    End Sub

    Public Overrides Function ToString() As String
        If _isArmed Then
            Return "Armed: {0} of {1}".Frmt(_period - _timer.ElapsedTime, _period)
        Else
            Return "Disarmed: {0}".Frmt(_period)
        End If
    End Function
End Class
