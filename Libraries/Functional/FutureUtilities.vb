Namespace Functional.Futures.Utilities
    '''<summary>Becomes ready once a set of futures is ready. Runs an overridable call just before becoming ready.</summary>
    Friend Class FutureKeeper
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
        Public Sub New(ByVal action As Action,
                       ByVal futures As IEnumerable(Of IFuture))
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
                    AddHandler future.Readied, AddressOf notify
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
                    RemoveHandler future.Readied, AddressOf notify
                Next future
            End SyncLock

            'Run
            Try
                Call action()
            Catch ex As Exception
                Logging.LogUnexpectedException("Exception rose past {0}.notify().".frmt(Me.GetType.Name), ex)
            End Try
            Call SetReady()
        End Sub
    End Class

    Friend Class FutureWaiter
        Inherits Future
        Private WithEvents timer As Timers.Timer
        Public Sub New(ByVal span As TimeSpan)
            Me.timer = New Timers.Timer(span.TotalMilliseconds)
            Me.timer.Start()
        End Sub
        Private Sub expire() Handles Timer.Elapsed
            If Not Me.timer.Enabled Then Return
            Me.timer.Enabled = False
            Me.timer.Dispose()
            Me.SetReady()
        End Sub
    End Class

    '''<summary>Abstract class for applying functions to a list of data in the future.</summary>
    '''<typeparam name="DOM">Domain. The type of data being looped over.</typeparam>
    '''<typeparam name="IMG">Image. The output type of the function being applied to the data.</typeparam>
    '''<typeparam name="RET">Return. The type returned by the loop.</typeparam>
    Friend MustInherit Class FutureLooper(Of DOM, IMG, RET)
        Inherits Future(Of RET)
        Private domain As IEnumerator(Of DOM) = Nothing
        Private mappingFunction As Func(Of DOM, IFuture(Of IMG))
        Private breakFlag As Boolean = False
        Protected curReturnVal As RET

        Public Sub New()
        End Sub
        Public Sub New(ByVal f_domain As IFuture(Of IEnumerable(Of DOM)),
                       ByVal mappingFunction As Func(Of DOM, IFuture(Of IMG)),
                       ByVal defaultReturnValue As RET)
            Me.init(f_domain, mappingFunction, defaultReturnValue)
        End Sub

        Protected Sub init(ByVal f_domain As IFuture(Of IEnumerable(Of DOM)),
                           ByVal mappingFunction As Func(Of DOM, IFuture(Of IMG)),
                           ByVal defaultReturnValue As RET)
            Me.mappingFunction = mappingFunction
            Me.curReturnVal = defaultReturnValue
            'wait for the list to be ready
            FutureSub.Call(f_domain,
                Sub(domain)
                    Me.domain = domain.GetEnumerator()
                    runValue() 'run first value
                End Sub
            )
        End Sub
        Protected MustOverride Sub iter(ByVal src As DOM, ByVal dst As IMG)
        Protected Sub break()
            breakFlag = True
        End Sub

        Private Sub runValue()
            'continue or break
            If domain Is Nothing OrElse breakFlag OrElse Not domain.MoveNext() Then
                SetValue(curReturnVal)
                Return
            End If

            'start processing the next item in the list
            Dim e = domain.Current()
            FutureSub.Call(mappingFunction(e),
                Sub(f)
                    'pass value to derived class
                    Call iter(e, f)
                    'run next value
                    runValue()
                End Sub
            )
        End Sub
    End Class

    Friend Class FutureFilterer(Of D)
        Inherits FutureLooper(Of D, Boolean, List(Of D))
        Public Sub New(ByVal f_domain As IFuture(Of IEnumerable(Of D)), ByVal filterFunction As Func(Of D, IFuture(Of Boolean)))
            MyBase.new(f_domain, filterFunction, New List(Of D))
        End Sub
        Protected Overrides Sub iter(ByVal src As D, ByVal dst As Boolean)
            If dst Then curReturnVal.Add(src)
        End Sub
    End Class

    Friend Class FutureMapper(Of D, I)
        Inherits FutureLooper(Of D, I, List(Of I))
        Public Sub New(ByVal f_domain As IFuture(Of IEnumerable(Of D)), ByVal mapFunction As Func(Of D, IFuture(Of I)))
            MyBase.New(f_domain, mapFunction, New List(Of I))
        End Sub
        Protected Overrides Sub iter(ByVal src As D, ByVal dst As I)
            curReturnVal.Add(dst)
        End Sub
    End Class

    Friend Class FutureSelecter(Of D)
        Inherits FutureLooper(Of D, Boolean, D)
        Public Sub New(ByVal f_domain As IFuture(Of IEnumerable(Of D)), ByVal filterFunction As Func(Of D, IFuture(Of Boolean)))
            MyBase.New(f_domain, filterFunction, Nothing)
        End Sub
        Protected Overrides Sub iter(ByVal src As D, ByVal dst As Boolean)
            If dst Then
                curReturnVal = src
                break()
            End If
        End Sub
    End Class

    Friend Class FutureReducer(Of D)
        Inherits FutureLooper(Of D, D, D)
        Private reduceFunction As Func(Of D, D, IFuture(Of D))
        Public Sub New(ByVal f_domain As IFuture(Of IEnumerable(Of D)), ByVal reduceFunction As Func(Of D, D, IFuture(Of D)))
            Me.reduceFunction = reduceFunction
            MyBase.init(f_domain, AddressOf reduce, Nothing)
        End Sub
        Private Function reduce(ByVal n As D) As IFuture(Of D)
            Return reduceFunction(n, curReturnVal)
        End Function
        Protected Overrides Sub iter(ByVal src As D, ByVal dst As D)
            curReturnVal = dst
        End Sub
    End Class
End Namespace
