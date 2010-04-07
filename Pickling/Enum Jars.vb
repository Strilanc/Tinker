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

        Public Overrides Function Pack(ByVal value As TEnum) As IEnumerable(Of Byte)
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(Describe(value), GetType(TEnum)))
            End If
            Return _subJar.Pack(value)
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of TEnum)
            Dim parsed = _subJar.Parse(data)
            Dim value = DirectCast(parsed.Value, TEnum).AssumeNotNull
            If _checkDefined AndAlso Not IsDefined(value) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(Describe(value), GetType(TEnum)))
            End If
            Return parsed.WithValue(value)
        End Function

        Public Overrides Function Describe(ByVal value As TEnum) As String
            Return If(_isFlagEnum, value.EnumFlagsToString(), value.ToString)
        End Function
        Public Overrides Function Parse(ByVal text As String) As TEnum
            Dim result As TEnum
            If _isFlagEnum Then
                Dim sflags = EnumAllFlags(Of TEnum)(onlyDefined:=_checkDefined).ToDictionary(
                    keySelector:=Function(e) New InvariantString(Describe(e)),
                    elementSelector:=Function(e) e)
                Dim flags = From word In text.Split(","c)
                            Select sflags(word.Trim)
                result = flags.Aggregate(Of TEnum)(Nothing, Function(e1, e2) e1.EnumWith(e2))
            Else
                result = text.EnumParse(Of TEnum)(ignoreCase:=True)
            End If

            If _checkDefined AndAlso Not IsDefined(result) Then
                Throw New PicklingException("Enumeration with value {0} of type {1} is not defined.".Frmt(Describe(result), GetType(TEnum)))
            End If
            Return result
        End Function

        <Pure()>
        Private Function IsDefined(ByVal value As TEnum) As Boolean
            Return If(_isFlagEnum, value.EnumFlagsAreDefined(), value.EnumValueIsDefined())
        End Function

        <ContractVerification(False)>
        Private Function MakeFlagsControl() As IValueEditor(Of TEnum)
            Contract.Requires(_isFlagEnum)
            Contract.Ensures(Contract.Result(Of IValueEditor(Of TEnum))() IsNot Nothing)

            Dim flags = EnumAllFlags(Of TEnum)(onlyDefined:=_checkDefined).ZipWithIndexes()
            Dim control = New CheckedListBox()
            For Each e In flags
                control.Items.Add(e.Item1.EnumFlagsToString())
            Next e
            control.Height = control.GetItemHeight(0) * Math.Min(10, control.Items.Count)

            Return New DelegatedValueEditor(Of TEnum)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.ItemCheck, Sub() action(),
                getter:=Function() flags.Aggregate(Of TEnum)(Nothing, Function(e1, e2) If(control.GetItemChecked(e2.Item2),
                                                                                          e1.EnumWith(e2.Item1),
                                                                                          e1)),
                setter:=Sub(value)
                            For Each pair In flags
                                control.SetItemChecked(pair.Item2, value.EnumIncludes(pair.Item1))
                            Next pair
                        End Sub,
                disposer:=Sub() control.Dispose())
        End Function
        Private Function MakeDefinedValueControl() As IValueEditor(Of TEnum)
            Contract.Requires(Not _isFlagEnum)
            Contract.Requires(_checkDefined)
            Contract.Ensures(Contract.Result(Of IValueEditor(Of TEnum))() IsNot Nothing)

            Dim control = New ComboBox()
            control.DropDownStyle = ComboBoxStyle.DropDownList
            For Each e In From v In EnumValues(Of TEnum)()
                          Order By v.ToString()
                control.Items.Add(e)
            Next e
            control.SelectedIndex = 0

            Return New DelegatedValueEditor(Of TEnum)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.SelectedIndexChanged, Sub() action(),
                getter:=Function() DirectCast(control.SelectedItem, TEnum),
                setter:=Sub(value) control.SelectedItem = value,
                disposer:=Sub() control.Dispose())
        End Function
        Private Function MakeUndefinedValueControl() As IValueEditor(Of TEnum)
            Contract.Requires(Not _isFlagEnum)
            Contract.Requires(Not _checkDefined)
            Contract.Ensures(Contract.Result(Of IValueEditor(Of TEnum))() IsNot Nothing)

            Dim control = New ComboBox()
            control.DropDownStyle = ComboBoxStyle.DropDown
            For Each e In From v In EnumValues(Of TEnum)()
                          Order By v.ToString()
                control.Items.Add(e)
            Next e
            control.SelectedIndex = 0

            Return New DelegatedValueEditor(Of TEnum)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function()
                            Try
                                Return control.Text.EnumParse(Of TEnum)(ignoreCase:=True)
                            Catch ex As ArgumentException
                                Throw New PicklingException("'{0}' is not a valid {1}".Frmt(control.Text, GetType(TEnum)), ex)
                            End Try
                        End Function,
                setter:=Sub(value)
                            control.SelectedIndex = -1
                            control.Text = Describe(value)
                        End Sub,
                disposer:=Sub() control.Dispose())
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of TEnum)
            If _isFlagEnum Then
                Return MakeFlagsControl()
            ElseIf _checkDefined Then
                Return MakeDefinedValueControl()
            Else
                Return MakeUndefinedValueControl()
            End If
        End Function
    End Class

    '''<summary>Pickles byte Enum types.</summary>
    Public NotInheritable Class EnumByteJar(Of T As Structure)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True)
            MyBase.New(New ByteJar(), checkDefined)
        End Sub
    End Class

    '''<summary>Pickles UInt16 Enum types.</summary>
    Public NotInheritable Class EnumUInt16Jar(Of T As Structure)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            MyBase.New(New UInt16Jar(byteOrder), checkDefined)
        End Sub
    End Class

    '''<summary>Pickles UInt32 Enum types.</summary>
    Public NotInheritable Class EnumUInt32Jar(Of T As Structure)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            MyBase.New(New UInt32Jar(byteOrder), checkDefined)
        End Sub
    End Class

    '''<summary>Pickles UInt64 Enum types.</summary>
    Public NotInheritable Class EnumUInt64Jar(Of T As Structure)
        Inherits EnumJar(Of T)
        Public Sub New(Optional ByVal checkDefined As Boolean = True,
                       Optional ByVal byteOrder As ByteOrder = ByteOrder.LittleEndian)
            MyBase.New(New UInt64Jar(byteOrder), checkDefined)
        End Sub
    End Class
End Namespace
