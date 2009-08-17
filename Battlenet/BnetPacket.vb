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

Imports HostBot.Warcraft3

Namespace Bnet
    '''<summary>Header values for packets to/from BNET</summary>
    '''<source>BNETDocs.org</source>
    Public Enum BnetPacketID As Byte
        Null = &H0
        CloseGame3 = &H2
        ServerList = &H4
        ClientId = &H5
        StartVersioning = &H6
        ReportVersion = &H7
        StartAdvex = &H8
        QueryGamesList = &H9
        EnterChat = &HA
        GetChannelList = &HB
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
        CreateGame3 = &H1C
        LogonChallengeEx = &H1D
        ClientId2 = &H1E
        LeaveGame = &H1F
        DisplayAd = &H21
        NotifyJoin = &H22
        Ping = &H25
        ReadUserData = &H26
        WriteUserData = &H27
        LogonChallenge = &H28
        LogonResponse = &H29
        CreateAccount = &H2A
        SystemInfo = &H2B
        GameResult = &H2C
        GetIconData = &H2D
        GetLadderData = &H2E
        FindLadderUser = &H2F
        CdKey = &H30
        ChangePassword = &H31
        CheckDataFile = &H32
        GetFileTime = &H33
        QueryRealms = &H34
        Profile = &H35
        CdKey2 = &H36
        LogonResponse2 = &H3A
        CheckDataFile2 = &H3C
        CreateAccount2 = &H3D
        LogonRealmEx = &H3E
        StartVersioning2 = &H3F
        QueryRealms2 = &H40
        QueryAdUrl = &H41
        WarcraftGeneral = &H44
        NetGamePort = &H45
        NewsInfo = &H46
        OptionalWork = &H4A
        ExtraWork = &H4B
        RequiredWork = &H4C
        Tournament = &H4E
        AuthenticationBegin = &H50
        AuthenticationFinish = &H51
        AccountCreate = &H52
        AccountLogonBegin = &H53
        AccountLogonFinish = &H54
        AccountChange = &H55
        AccountChangeProof = &H56
        AccountUpgrade = &H57
        AccountUpgradeProof = &H58
        AccountSetEmail = &H59
        AccountResetPassword = &H5A
        AccountChangeEmail = &H5B
        SwitchProduct = &H5C
        Warden = &H5E
        GamePlayerSearch = &H60
        FriendsList = &H65
        FriendsUpdate = &H66
        FriendsAdd = &H67
        FriendsRemove = &H68
        FriendsPosition = &H69
        ClanFindCandidates = &H70
        ClanInviteMultiple = &H71
        ClanCreationInvitiation = &H72
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

    Public Class BnetPacket
        Public Const PACKET_PREFIX As Byte = &HFF
        Public ReadOnly payload As IPickle(Of Object)
        Public ReadOnly id As BnetPacketID
        Private Shared ReadOnly packetJar As SwitchJar = MakeBnetPacketJar()

        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(payload IsNot Nothing)
        End Sub

#Region "New"
        Private Sub New(ByVal id As BnetPacketID, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me.payload = payload
            Me.id = id
        End Sub
        Private Sub New(ByVal id As BnetPacketID, ByVal value As Object)
            Me.New(id, packetJar.pack(id, value))
            Contract.Requires(value IsNot Nothing)
        End Sub
#End Region

#Region "Jar"
        Private Shared Sub regPack(ByVal jar As SwitchJar,
                                   ByVal id As BnetPacketID,
                                   ByVal ParamArray subjars() As IPackJar(Of Object))
            jar.regPacker(id, New TuplePackJar(id.ToString(), subjars).Weaken)
        End Sub
        Private Shared Sub regParse(ByVal jar As SwitchJar, ByVal id As BnetPacketID, ByVal ParamArray subjars() As IParseJar(Of Object))
            jar.regParser(id, New TupleParseJar(id.ToString(), subjars))
        End Sub
        Private Shared Sub reg_login(ByVal jar As SwitchJar)
            'AUTHENTICATION_BEGIN [Introductions, server authentication, and server challenge to client]
            '[client send] [the client introduces itself]
            regPack(jar, BnetPacketID.AuthenticationBegin,
                    New ValueJar("protocol", 4).Weaken,
                    New StringJar("platform", False, True, 4).Weaken,
                    New StringJar("product", False, True, 4).Weaken,
                    New ValueJar("product version", 4).Weaken,
                    New StringJar("product language", False, , 4).Weaken,
                    New IpBytesJar("internal ip").Weaken,
                    New ValueJar("time zone offset", 4).Weaken,
                    New ValueJar("location id", 4).Weaken,
                    New ValueJar("language id", 4).Weaken,
                    New StringJar("country abrev").Weaken,
                    New StringJar("country name").Weaken)
            '[client receive] [the server introduces itself, authenticates itself, and challenges the client for authentication]
            regParse(jar, BnetPacketID.AuthenticationBegin,
                    New ValueJar("logon type", 4, "2=war3").Weaken,
                    New ArrayJar("server cd key salt", 4),
                    New ArrayJar("udp value", 4),
                    New ArrayJar("mpq file time", 8),
                    New StringJar("mpq number string"),
                    New StringJar("mpq hash challenge"),
                    New ArrayJar("server signature", 128))

            'AUTHENTICATION_FINISH [Client authentication]
            '[client send] [the client authenticates itself against the server's challenge]
            regPack(jar, BnetPacketID.AuthenticationFinish,
                    New ArrayJar("client cd key salt", 4).Weaken,
                    New ArrayJar("exe version", 4).Weaken,
                    New ArrayJar("mpq challenge response", 4).Weaken,
                    New ValueJar("# cd keys", 4).Weaken,
                    New ValueJar("spawn [unused]", 4, "0=false, 1=true").Weaken,
                    New CDKeyJar("ROC cd key").Weaken,
                    New CDKeyJar("TFT cd key").Weaken,
                    New StringJar("exe info").Weaken,
                    New StringJar("owner").Weaken)
            '[client receive] [the server informs the client whether or not it was authenticated]
            regParse(jar, BnetPacketID.AuthenticationFinish,
                    New ValueJar("result", 4, "0=passed, 1 to 255=invalid code, " _
                                                + "0x100=old version, 0x101=invalid version, 0x102=future version, " _
                                                + "0x200=invalid cd key, 0x201=used cd key, 0x202=banned cd key, 0x203=wrong product").Weaken,
                    New StringJar("info"))

            'ACCOUNT_LOGON_BEGIN [Account check and cryptographic key transfer]
            '[client send]
            regPack(jar, BnetPacketID.AccountLogonBegin,
                    New ArrayJar("client public key", 32).Weaken,
                    New StringJar("username").Weaken)
            '[client receive]
            regParse(jar, BnetPacketID.AccountLogonBegin,
                    New ValueJar("result", 4, "0=continue, 1=invalid username, 5=upgrade").Weaken,
                    New ArrayJar("account password salt", 32),
                    New ArrayJar("server public key", 32))

            'ACCOUNT_LOGON_FINISH [exchange of password proofs]
            '[client receive]
            regParse(jar, BnetPacketID.AccountLogonFinish,
                    New ValueJar("result", 4, "0=successful, 2=incorrect password, 14=need email, 15=custom error").Weaken,
                    New ArrayJar("server password proof", 20),
                    New StringJar("custom error info"))
            '[client send]
            regPack(jar, BnetPacketID.AccountLogonFinish,
                    New ArrayJar("client password proof", 20).Weaken)


            'ENTER_CHAT [Request/response for entering chat]
            '[client send]
            regPack(jar, BnetPacketID.EnterChat,
                    New StringJar("username", , , , "[unused]").Weaken,
                    New StringJar("statstring", , , , "[unused]").Weaken)
            '[client receive]
            regParse(jar, BnetPacketID.EnterChat,
                    New StringJar("chat username"),
                    New StringJar("statstring", , True),
                    New StringJar("account username"))
        End Sub
        Private Shared Sub reg_state(ByVal jar As SwitchJar)
            regPack(jar, BnetPacketID.QueryGamesList,
                    New EnumJar(Of GameTypeFlags)("filter", 4, flags:=True).Weaken,
                    New EnumJar(Of GameTypeFlags)("filter mask", 4, flags:=True).Weaken,
                    New ValueJar("unknown0", 4, "=0").Weaken,
                    New ValueJar("list count", 4).Weaken,
                    New StringJar("game name", True, , , "empty means list games").Weaken,
                    New StringJar("game password", True).Weaken,
                    New StringJar("game stats", True).Weaken)
            regParse(jar, BnetPacketID.QueryGamesList, New ListParseJar(Of Dictionary(Of String, Object))("games", numSizePrefixBytes:=4, subjar:=New TupleParseJar("game",
                     New EnumJar(Of GameTypeFlags)("game type", 4, flags:=True).Weaken,
                     New ValueJar("language id", 4).Weaken,
                     New AddressJar("host address"),
                     New EnumJar(Of GameStateFlags)("game state", 4, flags:=True).Weaken,
                     New ValueJar("elapsed time", 4, "(seconds)").Weaken,
                     New StringJar("game name", True),
                     New StringJar("game password", True),
                     New TextHexValueJar("num free slots", 1, ByteOrder.LittleEndian).Weaken,
                     New TextHexValueJar("game id", 8, ByteOrder.LittleEndian).Weaken,
                     New W3MapSettingsJar("game statstring"))))

            'CREATE_GAME_3 [Request/response for listing a game]
            '[client send]
            regPack(jar, BnetPacketID.CreateGame3,
                    New EnumJar(Of GameStateFlags)("game state", 4, flags:=True).Weaken,
                    New ValueJar("time", 4, "time since creation in seconds").Weaken,
                    New EnumJar(Of GameTypeFlags)("game type", 4, flags:=True).Weaken,
                    New ValueJar("unknown1", 4, "=1023").Weaken,
                    New ValueJar("ladder", 4, "0=false, 1=true)").Weaken,
                    New StringJar("name").Weaken,
                    New StringJar("password", , , , "="""" [unused]").Weaken,
                    New TextHexValueJar("num free slots", 1, ByteOrder.LittleEndian).Weaken,
                    New TextHexValueJar("game id", 8, ByteOrder.LittleEndian).Weaken,
                    New W3MapSettingsJar("statstring"))
            '[client receive]
            regParse(jar, BnetPacketID.CreateGame3,
                    New ValueJar("result", 4, "0=success").Weaken)

            'CLOSE_GAME_3 [Client requests server close the client's listed game]
            '[client send]
            regPack(jar, BnetPacketID.CloseGame3)

            'JOIN_CHANNEL [Client requests joining a channel]
            '[client send]
            regPack(jar, BnetPacketID.JoinChannel,
                    New ArrayJar("flags", 4, , , "0=no create, 1=first join, 2=forced join, 3=diablo2 join").Weaken,
                    New StringJar("channel").Weaken)

            'NET_GAME_PORT [Client tells server what port it will listen for other clients on when hosting games]
            '[client send]
            regPack(jar, BnetPacketID.NetGamePort,
                    New ValueJar("port", 2).Weaken)
        End Sub
        Private Shared Sub reg_misc(ByVal jar As SwitchJar)
            'CHAT_EVENT [Informs the client what other clients are doing (talking, leaving, etc)]
            '[client receive]
            regParse(jar, BnetPacketID.ChatEvent,
                    New EnumJar(Of ChatEventId)("event id", 4, flags:=False).Weaken,
                    New ArrayJar("flags", 4),
                    New ValueJar("ping", 4).Weaken,
                    New IpBytesJar("ip", "[unused]"),
                    New ArrayJar("acc#", 4, , , "[unused]"),
                    New ArrayJar("authority", 4, , , "[unused]"),
                    New StringJar("username").Weaken,
                    New StringJar("text").Weaken)

            'NULL [ignored keep-alive packet]
            '[client receive]
            regParse(jar, BnetPacketID.Null)
            '[client send]
            regPack(jar, BnetPacketID.Null)

            'PING [sent periodically by server]
            '[client receive]
            regParse(jar, BnetPacketID.Ping,
                    New ValueJar("salt", 4).Weaken)
            '[client send]
            regPack(jar, BnetPacketID.Ping,
                    New ValueJar("salt", 4).Weaken)

            'MESSAGE_BOX [server yells at client]
            '[client receive]
            regParse(jar, BnetPacketID.MessageBox,
                    New ValueJar("style", 4).Weaken,
                    New StringJar("text"),
                    New StringJar("caption"))

            'CHAT_COMMAND [client sends text to server]
            '[client send]
            regPack(jar, BnetPacketID.ChatCommand,
                    New StringJar("text").Weaken)

            'FRIENDS_UPDATE
            '[client receive]
            regParse(jar, BnetPacketID.FriendsUpdate,
                    New ValueJar("entry number", 1).Weaken,
                    New ValueJar("location id", 1).Weaken,
                    New ValueJar("status", 1).Weaken,
                    New StringJar("product id", False, True, 4),
                    New StringJar("location"))

            'REQUIRED_WORKD
            '[client receive]
            regParse(jar, BnetPacketID.RequiredWork,
                    New StringJar("filename"))

            'WARDEN
            regParse(jar, BnetPacketID.Warden, New ArrayJar("encrypted data", , , True))
            regPack(jar, BnetPacketID.Warden, New ArrayJar("encrypted data", , , True).Weaken)
        End Sub
        Public Shared Function MakeBnetPacketJar() As SwitchJar
            Dim jar As New SwitchJar
            reg_login(jar)
            reg_misc(jar)
            reg_state(jar)
            Return jar
        End Function
#End Region

#Region "Enums"
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
        Public Enum GameStateFlags As UInteger
            [Private] = 1 << 0
            Full = 1 << 2
            NotEmpty = 1 << 3 'really unsure about this one
            InProgress = 1 << 4
            UnknownFlag = 1 << 5
        End Enum
        Public Enum JoinChannelType As UInteger
            NoCreate = 0
            FirstJoin = 1
            ForcedJoin = 2
            Diablo2Join = 3
        End Enum
#End Region

#Region "Packers: Logon"
        Public Shared Function MakeAuthenticationBegin(ByVal version As UInteger, ByVal local_ip() As Byte) As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketID.AuthenticationBegin, New Dictionary(Of String, Object) From {
                    {"protocol", 0},
                    {"platform", "IX86"},
                    {"product", "W3XP"},
                    {"product version", version},
                    {"product language", "SUne"},
                    {"internal ip", local_ip},
                    {"time zone offset", 240},
                    {"location id", 1033},
                    {"language id", 1033},
                    {"country abrev", "USA"},
                    {"country name", "United States"}
                })
        End Function
        Public Shared Function MakeAuthenticationFinish(ByVal version As Byte(),
                                                        ByVal mpqFolder As String,
                                                        ByVal mpqNumberString As String,
                                                        ByVal mpqHashChallenge As String,
                                                        ByVal serverCdKeySalt As Byte(),
                                                        ByVal cdKeyOwner As String,
                                                        ByVal exeInformation As String,
                                                        ByVal rocKey As String,
                                                        ByVal tftKey As String,
                                                        ByVal R As System.Security.Cryptography.RandomNumberGenerator) As BnetPacket
            Contract.Requires(version IsNot Nothing)
            Contract.Requires(mpqFolder IsNot Nothing)
            Contract.Requires(mpqNumberString IsNot Nothing)
            Contract.Requires(mpqHashChallenge IsNot Nothing)
            Contract.Requires(serverCdKeySalt IsNot Nothing)
            Contract.Requires(cdKeyOwner IsNot Nothing)
            Contract.Requires(exeInformation IsNot Nothing)
            Contract.Requires(rocKey IsNot Nothing)
            Contract.Requires(tftKey IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)

            Dim clientCdKeySalt(0 To 3) As Byte
            R.GetBytes(clientCdKeySalt)

            Return New BnetPacket(BnetPacketID.AuthenticationFinish, New Dictionary(Of String, Object) From {
                    {"client cd key salt", clientCdKeySalt},
                    {"exe version", version},
                    {"mpq challenge response", Bnet.Crypt.GenerateRevisionCheck(mpqFolder, mpqNumberString, mpqHashChallenge).bytes(ByteOrder.LittleEndian)},
                    {"# cd keys", 2},
                    {"spawn [unused]", 0},
                    {"ROC cd key", CDKeyJar.packCDKey(rocKey, clientCdKeySalt.ToView, serverCdKeySalt.ToView)},
                    {"TFT cd key", CDKeyJar.packCDKey(tftKey, clientCdKeySalt.ToView, serverCdKeySalt.ToView)},
                    {"exe info", exeInformation},
                    {"owner", cdKeyOwner}
                })
        End Function
        Public Shared Function MakeAccountLogonBegin(ByVal username As String,
                                                     ByVal clientPublicKey As Byte()) As BnetPacket
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(clientPublicKey IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketID.AccountLogonBegin, New Dictionary(Of String, Object) From {
                    {"client public key", clientPublicKey},
                    {"username", username}
                })
        End Function
        Public Shared Function MakeAccountLogonFinish(ByVal clientPasswordProof As Byte()) As BnetPacket
            Contract.Requires(clientPasswordProof IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketID.AccountLogonFinish, New Dictionary(Of String, Object) From {
                    {"client password proof", clientPasswordProof}
                })
        End Function
        Public Shared Function MakeEnterChat() As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketID.EnterChat, New Dictionary(Of String, Object) From {
                    {"username", ""},
                    {"statstring", ""}
                })
        End Function
#End Region
#Region "Packers: State"
        Public Shared Function MakeNetGamePort(ByVal port As UShort) As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Dim vals As New Dictionary(Of String, Object)
            vals("port") = port
            Return New BnetPacket(BnetPacketID.NetGamePort, vals)
        End Function
        Public Shared Function MakeQueryGamesList(Optional ByVal specificGameName As String = "",
                                                  Optional ByVal listCount As Integer = 20) As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketID.QueryGamesList, New Dictionary(Of String, Object) From {
                    {"filter", GameTypeFlags.MaskFilterable},
                    {"filter mask", 0},
                    {"unknown0", 0},
                    {"list count", listCount},
                    {"game name", specificGameName},
                    {"game password", ""},
                    {"game stats", ""}})
        End Function
        Public Shared Function MakeJoinChannel(ByVal flags As JoinChannelType,
                                               ByVal channel As String) As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Dim vals As New Dictionary(Of String, Object)
            Return New BnetPacket(BnetPacketID.JoinChannel, New Dictionary(Of String, Object) From {
                    {"flags", CUInt(flags).bytes(ByteOrder.LittleEndian)},
                    {"channel", channel}})
        End Function
        Public Shared Function MakeCreateGame3(ByVal game As IW3GameStateDescription,
                                               ByVal gameId As Integer) As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Const MAX_GAME_NAME_LENGTH As UInteger = 31
            If game.Name.Length > MAX_GAME_NAME_LENGTH Then
                Throw New ArgumentException("Game name must be less than 32 characters long.", "name")
            End If

            Return New BnetPacket(BnetPacketID.CreateGame3, New Dictionary(Of String, Object) From {
                    {"game state", game.GameState},
                    {"time", CUInt((DateTime.Now() - game.CreationTime).TotalSeconds)},
                    {"game type", game.GameType},
                    {"unknown1", 1023},
                    {"ladder", 0},
                    {"name", game.Name},
                    {"password", ""},
                    {"num free slots", game.NumFreeSlots},
                    {"game id", gameId},
                    {"statstring", New Dictionary(Of String, Object) From {{"settings", game.Settings}, {"username", game.HostUserName}}}})
        End Function
        Public Shared Function MakeCloseGame3() As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketID.CloseGame3, New Dictionary(Of String, Object))
        End Function
#End Region
#Region "Packers: CKL"
        Public Shared Function MakeCklAuthenticationFinish(ByVal Version As Byte(),
                                                           ByVal MpqFolder As String,
                                                           ByVal MpqNumberString As String,
                                                           ByVal MpqHashChallenge As String,
                                                           ByVal ServerCdKeySalt As Byte(),
                                                           ByVal CdKeyOwner As String,
                                                           ByVal ExeInformation As String,
                                                           ByVal CklRemoteHost As String,
                                                           ByVal CklRemotePort As UShort,
                                                           ByVal R As System.Security.Cryptography.RandomNumberGenerator) As IFuture(Of Outcome(Of BnetPacket))
            Contract.Requires(Version IsNot Nothing)
            Contract.Requires(MpqFolder IsNot Nothing)
            Contract.Requires(MpqNumberString IsNot Nothing)
            Contract.Requires(MpqHashChallenge IsNot Nothing)
            Contract.Requires(ServerCdKeySalt IsNot Nothing)
            Contract.Requires(CdKeyOwner IsNot Nothing)
            Contract.Requires(ExeInformation IsNot Nothing)
            Contract.Requires(CklRemoteHost IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome(Of BnetPacket)))() IsNot Nothing)
            Dim vals As New Dictionary(Of String, Object)

            Dim clientCdKeySalt(0 To 3) As Byte
            R.GetBytes(clientCdKeySalt)

            vals("client cd key salt") = clientCdKeySalt
            vals("exe version") = Version
            vals("mpq challenge response") = Bnet.Crypt.GenerateRevisionCheck(MpqFolder, MpqNumberString, MpqHashChallenge).bytes(ByteOrder.LittleEndian)
            vals("# cd keys") = 2
            vals("spawn [unused]") = 0
            vals("exe info") = ExeInformation
            vals("owner") = CdKeyOwner

            Return CKL.CklClient.BeginBorrowKeys(CklRemoteHost, CklRemotePort, clientCdKeySalt, ServerCdKeySalt).EvalWhenValueReady(
                Function(borrowed) As Outcome(Of BnetPacket)
                    If Not borrowed.succeeded Then  Return Failure(borrowed.Message)

                    vals("ROC cd key") = borrowed.Value.rocKey
                    vals("TFT cd key") = borrowed.Value.tftKey
                    Return Success(New BnetPacket(BnetPacketID.AuthenticationFinish, vals), borrowed.Message)
                End Function
            )
        End Function
#End Region
#Region "Packers: Misc"
        Public Shared Function MakeChatCommand(ByVal text As String) As BnetPacket
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)

            Const MAX_TEXT_LENGTH As Integer = 222
            If text.Length > MAX_TEXT_LENGTH Then
                text = text.Substring(0, MAX_TEXT_LENGTH)
                'Throw New ArgumentException(String.Format("Text cannot exceed {0} characters.", MAX_TEXT_LENGTH), "text")
            End If
            Return New BnetPacket(BnetPacketID.ChatCommand, New Dictionary(Of String, Object) From {
                    {"text", text}})
        End Function
        Public Shared Function MakePing(ByVal salt As UInteger) As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)

            Return New BnetPacket(BnetPacketID.Ping, New Dictionary(Of String, Object) From {
                    {"salt", salt}})
        End Function
        Public Shared Function MakeWarden(ByVal encryptedData As Byte()) As BnetPacket
            Contract.Requires(encryptedData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)

            Return New BnetPacket(BnetPacketID.Warden, New Dictionary(Of String, Object) From {
                    {"encrypted data", encryptedData}})
        End Function
#End Region

        Public Shared Function FromData(ByVal id As BnetPacketID, ByVal data As ViewableList(Of Byte)) As BnetPacket
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(id, packetJar.Parse(id, data))
        End Function

#Region "Jars"
        Public Class TextHexValueJar
            Inherits Jar(Of ULong)
            Private ReadOnly numDigits As Integer
            Private ReadOnly byteOrder As ByteOrder

            <ContractInvariantMethod()> Protected Overrides Sub Invariant()
                Contract.Invariant(numDigits > 0)
            End Sub

            Public Sub New(ByVal name As String,
                           ByVal numDigits As Integer,
                           ByVal byteOrder As ByteOrder)
                MyBase.New(name)
                Contract.Requires(name IsNot Nothing)
                Contract.Requires(numDigits > 0)
                Contract.Requires(numDigits <= 16)
                Me.numDigits = numDigits
                Me.byteOrder = byteOrder
            End Sub

            Public Overrides Function Pack(Of R As ULong)(ByVal value As R) As IPickle(Of R)
                Dim u = CULng(value)
                Dim digits(0 To numDigits - 1) As Char
                Select Case byteOrder
                    Case byteOrder.BigEndian
                        For i = numDigits - 1 To 0 Step -1
                            digits(i) = Hex(u And CULng(&HF)).ToLower()(0)
                            u >>= 4
                        Next i
                    Case byteOrder.LittleEndian
                        For i = 0 To numDigits - 1
                            digits(i) = Hex(u And CULng(&HF)).ToLower()(0)
                            u >>= 4
                        Next i
                    Case Else
                        Throw New UnreachableException()
                End Select

                Return New Pickling.Pickle(Of R)(Me.Name, value, New String(digits).ToAscBytes().ToView())
            End Function

            Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of ULong)
                If data.Length < numDigits Then Throw New PicklingException("Not enough data")
                data = data.SubView(0, numDigits)
                Return New Pickling.Pickle(Of ULong)(Me.Name, data.ParseChrString(nullTerminated:=False).ParseAsUnsignedHexNumber(byteOrder), data)
            End Function
        End Class

        Public Class CDKeyJar
            Inherits Pickling.Jars.TupleJar

            Public Sub New(ByVal name As String)
                MyBase.New(name,
                        New ValueJar("length", 4).Weaken,
                        New ArrayJar("product key", 4).Weaken,
                        New ArrayJar("public key", 4).Weaken,
                        New ValueJar("unknown", 4, "=0").Weaken,
                        New ArrayJar("hash", 20).Weaken)
                Contract.Requires(name IsNot Nothing)
            End Sub

            Public Shared Function packCDKey(ByVal key As String,
                                             ByVal clientToken As ViewableList(Of Byte),
                                             ByVal serverToken As ViewableList(Of Byte)) As Dictionary(Of String, Object)
                Contract.Requires(key IsNot Nothing)
                Contract.Requires(clientToken IsNot Nothing)
                Contract.Requires(serverToken IsNot Nothing)

                Dim cdkey = New Bnet.Crypt.CDKey(key)
                Return New Dictionary(Of String, Object) From {
                        {"length", CUInt(key.Length)},
                        {"product key", cdkey.productKey.ToArray()},
                        {"public key", cdkey.publicKey.ToArray()},
                        {"unknown", 0},
                        {"hash", Bnet.Crypt.SHA1(Concat(clientToken.ToArray,
                                                        serverToken.ToArray,
                                                        cdkey.productKey.ToArray,
                                                        cdkey.publicKey.ToArray,
                                                        cdkey.privateKey.ToArray))}}
            End Function

            Public Shared Function packBorrowedCdKey(ByVal data() As Byte) As Dictionary(Of String, Object)
                Contract.Requires(data IsNot Nothing)
                Contract.Requires(data.Length = 36)

                Return New Dictionary(Of String, Object) From {
                        {"length", data.SubArray(0, 4).ToUInt32(ByteOrder.LittleEndian)},
                        {"product key", data.SubArray(4, 4)},
                        {"public key", data.SubArray(8, 4)},
                        {"unknown", data.SubArray(12, 4).ToUInt32(ByteOrder.LittleEndian)},
                        {"hash", data.SubArray(16, 20)}}
            End Function
        End Class
#End Region
    End Class
End Namespace