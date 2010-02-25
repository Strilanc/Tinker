Namespace Pickling
    Public Module PicklingExtensions
        '''<summary>Weakens the type of an IJar from T to Object.</summary>
        <Extension()> <Pure()>
        Public Function Weaken(Of T)(ByVal jar As IJar(Of T)) As IJar(Of Object)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IJar(Of Object))() IsNot Nothing)
            Return New WeakJar(Of T)(jar)
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

        '''<summary>Exposes an IJar of arbitrary type as an IJar(Of Object).</summary>
        Private NotInheritable Class WeakJar(Of T)
            Inherits BaseJar(Of Object)
            Private ReadOnly subJar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal jar As IJar(Of T))
                MyBase.New(jar.Name)
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
                Return New Pickle(Of R)(value, p.Data, p.Description)
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
                MyBase.New(jar.Name)
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Overrides Function Pack(Of R As Object)(ByVal value As R) As IPickle(Of R)
                Contract.Assume(value IsNot Nothing)
                Dim p = subJar.Pack(CType(CType(value, Object), T).AssumeNotNull)
                Return New Pickle(Of R)(value, p.Data, p.Description)
            End Function
        End Class
        '''<summary>Exposes an IParseJar of arbitrary type as an IParseJar(Of Object).</summary>
        Private NotInheritable Class WeakParseJar(Of T)
            Inherits BaseParseJar(Of Object)
            Private ReadOnly subJar As IParseJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(subJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal jar As IParseJar(Of T))
                MyBase.New(jar.Name)
                Contract.Requires(jar IsNot Nothing)
                Me.subJar = jar
            End Sub
            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
                Dim p = subJar.Parse(data)
                Return New Pickle(Of Object)(p.Value, p.Data, p.Description)
            End Function
        End Class

        Private NotInheritable Class NamedJar(Of T)
            Inherits BaseJar(Of T)
            Private ReadOnly _subjar As IAnonymousJar(Of T)
            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_subjar IsNot Nothing)
            End Sub
            Public Sub New(ByVal name As InvariantString, ByVal subJar As IAnonymousJar(Of T))
                MyBase.New(name)
                Contract.Requires(subJar IsNot Nothing)
                Me._subjar = subJar
            End Sub
            Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
                Dim pickle = _subjar.Pack(value)
                Return New Pickle(Of TValue)(Name, value, pickle.Data, Function() pickle.Description.Value)
            End Function
            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
                Dim pickle = _subjar.Parse(data)
                Return New Pickle(Of T)(Name, pickle.Value, pickle.Data, Function() pickle.Description.Value)
            End Function
        End Class

        <Extension()> <Pure()>
        Public Function Repeated(Of T)(ByVal jar As IAnonymousJar(Of T)) As RepeatingJar(Of T)
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
        Public Function RepeatedWithCountPrefix(Of T)(ByVal jar As IAnonymousJar(Of T),
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
        Public Function Named(Of T)(ByVal jar As IAnonymousJar(Of T), ByVal name As InvariantString) As IJar(Of T)
            Return New NamedJar(Of T)(name, jar)
        End Function
    End Module
End Namespace
