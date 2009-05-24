Imports System.Runtime.CompilerServices

Namespace Immutable
    ''' <summary>
    ''' An immutable stack.
    ''' Pushed and popped operations will return a new stack instead of mutating the current one.
    ''' Pushed and popped are constant time.
    ''' </summary>
    Public NotInheritable Class MStack(Of T)
        Public Shared ReadOnly Empty As New MStack(Of T)()

        Private ReadOnly [next] As MStack(Of T)
        Private ReadOnly val As T
        Public ReadOnly size As Integer

        Private Sub New()
        End Sub
        Private Sub New(ByVal val As T, ByVal [next] As MStack(Of T))
            Me.val = val
            Me.next = [next]
            Me.size = [next].size + 1
        End Sub

        Public Function peek() As T
            If size = 0 Then Throw New InvalidOperationException("Empty Stack")
            Return val
        End Function
        Public Function popped() As MStack(Of T)
            If size = 0 Then Throw New InvalidOperationException("Empty Stack")
            Return Me.next
        End Function
        Public Function pushed(ByVal val As T) As MStack(Of T)
            Return New MStack(Of T)(val, Me)
        End Function
    End Class

    Public Module MStackExtensions
#Region "Enumeration Classes"
        Private MustInherit Class BaseEnumerator(Of T, R)
            Inherits BaseIterator(Of R)

            Protected ReadOnly start As MStack(Of T)
            Private cur As MStack(Of T)

            Protected MustOverride Function get_stack_val(ByVal cur As MStack(Of T)) As R

            Public Sub New(ByVal stack As MStack(Of T))
                If Not (stack IsNot Nothing) Then Throw New ArgumentException()
                Me.start = stack
            End Sub

            Public Overrides ReadOnly Property Current() As R
                Get
                    Return get_stack_val(cur)
                End Get
            End Property
            Public Overrides Sub Reset()
                cur = Nothing
            End Sub
            Public Overrides Function MoveNext() As Boolean
                If cur Is Nothing Then
                    If start.size = 0 Then Return False
                    cur = start
                Else
                    If cur.size <= 1 Then Return False
                    cur = cur.popped
                End If
                Return True
            End Function
        End Class
        Private Class ValueEnumerator(Of T)
            Inherits BaseEnumerator(Of T, T)
            Public Sub New(ByVal stack As MStack(Of T))
                MyBase.New(stack)
            End Sub
            Protected Overrides Function get_stack_val(ByVal cur As MStack(Of T)) As T
                Return cur.peek()
            End Function
            Public Overrides Function GetEnumerator() As IEnumerator(Of T)
                Return New ValueEnumerator(Of T)(start)
            End Function
        End Class
        Private Class StackEnumerator(Of T)
            Inherits BaseEnumerator(Of T, MStack(Of T))
            Public Sub New(ByVal stack As MStack(Of T))
                MyBase.New(stack)
            End Sub
            Protected Overrides Function get_stack_val(ByVal cur As MStack(Of T)) As MStack(Of T)
                Return cur
            End Function
            Public Overrides Function GetEnumerator() As IEnumerator(Of MStack(Of T))
                Return New StackEnumerator(Of T)(start)
            End Function
        End Class
#End Region

        <Extension()> Public Function stacks(Of T)(ByVal stack As MStack(Of T)) As IEnumerable(Of MStack(Of T))
            If Not (stack IsNot Nothing) Then Throw New ArgumentException()
            Return New StackEnumerator(Of T)(stack)
        End Function

        <Extension()> Public Function values(Of T)(ByVal stack As MStack(Of T)) As IEnumerable(Of T)
            If Not (stack IsNot Nothing) Then Throw New ArgumentException()
            Return New ValueEnumerator(Of T)(stack)
        End Function

        <Extension()> Public Function pushed(Of T)(ByVal stack As MStack(Of T), ByVal values As IEnumerable(Of T)) As MStack(Of T)
            If Not (stack IsNot Nothing) Then Throw New ArgumentException()
            If Not (values IsNot Nothing) Then Throw New ArgumentException()
            For Each e In values
                stack = stack.pushed(e)
            Next e
            Return stack
        End Function

        <Extension()> Public Function reversed(Of T)(ByVal stack As MStack(Of T)) As MStack(Of T)
            If Not (stack IsNot Nothing) Then Throw New ArgumentException()
            Dim r = MStack(Of T).Empty
            For Each e In stack.values
                r = r.pushed(e)
            Next e
            Return r
        End Function
    End Module
End Namespace