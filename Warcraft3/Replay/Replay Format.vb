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
        Private _payload As IPickle(Of Object)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public Sub New(ByVal id As ReplayEntryId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub

        Public ReadOnly Property Id As ReplayEntryId
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

        Public Shared ReadOnly ReplayEntryStartOfReplay As New TupleJar(ReplayEntryId.StartOfReplay.ToString,
                New UInt32Jar().Named("unknown1").Weaken,
                New PlayerIdJar("primary player id").Weaken,
                New NullTerminatedStringJar("primary player name", maximumContentSize:=15).Weaken,
                New RemainingDataJar().Named("primary player shared data").DataSizePrefixed(prefixSize:=1).Weaken,
                New NullTerminatedStringJar("game name").Weaken,
                New ByteJar().Named("unknown2").Weaken,
                New Protocol.GameStatsJar("game stats").Weaken,
                New UInt32Jar().Named("player count").Weaken,
                New EnumUInt32Jar(Of Protocol.GameTypes)().Named("game type").Weaken,
                New UInt32Jar().Named("language").Weaken)
        Public Shared ReadOnly ReplayEntryPlayerJoined As New TupleJar(ReplayEntryId.PlayerJoined.ToString,
                New PlayerIdJar("joiner id").Weaken,
                New NullTerminatedStringJar("name", maximumContentSize:=15).Weaken,
                New RemainingDataJar().Named("shared data").DataSizePrefixed(prefixSize:=1).Weaken,
                New UInt32Jar().Named("unknown").Weaken)
        Public Shared ReadOnly ReplayEntryPlayerLeft As New TupleJar(ReplayEntryId.PlayerLeft.ToString,
                New UInt32Jar().Named("unknown1").Weaken,
                New PlayerIdJar("leaver").Weaken,
                New EnumUInt32Jar(Of Protocol.PlayerLeaveReason)().Named("reason").Weaken,
                New UInt32Jar().Named("session leave count").Weaken)
        Public Shared ReadOnly ReplayEntryLoadStarted1 As New TupleJar(ReplayEntryId.LoadStarted1.ToString,
                New UInt32Jar().Named("unknown").Weaken)
        Public Shared ReadOnly ReplayEntryLoadStarted2 As New TupleJar(ReplayEntryId.LoadStarted2.ToString,
                New UInt32Jar().Named("unknown").Weaken)
        Public Shared ReadOnly ReplayEntryGameStarted As New TupleJar(ReplayEntryId.GameStarted.ToString,
                New UInt32Jar().Named("unknown").Weaken)
        Public Shared ReadOnly ReplayEntryChatMessage As IJar(Of Dictionary(Of InvariantString, Object)) = MakeTextJar()
        Private Shared Function MakeTextJar() As IJar(Of Dictionary(Of InvariantString, Object))
            Dim jar = New InteriorSwitchJar(Of WC3.Protocol.ChatType, Dictionary(Of InvariantString, Object))(
                    name:=ReplayEntryId.ChatMessage.ToString,
                    valueKeyExtractor:=Function(val) CType(val("type"), WC3.Protocol.ChatType),
                    dataKeyExtractor:=Function(data) CType(data(3), WC3.Protocol.ChatType))
            jar.AddPackerParser(WC3.Protocol.ChatType.Game, New TupleJar(ReplayEntryId.ChatMessage.ToString,
                    New PlayerIdJar("speaker").Weaken,
                    New UInt16Jar().Named("size").Weaken,
                    New EnumByteJar(Of WC3.Protocol.ChatType)().Named("type").Weaken,
                    New EnumUInt32Jar(Of WC3.Protocol.ChatGroup)(checkDefined:=False).Named("receiving group").Weaken,
                    New NullTerminatedStringJar("message").Weaken))
            jar.AddPackerParser(WC3.Protocol.ChatType.Lobby, New TupleJar(ReplayEntryId.ChatMessage.ToString,
                    New PlayerIdJar("speaker").Weaken,
                    New UInt16Jar().Named("size").Weaken,
                    New EnumByteJar(Of WC3.Protocol.ChatType)().Named("type").Weaken,
                    New NullTerminatedStringJar("message").Weaken))
            Return jar
        End Function
        Public Shared ReadOnly ReplayEntryGameStateChecksum As New TupleJar(ReplayEntryId.GameStateChecksum.ToString,
                New ByteJar().Named("unknown").Weaken,
                New UInt32Jar(showhex:=True).Named("checksum").Weaken)
        Public Shared ReadOnly ReplayEntryUnknown0x23 As New TupleJar(ReplayEntryId.Unknown0x23.ToString,
                New UInt32Jar().Named("unknown1").Weaken,
                New ByteJar().Named("unknown2").Weaken,
                New UInt32Jar().Named("unknown3").Weaken,
                New ByteJar().Named("unknown4").Weaken)
        Public Shared ReadOnly ReplayEntryTournamentForcedCountdown As New TupleJar(ReplayEntryId.TournamentForcedCountdown.ToString,
                New UInt32Jar().Named("counter state").Weaken,
                New UInt32Jar().Named("counter time").Weaken)
        Public Shared ReadOnly ReplayEntryLobbyState As IJar(Of Dictionary(Of InvariantString, Object)) = WC3.Protocol.Packets.LobbyState.Jar
        Public Shared ReadOnly ReplayEntryTick As IJar(Of Dictionary(Of InvariantString, Object)) = New TupleJar(ReplayEntryId.Tick.ToString,
                New UInt16Jar().Named("time span").Weaken,
                New Protocol.PlayerActionSetJar("player action set").Repeated("player action sets").Weaken
            ).DataSizePrefixed(prefixSize:=2)
    End Class
End Namespace
