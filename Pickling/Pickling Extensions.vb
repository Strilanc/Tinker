Namespace Pickling
    Public Module PicklingExtensions
        <Extension()> <Pure()>
        <ContractVerification(False)>
        Public Function PackPickle(Of T, TValue As T)(ByVal jar As IJar(Of T), ByVal value As TValue) As IPickle(Of TValue)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of TValue))() IsNot Nothing)
            Return New Pickle(Of TValue)(jar, value, jar.Pack(value).ToReadableList)
        End Function
        <Extension()> <Pure()>
        Public Function PackPickle(Of T)(ByVal jar As ISimpleJar, ByVal value As T) As IPickle(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            Return New Pickle(Of T)(jar, value, jar.Pack(value).ToReadableList)
        End Function
        <Extension()> <Pure()>
        Public Function ParsePickle(Of T)(ByVal jar As IJar(Of T), ByVal data As IRist(Of Byte)) As IPickle(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            Dim parsed = jar.Parse(data)
            Return New Pickle(Of T)(jar, parsed.Value, data.SubView(0, parsed.UsedDataCount))
        End Function
        <Extension()> <Pure()>
        Public Function ParsePickle(ByVal jar As ISimpleJar, ByVal data As IRist(Of Byte)) As ISimplePickle
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ISimplePickle)() IsNot Nothing)
            Dim parsed = jar.Parse(data)
            Return New Pickle(Of Object)(jar, parsed.Value, data.SubView(0, parsed.UsedDataCount))
        End Function
        <Extension()> <Pure()>
        Public Function ParsedWithDataCount(Of T)(ByVal value As T, ByVal usedDataCount As Int32) As ParsedValue(Of T)
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(usedDataCount >= 0)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T))().UsedDataCount = usedDataCount)
            Return New ParsedValue(Of T)(value, usedDataCount)
        End Function
        <Extension()> <Pure()>
        Public Function WithValue(Of T1, T2)(ByVal parsedValue As ParsedValue(Of T1), ByVal value As T2) As ParsedValue(Of T2)
            Contract.Requires(parsedValue IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T2))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T2))().UsedDataCount = parsedValue.UsedDataCount)
            Return New ParsedValue(Of T2)(value, parsedValue.UsedDataCount)
        End Function

        <Extension()> <Pure()>
        Public Function MakeListDescription(ByVal descriptions As IEnumerable(Of String),
                                            Optional ByVal useSingleLineDescription As Boolean = False) As String
            Contract.Requires(descriptions IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            If useSingleLineDescription Then
                Return descriptions.StringJoin("; ")
            Else
                If descriptions.None Then Return "{}"
                Return {"{", descriptions.StringJoin(Environment.NewLine).Indent(New String(" "c, 4)), "}"}.StringJoin(Environment.NewLine)
            End If
        End Function
        <Extension()> <Pure()>
        Public Function SplitListDescription(ByVal text As String, Optional ByVal usedSingleLineDescription As Boolean = False) As IEnumerable(Of String)
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of String))() IsNot Nothing)
            If usedSingleLineDescription Then
                Return text.Split({"; "}, StringSplitOptions.None)
            Else
                Dim lines = From line In text.Split({Environment.NewLine}, StringSplitOptions.None).
                            Skip(1).
                            SkipLast(1)
                            Select line.Substring(4)
                If lines.None Then Return lines
                Dim result = New List(Of String)
                Dim acc As String = Nothing
                For Each line In lines
                    Contract.Assume(line IsNot Nothing)
                    If acc Is Nothing Then
                        acc = line
                    ElseIf line = "}" OrElse line.StartsWith(" "c, StringComparison.Ordinal) Then
                        acc += Environment.NewLine + line
                    Else
                        result.Add(acc)
                        acc = line
                    End If
                Next line
                If acc IsNot Nothing Then result.Add(acc)
                Return result
            End If
        End Function

        <Extension()> <Pure()>
        Public Function Description(ByVal pickle As ISimplePickle) As String
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return pickle.Jar.Describe(pickle.Value)
        End Function
        <Extension()> <Pure()>
        Public Function Description(Of T)(ByVal pickle As IPickle(Of T)) As String
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return pickle.Jar.Describe(pickle.Value)
        End Function

        '''<summary>Frames a jar so that it uses a fixed amount of data.</summary>
        <Extension()> <Pure()>
        Public Function Fixed(Of T)(ByVal jar As IJar(Of T),
                                    ByVal exactDataCount As Integer) As FixedSizeFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(exactDataCount >= 0)
            Contract.Ensures(Contract.Result(Of FixedSizeFramingJar(Of T))() IsNot Nothing)
            Return New FixedSizeFramingJar(Of T)(subJar:=jar, dataSize:=exactDataCount)
        End Function
        '''<summary>Frames a jar so that it uses a limited amount of data.</summary>
        <Extension()> <Pure()>
        Public Function Limited(Of T)(ByVal jar As IJar(Of T),
                                      ByVal maxDataCount As Integer) As LimitedSizeFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(maxDataCount >= 0)
            Contract.Ensures(Contract.Result(Of LimitedSizeFramingJar(Of T))() IsNot Nothing)
            Return New LimitedSizeFramingJar(Of T)(subJar:=jar, maxDataCount:=maxDataCount)
        End Function
        '''<summary>Frames a jar so that it uses data terminated by a zero value.</summary>
        <Extension()> <Pure()>
        Public Function NullTerminated(Of T)(ByVal jar As IJar(Of T)) As NullTerminatedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of NullTerminatedFramingJar(Of T))() IsNot Nothing)
            Return New NullTerminatedFramingJar(Of T)(subJar:=jar)
        End Function
        '''<summary>Frames a jar so that it is repeatedly applied until there is no more data.</summary>
        <Extension()> <Pure()>
        Public Function Repeated(Of T)(ByVal jar As IJar(Of T),
                                       Optional ByVal useSingleLineDescription As Boolean = False) As RepeatedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of RepeatedFramingJar(Of T))() IsNot Nothing)
            Return New RepeatedFramingJar(Of T)(subJar:=jar, useSingleLineDescription:=useSingleLineDescription)
        End Function
        '''<summary>Frames a jar so that it uses data prefixed by a size.</summary>
        <Extension()> <Pure()>
        Public Function DataSizePrefixed(Of T)(ByVal jar As IJar(Of T),
                                               ByVal prefixSize As Integer) As SizePrefixedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Ensures(Contract.Result(Of SizePrefixedFramingJar(Of T))() IsNot Nothing)
            Return New SizePrefixedFramingJar(Of T)(subjar:=jar, prefixSize:=prefixSize)
        End Function
        '''<summary>Frames a jar so that it uses data prefixed by a count.</summary>
        <Extension()> <Pure()>
        Public Function RepeatedWithCountPrefix(Of T)(ByVal jar As IJar(Of T),
                                                      ByVal prefixSize As Integer,
                                                      Optional ByVal useSingleLineDescription As Boolean = False) As ItemCountPrefixedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Ensures(Contract.Result(Of ItemCountPrefixedFramingJar(Of T))() IsNot Nothing)
            Return New ItemCountPrefixedFramingJar(Of T)(jar, prefixSize, useSingleLineDescription)
        End Function
        '''<summary>Frames a jar so that it is not used if there is no more data.</summary>
        <Extension()> <Pure()>
        Public Function [Optional](Of T)(ByVal jar As IJar(Of T)) As OptionalFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of OptionalFramingJar(Of T))() IsNot Nothing)
            Return New OptionalFramingJar(Of T)(jar)
        End Function
        '''<summary>Frames a jar so that it uses reversed data.</summary>
        <Extension()> <Pure()>
        Public Function Reversed(Of T)(ByVal jar As IJar(Of T)) As ReversedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReversedFramingJar(Of T))() IsNot Nothing)
            Return New ReversedFramingJar(Of T)(subjar:=jar)
        End Function
        '''<summary>Prefixes serialized data with a crc32 checksum.</summary>
        '''<param name="prefixSize">Determines how many bytes of the crc32 are used (starting at index 0) (min 1, max 4).</param>
        <Extension()> <Pure()>
        Public Function CRC32ChecksumPrefixed(Of T)(ByVal jar As IJar(Of T),
                                                    Optional ByVal prefixSize As Integer = 4) As ChecksumPrefixedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Requires(prefixSize <= 4)
            Return New ChecksumPrefixedFramingJar(Of T)(jar, prefixSize, Function(data) data.CRC32.Bytes.Take(prefixSize).ToReadableList)
        End Function
        '''<summary>Exposes the jar as an INamedJar with the given name.</summary>
        <Extension()> <Pure()>
        Public Function Named(Of T)(ByVal jar As IJar(Of T), ByVal name As InvariantString) As INamedJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of INamedJar(Of T))() IsNot Nothing)
            Return New NamedJar(Of T)(name, jar)
        End Function

        <Extension()> <Pure()>
        Public Sub SetValueIfDifferent(ByVal editor As ISimpleValueEditor, ByVal value As Object)
            Contract.Requires(editor IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Try
                If value.Equals(editor.Value) Then Return
                editor.Value = value
            Catch ex As PicklingException
                editor.Value = value
            End Try
        End Sub
        <Extension()> <Pure()>
        Public Sub SetValueIfDifferent(Of T)(ByVal editor As IValueEditor(Of T), ByVal value As T)
            Contract.Requires(editor IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Try
                If value.Equals(editor.Value) Then Return
                editor.Value = value
            Catch ex As PicklingException
                editor.Value = value
            End Try
        End Sub
    End Module
End Namespace
