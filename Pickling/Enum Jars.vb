Namespace Pickling
    '''<summary>Pickles byte enumeration types.</summary>
    Public Class EnumByteJar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly isFlagEnum As Boolean

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
            Me.isFlagEnum = GetType(T).GetCustomAttributes(GetType(FlagsAttribute), inherit:=False).Any
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(Me.Name, value, {CByte(CType(value, Object))}.AsReadableList(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < 1 Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, 1)
            Dim value = CType(CType(datum(0), Object), T)
            Return New Pickle(Of T)(Me.Name, value.AssumeNotNull, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class

    '''<summary>Pickles UInt16 enumeration types.</summary>
    Public Class EnumUInt16Jar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly isFlagEnum As Boolean

        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Me.byteOrder = byteOrder
            Me.isFlagEnum = GetType(T).GetCustomAttributes(GetType(FlagsAttribute), inherit:=False).Any
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(Me.Name, value, CUShort(CType(value, Object)).Bytes(byteOrder).AsReadableList(), Function() ValueToString(value))
        End Function

        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < 2 Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, 2)
            Dim value = CType(CType(datum.ToUInt16(byteOrder), Object), T)
            Return New Pickle(Of T)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class

    '''<summary>Pickles UInt32 enumeration types.</summary>
    Public Class EnumUInt32Jar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly isFlagEnum As Boolean

        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Me.byteOrder = byteOrder
            Me.isFlagEnum = GetType(T).GetCustomAttributes(GetType(FlagsAttribute), inherit:=False).Any
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(Me.Name, value, CUInt(CType(value, Object)).Bytes(byteOrder).AsReadableList(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < 4 Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, 4)
            Dim value = CType(CType(datum.ToUInt32(byteOrder), Object), T)
            Contract.Assume(value IsNot Nothing)
            Return New Pickle(Of T)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class

    '''<summary>Pickles UInt64 enumeration types.</summary>
    Public Class EnumUInt64Jar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly isFlagEnum As Boolean

        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Me.byteOrder = byteOrder
            Me.isFlagEnum = GetType(T).GetCustomAttributes(GetType(FlagsAttribute), inherit:=False).Any
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(Me.Name, value, CULng(CType(value, Object)).Bytes(byteOrder).AsReadableList(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < 8 Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, 8)
            Dim value = CType(CType(datum.ToUInt64(byteOrder), Object), T)
            Contract.Assume(value IsNot Nothing)
            Return New Pickle(Of T)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class
End Namespace
