Imports HostBot.Links

Namespace Warcraft3
    Public Enum W3ServerState As Byte
        OnlyAcceptingPlayers = 0
        AcceptingPlayersAndPlayingGames = 1
        OnlyPlayingGames = 2
        Disposed = 3
    End Enum

    Public NotInheritable Class ServerSettings
        Private ReadOnly _map As W3Map
        Private ReadOnly _header As W3GameDescription
        Public ReadOnly creationTime As Date = DateTime.Now()
        Public ReadOnly isAdminGame As Boolean
        Public ReadOnly allowDownloads As Boolean
        Public ReadOnly allowUpload As Boolean
        Public ReadOnly instances As Integer
        Public ReadOnly isAutoStarted As Boolean
        Private ReadOnly _adminPassword As String
        Public ReadOnly teamSetup As String
        Public ReadOnly Reservations As New HashSet(Of String)
        Public ReadOnly permanent As Boolean
        Public ReadOnly defaultSlotLockState As W3Slot.Lock
        Public ReadOnly autoElevateUserName As String
        Public ReadOnly grabMap As Boolean
        Public ReadOnly defaultListenPorts As New List(Of UShort)
        Public ReadOnly loadInGame As Boolean
        Public ReadOnly testFakePlayers As Boolean
        Public ReadOnly HCLMode As String = ""
        Public ReadOnly multiObs As Boolean
        Public Const HCLChars As String = "abcdefghijklmnopqrstuvwxyz0123456789 -=,."
        Public ReadOnly Property Map() As W3Map
            Get
                Contract.Ensures(Contract.Result(Of W3Map)() IsNot Nothing)
                Return _map
            End Get
        End Property
        Public ReadOnly Property Header As W3GameDescription
            Get
                Contract.Ensures(Contract.Result(Of W3GameDescription)() IsNot Nothing)
                Return _header
            End Get
        End Property
        Public ReadOnly Property AdminPassword() As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _adminPassword
            End Get
        End Property

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_map IsNot Nothing)
            Contract.Invariant(_header IsNot Nothing)
            Contract.Invariant(_adminPassword IsNot Nothing)
            Contract.Invariant(defaultListenPorts IsNot Nothing)
            Contract.Invariant(HCLMode IsNot Nothing)
        End Sub

        Public Sub New(ByVal map As W3Map,
                       ByVal header As W3GameDescription,
                       ByVal argument As Commands.CommandArgument,
                       Optional ByVal allowDownloads As Boolean = True,
                       Optional ByVal allowUpload As Boolean = True,
                       Optional ByVal isAutoStarted As Boolean = False,
                       Optional ByVal defaultSlotLockState As W3Slot.Lock = W3Slot.Lock.Unlocked,
                       Optional ByVal instances As Integer = 1,
                       Optional ByVal password As String = Nothing,
                       Optional ByVal autoElevateUserName As String = Nothing,
                       Optional ByVal isAdminGame As Boolean = False,
                       Optional ByVal shouldGrabMap As Boolean = False,
                       Optional ByVal defaultListenPorts As IEnumerable(Of UShort) = Nothing)
            Contract.Requires(map IsNot Nothing)
            Contract.Requires(header IsNot Nothing)
            Me._map = map
            Me._header = header
            Me.creationTime = DateTime.Now()
            Me.allowDownloads = allowDownloads AndAlso Not argument.HasOptionalSwitch("NoDL")
            Me.allowUpload = allowUpload AndAlso Not argument.HasOptionalSwitch("NoUL")
            Me.instances = instances
            Me.isAutoStarted = isAutoStarted OrElse argument.HasOptionalSwitch("AutoStart") OrElse argument.HasOptionalSwitch("as")
            Me.defaultSlotLockState = defaultSlotLockState
            If password Is Nothing Then password = New Random().Next(0, 1000).ToString("000", CultureInfo.InvariantCulture)
            Me._adminPassword = password
            Me.isAdminGame = isAdminGame
            Me.grabMap = shouldGrabMap OrElse argument.HasOptionalSwitch("grab")
            Me.defaultListenPorts = If(defaultListenPorts, New List(Of UShort)).ToList()
            Me.HCLMode = If(argument.TryGetOptionalNamedValue("Mode"), "")
            Me.loadInGame = argument.HasOptionalSwitch("LoadInGame") OrElse argument.HasOptionalSwitch("lig")
            Me.permanent = argument.HasOptionalSwitch("Permanent") OrElse argument.HasOptionalSwitch("Perm")
            Me.testFakePlayers = argument.HasOptionalSwitch("Test")
            Me.teamSetup = If(argument.TryGetOptionalNamedValue("Teams"), argument.TryGetOptionalNamedValue("t"))
            Me.multiObs = argument.HasOptionalSwitch("MultiObs")
            'Reservations
            If argument.HasOptionalSwitch("Reserve") OrElse argument.HasOptionalSwitch("r") Then
                Reservations.Add(header.GameStats.HostName)
            End If
            For Each username In Concat(If(argument.TryGetOptionalNamedValue("Reserve"), "").Split(" "c),
                                        If(argument.TryGetOptionalNamedValue("r"), "").Split(" "c))
                If username <> "" Then Reservations.Add(username)
            Next username
            'Instance count
            If argument.TryGetOptionalNamedValue("Inst") Is Nothing Then
                Me.instances = 1
            Else
                If Not Integer.TryParse(argument.TryGetOptionalNamedValue("Inst"), Me.instances) OrElse Me.instances < 0 Then
                    Throw New ArgumentException("Invalid number of instances.")
                End If
            End If
            'Admin name
            Me.autoElevateUserName = autoElevateUserName
            If argument.HasOptionalSwitch("Admin") OrElse argument.HasOptionalSwitch("a") Then
                Me.autoElevateUserName = header.GameStats.HostName
            End If
            If Me.autoElevateUserName Is Nothing Then Me.autoElevateUserName = argument.TryGetOptionalNamedValue("Admin")
            If Me.autoElevateUserName Is Nothing Then Me.autoElevateUserName = argument.TryGetOptionalNamedValue("a")
            'Listen Port
            If argument.TryGetOptionalNamedValue("Port") IsNot Nothing Then
                Dim port As UShort
                If UShort.TryParse(argument.TryGetOptionalNamedValue("Port"), port) Then
                    Me.defaultListenPorts.Add(port)
                End If
            End If
        End Sub
        Public Shared ReadOnly PartialArgumentTemplates As String() = {
                "-Admin=user -Admin -a=user -a",
                "-AutoStart -as",
                "-Inst=#Instances",
                "-Grab",
                "-LoadInGame -lig",
                "-Mode=mode",
                "-NoUL",
                "-NoDL",
                "-Permanent -perm",
                "-Port=#",
                "-reserve -reserve=<name1 name2 ...> -r -r=<name1 name2 ...>",
                "-teams=#v#... -t=#v#...", "-port=#"
            }
        Public Shared ReadOnly PartialArgumentHelp As String() = {
                "Admin=-Admin, -a, -Admin=user, -a=user: Sets the auto-elevated username. Use no argument to match your name.",
                "Autostart=-Autostart, -as: Instances will start automatically when they fill up.",
                "Instances=-Inst=value: Sets the initial number of instances. Use 0 for unlimited instances.",
                "Grab=-Grab: Downloads the map file from joining players. Meant for use when hosting a map by meta-data.",
                "LoadInGame=-LoadInGame, -lig: Players wait for loaders in the game instead of at the load screen.",
                "Mode=-mode=mode: Passes a mode into maps supporting HCL. Usage on incompatible maps will ruin player handicaps.",
                "NoUL=-NoUL: Turns off uploads from the bot, but still allows players to download from each other.",
                "NoDL=-NoDL: Boots players who don't already have the map.",
                "Permanent=-Permanent, -Perm: Automatically recreate closed instances and automatically sets the game to private/public as new instances are available.",
                "Port=port: Sets the port the game server will listen on.",
                "Reserve=-Reserve, -r, -Reserve=<user1 user2 ...>, -r=<user1 user2 ...>: Reserves the slots for players or yourself.",
                "Teams=-Teams=#v#..., -t=#v#...: Sets the initial number of open slots for each team."
            }

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

            Dim dat(0 To Math.Min(handicaps.Length, HCLMode.Length) - 1) As Byte
            For i = 0 To dat.Length - 1
                Dim v = If(blocked(handicaps(i)), handicaps(i), 100)
                Dim c = If(HCLChars.Contains(HCLMode(i)), HCLMode(i), " ")
                dat(i) = indexMap((v - 50) \ 10 + HCLChars.IndexOf(c, StringComparison.CurrentCultureIgnoreCase) * 6)
            Next i
            Return dat
        End Function
    End Class
End Namespace
