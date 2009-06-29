Imports System.Runtime.CompilerServices

''''Provides type-safe methods for return values that will be ready in the future, and for passing future arguments into normal functions.
Namespace Functional.Futures
    '''<summary>Represents a thread-safe read-only class that fires an event when it becomes ready.</summary>
    Public Interface IFuture
        '''<summary>Raised when the future becomes ready.</summary>
        Event Readied()
        '''<summary>Returns true if the future is ready.</summary>
        ReadOnly Property IsReady() As Boolean
    End Interface

    '''<summary>Represents a thread-safe read-only class that fires an event when its value becomes ready.</summary>
    Public Interface IFuture(Of Out R)
        Inherits IFuture
        ''' <summary>
        ''' Returns the future's value.
        ''' Throws an InvalidOperationException if the value isn't ready yet.
        ''' </summary>
        Function GetValue() As R
    End Interface

    '''<summary>A thread-safe class that fires an event when it becomes ready.</summary>
    Public Class Future
        Implements IFuture
        Private ReadOnly lockReady As New OneTimeLock
        Public Event Readied() Implements IFuture.Readied

        '''<summary>Returns true if the future is ready.</summary>
        Public ReadOnly Property IsReady() As Boolean Implements IFuture.IsReady
            Get
                Return lockReady.WasAcquired
            End Get
        End Property

        ''' <summary>
        ''' Makes the future ready.
        ''' Throws an InvalidOperationException if the future was already ready.
        ''' </summary>
        Public Sub SetReady()
            If Not TrySetReady() Then
                Throw New InvalidOperationException("Future readied more than once.")
            End If
        End Sub
        ''' <summary>
        ''' Makes the future ready.
        ''' Returns false if the future was already ready.
        ''' </summary>
        Public Function TrySetReady() As Boolean
            If Not lockReady.TryAcquire Then Return False
            RaiseEvent Readied()
            Return True
        End Function
    End Class

    '''<summary>A thread-safe class that fires an event when its value becomes ready.</summary>
    Public Class Future(Of R)
        Implements IFuture(Of R)
        Private val As R
        Private ReadOnly lockReady As New OneTimeLock
        Public Event Readied() Implements IFuture.Readied

        '''<summary>Returns true if the future is ready.</summary>
        Public ReadOnly Property IsReady() As Boolean Implements IFuture.IsReady
            Get
                Return lockReady.WasAcquired
            End Get
        End Property

        '''<summary>
        '''Returns the future's value.
        '''Throws an InvalidOperationException if the value isn't ready yet.
        '''</summary>
        Public Function GetValue() As R Implements IFuture(Of R).GetValue
            If Not IsReady Then Throw New InvalidOperationException("Attempted to get a future value before it was ready.")
            Return val
        End Function

        ''' <summary>
        ''' Sets the future's value and makes the future ready.
        ''' Throws a InvalidOperationException if the future was already ready.
        ''' </summary>
        Public Sub SetValue(ByVal val As R)
            If Not TrySetValue(val) Then
                Throw New InvalidOperationException("Future readied more than once.")
            End If
        End Sub
        ''' <summary>
        ''' Sets the future's value and makes the future ready.
        ''' Fails if the future was already ready.
        ''' </summary>
        Public Function TrySetValue(ByVal val As R) As Boolean
            If Not lockReady.TryAcquire Then Return False
            Me.val = val
            RaiseEvent Readied()
            Return True
        End Function
    End Class

    Public Module Methods
        '''<summary>Runs an action once the future is ready, and returns a future for the action's completion.</summary>
        <Extension()>
        Public Function CallWhenReady(ByVal future As IFuture,
                                      ByVal action As Action) As IFuture
            Contract.Requires(future IsNot Nothing)
            Contract.Requires(action IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim _future = future 'avoids contract verification problems with hoisted arguments
            Dim _action = action

            Dim lock As New OneTimeLock()
            Dim f As New Future()
            Dim notify As IFuture.ReadiedEventHandler
            notify = Sub()
                         If lock.TryAcquire Then 'only run once
                             Call _action()
                             f.SetReady()
                             RemoveHandler _future.Readied, notify
                         End If
                     End Sub

            AddHandler _future.Readied, notify
            If _future.IsReady Then Call notify() 'in case the future was already ready

            Return f
        End Function

        '''<summary>Passes the future's value to an action once ready, and returns a future for the action's completion.</summary>
        <Extension()>
        Public Function CallWhenValueReady(Of A1)(ByVal future As IFuture(Of A1),
                                                  ByVal action As Action(Of A1)) As IFuture
            Contract.Requires(future IsNot Nothing)
            Contract.Requires(action IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim future_ = future 'avoids contract verification problems with hoisted arguments
            Dim action_ = action
            Return future_.CallWhenReady(Sub() action_(future_.GetValue))
        End Function

        '''<summary>Runs a function once the future is ready, and returns a future for the function's return value.</summary>
        <Extension()>
        Public Function EvalWhenReady(Of R)(ByVal future As IFuture,
                                            ByVal func As Func(Of R)) As IFuture(Of R)
            Contract.Requires(future IsNot Nothing)
            Contract.Requires(func IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of R))() IsNot Nothing)
            Dim func_ = func 'avoids contract verification problems with hoisted arguments
            Dim f As New Future(Of R)
            future.CallWhenReady(Sub() f.SetValue(func_()))
            Return f
        End Function

        '''<summary>Passes the future's value to a function once ready, and returns a future for the function's return value.</summary>
        <Extension()>
        Public Function EvalWhenValueReady(Of A1, R)(ByVal future As IFuture(Of A1),
                                                     ByVal func As Func(Of A1, R)) As IFuture(Of R)
            Contract.Requires(future IsNot Nothing)
            Contract.Requires(func IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of R))() IsNot Nothing)
            Dim future_ = future 'avoids contract verification problems with hoisted arguments
            Dim func_ = func
            Return future_.EvalWhenReady(Function() func_(future_.GetValue))
        End Function

        '''<summary>Wraps a normal value as an instantly ready future.</summary>
        <Extension()>
        Public Function Futurize(Of R)(ByVal value As R) As IFuture(Of R)
            Contract.Ensures(Contract.Result(Of IFuture(Of R))() IsNot Nothing)
            Dim f = New Future(Of R)
            f.SetValue(value)
            Return f
        End Function

        '''<summary>Returns a future for the final value of a future of a future.</summary>
        <Extension()>
        Public Function Defuturize(Of R)(ByVal futureFutureVal As IFuture(Of IFuture(Of R))) As IFuture(Of R)
            Contract.Requires(futureFutureVal IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of R))() IsNot Nothing)
            Dim f = New Future(Of R)
            futureFutureVal.CallWhenValueReady(Sub(futureVal) futureVal.CallWhenValueReady(Sub(value) f.SetValue(value)))
            Return f
        End Function

        '''<summary>Returns a future for the completion of a future of a future.</summary>
        <Extension()>
        Public Function Defuturize(ByVal futureFuture As IFuture(Of IFuture)) As IFuture
            Contract.Requires(futureFuture IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim f = New Future
            futureFuture.CallWhenValueReady(Sub(future) future.CallWhenReady(Sub() f.SetReady()))
            Return f
        End Function

        '''<summary>Returns a future which is ready after a specified amount of time.</summary>
        Public Function FutureWait(ByVal dt As TimeSpan) As IFuture
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)

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

        '''<summary>Returns a future which is ready once a set of futures is ready.</summary>
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
                future.CallWhenReady(notify)
            Next future
            If numFutures = 0 Then f.SetReady()

            Return f
        End Function

        '''<summary>Blocks the calling thread until the given futures is ready.</summary>
        Public Sub BlockOnFuture(ByVal future As IFuture)
            Contract.Requires(future IsNot Nothing)
            Dim t = New Threading.ManualResetEvent(False)
            future.CallWhenReady(Sub() t.Set())
            t.WaitOne()
        End Sub

        '''<summary>Returns a future sequence for the futures values of applying a function to the sequence.</summary>
        <Extension()>
        Public Function FutureMap(Of TDomain, TImage)(ByVal sequence As IEnumerable(Of TDomain),
                                                      ByVal mappingFunction As Func(Of TDomain, IFuture(Of TImage))) As IFuture(Of IEnumerable(Of TImage))
            Contract.Requires(sequence IsNot Nothing)
            Contract.Requires(mappingFunction IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IEnumerable(Of TImage)))() IsNot Nothing)

            Dim futureVals = (From item In sequence Select mappingFunction(item)).ToList()
            Return FutureCompress(futureVals).EvalWhenReady(
                                   Function() From item In futureVals Select item.GetValue)
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
            Return f.EvalWhenValueReady(YCombinator(Of Boolean, IFuture(Of Outcome(Of T)))(
                Function(self) Function(accept)
                                   Do
                                       If accept Then  Return successVal(enumerator.Current, "Matched").Futurize
                                       If Not enumerator.MoveNext Then  Return failure(Of T)("No matches").Futurize
                                       Dim futureAccept = filterFunction(enumerator.Current)
                                       If Not futureAccept.IsReady Then  Return futureAccept.EvalWhenValueReady(self).Defuturize()
                                       accept = futureAccept.GetValue
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
                                   Function() From item In pairs Where item.futureIncluded.GetValue Select item.value)
        End Function

        <Extension()>
        Public Function FutureAggregate(Of T, V)(ByVal sequence As IEnumerable(Of T),
                                                 ByVal aggregator As Func(Of V, T, IFuture(Of V)),
                                                 Optional ByVal initialValue As V = Nothing) As IFuture(Of V)
            Contract.Requires(sequence IsNot Nothing)
            Contract.Requires(aggregator IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of V))() IsNot Nothing)

            Dim enumerator = sequence.GetEnumerator()
            Return YCombinator(Of V, IFuture(Of V))(
                Function(self) Function(current)
                                   If Not enumerator.MoveNext Then  Return current.Futurize
                                   Return aggregator(current, enumerator.Current).EvalWhenValueReady(self).Defuturize
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

            Select Case sequence.CountUpTo(2)
                Case 0
                    Return defaultValue.Futurize
                Case 1
                    Return sequence.First.Futurize
                Case Else
                    Dim futurePartialReduction = sequence.EnumBlocks(2).FutureMap(
                        Function(block)
                            If block.Count = 1 Then  Return block(0).Futurize
                            Return reductionFunction(block(0), block(1))
                        End Function)
                    Return futurePartialReduction.EvalWhenValueReady(
                        Function(partialReduction) partialReduction.FutureReduce(reductionFunction)).Defuturize
            End Select
        End Function
    End Module
End Namespace
