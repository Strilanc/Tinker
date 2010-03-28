Namespace Pickling
    '''<summary>Pickles 8-bit unsigned integers.</summary>
    Public Class ByteJar
        Inherits BaseJar(Of Byte)
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As Byte)(ByVal value As TValue) As IPickle(Of TValue)
            Return value.Pickled(Me, {CByte(value)}.AsReadableList(), Function() ValueToString(value))
        End Function

        <ContractVerification(False)>
        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Byte)
            If data.Count < 1 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 1)
            Dim value = datum(0)
            Return value.Pickled(Me, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As Byte) As String
            If _showHex Then
                Return "0x" + value.ToString("X2", CultureInfo.InvariantCulture)
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Overrides Function ValueToControl(ByVal value As Byte) As Control
            Dim control = New NumericUpDown()
            control.Minimum = Byte.MinValue
            control.Maximum = Byte.MaxValue
            control.Value = value
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As Byte
            Return CByte(DirectCast(control, NumericUpDown).Value)
        End Function
    End Class

    '''<summary>Pickles 16-bit unsigned integers.</summary>
    Public Class UInt16Jar
        Inherits BaseJar(Of UInt16)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As UInt16)(ByVal value As TValue) As IPickle(Of TValue)
            Return value.Pickled(Me, value.Bytes(byteOrder).AsReadableList, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt16)
            If data.Count < 2 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 2)
            Dim value = datum.ToUInt16(byteOrder)
            Return value.Pickled(Me, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt16) As String
            If _showHex Then
                Return "0x" + value.ToString("X4", CultureInfo.InvariantCulture)
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Overrides Function ValueToControl(ByVal value As UInt16) As Control
            Dim control = New NumericUpDown()
            control.Minimum = UInt16.MinValue
            control.Maximum = UInt16.MaxValue
            control.Value = value
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As UInt16
            Return CUShort(DirectCast(control, NumericUpDown).Value)
        End Function
    End Class

    '''<summary>Pickles 32-bit unsigned integers.</summary>
    Public Class UInt32Jar
        Inherits BaseJar(Of UInt32)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As UInt32)(ByVal value As TValue) As IPickle(Of TValue)
            Return value.Pickled(Me, value.Bytes(byteOrder).AsReadableList(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt32)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = datum.ToUInt32(byteOrder)
            Return value.Pickled(Me, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt32) As String
            If _showHex Then
                Return "0x" + value.ToString("X8", CultureInfo.InvariantCulture)
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Overrides Function ValueToControl(ByVal value As UInt32) As Control
            Dim control = New NumericUpDown()
            control.Minimum = UInt32.MinValue
            control.Maximum = UInt32.MaxValue
            control.Value = value
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As UInt32
            Return CUInt(DirectCast(control, NumericUpDown).Value)
        End Function
    End Class

    '''<summary>Pickles 64-bit unsigned integers.</summary>
    Public Class UInt64Jar
        Inherits BaseJar(Of UInt64)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As UInt64)(ByVal value As TValue) As IPickle(Of TValue)
            Dim datum = value.Bytes(byteOrder).AsReadableList
            Return value.Pickled(Me, datum, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt64)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = datum.ToUInt64(byteOrder)
            Return value.Pickled(Me, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt64) As String
            If _showHex Then
                Return "0x" + value.ToString("X16", CultureInfo.InvariantCulture)
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Overrides Function ValueToControl(ByVal value As UInt64) As Control
            Dim control = New NumericUpDown()
            control.Minimum = UInt64.MinValue
            control.Maximum = UInt64.MaxValue
            control.Value = value
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As UInt64
            Return CULng(DirectCast(control, NumericUpDown).Value)
        End Function
    End Class

    '''<summary>Pickles 32-bit floating point values (singles).</summary>
    Public Class Float32Jar
        Inherits BaseJar(Of Single)

        Public NotOverridable Overrides Function Pack(Of TValue As Single)(ByVal value As TValue) As IPickle(Of TValue)
            Dim data = BitConverter.GetBytes(value).AsReadableList
            Return value.Pickled(Me, data, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Single)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = BitConverter.ToSingle(datum.ToArray, 0)
            Return value.Pickled(Me, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As Single) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function

        Public Overrides Function ValueToControl(ByVal value As Single) As Control
            Dim control = New TextBox()
            control.Text = value.ToString("r", CultureInfo.InvariantCulture)
            AddHandler control.TextChanged, Sub()
                                                If Single.TryParse(control.Text, NumberStyles.Float, CultureInfo.InvariantCulture, 0) Then
                                                    control.BackColor = SystemColors.Window
                                                Else
                                                    control.BackColor = Color.Pink
                                                End If
                                            End Sub
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As Single
            Dim result As Single
            Dim text = DirectCast(control, TextBox).Text
            If Not Single.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, result) Then
                Throw New PicklingException("'{0}' is not parsable as a Single.".Frmt(text))
            End If
            Return result
        End Function
    End Class

    '''<summary>Pickles 64-bit floating point values (doubles).</summary>
    Public Class Float64Jar
        Inherits BaseJar(Of Double)

        Public NotOverridable Overrides Function Pack(Of TValue As Double)(ByVal value As TValue) As IPickle(Of TValue)
            Dim data = BitConverter.GetBytes(value).AsReadableList
            Return value.Pickled(Me, data, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Double)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = BitConverter.ToDouble(datum.ToArray, 0)
            Return value.Pickled(Me, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As Double) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function

        Public Overrides Function ValueToControl(ByVal value As Double) As Control
            Dim control = New TextBox()
            control.Text = value.ToString("r", CultureInfo.InvariantCulture)
            AddHandler control.TextChanged, Sub()
                                                If Double.TryParse(control.Text, NumberStyles.Float, CultureInfo.InvariantCulture, 0) Then
                                                    control.BackColor = SystemColors.Window
                                                Else
                                                    control.BackColor = Color.Pink
                                                End If
                                            End Sub
            Return control
        End Function
        Public Overrides Function ControlToValue(ByVal control As Control) As Double
            Dim result As Double
            Dim text = DirectCast(control, TextBox).Text
            If Not Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, result) Then
                Throw New PicklingException("'{0}' is not parsable as a Double.".Frmt(text))
            End If
            Return result
        End Function
    End Class
End Namespace
