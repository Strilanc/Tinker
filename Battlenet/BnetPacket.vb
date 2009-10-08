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
    Public Enum BnetPacketId As Byte
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
        '''<summary>Client tells server what port it will listen for other clients on when hosting games.</summary>
        NetGamePort = &H45
        NewsInfo = &H46
        OptionalWork = &H4A
        ExtraWork = &H4B
        RequiredWork = &H4C
        Tournament = &H4E
        '''<summary>Introductions, server authentication, and server challenge to client.</summary>
        AuthenticationBegin = &H50
        '''<summary>The client authenticates itself against the server's challenge</summary>
        AuthenticationFinish = &H51
        AccountCreate = &H52
        AccountLogOnBegin = &H53
        '''<summary>Exchange of password proofs</summary>
        AccountLogOnFinish = &H54
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

    Public NotInheritable Class BnetPacket
        Public Const PacketPrefixValue As Byte = &HFF
        Private ReadOnly _payload As IPickle(Of Object)
        Public ReadOnly id As BnetPacketId
        Private Shared ReadOnly packetJar As ManualSwitchJar = MakeBnetPacketJar()
        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(ByVal id As BnetPacketId, ByVal payload As IPickle(Of Object))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload
            Me.id = id
        End Sub
        Private Sub New(ByVal id As BnetPacketId, ByVal value As Object)
            Me.New(id, packetJar.Pack(id, value))
            Contract.Requires(value IsNot Nothing)
        End Sub
        Public Shared Function FromData(ByVal id As BnetPacketId, ByVal data As ViewableList(Of Byte)) As BnetPacket
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(id, packetJar.Parse(id, data))
        End Function

#Region "Enums"
        Public Enum AuthenticationBeginLogOnType As Byte
            Warcraft3 = 2
        End Enum
        Public Enum AccountLogOnFinishResult As UInteger
            Passed = 0
            IncorrectPassword = 2
            NeedEmail = 14
            CustomError = 15
        End Enum
        Public Enum AccountLogOnBeginResult As UInteger
            Passed = 0
            BadUserName = 1
            UpgradeAccount = 5
        End Enum
        Public Enum AuthenticationFinishResult As UInteger
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
#End Region

#Region "Definition"
        Private Shared Sub regPack(ByVal jar As ManualSwitchJar,
                                   ByVal id As BnetPacketId,
                                   ByVal ParamArray subJars() As IPackJar(Of Object))
            jar.AddPacker(id, New TuplePackJar(id.ToString(), subJars).Weaken)
        End Sub
        Private Shared Sub regParse(ByVal jar As ManualSwitchJar, ByVal id As BnetPacketId, ByVal ParamArray subJars() As IParseJar(Of Object))
            jar.AddParser(id, New TupleParseJar(id.ToString(), subJars))
        End Sub

        Private Shared Function MakeBnetPacketJar() As ManualSwitchJar
            Dim jar As New ManualSwitchJar

            'Connection
            regPack(jar, BnetPacketId.AuthenticationBegin,
                    New UInt32Jar("protocol").Weaken,
                    New StringJar("platform", False, True, 4).Weaken,
                    New StringJar("product", False, True, 4).Weaken,
                    New UInt32Jar("product version").Weaken,
                    New StringJar("product language", False, , 4).Weaken,
                    New IPAddressJar("internal ip").Weaken,
                    New UInt32Jar("time zone offset").Weaken,
                    New UInt32Jar("location id").Weaken,
                    New UInt32Jar("language id").Weaken,
                    New StringJar("country abrev").Weaken,
                    New StringJar("country name").Weaken)
            regParse(jar, BnetPacketId.AuthenticationBegin,
                    New EnumUInt32Jar(Of AuthenticationBeginLogOnType)("logon type").Weaken,
                    New ArrayJar("server cd key salt", 4),
                    New ArrayJar("udp value", 4),
                    New ArrayJar("mpq file time", 8),
                    New StringJar("mpq number string"),
                    New StringJar("mpq hash challenge"),
                    New ArrayJar("server signature", 128))
            regPack(jar, BnetPacketId.AuthenticationFinish,
                    New ArrayJar("client cd key salt", 4).Weaken,
                    New ArrayJar("exe version", 4).Weaken,
                    New ArrayJar("mpq challenge response", 4).Weaken,
                    New UInt32Jar("# cd keys").Weaken,
                    New ValueJar("spawn [unused]", 4, "0=false, 1=true").Weaken,
                    New CDKeyJar("ROC cd key").Weaken,
                    New CDKeyJar("TFT cd key").Weaken,
                    New StringJar("exe info").Weaken,
                    New StringJar("owner").Weaken)
            regParse(jar, BnetPacketId.AuthenticationFinish,
                    New EnumUInt32Jar(Of AuthenticationFinishResult)("result").Weaken,
                    New StringJar("info"))
            regPack(jar, BnetPacketId.AccountLogOnBegin,
                    New ArrayJar("client public key", 32).Weaken,
                    New StringJar("username").Weaken)
            regParse(jar, BnetPacketId.AccountLogOnBegin,
                    New EnumUInt32Jar(Of AccountLogOnBeginResult)("result").Weaken,
                    New ArrayJar("account password salt", 32),
                    New ArrayJar("server public key", 32))
            regParse(jar, BnetPacketId.AccountLogOnFinish,
                    New EnumUInt32Jar(Of AccountLogOnFinishResult)("result").Weaken,
                    New ArrayJar("server password proof", 20),
                    New StringJar("custom error info"))
            regPack(jar, BnetPacketId.AccountLogOnFinish,
                    New ArrayJar("client password proof", 20).Weaken)
            regParse(jar, BnetPacketId.RequiredWork,
                    New StringJar("filename"))

            'Interaction
            regParse(jar, BnetPacketId.ChatEvent,
                    New EnumUInt32Jar(Of ChatEventId)("event id").Weaken,
                    New ArrayJar("flags", 4),
                    New UInt32Jar("ping").Weaken,
                    New IPAddressJar("ip"),
                    New ArrayJar("acc#", 4),
                    New ArrayJar("authority", 4),
                    New StringJar("username"),
                    New StringJar("text"))
            regParse(jar, BnetPacketId.MessageBox,
                    New UInt32Jar("style").Weaken,
                    New StringJar("text"),
                    New StringJar("caption"))
            regPack(jar, BnetPacketId.ChatCommand,
                    New StringJar("text").Weaken)
            regParse(jar, BnetPacketId.FriendsUpdate,
                    New ByteJar("entry number").Weaken,
                    New ByteJar("location id").Weaken,
                    New ByteJar("status").Weaken,
                    New StringJar("product id", False, True, 4),
                    New StringJar("location"))
            regPack(jar, BnetPacketId.QueryGamesList,
                    New EnumUInt32Jar(Of GameTypes)("filter").Weaken,
                    New EnumUInt32Jar(Of GameTypes)("filter mask").Weaken,
                    New UInt32Jar("unknown0").Weaken,
                    New UInt32Jar("list count").Weaken,
                    New StringJar("game name", True, , , "empty means list games").Weaken,
                    New StringJar("game password", True).Weaken,
                    New StringJar("game stats", True).Weaken)
            regParse(jar, BnetPacketId.QueryGamesList, New ListParseJar(Of Dictionary(Of String, Object))("games", numSizePrefixBytes:=4, subJar:=New TupleParseJar("game",
                    New EnumUInt32Jar(Of GameTypes)("game type").Weaken,
                    New UInt32Jar("language id").Weaken,
                    New AddressJar("host address"),
                    New EnumUInt32Jar(Of GameStates)("game state").Weaken,
                    New UInt32Jar("elapsed seconds").Weaken,
                    New StringJar("game name", True),
                    New StringJar("game password", True),
                    New TextHexValueJar("num free slots", numdigits:=1).Weaken,
                    New TextHexValueJar("game id", numdigits:=8).Weaken,
                    New W3MapSettingsJar("game statstring"))))

            'State
            regPack(jar, BnetPacketId.EnterChat,
                    New StringJar("username", , , , "[unused]").Weaken,
                    New StringJar("statstring", , , , "[unused]").Weaken)
            regParse(jar, BnetPacketId.EnterChat,
                    New StringJar("chat username"),
                    New StringJar("statstring", , True),
                    New StringJar("account username"))
            regPack(jar, BnetPacketId.CreateGame3,
                    New EnumUInt32Jar(Of GameStates)("game state").Weaken,
                    New UInt32Jar("seconds since creation").Weaken,
                    New EnumUInt32Jar(Of GameTypes)("game type").Weaken,
                    New UInt32Jar("unknown1=1023").Weaken,
                    New ValueJar("ladder", 4, "0=false, 1=true)").Weaken,
                    New StringJar("name").Weaken,
                    New StringJar("password").Weaken,
                    New TextHexValueJar("num free slots", numdigits:=1).Weaken,
                    New TextHexValueJar("game id", numdigits:=8).Weaken,
                    New W3MapSettingsJar("statstring"))
            regParse(jar, BnetPacketId.CreateGame3,
                    New ValueJar("result", 4, "0=success").Weaken)
            regPack(jar, BnetPacketId.CloseGame3)
            regPack(jar, BnetPacketId.JoinChannel,
                    New ArrayJar("flags", 4, , , "0=no create, 1=first join, 2=forced join, 3=diablo2 join").Weaken,
                    New StringJar("channel").Weaken)
            regPack(jar, BnetPacketId.NetGamePort,
                    New UInt16Jar("port").Weaken)

            'Periodic
            regParse(jar, BnetPacketId.Null)
            regPack(jar, BnetPacketId.Null)
            regParse(jar, BnetPacketId.Ping,
                    New UInt32Jar("salt").Weaken)
            regPack(jar, BnetPacketId.Ping,
                    New UInt32Jar("salt").Weaken)
            regParse(jar, BnetPacketId.Warden,
                    New ArrayJar("encrypted data", takeRest:=True))
            regPack(jar, BnetPacketId.Warden,
                    New ArrayJar("encrypted data", takeRest:=True).Weaken)

            Return jar
        End Function
#End Region

#Region "Packers: Logon"
        Public Shared Function MakeAuthenticationBegin(ByVal version As UInteger, ByVal localIPAddress As Byte()) As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketId.AuthenticationBegin, New Dictionary(Of String, Object) From {
                    {"protocol", 0},
                    {"platform", "IX86"},
                    {"product", "W3XP"},
                    {"product version", version},
                    {"product language", "SUne"},
                    {"internal ip", localIPAddress},
                    {"time zone offset", 240},
                    {"location id", 1033},
                    {"language id", 1033},
                    {"country abrev", "USA"},
                    {"country name", "United States"}
                })
        End Function
        Public Shared Function MakeAuthenticationFinish(ByVal version As Byte(),
                                                        ByVal mpqFolder As String,
                                                        ByVal indexString As String,
                                                        ByVal mpqHashChallenge As String,
                                                        ByVal serverCDKeySalt As Byte(),
                                                        ByVal cdKeyOwner As String,
                                                        ByVal exeInformation As String,
                                                        ByVal cdKeyROC As String,
                                                        ByVal cdKeyTFT As String,
                                                        ByVal secureRandomNumberGenerator As System.Security.Cryptography.RandomNumberGenerator) As BnetPacket
            Contract.Requires(version IsNot Nothing)
            Contract.Requires(mpqFolder IsNot Nothing)
            Contract.Requires(indexString IsNot Nothing)
            Contract.Requires(mpqHashChallenge IsNot Nothing)
            Contract.Requires(serverCDKeySalt IsNot Nothing)
            Contract.Requires(cdKeyOwner IsNot Nothing)
            Contract.Requires(exeInformation IsNot Nothing)
            Contract.Requires(cdKeyROC IsNot Nothing)
            Contract.Requires(cdKeyTFT IsNot Nothing)
            Contract.Requires(secureRandomNumberGenerator IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)

            Dim clientCdKeySalt(0 To 3) As Byte
            secureRandomNumberGenerator.GetBytes(clientCdKeySalt)

            Return New BnetPacket(BnetPacketId.AuthenticationFinish, New Dictionary(Of String, Object) From {
                    {"client cd key salt", clientCdKeySalt},
                    {"exe version", version},
                    {"mpq challenge response", Bnet.Crypt.GenerateRevisionCheck(mpqFolder, indexString, mpqHashChallenge).Bytes()},
                    {"# cd keys", 2},
                    {"spawn [unused]", 0},
                    {"ROC cd key", CDKeyJar.PackCDKey(cdKeyROC, clientCdKeySalt.ToView, serverCDKeySalt.ToView)},
                    {"TFT cd key", CDKeyJar.PackCDKey(cdKeyTFT, clientCdKeySalt.ToView, serverCDKeySalt.ToView)},
                    {"exe info", exeInformation},
                    {"owner", cdKeyOwner}
                })
        End Function
        Public Shared Function MakeAccountLogOnBegin(ByVal userName As String,
                                                     ByVal clientPublicKey As ViewableList(Of Byte)) As BnetPacket
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(clientPublicKey IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketId.AccountLogOnBegin, New Dictionary(Of String, Object) From {
                    {"client public key", clientPublicKey.ToArray},
                    {"username", userName}
                })
        End Function
        Public Shared Function MakeAccountLogOnFinish(ByVal clientPasswordProof As ViewableList(Of Byte)) As BnetPacket
            Contract.Requires(clientPasswordProof IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketId.AccountLogOnFinish, New Dictionary(Of String, Object) From {
                    {"client password proof", clientPasswordProof.ToArray}
                })
        End Function
        Public Shared Function MakeEnterChat() As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketId.EnterChat, New Dictionary(Of String, Object) From {
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
            Return New BnetPacket(BnetPacketId.NetGamePort, vals)
        End Function
        Public Shared Function MakeQueryGamesList(Optional ByVal specificGameName As String = "",
                                                  Optional ByVal listCount As Integer = 20) As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketId.QueryGamesList, New Dictionary(Of String, Object) From {
                    {"filter", GameTypes.MaskFilterable},
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
            Return New BnetPacket(BnetPacketId.JoinChannel, New Dictionary(Of String, Object) From {
                    {"flags", CUInt(flags).Bytes()},
                    {"channel", channel}})
        End Function
        Public Shared Function MakeCreateGame3(ByVal game As IW3GameStateDescription,
                                               ByVal gameId As Integer) As BnetPacket
            Contract.Requires(game IsNot Nothing)
            Contract.Requires(gameId >= 0)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Const MAX_GAME_NAME_LENGTH As UInteger = 31
            If game.Name.Length > MAX_GAME_NAME_LENGTH Then
                Throw New ArgumentException("Game name must be less than 32 characters long.", "name")
            End If

            Return New BnetPacket(BnetPacketId.CreateGame3, New Dictionary(Of String, Object) From {
                    {"game state", game.GameState},
                    {"seconds since creation", CUInt((DateTime.Now() - game.CreationTime).TotalSeconds)},
                    {"game type", game.GameType},
                    {"unknown1=1023", 1023},
                    {"ladder", 0},
                    {"name", game.Name},
                    {"password", ""},
                    {"num free slots", game.NumFreeSlots},
                    {"game id", gameId},
                    {"statstring", New Dictionary(Of String, Object) From {{"settings", game.Settings}, {"username", game.HostUserName}}}})
        End Function
        Public Shared Function MakeCloseGame3() As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)
            Return New BnetPacket(BnetPacketId.CloseGame3, New Dictionary(Of String, Object))
        End Function
#End Region

#Region "Packers: CKL"
        Public Shared Function MakeCKLAuthenticationFinish(ByVal version As Byte(),
                                                           ByVal mpqFolder As String,
                                                           ByVal mpqNumberString As String,
                                                           ByVal mpqHashChallenge As String,
                                                           ByVal serverCDKeySalt As Byte(),
                                                           ByVal cdKeyOwner As String,
                                                           ByVal exeInformation As String,
                                                           ByVal remoteHostName As String,
                                                           ByVal remotePort As UShort,
                                                           ByVal secureRandomNumberGenerator As Security.Cryptography.RandomNumberGenerator) As IFuture(Of BnetPacket)
            Contract.Requires(version IsNot Nothing)
            Contract.Requires(mpqFolder IsNot Nothing)
            Contract.Requires(mpqNumberString IsNot Nothing)
            Contract.Requires(mpqHashChallenge IsNot Nothing)
            Contract.Requires(serverCDKeySalt IsNot Nothing)
            Contract.Requires(cdKeyOwner IsNot Nothing)
            Contract.Requires(exeInformation IsNot Nothing)
            Contract.Requires(remoteHostName IsNot Nothing)
            Contract.Requires(secureRandomNumberGenerator IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of BnetPacket))() IsNot Nothing)

            Dim clientCdKeySalt(0 To 3) As Byte
            secureRandomNumberGenerator.GetBytes(clientCdKeySalt)

            Dim vals As New Dictionary(Of String, Object) From {
                {"client cd key salt", clientCdKeySalt},
                {"exe version", version},
                {"mpq challenge response", Bnet.Crypt.GenerateRevisionCheck(mpqFolder, mpqNumberString, mpqHashChallenge).Bytes()},
                {"# cd keys", 2},
                {"spawn [unused]", 0},
                {"exe info", exeInformation},
                {"owner", cdKeyOwner}}

            'Asynchronously borrow keys from server and return completed packet
            Return CKL.CKLClient.BeginBorrowKeys(remoteHostName, remotePort, clientCdKeySalt, serverCDKeySalt).Select(
                Function(borrowedKeys)
                    vals("ROC cd key") = borrowedKeys.CDKeyROC
                    vals("TFT cd key") = borrowedKeys.CDKeyTFT
                    Return New BnetPacket(BnetPacketId.AuthenticationFinish, vals)
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
            Return New BnetPacket(BnetPacketId.ChatCommand, New Dictionary(Of String, Object) From {
                    {"text", text}})
        End Function
        Public Shared Function MakePing(ByVal salt As UInteger) As BnetPacket
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)

            Return New BnetPacket(BnetPacketId.Ping, New Dictionary(Of String, Object) From {
                    {"salt", salt}})
        End Function
        Public Shared Function MakeWarden(ByVal encryptedData As Byte()) As BnetPacket
            Contract.Requires(encryptedData IsNot Nothing)
            Contract.Ensures(Contract.Result(Of BnetPacket)() IsNot Nothing)

            Return New BnetPacket(BnetPacketId.Warden, New Dictionary(Of String, Object) From {
                    {"encrypted data", encryptedData}})
        End Function
#End Region

#Region "Jars"
        Public NotInheritable Class TextHexValueJar
            Inherits Jar(Of ULong)
            Private ReadOnly numDigits As Integer
            Private ReadOnly byteOrder As ByteOrder

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(numDigits > 0)
            End Sub

            Public Sub New(ByVal name As String,
                           ByVal numDigits As Integer,
                           Optional ByVal byteOrder As ByteOrder = byteOrder.LittleEndian)
                MyBase.New(name)
                Contract.Requires(name IsNot Nothing)
                Contract.Requires(numDigits > 0)
                Contract.Requires(numDigits <= 16)
                Contract.Requires(byteOrder.EnumValueIsDefined())
                Me.numDigits = numDigits
                Me.byteOrder = byteOrder
            End Sub

            Public Overrides Function Pack(Of TValue As ULong)(ByVal value As TValue) As IPickle(Of TValue)
                Dim u = CULng(value)
                Dim digits(0 To numDigits - 1) As Char
                Select Case byteOrder
                    Case byteOrder.BigEndian
                        For i = numDigits - 1 To 0 Step -1
                            digits(i) = Hex(u And CULng(&HF)).ToLowerInvariant()(0)
                            u >>= 4
                        Next i
                    Case byteOrder.LittleEndian
                        For i = 0 To numDigits - 1
                            digits(i) = Hex(u And CULng(&HF)).ToLowerInvariant()(0)
                            u >>= 4
                        Next i
                    Case Else
                        Throw byteOrder.MakeImpossibleValueException()
                End Select

                Return New Pickling.Pickle(Of TValue)(Me.Name, value, New String(digits).ToAscBytes().ToView())
            End Function

            Public Overrides Function Parse(ByVal data As ViewableList(Of Byte)) As IPickle(Of ULong)
                If data.Length < numDigits Then Throw New PicklingException("Not enough data")
                data = data.SubView(0, numDigits)
                Return New Pickling.Pickle(Of ULong)(Me.Name, data.ParseChrString(nullTerminated:=False).FromHexStringToBytes(byteOrder), data)
            End Function
        End Class

        Public NotInheritable Class CDKeyJar
            Inherits Pickling.Jars.TupleJar

            Public Sub New(ByVal name As String)
                MyBase.New(name,
                        New UInt32Jar("length").Weaken,
                        New ArrayJar("product key", 4).Weaken,
                        New ArrayJar("public key", 4).Weaken,
                        New UInt32Jar("unknown").Weaken,
                        New ArrayJar("hash", 20).Weaken)
                Contract.Requires(name IsNot Nothing)
            End Sub

            Public Shared Function PackCDKey(ByVal key As String,
                                             ByVal clientToken As ViewableList(Of Byte),
                                             ByVal serverToken As ViewableList(Of Byte)) As Dictionary(Of String, Object)
                Contract.Requires(key IsNot Nothing)
                Contract.Requires(clientToken IsNot Nothing)
                Contract.Requires(serverToken IsNot Nothing)

                Dim cdkey = New Bnet.Crypt.CDKey(key)
                Return New Dictionary(Of String, Object) From {
                        {"length", CUInt(key.Length)},
                        {"product key", cdkey.ProductKey.ToArray()},
                        {"public key", cdkey.PublicKey.ToArray()},
                        {"unknown", 0},
                        {"hash", Bnet.Crypt.SHA1(Concat(clientToken.ToArray,
                                                        serverToken.ToArray,
                                                        cdkey.ProductKey.ToArray,
                                                        cdkey.PublicKey.ToArray,
                                                        cdkey.PrivateKey.ToArray))}}
            End Function

            Public Shared Function PackBorrowedCDKey(ByVal data() As Byte) As Dictionary(Of String, Object)
                Contract.Requires(data IsNot Nothing)
                Contract.Requires(data.Length = 36)

                Return New Dictionary(Of String, Object) From {
                        {"length", data.SubArray(0, 4).ToUInt32()},
                        {"product key", data.SubArray(4, 4)},
                        {"public key", data.SubArray(8, 4)},
                        {"unknown", data.SubArray(12, 4).ToUInt32()},
                        {"hash", data.SubArray(16, 20)}}
            End Function
        End Class
#End Region
    End Class
End Namespace