Namespace Pickling.Jars
    Public NotInheritable Class InteriorSwitchJar(Of K, T)
        Inherits BaseJar(Of T)
        Private ReadOnly _packers As New Dictionary(Of K, IPackJar(Of T))
        Private ReadOnly _parsers As New Dictionary(Of K, IParseJar(Of T))
        Private ReadOnly _valueKeyExtractor As Func(Of T, K)
        Private ReadOnly _dataKeyExtractor As Func(Of IReadableList(Of Byte), K)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_packers IsNot Nothing)
            Contract.Invariant(_parsers IsNot Nothing)
            Contract.Invariant(_valueKeyExtractor IsNot Nothing)
            Contract.Invariant(_dataKeyExtractor IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal valueKeyExtractor As Func(Of T, K),
                       ByVal dataKeyExtractor As Func(Of IReadableList(Of Byte), K))
            MyBase.new(name)
            Contract.Requires(valueKeyExtractor IsNot Nothing)
            Contract.Requires(dataKeyExtractor IsNot Nothing)
            Me._valueKeyExtractor = valueKeyExtractor
            Me._dataKeyExtractor = dataKeyExtractor
        End Sub

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Dim key = _dataKeyExtractor(data)
            If Not _parsers.ContainsKey(key) Then Throw New PicklingException("No parser registered to {0}.".Frmt(key))
            Return _parsers(key).Parse(data)
        End Function
        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim key = _valueKeyExtractor(value)
            If Not _packers.ContainsKey(key) Then Throw New PicklingException("No packer registered to {0}.".Frmt(key))
            Return _packers(key).Pack(value)
        End Function

        Public Sub AddPackerParser(ByVal key As K, ByVal jar As IJar(Of T))
            Contract.Requires(key IsNot Nothing)
            Contract.Requires(jar IsNot Nothing)
            If _parsers.ContainsKey(key) Then Throw New InvalidOperationException("Parser already registered to {0}".Frmt(key))
            If _packers.ContainsKey(key) Then Throw New InvalidOperationException("Packer already registered to {0}".Frmt(key))
            _parsers.Add(key, jar)
            _packers.Add(key, jar)
        End Sub
        Public Sub AddParser(ByVal key As K, ByVal parser As IParseJar(Of T))
            Contract.Requires(key IsNot Nothing)
            Contract.Requires(parser IsNot Nothing)
            If _parsers.ContainsKey(key) Then Throw New InvalidOperationException("Parser already registered to {0}".Frmt(key))
            _parsers.Add(key, parser)
        End Sub
        Public Sub AddPacker(ByVal key As K, ByVal packer As IPackJar(Of T))
            Contract.Requires(key IsNot Nothing)
            Contract.Requires(packer IsNot Nothing)
            If _packers.ContainsKey(key) Then Throw New InvalidOperationException("Packer already registered to {0}".Frmt(key))
            _packers.Add(key, packer)
        End Sub
    End Class
 End Namespace
