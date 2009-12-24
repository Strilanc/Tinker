Namespace WC3
    Public Class GameSettings
#Region "Data"
        Public Const HCLChars As String = "abcdefghijklmnopqrstuvwxyz0123456789 -=,."
        Public Shared ReadOnly PartialArgumentTemplates As String() = {
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
                "-reserve -reserve=<name1 name2 ...> -r -r=<name1 name2 ...>",
                "-teams=#v#... -t=#v#...",
                "-private -p"
            }
        Public Shared ReadOnly PartialArgumentHelp As String() = {
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
        Private ReadOnly _numInstances As Integer
        Private ReadOnly _useInstanceOnDemand As Boolean
        Private ReadOnly _isAutoStarted As Boolean
        Private ReadOnly _adminPassword As String
        Private ReadOnly _teamSizes As IList(Of Integer) = New List(Of Integer)
        Private ReadOnly _reservations As New List(Of String)
        Private ReadOnly _usePermanent As Boolean
        Private ReadOnly _defaultSlotLockState As Slot.Lock
        Private ReadOnly _autoElevateUserName As InvariantString?
        Private ReadOnly _shouldGrabMap As Boolean
        Private ReadOnly _useLoadInGame As Boolean
        Private ReadOnly _mapMode As String = ""
        Private ReadOnly _useMultiObs As Boolean
        Private ReadOnly _greeting As String
        Private ReadOnly _isPrivate As Boolean

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_map IsNot Nothing)
            Contract.Invariant(_gameDescription IsNot Nothing)
            Contract.Invariant(_adminPassword IsNot Nothing)
            Contract.Invariant(_mapMode IsNot Nothing)
            Contract.Invariant(_teamSizes IsNot Nothing)
            Contract.Invariant(_reservations IsNot Nothing)
            Contract.Invariant(_mapMode IsNot Nothing)
            Contract.Invariant(_greeting IsNot Nothing)
            Contract.Invariant(_numInstances >= 0)
            Contract.Invariant((_numInstances = 0) = _useInstanceOnDemand)
        End Sub

        Public Sub New(ByVal map As Map,
                       ByVal gameDescription As LocalGameDescription,
                       ByVal argument As Commands.CommandArgument)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Requires(argument IsNot Nothing)
            Me._map = map
            Me._gameDescription = gameDescription
            Me._allowDownloads = Not argument.HasOptionalSwitch("NoDL")
            Me._allowUpload = Not argument.HasOptionalSwitch("NoUL")
            Me._isAutoStarted = argument.HasOptionalSwitch("AutoStart") OrElse argument.HasOptionalSwitch("as")
            Me._defaultSlotLockState = Slot.Lock.Unlocked
            Me._adminPassword = New Random().Next(0, 1000).ToString("000", CultureInfo.InvariantCulture)
            Me._isAdminGame = False
            Me._shouldGrabMap = argument.HasOptionalSwitch("grab")
            Me._mapMode = If(argument.TryGetOptionalNamedValue("Mode"), "")
            Me._useLoadInGame = argument.HasOptionalSwitch("LoadInGame") OrElse argument.HasOptionalSwitch("lig")
            Me._usePermanent = argument.HasOptionalSwitch("Permanent") OrElse argument.HasOptionalSwitch("Perm")
            Me._greeting = If(argument.TryGetOptionalNamedValue("Greet"), "").Replace("\n", Environment.NewLine).Replace("\N", Environment.NewLine)
            Dim teamString = If(argument.TryGetOptionalNamedValue("Teams"), argument.TryGetOptionalNamedValue("t"))
            Me._isPrivate = argument.HasOptionalSwitch("p") OrElse argument.HasOptionalSwitch("private")
            If teamString IsNot Nothing Then
                Me._teamSizes = TeamVersusStringToTeamSizes(teamString)
            End If
            Me._useMultiObs = argument.HasOptionalSwitch("MultiObs")
            'Reservations
            If argument.HasOptionalSwitch("Reserve") OrElse argument.HasOptionalSwitch("r") Then
                _reservations.Add(gameDescription.GameStats.HostName)
            End If
            For Each username In Concat(If(argument.TryGetOptionalNamedValue("Reserve"), "").Split(" "c),
                                        If(argument.TryGetOptionalNamedValue("r"), "").Split(" "c))
                If username <> "" Then _reservations.Add(username)
            Next username
            'Instance count
            If argument.TryGetOptionalNamedValue("Inst") Is Nothing Then
                Me._numInstances = 1
            Else
                If Not Integer.TryParse(argument.TryGetOptionalNamedValue("Inst"), Me._numInstances) OrElse Me._numInstances < 0 Then
                    Throw New ArgumentException("Invalid number of instances.")
                End If
                Me._useInstanceOnDemand = _numInstances = 0
            End If
            'Admin name
            If argument.HasOptionalSwitch("Admin") OrElse argument.HasOptionalSwitch("a") Then
                Me._autoElevateUserName = gameDescription.GameStats.HostName
            End If
            If Me._autoElevateUserName Is Nothing Then Me._autoElevateUserName = argument.TryGetOptionalNamedValue("Admin")
            If Me._autoElevateUserName Is Nothing Then Me._autoElevateUserName = argument.TryGetOptionalNamedValue("a")
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
        Public ReadOnly Property NumInstances As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() > 0 OrElse UseInstanceOnDemand)
                Return _numInstances
            End Get
        End Property
        Public ReadOnly Property UseInstanceOnDemand As Boolean
            Get
                Return _numInstances = 0
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
        Public ReadOnly Property TeamSizes As IList(Of Integer)
            Get
                Contract.Ensures(Contract.Result(Of IList(Of Integer))() IsNot Nothing)
                Return _teamSizes
            End Get
        End Property
        Public ReadOnly Property Reservations As IEnumerable(Of String)
            Get
                Contract.Ensures(Contract.Result(Of IEnumerable(Of String))() IsNot Nothing)
                Return _reservations
            End Get
        End Property
        Public ReadOnly Property UsePermanent As Boolean
            Get
                Return _usePermanent
            End Get
        End Property
        Public ReadOnly Property DefaultSlotLockState As Slot.Lock
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
        Public ReadOnly Property UseMultiObs As Boolean
            Get
                Return _useMultiObs
            End Get
        End Property
#End Region

        Public Function EncodedHCLMode(ByVal handicaps As Byte()) As Byte()
            Contract.Requires(handicaps IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)

            Dim indexMap(0 To 255) As Byte
            Dim blocked(0 To 255) As Boolean
            blocked(50) = True
            blocked(60) = True
            blocked(70) = True
            blocked(80) = True
            blocked(90) = True
            blocked(100) = True
            Dim i = 0
            For j = 1 To 255
                If blocked(j) Then Continue For
                indexMap(i) = CByte(j)
                i += 1
            Next j

            Dim dat(0 To Math.Min(handicaps.Length, _mapMode.Length) - 1) As Byte
            For i = 0 To dat.Length - 1
                Dim v = If(blocked(handicaps(i)), handicaps(i), 100)
                Dim c = If(HCLChars.Contains(_mapMode(i)), _mapMode(i), " ")
                dat(i) = indexMap((v - 50) \ 10 + HCLChars.IndexOf(c, StringComparison.CurrentCultureIgnoreCase) * 6)
            Next i
            Return dat
        End Function
    End Class
End Namespace
