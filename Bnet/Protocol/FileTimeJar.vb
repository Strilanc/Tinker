Imports Tinker.Pickling

Namespace Bnet.Protocol
    Public NotInheritable Class FileTimeJar
        Inherits BaseJar(Of DateTime)

        Public Overrides Function Pack(ByVal value As DateTime) As IEnumerable(Of Byte)
            Return value.ToFileTime.BitwiseToUInt64.Bytes()
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of DateTime)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Return DateTime.FromFileTime(data.Take(8).ToUInt64.BitwiseToInt64).ParsedWithDataCount(8)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of DateTime)
            Dim control = New TextBox()
            control.Text = DateTime.Now().ToString(CultureInfo.InvariantCulture)
            AddHandler control.TextChanged, Sub() control.BackColor = If(Date.TryParse(control.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, Nothing),
                                                                         SystemColors.Window,
                                                                         Color.Pink)
            Return New DelegatedValueEditor(Of DateTime)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function()
                            Try
                                Return DateTime.Parse(control.Text, CultureInfo.InvariantCulture, DateTimeStyles.None)
                            Catch ex As ArgumentException
                                Throw New PicklingException("'{0}' is not a DateTime.".Frmt(control.Text), ex)
                            End Try
                        End Function,
                setter:=Sub(value) control.Text = value.ToString(CultureInfo.InvariantCulture))
        End Function
    End Class
End Namespace
