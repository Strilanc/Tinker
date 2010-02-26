Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class DwordStringJar
        Inherits BaseJar(Of String)

        Public Overrides Function Pack(Of TValue As String)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Length > 4 Then Throw New PicklingException("Value must be at most 4 characters.")
            Dim data = value.ToAscBytes().Reverse.PaddedTo(minimumLength:=4)
            Return value.Pickled(data.AsReadableList)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of String)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = New String(datum.ParseChrString(nullTerminated:=True).Reverse.ToArray)
            Return value.Pickled(datum)
        End Function
    End Class
End Namespace
