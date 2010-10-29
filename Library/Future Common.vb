Public Module FutureExtensionsEx
    <Extension()>
    Public Function AsyncRead(ByVal this As IO.Stream,
                              ByVal buffer() As Byte,
                              ByVal offset As Integer,
                              ByVal count As Integer) As Task(Of Integer)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(buffer IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Requires(count >= 0)
        Contract.Requires(offset + count <= buffer.Length)
        Contract.Ensures(Contract.Result(Of Task(Of Integer))() IsNot Nothing)

        Dim result = New TaskCompletionSource(Of Integer)
        result.DependentCall(Sub() this.BeginRead(
                buffer:=buffer,
                offset:=offset,
                count:=count,
                state:=Nothing,
                callback:=Sub(ar) result.SetByEvaluating(Function() this.EndRead(ar))))
        Return result.Task.AssumeNotNull
    End Function

    ''' <summary>
    ''' Passes a produced future into a consumer, waits for the consumer to finish, and repeats while the consumer outputs true.
    ''' </summary>
    Public Function FutureIterate(Of T)(ByVal producer As Func(Of Task(Of T)),
                                        ByVal consumer As Func(Of Task(Of T), Task(Of Boolean))) As Task
        Contract.Requires(producer IsNot Nothing)
        Contract.Requires(consumer IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)

        Dim result = New TaskCompletionSource(Of Boolean)
        Dim iterator As Action(Of Task(Of Boolean)) = Nothing
        Dim futureProduct As Task(Of T)
        iterator = Sub(task)
                       If task.Status = TaskStatus.Faulted Then
                           result.SetException(task.Exception.InnerExceptions)
                       ElseIf task.Result Then
                           futureProduct = producer()
                           Contract.Assume(futureProduct IsNot Nothing)
                           futureProduct.ContinueWith(consumer).Unwrap.ContinueWith(iterator)
                       Else
                           result.SetResult(True)
                       End If
                   End Sub
        futureProduct = producer()
        Contract.Assume(futureProduct IsNot Nothing)
        futureProduct.ContinueWith(consumer).Unwrap.AssumeNotNull.ContinueWith(iterator)
        Return result.Task.AssumeNotNull
    End Function

    ''' <summary>
    ''' Passes a produced future into a consumer, waits for the consumer to finish, and continues until an exception occurs.
    ''' </summary>
    ''' <param name="producer">Asynchronously produces values for the consumer to consume.</param>
    ''' <param name="consumer">Consumes values produced by the producer.</param>
    ''' <param name="errorHandler">Called when the produce/consume cycle eventually terminates due to an exception.</param>
    ''' <returns>A future which fails once the produce/consume cycle terminates due to an exception.</returns>
    Public Function AsyncProduceConsumeUntilError(Of T)(ByVal producer As Func(Of Task(Of T)),
                                                        ByVal consumer As Func(Of T, Task),
                                                        ByVal errorHandler As Action(Of AggregateException)) As Task
        Contract.Requires(producer IsNot Nothing)
        Contract.Requires(consumer IsNot Nothing)
        Contract.Requires(errorHandler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)

        Dim result = New TaskCompletionSource(Of NoValue)

        'Setup iteration
        Dim onFinishedConsuming As Action(Of Task) = Nothing
        Dim onFinishedProducing As Action(Of Task(Of T)) = Nothing
        onFinishedConsuming = Sub(task)
                                  If task IsNot Nothing AndAlso task.Status = TaskStatus.Faulted Then
                                      result.SetException(task.Exception.InnerExceptions)
                                  Else
                                      result.DependentCall(Sub() producer().ContinueWith(onFinishedProducing))
                                  End If
                              End Sub
        onFinishedProducing = Sub(task)
                                  If task.Status = TaskStatus.Faulted Then
                                      result.SetException(task.Exception.InnerExceptions)
                                  Else
                                      result.DependentCall(Sub() consumer(task.Result).ContinueWith(onFinishedConsuming))
                                  End If
                              End Sub

        'Start
        Call onFinishedConsuming(Nothing)
        Contract.Assume(result.Task IsNot Nothing)
        result.Task.Catch(errorHandler)
        Return result.Task
    End Function
    Public Function AsyncProduceConsumeUntilError2(Of T)(ByVal producer As Func(Of Task(Of T)),
                                                         ByVal consumer As Action(Of T),
                                                         ByVal errorHandler As Action(Of AggregateException)) As Task
        Contract.Requires(producer IsNot Nothing)
        Contract.Requires(consumer IsNot Nothing)
        Contract.Requires(errorHandler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)

        Return AsyncProduceConsumeUntilError(
            producer:=producer,
            errorHandler:=errorHandler,
            consumer:=Function(value)
                          Dim result = New TaskCompletionSource(Of NoValue)
                          result.SetByCalling(Sub() consumer(value))
                          Return result.Task
                      End Function)
    End Function

    ''' <summary>
    ''' Selects the first future value passing a filter.
    ''' Doesn't evaluate the filter on futures past the matching future.
    ''' </summary>
    <Extension()>
    Public Async Function FirstMatchAsync(Of T)(ByVal sequence As IEnumerable(Of T),
                                             ByVal filterFunction As Func(Of T, Task(Of Boolean))) As Task(Of T)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(filterFunction IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of T))() IsNot Nothing)

        For Each item In sequence
            If Await filterFunction(item) Then
                Return item
            End If
        Next item
        Throw New OperationFailedException("No Matches")
    End Function

    <Extension()>
    Public Function AsAggregateAllOrNoneTask(Of T As IDisposable)(ByVal tasks As IEnumerable(Of Task(Of T))) As Task(Of IEnumerable(Of T))
        Contract.Requires(tasks IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of IEnumerable(Of T)))() IsNot Nothing)
        Dim result = tasks.AsAggregateTask()
        result.Catch(
            Sub(exception)
                For Each task In tasks
                    If task.Status = TaskStatus.RanToCompletion Then
                        task.Result.Dispose()
                    End If
                Next task
            End Sub
        )
        Return result
    End Function
End Module
