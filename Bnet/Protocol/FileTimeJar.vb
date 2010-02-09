Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class FileTimeJar
        Inherits BaseJar(Of Date)

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Public Overrides Function Pack(Of TValue As Date)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
            Dim datum = CType(value, Date).ToFileTime.BitwiseToUInt64.Bytes().AsReadableList
            Return New Pickle(Of TValue)(Me.Name, value, datum)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of Date)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = Date.FromFileTime(datum.ToUInt64.BitwiseToInt64)
            Return New Pickle(Of Date)(Me.Name, value, datum)
        End Function
    End Class
End Namespace
