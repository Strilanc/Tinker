Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.IO.Path
Imports System.Text
Imports System.Net

'''<summary>A smattering of functions and other stuff that hasn't been placed in more reasonable groups yet.</summary>
Public Module Common
#Region "Numbers"
    <Extension()> Public Function ReversedByteOrder(ByVal u As UInteger) As UInteger
        Dim u2 As UInteger
        For i = 0 To 3
            u2 <<= 8
            u2 = u2 Or (u And CUInt(&HFF))
            u >>= 8
        Next i
        Return u2
    End Function
    <Extension()> Public Function ReversedByteOrder(ByVal u As ULong) As ULong
        Dim u2 As ULong
        For i = 0 To 7
            u2 <<= 8
            u2 = u2 Or (u And CULng(&HFF))
            u >>= 8
        Next i
        Return u2
    End Function

    '''<summary>Returns the smallest multiple of n that is not less than i. Formally: min {x in Z | x = 0 (mod n), x >= i}</summary>
    Public Function modCeiling(ByVal i As Integer, ByVal n As Integer) As Integer
        If i Mod n = 0 Then Return i
        If i < 0 Then Return (i \ n) * n
        Return (i \ n + 1) * n
    End Function

    Public Function alignedReadCount(ByVal count As Integer, ByVal numBufferedIn As Integer, ByVal numBufferedOut As Integer, ByVal alignModulo As Integer) As Integer
        Return Math.Max(0, modCeiling(count - numBufferedOut, alignModulo) - numBufferedIn)
    End Function

    <Extension()> Public Function between(Of N As IComparable)(ByVal v1 As N, ByVal v2 As N, ByVal v3 As N) As N
        'recursive sort
        If v1.CompareTo(v2) < 0 Then Return between(v2, v1, v3)
        If v2.CompareTo(v3) < 0 Then Return between(v1, v3, v2)
        'median
        Return v2
    End Function
#End Region

#Region "Strings"
    Public Function padded(ByVal text As String, ByVal min_chars As Integer, Optional ByVal pad_char As Char = " "c) As String
        Contract.Requires(text IsNot Nothing)
        If text.Length < min_chars Then
            text += New String(pad_char, min_chars - text.Length)
        End If
        Return text
    End Function
    Public Function indent(ByVal paragraph As String, Optional ByVal prefix As String = vbTab) As String
        Contract.Requires(paragraph IsNot Nothing)
        Contract.Requires(prefix IsNot Nothing)
        Return prefix + paragraph.Replace(Environment.NewLine, Environment.NewLine + prefix)
    End Function
    <Extension()> Public Function frmt(ByVal format As String, ByVal ParamArray args() As Object) As String
        Contract.Requires(format IsNot Nothing)
        Contract.Requires(args IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return String.Format(format, args)
    End Function
#End Region

#Region "Strings Extra"
    Public Function breakQuotedWords(ByVal text As String) As List(Of String)
        Contract.Requires(text IsNot Nothing)
        Contract.Ensures(Contract.Result(Of List(Of String))() IsNot Nothing)

        Dim quoted_words As New List(Of String)
        If text = "" Then Return quoted_words
        Dim cur_quoted_word As String = Nothing
        For Each word In text.Split(" "c)
            If word = "" Then Continue For
            If cur_quoted_word Is Nothing Then
                If word(0) = """"c Then
                    If word(word.Length - 1) = """"c Then '[start and end of quoted word]
                        Contract.Assume(word.Length >= 2)
                        quoted_words.Add(word.Substring(1, word.Length - 2))
                    Else '[start of quoted word]
                        Contract.Assume(word.Length >= 1)
                        cur_quoted_word = word.Substring(1) + " "
                    End If
                Else '[normal word]
                    quoted_words.Add(word)
                End If
            ElseIf word(word.Length - 1) = """"c Then '[end of quoted word]
                quoted_words.Add(cur_quoted_word + word.Substring(0, word.Length - 1))
                cur_quoted_word = Nothing
            Else '[middle of quoted word]
                cur_quoted_word += word + " "
            End If
        Next word
        Return quoted_words
    End Function
    Public Function mendQuotedWords(ByVal words As IList(Of String), Optional ByVal skip As Integer = 0) As String
        Dim sentence As String = ""
        For Each word As String In words
            If skip > 0 Then skip -= 1 : Continue For
            sentence += """" + word + """ "
        Next word
        Return sentence.Trim()
    End Function
    Public Function DictStrUInt(ByVal text As String, Optional ByVal pair_divider As String = ";"c, Optional ByVal value_divider As String = "="c) As Dictionary(Of String, UInteger)
        Return DictStrT(text, Function(x) UInteger.Parse(x), pair_divider, value_divider)
    End Function
    Public Function DictStrStr(ByVal text As String, Optional ByVal pair_divider As String = ";"c, Optional ByVal value_divider As String = "="c) As Dictionary(Of String, String)
        Return DictStrT(text, Function(x) x, pair_divider, value_divider)
    End Function
    Private Function DictStrT(Of T)(ByVal text As String, ByVal f As Func(Of String, T), Optional ByVal pair_divider As String = ";"c, Optional ByVal value_divider As String = "="c) As Dictionary(Of String, T)
        Dim d As New Dictionary(Of String, T)
        Dim pd = New String() {pair_divider}
        Dim vd = New String() {value_divider}
        For Each pair In text.Split(pd, StringSplitOptions.RemoveEmptyEntries)
            Dim args = pair.Split(vd, StringSplitOptions.None)
            If args.Count < 2 Then Continue For
            d(args(0)) = f(pair.Substring(args(0).Length + 1))
        Next pair
        Return d
    End Function
#End Region

#Region "Enums"
    Public Function EnumValues(Of T)() As IEnumerable(Of T)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of T))() IsNot Nothing)
        Return CType([Enum].GetValues(GetType(T)), IEnumerable(Of T))
    End Function
    Public Function EnumTryParse(Of T)(ByVal value As String, ByVal ignoreCase As Boolean, ByRef ret As T) As Boolean
        For Each e In EnumValues(Of T)()
            If String.Compare(value, e.ToString(), ignoreCase) = 0 Then
                ret = e
                Return True
            End If
        Next e
        Return False
    End Function
    Public Function IsEnumValid(Of T)(ByVal val As T) As Boolean
        Return EnumValues(Of T).Contains(val)
    End Function
#End Region

#Region "Arrays"
    Public Function ArraysEqual(Of T As IComparable(Of T))(ByVal array1() As T, ByVal array2() As T) As Boolean
        Contract.Requires(array1 IsNot Nothing)
        Contract.Requires(array2 IsNot Nothing)
        If array1.Length <> array2.Length Then Return False
        For i = 0 To array1.Length - 1
            If array1(i).CompareTo(array2(i)) <> 0 Then Return False
        Next i
        Return True
    End Function
    <Extension()> Public Function SubArray(Of T)(ByVal array As T(), ByVal offset As Integer) As T()
        Contract.Requires(array IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Ensures(Contract.Result(Of T())() IsNot Nothing)
        If offset > array.Length Then Throw New ArgumentOutOfRangeException("offset")

        Dim new_array(0 To array.Length - offset - 1) As T
        System.Array.Copy(array, offset, new_array, 0, new_array.Length)
        Return new_array
    End Function
    <Extension()> Public Function SubArray(Of T)(ByVal array As T(), ByVal offset As Integer, ByVal length As Integer) As T()
        Contract.Requires(array IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Requires(length >= 0)
        Contract.Ensures(Contract.Result(Of T())() IsNot Nothing)
        If offset + length > array.Length Then Throw New ArgumentOutOfRangeException("offset + length")

        Dim new_array(0 To length - 1) As T
        System.Array.Copy(array, offset, new_array, 0, new_array.Length)
        Return new_array
    End Function
    Public Function Concat(Of T)(ByVal arrays As IEnumerable(Of T())) As T()
        Contract.Requires(arrays IsNot Nothing)
        Contract.Ensures(Contract.Result(Of T())() IsNot Nothing)
        If (From array In arrays Where array Is Nothing).Any Then Throw New ArgumentNullException("array is null", "arrays")

        Dim totalLength = 0
        For Each array In arrays
            totalLength += array.Length
        Next array

        Dim flattenedArray(0 To totalLength - 1) As T
        Dim processingOffset = 0
        For Each array In arrays
            System.Array.Copy(array, 0, flattenedArray, processingOffset, array.Length)
            processingOffset += array.Length
        Next array

        Return flattenedArray
    End Function
#End Region

#Region "Filepaths"
    Public Function getFileNameSlash(ByVal s As String) As String
        Dim ss() As String = s.Split("\"c, "/"c)
        Return ss(ss.Length - 1)
    End Function
    Public Function findFileMatching(ByVal search_pattern As String, ByVal like_pattern As String, ByVal directory As String) As Outcome(Of String)
        Dim out = findFilesMatching(search_pattern, like_pattern, directory, 1)
        If out.val.Count = 0 Then
            Return failure(out.message)
        End If
        Return successVal(out.val(0), "{0} matched {1}".frmt(search_pattern, out.val(0)))
    End Function
    Public Function findFilesMatching(ByVal search_pattern As String, ByVal like_pattern As String, ByVal directory As String, ByVal max_results As Integer) As Outcome(Of List(Of String))
        Dim matches As New List(Of String)

        Try
            'Normalize input
            directory = directory.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar)
            If directory(directory.Length - 1) <> DirectorySeparatorChar Then
                directory += DirectorySeparatorChar
            End If

            'Separate directory and filename patterns
            search_pattern = search_pattern.Replace(AltDirectorySeparatorChar, DirectorySeparatorChar)
            Dim dir_pattern = "*"
            If search_pattern.Contains(DirectorySeparatorChar) Then
                Dim words = search_pattern.Split(DirectorySeparatorChar)
                Dim file_pattern = words(words.Length - 1)
                dir_pattern = search_pattern.Substring(0, search_pattern.Length - file_pattern.Length) + "*"
                search_pattern = "*" + file_pattern
            End If

            'patterns are not case-sensitive
            dir_pattern = dir_pattern.ToLower()
            like_pattern = like_pattern.ToLower

            'Check files in folder
            For Each filename In IO.Directory.GetFiles(directory, search_pattern, IO.SearchOption.AllDirectories)
                filename = filename.Substring(directory.Length)
                If filename.ToLower() Like like_pattern AndAlso filename.ToLower Like dir_pattern Then
                    matches.Add(filename)
                    If matches.Count >= max_results Then Exit For
                End If
            Next filename

            Return successVal(matches, "Matched {0} files.".frmt(matches.Count))
        Catch e As Exception
            Return failureVal(matches, "Matched {0} files before hitting error: {1}.".frmt(matches.Count, e.Message))
        End Try
    End Function
    Public Function setting_war3path() As String
        Return My.Settings.war3path
    End Function
    Public Function setting_mappath() As String
        Return My.Settings.mapPath
    End Function
    Public Function GetDataFolderPath(ByVal sub_folder As String) As String
        Dim folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        Try
            folder += IO.Path.DirectorySeparatorChar + "HostBot"
            If Not IO.Directory.Exists(folder) Then IO.Directory.CreateDirectory(folder)
            folder += IO.Path.DirectorySeparatorChar + sub_folder
            If Not IO.Directory.Exists(folder) Then IO.Directory.CreateDirectory(folder)
            folder += IO.Path.DirectorySeparatorChar
            Return folder
        Catch e As Exception
            Logging.LogUnexpectedException("Error getting folder My Documents\HostBot\{0}.".frmt(sub_folder), e)
            Throw
        End Try
    End Function
#End Region

#Region "Linq"
    '''<summary>Determines if a sequence has no elements.</summary>
    <Extension()>
    Public Function None(Of T)(ByVal sequence As IEnumerable(Of T)) As Boolean
        Return Not sequence.Any()
    End Function

    <Extension()>
    Public Function MaxPair(Of T, C As IComparable)(ByVal sequence As IEnumerable(Of T),
                                                    ByVal transformation As Func(Of T, C),
                                                    ByRef out_element As T,
                                                    ByRef out_transformation As C) As Boolean
        Dim any = False
        Dim max_element = out_element
        Dim max_transformation = out_transformation

        For Each e In sequence
            Dim f = transformation(e)
            If Not any OrElse f.CompareTo(max_transformation) > 0 Then
                max_element = e
                max_transformation = f
            End If
            any = True
        Next e

        If any Then
            out_element = max_element
            out_transformation = max_transformation
        End If
        Return any
    End Function
    <Extension()>
    Public Function Max(Of T)(ByVal sequence As IEnumerable(Of T),
                              ByVal comparator As Func(Of T, T, Integer)) As T
        Dim any = False
        Dim max_element As T = Nothing

        For Each e In sequence
            If Not any OrElse comparator(max_element, e) < 0 Then
                max_element = e
            End If
            any = True
        Next e

        Return max_element
    End Function

    <Extension()>
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
    Public Function ReduceUsing(Of T)(ByVal sequence As IEnumerable(Of T),
                                      ByVal reduction As Func(Of T, T, T),
                                      Optional ByVal initialValue As T = Nothing) As T
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(reduction IsNot Nothing)
        Return ReduceUsing(Of T, T)(sequence, reduction, initialValue)
    End Function

    <Extension()>
    Public Function EnumBlocks(Of T)(ByVal sequence As IEnumerator(Of T),
                                     ByVal blockSize As Integer) As IEnumerator(Of IList(Of T))
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(blockSize > 0)
        Contract.Ensures(Contract.Result(Of IEnumerator(Of IList(Of T)))() IsNot Nothing)
        Return New Enumerator(Of IList(Of T))(
            Function(controller)
                If Not sequence.MoveNext Then  Return controller.Break()

                Dim block = New List(Of T)(blockSize)
                block.Add(sequence.Current())
                While block.Count < blockSize AndAlso sequence.MoveNext
                    block.Add(sequence.Current)
                End While
                Return block
            End Function
        )
    End Function
    <Extension()>
    Public Function EnumBlocks(Of T)(ByVal sequence As IEnumerable(Of T),
                                     ByVal blockSize As Integer) As IEnumerable(Of IList(Of T))
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(blockSize > 0)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of IList(Of T)))() IsNot Nothing)
        Return sequence.Transform(Function(enumerator) EnumBlocks(enumerator, blockSize))
    End Function
    <Extension()>
    Public Function Transform(Of T, D)(ByVal sequence As IEnumerable(Of T),
                                       ByVal transformation As Func(Of IEnumerator(Of T), IEnumerator(Of D))) As IEnumerable(Of D)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(transformation IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of D))() IsNot Nothing)
        Return New Enumerable(Of D)(Function() transformation(sequence.GetEnumerator()))
    End Function
#End Region

    Public Sub Swap(Of T)(ByRef v1 As T, ByRef v2 As T)
        Dim vt = v1
        v1 = v2
        v2 = vt
    End Sub

    <Extension()>
    Public Function ReadBlock(ByVal stream As IO.Stream, ByRef length As Integer) As Byte()
        Dim buffer(0 To length - 1) As Byte
        length = stream.Read(buffer, 0, length)
        Return buffer
    End Function

    <Extension()>
    Public Function CountUpTo(Of T)(ByVal sequence As IEnumerable(Of T), ByVal maxCount As Integer) As Integer
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(maxCount >= 0)
        Contract.Ensures(Contract.Result(Of Integer)() >= 0)
        Contract.Ensures(Contract.Result(Of Integer)() <= maxCount)
        Dim count = 0
        For Each item In sequence
            count += 1
            If count >= maxCount Then Exit For
        Next item
        Return count
    End Function

    Public Function streamBytes(ByVal stream As IO.Stream) As Byte()
        Contract.Requires(stream IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Dim m = 1024
        Dim bb(0 To m - 1) As Byte
        Dim c = 0
        Do
            Dim n = stream.Read(bb, c, m - c)
            c += n
            If c <> m Then Exit Do
            m *= 2
            ReDim Preserve bb(0 To m - 1)
        Loop
        ReDim Preserve bb(0 To c - 1)
        Contract.Assume(bb IsNot Nothing)
        Return bb
    End Function

    <Extension()> Public Function ToList(Of T)(ByVal list As IList(Of T)) As List(Of T)
        Contract.Requires(list IsNot Nothing)
        Contract.Ensures(Contract.Result(Of List(Of T))() IsNot Nothing)
        Dim ret As New List(Of T)(list.Count)
        For i = 0 To list.Count - 1
            ret.Add(list(i))
        Next i
        Return ret
    End Function
    <Extension()> Public Function ToArray(Of T)(ByVal list As IList(Of T)) As T()
        Contract.Requires(list IsNot Nothing)
        Contract.Ensures(Contract.Result(Of T())() IsNot Nothing)
        Dim ret(0 To list.Count - 1) As T
        For i = 0 To list.Count - 1
            ret(i) = list(i)
        Next i
        Return ret
    End Function
    <Extension()> Public Function SubToArray(Of T)(ByVal list As IList(Of T), ByVal offset As Integer, ByVal count As Integer) As T()
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
    <Extension()> Public Function ToView(Of T)(ByVal list As IList(Of T)) As IViewableList(Of T)
        Contract.Requires(list IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IViewableList(Of T))() IsNot Nothing)
        Return New ViewableList(Of T)(list)
    End Function
    <Extension()> Public Function Reverse(Of T)(ByVal list As IList(Of T)) As IList(Of T)
        Contract.Requires(list IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IList(Of T))() IsNot Nothing)
        Dim n = list.Count
        Dim ret(0 To n - 1) As T
        For i = 0 To n - 1
            ret(i) = list(n - i - 1)
        Next i
        Return ret
    End Function

    Private Delegate Function RecursiveFunction(Of A1, R)(ByVal self As RecursiveFunction(Of A1, R)) As Func(Of A1, R)
    Public Function YCombinator(Of A1, R)(ByVal recursor As Func(Of Func(Of A1, R), Func(Of A1, R))) As Func(Of A1, R)
        Contract.Requires(recursor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Func(Of A1, R))() IsNot Nothing)
        Dim rec As RecursiveFunction(Of A1, R) = Function(self) Function(arg1) recursor(self(self))(arg1)
        Return rec(rec)
    End Function
    Private Delegate Function RecursiveAction(ByVal self As RecursiveAction) As Action
    Public Function YCombinator(ByVal recursor As Func(Of Action, Action)) As Action
        Contract.Requires(recursor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Action)() IsNot Nothing)
        Dim rec As RecursiveAction = Function(self) Sub() recursor(self(self))()
        Return rec(rec)
    End Function
    Private Delegate Function RecursiveAction(Of A1)(ByVal self As RecursiveAction(Of A1)) As Action(Of A1)
    Public Function YCombinator(Of A1)(ByVal recursor As Func(Of Action(Of A1), Action(Of A1))) As Action(Of A1)
        Contract.Requires(recursor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Action(Of A1))() IsNot Nothing)
        Dim rec As RecursiveAction(Of A1) = Function(self) Sub(arg1) recursor(self(self))(arg1)
        Return rec(rec)
    End Function
End Module

Public Class ExpensiveValue(Of T)
    Private func As Func(Of T)
    Private val As T
    Private computed As Boolean
    Public Sub New(ByVal func As Func(Of T))
        Me.func = func
    End Sub
    Public Sub New(ByVal val As T)
        Me.val = val
        Me.computed = True
    End Sub
    Public ReadOnly Property Value As T
        Get
            If Not computed Then
                computed = True
                val = func()
            End If
            Return val
        End Get
    End Property
    Public Shared Widening Operator CType(ByVal func As Func(Of T)) As ExpensiveValue(Of T)
        Return New ExpensiveValue(Of T)(func)
    End Operator
    Public Shared Widening Operator CType(ByVal val As T) As ExpensiveValue(Of T)
        Return New ExpensiveValue(Of T)(val)
    End Operator
End Class

Public Class KeyPair
    Private ReadOnly _value1 As Byte()
    Private ReadOnly _value2 As Byte()
    Public ReadOnly Property Value1 As Byte()
        Get
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
            Return _value1
        End Get
    End Property
    Public ReadOnly Property Value2 As Byte()
        Get
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
            Return _value2
        End Get
    End Property

    <ContractInvariantMethod()> Protected Sub Invariant()
        Contract.Invariant(_value1 IsNot Nothing)
        Contract.Invariant(_value2 IsNot Nothing)
    End Sub

    Public Sub New(ByVal value1 As Byte(), ByVal value2 As Byte())
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Me._value1 = value1
        Me._value2 = value2
    End Sub
End Class

Public Class LazyList(Of T)
    Private ReadOnly _value As T
    Private ReadOnly _next As Func(Of LazyList(Of T))
    Public Sub New(ByVal value As T, ByVal [next] As Func(Of LazyList(Of T)))
        Me._value = value
        Me._next = If([next], Function() Nothing)
    End Sub
    Public ReadOnly Property Value As T
        Get
            Return _value
        End Get
    End Property
    Public ReadOnly Property [Next] As LazyList(Of T)
        Get
            Return _next()
        End Get
    End Property

    Public ReadOnly Property Nodes() As IEnumerable(Of LazyList(Of T))
        Get
            Return New Enumerable(Of LazyList(Of T))(
                Function()
                    Dim cur = Me
                    Return New Enumerator(Of LazyList(Of T))(
                        Function(controller)
                            cur = cur.Next
                            Return If(cur, controller.Break())
                        End Function
                    )
                End Function
            )
        End Get
    End Property
    Public ReadOnly Property Values() As IEnumerable(Of T)
        Get
            Return From node In Nodes() Select node.Value
        End Get
    End Property
End Class
