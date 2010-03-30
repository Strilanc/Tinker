Namespace Pickling
    Public NotInheritable Class EmptyJar
        Inherits BaseJar(Of Object)

        Public Overrides Function Pack(ByVal value As Object) As IEnumerable(Of Byte)
            Return New Byte() {}
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
            Return New Pickle(Of Object)(Me, New Object, data)
        End Function

        Public Overrides Function Describe(ByVal value As Object) As String
            Return "[No Data]"
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of Object)
            Dim control = New Label()
            control.Text = "[No Data]"
            Return New DelegatedValueEditor(Of Object)(
                control:=control,
                eventAdder:=Sub(action)
                            End Sub,
                getter:=Function() New Object(),
                setter:=Sub(value)
                        End Sub)
        End Function
    End Class
End Namespace
