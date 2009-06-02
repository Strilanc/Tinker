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

Imports HostBot.Pickling
Imports HostBot.Pickling.Jars
Imports HostBot.Warcraft3

Namespace Bnet
    '''<summary>Header values for packets to/from BNET</summary>
    '''<source>BNETDocs.org</source>
    Public Enum BnetPacketID As Byte
        NULL = &H0
        CLOSE_GAME_3 = &H2
        SERVER_LIST = &H4
        CLIENT_ID = &H5
        START_VERSIONING = &H6
        REPORT_VERSION = &H7
        START_ADVEX = &H8
        GET_ADV_LIST_EX = &H9
        ENTER_CHAT = &HA
        GET_CHANNEL_LIST = &HB
        JOIN_CHANNEL = &HC
        CHAT_COMMAND = &HE
        CHAT_EVENT = &HF
        LEAVE_CHAT = &H10
        LOCALE_INFO = &H12
        FLOOD_DETECTED = &H13
        UDPPING_RESPONSE = &H14
        CHECK_AD = &H15
        CLICK_AD = &H16
        REGISTRY = &H18
        MESSAGE_BOX = &H19
        START_ADVEX_2 = &H1A
        GAME_DATA_ADDRESS = &H1B
        CREATE_GAME_3 = &H1C
        LOGON_CHALLENGE_EX = &H1D
        CLIENT_ID_2 = &H1E
        LEAVE_GAME = &H1F
        DISPLAY_AD = &H21
        NOTIFY_JOIN = &H22
        PING = &H25
        READ_USER_DATA = &H26
        WRITE_USER_DATA = &H27
        LOGON_CHALLENGE = &H28
        LOGON_RESPONSE = &H29
        CREATE_ACCOUNT = &H2A
        SYSTEM_INFO = &H2B
        GAME_RESULT = &H2C
        GET_ICON_DATA = &H2D
        GET_LADDER_DATA = &H2E
        FIND_LADDER_USER = &H2F
        CD_KEY = &H30
        CHANGE_PASSWORD = &H31
        CHECK_DATA_FILE = &H32
        GET_FILE_TIME = &H33
        QUERY_REALMS = &H34
        PROFILE = &H35
        CD_KEY_2 = &H36
        LOGON_RESPONSE_2 = &H3A
        CHECK_DATA_FILE_2 = &H3C
        CREATE_ACCOUNT_2 = &H3D
        LOGON_REALM_EX = &H3E
        START_VERSIONING_2 = &H3F
        QUERY_REALMS_2 = &H40
        QUERY_AD_URL = &H41
        WARCRAFT_GENERAL = &H44
        NET_GAME_PORT = &H45
        NEWS_INFO = &H46
        OPTIONAL_WORK = &H4A
        EXTRA_WORK = &H4B
        REQUIRED_WORK = &H4C
        TOURNAMENT = &H4E
        AUTHENTICATION_BEGIN = &H50
        AUTHENTICATION_FINISH = &H51
        ACCOUNT_CREATE = &H52
        ACCOUNT_LOGON_BEGIN = &H53
        ACCOUNT_LOGON_FINISH = &H54
        ACCOUNT_CHANGE = &H55
        ACCOUNT_CHANGE_PROOF = &H56
        ACCOUNT_UPGRADE = &H57
        ACCOUNT_UPGRADE_PROOF = &H58
        ACCOUNT_SET_EMAIL = &H59
        ACCOUNT_RESET_PASSWORD = &H5A
        ACCOUNT_CHANGE_EMAIL = &H5B
        SWITCH_PRODUCT = &H5C
        WARDEN = &H5E
        GAME_PLAYER_SEARCH = &H60
        FRIENDS_LIST = &H65
        FRIENDS_UPDATE = &H66
        FRIENDS_ADD = &H67
        FRIENDS_REMOVE = &H68
        FRIENDS_POSITION = &H69
        CLAN_FIND_CANDIDATES = &H70
        CLAN_INVITE_MULTIPLE = &H71
        CLAN_CREATION_INVITATION = &H72
        CLAN_DISBAND = &H73
        CLAN_MAKE_CHIEFTAIN = &H74
        CLAN_INFO = &H75
        CLAN_QUIT_NOTIFY = &H76
        CLAN_INVITATION = &H77
        CLAN_REMOVE_MEMBER = &H78
        CLAN_INVITATION_RESPONSE = &H79
        CLAN_RANK_CHANGE = &H7A
        CLAN_SET_MOTD = &H7B
        CLAN_MOTD = &H7C
        CLAN_MEMBER_LIST = &H7D
        CLAN_MEMBER_REMOVED = &H7E
        CLAN_MEMBER_STATUS_CHANGE = &H7F
        CLAN_MEMBER_RANK_CHANGE = &H81
        CLAN_MEMBER_INFORMATION = &H82
    End Enum

    Public Class BnetPacket
#Region "Members"
        Public Const PACKET_PREFIX As Byte = &HFF
        Public ReadOnly payload As IPickle
        Public ReadOnly id As BnetPacketID
        Private Shared ReadOnly packet_jar As ManualSwitchJar = MakeBnetPacketJar()
#End Region

#Region "New"
        Private Sub New(ByVal id As BnetPacketID, ByVal payload As IPickle)
            If Not (payload IsNot Nothing) Then Throw New ArgumentNullException()

            Me.payload = payload
            Me.id = id
        End Sub
        Private Sub New(ByVal id As BnetPacketID, ByVal val As Object)
            Me.New(id, packet_jar.pack(id, val))
        End Sub
#End Region

#Region "Jar"
        Private Shared Sub regPack(ByVal jar As ManualSwitchJar, ByVal id As BnetPacketID, ByVal ParamArray subjars() As IJar)
            jar.regPacker(id, New TupleJar(id.ToString(), subjars))
        End Sub
        Private Shared Sub regParse(ByVal jar As ManualSwitchJar, ByVal id As BnetPacketID, ByVal ParamArray subjars() As IJar)
            jar.regParser(id, New TupleJar(id.ToString(), subjars))
        End Sub
        Private Shared Sub reg_login(ByVal jar As ManualSwitchJar)
            'AUTHENTICATION_BEGIN [Introductions, server authentication, and server challenge to client]
            '[client send] [the client introduces itself]
            regPack(jar, BnetPacketID.AUTHENTICATION_BEGIN,
                    New ValueJar("protocol", 4),
                    New StringJar("platform", False, True, 4),
                    New StringJar("product", False, True, 4),
                    New ValueJar("product version", 4),
                    New StringJar("product language", False, , 4),
                    New ArrayJar("internal ip", 4),
                    New ValueJar("time zone offset", 4),
                    New ValueJar("location id", 4),
                    New ValueJar("language id", 4),
                    New StringJar("country abrev"),
                    New StringJar("country name"))
            '[client receive] [the server introduces itself, authenticates itself, and challenges the client for authentication]
            regParse(jar, BnetPacketID.AUTHENTICATION_BEGIN,
                    New ValueJar("logon type", 4, "2=war3"),
                    New ArrayJar("server cd key salt", 4),
                    New ArrayJar("udp value", 4),
                    New ArrayJar("mpq file time", 8),
                    New StringJar("mpq number string"),
                    New StringJar("mpq hash challenge"),
                    New ArrayJar("server signature", 128))

            'AUTHENTICATION_FINISH [Client authentication]
            '[client send] [the client authenticates itself against the server's challenge]
            regPack(jar, BnetPacketID.AUTHENTICATION_FINISH,
                    New ArrayJar("client cd key salt", 4),
                    New ArrayJar("exe version", 4),
                    New ArrayJar("mpq challenge response", 4),
                    New ValueJar("# cd keys", 4),
                    New ValueJar("spawn [unused]", 4, "0=false, 1=true"),
                    New CDKeyJar("ROC cd key"),
                    New CDKeyJar("TFT cd key"),
                    New StringJar("exe info"),
                    New StringJar("owner"))
            '[client receive] [the server informs the client whether or not it was authenticated]
            regParse(jar, BnetPacketID.AUTHENTICATION_FINISH,
                    New ValueJar("result", 4, "0=passed, 1 to 255=invalid code, " _
                                                + "0x100=old version, 0x101=invalid version, 0x102=future version, " _
                                                + "0x200=invalid cd key, 0x201=used cd key, 0x202=banned cd key, 0x203=wrong product"),
                    New StringJar("info"))

            'ACCOUNT_LOGON_BEGIN [Account check and cryptographic key transfer]
            '[client send]
            regPack(jar, BnetPacketID.ACCOUNT_LOGON_BEGIN,
                    New ArrayJar("client public key", 32),
                    New StringJar("username"))
            '[client receive]
            regParse(jar, BnetPacketID.ACCOUNT_LOGON_BEGIN,
                    New ValueJar("result", 4, "0=continue, 1=invalid username, 5=upgrade"),
                    New ArrayJar("account password salt", 32),
                    New ArrayJar("server public key", 32))

            'ACCOUNT_LOGON_FINISH [exchange of password proofs]
            '[client receive]
            regParse(jar, BnetPacketID.ACCOUNT_LOGON_FINISH,
                    New ValueJar("result", 4, "0=successful, 2=incorrect password, 14=need email, 15=custom error"),
                    New ArrayJar("server password proof", 20),
                    New StringJar("custom error info"))
            '[client send]
            regPack(jar, BnetPacketID.ACCOUNT_LOGON_FINISH,
                    New ArrayJar("client password proof", 20))


            'ENTER_CHAT [Request/response for entering chat]
            '[client send]
            regPack(jar, BnetPacketID.ENTER_CHAT,
                    New StringJar("username", , , , "[unused]"),
                    New StringJar("statstring", , , , "[unused]"))
            '[client receive]
            regParse(jar, BnetPacketID.ENTER_CHAT,
                    New StringJar("chat username"),
                    New StringJar("statstring", , True),
                    New StringJar("account username"))
        End Sub
        Private Shared Sub reg_state(ByVal jar As ManualSwitchJar)
            'CREATE_GAME_3 [Request/response for listing a game]
            '[client send]
            regPack(jar, BnetPacketID.CREATE_GAME_3,
                    New ArrayJar("flags", 4),
                    New ValueJar("time", 4, "time since creation in seconds"),
                    New ArrayJar("game type", 2),
                    New ArrayJar("parameter", 2),
                    New ValueJar("unknown1", 4, "=1023"),
                    New ValueJar("ladder", 4, "0=false, 1=true)"),
                    New StringJar("name"),
                    New StringJar("password", , , , "="""" [unused]"),
                    New StringJar("free slots (hex)", False, , 1),
                    New StringJar("host count (hex)", False, True, 8),
                    W3Map.makeStatStringParser())
            '[client receive]
            regParse(jar, BnetPacketID.CREATE_GAME_3,
                    New ValueJar("result", 4, "0=success"))

            'CLOSE_GAME_3 [Client requests server close the client's listed game]
            '[client send]
            regPack(jar, BnetPacketID.CLOSE_GAME_3)

            'JOIN_CHANNEL [Client requests joining a channel]
            '[client send]
            regPack(jar, BnetPacketID.JOIN_CHANNEL,
                    New ArrayJar("flags", 4, , , "0=no create, 1=first join, 2=forced join, 3=diablo2 join"),
                    New StringJar("channel"))

            'NET_GAME_PORT [Client tells server what port it will listen for other clients on when hosting games]
            '[client send]
            regPack(jar, BnetPacketID.NET_GAME_PORT,
                    New ValueJar("port", 2))
        End Sub
        Private Shared Sub reg_misc(ByVal jar As ManualSwitchJar)
            'CHAT_EVENT [Informs the client what other clients are doing (talking, leaving, etc)]
            '[client receive]
            regParse(jar, BnetPacketID.CHAT_EVENT,
                    New ValueJar("event id", 4),
                    New ArrayJar("flags", 4),
                    New ValueJar("ping", 4),
                    New ArrayJar("ip", 4, , , "[unused]"),
                    New ArrayJar("acc#", 4, , , "[unused]"),
                    New ArrayJar("authority", 4, , , "[unused]"),
                    New StringJar("username"),
                    New StringJar("text"))

            'NULL [ignored keep-alive packet]
            '[client receive]
            regParse(jar, BnetPacketID.NULL)
            '[client send]
            regPack(jar, BnetPacketID.NULL)

            'PING [sent periodically by server]
            '[client receive]
            regParse(jar, BnetPacketID.PING,
                    New ArrayJar("salt", 4))
            '[client send]
            regPack(jar, BnetPacketID.PING,
                    New ArrayJar("salt", 4))

            'MESSAGE_BOX [server yells at client]
            '[client receive]
            regParse(jar, BnetPacketID.MESSAGE_BOX,
                    New ValueJar("style", 4),
                    New StringJar("text"),
                    New StringJar("caption"))

            'CHAT_COMMAND [client sends text to server]
            '[client send]
            regPack(jar, BnetPacketID.CHAT_COMMAND,
                    New StringJar("text"))

            'FRIENDS_UPDATE
            '[client receive]
            regParse(jar, BnetPacketID.FRIENDS_UPDATE,
                    New ValueJar("entry number", 1),
                    New ValueJar("location id", 1),
                    New ValueJar("status", 1),
                    New StringJar("product id", False, True, 4),
                    New StringJar("location"))

            'REQUIRED_WORKD
            '[client receive]
            regParse(jar, BnetPacketID.REQUIRED_WORK,
                    New StringJar("filename"))

            'WARDEN
            regParse(jar, BnetPacketID.WARDEN, New ArrayJar("encrypted data", , , True))
            regPack(jar, BnetPacketID.WARDEN, New ArrayJar("encrypted data", , , True))
        End Sub
        Public Shared Function MakeBnetPacketJar() As ManualSwitchJar
            Dim jar As New ManualSwitchJar
            reg_login(jar)
            reg_misc(jar)
            reg_state(jar)
            Return jar
        End Function
#End Region

#Region "Enums"
        Public Enum CHAT_EVENT_ID
            SHOW_USER = &H1
            USER_JOINED = &H2
            USER_LEFT = &H3
            WHISPER = &H4
            TALK = &H5
            BROADCAST = &H6
            CHANNEL = &H7
            USER_FLAGS = &H9
            WHISPER_SENT = &HA
            CHANNEL_FULL = &HD
            CHANNEL_DOES_NOT_EXIST = &HE
            CHANNEL_RESTRICTED = &HF
            INFO = &H12
            ERRORS = &H13
            EMOTE = &H17
        End Enum
#End Region

#Region "Packers: Logon"
        Public Shared Function MakePacket_AUTHENTICATION_BEGIN(ByVal version As UInteger, ByVal local_ip() As Byte) As BnetPacket
            Dim vals As New Dictionary(Of String, Object)
            vals("protocol") = CUInt(0)
            vals("platform") = "IX86"
            vals("product") = "W3XP"
            vals("product version") = version
            vals("product language") = "SUne"
            vals("internal ip") = local_ip
            vals("time zone offset") = CUInt(240)
            vals("location id") = CUInt(1033)
            vals("language id") = CUInt(1033)
            vals("country abrev") = "USA"
            vals("country name") = "United States"
            Return New BnetPacket(BnetPacketID.AUTHENTICATION_BEGIN, vals)
        End Function
        Public Shared Function MakePacket_AUTHENTICATION_FINISH( _
                    ByVal version As Byte(),
                    ByVal mpq_folder As String,
                    ByVal mpq_number_string As String,
                    ByVal mpq_hash_challenge As String,
                    ByVal server_cd_key_salt As Byte(),
                    ByVal cd_key_owner As String,
                    ByVal exe_information As String,
                    ByVal rocKey As String,
                    ByVal tftKey As String) As BnetPacket
            If Not (version IsNot Nothing) Then Throw New ArgumentNullException()
            If Not (mpq_folder IsNot Nothing) Then Throw New ArgumentNullException()
            If Not (mpq_number_string IsNot Nothing) Then Throw New ArgumentNullException()
            If Not (mpq_hash_challenge IsNot Nothing) Then Throw New ArgumentNullException()
            If Not (server_cd_key_salt IsNot Nothing) Then Throw New ArgumentNullException()
            If Not (cd_key_owner IsNot Nothing) Then Throw New ArgumentNullException()
            If Not (exe_information IsNot Nothing) Then Throw New ArgumentNullException()
            If Not (rocKey IsNot Nothing) Then Throw New ArgumentNullException()
            If Not (tftKey IsNot Nothing) Then Throw New ArgumentNullException()

            Dim vals As New Dictionary(Of String, Object)

            Dim client_cd_key_salt(0 To 3) As Byte
            With New System.Random()
                .NextBytes(client_cd_key_salt)
            End With

            vals("client cd key salt") = client_cd_key_salt
            vals("exe version") = version
            vals("mpq challenge response") = Bnet.Crypt.generateRevisionCheck(mpq_folder, mpq_number_string, mpq_hash_challenge).bytes(ByteOrder.LittleEndian)
            vals("# cd keys") = CUInt(2)
            vals("spawn [unused]") = CUInt(0)
            vals("ROC cd key") = CDKeyJar.packCDKey(rocKey, client_cd_key_salt, server_cd_key_salt)
            vals("TFT cd key") = CDKeyJar.packCDKey(tftKey, client_cd_key_salt, server_cd_key_salt)
            vals("exe info") = exe_information
            vals("owner") = cd_key_owner

            Return New BnetPacket(BnetPacketID.AUTHENTICATION_FINISH, vals)
        End Function
        Public Shared Function MakePacket_ACCOUNT_LOGON_BEGIN( _
                    ByVal username As String,
                    ByVal client_public_key As Byte()) As BnetPacket
            Dim vals As New Dictionary(Of String, Object)
            vals("client public key") = client_public_key
            vals("username") = username
            Return New BnetPacket(BnetPacketID.ACCOUNT_LOGON_BEGIN, vals)
        End Function
        Public Shared Function MakePacket_ACCOUNT_LOGON_FINISH(ByVal client_password_proof As Byte()) As BnetPacket
            Dim vals As New Dictionary(Of String, Object)
            vals("client password proof") = client_password_proof
            Return New BnetPacket(BnetPacketID.ACCOUNT_LOGON_FINISH, vals)
        End Function
        Public Shared Function MakePacket_ENTER_CHAT() As BnetPacket
            Dim vals As New Dictionary(Of String, Object)
            vals("username") = ""
            vals("statstring") = ""
            Return New BnetPacket(BnetPacketID.ENTER_CHAT, vals)
        End Function
#End Region
#Region "Packers: State"
        Public Shared Function MakePacket_NET_GAME_PORT(ByVal port As UShort) As BnetPacket
            Dim vals As New Dictionary(Of String, Object)
            vals("port") = port
            Return New BnetPacket(BnetPacketID.NET_GAME_PORT, vals)
        End Function
        Public Shared Function MakePacket_JOIN_CHANNEL(ByVal flags As UInteger, ByVal channel As String) As BnetPacket
            Dim vals As New Dictionary(Of String, Object)
            vals("flags") = flags.bytes(ByteOrder.LittleEndian)
            vals("channel") = channel
            Return New BnetPacket(BnetPacketID.JOIN_CHANNEL, vals)
        End Function
        Public Shared Function MakePacket_CREATE_GAME_3( _
                    ByVal username As String,
                    ByVal name As String,
                    ByVal map As W3Map,
                    ByVal map_settings As W3Map.MapSettings,
                    ByVal creation_time As Date,
                    ByVal [private] As Boolean,
                    ByVal host_count As Integer,
                    Optional ByVal full As Boolean = False,
                    Optional ByVal in_progress As Boolean = False,
                    Optional ByVal empty As Boolean = False _
                    ) As BnetPacket
            Const FLAG_PRIVATE As UInteger = &H1
            Const FLAG_FULL As UInteger = &H2
            'Const FLAG_NOT_EMPTY As UInteger = &H4
            Const FLAG_IN_PROGRESS As UInteger = &H8
            Const FLAG_UNKNOWN As UInteger = &H10
            Const MAX_GAME_NAME_LENGTH As UInteger = 31
            If name.Length > MAX_GAME_NAME_LENGTH Then
                Throw New ArgumentException("Game name must be less than 32 characters long.", "name")
            End If

            Dim game_state_flags As UInteger = FLAG_UNKNOWN
            If [private] Then game_state_flags = game_state_flags Or FLAG_PRIVATE
            If full Then game_state_flags = game_state_flags Or FLAG_FULL
            If in_progress Then game_state_flags = game_state_flags Or FLAG_IN_PROGRESS
            'If Not empty Then game_state_flags = game_state_flags Or FLAG_NOT_EMPTY

            Dim vals As New Dictionary(Of String, Object)
            vals("flags") = game_state_flags.bytes(ByteOrder.LittleEndian)
            vals("time") = CUInt((DateTime.Now() - creation_time).TotalSeconds)
            Dim b1 As Byte = &H1 'bnet map |= 0x08
            Dim b2 As Byte = &H40 '&HC0
            Dim b3 As Byte = &H9 '&H2
            Dim b4 As Byte = 0
            If [private] Then b2 = b2 Or CByte(&H8)
            Select Case map_settings.observers
                Case W3Map.OBS.FULL_OBSERVERS, W3Map.OBS.REFEREES
                    b3 = b3 Or CByte(&H10)
                Case W3Map.OBS.OBSERVERS_ON_DEFEAT
                    b3 = b3 Or CByte(&H20)
                Case W3Map.OBS.NO_OBSERVERS
                    b3 = b3 Or CByte(&H40)
            End Select
            vals("game type") = New Byte() {b1, b2}
            vals("parameter") = New Byte() {b3, b4}
            vals("unknown1") = CUInt(1023)
            vals("ladder") = CUInt(0)
            vals("name") = name
            vals("password") = ""

            'free slots in text-hex
            Dim ccSlots(0 To 0) As Char
            ccSlots(0) = Hex(map.numPlayerAndObsSlots(map_settings) - 1).ToLower()(0)
            vals("free slots (hex)") = ccSlots

            'host counter in text-hex
            Dim ccHostCount(0 To 7) As Char
            For i As Integer = 7 To 0 Step -1
                ccHostCount(i) = Hex(host_count And &HF).ToLower()(0)
                host_count >>= 4
            Next i
            vals("host count (hex)") = ccHostCount

            vals("statstring") = map.generateStatStringVals(username, map_settings)
            Return New BnetPacket(BnetPacketID.CREATE_GAME_3, vals)
        End Function
        Public Shared Function MakePacket_CLOSE_GAME_3() As BnetPacket
            Dim vals As New Dictionary(Of String, Object)
            Return New BnetPacket(BnetPacketID.CLOSE_GAME_3, vals)
        End Function
#End Region
#Region "Packers: CKL"
        Public Shared Function CKL_MakePacket_AUTHENTICATION_FINISH( _
                    ByVal version As Byte(),
                    ByVal mpq_folder As String,
                    ByVal mpq_number_string As String,
                    ByVal mpq_hash_challenge As String,
                    ByVal server_cd_key_salt As Byte(),
                    ByVal cd_key_owner As String,
                    ByVal exe_information As String,
                    ByVal remote_CKL_host As String,
                    ByVal remote_CKL_port As UShort) As IFuture(Of Outcome(Of BnetPacket))
            Dim vals As New Dictionary(Of String, Object)

            Dim client_cd_key_salt(0 To 3) As Byte
            With New System.Random()
                .NextBytes(client_cd_key_salt)
            End With

            vals("client cd key salt") = client_cd_key_salt
            vals("exe version") = version
            vals("mpq challenge response") = Bnet.Crypt.generateRevisionCheck(mpq_folder, mpq_number_string, mpq_hash_challenge).bytes(ByteOrder.LittleEndian)
            vals("# cd keys") = CUInt(2)
            vals("spawn [unused]") = CUInt(0)
            vals("exe info") = exe_information
            vals("owner") = cd_key_owner

            Dim f = New Future(Of Outcome(Of BnetPacket))
            FutureSub.Call(New CKL.CKLClient(remote_CKL_host, remote_CKL_port, client_cd_key_salt, server_cd_key_salt).future(),
                           Sub(v) CKL_MakePacket_AUTHENTICATION_FINISH_2(f, vals, v))
            Return f
        End Function
        Private Shared Sub CKL_MakePacket_AUTHENTICATION_FINISH_2( _
                    ByVal f As Future(Of Outcome(Of BnetPacket)),
                    ByVal vals As Dictionary(Of String, Object),
                    ByVal borrowed As Outcome(Of CKL.CKLBorrowedKeyVals))
            If Not borrowed.succeeded Then
                f.SetValue(failure(borrowed.message))
            Else
                vals("ROC cd key") = borrowed.val.roc_key
                vals("TFT cd key") = borrowed.val.tft_key
                f.SetValue(successVal(New BnetPacket(BnetPacketID.AUTHENTICATION_FINISH, vals), borrowed.message))
            End If
        End Sub
#End Region
#Region "Packers: Misc"
        Public Shared Function MakePacket_CHAT_COMMAND(ByVal text As String) As BnetPacket
            If Not (text IsNot Nothing) Then Throw New ArgumentNullException()

            Dim vals As New Dictionary(Of String, Object)
            Const MAX_TEXT_LENGTH As Integer = 222
            If text.Length > MAX_TEXT_LENGTH Then
                text = text.Substring(0, MAX_TEXT_LENGTH)
                'Throw New ArgumentException(String.Format("Text cannot exceed {0} characters.", MAX_TEXT_LENGTH), "text")
            End If
            vals("text") = text
            Return New BnetPacket(BnetPacketID.CHAT_COMMAND, vals)
        End Function
        Public Shared Function MakePacket_PING(ByVal salt As Byte()) As BnetPacket
            If Not (salt IsNot Nothing) Then Throw New ArgumentNullException()
            If Not (salt.Length = 4) Then Throw New ArgumentException()

            Dim vals As New Dictionary(Of String, Object)
            vals("salt") = salt
            Return New BnetPacket(BnetPacketID.PING, vals)
        End Function
        Public Shared Function MakePacket_Warden(ByVal encrypted_data As Byte()) As BnetPacket
            If Not (encrypted_data IsNot Nothing) Then Throw New ArgumentException()

            Dim vals As New Dictionary(Of String, Object)
            vals("encrypted data") = encrypted_data
            Return New BnetPacket(BnetPacketID.WARDEN, vals)
        End Function
#End Region

#Region "Parsing"
        Public Shared Function FromData(ByVal id As BnetPacketID, ByVal data As ImmutableArrayView(Of Byte)) As BnetPacket
            If Not (data IsNot Nothing) Then Throw New ArgumentException()

            Return New BnetPacket(id, packet_jar.parse(id, data))
        End Function
#End Region

#Region "Jars"
        Public Class CDKeyJar
            Inherits Pickling.Jars.TupleJar

            Public Sub New(ByVal name As String)
                MyBase.New(name,
                        New ValueJar("length", 4),
                        New ArrayJar("product key", 4),
                        New ArrayJar("public key", 4),
                        New ValueJar("unknown", 4, "=0"),
                        New ArrayJar("hash", 20))
            End Sub

            Public Shared Function packCDKey(ByVal key As String,
                                             ByVal clientToken As ReadOnlyArrayView(Of Byte),
                                             ByVal serverToken As ReadOnlyArrayView(Of Byte)) As Dictionary(Of String, Object)
                If Not (key IsNot Nothing) Then Throw New ArgumentException()
                If Not (clientToken IsNot Nothing) Then Throw New ArgumentException()
                If Not (serverToken IsNot Nothing) Then Throw New ArgumentException()

                Dim d As New Dictionary(Of String, Object)
                With New Bnet.Crypt.CDKey(key)
                    d("length") = CUInt(key.Length)
                    d("product key") = .productKey
                    d("public key") = .publicKey
                    d("unknown") = CUInt(0)
                    d("hash") = Bnet.Crypt.SHA1(concat(clientToken.ToArray, serverToken.ToArray, .productKey, .publicKey, .privateKey))
                End With
                Return d
            End Function

            Public Shared Function packBorrowedCdKey(ByVal data() As Byte) As Dictionary(Of String, Object)
                If Not (data IsNot Nothing) Then Throw New ArgumentException()

                If data.Length <> 36 Then Throw New ArgumentException("Incorrect amount of data for borrowed cd key.")
                Dim d = New Dictionary(Of String, Object)
                d("length") = subArray(data, 0, 4).ToUInteger(ByteOrder.LittleEndian)
                d("product key") = subArray(data, 4, 4)
                d("public key") = subArray(data, 8, 4)
                d("unknown") = subArray(data, 12, 4).ToUInteger(ByteOrder.LittleEndian)
                d("hash") = subArray(data, 16, 20)
                Return d
            End Function
        End Class
#End Region
    End Class
End Namespace