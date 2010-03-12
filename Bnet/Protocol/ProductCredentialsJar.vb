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
                    {"unknown", 0},
                    {"proof", value.AuthenticationProof}})
            Dim pickle = DataJar.Pack(vals)
            Return value.Pickled(pickle.Data, pickle.Description)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of ProductCredentials)
            Dim pickle = DataJar.Parse(data)
            Dim vals = pickle.Value
            Dim proof = CType(vals("proof"), IReadableList(Of Byte))
            Contract.Assume(proof.Count = 20)
            Dim value = New ProductCredentials(
                    product:=CType(vals("product"), ProductType),
                    publicKey:=CUInt(vals("public key")),
                    length:=CUInt(vals("length")),
                    proof:=proof)
            Return value.Pickled(pickle.Data, pickle.Description)
        End Function
    End Class
End Namespace
