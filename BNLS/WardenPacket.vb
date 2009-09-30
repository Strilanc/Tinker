Namespace BNLS
    Public Enum BNLSWardenPacketId As Byte
        FullServiceConnect = 0
        FullServiceHandleWardenPacket = 1
        PartialServiceExecuteModule = 2
        PartialServiceMemoryCheck = 3
    End Enum

    Public Class BNLSWardenPacket
        Private Shared ReadOnly clientJar As ManualSwitchJar = MakeClientJar()
        Private Shared ReadOnly serverJar As New TupleJar("data",
                                                         New EnumByteJar(Of BNLSWardenPacketId)("type").Weaken,
                                                         New UInt32Jar("cookie").Weaken,
                                                         New ByteJar("result").Weaken,
                                                         New ArrayJar("data", sizePrefixSize:=2).Weaken,
                                                         New ArrayJar("unspecified", takeRest:=True).Weaken)
        Public ReadOnly id As BNLSWardenPacketId
        Private ReadOnly _payload As IPickle(Of Object)
        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(payload IsNot Nothing)
        End Sub


        Private Sub New(ByVal payload As IPickle(Of Dictionary(Of String, Object)))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload
            Me.id = CType(payload.Value("type"), BNLSWardenPacketId)
        End Sub
        Private Sub New(ByVal id As BNLSWardenPacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload
            Me.id = id
        End Sub
        Private Sub New(ByVal id As BNLSWardenPacketId, ByVal value As Dictionary(Of String, Object))
            Me.New(id, clientJar.Pack(id, value))
            Contract.Requires(value IsNot Nothing)
        End Sub

        Public Shared Function FromClientData(ByVal id As BNLSWardenPacketId, ByVal data As ViewableList(Of Byte)) As BNLSWardenPacket
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BNLSWardenPacket)() IsNot Nothing)
            Return New BNLSWardenPacket(id, clientJar.Parse(id, data))
        End Function
        Public Shared Function FromServerData(ByVal data As ViewableList(Of Byte)) As BNLSWardenPacket
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BNLSWardenPacket)() IsNot Nothing)
            Return New BNLSWardenPacket(serverJar.Parse(data))
        End Function

        Public Overrides Function ToString() As String
            Return "{0} = {1}".Frmt(id, payload.Description.Value())
        End Function

#Region "Definition"
        Private Shared Sub reg(ByVal jar As ManualSwitchJar,
                               ByVal id As BNLSWardenPacketId,
                               ByVal ParamArray subJars() As IJar(Of Object))
            jar.AddPackerParser(id, New TupleJar(id.ToString, subJars).Weaken)
        End Sub

        Private Shared Function MakeClientJar() As ManualSwitchJar
            Dim jar = New ManualSwitchJar()

            reg(jar, BNLSWardenPacketId.FullServiceConnect,
                    New UInt32Jar("cookie").Weaken,
                    New EnumUInt32Jar(Of BNLSClientType)("client type").Weaken,
                    New ArrayJar("seed", sizePrefixSize:=2).Weaken,
                    New StringJar("username").Weaken,
                    New ArrayJar("password", sizeprefixsize:=2).Weaken,
                    New ArrayJar("unspecified", takeRest:=True).Weaken)
            reg(jar, BNLSWardenPacketId.FullServiceHandleWardenPacket,
                    New UInt32Jar("cookie").Weaken,
                    New ArrayJar("raw warden packet data", sizePrefixSize:=2).Weaken,
                    New ArrayJar("unspecified", takeRest:=True).Weaken)
            reg(jar, BNLSWardenPacketId.PartialServiceExecuteModule,
                    New UInt32Jar("cookie").Weaken,
                    New EnumUInt32Jar(Of BNLSClientType)("client type").Weaken,
                    New ArrayJar("seed", sizePrefixSize:=2).Weaken,
                    New UInt32Jar("unused").Weaken,
                    New ArrayJar("module name", expectedSize:=16).Weaken,
                    New ArrayJar("decrypted warden packet data", sizePrefixSize:=2).Weaken,
                    New ArrayJar("unspecified", takeRest:=True).Weaken)
            reg(jar, BNLSWardenPacketId.PartialServiceMemoryCheck,
                    New UInt32Jar("cookie").Weaken,
                    New EnumUInt32Jar(Of BNLSClientType)("client type").Weaken,
                    New UInt32Jar("info type").Weaken,
                    New UInt32Jar("unused").Weaken,
                    New ArrayJar("unspecified", takeRest:=True).Weaken)

            Return jar
        End Function
#End Region

#Region "Enums"
        Public Enum BNLSClientType As UInteger
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
#End Region

#Region "Packers"
        Public Shared Function MakeFullServiceConnect(ByVal cookie As UInteger, ByVal seed As UInteger) As BNLSWardenPacket
            Return New BNLSWardenPacket(BNLSWardenPacketId.FullServiceConnect, New Dictionary(Of String, Object) From {
                    {"cookie", cookie},
                    {"client type", BNLSClientType.Warcraft3TFT},
                    {"seed", seed.Bytes()},
                    {"username", ""},
                    {"password", New Byte() {}},
                    {"unspecified", New Byte() {}}})
        End Function
        Public Shared Function MakeFullServiceHandleWardenPacket(ByVal cookie As UInteger, ByVal data As Byte()) As BNLSWardenPacket
            Return New BNLSWardenPacket(BNLSWardenPacketId.FullServiceHandleWardenPacket, New Dictionary(Of String, Object) From {
                    {"cookie", cookie},
                    {"raw warden packet data", data},
                    {"unspecified", New Byte() {}}})
        End Function
#End Region
    End Class
End Namespace
