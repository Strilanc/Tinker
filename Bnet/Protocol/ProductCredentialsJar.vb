Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class ProductCredentialsJar
        Inherits BaseJar(Of ProductCredentials)

        Private ReadOnly _dataJar As TupleJar

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_dataJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
            Me._dataJar = New TupleJar(name,
                    New UInt32Jar("length").Weaken,
                    New EnumUInt32Jar(Of ProductType)("product").Weaken,
                    New UInt32Jar("public key").Weaken,
                    New UInt32Jar("unknown").Weaken,
                    New RawDataJar("proof", Size:=20).Weaken)
        End Sub

        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Public Overrides Function Pack(Of TValue As ProductCredentials)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
            Dim vals = New Dictionary(Of InvariantString, Object) From {
                    {"length", value.Length},
                    {"product", value.Product},
                    {"public key", value.PublicKey},
                    {"unknown", 0},
                    {"proof", value.AuthenticationProof}}
            Dim pickle = _dataJar.Pack(vals)
            Return New Pickle(Of TValue)(value, pickle.Data, pickle.Description)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of ProductCredentials)
            Dim pickle = _dataJar.Parse(data)
            Dim vals = pickle.Value
            Dim proof = CType(vals("proof"), IReadableList(Of Byte)).AssumeNotNull
            Contract.Assume(proof.Count = 20)
            Dim value = New ProductCredentials(
                    product:=CType(vals("product"), ProductType),
                    publicKey:=CUInt(vals("public key")),
                    length:=CUInt(vals("length")),
                    proof:=proof)
            Return New Pickle(Of ProductCredentials)(value, pickle.Data, pickle.Description)
        End Function
    End Class
End Namespace
