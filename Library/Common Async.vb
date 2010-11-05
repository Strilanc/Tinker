Public Module FutureExtensionsEx
    <Extension()>
    Public Async Function ReadExactAsync(ByVal stream As IO.Stream, ByVal size As Integer) As Task(Of Byte())
        Contract.Assume(stream IsNot Nothing)
        Contract.Assume(size > 0)
        'Contract.Ensures(Contract.Result(Of Task(Of Byte()))() IsNot Nothing)

        Dim result = Await ReadBestEffortAsync(stream, size)
        If result.Length = 0 Then Throw New IO.IOException("End of stream.")
        If result.Length < size Then Throw New IO.IOException("End of stream (fragment).")
        Return result
    End Function

    <Extension()>
    Public Async Function ReadBestEffortAsync(ByVal stream As IO.Stream, ByVal maxSize As Integer) As Task(Of Byte())
        Contract.Assume(stream IsNot Nothing)
        Contract.Assume(maxSize > 0)
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

    ''' <summary>
    ''' Selects the first future value passing a filter.
    ''' Doesn't evaluate the filter on futures past the matching future.
    ''' </summary>
    <Extension()>
    Public Async Function FirstMatchAsync(Of T)(ByVal sequence As IEnumerable(Of T),
                                                ByVal filterFunction As Func(Of T, Task(Of Boolean))) As Task(Of T)
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
    Public Sub ChainEventualDisposalTo(ByVal source As DisposableWithTask, ByVal dest As IDisposable)
        Contract.Requires(source IsNot Nothing)
        Contract.Requires(dest IsNot Nothing)
        source.ChainEventualDisposalTo(AddressOf dest.Dispose)
    End Sub
    <Extension()>
    Public Sub ChainEventualDisposalTo(ByVal source As DisposableWithTask, ByVal action As Action)
        Contract.Requires(source IsNot Nothing)
        Contract.Requires(action IsNot Nothing)
        source.DisposalTask.ContinueWith(Sub() action(), TaskContinuationOptions.OnlyOnRanToCompletion)
    End Sub

    <Extension()>
    Public Function DisposeControlAsync(ByVal control As Control) As Task
        Contract.Requires(control IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Dim result = New TaskCompletionSource(Of NoValue)
        Try
            control.BeginInvoke(Sub()
                                    control.Dispose()
                                    result.SetResult(Nothing)
                                End Sub)
        Catch ex As Exception
            ex.RaiseAsUnexpected("Disposing control: {0}.".Frmt(control.Name))
            result.SetResult(Nothing)
        End Try
        Return result.Task.AssumeNotNull()
    End Function
    <Extension()>
    Public Function DisposeAsync(Of T As IDisposable)(ByVal value As Task(Of T)) As Task
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Return value.ContinueWith(Sub(task) task.Result.Dispose(), TaskContinuationOptions.OnlyOnRanToCompletion).AssumeNotNull()
    End Function
    <Extension()>
    Public Function DisposeAllAsync(ByVal values As IEnumerable(Of Task(Of IDisposable))) As Task
        Contract.Requires(values IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Dim results = New List(Of Task)
        For Each value In values
            Contract.Assume(value IsNot Nothing)
            results.Add(value.DisposeAsync())
        Next value
        Return results.AsAggregateTask()
    End Function

    <Pure()>
    Public Function InstantTask() As Task
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Dim result = New TaskCompletionSource(Of NoValue)()
        result.SetResult(Nothing)
        Return result.Task.AssumeNotNull()
    End Function

    <Extension()>
    Public Function IgnoreExceptions(Of T)(ByVal task As TaskCompletionSource(Of T)) As task
        Contract.Requires(task IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Contract.Assume(task.Task IsNot Nothing)
        Return task.Task.IgnoreExceptions()
    End Function
    <Extension()>
    Public Function IgnoreExceptions(ByVal task As Task) As task
        Contract.Requires(task IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
        Return task.Catch(Sub()
                          End Sub)
    End Function
End Module
