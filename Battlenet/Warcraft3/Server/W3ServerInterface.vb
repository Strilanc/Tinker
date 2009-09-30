Imports HostBot.Links

Namespace Warcraft3
    Public Enum W3ServerState As Byte
        OnlyAcceptingPlayers = 0
        AcceptingPlayersAndPlayingGames = 1
        OnlyPlayingGames = 2
        Disposed = 3
    End Enum

    Public Class ServerSettings
        Private ReadOnly _map As W3Map
        Private ReadOnly _header As W3GameHeader
        Public ReadOnly creationTime As Date = DateTime.Now()
        Public ReadOnly isAdminGame As Boolean
        Public ReadOnly allowDownloads As Boolean
        Public ReadOnly allowUpload As Boolean
        Public ReadOnly instances As Integer
        Public ReadOnly isAutoStarted As Boolean
        Private ReadOnly _adminPassword As String
        Public ReadOnly permanent As Boolean
        Public ReadOnly defaultSlotLockState As W3Slot.Lock
        Public ReadOnly autoElevateUserName As String
        Public ReadOnly grabMap As Boolean
        Public ReadOnly defaultListenPorts As New List(Of UShort)
        Public ReadOnly loadInGame As Boolean
        Public ReadOnly testFakePlayers As Boolean
        Public ReadOnly HCLMode As String = ""
        Public Const HCLChars As String = "abcdefghijklmnopqrstuvwxyz0123456789 -=,."

        Public ReadOnly Property Map() As W3Map
            Get
                Contract.Ensures(Contract.Result(Of W3Map)() IsNot Nothing)
                Return _map
            End Get
        End Property
        Public ReadOnly Property Header As W3GameHeader
            Get
                Contract.Ensures(Contract.Result(Of W3GameHeader)() IsNot Nothing)
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

        Public Sub New(ByVal map As W3Map,
                       ByVal header As W3GameHeader,
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
            Me.allowDownloads = allowDownloads
            Me.allowUpload = allowUpload
            Me.instances = instances
            Me.isAutoStarted = isAutoStarted
            Me.defaultSlotLockState = defaultSlotLockState
            If password Is Nothing Then password = New Random().Next(0, 1000).ToString("000", CultureInfo.InvariantCulture)
            Me._adminPassword = password
            Me.autoElevateUserName = autoElevateUserName
            Me.isAdminGame = isAdminGame
            Me.grabMap = shouldGrabMap
            Me.defaultListenPorts = If(defaultListenPorts, New List(Of UShort)).ToList()

            For Each arg In header.Options
                Dim arg2 = ""
                If arg.Contains("="c) Then
                    Dim n = arg.IndexOf("="c)
                    arg2 = arg.Substring(n + 1)
                    arg = arg.Substring(0, n + 1)
                End If
                arg = arg.ToUpperInvariant.Trim()
                arg2 = arg2.ToUpperInvariant.Trim()

                Select Case arg
                    Case "-MODE="
                        Me.HCLMode = arg2
                    Case "-PERMANENT", "-PERM"
                        Me.permanent = True
                    Case "-AUTOSTART", "-AS"
                        Me.isAutoStarted = True
                    Case "-NOUL"
                        Me.allowUpload = False
                    Case "-NODL"
                        Me.allowDownloads = False
                    Case "-INSTANCES=", "-I="
                        Dim i = 0
                        If Integer.TryParse(arg2, i) AndAlso i >= 0 Then
                            Me.instances = i
                        End If
                    Case "-ADMIN=", "-A="
                        Me.autoElevateUserName = arg2
                    Case "-ADMIN", "-A"
                        Me.autoElevateUserName = header.HostUserName
                    Case "-GRAB"
                        Me.grabMap = True
                    Case "-PORT="
                        Dim port As UShort
                        If UShort.TryParse(arg2, port) Then
                            Me.defaultListenPorts.Add(port)
                        End If
                    Case "-LOADINGAME", "-LIG"
                        Me.loadInGame = True
                    Case "-FAKE"
                        Me.testFakePlayers = True
                End Select
            Next arg
        End Sub
    End Class
End Namespace
