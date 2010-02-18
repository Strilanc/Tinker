Imports Tinker.Pickling

Namespace Warden
    Public Enum BNLSPacketId As Byte
        Null = &H0
        CDKey = &H1
        LogOnChallenge = &H2
        LogOnProof = &H3
        CreateAccount = &H4
        ChangeChallenge = &H5
        ChangeProof = &H6
        UpgradeChallenge = &H7
        UpgradeProof = &H8
        VersionCheck = &H9
        ConfirmLogOn = &HA
        HashData = &HB
        CDKeyEx = &HC
        ChooseNlsRevision = &HD
        Authorize = &HE
        AuthorizeProof = &HF
        RequestVersionByte = &H10
        VerifyServer = &H11
        ReserveServerSlots = &H12
        ServerLogOnChallenge = &H13
        ServerLogOnProof = &H14
        VersionCheckEx = &H18
        VersionCheckEx2 = &H1A
        Warden = &H7D
    End Enum

    Public Enum WardenPacketId As Byte
        FullServiceConnect = 0
        FullServiceHandleWardenPacket = 1
    End Enum

    Public Enum ClientType As UInteger
        Starcraft = &H1
        BroodWar = &H2
        Warcraft2 = &H3
        Diablo2 = &H4
        Diablo2LordOfDestruction = &H5
        StarcraftJapan = &H6
        Warcraft3ROC = &H7
        Warcraft3TFT = &H8
        Diablo = &H9
        DiabloSWare = &HA
        StarcraftSWare = &HB
    End Enum

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

        Public NotInheritable Class ClientPackets
            Private Sub New()
            End Sub

            Public Shared ReadOnly FullServerConnect As New TupleJar(WardenPacketId.FullServiceConnect.ToString,
                    New UInt32Jar("cookie").Weaken,
                    New EnumUInt32Jar(Of ClientType)("client type").Weaken,
                    New RemainingDataJar("seed").DataSizePrefixed(prefixSize:=2).Weaken,
                    New NullTerminatedStringJar("username").Weaken,
                    New RemainingDataJar("password").DataSizePrefixed(prefixSize:=2).Weaken,
                    New RemainingDataJar("unspecified").Weaken)
            Public Shared ReadOnly FullServiceHandleWardenPacket As New TupleJar(WardenPacketId.FullServiceHandleWardenPacket.ToString,
                    New UInt32Jar("cookie").Weaken,
                    New RemainingDataJar("raw warden packet data").DataSizePrefixed(prefixSize:=2).Weaken,
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

    <DebuggerDisplay("{ToString}")>
    Public Class ServerPacket
        Private Shared ReadOnly dataJar As New TupleJar("data",
                    New EnumByteJar(Of WardenPacketId)("type").Weaken,
                    New UInt32Jar("cookie").Weaken,
                    New ByteJar("result").Weaken,
                    New RemainingDataJar("data").DataSizePrefixed(prefixSize:=2).Weaken,
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
            Contract.Requires(responseData IsNot Nothing)
            Contract.Requires(unspecifiedData IsNot Nothing)
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
                                    ResponseData:=CType(vals("data"), IReadableList(Of Byte)).AssumeNotNull,
                                    UnspecifiedData:=CType(vals("unspecified"), IReadableList(Of Byte)).AssumeNotNull)
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
