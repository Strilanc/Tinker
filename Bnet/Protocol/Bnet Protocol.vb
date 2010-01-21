'Tinker - Warcraft 3 game hosting bot
'Copyright (C) 2010 Craig Gidney
'
'This program is free software: you can redistribute it and/or modify
'it under the terms of the GNU General Public License as published by
'the Free Software Foundation, either version 3 of the License, or
'(at your option) any later version.
'
'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License for more details.
'You should have received a copy of the GNU General Public License
'along with this program.  If not, see http://www.gnu.org/licenses/

Imports Tinker.Pickling

Namespace Bnet.Protocol
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
        Ok = 0
        NotFound = 1
        IncorrectPassword = 2
        Full = 3
        AlreadyStarted = 4
        TooManyServerRequests = 6
    End Enum
    Public Enum ClanRank As Byte
        NewInitiate = 0
        Initiate = 1
        Member = 2
        Officer = 3
        Leader = 4
    End Enum

    Public Class SimplePacketDefinition
        Inherits TupleJar
        Public ReadOnly id As PacketId
        Public Sub New(ByVal id As PacketId, ByVal ParamArray subjars() As IJar(Of Object))
            MyBase.New(id.ToString, subjars)
            Contract.Requires(subjars IsNot Nothing)
            Me.id = id
        End Sub
    End Class

    Public NotInheritable Class ServerPackets
        Public Const PacketPrefixValue As Byte = &HFF
        Private Sub New()
        End Sub

        Public Shared ReadOnly ProgramAuthenticationBegin As New SimplePacketDefinition(PacketId.ProgramAuthenticationBegin,
                New EnumUInt32Jar(Of ProgramAuthenticationBeginLogOnType)("logon type").Weaken,
                New UInt32Jar("server cd key salt", showHex:=True).Weaken,
                New UInt32Jar("udp value", showHex:=True).Weaken,
                New FileTimeJar("mpq filetime").Weaken,
                New NullTerminatedStringJar("revision check seed").Weaken,
                New NullTerminatedStringJar("revision check challenge").Weaken,
                New RawDataJar("server signature", Size:=128).Weaken)
        Public Shared ReadOnly ProgramAuthenticationFinish As New SimplePacketDefinition(PacketId.ProgramAuthenticationFinish,
                New EnumUInt32Jar(Of ProgramAuthenticationFinishResult)("result").Weaken,
                New NullTerminatedStringJar("info").Weaken)
        Public Shared ReadOnly UserAuthenticationBegin As New SimplePacketDefinition(PacketId.UserAuthenticationBegin,
                New EnumUInt32Jar(Of UserAuthenticationBeginResult)("result").Weaken,
                New RawDataJar("account password salt", Size:=32).Weaken,
                New RawDataJar("server public key", Size:=32).Weaken)
        Public Shared ReadOnly UserAuthenticationFinish As New SimplePacketDefinition(PacketId.UserAuthenticationFinish,
                New EnumUInt32Jar(Of UserAuthenticationFinishResult)("result").Weaken,
                New RawDataJar("server password proof", Size:=20).Weaken,
                New NullTerminatedStringJar("custom error info").Weaken)
        Public Shared ReadOnly RequiredWork As New SimplePacketDefinition(PacketId.RequiredWork,
                New NullTerminatedStringJar("filename").Weaken)

        Public Shared ReadOnly ChatEvent As New SimplePacketDefinition(PacketId.ChatEvent,
                New EnumUInt32Jar(Of ChatEventId)("event id").Weaken,
                New UInt32Jar("flags", showhex:=True).Weaken,
                New UInt32Jar("ping").Weaken,
                New IPAddressJar("ip").Weaken,
                New UInt32Jar("acc#", showhex:=True).Weaken,
                New UInt32Jar("authority", showhex:=True).Weaken,
                New NullTerminatedStringJar("username").Weaken,
                New NullTerminatedStringJar("text").Weaken)
        Public Shared ReadOnly MessageBox As New SimplePacketDefinition(PacketId.MessageBox,
                New UInt32Jar("style").Weaken,
                New NullTerminatedStringJar("text").Weaken,
                New NullTerminatedStringJar("caption").Weaken)
        Public Shared ReadOnly FriendsUpdate As New SimplePacketDefinition(PacketId.FriendsUpdate,
                New ByteJar("entry number").Weaken,
                New ByteJar("location id").Weaken,
                New ByteJar("status").Weaken,
                New DwordStringJar("product id").Weaken,
                New NullTerminatedStringJar("location").Weaken)
        Public Shared ReadOnly QueryGamesList As New QueryGamesListResponseJar()

        Public Shared ReadOnly EnterChat As New SimplePacketDefinition(PacketId.EnterChat,
                New NullTerminatedStringJar("chat username").Weaken,
                New NullTerminatedStringJar("statstring").Weaken,
                New NullTerminatedStringJar("account username").Weaken)
        Public Shared ReadOnly CreateGame3 As New SimplePacketDefinition(PacketId.CreateGame3,
                New UInt32Jar("result").Weaken)

        Public Shared ReadOnly Null As New SimplePacketDefinition(PacketId.Null)
        Public Shared ReadOnly Ping As New UInt32Jar("salt", showHex:=True)
        Public Shared ReadOnly Warden As New SimplePacketDefinition(PacketId.Warden,
                New RemainingDataJar("encrypted data").Weaken)

        Public Shared ReadOnly GetFileTime As New SimplePacketDefinition(PacketId.GetFileTime,
                New UInt32Jar("request id").Weaken,
                New UInt32Jar("unknown").Weaken,
                New FileTimeJar("filetime").Weaken,
                New NullTerminatedStringJar("filename").Weaken)
        Public Shared ReadOnly GetIconData As New SimplePacketDefinition(PacketId.GetIconData,
                New FileTimeJar("filetime").Weaken,
                New NullTerminatedStringJar("filename").Weaken)
        Public Shared ReadOnly ClanInfo As New SimplePacketDefinition(PacketId.ClanInfo,
                New ByteJar("unknown").Weaken,
                New DwordStringJar("clan tag").Weaken,
                New EnumByteJar(Of ClanRank)("rank").Weaken)
    End Class

    Public NotInheritable Class ClientPackets
        Public Const PacketPrefixValue As Byte = &HFF
        Private Sub New()
        End Sub

        Public Shared ReadOnly ProgramAuthenticationBegin As New SimplePacketDefinition(PacketId.ProgramAuthenticationBegin,
                New UInt32Jar("protocol").Weaken,
                New DwordStringJar("platform").Weaken,
                New DwordStringJar("product").Weaken,
                New UInt32Jar("product major version").Weaken,
                New FixedSizeStringJar("product language", Size:=4).Weaken,
                New IPAddressJar("internal ip").Weaken,
                New UInt32Jar("time zone offset").Weaken,
                New UInt32Jar("location id").Weaken,
                New EnumUInt32Jar(Of MPQ.LanguageId)("language id").Weaken,
                New NullTerminatedStringJar("country abrev").Weaken,
                New NullTerminatedStringJar("country name").Weaken)
        Public Shared ReadOnly ProgramAuthenticationFinish As New SimplePacketDefinition(PacketId.ProgramAuthenticationFinish,
                New UInt32Jar("client cd key salt", showHex:=True).Weaken,
                New RawDataJar("exe version", Size:=4).Weaken,
                New UInt32Jar("revision check response", showHex:=True).Weaken,
                New UInt32Jar("# cd keys").Weaken,
                New UInt32Jar("is spawn").Weaken,
                New ProductCredentialsJar("ROC cd key").Weaken,
                New ProductCredentialsJar("TFT cd key").Weaken,
                New NullTerminatedStringJar("exe info").Weaken,
                New NullTerminatedStringJar("owner").Weaken)
        Public Shared ReadOnly UserAuthenticationBegin As New SimplePacketDefinition(PacketId.UserAuthenticationBegin,
                New RawDataJar("client public key", Size:=32).Weaken,
                New NullTerminatedStringJar("username").Weaken)
        Public Shared ReadOnly UserAuthenticationFinish As New SimplePacketDefinition(PacketId.UserAuthenticationFinish,
                New RawDataJar("client password proof", Size:=20).Weaken)

        Public Const MaxChatCommandTextLength As Integer = 222
        Public Shared ReadOnly ChatCommand As New SimplePacketDefinition(PacketId.ChatCommand,
                New NullTerminatedStringJar("text").Weaken)
        Public Shared ReadOnly QueryGamesList As New SimplePacketDefinition(PacketId.QueryGamesList,
                New EnumUInt32Jar(Of WC3.GameTypes)("filter").Weaken,
                New EnumUInt32Jar(Of WC3.GameTypes)("filter mask").Weaken,
                New UInt32Jar("unknown0").Weaken,
                New UInt32Jar("list count").Weaken,
                New NullTerminatedStringJar("game name").Weaken,
                New NullTerminatedStringJar("game password").Weaken,
                New NullTerminatedStringJar("game stats").Weaken) '[empty game name means list games]

        Public Shared ReadOnly EnterChat As New SimplePacketDefinition(PacketId.EnterChat,
                New NullTerminatedStringJar("username").Weaken,
                New NullTerminatedStringJar("statstring").Weaken) '[both parameters are unused in wc3]
        Public Shared ReadOnly CreateGame3 As New SimplePacketDefinition(PacketId.CreateGame3,
                New EnumUInt32Jar(Of GameStates)("game state").Weaken,
                New UInt32Jar("seconds since creation").Weaken,
                New EnumUInt32Jar(Of WC3.GameTypes)("game type").Weaken,
                New UInt32Jar("unknown1=1023").Weaken,
                New UInt32Jar("is ladder").Weaken,
                New NullTerminatedStringJar("name").Weaken,
                New NullTerminatedStringJar("password").Weaken,
                New TextHexValueJar("num free slots", numdigits:=1).Weaken,
                New TextHexValueJar("game id", numdigits:=8).Weaken,
                New WC3.GameStatsJar("statstring").Weaken)
        Public Shared ReadOnly CloseGame3 As New SimplePacketDefinition(PacketId.CloseGame3)
        Public Shared ReadOnly JoinChannel As New SimplePacketDefinition(PacketId.JoinChannel,
                New EnumUInt32Jar(Of JoinChannelType)("join type").Weaken,
                New NullTerminatedStringJar("channel").Weaken)
        Public Shared ReadOnly NetGamePort As New SimplePacketDefinition(PacketId.NetGamePort,
                New UInt16Jar("port").Weaken)

        Public Shared ReadOnly Null As New SimplePacketDefinition(PacketId.Null)
        Public Shared ReadOnly Ping As New SimplePacketDefinition(PacketId.Ping,
                New UInt32Jar("salt", showhex:=True).Weaken)
        Public Shared ReadOnly Warden As New SimplePacketDefinition(PacketId.Warden,
                New RemainingDataJar("encrypted data").Weaken)

        Public Shared ReadOnly GetFileTime As New SimplePacketDefinition(PacketId.GetFileTime,
                New UInt32Jar("request id").Weaken,
                New UInt32Jar("unknown").Weaken,
                New NullTerminatedStringJar("filename").Weaken)
        Public Shared ReadOnly GetIconData As New SimplePacketDefinition(PacketId.GetIconData)
    End Class
End Namespace
