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
        Private ReadOnly _teamSizes As IRist(Of Integer)
        Private ReadOnly _reservations As IRist(Of InvariantString)
        Private ReadOnly _observerReservations As IRist(Of InvariantString)
        Private ReadOnly _observerCount As Integer
        Private ReadOnly _usePermanent As Boolean
        Private ReadOnly _defaultSlotLockState As Slot.LockState
        Private ReadOnly _autoElevateUserName As InvariantString?
        Private ReadOnly _shouldGrabMap As Boolean
        Private ReadOnly _useLoadInGame As Boolean
        Private ReadOnly _mapMode As String
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
            Contract.Invariant(_greeting IsNot Nothing)
            Contract.Invariant(_observerReservations IsNot Nothing)
            Contract.Invariant(_observerCount >= 0)
            Contract.Invariant(_initialInstanceCount >= 0)
        End Sub

        <CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId:="Multi")>
        Public Sub New(ByVal gameDescription As LocalGameDescription,
                       ByVal map As Map,
                       ByVal isAdminGame As Boolean,
                       ByVal allowDownloads As Boolean,
                       ByVal allowUpload As Boolean,
                       ByVal initialInstanceCount As Integer,
                       ByVal isAutoStarted As Boolean,
                       ByVal adminPassword As String,
                       ByVal teamSizes As IRist(Of Integer),
                       ByVal reservations As IRist(Of InvariantString),
                       ByVal observerReservations As IRist(Of InvariantString),
                       ByVal observerCount As Integer,
                       ByVal usePermanent As Boolean,
                       ByVal defaultSlotLockState As Slot.LockState,
                       ByVal autoElevateUserName As InvariantString?,
                       ByVal shouldGrabMap As Boolean,
                       ByVal useLoadInGame As Boolean,
                       ByVal mapMode As String,
                       ByVal useMultiObs As Boolean,
                       ByVal greeting As String,
                       ByVal isPrivate As Boolean,
                       ByVal shouldRecordReplay As Boolean,
                       Optional ByVal replayDefaultFileName As String = Nothing)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Requires(adminPassword IsNot Nothing)
            Contract.Requires(mapMode IsNot Nothing)
            Contract.Requires(teamSizes IsNot Nothing)
            Contract.Requires(reservations IsNot Nothing)
            Contract.Requires(greeting IsNot Nothing)
            Contract.Requires(observerReservations IsNot Nothing)
            Contract.Requires(observerCount >= 0)
            Contract.Requires(initialInstanceCount >= 0)
            Me._gameDescription = gameDescription
            Me._map = map
            Me._isAdminGame = isAdminGame
            Me._allowDownloads = allowDownloads
            Me._allowUpload = allowUpload
            Me._initialInstanceCount = initialInstanceCount
            Me._isAutoStarted = isAutoStarted
            Me._adminPassword = adminPassword
            Me._teamSizes = teamSizes
            Me._reservations = reservations
            Me._observerReservations = observerReservations
            Me._observerCount = observerCount
            Me._usePermanent = usePermanent
            Me._defaultSlotLockState = defaultSlotLockState
            Me._autoElevateUserName = autoElevateUserName
            Me._shouldGrabMap = shouldGrabMap
            Me._useLoadInGame = useLoadInGame
            Me._mapMode = mapMode
            Me._useMultiObs = useMultiObs
            Me._greeting = greeting
            Me._isPrivate = isPrivate
            Me._shouldRecordReplay = shouldRecordReplay
            Me._replayDefaultFileName = replayDefaultFileName
        End Sub
        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", justification:="INCORRECT WARNING")>
        Public Shared Function FromArgument(ByVal map As Map,
                                            ByVal gameDescription As LocalGameDescription,
                                            ByVal argument As Commands.CommandArgument,
                                            Optional ByVal isAdminGame As Boolean = False,
                                            Optional ByVal adminPassword As String = Nothing) As GameSettings
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameSettings)() IsNot Nothing)
            Return New GameSettings(map:=map,
                                    gameDescription:=gameDescription,
                                    AllowDownloads:=Not argument.HasOptionalSwitch("NoDL"),
                                    AllowUpload:=Not argument.HasOptionalSwitch("NoUL"),
                                    IsAutoStarted:=argument.HasOptionalSwitch("AutoStart") OrElse argument.HasOptionalSwitch("as"),
                                    DefaultSlotLockState:=Slot.LockState.Unlocked,
                                    adminPassword:=If(adminPassword, New Random().Next(0, 1000).ToString("000", CultureInfo.InvariantCulture)),
                                    isAdminGame:=isAdminGame,
                                    ShouldGrabMap:=argument.HasOptionalSwitch("grab"),
                                    MapMode:=If(argument.TryGetOptionalNamedValue("Mode"), ""),
                                    UseLoadInGame:=argument.HasOptionalSwitch("LoadInGame") OrElse argument.HasOptionalSwitch("lig"),
                                    UsePermanent:=argument.HasOptionalSwitch("Permanent") OrElse argument.HasOptionalSwitch("Perm"),
                                    Greeting:=ExtractGreet(argument),
                                    IsPrivate:=argument.HasOptionalSwitch("p") OrElse argument.HasOptionalSwitch("private"),
                                    replayDefaultFileName:=argument.TryGetOptionalNamedValue("replay"),
                                    ShouldRecordReplay:=argument.HasOptionalSwitch("replay") OrElse argument.HasOptionalNamedValue("replay"),
                                    TeamSizes:=ExtractTeamSizes(argument),
                                    ObserverCount:=ExtractObserverCount(argument),
                                    ObserverReservations:=ExtractObserverReservations(argument),
                                    UseMultiObs:=argument.HasOptionalSwitch("MultiObs"),
                                    Reservations:=ExtractReservations(argument, gameDescription),
                                    InitialInstanceCount:=ExtractInitialInstanceCount(argument),
                                    AutoElevateUserName:=ExtractAutoElevateUserName(gameDescription, argument))
        End Function

        Private Shared Function ExtractGreet(ByVal argument As Commands.CommandArgument) As String
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
            If argument.HasOptionalNamedValue("Greet") Then
                Return argument.OptionalNamedValue("Greet").Replace("\n", Environment.NewLine).Replace("\N", Environment.NewLine)
            Else
                Return My.Settings.DefaultGameGreet.AssumeNotNull
            End If
        End Function
        Private Shared Function ExtractTeamSizes(ByVal argument As Commands.CommandArgument) As IRist(Of Integer)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of Integer))() IsNot Nothing)
            Dim teamArg = If(argument.TryGetOptionalNamedValue("Teams"), argument.TryGetOptionalNamedValue("t"))
            If teamArg IsNot Nothing Then
                Return TeamVersusStringToTeamSizes(teamArg).AsReadableList
            Else
                Return New Integer() {}.AsReadableList
            End If
        End Function
        Private Shared Function ExtractObserverCount(ByVal argument As Commands.CommandArgument) As Integer
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Integer)() >= 0)
            If argument.HasOptionalNamedValue("obs") Then
                Dim result As Integer
                If Integer.TryParse(argument.OptionalNamedValue("obs"), NumberStyles.None, CultureInfo.InvariantCulture, result) Then
                    If result <= 0 Then Throw New ArgumentOutOfRangeException("argument", "Observer count must be positive.")
                End If
                Contract.Assume(result >= 0)
                Return result
            Else
                Return 0
            End If
        End Function
        Private Shared Function ExtractObserverReservations(ByVal argument As Commands.CommandArgument) As IRist(Of InvariantString)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of InvariantString))() IsNot Nothing)
            If argument.HasOptionalNamedValue("obs") AndAlso Not Integer.TryParse(argument.OptionalNamedValue("obs"), NumberStyles.None, CultureInfo.InvariantCulture, 0) Then
                Return (From name In argument.OptionalNamedValue("obs").Split(" "c)
                        Select New InvariantString(name)
                        ).ToReadableList
            Else
                Return New InvariantString() {}.AsReadableList
            End If
        End Function
        Private Shared Function ExtractReservations(ByVal argument As Commands.CommandArgument,
                                                    ByVal gameDescription As GameDescription) As IRist(Of InvariantString)
            Contract.Requires(argument IsNot Nothing)
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IRist(Of InvariantString))() IsNot Nothing)
            Dim result = New List(Of InvariantString)
            If argument.HasOptionalSwitch("Reserve") OrElse argument.HasOptionalSwitch("r") Then
                result.Add(gameDescription.GameStats.HostName)
            End If
            For Each username In Concat(If(argument.TryGetOptionalNamedValue("Reserve"), "").Split(" "c),
                                        If(argument.TryGetOptionalNamedValue("r"), "").Split(" "c))
                If username <> "" Then result.Add(username)
            Next username
            Return result.AsReadableList
        End Function
        Private Shared Function ExtractInitialInstanceCount(ByVal argument As Commands.CommandArgument) As Integer
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Integer)() >= 0)
            If argument.HasOptionalNamedValue("Inst") Then
                Dim result As Integer
                If Not Integer.TryParse(argument.OptionalNamedValue("Inst"), NumberStyles.None, CultureInfo.InvariantCulture, result) OrElse result < 0 Then
                    Throw New ArgumentException("Invalid number of instances.")
                End If
                Return result
            Else
                Return 1
            End If
        End Function
        Private Shared Function ExtractAutoElevateUserName(ByVal gameDescription As GameDescription,
                                                           ByVal argument As Commands.CommandArgument) As InvariantString
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            If argument.HasOptionalSwitch("Admin") OrElse argument.HasOptionalSwitch("a") Then
                Return gameDescription.GameStats.HostName
            ElseIf argument.HasOptionalNamedValue("Admin") Then
                Return argument.OptionalNamedValue("Admin")
            ElseIf argument.HasOptionalNamedValue("a") Then
                Return argument.OptionalNamedValue("a")
            Else
                Return Nothing
            End If
        End Function

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
        Public ReadOnly Property TeamSizes As IRist(Of Integer)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Integer))() IsNot Nothing)
                Return _teamSizes
            End Get
        End Property
        Public ReadOnly Property Reservations As IRist(Of InvariantString)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of InvariantString))() IsNot Nothing)
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
        Public ReadOnly Property ObserverReservations As IRist(Of InvariantString)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of InvariantString))() IsNot Nothing)
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
            Dim indexMap = (From e In 256.Range.Skip(1)
                            Select b = CByte(e)
                            Where Not defaultHandicaps.Contains(b)
                            ).ToArray

            'Scrub
            Dim handicapData = From handicap In handicaps
                               Select If(defaultHandicaps.Contains(handicap), handicap, CByte(100))
            Dim letterData = From letter In _mapMode.AsEnumerable
                             Select safeLetter = If(HCLChars.Contains(letter), letter, " "c)
                             Select CByte(HCLChars.IndexOf(CStr(safeLetter), StringComparison.OrdinalIgnoreCase))

            'Encode (map original handicap onto [0, 6), then include hcl mode data on top)
            Dim encodedHandicaps = From pair In handicapData.Zip(letterData)
                                   Let handicap = pair.Item1
                                   Let letter = pair.Item2
                                   Let originalData = handicap \ 10 - 5
                                   Let modeData = letter * 6
                                   Select indexMap(originalData + modeData)
            Dim remainingHandicaps = handicapData.Skip(encodedHandicaps.Count)

            Return encodedHandicaps.Concat(remainingHandicaps)
        End Function
    End Class
End Namespace
