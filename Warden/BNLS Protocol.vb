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
        Private ReadOnly _payload As ISimplePickle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(ByVal id As WardenPacketId, ByVal payload As ISimplePickle)
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload
            Me._id = id
        End Sub

        Public ReadOnly Property Id As WardenPacketId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Payload As ISimplePickle
            Get
                Contract.Ensures(Contract.Result(Of ISimplePickle)() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return "{0}: {1}".Frmt(Id, Payload.Description)
        End Function

        Public NotInheritable Class ClientPackets
            Private Sub New()
            End Sub

            Public Shared ReadOnly FullServerConnect As New TupleJar(
                    New UInt32Jar(showHex:=True).Named("cookie"),
                    New EnumUInt32Jar(Of ClientType)().Named("client type"),
                    New DataJar().DataSizePrefixed(prefixSize:=2).Named("seed"),
                    New UTF8Jar().NullTerminated.Named("username"),
                    New DataJar().DataSizePrefixed(prefixSize:=2).Named("password"),
                    New DataJar().Named("unspecified"))
            Public Shared ReadOnly FullServiceHandleWardenPacket As New TupleJar(
                    New UInt32Jar(showHex:=True).Named("cookie"),
                    New DataJar().DataSizePrefixed(prefixSize:=2).Named("raw warden packet data"),
                    New DataJar().Named("unspecified"))
        End Class

        Public Shared Function MakeFullServiceConnect(ByVal cookie As UInteger, ByVal seed As UInteger) As ClientPacket
            Contract.Ensures(Contract.Result(Of ClientPacket)() IsNot Nothing)
            Return New ClientPacket(WardenPacketId.FullServiceConnect,
                                    ClientPackets.FullServerConnect.PackPickle(New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                    {"cookie", cookie},
                    {"client type", ClientType.Warcraft3TFT},
                    {"seed", seed.Bytes()},
                    {"username", ""},
                    {"password", MakeRist(Of Byte)()},
                    {"unspecified", MakeRist(Of Byte)()}})))
        End Function
        Public Shared Function MakeFullServiceHandleWardenPacket(ByVal cookie As UInteger, ByVal data As IRist(Of Byte)) As ClientPacket
            Contract.Ensures(Contract.Result(Of ClientPacket)() IsNot Nothing)
            Return New ClientPacket(WardenPacketId.FullServiceHandleWardenPacket,
                                    ClientPackets.FullServiceHandleWardenPacket.PackPickle(New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                    {"cookie", cookie},
                    {"raw warden packet data", data},
                    {"unspecified", MakeRist(Of Byte)()}})))
        End Function
    End Class

    <DebuggerDisplay("{ToString()}")>
    Public Class ServerPacket
        Private Shared ReadOnly DataJar As New TupleJar(
                    New EnumByteJar(Of WardenPacketId)().Named("type"),
                    New UInt32Jar().Named("cookie"),
                    New ByteJar().Named("result"),
                    New DataJar().DataSizePrefixed(prefixSize:=2).Named("data"),
                    New DataJar().Named("unspecified"))

        Private ReadOnly _id As WardenPacketId
        Private ReadOnly _cookie As UInt32
        Private ReadOnly _result As Byte
        Private ReadOnly _responseData As IRist(Of Byte)
        Private ReadOnly _unspecifiedData As IRist(Of Byte)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_responseData IsNot Nothing)
            Contract.Invariant(_unspecifiedData IsNot Nothing)
        End Sub

        Public Sub New(ByVal id As WardenPacketId,
                       ByVal cookie As UInt32,
                       ByVal result As Byte,
                       ByVal responseData As IRist(Of Byte),
                       ByVal unspecifiedData As IRist(Of Byte))
            Contract.Requires(responseData IsNot Nothing)
            Contract.Requires(unspecifiedData IsNot Nothing)
            Me._id = id
            Me._cookie = cookie
            Me._result = result
            Me._responseData = responseData
            Me._unspecifiedData = unspecifiedData
        End Sub

        Public Shared Function FromData(ByVal packetData As IRist(Of Byte)) As ServerPacket
            Contract.Requires(packetData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ServerPacket)() IsNot Nothing)
            Dim vals = dataJar.Parse(packetData).Value
            Return New ServerPacket(Id:=vals.ItemAs(Of WardenPacketId)("type"),
                                    Cookie:=vals.ItemAs(Of UInt32)("cookie"),
                                    Result:=vals.ItemAs(Of Byte)("result"),
                                    ResponseData:=vals.ItemAs(Of IRist(Of Byte))("data"),
                                    UnspecifiedData:=vals.ItemAs(Of IRist(Of Byte))("unspecified"))
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
        Public ReadOnly Property ResponseData As IRist(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
                Return _responseData
            End Get
        End Property
        Public ReadOnly Property UnspecifiedData As IRist(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Byte))() IsNot Nothing)
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
