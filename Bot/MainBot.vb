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

Imports HostBot.Bnet
Imports HostBot.Warcraft3
Imports HostBot.Links
Imports HostBot.Commands.Specializations

'''<summary>The heart and soul of the bot. Handles all of the other pieces.</summary>
Public NotInheritable Class MainBot
#Region "Variables"
    Public ReadOnly portPool As New PortPool
    Public ReadOnly clientProfiles As New HashSet(Of ClientProfile)
    Public ReadOnly pluginProfiles As New HashSet(Of Plugins.PluginProfile)
    Private WithEvents pluginManager As Plugins.PluginManager
    Public ReadOnly BotCommands As New BotCommands
    Public ReadOnly ClientCommands As New ClientCommands
    Public ReadOnly ServerCommands As New ServerCommands()
    Public ReadOnly LanCommands As New LanCommands()
    Public ReadOnly GameCommandsLoadScreen As New Commands.UICommandSet(Of IW3Game)
    Public ReadOnly GameGuestCommandsLobby As New InstanceGuestSetupCommands
    Public ReadOnly GameGuestCommandsLoadScreen As New InstanceGuestLoadCommands
    Public ReadOnly GameGuestCommandsGamePlay As New InstanceGuestPlayCommands
    Public ReadOnly GameCommandsGamePlay As New InstancePlayCommands
    Public ReadOnly GameCommandsLobby As New InstanceSetupCommands
    Public ReadOnly GameCommandsAdmin As New InstanceAdminCommands
    Public ReadOnly logger As Logger

    Private ReadOnly ref As ICallQueue
    Private ReadOnly eref As ICallQueue
    Private ReadOnly wardenRef As ICallQueue
    Private intentionalDisconnectFlag As Boolean

    Private ReadOnly clients As New List(Of BnetClient)
    Private ReadOnly servers As New List(Of W3Server)
    Private ReadOnly widgets As New List(Of IBotWidget)

    Public Event AddedWidget(ByVal widget As IBotWidget)
    Public Event RemovedWidget(ByVal widget As IBotWidget)
    Public Event ServerStateChanged(ByVal server As W3Server, ByVal oldState As W3ServerStates, ByVal newState As W3ServerStates)
    Public Event AddedServer(ByVal server As W3Server)
    Public Event RemovedServer(ByVal server As W3Server)
    Public Event ClientStateChanged(ByVal client As BnetClient, ByVal oldState As BnetClient.States, ByVal newState As BnetClient.States)
    Public Event AddedClient(ByVal client As BnetClient)
    Public Event RemovedClient(ByVal client As BnetClient)
#End Region

#Region "New"
    Public Sub New(ByVal wardenRef As ICallQueue,
                   Optional ByVal logger As Logger = Nothing)
        'contract bug wrt interface event implementation requires this:
        'Contract.Requires(wardenRef IsNot Nothing)
        Contract.Assume(wardenRef IsNot Nothing)
        Me.wardenRef = wardenRef
        Me.logger = If(logger, New Logger)
        Me.eref = New ThreadPooledCallQueue
        Me.ref = New ThreadPooledCallQueue
        If My.Settings.botstore <> "" Then
            Try
                Using m As New IO.MemoryStream(My.Settings.botstore.ToAscBytes)
                    Using r As New IO.BinaryReader(m)
                        Load(r)
                    End Using
                End Using
            Catch e As Exception
                clientProfiles.Clear()
                clientProfiles.Add(New ClientProfile())
                LogUnexpectedException("Error loading profiles.", e)
            End Try
        Else
            clientProfiles.Clear()
            Dim p = New ClientProfile()
            Dim u = New BotUser(BotUserSet.NAME_NEW_USER, "games=1")
            clientProfiles.Add(p)
            p.users.AddUser(u)
            pluginProfiles.Clear()
        End If

        pluginManager = New Plugins.PluginManager(Me)
    End Sub
#End Region

    <ContractInvariantMethod()> Protected Sub Invariant()
        Contract.Invariant(ref IsNot Nothing)
        Contract.Invariant(eref IsNot Nothing)
        Contract.Invariant(wardenRef IsNot Nothing)
    End Sub

#Region "State"
    Private Const FormatVersion As UInteger = 0
    Public Sub Save(ByVal w As IO.BinaryWriter)
        w.Write(UShort.MaxValue) 'indicate we are not the old version without the format flag
        w.Write(FormatVersion)
        w.Write(CUInt(clientProfiles.Count))
        For Each profile In clientProfiles
            profile.save(w)
        Next profile
        w.Write(CUInt(pluginProfiles.Count))
        For Each profile In pluginProfiles
            profile.save(w)
        Next profile
    End Sub
    Public Sub Load(ByVal r As IO.BinaryReader)
        Dim first_version = True
        Dim n As UInteger = r.ReadUInt16()
        If n = UInt16.MaxValue Then 'not the old version without the format flag
            Dim ver = r.ReadUInt32()
            If ver > FormatVersion Then Throw New IO.IOException("Unrecognized bot data format version.")
            n = r.ReadUInt32()
            first_version = False
        End If

        clientProfiles.Clear()
        For repeat = 1UI To n
            clientProfiles.Add(New ClientProfile(r))
        Next repeat
        If first_version Then
            pluginProfiles.Clear()
            Return
        End If

        pluginProfiles.Clear()
        For repeat = 1UI To r.ReadUInt32()
            pluginProfiles.Add(New Plugins.PluginProfile(r))
        Next repeat
    End Sub

    Private Function CreateServer(ByVal name As String,
                                  ByVal serverSettings As ServerSettings,
                                  Optional ByVal suffix As String = "",
                                  Optional ByVal avoidNameCollision As Boolean = False) As Outcome(Of W3Server)
        Try
            If name.Trim = "" Then
                Return failure("Invalid server name.")
            ElseIf HaveServer(name) Then
                If Not avoidNameCollision Then
                    Return Failure(FindServer(name), "Server with name '{0}' already exists.".Frmt(name))
                End If
                Dim i = 2
                While HaveServer(name + i.ToString())
                    i += 1
                End While
                name += i.ToString()
            End If

            Dim server As W3Server = New W3Server(name, Me, serverSettings, suffix)
            AddHandler server.PlayerTalked, AddressOf CatchServerPlayerTalked
            AddHandler server.ChangedState, AddressOf CatchServerStateChanged
            servers.Add(server)
            ThrowAddedServer(server)

            Return Success(server, "Created server with name '{0}'. Admin password is {1}.".Frmt(name, server.settings.adminPassword))
        Catch e As Exception
            Return Failure("Failed to create server: " + e.ToString)
        End Try
    End Function
    Private Function KillServer(ByVal name As String) As Outcome
        Dim server = FindServer(name)
        If server Is Nothing Then
            Return Failure("No server with name {0}.".Frmt(name))
        End If

        RemoveHandler server.PlayerTalked, AddressOf CatchServerPlayerTalked
        RemoveHandler server.ChangedState, AddressOf CatchServerStateChanged
        servers.Remove(server)
        server.QueueKill()
        ThrowRemovedServer(server)
        Return Success("Removed server with name {0}.".Frmt(name))
    End Function

    Private Function AddWidget(ByVal widget As IBotWidget) As Outcome
        If FindWidget(widget.TypeName, widget.Name) IsNot Nothing Then
            Return Success("{0} with name {1} already exists.".Frmt(widget.TypeName, widget.Name))
        End If
        widgets.Add(widget)
        ThrowAddedWidget(widget)
        Return Success("Added {0} with name {1}.".Frmt(widget.TypeName, widget.Name))
    End Function
    Private Function RemoveWidget(ByVal typeName As String, ByVal name As String) As Outcome
        Dim widget = FindWidget(name, typeName)
        If widget Is Nothing Then Return Failure("No {0} with name {1}.".Frmt(typeName, name))
        widgets.Remove(widget)
        widget.[Stop]()
        ThrowRemovedWidget(widget)
        Return Success("Removed {0} with name {1}.".Frmt(typeName, name))
    End Function

    Private Function CreateClient(ByVal name As String, Optional ByVal profileName As String = "Default") As Outcome(Of BnetClient)
        If name.Trim = "" Then
            Return Failure("Invalid client name.")
        ElseIf HaveClient(name) Then
            Return Failure(FindClient(name), "Client with name '{0}' already exists.".Frmt(name))
        ElseIf FindClientProfile(profileName) Is Nothing Then
            Return Failure("Invalid profile.")
        End If

        Dim client = New BnetClient(Me, FindClientProfile(profileName), name, wardenRef)
        AddHandler client.ReceivedPacket, AddressOf CatchClientReceivedPacket
        AddHandler client.StateChanged, AddressOf CatchClientStateChanged
        clients.Add(client)

        ThrowAddedClient(client)
        Return Success(client, "Created client with name '{0}'.".Frmt(name))
    End Function
    Private Function KillClient(ByVal name As String) As Outcome
        Dim client = FindClient(name)
        If client Is Nothing Then
            Return failure("No client with name {0}.".frmt(name))
        End If

        RemoveHandler client.ReceivedPacket, AddressOf CatchClientReceivedPacket
        RemoveHandler client.StateChanged, AddressOf CatchClientStateChanged
        client.QueueDisconnect("Killed by MainBot")
        clients.Remove(client)
        ThrowRemovedClient(client)
        Return success("Removed client with name {0}.".frmt(name))
    End Function

    Private Function HaveClient(ByVal name As String) As Boolean
        Return FindClient(name) IsNot Nothing
    End Function
    Private Function HaveServer(ByVal name As String) As Boolean
        Return FindServer(name) IsNot Nothing
    End Function

    Private Function FindClient(ByVal name As String) As BnetClient
        Return (From x In clients Where x.Name.ToLower = name.ToLower).FirstOrDefault()
    End Function
    Private Function FindServer(ByVal name As String) As W3Server
        Return (From x In servers Where x.Name.ToLower = name.ToLower).FirstOrDefault()
    End Function
    Private Function FindWidget(ByVal name As String, ByVal typeName As String) As IBotWidget
        Return (From x In widgets Where x.Name.ToLower = name.ToLower AndAlso x.TypeName.ToLower = typeName.ToLower).FirstOrDefault()
    End Function
    Public Function FindClientProfile(ByVal name As String) As ClientProfile
        Return (From x In clientProfiles Where x.name.ToLower = name.ToLower).FirstOrDefault
    End Function
    Public Function FindPluginProfile(ByVal name As String) As Plugins.PluginProfile
        Return (From x In pluginProfiles Where x.name.ToLower = name.ToLower).FirstOrDefault
    End Function

    Private Function Kill() As Outcome
        'Kill clients
        For Each client In clients.ToList
            KillClient(client.Name)
        Next client

        'Kill servers
        For Each server In servers.ToList
            KillServer(server.Name)
        Next server

        'Kill widgets
        For Each widget In widgets.ToList
            RemoveWidget(widget.TypeName, widget.Name)
        Next widget

        Return success("Killed bot")
    End Function

    Private ReadOnly loadedPluginNames As New HashSet(Of String)
    Private Function LoadPlugin(ByVal name As String) As Outcome
        If loadedPluginNames.Contains(name) Then Return success("Plugin '{0}' is already loaded.".frmt(name))

        Dim profile = FindPluginProfile(name)
        If profile Is Nothing Then Return failure("No plugin matches the name '{0}'.".frmt(name))

        Dim loaded = pluginManager.LoadPlugin(profile.name, profile.location)
        If loaded.succeeded Then loadedPluginNames.Add(name)
        Return loaded
    End Function
    Private Sub CatchUnloadedPlugin(ByVal name As String, ByVal plugin As Plugins.IPlugin, ByVal reason As String) Handles pluginManager.UnloadedPlugin
        logger.log("Plugin '{0}' was unloaded ({1})".frmt(name, reason), LogMessageTypes.Negative)
    End Sub
#End Region

#Region "Access"
    Public Shared Function Wc3MajorVersion() As Byte
        Return Wc3Version(2)
    End Function
    Public Shared Function Wc3Version() As Byte()
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Dim exeV(0 To 3) As Byte
        Dim ss() = My.Settings.exeVersion.Split("."c)
        If ss.Length <> 4 Then Throw New ArgumentException("Invalid version specified in settings. Must have #.#.#.# form.")
        For i = 0 To 3
            If Not Integer.TryParse(ss(i), 0) Or ss(i).Length > 8 Then
                Throw New ArgumentException("Invalid version specified in settings. Must have #.#.#.# form.")
            End If
            exeV(3 - i) = CByte(CInt(ss(i)) And &HFF)
        Next i
        Return exeV
    End Function
    Public Shared Function Wc3Path() As String
        Return My.Settings.war3path
    End Function
    Public Shared Function MapPath() As String
        Return My.Settings.mapPath
    End Function

    Private Function CreateLanAdmin(ByVal name As String,
                                    ByVal password As String,
                                    Optional ByVal remoteHost As String = "localhost",
                                    Optional ByVal listenPort As UShort = 0) As IFuture(Of Outcome)
        Dim map = New W3Map("Maps\",
                            "AdminGame.w3x",
                            filesize:=1,
                            crc32:=(From b In Enumerable.Range(0, 4) Select CByte(b)).ToArray(),
                            sha1Checksum:=(From b In Enumerable.Range(0, 20) Select CByte(b)).ToArray(),
                            xoroChecksum:=(From b In Enumerable.Range(0, 4) Select CByte(b)).ToArray(),
                            numSlots:=2)
        map.slots(1).contents = New W3SlotContentsComputer(map.slots(1), W3Slot.ComputerLevel.Normal)
        Dim header = New W3GameHeader("Admin Game",
                                      My.Resources.ProgramName,
                                      New W3MapSettings(Nothing, map),
                                      0,
                                      0,
                                      0,
                                      options:=New String() {"-permanent"},
                                      numplayerslots:=map.NumPlayerSlots)
        Dim settings = New ServerSettings(map:=map,
                                          header:=header,
                                          allowUpload:=False,
                                          defaultSlotLockState:=W3Slot.Lock.frozen,
                                          instances:=0,
                                          password:=password,
                                          is_admin_game:=True)
        Dim server_out = CreateServer(name, settings)
        If Not server_out.succeeded Then
            Return failure("Failed to create server.").Futurize
        End If
        Dim server = server_out.Value

        Dim lan As W3LanAdvertiser
        Try
            lan = New W3LanAdvertiser(Me, name, listenPort, remoteHost)
        Catch e As Exception
            Return failure("Error creating lan advertiser: {0}".frmt(e.ToString)).Futurize
        End Try
        Dim added = AddWidget(lan)
        If Not added.succeeded Then
            Return failure("Failed to create lan advertiser.").Futurize
        End If
        lan.AddGame(header)

        Dim f = New Future(Of Outcome)
        server.QueueOpenPort(listenPort).CallWhenValueReady(
            Sub(listened)
                If Not listened.succeeded Then
                    server.QueueKill()
                    lan.Dispose()
                    f.SetValue(failure("Failed to listen on tcp port."))
                    Return
                End If

                DisposeLink.CreateOneWayLink(lan, server)
                DisposeLink.CreateOneWayLink(server, lan)
                f.SetValue(success("Created lan Admin Game"))
            End Sub
        )
        Return f
    End Function
#End Region

#Region "Events"
    Private Sub ThrowAddedWidget(ByVal widget As IBotWidget)
        eref.QueueAction(Sub()
                             RaiseEvent AddedWidget(widget)
                         End Sub)
    End Sub
    Private Sub ThrowRemovedWidget(ByVal widget As IBotWidget)
        eref.QueueAction(Sub()
                             RaiseEvent RemovedWidget(widget)
                         End Sub)
    End Sub
    Private Sub ThrowAddedServer(ByVal server As W3Server)
        eref.QueueAction(Sub()
                             RaiseEvent AddedServer(server)
                         End Sub)
    End Sub
    Private Sub ThrowRemovedServer(ByVal server As W3Server)
        eref.QueueAction(Sub()
                             RaiseEvent RemovedServer(server)
                         End Sub)
    End Sub
    Private Sub ThrowAddedClient(ByVal client As BnetClient)
        eref.QueueAction(Sub()
                             RaiseEvent AddedClient(client)
                         End Sub)
    End Sub
    Private Sub ThrowRemovedClient(ByVal client As BnetClient)
        eref.QueueAction(Sub()
                             RaiseEvent RemovedClient(client)
                         End Sub)
    End Sub

    Private Sub CatchClientReceivedPacket(ByVal client As BnetClient, ByVal packet As BnetPacket)
        If packet.id <> BnetPacketID.ChatEvent Then Return

        Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
        Dim id = CType(vals("event id"), BnetPacket.ChatEventId)
        Dim username = CStr(vals("username"))
        Dim text = CStr(vals("text"))

        'Exit if this is not a command
        If id <> Bnet.BnetPacket.ChatEventId.Talk And id <> Bnet.BnetPacket.ChatEventId.Whisper Then
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
            client.QueueSendWhisper(username, "Command prefix is '{0}'".frmt(My.Settings.commandPrefix))
            Return
        End If

        'Process prefixed commands
        Dim commandText = text.Substring(My.Settings.commandPrefix.Length)
        Dim commandOutcocme = ClientCommands.ProcessCommand(client, user, breakQuotedWords(commandText))
        commandOutcocme.CallWhenValueReady(
            Sub(output) client.QueueSendWhisper(user.name, If(output.succeeded, "", "(Failed) ") + output.Message)
        )
        If Not commandOutcocme.IsReady Then
            FutureWait(2.Seconds).CallWhenReady(
                Sub()
                    If Not commandOutcocme.IsReady Then
                        client.QueueSendWhisper(user.name, "Command '{0}' is running... You will be informed when it finishes.".frmt(text))
                    End If
                End Sub
            )
        End If
    End Sub

    Private Sub CatchServerPlayerTalked(ByVal sender As W3Server,
                                        ByVal game As IW3Game,
                                        ByVal player As W3Player,
                                        ByVal text As String)
        If text.Substring(0, My.Settings.commandPrefix.Length) <> My.Settings.commandPrefix Then
            If text.ToLower() <> "?trigger" Then
                Return
            End If
        End If

        'Process ?trigger command
        If text.ToLower() = "?trigger" Then
            game.QueueSendMessageTo("Command prefix is '{0}'".Frmt(My.Settings.commandPrefix), player)
            Return
        End If

        'Process prefixed commands
        Dim commandText = text.Substring(My.Settings.commandPrefix.Length)
        game.QueueProcessCommand(player, breakQuotedWords(commandText)).CallWhenValueReady(
            Sub(commandOutcome)
                Dim msg = If(commandOutcome.succeeded, "", "Failed: ") + commandOutcome.Message
                game.QueueSendMessageTo(msg, player)
            End Sub
        )
    End Sub

    Private Sub CatchClientStateChanged(ByVal sender As BnetClient, ByVal old_state As BnetClient.States, ByVal new_state As BnetClient.States)
        RaiseEvent ClientStateChanged(sender, old_state, new_state)
    End Sub
    Private Sub CatchServerStateChanged(ByVal sender As W3Server, ByVal old_state As W3ServerStates, ByVal new_state As W3ServerStates)
        RaiseEvent ServerStateChanged(sender, old_state, new_state)
    End Sub
#End Region

#Region "Remote Calls"
    Public Function QueueFindServer(ByVal name As String) As IFuture(Of W3Server)
        Return ref.QueueFunc(Function() FindServer(name))
    End Function
    Public Function QueueKill() As IFuture(Of Outcome)
        Return ref.QueueFunc(AddressOf Kill)
    End Function
    Public Function QueueCreateLanAdmin(ByVal name As String,
                                       ByVal password As String,
                                       Optional ByVal remote_host As String = "localhost",
                                       Optional ByVal listen_port As UShort = 0) As IFuture(Of Outcome)
        Return ref.QueueFunc(Function() CreateLanAdmin(name, password, remote_host, listen_port)).Defuturize
    End Function
    Public Function QueueAddWidget(ByVal widget As IBotWidget) As IFuture(Of Outcome)
        Return ref.QueueFunc(Function() AddWidget(widget))
    End Function
    Public Function QueueRemoveWidget(ByVal type_name As String, ByVal name As String) As IFuture(Of Outcome)
        Return ref.QueueFunc(Function() RemoveWidget(type_name, name))
    End Function
    Public Function QueueRemoveServer(ByVal name As String) As IFuture(Of Outcome)
        Return ref.QueueFunc(Function() KillServer(name))
    End Function
    Public Function QueueCreateServer(ByVal name As String,
                                      ByVal defaultSettings As ServerSettings,
                                      Optional ByVal suffix As String = "",
                                      Optional ByVal avoidNameCollisions As Boolean = False) As IFuture(Of Outcome(Of W3Server))
        Return ref.QueueFunc(Function() CreateServer(name, defaultSettings, suffix, avoidNameCollisions))
    End Function
    Public Function QueueFindClient(ByVal name As String) As IFuture(Of BnetClient)
        Return ref.QueueFunc(Function() FindClient(name))
    End Function
    Public Function QueueRemoveClient(ByVal name As String) As IFuture(Of Outcome)
        Return ref.QueueFunc(Function() KillClient(name))
    End Function
    Public Function QueueCreateClient(ByVal name As String, Optional ByVal profileName As String = "Default") As IFuture(Of Outcome(Of BnetClient))
        Return ref.QueueFunc(Function() CreateClient(name, profileName))
    End Function
    Public Function QueueGetServers() As IFuture(Of List(Of W3Server))
        Return ref.QueueFunc(Function() servers.ToList)
    End Function
    Public Function QueueGetClients() As IFuture(Of List(Of BnetClient))
        Return ref.QueueFunc(Function() clients.ToList)
    End Function
    Public Function QueueGetWidgets() As IFuture(Of List(Of IBotWidget))
        Return ref.QueueFunc(Function() widgets.ToList)
    End Function
    Public Function QueueLoadPlugin(ByVal name As String) As IFuture(Of Outcome)
        Return ref.QueueFunc(Function() LoadPlugin(name))
    End Function
#End Region
End Class

Public Class PortPool
    Private ReadOnly InPorts As New HashSet(Of UShort)
    Private ReadOnly OutPorts As New HashSet(Of UShort)
    Private ReadOnly PortPool As New HashSet(Of UShort)
    Private ReadOnly lock As New Object()

    <ContractInvariantMethod()> Protected Sub Invariant()
        Contract.Invariant(InPorts IsNot Nothing)
        Contract.Invariant(OutPorts IsNot Nothing)
        Contract.Invariant(PortPool IsNot Nothing)
    End Sub

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

    Public Function TryTakePortFromPool() As PortHandle
        SyncLock lock
            If InPorts.Count = 0 Then Return Nothing
            Dim port = New PortHandle(Me, InPorts.First)
            InPorts.Remove(port.Port)
            Return port
        End SyncLock
    End Function

    Public Class PortHandle
        Inherits NotifyingDisposable
        Private ReadOnly pool As PortPool
        Private ReadOnly _port As UShort

        Public Sub New(ByVal pool As PortPool, ByVal port As UShort)
            Me.pool = pool
            Me._port = port
        End Sub

        Public ReadOnly Property Port() As UShort
            Get
                If IsDisposed Then Throw New ObjectDisposedException(GetType(PortHandle).Name)
                Return _port
            End Get
        End Property

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            SyncLock pool.lock
                If pool.InPorts.Contains(_port) Then Throw New InvalidStateException("Unreturned Port was still in pool.")
                If Not pool.OutPorts.Contains(_port) Then Throw New InvalidStateException("Unreturned Port was not checked out from pool.")
                pool.OutPorts.Remove(_port)
                If pool.PortPool.Contains(_port) Then
                    pool.InPorts.Add(_port)
                End If
            End SyncLock
        End Sub
    End Class
End Class