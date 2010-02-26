Namespace Pickling
    '''<summary>Pickles fixed-size lists of bytes.</summary>
    Public Class RawDataJar
        Inherits BaseAnonymousJar(Of IReadableList(Of Byte))
        Private ReadOnly _size As Integer

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_size > 0)
        End Sub

        Public Sub New(ByVal size As Integer)
            Contract.Requires(size > 0)
            Me._size = size
        End Sub

        Public Overrides Function Pack(Of TValue As IReadableList(Of Byte))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Count <> _size Then Throw New PicklingException("Byte array is not of the correct length.")
            Dim data = value
            Return value.Pickled(data, Function() "[{0}]".Frmt(value.ToHexString))
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of Byte))
            If data.Count < _size Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, _size)
            Dim value = datum
            Return value.Pickled(datum, Function() "[{0}]".Frmt(value.ToHexString))
        End Function
    End Class
End Namespace
