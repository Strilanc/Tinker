Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class DwordStringJar
        Inherits BaseJar(Of String)
        Public Sub New(ByVal name As InvariantString)
            MyBase.new(name)
        End Sub

        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Public Overrides Function Pack(Of TValue As String)(ByVal value As TValue) As IPickle(Of TValue)
            If value.Length > 4 Then Throw New PicklingException("Value must be at most 4 characters.")
            Dim data = value.ToAscBytes().Reverse.PaddedTo(minimumLength:=4)
            Return New Pickling.Pickle(Of TValue)(Me.Name, value, data.AsReadableList)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of String)
            If data.Count < 4 Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, 4)
            Dim value As String = datum.ParseChrString(nullTerminated:=True).Reverse.ToArray
            Return New Pickling.Pickle(Of String)(Me.Name, value, datum)
        End Function
    End Class
End Namespace
