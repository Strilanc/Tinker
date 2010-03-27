Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class ProductCredentialsJar
        Inherits BaseJar(Of ProductCredentials)

        Private Shared ReadOnly DataJar As New TupleJar(
                New UInt32Jar().Named("length"),
                New EnumUInt32Jar(Of ProductType)().Named("product"),
                New UInt32Jar().Named("public key"),
                New UInt32Jar().Named("unknown"),
                New DataJar().Fixed(exactDataCount:=20).Named("proof"))

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Pack(Of TValue As ProductCredentials)(ByVal value As TValue) As IPickle(Of TValue)
            Dim vals = New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                    {"length", value.Length},
                    {"product", value.Product},
                    {"public key", value.PublicKey},
                    {"unknown", 0UI},
                    {"proof", value.AuthenticationProof}})
            Return DataJar.Pack(vals).WithValue(value)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of ProductCredentials)
            Dim pickle = DataJar.Parse(data)
            Dim vals = pickle.Value
            Dim proof = vals.ItemAs(Of IReadableList(Of Byte))("proof")
            Contract.Assume(proof.Count = 20)
            Dim value = New ProductCredentials(
                    product:=vals.ItemAs(Of ProductType)("product"),
                    publicKey:=vals.ItemAs(Of UInt32)("public key"),
                    length:=vals.ItemAs(Of UInt32)("length"),
                    proof:=proof)
            Return pickle.WithValue(value)
        End Function
    End Class
End Namespace
