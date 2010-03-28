Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class FileTimeJar
        Inherits BaseJar(Of Date)

        Public Overrides Function Pack(Of TValue As Date)(ByVal value As TValue) As IPickle(Of TValue)
            Dim data = DirectCast(value, Date).ToFileTime.BitwiseToUInt64.Bytes().AsReadableList
            Return value.Pickled(Me, data)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Date)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = Date.FromFileTime(datum.ToUInt64.BitwiseToInt64)
            Return value.Pickled(Me, datum)
        End Function
    End Class
End Namespace
