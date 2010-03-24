Namespace Pickling
    '''<summary>Pickles Enum values.</summary>
    Public Class EnumJar(Of TEnum)
        Inherits BaseJar(Of TEnum)

        Private ReadOnly _subJar As ISimpleJar
        Private ReadOnly _checkDefined As Boolean
        Private ReadOnly _isFlagEnum As Boolean = GetType(TEnum).GetCustomAttributes(GetType(FlagsAttribute), inherit:=False).Any

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As ISimpleJar,
                       ByVal checkDefined As Boolean)
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
            Me._checkDefined = checkDefined
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As TEnum)(ByVal value As TValue) As IPickle(Of TValue)
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(ValueToString(value), GetType(TEnum)))
            End If
            Dim pickle = _subJar.Pack(value)
            Return value.Pickled(pickle.Data, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of TEnum)
            Dim pickle = _subJar.Parse(data)
            Dim value = DirectCast(pickle.Value, TEnum).AssumeNotNull
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(ValueToString(value), GetType(TEnum)))
            End If
            Return value.Pickled(pickle.Data, Function() ValueToString(value))
        End Function

        <Pure()>
        Private Function IsDefined(ByVal value As TEnum) As Boolean
            Return If(_isFlagEnum, value.EnumFlagsAreDefined(), value.EnumValueIsDefined())
        End Function
        <Pure()>
        Protected Overridable Function ValueToString(ByVal value As TEnum) As String
            Return If(_isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class

    '''<summary>Pickles byte Enum types.</summary>
    Public Class EnumByteJar(Of T)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True)
            MyBase.New(New ByteJar(), checkDefined)
        End Sub
    End Class

    '''<summary>Pickles UInt16 Enum types.</summary>
    Public Class EnumUInt16Jar(Of T)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            MyBase.New(New UInt16Jar(byteOrder), checkDefined)
        End Sub
    End Class

    '''<summary>Pickles UInt32 Enum types.</summary>
    Public Class EnumUInt32Jar(Of T)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            MyBase.New(New UInt32Jar(byteOrder), checkDefined)
        End Sub
    End Class

    '''<summary>Pickles UInt64 Enum types.</summary>
    Public Class EnumUInt64Jar(Of T)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            MyBase.New(New UInt64Jar(byteOrder), checkDefined)
        End Sub
    End Class
End Namespace
