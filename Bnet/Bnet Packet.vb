''HostBot - Warcraft 3 game hosting bot
''Copyright (C) 2008 Craig Gidney
''
''This program is free software: you can redistribute it and/or modify
''it under the terms of the GNU General Public License as published by
''the Free Software Foundation, either version 3 of the License, or
''(at your option) any later version.
''
''This program is distributed in the hope that it will be useful,
''but WITHOUT ANY WARRANTY; without even the implied warranty of
''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''GNU General Public License for more details.
''You should have received a copy of the GNU General Public License
''along with this program.  If not, see http://www.gnu.org/licenses/

Imports Tinker.Pickling

Namespace Bnet
    '''<summary>Header values for packets to/from BNET</summary>
    '''<source>BNETDocs.org</source>
    Public Enum PacketId As Byte
        Null = &H0
        '''<summary>Client requests server close the client's listed game.</summary>
        CloseGame3 = &H2
        ServerList = &H4
        ClientId = &H5
        StartVersioning = &H6
        ReportVersion = &H7
        StartAdvex = &H8
        QueryGamesList = &H9
        '''<summary>Request/response for entering chat.</summary>
        EnterChat = &HA
        GetChannelList = &HB
        '''<summary>Request/response for joining a channel ... duh</summary>
        JoinChannel = &HC
        ChatCommand = &HE
        ChatEvent = &HF
        LeaveChat = &H10
        LocaleInfo = &H12
        FloodDetected = &H13
        UdpPingResponse = &H14
        CheckAd = &H15
        ClickAd = &H16
        Registry = &H18
        MessageBox = &H19
        StartAdvex2 = &H1A
        GameDataAddress = &H1B
        '''<summary>Request/response for listing a game.</summary>
        CreateGame3 = &H1C
        LogOnChallengeEx = &H1D
        ClientId2 = &H1E
        LeaveGame = &H1F
        DisplayAd = &H21
        NotifyJoin = &H22
        Ping = &H25
        ReadUserData = &H26
        WriteUserData = &H27
        LogOnChallenge = &H28
        LogOnResponse = &H29
        CreateAccount = &H2A
        SystemInfo = &H2B
        GameResult = &H2C
        GetIconData = &H2D
        GetLadderData = &H2E
        FindLadderUser = &H2F
        CDKey = &H30
        ChangePassword = &H31
        CheckDataFile = &H32
        GetFileTime = &H33
        QueryRealms = &H34
        Profile = &H35
        CDKey2 = &H36
        LogOnResponse2 = &H3A
        CheckDataFile2 = &H3C
        CreateAccount2 = &H3D
        LogOnRealmEx = &H3E
        StartVersioning2 = &H3F
        QueryRealms2 = &H40
        QueryAdUrl = &H41
        WarcraftGeneral = &H44
        'client: ff 44 39 00; 06; 01 00 00 00; e8 21 00 0b; b0 0b e6 4a; 02 88 90 96; e8 45 0e dc; af a0 b3 05; 1b a5 43 d7; be c6 a7 70; 7c; 00 00 00 00; 01 00; ff 0f 00 00; 08; 6c 96 dc 19; 02 00 00 00

        '''<summary>Client tells server what port it will listen for other clients on when hosting games.</summary>
        NetGamePort = &H45
        NewsInfo = &H46
        OptionalWork = &H4A
        ExtraWork = &H4B
        RequiredWork = &H4C
        Tournament = &H4E
        '''<summary>Introductions, server authentication, and server challenge to client.</summary>
        ProgramAuthenticationBegin = &H50
        '''<summary>The client authenticates itself against the server's challenge</summary>
        ProgramAuthenticationFinish = &H51
        AccountCreate = &H52
        UserAuthenticationBegin = &H53
        '''<summary>Exchange of password proofs</summary>
        UserAuthenticationFinish = &H54
        AccountChange = &H55
        AccountChangeProof = &H56
        AccountUpgrade = &H57
        AccountUpgradeProof = &H58
        AccountSetEmail = &H59
        AccountResetPassword = &H5A
        AccountChangeEmail = &H5B
        SwitchProduct = &H5C
        Warden = &H5E

        ArrangedTeamPlayerList = &H60

        ArrangedTeamInvitePlayers = &H61
        ArrangedTeamInvitation = &H63

        FriendsList = &H65
        FriendsUpdate = &H66
        FriendsAdd = &H67
        FriendsRemove = &H68
        FriendsPosition = &H69
        ClanFindCandidates = &H70
        ClanInviteMultiple = &H71
        ClanCreationInvitation = &H72
        ClanDisband = &H73
        ClanMakeChieftain = &H74
        ClanInfo = &H75
        ClanQuitNotify = &H76
        ClanInvitation = &H77
        ClanRemoveMember = &H78
        ClanInvitationResponse = &H79
        ClanRankChange = &H7A
        ClanSetMessageOfTheDay = &H7B
        ClanMessageOfTheDay = &H7C
        ClanMemberList = &H7D
        ClanMemberRemoved = &H7E
        ClanMemberStatusChange = &H7F
        ClanMemberRankChange = &H81
        ClanMemberInformation = &H82
    End Enum

    Public NotInheritable Class Packet
        Public Const PacketPrefixValue As Byte = &HFF
        Private ReadOnly _payload As IPickle(Of Object)
        Private ReadOnly _id As PacketId
        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property
        Public ReadOnly Property Id As PacketId
            Get
                Return _id
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(ByVal packer As DefJar, ByVal vals As Dictionary(Of InvariantString, Object))
            Contract.Requires(packer IsNot Nothing)
            Contract.Requires(vals IsNot Nothing)
            Contract.Ensures(Me.Id = packer.id)
            Me._id = packer.id
            Me._payload = packer.Pack(vals)
        End Sub
        Private Sub New(ByVal id As PacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Contract.Ensures(Me.Id = id)
            Contract.Ensures(Me.Payload Is payload)
            Me._id = id
            Me._payload = payload
        End Sub

#Region "Enums"
        Public Enum ProgramAuthenticationBeginLogOnType As Byte
            Warcraft3 = 2
        End Enum
        Public Enum UserAuthenticationFinishResult As UInteger
            Passed = 0
            IncorrectPassword = 2
            NeedEmail = 14
            CustomError = 15
        End Enum
        Public Enum UserAuthenticationBeginResult As UInteger
            Passed = 0
            BadUserName = 1
            UpgradeAccount = 5
        End Enum
        Public Enum ProgramAuthenticationFinishResult As UInteger
            Passed = 0
            OldVersion = &H101
            InvalidVersion = &H102
            FutureVersion = &H103
            InvalidCDKey = &H200
            UsedCDKey = &H201
            BannedCDKey = &H202
            WrongProduct = &H203
        End Enum
        Public Enum ChatEventId
            ShowUser = &H1
            UserJoined = &H2
            UserLeft = &H3
            Whisper = &H4
            Talk = &H5
            Broadcast = &H6
            Channel = &H7
            UserFlags = &H9
            WhisperSent = &HA
            ChannelFull = &HD
            ChannelDoesNotExist = &HE
            ChannelRestricted = &HF
            Info = &H12
            Errors = &H13
            Emote = &H17
        End Enum
        <Flags()>
        Public Enum GameStates As UInteger
            [Private] = 1 << 0
            Full = 1 << 1
            NotEmpty = 1 << 2 'really unsure about this one
            InProgress = 1 << 3
            Unknown0x10 = 1 << 4
        End Enum
        Public Enum JoinChannelType As UInteger
            NoCreate = 0
            FirstJoin = 1
            ForcedJoin = 2
            Diablo2Join = 3
        End Enum
        Public Enum QueryGameResponse As UInteger
            OK = 0
            NotFound = 1
            IncorrectPassword = 2
            Full = 3
            AlreadyStarted = 4
            TooManyServerRequests = 6
        End Enum
        Public Enum ClanRank
            NewInitiate
            Initiate
            Member
            Officer
            Leader
        End Enum
#End Region

        Public Class DefJar
            Inherits TupleJar
            Public ReadOnly id As PacketId
            Public Sub New(ByVal id As PacketId, ByVal ParamArray subjars() As IJar(Of Object))
                MyBase.New(id.ToString, subjars)
                Contract.Requires(subjars IsNot Nothing)
                Me.id = id
            End Sub
        End Class
        Public Class QueryGamesListResponse
            Private ReadOnly _games As WC3.RemoteGameDescription()
            Private ReadOnly _result As QueryGameResponse

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_games IsNot Nothing)
            End Sub

            Public Sub New(ByVal result As QueryGameResponse, ByVal games As IEnumerable(Of WC3.RemoteGameDescription))
                Contract.Requires(games IsNot Nothing)
                Me._games = games.ToArray
                Me._result = result
            End Sub

            Public ReadOnly Property Games As IList(Of WC3.RemoteGameDescription)
                Get
                    Return _games
                End Get
            End Property
            Public ReadOnly Property Result As QueryGameResponse
                Get
                    Return _result
                End Get
            End Property
        End Class
        Public Class QueryGamesListResponseJar
            Inherits BaseParseJar(Of QueryGamesListResponse)

            Private Shared ReadOnly gameDataJar As New TupleJar("game",
                    New EnumUInt32Jar(Of WC3.GameTypes)("game type").Weaken,
                    New UInt32Jar("language id").Weaken,
                    New NetIPEndPointJar("host address").Weaken,
                    New EnumUInt32Jar(Of GameStates)("game state").Weaken,
                    New UInt32Jar("elapsed seconds").Weaken,
                    New StringJar("game name").Weaken,
                    New StringJar("game password").Weaken,
                    New TextHexValueJar("num free slots", numdigits:=1).Weaken,
                    New TextHexValueJar("game id", numdigits:=8).Weaken,
                    New WC3.GameStatsJar("game statstring").Weaken)

            Public Sub New()
                MyBase.new(PacketId.QueryGamesList.ToString)
            End Sub

            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of QueryGamesListResponse)
                Dim count = data.SubView(0, 4).ToUInt32
                Dim games = New List(Of WC3.RemoteGameDescription)(capacity:=CInt(count))
                Dim pickles = New List(Of IPickle(Of Object))(capacity:=CInt(count + 1))
                Dim result = QueryGameResponse.OK
                Dim offset = 4
                If count = 0 Then
                    'result of single-game query
                    result = CType(data.SubView(4, 4).ToUInt32, QueryGameResponse)
                    offset += 4
                    pickles.Add(New Pickle(Of Object)("Result", result, data.SubView(4, 4)))
                Else
                    'games matching query
                    For repeat = 1UI To count
                        Dim pickle = gameDataJar.Parse(data.SubView(offset))
                        pickles.Add(pickle)
                        offset += pickle.Data.Count
                        Dim vals = pickle.Value
                        games.Add(New WC3.RemoteGameDescription(Name:=CStr(vals("game name")).AssumeNotNull,
                                                                gamestats:=CType(vals("game statstring"), WC3.GameStats).AssumeNotNull,
                                                                location:=CType(vals("host address"), Net.IPEndPoint).AssumeNotNull,
                                                                gameid:=CUInt(vals("game id")),
                                                                entryKey:=0,
                                                                totalSlotCount:=CInt(vals("num free slots")),
                                                                gameType:=CType(vals("game type"), WC3.GameTypes),
                                                                state:=CType(vals("game state"), GameStates),
                                                                usedSlotCount:=0,
                                                                baseageseconds:=CUInt(vals("elapsed seconds"))))
                    Next repeat
                End If

                Return New Pickle(Of QueryGamesListResponse)(
                    jarname:=Me.Name,
                    value:=New QueryGamesListResponse(result, games),
                    data:=data.SubView(0, offset),
                    valueDescription:=Function() Pickle(Of Object).MakeListDescription(pickles))
            End Function
        End Class

        Public Class ServerPackets
            Public Shared ReadOnly ProgramAuthenticationBegin As New DefJar(PacketId.ProgramAuthenticationBegin,
                    New EnumUInt32Jar(Of ProgramAuthenticationBeginLogOnType)("logon type").Weaken,
                    New UInt32Jar("server cd key salt", showHex:=True).Weaken,
                    New UInt32Jar("udp value", showHex:=True).Weaken,
                    New FileTimeJar("mpq filetime").Weaken,
                    New StringJar("revision check seed").Weaken,
                    New StringJar("revision check challenge").Weaken,
                    New RawDataJar("server signature", Size:=128).Weaken)
            Public Shared ReadOnly ProgramAuthenticationFinish As New DefJar(PacketId.ProgramAuthenticationFinish,
                    New EnumUInt32Jar(Of ProgramAuthenticationFinishResult)("result").Weaken,
                    New StringJar("info").Weaken)
            Public Shared ReadOnly UserAuthenticationBegin As New DefJar(PacketId.UserAuthenticationBegin,
                    New EnumUInt32Jar(Of UserAuthenticationBeginResult)("result").Weaken,
                    New RawDataJar("account password salt", Size:=32).Weaken,
                    New RawDataJar("server public key", Size:=32).Weaken)
            Public Shared ReadOnly UserAuthenticationFinish As New DefJar(PacketId.UserAuthenticationFinish,
                    New EnumUInt32Jar(Of UserAuthenticationFinishResult)("result").Weaken,
                    New RawDataJar("server password proof", Size:=20).Weaken,
                    New StringJar("custom error info").Weaken)
            Public Shared ReadOnly RequiredWork As New DefJar(PacketId.RequiredWork,
                    New StringJar("filename").Weaken)

            Public Shared ReadOnly ChatEvent As New DefJar(PacketId.ChatEvent,
                    New EnumUInt32Jar(Of ChatEventId)("event id").Weaken,
                    New UInt32Jar("flags", showhex:=True).Weaken,
                    New UInt32Jar("ping").Weaken,
                    New NetIPAddressJar("ip").Weaken,
                    New UInt32Jar("acc#", showhex:=True).Weaken,
                    New UInt32Jar("authority", showhex:=True).Weaken,
                    New StringJar("username").Weaken,
                    New StringJar("text").Weaken)
            Public Shared ReadOnly MessageBox As New DefJar(PacketId.MessageBox,
                    New UInt32Jar("style").Weaken,
                    New StringJar("text").Weaken,
                    New StringJar("caption").Weaken)
            Public Shared ReadOnly FriendsUpdate As New DefJar(PacketId.FriendsUpdate,
                    New ByteJar("entry number").Weaken,
                    New ByteJar("location id").Weaken,
                    New ByteJar("status").Weaken,
                    New DwordStringJar("product id").Weaken,
                    New StringJar("location").Weaken)
            Public Shared ReadOnly QueryGamesList As New QueryGamesListResponseJar()

            Public Shared ReadOnly EnterChat As New DefJar(PacketId.EnterChat,
                    New StringJar("chat username").Weaken,
                    New StringJar("statstring").Weaken,
                    New StringJar("account username").Weaken)
            Public Shared ReadOnly CreateGame3 As New DefJar(PacketId.CreateGame3,
                    New UInt32Jar("result").Weaken)

            Public Shared ReadOnly Null As New DefJar(PacketId.Null)
            Public Shared ReadOnly Ping As New UInt32Jar("salt", showHex:=True)
            Public Shared ReadOnly Warden As New DefJar(PacketId.Warden,
                    New RemainingDataJar("encrypted data").Weaken)

            Public Shared ReadOnly GetFileTime As New DefJar(PacketId.GetFileTime,
                    New UInt32Jar("request id").Weaken,
                    New UInt32Jar("unknown").Weaken,
                    New FileTimeJar("filetime").Weaken,
                    New StringJar("filename").Weaken)
            Public Shared ReadOnly GetIconData As New DefJar(PacketId.GetIconData,
                    New FileTimeJar("filetime").Weaken,
                    New StringJar("filename").Weaken)
            Public Shared ReadOnly ClanInfo As New DefJar(PacketId.ClanInfo,
                    New ByteJar("unknown").Weaken,
                    New DwordStringJar("clan tag").Weaken,
                    New EnumByteJar(Of ClanRank)("rank").Weaken)
        End Class
        Public Class ClientPackets
            Public Shared ReadOnly AuthenticationBegin As New DefJar(PacketId.ProgramAuthenticationBegin,
                    New UInt32Jar("protocol").Weaken,
                    New DwordStringJar("platform").Weaken,
                    New DwordStringJar("product").Weaken,
                    New UInt32Jar("product major version").Weaken,
                    New StringJar("product language", nullTerminated:=False, expectedSize:=4).Weaken,
                    New NetIPAddressJar("internal ip").Weaken,
                    New UInt32Jar("time zone offset").Weaken,
                    New UInt32Jar("location id").Weaken,
                    New EnumUInt32Jar(Of MPQ.LanguageId)("language id").Weaken,
                    New StringJar("country abrev").Weaken,
                    New StringJar("country name").Weaken)
            Public Shared ReadOnly AuthenticationFinish As New DefJar(PacketId.ProgramAuthenticationFinish,
                    New UInt32Jar("client cd key salt", showHex:=True).Weaken,
                    New RawDataJar("exe version", Size:=4).Weaken,
                    New UInt32Jar("revision check response", showHex:=True).Weaken,
                    New UInt32Jar("# cd keys").Weaken,
                    New UInt32Jar("is spawn").Weaken,
                    New ProductCredentialsJar("ROC cd key").Weaken,
                    New ProductCredentialsJar("TFT cd key").Weaken,
                    New StringJar("exe info").Weaken,
                    New StringJar("owner").Weaken)
            Public Shared ReadOnly AccountLogOnBegin As New DefJar(PacketId.UserAuthenticationBegin,
                    New RawDataJar("client public key", Size:=32).Weaken,
                    New StringJar("username").Weaken)
            Public Shared ReadOnly AccountLogOnFinish As New DefJar(PacketId.UserAuthenticationFinish,
                    New RawDataJar("client password proof", Size:=20).Weaken)

            Public Shared ReadOnly ChatCommand As New DefJar(PacketId.ChatCommand,
                    New StringJar("text").Weaken)
            Public Shared ReadOnly QueryGamesList As New DefJar(PacketId.QueryGamesList,
                    New EnumUInt32Jar(Of WC3.GameTypes)("filter").Weaken,
                    New EnumUInt32Jar(Of WC3.GameTypes)("filter mask").Weaken,
                    New UInt32Jar("unknown0").Weaken,
                    New UInt32Jar("list count").Weaken,
                    New StringJar("game name", info:="empty means list games").Weaken,
                    New StringJar("game password", True).Weaken,
                    New StringJar("game stats", True).Weaken)

            Public Shared ReadOnly EnterChat As New DefJar(PacketId.EnterChat,
                    New StringJar("username", info:="[unused]").Weaken,
                    New StringJar("statstring", info:="[unused]").Weaken)
            Public Shared ReadOnly CreateGame3 As New DefJar(PacketId.CreateGame3,
                    New EnumUInt32Jar(Of GameStates)("game state").Weaken,
                    New UInt32Jar("seconds since creation").Weaken,
                    New EnumUInt32Jar(Of WC3.GameTypes)("game type").Weaken,
                    New UInt32Jar("unknown1=1023").Weaken,
                    New UInt32Jar("is ladder").Weaken,
                    New StringJar("name").Weaken,
                    New StringJar("password").Weaken,
                    New TextHexValueJar("num free slots", numdigits:=1).Weaken,
                    New TextHexValueJar("game id", numdigits:=8).Weaken,
                    New WC3.GameStatsJar("statstring").Weaken)
            Public Shared ReadOnly CloseGame3 As New DefJar(PacketId.CloseGame3)
            Public Shared ReadOnly JoinChannel As New DefJar(PacketId.JoinChannel,
                    New EnumUInt32Jar(Of JoinChannelType)("join type").Weaken,
                    New StringJar("channel").Weaken)
            Public Shared ReadOnly NetGamePort As New DefJar(PacketId.NetGamePort,
                    New UInt16Jar("port").Weaken)

            Public Shared ReadOnly Null As New DefJar(PacketId.Null)
            Public Shared ReadOnly Ping As New DefJar(PacketId.Ping,
                    New UInt32Jar("salt", showhex:=True).Weaken)
            Public Shared ReadOnly Warden As New DefJar(PacketId.Warden,
                    New RemainingDataJar("encrypted data").Weaken)

            Public Shared ReadOnly GetFileTime As New DefJar(PacketId.GetFileTime,
                    New UInt32Jar("request id").Weaken,
                    New UInt32Jar("unknown").Weaken,
                    New StringJar("filename").Weaken)
            Public Shared ReadOnly GetIconData As New DefJar(PacketId.GetIconData)
        End Class

#Region "Packers: Logon"
        Public Shared Function MakeAuthenticationBegin(ByVal majorVersion As UInteger,
                                                       ByVal localIPAddress As Net.IPAddress) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.AuthenticationBegin, New Dictionary(Of InvariantString, Object) From {
                    {"protocol", 0},
                    {"platform", "IX86"},
                    {"product", "W3XP"},
                    {"product major version", majorVersion},
                    {"product language", "SUne"},
                    {"internal ip", localIPAddress},
                    {"time zone offset", 240},
                    {"location id", 1033},
                    {"language id", MPQ.LanguageId.English},
                    {"country abrev", "USA"},
                    {"country name", "United States"}
                })
        End Function
        <Pure()>
        Public Shared Function MakeAuthenticationFinish(ByVal version As IReadableList(Of Byte),
                                                        ByVal revisionCheckResponse As UInt32,
                                                        ByVal clientCDKeySalt As UInt32,
                                                        ByVal serverCDKeySalt As UInt32,
                                                        ByVal cdKeyOwner As String,
                                                        ByVal exeInformation As String,
                                                        ByVal productAuthentication As CKL.WC3CredentialPair) As Packet
            Contract.Requires(version IsNot Nothing)
            Contract.Requires(cdKeyOwner IsNot Nothing)
            Contract.Requires(exeInformation IsNot Nothing)
            Contract.Requires(productAuthentication IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            Return New Packet(ClientPackets.AuthenticationFinish, New Dictionary(Of InvariantString, Object) From {
                    {"client cd key salt", clientCDKeySalt},
                    {"exe version", version},
                    {"revision check response", revisionCheckResponse},
                    {"# cd keys", 2},
                    {"is spawn", 0},
                    {"ROC cd key", productAuthentication.AuthenticationROC},
                    {"TFT cd key", productAuthentication.AuthenticationTFT},
                    {"exe info", exeInformation},
                    {"owner", cdKeyOwner}
                })
        End Function
        Public Shared Function MakeAccountLogOnBegin(ByVal credentials As ClientCredentials) As Packet
            Contract.Requires(credentials IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.AccountLogOnBegin, New Dictionary(Of InvariantString, Object) From {
                    {"client public key", credentials.PublicKeyBytes},
                    {"username", credentials.UserName}
                })
        End Function
        Public Shared Function MakeAccountLogOnFinish(ByVal clientPasswordProof As IReadableList(Of Byte)) As Packet
            Contract.Requires(clientPasswordProof IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.AccountLogOnFinish, New Dictionary(Of InvariantString, Object) From {
                    {"client password proof", clientPasswordProof}
                })
        End Function
        Public Shared Function MakeEnterChat() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.EnterChat, New Dictionary(Of InvariantString, Object) From {
                    {"username", ""},
                    {"statstring", ""}
                })
        End Function
#End Region

#Region "Packers: State"
        Public Shared Function MakeNetGamePort(ByVal port As UShort) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim vals As New Dictionary(Of InvariantString, Object)
            vals("port") = port
            Return New Packet(ClientPackets.NetGamePort, vals)
        End Function
        Public Shared Function MakeQueryGamesList(Optional ByVal specificGameName As String = "",
                                                  Optional ByVal listCount As Integer = 20) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.QueryGamesList, New Dictionary(Of InvariantString, Object) From {
                    {"filter", WC3.GameTypes.MaskFilterable},
                    {"filter mask", 0},
                    {"unknown0", 0},
                    {"list count", listCount},
                    {"game name", specificGameName},
                    {"game password", ""},
                    {"game stats", ""}})
        End Function
        Public Shared Function MakeJoinChannel(ByVal joinType As JoinChannelType,
                                               ByVal channel As String) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Dim vals As New Dictionary(Of InvariantString, Object)
            Return New Packet(ClientPackets.JoinChannel, New Dictionary(Of InvariantString, Object) From {
                    {"join type", joinType},
                    {"channel", channel}})
        End Function
        Public Shared Function MakeCreateGame3(ByVal game As WC3.GameDescription) As Packet
            Contract.Requires(game IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Const MAX_GAME_NAME_LENGTH As UInteger = 31
            If game.Name.Length > MAX_GAME_NAME_LENGTH Then
                Throw New ArgumentException("Game name must be less than 32 characters long.", "name")
            End If

            Return New Packet(ClientPackets.CreateGame3, New Dictionary(Of InvariantString, Object) From {
                    {"game state", game.GameState},
                    {"seconds since creation", game.AgeSeconds},
                    {"game type", game.GameType},
                    {"unknown1=1023", 1023},
                    {"is ladder", 0},
                    {"name", game.Name.ToString},
                    {"password", ""},
                    {"num free slots", game.TotalSlotCount - game.UsedSlotCount},
                    {"game id", game.GameId},
                    {"statstring", game.GameStats}})
        End Function
        Public Shared Function MakeCloseGame3() As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)
            Return New Packet(ClientPackets.CloseGame3, New Dictionary(Of InvariantString, Object))
        End Function
#End Region

#Region "Packers: Misc"
        Public Const MaxChatCommandTextLength As Integer = 222
        Shared Function MakeChatCommand(ByVal text As String) As Packet
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            If text.Length > MaxChatCommandTextLength Then
                Throw New ArgumentException("Text cannot exceed {0} characters.".Frmt(MaxChatCommandTextLength), "text")
            End If
            Return New Packet(ClientPackets.ChatCommand, New Dictionary(Of InvariantString, Object) From {
                    {"text", text}})
        End Function
        Public Shared Function MakePing(ByVal salt As UInteger) As Packet
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            Return New Packet(ClientPackets.Ping, New Dictionary(Of InvariantString, Object) From {
                    {"salt", salt}})
        End Function
        Public Shared Function MakeWarden(ByVal encryptedData As IReadableList(Of Byte)) As Packet
            Contract.Requires(encryptedData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Packet)() IsNot Nothing)

            Return New Packet(ClientPackets.Warden, New Dictionary(Of InvariantString, Object) From {
                    {"encrypted data", encryptedData}})
        End Function
#End Region

#Region "Jars"
        Public NotInheritable Class DwordStringJar
            Inherits BaseJar(Of String)
            Public Sub New(ByVal name As InvariantString)
                MyBase.new(name)
            End Sub

            'verification disabled due to stupid verifier
            <ContractVerification(False)>
            Public Overrides Function Pack(Of TValue As String)(ByVal value As TValue) As IPickle(Of TValue)
                If value.Length > 4 Then Throw New ArgumentOutOfRangeException("value", "Value must be at most 4 characters.")
                Dim data = value.ToAscBytes().Reverse.PaddedTo(minimumLength:=4)
                Return New Pickling.Pickle(Of TValue)(Me.Name, value, data.AsReadableList)
            End Function

            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of String)
                If data.Count < 4 Then Throw New PicklingException("Not enough data")
                Dim datum = data.SubView(4)
                Dim value As String = datum.ParseChrString(nullTerminated:=True).Reverse.ToArray
                Return New Pickling.Pickle(Of String)(Me.Name, value, datum)
            End Function
        End Class

        Public NotInheritable Class TextHexValueJar
            Inherits BaseJar(Of ULong)
            Private ReadOnly numDigits As Integer
            Private ReadOnly byteOrder As ByteOrder

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(numDigits > 0)
            End Sub

            Public Sub New(ByVal name As InvariantString,
                           ByVal numDigits As Integer,
                           Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
                MyBase.New(name)
                Contract.Requires(numDigits > 0)
                Contract.Requires(numDigits <= 16)
                Me.numDigits = numDigits
                Me.byteOrder = byteOrder
            End Sub

            Public Overrides Function Pack(Of TValue As ULong)(ByVal value As TValue) As IPickle(Of TValue)
                Dim u = CULng(value)
                Dim digits As IList(Of Char) = New List(Of Char)
                For i = 0 To numDigits - 1
                    Dim val = (u And &HFUL).ToString("x", CultureInfo.InvariantCulture)
                    Contract.Assume(val.Length = 1)
                    digits.Add(val(0))
                    u >>= 4
                Next i

                Select Case byteOrder
                    Case byteOrder.BigEndian
                        digits = digits.Reverse
                    Case byteOrder.LittleEndian
                        'no change
                    Case Else
                        Throw byteOrder.MakeImpossibleValueException()
                End Select

                Return New Pickling.Pickle(Of TValue)(Me.Name, value.AssumeNotNull, New String(digits.ToArray).ToAscBytes().AsReadableList())
            End Function

            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of ULong)
                If data.Count < numDigits Then Throw New PicklingException("Not enough data")
                data = data.SubView(0, numDigits)
                Dim value = data.ParseChrString(nullTerminated:=False).FromHexToUInt64(byteOrder)
                Return New Pickling.Pickle(Of ULong)(Me.Name, value, data)
            End Function
        End Class

        Public NotInheritable Class FileTimeJar
            Inherits BaseJar(Of Date)

            Public Sub New(ByVal name As InvariantString)
                MyBase.New(name)
            End Sub

            'verification disabled due to stupid verifier
            <ContractVerification(False)>
            Public Overrides Function Pack(Of TValue As Date)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
                Dim datum = CType(value, Date).ToFileTime.BitwiseToUInt64.Bytes().AsReadableList
                Return New Pickle(Of TValue)(Me.Name, value, datum)
            End Function

            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of Date)
                If data.Count < 8 Then Throw New PicklingException("Not enough data.")
                Dim datum = data.SubView(0, 8)
                Dim value = Date.FromFileTime(datum.ToUInt64.BitwiseToInt64)
                Return New Pickle(Of Date)(Me.Name, value, datum)
            End Function
        End Class

        Public NotInheritable Class ProductCredentialsJar
            Inherits BaseJar(Of ProductCredentials)

            Private ReadOnly _dataJar As TupleJar

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_dataJar IsNot Nothing)
            End Sub

            Public Sub New(ByVal name As InvariantString)
                MyBase.New(name)
                Me._dataJar = New TupleJar(name,
                        New UInt32Jar("length").Weaken,
                        New EnumUInt32Jar(Of ProductType)("product").Weaken,
                        New UInt32Jar("public key").Weaken,
                        New UInt32Jar("unknown").Weaken,
                        New RawDataJar("proof", Size:=20).Weaken)
            End Sub

            'verification disabled due to stupid verifier
            <ContractVerification(False)>
            Public Overrides Function Pack(Of TValue As ProductCredentials)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
                Dim vals = New Dictionary(Of InvariantString, Object) From {
                        {"length", value.Length},
                        {"product", value.Product},
                        {"public key", value.PublicKey},
                        {"unknown", 0},
                        {"proof", value.AuthenticationProof}}
                Dim pickle = _dataJar.Pack(vals)
                Return New Pickle(Of TValue)(value, pickle.Data, pickle.Description)
            End Function

            Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of ProductCredentials)
                Dim pickle = _dataJar.Parse(data)
                Dim vals = pickle.Value
                Dim proof = CType(vals("proof"), IReadableList(Of Byte)).AssumeNotNull
                Contract.Assume(proof.Count = 20)
                Dim value = New ProductCredentials(
                        product:=CType(vals("product"), ProductType),
                        publicKey:=CUInt(vals("public key")),
                        length:=CUInt(vals("length")),
                        proof:=proof)
                Return New Pickle(Of ProductCredentials)(value, pickle.Data, pickle.Description)
            End Function
        End Class
#End Region
    End Class

    Public NotInheritable Class BnetPacketHandler
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
        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Protected Overrides Function ExtractKey(ByVal header As IReadableList(Of Byte)) As PacketId
            If header(0) <> Packet.PacketPrefixValue Then Throw New IO.InvalidDataException("Invalid packet header.")
            Return CType(header(1), PacketId)
        End Function
    End Class
End Namespace
