Namespace Pickling
    '''<summary>The identity jar. Pickles data as itself.</summary>
    Public Class DataJar
        Inherits BaseJar(Of IReadableList(Of Byte))

        Public Overrides Function Pack(ByVal value As IReadableList(Of Byte)) As IEnumerable(Of Byte)
            Return value
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of Byte))
            Return data.Pickled(Me, data)
        End Function

        Public Overrides Function Describe(ByVal value As IReadableList(Of Byte)) As String
            Return "[{0}]".Frmt(value.ToHexString)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of IReadableList(Of Byte))
            Dim control = New TextBox()
            control.Text = ""
            AddHandler control.TextChanged, Sub()
                                                Try
                                                    Dim v = (From word In DirectCast(control, TextBox).Text.Split(" "c)
                                                             Where word <> ""
                                                             Select CByte(word.FromHexToUInt64(ByteOrder.BigEndian))
                                                             ).ToReadableList
                                                    control.BackColor = SystemColors.Window
                                                Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                                                           TypeOf ex Is OverflowException
                                                    control.BackColor = Color.Pink
                                                End Try
                                            End Sub
            Return New DelegatedValueEditor(Of IReadableList(Of Byte))(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function()
                            Try
                                Return (From word In DirectCast(control, TextBox).Text.Split(" "c)
                                        Where word <> ""
                                        Select CByte(word.FromHexToUInt64(ByteOrder.BigEndian))
                                        ).ToReadableList
                            Catch ex As ArgumentException
                                Throw New PicklingException("Invalid hex data.", ex)
                            End Try
                        End Function,
                setter:=Sub(value) control.Text = value.ToHexString)
        End Function
    End Class
End Namespace
