Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class ProductCredentialsJar
        Inherits BaseConversionJar(Of ProductCredentials, NamedValueMap)

        Private Shared ReadOnly DataJar As New TupleJar(
                New UInt32Jar().Named("length"),
                New EnumUInt32Jar(Of ProductType)().Named("product"),
                New UInt32Jar().Named("public key"),
                New UInt32Jar().Named("unknown"),
                New DataJar().Fixed(exactDataCount:=20).Named("proof"))

        Public Overrides Function SubJar() As IJar(Of NamedValueMap)
            Return DataJar
        End Function
        Public Overrides Function ParseRaw(value As NamedValueMap) As ProductCredentials
            Dim proof = value.ItemAs(Of IRist(Of Byte))("proof")
            If proof.Count <> 20 Then Throw New PicklingException("Proof must have 20 bytes.")
            Return New ProductCredentials(
                    product:=value.ItemAs(Of ProductType)("product"),
                    publicKey:=value.ItemAs(Of UInt32)("public key"),
                    length:=value.ItemAs(Of UInt32)("length"),
                    proof:=proof)
        End Function
        Public Overrides Function PackRaw(value As ProductCredentials) As NamedValueMap
            Return New Dictionary(Of InvariantString, Object) From {
                    {"length", value.Length},
                    {"product", value.Product},
                    {"public key", value.PublicKey},
                    {"unknown", 0UI},
                    {"proof", value.AuthenticationProof}}
        End Function
    End Class
End Namespace
