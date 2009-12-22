Namespace Warden
    <DebuggerDisplay("{ToString}")>
    Public Class ServerPacket
        Private Shared ReadOnly dataJar As New TupleJar("data",
                    New EnumByteJar(Of WardenPacketId)("type").Weaken,
                    New UInt32Jar("cookie").Weaken,
                    New ByteJar("result").Weaken,
                    New SizePrefixedDataJar("data", prefixSize:=2).Weaken,
                    New RemainingDataJar("unspecified").Weaken)

        Private ReadOnly _id As WardenPacketId
        Private ReadOnly _cookie As UInt32
        Private ReadOnly _result As Byte
        Private ReadOnly _responseData As IReadableList(Of Byte)
        Private ReadOnly _unspecifiedData As IReadableList(Of Byte)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_responseData IsNot Nothing)
            Contract.Invariant(_unspecifiedData IsNot Nothing)
        End Sub

        Public Sub New(ByVal id As WardenPacketId,
                       ByVal cookie As UInt32,
                       ByVal result As Byte,
                       ByVal responseData As IReadableList(Of Byte),
                       ByVal unspecifiedData As IReadableList(Of Byte))
            Me._id = id
            Me._cookie = cookie
            Me._result = result
            Me._responseData = responseData
            Me._unspecifiedData = unspecifiedData
        End Sub

        Public Shared Function FromData(ByVal packetData As IReadableList(Of Byte)) As ServerPacket
            Contract.Requires(packetData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ServerPacket)() IsNot Nothing)
            Dim vals = dataJar.Parse(packetData).Value
            Return New ServerPacket(Id:=CType(vals("type"), WardenPacketId),
                                    Cookie:=CUInt(vals("cookie")),
                                    Result:=CByte(vals("result")),
                                    ResponseData:=CType(vals("data"), IReadableList(Of Byte)),
                                    UnspecifiedData:=CType(vals("unspecified"), IReadableList(Of Byte)))
        End Function

        Public ReadOnly Property Id As WardenPacketId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Cookie As UInt32
            Get
                Return _cookie
            End Get
        End Property
        Public ReadOnly Property Result As Byte
            Get
                Return _result
            End Get
        End Property
        Public ReadOnly Property ResponseData As IReadableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                Return _responseData
            End Get
        End Property
        Public ReadOnly Property UnspecifiedData As IReadableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
                Return _unspecifiedData
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return {"Id: {0}".Frmt(Id),
                    "Cookie: {0}".Frmt(Cookie),
                    "Result: {0}".Frmt(Result),
                    "ResponseData: [{0}]".Frmt(ResponseData.ToHexString),
                    "UnspecifiedData: [{0}]".Frmt(UnspecifiedData.ToHexString)
                   }.StringJoin(Environment.NewLine)
        End Function
    End Class
End Namespace
