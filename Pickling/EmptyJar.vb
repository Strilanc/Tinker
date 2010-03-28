Namespace Pickling
    Public NotInheritable Class EmptyJar
        Inherits BaseJar(Of Object)

        Public Overrides Function Pack(Of TValue As Object)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(Me, New Byte() {}.AsReadableList, Function() "[No Data]")
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
            Return New Pickle(Of Object)(Me, New Object(), New Byte() {}.AsReadableList, New Lazy(Of String)(Function() "[No Data]"))
        End Function

        Public Overrides Function ValueToControl(ByVal value As Object) As Control
            Dim control = New Label()
            control.Text = "[No Data]"
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As Object
            Return New Object
        End Function
    End Class
End Namespace
