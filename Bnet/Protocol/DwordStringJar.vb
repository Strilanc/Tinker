Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class DwordStringJar
        Inherits BaseAnonymousJar(Of String)

        Public Overrides Function Pack(Of TValue As String)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Length > 4 Then Throw New PicklingException("Value must be at most 4 characters.")
            Dim data = value.ToAscBytes().Reverse.PaddedTo(minimumLength:=4)
            Return New Pickling.Pickle(Of TValue)(value, data.AsReadableList, Function() value.ToString)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of String)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value As String = datum.ParseChrString(nullTerminated:=True).Reverse.ToArray
            Return New Pickling.Pickle(Of String)(value, datum, Function() value.ToString)
        End Function
    End Class
End Namespace
