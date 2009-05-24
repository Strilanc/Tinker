'Imports HostBot.Functional
'Imports HostBot.Functional.Futures
'Imports HostBot.Functional.Currying
'Imports HostBot.W3GameInstance

'Public Class BotCommands
'    Public PEEK_commander As BotUser = Nothing
'    Private WithEvents parent As MainBot
'    Private ReadOnly ref As New ThreadedCallQueue(Me.gettype.name + " ref")
'    Private ReadOnly LOCAL_userMap As New Dictionary(Of String, BotUser)

'    Public Sub New(ByVal parent As MainBot)
'        Me.parent = parent
'        loadUsers()
'    End Sub

'    Public Function getUser(ByVal name As String, Optional ByVal autoCreate As Boolean = True) As BotUser
'        name = name.ToLower()
'        If Not LOCAL_userMap.ContainsKey(name) Then
'            If Not autoCreate Then Return Nothing
'            If LOCAL_userMap.ContainsKey("?") Then
'                LOCAL_userMap(name) = BotUser.fromUserString(name + LOCAL_userMap("?").userstring.Substring(1) + "?")
'            Else
'                LOCAL_userMap(name) = New BotUser(name, name + " 0 ?")
'            End If
'        End If
'        Return LOCAL_userMap(name)
'    End Function

'    Public Sub loadUsers()
'        LOCAL_userMap.Clear()
'        PEEK_commander = Nothing
'        For Each s As String In My.Settings.users
'            Dim u As BotUser = BotUser.fromUserString(s)
'            If u IsNot Nothing Then LOCAL_userMap(u.name.ToLower()) = u
'        Next s
'    End Sub

'    Public Sub receiveChannelText(ByVal user As String, ByVal text As String)
'        ref.queueCall(curry(AddressOf LOCAL_receiveChannelText, user, text))
'    End Sub
'    Public Sub receiveGameText(ByVal p As W3Player, ByVal text As String)
'        ref.queueCall(curry(AddressOf LOCAL_receiveGameText, p, text))
'    End Sub

'    Private Function removeCommandPrefix(ByRef refText As String) As Boolean
'        If refText.Length <= 0 Then Return False
'        If refText.ToLower().IndexOf(My.Settings.commandPrefix.ToLower()) <> 0 Then Return False
'        refText = refText.Substring(My.Settings.commandPrefix.Length)
'        Return True
'    End Function

'    Private Sub LOCAL_receiveChannelText(ByVal user As String, ByVal text As String)
'        If PEEK_commander IsNot Nothing AndAlso user = PEEK_commander.name Then
'            Dim l As W3GameServer = parent.server
'            If l IsNot Nothing Then
'                If user = PEEK_commander.name And text.ToLower = "-cancel" Then
'                    parent.client.sendText("/m " + PEEK_commander.name + " cancelled.")
'                    parent.server.REF_kill()
'                    Return
'                Else
'                    schedule(Nothing, AddressOf LOCAL_receiveGameText, l.FUTURE_FindPlayerWithName(user), futurize(text))
'                End If
'            End If
'        End If

'        If Not removeCommandPrefix(text) Then Return
'        If Not My.Settings.allowRemoteHosting Then
'            If text = "command" Then parent.client.sendText("/m " + user + " Remote command is disabled.")
'            Return
'        End If
'        Dim s As String = LOCAL_processChannelCommand(user, text)
'        If s IsNot Nothing Then parent.client.sendText("/m " + user + " " + s.Replace("--", My.Settings.commandPrefix))
'    End Sub

'    Private Sub LOCAL_receiveGameText(ByVal p As W3Player, ByVal text As String)
'        If p Is Nothing Then Return
'        text = text.ToLower()

'        'Hints
'        If Not removeCommandPrefix(text) Then Return

'        'Guest Commands
'        Dim s As String = LOCAL_processGameGuestCommand(p, text)
'        If s IsNot Nothing Then
'            s = s.Replace("--", My.Settings.commandPrefix)
'            p.sendMessage(s)
'        End If

'        'Host Commands
'        If PEEK_commander IsNot Nothing AndAlso PEEK_commander.name.ToLower() = p.PERM_name.ToLower() AndAlso My.Settings.allowRemoteHosting Then
'            s = LOCAL_processGameHostCommand(p, text)
'            If s IsNot Nothing Then
'                s = s.Replace("--", My.Settings.commandPrefix)
'                p.PERM_game.REF_broadcastMessage(s)
'            End If
'        End If
'    End Sub

'    Private Function LOCAL_processGameGuestCommand(ByVal p As W3Player, ByVal text As String) As String
'        Select Case text
'            Case "leave"
'                If p.PERM_game.PEEK_STATE() < States.CountingDown Then
'                    p.REF_disconnect()
'                Else
'                    Return "You may not leave during the countdown!"
'                End If
'        End Select

'        Return Nothing
'    End Function
'    Private Function LOCAL_processGameHostCommand(ByVal p As W3Player, ByVal text As String) As String
'        Dim words() As String = text.Split(" "c)
'        Dim arg As String = ""
'        If words.Length > 1 Then arg = text.Substring(words(0).Length + 1)
'        Select Case words(0).ToLower()
'            Case "help"
'                Return "--start, --cancel, --boot $, --reserve $, --open, --close"
'            Case "start"
'                p.PERM_game.REF_startCountdown()
'            Case "cancel"
'                parent.client.REF_killGame()

'            Case "open", "close", "computer", "lock", "unlock"
'                Dim fs As itfFuture(Of Integer) = p.PERM_game.REF_FUTURE_getSlotIndexMatching(arg)
'                Select Case words(0).ToLower()
'                    Case "open"
'                        If words.Length < 2 Then Return "Incorrect command format, need color argument."
'                        schedule(Nothing, AddressOf p.PERM_game.REF_openSlot, fs)
'                    Case "close"
'                        If words.Length < 2 Then Return "Incorrect command format, need color argument."
'                        schedule(Nothing, AddressOf p.PERM_game.REF_closeSlot, fs)
'                    Case "computer"
'                        If words.Length < 2 Then Return "Incorrect command format, need color argument."
'                        schedule(Nothing, AddressOf p.PERM_game.REF_setSlotCPU, fs, futurize(W3Slot.CpuDifficulties.Normal))
'                    Case "lock"
'                        If words.Length < 2 Then Return "Incorrect command format, need name or color argument."
'                        schedule(Nothing, AddressOf p.PERM_game.REF_lockSlot, fs, futurize(W3Slot.LockStates.sticky))
'                    Case "unlock"
'                        If words.Length < 2 Then Return "Incorrect command format, need name or color argument."
'                        schedule(Nothing, AddressOf p.PERM_game.REF_lockSlot, fs, futurize(W3Slot.LockStates.unlocked))
'                End Select
'            Case "boot"
'                If words.Length < 2 Then Return "Incorrect command format, need name argument."
'                schedule(Of String, W3GameInstance, List(Of W3Player))(AddressOf ref.queueCall, _
'                            AddressOf LOCAL_bootPlayers, _
'                            futurize(arg), _
'                            futurize(p.PERM_game), _
'                            p.PERM_game.REF_FUTURE_makeShallowCopyOfPlayers())
'            Case "swap"
'                If words.Length <> 3 Then Return "Incorrect command format, need two color arguments."
'                schedule(Nothing, _
'                        AddressOf p.PERM_game.REF_swapSlots, _
'                            p.PERM_game.REF_FUTURE_getSlotIndexMatching(words(1)), _
'                            p.PERM_game.REF_FUTURE_getSlotIndexMatching(words(2)))
'            Case "reserve"
'                If words.Length < 2 Then Return "Incorrect command format, need name argument."
'                Dim q As New W3Player(arg, p.PERM_game, Me.parent.logger)
'        End Select

'        Return Nothing
'    End Function

'    Private Sub LOCAL_bootPlayers(ByVal username As String, ByVal g As W3GameInstance, ByVal players As List(Of W3Player))
'        username = username.ToLower()
'        For Each q As W3Player In players
'            If username = q.PERM_name.ToLower() Then q.REF_disconnect()
'        Next q
'        g.REF_updateGameState()
'    End Sub

'    Private Function LOCAL_processChannelCommand(ByVal username As String, ByVal text As String) As String
'        Dim user As BotUser = getUser(username)
'        If user.access.ignore Then Return Nothing
'        If user Is PEEK_commander Then PEEK_commander.timeout = DateTime.Now + New TimeSpan(0, 0, 30)

'        Dim words() As String = text.Split(" "c)
'        Dim rest As String = Nothing
'        If words.Length > 1 Then rest = text.Substring(words(0).Length + 1)
'        Try
'            Select Case words(0).ToLower()
'                Case "help"
'                    If words.Length = 1 Then
'                        Return "Use --help basic or --help advanced for list of commands. Use --help [command] for help with specific commands."
'                    End If
'                    Select Case words(1).ToLower()
'                        Case "basic"
'                            Return "--command, --release, --list #, --find $, --host $ @ $, --private, --public, --dl $"
'                        Case "advanced"
'                            Return "--auto, --manual, --submit # 0x#, --view $, --add $ # F, --remove $, --silent $, --order $"

'                        Case "view"
'                            Return "[--view USER] Shows the user's access string. " _
'                                        + "EXAMPLE: --view Strilanc"
'                        Case "add"
'                            Return "[--add USER LEVEL FLAGS] Adds a user. " _
'                                        + "Use --help flags for help with permission flags. " _
'                                        + "EXAMPLE: --add Strilanc 10 HD"
'                        Case "remove"
'                            Return "[--remove USER] Removes a user. " _
'                                        + "EXAMPLE: --remove Strilanc"
'                        Case "flags"
'                            Return "Possible flags are ?ACDGHORST. " _
'                                        + "? = temporary, A=add user, C=cooldown, D=dls from bot, G=ignore, H=host, O=override command, R=remove user, S=submit, T=auto instance hosting, /=orders"
'                        Case "dl"
'                            Return "[--dl (off|on|half)] Determines if players are allowed to download. 'on' = allowed, 'off' = dlers kicked, 'half' = dls allowed bot doesn't upload. (default is on if you have dl permission, half otherwise)"
'                        Case "auto"
'                            Return "[--auto] Turns on automated starting and instancing. (default is manual)"
'                        Case "override"
'                            Return "[--override] Overrides control of the bot if a lesser user has it, so you can use hosting commands."
'                        Case "manual"
'                            Return "[--manual] Turns off automated starting and instancing (default is manual)."
'                        Case "order"
'                            Return "[--order $] Makes the bot give a bnet order. EXAMPLE: --order join some_channel"
'                        Case "host"
'                            Return "[--host MAPQUERY @ GAME] Hosts the first map matching the query with the specified game name. " _
'                                        + "Use [-find MAPQUERY] to be sure which map the bot will host. " _
'                                        + "EXAMPLE: --host dota*6.49c @ Dota arem!!"
'                        Case "command"
'                            Return "[--command] Grants control of the bot if no one else has it, so you can use hosting commands. " _
'                                        + "If you give no commands for 30 seconds someone else can --command the bot."
'                        Case "release"
'                            Return "[--release] Releases control of the bot, so others can command it."
'                        Case "find"
'                            Return "[--find MAPQUERY] Returns the first map matching the given search string. " _
'                                        + "Wildcards are *(anything), ?(any char), #(any digit), [abc..](any specified char). " _
'                                        + "EXAMPLE: --find dota*6.49c"
'                        Case "list"
'                            Return "[--list NUMBER] Returns the nth 5 available maps. " _
'                                        + "EXAMPLE: --list 1"
'                        Case "private"
'                            Return "[--private] Sets the to-be game to private. (default is public)"
'                        Case "public"
'                            Return "[--public] Sets the to-be game to public. (default is public)"
'                        Case "silent"
'                            Return "[--silent on|off] Toggles whether or not the bot hides itself. (default is on)"
'                        Case "submit"
'                            Return "[--submit EPIC_WAR_NUMBER] " _
'                                        + "Tells the bot you want it to download the map with the given number from epicwar."
'                        Case Else
'                            Return "That is not a command. Use --help."
'                    End Select

'                Case "view"
'                    If words.Length <> 2 Then Return "Incorrect format. Expected text argument (name)"
'                    Dim u As BotUser = getUser(words(1), False)
'                    If u Is Nothing Then Return "That user does not exist."
'                    Return " " + u.userstring

'                Case "add"
'                    If Not user.access.add Then Return "Insufficient permissions: add (A)"
'                    If words.Length <> 4 OrElse words(1) = "" OrElse Not IsNumeric(words(2)) OrElse words(2).Length > 5 OrElse CInt(words(2)) < 0 Then
'                        Return "Incorrect format: Expected text, numeric, text arguments (name, level, flags)"
'                    End If
'                    If rest(0) = "?"c Then Return "Insufficient permissions: You can't add special users (begin with '?')."
'                    Dim u As BotUser = getUser(words(1), False)
'                    If u IsNot Nothing AndAlso Not u.access.temp Then Return "That user already exists."
'                    u = BotUser.fromUserString(rest)
'                    If Not u < user Then Return "Insufficient permissions: You can't add users with higher access"
'                    If u.access.temp Then Return "Insufficient permissions: You can't add temporary users"
'                    LOCAL_userMap(words(1).ToLower()) = u
'                    My.Settings.users.Add(rest)
'                    Return "Added " + rest

'                Case "remove"
'                    If Not user.access.remove Then Return "Insufficient permissions: remove (R)"
'                    If words.Length <> 2 Then Return "Incorrect format: Expected text argument (name)"
'                    Dim u As BotUser = getUser(words(1), False)
'                    If u Is Nothing Then Return "That user does not exist."
'                    If Not u < user Then Return "Insufficient permissions: You can't remove users with higher access"
'                    If words(1)(0) = "?" Then Return "Insufficient permissions: You can't remove special users (begin with '?')"
'                    LOCAL_userMap.Remove(u.name.ToLower())
'                    For i As Integer = My.Settings.users.Count - 1 To 0 Step -1
'                        If My.Settings.users(i).Split(":"c)(0).Trim().ToLower() = u.name.ToLower() Then
'                            My.Settings.users.RemoveAt(i)
'                        End If
'                    Next i
'                    Return "Removed " + words(1)

'                Case "command", "override"
'                    If words.Length <> 1 Then Return "Incorrect format: Expected no arguments"
'                    If Not user.access.host Then Return "Insufficient permissions: hosting (H)"
'                    If words(0).ToLower = "override" And Not user.access.override Then Return "Insufficient permissions: override (O)"
'                    If user Is PEEK_commander Then
'                        Return "You already have command."
'                    ElseIf PEEK_commander Is Nothing _
'                                OrElse DateTime.Now > PEEK_commander.timeout _
'                                OrElse (user.access.access > PEEK_commander.access.access And words(0).ToLower = "override") Then
'                        If user.access.cooldown AndAlso user.state = BotUserStates.cooling AndAlso user.timeout > DateTime.Now() Then
'                            Return "You are in command cooldown for " + CInt((user.timeout - DateTime.Now).TotalSeconds + 1).ToString() + " more seconds"
'                        End If
'                        If PEEK_commander IsNot Nothing Then
'                            parent.client.sendText("/w " + PEEK_commander.name + " " + user.name + " has taken command from you.")
'                            PEEK_commander.state = BotUserStates.other
'                        End If
'                        PEEK_commander = user
'                        PEEK_commander.timeout = DateTime.Now + New TimeSpan(0, 0, 30)
'                        PEEK_commander.state = BotUserStates.commanding
'                        My.Settings.defaultLobbyPrivate = False
'                        My.Settings.defaultLobbyAutoInstance = False
'                        My.Settings.defaultLobbyAutoStart = False
'                        My.Settings.defaultLobbyAllowDL = True
'                        My.Settings.lobbyAllowBotUpload = user.access.dl
'                        My.Settings.defaultSilent = True
'                        Return "You have command. Use --help for usage help."
'                    Else
'                        Return "Sorry, " + PEEK_commander.name + " has command."
'                    End If

'                Case "order"
'                    If words.Length < 2 Then Return "Incorrect format: Expected order argument."
'                    If Not user.access.speak Then Return "Insufficient permissions: Bnet Orders (/)"
'                    Me.parent.client.sendText("/" + rest)
'                    Return "Order performed: /" + rest

'                Case "find"
'                    If words.Length < 2 Then Return "Incorrect format: Expected pattern argument."
'                    Return LOCAL_findMatchingMapName(rest)

'                Case "list"
'                    If words.Length > 2 Then Return "Incorrect format: too many arguments."
'                    If words.Length < 2 OrElse Not IsNumeric(words(1)) Then Return "Incorrect format: Expected numeric argument (index)."
'                    Dim i As Integer = CInt(words(1))
'                    Dim t As Integer = 0

'                    Dim retText As String = ""
'                    For Each s As String In IO.Directory.GetFiles(My.Settings.war3path + My.Settings.relMapPath)
'                        s = getFileName(s).ToLower()
'                        If s Like "*.w3[mx]" Then
'                            If t \ 5 + 1 = i Then retText += s + ", "
'                            t += 1
'                        End If
'                    Next s
'                    If retText <> "" Then Return retText
'                    Return "You are past the end of the list (" + (t \ 5).ToString() + ")"

'                Case "release"
'                    If user IsNot PEEK_commander Then Return "You don't have command of the bot"
'                    If words.Length <> 1 Then Return "Incorrect format: Expected no arguments"
'                    PEEK_commander.state = BotUserStates.other
'                    PEEK_commander = Nothing
'                    Return "Released"

'                Case "private"
'                    If user IsNot PEEK_commander Then Return "Insufficient permissions: use --command first"
'                    If words.Length <> 1 Then Return "Incorrect format: Expected no arguments"
'                    My.Settings.defaultLobbyPrivate = True
'                    Return "Game will be private"
'                Case "public"
'                    If user IsNot PEEK_commander Then Return "Insufficient permissions: use --command first"
'                    If words.Length <> 1 Then Return "Incorrect format: Expected no arguments"
'                    My.Settings.defaultLobbyPrivate = False
'                    Return "Game will be public"

'                Case "dl"
'                    If user IsNot PEEK_commander Then Return "Insufficient permissions: use --command first"
'                    If words.Length <> 2 Then Return "Incorrect format. Expected on|off|half argument"
'                    Select Case words(1).ToLower()
'                        Case "on"
'                            If Not user.access.dl Then Return "Insufficient permissions: dl on (D)"
'                            My.Settings.defaultLobbyAllowDL = True
'                            My.Settings.lobbyAllowBotUpload = True
'                        Case "off"
'                            My.Settings.defaultLobbyAllowDL = False
'                            My.Settings.lobbyAllowBotUpload = False
'                        Case "half"
'                            My.Settings.defaultLobbyAllowDL = True
'                            My.Settings.lobbyAllowBotUpload = False
'                        Case Else
'                            Return "Incorrect format. Expected on|off|half argument"
'                    End Select
'                    Return "DL mode set to " + words(1)

'                Case "auto"
'                    If user IsNot PEEK_commander Then Return "Insufficient permissions: use --command first"
'                    If Not user.access.auto Then Return "Insufficient permissions: auto (T)"
'                    If words.Length <> 1 Then Return "Incorrect format. Expected no arguments"
'                    My.Settings.defaultLobbyAutoInstance = True
'                    My.Settings.defaultLobbyAutoStart = True
'                    Return "Game will be automated"

'                Case "manual"
'                    If user IsNot PEEK_commander Then Return "Insufficient permissions: use --command first"
'                    If words.Length <> 1 Then Return "Incorrect format. Expected no arguments"
'                    My.Settings.defaultLobbyAutoInstance = False
'                    My.Settings.defaultLobbyAutoStart = False
'                    Return "Game will be manual"

'                Case "host"
'                    If user IsNot PEEK_commander Then Return "Insufficient permissions: use --command first"
'                    If words.Length < 2 Then Throw New Exception("Incorrect format. Expected pattern @ name arguments")
'                    Dim argText As String = text.Substring(words(0).Length + 1)
'                    If argText Like "*@*@*" Or Not Trim(argText) Like "?*@*?" Then Throw New Exception("Incorrect format. Expected pattern @ name arguments")
'                    If Trim(argText) Like "*@" Or Trim(argText) Like "@*" Then Throw New Exception("Incorrect format. Expected pattern @ name arguments")
'                    If parent.server IsNot Nothing Then Return "Already trying to host."

'                    Dim ss() As String = Split(argText, "@")
'                    ss(0) = LOCAL_findMatchingMapName(RTrim(ss(0)))
'                    If Not ss(0).ToLower() Like "*.w3[mx]" Then Return ss(0) 'error returned by findMatchingMapName
'                    ss(1) = LTrim(ss(1))


'                    Try
'                        My.Settings.defaultLobbyName = ss(1)
'                        My.Settings.defaultLobbyMap = ss(0)
'                        Dim map As New W3Map(My.Settings.war3path + My.Settings.relMapPath + My.Settings.defaultLobbyMap)
'                        Dim settings As New W3GameSettings( _
'                                    My.Settings.defaultLobbyName, _
'                                    map, _
'                                    My.Settings.defaultSilent, _
'                                    My.Settings.defaultLobbyPrivate, _
'                                    My.Settings.defaultLobbyAllowDL, _
'                                    My.Settings.lobbyAllowBotUpload, _
'                                    My.Settings.defaultLobbyAutoInstance, _
'                                    My.Settings.defaultLobbyAutoStart, _
'                                    CType(My.Settings.default_slots_locked, W3Slot.LockStates))
'                        parent.REF_start_server(settings)
'                    Catch e As Exception
'                        Return "Error creating game: " + e.Message
'                    End Try
'                    Return "Attempting to create game..."

'                Case "submit"
'                    If Not user.access.auto Then Return "Insufficient permissions: submit (S)"
'                    If user IsNot PEEK_commander Then Return "Insufficient permissions: use --command first"
'                    If words.Length <> 2 Then Throw New Exception("Incorrect format. Expected epicwar map number argument")
'                    If Not IsNumeric(words(1)) Or words(1).Length > 9 Then Throw New Exception("Invalid epicwar map number")
'                    threadedCall(curry(AddressOf THREAD_downloadMap, PEEK_commander.name, CInt(words(1))), "Command --Submit : " + PEEK_commander.name)
'                    Return "You will be notified when the download is complete."

'                Case "silent"
'                    If user IsNot PEEK_commander Then Return "Insufficient permissions: use --command first"
'                    If words.Length <> 2 Then Return "Incorrect format. Expected on|off argument"
'                    Select Case words(1).ToLower()
'                        Case "on"
'                            My.Settings.defaultSilent = True
'                        Case "off"
'                            My.Settings.defaultSilent = False
'                        Case Else
'                            Return "Incorrect format. Expected on|off argument"
'                    End Select
'                    Return "Silent mode " + words(1)
'                Case "cancel"
'                    parent.client.REF_killGame()
'            End Select
'        Catch e As Exception
'            Return e.Message + " Try --help " + words(0)
'        End Try

'        Return Nothing
'    End Function

'    Private Function LOCAL_findMatchingMapName(ByVal query As String) As String
'        query = query.ToLower()
'        For Each s As String In IO.Directory.GetFiles(My.Settings.war3path + My.Settings.relMapPath)
'            s = getFileName(s)
'            If s.ToLower() Like "*.w3[mx]" Then
'                Try
'                    If s.ToLower() Like "*" + query + "*" Then Return s
'                Catch e As ArgumentException
'                    Return "Invalid search query"
'                End Try
'            End If
'        Next s
'        Return "No matching map filename found."
'    End Function

'    Private Sub THREAD_downloadMap(ByVal owner As String, ByVal epicWarNumber As Integer)
'        Dim path As String = Nothing
'        Try
'            Dim http As New Net.WebClient()
'            Dim httpFile As String = http.DownloadString("http://epicwar.com/maps/" + epicWarNumber.ToString() + "/")

'            'Find download link
'            Dim i As Integer = httpFile.IndexOf("alt=""Download""")
'            i = httpFile.IndexOf("a href=""", i)
'            i += "a href=""".Length
'            Dim j As Integer = httpFile.IndexOf(">", i)
'            Dim link As String = "http://epicwar.com" + httpFile.Substring(i, j - i)

'            'Find filename
'            i = httpFile.IndexOf("Download ", i) + "Download ".Length
'            j = httpFile.IndexOf("<", i)
'            path = My.Settings.war3path + My.Settings.relMapPath + httpFile.Substring(i, j - i)

'            'Check for existing files
'            If IO.File.Exists(path + ".dl") Then
'                parent.client.sendText("/m " + owner + " The map you submitted was already being downloaded.")
'                Return
'            End If
'            If IO.File.Exists(path) Then
'                parent.client.sendText("/m " + owner + " The map you submitted already existed.")
'                Return
'            End If

'            'Download
'            http.DownloadFile(link, path + ".dl")
'            IO.File.Move(path + ".dl", path)

'            parent.client.sendText("/m " + owner + " Your map submission has been completed.")
'        Catch e As Exception
'            parent.client.sendText("/m " + owner + " There was an error with your map submission: " + e.Message())
'            If path IsNot Nothing Then
'                IO.File.Delete(path + ".dl")
'                IO.File.Delete(path + ".txt")
'                IO.File.Delete(path)
'            End If
'        End Try
'    End Sub

'    Private Sub parent_client_state_changed(ByVal sender As BnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States) Handles parent.client_state_changed
'        If old_state = BnetClient.States.creating_game Then
'            Select Case new_state
'                Case BnetClient.States.channel
'                    parent.client.sendText("/m " + PEEK_commander.name + " Failed to create game.")
'                Case BnetClient.States.game
'                    If PEEK_commander IsNot Nothing Then
'                        With parent.server.PERM_settings
'                            PEEK_commander.timeout = DateTime.Now() + New TimeSpan(0, 10, 0)
'                            PEEK_commander.state = BotUserStates.hosting
'                            parent.client.sendText(String.Format("/m {0}  Created {1} @ {2}", PEEK_commander.name, .map.name, .name))
'                        End With
'                    End If
'            End Select
'        End If
'    End Sub
'End Class
