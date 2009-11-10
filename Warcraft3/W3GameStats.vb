Namespace Warcraft3
    Public Enum GameSpeedOption
        Slow
        Medium
        Fast
    End Enum
    Public Enum GameObserverOption
        NoObservers
        ObsOnDefeat
        FullObservers
        Referees
    End Enum
    Public Enum GameVisibilityOption
        MapDefault
        AlwaysVisible
        Explored
        HideTerrain
    End Enum

    ''' <summary>
    ''' Stores the data contained in a warcraft 3 game 'statstring'.
    ''' </summary>
    Public NotInheritable Class W3GameStats
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
        Public ReadOnly mapChecksumXORO As UInt32
        Private ReadOnly _mapChecksumSHA1 As ViewableList(Of Byte)
        Public ReadOnly relativePath As String
        Private ReadOnly _hostName As String

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_mapChecksumSHA1 IsNot Nothing)
            Contract.Invariant(_mapChecksumSHA1.Length = 20)
            Contract.Invariant(_hostName IsNot Nothing)
        End Sub

        Public ReadOnly Property HostName As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _hostName
            End Get
        End Property
        Public ReadOnly Property MapChecksumSHA1 As ViewableList(Of Byte)
            Get
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))() IsNot Nothing)
                Contract.Ensures(Contract.Result(Of ViewableList(Of Byte))().Length = 20)
                Return _mapChecksumSHA1
            End Get
        End Property

        ''' <summary>
        ''' Constructs the game stats directly.
        ''' </summary>
        Public Sub New(ByVal randomHero As Boolean,
                       ByVal randomRace As Boolean,
                       ByVal allowFullSharedControl As Boolean,
                       ByVal lockTeams As Boolean,
                       ByVal teamsTogether As Boolean,
                       ByVal observers As GameObserverOption,
                       ByVal visibility As GameVisibilityOption,
                       ByVal speed As GameSpeedOption,
                       ByVal playableWidth As Integer,
                       ByVal playableHeight As Integer,
                       ByVal mapChecksumXORO As UInt32,
                       ByVal mapChecksumSHA1 As Byte(),
                       ByVal relativePath As String,
                       ByVal hostName As String)
            Contract.Requires(MapChecksumSHA1 IsNot Nothing)
            Contract.Requires(MapChecksumSHA1.Length = 20)
            Contract.Requires(hostName IsNot Nothing)

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
            Me.mapChecksumXORO = mapChecksumXORO
            Me._mapChecksumSHA1 = MapChecksumSHA1.ToView
            Me.relativePath = relativePath
            Me._hostName = hostName
        End Sub

        ''' <summary>
        ''' Constructs the game stats based on a map and arguments.
        ''' </summary>
        Public Sub New(ByVal map As W3Map,
                       ByVal hostName As String,
                       ByVal argument As Commands.CommandArgument)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Requires(hostName IsNot Nothing)

            Me.playableWidth = map.playableWidth
            Me.playableHeight = map.playableHeight
            Me.mapChecksumXORO = map.MapChecksumXORO
            Me._mapChecksumSHA1 = map.MapChecksumSHA1
            Me.relativePath = map.RelativePath
            Me._hostName = hostName

            Me.randomHero = argument.HasOptionalSwitch("RandomHero")
            Me.randomRace = argument.HasOptionalSwitch("RandomRace")
            Me.lockTeams = Not argument.HasOptionalSwitch("UnlockTeams")
            Me.allowFullSharedControl = argument.HasOptionalSwitch("FullShare")
            Me.teamsTogether = Not argument.HasOptionalSwitch("TeamsApart")
            'Observers
            If argument.HasOptionalSwitch("Referees") OrElse argument.HasOptionalSwitch("ref") Then
                Me.observers = GameObserverOption.Referees
            ElseIf argument.HasOptionalSwitch("Obs") OrElse argument.HasOptionalSwitch("MultiObs") Then
                Me.observers = GameObserverOption.FullObservers
            ElseIf argument.HasOptionalSwitch("ObsOnDefeat") OrElse argument.HasOptionalSwitch("od") Then
                Me.observers = GameObserverOption.ObsOnDefeat
            Else
                Me.observers = GameObserverOption.NoObservers
            End If
            'Speed
            If argument.TryGetOptionalNamedValue("Speed") Is Nothing Then
                Me.speed = GameSpeedOption.Fast
            Else
                'Parse
                If Not argument.TryGetOptionalNamedValue("Speed").EnumTryParse(ignoreCase:=True, result:=Me.speed) Then
                    Throw New ArgumentException("Invalid game speed value: {0}".Frmt(argument.TryGetOptionalNamedValue("Speed")))
                End If
            End If
            'Visibility
            If argument.TryGetOptionalNamedValue("Visibility") Is Nothing Then
                Me.visibility = GameVisibilityOption.MapDefault
            Else
                If Not argument.TryGetOptionalNamedValue("Visibility").EnumTryParse(ignoreCase:=True, result:=Me.visibility) Then
                    Throw New ArgumentException("Invalid map visibility value: {0}".Frmt(argument.TryGetOptionalNamedValue("Visibility")))
                End If
            End If
        End Sub

        Public Shared ReadOnly PartialArgumentTemplates As String() = {
                "-Obs",
                "-ObsOnDefeat -od",
                "-Referees -ref",
                "-MultiObs",
                "-FullShare",
                "-RandomHero",
                "-RandomRace",
                "-Speed={Medium,Slow}",
                "-TeamsApart",
                "-UnlockTeams",
                "-Visibility={AlwaysVisible,Explored,HideTerrain}"
            }
        Public Shared ReadOnly PartialArgumentHelp As String() = {
                "Obs=-Obs: Turns on full observers.",
                "ObsOnDefeat=-ObsOnDefeat, -od: Turns on observers on defeat.",
                "FullShare=-FullShare: Turns on wc3's 'full shared control' option.",
                "MultiObs=-MultiObs, mo: Turns on observers, and creates a special slot which can accept large amounts of players. The map must have two available obs slots for this to work.",
                "RandomHero=-RandomHero: Turns on the wc3 'random hero' option.",
                "RandomRace=-RandomRace: Turns on the wc3 'random race' option.",
                "Referees=-Referees, -ref: Turns on observer referees.",
                "Speed=-Speed=value: Changes wc3's game speed option from Fast to Medium or Slow.",
                "TeamsApart=-TeamsApart: Turns off wc3's 'teams together' option.",
                "UnlockTeams=-UnlockTeams: Turns off wc3's 'lock teams' option.",
                "Visibility=-Visibility=value: Changes wc3's visibility option from MapDefault to AlwaysVisible, Explored, or HideTerrain."
            }
    End Class
End Namespace
