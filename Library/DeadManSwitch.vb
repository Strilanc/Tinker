Public Class DeadManSwitch
    Inherits NotifyingDisposable

    Private ReadOnly timer As Timers.Timer
    Private flipped As Boolean
    Public Event Triggered(ByVal sender As DeadManSwitch)

    Public Sub New(ByVal period As TimeSpan,
                   ByVal initiallyArmed As Boolean)
        If period.Ticks <= 0 Then Throw New ArgumentOutOfRangeException("TimeSpan must be positive.")
        Me.timer = New Timers.Timer(period.TotalMilliseconds)
        Me.timer.AutoReset = True
        Me.timer.Enabled = initiallyArmed
        AddHandler timer.Elapsed, Sub() c_Elapsed()
    End Sub

    Public Sub Arm()
        flipped = False
        Me.timer.Stop()
        Me.timer.Start()
    End Sub
    Public Sub Reset()
        flipped = True
    End Sub
    Public Sub Disarm()
        Me.timer.Stop()
    End Sub
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Me.timer.Stop()
        Me.timer.Dispose()
    End Sub

    Private Sub c_Elapsed()
        If flipped Then
            flipped = False
        Else
            Disarm()
            RaiseEvent Triggered(Me)
        End If
    End Sub
End Class
