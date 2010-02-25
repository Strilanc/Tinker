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

        Private Shared ReadOnly DataJar As New TupleJar("game stats",
                    New EnumUInt32Jar(Of GameSettings)("settings").Weaken,
                    New ByteJar().Named("unknown1").Weaken,
                    New UInt16Jar().Named("playable width").Weaken,
                    New UInt16Jar().Named("playable height").Weaken,
                    New UInt32Jar(showHex:=True).Named("xoro checksum").Weaken,
                    New NullTerminatedStringJar("relative path").Weaken,
                    New NullTerminatedStringJar("host name").Weaken,
                    New NullTerminatedStringJar("unknown2").Weaken,
                    New RawDataJar("sha1 checksum", Size:=20).Weaken)

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

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
            Return New Pickling.Pickle(Of TValue)(value:=value,
                                                  Data:=Concat(EncodeStatStringData(rawPickle.Data).ToArray(), {0}).AsReadableList,
                                                  description:=rawPickle.Description)
        End Function
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of GameStats)
            'StatString is null-terminated
            Dim p = data.IndexOf(0)
            If p < 0 Then Throw New PicklingException("No null terminator on game statstring.")
            Contract.Assume(p < data.Count)
            Dim datum = data.SubView(0, p + 1)
            Dim pickle = DataJar.Parse(DecodeStatStringData(datum))
            Dim vals = CType(pickle.Value, Dictionary(Of InvariantString, Object))

            'Decode settings
            Dim settings = CType(CUInt(vals("settings")), GameSettings)
            Dim randomHero = CBool(settings And GameSettings.OptionRandomHero)
            Dim randomRace = CBool(settings And GameSettings.OptionRandomRace)
            Dim allowFullSharedControl = CBool(settings And GameSettings.OptionAllowFullSharedControl)
            Dim lockTeams = CBool(settings And GameSettings.OptionLockTeams)
            Dim teamsTogether = CBool(settings And GameSettings.OptionTeamsTogether)
            Dim observers As GameObserverOption
            If CBool(settings And GameSettings.ObserversOnDefeat) Then
                observers = GameObserverOption.ObsOnDefeat
            ElseIf CBool(settings And GameSettings.ObserversFull) Then
                observers = GameObserverOption.FullObservers
            ElseIf CBool(settings And GameSettings.ObserversReferees) Then
                observers = GameObserverOption.Referees
            Else
                observers = GameObserverOption.NoObservers
            End If
            Dim visibility As GameVisibilityOption
            If CBool(settings And GameSettings.VisibilityAlwaysVisible) Then
                visibility = GameVisibilityOption.AlwaysVisible
            ElseIf CBool(settings And GameSettings.VisibilityExplored) Then
                visibility = GameVisibilityOption.Explored
            ElseIf CBool(settings And GameSettings.VisibilityHideTerrain) Then
                visibility = GameVisibilityOption.HideTerrain
            Else
                visibility = GameVisibilityOption.MapDefault
            End If
            Dim speed As GameSpeedOption
            If CBool(settings And GameSettings.SpeedMedium) Then
                speed = GameSpeedOption.Medium
            ElseIf CBool(settings And GameSettings.SpeedFast) Then
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
            Return New Pickling.Pickle(Of GameStats)(value, datum, pickle.Description)
        End Function

        Private Shared Function EncodeStatStringData(ByVal data As IEnumerable(Of Byte)) As IReadableList(Of Byte)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            Dim out As New List(Of Byte)
            For Each block In data.EnumBlocks(7)
                Contract.Assume(block IsNot Nothing)

                'Place bits into a header byte
                Dim head As Byte
                For Each b In block.Reverse()
                    head = head Or (b And CByte(&H1))
                    head <<= 1
                Next b

                'Output block
                out.Add(head Or CByte(&H1))
                For Each b In block
                    out.Add(b Or CByte(&H1))
                Next b
            Next block
            Return out.AsReadableList
        End Function

        Private Shared Function DecodeStatStringData(ByVal data As IEnumerable(Of Byte)) As IReadableList(Of Byte)
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IReadableList(Of Byte))() IsNot Nothing)
            Dim out As New List(Of Byte)
            For Each block In data.EnumBlocks(8)
                Contract.Assume(block IsNot Nothing)
                For i = 1 To block.Count - 1
                    Dim b = block(i)
                    'Take bit from first byte
                    If Not CBool((block(0) >> i) And &H1) Then b = b And CByte(&HFE)
                    'Output
                    out.Add(b)
                Next i
            Next block
            Return out.AsReadableList
        End Function
    End Class
End Namespace