Namespace Pickling
    '''<summary>Pickles the remaining bytes in data.</summary>
    Public Class RemainingDataJar
        Inherits BaseAnonymousJar(Of IReadableList(Of Byte))

        Public Overrides Function Pack(Of TValue As IReadableList(Of Byte))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(value, Function() "[{0}]".Frmt(value.ToHexString))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of Byte))
            Return data.Pickled(data, Function() "[{0}]".Frmt(data.ToHexString))
        End Function
    End Class
End Namespace
