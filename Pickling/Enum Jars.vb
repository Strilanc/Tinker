Namespace Pickling
    '''<summary>Pickles Enum values.</summary>
    Public Class EnumJar(Of TEnum As Structure)
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
            Return _subJar.Pack(value).With(jar:=Me, description:=Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of TEnum)
            Dim pickle = _subJar.Parse(data)
            Dim value = DirectCast(pickle.Value, TEnum).AssumeNotNull
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(ValueToString(value), GetType(TEnum)))
            End If
            Return pickle.With(jar:=Me, value:=value, description:=Function() ValueToString(value))
        End Function

        <Pure()>
        Private Function IsDefined(ByVal value As TEnum) As Boolean
            Return If(_isFlagEnum, value.EnumFlagsAreDefined(), value.EnumValueIsDefined())
        End Function
        <Pure()>
        Protected Overridable Function ValueToString(ByVal value As TEnum) As String
            Return If(_isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function

        Public Overrides Function ValueToControl(ByVal value As TEnum) As Control
            If _isFlagEnum Then
                Dim control = New CheckedListBox()
                For Each e In EnumAllFlags(Of TEnum)(onlyDefined:=_checkDefined)
                    control.Items.Add(e.EnumFlagsToString(), isChecked:=value.EnumIncludes(e))
                Next e
                control.Height = control.GetItemHeight(0) * control.Items.Count
                Return control
            Else
                Dim control = New ComboBox()
                control.DropDownStyle = If(_checkDefined, ComboBoxStyle.DropDownList, ComboBoxStyle.DropDown)
                For Each e In EnumValues(Of TEnum)()
                    control.Items.Add(e)
                Next e
                If _checkDefined Then
                    control.SelectedItem = value
                Else
                    control.Text = value.ToString
                End If
                Return control
            End If
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As TEnum
            If _isFlagEnum Then
                Dim c = DirectCast(control, CheckedListBox)
                Dim i = 0
                Dim result As TEnum
                For Each e In EnumAllFlags(Of TEnum)(onlyDefined:=_checkDefined)
                    If c.GetItemChecked(i) Then result = result.EnumWith(e)
                    i += 1
                Next e
                Return result
            Else
                Dim c = DirectCast(control, ComboBox)
                If _checkDefined Then
                    Return DirectCast(c.SelectedItem, TEnum)
                Else
                    Return c.Text.EnumParse(Of TEnum)(ignoreCase:=True)
                End If
            End If
        End Function
    End Class

    '''<summary>Pickles byte Enum types.</summary>
    Public Class EnumByteJar(Of T As Structure)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True)
            MyBase.New(New ByteJar(), checkDefined)
        End Sub
    End Class

    '''<summary>Pickles UInt16 Enum types.</summary>
    Public Class EnumUInt16Jar(Of T As Structure)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            MyBase.New(New UInt16Jar(byteOrder), checkDefined)
        End Sub
    End Class

    '''<summary>Pickles UInt32 Enum types.</summary>
    Public Class EnumUInt32Jar(Of T As Structure)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            MyBase.New(New UInt32Jar(byteOrder), checkDefined)
        End Sub
    End Class

    '''<summary>Pickles UInt64 Enum types.</summary>
    Public Class EnumUInt64Jar(Of T As Structure)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            MyBase.New(New UInt64Jar(byteOrder), checkDefined)
        End Sub
    End Class
End Namespace
