Public Module FutureExtensionsEx
    <Extension()>
    Public Async Function ReadExactAsync(stream As IO.Stream, size As Integer) As Task(Of Byte())
        Contract.Assume(stream IsNot Nothing)
        Contract.Assume(size >= 0)
        'Contract.Ensures(Contract.Result(Of Task(Of Byte()))() IsNot Nothing)

        Dim result = Await ReadBestEffortAsync(stream, size)
        If result.Length = 0 Then Throw New IO.IOException("End of stream.")
        If result.Length < size Then Throw New IO.IOException("End of stream (fragment).")
        Return result
    End Function

    <Extension()> <Pure()>
    Public Function ImmediateResult(Of T)(task As Task(Of T)) As T
        Contract.Requires(task IsNot Nothing)
        If Not task.Status = TaskStatus.RanToCompletion Then Throw New InvalidStateException("Task did not complete.")
        Return task.Result
    End Function

    <Extension()>
    Public Async Function ReadBestEffortAsync(stream As IO.Stream, maxSize As Integer) As Task(Of Byte())
        Contract.Assume(stream IsNot Nothing)
        Contract.Assume(maxSize >= 0)
        'Contract.Ensures(Contract.Result(Of Task(Of Byte()))() IsNot Nothing)

        Dim totalRead = 0
        Dim result(0 To maxSize - 1) As Byte
        While totalRead < maxSize
            Dim numRead = Await stream.ReadAsync(result, totalRead, maxSize - totalRead)
            If numRead <= 0 Then Exit While
            totalRead += numRead
        End While
        If totalRead <> maxSize Then ReDim Preserve result(0 To totalRead - 1)
        Return result
    End Function

    <Extension()>
    Public Function CancelledAsFalse(task As Task) As Task(Of Boolean)
        Contract.Requires(task IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of Boolean))() IsNot Nothing)
        Dim r = New TaskCompletionSource(Of Boolean)()
        task.ContinueWith(Sub()
                              If task.IsCanceled Then
                                  r.TrySetResult(False)
                              ElseIf task.IsFaulted Then
                                  If task.Exception.InnerExceptions.All(Function(e) TypeOf e Is TaskCanceledException) Then
                                      r.TrySetResult(False)
                                  Else
                                      r.TrySetException(task.Exception.InnerExceptions)
                                  End If
                              ElseIf task.IsCompleted Then
                                  r.TrySetResult(True)
                              Else
                                  Throw New UnreachableException()
                              End If
                          End Sub)
        Contract.Assume(r.Task IsNot Nothing)
        Return r.Task
    End Function
    <Extension()>
    Public Function MaybeCancelled(Of T)(task As Task(Of T)) As Task(Of Renullable(Of T))
        Contract.Requires(task IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of Renullable(Of T)))() IsNot Nothing)
        Dim r = New TaskCompletionSource(Of Renullable(Of T))()
        task.ContinueWith(Sub()
                              If task.IsCanceled Then
                                  r.TrySetResult(Nothing)
                              ElseIf task.IsFaulted Then
                                  If task.Exception.InnerExceptions.All(Function(e) TypeOf e Is TaskCanceledException) Then
                                      r.TrySetCanceled()
                                  Else
                                      r.TrySetException(task.Exception.InnerExceptions)
                                  End If
                              ElseIf task.IsCompleted Then
                                  r.TrySetResult(task.Result)
                              Else
                                  Throw New UnreachableException()
                              End If
                          End Sub)
        Contract.Assume(r.Task IsNot Nothing)
        Return r.Task
    End Function

    <Extension()>
    Public Async Function WithCancellation(Of T)(task As Task(Of T), ct As CancellationToken) As Task(Of T)
        Dim r = New TaskCompletionSource(Of T)()
        Using d1 = ct.Register(Sub() r.TrySetCanceled()),
              d2 = task.ContinueWith(Sub()
                                         If task.IsCanceled Then
                                             r.TrySetCanceled()
                                         ElseIf task.IsFaulted Then
                                             r.TrySetException(task.Exception.InnerExceptions)
                                         ElseIf task.IsCompleted Then
                                             r.TrySetResult(task.Result)
                                         Else
                                             Throw New UnreachableException()
                                         End If
                                     End Sub)
            Return Await r.Task
        End Using
    End Function

    ''' <summary>
    ''' Selects the first future value passing a filter.
    ''' Doesn't evaluate the filter on futures past the matching future.
    ''' </summary>
    <Extension()>
    Public Async Function FirstMatchAsync(Of T)(sequence As IEnumerable(Of T),
                                                filterFunction As Func(Of T, Task(Of Boolean))) As Task(Of T)
        Contract.Assume(sequence IsNot Nothing)
        Contract.Assume(filterFunction IsNot Nothing)
        'Contract.Ensures(Contract.Result(Of Task(Of T))() IsNot Nothing)

        For Each item In sequence
            If Await filterFunction(item) Then
                Return item
            End If
        Next item
        Throw New OperationFailedException("No Matches")
    End Function

    <Extension()>
    Public Sub ChainEventualDisposalTo(source As DisposableWithTask, dest As IDisposable)
        Contract.Requires(source IsNot Nothing)
        Contract.Requires(dest IsNot Nothing)
        source.ChainEventualDisposalTo(AddressOf dest.Dispose)
    End Sub
    <Extension()>
    Public Sub ChainEventualDisposalTo(source As DisposableWithTask, action As Action)
        Contract.Requires(source IsNot Nothing)
        Contract.Requires(action IsNot Nothing)
        source.DisposalTask.ContinueWith(Sub() action(), TaskContinuationOptions.OnlyOnRanToCompletion)
    End Sub

    <Extension()>
    Public Async Function DisposeControlAsync(control As Control) As Task
        Contract.Assume(control IsNot Nothing)
        Try
            If control.IsHandleCreated AndAlso control.InvokeRequired Then
                Dim r = New TaskCompletionSource(Of NoValue)
                control.BeginInvoke(Sub() r.SetByCalling(Sub() control.Dispose()))
                Await r.Task
            Else
                control.Dispose()
            End If
        Catch ex As Exception
            ex.RaiseAsUnexpected("Disposing control: {0}.".Frmt(control.Name))
        End Try
    End Function
    <Extension()>
    Public Function DisposeAsync(Of T As IDisposable)(value As Task(Of T)) As Task
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Return value.ContinueWith(Sub(task) task.Result.Dispose(), TaskContinuationOptions.OnlyOnRanToCompletion).AssumeNotNull()
    End Function
    <Extension()>
    Public Function DisposeAllAsync(values As IEnumerable(Of Task(Of IDisposable))) As Task
        Contract.Requires(values IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Dim results = New List(Of Task)
        For Each value In values
            Contract.Assume(value IsNot Nothing)
            results.Add(value.DisposeAsync())
        Next value
        Return Task.WhenAll(results).AssumeNotNull()
    End Function

    '''<summary>Exceptions thrown by this task will not be considered unhandled (i.e. so they won't kill the program).</summary>
    <Extension()>
    Public Async Sub ConsiderExceptionsHandled(task As Task)
        Contract.Assume(task IsNot Nothing)
        Try
            Await task
        Catch ex As Exception
        End Try
    End Sub
End Module
