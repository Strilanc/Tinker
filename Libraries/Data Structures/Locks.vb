Imports System.Threading

'''<summary>A lock which can only be acquired once, and never released.</summary>
Public Class OneTimeLock
    Private acquired As Integer

    Public Function TryAcquire() As Boolean
        Return Interlocked.Exchange(acquired, 1) = 0
    End Function

    Public ReadOnly Property WasAcquired As Boolean
        Get
            Return acquired <> 0
        End Get
    End Property
End Class

Public Class Throttle
    Private ReadOnly cooldown As TimeSpan
    Private nextAction As Action
    Private nextCallTime As Date
    Private running As Boolean
    Private ReadOnly lock As New Object()

    Public Sub New(ByVal cooldown As TimeSpan)
        Me.cooldown = cooldown
        Me.nextCallTime = Now
    End Sub
    Public Sub SetActionToRun(ByVal action As Action)
        Contract.Requires(action IsNot Nothing)

        Dim t = Now()
        SyncLock lock
            nextAction = action
            If running Then Return
            running = True
        End SyncLock

        If t >= nextCallTime Then
            Execute()
        Else
            FutureWait(nextCallTime - t).CallWhenReady(AddressOf Execute)
        End If
    End Sub
    Private Sub Execute()
        Dim action As action
        SyncLock lock
            action = nextAction
            nextCallTime = Now() + cooldown
            running = False
        End SyncLock
        Call action()
    End Sub
End Class