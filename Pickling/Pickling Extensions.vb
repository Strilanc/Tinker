Namespace Pickling
    Public Module PicklingExtensions
        <Extension()> <Pure()>
        Public Function [Then](Of T1, T2)(jar As INamedJar(Of T1), jar2 As INamedJar(Of T2)) As TupleJar
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(jar2 IsNot Nothing)
            Contract.Ensures(Contract.Result(Of TupleJar)() IsNot Nothing)
            Return New TupleJar({jar.Weaken(), jar2.Weaken()})
        End Function

        <Extension()> <Pure()>
        Public Function Weaken(Of T)(jar As IJar(Of T)) As IJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IJar(Of Object))() IsNot Nothing)
            Return If(TryCast(jar, IJar(Of Object)),
                      New DirectCastJar(Of Object, T)(jar))
        End Function
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(pickle As IPickle(Of T)) As IPickle(Of Object)
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
            Return If(TryCast(pickle, IPickle(Of Object)),
                      New Pickle(Of Object)(pickle.Jar, pickle.Value, pickle.Data))
        End Function
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(jar As INamedJar(Of T)) As INamedJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of INamedJar(Of Object))() IsNot Nothing)
            Return If(TryCast(jar, INamedJar(Of Object)),
                      DirectCast(jar, IJar(Of T)).Weaken().Named(jar.Name))
        End Function

        <Extension()> <Pure()>
        <SuppressMessage("Microsoft.Contracts", "Requires-12-59")>
        Public Function PackPickle(Of T, TValue As T)(jar As IJar(Of T), value As TValue) As IPickle(Of TValue)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of TValue))() IsNot Nothing)
            Return New Pickle(Of TValue)(jar.Weaken(), value, jar.Pack(value).ToRist)
        End Function
        <Extension()> <Pure()>
        Public Function PackPickle(Of T)(jar As IJar(Of Object), value As T) As IPickle(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            Return New Pickle(Of T)(jar, value, jar.Pack(value).ToRist)
        End Function
        <Extension()> <Pure()>
        Public Function ParsePickle(Of T)(jar As IJar(Of T), data As IRist(Of Byte)) As IPickle(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            Dim parsed = jar.Parse(data)
            Return New Pickle(Of T)(jar.Weaken(), parsed.Value, data.TakeExact(parsed.UsedDataCount))
        End Function
        <Extension()> <Pure()>
        Public Function ParsedWithDataCount(Of T)(value As T, usedDataCount As Int32) As ParsedValue(Of T)
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(usedDataCount >= 0)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T))().UsedDataCount = usedDataCount)
            Return New ParsedValue(Of T)(value, usedDataCount)
        End Function
        <Extension()> <Pure()>
        Public Function WithValue(Of T1, T2)(parsedValue As ParsedValue(Of T1), value As T2) As ParsedValue(Of T2)
            Contract.Requires(parsedValue IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T2))() IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ParsedValue(Of T2))().UsedDataCount = parsedValue.UsedDataCount)
            Return New ParsedValue(Of T2)(value, parsedValue.UsedDataCount)
        End Function

        <Extension()> <Pure()>
        Public Function MakeListDescription(descriptions As IEnumerable(Of String),
                                            Optional useSingleLineDescription As Boolean = False) As String
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
        <SuppressMessage("Microsoft.Contracts", "Requires-45-196")>
        Public Function SplitListDescription(text As String, Optional usedSingleLineDescription As Boolean = False) As IEnumerable(Of String)
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
        Public Function Description(pickle As IPickle(Of Object)) As String
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return pickle.Jar.Describe(pickle.Value)
        End Function
        <Extension()> <Pure()>
        Public Function Description(Of T)(pickle As IPickle(Of T)) As String
            Contract.Requires(pickle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return pickle.Jar.Describe(pickle.Value)
        End Function

        '''<summary>Frames a jar so that it uses a fixed amount of data.</summary>
        <Extension()> <Pure()>
        Public Function Fixed(Of T)(jar As IJar(Of T),
                                    exactDataCount As Integer) As FixedSizeFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(exactDataCount >= 0)
            Contract.Ensures(Contract.Result(Of FixedSizeFramingJar(Of T))() IsNot Nothing)
            Return New FixedSizeFramingJar(Of T)(subJar:=jar, dataSize:=exactDataCount)
        End Function
        '''<summary>Frames a jar so that it uses a limited amount of data.</summary>
        <Extension()> <Pure()>
        Public Function Limited(Of T)(jar As IJar(Of T),
                                      maxDataCount As Integer) As LimitedSizeFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(maxDataCount >= 0)
            Contract.Ensures(Contract.Result(Of LimitedSizeFramingJar(Of T))() IsNot Nothing)
            Return New LimitedSizeFramingJar(Of T)(subJar:=jar, maxDataCount:=maxDataCount)
        End Function
        '''<summary>Frames a jar so that it uses data terminated by a zero value.</summary>
        <Extension()> <Pure()>
        Public Function NullTerminated(Of T)(jar As IJar(Of T)) As NullTerminatedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of NullTerminatedFramingJar(Of T))() IsNot Nothing)
            Return New NullTerminatedFramingJar(Of T)(subJar:=jar)
        End Function
        '''<summary>Frames a jar so that it is repeatedly applied until there is no more data.</summary>
        <Extension()> <Pure()>
        Public Function Repeated(Of T)(jar As IJar(Of T),
                                       Optional useSingleLineDescription As Boolean = False) As RepeatedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of RepeatedFramingJar(Of T))() IsNot Nothing)
            Return New RepeatedFramingJar(Of T)(subJar:=jar, useSingleLineDescription:=useSingleLineDescription)
        End Function
        '''<summary>Frames a jar so that it uses data prefixed by a size.</summary>
        <Extension()> <Pure()>
        Public Function DataSizePrefixed(Of T)(jar As IJar(Of T),
                                               prefixSize As Integer) As SizePrefixedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Ensures(Contract.Result(Of SizePrefixedFramingJar(Of T))() IsNot Nothing)
            Return New SizePrefixedFramingJar(Of T)(subjar:=jar, prefixSize:=prefixSize)
        End Function
        '''<summary>Frames a jar so that it uses data prefixed by a count.</summary>
        <Extension()> <Pure()>
        Public Function RepeatedWithCountPrefix(Of T)(jar As IJar(Of T),
                                                      prefixSize As Integer,
                                                      Optional useSingleLineDescription As Boolean = False) As ItemCountPrefixedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Ensures(Contract.Result(Of ItemCountPrefixedFramingJar(Of T))() IsNot Nothing)
            Return New ItemCountPrefixedFramingJar(Of T)(jar, prefixSize, useSingleLineDescription)
        End Function
        '''<summary>Frames a jar so that it is not used if there is no more data.</summary>
        <Extension()> <Pure()>
        Public Function [Optional](Of T)(jar As IJar(Of T)) As OptionalFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of OptionalFramingJar(Of T))() IsNot Nothing)
            Return New OptionalFramingJar(Of T)(jar)
        End Function
        '''<summary>Frames a jar so that it uses reversed data.</summary>
        <Extension()> <Pure()>
        Public Function Reversed(Of T)(jar As IJar(Of T)) As ReversedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReversedFramingJar(Of T))() IsNot Nothing)
            Return New ReversedFramingJar(Of T)(subjar:=jar)
        End Function
        '''<summary>Prefixes serialized data with a crc32 checksum.</summary>
        '''<param name="prefixSize">Determines how many bytes of the crc32 are used (starting at index 0) (min 1, max 4).</param>
        <Extension()> <Pure()>
        Public Function CRC32ChecksumPrefixed(Of T)(jar As IJar(Of T),
                                                    Optional prefixSize As Integer = 4) As ChecksumPrefixedFramingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Requires(prefixSize <= 4)
            Return New ChecksumPrefixedFramingJar(Of T)(jar, prefixSize, Function(data) data.CRC32.Bytes.Take(prefixSize).ToRist)
        End Function
        '''<summary>Exposes the jar as an INamedJar with the given name.</summary>
        <Extension()> <Pure()>
        Public Function Named(Of T)(jar As IJar(Of T), name As InvariantString) As INamedJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of INamedJar(Of T))() IsNot Nothing)
            Return New NamedJar(Of T)(name, jar)
        End Function

        <Extension()> <Pure()>
        Public Sub SetValueIfDifferent(editor As ISimpleValueEditor, value As Object)
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
        Public Sub SetValueIfDifferent(Of T)(editor As IValueEditor(Of T), value As T)
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
