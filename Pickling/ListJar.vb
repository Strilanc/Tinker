Namespace Pickling.Jars
    '''<summary>Pickles lists of values, where the serialized form is prefixed by the number of items.</summary>
    Public NotInheritable Class ListJar(Of T)
        Inherits BaseJar(Of IList(Of T))
        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _prefixSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_prefixSize > 0)
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal subJar As IJar(Of T),
                       Optional ByVal prefixSize As Integer = 1)
            MyBase.New(name)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            Me._subJar = subJar
            Me._prefixSize = prefixSize
        End Sub

        Public Overrides Function Pack(Of TValue As IList(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = (From e In value Select CType(_subJar.Pack(e), IPickle(Of T))).ToList()
            Dim data = Concat(CUInt(value.Count).Bytes.SubArray(0, _prefixSize), Concat(From p In pickles Select p.Data.ToArray))
            Return New Pickle(Of TValue)(Me.Name, value, data.AsReadableList(), Function() Pickle(Of T).MakeListDescription(pickles))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IList(Of T))
            'Parse
            Dim vals As New List(Of T)
            Dim pickles As New List(Of IPickle(Of Object))
            Dim curOffset = 0
            'List Size
            If data.Count < _prefixSize Then Throw New PicklingException("Not enough data.")
            Dim numElements = data.SubView(0, _prefixSize).ToUInt64
            curOffset += _prefixSize
            'List Elements
            For repeat = 1UL To numElements
                Contract.Assume(curOffset <= data.Count)
                'Value
                Dim p = _subJar.Parse(data.SubView(curOffset, data.Count - curOffset))
                vals.Add(p.Value)
                pickles.Add(New Pickle(Of Object)(p.Value, p.Data, p.Description))
                'Size
                Dim n = p.Data.Count
                curOffset += n
            Next repeat

            Return New Pickle(Of IList(Of T))(Me.Name, vals, data.SubView(0, curOffset), Function() Pickle(Of Object).MakeListDescription(pickles))
        End Function
    End Class
End Namespace
