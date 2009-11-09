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
    Private ReadOnly _portPool As New PortPool
    Public ReadOnly clientProfiles As New HashSet(Of ClientProfile)
    Public ReadOnly pluginProfiles As New HashSet(Of Plugins.PluginProfile)
    Private WithEvents pluginManager As Plugins.PluginManager
    Public ReadOnly BotCommands As New BotCommands
    Public ReadOnly ClientCommands As New ClientCommands
    Public ReadOnly ServerCommands As New ServerCommands()
    Public ReadOnly LanCommands As New LanCommands()
    Public ReadOnly logger As Logger

    Private ReadOnly ref As ICallQueue
    Private ReadOnly eref As ICallQueue

    Private ReadOnly clients As New List(Of BnetClient)
    Private ReadOnly servers As New List(Of W3Server)
    Private ReadOnly widgets As New List(Of IBotWidget)

    Public Event AddedWidget(ByVal widget As IBotWidget)
    Public Event RemovedWidget(ByVal widget As IBotWidget)
    Public Event ServerStateChanged(ByVal server As W3Server, ByVal oldState As W3ServerState, ByVal newState As W3ServerState)
    Public Event AddedServer(ByVal server As W3Server)
    Public Event RemovedServer(ByVal server As W3Server)
    Public Event ClientStateChanged(ByVal client As BnetClient, ByVal oldState As BnetClientState, ByVal newState As BnetClientState)
    Public Event AddedClient(ByVal client As BnetClient)
    Public Event RemovedClient(ByVal client As BnetClient)
#End Region

#Region "New"
    Public Sub New(Optional ByVal logger As Logger = Nothing)
        Me.logger = If(logger, New Logger)
        Me.eref = New ThreadPooledCallQueue
        Me.ref = New ThreadPooledCallQueue
        Dim serializedData = My.Settings.botstore
        If serializedData IsNot Nothing AndAlso serializedData <> "" Then
            Try
                Using m As New IO.MemoryStream(serializedData.ToAscBytes)
                    Using r As New IO.BinaryReader(m)
                        Load(r)
                    End Using
                End Using
            Catch e As Exception
                clientProfiles.Clear()
                clientProfiles.Add(New ClientProfile())
                e.RaiseAsUnexpected("Error loading profiles.")
            End Try
        Else
            clientProfiles.Clear()
            Dim p = New ClientProfile()
            Dim u = New BotUser(BotUserSet.NewUserKey, "games=1")
            clientProfiles.Add(p)
            p.users.AddUser(u)
            pluginProfiles.Clear()
        End If

        pluginManager = New Plugins.PluginManager(Me)
    End Sub
#End Region

    Public ReadOnly Property PortPool As PortPool
        Get
            Contract.Ensures(Contract.Result(Of PortPool)() IsNot Nothing)
            Return _portPool
        End Get
    End Property
    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(ref IsNot Nothing)
        Contract.Invariant(eref IsNot Nothing)
        Contract.Invariant(servers IsNot Nothing)
        Contract.Invariant(clients IsNot Nothing)
        Contract.Invariant(pluginProfiles IsNot Nothing)
        Contract.Invariant(clientProfiles IsNot Nothing)
        Contract.Invariant(widgets IsNot Nothing)
        Contract.Invariant(_portPool IsNot Nothing)
    End Sub

#Region "State"
    Private Const FormatVersion As UInteger = 0
    Public Sub Save(ByVal writer As IO.BinaryWriter)
        writer.Write(UShort.MaxValue) 'indicate we are not the old version without the format flag
        writer.Write(FormatVersion)
        writer.Write(CUInt(clientProfiles.Count))
        For Each profile In clientProfiles
            profile.Save(writer)
        Next profile
        writer.Write(CUInt(pluginProfiles.Count))
        For Each profile In pluginProfiles
            profile.Save(writer)
        Next profile
    End Sub
    Public Sub Load(ByVal reader As IO.BinaryReader)
        Dim first_version = True
        Dim n As UInteger = reader.ReadUInt16()
        If n = UInt16.MaxValue Then 'not the old version without the format flag
            Dim ver = reader.ReadUInt32()
            If ver > FormatVersion Then Throw New IO.InvalidDataException("Unrecognized bot data format version.")
            n = reader.ReadUInt32()
            first_version = False
        End If

        clientProfiles.Clear()
        For repeat = 1UI To n
            clientProfiles.Add(New ClientProfile(reader))
        Next repeat
        If first_version Then
            pluginProfiles.Clear()
            Return
        End If

        pluginProfiles.Clear()
        For repeat = 1UI To reader.ReadUInt32()
            pluginProfiles.Add(New Plugins.PluginProfile(reader))
        Next repeat
    End Sub

    Private Function CreateServer(ByVal name As String,
                                  ByVal serverSettings As ServerSettings,
                                  Optional ByVal suffix As String = "",
                                  Optional ByVal avoidNameCollision As Boolean = False) As W3Server
        Contract.Ensures(Contract.Result(Of W3Server)() IsNot Nothing)
        If name.Trim = "" Then
            Throw New ArgumentException("Invalid server name.")
        ElseIf HaveServer(name) Then
            If Not avoidNameCollision Then
                Throw New InvalidOperationException("Server with name '{0}' already exists.".Frmt(name))
            End If
            Dim i = 2
            While HaveServer(name + i.ToString(CultureInfo.InvariantCulture))
                i += 1
            End While
            name += i.ToString(CultureInfo.InvariantCulture)
        End If

        Dim server As W3Server = New W3Server(name, Me, serverSettings, suffix)
        AddHandler server.PlayerTalked, AddressOf CatchServerPlayerTalked
        AddHandler server.ChangedState, AddressOf CatchServerStateChanged
        servers.Add(server)
        ThrowAddedServer(server)

        Return server
    End Function
    Private Sub KillServer(ByVal name As String)
        Contract.Requires(name IsNot Nothing)
        Dim server = FindServer(name)
        If server Is Nothing Then
            Throw New InvalidOperationException("No server with name {0}.".Frmt(name))
        End If

        RemoveHandler server.PlayerTalked, AddressOf CatchServerPlayerTalked
        RemoveHandler server.ChangedState, AddressOf CatchServerStateChanged
        servers.Remove(server)
        server.QueueKill()
        ThrowRemovedServer(server)
    End Sub

    Private Sub AddWidget(ByVal widget As IBotWidget)
        Contract.Requires(widget IsNot Nothing)
        If FindWidget(widget.TypeName, widget.Name) IsNot Nothing Then
            Throw New InvalidOperationException("A {0} named {1} already exists.".Frmt(widget.TypeName, widget.Name))
        End If
        widgets.Add(widget)
        ThrowAddedWidget(widget)
    End Sub
    Private Sub RemoveWidget(ByVal typeName As String, ByVal name As String)
        Contract.Requires(name IsNot Nothing)
        Contract.Requires(typeName IsNot Nothing)
        Dim widget = FindWidget(name, typeName)
        If widget Is Nothing Then Throw New InvalidOperationException("No {0} with name {1}.".Frmt(typeName, name))
        widgets.Remove(widget)
        widget.[Stop]()
        ThrowRemovedWidget(widget)
    End Sub

    Private Function CreateClient(ByVal name As String, Optional ByVal profileName As String = Nothing) As BnetClient
        Contract.Requires(name IsNot Nothing)
        Contract.Ensures(Contract.Result(Of BnetClient)() IsNot Nothing)

        If profileName Is Nothing Then profileName = "Default"
        Dim profile = FindClientProfile(profileName)
        If name.Trim = "" Then
            Throw New ArgumentException("Invalid client name.")
        ElseIf HaveClient(name) Then
            Throw New ArgumentException("Client named '{0}' already exists.".Frmt(name))
        ElseIf profile Is Nothing Then
            Throw New ArgumentException("Invalid profile.")
        End If

        Dim client = New BnetClient(Me, profile, name)
        AddHandler client.ReceivedPacket, AddressOf CatchClientReceivedPacket
        AddHandler client.StateChanged, AddressOf CatchClientStateChanged
        clients.Add(client)

        ThrowAddedClient(client)
        Return client
    End Function
    Private Sub KillClient(ByVal name As String, ByVal expected As Boolean, ByVal reason As String)
        Contract.Requires(name IsNot Nothing)
        Contract.Requires(reason IsNot Nothing)
        Dim client = FindClient(name)
        If client Is Nothing Then
            Throw New InvalidOperationException("No client named {0}.".Frmt(name))
        End If

        RemoveHandler client.ReceivedPacket, AddressOf CatchClientReceivedPacket
        RemoveHandler client.StateChanged, AddressOf CatchClientStateChanged
        client.QueueDisconnect(expected, reason)
        clients.Remove(client)
        ThrowRemovedClient(client)
    End Sub

    Private Function HaveClient(ByVal name As String) As Boolean
        Contract.Requires(name IsNot Nothing)
        Return FindClient(name) IsNot Nothing
    End Function
    Private Function HaveServer(ByVal name As String) As Boolean
        Contract.Requires(name IsNot Nothing)
        Return FindServer(name) IsNot Nothing
    End Function

    Private Function FindClient(ByVal name As String) As BnetClient
        Contract.Requires(name IsNot Nothing)
        Return (From x In clients Where x.Name.ToUpperInvariant = name.ToUpperInvariant).FirstOrDefault()
    End Function
    Private Function FindServer(ByVal name As String) As W3Server
        Contract.Requires(name IsNot Nothing)
        Return (From x In servers Where x.Name.ToUpperInvariant = name.ToUpperInvariant).FirstOrDefault()
    End Function
    Private Function FindWidget(ByVal name As String, ByVal typeName As String) As IBotWidget
        Contract.Requires(name IsNot Nothing)
        Contract.Requires(typeName IsNot Nothing)
        Return (From x In widgets
                Where x.Name.ToUpperInvariant = name.ToUpperInvariant
                Where x.TypeName.ToUpperInvariant = typeName.ToUpperInvariant).FirstOrDefault()
    End Function
    Public Function FindClientProfile(ByVal name As String) As ClientProfile
        Contract.Requires(name IsNot Nothing)
        Return (From x In clientProfiles Where x.name.ToUpperInvariant = name.ToUpperInvariant).FirstOrDefault
    End Function
    Public Function FindPluginProfile(ByVal name As String) As Plugins.PluginProfile
        Contract.Requires(name IsNot Nothing)
        Return (From x In pluginProfiles Where x.name.ToUpperInvariant = name.ToUpperInvariant).FirstOrDefault
    End Function

    Private Sub Kill()
        'Kill clients
        For Each client In clients.ToList
            KillClient(client.Name, expected:=True, reason:="Bot Killed")
        Next client

        'Kill servers
        For Each server In servers.ToList
            KillServer(server.Name)
        Next server

        'Kill widgets
        For Each widget In widgets.ToList
            RemoveWidget(widget.TypeName, widget.Name)
        Next widget
    End Sub

    Private ReadOnly loadedPluginNames As New HashSet(Of String)
    Private Function LoadPlugin(ByVal name As String) As Plugins.IPlugin
        Contract.Requires(name IsNot Nothing)
        If loadedPluginNames.Contains(name) Then Throw New InvalidOperationException("Plugin already loaded.")

        Dim profile = FindPluginProfile(name)
        If profile Is Nothing Then Throw New InvalidOperationException("No plugin matches the name '{0}'.".Frmt(name))

        Return pluginManager.LoadPlugin(profile.name, profile.location)
    End Function
    Private Sub CatchUnloadedPlugin(ByVal name As String, ByVal plugin As Plugins.IPlugin, ByVal reason As String) Handles pluginManager.UnloadedPlugin
        Contract.Requires(name IsNot Nothing)
        Contract.Requires(name IsNot Nothing)
        Contract.Requires(reason IsNot Nothing)
        logger.Log("Plugin '{0}' was unloaded ({1})".Frmt(name, reason), LogMessageType.Negative)
    End Sub
#End Region

#Region "Access"
    Public Shared Function WC3MajorVersion() As Byte
        Return WC3Version(2)
    End Function
    Public Shared Function WC3Version() As Byte()
        Contract.Ensures(Contract.Result(Of Byte())() IsNot Nothing)
        Dim exeS = My.Settings.exeVersion
        Contract.Assume(exeS IsNot Nothing)
        Dim ss = exeS.Split("."c)
        If ss.Length <> 4 Then Throw New ArgumentException("Invalid version specified in settings. Must have #.#.#.# form.")
        Dim exeV(0 To 3) As Byte
        For i = 0 To 3
            Contract.Assume(ss(i) IsNot Nothing)
            If Not Integer.TryParse(ss(i), 0) Or ss(i).Length > 8 Then
                Throw New ArgumentException("Invalid version specified in settings. Must have #.#.#.# form.")
            End If
            exeV(i) = CByte(CInt(ss(i)) And &HFF)
        Next i
        Return exeV.Reverse.ToArray
    End Function
    Public Shared Function WC3Path() As String
        Return My.Settings.war3path
    End Function
    Public Shared Function MapPath() As String
        Return My.Settings.mapPath
    End Function

    Private Function CreateLanAdmin(ByVal name As String,
                                    ByVal password As String,
                                    Optional ByVal remoteHost As String = "localhost",
                                    Optional ByVal listenPort As UShort = 0) As IFuture
        Dim map = New W3Map("Maps\",
                            "Maps\AdminGame.w3x",
                            filesize:=1,
                            fileChecksumCRC32:=&H12345678UI,
                            mapChecksumSHA1:=(From b In Enumerable.Range(0, 20) Select CByte(b)).ToArray(),
                            mapChecksumXORO:=&H2357BDUI,
                            slotCount:=2)
        Contract.Assume(map.Slots(1) IsNot Nothing)
        map.slots(1).contents = New W3SlotContentsComputer(map.slots(1), W3Slot.ComputerLevel.Normal)
        Dim header = New W3GameDescription("Admin Game",
                                      New W3GameStats(map, My.Resources.ProgramName, New Commands.CommandArgument("")),
                                      0,
                                      0,
                                      0,
                                      playerSlotCount:=map.NumPlayerSlots,
                                      gameType:=map.GameType,
                                      state:=0)
        Dim settings = New ServerSettings(map:=map,
                                          header:=header,
                                          allowUpload:=False,
                                          defaultSlotLockState:=W3Slot.Lock.Frozen,
                                          instances:=0,
                                          password:=password,
                                          isAdminGame:=True,
                                          argument:=New Commands.CommandArgument("-permanent"))
        Dim server = CreateServer(name, settings)
        Dim lan As W3LanAdvertiser
        lan = New W3LanAdvertiser(Me, name, listenPort, remoteHost)
        Try
            AddWidget(lan)
            lan.AddGame(header)
        Catch e As Exception
            lan.Dispose()
            Throw
        End Try

        Dim result = server.QueueOpenPort(listenPort)
        result.CallWhenReady(
            Sub(exception)
                Contract.Assume(lan IsNot Nothing)
                Contract.Assume(server IsNot Nothing)
                If exception IsNot Nothing Then
                    server.QueueKill()
                    lan.Dispose()
                Else
                    DisposeLink.CreateOneWayLink(lan, server)
                    DisposeLink.CreateOneWayLink(server, lan)
                End If
            End Sub
        )
        Return result
    End Function
#End Region

#Region "Events"
    Private Sub ThrowAddedWidget(ByVal widget As IBotWidget)
        Contract.Requires(widget IsNot Nothing)
        eref.QueueAction(Sub()
                             RaiseEvent AddedWidget(widget)
                         End Sub)
    End Sub
    Private Sub ThrowRemovedWidget(ByVal widget As IBotWidget)
        Contract.Requires(widget IsNot Nothing)
        eref.QueueAction(Sub()
                             RaiseEvent RemovedWidget(widget)
                         End Sub)
    End Sub
    Private Sub ThrowAddedServer(ByVal server As W3Server)
        Contract.Requires(server IsNot Nothing)
        eref.QueueAction(Sub()
                             RaiseEvent AddedServer(server)
                         End Sub)
    End Sub
    Private Sub ThrowRemovedServer(ByVal server As W3Server)
        Contract.Requires(server IsNot Nothing)
        eref.QueueAction(Sub()
                             RaiseEvent RemovedServer(server)
                         End Sub)
    End Sub
    Private Sub ThrowAddedClient(ByVal client As BnetClient)
        Contract.Requires(client IsNot Nothing)
        eref.QueueAction(Sub()
                             RaiseEvent AddedClient(client)
                         End Sub)
    End Sub
    Private Sub ThrowRemovedClient(ByVal client As BnetClient)
        Contract.Requires(client IsNot Nothing)
        eref.QueueAction(Sub()
                             RaiseEvent RemovedClient(client)
                         End Sub)
    End Sub

    Private Sub CatchClientReceivedPacket(ByVal client As BnetClient, ByVal packet As BnetPacket)
        Contract.Requires(client IsNot Nothing)
        Contract.Requires(packet IsNot Nothing)
        If packet.id <> BnetPacketId.ChatEvent Then Return

        Dim vals = CType(packet.payload.Value, Dictionary(Of String, Object))
        Dim id = CType(vals("event id"), BnetPacket.ChatEventId)
        Dim username = CStr(vals("username"))
        Dim text = CStr(vals("text"))

        'Exit if this is not a command
        If id <> Bnet.BnetPacket.ChatEventId.Talk And id <> Bnet.BnetPacket.ChatEventId.Whisper Then
            Return
        ElseIf text.Substring(0, My.Settings.commandPrefix.Length) <> My.Settings.commandPrefix Then
            If text.ToUpperInvariant <> "?TRIGGER" Then
                Return
            End If
        End If

        'Get user
        Dim user As BotUser = client.profile.users(username)
        If user Is Nothing Then Return

        'Process ?Trigger command
        If text.ToUpperInvariant = "?TRIGGER" Then
            client.QueueSendWhisper(username, "Command prefix is '{0}'".Frmt(My.Settings.commandPrefix))
            Return
        End If

        'Process prefixed commands
        Dim commandText = text.Substring(My.Settings.commandPrefix.Length)
        Dim commandResult = ClientCommands.Invoke(client, user, commandText)
        commandResult.CallWhenValueReady(
            Sub(message, messageException)
                Contract.Assume(user IsNot Nothing)
                If messageException IsNot Nothing Then
                    client.QueueSendWhisper(user.Name, "Failed: {0}".Frmt(messageException.Message))
                ElseIf message IsNot Nothing Then
                    client.QueueSendWhisper(user.Name, message)
                Else
                    client.QueueSendWhisper(user.Name, "Command Succeeded")
                End If
            End Sub
        )
        FutureWait(2.Seconds).CallWhenReady(
            Sub()
                If commandResult.State = FutureState.Unknown Then
                    client.QueueSendWhisper(user.Name, "Command '{0}' is running... You will be informed when it finishes.".Frmt(text))
                End If
            End Sub
            )
    End Sub

    Private Sub CatchServerPlayerTalked(ByVal sender As W3Server,
                                        ByVal game As W3Game,
                                        ByVal player As W3Player,
                                        ByVal text As String)
        Contract.Requires(sender IsNot Nothing)
        Contract.Requires(game IsNot Nothing)
        Contract.Requires(player IsNot Nothing)
        Contract.Requires(text IsNot Nothing)

        Dim prefix = My.Settings.commandPrefix
        Contract.Assume(prefix IsNot Nothing)
        If Not text.StartsWith(prefix) AndAlso text.ToUpperInvariant <> "?TRIGGER" Then
            Return
        End If

        'Process ?trigger command
        If text.ToUpperInvariant = "?TRIGGER" Then
            game.QueueSendMessageTo("Command prefix is '{0}'".Frmt(prefix), player)
            Return
        End If

        'Process prefixed commands
        Dim commandText = text.Substring(My.Settings.commandPrefix.Length)
        game.QueueCommandProcessText(Me, player, commandText).CallWhenValueReady(
            Sub(message, messageException)
                Contract.Assume(player IsNot Nothing)
                If messageException IsNot Nothing Then
                    game.QueueSendMessageTo("Failed: {0}".Frmt(messageException.Message), player)
                ElseIf message IsNot Nothing Then
                    game.QueueSendMessageTo(message, player)
                Else
                    game.QueueSendMessageTo("Command Succeeded", player)
                End If
            End Sub
        )
    End Sub

    Private Sub CatchClientStateChanged(ByVal sender As BnetClient, ByVal oldState As BnetClientState, ByVal newState As BnetClientState)
        Contract.Requires(sender IsNot Nothing)
        RaiseEvent ClientStateChanged(sender, oldState, newState)
    End Sub
    Private Sub CatchServerStateChanged(ByVal sender As W3Server, ByVal oldState As W3ServerState, ByVal newState As W3ServerState)
        Contract.Requires(sender IsNot Nothing)
        RaiseEvent ServerStateChanged(sender, oldState, newState)
    End Sub
#End Region

#Region "Remote Calls"
    Public Function QueueFindServer(ByVal name As String) As IFuture(Of W3Server)
        Contract.Requires(name IsNot Nothing)
        Return ref.QueueFunc(Function()
                                 Contract.Assume(name IsNot Nothing)
                                 Return FindServer(name)
                             End Function)
    End Function
    Public Function QueueKill() As IFuture
        Return ref.QueueAction(AddressOf Kill)
    End Function
    Public Function QueueCreateLanAdmin(ByVal name As String,
                                        ByVal password As String,
                                        Optional ByVal remoteHostName As String = "localhost",
                                        Optional ByVal listenPort As UShort = 0) As IFuture
        Contract.Requires(name IsNot Nothing)
        Contract.Requires(password IsNot Nothing)
        Contract.Requires(remoteHostName IsNot Nothing)
        Return ref.QueueFunc(Function() CreateLanAdmin(name, password, remoteHostName, listenPort)).Defuturized
    End Function
    Public Function QueueAddWidget(ByVal widget As IBotWidget) As IFuture
        Contract.Requires(widget IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(Sub()
                                   Contract.Assume(widget IsNot Nothing)
                                   AddWidget(widget)
                               End Sub)
    End Function
    Public Function QueueRemoveWidget(ByVal typeName As String, ByVal name As String) As IFuture
        Contract.Requires(name IsNot Nothing)
        Contract.Requires(typeName IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(Sub()
                                   Contract.Assume(typeName IsNot Nothing)
                                   Contract.Assume(name IsNot Nothing)
                                   RemoveWidget(typeName, name)
                               End Sub)
    End Function
    Public Function QueueRemoveServer(ByVal name As String) As IFuture
        Contract.Requires(name IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(Sub()
                                   Contract.Assume(name IsNot Nothing)
                                   KillServer(name)
                               End Sub)
    End Function
    Public Function QueueCreateServer(ByVal name As String,
                                      ByVal defaultSettings As ServerSettings,
                                      Optional ByVal suffix As String = "",
                                      Optional ByVal avoidNameCollisions As Boolean = False) As IFuture(Of W3Server)
        Contract.Requires(name IsNot Nothing)
        Contract.Requires(defaultSettings IsNot Nothing)
        Contract.Requires(suffix IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of W3Server))() IsNot Nothing)
        Return ref.QueueFunc(Function()
                                 Contract.Assume(name IsNot Nothing)
                                 Contract.Assume(defaultSettings IsNot Nothing)
                                 Contract.Assume(suffix IsNot Nothing)
                                 Return CreateServer(name, defaultSettings, suffix, avoidNameCollisions)
                             End Function)
    End Function
    Public Function QueueFindClient(ByVal name As String) As IFuture(Of BnetClient)
        Contract.Requires(name IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of BnetClient))() IsNot Nothing)
        Return ref.QueueFunc(Function()
                                 Contract.Assume(name IsNot Nothing)
                                 Return FindClient(name)
                             End Function)
    End Function
    Public Function QueueRemoveClient(ByVal name As String, ByVal expected As Boolean, ByVal reason As String) As IFuture
        Contract.Requires(name IsNot Nothing)
        Contract.Requires(reason IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture)() IsNot Nothing)
        Return ref.QueueAction(Sub()
                                   Contract.Assume(name IsNot Nothing)
                                   Contract.Assume(reason IsNot Nothing)
                                   KillClient(name, expected, reason)
                               End Sub)
    End Function
    Public Function QueueCreateClient(ByVal name As String, Optional ByVal profileName As String = Nothing) As IFuture(Of BnetClient)
        Contract.Requires(name IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of BnetClient))() IsNot Nothing)
        Return ref.QueueFunc(Function() CreateClient(name, profileName))
    End Function
    Public Function QueueGetServers() As IFuture(Of List(Of W3Server))
        Contract.Ensures(Contract.Result(Of IFuture(Of List(Of W3Server)))() IsNot Nothing)
        Return ref.QueueFunc(Function() servers.ToList)
    End Function
    Public Function QueueGetClients() As IFuture(Of List(Of BnetClient))
        Contract.Ensures(Contract.Result(Of IFuture(Of List(Of BnetClient)))() IsNot Nothing)
        Return ref.QueueFunc(Function() clients.ToList)
    End Function
    Public Function QueueGetWidgets() As IFuture(Of List(Of IBotWidget))
        Contract.Ensures(Contract.Result(Of IFuture(Of List(Of IBotWidget)))() IsNot Nothing)
        Return ref.QueueFunc(Function() widgets.ToList)
    End Function
    Public Function QueueLoadPlugin(ByVal name As String) As IFuture(Of Plugins.IPlugin)
        Contract.Requires(name IsNot Nothing)
        Contract.Ensures(Contract.Result(Of IFuture(Of Plugins.IPlugin))() IsNot Nothing)
        Return ref.QueueFunc(Function() LoadPlugin(name))
    End Function
#End Region
End Class

Public NotInheritable Class PortPool
    Private ReadOnly _inPorts As New HashSet(Of UShort)
    Private ReadOnly _outPorts As New HashSet(Of UShort)
    Private ReadOnly _portPool As New HashSet(Of UShort)
    Private ReadOnly lock As New Object()
    Private ReadOnly Property PortPool As HashSet(Of UShort)
        Get
            Contract.Ensures(Contract.Result(Of HashSet(Of UShort))() IsNot Nothing)
            Return _portPool
        End Get
    End Property
    Private ReadOnly Property InPorts As HashSet(Of UShort)
        Get
            Contract.Ensures(Contract.Result(Of HashSet(Of UShort))() IsNot Nothing)
            Return _inPorts
        End Get
    End Property
    Private ReadOnly Property OutPorts As HashSet(Of UShort)
        Get
            Contract.Ensures(Contract.Result(Of HashSet(Of UShort))() IsNot Nothing)
            Return _outPorts
        End Get
    End Property
    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(_inPorts IsNot Nothing)
        Contract.Invariant(_outPorts IsNot Nothing)
        Contract.Invariant(_portPool IsNot Nothing)
        Contract.Invariant(lock IsNot Nothing)
    End Sub

    Public Function EnumPorts() As IEnumerable(Of UShort)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of UShort))() IsNot Nothing)
        SyncLock lock
            Return PortPool.ToArray()
        End SyncLock
    End Function
    Public Function EnumUsedPorts() As IEnumerable(Of UShort)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of UShort))() IsNot Nothing)
        SyncLock lock
            Return OutPorts.ToArray()
        End SyncLock
    End Function
    Public Function EnumAvailablePorts() As IEnumerable(Of UShort)
        Contract.Ensures(Contract.Result(Of IEnumerable(Of UShort))() IsNot Nothing)
        SyncLock lock
            Return InPorts.ToArray()
        End SyncLock
    End Function

    Public Enum TryAddPortOutcome
        AlreadyInPool
        Added
        '''<summary>The port was still in use when removed from the pool, and is still in use now after being re-added.</summary>
        ReturnedButStillInUse
    End Enum
    Public Function TryAddPort(ByVal port As UShort) As TryAddPortOutcome
        SyncLock lock
            If PortPool.Contains(port) Then Return TryAddPortOutcome.AlreadyInPool
            PortPool.Add(port)
            If OutPorts.Contains(port) Then Return TryAddPortOutcome.ReturnedButStillInUse
            InPorts.Add(port)
            Return TryAddPortOutcome.Added
        End SyncLock
    End Function

    Public Enum TryRemovePortOutcome
        WasNotInPool
        RemovedFromPool
        '''<summary>The port was in use, but it is 'removed' in that it will not return to the pool when it is released.</summary>
        RemovedFromPoolButStillInUse
    End Enum
    Public Function TryRemovePort(ByVal port As UShort) As TryRemovePortOutcome
        SyncLock lock
            If Not PortPool.Contains(port) Then Return TryRemovePortOutcome.WasNotInPool
            PortPool.Remove(port)
            If OutPorts.Contains(port) Then Return TryRemovePortOutcome.RemovedFromPoolButStillInUse
            InPorts.Remove(port)
            Return TryRemovePortOutcome.RemovedFromPool
        End SyncLock
    End Function

    Public Function TryAcquireAnyPort() As PortHandle
        SyncLock lock
            If InPorts.Count = 0 Then Return Nothing
            Dim port = New PortHandle(Me, InPorts.First)
            InPorts.Remove(port.Port)
            OutPorts.Add(port.Port)
            Return port
        End SyncLock
    End Function

    Public NotInheritable Class PortHandle
        Inherits FutureDisposable
        Private ReadOnly pool As PortPool
        Private ReadOnly _port As UShort

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(pool IsNot Nothing)
        End Sub

        Public Sub New(ByVal pool As PortPool, ByVal port As UShort)
            Contract.Requires(pool IsNot Nothing)
            Me.pool = pool
            Me._port = port
        End Sub

        Public ReadOnly Property Port() As UShort
            Get
                If FutureDisposed.State <> FutureState.Unknown Then Throw New ObjectDisposedException(Me.GetType.Name)
                Return _port
            End Get
        End Property

        Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
            SyncLock pool.lock
                Contract.Assume(pool.OutPorts.Contains(_port))
                Contract.Assume(Not pool.InPorts.Contains(_port))
                pool.OutPorts.Remove(_port)
                If pool.PortPool.Contains(_port) Then
                    pool.InPorts.Add(_port)
                End If
            End SyncLock
        End Sub
    End Class
End Class
