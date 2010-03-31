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

        <ContractVerification(False)>
        Public Overrides Function Pack(ByVal value As ProductCredentials) As IEnumerable(Of Byte)
            Return DataJar.Pack(PackRawValue(value))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of ProductCredentials)
            Dim parsed = DataJar.Parse(data)
            Return parsed.WithValue(ParseRawValue(parsed.Value))
        End Function

        Private Shared Function ParseRawValue(ByVal vals As NamedValueMap) As ProductCredentials
            Contract.Requires(vals IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ProductCredentials)() IsNot Nothing)
            Dim proof = vals.ItemAs(Of IReadableList(Of Byte))("proof")
            Contract.Assume(proof.Count = 20)
            Return New ProductCredentials(
                    product:=vals.ItemAs(Of ProductType)("product"),
                    publicKey:=vals.ItemAs(Of UInt32)("public key"),
                    length:=vals.ItemAs(Of UInt32)("length"),
                    proof:=proof)
        End Function
        Private Shared Function PackRawValue(ByVal value As ProductCredentials) As NamedValueMap
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of NamedValueMap)() IsNot Nothing)
            Return New Dictionary(Of InvariantString, Object) From {
                    {"length", value.Length},
                    {"product", value.Product},
                    {"public key", value.PublicKey},
                    {"unknown", 0UI},
                    {"proof", value.AuthenticationProof}}
        End Function

        <ContractVerification(False)>
        Public Overrides Function Describe(ByVal value As ProductCredentials) As String
            Return DataJar.Describe(PackRawValue(value))
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
