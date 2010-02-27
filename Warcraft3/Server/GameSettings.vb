Namespace WC3
    Public Class GameSettings
#Region "Data"
        Public Const HCLChars As String = "abcdefghijklmnopqrstuvwxyz0123456789 -=,."
        Public Shared ReadOnly PartialArgumentTemplates As IEnumerable(Of String) = {
                "-Admin=user -Admin -a=user -a",
                "-AutoStart -as",
                "-Inst=#Instances",
                "-Grab",
                "-Greet=default",
                "-LoadInGame -lig",
                "-Mode=mode",
                "-NoUL",
                "-NoDL",
                "-Permanent -perm",
                "-replay -replay=filename",
                "-reserve -reserve=<name1 name2 ...> -r -r=<name1 name2 ...>",
                "-teams=#v#... -t=#v#...",
                "-private -p"
            }
        Public Shared ReadOnly PartialArgumentHelp As IEnumerable(Of String) = {
                "Admin=-Admin, -a, -Admin=user, -a=user: Sets the auto-elevated username. Use no argument to match your name.",
                "Autostart=-Autostart, -as: Instances will start automatically when they fill up.",
                "Instances=-Inst=value: Sets the initial number of instances. Use 0 for unlimited instances.",
                "Grab=-Grab: Downloads the map file from joining players. Meant for use when hosting a map by meta-data.",
                "Greet=-Greet=value: Sets the message sent to players as they join the game. Use \n for newlines.",
                "LoadInGame=-LoadInGame, -lig: Players wait for loaders in the game instead of at the load screen.",
                "Mode=-mode=mode: Passes a mode into maps supporting HCL. Usage on incompatible maps will ruin player handicaps.",
                "NoUL=-NoUL: Turns off uploads from the bot, but still allows players to download from each other.",
                "NoDL=-NoDL: Boots players who don't already have the map.",
                "Permanent=-Permanent, -Perm: Automatically recreate closed instances and automatically sets the game to private/public as new instances are available.",
                "Replay=-replay, -replay=filename: Causes the bot to save a replay of the game (under Documents\Tinker). You can specify a filename to use.",
                "Reserve=-Reserve, -r, -Reserve=<user1 user2 ...>, -r=<user1 user2 ...>: Reserves the slots for players or yourself.",
                "Teams=-Teams=#v#..., -t=#v#...: Sets the initial number of open slots for each team.",
                "Private=-Private, -p: Creates a private game instead of a public game."
            }
#End Region

        Private ReadOnly _gameDescription As LocalGameDescription
        Private ReadOnly _map As Map
        Private ReadOnly _isAdminGame As Boolean
        Private ReadOnly _allowDownloads As Boolean
        Private ReadOnly _allowUpload As Boolean
        Private ReadOnly _initialInstanceCount As Integer
        Private ReadOnly _isAutoStarted As Boolean
        Private ReadOnly _adminPassword As String
        Private ReadOnly _teamSizes As IReadableList(Of Integer) = New Integer() {}.AsReadableList
        Private ReadOnly _reservations As IReadableList(Of InvariantString)
        Private ReadOnly _observerReservations As IReadableList(Of InvariantString) = New InvariantString() {}.AsReadableList
        Private ReadOnly _observerCount As Integer
        Private ReadOnly _usePermanent As Boolean
        Private ReadOnly _defaultSlotLockState As Slot.LockState
        Private ReadOnly _autoElevateUserName As InvariantString?
        Private ReadOnly _shouldGrabMap As Boolean
        Private ReadOnly _useLoadInGame As Boolean
        Private ReadOnly _mapMode As String = ""
        Private ReadOnly _useMultiObs As Boolean
        Private ReadOnly _greeting As String
        Private ReadOnly _isPrivate As Boolean
        Private ReadOnly _shouldRecordReplay As Boolean
        Private ReadOnly _replayDefaultFileName As String

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_map IsNot Nothing)
            Contract.Invariant(_gameDescription IsNot Nothing)
            Contract.Invariant(_adminPassword IsNot Nothing)
            Contract.Invariant(_mapMode IsNot Nothing)
            Contract.Invariant(_teamSizes IsNot Nothing)
            Contract.Invariant(_reservations IsNot Nothing)
            Contract.Invariant(_mapMode IsNot Nothing)
            Contract.Invariant(_greeting IsNot Nothing)
            Contract.Invariant(_observerReservations IsNot Nothing)
            Contract.Invariant(_observerCount >= 0)
            Contract.Invariant(_initialInstanceCount >= 0)
        End Sub

        Public Sub New(ByVal map As Map,
                       ByVal gameDescription As LocalGameDescription,
                       ByVal argument As Commands.CommandArgument,
                       Optional ByVal isAdminGame As Boolean = False,
                       Optional ByVal adminPassword As String = Nothing)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Me._map = map
            Me._gameDescription = gameDescription
            Me._allowDownloads = Not argument.HasOptionalSwitch("NoDL")
            Me._allowUpload = Not argument.HasOptionalSwitch("NoUL")
            Me._isAutoStarted = argument.HasOptionalSwitch("AutoStart") OrElse argument.HasOptionalSwitch("as")
            Me._defaultSlotLockState = Slot.LockState.Unlocked
            Me._adminPassword = If(adminPassword, New Random().Next(0, 1000).ToString("000", CultureInfo.InvariantCulture))
            Me._isAdminGame = isAdminGame
            Me._shouldGrabMap = argument.HasOptionalSwitch("grab")
            Me._mapMode = If(argument.TryGetOptionalNamedValue("Mode"), "")
            Me._useLoadInGame = argument.HasOptionalSwitch("LoadInGame") OrElse argument.HasOptionalSwitch("lig")
            Me._usePermanent = argument.HasOptionalSwitch("Permanent") OrElse argument.HasOptionalSwitch("Perm")
            If argument.HasOptionalNamedValue("Greet") Then
                Me._greeting = argument.OptionalNamedValue("Greet").Replace("\n", Environment.NewLine).Replace("\N", Environment.NewLine)
            Else
                Me._greeting = My.Settings.DefaultGameGreet
            End If
            Dim teamString = If(argument.TryGetOptionalNamedValue("Teams"), argument.TryGetOptionalNamedValue("t"))
            Me._isPrivate = argument.HasOptionalSwitch("p") OrElse argument.HasOptionalSwitch("private")
            Me._replayDefaultFileName = argument.TryGetOptionalNamedValue("replay")
            Me._shouldRecordReplay = argument.HasOptionalSwitch("replay") OrElse argument.HasOptionalNamedValue("replay")
            If teamString IsNot Nothing Then
                Me._teamSizes = TeamVersusStringToTeamSizes(teamString).AsReadableList
            End If
            'Observers
            If argument.HasOptionalNamedValue("obs") Then
                Dim obsArg = argument.OptionalNamedValue("obs")
                If Integer.TryParse(obsArg, _observerCount) Then
                    If _observerCount <= 0 Then Throw New ArgumentOutOfRangeException("argument", "Observer count must be positive.")
                Else
                    _observerReservations = (From name In argument.OptionalNamedValue("obs").Split(" "c)
                                             Select New InvariantString(name)
                                            ).ToReadableList
                End If
            End If
            Me._useMultiObs = argument.HasOptionalSwitch("MultiObs")
            'Reservations
            Dim reserverations = New List(Of InvariantString)
            If argument.HasOptionalSwitch("Reserve") OrElse argument.HasOptionalSwitch("r") Then
                reserverations.Add(gameDescription.GameStats.HostName)
            End If
            For Each username In Concat(If(argument.TryGetOptionalNamedValue("Reserve"), "").Split(" "c),
                                        If(argument.TryGetOptionalNamedValue("r"), "").Split(" "c))
                If username <> "" Then reserverations.Add(username)
            Next username
            _reservations = reserverations.AsReadableList
            'Instance count
            If argument.TryGetOptionalNamedValue("Inst") Is Nothing Then
                Me._initialInstanceCount = 1
            Else
                If Not Integer.TryParse(argument.TryGetOptionalNamedValue("Inst"), Me._initialInstanceCount) OrElse Me._initialInstanceCount < 0 Then
                    Throw New ArgumentException("Invalid number of instances.")
                End If
            End If
            'Admin name
            If argument.HasOptionalSwitch("Admin") OrElse argument.HasOptionalSwitch("a") Then
                Me._autoElevateUserName = gameDescription.GameStats.HostName
            ElseIf argument.HasOptionalNamedValue("Admin") Then
                Me._autoElevateUserName = argument.OptionalNamedValue("Admin")
            ElseIf argument.HasOptionalNamedValue("a") Then
                Me._autoElevateUserName = argument.OptionalNamedValue("a")
            End If
        End Sub

#Region "Properties"
        Public ReadOnly Property IsPrivate As Boolean
            Get
                Return _isPrivate
            End Get
        End Property
        Public ReadOnly Property GameDescription As LocalGameDescription
            Get
                Contract.Ensures(Contract.Result(Of LocalGameDescription)() IsNot Nothing)
                Return _gameDescription
            End Get
        End Property
        Public ReadOnly Property Map() As Map
            Get
                Contract.Ensures(Contract.Result(Of Map)() IsNot Nothing)
                Return _map
            End Get
        End Property
        Public ReadOnly Property IsAdminGame As Boolean
            Get
                Return _isAdminGame
            End Get
        End Property
        Public ReadOnly Property AllowDownloads As Boolean
            Get
                Return _allowDownloads
            End Get
        End Property
        Public ReadOnly Property AllowUpload As Boolean
            Get
                Return _allowUpload
            End Get
        End Property
        Public ReadOnly Property InitialInstanceCount As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() > 0 OrElse UseInstanceOnDemand)
                Return _initialInstanceCount
            End Get
        End Property
        Public ReadOnly Property UseInstanceOnDemand As Boolean
            Get
                Contract.Ensures(Contract.Result(Of Boolean)() = (_initialInstanceCount = 0))
                Return _initialInstanceCount = 0
            End Get
        End Property
        Public ReadOnly Property IsAutoStarted As Boolean
            Get
                Return _isAutoStarted
            End Get
        End Property
        Public ReadOnly Property AdminPassword() As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _adminPassword
            End Get
        End Property
        Public ReadOnly Property TeamSizes As IReadableList(Of Integer)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Integer))() IsNot Nothing)
                Return _teamSizes
            End Get
        End Property
        Public ReadOnly Property Reservations As IReadableList(Of InvariantString)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of InvariantString))() IsNot Nothing)
                Return _reservations
            End Get
        End Property
        Public ReadOnly Property UsePermanent As Boolean
            Get
                Return _usePermanent
            End Get
        End Property
        Public ReadOnly Property DefaultSlotLockState As Slot.LockState
            Get
                Return _defaultSlotLockState
            End Get
        End Property
        Public ReadOnly Property AutoElevateUserName As InvariantString?
            Get
                Return _autoElevateUserName
            End Get
        End Property
        Public ReadOnly Property ShouldGrabMap As Boolean
            Get
                Return _shouldGrabMap
            End Get
        End Property
        Public ReadOnly Property UseLoadInGame As Boolean
            Get
                Return _useLoadInGame
            End Get
        End Property
        Public ReadOnly Property MapMode As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _mapMode
            End Get
        End Property
        Public ReadOnly Property Greeting As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _greeting
            End Get
        End Property
        <CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId:="Multi")>
        Public ReadOnly Property UseMultiObs As Boolean
            Get
                Return _useMultiObs
            End Get
        End Property
        Public ReadOnly Property ObserverCount As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Return _observerCount
            End Get
        End Property
        Public ReadOnly Property ObserverReservations As IReadableList(Of InvariantString)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of InvariantString))() IsNot Nothing)
                Return _observerReservations
            End Get
        End Property
        Public ReadOnly Property ShouldRecordReplay As Boolean
            Get
                Return _shouldRecordReplay
            End Get
        End Property
        Public ReadOnly Property DefaultReplayFileName As String
            Get
                Return _replayDefaultFileName
            End Get
        End Property
#End Region

        <ContractVerification(False)>
        Public Function EncodedHCLMode(ByVal handicaps As IEnumerable(Of Byte)) As IEnumerable(Of Byte)
            Contract.Requires(handicaps IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IEnumerable(Of Byte))() IsNot Nothing)

            'Prep
            Dim defaultHandicaps = New Byte() {50, 60, 70, 80, 90, 100}
            Dim indexMap = (From e In Enumerable.Range(start:=1, count:=255)
                            Select b = CByte(e)
                            Where Not defaultHandicaps.Contains(CByte(b))
                            ).ToArray

            'Scrub
            Dim handicapData = From handicap In handicaps
                               Select If(defaultHandicaps.Contains(handicap), handicap, CByte(100))
            Dim letterData = From letter In CType(_mapMode, Char())
                             Select safeLetter = If(HCLChars.Contains(letter), letter, " "c)
                             Select CByte(HCLChars.IndexOf(CStr(safeLetter), StringComparison.OrdinalIgnoreCase))

            'Encode
            Dim encodedHandicaps = Enumerable.Zip(handicapData,
                                                  letterData,
                                                  Function(handicap, letter) indexMap((handicap - 50) \ 10 + letter * 6))
            Dim remainingHandicaps = handicapData.Skip(encodedHandicaps.Count)

            Return encodedHandicaps.Concat(remainingHandicaps)
        End Function
    End Class
End Namespace
