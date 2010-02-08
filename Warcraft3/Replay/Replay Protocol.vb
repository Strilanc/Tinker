Imports Tinker.Pickling

Namespace WC3.Replay
    Public Enum ReplayEntryId As Byte
        EndOfReplay = &H0
        PlayerLeft = &H17
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
        Private _payload As Object

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public Sub New(ByVal id As ReplayEntryId, ByVal payload As Object)
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub

        Public ReadOnly Property Id As ReplayEntryId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Payload As Object
            Get
                Contract.Ensures(Contract.Result(Of Object)() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return _id.ToString
        End Function
    End Class

    ''' <summary>
    ''' Represents a player action at a particular time.
    ''' </summary>
    Public Class ReplayGameAction

        Public ReadOnly time As UInteger
        Public ReadOnly pid As Byte
        Public ReadOnly actions As IReadableList(Of WC3.Protocol.GameAction)
        Public Sub New(ByVal pid As Byte, ByVal time As UInteger, ByVal actions As IReadableList(Of WC3.Protocol.GameAction))
            Me.pid = pid
            Me.time = time
            Me.actions = actions
        End Sub
    End Class

    Public Class Prots
        Public Const HeaderMagicValue As String = "Warcraft III recorded game" + Microsoft.VisualBasic.Chr(&H1A)
        Public Shared ReadOnly HeaderSize As UInt32 = CUInt(HeaderMagicValue.Length + 1 + 10 * 4)
        Public Const HeaderVersion As UInt32 = 1
        Public Const BlockHeaderSize As Integer = 8

        Public Shared ReadOnly PlayerRecord As New TupleJar("player",
                New ByteJar("pid").Weaken,
                New NullTerminatedStringJar("name", maximumContentSize:=15).Weaken,
                New SizePrefixedDataJar("peer data", prefixSize:=1).Weaken)

        Public Shared ReadOnly GameActions As New RepeatingJar(Of WC3.Protocol.GameAction)("actions", New WC3.Protocol.W3GameActionJar("action"))

        Public Shared ReadOnly ReplayEntryPlayerLeft As New TupleJar(ReplayEntryId.PlayerLeft.ToString,
                New UInt32Jar("reason").Weaken,
                New ByteJar("pid").Weaken,
                New UInt32Jar("result").Weaken,
                New UInt32Jar("unknown").Weaken)
        Public Shared ReadOnly ReplayEntryLoadStarted1 As New TupleJar(ReplayEntryId.LoadStarted1.ToString,
                New UInt32Jar("value").Weaken)
        Public Shared ReadOnly ReplayEntryLoadStarted2 As New TupleJar(ReplayEntryId.LoadStarted2.ToString,
                New UInt32Jar("value").Weaken)
        Public Shared ReadOnly ReplayEntryGameStarted As New TupleJar(ReplayEntryId.GameStarted.ToString,
                New UInt32Jar("value").Weaken)
        Public Shared ReadOnly ReplayEntryChatMessage As IJar(Of Dictionary(Of InvariantString, Object)) = MakeTextJar()
        Private Shared Function MakeTextJar() As IJar(Of Dictionary(Of InvariantString, Object))
            Dim jar = New InteriorSwitchJar(Of WC3.Protocol.ChatType, Dictionary(Of InvariantString, Object))(
                    name:=ReplayEntryId.ChatMessage.ToString,
                    valueKeyExtractor:=Function(val) CType(val("type"), WC3.Protocol.ChatType),
                    dataKeyExtractor:=Function(data) CType(data(3), WC3.Protocol.ChatType))
            jar.AddPackerParser(WC3.Protocol.ChatType.Game, New TupleJar(ReplayEntryId.ChatMessage.ToString,
                    New ByteJar("pid").Weaken,
                    New UInt16Jar("size").Weaken,
                    New EnumByteJar(Of WC3.Protocol.ChatType)("type").Weaken,
                    New EnumUInt32Jar(Of WC3.Protocol.ChatReceiverType)("receiver type").Weaken,
                    New NullTerminatedStringJar("message").Weaken))
            jar.AddPackerParser(WC3.Protocol.ChatType.Lobby, New TupleJar(ReplayEntryId.ChatMessage.ToString,
                    New ByteJar("pid").Weaken,
                    New UInt16Jar("size").Weaken,
                    New EnumByteJar(Of WC3.Protocol.ChatType)("type").Weaken,
                    New NullTerminatedStringJar("message").Weaken))
            Return jar
        End Function
        Public Shared ReadOnly ReplayEntryGameStateChecksum As New TupleJar(ReplayEntryId.GameStateChecksum.ToString,
                New ByteJar("unknown").Weaken,
                New UInt32Jar("checksum", showhex:=True).Weaken)
        Public Shared ReadOnly ReplayEntryUnknown0x23 As New TupleJar(ReplayEntryId.Unknown0x23.ToString,
                New UInt32Jar("unknown1").Weaken,
                New ByteJar("unknown2").Weaken,
                New UInt32Jar("unknown3").Weaken,
                New ByteJar("unknown4").Weaken)
        Public Shared ReadOnly ReplayEntryTournamentForcedCountdown As New TupleJar(ReplayEntryId.TournamentForcedCountdown.ToString,
                New UInt32Jar("counter state").Weaken,
                New UInt32Jar("counter time").Weaken)
    End Class
End Namespace
