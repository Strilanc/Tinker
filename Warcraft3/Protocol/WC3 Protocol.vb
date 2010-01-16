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

    Public Class DefParser
        Inherits TupleJar
        Public ReadOnly id As PacketId
        Public Sub New(ByVal id As PacketId, ByVal ParamArray subjars() As IJar(Of Object))
            MyBase.New(id.ToString, subjars)
            Contract.Requires(subjars IsNot Nothing)
            Me.id = id
        End Sub
    End Class

    Public NotInheritable Class Jars
        Private Sub New()
        End Sub
        Public Const PacketPrefix As Byte = &HF7

        Public Shared ReadOnly Ping As New DefParser(PacketId.Ping,
                New UInt32Jar("salt").Weaken)
        Public Shared ReadOnly Pong As New DefParser(PacketId.Pong,
                New UInt32Jar("salt").Weaken)

        Public Shared ReadOnly Leaving As New DefParser(PacketId.Leaving,
                New EnumUInt32Jar(Of PlayerLeaveType)("leave type").Weaken)
        Public Shared ReadOnly OtherPlayerLeft As New DefParser(PacketId.OtherPlayerLeft,
                New ByteJar("player index").Weaken,
                New EnumUInt32Jar(Of PlayerLeaveType)("leave type").Weaken)

        Public Shared ReadOnly Knock As New DefParser(PacketId.Knock,
                New UInt32Jar("game id").Weaken,
                New UInt32Jar("entry key", showhex:=True).Weaken,
                New ByteJar("unknown value").Weaken,
                New UInt16Jar("listen port").Weaken,
                New UInt32Jar("peer key", showhex:=True).Weaken,
                New StringJar("name", maximumContentSize:=15).Weaken,
                New SizePrefixedDataJar("unknown data", prefixSize:=1).Weaken,
                New Bnet.Protocol.IPEndPointJar("internal address").Weaken)
        Public Shared ReadOnly Greet As New DefParser(PacketId.Greet,
                New SizePrefixedDataJar("slot data", prefixSize:=2).Weaken,
                New ByteJar("player index").Weaken,
                New Bnet.Protocol.IPEndPointJar("external address").Weaken)
        Public Shared ReadOnly HostMapInfo As New DefParser(PacketId.HostMapInfo,
                New UInt32Jar("unknown").Weaken,
                New StringJar("path").Weaken,
                New UInt32Jar("size").Weaken,
                New UInt32Jar("crc32", showhex:=True).Weaken,
                New UInt32Jar("xoro checksum", showhex:=True).Weaken,
                New RawDataJar("sha1 checksum", Size:=20).Weaken)
        Public Shared ReadOnly RejectEntry As New DefParser(PacketId.RejectEntry,
                New EnumUInt32Jar(Of RejectReason)("reason").Weaken)
        Public Shared ReadOnly OtherPlayerJoined As New DefParser(PacketId.OtherPlayerJoined,
                New UInt32Jar("peer key", showhex:=True).Weaken,
                New ByteJar("index").Weaken,
                New StringJar("name", maximumContentSize:=15).Weaken,
                New SizePrefixedDataJar("unknown data", prefixSize:=1).Weaken,
                New Bnet.Protocol.IPEndPointJar("external address").Weaken,
                New Bnet.Protocol.IPEndPointJar("internal address").Weaken)

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
                New UInt16Jar("player bitflags", showhex:=True).Weaken)

        Public Const MaxChatTextLength As Integer = 220
        Public Shared ReadOnly NonGameAction As IJar(Of Dictionary(Of InvariantString, Object)) = MakeNonGameActionJar()
        Private Shared Function MakeNonGameActionJar() As IJar(Of Dictionary(Of InvariantString, Object))
            Dim commandJar = New InteriorSwitchJar(Of NonGameAction, Dictionary(Of InvariantString, Object))(
                    PacketId.NonGameAction.ToString,
                    Function(val) CType(val("command type"), NonGameAction),
                    Function(data) CType(data(data(0) + 2), NonGameAction))
            commandJar.AddPackerParser(Protocol.NonGameAction.GameChat, New TupleJar(PacketId.NonGameAction.ToString,
                    New SizePrefixedDataJar("receiving player indexes", prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New EnumUInt32Jar(Of ChatReceiverType)("receiver type").Weaken,
                    New StringJar("message", maximumContentSize:=MaxChatTextLength).Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.LobbyChat, New TupleJar(PacketId.NonGameAction.ToString,
                    New SizePrefixedDataJar("receiving player indexes", prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New StringJar("message", maximumContentSize:=MaxChatTextLength).Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.SetTeam, New TupleJar(PacketId.NonGameAction.ToString,
                    New SizePrefixedDataJar("receiving player indexes", prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New ByteJar("new value").Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.SetHandicap, New TupleJar(PacketId.NonGameAction.ToString,
                    New SizePrefixedDataJar("receiving player indexes", prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New ByteJar("new value").Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.SetRace, New TupleJar(PacketId.NonGameAction.ToString,
                    New SizePrefixedDataJar("receiving player indexes", prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New EnumByteJar(Of Slot.Races)("new value").Weaken))
            commandJar.AddPackerParser(Protocol.NonGameAction.SetColor, New TupleJar(PacketId.NonGameAction.ToString,
                    New SizePrefixedDataJar("receiving player indexes", prefixSize:=1).Weaken,
                    New ByteJar("sending player").Weaken,
                    New EnumByteJar(Of NonGameAction)("command type").Weaken,
                    New EnumByteJar(Of Slot.PlayerColor)("new value").Weaken))
            Return commandJar
        End Function

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
                New RemainingDataJar("subpacket").Weaken)
        Public Shared ReadOnly Tock As New DefParser(PacketId.Tock,
                New RawDataJar("game state checksum", Size:=5).Weaken)
        Public Shared ReadOnly GameAction As New DefParser(PacketId.GameAction,
                New UInt32Jar("crc32").Weaken,
                New RepeatingJar(Of GameAction)("actions", New W3GameActionJar("action")).Weaken)
        Public Shared ReadOnly NewHost As New DefParser(PacketId.NewHost,
                New ByteJar("player index").Weaken)
        Public Shared ReadOnly ClientConfirmHostLeaving As New DefParser(PacketId.ClientConfirmHostLeaving)
        Public Shared ReadOnly HostConfirmHostLeaving As New DefParser(PacketId.HostConfirmHostLeaving)

        Public Shared ReadOnly LanRequestGame As New DefParser(PacketId.LanRequestGame,
                New Bnet.Protocol.DwordStringJar("product id").Weaken,
                New UInt32Jar("major version").Weaken,
                New UInt32Jar("unknown1").Weaken)
        Public Shared ReadOnly LanRefreshGame As New DefParser(PacketId.LanRefreshGame,
                New UInt32Jar("game id").Weaken,
                New UInt32Jar("num players").Weaken,
                New UInt32Jar("free slots").Weaken)
        Public Shared ReadOnly LanCreateGame As New DefParser(PacketId.LanCreateGame,
                New Bnet.Protocol.DwordStringJar("product id").Weaken,
                New UInt32Jar("major version").Weaken,
                New UInt32Jar("game id").Weaken)
        Public Shared ReadOnly LanDestroyGame As New DefParser(PacketId.LanDestroyGame,
                New UInt32Jar("game id").Weaken)
        Public Shared ReadOnly LanDescribeGame As New DefParser(PacketId.LanDescribeGame,
                New Bnet.Protocol.DwordStringJar("product id").Weaken,
                New UInt32Jar("major version").Weaken,
                New UInt32Jar("game id").Weaken,
                New UInt32Jar("entry key", showhex:=True).Weaken,
                New StringJar("name", True).Weaken,
                New StringJar("password").Weaken,
                New GameStatsJar("statstring").Weaken,
                New UInt32Jar("num slots").Weaken,
                New EnumUInt32Jar(Of GameTypes)("game type").Weaken,
                New UInt32Jar("num players + 1").Weaken,
                New UInt32Jar("free slots + 1").Weaken,
                New UInt32Jar("age").Weaken,
                New UInt16Jar("listen port").Weaken)

        Public Shared ReadOnly PeerKnock As New DefParser(PacketId.PeerKnock,
                New UInt32Jar("receiver peer key", showhex:=True).Weaken,
                New UInt32Jar("unknown1").Weaken,
                New ByteJar("sender player id").Weaken,
                New ByteJar("unknown3").Weaken,
                New UInt32Jar("sender peer connection flags").Weaken)
        Public Shared ReadOnly PeerPing As New DefParser(PacketId.PeerPing,
                New UInt32Jar("salt", showhex:=True).Weaken,
                New UInt32Jar("sender peer connection flags").Weaken,
                New UInt32Jar("unknown2").Weaken)
        Public Shared ReadOnly PeerPong As New DefParser(PacketId.PeerPong,
                New UInt32Jar("salt", showhex:=True).Weaken)

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
                New UInt32Jar("crc32", showhex:=True).Weaken,
                New RemainingDataJar("file data").Weaken)
        Public Shared ReadOnly MapFileDataReceived As New DefParser(PacketId.MapFileDataReceived,
                New ByteJar("sender index").Weaken,
                New ByteJar("receiver index").Weaken,
                New UInt32Jar("unknown").Weaken,
                New UInt32Jar("total downloaded").Weaken)
        Public Shared ReadOnly MapFileDataProblem As New DefParser(PacketId.MapFileDataProblem,
                New ByteJar("sender index").Weaken,
                New ByteJar("receiver index").Weaken,
                New UInt32Jar("unknown").Weaken)
    End Class
End Namespace
