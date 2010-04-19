Imports Tinker.Pickling

Namespace WC3.Protocol
    '''<summary>Identifies a warcraft 3 packet type.</summary>
    '''<data>
    '''  0 BYTE GAME_PACKET_PREFIX
    '''  1 BYTE packet type
    '''  2 WORD size including header = n
    '''  3 BYTE[4:n] data
    '''</data>
    Public Enum PacketId As Byte

        Ping = &H1

        Greet = &H4
        RejectEntry = &H5
        OtherPlayerJoined = &H6
        OtherPlayerLeft = &H7
        OtherPlayerReady = &H8
        LobbyState = &H9
        StartCountdown = &HA
        StartLoading = &HB
        Tick = &HC

        Text = &HF
        PlayersLagging = &H10
        PlayerStoppedLagging = &H11

        NewHost = &H14
        PeerHostTransferExternalAddresses = &H15
        PeerHostTransferPlayerInternalAddress = &H16
        HostConfirmHostLeaving = &H17

        PeerPromoteToHostAndContinue = &H19

        GameEnd = &H1B

        Knock = &H1E

        Leaving = &H21

        Ready = &H23

        GameAction = &H26
        Tock = &H27
        NonGameAction = &H28
        RequestDropLaggers = &H29

        PeerHostTransferSelfInternalAddress = &H2B
        ClientConfirmHostLeaving = &H2C

        LanRequestGame = &H2F
        LanGameDetails = &H30
        LanCreateGame = &H31
        LanRefreshGame = &H32
        LanDestroyGame = &H33
        PeerChat = &H34
        PeerPing = &H35
        PeerPong = &H36 'No; I refuse to say it. It's a bad pun.
        PeerKnock = &H37

        PeerConnectionInfo = &H3B

        HostMapInfo = &H3D
        SetUploadTarget = &H3E
        SetDownloadSource = &H3F

        ClientMapInfo = &H42
        MapFileData = &H43
        MapFileDataReceived = &H44
        MapFileDataProblem = &H45
        Pong = &H46
        MeleePlayerInfo = &H47
        TickPreOverflow = &H48

        TournamentCountdown = &H50
        EncryptedServerMeleeData = &H51
        EncryptedClientMeleeData = &H52
    End Enum

    <Flags()>
    Public Enum GameTypes As UInteger
        None = 0
        Unknown0 = 1 << 0 '[always seems to be set in replays and custom games?]

        '''<summary>Setting this bit causes wc3 to check the map and disc if it is not signed by Blizzard</summary>
        AuthenticatedMakerBlizzard = 1 << 3

        OfficialMeleeGame = 1 << 5

        SavedGame = 1 << 9

        PrivateGame = 1 << 11

        MakerUser = 1 << 13
        MakerBlizzard = 1 << 14
        TypeMelee = 1 << 15
        TypeScenario = 1 << 16
        SizeSmall = 1 << 17
        SizeMedium = 1 << 18
        SizeLarge = 1 << 19
        ObsFull = 1 << 20
        ObsOnDeath = 1 << 21
        ObsNone = 1 << 22

        MaskObs = ObsFull Or ObsOnDeath Or ObsNone
        MaskMaker = MakerBlizzard Or MakerUser
        MaskType = TypeMelee Or TypeScenario
        MaskSize = SizeLarge Or SizeMedium Or SizeSmall
        MaskFilterable = MaskObs Or MaskMaker Or MaskType Or MaskSize
    End Enum
    Public Enum PlayerLeaveReason As UInt32
        Disconnect = 1
        ProgramClosed = 4
        Quit = 7
        Defeat = 8
        Victory = 9
        Tie = 10
        NeutralOrEndOfGame = 11
    End Enum
    Public Enum LobbyLayoutStyle As Byte
        Melee = 0
        CustomForces = 1
        FixedPlayerSettings = 3
        AutoMatch = &HCC
    End Enum
    Public Enum MapTransferState As Byte
        Idle = 1
        Uploading = 2
        Downloading = 3
    End Enum
    Public Enum RejectReason As UInteger
        GameNotFound = 0
        GameFull = 9
        GameAlreadyStarted = 10
        IncorrectPassword = 27
    End Enum
    Public Enum NonGameActionType As Byte
        LobbyChat = &H10
        SetTeam = &H11
        SetColor = &H12
        SetRace = &H13
        SetHandicap = &H14
        GameChat = &H20
    End Enum
    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")>
    Public Enum ChatType As Byte
        Lobby = &H10
        Game = &H20
    End Enum
    ''' <remarks>
    ''' It appears that anything larger than 2 is considered 'Private', but wc3 does send different codes for each player.
    ''' </remarks>
    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")>
    Public Enum ChatGroup As UInt32
        AllPlayers = 0
        Allies = 1
        Observers = 2
        Player1 = 3
        Player2 = 4
        Player3 = 5
        Player4 = 6
        Player5 = 7
        Player6 = 8
        Player7 = 9
        Player8 = 10
        Player9 = 11
        Player10 = 12
        Player11 = 13
        Player12 = 14
        [Private] = 16
    End Enum
    Public Enum ComputerLevel As Byte
        Easy = 0
        Normal = 1
        Insane = 2
    End Enum
    <Flags()>
    Public Enum Races As Byte
        Human = 1 << 0
        Orc = 1 << 1
        NightElf = 1 << 2
        Undead = 1 << 3

        Random = 1 << 5
        Unlocked = 1 << 6 'presence determines if race selection enabled in lobby
    End Enum
    Public Enum PlayerColor As Byte
        Red = 0
        Blue = 1
        Teal = 2
        Purple = 3
        Yellow = 4
        Orange = 5
        Green = 6
        Pink = 7
        Grey = 8
        LightBlue = 9
        DarkGreen = 10
        Brown = 11
        Observer = 12
    End Enum
    Public Enum SlotState As Byte
        Open = 0
        Closed = 1
        Occupied = 2
    End Enum

    'verification disabled because this class causes the verifier to go OutOfMemory
    <ContractVerification(False)>
    Public Module Packets
        Public Const PacketPrefix As Byte = &HF7

        '''<completionlist cref="Packets"/>
        Public MustInherit Class Definition
            Private ReadOnly _id As PacketId
            Private ReadOnly _jar As ISimpleJar

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_jar IsNot Nothing)
            End Sub

            Friend Sub New(ByVal id As PacketId, ByVal jar As ISimpleJar)
                Contract.Requires(jar IsNot Nothing)
                Me._id = id
                Me._jar = jar
            End Sub

            Public ReadOnly Property Id As PacketId
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
        '''<completionlist cref="Packets"/>
        Public NotInheritable Class Definition(Of T)
            Inherits Definition
            Private ReadOnly _jar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_jar IsNot Nothing)
            End Sub

            Friend Sub New(ByVal id As PacketId, ByVal jar As IJar(Of T))
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

        Private Function Define(Of T)(ByVal id As PacketId,
                                      ByVal jar As IJar(Of T)) As Definition(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Definition(Of T))() IsNot Nothing)
            Return New Definition(Of T)(id, jar)
        End Function
        Private Function Define(ByVal id As PacketId,
                                ByVal jar1 As ISimpleNamedJar,
                                ByVal jar2 As ISimpleNamedJar,
                                ByVal ParamArray jars() As ISimpleNamedJar) As Definition(Of NamedValueMap)
            Contract.Requires(jar1 IsNot Nothing)
            Contract.Requires(jar2 IsNot Nothing)
            Contract.Requires(jars IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Definition(Of NamedValueMap))() IsNot Nothing)
            Return Define(id, New TupleJar(jars.Prepend(jar1, jar2).ToArray))
        End Function

        Public Const MaxSerializedPlayerNameLength As Integer = 16
        Public Const MaxPlayerNameLength As Integer = MaxSerializedPlayerNameLength - 1
        Public Const MaxFileDataSize As UInt32 = 1442
        Public Const MaxSerializedChatTextLength As Integer = 221
        Public Const MaxChatTextLength As Integer = MaxSerializedChatTextLength - 1

        Public NotInheritable Class ServerPackets
            Private Sub New()
            End Sub

            '''<summary>Sent from server in ladder games. Exact contents not known, but probably wraps things like <see cref="LobbyState"/>/<see cref="HostMapInfo"/>.</summary>
            Public Shared ReadOnly EncryptedServerMeleeData As Definition(Of IReadableList(Of Byte)) = Define(PacketId.EncryptedServerMeleeData,
                    New DataJar().Named("encrypted data"))
            '''<summary>Sent at end of ladder games. (? check again)</summary>
            Public Shared ReadOnly GameEnd As Definition(Of EmptyJar.EmptyValue) = Define(PacketId.GameEnd,
                    New EmptyJar())
            ''' <summary>
            ''' Sent by server in response to <see cref="ClientPackets.Knock"/> to indicate the client has entered the game.
            ''' This packet has two forms: one includes the data from the <see cref="LobbyState"/> packet, and the other doesn't.
            ''' </summary>
            Public Shared ReadOnly Greet As Definition(Of NamedValueMap) = Define(PacketId.Greet,
                    New DataJar().DataSizePrefixed(prefixSize:=2).Named("slot data"),
                    New PlayerIdJar().Named("assigned id"),
                    New Bnet.Protocol.IPEndPointJar().Named("external address"))
            ''' <summary>
            ''' Response to ClientConfirmHostLeaving.
            ''' </summary>
            Public Shared ReadOnly HostConfirmHostLeaving As Definition(Of EmptyJar.EmptyValue) = Define(PacketId.HostConfirmHostLeaving,
                         New EmptyJar())
            ''' <summary>
            ''' Sent by the server to new clients after they have entered the game.
            ''' Contains information about the map they must have to play the game.
            ''' Specifies the map transfer key, which is included in all map-related packets (players ignore them if it does not match)
            ''' </summary>
            Public Shared ReadOnly HostMapInfo As Definition(Of NamedValueMap) = Define(PacketId.HostMapInfo,
                    New UInt32Jar().Named("map transfer key"),
                    New UTF8Jar().NullTerminated.Named("path"),
                    New UInt32Jar().Named("size"),
                    New UInt32Jar(showhex:=True).Named("crc32"),
                    New UInt32Jar(showhex:=True).Named("xoro checksum"),
                    New DataJar().Fixed(exactDataCount:=20).Named("sha1 checksum"))
            '''<summary>Broadcast on lan when a game is created.</summary>
            Public Shared ReadOnly LanCreateGame As Definition(Of NamedValueMap) = Define(PacketId.LanCreateGame,
                    New ASCIIJar().Reversed.Fixed(exactDataCount:=4).Named("product id"),
                    New UInt32Jar().Named("major version"),
                    New UInt32Jar().Named("game id"))
            '''<summary>Broadcast on lan when a game is cancelled.</summary>
            Public Shared ReadOnly LanDestroyGame As Definition(Of UInt32) = Define(PacketId.LanDestroyGame,
                    New UInt32Jar().Named("game id"))
            '''<summary>Response to LanRequestGame containing detailed game information.</summary>
            Public Shared ReadOnly LanGameDetails As Definition(Of NamedValueMap) = Define(PacketId.LanGameDetails,
                    New ASCIIJar().Reversed.Fixed(exactDataCount:=4).Named("product id"),
                    New UInt32Jar().Named("major version"),
                    New UInt32Jar().Named("game id"),
                    New UInt32Jar(showhex:=True).Named("entry key"),
                    New UTF8Jar().NullTerminated.Named("name"),
                    New UTF8Jar().NullTerminated.Named("password"),
                    New GameStatsJar().Named("statstring"),
                    New UInt32Jar().Named("num slots"),
                    New EnumUInt32Jar(Of GameTypes)().Named("game type"),
                    New UInt32Jar().Named("num players + 1"),
                    New UInt32Jar().Named("free slots + 1"),
                    New UInt32Jar().Named("age"),
                    New UInt16Jar().Named("listen port"))
            ''' <summary>
            ''' Broadcast on lan periodically to inform new listening wc3 clients a game exists.
            ''' Contains only very basic information about the game [no map, name, etc].
            ''' </summary>
            Public Shared ReadOnly LanRefreshGame As Definition(Of NamedValueMap) = Define(PacketId.LanRefreshGame,
                    New UInt32Jar().Named("game id"),
                    New UInt32Jar().Named("num players"),
                    New UInt32Jar().Named("free slots"))
            '''<summary>Broadcast by server to all clients when the lobby state changes.</summary>
            Public Shared ReadOnly LobbyState As Definition(Of NamedValueMap) = Define(PacketId.LobbyState,
                    New TupleJar(
                            New SlotJar().RepeatedWithCountPrefix(prefixSize:=1).Named("slots"),
                            New UInt32Jar(showHex:=True).Named("random seed"),
                            New EnumByteJar(Of LobbyLayoutStyle)().Named("layout style"),
                            New ByteJar().Named("num player slots")
                        ).DataSizePrefixed(prefixSize:=2))
            '''<summary>Sent from server in ladder games before <see cref="StartCountdown"/>.</summary>
            Public Shared ReadOnly MeleePlayerInfo As Definition(Of IReadableList(Of NamedValueMap)) = Define(PacketId.MeleePlayerInfo,
                    New TupleJar(
                            New PlayerIdJar().Named("player"),
                            New DataJar().DataSizePrefixed(prefixSize:=1).Named("info")
                        ).RepeatedWithCountPrefix(prefixSize:=1))
            ''' <summary>
            ''' Sent by the host when leaving, telling players who the new host will be.
            ''' </summary>
            Public Shared ReadOnly NewHost As Definition(Of PlayerId) = Define(PacketId.NewHost,
                    New PlayerIdJar().Named("player"))
            '''<summary>Broadcast by server to other clients when a client enters the game.</summary>
            Public Shared ReadOnly OtherPlayerJoined As Definition(Of NamedValueMap) = Define(PacketId.OtherPlayerJoined,
                    New UInt32Jar(showhex:=True).Named("peer key"),
                    New PlayerIdJar().Named("joiner id"),
                    New UTF8Jar().NullTerminated.Limited(maxDataCount:=MaxSerializedPlayerNameLength).Named("name"),
                    New DataJar().DataSizePrefixed(prefixSize:=1).Named("peer data"),
                    New Bnet.Protocol.IPEndPointJar().Named("external address"),
                    New Bnet.Protocol.IPEndPointJar().Named("internal address"))
            '''<summary>Broadcast server to other clients when a client leaves the game.</summary>
            Public Shared ReadOnly OtherPlayerLeft As Definition(Of NamedValueMap) = Define(PacketId.OtherPlayerLeft,
                    New PlayerIdJar().Named("leaver"),
                    New EnumUInt32Jar(Of PlayerLeaveReason)().Named("reason"))
            ''' <summary>
            ''' Broadcast by server to all clients in response to a client sending <see cref="ClientPackets.Ready"/>.
            ''' Clients start playing as soon as they have received this packet for each client.
            ''' </summary>
            Public Shared ReadOnly OtherPlayerReady As Definition(Of PlayerId) = Define(PacketId.OtherPlayerReady,
                    New PlayerIdJar().Named("readied player"))
            ''' <summary>
            ''' Sent periodically by server to clients as a keep-alive packet.
            ''' Clients should respond with an equivalent <see cref="ClientPackets.Pong"/>.
            ''' Clients which do not receive a <see cref="ServerPackets.Ping"/> or <see cref="ServerPackets.Tick"/> for ~60s will disconnect.
            ''' If the server does not receive <see cref="ClientPackets.Pong"/> or <see cref="ClientPackets.Tock"/> from a client for ~60s, it will disconnect the client.
            ''' </summary>
            Public Shared ReadOnly Ping As Definition(Of UInt32) = Define(PacketId.Ping,
                    New UInt32Jar(showHex:=True).Named("salt"))
            ''' <summary>
            ''' Sent by the server when one or more players have stopped responding in a timely fashion.
            ''' </summary>
            Public Shared ReadOnly PlayersLagging As Definition(Of IReadableList(Of NamedValueMap)) = Define(PacketId.PlayersLagging,
                    New TupleJar(True,
                            New PlayerIdJar().Named("id"),
                            New UInt32Jar().Named("initial milliseconds used")
                        ).RepeatedWithCountPrefix(prefixSize:=1).Named("laggers"))
            ''' <summary>
            ''' Sent by the server when a player has resumed responding in a timely fashion.
            ''' </summary>
            Public Shared ReadOnly PlayerStoppedLagging As Definition(Of NamedValueMap) = Define(PacketId.PlayerStoppedLagging,
                    New PlayerIdJar().Named("lagger"),
                    New UInt32Jar().Named("marginal milliseconds used"))
            '''<summary>Sent by server in response to <see cref="ClientPackets.Knock"/> to indicate the client did not enter the game.</summary>
            Public Shared ReadOnly RejectEntry As Definition(Of RejectReason) = Define(PacketId.RejectEntry,
                    New EnumUInt32Jar(Of RejectReason)().Named("reason"))
            ''' <summary>
            ''' Sent by the server to tell a client to start downloading the map from the server or from another client.
            ''' <see cref="SetUploadTarget"/> must be sent to the other client for the peer to peer transfer to work.
            ''' </summary>
            Public Shared ReadOnly SetDownloadSource As Definition(Of NamedValueMap) = Define(PacketId.SetDownloadSource,
                    New UInt32Jar().Named("map transfer key"),
                    New PlayerIdJar().Named("uploader"))
            ''' <summary>
            ''' Sent by the server to tell a client to start uploading to another client.
            ''' <see cref="SetDownloadSource"/> must be sent to the other client for the transfer to work.
            ''' </summary>
            Public Shared ReadOnly SetUploadTarget As Definition(Of NamedValueMap) = Define(PacketId.SetUploadTarget,
                    New UInt32Jar().Named("map transfer key"),
                    New PlayerIdJar().Named("downloader"),
                    New UInt32Jar().Named("starting file pos"))
            ''' <summary>
            ''' Broadcast by server to all clients to start the countdown.
            ''' Clients will disconnect if they receive this packet more than once.
            ''' Can be sent without sending <see cref="StartLoading"/> afterwards (wc3 will wait at 0 seconds indefinitely).
            ''' </summary>
            Public Shared ReadOnly StartCountdown As Definition(Of EmptyJar.EmptyValue) = Define(PacketId.StartCountdown,
                    New EmptyJar())
            ''' <summary>
            ''' Broadcast by server to all clients to tell them to start loading the map.
            ''' Clients will disconnect if they receive this packet more than once.
            ''' Does not require <see cref="StartCountdown"/> to have been sent.
            ''' </summary>
            Public Shared ReadOnly StartLoading As Definition(Of EmptyJar.EmptyValue) = Define(PacketId.StartLoading,
                    New EmptyJar())
            ''' <summary>
            ''' Relayed by server to clients not connected directly to the sender.
            ''' Different formats in game and in lobby.
            ''' Clients will only request relay to clients who should receive the message (eg. only allies for ally chat).
            ''' </summary>
            Public Shared ReadOnly Text As Definition(Of NamedValueMap) = Define(PacketId.Text, New TupleJar(
                    New PlayerIdJar().RepeatedWithCountPrefix(prefixSize:=1, useSingleLineDescription:=True).Named("requested receivers"),
                    New PlayerIdJar().Named("speaker"),
                    New KeyPrefixedJar(Of ChatType)(
                            useSingleLineDescription:=False,
                            keyJar:=New EnumByteJar(Of ChatType)().Named("type"),
                            valueJars:=New Dictionary(Of ChatType, ISimpleJar) From {
                                {ChatType.Game, New EnumUInt32Jar(Of ChatGroup)().Named("receiving group")},
                                {ChatType.Lobby, New EmptyJar().Named("receiving group")}}
                        ).Named("type group"),
                    New UTF8Jar(maxCharCount:=MaxChatTextLength).NullTerminated.Limited(maxDataCount:=MaxSerializedChatTextLength).Named("message")))
            ''' <summary>
            ''' Broadcast by server to all clients periodically during game play.
            ''' Contains client actions received by the server, which will be applied at the current game time.
            ''' Contains a timespan, in milliseconds, during which no more actions will be applied.
            ''' - The client will run the game up to 'current game time + given timespan' before host-lag-pausing.
            ''' - This is how synchronization and smooth progression of game time are achieved.
            ''' Significantly altering the reported timespan to real time ratio can have weird effects, including game time stopping and losing apparent game time.
            ''' </summary>
            Public Shared ReadOnly Tick As Definition(Of NamedValueMap) = Define(PacketId.Tick,
                    New UInt16Jar().Named("time span"),
                    New PlayerActionSetJar().Repeated.CRC32ChecksumPrefixed(prefixSize:=2).Optional.Named("player action sets"))
            ''' <summary>
            ''' Same format as tick, except time span is always 0.
            ''' Used when there is too much action data to fit in a single tick.
            ''' </summary>
            Public Shared ReadOnly TickPreOverflow As Definition(Of NamedValueMap) = Define(PacketId.TickPreOverflow,
                    New UInt16Jar().Named("time span"),
                    New PlayerActionSetJar().Repeated.CRC32ChecksumPrefixed(prefixSize:=2).Named("player action sets"))
            '''<summary>Tells players the time remaining in a tournament game. Causes players to disc in custom games.</summary>
            Public Shared ReadOnly TournamentCountdown As Definition(Of NamedValueMap) = Define(PacketId.TournamentCountdown,
                    New UInt32Jar().Named("unknown"),
                    New UInt32Jar().Named("time left"))
        End Class
        Public NotInheritable Class ClientPackets
            Private Sub New()
            End Sub

            ''' <summary>
            ''' Sometimes (not sure on conditions) sent by clients to the host after the host declares it is leaving.
            ''' Host responds with <see cref="ServerPackets.HostConfirmHostLeaving"/>
            ''' </summary>
            Public Shared ReadOnly ClientConfirmHostLeaving As Definition(Of EmptyJar.EmptyValue) = Define(PacketId.ClientConfirmHostLeaving,
                    New EmptyJar())
            '''<summary>Sent by clients to the server in response to <see cref="ServerPackets.HostMapInfo"/> and when the client has received more of the map file.</summary>
            Public Shared ReadOnly ClientMapInfo As Definition(Of NamedValueMap) = Define(PacketId.ClientMapInfo,
                    New UInt32Jar().Named("map transfer key"),
                    New EnumByteJar(Of MapTransferState)().Named("transfer state"),
                    New UInt32Jar().Named("total downloaded"))
            '''<summary>Response to <see cref="ServerPackets.EncryptedServerMeleeData"/>.</summary>
            Public Shared ReadOnly EncryptedClientMeleeData As Definition(Of IReadableList(Of Byte)) = Define(PacketId.EncryptedClientMeleeData,
                    New DataJar().Named("encrypted data"))
            ''' <summary>
            ''' Sent by clients when they perform game actions such as orders, alliance changes, trigger events, etc.
            ''' The server includes this data in its next <see cref="ServerPackets.Tick"/> packet, broadcast to all the clients.
            ''' Clients don't perform an action until it shows up in the <see cref="ServerPackets.Tick"/> packet.
            ''' If the <see cref="ServerPackets.Tick"/> packet's actions disagree with the client's actions, the client will disconnect.
            ''' </summary>
            Public Shared ReadOnly GameAction As Definition(Of IReadableList(Of GameAction)) = Define(PacketId.GameAction,
                    New GameActionJar().Repeated.CRC32ChecksumPrefixed.Named("actions"))
            '''<summary>First thing sent by clients upon connection. Requests entry into the game.</summary>
            Public Shared ReadOnly Knock As Definition(Of NamedValueMap) = Define(PacketId.Knock,
                    New UInt32Jar().Named("game id"),
                    New UInt32Jar(showhex:=True).Named("entry key"),
                    New ByteJar().Named("unknown value"),
                    New UInt16Jar().Named("listen port"),
                    New UInt32Jar(showhex:=True).Named("peer key"),
                    New UTF8Jar(maxCharCount:=MaxPlayerNameLength).NullTerminated.Limited(maxDataCount:=MaxSerializedPlayerNameLength).Named("name"),
                    New DataJar().DataSizePrefixed(prefixSize:=1).Named("peer data"),
                    New Bnet.Protocol.IPEndPointJar().Named("internal address"))
            '''<summary>Response to <see cref="ServerPackets.LanRefreshGame"/> or <see cref="ServerPackets.LanCreateGame"/> when clients want to know game info.</summary>
            Public Shared ReadOnly LanRequestGame As Definition(Of NamedValueMap) = Define(PacketId.LanRequestGame,
                    New ASCIIJar().Reversed.Fixed(exactDataCount:=4).Named("product id"),
                    New UInt32Jar().Named("major version"),
                    New UInt32Jar().Named("unknown1"))
            '''<summary>Sent by clients before they intentionally disconnect.</summary>
            Public Shared ReadOnly Leaving As Definition(Of PlayerLeaveReason) = Define(PacketId.Leaving,
                    New EnumUInt32Jar(Of PlayerLeaveReason)().Named("reason"))
            ''' <summary>
            ''' Sent when clients chat or perform lobby actions.
            ''' </summary>
            Public Shared ReadOnly NonGameAction As Definition(Of NamedValueMap) = Define(PacketId.NonGameAction, New TupleJar(
                    New PlayerIdJar().RepeatedWithCountPrefix(prefixSize:=1, useSingleLineDescription:=True).Named("requested receivers"),
                    New PlayerIdJar().Named("sender"),
                    New KeyPrefixedJar(Of NonGameActionType)(
                        useSingleLineDescription:=False,
                        keyJar:=New EnumByteJar(Of NonGameActionType),
                        valueJars:=New Dictionary(Of NonGameActionType, ISimpleJar) From {
                                {NonGameActionType.GameChat, New TupleJar(
                                        New EnumUInt32Jar(Of ChatGroup)().Named("receiving group"),
                                        New UTF8Jar().NullTerminated.Limited(maxDataCount:=MaxSerializedChatTextLength).Named("message"))},
                                {NonGameActionType.LobbyChat, New TupleJar(
                                        New UTF8Jar().NullTerminated.Limited(maxDataCount:=MaxSerializedChatTextLength).Named("message"))},
                                {NonGameActionType.SetTeam, New ByteJar().Named("team")},
                                {NonGameActionType.SetHandicap, New ByteJar().Named("handicap")},
                                {NonGameActionType.SetRace, New EnumByteJar(Of Races)().Named("race")},
                                {NonGameActionType.SetColor, New EnumByteJar(Of PlayerColor)().Named("color")}
                            }).Named("value")))
            '''<summary>Sent by clients to the server to inform the server when the set of other clients they are interconnected with changes.</summary>
            Public Shared ReadOnly PeerConnectionInfo As Definition(Of UInt16) = Define(PacketId.PeerConnectionInfo,
                    New UInt16Jar(showhex:=True).Named("player bitflags"))
            '''<summary>Sent by clients in response to <see cref="ServerPackets.Ping"/>.</summary>
            Public Shared ReadOnly Pong As Definition(Of UInt32) = Define(PacketId.Pong,
                    New UInt32Jar(showHex:=True).Named("salt"))
            '''<summary>Sent by clients once they have finished loading the map and are ready to start playing.</summary>
            Public Shared ReadOnly Ready As Definition(Of EmptyJar.EmptyValue) = Define(PacketId.Ready,
                    New EmptyJar())
            Public Shared ReadOnly RequestDropLaggers As Definition(Of EmptyJar.EmptyValue) = Define(PacketId.RequestDropLaggers,
                    New EmptyJar())
            ''' <summary>
            ''' Sent by clients in response to <see cref="ServerPackets.Tick"/>.
            ''' Contains a checksum of the client's game state, which is used to detect desyncs.
            ''' The lag screen is shown if a client takes too long to send a response <see cref="Tock"/>.
            ''' </summary>
            Public Shared ReadOnly Tock As Definition(Of NamedValueMap) = Define(PacketId.Tock,
                    New ByteJar().Named("unknown"),
                    New UInt32Jar(showhex:=True).Named("game state checksum"))
        End Class
        Public NotInheritable Class PeerPackets
            Private Sub New()
            End Sub

            '''<summary>Sent to to downloaders during map transfer. Contains map file data.</summary>
            Public Shared ReadOnly MapFileData As Definition(Of NamedValueMap) = Define(PacketId.MapFileData,
                    New PlayerIdJar().Named("downloader"),
                    New PlayerIdJar().Named("uploader"),
                    New UInt32Jar().Named("map transfer key"),
                    New UInt32Jar().Named("file position"),
                    New DataJar().Limited(maxDataCount:=MaxFileDataSize).CRC32ChecksumPrefixed.Named("file data"))
            ''' <summary>
            ''' Negative response to <see cref="MapFileData"/>.
            ''' This can be caused by corrupted data or by sending <see cref="MapFileData"/> before <see cref="ServerPackets.SetDownloadSource"/> is sent.
            ''' Even though wc3 clients send this packet if data is sent before <see cref="ServerPackets.SetDownloadSource"/>, they still accept and use the data.
            ''' </summary>
            Public Shared ReadOnly MapFileDataProblem As Definition(Of NamedValueMap) = Define(PacketId.MapFileDataProblem,
                    New PlayerIdJar().Named("downloader"),
                    New PlayerIdJar().Named("uploader"),
                    New UInt32Jar().Named("map transfer key"))
            '''<summary>Positive response to <see cref="MapFileData"/>.</summary>
            Public Shared ReadOnly MapFileDataReceived As Definition(Of NamedValueMap) = Define(PacketId.MapFileDataReceived,
                    New PlayerIdJar().Named("downloader"),
                    New PlayerIdJar().Named("uploader"),
                    New UInt32Jar().Named("map transfer key"),
                    New UInt32Jar().Named("total downloaded"))
            Public Shared ReadOnly PeerKnock As Definition(Of NamedValueMap) = Define(PacketId.PeerKnock,
                    New UInt32Jar(showhex:=True).Named("receiver peer key"),
                    New UInt32Jar().Named("unknown1"),
                    New PlayerIdJar().Named("sender id"),
                    New ByteJar().Named("unknown3"),
                    New UInt32Jar().Named("sender peer connection flags"))
            Public Shared ReadOnly PeerPing As Definition(Of NamedValueMap) = Define(PacketId.PeerPing,
                    New UInt32Jar(showhex:=True).Named("salt"),
                    New UInt32Jar().Named("sender peer connection flags"),
                    New UInt32Jar().Named("unknown2"))
            Public Shared ReadOnly PeerPong As Definition(Of UInt32) = Define(PacketId.PeerPong,
                    New UInt32Jar(showhex:=True).Named("salt"))
        End Class
    End Module
End Namespace
