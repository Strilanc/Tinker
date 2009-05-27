Imports System.Runtime.CompilerServices

Namespace Functional.Queueing
    '''<summary>Describes a thread-safe call queue for non-blocking calls.</summary>
    Public Interface ICallQueue
        '''<summary>Queues a subroutine to be run and returns a future for the subroutine's eventual completion.</summary>
        Function enqueueAction(ByVal action As Action) As IFuture
        '''<summary>Queues a function to be run and returns a future for the function's eventual output.</summary>
        Function enqueueFunc(Of R)(ByVal func As Func(Of R)) As IFuture(Of R)
    End Interface

    ''' <summary>
    ''' Runs queued calls from the thread to queue the first call.
    ''' Logs unexpected exceptions from queued calls.
    ''' </summary>
    Public MustInherit Class BaseCallQueue
        Implements ICallQueue
        Private ReadOnly lock As New Object() 'Synchronizes access to all variables
        Private ReadOnly calls As New Queue(Of CallQueueCall) 'Queue of calls to run
        Private running As Boolean 'Indicates if calls are running

        Private Class CallQueueCall
            Public ReadOnly [call] As Action
            Public ReadOnly future As Future
            Public Sub New(ByVal [call] As Action, ByVal f As Future)
                Me.call = [call]
                Me.future = f
            End Sub
        End Class

        '''<summary>
        '''Queues a subroutine to be run and returns a future for the subroutine's eventual completion.
        '''Starts running calls from the queue if they were not already being run.
        '''</summary>
        Public Function enqueueAction(ByVal action As Action) As IFuture Implements ICallQueue.enqueueAction
            If action Is Nothing Then Throw New ArgumentNullException("action")
            Dim f As New Future
            SyncLock lock
                calls.Enqueue(New CallQueueCall(action, f))
                If running Then Return f
                running = True
            End SyncLock
            StartRunning()
            Return f
        End Function

        '''<summary>
        '''Queues a function to be run and returns a future for the function's eventual output.
        '''Starts running calls from the queue if they were not already being run.
        '''</summary>
        Public Function enqueueFunc(Of R)(ByVal func As Func(Of R)) As IFuture(Of R) Implements ICallQueue.enqueueFunc
            If func Is Nothing Then Throw New ArgumentNullException("func")
            Dim f As New Future(Of R)
            enqueueAction(Sub() f.setValue(func()))
            Return f
        End Function

        '''<summary>Starts running queued calls.</summary>
        '''<remarks>Overriding this subroutine is the easiest way to change how the queue runs its calls.</remarks>
        Protected MustOverride Sub StartRunning()

        '''<summary>Runs queued calls until there are none left.</summary>
        Protected Sub Run()
            Dim bq = New Queue(Of CallQueueCall) 'buffer queue
            Do
                'Buffer calls
                SyncLock lock
                    'Stop running if queue empty
                    If calls.Count <= 0 Then
                        running = False
                        Return
                    End If
                    'Copy queued calls to non-synced buffer queue
                    Do
                        bq.Enqueue(calls.Dequeue)
                    Loop While calls.Count() > 0
                End SyncLock

                'Run buffered calls
                Do
                    With bq.Dequeue()
                        Try
                            Call .call()
                        Catch ex As Exception
                            Logging.LogUnexpectedException("Exception rose past Call Queue Run ({0}, {1})".frmt(Me.GetType.Name), ex)
                        End Try
                        .future.setReady()
                    End With
                Loop While bq.Count() > 0
            Loop
        End Sub
    End Class

    '''<summary>Runs queued calls on a control's thread.</summary>
    Public Class InvokedCallQueue
        Inherits BaseCallQueue
        Private ReadOnly c As Control

        Public Sub New(ByVal c As Control)
            Me.c = c
        End Sub

        Protected Overrides Sub StartRunning()
            Try
                c.BeginInvoke(CType(AddressOf Run, Action))
            Catch e As InvalidOperationException
                Logging.logUnexpectedException("Invalid Invoke from {0}.StartRunning()".frmt(Me.GetType.Name), e)
            End Try
        End Sub
    End Class

    '''<summary>Runs queued calls on an independent thread.</summary>
    Public Class ThreadedCallQueue
        Inherits BaseCallQueue
        Private ReadOnly thread_name As String

        Public Sub New(Optional ByVal thread_name As String = Nothing)
            Me.thread_name = thread_name
        End Sub

        Protected Overrides Sub StartRunning()
            ThreadedAction(AddressOf Run, thread_name)
        End Sub
    End Class

    '''<summary>Runs queued calls on the thread pool.</summary>
    Public Class ThreadPoolCallQueue
        Inherits BaseCallQueue
        Protected Overrides Sub StartRunning()
            Threading.ThreadPool.QueueUserWorkItem(Sub() Run())
        End Sub
    End Class
End Namespace
