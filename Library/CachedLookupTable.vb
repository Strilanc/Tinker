Public Class CachedLookupTable(Of TKey, TValue)
    Private ReadOnly _indexMap As Dictionary(Of TKey, Integer)
    Private ReadOnly _values As List(Of TValue)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_indexMap IsNot Nothing)
        Contract.Invariant(_values IsNot Nothing)
    End Sub

    Public Sub New()
        _values = New List(Of TValue)()
        _indexMap = New Dictionary(Of TKey, Integer)()
    End Sub
    Public Sub New(ByVal capacity As Integer)
        _values = New List(Of TValue)(capacity:=capacity)
        _indexMap = New Dictionary(Of TKey, Integer)(capacity:=capacity)
    End Sub

    Public ReadOnly Property HasCharacter(ByVal key As TKey) As Boolean
        Get
            Contract.Requires(key IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Boolean)() = _indexMap.ContainsKey(key))
            Return _indexMap.ContainsKey(key)
        End Get
    End Property

    Public Function CacheIndexOf(ByVal key As TKey) As Integer
        Contract.Requires(key IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Integer)() >= 0)
        Contract.Ensures(Contract.Result(Of Integer)() < Count)
        Contract.Ensures(HasCharacter(key))
        Contract.Ensures(Count >= Contract.OldValue(Count))

        If Not _indexMap.ContainsKey(key) Then
            _indexMap.Add(key, _values.Count)
            _values.Add(Nothing)
        End If
        Dim result = _indexMap(key)
        Contract.Assume(result >= 0)
        Contract.Assume(result < Count)
        Contract.Assume(HasCharacter(key))
        Return result
    End Function

    Public Property ValueAt(ByVal index As Integer) As TValue
        Get
            Contract.Requires(index >= 0)
            Contract.Requires(index < Count)
            Return _values(index)
        End Get
        Set(ByVal value As TValue)
            Contract.Requires(index >= 0)
            Contract.Requires(index < Count)
            _values(index) = value
        End Set
    End Property

    Public ReadOnly Property Count As Integer
        Get
            Contract.Ensures(Contract.Result(Of Integer)() >= 0)
            Contract.Ensures(Contract.Result(Of Integer)() = _values.Count)
            Return _values.Count
        End Get
    End Property
End Class
