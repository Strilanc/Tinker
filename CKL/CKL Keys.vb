Imports Tinker.Bnet

Namespace CKL
    Public Enum CKLPacketId As Byte
        [Error] = 0
        Keys = 1
    End Enum

    Public NotInheritable Class KeyEntry
        Private ReadOnly _name As InvariantString
        Private ReadOnly _keyROC As String
        Private ReadOnly _keyTFT As String

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_keyROC IsNot Nothing)
            Contract.Invariant(_keyTFT IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal keyROC As String,
                       ByVal keyTFT As String)
            Contract.Requires(keyROC IsNot Nothing)
            Contract.Requires(keyTFT IsNot Nothing)
            If keyROC.ToWC3CDKeyCredentials({}, {}).Product <> ProductType.Warcraft3ROC Then Throw New ArgumentException("Not a WC3 ROC key.", "keyROC")
            If keyTFT.ToWC3CDKeyCredentials({}, {}).Product <> ProductType.Warcraft3TFT Then Throw New ArgumentException("Not a WC3 TFT key.", "keyTFT")
            Me._name = name
            Me._keyROC = keyROC
            Me._keyTFT = keyTFT
        End Sub

        Public ReadOnly Property Name As InvariantString
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property KeyROC As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _keyROC
            End Get
        End Property
        Public ReadOnly Property KeyTFT As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _keyTFT
            End Get
        End Property

        <Pure()>
        Public Function GenerateCredentials(ByVal clientToken As UInt32,
                                            ByVal serverToken As UInt32) As ProductCredentialPair
            Contract.Ensures(Contract.Result(Of ProductCredentialPair)() IsNot Nothing)
            Dim roc = _keyROC.ToWC3CDKeyCredentials(clientToken.Bytes, serverToken.Bytes)
            Dim tft = _keyTFT.ToWC3CDKeyCredentials(clientToken.Bytes, serverToken.Bytes)
            Contract.Assume(roc.Product = ProductType.Warcraft3ROC)
            Contract.Assume(tft.Product = ProductType.Warcraft3TFT)
            Return New ProductCredentialPair(authenticationROC:=roc, authenticationTFT:=tft)
        End Function
    End Class
End Namespace
