Namespace Bnet
    Public Enum ProductType As UInt32
        Warcraft3ROC = 14
        Warcraft3TFT = 18
    End Enum

    ''' <summary>
    ''' Precomputed credentials used for answering a challenge to prove ownership of a product.
    ''' </summary>
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class ProductCredentials
        Implements IEquatable(Of ProductCredentials)

        Private ReadOnly _length As UInt32
        Private ReadOnly _product As ProductType
        Private ReadOnly _publicKey As UInteger
        Private ReadOnly _proof As IReadableList(Of Byte)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_proof IsNot Nothing)
            Contract.Invariant(_proof.Count = 20)
        End Sub

        Public Sub New(ByVal length As UInt32,
                       ByVal product As ProductType,
                       ByVal publicKey As UInteger,
                       ByVal proof As IReadableList(Of Byte))
            Contract.Requires(proof IsNot Nothing)
            Contract.Requires(proof.Count = 20)
            Me._product = product
            Me._publicKey = publicKey
            Me._length = length
            Me._proof = proof
        End Sub

        '''<summary>Determines the length of the credentials' representation (eg. the number of characters in a cd key).</summary>
        Public ReadOnly Property Length As UInt32
            Get
                Return _length
            End Get
        End Property
        '''<summary>Determines the product the credentials apply to.</summary>
        Public ReadOnly Property Product As ProductType
            Get
                Return _product
            End Get
        End Property
        '''<summary>Identifies the credentials.</summary>
        Public ReadOnly Property PublicKey As UInteger
            Get
                Return _publicKey
            End Get
        End Property
        '''<summary>A stored response to an authentication challenge.</summary>
        Public ReadOnly Property AuthenticationProof() As IReadableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))().Count = 20)
                Return _proof
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return _publicKey.GetHashCode()
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Dim other = TryCast(obj, ProductCredentials)
            Return other IsNot Nothing AndAlso Me.Equals(other)
        End Function
        Public Overloads Function Equals(ByVal other As ProductCredentials) As Boolean Implements IEquatable(Of ProductCredentials).Equals
            If other Is Nothing Then Return False
            If other.Length <> Me.Length Then Return False
            If other.Product <> Me.Product Then Return False
            If other.PublicKey <> Me.PublicKey Then Return False
            If Not other.AuthenticationProof.SequenceEqual(Me.AuthenticationProof) Then Return False
            Return True
        End Function

        Public Overrides Function ToString() As String
            Return "{0}: {1}".Frmt(Me.Product, Me.PublicKey)
        End Function
    End Class

    ''' <summary>
    ''' Precomputed pair of credentials used for answering a challenge to prove ownership of a product.
    ''' </summary>
    Public NotInheritable Class ProductCredentialPair
        Private ReadOnly _authenticationROC As ProductCredentials
        Private ReadOnly _authenticationTFT As ProductCredentials

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_authenticationROC IsNot Nothing)
            Contract.Invariant(_authenticationTFT IsNot Nothing)
            Contract.Invariant(_authenticationROC.Product = Bnet.ProductType.Warcraft3ROC)
            Contract.Invariant(_authenticationTFT.Product = Bnet.ProductType.Warcraft3TFT)
        End Sub

        Public Sub New(ByVal authenticationROC As Bnet.ProductCredentials,
                       ByVal authenticationTFT As Bnet.ProductCredentials)
            Contract.Requires(authenticationROC IsNot Nothing)
            Contract.Requires(authenticationTFT IsNot Nothing)
            Contract.Requires(authenticationROC.Product = Bnet.ProductType.Warcraft3ROC)
            Contract.Requires(authenticationTFT.Product = Bnet.ProductType.Warcraft3TFT)
            Me._authenticationROC = authenticationROC
            Me._authenticationTFT = authenticationTFT
        End Sub

        Public ReadOnly Property AuthenticationROC As Bnet.ProductCredentials
            Get
                Contract.Ensures(Contract.Result(Of Bnet.ProductCredentials)() IsNot Nothing)
                Return _authenticationROC
            End Get
        End Property
        Public ReadOnly Property AuthenticationTFT As Bnet.ProductCredentials
            Get
                Contract.Ensures(Contract.Result(Of Bnet.ProductCredentials)() IsNot Nothing)
                Return _authenticationTFT
            End Get
        End Property
    End Class

    '''<summary>Computes credential pairs used for answering a challenge to prove ownership of a product.</summary>
    <ContractClass(GetType(IProductAuthenticator.ContractClass))>
    Public Interface IProductAuthenticator
        Function AsyncAuthenticate(ByVal clientSalt As IEnumerable(Of Byte), ByVal serverSalt As IEnumerable(Of Byte)) As IFuture(Of ProductCredentialPair)

        <ContractClassFor(GetType(IProductAuthenticator))>
        Class ContractClass
            Implements IProductAuthenticator
            Public Function AsyncAuthenticate(ByVal clientSalt As IEnumerable(Of Byte),
                                              ByVal serverSalt As IEnumerable(Of Byte)) As IFuture(Of ProductCredentialPair) Implements IProductAuthenticator.AsyncAuthenticate
                Contract.Requires(clientSalt IsNot Nothing)
                Contract.Requires(serverSalt IsNot Nothing)
                Contract.Ensures(Contract.Result(Of IFuture(Of ProductCredentialPair))() IsNot Nothing)
                Throw New NotSupportedException()
            End Function
        End Class
    End Interface

    Public Class CDKeyProductAuthenticator
        Implements IProductAuthenticator

        Private ReadOnly _rocKey As String
        Private ReadOnly _tftKey As String

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_rocKey IsNot Nothing)
            Contract.Invariant(_tftKey IsNot Nothing)
        End Sub

        Public Sub New(ByVal cdKeyROC As String, ByVal cdKeyTFT As String)
            Contract.Requires(cdKeyROC IsNot Nothing)
            Contract.Requires(cdKeyTFT IsNot Nothing)
            If cdKeyROC.ToWC3CDKeyCredentials({}, {}).Product <> ProductType.Warcraft3ROC Then Throw New ArgumentException("Invalid ROC cd key.")
            If cdKeyTFT.ToWC3CDKeyCredentials({}, {}).Product <> ProductType.Warcraft3TFT Then Throw New ArgumentException("Invalid TFT cd key.")
            Me._rocKey = cdKeyROC
            Me._tftKey = cdKeyTFT
        End Sub

        <ContractVerification(False)>
        Public Function AsyncAuthenticate(ByVal clientSalt As IEnumerable(Of Byte),
                                          ByVal serverSalt As IEnumerable(Of Byte)) As IFuture(Of ProductCredentialPair) Implements IProductAuthenticator.AsyncAuthenticate
            Return New ProductCredentialPair(_rocKey.ToWC3CDKeyCredentials(clientSalt, serverSalt),
                                             _tftKey.ToWC3CDKeyCredentials(clientSalt, serverSalt)).Futurized
        End Function
    End Class

    Public Module WC3CDKey
#Region "Data"
        Private Const NumDigitsBase2 As Integer = 120
        Private Const NumDigitsBase16 As Integer = 30
        Private Const NumDigitsBase256 As Integer = 15
        Private Const NumDigitsBase25 As Integer = 26
        Private Const NumDigitsBase5 As Integer = NumDigitsBase25 * 2

        Private ReadOnly nibblePermutations As Byte()() = {
                    New Byte() {&H9, &H4, &H7, &HF, &HD, &HA, &H3, &HB, &H1, &H2, &HC, &H8, &H6, &HE, &H5, &H0},
                    New Byte() {&H9, &HB, &H5, &H4, &H8, &HF, &H1, &HE, &H7, &H0, &H3, &H2, &HA, &H6, &HD, &HC},
                    New Byte() {&HC, &HE, &H1, &H4, &H9, &HF, &HA, &HB, &HD, &H6, &H0, &H8, &H7, &H2, &H5, &H3},
                    New Byte() {&HB, &H2, &H5, &HE, &HD, &H3, &H9, &H0, &H1, &HF, &H7, &HC, &HA, &H6, &H4, &H8},
                    New Byte() {&H6, &H2, &H4, &H5, &HB, &H8, &HC, &HE, &HD, &HF, &H7, &H1, &HA, &H0, &H3, &H9},
                    New Byte() {&H5, &H4, &HE, &HC, &H7, &H6, &HD, &HA, &HF, &H2, &H9, &H1, &H0, &HB, &H8, &H3},
                    New Byte() {&HC, &H7, &H8, &HF, &HB, &H0, &H5, &H9, &HD, &HA, &H6, &HE, &H2, &H4, &H3, &H1},
                    New Byte() {&H3, &HA, &HE, &H8, &H1, &HB, &H5, &H4, &H2, &HF, &HD, &HC, &H6, &H7, &H9, &H0},
                    New Byte() {&HC, &HD, &H1, &HF, &H8, &HE, &H5, &HB, &H3, &HA, &H9, &H0, &H7, &H2, &H4, &H6},
                    New Byte() {&HD, &HA, &H7, &HE, &H1, &H6, &HB, &H8, &HF, &HC, &H5, &H2, &H3, &H0, &H4, &H9},
                    New Byte() {&H3, &HE, &H7, &H5, &HB, &HF, &H8, &HC, &H1, &HA, &H4, &HD, &H0, &H6, &H9, &H2},
                    New Byte() {&HB, &H6, &H9, &H4, &H1, &H8, &HA, &HD, &H7, &HE, &H0, &HC, &HF, &H2, &H3, &H5},
                    New Byte() {&HC, &H7, &H8, &HD, &H3, &HB, &H0, &HE, &H6, &HF, &H9, &H4, &HA, &H1, &H5, &H2},
                    New Byte() {&HC, &H6, &HD, &H9, &HB, &H0, &H1, &H2, &HF, &H7, &H3, &H4, &HA, &HE, &H8, &H5},
                    New Byte() {&H3, &H6, &H1, &H5, &HB, &HC, &H8, &H0, &HF, &HE, &H9, &H4, &H7, &HA, &HD, &H2},
                    New Byte() {&HA, &H7, &HB, &HF, &H2, &H8, &H0, &HD, &HE, &HC, &H1, &H6, &H9, &H3, &H5, &H4},
                    New Byte() {&HA, &HB, &HD, &H4, &H3, &H8, &H5, &H9, &H1, &H0, &HF, &HC, &H7, &HE, &H2, &H6},
                    New Byte() {&HB, &H4, &HD, &HF, &H1, &H6, &H3, &HE, &H7, &HA, &HC, &H8, &H9, &H2, &H5, &H0},
                    New Byte() {&H9, &H6, &H7, &H0, &H1, &HA, &HD, &H2, &H3, &HE, &HF, &HC, &H5, &HB, &H4, &H8},
                    New Byte() {&HD, &HE, &H5, &H6, &H1, &H9, &H8, &HC, &H2, &HF, &H3, &H7, &HB, &H4, &H0, &HA},
                    New Byte() {&H9, &HF, &H4, &H0, &H1, &H6, &HA, &HE, &H2, &H3, &H7, &HD, &H5, &HB, &H8, &HC},
                    New Byte() {&H3, &HE, &H1, &HA, &H2, &HC, &H8, &H4, &HB, &H7, &HD, &H0, &HF, &H6, &H9, &H5},
                    New Byte() {&H7, &H2, &HC, &H6, &HA, &H8, &HB, &H0, &HF, &H4, &H3, &HE, &H9, &H1, &HD, &H5},
                    New Byte() {&HC, &H4, &H5, &H9, &HA, &H2, &H8, &HD, &H3, &HF, &H1, &HE, &H6, &H7, &HB, &H0},
                    New Byte() {&HA, &H8, &HE, &HD, &H9, &HF, &H3, &H0, &H4, &H6, &H1, &HC, &H7, &HB, &H2, &H5},
                    New Byte() {&H3, &HC, &H4, &HA, &H2, &HF, &HD, &HE, &H7, &H0, &H5, &H8, &H1, &H6, &HB, &H9},
                    New Byte() {&HA, &HC, &H1, &H0, &H9, &HE, &HD, &HB, &H3, &H7, &HF, &H8, &H5, &H2, &H4, &H6},
                    New Byte() {&HE, &HA, &H1, &H8, &H7, &H6, &H5, &HC, &H2, &HF, &H0, &HD, &H3, &HB, &H4, &H9},
                    New Byte() {&H3, &H8, &HE, &H0, &H7, &H9, &HF, &HC, &H1, &H6, &HD, &H2, &H5, &HA, &HB, &H4},
                    New Byte() {&H3, &HA, &HC, &H4, &HD, &HB, &H9, &HE, &HF, &H6, &H1, &H7, &H2, &H0, &H5, &H8}
                }

        Private ReadOnly digitMap As Dictionary(Of Char, Byte) = MakeDigitMap()
        Private Function MakeDigitMap() As Dictionary(Of Char, Byte)
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, Byte))() IsNot Nothing)
            Dim vals = New Dictionary(Of Char, Byte)
            Dim chars = "246789BCDEFGHJKMNPRTVWXYZ"
            For i = 0 To chars.Length - 1
                vals(chars(i)) = CByte(i)
            Next i
            Return vals
        End Function
#End Region

        '''<summary>Generates product credentials using a wc3 cd key.</summary>
        <Extension()> <Pure()>
        Public Function ToWC3CDKeyCredentials(ByVal key As String,
                                              ByVal clientSalt As IEnumerable(Of Byte),
                                              ByVal serverSalt As IEnumerable(Of Byte)) As ProductCredentials
            Contract.Requires(key IsNot Nothing)
            Contract.Requires(clientSalt IsNot Nothing)
            Contract.Requires(serverSalt IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ProductCredentials)() IsNot Nothing)

            'Normalize key
            key = key.ToUpperInvariant.Replace("-", "").Replace(" ", "")
            If key.Length < NumDigitsBase25 Then Throw New ArgumentException("Too short", "key")
            If key.Length > NumDigitsBase25 Then Throw New ArgumentException("Too long", "key")
            Dim badChars = From c In key Where Not digitMap.ContainsKey(c)
            If badChars.Any Then Throw New ArgumentException("Bad Char: '{0}'".Frmt(badChars.First), "key")

            'Map cd key characters into digits of a base 25 number
            Dim digits As IList(Of Byte) = (From c In key Select digitMap(c)).ToArray

            'Shuffle base 5 digits
            digits = digits.ConvertFromBaseToBase(25, 5).PaddedTo(minimumLength:=NumDigitsBase5)
            digits = (From i In Enumerable.Range(0, NumDigitsBase5)
                      Select If(i Mod 2 = 0, digits(i + 1), digits(i - 1))).ToArray
            digits = (From i In Enumerable.Range(0, NumDigitsBase5)
                      Select digits((10 + 17 * i) Mod NumDigitsBase5)).ToArray

            'Xor-Permute nibbles
            digits = digits.ConvertFromBaseToBase(5, 16).PaddedTo(minimumLength:=NumDigitsBase16)
            For i = NumDigitsBase16 - 1 To 0 Step -1
                Dim perm = nibblePermutations(i)
                Contract.Assume(perm IsNot Nothing)
                Dim c = perm(digits(i))

                'Xor-Permute
                For j = NumDigitsBase16 - 1 To 0 Step -1
                    If i = j Then Continue For
                    c = perm(perm(digits(j) Xor c))
                Next j

                digits(i) = c
            Next i

            'Swap bits
            digits = digits.ConvertFromBaseToBase(16, 2).PaddedTo(minimumLength:=NumDigitsBase2)
            digits = (From i In Enumerable.Range(0, NumDigitsBase2)
                      Select digits((i * 11) Mod NumDigitsBase2)).ToArray

            'Extract keys
            digits = digits.ConvertFromBaseToBase(2, 256).PaddedTo(minimumLength:=NumDigitsBase256)
            Dim product = CType({digits(13) >> &H2, CByte(0), CByte(0), CByte(0)}.ToUInt32, ProductType)
            Dim publicKey = {digits(10), digits(11), digits(12), CByte(0)}.ToUInt32
            Dim privateKey = {digits(8), digits(9),
                              digits(4), digits(5), digits(6), digits(7),
                              digits(0), digits(1), digits(2), digits(3)}
            Dim proof = {clientSalt,
                         serverSalt,
                         CUInt(product).Bytes,
                         publicKey.Bytes,
                         privateKey
                        }.Fold.SHA1

            Return New ProductCredentials(
                    product:=product,
                    publicKey:=publicKey,
                    length:=NumDigitsBase25,
                    proof:=proof)
        End Function
    End Module
End Namespace
