Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class GameObjectIdJar
        Inherits BaseJar(Of GameObjectId)

        Public Overrides Function Pack(value As GameObjectId) As IRist(Of Byte)
            Return value.AllocatedId.Bytes.Concat(value.CounterId.Bytes).ToRist()
        End Function

        Public Overrides Function Parse(data As IRist(Of Byte)) As ParsedValue(Of GameObjectId)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException("A GameObjectId requires 8 bytes.")
            Dim value = New GameObjectId(data.SubList(0, 4).ToUInt32,
                                         data.SubList(4, 4).ToUInt32)
            Return value.ParsedWithDataCount(8)
        End Function

        <SuppressMessage("Microsoft.Contracts", "Ensures-28-84")>
        Public Overrides Function Parse(text As String) As GameObjectId
            Try
                Return GameObjectId.Parse(text)
            Catch ex As Exception When TypeOf ex Is FormatException OrElse
                                       TypeOf ex Is ArgumentException
                Throw New PicklingException("'{0}' is not a GameObjectId value.".Frmt(text), ex)
            End Try
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
                        End Sub,
                disposer:=Sub()
                              allocControl.Dispose()
                              counterControl.Dispose()
                              panel.Dispose()
                          End Sub)
        End Function
    End Class
End Namespace
