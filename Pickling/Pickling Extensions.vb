Namespace Pickling
    Public Module PicklingExtensions
        '''<summary>Weakens the type of an IJar from T to Object.</summary>
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As INamedJar(Of T)) As INamedJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of INamedJar(Of Object))() IsNot Nothing)
            Return New WeakNamedJar(Of T)(jar)
        End Function

        '''<summary>Weakens the type of an IPackJar from T to Object.</summary>
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IPackJar(Of T)) As IPackJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPackJar(Of Object))() IsNot Nothing)
            Return New WeakPackJar(Of T)(jar)
        End Function

        '''<summary>Weakens the type of an IParseJar from T to Object.</summary>
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IParseJar(Of T)) As IParseJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IParseJar(Of Object))() IsNot Nothing)
            Return New WeakParseJar(Of T)(jar)
        End Function

        <Extension()> <Pure()>
        Public Function Pickled(Of T)(ByVal value As T,
                                      ByVal data As IReadableList(Of Byte),
                                      ByVal description As Lazy(Of String)) As IPickle(Of T)
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Requires(description IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            Return New Pickle(Of T)(value, data, description)
        End Function
        <Extension()> <Pure()>
        Public Function Pickled(Of T)(ByVal value As T,
                                      ByVal data As IReadableList(Of Byte),
                                      Optional ByVal description As Func(Of String) = Nothing) As IPickle(Of T)
            Contract.Requires(value IsNot Nothing)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IPickle(Of T))() IsNot Nothing)
            Return value.Pickled(data, New Lazy(Of String)(If(description, Function() value.ToString)))
        End Function

        <Extension()> <Pure()>
        Public Function MakeListDescription(ByVal pickles As IEnumerable(Of ISimplePickle),
                                            Optional ByVal useSingleLineDescription As Boolean = False) As String
            Contract.Requires(pickles IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Dim descriptions = From e In pickles Select e.Description.Value
            If useSingleLineDescription Then
                Return descriptions.StringJoin("; ")
            Else
                Return {"{", descriptions.StringJoin(Environment.NewLine).Indent("    "), "}"}.StringJoin(Environment.NewLine)
            End If
        End Function

        '''<summary>Exposes an IJar of arbitrary type as an IJar(Of Object).</summary>
        Private NotInheritable Class WeakNamedJar(Of T)
            Inherits BaseJar(Of Object)
            Implements INamedJar(Of Object)

            Private ReadOnly _subJar As INamedJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_subJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal jar As INamedJar(Of T))
                Me._subJar = jar
                Contract.Requires(jar IsNot Nothing)
            End Sub

            Public ReadOnly Property Name As InvariantString Implements IJarInfo.Name
                Get
                    Return _subJar.Name
                End Get
            End Property
            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
                Dim p = _subJar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
            Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
                Contract.Assume(value IsNot Nothing)
                Dim p = _subJar.Pack(CType(CType(value, Object), T).AssumeNotNull)
                Return value.Pickled(p.Data, p.Description)
            End Function
        End Class
        '''<summary>Exposes an IPackJar of arbitrary type as an IPackJar(Of Object).</summary>
        Private NotInheritable Class WeakPackJar(Of T)
            Inherits BasePackJar(Of Object)
            Private ReadOnly subJar As IPackJar(Of T)
            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub
            Public Sub New(ByVal jar As IPackJar(Of T))
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
                Contract.Assume(value IsNot Nothing)
                Dim p = subJar.Pack(CType(CType(value, Object), T).AssumeNotNull)
                Return value.Pickled(p.Data, p.Description)
            End Function
        End Class
        '''<summary>Exposes an IParseJar of arbitrary type as an IParseJar(Of Object).</summary>
        Private NotInheritable Class WeakParseJar(Of T)
            Implements IParseJar(Of Object)
            Private ReadOnly subJar As IParseJar(Of T)
            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub
            Public Sub New(ByVal jar As IParseJar(Of T))
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object) Implements IParseJar(Of Object).Parse
                Dim p = subJar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
        End Class

        Private NotInheritable Class NamedJar(Of T)
            Implements INamedJar(Of T)
            Private ReadOnly _name As InvariantString
            Private ReadOnly _subjar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_subjar IsNot Nothing)
            End Sub

            Public Sub New(ByVal name As InvariantString, ByVal subJar As IJar(Of T))
                Contract.Requires(subJar IsNot Nothing)
                Me._name = name
                Me._subjar = subJar
            End Sub

            Public ReadOnly Property Name As InvariantString Implements INamedJar(Of T).Name
                Get
                    Return _name
                End Get
            End Property

            Public Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue) Implements IPackJar(Of T).Pack
                Dim pickle = _subjar.Pack(value)
                Return value.Pickled(pickle.Data, Function() "{0}: {1}".Frmt(Name, pickle.Description.Value))
            End Function

            Public Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T) Implements IParseJar(Of T).Parse
                Dim pickle = _subjar.Parse(data)
                Return pickle.Value.Pickled(pickle.Data, Function() "{0}: {1}".Frmt(Name, pickle.Description.Value))
            End Function
        End Class

        <Extension()> <Pure()>
        Public Function Repeated(Of T)(ByVal jar As IJar(Of T)) As RepeatingJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of RepeatingJar(Of T))() IsNot Nothing)
            Return New RepeatingJar(Of T)(subJar:=jar)
        End Function
        <Extension()> <Pure()>
        Public Function DataSizePrefixed(Of T)(ByVal jar As IJar(Of T), ByVal prefixSize As Integer) As DataSizePrefixedJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Ensures(Contract.Result(Of DataSizePrefixedJar(Of T))() IsNot Nothing)
            Return New DataSizePrefixedJar(Of T)(subjar:=jar, prefixSize:=prefixSize)
        End Function
        <Extension()> <Pure()>
        Public Function RepeatedWithCountPrefix(Of T)(ByVal jar As IJar(Of T),
                                                      ByVal prefixSize As Integer,
                                                      Optional ByVal useSingleLineDescription As Boolean = False) As ListJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Ensures(Contract.Result(Of ListJar(Of T))() IsNot Nothing)
            Return New ListJar(Of T)(jar, prefixSize, useSingleLineDescription)
        End Function
        <Extension()> <Pure()>
        Public Function [Optional](Of T)(ByVal jar As IJar(Of T)) As OptionalJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of OptionalJar(Of T))() IsNot Nothing)
            Return New OptionalJar(Of T)(jar)
        End Function
        '''<summary>Prefixes serialized data with a crc32 checksum.</summary>
        '''<param name="prefixSize">Determines how many bytes of the crc32 are used (starting at index 0) (min 1, max 4).</param>
        <Extension()> <Pure()>
        Public Function CRC32ChecksumPrefixed(Of T)(ByVal jar As IJar(Of T),
                                                    Optional ByVal prefixSize As Integer = 4) As ChecksumPrefixedJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Contract.Requires(prefixSize <= 4)
            Return New ChecksumPrefixedJar(Of T)(jar, prefixSize, Function(data) data.CRC32.Bytes.Take(prefixSize).ToReadableList)
        End Function
        <Extension()> <Pure()>
        Public Function Named(Of T)(ByVal jar As IJar(Of T), ByVal name As InvariantString) As INamedJar(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of INamedJar(Of T))() IsNot Nothing)
            Return New NamedJar(Of T)(name, jar)
        End Function

        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IJar(Of T)) As IJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IJar(Of Object))() IsNot Nothing)
            Return New WeakJar(Of T)(jar)
        End Function
        Private NotInheritable Class WeakJar(Of T)
            Inherits BaseJar(Of Object)
            Private ReadOnly subJar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal jar As IJar(Of T))
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
                Dim p = subJar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
            Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
                Contract.Assume(value IsNot Nothing)
                Dim p = subJar.Pack(CType(CType(value, Object), T).AssumeNotNull)
                Return value.Pickled(p.Data, p.Description)
            End Function
        End Class
    End Module
End Namespace
