'''<summary>
'''Stops the subStream from being written to too quickly by queueing write calls.
'''Doesn't block the caller.
'''</summary>
Public NotInheritable Class ThrottledWriteStream
    Inherits WrappedStream
    Private ReadOnly writer As ThrottledWriter

    Public Sub New(ByVal subStream As IO.Stream,
                   Optional ByVal initialSlack As Double = 0,
                   Optional ByVal costPerWrite As Double = 0,
                   Optional ByVal costPerCharacter As Double = 0,
                   Optional ByVal costLimit As Double = 0,
                   Optional ByVal costRecoveredPerSecond As Double = 1)
        MyBase.New(subStream)
        Contract.Requires(subStream IsNot Nothing)
        Contract.Requires(initialSlack >= 0)
        Contract.Requires(costPerWrite >= 0)
        Contract.Requires(costPerCharacter >= 0)
        Contract.Requires(costLimit >= 0)
        Contract.Requires(costRecoveredPerSecond > 0)
        writer = New ThrottledWriter(subStream, initialSlack, costPerWrite, costPerCharacter, costLimit, costRecoveredPerSecond)
    End Sub

    Public Overrides ReadOnly Property CanSeek() As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides Property Position() As Long
        Get
            Return subStream.Position
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

    Public Overrides Function BeginRead(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal callback As System.AsyncCallback, ByVal state As Object) As System.IAsyncResult
        Return substream.BeginRead(buffer, offset, count, callback, state)
    End Function
    Public Overrides Function EndRead(ByVal asyncResult As System.IAsyncResult) As Integer
        Return substream.EndRead(asyncResult)
    End Function

    '''<summary>Queues a write to the subStream. Doesn't block.</summary>
    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        writer.QueueWrite(SubArray(buffer, offset, count))
    End Sub

    Public Overrides Function BeginWrite(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer, ByVal callback As System.AsyncCallback, ByVal state As Object) As System.IAsyncResult
        Throw New NotSupportedException
    End Function
End Class

Public NotInheritable Class ThrottledWriter
    Inherits AbstractLockFreeConsumer(Of Byte())

    Private ReadOnly destinationStream As IO.Stream
    Private ReadOnly consumptionQueue As New Queue(Of Byte())
    Private ReadOnly ref As ICallQueue = New TaskedCallQueue

    Private ReadOnly costPerWrite As Double
    Private ReadOnly costPerCharacter As Double
    Private ReadOnly costLimit As Double
    Private ReadOnly recoveryRatePerMillisecond As Double

    Private availableSlack As Double
    Private usedCost As Double
    Private lastTick As ModInt32 = Environment.TickCount
    Private consuming As Boolean

    Public Sub New(ByVal destinationStream As IO.Stream,
                   Optional ByVal initialSlack As Double = 0,
                   Optional ByVal costPerWrite As Double = 0,
                   Optional ByVal costPerCharacter As Double = 0,
                   Optional ByVal costLimit As Double = 0,
                   Optional ByVal costRecoveredPerSecond As Double = 1)
        Contract.Requires(destinationStream IsNot Nothing)
        Contract.Requires(initialSlack >= 0)
        Contract.Requires(costPerWrite >= 0)
        Contract.Requires(costPerCharacter >= 0)
        Contract.Requires(costLimit >= 0)
        Contract.Requires(costRecoveredPerSecond > 0)

        Me.destinationStream = destinationStream
        Me.availableSlack = initialSlack
        Me.costPerWrite = costPerWrite
        Me.costPerCharacter = costPerCharacter
        Me.costLimit = costLimit
        Me.recoveryRatePerMillisecond = costRecoveredPerSecond / 1000
    End Sub

    Public Sub QueueWrite(ByVal data As Byte())
        EnqueueConsume(data)
    End Sub

    Protected Overrides Sub StartRunning()
        ref.QueueAction(Sub() Run())
    End Sub
    Protected Overrides Sub Consume(ByVal item As Byte())
        ref.QueueAction(Sub() ContinueConsume(item))
    End Sub
    Private Sub ContinueConsume(ByVal item As Byte())
        If item IsNot Nothing Then
            consumptionQueue.Enqueue(item)
            If consuming Then Return
            consuming = True
        End If

        While consumptionQueue.Count > 0
            'Recover over time
            Dim t As ModInt32 = Environment.TickCount
            Dim dt = t - lastTick
            lastTick = t
            usedCost -= CUInt(dt) * recoveryRatePerMillisecond
            If usedCost < 0 Then usedCost = 0
            'Recover using slack
            If availableSlack > 0 Then
                Dim slack = Math.Min(usedCost, availableSlack)
                availableSlack -= slack
                usedCost -= slack
            End If

            'Wait if necessary
            If usedCost > costLimit Then
                Dim delay = New TimeSpan(ticks:=CLng((usedCost - costLimit) / recoveryRatePerMillisecond * TimeSpan.TicksPerMillisecond))
                delay.FutureWait.QueueCallWhenReady(ref, Sub() ContinueConsume(Nothing))
                Return
            End If

            'Perform write
            Dim data = consumptionQueue.Dequeue()
            usedCost += costPerWrite + costPerCharacter * data.Length
            destinationStream.Write(data, 0, data.Length)
        End While

        consuming = False
    End Sub
End Class
