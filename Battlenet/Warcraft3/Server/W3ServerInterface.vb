Imports HostBot.Links

Namespace Warcraft3
    <ContractClass(GetType(ContractClassIW3Server))>
    Public Interface IW3Server
        Inherits INotifyingDisposable

        ReadOnly Property parent() As MainBot
        ReadOnly Property Name() As String
        ReadOnly Property settings() As ServerSettings
        ReadOnly Property Logger() As Logger
        ReadOnly Property suffix() As String

        Event ChangedState(ByVal sender As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
        Event AddedGame(ByVal sender As IW3Server, ByVal game As IW3Game)
        Event RemovedGame(ByVal sender As IW3Server, ByVal game As IW3Game)
        Event PlayerTalked(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player, ByVal text As String)
        Event PlayerLeft(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal game_state As W3GameStates, ByVal player As IW3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String)
        Event PlayerSentData(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player, ByVal data As Byte())
        Event PlayerEntered(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player)

        Function QueueFindPlayer(ByVal username As String) As IFuture(Of IW3Player)
        Function QueueFindPlayerGame(ByVal username As String) As IFuture(Of Outcome(Of IW3Game))
        Function QueueFindGame(ByVal gameName As String) As IFuture(Of IW3Game)
        Function QueueGetGames() As IFuture(Of IEnumerable(Of IW3Game))
        Function QueueCreateGame(Optional ByVal gameName As String = Nothing) As IFuture(Of Outcome(Of IW3Game))
        Function QueueRemoveGame(ByVal gameName As String, Optional ByVal ignorePermanent As Boolean = False) As IFuture(Of Outcome)
        Function QueueClosePort(ByVal port As UShort) As IFuture(Of Outcome)
        Function QueueOpenPort(ByVal port As UShort) As IFuture(Of Outcome)
        Function QueueCloseAllPorts() As IFuture(Of Outcome)
        Function QueueStopAcceptingPlayers() As IFuture(Of Outcome)
        Function QueueKill() As IFuture(Of Outcome)
        Function QueueAddAvertiser(ByVal m As IGameSourceSink) As IFuture(Of outcome)

        Function CreateAdvertisingDependency() As INotifyingDisposable
    End Interface
    <ContractClassFor(GetType(IW3Server))>
    Public NotInheritable Class ContractClassIW3Server
        Implements IW3Server
        Public Event Disposed() Implements INotifyingDisposable.Disposed
        Public Event PlayerEntered(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player) Implements IW3Server.PlayerEntered
        Public Event PlayerLeft(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal game_state As W3GameStates, ByVal player As IW3Player, ByVal leaveType As W3PlayerLeaveTypes, ByVal reason As String) Implements IW3Server.PlayerLeft
        Public Event PlayerSentData(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player, ByVal data() As Byte) Implements IW3Server.PlayerSentData
        Public Event PlayerTalked(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player, ByVal text As String) Implements IW3Server.PlayerTalked
        Public Event RemovedGame(ByVal sender As IW3Server, ByVal game As IW3Game) Implements IW3Server.RemovedGame
        Public Event AddedGame(ByVal sender As IW3Server, ByVal game As IW3Game) Implements IW3Server.AddedGame
        Public Event ChangedState(ByVal sender As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates) Implements IW3Server.ChangedState
        Public ReadOnly Property logger As Logger Implements IW3Server.Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property name As String Implements IW3Server.Name
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property parent As MainBot Implements IW3Server.parent
            Get
                Contract.Ensures(Contract.Result(Of MainBot)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property settings As ServerSettings Implements IW3Server.settings
            Get
                Contract.Ensures(Contract.Result(Of ServerSettings)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property suffix As String Implements IW3Server.suffix
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Throw New NotSupportedException
            End Get
        End Property
        Public ReadOnly Property IsDisposed As Boolean Implements INotifyingDisposable.IsDisposed
            Get
                Throw New NotSupportedException
            End Get
        End Property
        Public Function CreateAdvertisingDependency() As INotifyingDisposable Implements IW3Server.CreateAdvertisingDependency
            Contract.Ensures(Contract.Result(Of INotifyingDisposable)() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_AddAvertiser(ByVal m As Links.IGameSourceSink) As IFuture(Of Outcome) Implements IW3Server.QueueAddAvertiser
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_CloseAllPorts() As IFuture(Of Outcome) Implements IW3Server.QueueCloseAllPorts
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_ClosePort(ByVal port As UShort) As IFuture(Of Outcome) Implements IW3Server.QueueClosePort
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_CreateGame(Optional ByVal gameName As String = Nothing) As IFuture(Of Outcome(Of IW3Game)) Implements IW3Server.QueueCreateGame
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome(Of IW3Game)))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_EnumGames() As IFuture(Of System.Collections.Generic.IEnumerable(Of IW3Game)) Implements IW3Server.QueueGetGames
            Contract.Ensures(Contract.Result(Of IFuture(Of IEnumerable(Of IW3Game)))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_FindGame(ByVal gameName As String) As IFuture(Of IW3Game) Implements IW3Server.QueueFindGame
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Game))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player) Implements IW3Server.QueueFindPlayer
            Contract.Ensures(Contract.Result(Of IFuture(Of IW3Player))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_FindPlayerGame(ByVal username As String) As IFuture(Of Outcome(Of IW3Game)) Implements IW3Server.QueueFindPlayerGame
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome(Of IW3Game)))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_Kill() As IFuture(Of Outcome) Implements IW3Server.QueueKill
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_OpenPort(ByVal port As UShort) As IFuture(Of Outcome) Implements IW3Server.QueueOpenPort
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_RemoveGame(ByVal gameName As String, Optional ByVal ignorePermanent As Boolean = False) As IFuture(Of Outcome) Implements IW3Server.QueueRemoveGame
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Function f_StopAcceptingPlayers() As IFuture(Of Outcome) Implements IW3Server.QueueStopAcceptingPlayers
            Contract.Ensures(Contract.Result(Of IFuture(Of Outcome))() IsNot Nothing)
            Throw New NotSupportedException
        End Function
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class

    Public Enum W3ServerStates As Byte
        only_accepting = 0
        accepting_and_playing = 1
        only_playing_out = 2
        killed = 3
    End Enum

    Public Class ServerSettings
        Public ReadOnly map As W3Map
        Public ReadOnly header As W3GameHeader
        Public ReadOnly creationTime As Date = DateTime.Now()
        Public ReadOnly isAdminGame As Boolean
        Public ReadOnly allowDownloads As Boolean
        Public ReadOnly allowUpload As Boolean
        Public ReadOnly instances As Integer
        Public ReadOnly autostarted As Boolean
        Public ReadOnly adminPassword As String
        Public ReadOnly managed_lifecycle As Boolean
        Public ReadOnly permanent As Boolean
        Public ReadOnly defaultSlotLockState As W3Slot.Lock
        Public ReadOnly auto_elevate_username As String
        Public ReadOnly grabMap As Boolean
        Public ReadOnly default_listen_ports As New List(Of UShort)
        Public ReadOnly loadInGame As Boolean
        Public ReadOnly testFakePlayers As Boolean
        Public ReadOnly HCLMode As String = ""
        Public Const HCLChars As String = "abcdefghijklmnopqrstuvwxyz0123456789 -=,."
        Public Function EncodedHCLMode(ByVal handicaps As Byte()) As Byte()
            Dim map(0 To 255) As Byte
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
                map(i) = CByte(j)
                i += 1
            Next j

            Dim dat(0 To Math.Min(handicaps.Length, HCLMode.Length) - 1) As Byte
            For i = 0 To dat.Length - 1
                Dim v = If(blocked(handicaps(i)), handicaps(i), 100)
                Dim c = If(HCLChars.Contains(HCLMode(i)), HCLMode(i), " ")
                dat(i) = map((v - 50) \ 10 + HCLChars.IndexOf(c) * 6)
            Next i
            Return dat
        End Function

        Public Sub New(ByVal map As W3Map,
                       ByVal header As W3GameHeader,
                       Optional ByVal allowDownloads As Boolean = True,
                       Optional ByVal allowUpload As Boolean = True,
                       Optional ByVal autostarted As Boolean = False,
                       Optional ByVal defaultSlotLockState As W3Slot.Lock = W3Slot.Lock.unlocked,
                       Optional ByVal instances As Integer = 1,
                       Optional ByVal password As String = Nothing,
                       Optional ByVal auto_elevate_user As String = Nothing,
                       Optional ByVal managed_lifecycle As Boolean = True,
                       Optional ByVal is_admin_game As Boolean = False,
                       Optional ByVal grab_map As Boolean = False,
                       Optional ByVal default_listen_ports As IEnumerable(Of UShort) = Nothing)
            If Not (map IsNot Nothing) Then Throw New ArgumentException()
            Me.map = map
            Me.header = header
            Me.creationTime = DateTime.Now()
            Me.allowDownloads = allowDownloads
            Me.allowUpload = allowUpload
            Me.instances = instances
            Me.autostarted = autostarted
            Me.defaultSlotLockState = defaultSlotLockState
            If password Is Nothing Then password = New Random().Next(0, 1000).ToString("000")
            Me.adminPassword = password
            Me.auto_elevate_username = auto_elevate_user
            Me.isAdminGame = is_admin_game
            Me.grabMap = grab_map
            Me.default_listen_ports = If(default_listen_ports, New List(Of UShort)).ToList()

            For Each arg In header.Options
                Dim arg2 = ""
                If arg.Contains("="c) Then
                    Dim n = arg.IndexOf("="c)
                    arg2 = arg.Substring(n + 1)
                    arg = arg.Substring(0, n + 1)
                End If
                arg = arg.ToLower.Trim()
                arg2 = arg2.ToLower.Trim()

                Select Case arg
                    Case "-mode="
                        Me.HCLMode = arg2
                    Case "-permanent", "-perm"
                        Me.permanent = True
                    Case "-autostart", "-as"
                        Me.autostarted = True
                    Case "-noul"
                        Me.allowUpload = False
                    Case "-nodl"
                        Me.allowDownloads = False
                    Case "-instances=", "-i="
                        Dim i = 0
                        If Integer.TryParse(arg2, i) AndAlso i >= 0 Then
                            Me.instances = i
                        End If
                    Case "-admin=", "-a="
                        Me.auto_elevate_username = arg2
                    Case "-admin", "-a"
                        Me.auto_elevate_username = header.hostUserName
                    Case "-grab"
                        Me.grabMap = True
                    Case "-port="
                        Dim port As UShort
                        If UShort.TryParse(arg2, port) Then
                            Me.default_listen_ports.Add(port)
                        End If
                    Case "-loadingame", "-lig"
                        Me.loadInGame = True
                    Case "-fake"
                        Me.testFakePlayers = True
                End Select
            Next arg
        End Sub
    End Class
End Namespace
