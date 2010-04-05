Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class OrderIdJar
        Inherits BaseJar(Of OrderId)

        Private Shared ReadOnly DataJar As New EnumUInt32Jar(Of OrderId)(checkDefined:=False)

        Public Overrides Function Pack(ByVal value As OrderId) As IEnumerable(Of Byte)
            Return DataJar.Pack(value)
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of OrderId)
            Return DataJar.Parse(data)
        End Function

        Public Overrides Function Describe(ByVal value As OrderId) As String
            If value.EnumValueIsDefined() Then
                Return value.ToString
            ElseIf value >= &HD0000UI AndAlso value < &HE0000UI Then
                Return "order#{0}".Frmt(value - &HD0000UI)
            Else
                Return New ObjectTypeJar().Describe(value)
            End If
        End Function
        Public Overloads Function Parse(ByVal text As String) As OrderId
            Contract.Requires(text IsNot Nothing)
            Try
                Dim enumVal = text.EnumTryParse(Of OrderId)(ignoreCase:=True)
                If enumVal.HasValue Then
                    Return enumVal.Value
                ElseIf text Like New InvariantString("order[#]*") Then
                    Dim orderVal = UInt16.Parse(text.Substring("order#".Length), NumberStyles.Number, CultureInfo.InvariantCulture)
                    Return DirectCast(orderVal + &HD0000UI, OrderId)
                Else
                    Return DirectCast(New ObjectTypeJar().Parse(text), OrderId)
                End If
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("Invalid order id. Must be a defined order (Smart), an explicit order (Order#1), a rawcode (type 'hpea'), or hex (0xABCDEF12).", ex)
            End Try
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of OrderId)
            Dim control = New ComboBox()
            control.DropDownStyle = ComboBoxStyle.DropDown
            For Each e In From v In EnumValues(Of OrderId)()
                          Order By v.ToString()
                control.Items.Add(e)
            Next e
            control.SelectedIndex = 0

            Return New DelegatedValueEditor(Of OrderId)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function() Parse(control.Text),
                setter:=Sub(value)
                            control.SelectedIndex = -1
                            control.Text = Describe(value)
                        End Sub)
        End Function
    End Class
End Namespace
