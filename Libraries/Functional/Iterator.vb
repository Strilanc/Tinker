Namespace Functional
    Public Interface IEnumeratorController(Of T)
        Function Break() As T
        Function Multiple(ByVal sequence As IEnumerator(Of T)) As T
        Function Multiple(ByVal sequence As IEnumerable(Of T)) As T
    End Interface

    ''' <summary>
    ''' Uses a lambda expression to enumerate elements.
    ''' </summary>
    Public NotInheritable Class Enumerator(Of T)
        Implements IEnumerator(Of T)
        Implements IEnumeratorController(Of T)

        Private ReadOnly generator As Func(Of IEnumeratorController(Of T), T)
        Private cur As T
        Private break As Boolean
        Private buffer As IEnumerator(Of T)
        Public Sub New(ByVal generator As Func(Of IEnumeratorController(Of T), T))
            Contract.Requires(generator IsNot Nothing)
            Me.generator = generator
        End Sub

#Region "IEnumerator Interface"
        Public Function MoveNext() As Boolean Implements IEnumerator(Of T).MoveNext
            For i = 0 To 1
                If Me.break Then Return False
                If buffer IsNot Nothing Then
                    If buffer.MoveNext Then
                        Me.cur = buffer.Current
                        Return True
                    Else
                        buffer = Nothing
                    End If
                End If

                If i = 0 Then Me.cur = generator(Me)
            Next i

            Return True
        End Function

        Public ReadOnly Property Current As T Implements IEnumerator(Of T).Current
            Get
                Return Me.cur
            End Get
        End Property

        Private ReadOnly Property CurrentObj As Object Implements System.Collections.IEnumerator.Current
            Get
                Return Me.cur
            End Get
        End Property
        Private Sub Reset() Implements System.Collections.IEnumerator.Reset
            Throw New NotSupportedException()
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            GC.SuppressFinalize(Me)
        End Sub
#End Region

#Region "Status Interface"
        Private Function _Break() As T Implements IEnumeratorController(Of T).Break
            Me.break = True
            Return Nothing
        End Function
        Private Function _Multiple(ByVal sequence As IEnumerator(Of T)) As T Implements IEnumeratorController(Of T).Multiple
            Me.buffer = sequence
            Return Nothing
        End Function
        Private Function _Multiple(ByVal sequence As IEnumerable(Of T)) As T Implements IEnumeratorController(Of T).Multiple
            Me.buffer = sequence.GetEnumerator()
            Return Nothing
        End Function
#End Region
    End Class

    ''' <summary>
    ''' Uses a lambda expression to create enumerators.
    ''' </summary>
    Public Class Enumerable(Of T)
        Implements IEnumerable(Of T)
        Private ReadOnly generator As Func(Of IEnumerator(Of T))
        Public Sub New(ByVal generator As Func(Of IEnumerator(Of T)))
            Contract.Requires(generator IsNot Nothing)
            Me.generator = generator
        End Sub
        Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Dim x = generator()
            If x Is Nothing Then Throw New OperationFailedException("The generator function returned a null value.")
            Return x
        End Function
        Private Function GetEnumeratorObj() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function
    End Class

    ''' <summary>
    ''' Reverses the direction of an enumerator, allowing you to push values instead of pulling them.
    ''' </summary>
    Public NotInheritable Class PushEnumerator(Of T)
        Inherits NotifyingDisposable
        Private finished As Boolean
        Private ReadOnly sequenceQueue As New Queue(Of IEnumerator(Of T))
        Private ReadOnly coroutine As Coroutine

        Public Sub New(ByVal consumer As Action(Of IEnumerator(Of T)))
            Me.coroutine = New Coroutine(
                Sub(coroutine)
                    'Construct the blocking sequence
                    Dim curSubsequence As IEnumerator(Of T) = Nothing
                    Dim sequence = New Enumerator(Of T)(
                        Function(controller)
                            'Move to next element, and when current sequence runs out grab another one
                            While curSubsequence Is Nothing OrElse Not curSubsequence.MoveNext
                                If sequenceQueue.Count <= 0 Then
                                    'Wait for more elements
                                    Call coroutine.Yield()

                                    'Break if there are no more elements to return
                                    If finished Then  Return controller.Break
                                End If

                                'Grab next sequence of elements to return
                                curSubsequence = sequenceQueue.Dequeue()
                            End While

                            Return curSubsequence.Current
                        End Function
                    )

                    'Consume the sequence
                    Call consumer(sequence)
                    While sequence.MoveNext 'get any remainder left by the consumer
                    End While
                End Sub
            )
        End Sub

        ''' <summary>Adds more elements for the consumer, and blocks until they have been consumed.</summary>
        Public Sub Push(ByVal sequence As IEnumerator(Of T))
            Contract.Requires(sequence IsNot Nothing)
            If finished Then Throw New InvalidOperationException("Can't push after PushDone.")
            sequenceQueue.Enqueue(sequence)
            coroutine.Continue()
        End Sub
        ''' <summary>Notifies the consumer that there are no elements, and blocks until the consumer finishes.</summary>
        Public Sub PushDone()
            If finished Then Throw New InvalidOperationException("Can't push after PushDone.")
            finished = True
            coroutine.Continue()
        End Sub

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                coroutine.Dispose()
            End If
        End Sub
    End Class
End Namespace














Public Interface IEnumeratorConsumer(Of T, S)
    Function Break() As S
    Sub YieldMany(ByVal sequence As IEnumerator(Of T))
    Sub Yield(ByVal value As T)
    Function YieldMany(ByVal sequence As IEnumerator(Of T), ByVal newState As S) As S
    Function Yield(ByVal value As T, ByVal newState As S) As S
End Interface
Public NotInheritable Class Enumerator(Of T, S)
    Implements IEnumerator(Of T)
    Implements IEnumeratorConsumer(Of T, S)

    Private ReadOnly generator As Func(Of IEnumeratorConsumer(Of T, S), S, S)
    Private cur As T
    Private break As Boolean
    Private buffer As IEnumerator(Of T)
    Private state As S
    Private ReadOnly queue As New Queue(Of T)

    Public Sub New(ByVal generator As Func(Of IEnumeratorConsumer(Of T, S), S, S), ByVal state As S)
        Contract.Requires(generator IsNot Nothing)
        Me.generator = generator
    End Sub

#Region "IEnumerator Interface"
    Public Function MoveNext() As Boolean Implements IEnumerator(Of T).MoveNext
        For i = 0 To 1
            If Me.break Then Return False
            If queue.Count > 0 Then
                Me.cur = queue.Dequeue
                Return True
            End If
            'If buffer IsNot Nothing Then
            '    If buffer.MoveNext Then
            '        Me.cur = buffer.Current
            '        Return True
            '    Else
            '        buffer = Nothing
            '    End If
            'End If

            If i = 0 Then
                state = generator(Me, state)
            Else
                Throw New InvalidOperationException("Generator didn't do anything.")
            End If
        Next i

        Throw New UnreachableException()
    End Function

    Public ReadOnly Property Current As T Implements IEnumerator(Of T).Current
        Get
            Return Me.cur
        End Get
    End Property

    Private ReadOnly Property CurrentObj As Object Implements System.Collections.IEnumerator.Current
        Get
            Return Me.cur
        End Get
    End Property
    Private Sub Reset() Implements System.Collections.IEnumerator.Reset
        Throw New NotSupportedException()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        GC.SuppressFinalize(Me)
    End Sub
#End Region

    Private Function _Break() As S Implements IEnumeratorConsumer(Of T, S).Break
        Me.break = True
        Return Nothing
    End Function

    Private Sub _Yield(ByVal value As T) Implements IEnumeratorConsumer(Of T, S).Yield
        queue.Enqueue(value)
    End Sub

    Private Sub _YieldMany(ByVal sequence As IEnumerator(Of T)) Implements IEnumeratorConsumer(Of T, S).YieldMany
        While sequence.MoveNext
            queue.Enqueue(sequence.Current())
        End While
    End Sub

    Private Function _Yield(ByVal value As T, ByVal newState As S) As S Implements IEnumeratorConsumer(Of T, S).Yield
        Call _Yield(value)
        Return newState
    End Function

    Private Function _YieldMany(ByVal sequence As System.Collections.Generic.IEnumerator(Of T), ByVal newState As S) As S Implements IEnumeratorConsumer(Of T, S).YieldMany
        Call _YieldMany(sequence)
        Return newState
    End Function
End Class
