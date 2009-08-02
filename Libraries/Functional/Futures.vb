Imports System.Runtime.CompilerServices

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
        ReadOnly Property Value() As R
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
        Protected val As R
        Protected ReadOnly lockReady As New OneTimeLock
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
        Public ReadOnly Property Value() As R Implements IFuture(Of R).Value
            Get
                If Not IsReady Then Throw New InvalidOperationException("Attempted to get a future value before it was ready.")
                Return val
            End Get
        End Property

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

    Public Module ExtensionsForIFuture
        '''<summary>Runs an action once the future is ready, and returns a future for the action's completion.</summary>
        <Extension()>
        Public Function CallWhenReady(ByVal future As IFuture,
                                      ByVal action As Action) As IFuture
            Contract.Requires(future IsNot Nothing)
            Contract.Requires(action IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim _future = future 'avoids contract verification problems with hoisted arguments
            Dim _action = action

            Dim lock = New OneTimeLock()
            Dim f = New Future()
            Dim callback As IFuture.ReadiedEventHandler
            callback = Sub() Threading.ThreadPool.QueueUserWorkItem(
                Sub()
                    If lock.TryAcquire Then 'only run once
                        RemoveHandler _future.Readied, callback
                        Call RunWithDebugTrap(_action, "Future callback")
                        f.SetReady()
                    End If
                End Sub
            )

            AddHandler _future.Readied, callback
            If _future.IsReady Then Call callback() 'in case the future was already ready

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
            Return future_.CallWhenReady(Sub() action_(future_.Value))
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
            Return future_.EvalWhenReady(Function() func_(future_.Value))
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

        '''<summary>Returns a future for the readyness of a future of a future.</summary>
        <Extension()>
        Public Function Defuturize(ByVal futureFuture As IFuture(Of IFuture)) As IFuture
            Contract.Requires(futureFuture IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
            Dim f = New Future
            futureFuture.CallWhenValueReady(Sub(future) future.CallWhenReady(Sub() f.SetReady()))
            Return f
        End Function
    End Module
End Namespace
