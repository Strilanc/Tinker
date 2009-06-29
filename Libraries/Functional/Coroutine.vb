Namespace Functional
    Public Interface ICoroutineYielder
        Sub Yield()
    End Interface
    Public Interface ICoroutineYielder(Of R)
        Sub Yield(ByVal value As R)
    End Interface

    Public Class Coroutine
        Implements IDisposable
        Implements ICoroutineYielder
        Private started As Boolean
        Private finished As Boolean
        Private exception As Exception
        Private ReadOnly lockDisposed As New OneTimeLock()
        Private ReadOnly lockJoined As New Threading.ManualResetEvent(False)
        Private ReadOnly lockProducer As New Threading.ManualResetEvent(True)
        Private ReadOnly lockConsumer As New Threading.ManualResetEvent(False)

        Public Sub New(ByVal coroutine As Action(Of ICoroutineYielder))
            Call ThreadedAction(
                Sub()
                    lockJoined.WaitOne()

                    Try
                        coroutine(Me)
                    Catch ex As Exception
                        If lockDisposed.WasAcquired Then  LogUnexpectedException("Coroutine threw an exception after being disposed.", ex)
                        exception = ex
                    End Try

                    finished = True
                    Dispose()
                End Sub
            )
        End Sub

        Public Enum ContinueOutcome
            Continuing
            Finished
        End Enum
        Public Function [Continue]() As ContinueOutcome
            If lockDisposed.WasAcquired Then Throw New ObjectDisposedException(Me.GetType.Name)

            lockProducer.Reset()
            lockConsumer.Set()
            If Not started Then
                lockJoined.Set()
                started = True
            End If
            lockProducer.WaitOne()

            If exception IsNot Nothing Then Throw New Exception("Coroutine threw an exception.", exception)
            If finished Then Return ContinueOutcome.Finished
            If lockDisposed.WasAcquired Then Throw New ObjectDisposedException(Me.GetType.Name)
            Return ContinueOutcome.Continuing
        End Function
        Private Sub [Yield]() Implements ICoroutineYielder.Yield
            If lockDisposed.WasAcquired Then Throw New ObjectDisposedException(Me.GetType.Name)

            lockConsumer.Reset()
            lockProducer.Set()
            lockConsumer.WaitOne()

            If lockDisposed.WasAcquired Then Throw New ObjectDisposedException(Me.GetType.Name)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If lockDisposed.TryAcquire Then
                lockProducer.Set()
                lockConsumer.Set()
                GC.SuppressFinalize(Me)
            End If
        End Sub

        Protected Overrides Sub Finalize()
            Dispose()
        End Sub
    End Class

    Public Class Coroutine(Of R)
        Implements ICoroutineYielder(Of R)
        Implements IEnumerator(Of R)
        Private coroutineContinuer As Coroutine
        Private coroutineYielder As ICoroutineYielder
        Private cur As R

        Public Sub New(ByVal coroutine As Action(Of ICoroutineYielder(Of R)))
            Contract.Requires(coroutine IsNot Nothing)
            Me.coroutineContinuer = New Coroutine(Sub(yielder)
                                                      Me.coroutineYielder = yielder
                                                      Call coroutine(Me)
                                                  End Sub)
        End Sub

        Private Sub [Yield](ByVal value As R) Implements ICoroutineYielder(Of R).Yield
            cur = value
            coroutineYielder.Yield()
        End Sub

        Public Enum ContinueOutcome
            ProducedValue
            Finished
        End Enum
        Public Function [Continue]() As ContinueOutcome
            Select Case coroutineContinuer.Continue()
                Case Coroutine.ContinueOutcome.Continuing
                    Return ContinueOutcome.ProducedValue
                Case Coroutine.ContinueOutcome.Finished
                    Return ContinueOutcome.Finished
                Case Else
                    Throw New UnreachableException()
            End Select
        End Function
        Public ReadOnly Property Current() As R Implements IEnumerator(Of R).Current
            Get
                Return cur
            End Get
        End Property

        Private ReadOnly Property CurrentObj As Object Implements System.Collections.IEnumerator.Current
            Get
                Return cur
            End Get
        End Property

        Public Function MoveNext() As Boolean Implements IEnumerator(Of R).MoveNext
            Return Me.Continue = ContinueOutcome.ProducedValue
        End Function

        Public Sub Reset() Implements IEnumerator(Of R).Reset
            Throw New NotSupportedException()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            coroutineContinuer.Dispose()
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overrides Sub Finalize()
            Dispose()
        End Sub
    End Class
End Namespace
