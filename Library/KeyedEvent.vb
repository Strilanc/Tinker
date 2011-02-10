Public Class KeyedEvent(Of TKey, TArg)
    Private ReadOnly handlers As New Dictionary(Of TKey, List(Of Func(Of TArg, Task)))
    Private ReadOnly lock As New Object()

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(handlers IsNot Nothing)
    End Sub

    Public Function [AddHandler](ByVal key As TKey,
                                 ByVal handler As Func(Of TArg, Task)) As IDisposable
        Contract.Requires(key IsNot Nothing)
        Contract.Requires(handler IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)

        SyncLock lock
            If Not handlers.ContainsKey(key) Then
                handlers(key) = New List(Of Func(Of TArg, Task))
            End If
            Contract.Assume(handlers(key) IsNot Nothing)
            handlers(key).Add(handler)
        End SyncLock

        Return New DelegatedDisposable(Sub()
                                           SyncLock lock
                                               handlers(key).Remove(handler)
                                           End SyncLock
                                       End Sub)
    End Function

    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")>
    Public Function Raise(ByVal key As TKey,
                          ByVal value As TArg) As IRist(Of Task)
        Contract.Requires(key IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IRist(Of Task))() IsNot Nothing)
        SyncLock lock
            If Not handlers.ContainsKey(key) Then Return New Task() {}.AsRist
            Contract.Assume(handlers(key) IsNot Nothing)
            Return (From handler In handlers(key) Select handler(value)).ToRist
        End SyncLock
    End Function
End Class
