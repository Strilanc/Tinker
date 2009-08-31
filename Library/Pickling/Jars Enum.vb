Namespace Pickling.Jars
    Public Class EnumByteJar(Of T)
        Inherits Jar(Of T)
        Private ReadOnly flags As Boolean

        Public Sub New(ByVal name As String,
                       Optional ByVal flags As Boolean = False)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Me.flags = flags
        End Sub

        Public NotOverridable Overrides Function Pack(Of R As T)(ByVal value As R) As IPickle(Of R)
            Return New Pickle(Of R)(Me.Name, value, {CByte(CType(value, Object))}.ToView(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T)
            Dim datum = data.SubView(0, 1)
            Dim value = CType(CType(datum(0), Object), T)
            Return New Pickle(Of T)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(flags, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class

    Public Class EnumUInt16Jar(Of T)
        Inherits Jar(Of T)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly flags As Boolean

        Public Sub New(ByVal name As String,
                       Optional ByVal flags As Boolean = False,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Me.byteOrder = byteOrder
            Me.flags = flags
        End Sub

        Public NotOverridable Overrides Function Pack(Of R As T)(ByVal value As R) As IPickle(Of R)
            Return New Pickle(Of R)(Me.Name, value, CUShort(CType(value, Object)).Bytes(byteOrder).ToView(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T)
            Dim datum = data.SubView(0, 2)
            Dim value = CType(CType(datum.ToUInt16(byteOrder), Object), T)
            Return New Pickle(Of T)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(flags, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class

    Public Class EnumUInt32Jar(Of T)
        Inherits Jar(Of T)
        Private ReadOnly byteOrder As ByteOrder
        Private ReadOnly flags As Boolean

        Public Sub New(ByVal name As String,
                       Optional ByVal flags As Boolean = False,
                       Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
            MyBase.New(name)
            Contract.Requires(name IsNot Nothing)
            Me.byteOrder = byteOrder
            Me.flags = flags
        End Sub

        Public NotOverridable Overrides Function Pack(Of R As T)(ByVal value As R) As IPickle(Of R)
            Return New Pickle(Of R)(Me.Name, value, CUInt(CType(value, Object)).Bytes(byteOrder).ToView(), Function() ValueToString(value))
        End Function

        Public NotOverridable Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of T)
            Dim datum = data.SubView(0, 4)
            Dim value = CType(CType(datum.ToUInt32(byteOrder), Object), T)
            Return New Pickle(Of T)(Me.Name, value, datum, Function() ValueToString(value))
        End Function

        Protected Overridable Function ValueToString(ByVal value As T) As String
            Return If(flags, value.EnumFlagsToString(), value.ToString)
        End Function
    End Class
End Namespace
