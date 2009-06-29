Imports HostBot.Immutable

'''<summary>Immutable BigNum</summary>
Public Class BigNum
    Implements IComparable(Of BigNum)

    Private ReadOnly _words As UInteger()
    Private ReadOnly Property words As UInteger()
        Get
            Contract.Ensures(Contract.Result(Of UInteger())() IsNot Nothing)
            Return _words
        End Get
    End Property
    Private ReadOnly neg As Boolean

    Private Const WORD_SIZE As Integer = 32
    Private Const WORD_MASK As UInteger = UInteger.MaxValue

    Public Shared ReadOnly Zero As New BigNum(0)
    Public Shared ReadOnly Unit As New BigNum(1)

    <ContractInvariantMethod()> Protected Sub Invariant()
        Contract.Invariant(_words IsNot Nothing)
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

    Private Sub New(ByVal n As ULong)
        Me.New(GetULongWords(n), False)
    End Sub
    Private Sub New(ByVal words As List(Of UInteger), ByVal negative As Boolean)
        Contract.Requires(words IsNot Nothing)
        Me.neg = negative

        Dim m As Integer
        For m = words.Count - 1 To 0 Step -1
            If words(m) <> 0 Then Exit For
        Next m
        If m < 0 Then Me.neg = False
        Me._words = words.SubToArray(0, m + 1)
    End Sub
    Private Sub New(ByVal words As UInteger(), ByVal negative As Boolean)
        Contract.Requires(words IsNot Nothing)
        Me._words = words
        Me.neg = negative And words.Count > 0
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
    Public ReadOnly Property Sign() As Integer
        Get
            If words.Count = 0 Then Return 0
            Return If(neg, -1, 1)
        End Get
    End Property
    Public ReadOnly Property Bit(ByVal index As Integer) As Boolean
        Get
            If index < 0 Then Return False
            Return CBool(wordValue(index \ WORD_SIZE) And (CUInt(1) << (index Mod WORD_SIZE)))
        End Get
    End Property

    '''<summary>Returns the BigNum's highest set bit position.</summary>
    Public ReadOnly Property MaxBit() As Integer
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

    Public Shared Function Gcd(ByVal a As BigNum, ByVal b As BigNum) As BigNum
        Contract.Requires(a IsNot Nothing)
        Contract.Requires(b IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If b = 0 Then Return a
        Return Gcd(b, a Mod b)
    End Function
    Public Shared Function Lcm(ByVal a As BigNum, ByVal b As BigNum) As BigNum
        Contract.Requires(a IsNot Nothing)
        Contract.Requires(b IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Lcm = (a * b) \ Gcd(a, b)
    End Function
    Public Shared Function GcdEx(ByVal a As BigNum, ByVal b As BigNum) As GcdExResult
        Contract.Requires(a IsNot Nothing)
        Contract.Requires(b IsNot Nothing)
        Contract.Ensures(Contract.Result(Of GcdExResult)() IsNot Nothing)

        Dim division = DivMod(a, b)
        If division.remainder = 0 Then Return New GcdExResult(0, 1)

        Dim recursion = GcdEx(b, division.remainder)
        Return New GcdExResult(recursion.Y, recursion.X - (recursion.Y * division.quotient))
    End Function
    Public Function InverseMod(ByVal n As BigNum) As BigNum
        Contract.Requires(n IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        InverseMod = GcdEx(Me, n).X
        If (InverseMod * Me) Mod n <> 1 Then Throw New OperationFailedException("Failed to compute modular inverse")
    End Function
    Public Function RandomUniformUpTo(Optional ByVal rand As Random = Nothing,
                                      Optional ByVal allowEqual As Boolean = False,
                                      Optional ByVal allowZero As Boolean = True) As BigNum
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If (Not allowZero Or Not allowEqual) And Me = 0 Then Throw New ArgumentException("Invalid Range")
        If (Not allowZero And Not allowEqual) And Me = 1 Then Throw New ArgumentException("Invalid Range")
        If Not allowZero Then Return (Me - 1).RandomUniformUpTo(rand, allowEqual, True) + 1
        If Not allowEqual Then Return (Me - 1).RandomUniformUpTo(rand, True, allowZero)
        If rand Is Nothing Then rand = New Random()
        If Me = 0 Then Return 0

        Dim n = Zero
        Dim isStrictlyLessThan = False
        For i = Me.MaxBit() To 0 Step -1
            If isStrictlyLessThan Or Me.Bit(i) Then
                Dim r = rand.Next(2)
                If r = 0 Then
                    isStrictlyLessThan = True
                Else
                    n = n Or (Unit << i)
                End If
            End If
        Next i
        Return n
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
            Throw New ArgumentOutOfRangeException("value", "Value is outside of range of ULong")
        End If

        Dim n = CULng(0)
        For Each word In value.words.Reverse()
            n = (n << WORD_SIZE) + word
        Next word
        Return n
    End Operator

    Public Shared Widening Operator CType(ByVal value As Long) As BigNum
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If value < 0 Then Return -New BigNum(CULng(-value))
        Return CULng(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As BigNum) As Long
        Contract.Requires(value IsNot Nothing)

        If value = Long.MinValue Then Return Long.MinValue
        If value < Long.MinValue OrElse value > Long.MaxValue Then
            Throw New ArgumentOutOfRangeException("value", "Value is outside of range of Long")
        End If
        Dim n = CLng(CULng(value.Abs()))
        Return If(value.neg, -n, n)
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
        Return CUInt(CULng(value))
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As BigNum) As Integer
        Contract.Requires(value IsNot Nothing)
        Return CInt(CLng(value))
    End Operator
#End Region

#Region "Comparisons"
    Public Function CompareTo(ByVal other As BigNum) As Integer Implements IComparable(Of BigNum).CompareTo
        Contract.Assume(other IsNot Nothing)

        If Me.neg Then Return -((-Me).CompareTo(-other))
        If other.neg Then Return 1
        If Me.words.Length <> other.words.Length Then
            Return Me.words.Length - other.words.Length
        End If
        For i = Me.words.Length - 1 To 0 Step -1
            If Me.words(i) <> other.words(i) Then
                Return Math.Sign(CLng(Me.words(i)) - CLng(other.words(i)))
            End If
        Next i
        Return 0
    End Function
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        If obj Is Nothing Then Return False
        If TypeOf obj Is BigNum Then Return CType(obj, BigNum) = Me
        Return False
    End Function
    Public Overrides Function GetHashCode() As Integer
        Dim x = 0
        For Each word In words
            x = x Xor word.GetHashCode()
        Next
        Return x
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
        Return New BigNum(value.words, Not value.neg)
    End Operator

    '''<summary>Returns the sum of two BigNums,</summary>
    Public Shared Operator +(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If value1 = 0 Then Return value2
        If value2 = 0 Then Return value1
        If value1.neg <> value2.neg Then Return value1 - -value2

        Dim neg = value1.neg
        Dim words As New List(Of UInteger)

        Dim carry = CULng(0)
        Dim max = Math.Max(value1.words.Length, value2.words.Length)
        Dim i = 0
        Do Until carry = 0 AndAlso i >= max
            Dim sum = CULng(value1.wordValue(i)) + CULng(value2.wordValue(i)) + carry
            carry = sum >> WORD_SIZE
            words.Add(CUInt(sum And WORD_MASK))
            i += 1
        Loop
        Return New BigNum(words, neg)
    End Operator
    '''<summary>Returns the difference between two BigNums</summary>
    Public Shared Operator -(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If value1 = 0 Then Return -value2
        If value2 = 0 Then Return value1
        If value1.Abs < value2.Abs Then Return -(value2 + -value1)
        If value1.neg <> value2.neg Then Return value1 + -value2

        Dim neg = value1.neg
        Dim words As New List(Of UInteger)(value1.words.Length - 1) 'b1.maxWord >= b2.maxWord

        Dim carry = CLng(0)
        Dim i = 0
        Do Until carry = 0 AndAlso i >= value1.words.Length
            Dim dif = CLng(value1.wordValue(i)) - CLng(value2.wordValue(i)) - carry
            carry = If(dif < 0, 1, 0)
            words.Add(CUInt(dif And WORD_MASK))
            i += 1
        Loop
        Return New BigNum(words, neg)
    End Operator

    '''<summary>Returns the product of two BigNums</summary>
    Public Shared Operator *(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim neg = value1.neg Xor value2.neg
        value1 = value1.Abs()
        value2 = value2.Abs()
        Dim p = Zero
        For Each word In value1.words
            p += value2 * word
            value2 <<= WORD_SIZE
        Next word
        Return If(neg, -p, p)
    End Operator
    '''<summary>Multiplies this bignum by the given word</summary>
    Public Shared Operator *(ByVal value1 As BigNum, ByVal value2 As UInteger) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim carry = CULng(0)
        Dim i = 0
        Dim ul = CULng(value2)
        Dim words = New List(Of UInteger)
        Do Until carry = 0 AndAlso i >= value1.words.Length
            Dim product = CULng(value1.wordValue(i)) * ul + carry
            carry = product >> WORD_SIZE
            words.Add(CUInt(product And WORD_MASK))
            i += 1
        Loop
        Return New BigNum(words, value1.neg)
    End Operator
    '''<summary>Returns the AND of two BigNums</summary>
    Public Shared Operator And(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim n = Math.Min(value1.words.Length, value2.words.Length)
        Dim words(0 To n - 1) As UInteger
        Dim neg = value1.neg And value2.neg
        For i = 0 To n - 1
            words(i) = value1.words(i) And value2.words(i)
        Next i
        Return New BigNum(words, neg)
    End Operator
    '''<summary>Returns the OR of two BigNums</summary>
    Public Shared Operator Or(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim n = Math.Max(value1.words.Length, value2.words.Length)
        Dim words(0 To n - 1) As UInteger
        Dim neg = value1.neg Or value2.neg
        For i = 0 To n - 1
            words(i) = value1.wordValue(i) Or value2.wordValue(i)
        Next i
        Return New BigNum(words, neg)
    End Operator
    '''<summary>Returns the XOR of two BigNums</summary>
    Public Shared Operator Xor(ByVal value1 As BigNum, ByVal value2 As BigNum) As BigNum
        Contract.Requires(value1 IsNot Nothing)
        Contract.Requires(value2 IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim n = Math.Max(value1.words.Length, value2.words.Length)
        Dim words(0 To n - 1) As UInteger
        Dim neg = value1.neg Or value2.neg
        For i = 0 To n - 1
            words(i) = value1.wordValue(i) Xor value2.wordValue(i)
        Next i
        Return New BigNum(words, neg)
    End Operator
    '''<summary>Returns the given BigNum left-shifted by 'offset' bits</summary>
    Public Shared Operator <<(ByVal value As BigNum, ByVal offset As Integer) As BigNum
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If offset = 0 Then Return value
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
        Return New BigNum(words, value.neg)
    End Operator
    '''<summary>Returns the given BigNum right-shifted by 'offset' bits</summary>
    Public Shared Operator >>(ByVal value As BigNum, ByVal offset As Integer) As BigNum
        Contract.Requires(value IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)

        If offset = 0 Then Return value
        If offset < 0 Then Return value << -offset
        If offset > value.MaxBit Then Return Zero

        Dim wordDif = offset \ WORD_SIZE
        Dim bitDif = offset Mod WORD_SIZE
        Dim lowMask = CUInt((1 << bitDif) - 1)
        Dim highMask = Not lowMask

        Dim words = New List(Of UInteger)
        For i = 0 To value.words.Length - 1
            Dim low = (value.wordValue(i + wordDif + 1) And lowMask) << (WORD_SIZE - bitDif)
            Dim high = (value.wordValue(i + wordDif) And highMask) >> bitDif
            words.Add(low Or high)
        Next i
        Return New BigNum(words, value.neg)
    End Operator
    '''<summary>Returns the quotient of b1\b2</summary>
    Public Shared Operator \(ByVal value As BigNum, ByVal divisor As BigNum) As BigNum
        Contract.Requires(value IsNot Nothing)
        Contract.Requires(divisor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Return DivMod(value, divisor).quotient
    End Operator
    '''<summary>Returns the remainder of b1\b2</summary>
    Public Shared Operator Mod(ByVal value As BigNum, ByVal divisor As BigNum) As BigNum
        Contract.Requires(value IsNot Nothing)
        Contract.Requires(divisor IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Return DivMod(value, divisor).remainder
    End Operator
#End Region

#Region "Mod Ops"
    Public Class DivModResult
        Private ReadOnly _quotient As BigNum
        Private ReadOnly _remainder As BigNum
        Public ReadOnly Property quotient As BigNum
            Get
                Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
                Return _quotient
            End Get
        End Property
        Public ReadOnly Property remainder As BigNum
            Get
                Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
                Return _remainder
            End Get
        End Property

        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(_quotient IsNot Nothing)
            Contract.Invariant(_remainder IsNot Nothing)
        End Sub

        Public Sub New(ByVal quotient As BigNum, ByVal remainder As BigNum)
            Contract.Requires(quotient IsNot Nothing)
            Contract.Requires(remainder IsNot Nothing)
            Me._quotient = quotient
            Me._remainder = remainder
        End Sub
    End Class
    Public Class GcdExResult
        Private ReadOnly _x As BigNum
        Private ReadOnly _y As BigNum
        Public ReadOnly Property X As BigNum
            Get
                Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
                Return _x
            End Get
        End Property
        Public ReadOnly Property Y As BigNum
            Get
                Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
                Return _y
            End Get
        End Property

        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(_x IsNot Nothing)
            Contract.Invariant(_y IsNot Nothing)
        End Sub

        Public Sub New(ByVal x As BigNum, ByVal y As BigNum)
            Contract.Requires(x IsNot Nothing)
            Contract.Requires(y IsNot Nothing)
            Me._x = x
            Me._y = y
        End Sub
    End Class

    '''<summary>Returns the quotient and remainder for the given numerator and denominator.</summary>
    Public Shared Function DivMod(ByVal numerator As BigNum, ByVal denominator As BigNum) As DivModResult
        Contract.Requires(numerator IsNot Nothing)
        Contract.Requires(denominator IsNot Nothing)
        Contract.Ensures(Contract.Result(Of DivModResult)() IsNot Nothing)
        If denominator = 0 Then Throw New DivideByZeroException()

        If denominator = 1 Then Return New DivModResult(numerator, 0)
        If numerator = denominator Then Return New DivModResult(1, 0)

        If numerator < 0 Then
            With DivMod(-numerator, denominator)
                If .remainder = 0 Then
                    Return New DivModResult(-.quotient, .remainder)
                Else
                    Return New DivModResult(-.quotient - 1, denominator - .remainder)
                End If
            End With
        ElseIf denominator < 0 Then
            With DivMod(numerator, -denominator)
                Return New DivModResult(-.quotient, .remainder)
            End With
        End If
        If numerator < denominator Then Return New DivModResult(0, numerator)

        Dim quotient = Zero
        Dim remainder = numerator
        Dim m = denominator << (numerator.MaxBit() - denominator.MaxBit())
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
    Public Function PowerMod(ByVal p As BigNum, ByVal m As BigNum) As BigNum
        Contract.Requires(p IsNot Nothing)
        Contract.Requires(m IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        If Me.neg Or p.neg Or m.neg Then Throw New ArgumentException("All arguments to PowerMod must be non-negative.")
        If m = 0 Then Throw New DivideByZeroException()
        If m = 1 Then Return 0
        If p = 0 Then Return 1

        Dim factor = Me Mod m
        Dim total = Unit
        For i = 0 To p.MaxBit() - 1
            If p.Bit(i) Then total = (total * factor) Mod m
            factor = (factor * factor) Mod m
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
        While d.quotient > 0
            d = BigNum.DivMod(d.quotient, base)
            L.Add(CUInt(d.remainder))
        End While

        Select Case byteOrder
            Case HostBot.ByteOrder.LittleEndian
                Return L
            Case HostBot.ByteOrder.BigEndian
                L.Reverse()
                Return L
            Case Else
                Throw New UnreachableException()
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
    Public Shared Function FromString(ByVal number As String,
                                      ByVal base As UInteger,
                                      ByVal byteOrder As ByteOrder) As BigNum
        Contract.Requires(number IsNot Nothing)
        Contract.Requires(base >= 2)
        Contract.Requires(base <= 36)
        Contract.Ensures(Contract.Result(Of BigNum)() IsNot Nothing)
        Dim L As New List(Of UInteger)
        For Each c In number
            Select Case c
                Case "0"c To "9"c
                    Dim s = CStr(c)
                    Contract.Assume(s IsNot Nothing)
                    L.Add(Byte.Parse(s))
                Case "A"c To "Z"c
                    L.Add(CUInt(Asc(c) - Asc("A") + 10))
                Case "a"c To "z"c
                    L.Add(CUInt(Asc(c) - Asc("a") + 10))
                Case Else
                    Throw New ArgumentException("Invalid string.")
            End Select
        Next c
        Return FromBase(L, base, byteOrder)
    End Function
#End Region

#Region "Testing"
    Friend Shared Sub RunTests(ByVal rand As Random)
        Debug.Print("Testing BigNum")
        Dim b = BigNum.FromString("B3500005D3AF30059ED523B65CCE3C442710DA2C566985346AD4835F1E122338", 16, ByteOrder.BigEndian)
        Dim p = BigNum.FromString("BEE2CA68607F273D7C5A53196CB0B8E5E4A92CA677E6841D1ECBAF0CB0A85F15", 16, ByteOrder.BigEndian)
        Dim m = BigNum.FromString("F8FF1A8B619918032186B68CA092B5557E976C78C73212D91216F6658523C787", 16, ByteOrder.BigEndian)
        Dim r = BigNum.FromString("A0B0FBE45B9679E962D87055524385E122C70011D6D4636624A690741A381171", 16, ByteOrder.BigEndian)
        If b <> b Then Throw New Exception("Incorrect result.")
        If b.PowerMod(p, m) <> r Then Throw New Exception("Incorrect result.")
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