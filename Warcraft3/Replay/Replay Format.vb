Imports Tinker.Pickling

Namespace WC3.Replay
    Public Enum ReplayEntryId As Byte
        StartOfReplay = &H10
        PlayerJoined = &H16
        PlayerLeft = &H17
        LobbyState = &H19
        LoadStarted1 = &H1A
        LoadStarted2 = &H1B
        GameStarted = &H1C
        Tick = &H1F
        ChatMessage = &H20
        GameStateChecksum = &H22
        Unknown0x23 = &H23
        TournamentForcedCountdown = &H2F
    End Enum

    <DebuggerDisplay("{ToString}")>
    Public Class ReplayEntry
        Private _id As ReplayEntryId
        Private _payload As ISimplePickle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public Sub New(ByVal id As ReplayEntryId, ByVal payload As ISimplePickle)
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub

        Public ReadOnly Property Id As ReplayEntryId
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
            Return _id.ToString
        End Function
    End Class

    Public NotInheritable Class Format
        Private Sub New()
        End Sub

        Public Const HeaderMagicValue As String = "Warcraft III recorded game" + Microsoft.VisualBasic.Chr(&H1A)
        Public Shared ReadOnly HeaderSize As UInt32 = CUInt(HeaderMagicValue.Length + 1 + 10 * 4)
        Public Const HeaderVersion As UInt32 = 1
        Public Const BlockHeaderSize As Integer = 8

        Public Shared ReadOnly ReplayEntryStartOfReplay As New TupleJar(
                New UInt32Jar().Named("unknown1"),
                New PlayerIdJar().Named("primary player id"),
                New UTF8Jar().NullTerminated.Limited(maxDataCount:=Protocol.Packets.MaxSerializedPlayerNameLength).Named("primary player name"),
                New DataJar().DataSizePrefixed(prefixSize:=1).Named("primary player shared data"),
                New UTF8Jar().NullTerminated.Named("game name"),
                New ByteJar().Named("unknown2"),
                New Protocol.GameStatsJar().Named("game stats"),
                New UInt32Jar().Named("player count"),
                New EnumUInt32Jar(Of Protocol.GameTypes)().Named("game type"),
                New UInt32Jar().Named("language"))
        Public Shared ReadOnly ReplayEntryPlayerJoined As New TupleJar(
                New PlayerIdJar().Named("joiner id"),
                New UTF8Jar().NullTerminated.Limited(maxDataCount:=Protocol.Packets.MaxSerializedPlayerNameLength).Named("name"),
                New DataJar().DataSizePrefixed(prefixSize:=1).Named("shared data"),
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryPlayerLeft As New TupleJar(
                New UInt32Jar().Named("unknown1"),
                New PlayerIdJar().Named("leaver"),
                New EnumUInt32Jar(Of Protocol.PlayerLeaveReason)().Named("reason"),
                New UInt32Jar().Named("session leave count"))
        Public Shared ReadOnly ReplayEntryLoadStarted1 As New TupleJar(
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryLoadStarted2 As New TupleJar(
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryGameStarted As New TupleJar(
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryChatMessage As IJar(Of NamedValueMap) = MakeTextJar()
        Private Shared Function MakeTextJar() As IJar(Of NamedValueMap)
            Contract.Ensures(Contract.Result(Of IJar(Of NamedValueMap))() IsNot Nothing)
            Dim jar = New InteriorSwitchJar(Of WC3.Protocol.ChatType, NamedValueMap)(
                    valueKeyExtractor:=Function(val) CType(val("type"), WC3.Protocol.ChatType),
                    dataKeyExtractor:=Function(data) CType(data(3), WC3.Protocol.ChatType))
            jar.AddSubJar(WC3.Protocol.ChatType.Game, New TupleJar(
                    New PlayerIdJar().Named("speaker"),
                    New UInt16Jar().Named("size"),
                    New EnumByteJar(Of WC3.Protocol.ChatType)().Named("type"),
                    New EnumUInt32Jar(Of WC3.Protocol.ChatGroup)(checkDefined:=False).Named("receiving group"),
                    New UTF8Jar().NullTerminated.Named("message")))
            jar.AddSubJar(WC3.Protocol.ChatType.Lobby, New TupleJar(
                    New PlayerIdJar().Named("speaker"),
                    New UInt16Jar().Named("size"),
                    New EnumByteJar(Of WC3.Protocol.ChatType)().Named("type"),
                    New UTF8Jar().NullTerminated.Named("message")))
            Return jar.Named(ReplayEntryId.ChatMessage.ToString)
        End Function
        Public Shared ReadOnly ReplayEntryGameStateChecksum As New TupleJar(
                New ByteJar().Named("unknown"),
                New UInt32Jar(showhex:=True).Named("checksum"))
        Public Shared ReadOnly ReplayEntryUnknown0x23 As New TupleJar(
                New UInt32Jar().Named("unknown1"),
                New ByteJar().Named("unknown2"),
                New UInt32Jar().Named("unknown3"),
                New ByteJar().Named("unknown4"))
        Public Shared ReadOnly ReplayEntryTournamentForcedCountdown As New TupleJar(
                New UInt32Jar().Named("counter state"),
                New UInt32Jar().Named("counter time"))
        Public Shared ReadOnly ReplayEntryLobbyState As IJar(Of NamedValueMap) = WC3.Protocol.Packets.LobbyState.Jar
        Public Shared ReadOnly ReplayEntryTick As IJar(Of NamedValueMap) = New TupleJar(
                New UInt16Jar().Named("time span"),
                New Protocol.PlayerActionSetJar().Repeated.Named("player action sets")
            ).DataSizePrefixed(prefixSize:=2)
    End Class
End Namespace
