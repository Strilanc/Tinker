Namespace WC3
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

        Private Shared ReadOnly DataJar As New TupleJar("data",
                    New EnumUInt32Jar(Of GameSettings)("settings").Weaken,
                    New ByteJar("unknown1").Weaken,
                    New UInt16Jar("playable width").Weaken,
                    New UInt16Jar("playable height").Weaken,
                    New UInt32Jar("xoro checksum").Weaken,
                    New StringJar("relative path").Weaken,
                    New StringJar("host name").Weaken,
                    New StringJar("unknown2").Weaken,
                    New RawDataJar("sha1 checksum", Size:=20).Weaken)

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Public Overrides Function Pack(Of TValue As GameStats)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)

            'Encode settings
            Dim settings As GameSettings
            Select Case value.speed
                Case GameSpeedOption.Slow
                    'no flags set
                Case GameSpeedOption.Medium
                    settings = settings Or GameSettings.SpeedMedium
                Case GameSpeedOption.Fast
                    settings = settings Or GameSettings.SpeedFast
                Case Else
                    Throw value.speed.MakeImpossibleValueException()
            End Select
            Select Case value.observers
                Case GameObserverOption.FullObservers
                    settings = settings Or GameSettings.ObserversFull
                Case GameObserverOption.NoObservers
                    'no flags set
                Case GameObserverOption.ObsOnDefeat
                    settings = settings Or GameSettings.ObserversOnDefeat
                Case GameObserverOption.Referees
                    settings = settings Or GameSettings.ObserversReferees
                Case Else
                    Throw value.observers.MakeImpossibleValueException()
            End Select
            Select Case value.visibility
                Case GameVisibilityOption.AlwaysVisible
                    settings = settings Or GameSettings.VisibilityAlwaysVisible
                Case GameVisibilityOption.Explored
                    settings = settings Or GameSettings.VisibilityExplored
                Case GameVisibilityOption.HideTerrain
                    settings = settings Or GameSettings.VisibilityHideTerrain
                Case GameVisibilityOption.MapDefault
                    settings = settings Or GameSettings.VisibilityDefault
                Case Else
                    Throw value.visibility.MakeImpossibleValueException()
            End Select
            If value.teamsTogether Then settings = settings Or GameSettings.OptionTeamsTogether
            If value.lockTeams Then settings = settings Or GameSettings.OptionLockTeams
            If value.lockTeams Then settings = settings Or GameSettings.OptionLockTeams2
            If value.randomHero Then settings = settings Or GameSettings.OptionRandomHero
            If value.randomRace Then settings = settings Or GameSettings.OptionRandomRace
            If value.allowFullSharedControl Then settings = settings Or GameSettings.OptionAllowFullSharedControl

            'Pack
            Dim rawPickle = DataJar.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"playable width", value.playableWidth},
                    {"playable height", value.playableHeight},
                    {"settings", settings},
                    {"xoro checksum", value.mapChecksumXORO},
                    {"sha1 checksum", value.MapChecksumSHA1},
                    {"relative path", value.relativePath},
                    {"host name", value.HostName},
                    {"unknown1", 0},
                    {"unknown2", ""}
                })
            Return New Pickling.Pickle(Of TValue)(value:=value,
                                                  Data:=Concat(EncodeStatStringData(rawPickle.Data).ToArray(), {0}).AsReadableList,
                                                  description:=rawPickle.Description)
        End Function
        'verification disabled due to stupid verifier
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of GameStats)
            'StatString is null-terminated
            Dim n As Integer
            For n = 0 To data.Count - 1
                If data(n) = 0 Then
                    data = data.SubView(0, n + 1)
                    Exit For
                End If
            Next n
            Dim pickle = DataJar.Parse(DecodeStatStringData(data.SubView(0, n)))
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
            Dim playableWidth = CInt(vals("playable width"))
            Dim playableHeight = CInt(vals("playable height"))
            Dim xoroChecksum = CUInt(vals("xoro checksum"))
            Dim sha1Checksum = CType(vals("sha1 checksum"), Byte()).AssumeNotNull.AsReadableList
            Dim relativePath = CStr(vals("relative path")).AssumeNotNull
            Dim hostName = CStr(vals("host name")).AssumeNotNull
            Contract.Assume(sha1Checksum.Count = 20)

            'Finish
            Dim value = New GameStats(randomHero,
                                      randomRace,
                                      allowFullSharedControl,
                                      lockTeams,
                                      teamsTogether,
                                      observers,
                                      visibility,
                                      speed,
                                      playableWidth,
                                      playableHeight,
                                      xoroChecksum,
                                      sha1Checksum,
                                      relativePath,
                                      hostName)
            Return New Pickling.Pickle(Of GameStats)(value, data, pickle.Description)
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