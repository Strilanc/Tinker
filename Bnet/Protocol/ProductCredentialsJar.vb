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
            Return DataJar.Pack(PackRawValue(value)).With(jar:=Me, value:=value)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of ProductCredentials)
            Dim pickle = DataJar.Parse(data)
            Dim value = ParseRawValue(pickle.Value)
            Return pickle.With(jar:=Me, value:=value)
        End Function

        Private Shared Function ParseRawValue(ByVal vals As NamedValueMap) As ProductCredentials
            Dim proof = vals.ItemAs(Of IReadableList(Of Byte))("proof")
            Contract.Assume(proof.Count = 20)
            Return New ProductCredentials(
                    product:=vals.ItemAs(Of ProductType)("product"),
                    publicKey:=vals.ItemAs(Of UInt32)("public key"),
                    length:=vals.ItemAs(Of UInt32)("length"),
                    proof:=proof)
        End Function
        Private Shared Function PackRawValue(ByVal value As ProductCredentials) As NamedValueMap
            Return New Dictionary(Of InvariantString, Object) From {
                    {"length", value.Length},
                    {"product", value.Product},
                    {"public key", value.PublicKey},
                    {"unknown", 0UI},
                    {"proof", value.AuthenticationProof}}
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of ProductCredentials)
            Dim subControl = DataJar.MakeControl()
            Return New DelegatedValueEditor(Of ProductCredentials)(
                Control:=subControl.Control,
                eventAdder:=Sub(action) AddHandler subControl.ValueChanged, Sub() action(),
                getter:=Function() ParseRawValue(subControl.Value),
                setter:=Sub(value) subControl.Value = PackRawValue(value))
        End Function
    End Class
End Namespace
