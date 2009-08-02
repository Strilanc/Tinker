Public Module ExtensionsToLinq
    '''<summary>Determines if a sequence has no elements.</summary>
    <Extension()>
    <Pure()>
    Public Function None(Of T)(ByVal sequence As IEnumerable(Of T)) As Boolean
        Contract.Requires(sequence IsNot Nothing)
        Return Not sequence.Any()
    End Function

    <Pure()>
    <Extension()>
    Public Function MaxPair(Of T, C As IComparable)(ByVal sequence As IEnumerable(Of T),
                                                    ByVal transformation As Func(Of T, C),
                                                    ByRef out_element As T,
                                                    ByRef out_transformation As C) As Boolean
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(transformation IsNot Nothing)
        Dim any = False
        Dim maxElement = out_element
        Dim maxImage = out_transformation

        For Each e In sequence
            Dim f = transformation(e)
            If Not any OrElse f.CompareTo(maxImage) > 0 Then
                maxElement = e
                maxImage = f
            End If
            any = True
        Next e

        If any Then
            out_element = maxElement
            out_transformation = maxImage
        End If
        Return any
    End Function

    <Extension()>
    <Pure()>
    Public Function Max(Of T)(ByVal sequence As IEnumerable(Of T),
                              ByVal comparator As Func(Of T, T, Integer)) As T
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(comparator IsNot Nothing)
        Dim any = False
        Dim maxElement As T = Nothing

        For Each e In sequence
            If Not any OrElse comparator(maxElement, e) < 0 Then
                maxElement = e
            End If
            any = True
        Next e

        Return maxElement
    End Function

    <Extension()>
    <Pure()>
    Public Function ReduceUsing(Of TSource, TResult)(ByVal sequence As IEnumerable(Of TSource),
                                                     ByVal reduction As Func(Of TResult, TSource, TResult),
                                                     Optional ByVal initialValue As TResult = Nothing) As TResult
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(reduction IsNot Nothing)
        Dim accumulator = initialValue
        For Each item In sequence
            accumulator = reduction(accumulator, item)
        Next item
        Return accumulator
    End Function

    <Extension()>
    <Pure()>
    Public Function ReduceUsing(Of T)(ByVal sequence As IEnumerable(Of T),
                                      ByVal reduction As Func(Of T, T, T),
                                      Optional ByVal initialValue As T = Nothing) As T
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(reduction IsNot Nothing)
        Return ReduceUsing(Of T, T)(sequence, reduction, initialValue)
    End Function

    <Extension()>
    <Pure()>
    Public Function EnumBlocks(Of T)(ByVal sequence As IEnumerator(Of T),
                                     ByVal blockSize As Integer) As IEnumerator(Of IList(Of T))
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(blockSize > 0)
        Contract.Ensures(Contract.Result(Of IEnumerator(Of IList(Of T)))() IsNot Nothing)
        Dim sequence_ = sequence
        Dim blockSize_ = blockSize
        Return New Enumerator(Of IList(Of T))(
            Function(controller)
                If Not sequence_.MoveNext Then  Return controller.Break()

                Dim block = New List(Of T)(blockSize_)
                block.Add(sequence_.Current())
                While block.Count < blockSize_ AndAlso sequence_.MoveNext
                    block.Add(sequence_.Current)
                End While
                Return block
            End Function
        )
    End Function
    <Extension()>
    <Pure()>
    Public Function EnumBlocks(Of T)(ByVal sequence As IEnumerable(Of T),
                                     ByVal blockSize As Integer) As IEnumerable(Of IList(Of T))
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(blockSize > 0)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of IList(Of T)))() IsNot Nothing)
        Dim blockSize_ = blockSize
        Return sequence.Transform(Function(enumerator) EnumBlocks(enumerator, blockSize_))
    End Function
    <Extension()>
    <Pure()>
    Public Function Transform(Of T, D)(ByVal sequence As IEnumerable(Of T),
                                       ByVal transformation As Func(Of IEnumerator(Of T), IEnumerator(Of D))) As IEnumerable(Of D)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(transformation IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of D))() IsNot Nothing)
        Dim sequence_ = sequence
        Dim transformation_ = transformation
        Return New Enumerable(Of D)(Function() transformation_(sequence_.GetEnumerator()))
    End Function

    <Extension()>
    <Pure()>
    Public Function CountUpTo(Of T)(ByVal sequence As IEnumerable(Of T), ByVal maxCount As Integer) As Integer
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(maxCount >= 0)
        Contract.Ensures(Contract.Result(Of Integer)() >= 0)
        Contract.Ensures(Contract.Result(Of Integer)() <= maxCount)
        If maxCount = 0 Then Return 0
        Dim count = 0
        For Each item In sequence
            count += 1
            If count >= maxCount Then Exit For
        Next item
        Return count
    End Function

    <Extension()>
    <Pure()>
    Public Function ToList(Of T)(ByVal list As IList(Of T)) As List(Of T)
        Contract.Requires(list IsNot Nothing)
        Contract.Ensures(Contract.Result(Of List(Of T))() IsNot Nothing)
        Dim ret As New List(Of T)(list.Count)
        For i = 0 To list.Count - 1
            ret.Add(list(i))
        Next i
        Return ret
    End Function
    <Extension()>
    <Pure()>
    Public Function ToArray(Of T)(ByVal list As IList(Of T)) As T()
        Contract.Requires(list IsNot Nothing)
        Contract.Ensures(Contract.Result(Of T())() IsNot Nothing)
        Dim ret(0 To list.Count - 1) As T
        For i = 0 To list.Count - 1
            ret(i) = list(i)
        Next i
        Return ret
    End Function
    <Extension()>
    <Pure()>
    Public Function SubToArray(Of T)(ByVal list As IList(Of T), ByVal offset As Integer) As T()
        Contract.Requires(list IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Ensures(Contract.Result(Of T())() IsNot Nothing)
        Return SubToArray(list, offset, list.Count - offset)
    End Function
    <Extension()>
    <Pure()>
    Public Function SubToArray(Of T)(ByVal list As IList(Of T), ByVal offset As Integer, ByVal count As Integer) As T()
        Contract.Requires(list IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Requires(count >= 0)
        Contract.Ensures(Contract.Result(Of T())() IsNot Nothing)
        If offset + count > list.Count Then Throw New ArgumentOutOfRangeException("count")
        Dim ret(0 To count - 1) As T
        For i = 0 To count - 1
            ret(i) = list(i + offset)
        Next i
        Return ret
    End Function

    <Extension()>
    <Pure()>
    Public Function Reverse(Of T)(ByVal list As IList(Of T)) As IList(Of T)
        Contract.Requires(list IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of T))() IsNot Nothing)
        Dim n = list.Count
        Dim ret(0 To n - 1) As T
        For i = 0 To n - 1
            ret(i) = list(n - i - 1)
        Next i
        Return ret
    End Function
End Module