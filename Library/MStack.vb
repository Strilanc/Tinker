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
        Private ReadOnly value As T
        Public ReadOnly size As Integer

        Private Sub New()
        End Sub
        Private Sub New(ByVal value As T, ByVal [next] As MStack(Of T))
            Me.value = value
            Me.next = [next]
            Me.size = [next].size + 1
        End Sub

        Public Function Peek() As T
            If size = 0 Then Throw New InvalidOperationException("Empty Stack")
            Return value
        End Function
        Public Function Popped() As MStack(Of T)
            If size = 0 Then Throw New InvalidOperationException("Empty Stack")
            Return Me.next
        End Function
        Public Function Pushed(ByVal val As T) As MStack(Of T)
            Return New MStack(Of T)(val, Me)
        End Function
    End Class

    Public Module MStackExtensions
        <Extension()> Public Function EnumStack(Of T)(ByVal stack As MStack(Of T)) As IEnumerable(Of MStack(Of T))
            If stack Is Nothing Then Throw New ArgumentNullException("stack")
            Return New Enumerable(Of MStack(Of T))(
                Function()
                    Dim nextStack = stack
                    Return New Enumerator(Of MStack(Of T))(
                        Function(controller)
                            If nextStack.size = 0 Then  Return controller.Break()

                            Dim s = nextStack
                            nextStack = nextStack.Popped()
                            Return s
                        End Function)
                End Function)
        End Function

        <Extension()> Public Function EnumValues(Of T)(ByVal stack As MStack(Of T)) As IEnumerable(Of T)
            Return From e In stack.EnumStack() Select e.Peek()
        End Function

        <Extension()> Public Function Pushed(Of T)(ByVal stack As MStack(Of T), ByVal values As IEnumerable(Of T)) As MStack(Of T)
            If stack Is Nothing Then Throw New ArgumentNullException("stack")
            If values Is Nothing Then Throw New ArgumentNullException("values")
            For Each e In values
                stack = stack.Pushed(e)
            Next e
            Return stack
        End Function

        <Extension()> Public Function Reversed(Of T)(ByVal stack As MStack(Of T)) As MStack(Of T)
            If stack Is Nothing Then Throw New ArgumentNullException("stack")
            Dim r = MStack(Of T).Empty
            For Each e In stack.EnumValues
                r = r.Pushed(e)
            Next e
            Return r
        End Function
    End Module
End Namespace