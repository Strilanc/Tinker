Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Tinker
Imports Tinker.WC3
Imports Tinker.WC3.Protocol
Imports Tinker.Bnet.Protocol

Friend Module SharedTestObjects
    Friend ReadOnly TestMap As New Map(
        streamFactory:=Nothing,
        advertisedPath:="Maps\test.w3x",
        filesize:=1,
        fileChecksumCRC32:=1,
        mapChecksumSHA1:=CByte(20).Range(),
        mapChecksumXORO:=1,
        ismelee:=False,
        usesCustomForces:=True,
        usesFixedPlayerSettings:=True,
        name:="Test Map",
        playableWidth:=256,
        playableHeight:=256,
        lobbySlots:=MakeRist(
                New Slot(index:=0,
                         raceunlocked:=False,
                         color:=PlayerColor.Red,
                         team:=0,
                         contents:=New SlotContentsOpen),
                New Slot(index:=1,
                         raceunlocked:=False,
                         color:=PlayerColor.Blue,
                         team:=0,
                         contents:=New SlotContentsOpen)))
    Friend ReadOnly TestArgument As New Tinker.Commands.CommandArgument("")
    Friend ReadOnly TestStats As GameStats = GameStats.FromMapAndArgument(
            map:=TestMap,
            hostName:="StrilancHost",
            argument:=TestArgument)
    Friend ReadOnly TestDesc As New RemoteGameDescription(
            name:="test",
            GameStats:=TestStats,
            location:=Net.IPAddress.Loopback.WithPort(6112),
            gameid:=42,
            entrykey:=0,
            totalslotcount:=12,
            gameType:=GameTypes.AuthenticatedMakerBlizzard,
            state:=GameStates.Private,
            usedSlotCount:=0,
            ageClock:=FixedClock(5.Seconds).StartTimer())
    Friend ReadOnly TestSettings As GameSettings = GameSettings.FromArgument(
            map:=TestMap,
            gameDescription:=TestDesc,
            argument:=TestArgument)
    Friend ReadOnly TestPlayer As Player = Player.MakeFake(
            id:=New PlayerId(1),
            name:="test",
            logger:=New Logger)
End Module
