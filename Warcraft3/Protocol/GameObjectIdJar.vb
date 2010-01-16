Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class GameObjectIdJar
        Inherits BaseJar(Of GameObjectId)

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Public Overrides Function Pack(Of R As GameObjectId)(ByVal value As R) As IPickle(Of R)
            Dim valued As GameObjectId = value
            Dim data = Concat(valued.AllocatedId.Bytes, valued.CounterId.Bytes).AsReadableList
            Return New Pickle(Of R)(Me.Name, value, data, Function() ValueToString(valued))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of GameObjectId)
            If data.Count < 8 Then Throw New PicklingException("Not enough data.")
            Dim datum = data.SubView(0, 8)
            Dim value = New GameObjectId(datum.SubView(0, 4).ToUInt32,
                                     datum.SubView(4, 4).ToUInt32)
            Return New Pickle(Of GameObjectId)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Private Function ValueToString(ByVal value As GameObjectId) As String
            If value.AllocatedId = UInt32.MaxValue AndAlso value.CounterId = UInt32.MaxValue Then Return "[none]"
            If value.AllocatedId = value.CounterId Then Return "preplaced id = {0}".Frmt(value.AllocatedId)
            Return "allocated id = {0}, counter id = {1}".Frmt(value.AllocatedId, value.CounterId)
        End Function
    End Class
End Namespace
