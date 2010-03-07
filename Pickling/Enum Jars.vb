Namespace Pickling
    '''<summary>Pickles byte enumeration types.</summary>
    Public Class EnumByteJar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly _byteOrder As ByteOrder
        Private ReadOnly _isFlagEnum As Boolean
        Private ReadOnly _checkDefined As Boolean

        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            Me._byteOrder = byteOrder
            Me._checkDefined = checkDefined
            Me._isFlagEnum = GetType(T).GetCustomAttributes(GetType(FlagsAttribute), inherit:=False).Any
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            If _checkDefined AndAlso Not IsDefined(value) Then Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(value, GetType(T)))
            Dim data = {CType(CType(value, Object), Byte)}.AsReadableList()
            Return value.Pickled(data, Function() ValueToString(value))
        End Function

        <ContractVerification(False)>
        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < 1 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 1)
            Dim value = CType(CType(datum(0), Object), T)
            If _checkDefined AndAlso Not IsDefined(value) Then Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(value, GetType(T)))
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(datum, Function() ValueToString(value))
        End Function

        <Pure()>
        Private Function IsDefined(ByVal value As T) As Boolean
            If _isFlagEnum Then
                Dim n = CType(CType(value, Object), Byte)
                Return (From i In Enumerable.Range(0, 8)
                        Select v = CByte(1) << i
                        Where (n And v) <> 0
                        Where Not CType(CType(v, Object), T).EnumValueIsDefined()).None
            Else
                Return value.EnumValueIsDefined()
            End If
        End Function
        <Pure()>
        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(_isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class

    '''<summary>Pickles UInt16 enumeration types.</summary>
    Public Class EnumUInt16Jar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly _byteOrder As ByteOrder
        Private ReadOnly _isFlagEnum As Boolean
        Private ReadOnly _checkDefined As Boolean

        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            Me._byteOrder = byteOrder
            Me._checkDefined = checkDefined
            Me._isFlagEnum = GetType(T).GetCustomAttributes(GetType(FlagsAttribute), inherit:=False).Any
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(ValueToString(value), GetType(T)))
            End If
            Dim data = CType(CType(value, Object), UInt16).Bytes(_byteOrder).AsReadableList()
            Return value.Pickled(data, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < 2 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 2)
            Dim value = CType(CType(datum.ToUInt16(_byteOrder), Object), T)
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(ValueToString(value), GetType(T)))
            End If
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(datum, Function() ValueToString(value))
        End Function

        <Pure()>
        Private Function IsDefined(ByVal value As T) As Boolean
            If _isFlagEnum Then
                Dim n = CType(CType(value, Object), UInt16)
                Return (From i In Enumerable.Range(0, 16)
                        Select v = 1US << i
                        Where (n And v) <> 0
                        Where Not CType(CType(v, Object), T).EnumValueIsDefined()).None
            Else
                Return value.EnumValueIsDefined()
            End If
        End Function
        <Pure()>
        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(_isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class

    '''<summary>Pickles UInt32 enumeration types.</summary>
    Public Class EnumUInt32Jar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly _byteOrder As ByteOrder
        Private ReadOnly _isFlagEnum As Boolean
        Private ReadOnly _checkDefined As Boolean

        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            Me._byteOrder = byteOrder
            Me._checkDefined = checkDefined
            Me._isFlagEnum = GetType(T).GetCustomAttributes(GetType(FlagsAttribute), inherit:=False).Any
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(ValueToString(value), GetType(T)))
            End If
            Dim data = CType(CType(value, Object), UInt32).Bytes(_byteOrder).AsReadableList()
            Return value.Pickled(data, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = CType(CType(datum.ToUInt32(_byteOrder), Object), T)
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(ValueToString(value), GetType(T)))
            End If
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(datum, Function() ValueToString(value))
        End Function

        <Pure()>
        Private Function IsDefined(ByVal value As T) As Boolean
            If _isFlagEnum Then
                Dim n = CType(CType(value, Object), UInt32)
                Return (From i In Enumerable.Range(0, 32)
                        Select v = 1UI << i
                        Where (n And v) <> 0
                        Where Not CType(CType(v, Object), T).EnumValueIsDefined()).None
            Else
                Return value.EnumValueIsDefined()
            End If
        End Function
        <Pure()>
        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(_isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class

    '''<summary>Pickles UInt64 enumeration types.</summary>
    Public Class EnumUInt64Jar(Of T)
        Inherits BaseJar(Of T)
        Private ReadOnly _byteOrder As ByteOrder
        Private ReadOnly _isFlagEnum As Boolean
        Private ReadOnly _checkDefined As Boolean

        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            Me._byteOrder = byteOrder
            Me._checkDefined = checkDefined
            Me._isFlagEnum = GetType(T).GetCustomAttributes(GetType(FlagsAttribute), inherit:=False).Any
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As T)(ByVal value As TValue) As IPickle(Of TValue)
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(ValueToString(value), GetType(T)))
            End If
            Dim data = CType(CType(value, Object), UInt64).Bytes(_byteOrder).AsReadableList()
            Return value.Pickled(data, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of T)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = CType(CType(datum.ToUInt64(_byteOrder), Object), T)
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(ValueToString(value), GetType(T)))
            End If
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(datum, Function() ValueToString(value))
        End Function

        <Pure()>
        Private Function IsDefined(ByVal value As T) As Boolean
            If _isFlagEnum Then
                Dim n = CType(CType(value, Object), UInt64)
                Return (From i In Enumerable.Range(0, 64)
                        Select v = 1UL << i
                        Where (n And v) <> 0
                        Where Not CType(CType(v, Object), T).EnumValueIsDefined()).None
            Else
                Return value.EnumValueIsDefined()
            End If
        End Function
        <Pure()>
        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(_isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class
End Namespace
