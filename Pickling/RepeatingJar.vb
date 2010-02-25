Namespace Pickling
    '''<summary>Pickles lists of values, where the serialized form simply continues until there are no more items.</summary>
    Public NotInheritable Class RepeatingJar(Of T)
        Inherits BaseAnonymousJar(Of IReadableList(Of T))
        Private ReadOnly _subJar As IAnonymousJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IAnonymousJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As IReadableList(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = (From e In value Select CType(_subJar.Pack(e), IPickle(Of T))).ToList()
            Dim data = Concat(From p In pickles Select p.Data.ToArray)
            Return New Pickle(Of TValue)(value, data.AsReadableList(), Function() Pickle(Of T).MakeListDescription(pickles))
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of T))
            'Parse
            Dim vals = New List(Of T)
            Dim pickles = New List(Of IPickle(Of Object))
            Dim curCount = data.Count
            Dim curOffset = 0
            'List Elements
            While curOffset < data.Count
                'Value
                Dim p = _subJar.Parse(data.SubView(curOffset, curCount))
                vals.Add(p.Value)
                pickles.Add(New Pickle(Of Object)(p.Value, p.Data, p.Description))
                'Size
                Dim n = p.Data.Count
                curCount -= n
                curOffset += n
            End While

            Dim datum = data.SubView(0, curOffset)
            Dim value = vals.AsReadableList
            Return New Pickle(Of IReadableList(Of T))(value, datum, Function() Pickle(Of Object).MakeListDescription(pickles))
        End Function
    End Class
End Namespace
