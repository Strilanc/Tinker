''HostBot - Warcraft 3 game hosting bot
''Copyright (C) 2008 Craig Gidney
''
''This program is free software: you can redistribute it and/or modify
''it under the terms of the GNU General Public License as published by
''the Free Software Foundation, either version 3 of the License, or
''(at your option) any later version.
''
''This program is distributed in the hope that it will be useful,
''but WITHOUT ANY WARRANTY; without even the implied warranty of
''MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
''GNU General Public License for more details.
''You should have received a copy of the GNU General Public License
''along with this program.  If not, see http://www.gnu.org/licenses/

Imports HostBot.BNET
Imports HostBot.Warcraft3
Imports HostBot.Links

'''<summary>The heart and soul of the bot. Handles all of the other pieces.</summary>
Public NotInheritable Class MainBot
#Region "Variables"
    Public ReadOnly client_profiles As New List(Of ClientProfile)
    Public ReadOnly plugin_profiles As New List(Of Plugins.PluginProfile)
    Private WithEvents plugin_manager As Plugins.PluginManager
    Public ReadOnly bot_commands As New Commands.Specializations.BotCommands
    Public ReadOnly client_commands As New Commands.Specializations.ClientCommands
    Public ReadOnly server_commands As New Commands.Specializations.ServerCommands()
    Public ReadOnly lan_commands As New Commands.Specializations.LanCommands()
    Public ReadOnly instance_commands_load As New Commands.UICommandSet(Of IW3GameLoadScreen)
    Public ReadOnly instance_commands_guest_setup As New Commands.Specializations.InstanceGuestSetupCommands
    Public ReadOnly instance_commands_guest_load As New Commands.Specializations.InstanceGuestLoadCommands
    Public ReadOnly instance_commands_guest_play As New Commands.Specializations.InstanceGuestPlayCommands
    Public ReadOnly instance_commands_play As New Commands.Specializations.InstancePlayCommands
    Public ReadOnly instance_commands_setup As New Commands.Specializations.InstanceSetupCommands
    Public ReadOnly instance_commands_admin As New Commands.Specializations.InstanceAdminCommands
    Public ReadOnly logger As MultiLogger

    Private ReadOnly ref As ICallQueue
    Private ReadOnly eventRef As ICallQueue
    Private intentionalDisconnectFlag As Boolean = False

    Private ReadOnly clients As New List(Of BnetClient)
    Private ReadOnly servers As New List(Of IW3Server)
    Private ReadOnly widgets As New List(Of IBotWidget)

    Public Event added_widget(ByVal widget As IBotWidget)
    Public Event removed_widget(ByVal widget As IBotWidget)
    Public Event server_state_changed(ByVal server As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
    Public Event added_server(ByVal server As IW3Server)
    Public Event removed_server(ByVal server As IW3Server)
    Public Event client_state_changed(ByVal client As BnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States)
    Public Event added_client(ByVal client As BnetClient)
    Public Event removed_client(ByVal client As BnetClient)
#End Region

#Region "New"
    Public Sub New(Optional ByVal logger As MultiLogger = Nothing)
        Me.logger = If(logger, New MultiLogger)
        Me.eventRef = New ThreadedCallQueue("{0} eref".frmt(Me.GetType.Name))
        Me.ref = New ThreadedCallQueue("{0} ref".frmt(Me.GetType.Name))
        If My.Settings.botstore <> "" Then
            Try
                Using m As New IO.MemoryStream(packString(My.Settings.botstore))
                    Using r As New IO.BinaryReader(m)
                        load(r)
                    End Using
                End Using
            Catch e As Exception
                client_profiles.Clear()
                client_profiles.Add(New ClientProfile())
                Logging.logUnexpectedException("Error loading profiles.", e)
            End Try
        Else
            client_profiles.Clear()
            Dim p = New ClientProfile()
            Dim u = New BotUser(BotUserSet.NAME_NEW_USER, "games=1")
            client_profiles.Add(p)
            p.users.add_user(u)
            plugin_profiles.Clear()
        End If

        plugin_manager = New Plugins.PluginManager(Me)
    End Sub
#End Region

#Region "State"
    Private Const format_version As UInteger = 0
    Public Sub save(ByVal w As IO.BinaryWriter)
        w.Write(UShort.MaxValue) 'indicate we are not the old version without the format flag
        w.Write(format_version)
        w.Write(CUInt(client_profiles.Count))
        For Each profile In client_profiles
            profile.save(w)
        Next profile
        w.Write(CUInt(plugin_profiles.Count))
        For Each profile In plugin_profiles
            profile.save(w)
        Next profile
    End Sub
    Public Sub load(ByVal r As IO.BinaryReader)
        Dim first_version = True
        Dim n As UInteger = r.ReadUInt16()
        If n = UInt16.MaxValue Then 'not the old version without the format flag
            Dim ver = r.ReadUInt32()
            If ver > format_version Then Throw New IO.IOException("Unrecognized bot data format version.")
            n = r.ReadUInt32()
            first_version = False
        End If

        client_profiles.Clear()
        For repeat = CUInt(1) To n
            client_profiles.Add(New ClientProfile(r))
        Next repeat
        If first_version Then
            plugin_profiles.Clear()
            Return
        End If

        plugin_profiles.Clear()
        For repeat = CUInt(1) To r.ReadUInt32()
            plugin_profiles.Add(New Plugins.PluginProfile(r))
        Next repeat
    End Sub

    Private Function create_server_L(ByVal name As String, _
                                     ByVal default_settings As ServerSettings, _
                                     Optional ByVal suffix As String = "", _
                                     Optional ByVal avoid_name_collision As Boolean = False) _
                                     As Outcome(Of IW3Server)
        Try
            If name.Trim = "" Then
                Return failure("Invalid server name.")
            ElseIf has_server_L(name) Then
                If Not avoid_name_collision Then
                    Return failureVal(find_server_L(name), "Server with name '{0}' already exists.".frmt(name))
                End If
                Dim i = 2
                While has_server_L(name + i.ToString())
                    i += 1
                End While
                name += i.ToString()
            End If

            Dim server As IW3Server = New W3Server(name, Me, default_settings, suffix)
            AddHandler server.PlayerTalked, AddressOf catch_server_player_talked_R
            AddHandler server.ChangedState, AddressOf catch_server_state_changed_R
            servers.Add(server)
            e_ThrowAddedServer(server)

            Return successVal(server, "Created server with name '{0}'. Admin password is {1}.".frmt(name, server.settings.admin_password))
        Catch e As Exception
            Return failure("Failed to create server: " + e.Message)
        End Try
    End Function
    Private Function remove_server_L(ByVal name As String) As Outcome
        Dim server = find_server_L(name)
        If server Is Nothing Then
            Return failure("No server with name {0}.".frmt(name))
        End If

        RemoveHandler server.PlayerTalked, AddressOf catch_server_player_talked_R
        RemoveHandler server.ChangedState, AddressOf catch_server_state_changed_R
        servers.Remove(server)
        server.f_Kill()
        e_ThrowRemovedServer(server)
        Return success("Removed server with name {0}.".frmt(name))
    End Function

    Private Function add_widget_L(ByVal widget As IBotWidget) As Outcome
        If find_widget_L(widget.type_name, widget.name) IsNot Nothing Then
            Return success("{0} with name {1} already exists.".frmt(widget.type_name, widget.name))
        End If
        widgets.Add(widget)
        e_ThrowAddedWidget(widget)
        Return success("Added {0} with name {1}.".frmt(widget.type_name, widget.name))
    End Function
    Private Function remove_widget_L(ByVal type_name As String, ByVal name As String) As Outcome
        Dim widget = find_widget_L(name, type_name)
        If widget Is Nothing Then Return failure("No {0} with name {1}.".frmt(type_name, name))
        widgets.Remove(widget)
        widget.stop()
        e_ThrowRemovedWidget(widget)
        Return success("Removed {0} with name {1}.".frmt(type_name, name))
    End Function

    Private Function create_client_L(ByVal name As String, Optional ByVal profile_name As String = "Default") As Outcome(Of BnetClient)
        If name.Trim = "" Then
            Return failure("Invalid client name.")
        ElseIf has_client_L(name) Then
            Return failureVal(find_client_L(name), "Client with name '{0}' already exists.".frmt(name))
        ElseIf find_client_profile_L(profile_name) Is Nothing Then
            Return failure("Invalid profile.")
        End If

        Dim client = New BnetClient(Me, find_client_profile_L(profile_name), name)
        AddHandler client.chat_event, AddressOf catch_client_chat_event_R
        AddHandler client.state_changed, AddressOf catch_client_state_changed_R
        clients.Add(client)

        e_ThrowAddedClient(client)
        Return successVal(client, "Created client with name '{0}'.".frmt(name))
    End Function
    Private Function remove_client_L(ByVal name As String) As Outcome
        Dim client = find_client_L(name)
        If client Is Nothing Then
            Return failure("No client with name {0}.".frmt(name))
        End If

        RemoveHandler client.chat_event, AddressOf catch_client_chat_event_R
        RemoveHandler client.state_changed, AddressOf catch_client_state_changed_R
        client.disconnect_R()
        clients.Remove(client)
        e_ThrowRemovedClient(client)
        Return success("Removed client with name {0}.".frmt(name))
    End Function

    Private Function has_client_L(ByVal name As String) As Boolean
        Return find_client_L(name) IsNot Nothing
    End Function
    Private Function has_server_L(ByVal name As String) As Boolean
        Return find_server_L(name) IsNot Nothing
    End Function

    Private Function find_client_L(ByVal name As String) As BnetClient
        Return (From x In clients Where x.name.ToLower = name.ToLower).FirstOrDefault()
    End Function
    Private Function find_server_L(ByVal name As String) As IW3Server
        Return (From x In servers Where x.name.ToLower = name.ToLower).FirstOrDefault()
    End Function
    Private Function find_widget_L(ByVal name As String, ByVal type_name As String) As IBotWidget
        Return (From x In widgets Where x.name.ToLower = name.ToLower AndAlso x.type_name.ToLower = type_name.ToLower).FirstOrDefault()
    End Function
    Public Function find_client_profile_L(ByVal name As String) As ClientProfile
        Return (From x In client_profiles Where x.name.ToLower = name.ToLower).FirstOrDefault
    End Function
    Public Function find_plugin_profile_L(ByVal name As String) As Plugins.PluginProfile
        Return (From x In plugin_profiles Where x.name.ToLower = name.ToLower).FirstOrDefault
    End Function

    Private Function kill_L() As Outcome
        'Kill clients
        For Each client In clients.ToList
            remove_client_L(client.name)
        Next client

        'Kill servers
        For Each server In servers.ToList
            remove_server_L(server.name)
        Next server

        'Kill widgets
        For Each widget In widgets.ToList
            remove_widget_L(widget.type_name, widget.name)
        Next widget

        Return success("Killed bot")
    End Function

    Private ReadOnly loaded_plugin_names As New List(Of String)
    Private Function pluginLoaded_L(ByVal name As String) As Boolean
        Return loaded_plugin_names.Contains(name)
    End Function
    Private Function loadPlugin_L(ByVal name As String) As Outcome
        If loaded_plugin_names.Contains(name) Then Return success("Plugin '{0}' is already loaded.".frmt(name))

        Dim profile = find_plugin_profile_L(name)
        If profile Is Nothing Then Return failure("No plugin matches the name '{0}'.".frmt(name))

        Dim loaded = plugin_manager.load_plugin(profile.name, profile.location)
        If loaded.outcome = Outcomes.succeeded Then loaded_plugin_names.Add(name)
        Return loaded
    End Function
    Private Sub plugin_manager_Unloaded_Plugin(ByVal name As String, ByVal plugin As Plugins.IPlugin, ByVal reason As String) Handles plugin_manager.Unloaded_Plugin
        logger.log("Plugin '{0}' was unloaded ({1})".frmt(name, reason), LogMessageTypes.NegativeEvent)
    End Sub
#End Region

#Region "Access"
    Public Shared Function Wc3MajorVersion() As Byte
        Return Wc3Version(2)
    End Function
    Public Shared Function Wc3Version() As Byte()
        Dim exeV(0 To 3) As Byte
        Dim ss() As String = My.Settings.exeVersion.Split("."c)
        If ss.Length <> 4 Then Throw New ArgumentException("Invalid version specified in settings. Must have #.#.#.# form.")
        For i As Integer = 0 To 3
            If Not Integer.TryParse(ss(i), 0) Or ss(i).Length > 8 Then
                Throw New ArgumentException("Invalid version specified in settings. Must have #.#.#.# form.")
            End If
            exeV(3 - i) = CByte(CInt(ss(i)) And &HFF)
        Next i
        Return exeV
    End Function

    Private Function create_lan_admin_L(ByVal name As String, _
                                          ByVal password As String, _
                                          Optional ByVal remote_host As String = "localhost", _
                                          Optional ByVal listen_port As UShort = 0) As IFuture(Of Outcome)
        Dim map = New W3Map("Maps\", _
                            "AdminGame.w3x", _
                            1, _
                            (From b In Enumerable.Range(0, 4) Select CByte(b)).ToArray(), _
                            (From b In Enumerable.Range(0, 20) Select CByte(b)).ToArray(), _
                            (From b In Enumerable.Range(0, 4) Select CByte(b)).ToArray(), _
                            2)
        map.slots(1).contents = New W3SlotContentsComputer(map.slots(1), W3Slot.ComputerLevel.Normal)
        Dim settings = New ServerSettings(map:=map, _
                                          username:=Nothing, _
                                          allowUpload:=False, _
                                          defaultSlotLockState:=W3Slot.Lock.frozen, _
                                          instances:=0, _
                                          password:=password, _
                                          arguments:=New String() {"-permanent"}, _
                                          is_admin_game:=True)
        Dim server_out = create_server_L(name, settings)
        If server_out.outcome <> Outcomes.succeeded Then
            Return futurize(failure("Failed to create server."))
        End If
        Dim server = server_out.val

        Dim lan As W3LanAdvertiser
        Try
            lan = New W3LanAdvertiser(Me, name, listen_port, remote_host)
        Catch e As Exception
            Return futurize(failure("Error creating lan advertiser: {0}".frmt(e.Message)))
        End Try
        Dim added = add_widget_L(lan)
        If added.outcome = Outcomes.failed Then
            Return futurize(failure("Failed to create LAN advertizer."))
        End If
        lan.add_game("Admin Game", map, server.settings.map_settings)

        Dim listen_out = server.f_OpenPort(listen_port)
        Dim f = New Future(Of Outcome)
        FutureSub.frun(AddressOf create_lan_admin_2_T, _
                       futurize(f), _
                       futurize(server), _
                       futurize(lan), _
                       listen_out)
        Return f
    End Function
    Private Sub create_lan_admin_2_T(ByVal f As Future(Of Outcome), _
                                     ByVal server As IW3Server, _
                                     ByVal lan As W3LanAdvertiser, _
                                     ByVal listened As outcome)
        If listened.outcome <> Outcomes.succeeded Then
            server.f_Kill()
            lan.Kill()
            f.setValue(failure("Failed to listen on tcp port."))
            Return
        End If

        DependencyLink.link(lan, server)
        DependencyLink.link(server, lan)
        f.setValue(success("Created LAN Admin Game"))
    End Sub
#End Region

#Region "Events"
    Private Sub e_ThrowAddedWidget(ByVal widget As IBotWidget)
        eventRef.enqueue(Function() eval(AddressOf _e_ThrowAddedWidget, widget))
    End Sub
    Private Sub _e_ThrowAddedWidget(ByVal widget As IBotWidget)
        RaiseEvent added_widget(widget)
    End Sub

    Private Sub e_ThrowRemovedWidget(ByVal widget As IBotWidget)
        eventRef.enqueue(Function() eval(AddressOf _e_ThrowRemovedWidget, widget))
    End Sub
    Private Sub _e_ThrowRemovedWidget(ByVal widget As IBotWidget)
        RaiseEvent removed_widget(widget)
    End Sub

    Private Sub e_ThrowAddedServer(ByVal server As IW3Server)
        eventRef.enqueue(Function() eval(AddressOf _e_ThrowAddedServer, server))
    End Sub
    Private Sub _e_ThrowAddedServer(ByVal server As IW3Server)
        RaiseEvent added_server(server)
    End Sub

    Private Sub e_ThrowRemovedServer(ByVal server As IW3Server)
        eventRef.enqueue(Function() eval(AddressOf _e_ThrowRemovedServer, server))
    End Sub
    Private Sub _e_ThrowRemovedServer(ByVal server As IW3Server)
        RaiseEvent removed_server(server)
    End Sub

    Private Sub e_ThrowAddedClient(ByVal client As BnetClient)
        eventRef.enqueue(Function() eval(AddressOf _e_ThrowAddedClient, client))
    End Sub
    Private Sub _e_ThrowAddedClient(ByVal client As BnetClient)
        RaiseEvent added_client(client)
    End Sub

    Private Sub e_ThrowRemovedClient(ByVal client As BnetClient)
        eventRef.enqueue(Function() eval(AddressOf _e_ThrowRemovedClient, client))
    End Sub
    Private Sub _e_ThrowRemovedClient(ByVal client As BnetClient)
        RaiseEvent removed_client(client)
    End Sub

    Private Sub e_ThrowClientStateChanged(ByVal sender As BnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States)
        eventRef.enqueue(Function() eval(AddressOf _e_ThrowClientStateChanged, sender, old_state, new_state))
    End Sub
    Private Sub _e_ThrowClientStateChanged(ByVal sender As BnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States)
        RaiseEvent client_state_changed(sender, old_state, new_state)
    End Sub

    Private Sub e_ThrowServerStateChanged(ByVal sender As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
        eventRef.enqueue(Function() eval(AddressOf _e_ThrowServerStateChanged, sender, old_state, new_state))
    End Sub
    Private Sub _e_ThrowServerStateChanged(ByVal sender As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
        RaiseEvent server_state_changed(sender, old_state, new_state)
    End Sub

    Private Sub catch_client_chat_event_R(ByVal client As BnetClient, ByVal id As BNET.BnetPacket.CHAT_EVENT_ID, ByVal username As String, ByVal text As String)
        'Exit if this is not a command
        If id <> BNET.BnetPacket.CHAT_EVENT_ID.TALK And id <> BNET.BnetPacket.CHAT_EVENT_ID.WHISPER Then
            Return
        ElseIf text.Substring(0, My.Settings.commandPrefix.Length) <> My.Settings.commandPrefix Then
            If text.ToLower() <> "?trigger" Then
                Return
            End If
        End If

        'Get user
        Dim user As BotUser = client.profile.users(username)
        If user Is Nothing Then Return

        'Process ?Trigger command
        If text.ToLower() = "?trigger" Then
            client.sendWhisper_R(username, "Command prefix is '{0}'".frmt(My.Settings.commandPrefix))
            Return
        End If

        'Process prefixed commands
        Dim command_text = text.Substring(My.Settings.commandPrefix.Length)
        Dim f = client_commands.processText(client, user, command_text)
        FutureSub.frun(AddressOf command_response_T, futurize(client), futurize(user), f)
        If Not f.isReady Then
            threadedCall(Function() eval(AddressOf command_response_wait_T, client, command_text, user, f), "commandWait " + command_text)
        End If
    End Sub
    Private Sub command_response_T(ByVal client As BnetClient, ByVal user As BotUser, ByVal output As Outcome)
        client.sendWhisper_R(user.name, If(output.outcome = Outcomes.succeeded, "", "(" + output.outcome.ToString() + ") ") + output.message)
    End Sub
    Private Sub command_response_wait_T(ByVal client As BnetClient, ByVal text As String, ByVal user As BotUser, ByVal response As IFuture(Of Outcome))
        Threading.Thread.Sleep(2000)
        If response.isReady Then Return
        client.sendWhisper_R(user.name, "Command '{0}' is running... You will be informed when it finishes.".frmt(text))
    End Sub

    Private Sub catch_server_player_talked_R(ByVal sender As IW3Server, _
                                             ByVal game As IW3Game, _
                                             ByVal player As IW3Player, _
                                             ByVal text As String)
        If text.Substring(0, My.Settings.commandPrefix.Length) <> My.Settings.commandPrefix Then
            If text.ToLower() <> "?trigger" Then
                Return
            End If
        End If

        'Process ?trigger command
        If text.ToLower() = "?trigger" Then
            game.f_SendMessageTo("Command prefix is '{0}'".frmt(My.Settings.commandPrefix), player)
            Return
        End If

        'Process prefixed commands
        Dim command_text = text.Substring(My.Settings.commandPrefix.Length)
        FutureSub.frun(Function(out) finish_instance_command(out, player, game), game.f_CommandProcessText(player, command_text))
    End Sub
    Private Function finish_instance_command(ByVal out As outcome, ByVal player As IW3Player, ByVal game As IW3Game) As Boolean
        If player Is Nothing Then Return False
        Dim msg = out.outcome.ToString() + ": " + out.message
        game.f_SendMessageTo(msg, player)
        game.logger.log("(Private to " + player.name + "): " + msg, LogMessageTypes.NormalEvent)
        Return True
    End Function

    Private Sub catch_client_state_changed_R(ByVal sender As BnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States)
        e_ThrowClientStateChanged(sender, old_state, new_state)
    End Sub
    Private Sub catch_server_state_changed_R(ByVal sender As IW3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
        e_ThrowServerStateChanged(sender, old_state, new_state)
    End Sub
#End Region

#Region "Remote Calls"
    Public Function find_server_R(ByVal name As String) As IFuture(Of IW3Server)
        Return ref.enqueue(Function() find_server_L(name))
    End Function
    Public Function kill_R() As IFuture(Of Outcome)
        Return ref.enqueue(AddressOf kill_L)
    End Function
    Public Function create_lan_admin_R(ByVal name As String, _
                                       ByVal password As String, _
                                       Optional ByVal remote_host As String = "localhost", _
                                       Optional ByVal listen_port As UShort = 0) As IFuture(Of Outcome)
        Return futurefuture(ref.enqueue(Function() create_lan_admin_L(name, password, remote_host, listen_port)))
    End Function
    Public Function add_widget_R(ByVal widget As IBotWidget) As IFuture(Of Outcome)
        Return ref.enqueue(Function() add_widget_L(widget))
    End Function
    Public Function remove_widget_R(ByVal type_name As String, ByVal name As String) As IFuture(Of Outcome)
        Return ref.enqueue(Function() remove_widget_L(type_name, name))
    End Function
    Public Function has_server_R(ByVal name As String) As IFuture(Of Boolean)
        Return ref.enqueue(Function() has_server_L(name))
    End Function
    Public Function remove_server_R(ByVal name As String) As IFuture(Of Outcome)
        Return ref.enqueue(Function() remove_server_L(name))
    End Function
    Public Function create_server_R(ByVal name As String, _
                                    ByVal default_settings As ServerSettings, _
                                    Optional ByVal suffix As String = "", _
                                    Optional ByVal avoid_name_collision As Boolean = False) _
                                    As IFuture(Of Outcome(Of IW3Server))
        Return ref.enqueue(Function() create_server_L(name, default_settings, suffix, avoid_name_collision))
    End Function
    Public Function find_client_R(ByVal name As String) As IFuture(Of BnetClient)
        Return ref.enqueue(Function() find_client_L(name))
    End Function
    Public Function has_client_R(ByVal name As String) As IFuture(Of Boolean)
        Return ref.enqueue(Function() has_client_L(name))
    End Function
    Public Function remove_client_R(ByVal name As String) As IFuture(Of Outcome)
        Return ref.enqueue(Function() remove_client_L(name))
    End Function
    Public Function create_client_R(ByVal name As String, Optional ByVal profile_name As String = "Default") As IFuture(Of Outcome(Of BnetClient))
        Return ref.enqueue(Function() create_client_L(name, profile_name))
    End Function
    Public Function shallow_copy_servers_R() As IFuture(Of List(Of IW3Server))
        Return ref.enqueue(Function() servers.ToList)
    End Function
    Public Function shallow_copy_clients_R() As IFuture(Of List(Of BnetClient))
        Return ref.enqueue(Function() clients.ToList)
    End Function
    Public Function shallow_copy_widgets_R() As IFuture(Of List(Of IBotWidget))
        Return ref.enqueue(Function() widgets.ToList)
    End Function
    Public Function loadPlugin_R(ByVal name As String) As IFuture(Of Outcome)
        Return ref.enqueue(Function() loadPlugin_L(name))
    End Function
#End Region

    Public ReadOnly port_pool As New PortPool
End Class

Public Class PortPool
    Private ReadOnly InPorts As New HashSet(Of UShort)
    Private ReadOnly OutPorts As New HashSet(Of UShort)
    Private ReadOnly PortPool As New HashSet(Of UShort)
    Private ReadOnly lock As New Object()

    Public Function EnumPorts() As IEnumerable(Of UShort)
        SyncLock lock
            Return PortPool.ToList()
        End SyncLock
    End Function
    Public Function EnumUsedPorts() As IEnumerable(Of UShort)
        SyncLock lock
            Return OutPorts.ToList()
        End SyncLock
    End Function
    Public Function EnumAvailablePorts() As IEnumerable(Of UShort)
        SyncLock lock
            Return InPorts.ToList()
        End SyncLock
    End Function

    Public Function TryAddPort(ByVal port As UShort) As Outcome
        SyncLock lock
            If PortPool.Contains(port) Then Return failure("Port {0} is already in the pool.".frmt(port))
            PortPool.Add(port)
            If OutPorts.Contains(port) Then Return success("Port {0} re-added to the pool, but was still in use.".frmt(port))
            InPorts.Add(port)
            Return success("Port {0} added to the pool.".frmt(port))
        End SyncLock
    End Function
    Public Function TryRemovePort(ByVal port As UShort) As Outcome
        SyncLock lock
            If Not PortPool.Contains(port) Then Return failure("Port {0} is not in the pool.".frmt(port))
            PortPool.Remove(port)
            If OutPorts.Contains(port) Then Return success("Port {0} removed from the pool, but is still in use.".frmt(port))
            InPorts.Remove(port)
            Return success("Port {0} removed from the pool.".frmt(port))
        End SyncLock
    End Function

    Private Function TryReturnPortToPool(ByVal port As UShort) As Outcome
        SyncLock lock
            If InPorts.Contains(port) Then Return failure("Port {0} is already in the pool.".frmt(port))
            If Not OutPorts.Contains(port) Then Return failure("Port {0} wasn't taken from the pool.".frmt(port))
            If PortPool.Contains(port) Then
                OutPorts.Remove(port)
                Return success("Returned port {0}, but it is no longer in the pool.".frmt(port))
            Else
                InPorts.Add(port)
                OutPorts.Remove(port)
                Return success("Returned port {0} to the pool.".frmt(port))
            End If
        End SyncLock
    End Function
    Public Function TryTakePortFromPool() As Outcome(Of PortHandle)
        SyncLock lock
            If InPorts.Count = 0 Then Return failure("No ports are in the pool.")
            Dim port = New PortHandle(Me, InPorts.First)
            InPorts.Remove(port.port)
            Return successVal(port, "Took port {0} from the pool.".frmt(port.port))
        End SyncLock
    End Function

    Public Class PortHandle
        Implements IDisposable
        Private ReadOnly pool As PortPool
        Private ReadOnly _port As UShort
        Private disposed As Boolean
        Private ReadOnly lock As New Object()

        Public Sub New(ByVal pool As PortPool, ByVal port As UShort)
            Me.pool = pool
            Me._port = port
        End Sub

        Public ReadOnly Property port() As UShort
            Get
                SyncLock lock
                    If disposed Then Throw New InvalidOperationException("Can't access a disposed object.")
                    Return _port
                End SyncLock
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            SyncLock lock
                If disposed Then Return
                disposed = True
            End SyncLock

            pool.TryReturnPortToPool(_port)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overrides Sub Finalize()
            disposed = True
            pool.TryReturnPortToPool(_port)
        End Sub
    End Class
End Class