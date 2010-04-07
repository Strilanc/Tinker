Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPAddressJar
        Inherits BaseJar(Of Net.IPAddress)

        <ContractVerification(False)>
        Public Overrides Function Pack(ByVal value As Net.IPAddress) As IEnumerable(Of Byte)
            Return value.GetAddressBytes()
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of Net.IPAddress)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Return New Net.IPAddress(data.Take(4).ToArray).ParsedWithDataCount(4)
        End Function

        Public Overrides Function Parse(ByVal text As String) As Net.IPAddress
            Try
                Return Net.IPAddress.Parse(text)
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("'{0}' is not an IPAddress.".Frmt(text), ex)
            End Try
        End Function
    End Class
End Namespace
