Imports System.Net.NetworkInformation
Imports System.Runtime.CompilerServices
Imports System.IO.Path
Imports System.Text
Imports System.Net
Imports System.IO

'''<summary>A smattering of functions and other stuff that hasn't been placed in more reasonable groups yet.</summary>
Public Module Common
#Region "Strings Extra"
    <Extension()> <Pure()>
    Public Function Linefy(ByVal s As String) As String
        Contract.Requires(s IsNot Nothing)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        Return s.Replace("\n", Environment.NewLine)
    End Function
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

#Region "Filepaths"
    Public Function findFileMatching(ByVal fileQuery As String, ByVal likeQuery As String, ByVal directory As String) As Outcome(Of String)
        Contract.Requires(fileQuery IsNot Nothing)
        Contract.Requires(likeQuery IsNot Nothing)
        Contract.Requires(directory IsNot Nothing)
        Dim out = findFilesMatching(fileQuery, likeQuery, directory, 1)
        If out.Value.Count = 0 Then
            Return Failure(out.Message)
        End If
        Return Success(out.Value(0), "{0} matched {1}".Frmt(fileQuery, out.Value(0)))
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

            Return Success(matches, "Matched {0} files.".frmt(matches.Count))
        Catch e As Exception
            Return Failure(matches, "Matched {0} files before hitting error: {1}.".Frmt(matches.Count, e.ToString))
        End Try
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
            LogUnexpectedException("Error getting folder My Documents\HostBot\{0}.".frmt(sub_folder), e)
            Throw
        End Try
    End Function
#End Region

    Public Function EnumSizePrefixedBlocks(ByVal data As ViewableList(Of Byte),
                                           Optional ByVal wordSize As Integer = 2,
                                           Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian) As IEnumerable(Of ViewableList(Of Byte))
        Return New Enumerable(Of ViewableList(Of Byte))(
            Function()
                Dim offset = 0
                Return New Enumerator(Of ViewableList(Of Byte))(
                    Function(controller)
                        If offset >= data.Length Then  Return controller.Break
                        Dim r = CInt(data.SubView(0, wordSize).ToUInt32(byteOrder))
                        If r < wordSize Then  Throw New IO.IOException("Invalid data.")
                        If offset + r > data.Length Then  Throw New IO.IOException("Invalid data.")
                        offset += r
                        r -= wordSize
                        Return data.SubView(offset - r, r)
                    End Function)
            End Function)
    End Function

    <Extension()>
    Public Sub WriteNullTerminatedString(ByVal bw As BinaryWriter, ByVal data As String)
        bw.Write(data.ToAscBytes(True))
    End Sub
    <Extension()>
    Public Function ReadNullTerminatedString(ByVal br As BinaryReader,
                                             ByVal maxLength As Integer) As String
        Dim result As String = Nothing
        If Not TryReadNullTerminatedString(br, maxLength, result) Then
            Throw New IOException("Null-terminated string exceeded maximum length.")
        End If
        Return result
    End Function
    <Extension()>
    Public Function TryReadNullTerminatedString(ByVal br As BinaryReader,
                                                ByVal maxLength As Integer,
                                                ByRef result As String) As Boolean
        Dim data(0 To maxLength - 1) As Byte
        Dim n = 0
        Do
            Dim b = br.ReadByte()
            If b = 0 Then
                result = data.Take(n).ParseChrString(False)
                Return True
            End If
            If n >= maxLength Then Return False

            data(n) = b
            n += 1
        Loop
    End Function









    '''<summary>Returns a future sequence for the outcomes of applying a futurizing function to a sequence.</summary>
    <Extension()>
    Public Function FutureMap(Of TDomain, TImage)(ByVal sequence As IEnumerable(Of TDomain),
                                                  ByVal mappingFunction As Func(Of TDomain, IFuture(Of TImage))) As IFuture(Of IEnumerable(Of TImage))
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(mappingFunction IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of IEnumerable(Of TImage)))() IsNot Nothing)

        Dim mappingFunction_ = mappingFunction
        Dim futureVals = (From item In sequence Select mappingFunction_(item)).ToList()
        Return FutureCompress(futureVals).EvalWhenReady(
                               Function() From item In futureVals Select item.Value)
    End Function

    '''<summary>Returns a future for the first value which is not filtered out of the sequence.</summary>
    <Extension()>
    Public Function FutureSelect(Of T)(ByVal sequence As IEnumerable(Of T),
                                       ByVal filterFunction As Func(Of T, IFuture(Of Boolean))) As IFuture(Of Outcome(Of T))
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(filterFunction IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of Outcome(Of T)))() IsNot Nothing)

        Dim enumerator = sequence.GetEnumerator
        If Not enumerator.MoveNext Then Return Failure(Of T)("No matches").Futurize
        Dim f = filterFunction(enumerator.Current)
        Contract.Assume(f IsNot Nothing)
        Dim filterFunction_ = filterFunction
        Return f.EvalWhenValueReady(YCombinator(Of Boolean, IFuture(Of Outcome(Of T)))(
            Function(self) Function(accept)
                               Do
                                   If accept Then  Return Success(enumerator.Current, "Matched").Futurize
                                   If Not enumerator.MoveNext Then  Return Failure(Of T)("No matches").Futurize
                                   Dim futureAccept = filterFunction_(enumerator.Current)
                                   Contract.Assume(futureAccept IsNot Nothing)
                                   If futureAccept.State <> FutureState.Ready Then  Return futureAccept.EvalWhenValueReady(self).Defuturize()
                                   accept = futureAccept.Value
                               Loop
                           End Function)).Defuturize()
    End Function

    '''<summary>Returns a future sequence for the values accepted by the filter.</summary>
    <Extension()>
    Public Function FutureFilter(Of T)(ByVal sequence As IEnumerable(Of T),
                                       ByVal filterFunction As Func(Of T, IFuture(Of Boolean))) As IFuture(Of IEnumerable(Of T))
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(filterFunction IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of IEnumerable(Of T)))() IsNot Nothing)

        Dim pairs = (From item In sequence Select value = item, futureIncluded = filterFunction(item)).ToList()
        Return FutureCompress(From item In pairs Select item.futureIncluded).EvalWhenReady(
                           Function() From item In pairs Where item.futureIncluded.Value Select item.value)
    End Function

    <Extension()>
    Public Function FutureAggregate(Of T, V)(ByVal sequence As IEnumerable(Of T),
                                             ByVal aggregator As Func(Of V, T, IFuture(Of V)),
                                             Optional ByVal initialValue As V = Nothing) As IFuture(Of V)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(aggregator IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of V))() IsNot Nothing)

        Dim enumerator = sequence.GetEnumerator()
        Dim aggregator_ = aggregator
        Return YCombinator(Of V, IFuture(Of V))(
            Function(self) Function(current)
                               If Not enumerator.MoveNext Then  Return current.Futurize
                               Return aggregator_(current, enumerator.Current).EvalWhenValueReady(self).Defuturize
                           End Function)(initialValue)
    End Function

    '''<summary>Returns a future for the value obtained by recursively reducing the sequence.</summary>
    <Extension()>
    Public Function FutureReduce(Of T)(ByVal sequence As IEnumerable(Of T),
                                       ByVal reductionFunction As Func(Of T, T, IFuture(Of T)),
                                       Optional ByVal defaultValue As T = Nothing) As IFuture(Of T)
        Contract.Requires(sequence IsNot Nothing)
        Contract.Requires(reductionFunction IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of T))() IsNot Nothing)

        Dim reductionFunction_ = reductionFunction
        Select Case sequence.CountUpTo(2)
            Case 0
                Return defaultValue.Futurize
            Case 1
                Return sequence.First.Futurize
            Case Else
                Dim futurePartialReduction = sequence.EnumBlocks(2).FutureMap(
                    Function(block)
                        If block.Count = 1 Then  Return block(0).Futurize
                        Return reductionFunction_(block(0), block(1))
                    End Function
                )

                Return futurePartialReduction.EvalWhenValueReady(
                            Function(partialReduction) partialReduction.FutureReduce(reductionFunction_)).Defuturize
        End Select
    End Function
End Module

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

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
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
