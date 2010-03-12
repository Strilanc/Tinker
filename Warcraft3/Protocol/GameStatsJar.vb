Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class GameStatsJar
        Inherits BaseJar(Of GameStats)

        <Flags()>
        Private Enum GameSettings As UInteger
            SpeedMedium = 1 << 0
            SpeedFast = 1 << 1

            VisibilityHideTerrain = 1 << 8
            VisibilityExplored = 1 << 9
            VisibilityAlwaysVisible = 1 << 10
            VisibilityDefault = 1 << 11
            ObserversFull = 1 << 12
            ObserversOnDefeat = 1 << 13
            OptionTeamsTogether = 1 << 14

            OptionLockTeams = 1 << 17
            OptionLockTeams2 = 1 << 18

            OptionAllowFullSharedControl = 1 << 24
            OptionRandomRace = 1 << 25
            OptionRandomHero = 1 << 26

            ObserversReferees = 1 << 30
        End Enum

        Private Shared ReadOnly DataJar As New TupleJar(
                    New EnumUInt32Jar(Of GameSettings)().Named("settings"),
                    New ByteJar().Named("unknown1"),
                    New UInt16Jar().Named("playable width"),
                    New UInt16Jar().Named("playable height"),
                    New UInt32Jar(showHex:=True).Named("xoro checksum"),
                    New StringJar().NullTerminated.Named("relative path"),
                    New StringJar().NullTerminated.Named("host name"),
                    New StringJar().NullTerminated.Named("unknown2"),
                    New DataJar().Fixed(exactDataCount:=20).Named("sha1 checksum"))

        Public Overrides Function Pack(Of TValue As GameStats)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)

            'Encode settings
            Dim settings As GameSettings
            Select Case value.Speed
                Case GameSpeedOption.Slow
                    'no flags set
                Case GameSpeedOption.Medium
                    settings = settings Or GameSettings.SpeedMedium
                Case GameSpeedOption.Fast
                    settings = settings Or GameSettings.SpeedFast
                Case Else
                    Throw value.Speed.MakeImpossibleValueException()
            End Select
            Select Case value.Observers
                Case GameObserverOption.FullObservers
                    settings = settings Or GameSettings.ObserversFull
                Case GameObserverOption.NoObservers
                    'no flags set
                Case GameObserverOption.ObsOnDefeat
                    settings = settings Or GameSettings.ObserversOnDefeat
                Case GameObserverOption.Referees
                    settings = settings Or GameSettings.ObserversReferees
                Case Else
                    Throw value.Observers.MakeImpossibleValueException()
            End Select
            Select Case value.Visibility
                Case GameVisibilityOption.AlwaysVisible
                    settings = settings Or GameSettings.VisibilityAlwaysVisible
                Case GameVisibilityOption.Explored
                    settings = settings Or GameSettings.VisibilityExplored
                Case GameVisibilityOption.HideTerrain
                    settings = settings Or GameSettings.VisibilityHideTerrain
                Case GameVisibilityOption.MapDefault
                    settings = settings Or GameSettings.VisibilityDefault
                Case Else
                    Throw value.Visibility.MakeImpossibleValueException()
            End Select
            If value.TeamsTogether Then settings = settings Or GameSettings.OptionTeamsTogether
            If value.LockTeams Then settings = settings Or GameSettings.OptionLockTeams
            If value.LockTeams Then settings = settings Or GameSettings.OptionLockTeams2
            If value.RandomHero Then settings = settings Or GameSettings.OptionRandomHero
            If value.RandomRace Then settings = settings Or GameSettings.OptionRandomRace
            If value.AllowFullSharedControl Then settings = settings Or GameSettings.OptionAllowFullSharedControl

            'Pack
            Dim rawPickle = DataJar.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"playable width", value.PlayableWidth},
                    {"playable height", value.PlayableHeight},
                    {"settings", settings},
                    {"xoro checksum", value.MapChecksumXORO},
                    {"sha1 checksum", value.MapChecksumSHA1},
                    {"relative path", value.AdvertisedPath.ToString},
                    {"host name", value.HostName.ToString},
                    {"unknown1", 0},
                    {"unknown2", ""}
                })
            Dim data = EncodeStatStringData(rawPickle.Data).Append(0).ToReadableList
            Return value.Pickled(data, rawPickle.Description)
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of GameStats)
            'StatString is null-terminated
            Dim p = data.IndexOf(0)
            If p < 0 Then Throw New PicklingException("No null terminator on game statstring.")
            Contract.Assume(p < data.Count)
            Dim datum = data.SubView(0, p + 1)
            Dim pickle = DataJar.Parse(DecodeStatStringData(datum).ToReadableList)
            Dim vals = CType(pickle.Value, Dictionary(Of InvariantString, Object))

            'Decode settings
            Dim settings = CType(CUInt(vals("settings")), GameSettings)
            Dim randomHero = settings.EnumIncludes(GameSettings.OptionRandomHero)
            Dim randomRace = settings.EnumIncludes(GameSettings.OptionRandomRace)
            Dim allowFullSharedControl = settings.EnumIncludes(GameSettings.OptionAllowFullSharedControl)
            Dim lockTeams = settings.EnumIncludes(GameSettings.OptionLockTeams)
            Dim teamsTogether = settings.EnumIncludes(GameSettings.OptionTeamsTogether)
            Dim observers As GameObserverOption
            If settings.EnumIncludes(GameSettings.ObserversOnDefeat) Then
                observers = GameObserverOption.ObsOnDefeat
            ElseIf settings.EnumIncludes(GameSettings.ObserversFull) Then
                observers = GameObserverOption.FullObservers
            ElseIf settings.EnumIncludes(GameSettings.ObserversReferees) Then
                observers = GameObserverOption.Referees
            Else
                observers = GameObserverOption.NoObservers
            End If
            Dim visibility As GameVisibilityOption
            If settings.EnumIncludes(GameSettings.VisibilityAlwaysVisible) Then
                visibility = GameVisibilityOption.AlwaysVisible
            ElseIf settings.EnumIncludes(GameSettings.VisibilityExplored) Then
                visibility = GameVisibilityOption.Explored
            ElseIf settings.EnumIncludes(GameSettings.VisibilityHideTerrain) Then
                visibility = GameVisibilityOption.HideTerrain
            Else
                visibility = GameVisibilityOption.MapDefault
            End If
            Dim speed As GameSpeedOption
            If settings.EnumIncludes(GameSettings.SpeedMedium) Then
                speed = GameSpeedOption.Medium
            ElseIf settings.EnumIncludes(GameSettings.SpeedFast) Then
                speed = GameSpeedOption.Fast
            Else
                speed = GameSpeedOption.Slow
            End If

            'Decode rest
            Dim playableWidth = CUInt(vals("playable width"))
            Dim playableHeight = CUInt(vals("playable height"))
            Dim xoroChecksum = CUInt(vals("xoro checksum"))
            Dim sha1Checksum = CType(vals("sha1 checksum"), IReadableList(Of Byte)).AssumeNotNull
            Dim relativePath As InvariantString = CStr(vals("relative path")).AssumeNotNull
            Dim hostName As InvariantString = CStr(vals("host name")).AssumeNotNull
            Contract.Assume(sha1Checksum.Count = 20)
            If Not relativePath.StartsWith("Maps\") Then Throw New PicklingException("Relative path must start with 'Maps\'")

            'Finish
            Dim value = New GameStats(randomHero:=randomHero,
                                      randomRace:=randomRace,
                                      allowFullSharedControl:=allowFullSharedControl,
                                      lockTeams:=lockTeams,
                                      teamsTogether:=teamsTogether,
                                      observers:=observers,
                                      visibility:=visibility,
                                      speed:=speed,
                                      playableWidth:=playableWidth,
                                      playableHeight:=playableHeight,
                                      mapChecksumXORO:=xoroChecksum,
                                      mapchecksumsha1:=sha1Checksum,
                                      advertisedPath:=relativePath,
                                      hostName:=hostName)
            Return value.Pickled(datum, pickle.Description)
        End Function

        Private Shared Function EncodeStatStringData(ByVal data As IEnumerable(Of Byte)) As IEnumerable(Of Byte)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
            Return From b In Concat(From plainBlock In data.Partitioned(partitionSize:=7)
                                    Let maskByte = plainBlock.Reverse.Aggregate(CByte(0), Function(acc, e) (acc Or (e And CByte(1))) << 1)
                                    Select {maskByte}.Concat(plainBlock))
                   Select b Or CByte(1)
        End Function

        Private Shared Function DecodeStatStringData(ByVal data As IEnumerable(Of Byte)) As IEnumerable(Of Byte)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
            Return From encodedBlock In data.Partitioned(partitionSize:=8)
                   From valueMaskBitPair In encodedBlock.Zip(encodedBlock.First.Bits).Skip(1)
                   Select decodedValue = valueMaskBitPair.Item1.WithBitSetTo(bitPosition:=0, bitValue:=valueMaskBitPair.Item2)
        End Function
    End Class
End Namespace
