Namespace DataStructures
    '''<summary>Implements common array view features.</summary>
    Public MustInherit Class BaseArrayView(Of T)
        Implements IEnumerable(Of T)
        '''<summary>The view's backing store.</summary>
        Protected ReadOnly items() As T
        '''<summary>The start of the view's range.</summary>
        Protected ReadOnly offset As Integer
        '''<summary>The size of the view's range.</summary>
        Public ReadOnly length As Integer

#Region "New"
        '''<summary>Creates a view covering an entire array.</summary>
        Protected Sub New(ByVal items() As T)
            Me.New(items, 0, items.Length)
        End Sub
        '''<summary>Creates a view covering part of an array.</summary>
        Protected Sub New(ByVal items() As T, ByVal offset As Integer, ByVal length As Integer)
            Me.New(items, offset, length, 0, items.Length)
        End Sub
        '''<summary>Creates a sub view covering part of an array.</summary>
        Protected Sub New(ByVal items() As T, ByVal rel_offset As Integer, ByVal rel_length As Integer, ByVal base_offset As Integer, ByVal base_length As Integer)
            If Not (items IsNot Nothing) Then Throw New ArgumentException()
            If Not (base_offset >= 0) Then Throw New ArgumentException()
            If Not (rel_offset >= 0) Then Throw New ArgumentException()
            If Not (rel_length >= 0) Then Throw New ArgumentException()
            If Not (base_length >= 0) Then Throw New ArgumentException()
            If Not (rel_offset + rel_length <= base_length) Then Throw New ArgumentException()
            If Not (base_offset + base_length <= items.Length) Then Throw New ArgumentException()
            If Not (base_offset + rel_offset + rel_length <= items.Length) Then Throw New ArgumentException()

            Me.items = items
            Me.offset = base_offset + rel_offset
            Me.length = rel_length
        End Sub
        '''<summary>Creates a view covering a new array.</summary>
        Protected Sub New(ByVal length As Integer)
            If Not (length >= 0) Then Throw New ArgumentException()

            Me.items = New T() {}
            If length > 0 Then ReDim Me.items(0 To length - 1)
            Me.length = length
        End Sub
#End Region

#Region "Access"
        '''<summary>Accesses array items by index relative to the view's offset, and limited to the view's range.</summary>
        Default Public Property item(ByVal index As Integer) As T
            Get
                If index < 0 Or index >= length Then Throw New ArgumentOutOfRangeException("index")
                Return items(index + offset)
            End Get
            Protected Set(ByVal value As T)
                If index < 0 Or index >= length Then Throw New ArgumentOutOfRangeException("index")
                items(index + offset) = value
            End Set
        End Property

        Private Function GetEnumerable() As IEnumerable(Of T)
            Return items.Skip(offset).Take(length)
        End Function
        Private Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Return GetEnumerable.GetEnumerator
        End Function
        Private Function GetEnumeratorObj() As Collections.IEnumerator Implements Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function
#End Region
    End Class

    '''<summary>Provides read-only access restricted to part of an array, relative to a starting offset.</summary>
    Public Class ReadOnlyArrayView(Of T)
        Inherits BaseArrayView(Of T)
#Region "New"
        '''<summary>Creates a view covering an entire array.</summary>
        Public Sub New(ByVal items() As T)
            MyBase.New(items)
        End Sub
        '''<summary>Creates a view covering part of an array.</summary>
        Public Sub New(ByVal items() As T, ByVal offset As Integer, ByVal length As Integer)
            MyBase.New(items, offset, length)
        End Sub
        '''<summary>Creates a sub view covering part of an array.</summary>
        Protected Sub New(ByVal items() As T, ByVal rel_offset As Integer, ByVal rel_length As Integer, ByVal base_offset As Integer, ByVal base_length As Integer)
            MyBase.New(items, rel_offset, rel_length, base_offset, base_length)
        End Sub
        '''<summary>Creates a view covering a new array.</summary>
        Public Sub New(ByVal length As Integer)
            MyBase.New(length)
        End Sub
#End Region

#Region "Transform"
        '''<summary>Creates a more restrictive view of the array, relative to the current view.</summary>
        Public Shadows Function SubView(ByVal rel_offset As Integer, ByVal rel_length As Integer) As ReadOnlyArrayView(Of T)
            Return New ReadOnlyArrayView(Of T)(items, rel_offset, rel_length, Me.offset, Me.length)
        End Function

        Public Overloads Shared Widening Operator CType(ByVal array As T()) As ReadOnlyArrayView(Of T)
            If Not (array IsNot Nothing) Then Throw New ArgumentException()


            Return New ReadOnlyArrayView(Of T)(array)
        End Operator
#End Region
    End Class

    '''<summary>Provides read-only access restricted to part of an immutable array, relative to a starting offset.</summary>
    Public NotInheritable Class ImmutableArrayView(Of T)
        Inherits ReadOnlyArrayView(Of T)
#Region "New"
        '''<summary>Creates a view covering a copy of an entire array.</summary>
        Public Sub New(ByVal items As IEnumerable(Of T))
            MyBase.New(items.ToArray)
        End Sub
        '''<summary>Creates a view covering a copy of part of an array.</summary>
        Public Sub New(ByVal items As T(), ByVal offset As Integer, ByVal length As Integer)
            MyBase.new(items.Skip(offset).Take(length).ToArray)
        End Sub
        '''<summary>Creates a sub view covering part of an immutable array.</summary>
        Private Sub New(ByVal items() As T, ByVal rel_offset As Integer, ByVal rel_length As Integer, ByVal base_offset As Integer, ByVal base_length As Integer)
            MyBase.New(items, rel_offset, rel_length, base_offset, base_length)
        End Sub
        '''<summary>Creates a view covering a new array.</summary>
        Public Sub New(ByVal length As Integer)
            MyBase.New(length)
        End Sub
#End Region

#Region "Transform"
        '''<summary>Creates a more restrictive view of the array, relative to the current view.</summary>
        Public Shadows Function SubView(ByVal rel_offset As Integer, ByVal rel_length As Integer) As ImmutableArrayView(Of T)
            Return New ImmutableArrayView(Of T)(items, rel_offset, rel_length, Me.offset, Me.length)
        End Function
        Public Shadows Function SubView(ByVal rel_offset As Integer) As ImmutableArrayView(Of T)
            Return New ImmutableArrayView(Of T)(items, rel_offset, Me.length - rel_offset, Me.offset, Me.length)
        End Function

        Public Overloads Shared Widening Operator CType(ByVal array As T()) As ImmutableArrayView(Of T)
            If Not (array IsNot Nothing) Then Throw New ArgumentException()


            Return New ImmutableArrayView(Of T)(array)
        End Operator
#End Region
    End Class

    '''<summary>Provides access restricted to part of an array, relative to a starting offset.</summary>
    Public Class ArrayView(Of T)
        Inherits BaseArrayView(Of T)
#Region "New"
        '''<summary>Creates a view covering an entire array.</summary>
        Public Sub New(ByVal items() As T)
            MyBase.New(items)
        End Sub
        '''<summary>Creates a view covering part of an array.</summary>
        Public Sub New(ByVal items() As T, ByVal offset As Integer, ByVal length As Integer)
            MyBase.New(items, offset, length)
        End Sub
        '''<summary>Creates a sub view covering part of an array.</summary>
        Protected Sub New(ByVal items() As T, ByVal rel_offset As Integer, ByVal rel_length As Integer, ByVal base_offset As Integer, ByVal base_length As Integer)
            MyBase.New(items, rel_offset, rel_length, base_offset, base_length)
        End Sub
        '''<summary>Creates a view covering a new array.</summary>
        Public Sub New(ByVal length As Integer)
            MyBase.New(length)
        End Sub
#End Region

#Region "Transform"
        '''<summary>Creates a more restrictive view of the array, relative to the current view.</summary>
        Public Shadows Function SubView(ByVal rel_offset As Integer, ByVal rel_length As Integer) As ArrayView(Of T)
            Return New ArrayView(Of T)(Me.items, rel_offset, rel_length, Me.offset, Me.length)
        End Function

        '''<summary>Creates a read-only view of the array.</summary>
        Public Function ReadOnlyView() As ReadOnlyArrayView(Of T)


            Return New ReadOnlyArrayView(Of T)(Me.items, Me.offset, Me.length)
        End Function

        Public Overloads Shared Widening Operator CType(ByVal array As T()) As ArrayView(Of T)
            If Not (array IsNot Nothing) Then Throw New ArgumentException()


            Return New ArrayView(Of T)(array)
        End Operator
#End Region

#Region "Access"
        '''<summary>Accesses array items by index relative to the view's offset, and limited to the view's range.</summary>
        Default Public Shadows Property item(ByVal index As Integer) As T
            Get
                Return MyBase.item(index)
            End Get
            Set(ByVal value As T)
                MyBase.item(index) = value
            End Set
        End Property
#End Region
    End Class
End Namespace
