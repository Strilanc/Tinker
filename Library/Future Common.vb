Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

Public Module FutureExtensionsEx
    <Extension()>
    Public Function FutureRead(ByVal this As IO.Stream,
                               ByVal buffer() As Byte,
                               ByVal offset As Integer,
                               ByVal count As Integer) As IFuture(Of Integer)
        Dim result = New FutureFunction(Of Integer)
        Try
            this.BeginRead(buffer:=buffer, offset:=offset, count:=count, state:=Nothing,
                 callback:=Sub(ar) result.SetByEvaluating(Function() this.EndRead(ar)))
        Catch e As Exception
            result.SetFailed(e)
        End Try
        Return result
    End Function

    ''' <summary>
    ''' Passes a produced future into a consumer, waits for the consumer to finish, and repeats while the consumer outputs true.
    ''' </summary>
    Public Function FutureIterate(Of T)(ByVal producer As Func(Of IFuture(Of T)),
                                         ByVal consumer As Func(Of T, Exception, IFuture(Of Boolean))) As IFuture
        Contract.Requires(producer IsNot Nothing)
        Contract.Requires(consumer IsNot Nothing)

        Dim result = New FutureAction
        Dim iterator As Action(Of Boolean, Exception) = Nothing
        iterator = Sub([continue], consumerException)
                       If consumerException IsNot Nothing Then
                           result.SetFailed(consumerException)
                       ElseIf [continue] Then
                           producer().EvalWhenValueReady(consumer).Defuturized.CallWhenValueReady(iterator)
                       Else
                           result.SetSucceeded()
                       End If
                   End Sub
        producer().EvalWhenValueReady(consumer).Defuturized.CallWhenValueReady(iterator)
        Return result
    End Function

    ''' <summary>
    ''' Passes a produced future into a consumer, waits for the consumer to finish, and continues until an exception occurs.
    ''' </summary>
    Public Function FutureIterateExcept(Of T)(ByVal producer As Func(Of IFuture(Of T)),
                                              ByVal consumer As Action(Of T)) As IFuture
        Contract.Requires(producer IsNot Nothing)
        Contract.Requires(consumer IsNot Nothing)

        Dim result = New FutureAction
        Dim iterator As Action(Of T, Exception) = Nothing
        iterator = Sub(value, valueException)
                       If valueException IsNot Nothing Then
                           result.SetFailed(valueException)
                       Else
                           Try
                               Call consumer(value)
                           Catch e As Exception
                               result.SetFailed(e)
                           End Try
                           producer().CallWhenValueReady(iterator)
                       End If
                   End Sub
        producer().CallWhenValueReady(iterator)
        Return result
    End Function

    ''' <summary>
    ''' Selects the first future value passing a filter.
    ''' Doesn't evaluate the filter on futures past the matching future.
    ''' </summary>
    <Extension()>
    Public Function FutureSelect(Of T)(ByVal sequence As IEnumerable(Of T),
                                       ByVal filterFunction As Func(Of T, IFuture(Of Boolean))) As IFuture(Of T)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(filterFunction IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of T))() IsNot Nothing)

        Dim enumerator = sequence.GetEnumerator
        Dim result = New FutureFunction(Of T)
        Dim iterator As Action(Of Boolean, Exception)
        iterator = Sub(accept, exception)
                       If exception IsNot Nothing Then
                           result.SetFailed(exception)
                       ElseIf accept Then
                           result.SetSucceeded(enumerator.Current)
                       ElseIf Not enumerator.MoveNext Then
                           result.SetFailed(New OperationFailedException("No Matches"))
                       Else
                           Dim futureAccept = filterFunction(enumerator.Current)
                           Contract.Assume(futureAccept IsNot Nothing)
                           futureAccept.CallWhenValueReady(iterator)
                       End If
                   End Sub
        Call iterator(False, Nothing)
        Return result
    End Function
End Module
