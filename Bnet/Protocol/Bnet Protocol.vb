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

    Public Enum ProgramAuthenticationBeginLogOnType As UInt32
        Warcraft3 = 2
    End Enum
    Public Enum UserAuthenticationFinishResult As UInt32
        Passed = 0
        IncorrectPassword = 2
        NeedEmail = 14
        CustomError = 15
    End Enum
    Public Enum UserAuthenticationBeginResult As UInt32
        Passed = 0
        BadUserName = 1
        UpgradeAccount = 5
    End Enum
    Public Enum ProgramAuthenticationFinishResult As UInt32
        Passed = 0

        OldVersion = &H101
        InvalidVersion = &H102
        FutureVersion = &H103

        InvalidCDKeyROC = &H200
        UsedCDKeyROC = &H201
        BannedCDKeyROC = &H202
        WrongProductROC = &H203

        InvalidCDKeyTFT = &H210
        UsedCDKeyTFT = &H211
        BannedCDKeyTFT = &H212
        WrongProductTFT = &H213
    End Enum
    Public Enum ChatEventId As UInt32
        ShowUser = &H1
        UserJoined = &H2
        UserLeft = &H3
        Whisper = &H4
        Talk = &H5
        Broadcast = &H6
        Channel = &H7
        UserOptions = &H9
        WhisperSent = &HA
        ChannelFull = &HD
        ChannelDoesNotExist = &HE
        ChannelRestricted = &HF
        Info = &H12
        Errors = &H13
        Emote = &H17
    End Enum
    <Flags()>
    Public Enum GameStates As UInt32
        [Private] = 1 << 0
        Full = 1 << 1
        NotEmpty = 1 << 2 'really unsure about this one
        InProgress = 1 << 3
        Unknown0x10 = 1 << 4
    End Enum
    Public Enum JoinChannelType As UInt32
        NoCreate = 0
        FirstJoin = 1
        ForcedJoin = 2
        Diablo2Join = 3
    End Enum
    <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")>
    Public Enum QueryGameResponse As UInt32
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

    Public NotInheritable Class Packets
        Private Sub New()
        End Sub
        Public Const PacketPrefixValue As Byte = &HFF

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

        Private Shared Function Define(Of T)(ByVal id As PacketId, ByVal jar As IJar(Of T)) As Definition(Of T)
            Contract.Requires(jar IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Definition(Of T))() IsNot Nothing)
            Return New Definition(Of T)(id, jar)
        End Function
        Private Shared Function Define(ByVal id As PacketId,
                                       ByVal jar1 As ISimpleNamedJar,
                                       ByVal jar2 As ISimpleNamedJar,
                                       ByVal ParamArray jars() As ISimpleNamedJar) As Definition(Of NamedValueMap)
            Contract.Requires(jar1 IsNot Nothing)
            Contract.Requires(jar2 IsNot Nothing)
            Contract.Requires(jars IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Definition(Of NamedValueMap))() IsNot Nothing)
            Return Define(id, New TupleJar(jars.Prepend(jar1, jar2).ToArray))
        End Function

        Public NotInheritable Class ServerToClient
            Private Sub New()
            End Sub

            Public Shared ReadOnly ProgramAuthenticationBegin As Definition(Of NamedValueMap) = Define(PacketId.ProgramAuthenticationBegin,
                    New EnumUInt32Jar(Of ProgramAuthenticationBeginLogOnType)().Named("logon type"),
                    New UInt32Jar(showHex:=True).Named("server cd key salt"),
                    New UInt32Jar(showHex:=True).Named("udp value"),
                    New FileTimeJar().Named("mpq filetime"),
                    New UTF8Jar().NullTerminated.Named("revision check seed"),
                    New UTF8Jar().NullTerminated.Named("revision check challenge"),
                    New DataJar().Fixed(exactDataCount:=128).Named("server signature"))
            Public Shared ReadOnly ProgramAuthenticationFinish As Definition(Of NamedValueMap) = Define(PacketId.ProgramAuthenticationFinish,
                    New EnumUInt32Jar(Of ProgramAuthenticationFinishResult)().Named("result"),
                    New UTF8Jar().NullTerminated.Named("info"))
            Public Shared ReadOnly UserAuthenticationBegin As Definition(Of NamedValueMap) = Define(PacketId.UserAuthenticationBegin,
                    New EnumUInt32Jar(Of UserAuthenticationBeginResult)().Named("result"),
                    New DataJar().Fixed(exactDataCount:=32).Named("account password salt"),
                    New DataJar().Fixed(exactDataCount:=32).Named("server public key"))
            Public Shared ReadOnly UserAuthenticationFinish As Definition(Of NamedValueMap) = Define(PacketId.UserAuthenticationFinish,
                    New EnumUInt32Jar(Of UserAuthenticationFinishResult)().Named("result"),
                    New DataJar().Fixed(exactDataCount:=20).Named("server password proof"),
                    New UTF8Jar().NullTerminated.Optional.Named("custom error info"))
            Public Shared ReadOnly RequiredWork As Definition(Of String) = Define(PacketId.RequiredWork,
                    New UTF8Jar().NullTerminated.Named("filename"))

            Public Shared ReadOnly ChatEvent As Definition(Of NamedValueMap) = Define(PacketId.ChatEvent,
                    New EnumUInt32Jar(Of ChatEventId)().Named("event id"),
                    New UInt32Jar(showhex:=True).Named("flags"),
                    New UInt32Jar().Named("ping"),
                    New IPAddressJar().Named("ip"),
                    New UInt32Jar(showhex:=True).Named("acc#"),
                    New UInt32Jar(showhex:=True).Named("authority"),
                    New UTF8Jar().NullTerminated.Named("username"),
                    New UTF8Jar().NullTerminated.Named("text"))
            Public Shared ReadOnly MessageBox As Definition(Of NamedValueMap) = Define(PacketId.MessageBox,
                    New UInt32Jar().Named("style"),
                    New UTF8Jar().NullTerminated.Named("text"),
                    New UTF8Jar().NullTerminated.Named("caption"))
            Public Shared ReadOnly FriendsUpdate As Definition(Of NamedValueMap) = Define(PacketId.FriendsUpdate,
                    New ByteJar().Named("entry number"),
                    New ByteJar().Named("location id"),
                    New ByteJar().Named("status"),
                    New ASCIIJar().Reversed.Fixed(exactDataCount:=4).Named("product id"),
                    New UTF8Jar().NullTerminated.Named("location"))
            Public Shared ReadOnly QueryGamesList As Definition(Of QueryGamesListResponse) = Define(PacketId.QueryGamesList,
                    New QueryGamesListResponseJar(New SystemClock()))

            Public Shared ReadOnly EnterChat As Definition(Of NamedValueMap) = Define(PacketId.EnterChat,
                    New UTF8Jar().NullTerminated.Named("chat username"),
                    New UTF8Jar().NullTerminated.Named("statstring"),
                    New UTF8Jar().NullTerminated.Named("account username"))
            Public Shared ReadOnly CreateGame3 As Definition(Of UInt32) = Define(PacketId.CreateGame3,
                    New UInt32Jar().Named("result"))

            Public Shared ReadOnly Null As Definition(Of Object) = Define(PacketId.Null,
                    New EmptyJar())
            Public Shared ReadOnly Ping As Definition(Of UInt32) = Define(PacketId.Ping,
                    New UInt32Jar(showHex:=True).Named("salt"))
            Public Shared ReadOnly Warden As Definition(Of IReadableList(Of Byte)) = Define(PacketId.Warden,
                    New DataJar().Named("encrypted data"))

            Public Shared ReadOnly GetFileTime As Definition(Of NamedValueMap) = Define(PacketId.GetFileTime,
                    New UInt32Jar().Named("request id"),
                    New UInt32Jar().Named("unknown"),
                    New FileTimeJar().Named("filetime"),
                    New UTF8Jar().NullTerminated.Named("filename"))
            Public Shared ReadOnly GetIconData As Definition(Of NamedValueMap) = Define(PacketId.GetIconData,
                    New FileTimeJar().Named("filetime"),
                    New UTF8Jar().NullTerminated.Named("filename"))
            Public Shared ReadOnly ClanInfo As Definition(Of NamedValueMap) = Define(PacketId.ClanInfo,
                    New ByteJar().Named("unknown"),
                    New ASCIIJar().Reversed.Fixed(exactDataCount:=4).Named("clan tag"),
                    New EnumByteJar(Of ClanRank)().Named("rank"))
        End Class

        Public NotInheritable Class ClientToServer
            Private Sub New()
            End Sub

            Public Shared ReadOnly ProgramAuthenticationBegin As Definition(Of NamedValueMap) = Define(PacketId.ProgramAuthenticationBegin,
                    New UInt32Jar().Named("protocol"),
                    New ASCIIJar().Reversed.Fixed(exactDataCount:=4).Named("platform"),
                    New ASCIIJar().Reversed.Fixed(exactDataCount:=4).Named("product"),
                    New UInt32Jar().Named("product major version"),
                    New ASCIIJar().Fixed(exactDataCount:=4).Named("product language"),
                    New IPAddressJar().Named("internal ip"),
                    New UInt32Jar().Named("time zone offset"),
                    New UInt32Jar().Named("location id"),
                    New EnumUInt32Jar(Of MPQ.LanguageId)(checkDefined:=False).Named("language id"),
                    New UTF8Jar().NullTerminated.Named("country abrev"),
                    New UTF8Jar().NullTerminated.Named("country name"))
            Public Shared ReadOnly ProgramAuthenticationFinish As Definition(Of NamedValueMap) = Define(PacketId.ProgramAuthenticationFinish,
                    New UInt32Jar(showHex:=True).Named("client cd key salt"),
                    New DataJar().Fixed(exactDataCount:=4).Named("exe version"),
                    New UInt32Jar(showHex:=True).Named("revision check response"),
                    New UInt32Jar().Named("# cd keys"),
                    New UInt32Jar().Named("is spawn"),
                    New ProductCredentialsJar().Named("ROC cd key"),
                    New ProductCredentialsJar().Named("TFT cd key"),
                    New UTF8Jar().NullTerminated.Named("exe info"),
                    New UTF8Jar().NullTerminated.Named("owner"))
            Public Shared ReadOnly UserAuthenticationBegin As Definition(Of NamedValueMap) = Define(PacketId.UserAuthenticationBegin,
                    New DataJar().Fixed(exactDataCount:=32).Named("client public key"),
                    New UTF8Jar().NullTerminated.Named("username"))
            Public Shared ReadOnly UserAuthenticationFinish As Definition(Of IReadableList(Of Byte)) = Define(PacketId.UserAuthenticationFinish,
                    New DataJar().Fixed(exactDataCount:=20).Named("client password proof"))

            Public Const MaxSerializedChatCommandTextLength As Integer = 223
            Public Const MaxChatCommandTextLength As Integer = MaxSerializedChatCommandTextLength - 1
            Public Shared ReadOnly ChatCommand As Definition(Of String) = Define(PacketId.ChatCommand,
                    New UTF8Jar().NullTerminated.Limited(maxDataCount:=MaxSerializedChatCommandTextLength).Named("text"))
            Public Shared ReadOnly QueryGamesList As Definition(Of NamedValueMap) = Define(PacketId.QueryGamesList,
                    New EnumUInt32Jar(Of WC3.Protocol.GameTypes)().Named("filter"),
                    New EnumUInt32Jar(Of WC3.Protocol.GameTypes)().Named("filter mask"),
                    New UInt32Jar().Named("unknown0"),
                    New UInt32Jar().Named("list count"),
                    New UTF8Jar().NullTerminated.Named("game name"),
                    New UTF8Jar().NullTerminated.Named("game password"),
                    New UTF8Jar().NullTerminated.Named("game stats")) '[empty game name means list games]

            Public Shared ReadOnly EnterChat As Definition(Of NamedValueMap) = Define(PacketId.EnterChat,
                    New UTF8Jar().NullTerminated.Named("username"),
                    New UTF8Jar().NullTerminated.Named("statstring")) '[both parameters are unused in wc3]
            Public Const MaxSerializedGameNameLength As Integer = 32
            Public Const MaxGameNameLength As Integer = MaxSerializedGameNameLength - 1
            Public Shared ReadOnly CreateGame3 As Definition(Of NamedValueMap) = Define(PacketId.CreateGame3,
                    New EnumUInt32Jar(Of GameStates)().Named("game state"),
                    New UInt32Jar().Named("seconds since creation"),
                    New EnumUInt32Jar(Of WC3.Protocol.GameTypes)().Named("game type"),
                    New UInt32Jar().Named("unknown1=1023"),
                    New UInt32Jar().Named("is ladder"),
                    New UTF8Jar().NullTerminated.Limited(MaxSerializedGameNameLength).Named("name"),
                    New UTF8Jar().NullTerminated.Named("password"),
                    New TextHexUInt32Jar(digitCount:=1).Named("num free slots"),
                    New TextHexUInt32Jar(digitCount:=8).Named("game id"),
                    New WC3.Protocol.GameStatsJar().Named("statstring"))
            Public Shared ReadOnly CloseGame3 As Definition(Of Object) = Define(PacketId.CloseGame3,
                    New EmptyJar())
            Public Shared ReadOnly JoinChannel As Definition(Of NamedValueMap) = Define(PacketId.JoinChannel,
                    New EnumUInt32Jar(Of JoinChannelType)().Named("join type"),
                    New UTF8Jar().NullTerminated.Named("channel"))
            Public Shared ReadOnly NetGamePort As Definition(Of UInt16) = Define(PacketId.NetGamePort,
                    New UInt16Jar().Named("port"))

            Public Shared ReadOnly Null As Definition(Of Object) = Define(PacketId.Null,
                    New EmptyJar())
            Public Shared ReadOnly Ping As Definition(Of UInt32) = Define(PacketId.Ping,
                    New UInt32Jar(showhex:=True).Named("salt"))
            Public Shared ReadOnly Warden As Definition(Of IReadableList(Of Byte)) = Define(PacketId.Warden,
                    New DataJar().Named("encrypted data"))

            Public Shared ReadOnly GetFileTime As Definition(Of NamedValueMap) = Define(PacketId.GetFileTime,
                    New UInt32Jar().Named("request id"),
                    New UInt32Jar().Named("unknown"),
                    New UTF8Jar().NullTerminated.Named("filename"))
            Public Shared ReadOnly GetIconData As Definition(Of Object) = Define(PacketId.GetIconData,
                    New EmptyJar())
        End Class
    End Class
End Namespace
