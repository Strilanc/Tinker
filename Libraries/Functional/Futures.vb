''''Provides type-safe methods for return values that will be ready in the future, and for passing future arguments into normal functions.
Namespace Functional.Futures
    '''<summary>Represents a thread-safe read-only class that fires an event when it becomes ready.</summary>
    Public Interface IFuture
        '''<summary>Raised when the future becomes ready.</summary>
        Event ready()
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
        Function getValue() As R
    End Interface

    '''<summary>A thread-safe class that fires an event when it becomes ready.</summary>
    Public Class Future
        Implements IFuture

        Private is_ready As Boolean = False
        Private ReadOnly lock As New Object()
        Public Event ready() Implements IFuture.ready

        '''<summary>Returns true if the future is ready.</summary>
        Public ReadOnly Property isReady() As Boolean Implements IFuture.isReady
            Get
                Return is_ready
            End Get
        End Property

        '''<summary>
        '''Makes the future ready.
        '''Throws an InvalidOperationException if run twice.
        '''</summary>
        Public Sub setReady()
            'Transition from unready to ready
            SyncLock lock
                '[only once!]
                If is_ready Then Throw New InvalidOperationException("Future readied more than once.")
                is_ready = True
            End SyncLock

            RaiseEvent ready()
        End Sub
    End Class

    '''<summary>A thread-safe class that fires an event when its value becomes ready.</summary>
    Public Class Future(Of R)
        Implements IFuture(Of R)
        Private val As R = Nothing
        Private is_ready As Boolean = False
        Private ReadOnly lock As New Object()
        Public Event ready() Implements IFuture.ready

        '''<summary>Returns true if the future is ready.</summary>
        Public ReadOnly Property isReady() As Boolean Implements IFuture.isReady
            Get
                Return is_ready
            End Get
        End Property

        '''<summary>
        '''Returns the future's value.
        '''Throws an InvalidOperationException if the value isn't ready yet.
        '''</summary>
        Public Function getValue() As R Implements IFuture(Of R).getValue
            If Not isReady() Then Throw New InvalidOperationException("Attempted to get a future value before it was ready.")
            Return val
        End Function

        '''<summary>
        '''Sets the future's value and makes the future ready.
        '''Throws a InvalidOperationException if the future was already ready.
        '''</summary>
        Public Sub setValue(ByVal v As R)
            SyncLock lock
                If is_ready Then Throw New InvalidOperationException("Future readied more than once.")
                is_ready = True
            End SyncLock

            val = v
            RaiseEvent ready()
        End Sub
    End Class

    '''<summary>Becomes ready once a set of futures is ready. Runs an overridable call just before becoming ready.</summary>
    Public Class FutureKeeper
        Inherits Future
        Private ReadOnly lock As New Object()
        Private ReadOnly futures As New List(Of IFuture)
        Private initialized As Boolean
        Private waiting_to_run As Boolean
        Private next_future_index As Integer
        Private ReadOnly action As Action

        '''<summary>
        '''Adds the BaseFutureKeeper as a parent to all its child futures.
        '''Runs notify in case all children were already ready.
        '''Throws an InvalidOperationException if called twice.
        '''Should only be run after child has finished initializing, because 'run' may be called before init finishes.
        '''</summary>
        Public Sub New(ByVal action As Action, ByVal futures As IEnumerable(Of IFuture))
            If futures Is Nothing Then Throw New ArgumentNullException("futures")
            If action Is Nothing Then Throw New ArgumentNullException("action")
            Me.action = action

            SyncLock lock
                'Only allow one execution past this point
                If initialized Then Throw New InvalidOperationException("{0} attempted to initialize twice.".frmt(Me.GetType.Name))
                initialized = True

                'Add to child futures
                For Each future In futures
                    If future Is Nothing Then Throw New ArgumentNullException("A member of futures is null.", "futures")
                    Me.futures.Add(future)
                Next future

                'tell children to notify when they become ready
                '[do not combine this with the add-to-list loop, because you could end up with
                '  dangling event handlers if a future is null and the ArgumentNullException is thrown]
                For Each future In futures
                    AddHandler future.ready, AddressOf notify
                Next future

                'start waiting for values
                waiting_to_run = True
            End SyncLock

            'check if children were already ready
            notify()
        End Sub

        '''<summary>
        '''Runs the abstract 'run' method if all child futures are ready for the first time.
        '''Instance future becomes ready after the 'run' method has finished.
        '''Called by child futures when they become ready.
        '''</summary>
        Private Sub notify()
            'Return without doing anything if not all children are ready
            SyncLock lock
                'Exit if not all children are ready
                While next_future_index < futures.Count
                    If Not futures(next_future_index).isReady Then Return
                    next_future_index += 1
                End While

                'Only allow one execution past this point
                If Not initialized Then Return
                If Not waiting_to_run Then Return
                waiting_to_run = False

                'Remove self from children's notify events
                For Each future In futures
                    RemoveHandler future.ready, AddressOf notify
                Next future
            End SyncLock

            'Run
            Try
                Call action()
            Catch ex As Exception
                Logging.LogUnexpectedException("Exception rose past {0}.notify().".frmt(Me.GetType.Name), ex)
            End Try
            Call setReady()
        End Sub
    End Class

    '''<summary>Runs a subroutine once a set of futures is ready. Makes the completion available as a future.</summary>
    Public Class FutureSub
        '''<summary>Runs the given subroutine once the given futures become ready, and returns a future for the sub's completion.</summary>
        Public Shared Function schedule(ByVal callback As Action, ByVal ParamArray futures() As IFuture) As IFuture
            Return New FutureKeeper(callback, futures)
        End Function
        '''<summary>Runs the given subroutine once the given futures become ready, and returns a future for the sub's completion.</summary>
        Public Shared Function schedule(ByVal callback As Action, ByVal futures As IEnumerable(Of IFuture)) As IFuture
            Return New FutureKeeper(callback, futures)
        End Function

        '''<summary>Runs the given subroutine once its arguments are ready, and returns a future for its completion.</summary>
        Public Shared Function frun(Of A1)(ByVal arg1 As IFuture(Of A1), ByVal action As Action(Of A1)) As IFuture
            Return schedule(Sub() action(arg1.getValue), arg1)
        End Function
        '''<summary>Runs the given subroutine once its arguments are ready, and returns a future for its completion.</summary>
        Public Shared Function frun(Of A1, A2)(ByVal action As Action(Of A1, A2), ByVal arg1 As IFuture(Of A1), ByVal arg2 As IFuture(Of A2)) As IFuture
            Return schedule(Sub() action(arg1.getValue, arg2.getValue), arg1, arg2)
        End Function
        '''<summary>Runs the given subroutine once its arguments are ready, and returns a future for its completion.</summary>
        Public Shared Function frun(Of A1, A2, A3)(ByVal action As Action(Of A1, A2, A3), ByVal arg1 As IFuture(Of A1), ByVal arg2 As IFuture(Of A2), ByVal arg3 As IFuture(Of A3)) As IFuture
            Return schedule(Sub() action(arg1.getValue, arg2.getValue, arg3.getValue), arg1, arg2, arg3)
        End Function
        '''<summary>Runs the given subroutine once its arguments are ready, and returns a future for its completion.</summary>
        Public Shared Function frun(Of A1, A2, A3, A4)(ByVal action As Action(Of A1, A2, A3, A4), ByVal arg1 As IFuture(Of A1), ByVal arg2 As IFuture(Of A2), ByVal arg3 As IFuture(Of A3), ByVal arg4 As IFuture(Of A4)) As IFuture
            Return schedule(Sub() action(arg1.getValue, arg2.getValue, arg3.getValue, arg4.getValue), arg1, arg2, arg3, arg4)
        End Function
        '''<summary>Runs the given subroutine once its arguments are ready, and returns a future for its completion.</summary>
        Public Shared Function frun(Of A1, A2, A3, A4, A5)(ByVal action As Action(Of A1, A2, A3, A4, A5), ByVal arg1 As IFuture(Of A1), ByVal arg2 As IFuture(Of A2), ByVal arg3 As IFuture(Of A3), ByVal arg4 As IFuture(Of A4), ByVal arg5 As IFuture(Of A5)) As IFuture
            Return schedule(Sub() action(arg1.getValue, arg2.getValue, arg3.getValue, arg4.getValue, arg5.getValue), arg1, arg2, arg3, arg4, arg5)
        End Function
        '''<summary>Runs the given subroutine once its arguments are ready, and returns a future for its completion.</summary>
        Public Shared Function frun(Of A1, A2, A3, A4, A5, A6)(ByVal action As Action(Of A1, A2, A3, A4, A5, A6), ByVal arg1 As IFuture(Of A1), ByVal arg2 As IFuture(Of A2), ByVal arg3 As IFuture(Of A3), ByVal arg4 As IFuture(Of A4), ByVal arg5 As IFuture(Of A5), ByVal arg6 As IFuture(Of A6)) As IFuture
            Return schedule(Sub() action(arg1.getValue, arg2.getValue, arg3.getValue, arg4.getValue, arg5.getValue, arg6.getValue), arg1, arg2, arg3, arg4, arg5, arg6)
        End Function
    End Class

    '''<summary>Runs a function once a set of futures is ready. Makes the return value available as a future.</summary>
    Public Class FutureFunc
#Region "schedule"
        '''<summary>Runs the given function once the given futures become ready, and returns a future for the function's return value.</summary>
        Public Shared Function schedule(Of R)(ByVal func As Func(Of R), Optional ByVal futures As IEnumerable(Of IFuture) = Nothing) As IFuture(Of R)
            Dim f As New Future(Of R)
            FutureSub.schedule(Sub() f.setValue(func()), futures)
            Return f
        End Function
#End Region

#Region "frun"
        Public Shared Function ffrun(Of R, A1, A2)(ByVal arg1 As IFuture(Of A1),
                                                   ByVal arg2 As IFuture(Of A2),
                                                   ByVal f As Func(Of A1, A2, IFuture(Of R))) As IFuture(Of R)
            Return futurefuture(FutureFunc.frun(arg1, arg2, f))
        End Function
        Public Shared Function ffrun(Of R, A1)(ByVal arg1 As IFuture(Of A1),
                                               ByVal f As Func(Of A1, IFuture(Of R))) As IFuture(Of R)
            Return futurefuture(FutureFunc.frun(arg1, f))
        End Function

        '''<summary>Runs the given function once its arguments are ready, and returns a future for its return value.</summary>
        Public Shared Function frun(Of A1, R)(ByVal arg1 As IFuture(Of A1),
                                              ByVal func As Func(Of A1, R)) As IFuture(Of R)
            Return schedule(Function() func(arg1.getValue), {arg1})
        End Function
        '''<summary>Runs the given function once its arguments are ready, and returns a future for its return value.</summary>
        Public Shared Function frun(Of A1, A2, R)(ByVal arg1 As IFuture(Of A1),
                                                  ByVal arg2 As IFuture(Of A2),
                                                  ByVal func As Func(Of A1, A2, R)) As IFuture(Of R)
            Return schedule(Function() func(arg1.getValue, arg2.getValue), {arg1, arg2})
        End Function
        '''<summary>Runs the given function once its arguments are ready, and returns a future for its return value.</summary>
        Public Shared Function frun(Of A1, A2, A3, R)(ByVal arg1 As IFuture(Of A1),
                                                      ByVal arg2 As IFuture(Of A2),
                                                      ByVal arg3 As IFuture(Of A3),
                                                      ByVal func As Func(Of A1, A2, A3, R)) As IFuture(Of R)
            Return schedule(Function() func(arg1.getValue, arg2.getValue, arg3.getValue), {arg1, arg2, arg3})
        End Function
        '''<summary>Runs the given function once its arguments are ready, and returns a future for its return value.</summary>
        Public Shared Function frun(Of A1, A2, A3, A4, R)(ByVal arg1 As IFuture(Of A1),
                                                          ByVal arg2 As IFuture(Of A2),
                                                          ByVal arg3 As IFuture(Of A3),
                                                          ByVal arg4 As IFuture(Of A4),
                                                          ByVal func As Func(Of A1, A2, A3, A4, R)) As IFuture(Of R)
            Return schedule(Function() func(arg1.getValue, arg2.getValue, arg3.getValue, arg4.getValue), {arg1, arg2, arg3, arg4})
        End Function
#End Region
    End Class

    Public Module Common
#Region "Secondary Functions"
        '''<summary>Wraps a normal value as an instantly ready future.</summary>
        Public Function futurize(Of R)(ByVal v As R) As IFuture(Of R)
            Dim ret = New Future(Of R)
            ret.setValue(v)
            Return ret
        End Function

        '''<summary>Returns a future for the final value of a future of a future.</summary>
        Public Function futurefuture(Of R)(ByVal futureFutureVal As IFuture(Of IFuture(Of R))) As IFuture(Of R)
            Dim f = New Future(Of R)
            FutureSub.frun(
                futureFutureVal,
                Sub(futureVal)
                    FutureSub.frun(futureVal,
                                   Sub(val As R) f.setValue(val))
                End Sub
            )
            Return f
        End Function
        '''<summary>Returns a future for the completion of a future of a future.</summary>
        Public Function futurefuture(ByVal futureFutureAction As IFuture(Of IFuture)) As IFuture
            Dim f = New Future
            FutureSub.frun(
                futureFutureAction,
                Sub(futureAction)
                    FutureSub.schedule(Sub() f.setReady(),
                                       futureAction)
                End Sub
            )
            Return f
        End Function

        Public Function futurecast(Of P, T As P)(ByVal f As IFuture(Of T)) As IFuture(Of P)
            Dim f2 As New Future(Of P)
            FutureSub.frun(f, AddressOf f2.setValue)
            Return f2
        End Function

        Private Class FutureWaiter
            Inherits Future
            Private WithEvents timer As Timers.Timer
            Public Sub New(ByVal span As TimeSpan)
                Me.timer = New Timers.Timer(span.TotalMilliseconds)
                Me.timer.Start()
            End Sub
            Private Sub expire() Handles timer.Elapsed
                If Not Me.timer.Enabled Then Return
                Me.timer.Enabled = False
                Me.timer.Dispose()
                Me.setReady()
            End Sub
        End Class

        '''<summary>Returns a future which becomes ready after a specified amount of time.</summary>
        Public Function futurewait(ByVal dt As TimeSpan) As IFuture
            Return New FutureWaiter(dt)
        End Function

        '''<summary>Blocks the calling thread until the given futures finish.</summary>
        Public Sub BlockOnFutures(ByVal futures As IEnumerable(Of IFuture))
            Dim t = New Threading.ManualResetEvent(False)
            FutureSub.schedule(Function() t.Set(), futures)
            t.WaitOne()
        End Sub
        Public Sub BlockOnFutures(Of R)(ByVal futures As IEnumerable(Of IFuture(Of R)))
            BlockOnFutures(From x In futures Select CType(x, IFuture))
        End Sub
#End Region

#Region "Tertiary Classes"
        '''<summary>Abstract class for applying functions to a list of data in the future.</summary>
        '''<typeparam name="DOM">Domain. The type of data being looped over.</typeparam>
        '''<typeparam name="IMG">Image. The output type of the function being applied to the data.</typeparam>
        '''<typeparam name="RET">Return. The type returned by the loop.</typeparam>
        Private MustInherit Class FutureLooper(Of DOM, IMG, RET)
            Inherits Future(Of RET)
            Private srcList As List(Of DOM) = Nothing
            Private iterFunction As Func(Of DOM, IFuture(Of IMG))
            Private curIndex As Integer = 0
            Private breakFlag As Boolean = False

            Protected curReturnVal As RET
            Protected MustOverride Sub iter(ByVal src As DOM, ByVal dst As IMG)
            Protected Sub break()
                breakFlag = True
            End Sub

            Public Sub New()
            End Sub
            Public Sub New(ByVal srcListPromise As IFuture(Of IEnumerable(Of DOM)), ByVal iterFunction As Func(Of DOM, IFuture(Of IMG)), ByVal baseReturnVal As RET)
                Me.init(srcListPromise, iterFunction, baseReturnVal)
            End Sub
            Protected Sub init(ByVal srcListPromise As IFuture(Of IEnumerable(Of DOM)), ByVal iterFunction As Func(Of DOM, IFuture(Of IMG)), ByVal baseReturnVal As RET)
                Me.iterFunction = iterFunction
                Me.curReturnVal = baseReturnVal
                'wait for the list to be ready
                FutureSub.frun(srcListPromise, AddressOf retrieveList)
            End Sub

            Private Sub retrieveList(ByVal srcList As IEnumerable(Of DOM))
                Me.srcList = srcList.ToList()
                runValue() 'run first value
            End Sub

            Private Sub retrieveValue(ByVal image As IMG)
                'pass value to derived class
                Call iter(srcList(curIndex), image)
                'run next value
                curIndex += 1
                runValue()
            End Sub

            Private Sub runValue()
                'continue or break
                If srcList Is Nothing Or curIndex >= srcList.Count Or breakFlag Then
                    setValue(curReturnVal)
                    Return
                End If
                'start processing the next item in the list
                FutureSub.frun(iterFunction(srcList(curIndex)), AddressOf retrieveValue)
            End Sub
        End Class
        Private Class FutureFilterer(Of D)
            Inherits FutureLooper(Of D, Boolean, List(Of D))
            Public Sub New(ByVal srcListPromise As IFuture(Of IEnumerable(Of D)), ByVal filterFunction As Func(Of D, IFuture(Of Boolean)))
                MyBase.new(srcListPromise, filterFunction, New List(Of D))
            End Sub
            Protected Overrides Sub iter(ByVal src As D, ByVal dst As Boolean)
                If dst Then curReturnVal.Add(src)
            End Sub
        End Class
        Private Class FutureMapper(Of D, I)
            Inherits FutureLooper(Of D, I, List(Of I))
            Public Sub New(ByVal srcListPromise As IFuture(Of IEnumerable(Of D)), ByVal mapFunction As Func(Of D, IFuture(Of I)))
                MyBase.New(srcListPromise, mapFunction, New List(Of I))
            End Sub
            Protected Overrides Sub iter(ByVal src As D, ByVal dst As I)
                curReturnVal.Add(dst)
            End Sub
        End Class
        Private Class FutureSelecter(Of D)
            Inherits FutureLooper(Of D, Boolean, D)
            Public Sub New(ByVal srcListPromise As IFuture(Of IEnumerable(Of D)), ByVal filterFunction As Func(Of D, IFuture(Of Boolean)))
                MyBase.New(srcListPromise, filterFunction, Nothing)
            End Sub
            Protected Overrides Sub iter(ByVal src As D, ByVal dst As Boolean)
                If dst Then
                    curReturnVal = src
                    break()
                End If
            End Sub
        End Class
        Private Class FutureReducer(Of D)
            Inherits FutureLooper(Of D, D, D)
            Private reduceFunction As Func(Of D, D, IFuture(Of D))
            Public Sub New(ByVal srcListPromise As IFuture(Of IEnumerable(Of D)), ByVal reduceFunction As Func(Of D, D, IFuture(Of D)))
                Me.reduceFunction = reduceFunction
                MyBase.init(srcListPromise, AddressOf reduce, Nothing)
            End Sub
            Private Function reduce(ByVal n As D) As IFuture(Of D)
                Return reduceFunction(n, curReturnVal)
            End Function
            Protected Overrides Sub iter(ByVal src As D, ByVal dst As D)
                curReturnVal = dst
            End Sub
        End Class

        Private Class FutureForEacher(Of D)
            Inherits FutureLooper(Of D, Action(Of D), Boolean)
            Public Sub New(ByVal srcListPromise As IFuture(Of IEnumerable(Of D)), ByVal mapFunction As Func(Of D, IFuture(Of Action(Of D))))
                MyBase.New(srcListPromise, mapFunction, True)
            End Sub
            Protected Overrides Sub iter(ByVal src As D, ByVal dst As Action(Of D))
                Call dst(src)
            End Sub
        End Class
#End Region

#Region "Tertiary Functions"
        '''<summary>Selects the first element in the list that causes the filter function to return true.</summary>
        Public Function futureSelect(Of D)(ByVal L As IFuture(Of List(Of D)), ByVal filterFunc As Func(Of D, IFuture(Of Boolean))) As IFuture(Of D)
            Return New FutureSelecter(Of D)(futurecast(Of IEnumerable(Of D), List(Of D))(L), filterFunc)
        End Function

        '''<summary>Applies a function to each element in a list and returns a list of all the outputs.</summary>
        Public Function futureMap(Of D, I)(ByVal L As IFuture(Of List(Of D)), ByVal mapFunc As Func(Of D, IFuture(Of I))) As IFuture(Of List(Of I))
            Return New FutureMapper(Of D, I)(futurecast(Of IEnumerable(Of D), List(Of D))(L), mapFunc)
        End Function

        '''<summary>Selects the first element in the list that causes the filter function to return true.</summary>
        Public Function futureSelect(Of D)(ByVal L As IFuture(Of IEnumerable(Of D)), ByVal filterFunc As Func(Of D, IFuture(Of Boolean))) As IFuture(Of D)
            Return New FutureSelecter(Of D)(L, filterFunc)
        End Function

        '''<summary>Applies a function to each element in a list and returns a list of all the outputs.</summary>
        Public Function futureMap(Of D, I)(ByVal L As IFuture(Of IEnumerable(Of D)), ByVal mapFunc As Func(Of D, IFuture(Of I))) As IFuture(Of List(Of I))
            Return New FutureMapper(Of D, I)(L, mapFunc)
        End Function
#End Region
    End Module
End Namespace
