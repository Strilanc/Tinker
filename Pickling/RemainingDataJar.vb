Namespace Pickling
    '''<summary>Pickles the remaining bytes in data.</summary>
    Public Class RemainingDataJar
        Inherits BaseAnonymousJar(Of IReadableList(Of Byte))

        Public Overrides Function Pack(Of TValue As IReadableList(Of Byte))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Return New Pickle(Of TValue)(value, value, Function() "[{0}]".Frmt(value.ToHexString))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of Byte))
            Return New Pickle(Of IReadableList(Of Byte))(data, data, Function() "[{0}]".Frmt(data.ToHexString))
        End Function
    End Class
End Namespace
