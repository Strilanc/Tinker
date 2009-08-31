Namespace Pickling.Jars
    Public Class FloatSingleJar
        Inherits Jar(Of Single)

        Public Sub New(ByVal name As String)
            MyBase.New(name)
        End Sub

        Public Overrides Function Pack(Of R As Single)(ByVal value As R) As IPickle(Of R)
            Dim buffer(0 To 3) As Byte
            Using bw = New IO.BinaryWriter(New IO.MemoryStream(buffer))
                bw.Write(value)
            End Using
            Return New Pickle(Of R)(Name, value, buffer.ToArray.ToView)
        End Function

        Public Overrides Function Parse(ByVal data As Strilbrary.ViewableList(Of Byte)) As IPickle(Of Single)
            data = data.SubView(0, 4)
            Using br = New IO.BinaryReader(New IO.MemoryStream(data.ToArray()))
                Return New Pickle(Of Single)(Name, br.ReadSingle(), data)
            End Using
        End Function
    End Class

    Public Class UInt32Jar
        Inherits Jar(Of UInt32)
        Private ReadOnly byteOrder As ByteOrder

        Public Sub New(ByVal name As String,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(Of R As UInt32)(ByVal value As R) As IPickle(Of R)
            Return New Pickle(Of R)(Me.Name, value, value.Bytes(byteOrder).ToView(), Function() ValueToString(value))
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of UInt32)
            Dim datum = data.SubView(0, 4)
            Dim value = datum.ToUInt32(byteOrder)
            Return New Pickle(Of UInt32)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt32) As String
            Return value.ToString
        End Function
    End Class

    Public Class UInt16Jar
        Inherits Jar(Of UInt16)
        Private ReadOnly byteOrder As ByteOrder

        Public Sub New(ByVal name As String,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(Of R As UInt16)(ByVal value As R) As IPickle(Of R)
            Return New Pickle(Of R)(Me.Name, value, value.Bytes(byteOrder).ToView(), Function() ValueToString(value))
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of UInt16)
            Dim datum = data.SubView(0, 2)
            Dim value = datum.ToUInt16(byteOrder)
            Return New Pickle(Of UInt16)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As UInt16) As String
            Return value.ToString
        End Function
    End Class

    Public Class ByteJar
        Inherits Jar(Of Byte)

        Public Sub New(ByVal name As String)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
        End Sub

        Public Overrides Function Pack(Of R As Byte)(ByVal value As R) As IPickle(Of R)
            Return New Pickle(Of R)(Me.Name, value, {CByte(value)}.ToView())
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of Byte)
            Dim datum = data.SubView(0, 1)
            Return New Pickle(Of Byte)(Me.Name, datum(0), datum)
        End Function
    End Class

    '''<summary>Pickles fixed-size unsigned integers</summary>
    Public Class ValueJar
        Inherits Jar(Of ULong)
        Private ReadOnly numBytes As Integer
        Private ReadOnly byteOrder As ByteOrder

        Public Sub New(ByVal name As String,
                       ByVal numBytes As Integer,
                       Optional ByVal info As String = "No Info",
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(info IsNot Nothing)
            Contract.Requires(numBytes > 0)
            Contract.Requires(numBytes <= 8)
            Me.numBytes = numBytes
            Me.byteOrder = byteOrder
        End Sub

        Public Overrides Function Pack(Of R As ULong)(ByVal value As R) As IPickle(Of R)
            Return New Pickle(Of R)(Me.Name, value, value.Bytes(byteOrder, numBytes).ToView())
        End Function

        Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of ULong)
            data = data.SubView(0, numBytes)
            Return New Pickle(Of ULong)(Me.Name, data.ToUInt64(byteOrder), data)
        End Function
    End Class
End Namespace
