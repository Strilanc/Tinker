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

        Public Overrides Function MakeControl() As IValueEditor(Of GameObjectId)
            Dim allocControl = New UInt32Jar().Named("allocated id").MakeControl()
            Dim counterControl = New UInt32Jar().Named("counter id").MakeControl()
            Dim panel = PanelWithControls({allocControl.Control, counterControl.Control}, leftToRight:=True, margin:=0)
            Return New DelegatedValueEditor(Of GameObjectId)(
                Control:=panel,
                eventAdder:=Sub(action)
                                AddHandler allocControl.ValueChanged, Sub() action()
                                AddHandler counterControl.ValueChanged, Sub() action()
                            End Sub,
                getter:=Function() New GameObjectId(allocControl.Value, counterControl.Value),
                setter:=Sub(value)
                            allocControl.Value = value.AllocatedId
                            counterControl.Value = value.CounterId
                        End Sub)
        End Function
    End Class
End Namespace
