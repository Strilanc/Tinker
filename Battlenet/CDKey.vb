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
    Public NotInheritable Class CDKey
        Private ReadOnly _key As String
        Private ReadOnly _product As CDKeyProduct
        Private ReadOnly _privateKey As ViewableList(Of Byte)
        Private ReadOnly _publicKey As UInteger
        Public ReadOnly Property Key As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _key
            End Get
        End Property
        ''' <summary>
        ''' Determines the product the CD Key applies to.
        ''' This value can be revealed to others without compromising the CD Key.
        ''' </summary>
        Public ReadOnly Property Product As CDKeyProduct
            Get
                Return _product
            End Get
        End Property
        ''' <summary>
        ''' Identifies the CD Key.
        ''' This value can be revealed to others without compromising the CD Key.
        ''' </summary>
        Public ReadOnly Property PublicKey As UInteger
            Get
                Return _publicKey
            End Get
        End Property
        ''' <summary>
        ''' Authenticates the CD Key.
        ''' Do not share this value with others!
        ''' </summary>
        Public ReadOnly Property PrivateKey As ViewableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
                Return _privateKey
            End Get
        End Property

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

#Region "WC3 Data"
        Private Const NumDigitsBase2 As Integer = 120
        Private Const NumDigitsBase16 As Integer = 30
        Private Const NumDigitsBase256 As Integer = 15
        Private Const NumDigitsBase25 As Integer = 26
        Private Const NumDigitsBase5 As Integer = NumDigitsBase25 * 2
        Private Shared ReadOnly keyMap As Dictionary(Of Char, Byte) = MakeKeyMap()
        Private Shared ReadOnly invKeyMap As Dictionary(Of Byte, Char) = MakeInvKeyMap()
        '''<summary>30 permutations of 0-15</summary>
        Private Shared ReadOnly permutationSet As Byte()() = {
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
        Private Shared ReadOnly invPermutationSet As Byte()() = MakeInvPermMap()

        Private Shared Function MakeKeyMap() As Dictionary(Of Char, Byte)
            Dim vals = New Dictionary(Of Char, Byte)
            Dim chars = "246789BCDEFGHJKMNPRTVWXYZ"
            For b = 0 To chars.Length - 1
                vals(chars(b)) = CByte(b)
            Next b
            Return vals
        End Function
        Private Shared Function MakeInvKeyMap() As Dictionary(Of Byte, Char)
            If keyMap Is Nothing Then Throw New InvalidOperationException("Key map must be initialized before inverse key map")
            Dim vals As New Dictionary(Of Byte, Char)
            For Each e In keyMap.Keys
                vals(keyMap(e)) = e
            Next e
            Return vals
        End Function
        Private Shared Function MakeInvPermMap() As Byte()()
            Dim invPermSet(0 To 30 - 1)() As Byte
            For i = 0 To 30 - 1
                ReDim invPermSet(i)(0 To 16 - 1)
                For j = 0 To 16 - 1
                    invPermSet(i)(permutationSet(i)(j)) = CByte(j)
                Next j
            Next i
            Return invPermSet
        End Function

        Private Shared Function Shuffle(ByVal data As IList(Of Byte),
                                        ByVal initialOffset As Integer,
                                        ByVal cumulativeOffset As Integer,
                                        ByVal invert As Boolean) As Byte()
            Dim result(0 To data.Count - 1) As Byte
            Dim o = initialOffset
            For i = 0 To data.Count - 1 Step 2
                For j = i + 1 To i Step -1
                    o = (o + cumulativeOffset) Mod data.Count
                    If invert Then
                        result(j) = data(o)
                    Else
                        result(o) = data(j)
                    End If
                Next j
            Next i
            Return result
        End Function
#End Region

        Public Shared Function FromWC3StyleKey(ByVal key As String) As CDKey
            Contract.Requires(key IsNot Nothing)
            key = key.ToUpperInvariant.Replace("-", "").Replace(" ", "")

            'Map cd key characters into digits of a base 25 number
            If key.Length <> NumDigitsBase25 Then Throw New ArgumentException("Invalid cd key length.")
            Dim digits As IList(Of Byte) = (From c In key Select keyMap(c)).ToArray

            'Shuffle base5 digits
            digits = digits.ConvertFromBaseToBase(25, 5, minOutputLength:=NumDigitsBase5)
            digits = Shuffle(digits, 33, 49, invert:=False)

            'Permute nibbles
            digits = digits.ConvertFromBaseToBase(5, 16, minOutputLength:=NumDigitsBase16).ToArray
            For r = NumDigitsBase16 - 1 To 0 Step -1
                Dim perm = permutationSet(r)
                Contract.Assume(perm IsNot Nothing)
                Dim c = digits(r)

                'Permute
                For r2 = NumDigitsBase16 - 1 To 0 Step -1
                    If r = r2 Then Continue For
                    c = perm(digits(r2) Xor perm(c))
                Next r2

                digits(r) = perm(c)
            Next r

            'Swap bits
            digits = digits.ConvertFromBaseToBase(16, 2, minOutputLength:=NumDigitsBase2).ToArray
            digits = (From i In Enumerable.Range(0, NumDigitsBase2)
                      Select digits((i * 11) Mod NumDigitsBase2)).ToArray

            'Extract keys
            digits = digits.ConvertFromBaseToBase(2, 256, minOutputLength:=NumDigitsBase256).ToArray
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

            'Inject keys
            Dim publicKeyBytes = publicKey.Bytes
            Dim productBytes = CUInt(product).Bytes
            Dim digits = New Byte() {privateKey(6), privateKey(7), privateKey(8), privateKey(9),
                                     privateKey(2), privateKey(3), privateKey(4), privateKey(5),
                                     privateKey(0), privateKey(1), publicKeyBytes(0), publicKeyBytes(1),
                                     publicKeyBytes(2), productBytes(0) << 2}

            'Swap bits
            digits = digits.ConvertFromBaseToBase(256, 2, minOutputLength:=NumDigitsBase2).ToArray
            digits = (From i In Enumerable.Range(0, NumDigitsBase2)
                      Select digits((i * 11) Mod NumDigitsBase2)).ToArray

            'Unpermute nibbles
            digits = digits.ConvertFromBaseToBase(2, 16, minOutputLength:=NumDigitsBase16).ToArray
            For r = 0 To NumDigitsBase16 - 1
                Dim unperm = invPermutationSet(r)
                Dim c = unperm(digits(r))

                'Unpermute
                For r2 = 0 To NumDigitsBase16 - 1
                    If r = r2 Then Continue For
                    c = unperm(digits(r2) Xor unperm(c))
                Next r2

                digits(r) = c
            Next r

            'Shuffle base 5 digits
            digits = digits.ConvertFromBaseToBase(16, 5, minOutputLength:=NumDigitsBase5).ToArray
            digits = Shuffle(digits, 33, 49, invert:=True)

            'Map base 25 digits into cd key
            digits = digits.ConvertFromBaseToBase(5, 25, minOutputLength:=NumDigitsBase25).ToArray
            Dim cdkey = New String((From digit In digits
                                    Select invKeyMap(digit)).ToArray).ToUpperInvariant

            Return New CDKey(cdkey, product, publicKey, privateKey.ToView)
        End Function
    End Class
End Namespace
