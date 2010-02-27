Namespace Pickling
    '''<summary>Pickles lists of values, where the serialized form simply continues until there are no more items.</summary>
    Public NotInheritable Class RepeatingJar(Of T)
        Inherits BaseJar(Of IReadableList(Of T))
        Private ReadOnly _subJar As IJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As IReadableList(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = (From e In value Select CType(_subJar.Pack(e), IPickle(Of T))).Cache
            Dim data = Concat(From p In pickles Select (p.Data)).ToReadableList
            Return value.Pickled(data, Function() pickles.MakeListDescription())
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of T))
            'Parse
            Dim pickles = New List(Of IPickle(Of T))
            Dim curCount = data.Count
            Dim curOffset = 0
            'List Elements
            While curOffset < data.Count
                'Value
                Dim p = _subJar.Parse(data.SubView(curOffset, curCount))
                pickles.Add(p)
                'Size
                Dim n = p.Data.Count
                curCount -= n
                curOffset += n
            End While

            Dim datum = data.SubView(0, curOffset)
            Dim value = (From p In pickles Select (p.Value)).ToReadableList
            Return value.Pickled(datum, Function() pickles.MakeListDescription())
        End Function
    End Class
End Namespace
