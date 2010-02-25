Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class GameObjectIdJar
        Inherits BaseAnonymousJar(Of GameObjectId)

        Public Overrides Function Pack(Of TValue As GameObjectId)(ByVal value As TValue) As IPickle(Of TValue)
            Dim valued As GameObjectId = value
            Dim data = Concat(valued.AllocatedId.Bytes, valued.CounterId.Bytes).AsReadableList
            Return New Pickle(Of TValue)(value, data, Function() value.ToString)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of GameObjectId)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = New GameObjectId(datum.SubView(0, 4).ToUInt32,
                                         datum.SubView(4, 4).ToUInt32)
            Return New Pickle(Of GameObjectId)(value, datum, Function() value.ToString)
        End Function
    End Class
End Namespace
