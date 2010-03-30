Namespace Pickling
    '''<summary>Pickles 8-bit unsigned integers.</summary>
    Public NotInheritable Class ByteJar
        Inherits BaseJar(Of Byte)
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
        End Sub

        Public Overrides Function Pack(Of TValue As Byte)(ByVal value As TValue) As IPickle(Of TValue)
            Return value.Pickled(Me, {CByte(value)}.AsReadableList())
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Byte)
            If data.Count < 1 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 1)
            Dim value = datum(0)
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Describe(ByVal value As Byte) As String
            Return If(_showHex,
                      "0x" + value.ToString("X4", CultureInfo.InvariantCulture),
                      value.ToString(CultureInfo.InvariantCulture))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of Byte)
            Dim control = New NumericUpDown()
            control.Minimum = Byte.MinValue
            control.Maximum = Byte.MaxValue
            control.MaximumSize = New Size(50, control.PreferredSize.Height)
            control.Value = 0
            Return New DelegatedValueEditor(Of Byte)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.ValueChanged, Sub() action(),
                getter:=Function() CByte(control.Value),
                setter:=Sub(value) control.Value = value)
        End Function
    End Class

    '''<summary>Pickles 16-bit unsigned integers.</summary>
    Public NotInheritable Class UInt16Jar
        Inherits BaseJar(Of UInt16)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(Of TValue As UInt16)(ByVal value As TValue) As IPickle(Of TValue)
            Return value.Pickled(Me, value.Bytes(byteOrder).AsReadableList)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt16)
            If data.Count < 2 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 2)
            Dim value = datum.ToUInt16(byteOrder)
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Describe(ByVal value As UInt16) As String
            Return If(_showHex,
                      "0x" + value.ToString("X4", CultureInfo.InvariantCulture),
                      value.ToString(CultureInfo.InvariantCulture))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of UInt16)
            Dim control = New NumericUpDown()
            control.Minimum = UInt16.MinValue
            control.Maximum = UInt16.MaxValue
            control.MaximumSize = New Size(70, control.PreferredSize.Height)
            control.Value = 0
            Return New DelegatedValueEditor(Of UInt16)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.ValueChanged, Sub() action(),
                getter:=Function() CUShort(control.Value),
                setter:=Sub(value) control.Value = value)
        End Function
    End Class

    '''<summary>Pickles 32-bit unsigned integers.</summary>
    Public NotInheritable Class UInt32Jar
        Inherits BaseJar(Of UInt32)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(Of TValue As UInt32)(ByVal value As TValue) As IPickle(Of TValue)
            Return value.Pickled(Me, value.Bytes(byteOrder).AsReadableList())
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt32)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = datum.ToUInt32(byteOrder)
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Describe(ByVal value As UInt32) As String
            Return If(_showHex,
                      "0x" + value.ToString("X8", CultureInfo.InvariantCulture),
                      value.ToString(CultureInfo.InvariantCulture))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of UInt32)
            Dim control = New NumericUpDown()
            control.Minimum = UInt32.MinValue
            control.Maximum = UInt32.MaxValue
            control.MaximumSize = New Size(100, control.PreferredSize.Height)
            control.Value = 0
            Return New DelegatedValueEditor(Of UInt32)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.ValueChanged, Sub() action(),
                getter:=Function() CUInt(control.Value),
                setter:=Sub(value) control.Value = value)
        End Function
    End Class

    '''<summary>Pickles 64-bit unsigned integers.</summary>
    Public NotInheritable Class UInt64Jar
        Inherits BaseJar(Of UInt64)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(Of TValue As UInt64)(ByVal value As TValue) As IPickle(Of TValue)
            Dim datum = value.Bytes(byteOrder).AsReadableList
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt64)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = datum.ToUInt64(byteOrder)
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Describe(ByVal value As UInt64) As String
            Return If(_showHex,
                      "0x" + value.ToString("X16", CultureInfo.InvariantCulture),
                      value.ToString(CultureInfo.InvariantCulture))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of UInt64)
            Dim control = New NumericUpDown()
            control.Minimum = UInt64.MinValue
            control.Maximum = UInt64.MaxValue
            control.MaximumSize = New Size(200, control.PreferredSize.Height)
            control.Value = 0
            Return New DelegatedValueEditor(Of UInt64)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.ValueChanged, Sub() action(),
                getter:=Function() CULng(control.Value),
                setter:=Sub(value) control.Value = value)
        End Function
    End Class

    '''<summary>Pickles 32-bit floating point values (singles).</summary>
    Public NotInheritable Class Float32Jar
        Inherits BaseJar(Of Single)

        Public Overrides Function Pack(Of TValue As Single)(ByVal value As TValue) As IPickle(Of TValue)
            Dim data = BitConverter.GetBytes(value).AsReadableList
            Return value.Pickled(Me, data)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Single)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = BitConverter.ToSingle(datum.ToArray, 0)
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Describe(ByVal value As Single) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of Single)
            Dim control = New TextBox()
            control.Text = CSng(0).ToString("r", CultureInfo.InvariantCulture)
            AddHandler control.TextChanged, Sub() control.BackColor = If(Single.TryParse(control.Text, NumberStyles.Float, CultureInfo.InvariantCulture, 0),
                                                                         SystemColors.Window,
                                                                         Color.Pink)
            Return New DelegatedValueEditor(Of Single)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function()
                            Try
                                Return Single.Parse(control.Text, NumberStyles.Float, CultureInfo.InvariantCulture)
                            Catch ex As ArgumentException
                                Throw New PicklingException("'{0}' is not a Single-precision value.".Frmt(control.Text), ex)
                            End Try
                        End Function,
                setter:=Sub(value) control.Text = value.ToString("r", CultureInfo.InvariantCulture))
        End Function
    End Class

    '''<summary>Pickles 64-bit floating point values (doubles).</summary>
    Public NotInheritable Class Float64Jar
        Inherits BaseJar(Of Double)

        Public Overrides Function Pack(Of TValue As Double)(ByVal value As TValue) As IPickle(Of TValue)
            Dim data = BitConverter.GetBytes(value).AsReadableList
            Return value.Pickled(Me, data)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Double)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = BitConverter.ToDouble(datum.ToArray, 0)
            Return value.Pickled(Me, datum)
        End Function

        Public Overrides Function Describe(ByVal value As Double) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of Double)
            Dim control = New TextBox()
            control.Text = CDbl(0).ToString("r", CultureInfo.InvariantCulture)
            AddHandler control.TextChanged, Sub() control.BackColor = If(Double.TryParse(control.Text, NumberStyles.Float, CultureInfo.InvariantCulture, 0),
                                                                         SystemColors.Window,
                                                                         Color.Pink)
            Return New DelegatedValueEditor(Of Double)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.TextChanged, Sub() action(),
                getter:=Function()
                            Try
                                Return Double.Parse(control.Text, NumberStyles.Float, CultureInfo.InvariantCulture)
                            Catch ex As ArgumentException
                                Throw New PicklingException("'{0}' is not a Double-precision value.".Frmt(control.Text), ex)
                            End Try
                        End Function,
                setter:=Sub(value) control.Text = value.ToString("r", CultureInfo.InvariantCulture))
        End Function
    End Class
End Namespace
