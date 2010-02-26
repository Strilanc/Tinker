Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPAddressJar
        Inherits BaseAnonymousJar(Of Net.IPAddress)

        Public Overrides Function Pack(Of TValue As Net.IPAddress)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim data = value.GetAddressBytes()
            Return value.Pickled(data.AsReadableList)
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of System.Net.IPAddress)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = New Net.IPAddress(datum.ToArray)
            Return value.Pickled(datum)
        End Function
    End Class
End Namespace
