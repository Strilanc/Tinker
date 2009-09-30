'''<summary>Immutable BigNum</summary>
<ContractVerification(False)>
<DebuggerDisplay("{ToDecimal}")>
Public Class BigNum
    Implements IComparable(Of BigNum)

    Private ReadOnly _words As UInteger()
    Private ReadOnly Property words As UInteger()
        Get
            Contract.Ensures(Contract.Result(Of UInteger())() IsNot Nothing)
            Contract.Ensures((Contract.Result(Of UInteger()).Length = 0) = (Sign() = 0))
            Return _words
        End Get
    End Property
    Private ReadOnly _sign As Integer
    Public ReadOnly Property Sign() As Integer
        Get
            Contract.Ensures(Contract.Result(Of Integer)() >= -1)
            Contract.Ensures(Contract.Result(Of Integer)() <= 1)
            Contract.Ensures((Contract.Result(Of Integer)() = 0) = (words.Length = 0))
            Return _sign
        End Get
    End Property

    Private Const WORD_SIZE As Integer = 32
    Private Const WORD_MASK As UInteger = UInteger.MaxValue

    Public Shared ReadOnly Zero As New BigNum(0)
    Public Shared ReadOnly Unit As New BigNum(1)

    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_words IsNot Nothing)
        Contract.Invariant(_sign >= -1)
        Contract.Invariant(_sign <= 1)
        Contract.Invariant((_sign = 0) = (_words.Length = 0))
    End Sub

#Region "New"
    Private Shared Function GetULongWords(ByVal n As ULong) As List(Of UInteger)
        Contract.Ensures(Contract.Result(Of List(Of UInteger))() IsNot Nothing)
        Dim m = 64 \ WORD_SIZE + 1
        Dim L = New List(Of UInteger)(m)
        For i = 0 To m
            L.Add(CUInt(n And WORD_MASK))
            n >>= WORD_SIZE
        Next i
        Return L
    End Function

    Public Sub New(ByVal value As ULong, Optional ByVal negative As Boolean = False)
        Me.New(GetULongWords(value), If(negative, -1, 1))
    End Sub
    Private Sub New(ByVal words As IList(Of UInteger), ByVal sign As Integer)
        Contract.Requires(words IsNot Nothing)
        If sign = 0 Then
            Me._words = New UInteger() {}
        Else
            Dim m As Integer
            For m = words.Count To 1 Step -1
                If words(m - 1) <> 0 Then Exit For
            Next m

            Me._words = words.SubToArray(0, m)
            Me._sign = If(Me._words.Length = 0, 0, Math.Sign(sign))
        End If
    End Sub
    Private Sub New(ByVal exactWords As UInteger(), ByVal sign As Integer)
        Contract.Requires(exactWords IsNot Nothing)
        Contract.Requires(sign >= -1)
        Contract.Requires(sign <= 1)
        Contract.Requires((exactWords.Length = 0) = (sign = 0))
        Me._words = exactWords
        Me._sign = sign
    End Sub
#End Region

#Region "Access"
    '''<summary>Gets/sets a value in the word array</summary>
    Private ReadOnly Property wordValue(ByVal index As Integer) As UInteger
        Get
            If index < 0 Then Return 0
            If index >= words.Length Then Return 0
            Return words(index)
        End Get
    End Property

    Public ReadOnly Property Abs() As BigNum
        Get
            Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
            Return If(Me < 0, -Me, Me)
        End Get
    End Property
    Public ReadOnly Property Bit(ByVal index As Integer) As Boolean
        Get
            If index < 0 Then Return False
            Return CBool(wordValue(index \ WORD_SIZE) And (1UI << (index Mod WORD_SIZE)))
        End Get
    End Property

    '''<summary>Returns the number of bits required to store the BigNum's value.</summary>
    Public ReadOnly Property NumBits() As Integer
        Get
            Dim n = (words.Length - 1) * WORD_SIZE
            Dim u = wordValue(words.Length - 1)
            While u <> 0
                u >>= 1
                n += 1
            End While
            If n < 0 Then Return 0
            Return n
        End Get
    End Property

    Public Function RandomUniformUpTo(ByVal rand As System.Security.Cryptography.RandomNumberGenerator,
                                      Optional ByVal allowEqual As Boolean = False,
                                      Optional ByVal allowZero As Boolean = True) As BigNum
        Contract.Requires(rand IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If (Not allowZero Or Not allowEqual) And Me = 0 Then Throw New ArgumentException("Invalid Range")
        If (Not allowZero And Not allowEqual) And Me = 1 Then Throw New ArgumentException("Invalid Range")
        If Not allowZero Then Return (Me - 1).RandomUniformUpTo(rand, allowEqual, True) + 1
        If Not allowEqual Then Return (Me - 1).RandomUniformUpTo(rand, True, allowZero)
        If Me = 0 Then Return 0

        'Warning: Loses up to 1 bit of entropy due to partial wrap-around
        Dim data(0 To CInt(Math.Ceiling(Me.NumBits / 8)) - 1) As Byte
        rand.GetBytes(data)
        Return BigNum.FromBytes(data, ByteOrder.LittleEndian) Mod Me
    End Function
#End Region

#Region "CType Operators"
    Public Shared Widening Operator CType(ByVal value As ULong) As BigNum
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If value = 0 Then Return Zero
        If value = 1 Then Return Unit
        Return New BigNum(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As BigNum) As ULong
        Contract.Requires(value IsNot Nothing)

        If value < ULong.MinValue OrElse value > ULong.MaxValue Then
            Throw New ArgumentOutOfRangeException("value", "Value won't fit inside a UInt64")
        End If

        Dim n = 0UL
        For Each word In value.words.Reverse()
            n = (n << WORD_SIZE) + word
        Next word
        Return n
    End Operator

    Public Shared Widening Operator CType(ByVal value As Long) As BigNum
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If value = Long.MinValue Then Return -New BigNum(CULng(Long.MaxValue) + 1UL)
        If value < 0 Then Return -New BigNum(CULng(-value))
        Return CULng(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As BigNum) As Long
        Contract.Requires(value IsNot Nothing)

        If value < Long.MinValue OrElse value > Long.MaxValue Then
            Throw New ArgumentOutOfRangeException("value", "Value won't fit inside an Int64")
        End If
        If value = Long.MinValue Then Return Long.MinValue
        Return CLng(CULng(value.Abs())) * value.Sign
    End Operator

    Public Shared Widening Operator CType(ByVal value As UInteger) As BigNum
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Return CULng(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As Integer) As BigNum
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Return CLng(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As BigNum) As UInteger
        Contract.Requires(value IsNot Nothing)
        If value < UInteger.MinValue OrElse value > UInteger.MaxValue Then
            Throw New ArgumentOutOfRangeException("value", "Value won't fit inside a UInt32")
        End If
        Return CUInt(CULng(value))
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As BigNum) As Integer
        Contract.Requires(value IsNot Nothing)
        If value < Integer.MinValue OrElse value > Integer.MaxValue Then
            Throw New ArgumentOutOfRangeException("value", "Value won't fit inside an Int32")
        End If
        Return CInt(CLng(value))
    End Operator
#End Region

#Region "Comparisons"
    Public Function CompareTo(ByVal other As BigNum) As Integer Implements IComparable(Of BigNum).CompareTo
        If other Is Nothing Then Throw New ArgumentNullException("other")

        'Compare signs
        If Me.Sign <> other.Sign Then Return Me.Sign - other.Sign
        'Compare powers
        If Me.words.Length <> other.words.Length Then
            Return Me.Sign * (Me.words.Length - other.words.Length)
        End If
        'Compare words
        For i = Me.words.Length - 1 To 0 Step -1
            If Me.words(i) <> other.words(i) Then
                Return Me.Sign * If(Me.words(i) > other.words(i), 1, -1)
            End If
        Next i
        'Equal
        Return 0
    End Function
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        Dim other = TryCast(obj, BigNum)
        Return other IsNot Nothing AndAlso other = Me
    End Function
    Public Overrides Function GetHashCode() As Integer
        Dim hash = 0
        For Each word In words
            hash = hash Xor word.GetHashCode()
        Next word
        Return hash
    End Function

    Public Shared Operator =(ByVal value1 As BigNum, ByVal value2 As BigNum) As Boolean
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Return value1.CompareTo(value2) = 0
    End Operator
    Public Shared Operator <>(ByVal value1 As BigNum, ByVal value2 As BigNum) As Boolean
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Return value1.CompareTo(value2) <> 0
    End Operator
    Public Shared Operator <(ByVal value1 As BigNum, ByVal value2 As BigNum) As Boolean
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Return value1.CompareTo(value2) < 0
    End Operator
    Public Shared Operator <=(ByVal value1 As BigNum, ByVal value2 As BigNum) As Boolean
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Return value1.CompareTo(value2) <= 0
    End Operator
    Public Shared Operator >=(ByVal value1 As BigNum, ByVal value2 As BigNum) As Boolean
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Return value1.CompareTo(value2) >= 0
    End Operator
    Public Shared Operator >(ByVal value1 As BigNum, ByVal value2 As BigNum) As Boolean
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Return value1.CompareTo(value2) > 0
    End Operator
#End Region

#Region "Operators"
    '''<summary>Returns the negation of a bignum.</summary>
    Public Shared Operator -(ByVal value As BigNum) As BigNum
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Return New BigNum(exactWords:=value.words, Sign:=-value.Sign)
    End Operator

    '''<summary>Returns the sum of two BigNums,</summary>
    Public Shared Operator +(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If value1 = 0 Then Return value2
        If value2 = 0 Then Return value1
        If value1.Sign <> value2.Sign Then Return value1 - -value2

        Dim sign = value1.Sign
        Dim words As New List(Of UInteger)

        Dim carry = 0UL
        Dim max = Math.Max(value1.words.Length, value2.words.Length)
        Dim i = 0
        Do Until carry = 0 AndAlso i >= max
            Dim sum = CULng(value1.wordValue(i)) + CULng(value2.wordValue(i)) + carry
            carry = sum >> WORD_SIZE
            words.Add(CUInt(sum And WORD_MASK))
            i += 1
        Loop
        Return New BigNum(words, sign)
    End Operator
    '''<summary>Returns the difference between two BigNums</summary>
    Public Shared Operator -(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If value1 = 0 Then Return -value2
        If value2 = 0 Then Return value1
        If value1.Abs < value2.Abs Then Return -(value2 + -value1)
        If value1.Sign <> value2.Sign Then Return value1 + -value2

        Dim sign = value1.Sign
        Dim words As New List(Of UInteger)(value1.words.Length - 1) 'b1.maxWord >= b2.maxWord

        Dim carry = 0L
        Dim i = 0
        Do Until carry = 0 AndAlso i >= value1.words.Length
            Dim dif = CLng(value1.wordValue(i)) - CLng(value2.wordValue(i)) - carry
            carry = If(dif < 0, 1, 0)
            words.Add(CUInt(dif And WORD_MASK))
            i += 1
        Loop
        Return New BigNum(words, sign)
    End Operator

    '''<summary>Returns the product of two BigNums</summary>
    Public Shared Operator *(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim sign = value1.Sign * value2.Sign
        value1 = value1.Abs()
        value2 = value2.Abs()
        Dim p = Zero
        For Each word In value1.words
            p += value2 * word
            value2 <<= WORD_SIZE
        Next word
        If sign = -1 Then p = -p
        Return p
    End Operator
    '''<summary>Multiplies this bignum by the given word</summary>
    Public Shared Operator *(ByVal value1 As BigNum, ByVal value2 As UInteger) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim carry = 0UL
        Dim i = 0
        Dim ul = CULng(value2)
        Dim words = New List(Of UInteger)
        Do Until carry = 0 AndAlso i >= value1.words.Length
            Dim product = CULng(value1.wordValue(i)) * ul + carry
            carry = product >> WORD_SIZE
            words.Add(CUInt(product And WORD_MASK))
            i += 1
        Loop
        Return New BigNum(words, value1.Sign)
    End Operator
    '''<summary>Returns the AND of two BigNums. Result is always non-negative.</summary>
    Public Shared Operator And(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim n = Math.Min(value1.words.Length, value2.words.Length)
        Dim words(0 To n - 1) As UInteger
        For i = 0 To n - 1
            words(i) = value1.words(i) And value2.words(i)
        Next i
        Return New BigNum(words:=words, Sign:=1)
    End Operator
    '''<summary>Returns the OR of two BigNums. Result is always non-negative.</summary>
    Public Shared Operator Or(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim n = Math.Max(value1.words.Length, value2.words.Length)
        If n <= 0 Then Return 0
        Dim words(0 To n - 1) As UInteger
        For i = 0 To n - 1
            words(i) = value1.wordValue(i) Or value2.wordValue(i)
        Next i
        Return New BigNum(exactWords:=words, Sign:=1)
    End Operator
    '''<summary>Returns the XOR of two BigNums. Result is always non-negative.</summary>
    Public Shared Operator Xor(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim n = Math.Max(value1.words.Length, value2.words.Length)
        Dim words(0 To n - 1) As UInteger
        For i = 0 To n - 1
            words(i) = value1.wordValue(i) Xor value2.wordValue(i)
        Next i
        Return New BigNum(words:=words, Sign:=1)
    End Operator
    '''<summary>Returns the given BigNum left-shifted by 'offset' bits.</summary>
    Public Shared Operator <<(ByVal value As BigNum, ByVal offset As Integer) As BigNum
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)

        If offset = 0 OrElse value = 0 Then Return value
        If offset = Integer.MinValue Then Return (value >> 1) >> Integer.MaxValue
        If offset < 0 Then Return value >> -offset

        Dim wordDif = offset \ WORD_SIZE
        Dim bitDif = offset Mod WORD_SIZE
        Dim lowMask = WORD_MASK >> bitDif
        Dim highMask = Not lowMask

        Dim words As New List(Of UInteger)
        For i = 0 To wordDif - 1
            words.Add(0)
        Next i
        For i = wordDif To value.words.Length + wordDif
            Dim low = (value.wordValue(i - wordDif) And lowMask) << bitDif
            Dim high = (value.wordValue(i - wordDif - 1) And highMask) >> (WORD_SIZE - bitDif)
            words.Add(high Or low)
        Next i
        Return New BigNum(words, value.Sign)
    End Operator
    '''<summary>Returns the given BigNum right-shifted by 'offset' bits</summary>
    Public Shared Operator >>(ByVal value As BigNum, ByVal offset As Integer) As BigNum
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)

        If offset = 0 OrElse value = 0 Then Return value
        If offset = Integer.MinValue Then Return (value << 1) << Integer.MaxValue
        If offset < 0 Then Return value << -offset
        If offset > value.NumBits Then Return Zero

        Dim wordDif = offset \ WORD_SIZE
        Dim bitDif = offset Mod WORD_SIZE
        Dim lowMask = (1UI << bitDif) - 1UI
        Dim highMask = Not lowMask

        Dim words = New List(Of UInteger)
        For i = 0 To value.words.Length - 1
            Dim low = (value.wordValue(i + wordDif + 1) And lowMask) << (WORD_SIZE - bitDif)
            Dim high = (value.wordValue(i + wordDif) And highMask) >> bitDif
            words.Add(low Or high)
        Next i
        Return New BigNum(words, value.Sign)
    End Operator
    '''<summary>Returns the quotient of b1\b2</summary>
    Public Shared Operator \(ByVal value As BigNum, ByVal divisor As BigNum) As BigNum
        Contract.Requires(value IsNot Nothing)
        Contract.Requires(divisor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Return DivMod(value, divisor).Quotient
    End Operator
    '''<summary>Returns the remainder of b1\b2</summary>
    Public Shared Operator Mod(ByVal value As BigNum, ByVal divisor As BigNum) As BigNum
        Contract.Requires(value IsNot Nothing)
        Contract.Requires(divisor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Return DivMod(value, divisor).Remainder
    End Operator
#End Region

#Region "Mod Ops"
    <DebuggerDisplay("{ToDecimal}")>
    Public Class DivModResult
        Private ReadOnly _quotient As BigNum
        Private ReadOnly _remainder As BigNum
        Public ReadOnly Property Quotient As BigNum
            Get
                Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
                Return _quotient
            End Get
        End Property
        Public ReadOnly Property Remainder As BigNum
            Get
                Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
                Return _remainder
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_quotient IsNot Nothing)
            Contract.Invariant(_remainder IsNot Nothing)
        End Sub

        Public Sub New(ByVal quotient As BigNum, ByVal remainder As BigNum)
            Contract.Requires(quotient IsNot Nothing)
            Contract.Requires(remainder IsNot Nothing)
            Me._quotient = quotient
            Me._remainder = remainder
        End Sub

        Public ReadOnly Property ToDecimal() As String
            Get
                Return Quotient.ToDecimal + " R=" + Remainder.ToDecimal
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Returns the quotient and remainder of a division.
    ''' The remainder is guaranteed to be non-negative and less than the absolute value of the denominator.
    ''' </summary>
    Public Shared Function DivMod(ByVal numerator As BigNum, ByVal denominator As BigNum) As DivModResult
        Contract.Requires(numerator IsNot Nothing)
        Contract.Requires(denominator IsNot Nothing)
        Contract.Ensures(Contract.Result(Of DivModResult)() IsNot Nothing)
        Contract.Ensures(Contract.Result(Of DivModResult)().Remainder >= 0)
        Contract.Ensures(Contract.Result(Of DivModResult)().Remainder < denominator.Abs())
        Contract.Ensures(Contract.Result(Of DivModResult)().Quotient * denominator + Contract.Result(Of DivModResult)().Remainder = numerator)
        If denominator = 0 Then Throw New ArgumentException("Denominator must be non-zero.", "denominator")

        'Special Cases
        If numerator < 0 Then
            'Truncated division
            Dim d = DivMod(-numerator, denominator)
            Dim q = -d.Quotient
            Dim r = -d.Remainder
            'Switch to floored division
            If r < 0 Then
                q -= 1
                r += denominator
            End If
            Return New DivModResult(q, r)
        ElseIf denominator < 0 Then
            Dim d = DivMod(numerator, -denominator)
            Return New DivModResult(-d.Quotient, d.Remainder)
        ElseIf denominator = 1 << (denominator.NumBits - 1) Then 'Power-of-2 division
            'Perform with shifts and masks
            Return New DivModResult(numerator >> (denominator.NumBits - 1), numerator And (denominator - 1))
        End If

        'Long Division
        Dim quotient As BigNum = 0
        Dim remainder = numerator
        Dim lastShift = 0
        Do
            'Compute maximum denominator shift which won't exceed the remainder
            Dim shift = remainder.NumBits - denominator.NumBits
            If denominator << shift > remainder Then shift -= 1

            'Jump to next bit position
            quotient <<= lastShift - Math.Max(0, shift)
            If remainder < denominator Then Exit Do

            'Start next bit
            quotient += 1
            remainder -= denominator << shift
            lastShift = shift
        Loop
        Return New DivModResult(quotient, remainder)
    End Function

    '''<summary>Returns a BigNum equal to this BigNum raised to the power p, mod m.</summary>
    Public Function PowerMod(ByVal power As BigNum,
                             ByVal divisor As BigNum) As BigNum
        Contract.Requires(power IsNot Nothing)
        Contract.Requires(divisor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If Me < 0 OrElse power < 0 OrElse divisor < 0 Then Throw New ArgumentException("All arguments to PowerMod must be non-negative.")
        If divisor = 0 Then Throw New ArgumentException("Denominator must be non-zero.", "divisor")
        If divisor = 1 Then Return 0
        If power = 0 Then Return 1

        Dim factor = Me Mod divisor
        Dim total = Unit
        For i = 0 To power.NumBits() - 1
            If power.Bit(i) Then total = (total * factor) Mod divisor
            factor = (factor * factor) Mod divisor
        Next i

        Return total
    End Function
#End Region

#Region "Base Conversions"
    Public Shared Function FromBase(ByVal digits As IEnumerable(Of UInteger),
                                    ByVal base As UInteger,
                                    ByVal byteOrder As ByteOrder) As BigNum
        Contract.Requires(digits IsNot Nothing)
        Contract.Requires(base >= 2)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim n = Zero
        If byteOrder = byteOrder.LittleEndian Then digits = digits.Reverse()
        For Each e In digits
            n *= base
            If e >= base Then Throw New ArgumentException("A digit was larger than the base")
            n += e
        Next e
        Return n
    End Function
    Public Shared Function FromBaseBytes(ByVal digits As IEnumerable(Of Byte),
                                         ByVal base As UInteger,
                                         ByVal byteOrder As ByteOrder) As BigNum
        Contract.Requires(digits IsNot Nothing)
        Contract.Requires(base >= 2)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim digits_ = (From d In digits Select CUInt(d))
        Contract.Assume(digits_ IsNot Nothing)
        Dim x = FromBase(digits_, base, byteOrder)
        Contract.Assume(x IsNot Nothing)
        Return x
    End Function

    Public Shared Function FromBytes(ByVal digits As IEnumerable(Of Byte),
                                     ByVal byteOrder As ByteOrder) As BigNum
        Contract.Requires(digits IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Return FromBaseBytes(digits, 256, byteOrder)
    End Function

    Public Function ToBase(ByVal base As UInteger,
                           ByVal byteOrder As ByteOrder) As IList(Of UInteger)
        Contract.Requires(base >= 2)
        Contract.Ensures(Contract.Result(Of IList(Of UInteger))() IsNot Nothing)

        Dim L = New List(Of UInteger)
        Dim d = New DivModResult(Me.Abs(), 0)
        While d.Quotient > 0
            d = BigNum.DivMod(d.Quotient, base)
            L.Add(CUInt(d.Remainder))
        End While

        Select Case byteOrder
            Case byteOrder.LittleEndian
                Return L
            Case byteOrder.BigEndian
                L.Reverse()
                Return L
            Case Else
                Throw New ArgumentException("Unrecognized byte order")
        End Select
    End Function
    Public Function ToBaseBytes(ByVal base As UInteger,
                                ByVal byteOrder As ByteOrder) As IList(Of Byte)
        Contract.Requires(base >= 2)
        Contract.Requires(base <= 256)
        Contract.Ensures(Contract.Result(Of IList(Of Byte))() IsNot Nothing)
        Dim x = (From u In ToBase(base, byteOrder) Select CByte(u))
        Contract.Assume(x IsNot Nothing)
        Dim y = x.ToList()
        Contract.Assume(y IsNot Nothing)
        Return y
    End Function
    Public Function ToBytes(ByVal byteOrder As ByteOrder) As IList(Of Byte)
        Contract.Ensures(Contract.Result(Of IList(Of Byte))() IsNot Nothing)
        Return ToBaseBytes(256, byteOrder)
    End Function

    '''<summary>Returns a decimal representation of this number</summary>
    Public ReadOnly Property ToDecimal() As String
        Get
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return ToString(10, ByteOrder.BigEndian)
        End Get
    End Property
    '''<summary>Returns a binary representation of this number</summary>
    Public ReadOnly Property ToBinary() As String
        Get
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return ToString(2, ByteOrder.BigEndian)
        End Get
    End Property
    '''<summary>Returns a hexadecimal representation of this number</summary>
    Public ReadOnly Property ToHex() As String
        Get
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            Return ToString(16, ByteOrder.BigEndian)
        End Get
    End Property

    '''<summary>Returns a string representation of this number.</summary>
    Public Shadows Function ToString(ByVal base As UInteger,
                                     ByVal byteOrder As ByteOrder) As String
        Contract.Requires(base >= 2)
        Contract.Requires(base <= 36)
        Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
        If base < 2 Or base > 36 Then Throw New ArgumentOutOfRangeException("base", "base must be in [2,36]")
        If Me = 0 Then Return "0"
        If Me < 0 Then Return "-" + Me.Abs().ToString(base, byteOrder)
        Dim digits = ToBase(base, byteOrder)
        Dim result = New System.Text.StringBuilder()
        For i = 0 To digits.Count - 1
            Select Case digits(i)
                Case 0 To 9
                    result.Append(digits(i).ToString(CultureInfo.InvariantCulture))
                Case 10 To 35
                    result.Append(Chr(CByte(Asc("A"c) + digits(i) - 10)))
                Case Else
                    result.Append("?")
            End Select
        Next i
        Return result.ToString
    End Function
    Public Shared Function FromString(ByVal number As String,
                                      ByVal base As UInteger,
                                      ByVal byteOrder As ByteOrder) As BigNum
        Contract.Requires(number IsNot Nothing)
        Contract.Requires(base >= 2)
        Contract.Requires(base <= 36)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim digits = New List(Of UInteger)
        For Each c In number
            Select Case c
                Case "0"c To "9"c
                    Dim s = CStr(c)
                    Contract.Assume(s IsNot Nothing)
                    digits.Add(Byte.Parse(s, CultureInfo.InvariantCulture))
                Case "A"c To "Z"c
                    digits.Add(CUInt(Asc(c) - Asc("A") + 10))
                Case "a"c To "z"c
                    digits.Add(CUInt(Asc(c) - Asc("a") + 10))
                Case Else
                    Throw New ArgumentException("Invalid string.")
            End Select
        Next c
        Return FromBase(digits, base, byteOrder)
    End Function
#End Region

    '#Region "Testing"
    '    Friend Shared Sub RunTests(ByVal rand As Random)
    '        Debug.Print("Testing BigNum")
    '        Dim b = BigNum.FromString("B3500005D3AF30059ED523B65CCE3C442710DA2C566985346AD4835F1E122338", 16, ByteOrder.BigEndian)
    '        Dim p = BigNum.FromString("BEE2CA68607F273D7C5A53196CB0B8E5E4A92CA677E6841D1ECBAF0CB0A85F15", 16, ByteOrder.BigEndian)
    '        Dim m = BigNum.FromString("F8FF1A8B619918032186B68CA092B5557E976C78C73212D91216F6658523C787", 16, ByteOrder.BigEndian)
    '        Dim r = BigNum.FromString("A0B0FBE45B9679E962D87055524385E122C70011D6D4636624A690741A381171", 16, ByteOrder.BigEndian)
    '        If b <> b Then Throw New Exception("Incorrect result.")
    '        If b.PowerMod(p, m) <> r Then Throw New Exception("Incorrect result.")
    '        If b + r <> r + b Then Throw New Exception("Incorrect result.")
    '        If b * r <> r * b Then Throw New Exception("Incorrect result.")
    '        If -(-b) <> b Then Throw New Exception("Incorrect result.")
    '        If b - r <> -(r - b) Then Throw New Exception("Incorrect result.")
    '        If b - r <> b + -r Then Throw New Exception("Incorrect result.")
    '        If (p * r) \ r <> p Then Throw New Exception("Incorrect result.")
    '        If (p * r) Mod r <> 0 Then Throw New Exception("Incorrect result.")
    '        If (p * r) * b <> p * (r * b) Then Throw New Exception("Incorrect result.")
    '        If (p + r) + b <> p + (r + b) Then Throw New Exception("Incorrect result.")
    '        If (p + r) * b <> p * b + r * b Then Throw New Exception("Incorrect result.")
    '        If p * p <= p Then Throw New Exception("Incorrect result.")
    '        If p + p <= p Then Throw New Exception("Incorrect result.")
    '        If p - p <> 0 Then Throw New Exception("Incorrect result.")
    '        If p \ p <> 1 Then Throw New Exception("Incorrect result.")
    '        If p + p <> 2 * p Then Throw New Exception("Incorrect result.")
    '        Debug.Print("BigNum Passed")
    '    End Sub
    '#End Region
End Class
