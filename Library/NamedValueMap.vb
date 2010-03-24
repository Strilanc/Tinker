Public Class NamedValueMap
    Implements IEnumerable(Of KeyValuePair(Of InvariantString, Object))

    Private ReadOnly _dictionary As New Dictionary(Of InvariantString, Object)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_dictionary IsNot Nothing)
    End Sub

    Public Sub New(ByVal dictionary As Dictionary(Of InvariantString, Object))
        Contract.Requires(dictionary IsNot Nothing)
        If (From v In dictionary Where v.Value Is Nothing).Any Then
            Throw New ArgumentException("Dictionary contained a null value.", "dictionary")
        End If
        Me._dictionary = dictionary
    End Sub

    Public ReadOnly Property ItemRaw(ByVal key As InvariantString) As Object
        Get
            Contract.Ensures(Contract.Result(Of Object)() IsNot Nothing)
            If Not _dictionary.ContainsKey(key) Then Throw New InvalidOperationException("No item with key '{0}'".Frmt(key))
            Dim result = _dictionary(key)
            Contract.Assume(result IsNot Nothing)
            Return result
        End Get
    End Property
    <Pure()>
    Public Function ItemAs(Of TResult)(ByVal key As InvariantString) As TResult
        Contract.Ensures(Contract.Result(Of TResult)() IsNot Nothing)
        Dim item = Me.ItemRaw(key)
        Try
            Return DirectCast(item, TResult).AssumeNotNull
        Catch ex As InvalidCastException
            Throw New InvalidOperationException("Value with key '{0}' has type {1}, not {2}".Frmt(key,
                                                                                                  item.GetType,
                                                                                                  GetType(TResult)),
                                                innerException:=ex)
        End Try
    End Function

    Public ReadOnly Property ContainsKey(ByVal key As InvariantString) As Boolean
        Get
            Return _dictionary.ContainsKey(key)
        End Get
    End Property

    Public Function ToDictionary() As Dictionary(Of InvariantString, Object)
        Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, Object))() IsNot Nothing)
        Return _dictionary.ToDictionary(keySelector:=Function(pair) pair.Key,
                                        elementSelector:=Function(pair) pair.Value)
    End Function

    Public Function GetEnumerator() As IEnumerator(Of KeyValuePair(Of InvariantString, Object)) Implements IEnumerable(Of KeyValuePair(Of InvariantString, Object)).GetEnumerator
        Return _dictionary.GetEnumerator
    End Function
    Private Function GetEnumeratorObj() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function

    Public Shared Widening Operator CType(ByVal dictionary As Dictionary(Of InvariantString, Object)) As NamedValueMap
        Contract.Requires(dictionary IsNot Nothing)
        Contract.Ensures(Contract.Result(Of NamedValueMap)() IsNot Nothing)
        Return New NamedValueMap(dictionary)
    End Operator
End Class
