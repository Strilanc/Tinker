Namespace WC3
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
    Public NotInheritable Class GameStats
        Implements IEquatable(Of GameStats)

        Private ReadOnly _randomHero As Boolean
        Private ReadOnly _randomRace As Boolean
        Private ReadOnly _allowFullSharedControl As Boolean
        Private ReadOnly _lockTeams As Boolean
        Private ReadOnly _teamsTogether As Boolean
        Private ReadOnly _observers As GameObserverOption
        Private ReadOnly _visibility As GameVisibilityOption
        Private ReadOnly _speed As GameSpeedOption
        Private ReadOnly _playableWidth As UInt16
        Private ReadOnly _playableHeight As UInt16
        Private ReadOnly _mapChecksumXORO As UInt32
        Private ReadOnly _mapChecksumSHA1 As Maybe(Of IReadableList(Of Byte))
        Private ReadOnly _advertisedPath As InvariantString
        Private ReadOnly _hostName As InvariantString

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(Not _mapChecksumSHA1.HasValue OrElse _mapChecksumSHA1.Value IsNot Nothing)
            Contract.Invariant(Not _mapChecksumSHA1.HasValue OrElse _mapChecksumSHA1.Value.Count = 20)
        End Sub

#Region "Properties"
        Public ReadOnly Property HostName As InvariantString
            Get
                Return _hostName
            End Get
        End Property
        Public ReadOnly Property MapChecksumSHA1 As Maybe(Of IReadableList(Of Byte))
            Get
                Contract.Ensures(Contract.Result(Of Maybe(Of IReadableList(Of Byte)))().HasValue OrElse Contract.Result(Of Maybe(Of IReadableList(Of Byte)))().Value IsNot Nothing)
                Contract.Ensures(Contract.Result(Of Maybe(Of IReadableList(Of Byte)))().HasValue OrElse Contract.Result(Of Maybe(Of IReadableList(Of Byte)))().Value.Count = 20)
                Return _mapChecksumSHA1
            End Get
        End Property
        Public ReadOnly Property AdvertisedPath As InvariantString
            Get
                Return _advertisedPath
            End Get
        End Property
        Public ReadOnly Property RandomHero As Boolean
            Get
                Return _randomHero
            End Get
        End Property
        Public ReadOnly Property RandomRace As Boolean
            Get
                Return _randomRace
            End Get
        End Property
        Public ReadOnly Property AllowFullSharedControl As Boolean
            Get
                Return _allowFullSharedControl
            End Get
        End Property
        Public ReadOnly Property LockTeams As Boolean
            Get
                Return _lockTeams
            End Get
        End Property
        Public ReadOnly Property TeamsTogether As Boolean
            Get
                Return _teamsTogether
            End Get
        End Property
        Public ReadOnly Property Observers As GameObserverOption
            Get
                Return _observers
            End Get
        End Property
        Public ReadOnly Property Visibility As GameVisibilityOption
            Get
                Return _visibility
            End Get
        End Property
        Public ReadOnly Property Speed As GameSpeedOption
            Get
                Return _speed
            End Get
        End Property
        Public ReadOnly Property PlayableWidth As UInt16
            Get
                Return _playableWidth
            End Get
        End Property
        Public ReadOnly Property PlayableHeight As UInt16
            Get
                Return _playableHeight
            End Get
        End Property
        Public ReadOnly Property MapChecksumXORO As UInt32
            Get
                Return _mapChecksumXORO
            End Get
        End Property
#End Region

        Public Sub New(ByVal randomHero As Boolean,
                       ByVal randomRace As Boolean,
                       ByVal allowFullSharedControl As Boolean,
                       ByVal lockTeams As Boolean,
                       ByVal teamsTogether As Boolean,
                       ByVal observers As GameObserverOption,
                       ByVal visibility As GameVisibilityOption,
                       ByVal speed As GameSpeedOption,
                       ByVal playableWidth As UInt16,
                       ByVal playableHeight As UInt16,
                       ByVal mapChecksumXORO As UInt32,
                       ByVal mapChecksumSHA1 As Maybe(Of IReadableList(Of Byte)),
                       ByVal advertisedPath As InvariantString,
                       ByVal hostName As InvariantString)
            Contract.Requires(Not mapChecksumSHA1.HasValue OrElse mapChecksumSHA1.Value IsNot Nothing)
            Contract.Requires(Not mapChecksumSHA1.HasValue OrElse mapChecksumSHA1.Value.Count = 20)

            Me._randomHero = randomHero
            Me._randomRace = randomRace
            Me._allowFullSharedControl = allowFullSharedControl
            Me._lockTeams = lockTeams
            Me._teamsTogether = teamsTogether
            Me._observers = observers
            Me._visibility = visibility
            Me._speed = speed
            Me._playableWidth = playableWidth
            Me._playableHeight = playableHeight
            Me._mapChecksumXORO = mapChecksumXORO
            Me._mapChecksumSHA1 = mapChecksumSHA1
            Me._advertisedPath = advertisedPath
            Me._hostName = hostName
        End Sub

        ''' <summary>
        ''' Constructs the game stats using some values from a map.
        ''' </summary>
        Public Shared Function FromMapAndSettings(ByVal map As Map,
                                                  ByVal randomHero As Boolean,
                                                  ByVal randomRace As Boolean,
                                                  ByVal allowFullSharedControl As Boolean,
                                                  ByVal lockTeams As Boolean,
                                                  ByVal teamsTogether As Boolean,
                                                  ByVal observers As GameObserverOption,
                                                  ByVal visibility As GameVisibilityOption,
                                                  ByVal speed As GameSpeedOption,
                                                  ByVal hostName As InvariantString) As GameStats
            Contract.Requires(map IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameStats)() IsNot Nothing)
            Return New GameStats(randomHero:=randomHero,
                                 randomRace:=randomRace,
                                 allowFullSharedControl:=allowFullSharedControl,
                                 lockTeams:=lockTeams,
                                 teamsTogether:=teamsTogether,
                                 observers:=observers,
                                 visibility:=visibility,
                                 speed:=speed,
                                 PlayableWidth:=map.PlayableWidth,
                                 PlayableHeight:=map.PlayableHeight,
                                 MapChecksumXORO:=map.MapChecksumXORO,
                                 MapChecksumSHA1:=map.MapChecksumSHA1.Maybe,
                                 AdvertisedPath:=map.AdvertisedPath,
                                 hostName:=hostName)
        End Function

        ''' <summary>
        ''' Constructs the game stats based on a map and arguments.
        ''' </summary>
        Public Shared Function FromMapAndArgument(ByVal map As Map,
                                                  ByVal hostName As InvariantString,
                                                  ByVal argument As Commands.CommandArgument) As GameStats
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameStats)() IsNot Nothing)

            Dim randomHero = argument.HasOptionalSwitch("RandomHero")
            Dim randomRace = argument.HasOptionalSwitch("RandomRace")
            Dim lockTeams = Not argument.HasOptionalSwitch("UnlockTeams")
            Dim allowFullSharedControl = argument.HasOptionalSwitch("FullShare")
            Dim teamsTogether = Not argument.HasOptionalSwitch("TeamsApart")
            'Observers
            Dim observers = GameObserverOption.NoObservers
            If argument.HasOptionalSwitch("Referees") OrElse argument.HasOptionalSwitch("ref") Then
                observers = GameObserverOption.Referees
            ElseIf argument.HasOptionalSwitch("Obs") OrElse argument.HasOptionalSwitch("MultiObs") OrElse argument.HasOptionalNamedValue("Obs") Then
                observers = GameObserverOption.FullObservers
            ElseIf argument.HasOptionalSwitch("ObsOnDefeat") OrElse argument.HasOptionalSwitch("od") Then
                observers = GameObserverOption.ObsOnDefeat
            End If
            'Speed
            Dim speed = GameSpeedOption.Fast
            If argument.HasOptionalNamedValue("Speed") Then
                speed = argument.OptionalNamedValue("Speed").EnumParse(Of GameSpeedOption)(ignoreCase:=True)
            End If
            'Visibility
            Dim visibility = GameVisibilityOption.MapDefault
            If argument.HasOptionalNamedValue("Visibility") Then
                visibility = argument.OptionalNamedValue("Visibility").EnumParse(Of GameVisibilityOption)(ignoreCase:=True)
            End If

            Return FromMapAndSettings(map:=map,
                                      randomHero:=randomHero,
                                      randomRace:=randomRace,
                                      allowFullSharedControl:=allowFullSharedControl,
                                      lockTeams:=lockTeams,
                                      teamsTogether:=teamsTogether,
                                      observers:=observers,
                                      visibility:=visibility,
                                      speed:=speed,
                                      hostName:=hostName)
        End Function

        Public Shared ReadOnly PartialArgumentTemplates As IEnumerable(Of String) = {
                "-Obs",
                "-Obs=<# or reservations>",
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
        Public Shared ReadOnly PartialArgumentHelp As IEnumerable(Of String) = {
                "Obs=-Obs, -Obs=#, -Obs=<name1 name2 ...>: Turns on full observers. If a quantity or reservations are specified, only the minimum number of observer slots will be open.",
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

        Public Overrides Function GetHashCode() As Integer
            Return AdvertisedPath.GetHashCode Xor HostName.GetHashCode Xor MapChecksumXORO.GetHashCode
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, GameStats))
        End Function
        Public Overloads Function Equals(ByVal other As GameStats) As Boolean Implements IEquatable(Of GameStats).Equals
            If other Is Nothing Then Return False
            If Me.AdvertisedPath <> other.AdvertisedPath Then Return False
            If Me.AllowFullSharedControl <> other.AllowFullSharedControl Then Return False
            If Me.HostName <> other.HostName Then Return False
            If Me.LockTeams <> other.LockTeams Then Return False
            If Me.MapChecksumSHA1.HasValue <> other.MapChecksumSHA1.HasValue Then Return False
            If Me.MapChecksumSHA1.HasValue Then
                If Not Me.MapChecksumSHA1.Value.SequenceEqual(other.MapChecksumSHA1.Value) Then Return False
            End If
            If Me.MapChecksumXORO <> other.MapChecksumXORO Then Return False
            If Me.Observers <> other.Observers Then Return False
            If Me.PlayableHeight <> other.PlayableHeight Then Return False
            If Me.PlayableWidth <> other.PlayableWidth Then Return False
            If Me.RandomHero <> other.RandomHero Then Return False
            If Me.RandomRace <> other.RandomRace Then Return False
            If Me.Speed <> other.Speed Then Return False
            If Me.TeamsTogether <> other.TeamsTogether Then Return False
            If Me.Visibility <> other.Visibility Then Return False
            Return True
        End Function
    End Class
End Namespace
