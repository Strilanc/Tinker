Imports System.Runtime.CompilerServices

Namespace Functional.Futures
    Public Module FutureMethods
        '''<summary>Returns a future which is ready after a specified amount of time.</summary>
        Public Function FutureWait(ByVal dt As TimeSpan) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            If dt.Ticks > Int32.MaxValue Then Throw New ArgumentOutOfRangeException("dt", "Can't wait that long")

            Dim f As New Future
            Dim ds = dt.TotalMilliseconds
            If ds <= 0 Then
                f.SetReady()
            Else
                Dim timer = New Timers.Timer(ds)
                AddHandler timer.Elapsed, Sub()
                                              timer.Dispose()
                                              f.SetReady()
                                          End Sub
                timer.AutoReset = False
                timer.Start()
            End If
            Return f
        End Function

        '''<summary>Returns a future which is ready once all members of a sequence of futures is ready.</summary>
        <Extension()>
        Public Function FutureCompress(ByVal futures As IEnumerable(Of IFuture)) As IFuture
            Contract.Requires(futures IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)

            Dim f As New Future()
            Dim numReady = 0
            Dim numFutures = futures.Count

            Dim notify = Sub()
                             If Threading.Interlocked.Increment(numReady) >= numFutures Then
                                 Call f.SetReady()
                             End If
                         End Sub

            For Each future In futures
                Contract.Assume(future IsNot Nothing)
                future.CallWhenReady(notify)
            Next future
            If numFutures = 0 Then f.SetReady()

            Return f
        End Function

        '''<summary>Returns a future sequence for the outcomes of applying a futurizing function to a sequence.</summary>
        <Extension()>
        Public Function FutureMap(Of TDomain, TImage)(ByVal sequence As IEnumerable(Of TDomain),
                                                      ByVal mappingFunction As Func(Of TDomain, IFuture(Of TImage))) As IFuture(Of IEnumerable(Of TImage))
            Contract.Requires(sequence IsNot Nothing)
            Contract.Requires(mappingFunction IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IEnumerable(Of TImage)))() IsNot Nothing)

            Dim mappingFunction_ = mappingFunction
            Dim futureVals = (From item In sequence Select mappingFunction_(item)).ToList()
            Return FutureCompress(futureVals).EvalWhenReady(
                                   Function() From item In futureVals Select item.Value)
        End Function

        '''<summary>Returns a future for the first value which is not filtered out of the sequence.</summary>
        <Extension()>
        Public Function FutureSelect(Of T)(ByVal sequence As IEnumerable(Of T),
                                           ByVal filterFunction As Func(Of T, IFuture(Of Boolean))) As IFuture(Of Outcome(Of T))
            Contract.Requires(sequence IsNot Nothing)
            Contract.Requires(filterFunction IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome(Of T)))() IsNot Nothing)

            Dim enumerator = sequence.GetEnumerator
            If Not enumerator.MoveNext Then Return failure(Of T)("No matches").Futurize
            Dim f = filterFunction(enumerator.Current)
            Contract.Assume(f IsNot Nothing)
            Dim filterFunction_ = filterFunction
            Return f.EvalWhenValueReady(YCombinator(Of Boolean, IFuture(Of Outcome(Of T)))(
                Function(self) Function(accept)
                                   Do
                                       If accept Then  Return successVal(enumerator.Current, "Matched").Futurize
                                       If Not enumerator.MoveNext Then  Return failure(Of T)("No matches").Futurize
                                       Dim futureAccept = filterFunction_(enumerator.Current)
                                       Contract.Assume(futureAccept IsNot Nothing)
                                       If Not futureAccept.IsReady Then  Return futureAccept.EvalWhenValueReady(self).Defuturize()
                                       accept = futureAccept.Value
                                   Loop
                               End Function)).Defuturize()
        End Function

        '''<summary>Returns a future sequence for the values accepted by the filter.</summary>
        <Extension()>
        Public Function FutureFilter(Of T)(ByVal sequence As IEnumerable(Of T),
                                           ByVal filterFunction As Func(Of T, IFuture(Of Boolean))) As IFuture(Of IEnumerable(Of T))
            Contract.Requires(sequence IsNot Nothing)
            Contract.Requires(filterFunction IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IEnumerable(Of T)))() IsNot Nothing)

            Dim pairs = (From item In sequence Select value = item, futureIncluded = filterFunction(item)).ToList()
            Return FutureCompress(From item In pairs Select item.futureIncluded).EvalWhenReady(
                               Function() From item In pairs Where item.futureIncluded.Value Select item.value)
        End Function

        <Extension()>
        Public Function FutureAggregate(Of T, V)(ByVal sequence As IEnumerable(Of T),
                                                 ByVal aggregator As Func(Of V, T, IFuture(Of V)),
                                                 Optional ByVal initialValue As V = Nothing) As IFuture(Of V)
            Contract.Requires(sequence IsNot Nothing)
            Contract.Requires(aggregator IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of V))() IsNot Nothing)

            Dim enumerator = sequence.GetEnumerator()
            Dim aggregator_ = aggregator
            Return YCombinator(Of V, IFuture(Of V))(
                Function(self) Function(current)
                                   If Not enumerator.MoveNext Then  Return current.Futurize
                                   Return aggregator_(current, enumerator.Current).EvalWhenValueReady(self).Defuturize
                               End Function)(initialValue)
        End Function

        '''<summary>Returns a future for the value obtained by recursively reducing the sequence.</summary>
        <Extension()>
        Public Function FutureReduce(Of T)(ByVal sequence As IEnumerable(Of T),
                                           ByVal reductionFunction As Func(Of T, T, IFuture(Of T)),
                                           Optional ByVal defaultValue As T = Nothing) As IFuture(Of T)
            Contract.Requires(sequence IsNot Nothing)
            Contract.Requires(reductionFunction IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of T))() IsNot Nothing)

            Dim reductionFunction_ = reductionFunction
            Select Case sequence.CountUpTo(2)
                Case 0
                    Return defaultValue.Futurize
                Case 1
                    Return sequence.First.Futurize
                Case Else
                    Dim futurePartialReduction = sequence.EnumBlocks(2).FutureMap(
                        Function(block)
                            If block.Count = 1 Then  Return block(0).Futurize
                            Return reductionFunction_(block(0), block(1))
                        End Function
                    )

                    Return futurePartialReduction.EvalWhenValueReady(
                                Function(partialReduction) partialReduction.FutureReduce(reductionFunction_)).Defuturize
            End Select
        End Function

        <Extension()>
        Public Function QueueCallWhenReady(ByVal future As IFuture,
                                           ByVal queue As ICallQueue,
                                           ByVal action As Action) As IFuture
            Contract.Requires(future IsNot Nothing)
            Contract.Requires(queue IsNot Nothing)
            Contract.Requires(action IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim _queue = queue 'avoids contract verification problems with hoisted arguments
            Dim _action = action
            Return future.EvalWhenReady(Function() _queue.QueueAction(_action)).Defuturize
        End Function

        <Extension()>
        Public Function QueueCallWhenValueReady(Of A1)(ByVal future As IFuture(Of A1),
                                                       ByVal queue As ICallQueue,
                                                       ByVal action As Action(Of A1)) As IFuture
            Contract.Requires(future IsNot Nothing)
            Contract.Requires(queue IsNot Nothing)
            Contract.Requires(action IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim _queue = queue 'avoids contract verification problems with hoisted arguments
            Dim _action = action
            Return future.EvalWhenValueReady(Function(result) _queue.QueueAction(Sub() _action(result))).Defuturize
        End Function

        <Extension()>
        Public Function QueueEvalWhenReady(Of R)(ByVal future As IFuture,
                                                 ByVal queue As ICallQueue,
                                                 ByVal func As Func(Of R)) As IFuture(Of R)
            Contract.Requires(future IsNot Nothing)
            Contract.Requires(queue IsNot Nothing)
            Contract.Requires(func IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of R))() IsNot Nothing)
            Dim _queue = queue 'avoids contract verification problems with hoisted arguments
            Dim _func = func
            Return future.EvalWhenReady(Function() _queue.QueueFunc(_func)).Defuturize
        End Function

        <Extension()>
        Public Function QueueEvalWhenValueReady(Of A1, R)(ByVal future As IFuture(Of A1),
                                                          ByVal queue As ICallQueue,
                                                          ByVal func As Func(Of A1, R)) As IFuture(Of R)
            Contract.Requires(future IsNot Nothing)
            Contract.Requires(queue IsNot Nothing)
            Contract.Requires(func IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of R))() IsNot Nothing)
            Dim _queue = queue 'avoids contract verification problems with hoisted arguments
            Dim _func = func
            Return future.EvalWhenValueReady(Function(result) _queue.QueueFunc(Function() _func(result))).Defuturize
        End Function
    End Module
End Namespace
