<ContractClass(GetType(ContractClassForIReadableList(Of )))>
Public Interface IReadableList(Of Out T)
    Inherits IEnumerable(Of T)
    ReadOnly Property Length As Integer
    Default ReadOnly Property Item(ByVal index As Integer) As T
End Interface
<ContractClassFor(GetType(IReadableList(Of )))>
Public Class ContractClassForIReadableList(Of T)
    Implements IReadableList(Of T)
    Default Public ReadOnly Property Item(ByVal index As Integer) As T Implements IReadableList(Of T).Item
        Get
            Contract.Requires(index >= 0)
            Contract.Requires(index < CType(Me, IReadableList(Of T)).Length)
            Throw New NotSupportedException()
        End Get
    End Property
    Public ReadOnly Property Length As Integer Implements IReadableList(Of T).Length
        Get
            Contract.Ensures(Contract.Result(Of Integer)() >= 0)
            Throw New NotSupportedException()
        End Get
    End Property
    Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
        Throw New NotSupportedException()
    End Function
    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Throw New NotSupportedException()
    End Function
End Class

Public Class ViewableList(Of T)
    Implements IReadableList(Of T)
    Implements IEnumerable(Of T)
    Protected ReadOnly items As IList(Of T)
    Protected ReadOnly offset As Integer
    Protected ReadOnly _length As Integer

    <ContractInvariantMethod()> Protected Sub Invariant()
        Contract.Invariant(offset >= 0)
        Contract.Invariant(_length >= 0)
        Contract.Invariant(offset + _length <= items.Count)
    End Sub
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
            Return items(index + offset)
        End Get
    End Property

    Public ReadOnly Property Length As Integer Implements IReadableList(Of T).Length
        Get
            Return _length
        End Get
    End Property

    <Pure()>
    Public Function SubView(ByVal relOffset As Integer) As ViewableList(Of T)
        Contract.Requires(relOffset >= 0)
        Contract.Requires(relOffset <= Length)
        Contract.Ensures(Contract.Result(Of ViewableList(Of T))() IsNot Nothing)
        Return SubView(relOffset, Length - relOffset)
    End Function
    <Pure()>
    Public Function SubView(ByVal relOffset As Integer, ByVal relLength As Integer) As ViewableList(Of T)
        Contract.Requires(relOffset >= 0)
        Contract.Requires(relLength >= 0)
        Contract.Requires(relOffset + relLength <= Me.Length)
        Contract.Ensures(Contract.Result(Of ViewableList(Of T))() IsNot Nothing)
        Return New ViewableList(Of T)(items, relOffset, relLength, offset, Length)
    End Function

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
End Class
