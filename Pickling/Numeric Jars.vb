Namespace Pickling
    '''<summary>Pickles 8-bit unsigned integers.</summary>
    Public NotInheritable Class ByteJar
        Inherits BaseFixedSizeJar(Of Byte)
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional showHex As Boolean = False)
            Me._showHex = showHex
        End Sub

        Public Overrides Function Pack(value As Byte) As IRist(Of Byte)
            Return MakeRist(value)
        End Function

        Protected Overrides ReadOnly Property DataSize As UInt16
            Get
                Return 1
            End Get
        End Property
        Protected Overrides Function FixedSizeParse(data As IRist(Of Byte)) As Byte
            Return data.Single
        End Function

        Public Overrides Function Describe(value As Byte) As String
            Return If(_showHex,
                      "0x" + value.ToString("X4", CultureInfo.InvariantCulture),
                      value.ToString(CultureInfo.InvariantCulture))
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-164")>
        Public Overrides Function Parse(text As String) As Byte
            Try
                If New InvariantString(text).StartsWith("0x") Then
                    Contract.Assume(text.Length >= 2)
                    Return Byte.Parse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                Else
                    Return Byte.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture)
                End If
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("'{0}' is not a Byte value.".Frmt(text), ex)
            End Try
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of Byte)
            Dim control = New NumericUpDown()
            control.Minimum = Byte.MinValue
            control.Maximum = Byte.MaxValue
            control.MaximumSize = New Size(50, control.PreferredSize.Height)
            control.Hexadecimal = _showHex
            control.Value = 0
            Return New DelegatedValueEditor(Of Byte)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.ValueChanged, Sub() action(),
                getter:=Function() CByte(control.Value),
                setter:=Sub(value) control.Value = value,
                disposer:=Sub() control.Dispose())
        End Function
    End Class

    '''<summary>Pickles 16-bit unsigned integers.</summary>
    Public NotInheritable Class UInt16Jar
        Inherits BaseFixedSizeJar(Of UInt16)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(value As UShort) As IRist(Of Byte)
            Return value.Bytes(byteOrder)
        End Function

        Protected Overrides ReadOnly Property DataSize As UInt16
            Get
                Return 2
            End Get
        End Property
        <SuppressMessage("Microsoft.Contracts", "Ensures-47-26")>
        Protected Overrides Function FixedSizeParse(data As IRist(Of Byte)) As UInt16
            Contract.Assume(data.Count = 2)
            Return data.ToUInt16(byteOrder)
        End Function

        Public Overrides Function Describe(value As UInt16) As String
            Return If(_showHex,
                      "0x" + value.ToString("X4", CultureInfo.InvariantCulture),
                      value.ToString(CultureInfo.InvariantCulture))
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-164")>
        Public Overrides Function Parse(text As String) As UInt16
            Try
                If New InvariantString(text).StartsWith("0x") Then
                    Contract.Assume(text.Length >= 2)
                    Return UInt16.Parse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                Else
                    Return UInt16.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture)
                End If
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("'{0}' is not a UInt16 value.".Frmt(text), ex)
            End Try
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of UInt16)
            Dim control = New NumericUpDown()
            control.Minimum = UInt16.MinValue
            control.Maximum = UInt16.MaxValue
            control.MaximumSize = New Size(70, control.PreferredSize.Height)
            control.Hexadecimal = _showHex
            control.Value = 0
            Return New DelegatedValueEditor(Of UInt16)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.ValueChanged, Sub() action(),
                getter:=Function() CUShort(control.Value),
                setter:=Sub(value) control.Value = value,
                disposer:=Sub() control.Dispose())
        End Function
    End Class

    '''<summary>Pickles 32-bit unsigned integers.</summary>
    Public NotInheritable Class UInt32Jar
        Inherits BaseFixedSizeJar(Of UInt32)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(value As UInt32) As IRist(Of Byte)
            Return value.Bytes(byteOrder)
        End Function

        Protected Overrides ReadOnly Property DataSize As UInt16
            Get
                Return 4
            End Get
        End Property
        Protected Overrides Function FixedSizeParse(data As IRist(Of Byte)) As UInt32
            Contract.Assume(data.Count = 4)
            Return data.ToUInt32(byteOrder)
        End Function

        Public Overrides Function Describe(value As UInt32) As String
            Return If(_showHex,
                      "0x" + value.ToString("X8", CultureInfo.InvariantCulture),
                      value.ToString(CultureInfo.InvariantCulture))
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-164")>
        Public Overrides Function Parse(text As String) As UInt32
            Try
                If New InvariantString(text).StartsWith("0x") Then
                    Contract.Assume(text.Length >= 2)
                    Return UInt32.Parse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                Else
                    Return UInt32.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture)
                End If
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("'{0}' is not a UInt32 value.".Frmt(text), ex)
            End Try
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of UInt32)
            Dim control = New NumericUpDown()
            control.Minimum = UInt32.MinValue
            control.Maximum = UInt32.MaxValue
            control.MaximumSize = New Size(100, control.PreferredSize.Height)
            control.Hexadecimal = _showHex
            control.Value = 0
            Return New DelegatedValueEditor(Of UInt32)(
                control:=control,
                eventAdder:=Sub(action) AddHandler control.ValueChanged, Sub() action(),
                getter:=Function() CUInt(control.Value),
                setter:=Sub(value) control.Value = value,
                disposer:=Sub() control.Dispose())
        End Function
    End Class

    '''<summary>Pickles 64-bit unsigned integers.</summary>
    Public NotInheritable Class UInt64Jar
        Inherits BaseFixedSizeJar(Of UInt64)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(value As UInt64) As IRist(Of Byte)
            Return value.Bytes(byteOrder)
        End Function

        Protected Overrides ReadOnly Property DataSize As UInt16
            Get
                Return 8
            End Get
        End Property
        <SuppressMessage("Microsoft.Contracts", "Ensures-47-26")>
        Protected Overrides Function FixedSizeParse(data As IRist(Of Byte)) As UInt64
            Contract.Assume(data.Count = 8)
            Return data.ToUInt64(byteOrder)
        End Function

        Public Overrides Function Describe(value As UInt64) As String
            Return If(_showHex,
                      "0x" + value.ToString("X16", CultureInfo.InvariantCulture),
                      value.ToString(CultureInfo.InvariantCulture))
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-164")>
        Public Overrides Function Parse(text As String) As UInt64
            Try
                If New InvariantString(text).StartsWith("0x") Then
                    Contract.Assume(text.Length >= 2)
                    Return UInt64.Parse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                Else
                    Return UInt64.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture)
                End If
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("'{0}' is not a UInt64 value.".Frmt(text), ex)
            End Try
        End Function
    End Class

    '''<summary>Pickles 32-bit floating point values (singles).</summary>
    Public NotInheritable Class Float32Jar
        Inherits BaseFixedSizeJar(Of Single)

        Public Overrides Function Pack(value As Single) As IRist(Of Byte)
            Return BitConverter.GetBytes(value).AsRist()
        End Function

        Protected Overrides ReadOnly Property DataSize As UInt16
            Get
                Return 4
            End Get
        End Property
        <SuppressMessage("Microsoft.Contracts", "Ensures-47-25")>
        Protected Overrides Function FixedSizeParse(data As IRist(Of Byte)) As Single
            Dim buffer = data.ToArray()
            Contract.Assume(buffer.Length = 4)
            Return BitConverter.ToSingle(buffer, 0)
        End Function

        Public Overrides Function Describe(value As Single) As String
            Return value.ToString("r", CultureInfo.InvariantCulture)
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-94")>
        Public Overrides Function Parse(text As String) As Single
            Try
                Return Single.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture)
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("'{0}' is not a Single-precision value.".Frmt(text), ex)
            End Try
        End Function
    End Class

    '''<summary>Pickles 64-bit floating point values (doubles).</summary>
    Public NotInheritable Class Float64Jar
        Inherits BaseFixedSizeJar(Of Double)

        Public Overrides Function Pack(value As Double) As IRist(Of Byte)
            Return BitConverter.GetBytes(value).AsRist()
        End Function

        Protected Overrides ReadOnly Property DataSize As UShort
            Get
                Return 8
            End Get
        End Property
        <SuppressMessage("Microsoft.Contracts", "Ensures-47-25")>
        Protected Overrides Function FixedSizeParse(data As IRist(Of Byte)) As Double
            Dim buffer = data.ToArray()
            Contract.Assume(buffer.Length = 8)
            Return BitConverter.ToDouble(buffer, 0)
        End Function

        Public Overrides Function Describe(value As Double) As String
            Return value.ToString("r", CultureInfo.InvariantCulture)
        End Function
        <SuppressMessage("Microsoft.Contracts", "Ensures-28-94")>
        Public Overrides Function Parse(text As String) As Double
            Try
                Return Double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture)
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse
                                       TypeOf ex Is FormatException
                Throw New PicklingException("'{0}' is not a Double-precision value.".Frmt(text), ex)
            End Try
        End Function
    End Class
End Namespace
