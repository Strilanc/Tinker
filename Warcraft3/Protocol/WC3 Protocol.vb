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
        '_Unseen0 = &H0
        ''' <summary>
        ''' Sent periodically by server to clients as a keep-alive packet.
        ''' Clients should respond with an equivalent PONG.
        ''' Clients which do not receive a PING or TICK for ~60s will disconnect.
        ''' If the server does not receive PONG or GAME_TICK_GUEST from a client for ~60s, it will disconnect the client.
        ''' </summary>
        Ping = &H1
        '_Unseen2 = &H2
        '_Unseen3 = &H3
        ''' <summary>
        ''' Sent by server in response to KNOCK to indicate the client has entered the game.
        ''' This packet has two forms: one includes the data from the SLOT_LAYOUT packet, and the other doesn't.
        ''' </summary>
        Greet = &H4
        '''<summary>Sent by server in response to KNOCK to indicate the client did not enter the game.</summary>
        RejectEntry = &H5
        '''<summary>Broadcast by server to other clients when a client enters the game.</summary>
        OtherPlayerJoined = &H6
        '''<summary>Broadcast server to other clients when a client leaves the game.</summary>
        OtherPlayerLeft = &H7
        ''' <summary>
        ''' Broadcast by server to all clients in response to a client sending READY.
        ''' Clients start playing as soon as they have received this packet for each client.
        ''' </summary>
        OtherPlayerReady = &H8
        '''<summary>Broadcast by server to all clients when the lobby state changes.</summary>
        LobbyState = &H9
        ''' <summary>
        ''' Broadcast by server to all clients to start the countdown.
        ''' Clients will disconnect if they receive this packet more than once.
        ''' StartCountdown can be sent without sending StartLoading afterwards (wc3 will wait at 0 seconds indefinitely).
        ''' </summary>
        StartCountdown = &HA
        ''' <summary>
        ''' Broadcast by server to all clients to tell them to start loading the map.
        ''' Clients will disconnect if they receive this packet more than once.
        ''' StartLoading does not require StartCountdown to have been sent.
        ''' </summary>
        StartLoading = &HB
        ''' <summary>
        ''' Broadcast by server to all clients periodically during game play.
        ''' Contains client actions received by the server, which will be applied at the current game time.
        ''' Contains a timespan, in milliseconds, during which no more actions will be applied.
        ''' - The client will run the game up to 'current game time + given timespan' before host-lag-pausing.
        ''' - This is how synchronization and smooth progression of game time are achieved.
        ''' Significantly altering the reported timespan to real time ratio can have weird effects, including game time stopping and losing apparent game time.
        ''' 
        ''' The sub packet format:
        '''   0 WORD truncated crc32 of following data
        '''   1 BYTE player index of sender
        '''   2 DWORD following size of subpacket
        '''   3 BYTE subpacket id
        '''   ... [depends on subpacket] ...
        ''' </summary>
        Tick = &HC
        '_UnseenD = &HD
        '_UnseenE = &HE
        ''' <summary>
        ''' Relayed by server to clients not connected directly to the sender.
        ''' Different formats in game and in lobby.
        ''' Clients will only request relay to clients who should receive the message (eg. only allies for ally chat).
        ''' </summary>
        Text = &HF
        ShowLagScreen = &H10
        RemovePlayerFromLagScreen = &H11
        '_Unseen12 = &H12
        '_Unseen13 = &H13
        ''' <summary>
        ''' Sent by the host when leaving, telling players who the new host will be.
        ''' </summary>
        NewHost = &H14
        PeerHostTransferExternalAddresses = &H15
        PeerHostTransferPlayerInternalAddress = &H16
        ''' <summary>
        ''' Response to ClientConfirmHostLeaving.
        ''' </summary>
        HostConfirmHostLeaving = &H17
        '_Unseen18 = &H18
        PeerPromoteToHostAndContinue = &H19
        '_Unseen1A = &H1A
        GameEnd = &H1B
        '_Unseen1C = &H1C
        '_Unseen1D = &H1D
        '''<summary>First thing sent by clients upon connection. Requests entry into the game.</summary>
        Knock = &H1E
        '_Unseen1F = &H1F
        '_Unseen20 = &H20
        '''<summary>Sent by clients before they intentionally disconnect.</summary>
        Leaving = &H21
        '_Unseen22 = &H22
        '''<summary>Sent by clients once they have finished loading the map and are ready to start playing.</summary>
        Ready = &H23
        '_Unseen24 = &H24
        '_Unseen25 = &H25
        ''' <summary>
        ''' Sent by clients when they perform game actions such as orders, alliance changes, trigger events, etc.
        ''' The server includes this data in its next Tick packet, broadcast to all the clients.
        ''' Clients don't perform an action until it shows up in the Tick packet.
        ''' If the TICK packet's actions disagree with the client's actions, the client will disconnect.
        ''' </summary>
        GameAction = &H26
        ''' <summary>
        ''' Sent by clients in response to Tick.
        ''' Contains a checksum of the client's game state, which is used to detect desyncs.
        ''' The lag screen is shown if a client takes too long to send a response TOCK.
        ''' </summary>
        Tock = &H27
        NonGameAction = &H28
        RequestDropLaggers = &H29
        '_Unseen2A = &H2A
        PeerHostTransferSelfInternalAddress = &H2B
        ''' <summary>
        ''' Sometimes (not sure on conditions) sent by clients to the host after the host declares it is leaving.
        ''' Host responds with HostConfirmHostLeaving
        ''' </summary>
        ClientConfirmHostLeaving = &H2C
        '_Unseen2D = &H2D
        '_Unseen2E = &H2E
        '''<summary>Response to LanRefreshGame or LanCreateGame when clients want to know game info.</summary>
        LanRequestGame = &H2F
        '''<summary>Response to LanRequestGame containing detailed game information.</summary>
        LanGameDetails = &H30
        '''<summary>Broadcast on lan when a game is created.</summary>
        LanCreateGame = &H31
        ''' <summary>
        ''' Broadcast on lan periodically to inform new listening wc3 clients a game exists.
        ''' Contains only very basic information about the game [no map, name, etc].
        ''' </summary>
        LanRefreshGame = &H32
        '''<summary>Broadcast on lan when a game is cancelled.</summary>
        LanDestroyGame = &H33
        PeerChat = &H34
        PeerPing = &H35
        PeerPong = &H36 'No; I refuse to say it. It's a bad pun.
        PeerKnock = &H37
        '_PeerUnknown38 = &H38
        '_PeerUnknown39 = &H39
        '_Unseen3A = &H3A
        '''<summary>Sent by clients to the server to inform the server when the set of other clients they are interconnected with changes.</summary>
        PeerConnectionInfo = &H3B
        '_Unseen3C = &H3C
        ''' <summary>
        ''' Sent by the server to new clients after they have entered the game.
        ''' Contains information about the map they must have to play the game.
        ''' Specifies the map transfer key, which is included in all map-related packets (players ignore them if it does not match)
        ''' </summary>
        HostMapInfo = &H3D
        ''' <summary>
        ''' Sent by the server to tell a client to start uploading to another client.
        ''' SetDownloadSource must be sent to the other client for the transfer to work.
        ''' </summary>
        SetUploadTarget = &H3E
        ''' <summary>
        ''' Sent by the server to tell a client to start downloading the map from the server or from another client.
        ''' SetUploadTarget must be sent to the other client for the peer to peer transfer to work.
        ''' </summary>
        SetDownloadSource = &H3F
        '_Unseen40 = &H40
        '_Unseen41 = &H41
        '''<summary>Sent by clients to the server in response to HostMapInfo and when the client has received more of the map file.</summary>
        ClientMapInfo = &H42
        '''<summary>Sent to to downloaders during map transfer. Contains map file data.</summary>
        MapFileData = &H43
        '''<summary>Positive response to MapFileData.</summary>
        MapFileDataReceived = &H44
        ''' <summary>
        ''' Negative response to MapFileData.
        ''' This can be caused by corrupted data or by sending MapFileData before SetDownloadSource is sent.
        ''' Even though wc3 clients send this packet if data is sent before SetDownloadSource, they still accept and use the data.
        ''' </summary>
        MapFileDataProblem = &H45
        '''<summary>Sent by clients in response to PING.</summary>
        Pong = &H46

        '''<summary>Tells players the time remaining in a tournament game. Causes players to disc in custom games.</summary>
        TournamentCountdown = &H50
        EncryptedServerMeleeData = &H51
        EncryptedClientMeleeData = &H52
    End Enum

    <Flags()>
    Public Enum GameTypes As UInteger
        CreateGameUnknown0 = 1 << 0 'this bit always seems to be set by wc3

        '''<summary>Setting this bit causes wc3 to check the map and disc if it is not signed by Blizzard</summary>
        AuthenticatedMakerBlizzard = 1 << 3
        OfficialMeleeGame = 1 << 5

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
    Public Enum PlayerLeaveReason As Byte
        Disconnect = 1
        Quit = 7
        Defeat = 8
        Victory = 9
        Tie = 10
        NeutralOrEndOfGame = 11
        Lobby = 13
    End Enum
    Public Enum LobbyLayoutStyle
        Melee = 0
        CustomForces = 1
        FixedPlayerSettings = 3
        AutoMatch = &HCC
    End Enum
    Public Enum MapTransferState As Byte
        Idle = 1
        Uploading = 2
        Downloading = 3
        Unknown4 = 4
    End Enum
    Public Enum RejectReason As UInteger
        GameNotFound = 0
        GameFull = 9
        GameAlreadyStarted = 10
        IncorrectPassword = 27
    End Enum
    Public Enum NonGameAction As Byte
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
    Public Enum ChatReceiverType As UInt32
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

    'verification disabled because this class causes the verifier to go OutOfMemory
    <ContractVerification(False)>
    Public NotInheritable Class Packets
        Private Sub New()
        End Sub
        Public Const PacketPrefix As Byte = &HF7

        Public NotInheritable Class Definition(Of T)
            Private ReadOnly _id As PacketId
            Private ReadOnly _jar As IJar(Of T)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_jar IsNot Nothing)
            End Sub

            Friend Sub New(ByVal id As PacketId, ByVal jar As IJar(Of T))
                Contract.Requires(jar IsNot Nothing)
                Me._id = id
                Me._jar = jar
            End Sub

            Public ReadOnly Property Id As PacketId
                Get
                    Return _id
                End Get
            End Property
            Public ReadOnly Property Jar As IJar(Of T)
                Get
                    Contract.Ensures(Contract.Result(Of IJar(Of T))() IsNot Nothing)
                    Return _jar
                End Get
            End Property
        End Class
        Private Shared Function Define(ByVal id As PacketId) As Definition(Of Object)
            Return New Definition(Of Object)(id, New EmptyJar(id.ToString))
        End Function
        Private Shared Function Define(Of T)(ByVal id As PacketId, ByVal jar As IJar(Of T)) As Definition(Of T)
            Contract.Requires(jar IsNot Nothing)
            Return New Definition(Of T)(id, jar)
        End Function
        Private Shared Function Define(ByVal id As PacketId,
                                       ByVal jar1 As IJar(Of Object),
                                       ByVal jar2 As IJar(Of Object),
                                       ByVal ParamArray jars() As IJar(Of Object)) As Definition(Of Dictionary(Of InvariantString, Object))
            Contract.Requires(jar1 IsNot Nothing)
            Contract.Requires(jar2 IsNot Nothing)
            Contract.Requires(jars IsNot Nothing)
            Return New Definition(Of Dictionary(Of InvariantString, Object))(id, New TupleJar(id.ToString, Concat({jar1, jar2}, jars)))
        End Function

        Public Shared ReadOnly Ping As Definition(Of UInt32) = Define(PacketId.Ping,
                New UInt32Jar("salt", showHex:=True))
        Public Shared ReadOnly Pong As Definition(Of UInt32) = Define(PacketId.Pong,
                New UInt32Jar("salt", showHex:=True))

        Public Shared ReadOnly Leaving As Definition(Of PlayerLeaveReason) = Define(PacketId.Leaving,
                New EnumUInt32Jar(Of PlayerLeaveReason)("reason"))
        Public Shared ReadOnly OtherPlayerLeft As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.OtherPlayerLeft,
                New ByteJar("player index").Weaken,
                New EnumUInt32Jar(Of PlayerLeaveReason)("reason").Weaken)

        Public Const MaxPlayerNameLength As Integer = 15
        Public Shared ReadOnly Knock As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.Knock,
                New UInt32Jar("game id").Weaken,
                New UInt32Jar("entry key", showhex:=True).Weaken,
                New ByteJar("unknown value").Weaken,
                New UInt16Jar("listen port").Weaken,
                New UInt32Jar("peer key", showhex:=True).Weaken,
                New NullTerminatedStringJar("name", maximumContentSize:=MaxPlayerNameLength).Weaken,
                New RemainingDataJar("peer data").DataSizePrefixed(prefixSize:=1).Weaken,
                New Bnet.Protocol.IPEndPointJar("internal address").Weaken)
        Public Shared ReadOnly Greet As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.Greet,
                New RemainingDataJar("slot data").DataSizePrefixed(prefixSize:=2).Weaken,
                New ByteJar("player index").Weaken,
                New Bnet.Protocol.IPEndPointJar("external address").Weaken)
        Public Shared ReadOnly HostMapInfo As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.HostMapInfo,
                New UInt32Jar("map transfer key").Weaken,
                New NullTerminatedStringJar("path").Weaken,
                New UInt32Jar("size").Weaken,
                New UInt32Jar("crc32", showhex:=True).Weaken,
                New UInt32Jar("xoro checksum", showhex:=True).Weaken,
                New RawDataJar("sha1 checksum", Size:=20).Weaken)
        Public Shared ReadOnly RejectEntry As Definition(Of RejectReason) = Define(PacketId.RejectEntry,
                New EnumUInt32Jar(Of RejectReason)("reason"))
        Public Shared ReadOnly OtherPlayerJoined As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.OtherPlayerJoined,
                New UInt32Jar("peer key", showhex:=True).Weaken,
                New ByteJar("index").Weaken,
                New NullTerminatedStringJar("name", maximumContentSize:=15).Weaken,
                New RemainingDataJar("peer data").DataSizePrefixed(prefixSize:=1).Weaken,
                New Bnet.Protocol.IPEndPointJar("external address").Weaken,
                New Bnet.Protocol.IPEndPointJar("internal address").Weaken)
        Public Shared ReadOnly Text As Definition(Of Dictionary(Of InvariantString, Object)) = MakeTextJar()
        Private Shared Function MakeTextJar() As Definition(Of Dictionary(Of InvariantString, Object))
            Dim jar = New InteriorSwitchJar(Of ChatType, Dictionary(Of InvariantString, Object))(
                    name:=PacketId.Text.ToString,
                    valueKeyExtractor:=Function(val) CType(val("type"), ChatType),
                    dataKeyExtractor:=Function(data) CType(data(data(0) + 2), ChatType))
            jar.AddPackerParser(ChatType.Game, New TupleJar(PacketId.Text.ToString,
                    New ByteJar("pid").RepeatedWithCountPrefix("receiving players", prefixSize:=1, useSingleLineDescription:=True).Weaken,
                    New ByteJar("sending player index").Weaken,
                    New EnumByteJar(Of ChatType)("type").Weaken,
                    New EnumUInt32Jar(Of ChatReceiverType)("receiver type", checkDefined:=False).Weaken,
                    New NullTerminatedStringJar("message").Weaken))
            jar.AddPackerParser(ChatType.Lobby, New TupleJar(PacketId.Text.ToString,
                    New ByteJar("pid").RepeatedWithCountPrefix("receiving players", prefixSize:=1, useSingleLineDescription:=True).Weaken,
                    New ByteJar("sending player index").Weaken,
                    New EnumByteJar(Of ChatType)("type").Weaken,
                    New NullTerminatedStringJar("message").Weaken))
            Return Define(PacketId.Text, jar)
        End Function

        Public Shared ReadOnly OtherPlayerReady As Definition(Of Byte) = Define(PacketId.OtherPlayerReady,
                New ByteJar("player index"))
        Public Shared ReadOnly StartLoading As Definition(Of Object) = Define(PacketId.StartLoading)
        Public Shared ReadOnly StartCountdown As Definition(Of Object) = Define(PacketId.StartCountdown)
        Public Shared ReadOnly Ready As Definition(Of Object) = Define(PacketId.Ready)
        Public Shared ReadOnly LobbyState As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.LobbyState,
                New TupleJar(PacketId.LobbyState.ToString,
                        New SlotJar("slot").RepeatedWithCountPrefix("slots", prefixSize:=1).Weaken,
                        New UInt32Jar("random seed").Weaken,
                        New EnumByteJar(Of LobbyLayoutStyle)("layout style").Weaken,
                        New ByteJar("num player slots").Weaken
                    ).DataSizePrefixed(prefixSize:=2))
        Public Shared ReadOnly PeerConnectionInfo As Definition(Of UInt16) = Define(PacketId.PeerConnectionInfo,
                New UInt16Jar("player bitflags", showhex:=True))

        Public Const MaxChatTextLength As Integer = 220
        Public Shared ReadOnly NonGameAction As Definition(Of Dictionary(Of InvariantString, Object)) = MakeNonGameActionJar()
        Private Shared Function MakeNonGameActionJar() As Definition(Of Dictionary(Of InvariantString, Object))
            Dim commandJar = New InteriorSwitchJar(Of NonGameAction, Dictionary(Of InvariantString, Object))(
                    PacketId.NonGameAction.ToString,
                    Function(val) CType(val("command type"), NonGameAction),
                    Function(data) CType(data(data(0) + 2), NonGameAction))
            commandJar.AddPackerParser(Protocol.NonGameAction.GameChat, New TupleJar(PacketId.NonGameAction.ToString,
                    New RemainingDataJar("receiving player indexes").DataSizePrefixed(prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New EnumUInt32Jar(Of ChatReceiverType)("receiver type").Weaken,
                    New NullTerminatedStringJar("message", maximumContentSize:=MaxChatTextLength).Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.LobbyChat, New TupleJar(PacketId.NonGameAction.ToString,
                    New RemainingDataJar("receiving player indexes").DataSizePrefixed(prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New NullTerminatedStringJar("message", maximumContentSize:=MaxChatTextLength).Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.SetTeam, New TupleJar(PacketId.NonGameAction.ToString,
                    New RemainingDataJar("receiving player indexes").DataSizePrefixed(prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New ByteJar("new value").Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.SetHandicap, New TupleJar(PacketId.NonGameAction.ToString,
                    New RemainingDataJar("receiving player indexes").DataSizePrefixed(prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New ByteJar("new value").Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.SetRace, New TupleJar(PacketId.NonGameAction.ToString,
                    New RemainingDataJar("receiving player indexes").DataSizePrefixed(prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New EnumByteJar(Of Slot.Races)("new value").Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.SetColor, New TupleJar(PacketId.NonGameAction.ToString,
                    New RemainingDataJar("receiving player indexes").DataSizePrefixed(prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New EnumByteJar(Of Slot.PlayerColor)("new value").Weaken))
            Return Define(PacketId.NonGameAction, commandJar)
        End Function

        Public Shared ReadOnly ShowLagScreen As Definition(Of IList(Of Dictionary(Of InvariantString, Object))) = Define(PacketId.ShowLagScreen,
                New TupleJar("lagger", True,
                        New ByteJar("player index").Weaken,
                        New UInt32Jar("initial milliseconds used").Weaken
                    ).RepeatedWithCountPrefix("laggers", prefixSize:=1))
        Public Shared ReadOnly RemovePlayerFromLagScreen As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.RemovePlayerFromLagScreen,
                New ByteJar("player index").Weaken,
                New UInt32Jar("marginal milliseconds used").Weaken)
        Public Shared ReadOnly RequestDropLaggers As Definition(Of Object) = Define(PacketId.RequestDropLaggers)
        Public Shared ReadOnly Tick As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.Tick,
                New UInt16Jar("time span").Weaken,
                New PlayerActionSetJar("player action set").Repeated("player action sets").CRC32ChecksumPrefixed(prefixSize:=2).Optional.Weaken)
        Public Shared ReadOnly Tock As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.Tock,
                New ByteJar("unknown").Weaken,
                New UInt32Jar("game state checksum", showhex:=True).Weaken)
        Public Shared ReadOnly GameAction As Definition(Of IReadableList(Of GameAction)) = Define(PacketId.GameAction,
                New GameActionJar("action").Repeated(name:="actions").CRC32ChecksumPrefixed)
        Public Shared ReadOnly NewHost As Definition(Of Byte) = Define(PacketId.NewHost,
                New ByteJar("player index"))
        Public Shared ReadOnly ClientConfirmHostLeaving As Definition(Of Object) = Define(PacketId.ClientConfirmHostLeaving)
        Public Shared ReadOnly HostConfirmHostLeaving As Definition(Of Object) = Define(PacketId.HostConfirmHostLeaving)

        Public Shared ReadOnly LanRequestGame As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.LanRequestGame,
                New Bnet.Protocol.DwordStringJar("product id").Weaken,
                New UInt32Jar("major version").Weaken,
                New UInt32Jar("unknown1").Weaken)
        Public Shared ReadOnly LanRefreshGame As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.LanRefreshGame,
                New UInt32Jar("game id").Weaken,
                New UInt32Jar("num players").Weaken,
                New UInt32Jar("free slots").Weaken)
        Public Shared ReadOnly LanCreateGame As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.LanCreateGame,
                New Bnet.Protocol.DwordStringJar("product id").Weaken,
                New UInt32Jar("major version").Weaken,
                New UInt32Jar("game id").Weaken)
        Public Shared ReadOnly LanDestroyGame As Definition(Of UInt32) = Define(PacketId.LanDestroyGame,
                New UInt32Jar("game id"))
        Public Shared ReadOnly LanGameDetails As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.LanGameDetails,
                New Bnet.Protocol.DwordStringJar("product id").Weaken,
                New UInt32Jar("major version").Weaken,
                New UInt32Jar("game id").Weaken,
                New UInt32Jar("entry key", showhex:=True).Weaken,
                New NullTerminatedStringJar("name").Weaken,
                New NullTerminatedStringJar("password").Weaken,
                New GameStatsJar("statstring").Weaken,
                New UInt32Jar("num slots").Weaken,
                New EnumUInt32Jar(Of GameTypes)("game type").Weaken,
                New UInt32Jar("num players + 1").Weaken,
                New UInt32Jar("free slots + 1").Weaken,
                New UInt32Jar("age").Weaken,
                New UInt16Jar("listen port").Weaken)

        Public Shared ReadOnly PeerKnock As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.PeerKnock,
                New UInt32Jar("receiver peer key", showhex:=True).Weaken,
                New UInt32Jar("unknown1").Weaken,
                New ByteJar("sender player id").Weaken,
                New ByteJar("unknown3").Weaken,
                New UInt32Jar("sender peer connection flags").Weaken)
        Public Shared ReadOnly PeerPing As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.PeerPing,
                New UInt32Jar("salt", showhex:=True).Weaken,
                New UInt32Jar("sender peer connection flags").Weaken,
                New UInt32Jar("unknown2").Weaken)
        Public Shared ReadOnly PeerPong As Definition(Of UInt32) = Define(PacketId.PeerPong,
                New UInt32Jar("salt", showhex:=True))

        Public Shared ReadOnly ClientMapInfo As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.ClientMapInfo,
                New UInt32Jar("map transfer key").Weaken,
                New EnumByteJar(Of MapTransferState)("transfer state").Weaken,
                New UInt32Jar("total downloaded").Weaken)
        Public Shared ReadOnly SetUploadTarget As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.SetUploadTarget,
                New UInt32Jar("map transfer key").Weaken,
                New ByteJar("receiving player index").Weaken,
                New UInt32Jar("starting file pos").Weaken)
        Public Shared ReadOnly SetDownloadSource As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.SetDownloadSource,
                New UInt32Jar("map transfer key").Weaken,
                New ByteJar("sending player index").Weaken)
        Public Const MaxFileDataSize As UInt32 = 1442
        Public Shared ReadOnly MapFileData As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.MapFileData,
                New ByteJar("receiving player index").Weaken,
                New ByteJar("sending player index").Weaken,
                New UInt32Jar("map transfer key").Weaken,
                New UInt32Jar("file position").Weaken,
                New UInt32Jar("crc32", showhex:=True).Weaken,
                New RemainingDataJar("file data").Weaken)
        Public Shared ReadOnly MapFileDataReceived As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.MapFileDataReceived,
                New ByteJar("sender index").Weaken,
                New ByteJar("receiver index").Weaken,
                New UInt32Jar("map transfer key").Weaken,
                New UInt32Jar("total downloaded").Weaken)
        Public Shared ReadOnly MapFileDataProblem As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.MapFileDataProblem,
                New ByteJar("sender index").Weaken,
                New ByteJar("receiver index").Weaken,
                New UInt32Jar("map transfer key").Weaken)

        Public Shared ReadOnly TournamentCountdown As Definition(Of Dictionary(Of InvariantString, Object)) = Define(PacketId.TournamentCountdown,
                New UInt32Jar("unknown").Weaken,
                New UInt32Jar("time left").Weaken)
        Public Shared ReadOnly GameEnd As Definition(Of Object) = Define(PacketId.GameEnd)
        Public Shared ReadOnly EncryptedServerMeleeData As Definition(Of IReadableList(Of Byte)) = Define(PacketId.EncryptedServerMeleeData,
                New RemainingDataJar("encrypted data"))
        Public Shared ReadOnly EncryptedClientMeleeData As Definition(Of IReadableList(Of Byte)) = Define(PacketId.EncryptedClientMeleeData,
                New RemainingDataJar("encrypted data"))
    End Class
End Namespace
