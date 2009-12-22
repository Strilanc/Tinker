Namespace Warden
    Public NotInheritable Class ClientPacket
        Private ReadOnly _id As WardenPacketId
        Private ReadOnly _payload As IPickle(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(ByVal id As WardenPacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload
            Me._id = id
        End Sub

        Public ReadOnly Property Id As WardenPacketId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return "{0}: {1}".Frmt(Id, Payload.Description.Value())
        End Function

        Public Class ClientPackets
            Public Shared ReadOnly FullServerConnect As New TupleJar(WardenPacketId.FullServiceConnect.ToString,
                    New UInt32Jar("cookie").Weaken,
                    New EnumUInt32Jar(Of ClientType)("client type").Weaken,
                    New SizePrefixedDataJar("seed", prefixsize:=2).Weaken,
                    New StringJar("username").Weaken,
                    New SizePrefixedDataJar("password", prefixsize:=2).Weaken,
                    New RemainingDataJar("unspecified").Weaken)
            Public Shared ReadOnly FullServiceHandleWardenPacket As New TupleJar(WardenPacketId.FullServiceHandleWardenPacket.ToString,
                    New UInt32Jar("cookie").Weaken,
                    New SizePrefixedDataJar("raw warden packet data", prefixsize:=2).Weaken,
                    New RemainingDataJar("unspecified").Weaken)
        End Class

        Public Shared Function MakeFullServiceConnect(ByVal cookie As UInteger, ByVal seed As UInteger) As ClientPacket
            Contract.Ensures(Contract.Result(Of ClientPacket)() IsNot Nothing)
            Return New ClientPacket(WardenPacketId.FullServiceConnect, ClientPackets.FullServerConnect.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"cookie", cookie},
                    {"client type", ClientType.Warcraft3TFT},
                    {"seed", seed.Bytes.AsReadableList},
                    {"username", ""},
                    {"password", New Byte() {}.AsReadableList},
                    {"unspecified", New Byte() {}.AsReadableList}}))
        End Function
        Public Shared Function MakeFullServiceHandleWardenPacket(ByVal cookie As UInteger, ByVal data As IReadableList(Of Byte)) As ClientPacket
            Contract.Ensures(Contract.Result(Of ClientPacket)() IsNot Nothing)
            Return New ClientPacket(WardenPacketId.FullServiceHandleWardenPacket, ClientPackets.FullServiceHandleWardenPacket.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"cookie", cookie},
                    {"raw warden packet data", data},
                    {"unspecified", New Byte() {}.AsReadableList}}))
        End Function
    End Class
End Namespace
