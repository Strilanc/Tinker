Imports HostBot.Functional.Futures.Utilities

''''Provides type-safe methods for return values that will be ready in the future, and for passing future arguments into normal functions.
Namespace Functional.Futures
    '''<summary>Represents a thread-safe read-only class that fires an event when it becomes ready.</summary>
    Public Interface IFuture
        '''<summary>Raised when the future becomes ready.</summary>
        Event Readied()
        '''<summary>Returns true if the future is ready.</summary>
        ReadOnly Property isReady() As Boolean
    End Interface

    '''<summary>Represents a thread-safe read-only class that fires an event when its value becomes ready.</summary>
    Public Interface IFuture(Of Out R)
        Inherits IFuture
        '''<summary>
        '''Returns the future's value.
        '''Throws an InvalidOperationException if the value isn't ready yet.
        '''</summary>
        Function GetValue() As R
    End Interface

    '''<summary>A thread-safe class that fires an event when it becomes ready.</summary>
    Public Class Future
        Implements IFuture
        Private _isReady As Integer
        Public Event Readied() Implements IFuture.Readied

        '''<summary>Returns true if the future is ready.</summary>
        Public ReadOnly Property isReady() As Boolean Implements IFuture.isReady
            Get
                Return _isReady <> 0
            End Get
        End Property

        '''<summary>
        '''Makes the future ready.
        '''Throws an InvalidOperationException if run twice.
        '''</summary>
        Public Sub SetReady()
            If Threading.Interlocked.Exchange(_isReady, 1) <> 0 Then
                Throw New InvalidOperationException("Future readied more than once.")
            End If
            RaiseEvent Readied()
        End Sub
    End Class

    '''<summary>A thread-safe class that fires an event when its value becomes ready.</summary>
    Public Class Future(Of R)
        Implements IFuture(Of R)
        Private val As R
        Private _isReady As Integer
        Public Event Readied() Implements IFuture.Readied

        '''<summary>Returns true if the future is ready.</summary>
        Public ReadOnly Property isReady() As Boolean Implements IFuture.isReady
            Get
                Return _isReady <> 0
            End Get
        End Property

        '''<summary>
        '''Returns the future's value.
        '''Throws an InvalidOperationException if the value isn't ready yet.
        '''</summary>
        Public Function GetValue() As R Implements IFuture(Of R).GetValue
            If _isReady = 0 Then
                Throw New InvalidOperationException("Attempted to get a future value before it was ready.")
            End If
            Return val
        End Function

        '''<summary>
        '''Sets the future's value and makes the future ready.
        '''Throws a InvalidOperationException if the future was already ready.
        '''</summary>
        Public Sub SetValue(ByVal val As R)
            If Threading.Interlocked.Exchange(_isReady, 1) <> 0 Then
                Throw New InvalidOperationException("Future readied more than once.")
            End If
            Me.val = val
            RaiseEvent Readied()
        End Sub
    End Class

    '''<summary>Runs a subroutine once a set of futures is ready. Makes the completion available as a future.</summary>
    Public Class FutureSub
        '''<summary>Runs an action once all the given futures become ready and returns a future for the action's completion.</summary>
        Public Shared Function [Call](ByVal futures As IEnumerable(Of IFuture),
                                      ByVal action As Action) As IFuture
            Return New FutureKeeper(action, Futures)
        End Function

        '''<summary>Runs the given subroutine once its arguments are ready, and returns a future for its completion.</summary>
        Public Shared Function [Call](Of A1)(ByVal arg1 As IFuture(Of A1),
                                             ByVal action As Action(Of A1)) As IFuture
            Return FutureSub.Call({arg1}, Sub() action(arg1.GetValue))
        End Function
        '''<summary>Runs the given subroutine once its arguments are ready, and returns a future for its completion.</summary>
        Public Shared Function [Call](Of A1, A2)(ByVal arg1 As IFuture(Of A1),
                                                 ByVal arg2 As IFuture(Of A2),
                                                 ByVal action As Action(Of A1, A2)) As IFuture
            Return FutureSub.Call({arg1, arg2}, Sub() action(arg1.GetValue, arg2.GetValue))
        End Function
        '''<summary>Runs the given subroutine once its arguments are ready, and returns a future for its completion.</summary>
        Public Shared Function [Call](Of A1, A2, A3)(ByVal arg1 As IFuture(Of A1),
                                                     ByVal arg2 As IFuture(Of A2),
                                                     ByVal arg3 As IFuture(Of A3),
                                                     ByVal action As Action(Of A1, A2, A3)) As IFuture
            Return FutureSub.Call({arg1, arg2, arg3}, Sub() action(arg1.GetValue, arg2.GetValue, arg3.GetValue))
        End Function
    End Class

    '''<summary>Runs a function once a set of futures is ready. Makes the return value available as a future.</summary>
    Public Class FutureFunc
        '''<summary>Runs a function once the given futures are ready, and returns a future for its return value.</summary>
        Public Shared Function [Call](Of R)(ByVal func As Func(Of R),
                                            ByVal futures As IEnumerable(Of IFuture)) As IFuture(Of R)
            Dim f As New Future(Of R)
            FutureSub.Call(futures, Sub() f.SetValue(func()))
            Return f
        End Function

        '''<summary>Runs a function once its arguments are ready, and returns a future for its return value.</summary>
        Public Shared Function [Call](Of A1, R)(ByVal arg1 As IFuture(Of A1),
                                                ByVal func As Func(Of A1, R)) As IFuture(Of R)
            Return FutureFunc.Call(Function() func(arg1.GetValue), {arg1})
        End Function
        '''<summary>Runs a function once its arguments are ready, and returns a future for its return value.</summary>
        Public Shared Function [Call](Of A1, A2, R)(ByVal arg1 As IFuture(Of A1),
                                                    ByVal arg2 As IFuture(Of A2),
                                                    ByVal func As Func(Of A1, A2, R)) As IFuture(Of R)
            Return FutureFunc.Call(Function() func(arg1.GetValue, arg2.GetValue), {arg1, arg2})
        End Function
        '''<summary>Runs a function once its arguments are ready, and returns a future for its return value.</summary>
        Public Shared Function [Call](Of A1, A2, A3, R)(ByVal arg1 As IFuture(Of A1),
                                                        ByVal arg2 As IFuture(Of A2),
                                                        ByVal arg3 As IFuture(Of A3),
                                                        ByVal func As Func(Of A1, A2, A3, R)) As IFuture(Of R)
            Return FutureFunc.Call(Function() func(arg1.GetValue, arg2.GetValue, arg3.GetValue), {arg1, arg2, arg3})
        End Function

        '''<summary>Runs a function once its arguments are ready, and returns a future for its final return value.</summary>
        Public Shared Function FCall(Of R, A1)(ByVal arg1 As IFuture(Of A1),
                                               ByVal func As Func(Of A1, IFuture(Of R))) As IFuture(Of R)
            Return futurefuture(FutureFunc.Call(arg1, func))
        End Function
        '''<summary>Runs a function once its arguments are ready, and returns a future for its final return value.</summary>
        Public Shared Function FCall(Of R, A1, A2)(ByVal arg1 As IFuture(Of A1),
                                                   ByVal arg2 As IFuture(Of A2),
                                                   ByVal func As Func(Of A1, A2, IFuture(Of R))) As IFuture(Of R)
            Return futurefuture(FutureFunc.Call(arg1, arg2, func))
        End Function
        '''<summary>Runs a function once its arguments are ready, and returns a future for its final return value.</summary>
        Public Shared Function FCall(Of R, A1, A2, A3)(ByVal arg1 As IFuture(Of A1),
                                                       ByVal arg2 As IFuture(Of A2),
                                                       ByVal arg3 As IFuture(Of A3),
                                                       ByVal func As Func(Of A1, A2, A3, IFuture(Of R))) As IFuture(Of R)
            Return futurefuture(FutureFunc.Call(arg1, arg2, arg3, func))
        End Function
    End Class

    Public Module Common
        '''<summary>Wraps a normal value as an instantly ready future.</summary>
        Public Function futurize(Of R)(ByVal value As R) As IFuture(Of R)
            Dim f = New Future(Of R)
            f.SetValue(value)
            Return f
        End Function

        '''<summary>Returns a future for the final value of a future of a future.</summary>
        Public Function futurefuture(Of R)(ByVal futureFutureVal As IFuture(Of IFuture(Of R))) As IFuture(Of R)
            Dim f = New Future(Of R)
            FutureSub.Call(futureFutureVal,
                Sub(futureVal) FutureSub.Call(futureVal, Sub(val As R) f.SetValue(val))
            )
            Return f
        End Function
        '''<summary>Returns a future for the completion of a future of a future.</summary>
        Public Function futurefuture(ByVal futureFutureAction As IFuture(Of IFuture)) As IFuture
            Dim f = New Future
            FutureSub.Call(futureFutureAction,
                Sub(futureAction) FutureSub.Call({futureAction}, Sub() f.SetReady())
            )
            Return f
        End Function

        '''<summary>Returns a future which becomes ready after a specified amount of time.</summary>
        Public Function FutureWait(ByVal dt As TimeSpan) As IFuture
            Return New FutureWaiter(dt)
        End Function

        '''<summary>Blocks the calling thread until the given futures finish.</summary>
        Public Sub BlockOnFutures(ByVal futures As IEnumerable(Of IFuture))
            Dim t = New Threading.ManualResetEvent(False)
            FutureSub.Call(futures, Function() t.Set())
            t.WaitOne()
        End Sub

        '''<summary>Selects the first element in the list that causes the filter function to return true.</summary>
        Public Function FutureSelect(Of D)(ByVal f_domain As IFuture(Of IEnumerable(Of D)),
                                           ByVal filterFunc As Func(Of D, IFuture(Of Boolean))) As IFuture(Of D)
            Return New FutureSelecter(Of D)(f_domain, filterFunc)
        End Function

        '''<summary>Applies a function to each element in a list and returns a list of all the outputs.</summary>
        Public Function FutureMap(Of D, I)(ByVal f_domain As IFuture(Of IEnumerable(Of D)),
                                           ByVal mapFunction As Func(Of D, IFuture(Of I))) As IFuture(Of List(Of I))
            Return New FutureMapper(Of D, I)(f_domain, mapFunction)
        End Function
    End Module
End Namespace
