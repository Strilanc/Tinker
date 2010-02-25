Namespace Pickling
    '''<summary>Pickles 8-bit unsigned integers.</summary>
    Public Class ByteJar
        Inherits BaseAnonymousJar(Of Byte)
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As Byte)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(value, {CByte(value)}.AsReadableList(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Byte)
            If data.Count < 1 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 1)
            Dim value = datum(0)
            Return New Pickle(Of Byte)(value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As Byte) As String
            If _showHex Then
                Return "0x" + value.ToString("X2", CultureInfo.InvariantCulture)
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function
    End Class

    '''<summary>Pickles 16-bit unsigned integers.</summary>
    Public Class UInt16Jar
        Inherits BaseAnonymousJar(Of UInt16)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As UInt16)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(value, value.Bytes(byteOrder).AsReadableList(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt16)
            If data.Count < 2 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 2)
            Dim value = datum.ToUInt16(byteOrder)
            Return New Pickle(Of UInt16)(value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt16) As String
            If _showHex Then
                Return "0x" + value.ToString("X4", CultureInfo.InvariantCulture)
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function
    End Class

    '''<summary>Pickles 32-bit unsigned integers.</summary>
    Public Class UInt32Jar
        Inherits BaseAnonymousJar(Of UInt32)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As UInt32)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(value, value.Bytes(byteOrder).AsReadableList(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt32)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = datum.ToUInt32(byteOrder)
            Return New Pickle(Of UInt32)(value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt32) As String
            If _showHex Then
                Return "0x" + value.ToString("X8", CultureInfo.InvariantCulture)
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function
    End Class

    '''<summary>Pickles 64-bit unsigned integers.</summary>
    Public Class UInt64Jar
        Inherits BaseAnonymousJar(Of UInt64)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly _showHex As Boolean

        Public Sub New(Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian,
                       Optional ByVal showHex As Boolean = False)
            Me._showHex = showHex
            Me.byteOrder = byteOrder
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As UInt64)(ByVal value As TValue) As IPickle(Of TValue)
            Dim datum = value.Bytes(byteOrder).AsReadableList
            Return New Pickle(Of TValue)(value, datum, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of UInt64)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = datum.ToUInt64(byteOrder)
            Return New Pickle(Of UInt64)(value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt64) As String
            If _showHex Then
                Return "0x" + value.ToString("X16", CultureInfo.InvariantCulture)
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function
    End Class

    '''<summary>Pickles 32-bit floating point values (singles).</summary>
    Public Class Float32Jar
        Inherits BaseAnonymousJar(Of Single)

        Public NotOverridable Overrides Function Pack(Of TValue As Single)(ByVal value As TValue) As IPickle(Of TValue)
            Dim data = BitConverter.GetBytes(value).AsReadableList
            Return New Pickle(Of TValue)(value, data, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Single)
            If data.Count < 4 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 4)
            Dim value = BitConverter.ToSingle(datum.ToArray, 0)
            Return New Pickle(Of Single)(value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As Single) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
    End Class

    '''<summary>Pickles 64-bit floating point values (doubles).</summary>
    Public Class Float64Jar
        Inherits BaseAnonymousJar(Of Double)

        Public NotOverridable Overrides Function Pack(Of TValue As Double)(ByVal value As TValue) As IPickle(Of TValue)
            Dim data = BitConverter.GetBytes(value).AsReadableList
            Return New Pickle(Of TValue)(value, data, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Double)
            If data.Count < 8 Then Throw New PicklingNotEnoughDataException()
            Dim datum = data.SubView(0, 8)
            Dim value = BitConverter.ToDouble(datum.ToArray, 0)
            Return New Pickle(Of Double)(value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As Double) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
    End Class
End Namespace
