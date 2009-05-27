'''<summary>
'''Stops the substream from being written to too quickly by queueing write calls.
'''Doesn't block the caller.
'''</summary>
Public Class ThrottledWriteStream
    Inherits StreamWrapper
    Private ReadOnly cost_per_write As Double
    Private ReadOnly cost_per_character As Double
    Private ReadOnly cost_limit As Double
    Private ReadOnly recovery_rate As Double
    Private ReadOnly writeQueue As New Queue(Of Byte())()
    Private available_slack As Double = 0
    Private used_cost As Double = 0
    Private writing As Boolean = False
    Private last_write_time As Date = DateTime.Now()
    Private ReadOnly lock As New Object()

    Public Sub New(ByVal substream As IO.Stream,
                Optional ByVal initial_slack As Double = 0,
                Optional ByVal cost_per_write As Double = 0,
                Optional ByVal cost_per_character As Double = 0,
                Optional ByVal cost_limit As Double = 0,
                Optional ByVal cost_recovered_per_second As Double = 1)
        MyBase.New(substream)
        Me.available_slack = initial_slack
        Me.cost_per_write = cost_per_character
        Me.cost_per_character = cost_per_write
        Me.cost_limit = cost_limit
        Me.recovery_rate = cost_recovered_per_second / TimeSpan.TicksPerSecond
        If Me.recovery_rate <= 0 Then Throw New ArgumentException("recovery_rate must be positive.")
        If Me.cost_limit < 0 Then Throw New ArgumentException("cost_limit must be non-negative.")
    End Sub

    Public Overrides ReadOnly Property CanSeek() As Boolean
        Get
            Return False
        End Get
    End Property
    Public Overrides Property Position() As Long
        Get
            Return substream.Position
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

    '''<summary>Queues a write to the substream. Doesn't block.</summary>
    Public Overrides Sub Write(ByVal buffer() As Byte, ByVal offset As Integer, ByVal count As Integer)
        If count - offset <= 0 Or offset + count > buffer.Length Then Return

        'buffer output
        Dim bb(0 To count - 1) As Byte
        For i = 0 To count - 1
            bb(i) = buffer(offset + i)
        Next i

        'queue writing
        SyncLock lock
            writeQueue.Enqueue(bb)
            If writing Then Return
            writing = True
            ThreadedAction(AddressOf writeLoop, "{0} writeLoop".frmt(Me.GetType.Name))
        End SyncLock
    End Sub

    Private Sub writeLoop()
        Try
            Do
                'Exit when the queue is empty
                SyncLock lock
                    If writeQueue.Count = 0 Then
                        writing = False
                        Return
                    End If
                End SyncLock

                'Wait until cost is under the maximum
                Dim d = DateTime.Now()
                used_cost -= (d - last_write_time).Ticks * recovery_rate
                If used_cost < 0 Then used_cost = 0
                If available_slack > 0 Then
                    Dim x = Math.Min(used_cost, available_slack)
                    available_slack -= x
                    used_cost -= x
                End If
                last_write_time = d
                If used_cost > cost_limit Then
                    Threading.Thread.Sleep(New TimeSpan(CLng((used_cost - cost_limit) / recovery_rate)))
                End If

                'Write next chunk to stream
                Dim bb() As Byte
                SyncLock lock
                    bb = writeQueue.Dequeue()
                End SyncLock
                used_cost += bb.Length * cost_per_write + cost_per_character
                substream.Write(bb, 0, bb.Length)
            Loop
        Catch e As Exception
            Debug.Print("Error writing to {0}: {1}".frmt(Me.GetType.Name, e.Message))
        End Try
    End Sub
End Class
