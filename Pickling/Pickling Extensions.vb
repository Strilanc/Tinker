Namespace Pickling
    Public Module PicklingExtensions
        <Extension()> <Pure()>
        Public Function Pickled(Of T)(ByVal value As T,
                                      ByVal jar As ISimpleJar,
                                      ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            Return New Pickle(Of T)(jar, value, data)
        End Function

        <Extension()> <Pure()>
        Public Function [With](Of T)(ByVal pickle As IPickle(Of T),
                                     ByVal jar As ISimpleJar,
                                     Optional ByVal data As IReadableList(Of Byte) = Nothing) As IPickle(Of T)
            Contract.Requires(pickle IsNot Nothing)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Pickle(Of T))() IsNot Nothing)
            Return New Pickle(Of T)(jar, pickle.Value, If(data, pickle.Data))
        End Function
        <Extension()> <Pure()>
        Public Function [With](Of T)(ByVal pickle As ISimplePickle,
                                     ByVal jar As ISimpleJar,
                                     ByVal value As T,
                                     Optional ByVal data As IReadableList(Of Byte) = Nothing) As IPickle(Of T)
            Contract.Requires(pickle IsNot Nothing)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Pickle(Of T))() IsNot Nothing)
            Return New Pickle(Of T)(jar, value, If(data, pickle.Data))
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
                Return {"{", descriptions.StringJoin(Environment.NewLine).Indent("    "), "}"}.StringJoin(Environment.NewLine)
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
    End Module
End Namespace
