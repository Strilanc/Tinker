Namespace Pickling.Jars
    '''<summary>Pickles the remaining bytes in data.</summary>
    Public Class RemainingDataJar
        Inherits BaseJar(Of IReadableList(Of Byte))

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Public Overrides Function Pack(Of TValue As IReadableList(Of Byte))(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(Me.Name, value, value, Function() "[{0}]".Frmt(value.ToHexString))
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of Byte))
            Return New Pickle(Of IReadableList(Of Byte))(Me.Name, data, data, Function() "[{0}]".Frmt(data.ToHexString))
        End Function
    End Class
End Namespace
