Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class GameObjectIdJar
        Inherits BaseJar(Of GameObjectId)

        Public Overrides Function Pack(Of TValue As GameObjectId)(ByVal value As TValue) As IPickle(Of TValue)
            Dim valued As GameObjectId = value
            Dim data = valued.AllocatedId.Bytes.Concat(valued.CounterId.Bytes).ToReadableList
            Return value.Pickled(Me, data)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of GameObjectId)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = New GameObjectId(datum.SubView(0, 4).ToUInt32,
                                         datum.SubView(4, 4).ToUInt32)
            Return value.Pickled(Me, datum)
        End Function


        Public Overrides Function ValueToControl(ByVal value As GameObjectId) As Control
            Dim allocControl = New UInt32Jar().Named("allocated id").ValueToControl(value.AllocatedId)
            Dim counterControl = New UInt32Jar().Named("counter id").ValueToControl(value.CounterId)
            Return PanelWithControls({allocControl, counterControl},
                                     leftToRight:=True)
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As GameObjectId
            Return New GameObjectId(New UInt32Jar().Named("allocated id").ControlToValue(control.Controls(0)),
                                    New UInt32Jar().Named("counter id").ControlToValue(control.Controls(1)))
        End Function
    End Class
End Namespace
