''' <summary>
''' A class which raises an event if it is armed and not continuously reset.
''' </summary>
<DebuggerDisplay("{ToString}")>
Public NotInheritable Class DeadManSwitch
    Private ReadOnly _period As TimeSpan
    Private _isArmed As Boolean
    Private _wasReset As Boolean
    Private _isTimerRunning As Boolean
    Private _timerStartTick As ModInt32
    Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue()

    Public Event Triggered(ByVal sender As DeadManSwitch)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_period.Ticks > 0)
        Contract.Invariant(inQueue IsNot Nothing)
    End Sub

    Public Sub New(ByVal period As TimeSpan)
        Contract.Requires(period.Ticks > 0)
        Me._period = period
    End Sub

    ''' <summary>
    ''' Starts the countdown timer.
    ''' No effect if the timer is already started.
    ''' </summary>
    Public Sub Arm()
        inQueue.QueueAction(
            Sub()
                If _isArmed Then Return
                _isArmed = True
                Reset()
                _isTimerRunning = True
                OnTimeout()
            End Sub)
    End Sub
    ''' <summary>
    ''' Resets the countdown timer.
    ''' No effect if the timer is stopped.
    ''' </summary>
    Public Sub Reset()
        inQueue.QueueAction(
            Sub()
                _timerStartTick = Environment.TickCount
                _wasReset = True
            End Sub)
    End Sub
    ''' <summary>
    ''' Cancels the countdown timer.
    ''' No effect if the timer is already stopped.
    ''' </summary>
    Public Sub Disarm()
        inQueue.QueueAction(
            Sub()
                _isArmed = False
            End Sub)
    End Sub

    Private Sub OnTimeout()
        inQueue.QueueAction(
            Sub()
                If Not _isTimerRunning Then Throw New InvalidStateException("OnTimeout called without running timer.")
                _isTimerRunning = False
                If Not _isArmed Then Return

                If _wasReset Then
                    _wasReset = False
                    _isTimerRunning = True
                    Dim dt = _period - CInt(Environment.TickCount - _timerStartTick).Milliseconds
                    dt.AsyncWait().CallWhenReady(Sub() OnTimeout())
                Else
                    _isArmed = False
                    RaiseEvent Triggered(Me)
                End If
            End Sub)
    End Sub

    Public Overrides Function ToString() As String
        If _isArmed Then
            Return "Armed: {0} of {1}".Frmt(_period - CInt(Environment.TickCount - _timerStartTick).Milliseconds, _period)
        Else
            Return "Disarmed: {0}".Frmt(_period)
        End If
    End Function
End Class
