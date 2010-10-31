Public Module FutureExtensionsEx
    <Extension()>
    Public Async Function AsyncReadExact(ByVal stream As IO.Stream, ByVal size As Integer) As Task(Of Byte())
        Contract.Requires(size >= 0)
        Contract.Ensures(Contract.Result(Of Task(Of Byte()))() IsNot Nothing)

        Dim totalRead = 0
        Dim result(0 To size - 1) As Byte
        While totalRead < size
            Dim numRead = Await stream.AsyncRead(result, totalRead, size - totalRead)
            totalRead += numRead

            'End of stream?
            If numRead <= 0 Then
                If totalRead = 0 Then
                    Throw New IO.IOException("End of stream.")
                Else
                    Throw New IO.IOException("End of stream (fragment)")
                End If
            End If
        End While
        Return result
    End Function

    <Extension()>
    Public Function AsyncRead(ByVal this As IO.Stream,
                              ByVal buffer() As Byte,
                              ByVal offset As Integer,
                              ByVal count As Integer) As Task(Of Integer)
        Contract.Requires(this IsNot Nothing)
        Contract.Requires(buffer IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Requires(count >= 0)
        Contract.Requires(offset + count <= buffer.Length)
        Contract.Ensures(Contract.Result(Of Task(Of Integer))() IsNot Nothing)

        Dim result = New TaskCompletionSource(Of Integer)
        result.DependentCall(Sub() this.BeginRead(
                buffer:=buffer,
                offset:=offset,
                count:=count,
                state:=Nothing,
                callback:=Sub(ar) result.SetByEvaluating(Function() this.EndRead(ar))))
        Return result.Task.AssumeNotNull
    End Function

    ''' <summary>
    ''' Selects the first future value passing a filter.
    ''' Doesn't evaluate the filter on futures past the matching future.
    ''' </summary>
    <Extension()>
    Public Async Function FirstMatchAsync(Of T)(ByVal sequence As IEnumerable(Of T),
                                                ByVal filterFunction As Func(Of T, Task(Of Boolean))) As Task(Of T)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(filterFunction IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Task(Of T))() IsNot Nothing)

        For Each item In sequence
            If Await filterFunction(item) Then
                Return item
            End If
        Next item
        Throw New OperationFailedException("No Matches")
    End Function
End Module
