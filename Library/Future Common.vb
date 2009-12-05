Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

Public Module FutureExtensionsEx
    <Extension()>
    <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
    Public Function FutureRead(ByVal this As IO.Stream,
                               ByVal buffer() As Byte,
                               ByVal offset As Integer,
                               ByVal count As Integer) As IFuture(Of Integer)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(buffer IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Requires(count >= 0)
        Contract.Requires(offset + count <= buffer.Length)
        Contract.Ensures(Contract.Result(Of IFuture(Of Integer))() IsNot Nothing)

        Dim result = New FutureFunction(Of Integer)
        Try
            this.BeginRead(buffer:=buffer,
                           offset:=offset,
                           count:=count,
                           state:=Nothing,
                           callback:=Sub(ar) result.SetByEvaluating(Function() this.EndRead(ar)))
        Catch e As Exception
            result.SetFailed(e)
        End Try
        Return result
    End Function

    ''' <summary>
    ''' Passes a produced future into a consumer, waits for the consumer to finish, and repeats while the consumer outputs true.
    ''' </summary>
    Public Function FutureIterate(Of T)(ByVal producer As Func(Of IFuture(Of T)),
                                         ByVal consumer As Func(Of T, Exception, IFuture(Of Boolean))) As IFuture
        Contract.Requires(producer IsNot Nothing)
        Contract.Requires(consumer IsNot Nothing)
        Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)

        Dim result = New FutureAction
        Dim iterator As Action(Of Boolean, Exception) = Nothing
        Dim futureProduct As IFuture(Of T)
        iterator = Sub([continue], consumerException)
                       If consumerException IsNot Nothing Then
                           result.SetFailed(consumerException)
                       ElseIf [continue] Then
                           futureProduct = producer()
                           Contract.Assume(futureProduct IsNot Nothing)
                           futureProduct.EvalWhenValueReady(consumer).Defuturized.CallWhenValueReady(iterator)
                       Else
                           result.SetSucceeded()
                       End If
                   End Sub
        futureProduct = producer()
        Contract.Assume(futureProduct IsNot Nothing)
        futureProduct.EvalWhenValueReady(consumer).Defuturized.CallWhenValueReady(iterator)
        Return result
    End Function

    ''' <summary>
    ''' Passes a produced future into a consumer, waits for the consumer to finish, and continues until an exception occurs.
    ''' </summary>
    ''' <param name="producer">Asynchronously produces values for the consumer to consume.</param>
    ''' <param name="consumer">Consumes values produced by the producer.</param>
    ''' <param name="errorHandler">Called when the produce/consume cycle eventually terminates due to an exception.</param>
    ''' <returns>A future which fails once the produce/consume cycle terminates due to an exception.</returns>
    <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
    Public Function AsyncProduceConsumeUntilError(Of T)(ByVal producer As Func(Of IFuture(Of T)),
                                                        ByVal consumer As Func(Of T, IFuture),
                                                        ByVal errorHandler As Action(Of Exception)) As IFuture
        Contract.Requires(producer IsNot Nothing)
        Contract.Requires(consumer IsNot Nothing)
        Contract.Requires(errorHandler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)

        Dim result = New FutureAction

        'Setup iteration
        Dim finishedConsuming As Action(Of Exception) = Nothing
        Dim finishedProducing As Action(Of T, Exception) = Nothing
        finishedConsuming = Sub(consumerException)
                                If consumerException IsNot Nothing Then
                                    result.SetFailed(consumerException)
                                    Return
                                End If

                                'Produce
                                Try
                                    producer().CallWhenValueReady(finishedProducing)
                                Catch e As Exception
                                    result.SetFailed(e)
                                End Try
                            End Sub
        finishedProducing = Sub(producedValue, producerException)
                                If producerException IsNot Nothing Then
                                    result.SetFailed(producerException)
                                    Return
                                End If

                                'Consume
                                Try
                                    consumer(producedValue).CallWhenReady(finishedConsuming)
                                Catch e As Exception
                                    result.SetFailed(e)
                                End Try
                            End Sub

        'Start
        producer().AssumeNotNull.CallWhenValueReady(finishedProducing)
        result.Catch(errorHandler)

        Return result
    End Function
    Public Function AsyncProduceConsumeUntilError2(Of T)(ByVal producer As Func(Of IFuture(Of T)),
                                                        ByVal consumer As Action(Of T),
                                                        ByVal errorHandler As Action(Of Exception)) As IFuture
        Contract.Requires(producer IsNot Nothing)
        Contract.Requires(consumer IsNot Nothing)
        Contract.Requires(errorHandler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)

        Return AsyncProduceConsumeUntilError(
            producer:=producer,
            errorHandler:=errorHandler,
            consumer:=Function(value)
                          Dim result = New FutureAction
                          result.SetByCalling(Sub() consumer(value))
                          Return result
                      End Function)
    End Function

    ''' <summary>
    ''' Selects the first future value passing a filter.
    ''' Doesn't evaluate the filter on futures past the matching future.
    ''' </summary>
    <Extension()>
    Public Function FutureSelect(Of T)(ByVal sequence As IEnumerable(Of T),
                                       ByVal filterFunction As Func(Of T, IFuture(Of Boolean))) As IFuture(Of T)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(filterFunction IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of T))() IsNot Nothing)

        Dim enumerator = sequence.GetEnumerator
        Dim result = New FutureFunction(Of T)
        Dim iterator As Action(Of Boolean, Exception)
        iterator = Sub(accept, exception)
                       If exception IsNot Nothing Then
                           result.SetFailed(exception)
                       ElseIf accept Then
                           result.SetSucceeded(enumerator.Current)
                       ElseIf Not enumerator.MoveNext Then
                           result.SetFailed(New OperationFailedException("No Matches"))
                       Else
                           Dim futureAccept = filterFunction(enumerator.Current)
                           Contract.Assume(futureAccept IsNot Nothing)
                           futureAccept.CallWhenValueReady(iterator)
                       End If
                   End Sub
        Call iterator(False, Nothing)
        Return result
    End Function
End Module
