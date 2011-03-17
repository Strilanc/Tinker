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
                    New UInt16Jar().Named("playable height"),
                    New UInt16Jar().Named("playable width"),
                    New UInt32Jar(showHex:=True).Named("xoro checksum"),
                    New UTF8Jar().NullTerminated.Named("relative path"),
                    New UTF8Jar().NullTerminated.Named("host name"),
                    New ByteJar().Named("unknown2"),
                    New DataJar().Fixed(exactDataCount:=20).Optional.Named("sha1 checksum"))

        Public Overrides Function Pack(value As GameStats) As IRist(Of Byte)
            Contract.Assume(value IsNot Nothing)
            Return EncodeStatStringData(DataJar.Pack(PackDataValue(value))).Append(0).ToRist()
        End Function
        Public Overrides Function Parse(data As IRist(Of Byte)) As ParsedValue(Of GameStats)
            'null-terminated
            Dim indexOfNull = data.IndexOf(0)
            If Not indexOfNull.HasValue Then Throw New PicklingException("No null terminator on game statstring.")
            Dim usedDataCount = indexOfNull.Value + 1
            Contract.Assume(usedDataCount > 0)
            Contract.Assume(usedDataCount <= data.Count)

            Dim decodedData = DecodeStatStringData(data.TakeExact(usedDataCount - 1)).ToRist
            Dim parsed = DataJar.Parse(decodedData)
            If parsed.UsedDataCount <> decodedData.Count Then Throw New PicklingException("Leftover data before null terminator.")
            Return ParseDataValue(parsed.Value).ParsedWithDataCount(usedDataCount)
        End Function

        Private Shared Function PackDataValue(value As GameStats) As NamedValueMap
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of NamedValueMap)() IsNot Nothing)
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
                    settings = settings Or GameSettings.ObserversFull Or GameSettings.ObserversOnDefeat
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
            Return New Dictionary(Of InvariantString, Object) From {
                    {"playable width", value.PlayableWidth},
                    {"playable height", value.PlayableHeight},
                    {"settings", settings},
                    {"xoro checksum", value.MapChecksumXORO},
                    {"sha1 checksum", value.MapChecksumSHA1},
                    {"relative path", value.AdvertisedPath.ToString},
                    {"host name", value.HostName.ToString},
                    {"unknown1", CByte(0)},
                    {"unknown2", CByte(0)}}
        End Function
        Private Shared Function ParseDataValue(vals As NamedValueMap) As GameStats
            Contract.Requires(vals IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameStats)() IsNot Nothing)
            Dim settings = vals.ItemAs(Of GameSettings)("settings")
            Dim randomHero = settings.EnumUInt32Includes(GameSettings.OptionRandomHero)
            Dim randomRace = settings.EnumUInt32Includes(GameSettings.OptionRandomRace)
            Dim allowFullSharedControl = settings.EnumUInt32Includes(GameSettings.OptionAllowFullSharedControl)
            Dim lockTeams = settings.EnumUInt32Includes(GameSettings.OptionLockTeams)
            Dim teamsTogether = settings.EnumUInt32Includes(GameSettings.OptionTeamsTogether)
            Dim observers As GameObserverOption
            If settings.EnumUInt32Includes(GameSettings.ObserversFull) Then
                observers = GameObserverOption.FullObservers
            ElseIf settings.EnumUInt32Includes(GameSettings.ObserversOnDefeat) Then
                observers = GameObserverOption.ObsOnDefeat
            ElseIf settings.EnumUInt32Includes(GameSettings.ObserversReferees) Then
                observers = GameObserverOption.Referees
            Else
                observers = GameObserverOption.NoObservers
            End If
            Dim visibility As GameVisibilityOption
            If settings.EnumUInt32Includes(GameSettings.VisibilityAlwaysVisible) Then
                visibility = GameVisibilityOption.AlwaysVisible
            ElseIf settings.EnumUInt32Includes(GameSettings.VisibilityExplored) Then
                visibility = GameVisibilityOption.Explored
            ElseIf settings.EnumUInt32Includes(GameSettings.VisibilityHideTerrain) Then
                visibility = GameVisibilityOption.HideTerrain
            Else
                visibility = GameVisibilityOption.MapDefault
            End If
            Dim speed As GameSpeedOption
            If settings.EnumUInt32Includes(GameSettings.SpeedMedium) Then
                speed = GameSpeedOption.Medium
            ElseIf settings.EnumUInt32Includes(GameSettings.SpeedFast) Then
                speed = GameSpeedOption.Fast
            Else
                speed = GameSpeedOption.Slow
            End If

            'Decode rest
            Dim playableWidth = vals.ItemAs(Of UInt16)("playable width")
            Dim playableHeight = vals.ItemAs(Of UInt16)("playable height")
            Dim xoroChecksum = vals.ItemAs(Of UInt32)("xoro checksum")
            Dim sha1Checksum = vals.ItemAs(Of Maybe(Of IRist(Of Byte)))("sha1 checksum")
            Dim relativePath = vals.ItemAs(Of String)("relative path").ToInvariant
            Dim hostName = vals.ItemAs(Of String)("host name").ToInvariant
            If sha1Checksum.HasValue AndAlso sha1Checksum.Value.Count <> 20 Then
                Throw New PicklingException("sha1 checksum must have have 20 bytes.")
            End If

            'Finish
            Return New GameStats(randomHero:=randomHero,
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
        End Function

        Private Shared Function EncodeStatStringData(data As IEnumerable(Of Byte)) As IEnumerable(Of Byte)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
            Return From b In Concat(From plainBlock In data.Partitioned(partitionSize:=7)
                                    Let maskByte = plainBlock.Reverse.Aggregate(CByte(0), Function(acc, e) (acc Or (e And CByte(1))) << 1)
                                    Select {maskByte}.Concat(plainBlock))
                   Select b Or CByte(1)
        End Function

        Private Shared Function DecodeStatStringData(data As IEnumerable(Of Byte)) As IEnumerable(Of Byte)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)
            Return From encodedBlock In data.Partitioned(partitionSize:=8)
                   From valueMaskBitPair In encodedBlock.Zip(encodedBlock.First.Bits).Skip(1)
                   Select decodedValue = valueMaskBitPair.Item1.WithBitSetTo(bitPosition:=0, bitValue:=valueMaskBitPair.Item2)
        End Function

        Public Overrides Function Describe(value As GameStats) As String
            Contract.Assume(value IsNot Nothing)
            Return DataJar.Describe(PackDataValue(value))
        End Function
        <SuppressMessage("Microsoft.Contracts", "Requires-7-11")>
        Public Overrides Function Parse(text As String) As GameStats
            Return ParseDataValue(DataJar.Parse(text))
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of GameStats)
            Dim subControl = DataJar.MakeControl()
            Return New DelegatedValueEditor(Of GameStats)(
                Control:=subControl.Control,
                eventAdder:=Sub(action) AddHandler subControl.ValueChanged, Sub() action(),
                getter:=Function() ParseDataValue(subControl.Value),
                setter:=Sub(value) subControl.Value = PackDataValue(value),
                disposer:=Sub() subControl.Dispose())
        End Function
    End Class
End Namespace
