Namespace Pickling
    Public NotInheritable Class InteriorSwitchJar(Of TKey, T)
        Inherits BaseJar(Of T)
        Private ReadOnly _subJars As New Dictionary(Of TKey, IJar(Of T))
        Private ReadOnly _valueKeyExtractor As Func(Of T, TKey)
        Private ReadOnly _dataKeyExtractor As Func(Of IReadableList(Of Byte), TKey)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJars IsNot Nothing)
            Contract.Invariant(_valueKeyExtractor IsNot Nothing)
            Contract.Invariant(_dataKeyExtractor IsNot Nothing)
        End Sub

        Public Sub New(ByVal valueKeyExtractor As Func(Of T, TKey),
                       ByVal dataKeyExtractor As Func(Of IReadableList(Of Byte), TKey))
            Contract.Requires(valueKeyExtractor IsNot Nothing)
            Contract.Requires(dataKeyExtractor IsNot Nothing)
            Me._valueKeyExtractor = valueKeyExtractor
            Me._dataKeyExtractor = dataKeyExtractor
        End Sub

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            Dim key = _dataKeyExtractor(data)
            If Not _subJars.ContainsKey(key) Then Throw New PicklingException("No parser registered to {0}.".Frmt(key))
            Return _subJars(key).AssumeNotNull.Parse(data)
        End Function
        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim key = _valueKeyExtractor(value)
            If Not _subJars.ContainsKey(key) Then Throw New PicklingException("No packer registered to {0}.".Frmt(key))
            Return _subJars(key).AssumeNotNull.Pack(value)
        End Function

        Public Sub AddSubJar(ByVal key As TKey, ByVal jar As IJar(Of T))
            Contract.Requires(key IsNot Nothing)
            Contract.Requires(jar IsNot Nothing)
            If _subJars.ContainsKey(key) Then Throw New InvalidOperationException("Parser already registered to {0}".Frmt(key))
            _subJars.Add(key, jar)
        End Sub
    End Class
End Namespace
