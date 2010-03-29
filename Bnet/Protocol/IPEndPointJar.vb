Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPEndPointJar
        Inherits BaseJar(Of Net.IPEndPoint)

        Private Shared ReadOnly DataJar As TupleJar = New TupleJar(
                    New UInt16Jar().Named("protocol"),
                    New UInt16Jar(ByteOrder:=ByteOrder.BigEndian).Named("port"),
                    New IPAddressJar().Named("ip"),
                    New DataJar().Fixed(exactDataCount:=8).Named("unknown"))

        Public Overrides Function Pack(Of TValue As Net.IPEndPoint)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Contract.Assume(value.Address IsNot Nothing)
            Dim addrBytes = value.Address.GetAddressBytes
            Dim vals = New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                    {"protocol", If(addrBytes.SequenceEqual({0, 0, 0, 0}) AndAlso value.Port = 0, 0US, 2US)},
                    {"ip", value.Address},
                    {"port", CUShort(value.Port)},
                    {"unknown", New Byte() {0, 0, 0, 0, 0, 0, 0, 0}.AsReadableList}})
            Dim pickle = DataJar.Pack(vals)
            Return pickle.With(jar:=Me, value:=value, description:=Function() value.ToString)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Net.IPEndPoint)
            Dim pickle = DataJar.Parse(data)
            Dim vals = pickle.Value
            Dim value = New Net.IPEndPoint(vals.ItemAs(Of Net.IPAddress)("ip"), vals.ItemAs(Of UInt16)("port"))
            Return pickle.With(jar:=Me, value:=value, description:=Function() value.ToString)
        End Function

        Public Overrides Function ValueToControl(ByVal value As Net.IPEndPoint) As Control
            Dim addressControl = New IPAddressJar().Named("address").ValueToControl(value.Address)
            Dim portControl = New UInt16Jar().Named("port").ValueToControl(CUShort(value.Port))
            Return PanelWithControls({addressControl, portControl}, leftToRight:=True)
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As Net.IPEndPoint
            Return New Net.IPEndPoint(New IPAddressJar().Named("address").ControlToValue(control.Controls(0)),
                                      New UInt16Jar().Named("port").ControlToValue(control.Controls(1)))
        End Function
    End Class
End Namespace
