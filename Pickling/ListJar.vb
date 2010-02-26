Namespace Pickling
    '''<summary>Pickles lists of values, where the serialized form is prefixed by the number of items.</summary>
    Public NotInheritable Class ListJar(Of T)
        Inherits BaseAnonymousJar(Of IReadableList(Of T))
        Private ReadOnly _subJar As IAnonymousJar(Of T)
        Private ReadOnly _prefixSize As Integer
        Private ReadOnly _useSingleLineDescription As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_prefixSize > 0)
            Contract.Invariant(_prefixSize <= 8)
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IAnonymousJar(Of T),
                       ByVal prefixSize As Integer,
                       Optional ByVal useSingleLineDescription As Boolean = False)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(prefixSize > 0)
            If prefixSize > 8 Then Throw New ArgumentOutOfRangeException("prefixSize", "prefixSize must be less than or equal to 8.")
            Me._subJar = subJar
            Me._prefixSize = prefixSize
            Me._useSingleLineDescription = useSingleLineDescription
        End Sub

        Public Overrides Function Pack(Of TValue As IReadableList(Of T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickles = (From e In value Select _subJar.Pack(e)).ToList
            Dim data = Concat(CULng(value.Count).Bytes.SubArray(0, _prefixSize), Concat(From p In pickles Select p.Data.ToArray))
            Return value.Pickled(data.AsReadableList, Function() pickles.MakeListDescription(_useSingleLineDescription))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of T))
            'Parse
            Dim pickles = New List(Of IPickle(Of T))
            Dim curOffset = 0
            'List Size
            If data.Count < _prefixSize Then Throw New PicklingNotEnoughDataException()
            Dim numElements = data.SubView(0, _prefixSize).ToUInt64
            curOffset += _prefixSize
            'List Elements
            For repeat = 1UL To numElements
                'Value
                Dim p = _subJar.Parse(data.SubView(curOffset, data.Count - curOffset))
                pickles.Add(p)
                'Size
                Dim n = p.Data.Count
                curOffset += n
                If curOffset > data.Count Then Throw New InvalidStateException("Subjar '{0}' reported taking more data than was available.".Frmt(_subJar.GetType.Name))
            Next repeat

            Dim value = (From p In pickles Select (p.Value)).ToReadableList
            Dim datum = data.SubView(0, curOffset)
            Dim desc = Function() pickles.MakeListDescription(_useSingleLineDescription)
            Return value.Pickled(datum, desc)
        End Function
    End Class
End Namespace
