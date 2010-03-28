Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public Class IPAddressJar
        Inherits BaseJar(Of Net.IPAddress)

        Public Overrides Function Pack(Of TValue As Net.IPAddress)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim data = value.GetAddressBytes()
            Return value.Pickled(Me, data.AsReadableList)
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of System.Net.IPAddress)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = New Net.IPAddress(datum.ToArray)
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function ValueToControl(ByVal value As Net.IPAddress) As Control
            Dim control = New TextBox()
            control.Text = value.ToString()
            AddHandler control.TextChanged, Sub()
                                                If Net.IPAddress.TryParse(control.Text, Nothing) Then
                                                    control.BackColor = SystemColors.Window
                                                Else
                                                    control.BackColor = Color.Pink
                                                End If
                                            End Sub
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As Net.IPAddress
            Dim result As Net.IPAddress = Nothing
            Dim text = DirectCast(control, TextBox).Text
            If Not Net.IPAddress.TryParse(control.Text, result) Then
                Throw New PicklingException("'{0}' is not parsable as a Date.".Frmt(text))
            End If
            Return result
        End Function
    End Class
End Namespace
