Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class FileTimeJar
        Inherits BaseJar(Of Date)

        Public Overrides Function Pack(Of TValue As Date)(ByVal value As TValue) As IPickle(Of TValue)
            Dim data = DirectCast(value, Date).ToFileTime.BitwiseToUInt64.Bytes().AsReadableList
            Return value.Pickled(Me, data)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Date)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = Date.FromFileTime(datum.ToUInt64.BitwiseToInt64)
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function ValueToControl(ByVal value As Date) As Control
            Dim control = New TextBox()
            control.Text = value.ToString(CultureInfo.InvariantCulture)
            AddHandler control.TextChanged, Sub()
                                                If Date.TryParse(control.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, Nothing) Then
                                                    control.BackColor = SystemColors.Window
                                                Else
                                                    control.BackColor = Color.Pink
                                                End If
                                            End Sub
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As Date
            Dim result As Date
            Dim text = DirectCast(control, TextBox).Text
            If Not Date.TryParse(control.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, result) Then
                Throw New PicklingException("'{0}' is not parsable as a Date.".Frmt(text))
            End If
            Return result
        End Function
    End Class
End Namespace
