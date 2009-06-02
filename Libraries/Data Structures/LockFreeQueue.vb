Public Class SingleConsumerLockFreeQueue(Of T)
    Private head As Node = New Node(Nothing) 'Owned by consumer
    Private tail As Node = head 'Owned by producers
    Private Class Node
        Public ReadOnly value As T
        Public [next] As Node
        Public Sub New(ByVal value As T)
            Me.value = value
        End Sub
    End Class

    ''' <summary>
    ''' Adds a new item to the queue.
    ''' </summary>
    Public Sub Enqueue(ByVal item As T)
        'Essentially, this works because each node is returned by InterLocked.Exchange *exactly once*
        'Each node has it's .next property set exactly once, and also each node is targeted by .next exactly once, so they end up forming a valid tail
        'This must be the only place 'tail' is modified
        Dim n = New Node(item)
        Threading.Interlocked.Exchange(tail, n).next = n
    End Sub

    ''' <summary>
    ''' Returns the next item in the queue.
    ''' This function must only be called from the consumer thread.
    ''' </summary>
    Public Function Peek() As T
        If head.next Is Nothing Then Throw New InvalidOperationException("Empty Queue")
        Return head.next.value
    End Function
    ''' <summary>
    ''' Removes and returns an item from the queue.
    ''' This function must only be called from the consumer thread.
    ''' </summary>
    Public Function Dequeue() As T
        If head.next Is Nothing Then Throw New InvalidOperationException("Empty Queue")
        head = head.next
        Return head.value
    End Function
    ''' <summary>
    ''' Returns true if there any items in the queue.
    ''' The return value of this function is only stable if the queue is non-empty and you are calling from the consumer thread.
    ''' </summary>
    Public ReadOnly Property IsEmpty As Boolean
        Get
            Return head.next Is Nothing
        End Get
    End Property
End Class

Public MustInherit Class BaseConsumingLockFreeCallQueue(Of T)
    Private ReadOnly queue As New SingleConsumerLockFreeQueue(Of T)
    Private running As Integer 'stores consumer state and is used as a semaphore

    Protected Sub EnqueueConsume(ByVal item As T)
        queue.Enqueue(item)

        'Start the consumer thread if it is not already running
        If TryAcquireConsumer() Then
            Call StartRunning()
        End If
    End Sub

    Private Function TryAcquireConsumer() As Boolean
        'Don't bother acquiring if there are no items to consume
        'This unsafe check is alright because enqueuers call this method after enqueuing
        'Even if an item is queued between the check and returning false, the enqueuer will call this method again
        'So we never end up with a non-empty idle queue
        If queue.IsEmpty Then Return False

        'Try to acquire consumer responsibilities
        Return Threading.Interlocked.Exchange(running, 1) = 0

        'Note that between the empty check and acquiring the consumer, all queued actions may have been processed.
        'Therefore the queue may be empty at this point, but that's alright. Just a bit of extra work, nothing unsafe.
    End Function
    Private Function TryReleaseConsumer() As Boolean
        Do
            'Don't release while there's still things to consume
            If Not queue.IsEmpty Then Return False

            'Release consumer responsibilities
            Threading.Interlocked.Exchange(running, 0)

            'It is possible that a new item was queued between the empty check and actually releasing
            'Therefore it is necessary to check if we can re-acquire in order to guarantee we don't leave a non-empty queue idle
            If Not TryAcquireConsumer() Then Return True

            'Even though we've now acquired consumer, we may have ended up with nothing to process!
            'So let's repeat this whole check for empty/release dance!
            'A caller could become live-locked here if other threads keep emptying and filling the queue.
            'But only consumer threads call here, and the live-lock requires that progress is being made.
            'So it's alright. We still make progress and we still don't end up in an invalid state.
        Loop
    End Function

    '''<summary>Used to start the Run method using the child class' desired method (eg. on a new thread).</summary>
    Protected MustOverride Sub StartRunning()
    '''<summary>Consumes an item.</summary>
    Protected MustOverride Sub Consume(ByVal item As T)
    '''<summary>Runs queued calls until there are none left.</summary>
    Protected Sub Run()
        'Run until queue is empty
        Do Until TryReleaseConsumer()
            Consume(queue.Dequeue())
        Loop
    End Sub
End Class
