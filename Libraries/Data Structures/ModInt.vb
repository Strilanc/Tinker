<DebuggerDisplay("{ToString} (mod 2^32)")>
Public Structure ModInt32
    Implements IEquatable(Of ModInt32)
    Private ReadOnly value As UInt32

#Region "Constructors"
    Private Sub New(ByVal value As UInt32)
        Me.value = value
    End Sub
    Private Sub New(ByVal value As UInt64)
        Me.value = CUInt(value And UInt32.MaxValue)
    End Sub

    Private Sub New(ByVal value As Int32)
        Me.value = CUInt(value + If(value < 0, &H100000000L, 0))
    End Sub
    Private Sub New(ByVal value As Int64)
        Me.value = CUInt(value And UInt32.MaxValue)
    End Sub
#End Region

#Region "Operators"
    Public Shared Operator *(ByVal value1 As ModInt32, ByVal value2 As ModInt32) As ModInt32
        Return New ModInt32(CULng(value1.value) * CULng(value2.value))
    End Operator
    Public Shared Operator +(ByVal value1 As ModInt32, ByVal value2 As ModInt32) As ModInt32
        Return New ModInt32(CULng(value1.value) + CULng(value2.value))
    End Operator
    Public Shared Operator -(ByVal value1 As ModInt32, ByVal value2 As ModInt32) As ModInt32
        Return New ModInt32(CLng(value1.value) - CLng(value2.value))
    End Operator
    Public Shared Operator And(ByVal value1 As ModInt32, ByVal value2 As ModInt32) As ModInt32
        Return New ModInt32(value1.value And value2.value)
    End Operator
    Public Shared Operator Xor(ByVal value1 As ModInt32, ByVal value2 As ModInt32) As ModInt32
        Return New ModInt32(value1.value Xor value2.value)
    End Operator
    Public Shared Operator Or(ByVal value1 As ModInt32, ByVal value2 As ModInt32) As ModInt32
        Return New ModInt32(value1.value Or value2.value)
    End Operator
    Public Shared Operator Not(ByVal value As ModInt32) As ModInt32
        Return New ModInt32(Not value.value)
    End Operator
    Public Shared Operator >>(ByVal value As ModInt32, ByVal offset As Integer) As ModInt32
        Return New ModInt32(value.value >> offset)
    End Operator
    Public Shared Operator <<(ByVal value As ModInt32, ByVal offset As Integer) As ModInt32
        Return New ModInt32(value.value << offset)
    End Operator
    Public Shared Operator =(ByVal value1 As ModInt32, ByVal value2 As ModInt32) As Boolean
        Return value1.value = value2.value
    End Operator
    Public Shared Operator <>(ByVal value1 As ModInt32, ByVal value2 As ModInt32) As Boolean
        Return value1.value <> value2.value
    End Operator
    Public Function ShiftRotateLeft(ByVal offset As Integer) As ModInt32
        offset = offset And &H1F
        Return New ModInt32((value << offset) Or (value >> 32 - offset))
    End Function
    Public Function ShiftRotateRight(ByVal offset As Integer) As ModInt32
        offset = offset And &H1F
        Return New ModInt32((value >> offset) Or (value << 32 - offset))
    End Function
#End Region

#Region "Methods"
    Public Overrides Function GetHashCode() As Integer
        Return (value.GetHashCode)
    End Function
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        If Not TypeOf obj Is ModInt32 Then Return False
        Return Me.value = CType(obj, ModInt32).value
    End Function
    Public Function EqualsProper(ByVal other As ModInt32) As Boolean Implements IEquatable(Of ModInt32).Equals
        Return Me.value = other.value
    End Function
    Public Overrides Function ToString() As String
        Return value.ToString
    End Function
#End Region

#Region " -> ModInt32"
    Public Shared Narrowing Operator CType(ByVal value As UInt64) As ModInt32
        Return New ModInt32(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As UInt32) As ModInt32
        Return New ModInt32(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As UInt16) As ModInt32
        Return New ModInt32(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As Byte) As ModInt32
        Return New ModInt32(value)
    End Operator

    Public Shared Narrowing Operator CType(ByVal value As Int64) As ModInt32
        Return New ModInt32(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As Int32) As ModInt32
        Return New ModInt32(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As Int16) As ModInt32
        Return New ModInt32(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As SByte) As ModInt32
        Return New ModInt32(value)
    End Operator
#End Region

#Region "ModInt32 -> "
    Public Shared Narrowing Operator CType(ByVal value As ModInt32) As Byte
        Return CByte(value.value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As ModInt32) As UInt16
        Return CUShort(value.value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt32) As UInt32
        Return value.value
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt32) As UInt64
        Return value.value
    End Operator

    Public Shared Narrowing Operator CType(ByVal value As ModInt32) As SByte
        Return CSByte(value.value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As ModInt32) As Int16
        Return CShort(value.value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt32) As Int32
        Return CInt(value.value - If(value.value > Int32.MaxValue, &H100000000L, 0))
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt32) As Int64
        Return value.value
    End Operator
#End Region
End Structure

<DebuggerDisplay("{ToString} (mod 2^16)")>
Public Structure ModInt16
    Implements IEquatable(Of ModInt16)
    Private ReadOnly value As UInt16

#Region "Constructors"
    Private Sub New(ByVal value As UShort)
        Me.value = value
    End Sub
    Private Sub New(ByVal value As UInt32)
        Me.value = CUShort(value And UInt16.MaxValue)
    End Sub
    Private Sub New(ByVal value As UInt64)
        Me.value = CUShort(value And UInt16.MaxValue)
    End Sub

    Private Sub New(ByVal value As Int16)
        Me.value = CUShort(value + If(value < 0, &H10000, 0))
    End Sub
    Private Sub New(ByVal value As Int32)
        Me.value = CUShort(value And UInt16.MaxValue)
    End Sub
    Private Sub New(ByVal value As Int64)
        Me.value = CUShort(value And UInt16.MaxValue)
    End Sub
#End Region

#Region "Operators"
    Public Shared Operator *(ByVal value1 As ModInt16, ByVal value2 As ModInt16) As ModInt16
        Return New ModInt16(CUInt(value1.value) * CUInt(value2.value))
    End Operator
    Public Shared Operator +(ByVal value1 As ModInt16, ByVal value2 As ModInt16) As ModInt16
        Return New ModInt16(CUInt(value1.value) + CUInt(value2.value))
    End Operator
    Public Shared Operator -(ByVal value1 As ModInt16, ByVal value2 As ModInt16) As ModInt16
        Return New ModInt16(CInt(value1.value) - CInt(value2.value))
    End Operator
    Public Shared Operator And(ByVal value1 As ModInt16, ByVal value2 As ModInt16) As ModInt16
        Return New ModInt16(value1.value And value2.value)
    End Operator
    Public Shared Operator Xor(ByVal value1 As ModInt16, ByVal value2 As ModInt16) As ModInt16
        Return New ModInt16(value1.value Xor value2.value)
    End Operator
    Public Shared Operator Or(ByVal value1 As ModInt16, ByVal value2 As ModInt16) As ModInt16
        Return New ModInt16(value1.value Or value2.value)
    End Operator
    Public Shared Operator Not(ByVal value As ModInt16) As ModInt16
        Return New ModInt16(Not value.value)
    End Operator
    Public Shared Operator >>(ByVal value As ModInt16, ByVal offset As Integer) As ModInt16
        Return New ModInt16(value.value >> offset)
    End Operator
    Public Shared Operator <<(ByVal value As ModInt16, ByVal offset As Integer) As ModInt16
        Return New ModInt16(value.value << offset)
    End Operator
    Public Shared Operator =(ByVal value1 As ModInt16, ByVal value2 As ModInt16) As Boolean
        Return value1.value = value2.value
    End Operator
    Public Shared Operator <>(ByVal value1 As ModInt16, ByVal value2 As ModInt16) As Boolean
        Return value1.value <> value2.value
    End Operator
    Public Function ShiftRotateLeft(ByVal offset As Integer) As ModInt16
        offset = offset And &HF
        Return New ModInt16((value << offset) Or (value >> 16 - offset))
    End Function
    Public Function ShiftRotateRight(ByVal offset As Integer) As ModInt16
        offset = offset And &HF
        Return New ModInt16((value >> offset) Or (value << 16 - offset))
    End Function
#End Region

#Region "Methods"
    Public Overrides Function GetHashCode() As Integer
        Return (value.GetHashCode)
    End Function
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        If Not TypeOf obj Is ModInt16 Then Return False
        Return Me.value = CType(obj, ModInt16).value
    End Function
    Public Function EqualsProper(ByVal other As ModInt16) As Boolean Implements IEquatable(Of ModInt16).Equals
        Return Me.value = other.value
    End Function
    Public Overrides Function ToString() As String
        Return value.ToString
    End Function
#End Region

#Region " -> ModInt16"
    Public Shared Narrowing Operator CType(ByVal value As UInt64) As ModInt16
        Return New ModInt16(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As UInt32) As ModInt16
        Return New ModInt16(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As UInt16) As ModInt16
        Return New ModInt16(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As Byte) As ModInt16
        Return New ModInt16(value)
    End Operator

    Public Shared Narrowing Operator CType(ByVal value As Int64) As ModInt16
        Return New ModInt16(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As Int32) As ModInt16
        Return New ModInt16(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As Int16) As ModInt16
        Return New ModInt16(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As SByte) As ModInt16
        Return New ModInt16(value)
    End Operator
#End Region

#Region "ModInt16 -> "
    Public Shared Narrowing Operator CType(ByVal value As ModInt16) As Byte
        Return CByte(value.value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt16) As UInt16
        Return value.value
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt16) As UInt32
        Return value.value
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt16) As UInt64
        Return value.value
    End Operator

    Public Shared Narrowing Operator CType(ByVal value As ModInt16) As SByte
        Return CSByte(value.value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt16) As Int16
        Return CShort(value.value - If(value.value < 0, &H10000, 0))
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt16) As Int32
        Return value.value
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModInt16) As Int64
        Return value.value
    End Operator
#End Region
End Structure

<DebuggerDisplay("{ToString} (mod 2^8)")>
Public Structure ModByte
    Implements IEquatable(Of ModByte)
    Private ReadOnly value As Byte

#Region "Constructors"
    Private Sub New(ByVal value As Byte)
        Me.value = value
    End Sub
    Private Sub New(ByVal value As UInt16)
        Me.value = CByte(value And Byte.MaxValue)
    End Sub
    Private Sub New(ByVal value As UInt32)
        Me.value = CByte(value And Byte.MaxValue)
    End Sub
    Private Sub New(ByVal value As UInt64)
        Me.value = CByte(value And Byte.MaxValue)
    End Sub

    Private Sub New(ByVal value As SByte)
        Me.value = CByte(value + If(value < 0, &H100, 0))
    End Sub
    Private Sub New(ByVal value As Int16)
        Me.value = CByte(value And Byte.MaxValue)
    End Sub
    Private Sub New(ByVal value As Int32)
        Me.value = CByte(value And Byte.MaxValue)
    End Sub
    Private Sub New(ByVal value As Int64)
        Me.value = CByte(value And Byte.MaxValue)
    End Sub
#End Region

#Region "Operators"
    Public Shared Operator *(ByVal value1 As ModByte, ByVal value2 As ModByte) As ModByte
        Return New ModByte(CUShort(value1.value) * CUShort(value2.value))
    End Operator
    Public Shared Operator +(ByVal value1 As ModByte, ByVal value2 As ModByte) As ModByte
        Return New ModByte(CUShort(value1.value) + CUShort(value2.value))
    End Operator
    Public Shared Operator -(ByVal value1 As ModByte, ByVal value2 As ModByte) As ModByte
        Return New ModByte(CShort(value1.value) - CShort(value2.value))
    End Operator
    Public Shared Operator And(ByVal value1 As ModByte, ByVal value2 As ModByte) As ModByte
        Return New ModByte(value1.value And value2.value)
    End Operator
    Public Shared Operator Xor(ByVal value1 As ModByte, ByVal value2 As ModByte) As ModByte
        Return New ModByte(value1.value Xor value2.value)
    End Operator
    Public Shared Operator Or(ByVal value1 As ModByte, ByVal value2 As ModByte) As ModByte
        Return New ModByte(value1.value Or value2.value)
    End Operator
    Public Shared Operator Not(ByVal value As ModByte) As ModByte
        Return New ModByte(Not value.value)
    End Operator
    Public Shared Operator >>(ByVal value As ModByte, ByVal offset As Integer) As ModByte
        Return New ModByte(value.value >> offset)
    End Operator
    Public Shared Operator <<(ByVal value As ModByte, ByVal offset As Integer) As ModByte
        Return New ModByte(value.value << offset)
    End Operator
    Public Shared Operator =(ByVal value1 As ModByte, ByVal value2 As ModByte) As Boolean
        Return value1.value = value2.value
    End Operator
    Public Shared Operator <>(ByVal value1 As ModByte, ByVal value2 As ModByte) As Boolean
        Return value1.value <> value2.value
    End Operator
    Public Function ShiftRotateLeft(ByVal offset As Integer) As ModByte
        offset = offset And &H7
        Return New ModByte((value << offset) Or (value >> 8 - offset))
    End Function
    Public Function ShiftRotateRight(ByVal offset As Integer) As ModByte
        offset = offset And &H7
        Return New ModByte((value >> offset) Or (value << 8 - offset))
    End Function
#End Region

#Region "Methods"
    Public Overrides Function GetHashCode() As Integer
        Return (value.GetHashCode)
    End Function
    Public Overrides Function Equals(ByVal obj As Object) As Boolean
        If Not TypeOf obj Is ModByte Then Return False
        Return Me.value = CType(obj, ModByte).value
    End Function
    Public Function EqualsProper(ByVal other As ModByte) As Boolean Implements IEquatable(Of ModByte).Equals
        Return Me.value = other.value
    End Function
#End Region

#Region " -> ModByte"
    Public Shared Narrowing Operator CType(ByVal value As UInt64) As ModByte
        Return New ModByte(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As UInt32) As ModByte
        Return New ModByte(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As UInt16) As ModByte
        Return New ModByte(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As Byte) As ModByte
        Return New ModByte(value)
    End Operator

    Public Shared Narrowing Operator CType(ByVal value As Int64) As ModByte
        Return New ModByte(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As Int32) As ModByte
        Return New ModByte(value)
    End Operator
    Public Shared Narrowing Operator CType(ByVal value As Int16) As ModByte
        Return New ModByte(value)
    End Operator
    Public Shared Widening Operator CType(ByVal value As SByte) As ModByte
        Return New ModByte(value)
    End Operator
#End Region

#Region "ModByte -> "
    Public Shared Widening Operator CType(ByVal value As ModByte) As Byte
        Return value.value
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModByte) As UInt16
        Return value.value
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModByte) As UInt32
        Return value.value
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModByte) As UInt64
        Return value.value
    End Operator

    Public Shared Widening Operator CType(ByVal value As ModByte) As SByte
        Return CSByte(value.value - If(value.value > SByte.MaxValue, &H100, 0))
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModByte) As Int16
        Return value.value
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModByte) As Int32
        Return value.value
    End Operator
    Public Shared Widening Operator CType(ByVal value As ModByte) As Int64
        Return value.value
    End Operator
#End Region
End Structure
