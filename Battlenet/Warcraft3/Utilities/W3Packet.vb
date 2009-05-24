Imports HostBot.Pickling
Imports HostBot.Pickling.Jars
Imports HostBot.Warcraft3.W3PacketId

Namespace Warcraft3
    '''<summary>Identifies a warcraft 3 packet type.</summary>
    '''<data>
    '''  0 BYTE GAME_PACKET_PREFIX
    '''  1 BYTE packet type
    '''  2 WORD size including header = n
    '''  3 BYTE[4:n] data
    '''</data>
    Public Enum W3PacketId As Byte
        _unseen_0 = &H0
        ''' <summary>
        ''' Sent periodically by server to clients as a keep-alive packet.
        ''' Clients should respond with an equivalent PONG.
        ''' Clients which do not receive a PING or TICK for ~60s will disconnect.
        ''' If the server does not receive PONG or GAME_TICK_GUEST from a client for ~60s, it will disconnect the client.
        ''' </summary>
        PING = &H1
        _unseen_2 = &H2
        _unseen_3 = &H3
        ''' <summary>
        ''' Sent by server in response to KNOCK to indicate the client has entered the game.
        ''' This packet has two forms: one includes the data from the SLOT_LAYOUT packet, and the other doesn't.
        ''' </summary>
        GREET = &H4
        '''<summary>Sent by server in response to KNOCK to indicate the client did not enter the game.</summary>
        REJECT_ENTRY = &H5
        '''<summary>Broadcast by server to other clients when a client enters the game.</summary>
        OTHER_PLAYER_JOINED = &H6
        '''<summary>Broadcast server to other clients when a client leaves the game.</summary>
        OTHER_PLAYER_LEFT = &H7
        ''' <summary>
        ''' Broadcast by server to all clients in response to a client sending READY.
        ''' Clients start playing as soon as they have received this packet for each client.
        ''' </summary>
        OTHER_PLAYER_READY = &H8
        '''<summary>Broadcast by server to all clients when the lobby state changes.</summary>
        SLOT_LAYOUT = &H9
        ''' <summary>
        ''' Broadcast by server to all clients to start the countdown.
        ''' Clients will disconnect if they receive this packet more than once.
        ''' START_COUNTDOWN can be sent without sending START_LOADING afterwards (wc3 will wait at 0 seconds indefinitely).
        ''' </summary>
        START_COUNTDOWN = &HA
        ''' <summary>
        ''' Broadcast by server to all clients to tell them to start loading the map.
        ''' Clients will disconnect if they receive this packet more than once.
        ''' START_LOADING does not require START_COUNTDOWN to have been sent.
        ''' </summary>
        START_LOADING = &HB
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
        TICK = &HC
        _unseen_D = &HD
        _unseen_E = &HE
        ''' <summary>
        ''' Relayed by server to clients not connected directly to the sender.
        ''' Different formats in game and in lobby.
        ''' Clients will only request relay to clients who should receive the message (eg. only allies for ally chat).
        ''' </summary>
        TEXT = &HF
        START_LAG = &H10
        END_LAG = &H11
        _unseen_12 = &H12
        _unseen_13 = &H13
        SET_HOST = &H14 'unsure
        _p2p_unknown_15 = &H15
        _p2p_unknown_16 = &H16
        CONFIRM_HOST = &H17 'unsure
        _unseen_18 = &H18
        _p2p_unknown_19 = &H19
        _unseen_1A = &H1A
        _unseen_1B = &H1B
        _unseen_1C = &H1C
        _unseen_1D = &H1D
        '''<summary>First thing sent by clients upon connection. Requests entry into the game.</summary>
        KNOCK = &H1E
        _unseen_1F = &H1F
        _unseen_20 = &H20
        '''<summary>Sent by clients before they intentionally disconnect.</summary>
        LEAVING = &H21
        _unseen_22 = &H22
        '''<summary>Sent by clients once they have finished loading the map and are ready to start playing.</summary>
        READY = &H23
        _unseen_24 = &H24
        _unseen_25 = &H25
        ''' <summary>
        ''' Sent by clients when they perform game actions such as orders, alliance changes, trigger events, etc.
        ''' The server includes this data in its next TICK packet, broadcast to all the clients.
        ''' Clients don't perform an action until it shows up in the TICK packet.
        ''' If the TICK packet's actions disagree with the client's actions, the client will disconnect.
        ''' </summary>
        GAME_ACTION = &H26
        ''' <summary>
        ''' Sent by clients in response to TICK.
        ''' Contains a checksum of the client's game state, which is used to detect desyncs.
        ''' The lag screen is shown if a client takes too long to send a response TOCK.
        ''' </summary>
        TOCK = &H27
        CLIENT_COMMAND = &H28
        CLIENT_DROP_LAGGER = &H29
        _unseen_2A = &H2A
        _p2p_unknown_2B = &H2B
        ACCEPT_HOST = &H2C 'unsure
        _unseen_2D = &H2D
        _unseen_2E = &H2E
        '''<summary>Response to LAN_REFRESH_GAME or LAN_CREATE_GAME when clients want to know game info.</summary>
        LAN_REQUEST_GAME = &H2F
        '''<summary>Response to LAN_REQUEST_GAME containing detailed game information.</summary>
        LAN_DESCRIBE_GAME = &H30
        '''<summary>Broadcast on lan when a game is created.</summary>
        LAN_CREATE_GAME = &H31
        ''' <summary>
        ''' Broadcast on lan periodically to inform new listening wc3 clients a game exists.
        ''' Contains only very basic information about the game [no map, name, etc].
        ''' </summary>
        LAN_REFRESH_GAME = &H32
        '''<summary>Broadcast on lan when a game is cancelled.</summary>
        LAN_DESTROY_GAME = &H33
        P2P_CHAT = &H34
        P2P_PING = &H35
        P2P_PONG = &H36
        P2P_KNOCK = &H37
        _p2p_unknown_38 = &H38
        _p2p_unknown_39 = &H39
        _unseen_3A = &H3A
        '''<summary>Sent by clients to the server to inform the server when the set of other clients they are interconnected with changes.</summary>
        PLAYERS_CONNECTED = &H3B
        _unseen_3C = &H3C
        ''' <summary>
        ''' Sent by the server to new clients after they have entered the game.
        ''' Contains information about the map they must have to play the game.
        ''' </summary>
        MAP_INFO = &H3D
        ''' <summary>
        ''' Sent by the server to tell a client to start uploading to another client.
        ''' DL_START must be sent to the other client for the transfer to work.
        ''' </summary>
        UL_START = &H3E
        ''' <summary>
        ''' Sent by the server to tell a client to start downloading the map from the server or from another client.
        ''' UL_START must be sent to the other client for the p2p transfer to work.
        ''' </summary>
        DL_START = &H3F
        _unseen_40 = &H40
        _unseen_41 = &H41
        '''<summary>Sent by clients to the server in response to MAP_INFO and when the client has received more of the map file.</summary>
        DL_STATE = &H42
        '''<summary>Sent to to downloaders during map transfer. Contains map file data.</summary>
        DL_MAP_CHUNK = &H43
        '''<summary>Positive response to DL_MAP_CHUNK.</summary>
        DL_RECEIVED_CHUNK = &H44
        ''' <summary>
        ''' Negative response to DL_MAP_CHUNK.
        ''' This can be caused by corrupted data or by sending DL_MAP_CHUNK before DL_START is sent.
        ''' Even though wc3 clients send this packet if data is sent before DL_START, they still accept and use the data.
        ''' </summary>
        DL_CHUNK_PROBLEM = &H45
        '''<summary>Sent by clients in response to PING.</summary>
        PONG = &H46
    End Enum

    Public Class W3Packet
#Region "Members"
        Public Const PACKET_PREFIX As Byte = &HF7
        Public ReadOnly id As W3PacketId
        Public ReadOnly payload As IPickle
        Private Shared ReadOnly packet_jar As ManualSwitchJar = MakeW3PacketJar()
#End Region

#Region "New"
        Private Sub New(ByVal id As W3PacketId, ByVal payload As IPickle)
            If Not (payload IsNot Nothing) Then Throw New ArgumentException()

            Me.payload = payload
            Me.id = id
        End Sub
        Private Sub New(ByVal id As W3PacketId, ByVal val As Object)
            Me.New(id, packet_jar.pack(id, val))
        End Sub
#End Region

#Region "Jar"
        Public Shared Function MakeW3PacketJar() As ManualSwitchJar
            Dim jar = New ManualSwitchJar
            reg_general(jar)
            reg_leave(jar)
            reg_new(jar)
            reg_lobby_to_play(jar)
            reg_lobby(jar)
            reg_play(jar)
            reg_lan(jar)
            reg_p2p(jar)
            reg_dl(jar)
            Return jar
        End Function
        Private Shared Sub reg(ByVal jar As ManualSwitchJar, ByVal id As W3PacketId, ByVal ParamArray subjars() As IJar)
            jar.reg(id, New TupleJar(id.ToString(), subjars))
        End Sub
        Private Shared Sub reg_general(ByVal jar As ManualSwitchJar)
            reg(jar, PING, _
                    New ArrayJar("salt", 4))

            reg(jar, PONG, _
                    New ArrayJar("salt", 4))

            '[server receive] [Informs the server when the set of clients a client is interconnected with changes]
            reg(jar, PLAYERS_CONNECTED, _
                    New ValueJar("player bitflags", 2))

            '[server send] [Tells clients to display a message]
            Dim text_value_jar As New AutoSwitchJar(TEXT.ToString(), -1, Nothing)
            text_value_jar.regPacker(&H10, New TupleJar("lobby text", _
                    New StringJar("text")))
            text_value_jar.regPacker(&H20, New TupleJar("game Text", _
                    New ArrayJar("flags", 4), _
                    New StringJar("text")))
            reg(jar, TEXT, _
                    New ListJar("receiving player indexes", New ValueJar("player index", 1)), _
                    New ValueJar("sending player index", 1), _
                    New ValueJar("chat type", 1), _
                    text_value_jar)

            '[server receive] [Tells the server a client wants to perform a slot action or talk]
            Dim command_value_jar As New AutoSwitchJar("command value", -1, Nothing)
            command_value_jar.regParser(&H10, New TupleJar("lobby chat", _
                    New StringJar("message")))
            command_value_jar.regParser(&H11, New TupleJar("set team", _
                    New ValueJar("new value", 1)))
            command_value_jar.regParser(&H12, New TupleJar("set color", _
                    New ValueJar("new value", 1)))
            command_value_jar.regParser(&H13, New TupleJar("set race", _
                    New ValueJar("new value", 1)))
            command_value_jar.regParser(&H14, New TupleJar("set handicap", _
                    New ValueJar("new value", 1)))
            command_value_jar.regParser(&H20, New TupleJar("game chat", _
                    New ArrayJar("flags", 4), _
                    New StringJar("message")))
            reg(jar, CLIENT_COMMAND, _
                    New ArrayJar("receiving player indexes", , 1), _
                    New ValueJar("sending player", 1), _
                    New ValueJar("command type", 1), _
                    command_value_jar)
        End Sub
        Private Shared Sub reg_leave(ByVal jar As ManualSwitchJar)
            'EXPERIMENTAL
            reg(jar, CONFIRM_HOST)
            reg(jar, SET_HOST, _
                    New ValueJar("player index", 1))
            reg(jar, ACCEPT_HOST)

            '[server receive] [Informs the server a client is leaving the game]
            reg(jar, LEAVING, _
                    New ValueJar("leave type", 4))

            '[server send; broadcast when a player leaves] [informs other players a player has left]
            reg(jar, OTHER_PLAYER_LEFT, _
                    New ValueJar("player index", 1), _
                    New ValueJar("leave type", 4, "1=disc, 7=lose, 8=melee lose, 9=win, 10=draw, 11=obs, 13=lobby"))
        End Sub
        Private Shared Sub reg_new(ByVal jar As ManualSwitchJar)
            reg(jar, KNOCK, _
                    New ValueJar("entry key", 1), _
                    New ValueJar("unknown1", 4, "=0?"), _
                    New ValueJar("unknown2", 4, "=0?"), _
                    New ValueJar("listen port", 2), _
                    New ValueJar("connection key", 4, "value other players must provide when interconnecting"), _
                    New StringJar("name", , , , "max 15 characters + terminator"), _
                    New ValueJar("unknown3", 2, "=1"), _
                    New AddressJar("internal address"))

            reg(jar, GREET, _
                    New ValueJar("slot layout included", 2, "=0; other mode not supported"), _
                    New ValueJar("player index", 1), _
                    New AddressJar("external address"))

            reg(jar, MAP_INFO, _
                    New ValueJar("unknown", 4, "=1"), _
                    New StringJar("path"), _
                    New ValueJar("size", 4), _
                    New ArrayJar("crc32", 4), _
                    New ArrayJar("xoro checksum", 4), _
                    New ArrayJar("sha1 checksum", 20))

            reg(jar, REJECT_ENTRY, _
                    New ValueJar("reason", 4, "9=full, 10=started, 27=password wrong, else=game not found"))
        End Sub
        Private Shared Sub reg_lobby_to_play(ByVal jar As ManualSwitchJar)
            '[server send; broadcast when a player becomes ready to play] [informs other players a player is ready] [players auto start when all others ready]
            reg(jar, OTHER_PLAYER_READY, _
                    New ValueJar("player index", 1))

            reg(jar, START_LOADING)
            reg(jar, START_COUNTDOWN)
            reg(jar, READY)
        End Sub
        Private Shared Sub reg_lobby(ByVal jar As ManualSwitchJar)
            reg(jar, OTHER_PLAYER_JOINED, _
                    New ValueJar("p2p key", 4), _
                    New ValueJar("index", 1), _
                    New StringJar("name", , , , "max 15 chars + terminator"), _
                    New ValueJar("unknown[0x01]", 2, "=1"), _
                    New AddressJar("external address"), _
                    New AddressJar("internal address"))

            reg(jar, SLOT_LAYOUT, _
                    New ValueJar("state size", 2), _
                    New ListJar("slots", New SlotJar("slot")), _
                    New ValueJar("time", 4), _
                    New ValueJar("layout style", 1), _
                    New ValueJar("num player slots", 1))
        End Sub
        Private Shared Sub reg_play(ByVal jar As ManualSwitchJar)
            reg(jar, START_LAG, _
                    New ListJar("laggers", _
                        New TupleJar("lagger", _
                            New ValueJar("player index", 1), _
                            New ValueJar("initial time used", 4, "in milliseconds"))))
            reg(jar, END_LAG, _
                    New ValueJar("player index", 1), _
                    New ValueJar("marginal time used", 4, "in milliseconds"))
            reg(jar, CLIENT_DROP_LAGGER)

            reg(jar, TICK, _
                    New ValueJar("time span", 2), _
                    New ArrayJar("subpacket", , , True))
            reg(jar, TOCK, _
                    New ArrayJar("game state checksum", 5))
            jar.reg(GAME_ACTION, _
                    New CRC32Jar(GAME_ACTION.ToString(), _
                        New TupleJar("subpacket", _
                            New ValueJar("id", 1), _
                            New ArrayJar("data", , , True))))
        End Sub
        Private Shared Sub reg_lan(ByVal jar As ManualSwitchJar)
            reg(jar, LAN_REQUEST_GAME, _
                    New StringJar("product id", False, True, 4), _
                    New ValueJar("major version", 4), _
                    New ValueJar("unknown1", 4, "=0"))

            reg(jar, LAN_REFRESH_GAME, _
                    New ValueJar("game id", 4, "=0"), _
                    New ValueJar("num players", 4), _
                    New ValueJar("free slots", 4))

            reg(jar, LAN_CREATE_GAME, _
                    New StringJar("product id", False, True, 4), _
                    New ValueJar("major version", 4), _
                    New ValueJar("game id", 4))

            reg(jar, LAN_DESTROY_GAME, _
                    New ValueJar("game id", 4))

            reg(jar, LAN_DESCRIBE_GAME, _
                    New StringJar("product id", False, True, 4), _
                    New ValueJar("major version", 4), _
                    New ValueJar("game id", 4), _
                    New ValueJar("unknown1", 4, "varies"), _
                    New StringJar("name", True), _
                    New StringJar("password", True, , , "unused"), _
                    Warcraft3.W3Map.makeStatStringParser(), _
                    New ValueJar("num slots", 4), _
                    New ArrayJar("game type", 4), _
                    New ValueJar("unknown2", 4, "=1"), _
                    New ValueJar("free slots + 1", 4), _
                    New ValueJar("age", 4), _
                    New ValueJar("listen port", 2))
        End Sub
        Private Shared Sub reg_p2p(ByVal jar As ManualSwitchJar)
            '[Peer introduction]
            reg(jar, P2P_KNOCK, _
                    New ValueJar("receiver p2p key", 4, "As received from host in OTHER_PLAYER_JOINED"), _
                    New ValueJar("unknown1", 4, "=0"), _
                    New ValueJar("sender player id", 1), _
                    New ValueJar("unknown3", 1, "=0xFF"), _
                    New ValueJar("sender p2p flags", 4, "connection bit flags"))

            '[Periodic update and keep-alive]
            reg(jar, P2P_PING, _
                    New ArrayJar("salt", 4), _
                    New ValueJar("sender p2p flags", 4, "connection bit flags"), _
                    New ValueJar("unknown2", 4, "=0"))

            '[Response to periodic keep-alive]
            reg(jar, P2P_PONG, _
                    New ArrayJar("salt", 4))
        End Sub
        Private Shared Sub reg_dl(ByVal jar As ManualSwitchJar)
            reg(jar, DL_STATE, _
                    New ValueJar("unknown", 4, "=1"), _
                    New ValueJar("dl state", 1, "1=no dl, 3=active dl"), _
                    New ValueJar("total downloaded", 4))

            reg(jar, UL_START, _
                    New ValueJar("unknown1", 4, "=1"), _
                    New ValueJar("receiving player index", 1), _
                    New ValueJar("starting file pos", 4, "=0"))

            reg(jar, DL_START, _
                    New ValueJar("unknown", 4, "=1"), _
                    New ValueJar("sending player index", 1))

            reg(jar, DL_MAP_CHUNK, _
                    New ValueJar("receiving player index", 1), _
                    New ValueJar("sending player index", 1), _
                    New ValueJar("unknown", 4, "=1"), _
                    New ValueJar("file position", 4), _
                    New ArrayJar("crc32", 4), _
                    New ArrayJar("file data", , , True))

            reg(jar, DL_RECEIVED_CHUNK, _
                    New ValueJar("sender index", 1), _
                    New ValueJar("receiver index", 1), _
                    New ValueJar("unknown", 4, "=1"), _
                    New ValueJar("total downloaded", 4))

            reg(jar, DL_CHUNK_PROBLEM, _
                    New ValueJar("sender index", 1), _
                    New ValueJar("receiver index", 1), _
                    New ValueJar("unknown", 4, "=1"))
        End Sub
#End Region

#Region "Parsing"
        Public Shared Function FromData(ByVal id As W3PacketId, ByVal data As ImmutableArrayView(Of Byte)) As W3Packet
            If Not (data IsNot Nothing) Then Throw New ArgumentException()
            Return New W3Packet(id, packet_jar.parse(id, data))
        End Function
#End Region

#Region "Enums"
        Public Enum DownloadState As Byte
            NotDownloading = 1
            Downloading = 3
        End Enum
        Public Enum RejectReason As UInteger
            GameNotFound = 0
            GameFull = 9
            GameAlreadyStarted = 10
            IncorrectPassword = 27
        End Enum
#End Region

#Region "Packing: Misc Packets"
        Public Shared Function MakePacket_START_LAG(ByVal laggers As IEnumerable(Of IW3Player)) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            Dim L As New List(Of Object)
            For Each p In laggers
                Dim d As New Dictionary(Of String, Object)
                d("player index") = p.index
                d("initial time used") = 2000
                L.Add(d)
            Next p
            vals("laggers") = L
            Return New W3Packet(START_LAG, vals)
        End Function
        Public Shared Function MakePacket_END_LAG(ByVal p As IW3Player, _
                                                  ByVal lag_time_milliseconds As UInteger) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("player index") = p.index
            vals("marginal time used") = lag_time_milliseconds
            Return New W3Packet(END_LAG, vals)
        End Function
        Public Shared Function MakePacket_TEXT(ByVal text As String, _
                                               ByVal flags() As Byte, _
                                               ByVal receivingPlayers As IEnumerable(Of IW3Player), _
                                               ByVal sender As IW3Player) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            Dim d As New Dictionary(Of String, Object)
            Dim L As New List(Of Object)
            For Each e In receivingPlayers
                L.Add(e.index)
            Next e
            vals("receiving player indexes") = L
            vals("sending player index") = sender.index
            Dim chat_type As Byte
            If flags IsNot Nothing Then
                chat_type = &H20
                d("flags") = flags
            Else
                chat_type = &H10
            End If
            d("text") = text
            vals(W3PacketId.TEXT.ToString()) = New Pair(Of Byte, Object)(chat_type, d)
            vals("chat type") = chat_type
            Return New W3Packet(W3PacketId.TEXT, vals)
        End Function
        Public Shared Function MakePacket_GREET(ByVal p As IW3Player, _
                                                ByVal newIndex As Byte, _
                                                ByVal map As W3Map, _
                                                ByVal slots As List(Of W3Slot)) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            'vals(SLOT_LAYOUT.ToString()) = packPacket_SLOT_LAYOUT_vals(p, map, slots)
            vals("slot layout included") = 0
            vals("player index") = newIndex
            vals("external address") = AddressJar.packIPv4Address(p.remote_ip_external, p.remote_port_external)
            Return New W3Packet(GREET, vals)
        End Function
        Public Shared Function MakePacket_REJECT(ByVal reason As RejectReason) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("reason") = reason
            Return New W3Packet(REJECT_ENTRY, vals)
        End Function
        Public Shared Function MakePacket_MAP_INFO(ByVal map As W3Map) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("unknown") = CUShort(1)
            vals("path") = "Maps\" + map.relative_path
            vals("size") = map.fileSize
            vals("crc32") = map.crc32
            vals("xoro checksum") = map.checksum_xoro
            vals("sha1 checksum") = map.checksum_sha1
            Return New W3Packet(MAP_INFO, vals)
        End Function
        Public Shared Function MakePacket_OTHER_PLAYER_JOINED(ByVal stranger As IW3Player, _
                                                              Optional ByVal overrideIndex As Byte = 0) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("p2p key") = stranger.p2p_key
            vals("index") = If(overrideIndex <> 0, overrideIndex, stranger.index)
            vals("name") = stranger.name
            vals("unknown[0x01]") = CUInt(1)
            vals("external address") = AddressJar.packIPv4Address(stranger.remote_ip_external, stranger.listen_port)
            vals("internal address") = AddressJar.packIPv4Address(stranger.remote_ip_external, stranger.listen_port)
            Return New W3Packet(OTHER_PLAYER_JOINED, vals)
        End Function
        Public Shared Function MakePacket_PING(ByVal nonce As UInteger) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("salt") = nonce.bytes()
            Return New W3Packet(PING, vals)
        End Function

        Public Shared Function MakePacket_OTHER_PLAYER_READY(ByVal p As IW3Player) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("player index") = p.index
            Return New W3Packet(OTHER_PLAYER_READY, vals)
        End Function
        Public Shared Function MakePacket_OTHER_PLAYER_LEFT(ByVal p As IW3Player, ByVal leave_type As W3PlayerLeaveTypes) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("player index") = p.index
            vals("leave type") = CByte(leave_type)
            Return New W3Packet(OTHER_PLAYER_LEFT, vals)
        End Function
        Public Shared Function MakePacket_SLOT_LAYOUT(ByVal receiver As IW3Player, _
                                               ByVal map As W3Map, _
                                               ByVal slots As List(Of W3Slot), _
                                               ByVal time As UInteger, _
                                               Optional ByVal hide_slots As Boolean = False) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("state size") = CUShort(slots.Count() * 9 + 7)
            vals("slots") = (From slot In slots Select SlotJar.packSlot(slot, receiver)).Cast(Of Object).ToList()
            vals("time") = time
            vals("layout style") = CByte(If(map.isMelee, 0, 3))
            vals("num player slots") = If(Not hide_slots, map.numPlayerSlots, If(map.numPlayerSlots = 12, 11, 12))
            Return New W3Packet(SLOT_LAYOUT, vals)
        End Function
        Public Shared Function MakePacket_SET_HOST(ByVal new_host As Byte) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("player index") = new_host
            Return New W3Packet(SET_HOST, vals)
        End Function
        Public Shared Function MakePacket_START_COUNTDOWN() As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            Return New W3Packet(START_COUNTDOWN, vals)
        End Function
        Public Shared Function MakePacket_START_LOADING() As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            Return New W3Packet(START_LOADING, vals)
        End Function
        Public Shared Function MakePacket_CONFIRM_HOST() As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            Return New W3Packet(CONFIRM_HOST, vals)
        End Function
        Public Shared Function MakePacket_TICK(Optional ByVal delta As UShort = 250, Optional ByVal subdata() As Byte = Nothing) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("time span") = delta
            If subdata IsNot Nothing AndAlso subdata.Length > 0 Then
                vals("subpacket") = concat(subArray(Bnet.Crypt.crc32(New IO.MemoryStream(subdata)).bytes(), 0, 2), subdata)
            Else
                vals("subpacket") = New Byte() {}
            End If

            Return New W3Packet(TICK, vals)
        End Function
#End Region

#Region "Packing: DL Packets"
        Public Shared Function MakePacket_DL_MAP_CHUNK(ByVal sender_index As Byte, _
                                                       ByVal map As W3Map, _
                                                       ByVal receiver_index As Byte, _
                                                       ByVal file_pos As Integer, _
                                                       ByRef data_size As Integer) As W3Packet
            Dim filedata = map.getChunk(file_pos)
            data_size = 0
            If filedata Is Nothing Then Return Nothing
            If sender_index = 0 Then sender_index = If(receiver_index = 1, CByte(2), CByte(1))

            Dim vals As New Dictionary(Of String, Object)
            vals("receiving player index") = receiver_index
            vals("sending player index") = sender_index
            vals("unknown") = CUInt(1)
            vals("file position") = file_pos
            vals("crc32") = Bnet.Crypt.crc32(New IO.MemoryStream(filedata)).bytes()
            vals("file data") = filedata
            data_size = filedata.Length
            Return New W3Packet(DL_MAP_CHUNK, vals)
        End Function
        Public Shared Function MakePacket_UL_START(ByVal receiver As Byte, ByVal file_pos As UInteger) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("unknown1") = CUInt(1)
            vals("receiving player index") = receiver
            vals("starting file pos") = file_pos
            Return New W3Packet(UL_START, vals)
        End Function
        Public Shared Function MakePacket_DL_START(ByVal sender As Byte) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("unknown") = CUInt(1)
            vals("sending player index") = sender
            Return New W3Packet(DL_START, vals)
        End Function
        Public Shared Function MakePacket_DL_TOTAL_RECEIVED(ByVal state As DownloadState, ByVal total_downloaded As UInteger) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("unknown") = 1
            vals("dl state") = state
            vals("total downloaded") = total_downloaded
            Return New W3Packet(DL_STATE, vals)
        End Function
        Public Shared Function MakePacket_DL_RECEIVED_CHUNK(ByVal sender_index As Byte, ByVal receiver_index As Byte, ByVal total_downloaded As UInteger) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("sender index") = sender_index
            vals("receiver index") = receiver_index
            vals("unknown") = 1
            vals("total downloaded") = total_downloaded
            Return New W3Packet(DL_RECEIVED_CHUNK, vals)
        End Function
#End Region

#Region "Packing: Lan Packets"
        Public Shared Function MakePacket_LAN_CREATE_GAME(ByVal major_version As UInteger, ByVal game_id As UInteger) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("product id") = "W3XP"
            vals("major version") = major_version
            vals("game id") = game_id
            Return New W3Packet(LAN_CREATE_GAME, vals)
        End Function
        Public Shared Function MakePacket_LAN_REFRESH_GAME(ByVal game_id As UInteger, ByVal map As W3Map, ByVal map_settings As W3Map.MapSettings) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("game id") = game_id
            vals("num players") = 0
            vals("free slots") = map.numPlayerAndObsSlots(map_settings)
            Return New W3Packet(LAN_REFRESH_GAME, vals)
        End Function
        Public Shared Function MakePacket_LAN_DESCRIBE_GAME(ByVal listen_port As UShort, _
                                                     ByVal creation_time As Integer, _
                                                     ByVal game_name As String, _
                                                     ByVal user_name As String, _
                                                     ByVal major_version As UInteger, _
                                                     ByVal game_id As UInteger, _
                                                     ByVal map As W3Map, _
                                                     ByVal map_settings As W3Map.MapSettings, _
                                                     Optional ByVal game_type As UInteger = 1) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("product id") = "W3XP"
            vals("major version") = major_version
            vals("game id") = game_id
            vals("unknown1") = 1
            vals("name") = game_name
            vals("password") = ""
            vals("statstring") = map.generateStatStringVals(user_name, map_settings)
            vals("num slots") = map.numPlayerAndObsSlots(map_settings)
            vals("game type") = game_type.bytes()
            vals("unknown2") = 1
            vals("free slots + 1") = map.numPlayerAndObsSlots(map_settings) + 1
            vals("age") = TickCountDelta(Environment.TickCount, creation_time)
            vals("listen port") = listen_port
            Return New W3Packet(LAN_DESCRIBE_GAME, vals)
        End Function
        Public Shared Function MakePacket_LAN_DESTROY_GAME(ByVal game_id As UInteger) As W3Packet


            Dim vals As New Dictionary(Of String, Object)
            vals("game id") = game_id
            Return New W3Packet(LAN_DESTROY_GAME, vals)
        End Function
#End Region

#Region "Packing: Client Packets"
        Public Shared Function MakePacket_KNOCK(ByVal name As String, ByVal listen_port As UShort, ByVal sending_port As UShort) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("entry key") = 0
            vals("unknown1") = 0
            vals("unknown2") = 0
            vals("listen port") = listen_port
            vals("connection key") = 0
            vals("name") = name
            vals("unknown3") = 1
            vals("internal address") = AddressJar.packIPv4Address(GetExternalIp(), sending_port)
            Return New W3Packet(KNOCK, vals)
        End Function
        Public Shared Function MakePacket_READY() As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            Return New W3Packet(READY, vals)
        End Function
        Public Shared Function MakePacket_PONG(ByVal salt As Byte()) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("salt") = salt
            Return New W3Packet(PONG, vals)
        End Function
        Public Shared Function MakePacket_TOCK(Optional ByVal checksum As Byte() = Nothing) As W3Packet
            If checksum Is Nothing Then checksum = New Byte() {0, 0, 0, 0, 0}
            If checksum.Length <> 5 Then Throw New ArgumentException("Checksum length must be 5.")
            Dim vals As New Dictionary(Of String, Object)
            vals("game state checksum") = checksum
            Return New W3Packet(TOCK, vals)
        End Function
        Public Shared Function MakePacket_PLAYERS_CONNECTED(ByVal indexes As IEnumerable(Of Byte)) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            Dim val As UShort
            For Each index In indexes
                val = val Or (CUShort(1) << CInt(index))
            Next index
            vals("player bitflags") = val
            Return New W3Packet(PLAYERS_CONNECTED, vals)
        End Function
#End Region

#Region "Packing: P2P Packets"
        Public Shared Function MakePacket_P2P_KNOCK(ByVal receiver_key As UInteger, ByVal sender_id As Byte, ByVal sender_flags As UInteger) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("receiver p2p key") = receiver_key
            vals("unknown1") = 0
            vals("sender player id") = sender_id
            vals("unknown3") = &HFF
            vals("sender p2p flags") = sender_flags
            Return New W3Packet(P2P_KNOCK, vals)
        End Function
        Public Shared Function MakePacket_P2P_PING(ByVal salt As Byte(), ByVal sender_flags As UInteger) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("salt") = salt
            vals("sender p2p flags") = sender_flags
            vals("unknown2") = 0
            Return New W3Packet(P2P_PING, vals)
        End Function
        Public Shared Function MakePacket_P2P_PONG(ByVal salt As Byte()) As W3Packet
            Dim vals As New Dictionary(Of String, Object)
            vals("salt") = salt
            Return New W3Packet(P2P_PONG, vals)
        End Function
#End Region
    End Class

#Region "Jars"
    Public Class AddressJar
        Inherits TupleJar

        Public Sub New(ByVal name As String)
            MyBase.New(name, _
                    New ValueJar("protocol", 2), _
                    New ValueJar("port", 2, , False), _
                    New ArrayJar("ip", 4), _
                    New ArrayJar("unknown", 8))
        End Sub

        Public Shared Function packIPv4Address(ByVal ip As Byte(), ByVal port As UShort) As Dictionary(Of String, Object)
            Dim d As New Dictionary(Of String, Object)
            d("unknown") = New Byte() {0, 0, 0, 0, 0, 0, 0, 0}
            If ip Is Nothing Then
                d("protocol") = CUInt(0)
                d("ip") = New Byte() {0, 0, 0, 0}
                d("port") = CUShort(0)
            Else
                d("protocol") = CUInt(2)
                d("ip") = ip
                d("port") = port
            End If
            Return d
        End Function
    End Class

    Public Class CRC32Jar
        Inherits TupleJar
        Private ReadOnly subjar As IJar
        Private ReadOnly size As Integer

        Public Sub New(ByVal name As String, ByVal subjar As IJar, Optional ByVal size As Integer = 4)
            MyBase.New(name, New ArrayJar(subjar.getName() + ":crc32", size), subjar)
            Me.subjar = subjar
            Me.size = size
        End Sub

        Public Overrides Function pack(ByVal o As Object) As Pickling.IPickle
            Dim vals As New Dictionary(Of String, Object)
            vals(subjar.getName()) = o
            vals(subjar.getName() + ":crc32") = subArray(Bnet.Crypt.crc32(New IO.MemoryStream(subjar.pack(o).getData().ToArray)).bytes(), 0, size)
            Return MyBase.pack(vals)
        End Function

        Public Overrides Function parse(ByVal view As ImmutableArrayView(Of Byte)) As Pickling.IPickle
            Dim p = MyBase.parse(view)

            'check CRC
            Using m As New IO.MemoryStream(view.ToArray())
                Dim vals = CType(p.getVal(), Dictionary(Of String, Object))
                m.Seek(size, IO.SeekOrigin.Begin)
                Dim crc = CType(vals(subjar.getName() + ":crc32"), Byte())
                Dim crc2 = subArray(Bnet.Crypt.crc32(m, view.length - size).bytes(), 0, size)
                If Not ArraysEqual(crc, crc2) Then Throw New PicklingException("Incorrect CRC")
            End Using

            Return p
        End Function
    End Class

    Public Class SlotJar
        Inherits TupleJar

        Public Sub New(ByVal name As String)
            MyBase.New(name, _
                    New ValueJar("player index", 1), _
                    New ValueJar("dl percent", 1), _
                    New ValueJar("slot state", 1), _
                    New ValueJar("is computer", 1), _
                    New ValueJar("team index", 1), _
                    New ValueJar("color", 1), _
                    New ValueJar("race", 1), _
                    New ValueJar("computer difficulty", 1), _
                    New ValueJar("handicap", 1))
        End Sub

        Public Shared Function packSlot(ByVal s As W3Slot, ByVal receiver As IW3Player) As Dictionary(Of String, Object)
            Dim vals As New Dictionary(Of String, Object)
            vals("team index") = s.team
            vals("color") = If(s.team = W3Slot.OBS_TEAM, W3Slot.OBS_TEAM, s.color)
            vals("race") = If(s.game.map.isMelee, s.race Or W3Slot.RaceFlags.Unlocked, s.race)
            vals("computer difficulty") = W3Slot.ComputerLevel.Normal
            vals("handicap") = s.handicap
            vals("is computer") = If(s.contents.Type = W3SlotContents.ContentType.Computer, 1, 0)
            vals("computer difficulty") = s.contents.DataComputerLevel
            vals("slot state") = s.contents.DataState(receiver)
            vals("player index") = s.contents.DataPlayerIndex(receiver)
            vals("dl percent") = s.contents.DataDownloadPercent(receiver)
            Return vals
        End Function
    End Class
#End Region
End Namespace
