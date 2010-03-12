Namespace Pickling
    Public NotInheritable Class EmptyJar
        Implements ISimpleJar

        Public Function Pack(Of TValue)(ByVal value As TValue) As IPickle(Of TValue) Implements ISimplePackJar.Pack
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(New Byte() {}.AsReadableList, Function() "[No Data]")
        End Function

        Public Function Parse(ByVal data As IReadableList(Of Byte)) As ISimplePickle Implements ISimpleParseJar.Parse
            Return New Pickle(Of Object)(New Object(), New Byte() {}.AsReadableList, New Lazy(Of String)(Function() "[No Data]"))
        End Function
    End Class
End Namespace
