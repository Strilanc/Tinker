''' <summary>
''' A multiple-producer, single-consumer lock-free queue.
''' Does NOT guarantee an item be have been enqueued when BeginEnqueue finishes.
''' Does guarantee an item will eventually be queued after BeginEnqueue finishes.
''' Does guarantee that for calls simultaneously in BeginEnqueue, at least one will finish with its item having been enqueued.
''' Does guarantee that if BeginEnqueue(Y) is called after BeginEnqueue(X) finishes, Y will follow X in the queue.
''' </summary>
Public Class SingleConsumerLockFreeQueue(Of T)
    ''' <summary>
    ''' Owned by the consumer.
    ''' This node is the end marker of the consumed nodes.
    ''' The node's next is the next node to be consumed.
    ''' </summary>
    Private head As Node = New Node(Nothing)
    ''' <summary>
    ''' Owned by producers.
    ''' This node is the insertion point, and is not guaranteed to be in the queue at all times. 
    ''' For example while an item is partially enqueued the tail is not in the queue until prev.next is set.
    ''' </summary>
    Private tail As Node = head
    ''' <summary>
    ''' Singly linked list node containing queue items.
    ''' </summary>
    Private Class Node
        Public ReadOnly value As T
        Public [next] As Node
        Public Sub New(ByVal value As T)
            Me.value = value
        End Sub
    End Class

    ''' <summary>
    ''' Begins adding new items to the queue.
    ''' The items may not be dequeueable when this method finishes, but eventually it will be.
    ''' The items are guaranteed to end up adjacent in the queue and in the correct order.
    ''' </summary>
    ''' <remarks>
    ''' An example of what can occur when two items are queued simultaneously:
    ''' Inital state:
    '''   head=tail -> null
    '''   [queue is empty]
    ''' Step 1: First item is created and exchanged with tail.
    '''   head=prev1 -> null
    '''   tail=node1 -> null
    '''   [queue is empty]
    ''' Step 2: Second thread preempts and second item is created and exchanged with tail.
    '''   head=prev1 -> null
    '''   node1=prev2 -> null
    '''   tail=node2 -> null
    '''   [queue is empty]
    ''' Step 3: Second thread finishes setting prev.next.
    '''   head=prev1 -> null
    '''   node1=prev2 -> tail=node2 -> null
    '''   [queue is empty]
    ''' Step 4: First thread comes back and finishes setting prev.next.
    '''   head=prev1 -> node1=prev2 -> tail=node2 -> null
    '''   [queue contains 2 elements]
    ''' </remarks>
    ''' <implementation>
    ''' Each producer creates a new chain, and exchanges the shared tail for the tail of the new chain.
    ''' The producer then links the tail of the previous chain to the head of the new chain.
    ''' A new chain might not be in the main chain when the function exits, but it will be in a chain that will eventually be in the main chain.
    ''' </implementation>
    Public Sub BeginEnqueue(ByVal items As IEnumerable(Of T))
        If items Is Nothing Then Throw New ArgumentNullException("items")
        If Not items.Any Then Return

        'Create new chain
        Dim new_chain_head As Node = Nothing
        Dim new_chain_tail As Node = Nothing
        For Each item In items
            If new_chain_head Is Nothing Then
                new_chain_head = New Node(item)
                new_chain_tail = new_chain_head
            Else
                new_chain_tail.next = New Node(item)
                new_chain_tail = new_chain_tail.next
            End If
        Next item

        'Append chain to previous chain
        Dim prev_chain_tail = Threading.Interlocked.Exchange(Me.tail, new_chain_tail)
        prev_chain_tail.next = new_chain_head
    End Sub
    ''' <summary>
    ''' Begins adding a new item to the queue.
    ''' The item may not be dequeueable when this method finishes, but eventually it will be.
    ''' </summary>
    ''' <implementation>
    ''' Just an inlined and simplified version of BeginEnqueue(IEnumerable(Of T))
    ''' </implementation>
    Public Sub BeginEnqueue(ByVal item As T)
        Dim new_chain = New Node(item)
        Dim prev_chain_tail = Threading.Interlocked.Exchange(Me.tail, new_chain)
        prev_chain_tail.next = new_chain
    End Sub

    ''' <summary>
    ''' Returns true if there were any items in the queue.
    ''' The return value of this function is only stable if the queue is non-empty and you are calling from the consumer thread.
    ''' </summary>
    Public ReadOnly Property WasEmpty As Boolean
        Get
            Return head.next Is Nothing
        End Get
    End Property

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
End Class

''' <summary>
''' Consumes items produced by multiple producers.
''' Ensures at all exit points that either the consumption queue is empty or exactly one consumer will exist.
''' </summary>
Public MustInherit Class BaseLockFreeConsumer(Of T)
    Private ReadOnly queue As New SingleConsumerLockFreeQueue(Of T)
    Private running As Integer 'stores consumer state and is used as a semaphore

    ''' <summary>
    ''' Enqueues an item to be consumed by the consumer.
    ''' Ensures that when the function finishes either the queue is empty or exactly one consumer will exist.
    ''' </summary>
    Protected Sub EnqueueConsume(ByVal item As T)
        queue.BeginEnqueue(item)

        'Start the consumer thread if it is not already running
        If TryAcquireConsumer() Then
            Call StartRunning()
        End If
    End Sub
    ''' <summary>
    ''' Enqueues a sequence of items to be consumed by the consumer.
    ''' The items are guaranteed to end up adjacent in the queue.
    ''' Ensures that when the function finishes either the queue is empty or exactly one consumer will exist.
    ''' </summary>
    Protected Sub EnqueueConsume(ByVal items As IEnumerable(Of T))
        queue.BeginEnqueue(items)

        'Start the consumer thread if it is not already running
        If TryAcquireConsumer() Then
            Call StartRunning()
        End If
    End Sub

    ''' <summary>
    ''' Returns true if consumer responsibilities were acquired by this thread.
    ''' Ensures that when the function finishes either the queue is empty or exactly one consumer will exist.
    ''' </summary>
    Private Function TryAcquireConsumer() As Boolean
        'Don't bother acquiring if there are no items to consume
        'This unsafe check is alright because enqueuers call this method after enqueuing
        'Even if an item is queued between the check and returning false, the enqueuer will call this method again
        'So we never end up with a non-empty idle queue
        If queue.WasEmpty Then Return False

        'Try to acquire consumer responsibilities
        Return Threading.Interlocked.Exchange(running, 1) = 0

        'Note that between the empty check and acquiring the consumer, all queued actions may have been processed.
        'Therefore the queue may be empty at this point, but that's alright. Just a bit of extra work, nothing unsafe.
    End Function
    ''' <summary>
    ''' Returns true if consumer responsibilities were released by this thread.
    ''' Ensures that when the function finishes either the queue is empty or exactly one consumer will exist.
    ''' </summary>
    Private Function TryReleaseConsumer() As Boolean
        Do
            'Don't release while there's still things to consume
            If Not queue.WasEmpty Then Return False

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
    ''' <summary>
    ''' Runs queued calls until there are none left.
    ''' Ensures that when the function finishes either the queue is empty or exactly one consumer will exist.
    ''' </summary>
    Protected Sub Run()
        Do Until TryReleaseConsumer()
            Consume(queue.Dequeue())
        Loop
    End Sub
End Class
