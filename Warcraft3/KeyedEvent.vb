Public Class KeyedEvent(Of TKey, TArg)
    Private ReadOnly handlers As New Dictionary(Of TKey, List(Of Func(Of TArg, IFuture)))

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(handlers IsNot Nothing)
    End Sub

    Public Sub [AddHandler](ByVal key As TKey,
                            ByVal handler As Func(Of TArg, IFuture))
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        If Not handlers.ContainsKey(key) Then
            handlers(key) = New List(Of Func(Of TArg, IFuture))
        End If
        Contract.Assume(handlers(key) IsNot Nothing)
        handlers(key).Add(handler)
    End Sub

    Public Function Raise(ByVal key As TKey,
                          ByVal value As TArg) As IList(Of IFuture)
        Contract.Requires(key IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of IFuture))() IsNot Nothing)
        If Not handlers.ContainsKey(key) Then Return New IFuture() {}
        Contract.Assume(handlers(key) IsNot Nothing)
        Return (From handler In handlers(key)
                Select handler(value)
               ).ToArray
    End Function
End Class
