Imports HostBot.Links

Namespace Warcraft3
    Public Interface IW3Server
        Inherits Links.IDependencyLinkServant

        ReadOnly Property parent() As MainBot
        ReadOnly Property name() As String
        ReadOnly Property settings() As ServerSettings
        ReadOnly Property logger() As MultiLogger
        ReadOnly Property suffix() As String

        Event ChangedState(ByVal sender As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
        Event AddedGame(ByVal sender As IW3Server, ByVal game As IW3Game)
        Event RemovedGame(ByVal sender As IW3Server, ByVal game As IW3Game)
        Event PlayerTalked(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal player As IW3Player, ByVal text As String)
        Event PlayerLeft(ByVal sender As IW3Server, ByVal game As IW3Game, ByVal game_state As W3GameStates, ByVal player As IW3Player, ByVal reason As W3PlayerLeaveTypes)
        Event PlayerSentData(ByVal sender As IW3Server, ByVal game As IW3GamePlay, ByVal player As IW3PlayerGameplay, ByVal data As Byte())
        Event PlayerEntered(ByVal sender As IW3Server, ByVal game As IW3GameLobby, ByVal player As IW3PlayerLobby)

        Function f_FindPlayer(ByVal username As String) As IFuture(Of IW3Player)
        Function f_FindPlayerGame(ByVal username As String) As IFuture(Of IW3Game)
        Function f_FindGame(ByVal game_name As String) As IFuture(Of IW3Game)
        Function f_EnumGames() As IFuture(Of IEnumerable(Of IW3Game))
        Function f_CreateGame(Optional ByVal game_name As String = Nothing) As IFuture(Of Outcome(Of IW3Game))
        Function f_RemoveGame(ByVal game_name As String) As IFuture(Of Outcome)
        Function f_ClosePort(ByVal port As UShort) As IFuture(Of Outcome)
        Function f_OpenPort(ByVal port As UShort) As IFuture(Of Outcome)
        Function f_CloseAllPorts() As IFuture(Of Outcome)
        Function f_StopAcceptingPlayers() As IFuture(Of Outcome)
        Function f_Kill() As IFuture(Of Outcome)
        Function f_AddAvertiser(ByVal m As IAdvertisingLinkMember) As IFuture(Of outcome)

        Function advertising_dep() As IDependencyLinkServant
    End Interface

    Public Enum W3ServerStates As Byte
        only_accepting = 0
        accepting_and_playing = 1
        only_playing_out = 2
        killed = 3
    End Enum

    Public Class W3ConnectingPlayer
        Public ReadOnly name As String
        Public ReadOnly p2p_key As UInteger
        Public ReadOnly listen_port As UShort
        Public ReadOnly remote_port As UShort
        Public ReadOnly remote_ip As Byte()
        Public ReadOnly socket As W3Socket
        Public Sub New(ByVal name As String, _
                       ByVal connection_key As UInteger, _
                       ByVal listen_port As UShort, _
                       ByVal remote_port As UShort, _
                       ByVal remote_ip As Byte(), _
                       ByVal socket As W3Socket)
            Me.name = name
            Me.p2p_key = connection_key
            Me.listen_port = listen_port
            Me.remote_ip = remote_ip
            Me.remote_port = remote_port
            Me.socket = socket
        End Sub
    End Class

    Public Class ServerSettings
        Public ReadOnly map As W3Map
        Public ReadOnly map_settings As W3Map.MapSettings
        Public ReadOnly creationTime As Date = DateTime.Now()
        Public ReadOnly is_admin_game As Boolean
        Public allowDownloads As Boolean
        Public allowUpload As Boolean
        Public instances As Integer
        Public autostarted As Boolean
        Public admin_password As String
        Public managed_lifecycle As Boolean
        Public permanent As Boolean
        Public defaultSlotLockState As W3Slot.Lock
        Public auto_elevate_username As String
        Public grab_map As Boolean
        Public default_listen_ports As New List(Of UShort)
        Public arguments As IList(Of String)
        Public load_in_game As Boolean

        Public Sub New(ByVal map As W3Map, _
                       ByVal username As String, _
                       Optional ByVal allowDownloads As Boolean = True, _
                       Optional ByVal allowUpload As Boolean = True, _
                       Optional ByVal autostarted As Boolean = False, _
                       Optional ByVal defaultSlotLockState As W3Slot.Lock = W3Slot.Lock.unlocked, _
                       Optional ByVal instances As Integer = 1, _
                       Optional ByVal password As String = Nothing, _
                       Optional ByVal arguments As IList(Of String) = Nothing, _
                       Optional ByVal auto_elevate_user As String = Nothing, _
                       Optional ByVal managed_lifecycle As Boolean = True, _
                       Optional ByVal is_admin_game As Boolean = False, _
                       Optional ByVal grab_map As Boolean = False, _
                       Optional ByVal default_listen_ports As IEnumerable(Of UShort) = Nothing)
            If Not (map IsNot Nothing) Then Throw New ArgumentException()
            If arguments Is Nothing Then arguments = New List(Of String)
            Me.map = map
            Me.map_settings = New W3Map.MapSettings(arguments)
            Me.creationTime = DateTime.Now()
            Me.allowDownloads = allowDownloads
            Me.allowUpload = allowUpload
            Me.instances = instances
            Me.autostarted = autostarted
            Me.defaultSlotLockState = defaultSlotLockState
            If password Is Nothing Then password = New Random().Next(0, 1000).ToString("000")
            Me.admin_password = password
            Me.arguments = arguments
            Me.auto_elevate_username = auto_elevate_user
            Me.is_admin_game = is_admin_game
            Me.grab_map = grab_map
            Me.default_listen_ports = If(default_listen_ports, New List(Of UShort)).ToList()

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
                        Me.auto_elevate_username = username
                    Case "-grab"
                        Me.grab_map = True
                    Case "-port="
                        Dim port As UShort
                        If UShort.TryParse(arg2, port) Then
                            Me.default_listen_ports.Add(port)
                        End If
                    Case "-loadingame", "-lig"
                        Me.load_in_game = True
                End Select
            Next arg
        End Sub
    End Class
End Namespace
