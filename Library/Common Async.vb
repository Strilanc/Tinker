Public Module FutureExtensionsEx
    <Extension()>
    Public Async Function ReadExactAsync(ByVal stream As IO.Stream, ByVal size As Integer) As Task(Of Byte())
        Contract.Requires(size >= 0)
        Contract.Ensures(Contract.Result(Of Task(Of Byte()))() IsNot Nothing)

        Dim result = Await ReadBestEffortAsync(stream, size)
        If result.Length = 0 Then Throw New IO.IOException("End of stream.")
        If result.Length < size Then Throw New IO.IOException("End of stream (fragment)")
        Return result
    End Function

    <Extension()>
    Public Async Function ReadBestEffortAsync(ByVal stream As IO.Stream, ByVal maxSize As Integer) As Task(Of Byte())
        Contract.Requires(maxSize >= 0)
        Contract.Ensures(Contract.Result(Of Task(Of Byte()))() IsNot Nothing)

        Dim totalRead = 0
        Dim result(0 To maxSize - 1) As Byte
        While totalRead < maxSize
            Dim numRead = Await stream.ReadAsync(result, totalRead, maxSize - totalRead)
            If numRead <= 0 Then Exit While
            totalRead += numRead
        End While
        If totalRead <> maxSize Then ReDim Preserve result(0 To totalRead - 1)
        Return result
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
