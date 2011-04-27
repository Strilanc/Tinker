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
        TickPreOverflow = &H1E
        Tick = &H1F
        ChatMessage = &H20

        GameStateChecksum = &H22
        Desync = &H23

        TournamentForcedCountdown = &H2F
    End Enum
    <Flags()>
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

        Private Shared ReadOnly _allDefinitions As New Dictionary(Of ReplayEntryId, Definition)
        Public Shared ReadOnly Property AllDefinitions As IEnumerable(Of Definition)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of Definition))() IsNot Nothing)
                Return _allDefinitions.Values
            End Get
        End Property
        Public Shared ReadOnly Property DefinitionFor(id As ReplayEntryId) As Definition
            Get
                Contract.Ensures(Contract.Result(Of Definition)() IsNot Nothing)
                If Not _allDefinitions.ContainsKey(id) Then Throw New ArgumentException("No definition defined for id: {0}.".Frmt(id))
                Return _allDefinitions(id).AssumeNotNull
            End Get
        End Property

        '''<completionlist cref="Format"/>
        Public MustInherit Class Definition
            Private ReadOnly _id As ReplayEntryId
            Private ReadOnly _jar As IJar(Of Object)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_jar IsNot Nothing)
            End Sub

            Friend Sub New(id As ReplayEntryId, jar As IJar(Of Object))
                Contract.Requires(jar IsNot Nothing)
                Me._id = id
                Me._jar = jar
            End Sub

            Public ReadOnly Property Id As ReplayEntryId
                Get
                    Return _id
                End Get
            End Property
            Public ReadOnly Property Jar As IJar(Of Object)
                Get
                    Contract.Ensures(Contract.Result(Of IJar(Of Object))() IsNot Nothing)
                    Return _jar
                End Get
            End Property
        End Class
        '''<completionlist cref="Format"/>
        Public NotInheritable Class Definition(Of T)
            Inherits Definition
            Private ReadOnly _jar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_jar IsNot Nothing)
            End Sub

            Friend Sub New(id As ReplayEntryId, jar As IJar(Of T))
                MyBase.New(id, jar.Weaken())
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

        Private Shared Function Define(Of T)(id As ReplayEntryId, jar As IJar(Of T)) As Definition(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Definition(Of T))() IsNot Nothing)
            Dim result = New Definition(Of T)(id, jar)
            _allDefinitions.Add(id, result)
            Return result
        End Function

        Public Shared ReadOnly ReplayHeader As IJar(Of NamedValueMap) =
                New ASCIIJar().NullTerminated().Named("magic").
                Then(New UInt32Jar().Named("header size")).
                Then(New UInt32Jar().Named("data compressed size")).
                Then(New UInt32Jar().Named("header version")).
                Then(New UInt32Jar().Named("data decompressed size")).
                Then(New UInt32Jar().Named("data block count")).
                Then(New ASCIIJar().Reversed.Fixed(exactDataCount:=4).Named("product id")).
                Then(New UInt32Jar().Named("wc3 version")).
                Then(New UInt16Jar().Named("replay version")).
                Then(New EnumUInt16Jar(Of ReplaySettings)().Named("settings")).
                Then(New UInt32Jar().Named("duration in game milliseconds")).
                Then(New UInt32Jar(showHex:=True).Named("header crc32"))

        Public Shared ReadOnly ReplayEntryStartOfReplay As Definition(Of NamedValueMap) = Define(ReplayEntryId.StartOfReplay,
                New UInt32Jar().Named("unknown1").
                Then(New PlayerIdJar().Named("primary player id")).
                Then(New UTF8Jar().NullTerminated.Limited(maxDataCount:=Protocol.Packets.MaxSerializedPlayerNameLength).Named("primary player name")).
                Then(New DataJar().DataSizePrefixed(prefixSize:=1).Named("primary player shared data")).
                Then(New UTF8Jar().NullTerminated.Named("game name")).
                Then(New ByteJar().Named("unknown2")).
                Then(New Protocol.GameStatsJar().Named("game stats")).
                Then(New UInt32Jar().Named("player count")).
                Then(New EnumUInt32Jar(Of Protocol.GameTypes)(checkDefined:=False).Named("game type")).
                Then(New UInt32Jar().Named("language")))
        Public Shared ReadOnly ReplayEntryPlayerJoined As Definition(Of NamedValueMap) = Define(ReplayEntryId.PlayerJoined,
                New PlayerIdJar().Named("joiner id").
                Then(New UTF8Jar().NullTerminated.Limited(maxDataCount:=Protocol.Packets.MaxSerializedPlayerNameLength).Named("name")).
                Then(New DataJar().DataSizePrefixed(prefixSize:=1).Named("shared data")).
                Then(New UInt32Jar().Named("unknown")))
        Public Shared ReadOnly ReplayEntryPlayerLeft As Definition(Of NamedValueMap) = Define(ReplayEntryId.PlayerLeft,
                New UInt32Jar().Named("unknown1").
                Then(New PlayerIdJar().Named("leaver")).
                Then(New EnumUInt32Jar(Of Protocol.PlayerLeaveReason)().Named("reason")).
                Then(New UInt32Jar().Named("session leave count")))
        Public Shared ReadOnly ReplayEntryLoadStarted1 As Definition(Of UInt32) = Define(ReplayEntryId.LoadStarted1,
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryLoadStarted2 As Definition(Of UInt32) = Define(ReplayEntryId.LoadStarted2,
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryGameStarted As Definition(Of UInt32) = Define(ReplayEntryId.GameStarted,
                New UInt32Jar().Named("unknown"))
        Public Shared ReadOnly ReplayEntryChatMessage As Definition(Of NamedValueMap) = Define(ReplayEntryId.ChatMessage,
            New PlayerIdJar().Named("speaker").
            Then(New KeyPrefixedJar(Of Protocol.ChatType)(
                    useSingleLineDescription:=False,
                    keyJar:=New EnumByteJar(Of Protocol.ChatType)().Named("type"),
                    valueJars:=New Dictionary(Of Protocol.ChatType, IJar(Of Object)) From {
                        {Protocol.ChatType.Game, New EnumUInt32Jar(Of Protocol.ChatGroup)().Named("receiving group").Weaken()},
                        {Protocol.ChatType.Lobby, New EmptyJar().Named("receiving group").Weaken()}}
                    ).Named("type group").
                 Then(New UTF8Jar().NullTerminated.Named("message")).
                 DataSizePrefixed(prefixSize:=2).
                 Named("type group message")))
        Public Shared ReadOnly ReplayEntryGameStateChecksum As Definition(Of UInt32) = Define(ReplayEntryId.GameStateChecksum,
                New UInt32Jar(showhex:=True).DataSizePrefixed(prefixSize:=1))
        Public Shared ReadOnly ReplayEntryDesync As Definition(Of NamedValueMap) = Define(ReplayEntryId.Desync,
                New UInt32Jar().Named("tick count").
                Then(New UInt32Jar(showHex:=True).DataSizePrefixed(prefixSize:=1).Named("checksum")).
                Then(New PlayerIdJar().RepeatedWithCountPrefix(prefixSize:=1, useSingleLineDescription:=True).Named("remaining players")))
        Public Shared ReadOnly ReplayEntryTournamentForcedCountdown As Definition(Of NamedValueMap) = Define(ReplayEntryId.TournamentForcedCountdown,
                New UInt32Jar().Named("counter state").
                Then(New UInt32Jar().Named("counter time")))
        Public Shared ReadOnly ReplayEntryLobbyState As Definition(Of NamedValueMap) = Define(ReplayEntryId.LobbyState,
                WC3.Protocol.LobbyStateTupleJar.DataSizePrefixed(prefixSize:=2))
        Public Shared ReadOnly ReplayEntryTickPreOverflow As Definition(Of NamedValueMap) = Define(ReplayEntryId.TickPreOverflow,
                New UInt16Jar().Named("time span").
                Then(New Protocol.PlayerActionSetJar().Repeated.Named("player action sets")
            ).DataSizePrefixed(prefixSize:=2))
        Public Shared ReadOnly ReplayEntryTick As Definition(Of NamedValueMap) = Define(ReplayEntryId.Tick,
                New UInt16Jar().Named("time span").
                Then(New Protocol.PlayerActionSetJar().Repeated.Named("player action sets")
            ).DataSizePrefixed(prefixSize:=2))
    End Class
End Namespace
