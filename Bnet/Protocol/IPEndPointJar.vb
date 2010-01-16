Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPEndPointJar
        Inherits BaseJar(Of Net.IPEndPoint)

        Private Shared ReadOnly DataJar As TupleJar = New TupleJar("NetIPEndPoint",
                    New UInt16Jar("protocol").Weaken,
                    New UInt16Jar("port", ByteOrder:=ByteOrder.BigEndian).Weaken,
                    New IPAddressJar("ip").Weaken,
                    New RawDataJar("unknown", Size:=8).Weaken)

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Public Overrides Function Pack(Of TValue As Net.IPEndPoint)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
            Dim addrBytes = value.Address.GetAddressBytes
            Contract.Assume(addrBytes IsNot Nothing)
            Dim vals = New Dictionary(Of InvariantString, Object) From {
                    {"protocol", If(addrBytes.SequenceEqual({0, 0, 0, 0}) AndAlso value.Port = 0, 0, 2)},
                    {"ip", value.Address},
                    {"port", value.Port},
                    {"unknown", New Byte() {0, 0, 0, 0, 0, 0, 0, 0}.AsReadableList}}
            Dim pickle = DataJar.Pack(vals)
            Return New Pickle(Of TValue)(Name, value, pickle.Data)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of Net.IPEndPoint)
            Dim pickle = DataJar.Parse(data)
            Dim vals = pickle.Value
            Dim value = New Net.IPEndPoint(CType(vals("ip"), Net.IPAddress).AssumeNotNull, CUShort(vals("port")))
            Return New Pickle(Of Net.IPEndPoint)(Name, value, pickle.Data)
        End Function
    End Class
End Namespace
