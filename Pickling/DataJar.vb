Namespace Pickling
    '''<summary>The identity jar. Pickles data as itself.</summary>
    Public Class DataJar
        Inherits BaseJar(Of IReadableList(Of Byte))

        Public Overrides Function Pack(Of TValue As IReadableList(Of Byte))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(Me, value, Function() "[{0}]".Frmt(value.ToHexString))
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of IReadableList(Of Byte))
            Return data.Pickled(Me, data, Function() "[{0}]".Frmt(data.ToHexString))
        End Function

        Public Overrides Function ValueToControl(ByVal value As IReadableList(Of Byte)) As Control
            Dim control = New TextBox()
            control.Text = value.ToHexString()
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
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As IReadableList(Of Byte)
            Try
                Return (From word In DirectCast(control, TextBox).Text.Split(" "c)
                        Where word <> ""
                        Select CByte(word.FromHexToUInt64(ByteOrder.BigEndian))
                        ).ToReadableList
            Catch ex As ArgumentException
                Throw New PicklingException("Invalid hex data.", ex)
            End Try
        End Function
    End Class
End Namespace
