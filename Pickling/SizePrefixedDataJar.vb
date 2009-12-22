Namespace Pickling.Jars
    '''<summary>Pickles lists of bytes prefixed by the size of the list.</summary>
    Public Class SizePrefixedDataJar
        Inherits BaseJar(Of IReadableList(Of Byte))
        Private ReadOnly _prefixSize As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_prefixSize > 0)
            Contract.Invariant(_prefixSize <= 4)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal prefixSize As Integer)
            MyBase.New(name)
            Contract.Requires(prefixSize > 0)
            If prefixSize > 4 Then Throw New ArgumentOutOfRangeException("sizePrefixSize", "prefix size must be less than or equal to 4.")
            Me._prefixSize = prefixSize
        End Sub

        'verification disabled due to inherited preconditions being lost
        <ContractVerification(False)>
        Public Overrides Function Pack(Of TValue As IReadableList(Of Byte))(ByVal value As TValue) As IPickle(Of TValue)
            Dim prefix = CUInt(value.Count).Bytes().SubArray(0, _prefixSize).AsReadableList
            If prefix.ToUInt32 <> value.Count Then Throw New PicklingException("Data size won't fit in {0}-byte prefix.".Frmt(_prefixSize))

            Dim data = {prefix, value}.Fold.ToArray.AsReadableList
            Return New Pickle(Of TValue)(Me.Name, value, data, Function() "[{0}]".Frmt(value.ToHexString))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of Byte))
            If data.Count < _prefixSize Then Throw New PicklingException("Not enough data.")
            Dim prefix = data.SubView(0, _prefixSize)
            Dim size = CInt(prefix.ToUInt32)

            If data.Count < _prefixSize + size Then Throw New PicklingException("Not enough data.")
            Dim datum = data.SubView(0, _prefixSize + size)
            Dim value = datum.SubView(_prefixSize)
            Return New Pickle(Of IReadableList(Of Byte))(Me.Name, value, data, Function() "[{0}]".Frmt(value.ToHexString))
        End Function
    End Class
End Namespace
