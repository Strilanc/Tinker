Namespace BattleNetLogonServer
    Public Enum BnlsWardenPacketId As Byte
        FullServiceConnect = 0
        FullServiceHandleWardenPacket = 1
        PartialServiceExecuteModule = 2
        PartialServiceMemoryCheck = 3
    End Enum

    Public Class BnlsWardenPacket
        Public ReadOnly id As BnlsWardenPacketId
        Public ReadOnly payload As IPickle(Of Object)
        Public Shared ReadOnly clientJar As ManualSwitchJar = MakeClientJar()
        Public Shared ReadOnly serverJar As New TupleJar("data",
                                                         New EnumByteJar(Of BnlsWardenPacketId)("type").Weaken,
                                                         New UInt32Jar("cookie").Weaken,
                                                         New ByteJar("result").Weaken,
                                                         New ArrayJar("data", sizePrefixSize:=2).Weaken,
                                                         New ArrayJar("unspecified", takeRest:=True).Weaken)

        Private Sub New(ByVal payload As IPickle(Of Dictionary(Of String, Object)))
            Contract.Requires(payload IsNot Nothing)
            Me.payload = payload
            Me.id = CType(payload.Value("type"), BnlsWardenPacketId)
        End Sub
        Private Sub New(ByVal id As BnlsWardenPacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me.payload = payload
            Me.id = id
        End Sub
        Private Sub New(ByVal id As BnlsWardenPacketId, ByVal value As Dictionary(Of String, Object))
            Me.New(id, clientJar.Pack(id, value))
            Contract.Requires(value IsNot Nothing)
        End Sub

        Public Shared Function FromClientData(ByVal id As BnlsWardenPacketId, ByVal data As ViewableList(Of Byte)) As BnlsWardenPacket
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnlsWardenPacket)() IsNot Nothing)
            Return New BnlsWardenPacket(id, clientJar.Parse(id, data))
        End Function
        Public Shared Function FromServerData(ByVal data As ViewableList(Of Byte)) As BnlsWardenPacket
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnlsWardenPacket)() IsNot Nothing)
            Return New BnlsWardenPacket(serverJar.Parse(data))
        End Function

        Public Overrides Function ToString() As String
            Return "{0} = {1}".Frmt(id, payload.Description.Value())
        End Function

#Region "Definition"
        Private Shared Sub reg(ByVal jar As ManualSwitchJar,
                               ByVal id As BnlsWardenPacketId,
                               ByVal ParamArray subjars() As IJar(Of Object))
            jar.reg(id, New TupleJar(id.ToString, subjars).Weaken)
        End Sub

        Private Shared Function MakeClientJar() As ManualSwitchJar
            Dim jar = New ManualSwitchJar()

            reg(jar, BnlsWardenPacketId.FullServiceConnect,
                    New UInt32Jar("cookie").Weaken,
                    New EnumUInt32Jar(Of BnlsClientType)("client type").Weaken,
                    New ArrayJar("seed", sizePrefixSize:=2).Weaken,
                    New StringJar("username").Weaken,
                    New ArrayJar("password", sizeprefixsize:=2).Weaken,
                    New ArrayJar("unspecified", takeRest:=True).Weaken)
            reg(jar, BnlsWardenPacketId.FullServiceHandleWardenPacket,
                    New UInt32Jar("cookie").Weaken,
                    New ArrayJar("raw warden packet data", sizePrefixSize:=2).Weaken,
                    New ArrayJar("unspecified", takeRest:=True).Weaken)
            reg(jar, BnlsWardenPacketId.PartialServiceExecuteModule,
                    New UInt32Jar("cookie").Weaken,
                    New EnumUInt32Jar(Of BnlsClientType)("client type").Weaken,
                    New ArrayJar("seed", sizePrefixSize:=2).Weaken,
                    New UInt32Jar("unused").Weaken,
                    New ArrayJar("module name", expectedSize:=16).Weaken,
                    New ArrayJar("decrypted warden packet data", sizePrefixSize:=2).Weaken,
                    New ArrayJar("unspecified", takeRest:=True).Weaken)
            reg(jar, BnlsWardenPacketId.PartialServiceMemoryCheck,
                    New UInt32Jar("cookie").Weaken,
                    New EnumUInt32Jar(Of BnlsClientType)("client type").Weaken,
                    New UInt32Jar("info type").Weaken,
                    New UInt32Jar("unused").Weaken,
                    New ArrayJar("unspecified", takeRest:=True).Weaken)

            Return jar
        End Function
#End Region

#Region "Enums"
        Public Enum BnlsClientType As UInteger
            Starcraft = &H1
            Broodwar = &H2
            Warcraft2 = &H3
            Diablo2 = &H4
            Diablo2LoD = &H5
            StarcraftJapan = &H6
            Warcraft3RoC = &H7
            Warcraft3TFT = &H8
            Diablo = &H9
            DiabloSWare = &HA
            StarcraftSWare = &HB
        End Enum
#End Region

#Region "Packers"
        Public Shared Function MakeFullServiceConnect(ByVal cookie As UInteger, ByVal seed As UInteger) As BnlsWardenPacket
            Return New BnlsWardenPacket(BnlsWardenPacketId.FullServiceConnect, New Dictionary(Of String, Object) From {
                    {"cookie", cookie},
                    {"client type", BnlsClientType.Warcraft3TFT},
                    {"seed", seed.Bytes()},
                    {"username", ""},
                    {"password", New Byte() {}},
                    {"unspecified", New Byte() {}}})
        End Function
        Public Shared Function MakeFullServiceHandleWardenPacket(ByVal cookie As UInteger, ByVal data As Byte()) As BnlsWardenPacket
            Return New BnlsWardenPacket(BnlsWardenPacketId.FullServiceHandleWardenPacket, New Dictionary(Of String, Object) From {
                    {"cookie", cookie},
                    {"raw warden packet data", data},
                    {"unspecified", New Byte() {}}})
        End Function
#End Region
    End Class
End Namespace
