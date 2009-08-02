Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.IO.Path
Imports System.Text
Imports System.Net

'''<summary>A smattering of functions and other stuff that hasn't been placed in more reasonable groups yet.</summary>
Public Module Common
#Region "Numbers"
    <Extension()>
    <Pure()>
    Public Function ReversedByteOrder(ByVal u As UInteger) As UInteger
        Dim u2 As UInteger
        For i = 0 To 3
            u2 <<= 8
            u2 = u2 Or (u And CUInt(&HFF))
            u >>= 8
        Next i
        Return u2
    End Function
    <Extension()>
    <Pure()>
    Public Function ReversedByteOrder(ByVal u As ULong) As ULong
        Dim u2 As ULong
        For i = 0 To 7
            u2 <<= 8
            u2 = u2 Or (u And CULng(&HFF))
            u >>= 8
        Next i
        Return u2
    End Function

    '''<summary>Returns the smallest multiple of n that is not less than i. Formally: min {x in Z | x = 0 (mod n), x >= i}</summary>
    <Pure()>
    Public Function ModCeiling(ByVal i As Integer, ByVal n As Integer) As Integer
        Contract.Requires(n > 0)
        If i Mod n = 0 Then Return i
        Dim m = (i \ n) * n
        If i < 0 Then Return m
        If m > Integer.MaxValue - n Then
            Throw New InvalidOperationException("The result will not fit into an Int32.")
        End If
        Return m + n
    End Function

    <Pure()>
    <Extension()>
    Public Function Between(Of N As IComparable(Of N))(ByVal v1 As N,
                                                       ByVal v2 As N,
                                                       ByVal v3 As N) As N
        Contract.Requires(v1.IsNotNullReferenceGeneric())
        Contract.Requires(v2.IsNotNullReferenceGeneric())
        Contract.Requires(v3.IsNotNullReferenceGeneric())
        Contract.Ensures(Contract.Result(Of N).IsNotNullReferenceGeneric)
        'recursive sort
        If v2.CompareTo(v1) > 0 Then Return Between(v2, v1, v3)
        If v2.CompareTo(v3) < 0 Then Return Between(v1, v3, v2)
        'median
        Return v2
    End Function
#End Region

#Region "Strings"
    <Pure()>
    <Extension()>
    Public Function Padded(ByVal text As String,
                           ByVal minChars As Integer,
                           Optional ByVal paddingChar As Char = " "c) As String
        Contract.Requires(text IsNot Nothing)
        Contract.Requires(minChars >= 0)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        If text.Length < minChars Then
            text += New String(paddingChar, minChars - text.Length)
        End If
        Return text
    End Function
    <Pure()>
    Public Function indent(ByVal paragraph As String,
                           Optional ByVal prefix As String = vbTab) As String
        Contract.Requires(paragraph IsNot Nothing)
        Contract.Requires(prefix IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return prefix + paragraph.Replace(Environment.NewLine, Environment.NewLine + prefix)
    End Function
    <Pure()>
    <Extension()>
    Public Function frmt(ByVal format As String, ByVal ParamArray args() As Object) As String
        Contract.Requires(format IsNot Nothing)
        Contract.Requires(args IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return String.Format(format, args)
    End Function
#End Region

#Region "Strings Extra"
    <Pure()>
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
    <Pure()>
    Public Function DictStrT(Of T)(ByVal text As String,
                                   ByVal parser As Func(Of String, T),
                                   Optional ByVal pairDivider As String = ";"c,
                                   Optional ByVal valueDivider As String = "="c) As Dictionary(Of String, T)
        Contract.Requires(text IsNot Nothing)
        Contract.Requires(parser IsNot Nothing)
        Contract.Requires(pairDivider IsNot Nothing)
        Contract.Requires(valueDivider IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Dictionary(Of String, T))() IsNot Nothing)
        Dim d As New Dictionary(Of String, T)
        Dim pd = New String() {pairDivider}
        Dim vd = New String() {valueDivider}
        For Each pair In text.Split(pd, StringSplitOptions.RemoveEmptyEntries)
            Dim args = pair.Split(vd, StringSplitOptions.None)
            If args.Count < 2 Then Continue For
            d(args(0)) = parser(pair.Substring(args(0).Length + 1))
        Next pair
        Return d
    End Function
#End Region

#Region "Enums"
    <Pure()>
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
    <Pure()>
    Public Function IsEnumValid(Of T)(ByVal val As T) As Boolean
        Return EnumValues(Of T).Contains(val)
    End Function
#End Region

#Region "Arrays"
    <Pure()>
    Public Function ArraysEqual(Of T As IComparable(Of T))(ByVal array1() As T, ByVal array2() As T) As Boolean
        Contract.Requires(array1 IsNot Nothing)
        Contract.Requires(array2 IsNot Nothing)
        If array1.Length <> array2.Length Then Return False
        For i = 0 To array1.Length - 1
            If array1(i).CompareTo(array2(i)) <> 0 Then Return False
        Next i
        Return True
    End Function
    <Extension()>
    <Pure()>
    Public Function SubArray(Of T)(ByVal array As T(), ByVal offset As Integer) As T()
        Contract.Requires(array IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Ensures(Contract.Result(Of T())() IsNot Nothing)
        If offset > array.Length Then Throw New ArgumentOutOfRangeException("offset")

        Dim new_array(0 To array.Length - offset - 1) As T
        System.Array.Copy(array, offset, new_array, 0, new_array.Length)
        Return new_array
    End Function
    <Extension()>
    <Pure()>
    Public Function SubArray(Of T)(ByVal array As T(), ByVal offset As Integer, ByVal length As Integer) As T()
        Contract.Requires(array IsNot Nothing)
        Contract.Requires(offset >= 0)
        Contract.Requires(length >= 0)
        Contract.Ensures(Contract.Result(Of T())() IsNot Nothing)
        If offset + length > array.Length Then Throw New ArgumentOutOfRangeException("offset + length")

        Dim new_array(0 To length - 1) As T
        System.Array.Copy(array, offset, new_array, 0, new_array.Length)
        Return new_array
    End Function
    <Pure()>
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
    <Pure()>
    Public Function GetFileNameSlash(ByVal path As String) As String
        Contract.Requires(path IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Dim words = path.Split("\"c, "/"c)
        Return words(words.Length - 1)
    End Function
    Public Function findFileMatching(ByVal fileQuery As String, ByVal likeQuery As String, ByVal directory As String) As Outcome(Of String)
        Contract.Requires(fileQuery IsNot Nothing)
        Contract.Requires(likeQuery IsNot Nothing)
        Contract.Requires(directory IsNot Nothing)
        Dim out = findFilesMatching(fileQuery, likeQuery, directory, 1)
        If out.val.Count = 0 Then
            Return failure(out.Message)
        End If
        Return successVal(out.val(0), "{0} matched {1}".frmt(fileQuery, out.val(0)))
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
            Return failureVal(matches, "Matched {0} files before hitting error: {1}.".frmt(matches.Count, e.ToString))
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

    Public Sub Swap(Of T)(ByRef v1 As T, ByRef v2 As T)
        Dim vt = v1
        v1 = v2
        v2 = vt
    End Sub

    Public Sub RunWithDebugTrap(ByVal action As Action, ByVal context As String)
        Contract.Requires(action IsNot Nothing)
        Contract.Requires(context IsNot Nothing)

        If My.Settings.debugMode Then
            Call action()
        Else
            Try
                Call action()
            Catch e As Exception
                LogUnexpectedException("{0} threw an unhandled exception.".frmt(context), e)
            End Try
        End If
    End Sub

    <Extension()>
    Public Function ReadBlock(ByVal stream As IO.Stream, ByRef length As Integer) As Byte()
        Dim buffer(0 To length - 1) As Byte
        length = stream.Read(buffer, 0, length)
        Return buffer
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

    <Extension()>
    <Pure()>
    Public Function Minutes(ByVal quantity As Integer) As TimeSpan
        Return New TimeSpan(0, quantity, 0)
    End Function
    <Extension()>
    <Pure()>
    Public Function Seconds(ByVal quantity As Integer) As TimeSpan
        Return New TimeSpan(0, 0, quantity)
    End Function
    <Extension()>
    <Pure()>
    Public Function MilliSeconds(ByVal quantity As Integer) As TimeSpan
        Return New TimeSpan(0, 0, 0, 0, quantity)
    End Function
    <Extension()>
    <Pure()>
    Public Function ToView(Of T)(ByVal list As IList(Of T)) As ViewableList(Of T)
        Contract.Requires(list IsNot Nothing)
        Contract.Ensures(Contract.Result(Of ViewableList(Of T))() IsNot Nothing)
        Return New ViewableList(Of T)(list)
    End Function

    Private Delegate Function RecursiveFunction(Of A1, R)(ByVal self As RecursiveFunction(Of A1, R)) As Func(Of A1, R)
    <Pure()>
    Public Function YCombinator(Of A1, R)(ByVal recursor As Func(Of Func(Of A1, R), Func(Of A1, R))) As Func(Of A1, R)
        Contract.Requires(recursor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Func(Of A1, R))() IsNot Nothing)
        Dim recursor_ = recursor 'avoids hoisted argument contract verification flaw
        Dim rec As RecursiveFunction(Of A1, R) = Function(self) Function(arg1) recursor_(self(self))(arg1)
        Dim ret = rec(rec)
        Contract.Assume(ret IsNot Nothing)
        Return ret
    End Function
    Private Delegate Function RecursiveAction(ByVal self As RecursiveAction) As Action
    <Pure()>
    Public Function YCombinator(ByVal recursor As Func(Of Action, Action)) As Action
        Contract.Requires(recursor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Action)() IsNot Nothing)
        Dim recursor_ = recursor 'avoids hoisted argument contract verification flaw
        Dim rec As RecursiveAction = Function(self) Sub() recursor_(self(self))()
        Dim ret = rec(rec)
        Contract.Assume(ret IsNot Nothing)
        Return ret
    End Function
    Private Delegate Function RecursiveAction(Of A1)(ByVal self As RecursiveAction(Of A1)) As Action(Of A1)
    <Pure()>
    Public Function YCombinator(Of A1)(ByVal recursor As Func(Of Action(Of A1), Action(Of A1))) As Action(Of A1)
        Contract.Requires(recursor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of Action(Of A1))() IsNot Nothing)
        Dim recursor_ = recursor 'avoids hoisted argument contract verification flaw
        Dim rec As RecursiveAction(Of A1) = Function(self) Sub(arg1) recursor_(self(self))(arg1)
        Dim ret = rec(rec)
        Contract.Assume(ret IsNot Nothing)
        Return ret
    End Function

    Public Sub FutureIterate(Of T)(ByVal producer As Func(Of IFuture(Of T)),
                                   ByVal consumer As Func(Of T, IFuture(Of Boolean)))
        producer().CallWhenValueReady(YCombinator(Of T)(
            Function(self) Sub(result)
                               consumer(result).CallWhenValueReady(
                                   Sub([continue])
                                       If [continue] Then
                                           producer().CallWhenValueReady(self)
                                       End If
                                   End Sub)
                           End Sub))
    End Sub

    '''<summary>Returns true if T is a class type and arg is nothing.</summary>
    <Extension()>
    <Pure()>
    Public Function IsNullReferenceGeneric(Of T)(ByVal arg As T) As Boolean
        Contract.Ensures(Contract.Result(Of Boolean)() = Object.ReferenceEquals(arg, Nothing))
        Return Object.ReferenceEquals(arg, Nothing)
    End Function
    '''<summary>Returns true if T is a structure type or arg is not nothing.</summary>
    <Extension()>
    <Pure()>
    Public Function IsNotNullReferenceGeneric(Of T)(ByVal arg As T) As Boolean
        Contract.Ensures(Contract.Result(Of Boolean)() = Not Object.ReferenceEquals(arg, Nothing))
        Return Not Object.ReferenceEquals(arg, Nothing)
    End Function

    <Extension()>
    Public Function FutureRead(ByVal stream As IO.Stream,
                               ByVal buffer() As Byte,
                               ByVal offset As Integer,
                               ByVal count As Integer) As IFuture(Of PossibleException(Of Integer, Exception))
        Contract.Requires(stream IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of PossibleException(Of Integer, Exception)))() IsNot Nothing)
        Dim stream_ = stream
        Dim f = New Future(Of PossibleException(Of Integer, Exception))
        Try
            stream.BeginRead(buffer, offset, count, Sub(ar)
                                                        Try
                                                            f.SetValue(stream_.EndRead(ar))
                                                        Catch e As Exception
                                                            f.SetValue(e)
                                                        End Try
                                                    End Sub, Nothing)
        Catch e As Exception
            f.SetValue(e)
        End Try
        Return f
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
