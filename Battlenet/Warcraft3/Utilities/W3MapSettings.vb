Imports HostBot.Pickling.Jars
Imports HostBot.Pickling

Namespace Warcraft3
    Public Class W3MapSettingsJar
        Inherits Pickling.Jars.Jar(Of Object)
        Private Enum GameSettingFlags As UInteger
            'SpeedSlow = 0 'no flags set
            SpeedMedium = 1 << 0
            SpeedFast = 1 << 1
            VisibilityHideTerrain = 1 << 8
            VisibilityExplored = 1 << 9
            VisibilityAlwaysVisible = 1 << 10
            VisibilityDefault = 1 << 11
            'ObserversNone = 0 'no flags set
            ObserversFull = 1 << 12 Or 1 << 13
            ObserversOnDefeat = 1 << 13
            OptionTeamsTogether = 1 << 14
            OptionLockTeams = 1 << 17 Or 1 << 18
            OptionAllowFullSharedControl = 1 << 24
            OptionRandomRace = 1 << 25
            OptionRandomHero = 1 << 26
            ObserversReferees = 1 << 30
        End Enum

        Private Shared ReadOnly DataJar As New TupleJar("data",
                    New EnumJar(Of GameSettingFlags)("settings", 4).Weaken,
                    New ValueJar("unknown1", 1).Weaken,
                    New ValueJar("playable width", 2).Weaken,
                    New ValueJar("playable height", 2).Weaken,
                    New ArrayJar("xoro checksum", 4).Weaken,
                    New StringJar("relative path").Weaken,
                    New StringJar("username").Weaken,
                    New StringJar("unknown2").Weaken)

        Public Sub New(ByVal name As String)
            MyBase.New(name)
        End Sub

        Public Overrides Function Pack(Of R As Object)(ByVal value As R) As Pickling.IPickle(Of R)
            Dim dd = CType(CType(value, Object), Dictionary(Of String, Object))
            Dim settings = CType(dd("settings"), W3MapSettings)
            Dim username = CStr(dd("username"))

            'settings
            Dim u As GameSettingFlags
            Select Case settings.speed
                Case GameSpeedOption.Slow
                    'no flags set
                Case GameSpeedOption.Medium
                    u = u Or GameSettingFlags.SpeedMedium
                Case GameSpeedOption.Fast
                    u = u Or GameSettingFlags.SpeedFast
            End Select
            Select Case settings.observers
                Case GameObserverOption.FullObservers
                    u = u Or GameSettingFlags.ObserversFull
                Case GameObserverOption.NoObservers
                    'no flags set
                Case GameObserverOption.ObsOnDefeat
                    u = u Or GameSettingFlags.ObserversOnDefeat
                Case GameObserverOption.Referees
                    u = u Or GameSettingFlags.ObserversReferees
            End Select
            Select Case settings.visibility
                Case GameVisibilityOption.AlwaysVisible
                    u = u Or GameSettingFlags.VisibilityAlwaysVisible
                Case GameVisibilityOption.Explored
                    u = u Or GameSettingFlags.VisibilityExplored
                Case GameVisibilityOption.HideTerrain
                    u = u Or GameSettingFlags.VisibilityHideTerrain
                Case GameVisibilityOption.MapDefault
                    u = u Or GameSettingFlags.VisibilityDefault
            End Select
            If settings.teamsTogether Then u = u Or GameSettingFlags.OptionTeamsTogether
            If settings.lockTeams Then u = u Or GameSettingFlags.OptionLockTeams
            If settings.randomHero Then u = u Or GameSettingFlags.OptionRandomHero
            If settings.randomRace Then u = u Or GameSettingFlags.OptionRandomRace
            If settings.allowFullSharedControl Then u = u Or GameSettingFlags.OptionAllowFullSharedControl

            'values
            Dim p = DataJar.Pack(New Dictionary(Of String, Object) From {
                    {"playable width", settings.playableWidth},
                    {"playable height", settings.playableHeight},
                    {"settings", u},
                    {"xoro checksum", settings.xoroChecksum},
                    {"relative path", settings.relativePath},
                    {"username", username},
                    {"unknown1", 0},
                    {"unknown2", ""}
                })
            Dim data = Concat({EncodeStatStringData(p.Data).ToArray(), New Byte() {0}})
            Return New Pickling.Pickles.Pickle(Of R)(value, data.ToView, p.Description)
        End Function
        Public Overrides Function Parse(ByVal data As IViewableList(Of Byte)) As Pickling.IPickle(Of Object)
            Dim i As Integer
            For i = 0 To data.Length - 1
                If data(i) = 0 Then
                    data = data.SubView(0, i + 1)
                    Exit For
                End If
            Next i
            Dim p = DataJar.Parse(DecodeStatStringData(data.SubView(0, i - 1)))
            Dim vals = CType(p.Value, Dictionary(Of String, Object))

            Dim u = CType(CUInt(vals("settings")), GameSettingFlags)
            Dim randomHero = CBool(u And GameSettingFlags.OptionRandomHero)
            Dim randomRace = CBool(u And GameSettingFlags.OptionRandomRace)
            Dim allowFullSharedControl = CBool(u And GameSettingFlags.OptionAllowFullSharedControl)
            Dim lockTeams = CBool(u And GameSettingFlags.OptionLockTeams)
            Dim teamsTogether = CBool(u And GameSettingFlags.OptionTeamsTogether)
            Dim observers As GameObserverOption
            If CBool(u And GameSettingFlags.ObserversOnDefeat) Then
                observers = GameObserverOption.ObsOnDefeat
            ElseIf CBool(u And GameSettingFlags.ObserversFull) Then
                observers = GameObserverOption.FullObservers
            ElseIf CBool(u And GameSettingFlags.ObserversReferees) Then
                observers = GameObserverOption.Referees
            Else
                observers = GameObserverOption.NoObservers
            End If
            Dim visibility As GameVisibilityOption
            If CBool(u And GameSettingFlags.VisibilityAlwaysVisible) Then
                visibility = GameVisibilityOption.AlwaysVisible
            ElseIf CBool(u And GameSettingFlags.VisibilityExplored) Then
                visibility = GameVisibilityOption.Explored
            ElseIf CBool(u And GameSettingFlags.VisibilityHideTerrain) Then
                visibility = GameVisibilityOption.HideTerrain
            Else
                visibility = GameVisibilityOption.MapDefault
            End If
            Dim speed As GameSpeedOption
            If CBool(u And GameSettingFlags.SpeedMedium) Then
                speed = GameSpeedOption.Medium
            ElseIf CBool(u And GameSettingFlags.SpeedFast) Then
                speed = GameSpeedOption.Fast
            Else
                speed = GameSpeedOption.Slow
            End If
            Dim playableWidth = CInt(vals("playable width"))
            Dim playableHeight = CInt(vals("playable height"))
            Dim xoroChecksum = CType(vals("xoro checksum"), Byte())
            Dim relativePath = CStr(vals("relative path"))

            Dim ms = New W3MapSettings(Nothing,
                                       Nothing,
                                       randomHero,
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
                                       relativePath)
            Dim dd = New Dictionary(Of String, Object) From {{"settings", ms}, {"username", CStr(vals("username"))}}
            Return New Pickling.Pickles.Pickle(Of Object)(dd, data, p.Description)
        End Function

        Private Shared Function EncodeStatStringData(ByVal data As IEnumerable(Of Byte)) As IViewableList(Of Byte)
            Dim out As New List(Of Byte)
            For Each block In data.EnumBlocks(7)
                'Compute block header
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
            Return out.ToView
        End Function
        Private Shared Function DecodeStatStringData(ByVal data As IEnumerable(Of Byte)) As IViewableList(Of Byte)
            Dim out As New List(Of Byte)
            For Each block In data.EnumBlocks(8)
                'Output block
                For i = 1 To block.Count - 1
                    Dim b = block(i)
                    If Not CBool(block(0) >> i And &H1) Then
                        'Clear bit0
                        b = b And CByte(&HFE)
                    End If
                    out.Add(b)
                Next i
            Next block
            Return out.ToView()
        End Function
    End Class

    Public Class W3MapSettings
        Public ReadOnly randomHero As Boolean
        Public ReadOnly randomRace As Boolean
        Public ReadOnly allowFullSharedControl As Boolean
        Public ReadOnly lockTeams As Boolean
        Public ReadOnly teamsTogether As Boolean
        Public ReadOnly observers As GameObserverOption
        Public ReadOnly visibility As GameVisibilityOption
        Public ReadOnly speed As GameSpeedOption
        Public ReadOnly playableWidth As Integer
        Public ReadOnly playableHeight As Integer
        Public ReadOnly xoroChecksum As Byte()
        Public ReadOnly relativePath As String
        Public ReadOnly gameType As GameTypeFlags

        Public Sub New(ByVal arguments As IList(Of String),
                       ByVal map As W3Map,
                       Optional ByVal randomHero As Boolean = False,
                       Optional ByVal randomRace As Boolean = False,
                       Optional ByVal allowFullSharedControl As Boolean = False,
                       Optional ByVal lockTeams As Boolean = True,
                       Optional ByVal teamsTogether As Boolean = True,
                       Optional ByVal observers As GameObserverOption = GameObserverOption.NoObservers,
                       Optional ByVal visibility As GameVisibilityOption = GameVisibilityOption.MapDefault,
                       Optional ByVal speed As GameSpeedOption = GameSpeedOption.Fast,
                       Optional ByVal playableWidth As Integer = 0,
                       Optional ByVal playableHeight As Integer = 0,
                       Optional ByVal xoroChecksum As Byte() = Nothing,
                       Optional ByVal relativePath As String = Nothing,
                       Optional ByVal gameType As GameTypeFlags = 0)
            Me.randomHero = randomHero
            Me.randomRace = randomRace
            Me.allowFullSharedControl = allowFullSharedControl
            Me.lockTeams = lockTeams
            Me.teamsTogether = teamsTogether
            Me.observers = observers
            Me.visibility = visibility
            Me.speed = speed
            Me.playableWidth = playableWidth
            Me.playableHeight = playableHeight
            Me.xoroChecksum = xoroChecksum
            Me.relativePath = relativePath
            Me.gameType = gameType
            If map IsNot Nothing Then
                Me.playableWidth = map.playableWidth
                Me.playableHeight = map.playableHeight
                Me.xoroChecksum = map.checksumXoro
                Me.relativePath = "Maps\" + map.relativePath
                Me.gameType = map.gameType
            Else
                If gameType = 0 Then
                    Me.gameType = GameTypeFlags.MakerUser Or GameTypeFlags.SizeLarge Or GameTypeFlags.TypeScenario
                End If
            End If

            If arguments IsNot Nothing Then
                For Each arg In arguments
                    Dim arg2 = ""
                    If arg.Contains("="c) Then
                        Dim n = arg.IndexOf("="c)
                        arg2 = arg.Substring(n + 1)
                        arg = arg.Substring(0, n + 1)
                    End If
                    arg = arg.ToLower.Trim()
                    arg2 = arg2.ToLower.Trim()

                    Select Case arg
                        Case "-obs", "-multiobs", "-mo", "-o"
                            observers = GameObserverOption.FullObservers
                        Case "-referees", "-ref"
                            observers = GameObserverOption.Referees
                        Case "-obsondefeat", "-od"
                            observers = GameObserverOption.ObsOnDefeat
                        Case "-rh", "-randomhero"
                            randomHero = True
                        Case "-rr", "-randomrace"
                            randomRace = True
                        Case "-unlockteams"
                            lockTeams = False
                        Case "-fullshared", "-fullshare", "-allowfullshared", "-allowfullshare", "-fullsharedcontrol", "-allowfullsharedcontrol"
                            allowFullSharedControl = True
                        Case "-teamsapart"
                            teamsTogether = False
                        Case "-speed="
                            Select Case arg2
                                Case "medium"
                                    speed = GameSpeedOption.Medium
                                Case "slow"
                                    speed = GameSpeedOption.Slow
                            End Select
                        Case "-visibility=", "-vis="
                            Select Case arg2
                                Case "all", "always visible", "visible", "alwaysvisible"
                                    visibility = GameVisibilityOption.AlwaysVisible
                                Case "explored"
                                    visibility = GameVisibilityOption.Explored
                                Case "none", "hide", "hideterrain"
                                    visibility = GameVisibilityOption.HideTerrain
                            End Select
                    End Select
                Next arg
            End If
        End Sub
    End Class
End Namespace