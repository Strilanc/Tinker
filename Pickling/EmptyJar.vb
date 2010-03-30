Namespace Pickling
    Public NotInheritable Class EmptyJar
        Inherits BaseJar(Of Object)

        Public Overrides Function Pack(Of TValue As Object)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(Me, New Byte() {}.AsReadableList)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
            Return Pack(New Object())
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
