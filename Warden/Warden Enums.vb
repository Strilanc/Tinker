Namespace Warden
    Public Enum BNLSPacketId As Byte
        Null = &H0
        CDKey = &H1
        LogOnChallenge = &H2
        LogOnProof = &H3
        CreateAccount = &H4
        ChangeChallenge = &H5
        ChangeProof = &H6
        UpgradeChallenge = &H7
        UpgradeProof = &H8
        VersionCheck = &H9
        ConfirmLogOn = &HA
        HashData = &HB
        CDKeyEx = &HC
        ChooseNlsRevision = &HD
        Authorize = &HE
        AuthorizeProof = &HF
        RequestVersionByte = &H10
        VerifyServer = &H11
        ReserveServerSlots = &H12
        ServerLogOnChallenge = &H13
        ServerLogOnProof = &H14
        VersionCheckEx = &H18
        VersionCheckEx2 = &H1A
        Warden = &H7D
    End Enum

    Public Enum WardenPacketId As Byte
        FullServiceConnect = 0
        FullServiceHandleWardenPacket = 1
    End Enum

    Public Enum ClientType As UInteger
        Starcraft = &H1
        BroodWar = &H2
        Warcraft2 = &H3
        Diablo2 = &H4
        Diablo2LordOfDestruction = &H5
        StarcraftJapan = &H6
        Warcraft3ROC = &H7
        Warcraft3TFT = &H8
        Diablo = &H9
        DiabloSWare = &HA
        StarcraftSWare = &HB
    End Enum
End Namespace
