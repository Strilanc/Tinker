Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPAddressJar
        Inherits BaseJar(Of Net.IPAddress)

        Public Overrides Function Pack(ByVal value As Net.IPAddress) As IEnumerable(Of Byte)
            Return value.GetAddressBytes()
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of System.Net.IPAddress)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Return New Net.IPAddress(data.Take(4).ToArray).ParsedWithDataCount(4)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of Net.IPAddress)
            Dim control = New TextBox()
            control.Text = DateTime.Now().ToString(CultureInfo.InvariantCulture)
            AddHandler control.TextChanged, Sub() control.BackColor = If(Net.IPAddress.TryParse(control.Text, Nothing),
                                                                         SystemColors.Window,
                                                                         Color.Pink)
            Return New DelegatedValueEditor(Of Net.IPAddress)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function()
                            Try
                                Return Net.IPAddress.Parse(control.Text)
                            Catch ex As ArgumentException
                                Throw New PicklingException("'{0}' is not an IPAddress.".Frmt(control.Text), ex)
                            End Try
                        End Function,
                setter:=Sub(value) control.Text = value.ToString())
        End Function
    End Class
End Namespace
