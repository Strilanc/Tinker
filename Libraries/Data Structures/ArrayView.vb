Public Interface ISized
    ReadOnly Property Length As Integer
End Interface
Public Interface IReadableList(Of Out T)
    Inherits IEnumerable(Of T)
    Inherits ISized
    Default ReadOnly Property Item(ByVal index As Integer) As T
End Interface
Public Interface IWritableList(Of In T)
    Inherits ISized
    Default WriteOnly Property Item(ByVal index As Integer) As T
End Interface
<ContractClass(GetType(ContractClassIViewableList(Of )))>
Public Interface IViewableList(Of Out T)
    Inherits IReadableList(Of T)
    <Pure()> Function SubView(ByVal relOffset As Integer) As IViewableList(Of T)
    <Pure()> Function SubView(ByVal relOffset As Integer, ByVal relLength As Integer) As IViewableList(Of T)
End Interface

<ContractClassFor(GetType(IViewableList(Of )))>
Public Class ContractClassIViewableList(Of T)
    Implements IViewableList(Of T)
    Default Public ReadOnly Property Item(ByVal index As Integer) As T Implements IReadableList(Of T).Item
        Get
            Throw New NotSupportedException()
        End Get
    End Property

    Public ReadOnly Property Length As Integer Implements ISized.Length
        Get
            Contract.Ensures(Contract.Result(Of Integer)() >= 0)
            Throw New NotSupportedException()
        End Get
    End Property

    <Pure()> Public Function SubView(ByVal relOffset As Integer) As IViewableList(Of T) Implements IViewableList(Of T).SubView
        Contract.Requires(relOffset >= 0)
        Contract.Ensures(Contract.Result(Of IViewableList(Of T))() IsNot Nothing)
        Throw New NotSupportedException()
    End Function

    <Pure()> Public Function SubView(ByVal relOffset As Integer, ByVal relLength As Integer) As IViewableList(Of T) Implements IViewableList(Of T).SubView
        Contract.Requires(relOffset >= 0)
        Contract.Requires(relLength >= 0)
        Contract.Requires(relLength <= Me.Length)
        Contract.Ensures(Contract.Result(Of IViewableList(Of T))() IsNot Nothing)
        Throw New NotSupportedException()
    End Function

    Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        Throw New NotSupportedException()
    End Function
    Public Function GetEnumeratorObj() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Throw New NotSupportedException()
    End Function
End Class

Public Class ViewableList(Of T)
    Implements IViewableList(Of T)
    Protected ReadOnly items As IList(Of T)
    Protected ReadOnly offset As Integer
    Protected ReadOnly _length As Integer

    Public Sub New(ByVal items As IList(Of T))
        Me.New(items, 0, items.Count, 0, items.Count)
        Contract.Requires(items IsNot Nothing)
    End Sub

    Private Sub New(ByVal items As IList(Of T),
                    ByVal relOffset As Integer,
                    ByVal relLength As Integer,
                    ByVal baseOffset As Integer,
                    ByVal baseLength As Integer)
        Contract.Requires(items IsNot Nothing)
        Contract.Requires(baseOffset >= 0)
        Contract.Requires(baseLength >= 0)
        Contract.Requires(relOffset >= 0)
        Contract.Requires(relLength >= 0)
        Contract.Requires(relOffset + relLength <= baseLength)
        Contract.Requires(baseOffset + baseLength <= items.Count)

        Me.items = items
        Me.offset = baseOffset + relOffset
        Me._length = relLength
    End Sub

    Default Public ReadOnly Property Item(ByVal index As Integer) As T Implements IReadableList(Of T).Item
        Get
            If index < 0 Or index >= _length Then Throw New ArgumentOutOfRangeException("index")
            Return items(index + offset)
        End Get
    End Property

    Public ReadOnly Property Length As Integer Implements IReadableList(Of T).Length
        Get
            Return _length
        End Get
    End Property

    Private Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        Dim nextIndex = 0
        Return New Enumerator(Of T)(Function(controller)
                                        If nextIndex >= _length Then  Return controller.Break()
                                        nextIndex += 1
                                        Return items(offset + nextIndex - 1)
                                    End Function)
    End Function
    Private Function GetEnumeratorObj() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function

    <Pure()> Private Function SubView(ByVal offset As Integer) As IViewableList(Of T) Implements IViewableList(Of T).SubView
        Return SubView(offset, Length - offset)
    End Function
    <Pure()> Private Function SubView(ByVal offset As Integer, ByVal length As Integer) As IViewableList(Of T) Implements IViewableList(Of T).SubView
        Return New ViewableList(Of T)(items, offset, length, Me.offset, Me.Length)
    End Function
End Class
