Namespace Pickling
    '''<summary>Pickles values where the serialized form is prefixed with a checksum.</summary>
    Public NotInheritable Class ChecksumPrefixedJar(Of T)
        Inherits BaseJar(Of T)

        Private ReadOnly _subJar As IJar(Of T)
        Private ReadOnly _checksumFunction As Func(Of IReadableList(Of Byte), IReadableList(Of Byte))
        Private ReadOnly _checksumSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
            Contract.Invariant(_checksumFunction IsNot Nothing)
            Contract.Invariant(_checksumSize > 0)
        End Sub

        Public Sub New(ByVal subJar As IJar(Of T),
                       ByVal checksumSize As Integer,
                       ByVal checksumFunction As Func(Of IReadableList(Of Byte), IReadableList(Of Byte)))
            Contract.Requires(checksumSize > 0)
            Contract.Requires(subJar IsNot Nothing)
            Contract.Requires(checksumFunction IsNot Nothing)
            Me._subJar = subJar
            Me._checksumSize = checksumSize
            Me._checksumFunction = checksumFunction
        End Sub

        Public Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Dim pickle = _subJar.Pack(value)
            Dim checksum = _checksumFunction(pickle.Data)
            Contract.Assume(checksum IsNot Nothing)
            Contract.Assume(checksum.Count = _checksumSize)
            Dim data = checksum.Concat(pickle.Data).ToReadableList
            Return value.Pickled(data, pickle.Description)
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < _checksumSize Then Throw New PicklingNotEnoughDataException()
            Dim checksum = data.SubView(0, _checksumSize)
            Dim pickle = _subJar.Parse(data.SubView(_checksumSize))
            If Not _checksumFunction(pickle.Data).SequenceEqual(checksum) Then Throw New PicklingException("Checksum didn't match.")
            Dim datum = data.SubView(0, _checksumSize + pickle.Data.Count)
            Return pickle.Value.Pickled(datum, pickle.Description)
        End Function
    End Class
End Namespace
