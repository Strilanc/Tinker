Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class ObjectTypeJar
        Inherits BaseJar(Of UInt32)

        Private Shared ReadOnly DataJar As New UInt32Jar()

        Public Overrides Function Pack(ByVal value As UInteger) As IRist(Of Byte)
            Return DataJar.Pack(value)
        End Function
        Public Overrides Function Parse(ByVal data As IRist(Of Byte)) As ParsedValue(Of UInt32)
            Return DataJar.Parse(data)
        End Function

        Public Overrides Function Describe(ByVal value As UInt32) As String
            If value.Bytes.All(Function(b) b >= 32 AndAlso b <= 127) Then
                Return "type '{0}'".Frmt(New String(System.Text.Encoding.ASCII.GetChars(value.Bytes(ByteOrder.BigEndian).ToArray())))
            Else
                Return "0x{0}".Frmt(value.ToString("X", CultureInfo.InvariantCulture))
            End If
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-263")>
        Public Overrides Function Parse(ByVal text As String) As UInt32
            Try
                If text Like New InvariantString("type '????'") Then
                    Contract.Assume(text.Length >= "type '".Length + "????".Length)
                    Dim bytes = System.Text.Encoding.ASCII.GetBytes(text.Substring("type '".Length, "????".Length))
                    Return bytes.ToUInt32(ByteOrder.BigEndian)
                ElseIf text Like New InvariantString("0x*") Then
                    Contract.Assume(text.Length >= "0x".Length)
                    Return UInt32.Parse(text.Substring("0x".Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                Else
                    Return UInt32.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture)
                End If
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("Invalid game object type. Must be in rawcode (type 'hpea') or hex (0xABCDEF12) format.", ex)
            End Try
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of UInt32)
            Dim control = New TextBox()
            control.Text = "type 'hpea'"
            Return New DelegatedValueEditor(Of UInt32)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function() Parse(control.Text),
                setter:=Sub(value) control.Text = Describe(value),
                disposer:=Sub() control.Dispose())
        End Function
    End Class
End Namespace
