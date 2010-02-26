Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPEndPointJar
        Inherits BaseAnonymousJar(Of Net.IPEndPoint)

        Private Shared ReadOnly DataJar As TupleJar = New TupleJar(
                    New UInt16Jar().Named("protocol").Weaken,
                    New UInt16Jar(ByteOrder:=ByteOrder.BigEndian).Named("port").Weaken,
                    New IPAddressJar().Named("ip").Weaken,
                    New RawDataJar(Size:=8).Named("unknown").Weaken)

        Public Overrides Function Pack(Of TValue As Net.IPEndPoint)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Contract.Assume(value.Address IsNot Nothing)
            Dim addrBytes = value.Address.GetAddressBytes
            Dim vals = New Dictionary(Of InvariantString, Object) From {
                    {"protocol", If(addrBytes.SequenceEqual({0, 0, 0, 0}) AndAlso value.Port = 0, 0, 2)},
                    {"ip", value.Address},
                    {"port", value.Port},
                    {"unknown", New Byte() {0, 0, 0, 0, 0, 0, 0, 0}.AsReadableList}}
            Dim pickle = DataJar.Pack(vals)
            Return value.Pickled(pickle.Data)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of Net.IPEndPoint)
            Dim pickle = DataJar.Parse(data)
            Dim vals = pickle.Value
            Dim value = New Net.IPEndPoint(CType(vals("ip"), Net.IPAddress).AssumeNotNull, CUShort(vals("port")))
            Return value.Pickled(pickle.Data)
        End Function
    End Class
End Namespace
