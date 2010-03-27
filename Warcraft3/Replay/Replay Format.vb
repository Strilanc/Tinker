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
    Public Enum ReplaySettings As UInt16
        Online = 1US << 15
    End Enum

    Public NotInheritable Class Format
        Private Sub New()
        End Sub

        Public Const HeaderMagicValue As String = "Warcraft III recorded game" + Microsoft.VisualBasic.Chr(&H1A)
        Public Shared ReadOnly HeaderSize As UInt32 = CUInt(HeaderMagicValue.Length + 1 + 10 * 4)
        Public Const HeaderVersion As UInt32 = 1
        Public Const BlockHeaderSize As Integer = 8

        Private Shared ReadOnly _allDefinitions As New List(Of Definition)
        Public Shared ReadOnly Property AllDefinitions As IEnumerable(Of Definition)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of Definition))() IsNot Nothing)
                Return _allDefinitions.AsReadOnly
            End Get
        End Property

        Public MustInherit Class Definition
            Private ReadOnly _id As ReplayEntryId
            Private ReadOnly _jar As ISimpleJar

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_jar IsNot Nothing)
            End Sub

            Friend Sub New(ByVal id As ReplayEntryId, ByVal jar As ISimpleJar)
                Contract.Requires(jar IsNot Nothing)
                Me._id = id
                Me._jar = jar
            End Sub

            Public ReadOnly Property Id As ReplayEntryId
                Get
                    Return _id
                End Get
            End Property
            Public ReadOnly Property Jar As ISimpleJar
                Get
                    Contract.Ensures(Contract.Result(Of ISimpleJar)() IsNot Nothing)
                    Return _jar
                End Get
            End Property
        End Class
        Public NotInheritable Class Definition(Of T)
            Inherits Definition
            Private ReadOnly _jar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_jar IsNot Nothing)
            End Sub

            Friend Sub New(ByVal id As ReplayEntryId, ByVal jar As IJar(Of T))
                MyBase.New(id, jar)
                Contract.Requires(jar IsNot Nothing)
                Me._jar = jar
            End Sub

            Public Shadows ReadOnly Property Jar As IJar(Of T)
                Get
                    Contract.Ensures(Contract.Result(Of IJar(Of T))() IsNot Nothing)
                    Return _jar
                End Get
            End Property
        End Class

        Private Shared Function IncludeDefinitionInAll(Of T As Definition)(ByVal def As T) As T
            Contract.Requires(def IsNot Nothing)
            Contract.Ensures(Contract.Result(Of T)() Is def)
            _allDefinitions.Add(def)
            Return def
        End Function
        Private Shared Function Define(Of T)(ByVal id As ReplayEntryId, ByVal jar As IJar(Of T)) As Definition(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Definition(Of T))() IsNot Nothing)
            Return IncludeDefinitionInAll(New Definition(Of T)(id, jar))
        End Function
        Private Shared Function Define(ByVal id As ReplayEntryId,
                                       ByVal jar1 As ISimpleNamedJar,
                                       ByVal jar2 As ISimpleNamedJar,
                                       ByVal ParamArray jars() As ISimpleNamedJar) As Definition(Of NamedValueMap)
            Contract.Requires(jar1 IsNot Nothing)
            Contract.Requires(jar2 IsNot Nothing)
            Contract.Requires(jars IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Definition(Of NamedValueMap))() IsNot Nothing)
            Return Define(id, New TupleJar(jars.Prepend(jar1, jar2).ToArray))
        End Function

        Public Shared ReadOnly ReplayHeader As New TupleJar(
                New ASCIIJar().NullTerminated().Named("magic"),
                New UInt32Jar().Named("header size"),
                New UInt32Jar().Named("data compressed size"),
                New UInt32Jar().Named("header version"),
                New UInt32Jar().Named("data decompressed size"),
                New UInt32Jar().Named("data block count"),
                New ASCIIJar().Reversed.Fixed(exactDataCount:=4).Named("product id"),
                New UInt32Jar().Named("wc3 version"),
                New UInt16Jar().Named("replay version"),
                New EnumUInt16Jar(Of ReplaySettings)().Named("settings"),
                New UInt32Jar().Named("duration in game milliseconds"),
                New UInt32Jar(showHex:=True).Named("header crc32"))

        Public Shared ReadOnly ReplayEntryStartOfReplay As Definition(Of NamedValueMap) = Define(ReplayEntryId.StartOfReplay,
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
        Public Shared ReadOnly ReplayEntryPlayerJoined As Definition(Of NamedValueMap) = Define(ReplayEntryId.PlayerJoined,
                New PlayerIdJar().Named("joiner id"),
                New UTF8Jar().NullTerminated.Limited(maxDataCount:=Protocol.Packets.MaxSerializedPlayerNameLength).Named("name"),
                New DataJar().DataSizePrefixed(prefixSize:=1).Named("shared data"),
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryPlayerLeft As Definition(Of NamedValueMap) = Define(ReplayEntryId.PlayerLeft,
                New UInt32Jar().Named("unknown1"),
                New PlayerIdJar().Named("leaver"),
                New EnumUInt32Jar(Of Protocol.PlayerLeaveReason)().Named("reason"),
                New UInt32Jar().Named("session leave count"))
        Public Shared ReadOnly ReplayEntryLoadStarted1 As Definition(Of UInt32) = Define(ReplayEntryId.LoadStarted1,
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryLoadStarted2 As Definition(Of UInt32) = Define(ReplayEntryId.LoadStarted2,
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryGameStarted As Definition(Of UInt32) = Define(ReplayEntryId.GameStarted,
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryChatMessage As Definition(Of NamedValueMap) = Define(ReplayEntryId.ChatMessage,
            New InteriorSwitchJar(Of Protocol.ChatType, NamedValueMap)(
                valueKeyExtractor:=Function(val) val.ItemAs(Of Protocol.ChatType)("type"),
                dataKeyExtractor:=Function(data) CType(data(3), Protocol.ChatType),
                subJars:=New Dictionary(Of Protocol.ChatType, NonNull(Of IJar(Of NamedValueMap))) From {
                    {WC3.Protocol.ChatType.Game, New TupleJar(
                        New PlayerIdJar().Named("speaker"),
                        New UInt16Jar().Named("size"),
                        New EnumByteJar(Of Protocol.ChatType)().Named("type"),
                        New EnumUInt32Jar(Of WC3.Protocol.ChatGroup)(checkDefined:=False).Named("receiving group"),
                        New UTF8Jar().NullTerminated.Named("message"))},
                    {WC3.Protocol.ChatType.Lobby, New TupleJar(
                        New PlayerIdJar().Named("speaker"),
                        New UInt16Jar().Named("size"),
                        New EnumByteJar(Of WC3.Protocol.ChatType)().Named("type"),
                        New UTF8Jar().NullTerminated.Named("message"))}}))
        Public Shared ReadOnly ReplayEntryGameStateChecksum As Definition(Of NamedValueMap) = Define(ReplayEntryId.GameStateChecksum,
                New ByteJar().Named("unknown"),
                New UInt32Jar(showhex:=True).Named("checksum"))
        Public Shared ReadOnly ReplayEntryUnknown0x23 As Definition(Of NamedValueMap) = Define(ReplayEntryId.Unknown0x23,
                New UInt32Jar().Named("unknown1"),
                New ByteJar().Named("unknown2"),
                New UInt32Jar().Named("unknown3"),
                New ByteJar().Named("unknown4"))
        Public Shared ReadOnly ReplayEntryTournamentForcedCountdown As Definition(Of NamedValueMap) = Define(ReplayEntryId.TournamentForcedCountdown,
                New UInt32Jar().Named("counter state"),
                New UInt32Jar().Named("counter time"))
        Public Shared ReadOnly ReplayEntryLobbyState As Definition(Of NamedValueMap) = Define(ReplayEntryId.LobbyState,
                WC3.Protocol.Packets.LobbyState.Jar)
        Public Shared ReadOnly ReplayEntryTick As Definition(Of NamedValueMap) = Define(ReplayEntryId.Tick, New TupleJar(
                New UInt16Jar().Named("time span"),
                New Protocol.PlayerActionSetJar().Repeated.Named("player action sets")
            ).DataSizePrefixed(prefixSize:=2))
    End Class
End Namespace
