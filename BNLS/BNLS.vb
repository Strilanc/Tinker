Imports System.Net
Imports System.Net.Sockets

Public Enum BnlsPacketId As Byte
    Null = &H0
    CdKey = &H1
    LogonChallenge = &H2
    LogonProof = &H3
    CreateAccount = &H4
    ChangeChallenge = &H5
    ChangeProof = &H6
    UpgradeChallenge = &H7
    UpgradeProof = &H8
    VersionCheck = &H9
    ConfirmLogon = &HA
    HashData = &HB
    CdKeyEx = &HC
    ChooseNlsRevision = &HD
    Authorize = &HE
    AuthorizeProof = &HF
    RequestVersionByte = &H10
    VerifyServer = &H11
    ReserveServerSlots = &H12
    ServerLogonChallenge = &H13
    ServerLogonProof = &H14
    Reserved0 = &H15
    Reserved1 = &H16
    Reserved2 = &H17
    VersionCheckEx = &H18
    Reserved3 = &H19
    VersionCheckEx2 = &H1A
    Warden = &H7D
End Enum
Public Enum BnlsWardenPacketId As Byte
    WardenSeed = 0
    WardenPacket = 1
    Warden = 2
    WardenCheckIni = 3
End Enum
Public Enum BnlsClientType As UInteger
    Starcraft = &H1
    Broodwar = &H2
    Warcraft2 = &H3
    Diablo2 = &H4
    Diablo2LoD = &H5
    StarcraftJapan = &H6
    Warcraft3RoC = &H7
    Warcraft3TFT = &H8
    Diablo = &H9
    DiabloSWare = &HA
    StarcraftSWare = &HB
End Enum
Public Class BnlsClient
    Private ReadOnly socket As PacketSocket
    Public ReadOnly logger As Logger
    Public Sub New(ByVal client As TcpClient, ByVal seed As UInteger, Optional ByVal logger As Logger = Nothing)
        Me.logger = If(logger, New Logger())
        Me.socket = New PacketSocket(client, 5.Minutes, logger, numByteBeforeSize:=0)
        Me.socket.WritePacket(Concat(Of Byte)({0, 0, BnlsPacketId.Warden},
                                              CUInt(BnlsClientType.Warcraft3TFT).Bytes(ByteOrder.LittleEndian),
                                              4US.Bytes(ByteOrder.LittleEndian), seed.Bytes(ByteOrder.LittleEndian),
                                              {0, 0, 0}))
    End Sub

    '(BYTE)     Useage
    '(DWORD)    Cookie
    'Useage 0x00 (Warden Seed)
    '    (DWORD)    Client
    '    (WORD)     Lengh of Seed
    '    (VOID)     Seed
    '    (STRING)   Username (blank)
    '    (WORD)     Lengh of password
    '    (VOID)     Password
    'Useage 0x01 (warden packet)
    '    (WORD)     Lengh Of Warden Packet
    '    (VOID)     Warden Packet Data
    'Useage 0x02 (warden 0x05)
    '    (DWORD)    Client
    '    (WORD)     Lengh Of Seed
    '    (VOID)     Seed
    '    (DWORD)    Unused
    '    (BYTE[16]) Module MD5 Name
    '    (WORD)     Lengh of Warden 0x05 packet
    '    (VOID)     Warden 0x05 packet
    'Useage 0x03 (warden checks/ini file)
    '    (DWORD)    Client
    '    (DWORD)    Info Type (0x01)
    '    (WORD)     Unused (must be 0x00)
    '    (VOID)     Unused


    '(BYTE)     Useage
    '(DWORD)    Cookie
    '(BYTE)     Result
    '(WORD)     Lengh of data
    '(VOID)     Data
End Class