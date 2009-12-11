Namespace Pickling.Jars
    Public Class FloatSingleJar
        Inherits Jar(Of Single)

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As Single)(ByVal value As TValue) As IPickle(Of TValue)
            Dim buffer(0 To 3) As Byte
            Using bw = New IO.BinaryWriter(New IO.MemoryStream(buffer))
                bw.Write(value)
            End Using
            Return New Pickle(Of TValue)(Name, value, buffer.ToArray.ToView, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As Strilbrary.ViewableList(Of Byte)) As IPickle(Of Single)
            data = data.SubView(0, 4)
            Dim value As Single
            Using br = New IO.BinaryReader(New IO.MemoryStream(data.ToArray()))
                value = br.ReadSingle()
            End Using
            Return New Pickle(Of Single)(Name, value, data, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As Single) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
    End Class

    Public Class UInt64Jar
        Inherits Jar(Of UInt64)
        Private ReadOnly byteOrder As ByteOrder

        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Me.byteOrder = byteOrder
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As UInt64)(ByVal value As TValue) As IPickle(Of TValue)
            Dim datum = value.Bytes(byteOrder).ToView
            Return New Pickle(Of TValue)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of UInt64)
            If data.Length < 8 Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, 8)
            Dim value = datum.ToUInt64(byteOrder)
            Return New Pickle(Of UInt64)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt64) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
    End Class

    Public Class UInt32Jar
        Inherits Jar(Of UInt32)
        Private ReadOnly byteOrder As ByteOrder

        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Me.byteOrder = byteOrder
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As UInt32)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(CType(value, Object) IsNot Nothing)
            Return New Pickle(Of TValue)(Me.Name, value, value.Bytes(byteOrder).ToView(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of UInt32)
            If data.Length < 4 Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, 4)
            Dim value = datum.ToUInt32(byteOrder)
            Return New Pickle(Of UInt32)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt32) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
    End Class

    Public Class UInt16Jar
        Inherits Jar(Of UInt16)
        Private ReadOnly byteOrder As ByteOrder

        Public Sub New(ByVal name As InvariantString,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Me.byteOrder = byteOrder
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As UInt16)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(Me.Name, value, value.Bytes(byteOrder).ToView(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of UInt16)
            If data.Length < 2 Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, 2)
            Dim value = datum.ToUInt16(byteOrder)
            Return New Pickle(Of UInt16)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt16) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
    End Class

    Public Class ByteJar
        Inherits Jar(Of Byte)

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Public NotOverridable Overrides Function Pack(Of TValue As Byte)(ByVal value As TValue) As IPickle(Of TValue)
            Return New Pickle(Of TValue)(Me.Name, value, {CByte(value)}.ToView(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Byte)
            If data.Length < 1 Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, 1)
            Dim value = datum(0)
            Return New Pickle(Of Byte)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As Byte) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function
    End Class

    '''<summary>Pickles fixed-size unsigned integers</summary>
    Public NotInheritable Class ValueJar
        Inherits Jar(Of ULong)
        Private ReadOnly byteCount As Integer
        Private ReadOnly byteOrder As ByteOrder

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(byteCount >= 0)
            Contract.Invariant(byteCount <= 8)
        End Sub

        Public Sub New(ByVal name As InvariantString,
                       ByVal byteCount As Integer,
                       Optional ByVal info As String = "No Info",
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Contract.Requires(info IsNot Nothing)
            Contract.Requires(byteCount > 0)
            Contract.Requires(byteCount <= 8)
            Me.byteCount = byteCount
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(Of TValue As ULong)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(CType(value, Object) IsNot Nothing)
            Return New Pickle(Of TValue)(Me.Name, value, value.Bytes(byteOrder, byteCount).ToView())
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of ULong)
            If data.Length < byteCount Then Throw New PicklingException("Not enough data")
            Dim datum = data.SubView(0, byteCount)
            Dim value = datum.ToUInt64(byteOrder)
            Return New Pickle(Of ULong)(Me.Name, value, datum)
        End Function
    End Class
End Namespace
