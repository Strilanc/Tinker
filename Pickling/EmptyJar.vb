Namespace Pickling
    Public NotInheritable Class EmptyJar
        Inherits BaseJar(Of Object)
        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub
        Public Overrides Function Pack(Of TValue As Object)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Return New Pickle(Of TValue)(Name, value, New Byte() {}.AsReadableList, Function() "[No Data]")
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Object)
            Return New Pickle(Of Object)(Name, New Object(), New Byte() {}.AsReadableList, Function() "[No Data]")
        End Function
    End Class
End Namespace
