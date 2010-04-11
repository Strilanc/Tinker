Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPAddressJar
        Inherits BaseFixedSizeJar(Of Net.IPAddress)

        <ContractVerification(False)>
        Public Overrides Function Pack(ByVal value As Net.IPAddress) As IEnumerable(Of Byte)
            Return value.GetAddressBytes()
        End Function
        Protected Overrides ReadOnly Property DataSize As UInt16
            Get
                Return 4
            End Get
        End Property
        Protected Overrides Function FixedSizeParse(ByVal data As IReadableList(Of Byte)) As Net.IPAddress
            Return New Net.IPAddress(data.ToArray)
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
