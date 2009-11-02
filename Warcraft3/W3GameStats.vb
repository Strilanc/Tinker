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
                       ByVal arguments As IEnumerable(Of String))
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(arguments IsNot Nothing)
            Contract.Requires(hostName IsNot Nothing)

            Me.randomHero = False
            Me.randomRace = False
            Me.allowFullSharedControl = False
            Me.lockTeams = True
            Me.teamsTogether = True
            Me.observers = GameObserverOption.NoObservers
            Me.visibility = GameVisibilityOption.MapDefault
            Me.speed = GameSpeedOption.Fast
            Me.playableWidth = map.playableWidth
            Me.playableHeight = map.playableHeight
            Me.mapChecksumXORO = map.MapChecksumXORO
            Me._mapChecksumSHA1 = map.MapChecksumSHA1.ToView
            Me.relativePath = map.RelativePath
            Me._hostName = hostName

            For Each arg In arguments
                Contract.Assume(arg IsNot Nothing)
                Dim arg2 = ""
                If arg.Contains("="c) Then
                    Dim n = arg.IndexOf("="c)
                    arg2 = arg.Substring(n + 1)
                    arg = arg.Substring(0, n + 1)
                End If
                arg = arg.ToUpperInvariant.Trim()
                arg2 = arg2.ToUpperInvariant.Trim()

                Select Case arg
                    Case "-OBS", "-MULTIOBS", "-MO", "-O"
                        observers = GameObserverOption.FullObservers
                    Case "-REFEREES", "-REF"
                        observers = GameObserverOption.Referees
                    Case "-OBSONDEFEAT", "-OD"
                        observers = GameObserverOption.ObsOnDefeat
                    Case "-RH", "-RANDOMHERO"
                        randomHero = True
                    Case "-RR", "-RANDOMRACE"
                        randomRace = True
                    Case "-UNLOCKTEAMS"
                        lockTeams = False
                    Case "-FULLSHARED", "-FULLSHARE", "-ALLOWFULLSHARED", "-ALLOWFULLSHARE", "-FULLSHAREDCONTROL", "-ALLOWFULLSHAREDCONTROL"
                        allowFullSharedControl = True
                    Case "-TEAMSAPART"
                        teamsTogether = False
                    Case "-SPEED="
                        Select Case arg2
                            Case "MEDIUM"
                                speed = GameSpeedOption.Medium
                            Case "SLOW"
                                speed = GameSpeedOption.Slow
                        End Select
                    Case "-VISIBILITY=", "-VIS="
                        Select Case arg2
                            Case "ALL", "ALWAYSVISIBLE", "VISIBLE"
                                visibility = GameVisibilityOption.AlwaysVisible
                            Case "EXPLORED"
                                visibility = GameVisibilityOption.Explored
                            Case "NONE", "HIDE", "HIDETERRAIN"
                                visibility = GameVisibilityOption.HideTerrain
                        End Select
                End Select
            Next arg
        End Sub
    End Class
End Namespace
