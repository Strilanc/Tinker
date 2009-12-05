Namespace Bnet
    Public Enum CDKeyProduct As UInt32
        Warcraft3ROC = 14
        Warcraft3TFT = 18
    End Enum

    ''' <summary>
    ''' Stores the encoded and decoded forms of a CDKey.
    ''' </summary>
    ''' <remarks>
    ''' Blizzard protects cd keys by splitting them into public and private parts.
    ''' The public part is probably just incremented for each new key, while the private part is generated randomly.
    ''' The public part identifies which key, while the private part authenticates the key.
    ''' Therefore it is impossible to create a true key generator, because the private data is not knowable from the public data.
    ''' </remarks>
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class CDKey
#Region "Data"
        Private Const NumDigitsBase2 As Integer = 120
        Private Const NumDigitsBase16 As Integer = 30
        Private Const NumDigitsBase256 As Integer = 15
        Private Const NumDigitsBase25 As Integer = 26
        Private Const NumDigitsBase5 As Integer = NumDigitsBase25 * 2
        Private Shared ReadOnly digitMap As Dictionary(Of Char, Byte) = MakeDigitMap()
        Private Shared ReadOnly inverseDigitMap As Dictionary(Of Byte, Char) = MakeInverseDigitMap()
        Private Shared ReadOnly nibblePermutations As Byte()() = {
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
        Private Shared ReadOnly inverseNibblePermutations As Byte()() = MakeInversePermutationMap()

        Private Shared Function MakeDigitMap() As Dictionary(Of Char, Byte)
            Contract.Ensures(Contract.Result(Of Dictionary(Of Char, Byte))() IsNot Nothing)
            Dim vals = New Dictionary(Of Char, Byte)
            Dim chars = "246789BCDEFGHJKMNPRTVWXYZ"
            For i = 0 To chars.Length - 1
                vals(chars(i)) = CByte(i)
            Next i
            Return vals
        End Function
        Private Shared Function MakeInverseDigitMap() As Dictionary(Of Byte, Char)
            Contract.Ensures(Contract.Result(Of Dictionary(Of Byte, Char))() IsNot Nothing)
            If digitMap Is Nothing Then Throw New InvalidOperationException("Key map must be initialized before inverse key map")
            Dim vals = New Dictionary(Of Byte, Char)
            For Each e In digitMap.Keys
                vals(digitMap(e)) = e
            Next e
            Return vals
        End Function
        Private Shared Function MakeInversePermutationMap() As Byte()()
            Contract.Ensures(Contract.Result(Of Byte()())() IsNot Nothing)
            Dim inversePermSet(0 To 30 - 1)() As Byte
            For i = 0 To 30 - 1
                ReDim inversePermSet(i)(0 To 16 - 1)
                Contract.Assume(nibblePermutations(i) IsNot Nothing)
                For j = 0 To 16 - 1
                    inversePermSet(i)(nibblePermutations(i)(j)) = CByte(j)
                Next j
            Next i
            Return inversePermSet
        End Function
#End Region

        Private ReadOnly _key As String
        Private ReadOnly _product As CDKeyProduct
        Private ReadOnly _privateKey As ViewableList(Of Byte)
        Private ReadOnly _publicKey As UInteger

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_key IsNot Nothing)
            Contract.Invariant(_privateKey IsNot Nothing)
        End Sub

        Private Sub New(ByVal key As String,
                        ByVal product As CDKeyProduct,
                        ByVal publicKey As UInteger,
                        ByVal privateKey As ViewableList(Of Byte))
            Contract.Requires(key IsNot Nothing)
            Contract.Requires(privateKey IsNot Nothing)
            Me._key = key
            Me._product = product
            Me._publicKey = publicKey
            Me._privateKey = privateKey
        End Sub

        Public ReadOnly Property Key As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _key
            End Get
        End Property
        '''<summary>Determines the product the CD Key applies to.</summary>
        '''<remarks>This value can be revealed to others without compromising the CD Key.</remarks>
        Public ReadOnly Property Product As CDKeyProduct
            Get
                Return _product
            End Get
        End Property
        '''<summary>Identifies the CD Key.</summary>
        '''<remarks>This value can be revealed to others without compromising the CD Key.</remarks>
        Public ReadOnly Property PublicKey As UInteger
            Get
                Return _publicKey
            End Get
        End Property
        '''<summary>Authenticates the CD Key.</summary>
        '''<remarks>Do not share this value with others!</remarks>
        Public ReadOnly Property PrivateKey As ViewableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
                Return _privateKey
            End Get
        End Property

        Public Shared Function FromWC3StyleKey(ByVal key As String) As CDKey
            Contract.Requires(key IsNot Nothing)
            Contract.Ensures(Contract.Result(Of CDKey)() IsNot Nothing)

            'Map cd key characters into digits of a base 25 number
            key = key.ToUpperInvariant.Replace("-", "").Replace(" ", "")
            If key.Length < NumDigitsBase25 Then Throw New ArgumentException("Too short", "key")
            If key.Length > NumDigitsBase25 Then Throw New ArgumentException("Too long", "key")
            If (From c In key Where Not digitMap.ContainsKey(c)).Any Then
                Throw New ArgumentException("Bad Char: '{0}'".Frmt((From c In key Where Not digitMap.ContainsKey(c)).First), "key")
            End If
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
            Dim product = CType({digits(13) >> &H2, CByte(0), CByte(0), CByte(0)}.ToUInt32, CDKeyProduct)
            Dim publicKey = {digits(10), digits(11), digits(12), CByte(0)}.ToUInt32
            Dim privateKey = {digits(8), digits(9),
                              digits(4), digits(5), digits(6), digits(7),
                              digits(0), digits(1), digits(2), digits(3)}.ToView
            Return New CDKey(key, product, publicKey, privateKey)
        End Function

        Public Shared Function ToWC3StyleKey(ByVal product As CDKeyProduct,
                                             ByVal publicKey As UInteger,
                                             ByVal privateKey As Byte()) As CDKey
            Contract.Requires(privateKey IsNot Nothing)
            Contract.Ensures(Contract.Result(Of CDKey)() IsNot Nothing)

            'Inject keys
            Dim publicKeyBytes = publicKey.Bytes
            Dim productBytes = CUInt(product).Bytes
            Dim digits As IList(Of Byte) = {privateKey(6), privateKey(7), privateKey(8), privateKey(9),
                                            privateKey(2), privateKey(3), privateKey(4), privateKey(5),
                                            privateKey(0), privateKey(1), publicKeyBytes(0), publicKeyBytes(1),
                                            publicKeyBytes(2), productBytes(0) << 2}

            'Swap bits
            digits = digits.ConvertFromBaseToBase(256, 2).PaddedTo(minimumLength:=NumDigitsBase2)
            digits = (From i In Enumerable.Range(0, NumDigitsBase2)
                      Select digits((i * 11) Mod NumDigitsBase2)).ToArray

            'Un-xor-permute nibbles
            digits = digits.ConvertFromBaseToBase(2, 16).PaddedTo(minimumLength:=NumDigitsBase16)
            For r = 0 To NumDigitsBase16 - 1
                Dim unperm = inverseNibblePermutations(r)
                Contract.Assume(unperm IsNot Nothing)
                Dim c = digits(r)

                'Un-xor-permute
                For r2 = 0 To NumDigitsBase16 - 1
                    If r = r2 Then Continue For
                    c = digits(r2) Xor unperm(unperm(c))
                Next r2

                digits(r) = unperm(c)
            Next r

            'Shuffle base 5 digits
            digits = digits.ConvertFromBaseToBase(16, 5).PaddedTo(minimumLength:=NumDigitsBase5)
            digits = (From i In Enumerable.Range(0, NumDigitsBase5)
                      Select digits((30 + 49 * i) Mod NumDigitsBase5)).ToArray
            digits = (From i In Enumerable.Range(0, NumDigitsBase5)
                      Select If(i Mod 2 = 0, digits(i + 1), digits(i - 1))).ToArray

            'Map base 25 digits into cd key
            digits = digits.ConvertFromBaseToBase(5, 25).PaddedTo(minimumLength:=NumDigitsBase25)
            Dim cdkey = New String((From digit In digits
                                    Select inverseDigitMap(digit)).ToArray).ToUpperInvariant

            Return New CDKey(cdkey, product, publicKey, privateKey.ToView)
        End Function

        Public Overrides Function ToString() As String
            Return "{0}: product = {1}, public = {2}".Frmt(Me.Key, Me.Product, Me.PublicKey)
        End Function
    End Class
End Namespace
