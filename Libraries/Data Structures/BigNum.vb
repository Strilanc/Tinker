Imports HostBot.Immutable

'''<summary>Immutable BigNum</summary>
Public Class BigNum
#Region "Members"
    Private ReadOnly words As ImmutableArrayView(Of UInteger)
    Private ReadOnly neg As Boolean

    Private Const WORD_SIZE As Integer = 32
    Private Const WORD_MASK As UInteger = UInteger.MaxValue

    Public Shared ReadOnly Zero As New BigNum(0)
    Public Shared ReadOnly Unit As New BigNum(1)
#End Region

#Region "New"
    Private Shared Function new_ulong_helper(ByVal n As ULong) As IList(Of UInteger)
        Dim m = 64 \ WORD_SIZE + 1
        Dim L = New List(Of UInteger)(m)
        For i = 0 To m
            L.Add(CUInt(n And WORD_MASK))
            n >>= WORD_SIZE
        Next i
        Return L
    End Function

    Private Sub New(ByVal n As ULong)
        Me.New(new_ulong_helper(n), False)
    End Sub
    Private Sub New(ByVal words As IList(Of UInteger), ByVal negative As Boolean)
        If Not (words IsNot Nothing) Then Throw New ArgumentException()

        Me.neg = negative

        Dim m As Integer
        For m = words.Count - 1 To 0 Step -1
            If words(m) <> 0 Then Exit For
        Next m
        If m < 0 Then Me.neg = False

        Me.words = New ImmutableArrayView(Of UInteger)(words.Take(m + 1))
    End Sub
    Private Sub New(ByVal words As ImmutableArrayView(Of UInteger), ByVal negative As Boolean)
        If Not (words IsNot Nothing) Then Throw New ArgumentException()

        Me.words = words
        Me.neg = negative And words.Count > 0
    End Sub
#End Region

#Region "Access"
    '''<summary>Gets/sets a value in the word array</summary>
    Private ReadOnly Property wordValue(ByVal index As Integer) As UInteger
        Get
            If index < 0 Then Return 0
            If index >= words.length Then Return 0
            Return words(index)
        End Get
    End Property

    Public ReadOnly Property abs() As BigNum
        Get

            Return If(Me < 0, -Me, Me)
        End Get
    End Property
    Public ReadOnly Property bit(ByVal i As Integer) As Boolean
        Get
            If i < 0 Then Return False
            Return CBool(wordValue(i \ WORD_SIZE) And (CUInt(1) << (i Mod WORD_SIZE)))
        End Get
    End Property

    '''<summary>Returns the minimum number of bits required to store this BigNum</summary>
    Public ReadOnly Property lg() As UInteger
        Get
            If neg Then Throw New InvalidOperationException("Negative lg")
            Dim n = (words.length - 1) * WORD_SIZE
            Dim u = wordValue(words.length - 1)
            While u <> 0
                u >>= 1
                n += 1
            End While
            If n < 0 Then Return 0
            Return CUInt(n)
        End Get
    End Property

    Public Shared Function gcd(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()


        If b2 = 0 Then Return b1
        Return gcd(b2, b1 Mod b2)
    End Function
    Public Shared Function lcm(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()


        lcm = (b1 * b2) \ gcd(b1, b2)
    End Function
    Public Shared Function gcdex(ByVal a As BigNum, ByVal b As BigNum) As Pair(Of BigNum, BigNum)
        If Not (a IsNot Nothing) Then Throw New ArgumentException()
        If Not (b IsNot Nothing) Then Throw New ArgumentException()

        Dim a_over_b_quot As BigNum
        Dim a_over_b_rem As BigNum
        With divMod(a, b)
            a_over_b_quot = .quotient
            a_over_b_rem = .remainder
        End With
        If a_over_b_rem = 0 Then
            Return New Pair(Of BigNum, BigNum)(0, 1)
        Else
            Dim x As BigNum, y As BigNum
            With gcdex(b, a_over_b_rem)
                x = .v1
                y = .v2
            End With
            Return New Pair(Of BigNum, BigNum)(y, x - (y * a_over_b_quot))
        End If
    End Function
    Public Function inverseMod(ByVal n As BigNum) As BigNum
        If Not (n IsNot Nothing) Then Throw New ArgumentException()
        inverseMod = gcdex(Me, n).v1
        If (inverseMod * Me) Mod n <> 1 Then Throw New OperationFailedException("Failed to computed modular inverse")
    End Function
    Public Function randomUniformUpTo(Optional ByVal rand As Random = Nothing, Optional ByVal allowEqual As Boolean = False, Optional ByVal allowZero As Boolean = True) As BigNum
        If (Not allowZero Or Not allowEqual) And Me = 0 Then Throw New ArgumentException("Invalid Range")
        If (Not allowZero And Not allowEqual) And Me = 1 Then Throw New ArgumentException("Invalid Range")
        If Not allowZero Then Return (Me - 1).randomUniformUpTo(rand, allowEqual, True) + 1
        If Not allowEqual Then Return (Me - 1).randomUniformUpTo(rand, True, allowZero)
        If rand Is Nothing Then rand = New Random()
        If Me = 0 Then Return 0

        Dim n = Zero
        Dim strictly_less_than = False
        For i = CInt(Me.lg()) To 0 Step -1
            If strictly_less_than Or Me.bit(i) Then
                Dim r = rand.Next(2)
                If r = 0 Then
                    strictly_less_than = True
                Else
                    n = n Or (Unit << i)
                End If
            End If
        Next i
        Return n
    End Function
#End Region

#Region "CType Operators"
    Public Shared Widening Operator CType(ByVal n As ULong) As BigNum
        If n = 0 Then Return Zero
        If n = 1 Then Return Unit
        Return New BigNum(n)
    End Operator
    Public Shared Narrowing Operator CType(ByVal b As BigNum) As ULong
        If Not (b IsNot Nothing) Then Throw New ArgumentException()

        If b < ULong.MinValue OrElse b > ULong.MaxValue Then
            Throw New ArgumentOutOfRangeException("b", "Value is outside of range of ULong")
        End If

        Dim n = CULng(0)
        For Each word In b.words.Reverse()
            n = (n << WORD_SIZE) + word
        Next word
        Return n
    End Operator

    Public Shared Widening Operator CType(ByVal n As Long) As BigNum
        If n < 0 Then Return -New BigNum(CULng(-n))
        Return CULng(n)
    End Operator
    Public Shared Narrowing Operator CType(ByVal b As BigNum) As Long
        If Not (b IsNot Nothing) Then Throw New ArgumentException()

        If b = Long.MinValue Then Return Long.MinValue
        If b < Long.MinValue OrElse b > Long.MaxValue Then
            Throw New ArgumentOutOfRangeException("b", "Value is outside of range of Long")
        End If
        Dim n = CLng(CULng(b.abs()))
        Return If(b.neg, -n, n)
    End Operator

    Public Shared Widening Operator CType(ByVal u As UInteger) As BigNum
        Return CULng(u)
    End Operator
    Public Shared Widening Operator CType(ByVal i As Integer) As BigNum
        Return CLng(i)
    End Operator
    Public Shared Narrowing Operator CType(ByVal b As BigNum) As UInteger
        If Not (b IsNot Nothing) Then Throw New ArgumentException()
        Return CUInt(CULng(b))
    End Operator
    Public Shared Narrowing Operator CType(ByVal b As BigNum) As Integer
        If Not (b IsNot Nothing) Then Throw New ArgumentException()
        Return CInt(CLng(b))
    End Operator
#End Region

#Region "Comparisons"
    Public Shared Operator <(ByVal b1 As BigNum, ByVal b2 As BigNum) As Boolean
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()

        If b1.neg And Not b2.neg Then Return True
        If b2.neg And Not b1.neg Then Return False
        If b1.words.length > b2.words.length Then Return b1.neg
        If b1.words.length < b2.words.length Then Return Not b1.neg
        For i = b1.words.length - 1 To 0 Step -1
            If b1.words(i) > b2.words(i) Then Return b1.neg
            If b1.words(i) < b2.words(i) Then Return Not b1.neg
        Next i
        Return False
    End Operator
    Public Shared Operator =(ByVal b1 As BigNum, ByVal b2 As BigNum) As Boolean
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()

        If b1.neg <> b2.neg Then Return False
        If b1.words.length <> b2.words.length Then Return False
        For i = 0 To b1.words.length - 1
            If b1.words(i) <> b2.words(i) Then Return False
        Next i
        Return True
    End Operator
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        If obj Is Nothing Then Return False
        If TypeOf obj Is BigNum Then Return CType(obj, BigNum) = Me
        Return False
    End Function

    Public Shared Operator <>(ByVal b1 As BigNum, ByVal b2 As BigNum) As Boolean
        Return Not b1 = b2
    End Operator
    Public Shared Operator <=(ByVal b1 As BigNum, ByVal b2 As BigNum) As Boolean
        Return Not b2 < b1
    End Operator
    Public Shared Operator >=(ByVal b1 As BigNum, ByVal b2 As BigNum) As Boolean
        Return Not b1 < b2
    End Operator
    Public Shared Operator >(ByVal b1 As BigNum, ByVal b2 As BigNum) As Boolean
        Return b2 < b1
    End Operator
#End Region

#Region "Operators"
    '''<summary>Returns the negation of a bignum.</summary>
    Public Shared Operator -(ByVal b As BigNum) As BigNum
        If Not (b IsNot Nothing) Then Throw New ArgumentException()

        Return New BigNum(b.words, Not b.neg)
    End Operator

    '''<summary>Returns the sum of two BigNums,</summary>
    Public Shared Operator +(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()


        If b1 = 0 Then Return b2
        If b2 = 0 Then Return b1
        If b1.neg <> b2.neg Then Return b1 - -b2

        Dim neg = b1.neg
        Dim words As New List(Of UInteger)

        Dim carry = CULng(0)
        Dim max = Math.Max(b1.words.length, b2.words.length)
        Dim i = 0
        Do Until carry = 0 AndAlso i >= max
            Dim sum = CULng(b1.wordValue(i)) + CULng(b2.wordValue(i)) + carry
            carry = sum >> WORD_SIZE
            words.Add(CUInt(sum And WORD_MASK))
            i += 1
        Loop
        Return New BigNum(words, neg)
    End Operator
    '''<summary>Returns the difference between two BigNums</summary>
    Public Shared Operator -(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()


        If b1 = 0 Then Return -b2
        If b2 = 0 Then Return b1
        If b1.abs < b2.abs Then Return -(b2 + -b1)
        If b1.neg <> b2.neg Then Return b1 + -b2

        Dim neg = b1.neg
        Dim words As New List(Of UInteger)(b1.words.length - 1) 'b1.maxWord >= b2.maxWord

        Dim carry = CLng(0)
        Dim i = 0
        Do Until carry = 0 AndAlso i >= b1.words.length
            Dim dif = CLng(b1.wordValue(i)) - CLng(b2.wordValue(i)) - carry
            carry = If(dif < 0, 1, 0)
            words.Add(CUInt(dif And WORD_MASK))
            i += 1
        Loop
        Return New BigNum(words, neg)
    End Operator

    '''<summary>Returns the product of two BigNums</summary>
    Public Shared Operator *(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()


        Dim neg = b1.neg Xor b2.neg
        b1 = b1.abs()
        b2 = b2.abs()
        Dim p = Zero
        For Each word In b1.words
            p += b2 * word
            b2 <<= WORD_SIZE
        Next word
        Return If(neg, -p, p)
    End Operator
    '''<summary>Multiplies this bignum by the given word</summary>
    Public Shared Operator *(ByVal b As BigNum, ByVal u As UInteger) As BigNum
        If Not (b IsNot Nothing) Then Throw New ArgumentException()


        Dim carry = CULng(0)
        Dim i = 0
        Dim ul = CULng(u)
        Dim words = New List(Of UInteger)
        Do Until carry = 0 AndAlso i >= b.words.length
            Dim product = CULng(b.wordValue(i)) * ul + carry
            carry = product >> WORD_SIZE
            words.Add(CUInt(product And WORD_MASK))
            i += 1
        Loop
        Return New BigNum(words, b.neg)
    End Operator
    '''<summary>Returns the AND of two BigNums</summary>
    Public Shared Operator And(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()

        Dim words = New List(Of UInteger)()
        Dim neg = b1.neg And b2.neg
        Dim i = 0
        Do Until i >= b1.words.length Or i >= b2.words.length
            words.Add(b1.words(i) And b2.words(i))
            i += 1
        Loop
        Return New BigNum(words, neg)
    End Operator
    '''<summary>Returns the OR of two BigNums</summary>
    Public Shared Operator Or(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()

        Dim words = New List(Of UInteger)()
        Dim neg = b1.neg Or b2.neg
        Dim i = 0
        Do Until i >= b1.words.length Or i >= b2.words.length
            words.Add(b1.words(i) Or b2.words(i))
            i += 1
        Loop
        Do Until i >= b1.words.length
            words.Add(b1.wordValue(i))
            i += 1
        Loop
        Do Until i >= b2.words.length
            words.Add(b2.wordValue(i))
            i += 1
        Loop
        Return New BigNum(words, neg)
    End Operator
    '''<summary>Returns the XOR of two BigNums</summary>
    Public Shared Operator Xor(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()


        Dim words = New List(Of UInteger)()
        Dim neg = b1.neg Or b2.neg
        Dim i = 0
        Do Until i >= b1.words.length Or i >= b2.words.length
            words.Add(b1.words(i) Xor b2.words(i))
            i += 1
        Loop
        Do Until i >= b1.words.length
            words.Add(b1.wordValue(i))
            i += 1
        Loop
        Do Until i >= b2.words.length
            words.Add(b2.wordValue(i))
            i += 1
        Loop
        Return New BigNum(words, neg)
    End Operator
    '''<summary>Returns the given BigNum left-shifted by 'offset' bits</summary>
    Public Shared Operator <<(ByVal b As BigNum, ByVal offset As Integer) As BigNum
        If Not (b IsNot Nothing) Then Throw New ArgumentException()


        If offset = 0 Then Return b
        If offset < 0 Then Return b >> -offset

        Dim wordDif = offset \ WORD_SIZE
        Dim bitDif = offset Mod WORD_SIZE
        Dim lowMask = WORD_MASK >> bitDif
        Dim highMask = Not lowMask

        Dim words As New List(Of UInteger)
        For i = 0 To wordDif - 1
            words.Add(0)
        Next i
        For i = wordDif To b.words.length + wordDif
            Dim low = (b.wordValue(i - wordDif) And lowMask) << bitDif
            Dim high = (b.wordValue(i - wordDif - 1) And highMask) >> (WORD_SIZE - bitDif)
            words.Add(high Or low)
        Next i
        Return New BigNum(words, b.neg)
    End Operator
    '''<summary>Returns the given BigNum right-shifted by 'offset' bits</summary>
    Public Shared Operator >>(ByVal b As BigNum, ByVal offset As Integer) As BigNum
        If Not (b IsNot Nothing) Then Throw New ArgumentException()


        If offset = 0 Then Return b
        If offset < 0 Then Return b << -offset
        If offset > b.lg Then Return Zero

        Dim wordDif = offset \ WORD_SIZE
        Dim bitDif = offset Mod WORD_SIZE
        Dim lowMask = CUInt((1 << bitDif) - 1)
        Dim highMask = Not lowMask

        Dim words = New List(Of UInteger)
        For i = 0 To b.words.length - 1
            Dim low = (b.wordValue(i + wordDif + 1) And lowMask) << (WORD_SIZE - bitDif)
            Dim high = (b.wordValue(i + wordDif) And highMask) >> bitDif
            words.Add(low Or high)
        Next i
        Return New BigNum(words, b.neg)
    End Operator
    '''<summary>Returns the quotient of b1\b2</summary>
    Public Shared Operator \(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()

        Return divMod(b1, b2).quotient
    End Operator
    '''<summary>Returns the remainder of b1\b2</summary>
    Public Shared Operator Mod(ByVal b1 As BigNum, ByVal b2 As BigNum) As BigNum
        If b1 Is Nothing Then Throw New ArgumentException()
        If b2 Is Nothing Then Throw New ArgumentException()


        Return divMod(b1, b2).remainder
    End Operator
#End Region

#Region "Mod Ops"
    Public Class DivModResult
        Public ReadOnly quotient As BigNum
        Public ReadOnly remainder As BigNum

        Public Sub New(ByVal quotient As BigNum, ByVal remainder As BigNum)
            If Not (quotient IsNot Nothing) Then Throw New ArgumentException()
            If Not (remainder IsNot Nothing) Then Throw New ArgumentException()

            Me.quotient = quotient
            Me.remainder = remainder
        End Sub
    End Class
    '''<summary>Returns the quotient and remainder for the given numerator and denominator.</summary>
    Public Shared Function divMod(ByVal numerator As BigNum, ByVal denominator As BigNum) As DivModResult
        If Not (numerator IsNot Nothing) Then Throw New ArgumentException()
        If Not (denominator IsNot Nothing) Then Throw New ArgumentException()
        If Not (denominator <> 0) Then Throw New ArgumentException()


        If denominator = 1 Then Return New DivModResult(numerator, 0)
        If numerator = denominator Then Return New DivModResult(1, 0)

        If numerator < 0 Then
            With divMod(-numerator, denominator)
                If .remainder = 0 Then
                    Return New DivModResult(-.quotient, .remainder)
                Else
                    Return New DivModResult(-.quotient - 1, denominator - .remainder)
                End If
            End With
        ElseIf denominator < 0 Then
            With divMod(numerator, -denominator)
                Return New DivModResult(-.quotient, .remainder)
            End With
        End If
        If numerator < denominator Then Return New DivModResult(0, numerator)

        Dim quotient = Zero
        Dim remainder = numerator
        Dim m = denominator << CInt(numerator.lg() - denominator.lg())
        If m > numerator Then m >>= 1

        While remainder >= denominator
            remainder -= m
            quotient += 1
            While remainder < m AndAlso denominator < m
                m >>= 1
                quotient <<= 1
            End While
        End While
        Return New DivModResult(quotient, remainder)
    End Function

    '''<summary>Returns a BigNum equal to this BigNum raised to the power p, mod m.</summary>
    Public Function powerMod(ByVal p As BigNum, ByVal m As BigNum) As BigNum
        If Not (p IsNot Nothing) Then Throw New ArgumentException()
        If Not (m IsNot Nothing) Then Throw New ArgumentException()


        If Me.neg Or p.neg Or m.neg Then Throw New ArgumentException("All arguments to powerMod must be non-negative.")
        If m = 0 Then Throw New DivideByZeroException()
        If m = 1 Then Return 0
        If p = 0 Then Return 1

        Dim factor = Me Mod m
        Dim total = Unit
        For i = 0 To CInt(p.lg()) - 1
            If p.bit(i) Then total = (total * factor) Mod m
            factor = (factor * factor) Mod m
        Next i

        Return total
    End Function
#End Region

#Region "Base Conversions"
    Public Shared Function fromBase(ByVal L As IEnumerable(Of UInteger), ByVal base As UInteger, Optional ByVal BigEndian As Boolean = False) As BigNum
        If Not (L IsNot Nothing) Then Throw New ArgumentException()
        If Not (base >= 2) Then Throw New ArgumentException()


        Dim n = Zero
        If Not BigEndian Then L = L.Reverse()
        For Each e In L
            n *= base
            If e >= base Then Throw New ArgumentException("A digit was larger than the base")
            n += e
        Next e
        Return n
    End Function
    Public Shared Function fromBaseBytes(ByVal L As IEnumerable(Of Byte), ByVal base As UInteger, Optional ByVal BigEndian As Boolean = False) As BigNum
        If Not (L IsNot Nothing) Then Throw New ArgumentException()
        If Not (base >= 2) Then Throw New ArgumentException()


        Dim Lu As New List(Of UInteger)
        For Each e In L
            Lu.Add(e)
        Next e
        Return fromBase(Lu, base, BigEndian)
    End Function
    Public Shared Function fromBytes(ByVal L As IEnumerable(Of Byte), Optional ByVal BigEndian As Boolean = False) As BigNum
        If Not (L IsNot Nothing) Then Throw New ArgumentException()


        Return fromBaseBytes(L, 256, BigEndian)
    End Function

    Public Function toBase(ByVal base As UInteger, Optional ByVal BigEndian As Boolean = False) As IEnumerable(Of UInteger)
        If Not (base >= 2) Then Throw New ArgumentException()


        Dim L As New List(Of UInteger)
        Dim d As New DivModResult(Me.abs(), 0)
        While d.quotient > 0
            d = BigNum.divMod(d.quotient, base)
            L.Add(CUInt(d.remainder))
        End While
        Dim Li = CType(L, IEnumerable(Of UInteger))
        If BigEndian Then Li = Li.Reverse()
        Return Li
    End Function
    Public Function toBaseBytes(ByVal base As UInteger, Optional ByVal BigEndian As Boolean = False) As List(Of Byte)

        If Not (base >= 2) Then Throw New ArgumentException()

        If base > 256 Then Throw New ArgumentOutOfRangeException("base")
        Dim L = toBase(base, BigEndian)
        Dim Lb As New List(Of Byte)
        For Each u In L
            Lb.Add(CByte(u))
        Next u
        Return Lb
    End Function
    Public Function toBytes(Optional ByVal BigEndian As Boolean = False) As List(Of Byte)


        Return toBaseBytes(256, BigEndian)
    End Function

    '''<summary>Returns a decimal representation of this number</summary>
    Public ReadOnly Property toDecimal() As String
        Get
            Return toString(10)
        End Get
    End Property
    '''<summary>Returns a binary representation of this number</summary>
    Public ReadOnly Property toBinary() As String
        Get
            Return toString(2)
        End Get
    End Property
    '''<summary>Returns a hexadecimal representation of this number</summary>
    Public ReadOnly Property toHex() As String
        Get
            Return toString(16)
        End Get
    End Property

    '''<summary>Returns a string representation of this number.</summary>
    Public Shadows Function toString(ByVal base As UInteger, Optional ByVal BigEndian As Boolean = True) As String
        If base < 2 Or base > 36 Then Throw New ArgumentOutOfRangeException("base", "base must be in [2,36]")
        If Me = 0 Then Return "0"
        If Me < 0 Then Return "-" + Me.abs().toString(base)
        Dim digits = toBase(base, BigEndian)
        Dim s As New System.Text.StringBuilder()
        For i = 0 To digits.Count - 1
            Select Case digits(i)
                Case 0 To 9
                    s.Append(digits(i).ToString)
                Case 10 To 35
                    s.Append(Chr(CByte(Asc("A"c) + digits(i) - 10)))
                Case Else
                    s.Append("?")
            End Select
        Next i
        Return s.ToString
    End Function
    Public Shared Function fromString(ByVal s As String, ByVal base As UInteger, Optional ByVal BigEndian As Boolean = True) As BigNum
        If base < 2 Or base > 36 Then Throw New ArgumentOutOfRangeException("base", "base must be in [2,36]")
        Dim L As New List(Of UInteger)
        For Each c In s
            Select Case c
                Case "0"c To "9"c
                    L.Add(Byte.Parse(c))
                Case "A"c To "Z"c
                    L.Add(CUInt(Asc(c) - Asc("A") + 10))
                Case "a"c To "z"c
                    L.Add(CUInt(Asc(c) - Asc("a") + 10))
                Case Else
                    Throw New ArgumentException("Invalid string.")
            End Select
        Next c
        Return fromBase(L, base, BigEndian)
    End Function
#End Region

#Region "Testing"
    Friend Shared Sub run_tests(ByVal rand As Random)
        Debug.Print("Testing BigNum")
        Dim b = BigNum.fromString("B3500005D3AF30059ED523B65CCE3C442710DA2C566985346AD4835F1E122338", 16)
        Dim p = BigNum.fromString("BEE2CA68607F273D7C5A53196CB0B8E5E4A92CA677E6841D1ECBAF0CB0A85F15", 16)
        Dim m = BigNum.fromString("F8FF1A8B619918032186B68CA092B5557E976C78C73212D91216F6658523C787", 16)
        Dim r = BigNum.fromString("A0B0FBE45B9679E962D87055524385E122C70011D6D4636624A690741A381171", 16)
        If b <> b Then Throw New Exception("Incorrect result.")
        If b.powerMod(p, m) <> r Then Throw New Exception("Incorrect result.")
        If b + r <> r + b Then Throw New Exception("Incorrect result.")
        If b * r <> r * b Then Throw New Exception("Incorrect result.")
        If -(-b) <> b Then Throw New Exception("Incorrect result.")
        If b - r <> -(r - b) Then Throw New Exception("Incorrect result.")
        If b - r <> b + -r Then Throw New Exception("Incorrect result.")
        If (p * r) \ r <> p Then Throw New Exception("Incorrect result.")
        If (p * r) Mod r <> 0 Then Throw New Exception("Incorrect result.")
        If (p * r) * b <> p * (r * b) Then Throw New Exception("Incorrect result.")
        If (p + r) + b <> p + (r + b) Then Throw New Exception("Incorrect result.")
        If (p + r) * b <> p * b + r * b Then Throw New Exception("Incorrect result.")
        If p * p <= p Then Throw New Exception("Incorrect result.")
        If p + p <= p Then Throw New Exception("Incorrect result.")
        If p - p <> 0 Then Throw New Exception("Incorrect result.")
        If p \ p <> 1 Then Throw New Exception("Incorrect result.")
        If p + p <> 2 * p Then Throw New Exception("Incorrect result.")
        Debug.Print("BigNum Passed")
    End Sub
#End Region
End Class