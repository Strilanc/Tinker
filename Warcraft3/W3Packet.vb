Namespace WC3
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
        '_Unseen1B = &H1B
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
        LanDescribeGame = &H30
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
    End Enum

    Public NotInheritable Class Packet
        Public Const PacketPrefixValue As Byte = &HF7
        Public ReadOnly id As PacketId
        Private ReadOnly _payload As IPickle(Of Object)
        Private Shared ReadOnly packetJar As ManualSwitchJar = MakeW3PacketJar()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property

#Region "New"
        Private Sub New(ByVal id As PacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload
            Me.id = id
        End Sub
        Private Sub New(ByVal id As PacketId, ByVal value As Object)
            Me.New(id, packetJar.Pack(id, value))
            Contract.Requires(value IsNot Nothing)
        End Sub
#End Region

#Region "Enums"
        Public Shared ReadOnly MaxChatTextLength As Integer = 220
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
        Public Enum NonGameAction As Byte
            LobbyChat = &H10
            SetTeam = &H11
            SetColor = &H12
            SetRace = &H13
            SetHandicap = &H14
            GameChat = &H20
        End Enum
        Public Enum ChatType As Byte
            Lobby = &H10
            Game = &H20
        End Enum
        ''' <remarks>
        ''' It appears that anything larger than 2 is considered 'Private', but wc3 does send different codes for each player.
        ''' </remarks>
        Public Enum ChatReceiverType As Byte
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
#End Region

        Private Shared Sub reg(ByVal jar As ManualSwitchJar, ByVal id As PacketId, ByVal ParamArray subJars() As IJar(Of Object))
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(subJars IsNot Nothing)
            jar.AddPackerParser(id, New DefParser(id, subJars).Weaken)
        End Sub
        Private Shared Sub reg(ByVal jar As ManualSwitchJar, ByVal subjar As DefParser)
            Contract.Requires(jar IsNot Nothing)
            Contract.Requires(subjar IsNot Nothing)
            jar.AddPackerParser(subjar.id, subjar.Weaken)
        End Sub
        Public Class DefParser
            Inherits TupleJar
            Public ReadOnly id As PacketId
            Public Sub New(ByVal id As PacketId, ByVal ParamArray subjars() As IJar(Of Object))
                MyBase.New(id.ToString, subjars)
                Contract.Requires(subjars IsNot Nothing)
                Me.id = id
            End Sub
        End Class

        Public Class Jars
#Region "Misc"
            Public Shared ReadOnly Ping As New DefParser(PacketId.Ping,
                    New UInt32Jar("salt").Weaken)
            Public Shared ReadOnly Pong As New DefParser(PacketId.Pong,
                    New UInt32Jar("salt").Weaken)
#End Region

#Region "Player Exit"
            Public Shared ReadOnly Leaving As New DefParser(PacketId.Leaving,
                    New EnumUInt32Jar(Of PlayerLeaveType)("leave type").Weaken)
            Public Shared ReadOnly OtherPlayerLeft As New DefParser(PacketId.OtherPlayerLeft,
                    New ByteJar("player index").Weaken,
                    New EnumUInt32Jar(Of PlayerLeaveType)("leave type").Weaken)
#End Region

#Region "Player Entry"
            Public Shared ReadOnly Knock As New DefParser(PacketId.Knock,
                    New UInt32Jar("game id").Weaken,
                    New UInt32Jar("entry key").Weaken,
                    New ByteJar("unknown value").Weaken,
                    New UInt16Jar("listen port").Weaken,
                    New UInt32Jar("peer key").Weaken,
                    New StringJar("name", maximumContentSize:=15).Weaken,
                    New ArrayJar("unknown data", sizePrefixSize:=1).Weaken,
                    New NetIPEndPointJar("internal address").Weaken)
            Public Shared ReadOnly Greet As New DefParser(PacketId.Greet,
                    New ArrayJar("slot data", sizePrefixSize:=2).Weaken,
                    New ByteJar("player index").Weaken,
                    New NetIPEndPointJar("external address").Weaken)
            Public Shared ReadOnly HostMapInfo As New DefParser(PacketId.HostMapInfo,
                    New UInt32Jar("unknown").Weaken,
                    New StringJar("path").Weaken,
                    New UInt32Jar("size").Weaken,
                    New UInt32Jar("crc32").Weaken,
                    New UInt32Jar("xoro checksum").Weaken,
                    New ArrayJar("sha1 checksum", 20).Weaken)
            Public Shared ReadOnly RejectEntry As New DefParser(PacketId.RejectEntry,
                    New EnumUInt32Jar(Of RejectReason)("reason").Weaken)
            Public Shared ReadOnly OtherPlayerJoined As New DefParser(PacketId.OtherPlayerJoined,
                    New UInt32Jar("peer key").Weaken,
                    New ByteJar("index").Weaken,
                    New StringJar("name", maximumContentSize:=15).Weaken,
                    New ArrayJar("unknown data", sizePrefixSize:=1).Weaken,
                    New NetIPEndPointJar("external address").Weaken,
                    New NetIPEndPointJar("internal address").Weaken)
#End Region

#Region "Lobby"
            Public Shared ReadOnly OtherPlayerReady As New DefParser(PacketId.OtherPlayerReady,
                    New ByteJar("player index").Weaken)
            Public Shared ReadOnly StartLoading As New DefParser(PacketId.StartLoading)
            Public Shared ReadOnly StartCountdown As New DefParser(PacketId.StartCountdown)
            Public Shared ReadOnly Ready As New DefParser(PacketId.Ready)
            Public Shared ReadOnly LobbyState As New DefParser(PacketId.LobbyState,
                    New UInt16Jar("state size").Weaken,
                    New ListJar(Of Dictionary(Of InvariantString, Object))("slots", New SlotJar("slot")).Weaken,
                    New UInt32Jar("time").Weaken,
                    New ByteJar("layout style").Weaken,
                    New ByteJar("num player slots").Weaken)
            Public Shared ReadOnly PeerConnectionInfo As New DefParser(PacketId.PeerConnectionInfo,
                    New UInt16Jar("player bitflags").Weaken)
            Public Shared ReadOnly NonGameAction As IJar(Of Dictionary(Of InvariantString, Object)) = MakeNonGameActionJar()
#End Region

#Region "Gameplay"
            Public Shared ReadOnly ShowLagScreen As New DefParser(PacketId.ShowLagScreen,
                    New ListJar(Of Dictionary(Of InvariantString, Object))("laggers",
                        New TupleJar("lagger",
                            New ByteJar("player index").Weaken,
                            New UInt32Jar("initial milliseconds used").Weaken)).Weaken)
            Public Shared ReadOnly RemovePlayerFromLagScreen As New DefParser(PacketId.RemovePlayerFromLagScreen,
                    New ByteJar("player index").Weaken,
                    New UInt32Jar("marginal milliseconds used").Weaken)
            Public Shared ReadOnly RequestDropLaggers As New DefParser(PacketId.RequestDropLaggers)
            Public Shared ReadOnly Tick As New DefParser(PacketId.Tick,
                    New UInt16Jar("time span").Weaken,
                    New ArrayJar("subpacket", takerest:=True).Weaken)
            Public Shared ReadOnly Tock As New DefParser(PacketId.Tock,
                    New ArrayJar("game state checksum", 5).Weaken)
            Public Shared ReadOnly GameAction As New DefParser(PacketId.GameAction,
                    New ArrayJar("crc32", expectedSize:=4).Weaken,
                    New RepeatingJar(Of GameAction)("actions", New W3GameActionJar("action")).Weaken)
            Public Shared ReadOnly NewHost As New DefParser(PacketId.NewHost,
                    New ByteJar("player index").Weaken)
            Public Shared ReadOnly ClientConfirmHostLeaving As New DefParser(PacketId.ClientConfirmHostLeaving)
            Public Shared ReadOnly HostConfirmHostLeaving As New DefParser(PacketId.HostConfirmHostLeaving)
#End Region

#Region "Lan"
            Public Shared ReadOnly LanRequestGame As New DefParser(PacketId.LanRequestGame,
                    New StringJar("product id", nullTerminated:=False, reversed:=True, expectedsize:=4).Weaken,
                    New UInt32Jar("major version").Weaken,
                    New UInt32Jar("unknown1").Weaken)
            Public Shared ReadOnly LanRefreshGame As New DefParser(PacketId.LanRefreshGame,
                    New UInt32Jar("game id").Weaken,
                    New UInt32Jar("num players").Weaken,
                    New UInt32Jar("free slots").Weaken)
            Public Shared ReadOnly LanCreateGame As New DefParser(PacketId.LanCreateGame,
                    New StringJar("product id", False, True, 4).Weaken,
                    New UInt32Jar("major version").Weaken,
                    New UInt32Jar("game id").Weaken)
            Public Shared ReadOnly LanDestroyGame As New DefParser(PacketId.LanDestroyGame,
                    New UInt32Jar("game id").Weaken)
            Public Shared ReadOnly LanDescribeGame As New DefParser(PacketId.LanDescribeGame,
                    New StringJar("product id", False, True, 4).Weaken,
                    New UInt32Jar("major version").Weaken,
                    New UInt32Jar("game id").Weaken,
                    New UInt32Jar("entry key").Weaken,
                    New StringJar("name", True).Weaken,
                    New StringJar("password", info:="unused").Weaken,
                    New GameStatsJar("statstring").Weaken,
                    New UInt32Jar("num slots").Weaken,
                    New EnumUInt32Jar(Of GameTypes)("game type").Weaken,
                    New UInt32Jar("num players + 1").Weaken,
                    New UInt32Jar("free slots + 1").Weaken,
                    New UInt32Jar("age").Weaken,
                    New UInt16Jar("listen port").Weaken)
#End Region

#Region "Peer"
            Public Shared ReadOnly PeerKnock As New DefParser(PacketId.PeerKnock,
                    New UInt32Jar("receiver peer key").Weaken,
                    New UInt32Jar("unknown1").Weaken,
                    New ByteJar("sender player id").Weaken,
                    New ByteJar("unknown3").Weaken,
                    New UInt32Jar("sender peer connection flags").Weaken)
            Public Shared ReadOnly PeerPing As New DefParser(PacketId.PeerPing,
                    New ArrayJar("salt", 4).Weaken,
                    New UInt32Jar("sender peer connection flags").Weaken,
                    New UInt32Jar("unknown2").Weaken)
            Public Shared ReadOnly PeerPong As New DefParser(PacketId.PeerPong,
                    New ArrayJar("salt", 4).Weaken)
#End Region

#Region "Download"
            Public Shared ReadOnly ClientMapInfo As New DefParser(PacketId.ClientMapInfo,
                    New UInt32Jar("unknown").Weaken,
                    New EnumByteJar(Of DownloadState)("dl state").Weaken,
                    New UInt32Jar("total downloaded").Weaken)
            Public Shared ReadOnly SetUploadTarget As New DefParser(PacketId.SetUploadTarget,
                    New UInt32Jar("unknown1").Weaken,
                    New ByteJar("receiving player index").Weaken,
                    New UInt32Jar("starting file pos").Weaken)
            Public Shared ReadOnly SetDownloadSource As New DefParser(PacketId.SetDownloadSource,
                    New UInt32Jar("unknown").Weaken,
                    New ByteJar("sending player index").Weaken)
            Public Shared ReadOnly MapFileData As New DefParser(PacketId.MapFileData,
                    New ByteJar("receiving player index").Weaken,
                    New ByteJar("sending player index").Weaken,
                    New UInt32Jar("unknown").Weaken,
                    New UInt32Jar("file position").Weaken,
                    New ArrayJar("crc32", 4).Weaken,
                    New ArrayJar("file data", takerest:=True).Weaken)
            Public Shared ReadOnly MapFileDataReceived As New DefParser(PacketId.MapFileDataReceived,
                    New ByteJar("sender index").Weaken,
                    New ByteJar("receiver index").Weaken,
                    New UInt32Jar("unknown").Weaken,
                    New UInt32Jar("total downloaded").Weaken)
            Public Shared ReadOnly MapFileDataProblem As New DefParser(PacketId.MapFileDataProblem,
                    New ByteJar("sender index").Weaken,
                    New ByteJar("receiver index").Weaken,
                    New UInt32Jar("unknown").Weaken)
#End Region

#Region "Factory Methods"
            Private Shared Function MakeNonGameActionJar() As IJar(Of Dictionary(Of InvariantString, Object))
                Dim commandJar = New InteriorSwitchJar(Of Dictionary(Of InvariantString, Object))(
                        PacketId.NonGameAction.ToString,
                        Function(val) CByte(val("command type")),
                        Function(data) data(data(0) + 2))
                commandJar.AddPackerParser(Packet.NonGameAction.GameChat, New TupleJar(PacketId.NonGameAction.ToString,
                        New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                        New ByteJar("sending player").Weaken,
                        New EnumByteJar(Of NonGameAction)("command type").Weaken,
                        New EnumUInt32Jar(Of ChatReceiverType)("receiver type").Weaken,
                        New StringJar("message").Weaken))
                commandJar.AddPackerParser(Packet.NonGameAction.LobbyChat, New TupleJar(PacketId.NonGameAction.ToString,
                        New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                        New ByteJar("sending player").Weaken,
                        New EnumByteJar(Of NonGameAction)("command type").Weaken,
                        New StringJar("message").Weaken))
                commandJar.AddPackerParser(Packet.NonGameAction.SetTeam, New TupleJar(PacketId.NonGameAction.ToString,
                        New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                        New ByteJar("sending player").Weaken,
                        New EnumByteJar(Of NonGameAction)("command type").Weaken,
                        New ByteJar("new value").Weaken))
                commandJar.AddPackerParser(Packet.NonGameAction.SetHandicap, New TupleJar(PacketId.NonGameAction.ToString,
                        New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                        New ByteJar("sending player").Weaken,
                        New EnumByteJar(Of NonGameAction)("command type").Weaken,
                        New ByteJar("new value").Weaken))
                commandJar.AddPackerParser(Packet.NonGameAction.SetRace, New TupleJar(PacketId.NonGameAction.ToString,
                        New ArrayJar("receiving player indexes", , 1).Weaken,
                        New ByteJar("sending player").Weaken,
                        New EnumByteJar(Of NonGameAction)("command type").Weaken,
                        New EnumByteJar(Of Slot.Races)("new value").Weaken))
                commandJar.AddPackerParser(Packet.NonGameAction.SetColor, New TupleJar(PacketId.NonGameAction.ToString,
                        New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                        New ByteJar("sending player").Weaken,
                        New EnumByteJar(Of NonGameAction)("command type").Weaken,
                        New EnumByteJar(Of Slot.PlayerColor)("new value").Weaken))
                Return commandJar
            End Function
#End Region
        End Class

#Region "Definition"

        Private Shared Function MakeW3PacketJar() As ManualSwitchJar
            Dim jar = New ManualSwitchJar

            'Misc
            reg(jar, Jars.Ping)
            reg(jar, Jars.Pong)

            'Chat
            Dim chatJar = New InteriorSwitchJar(Of Dictionary(Of InvariantString, Object))(
                        PacketId.Text.ToString,
                        Function(val) CByte(val("type")),
                        Function(data) data(data(0) + 2))
            chatJar.AddPackerParser(ChatType.Game, New TupleJar(PacketId.Text.ToString,
                    New ListJar(Of Byte)("receiving player indexes", New ByteJar("player index")).Weaken,
                    New ByteJar("sending player index").Weaken,
                    New EnumByteJar(Of ChatType)("type").Weaken,
                    New EnumUInt32Jar(Of ChatReceiverType)("receiver type").Weaken,
                    New StringJar("message").Weaken))
            chatJar.AddPackerParser(ChatType.Lobby, New TupleJar(PacketId.Text.ToString,
                    New ListJar(Of Byte)("receiving player indexes", New ByteJar("player index")).Weaken,
                    New ByteJar("sending player index").Weaken,
                    New EnumByteJar(Of ChatType)("type").Weaken,
                    New StringJar("message").Weaken))
            jar.AddPackerParser(PacketId.Text, chatJar.Weaken)

            'NonGameAction commands
            Dim commandJar = New InteriorSwitchJar(Of Dictionary(Of InvariantString, Object))(
                        PacketId.NonGameAction.ToString,
                        Function(val) CByte(val("command type")),
                        Function(data) data(data(0) + 2))
            commandJar.AddPackerParser(NonGameAction.GameChat, New TupleJar(PacketId.NonGameAction.ToString,
                    New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New EnumUInt32Jar(Of ChatReceiverType)("receiver type").Weaken,
                    New StringJar("message").Weaken))
            commandJar.AddPackerParser(NonGameAction.LobbyChat, New TupleJar(PacketId.NonGameAction.ToString,
                    New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New StringJar("message").Weaken))
            commandJar.AddPackerParser(NonGameAction.SetTeam, New TupleJar(PacketId.NonGameAction.ToString,
                    New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New ByteJar("new value").Weaken))
            commandJar.AddPackerParser(NonGameAction.SetHandicap, New TupleJar(PacketId.NonGameAction.ToString,
                    New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New ByteJar("new value").Weaken))
            commandJar.AddPackerParser(NonGameAction.SetRace, New TupleJar(PacketId.NonGameAction.ToString,
                    New ArrayJar("receiving player indexes", , 1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New EnumByteJar(Of Slot.Races)("new value").Weaken))
            commandJar.AddPackerParser(NonGameAction.SetColor, New TupleJar(PacketId.NonGameAction.ToString,
                    New ArrayJar("receiving player indexes", sizePrefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New EnumByteJar(Of Slot.PlayerColor)("new value").Weaken))
            jar.AddPackerParser(PacketId.NonGameAction, commandJar.Weaken)

            'Player Exit
            reg(jar, Jars.Leaving)
            reg(jar, Jars.OtherPlayerLeft)

            'Player Entry
            reg(jar, Jars.Knock)
            reg(jar, Jars.Greet)
            reg(jar, Jars.HostMapInfo)
            reg(jar, Jars.RejectEntry)
            reg(jar, Jars.OtherPlayerJoined)

            'Lobby
            reg(jar, Jars.OtherPlayerReady)
            reg(jar, Jars.StartLoading)
            reg(jar, Jars.StartCountdown)
            reg(jar, Jars.Ready)
            reg(jar, Jars.LobbyState)
            reg(jar, Jars.PeerConnectionInfo)

            'Gameplay
            reg(jar, Jars.ShowLagScreen)
            reg(jar, Jars.RemovePlayerFromLagScreen)
            reg(jar, Jars.RequestDropLaggers)
            reg(jar, Jars.Tick)
            reg(jar, Jars.Tock)
            reg(jar, Jars.GameAction)

            'Lan
            reg(jar, Jars.LanRequestGame)
            reg(jar, Jars.LanRefreshGame)
            reg(jar, Jars.LanCreateGame)
            reg(jar, Jars.LanDestroyGame)
            reg(jar, Jars.LanDescribeGame)

            'Peer
            reg(jar, Jars.PeerKnock)
            reg(jar, Jars.PeerPing)
            reg(jar, Jars.PeerPong)

            'Map Download
            reg(jar, Jars.ClientMapInfo)
            reg(jar, Jars.SetUploadTarget)
            reg(jar, Jars.SetDownloadSource)
            reg(jar, Jars.MapFileData)
            reg(jar, Jars.MapFileDataReceived)
            reg(jar, Jars.MapFileDataProblem)

            Return jar
        End Function
#End Region

#Region "Parsing"
        Public Shared Function FromData(ByVal id As PacketId, ByVal data As ViewableList(Of Byte)) As Packet
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(id, packetJar.Parse(id, data))
        End Function
#End Region

#Region "Packing: Misc Packets"
        <Pure()>
        Public Shared Function MakeShowLagScreen(ByVal laggers As IEnumerable(Of Player)) As Packet
            Contract.Requires(laggers IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.ShowLagScreen, New Dictionary(Of InvariantString, Object) From {
                    {"laggers", (From p In laggers
                                 Select New Dictionary(Of InvariantString, Object) From {
                                        {"player index", p.Index},
                                        {"initial milliseconds used", 2000}}).ToList()}})
        End Function
        <Pure()>
        Public Shared Function MakeRemovePlayerFromLagScreen(ByVal player As Player,
                                                             ByVal lagTimeInMilliseconds As UInteger) As Packet
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.RemovePlayerFromLagScreen, New Dictionary(Of InvariantString, Object) From {
                    {"player index", player.Index},
                    {"marginal milliseconds used", lagTimeInMilliseconds}})
        End Function
        <Pure()>
        Public Shared Function MakeText(ByVal text As String,
                                        ByVal chatType As ChatType,
                                        ByVal receiverType As ChatReceiverType,
                                        ByVal receivingPlayers As IEnumerable(Of Player),
                                        ByVal sender As Player) As Packet
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(receivingPlayers IsNot Nothing)
            Contract.Requires(sender IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Select Case chatType
                Case chatType.Game
                    Return New Packet(PacketId.Text, New Dictionary(Of InvariantString, Object) From {
                            {"receiving player indexes", (From p In receivingPlayers Select p.Index).ToList},
                            {"sending player index", sender.Index},
                            {"type", chatType},
                            {"message", text},
                            {"receiver type", receiverType}})
                Case chatType.Lobby
                    Return New Packet(PacketId.Text, New Dictionary(Of InvariantString, Object) From {
                            {"receiving player indexes", (From p In receivingPlayers Select p.Index).ToList},
                            {"sending player index", sender.Index},
                            {"type", chatType},
                            {"message", text}})
                Case Else
                    Throw chatType.MakeArgumentValueException("chatType")
            End Select
        End Function
        <Pure()>
        Public Shared Function MakeGreet(ByVal player As Player,
                                         ByVal assignedIndex As Byte) As Packet
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.Greet, New Dictionary(Of InvariantString, Object) From {
                    {"slot data", New Byte() {}},
                    {"player index", assignedIndex},
                    {"external address", player.GetRemoteEndPoint()}})
        End Function
        <Pure()>
        Public Shared Function MakeReject(ByVal reason As RejectReason) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.RejectEntry, New Dictionary(Of InvariantString, Object) From {
                    {"reason", reason}})
        End Function
        <Pure()>
        Public Shared Function MakeHostMapInfo(ByVal map As Map) As Packet
            Contract.Requires(map IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.HostMapInfo, New Dictionary(Of InvariantString, Object) From {
                    {"unknown", 1},
                    {"path", "Maps\" + map.RelativePath},
                    {"size", map.FileSize},
                    {"crc32", map.FileChecksumCRC32},
                    {"xoro checksum", map.MapChecksumXORO},
                    {"sha1 checksum", map.MapChecksumSHA1.ToArray}})
        End Function
        <Pure()>
        Public Shared Function MakeOtherPlayerJoined(ByVal stranger As Player,
                                                     Optional ByVal overrideIndex As Byte = 0) As Packet
            Contract.Requires(stranger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim address = New Net.IPEndPoint(stranger.GetRemoteEndPoint.Address, stranger.listenPort)
            Return New Packet(PacketId.OtherPlayerJoined, New Dictionary(Of InvariantString, Object) From {
                    {"peer key", stranger.peerKey},
                    {"index", If(overrideIndex <> 0, overrideIndex, stranger.Index)},
                    {"name", stranger.Name.ToString},
                    {"unknown data", New Byte() {0}},
                    {"external address", address},
                    {"internal address", address}})
        End Function
        <Pure()>
        Public Shared Function MakePing(ByVal salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.Ping, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt}})
        End Function

        <Pure()>
        Public Shared Function MakeOtherPlayerReady(ByVal player As Player) As Packet
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.OtherPlayerReady, New Dictionary(Of InvariantString, Object) From {
                    {"player index", player.Index}})
        End Function
        <Pure()>
        Public Shared Function MakeOtherPlayerLeft(ByVal player As Player,
                                                   ByVal leaveType As PlayerLeaveType) As Packet
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.OtherPlayerLeft, New Dictionary(Of InvariantString, Object) From {
                                {"player index", player.Index},
                                {"leave type", CByte(leaveType)}})
        End Function
        <Pure()>
        Public Shared Function MakeLobbyState(ByVal receiver As Player,
                                              ByVal map As Map,
                                              ByVal slots As List(Of Slot),
                                              ByVal time As ModInt32,
                                              Optional ByVal hideSlots As Boolean = False) As Packet
            Contract.Requires(receiver IsNot Nothing)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(slots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LobbyState, New Dictionary(Of InvariantString, Object) From {
                    {"state size", CUShort(slots.Count() * 9 + 7)},
                    {"slots", (From slot In slots Select SlotJar.PackSlot(slot, receiver)).ToList()},
                    {"time", CUInt(time)},
                    {"layout style", If(map.isMelee, 0, 3)},
                    {"num player slots", If(Not hideSlots, map.NumPlayerSlots, If(map.NumPlayerSlots = 12, 11, 12))}})
        End Function
        <Pure()>
        Public Shared Function MakeStartCountdown() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.StartCountdown, New Dictionary(Of InvariantString, Object))
        End Function
        <Pure()>
        Public Shared Function MakeStartLoading() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.StartLoading, New Dictionary(Of InvariantString, Object))
        End Function
        <Pure()>
        Public Shared Function MakeHostConfirmHostLeaving() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.HostConfirmHostLeaving, New Dictionary(Of InvariantString, Object))
        End Function
        <Pure()>
        Public Shared Function MakeTick(Optional ByVal delta As UShort = 250,
                                        Optional ByVal tickData() As Byte = Nothing) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            tickData = If(tickData, {})
            If tickData.Length > 0 Then
                tickData = Concat(tickData.CRC32.Bytes.SubArray(0, 2), tickData)
            End If

            Return New Packet(PacketId.Tick, New Dictionary(Of InvariantString, Object) From {
                    {"subpacket", tickData},
                    {"time span", delta}})
        End Function
#End Region

#Region "Packing: DL Packets"
        Public Shared Function MakeMapFileData(ByVal map As Map,
                                               ByVal receiverIndex As Byte,
                                               ByVal filePosition As Integer,
                                               ByRef refSizeDataSent As Integer,
                                               Optional ByVal senderIndex As Byte = 0) As Packet
            Contract.Requires(senderIndex >= 0)
            Contract.Requires(senderIndex <= 12)
            Contract.Requires(receiverIndex > 0)
            Contract.Requires(receiverIndex <= 12)
            Contract.Requires(filePosition >= 0)
            Contract.Requires(map IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim filedata = map.ReadChunk(filePosition)
            refSizeDataSent = 0
            If senderIndex = 0 Then senderIndex = If(receiverIndex = 1, CByte(2), CByte(1))

            refSizeDataSent = filedata.Length
            Return New Packet(PacketId.MapFileData, New Dictionary(Of InvariantString, Object) From {
                    {"receiving player index", receiverIndex},
                    {"sending player index", senderIndex},
                    {"unknown", 1},
                    {"file position", filePosition},
                    {"crc32", filedata.CRC32.Bytes},
                    {"file data", filedata}})
        End Function
        Public Shared Function MakeSetUploadTarget(ByVal receiverIndex As Byte,
                                                   ByVal filePosition As UInteger) As Packet
            Contract.Requires(receiverIndex > 0)
            Contract.Requires(receiverIndex <= 12)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.SetUploadTarget, New Dictionary(Of InvariantString, Object) From {
                    {"unknown1", 1},
                    {"receiving player index", receiverIndex},
                    {"starting file pos", filePosition}})
        End Function
        Public Shared Function MakeSetDownloadSource(ByVal senderIndex As Byte) As Packet
            Contract.Requires(senderIndex > 0)
            Contract.Requires(senderIndex <= 12)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.SetDownloadSource, New Dictionary(Of InvariantString, Object) From {
                    {"unknown", 1},
                    {"sending player index", senderIndex}})
        End Function
        Public Shared Function MakeClientMapInfo(ByVal state As DownloadState,
                                                 ByVal totalDownloaded As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.ClientMapInfo, New Dictionary(Of InvariantString, Object) From {
                    {"unknown", 1},
                    {"dl state", state},
                    {"total downloaded", totalDownloaded}})
        End Function
        Public Shared Function MakeMapFileDataReceived(ByVal senderIndex As Byte,
                                                       ByVal receiverIndex As Byte,
                                                       ByVal totalDownloaded As UInteger) As Packet
            Contract.Requires(senderIndex > 0)
            Contract.Requires(senderIndex <= 12)
            Contract.Requires(receiverIndex > 0)
            Contract.Requires(receiverIndex <= 12)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.MapFileDataReceived, New Dictionary(Of InvariantString, Object) From {
                    {"sender index", senderIndex},
                    {"receiver index", receiverIndex},
                    {"unknown", 1},
                    {"total downloaded", totalDownloaded}})
        End Function
#End Region

#Region "Packing: Lan Packets"
        Public Shared Function MakeLanCreateGame(ByVal wc3MajorVersion As UInteger,
                                                 ByVal gameId As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LanCreateGame, New Dictionary(Of InvariantString, Object) From {
                    {"product id", "W3XP"},
                    {"major version", wc3MajorVersion},
                    {"game id", gameId}})
        End Function
        Public Shared Function MakeLanRefreshGame(ByVal gameId As UInteger,
                                                  ByVal game As GameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LanRefreshGame, New Dictionary(Of InvariantString, Object) From {
                    {"game id", gameId},
                    {"num players", 0},
                    {"free slots", game.TotalSlotCount - game.UsedSlotCount}})
        End Function
        Public Shared Function MakeLanDescribeGame(ByVal majorVersion As UInteger,
                                                   ByVal game As LocalGameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LanDescribeGame, New Dictionary(Of InvariantString, Object) From {
                    {"product id", "W3XP"},
                    {"major version", majorVersion},
                    {"game id", game.GameId},
                    {"entry key", 2642024974UI},
                    {"name", game.Name.ToString},
                    {"password", ""},
                    {"statstring", game.GameStats},
                    {"num slots", game.TotalSlotCount()},
                    {"game type", game.GameType},
                    {"num players + 1", 1},
                    {"free slots + 1", game.TotalSlotCount + 1 - game.UsedSlotCount},
                    {"age", game.AgeMilliseconds},
                    {"listen port", game.Port}})
        End Function
        Public Shared Function MakeLanDestroyGame(ByVal gameId As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.LanDestroyGame, New Dictionary(Of InvariantString, Object) From {
                    {"game id", gameId}})
        End Function
#End Region

#Region "Packing: Client Packets"
        Public Shared Function MakeKnock(ByVal name As InvariantString,
                                         ByVal listenPort As UShort,
                                         ByVal sendingPort As UShort,
                                         Optional ByVal gameId As UInt32 = 0,
                                         Optional ByVal entryKey As UInt32 = 0,
                                         Optional ByVal peerKey As UInt32 = 0,
                                         Optional ByVal internalAddress As Net.IPAddress = Nothing) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            If internalAddress Is Nothing Then
                internalAddress = New Net.IPAddress(GetCachedIPAddressBytes(external:=True))
            End If
            Return New Packet(PacketId.Knock, New Dictionary(Of InvariantString, Object) From {
                    {"game id", gameId},
                    {"entry key", entryKey},
                    {"unknown value", 0},
                    {"listen port", listenPort},
                    {"peer key", peerKey},
                    {"name", name},
                    {"unknown data", New Byte() {0}},
                    {"internal address", New Net.IPEndPoint(internalAddress, sendingPort)}})
        End Function
        Public Shared Function MakeReady() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.Ready, New Dictionary(Of InvariantString, Object))
        End Function
        Public Shared Function MakePong(ByVal salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.Pong, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt}})
        End Function
        Public Shared Function MakeTock(Optional ByVal checksum As Byte() = Nothing) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            If checksum Is Nothing Then checksum = New Byte() {0, 0, 0, 0, 0}
            If checksum.Length <> 5 Then Throw New ArgumentException("Checksum length must be 5.")
            Return New Packet(PacketId.Tock, New Dictionary(Of InvariantString, Object) From {
                    {"game state checksum", checksum}})
        End Function
        Public Shared Function MakePeerConnectionInfo(ByVal indexes As IEnumerable(Of Byte)) As Packet
            Contract.Requires(indexes IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim bitFlags = From index In indexes Select CUShort(1) << (index - 1)
            Dim dword = bitFlags.ReduceUsing(Function(flag1, flag2) flag1 Or flag2)

            Return New Packet(PacketId.PeerConnectionInfo, New Dictionary(Of InvariantString, Object) From {
                    {"player bitflags", dword}})
        End Function
#End Region

#Region "Packing: Peer Packets"
        Public Shared Function MakePeerKnock(ByVal receiverPeerKey As UInteger,
                                             ByVal senderId As Byte,
                                             ByVal senderPeerConnectionFlags As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.PeerKnock, New Dictionary(Of InvariantString, Object) From {
                    {"receiver peer key", receiverPeerKey},
                    {"unknown1", 0},
                    {"sender player id", senderId},
                    {"unknown3", &HFF},
                    {"sender peer connection flags", senderPeerConnectionFlags}})
        End Function
        Public Shared Function MakePeerPing(ByVal salt As Byte(),
                                            ByVal senderFlags As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.PeerPing, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt},
                    {"sender peer connection flags", senderFlags},
                    {"unknown2", 0}})
        End Function
        Public Shared Function MakePeerPong(ByVal salt As Byte()) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(PacketId.PeerPong, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt}})
        End Function
#End Region
    End Class

    Public NotInheritable Class SlotJar
        Inherits TupleJar

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name,
                    New ByteJar("player index").Weaken,
                    New ByteJar("dl percent").Weaken,
                    New EnumByteJar(Of SlotContents.State)("slot state").Weaken,
                    New ByteJar("is computer").Weaken,
                    New ByteJar("team index").Weaken,
                    New EnumByteJar(Of Slot.PlayerColor)("color").Weaken,
                    New EnumByteJar(Of Slot.Races)("race").Weaken,
                    New EnumByteJar(Of Slot.ComputerLevel)("computer difficulty").Weaken,
                    New ByteJar("handicap").Weaken)
        End Sub

        Public Shared Function PackSlot(ByVal slot As Slot,
                                        ByVal receiver As Player) As Dictionary(Of InvariantString, Object)
            Return New Dictionary(Of InvariantString, Object) From {
                    {"team index", slot.Team},
                    {"color", If(slot.Team = slot.ObserverTeamIndex, slot.ObserverTeamIndex, slot.color)},
                    {"race", If(slot.game.Map.isMelee, slot.race Or slot.Races.Unlocked, slot.race)},
                    {"handicap", slot.handicap},
                    {"is computer", If(slot.Contents.ContentType = SlotContentType.Computer, 1, 0)},
                    {"computer difficulty", slot.Contents.DataComputerLevel},
                    {"slot state", slot.Contents.DataState(receiver)},
                    {"player index", slot.Contents.DataPlayerIndex(receiver)},
                    {"dl percent", slot.Contents.DataDownloadPercent(receiver)}}
        End Function
    End Class

    Public NotInheritable Class W3PacketHandler
        Inherits PacketHandler(Of PacketId)

        Public Sub New(ByVal logger As Logger)
            MyBase.New(logger)
        End Sub

        Public Overrides ReadOnly Property HeaderSize As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() = 4)
                Return 4
            End Get
        End Property
        Protected Overrides Function ExtractKey(ByVal header As Strilbrary.ViewableList(Of Byte)) As PacketId
            Contract.Assume(header.Length >= 4)
            Contract.Assert(header.Length >= 4)
            If header(0) <> Packet.PacketPrefixValue Then Throw New IO.InvalidDataException("Invalid packet header.")
            Return CType(header(1), PacketId)
        End Function
    End Class
End Namespace
