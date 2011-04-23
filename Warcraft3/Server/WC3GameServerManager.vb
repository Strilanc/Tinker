Imports Tinker.Commands
Imports Tinker.Components

Namespace WC3
    Public Class GameServerManager
        Inherits DisposableWithTask
        Implements IBotComponent

        Public Shared ReadOnly TypeName As String = "Server"

        Private ReadOnly _commands As New CommandSet(Of GameServerManager)
        Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue()
        Private ReadOnly _name As InvariantString
        Private ReadOnly _gameServer As WC3.GameServer
        Private ReadOnly _hooks As New List(Of IDisposable)
        Private ReadOnly _control As Control
        Private ReadOnly _bot As Bot.MainBot
        Private _listener As Net.Sockets.TcpListener
        Private _portHandle As PortPool.PortHandle

        Private _gameIdCount As UInteger
        Private ReadOnly _gameIdGenerator As New Random()

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_gameIdGenerator IsNot Nothing)
            Contract.Invariant(_gameServer IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(_control IsNot Nothing)
            Contract.Invariant(_bot IsNot Nothing)
            Contract.Invariant(_listener IsNot Nothing)
            Contract.Invariant(_portHandle IsNot Nothing)
            Contract.Invariant(_commands IsNot Nothing)
        End Sub

        Public Sub New(name As InvariantString,
                       gameServer As WC3.GameServer,
                       bot As Bot.MainBot)
            Contract.Requires(gameServer IsNot Nothing)
            Contract.Requires(bot IsNot Nothing)

            Me._portHandle = bot.PortPool.TryAcquireAnyPort()
            If Me._portHandle Is Nothing Then Throw New InvalidOperationException("There are no ports for the server available in the pool.")
            Me._name = name
            Me._gameServer = gameServer

            Dim control = New WC3.W3ServerControl(Me)
            Me._control = control
            Me._bot = bot
            Me._listener = New Net.Sockets.TcpListener(Net.IPAddress.Any.WithPort(Me._portHandle.Port))
            Me._listener.Start()

            AddHandler _gameServer.PlayerTalked, AddressOf OnPlayerTalked
            _hooks.Add(New DelegatedDisposable(Sub() RemoveHandler _gameServer.PlayerTalked, AddressOf OnPlayerTalked))

            BeginAccepting()

            _gameServer.ChainEventualDisposalTo(Me)
        End Sub

        Public ReadOnly Property Server As WC3.GameServer
            Get
                Contract.Ensures(Contract.Result(Of WC3.GameServer)() IsNot Nothing)
                Return _gameServer
            End Get
        End Property

        Private Async Sub BeginAccepting()
            Dim listener = Me._listener
            Try
                Do
                    Dim tcpClient = Await listener.AsyncAcceptConnection()
                    Dim socket = New WC3.W3Socket(New PacketSocket(
                                                      stream:=tcpClient.GetStream,
                                                      localendpoint:=DirectCast(tcpClient.Client.LocalEndPoint, Net.IPEndPoint),
                                                      remoteendpoint:=DirectCast(tcpClient.Client.RemoteEndPoint, Net.IPEndPoint),
                                                      timeout:=60.Seconds,
                                                      Logger:=Logger,
                                                      clock:=_gameServer.Clock))
                    Await _gameServer.QueueAcceptSocket(socket)
                Loop
            Catch ex As Exception
                If listener IsNot Me._listener Then Return 'listener was just closed or changed
                ex.RaiseAsUnexpected("Accepting connections for game server.")
                Logger.Log("Error accepting connections: {0}".Frmt(ex.Summarize), LogMessageType.Problem)
            End Try
        End Sub

        Private Sub ChangeListenPort(portHandle As PortPool.PortHandle)
            Contract.Requires(portHandle IsNot Nothing)
            If portHandle.Port = Me._portHandle.Port Then Return

            'Try new port before disposing the old one
            Dim oldListener = Me._listener
            Dim newListener = New Net.Sockets.TcpListener(Net.IPAddress.Any.WithPort(portHandle.Port))
            newListener.Start()

            'Switch handles
            Me._portHandle.Dispose()
            Me._portHandle = portHandle
            Me._listener = newListener

            'Continue accepting
            BeginAccepting()
            oldListener.Stop()
        End Sub

        Public ReadOnly Property Name As InvariantString Implements IBotComponent.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property Type As InvariantString Implements IBotComponent.Type
            Get
                Return TypeName
            End Get
        End Property
        Public ReadOnly Property Logger As Logger Implements IBotComponent.Logger
            Get
                Return _gameServer.Logger
            End Get
        End Property
        Public ReadOnly Property HasControl As Boolean Implements IBotComponent.HasControl
            Get
                Contract.Ensures(Contract.Result(Of Boolean)())
                Return True
            End Get
        End Property
        Public ReadOnly Property Bot As Bot.MainBot
            Get
                Contract.Ensures(Contract.Result(Of Bot.MainBot)() IsNot Nothing)
                Return _bot
            End Get
        End Property
        Public Function IsArgumentPrivate(argument As String) As Boolean Implements IBotComponent.IsArgumentPrivate
            Return _commands.IsArgumentPrivate(argument)
        End Function
        Public ReadOnly Property Control As System.Windows.Forms.Control Implements IBotComponent.Control
            Get
                Return _control
            End Get
        End Property

        Public Function InvokeCommand(user As BotUser, argument As String) As Task(Of String) Implements IBotComponent.InvokeCommand
            Return _commands.Invoke(Me, user, argument)
        End Function

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            For Each hook In _hooks
                Contract.Assume(hook IsNot Nothing)
                hook.Dispose()
            Next hook
            _gameServer.Dispose()
            Return _control.DisposeControlAsync()
        End Function

        Private Async Sub OnPlayerTalked(sender As WC3.GameServer,
                                         game As WC3.Game,
                                         player As WC3.Player,
                                         text As String)
            Contract.Assume(sender IsNot Nothing)
            Contract.Assume(game IsNot Nothing)
            Contract.Assume(player IsNot Nothing)
            Contract.Assume(text IsNot Nothing)

            Dim prefix = My.Settings.commandPrefix.AssumeNotNull
            If text = Tinker.Bot.MainBot.TriggerCommandText Then '?trigger command
                game.QueueSendMessageTo("Command prefix is '{0}'".Frmt(prefix), player)
                Return
            ElseIf Not text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) Then 'not a command
                Return
            End If

            'Normal commands
            Dim commandText = text.Substring(prefix.Length)

            Try
                Dim result = Await game.QueueCommandProcessText(game.HackManager, player, commandText)
                If String.IsNullOrEmpty(result) Then result = "Command Succeeded"
                game.QueueSendMessageTo(result, player)
            Catch ex As Exception
                game.QueueSendMessageTo("Failed: {0}".Frmt(ex.Summarize), player)
            End Try
        End Sub

        Private Function AllocateGameId() As UInt32
            Contract.Ensures(Contract.Result(Of UInt32)() > 0)
            _gameIdCount += 1UI
            If _gameIdCount > 1000 Then _gameIdCount = 1
            Return _gameIdCount * 10000UI + CUInt(_gameIdGenerator.Next(1000))
        End Function

        <ContractVerification(False)>
        Private Function AsyncAddGameFromArguments(argument As Commands.CommandArgument,
                                                   user As BotUser) As Task(Of WC3.GameSet)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of WC3.GameSet))() IsNot Nothing)

            Dim map = WC3.Map.FromArgument(argument.NamedValue("map"))

            Dim hostName = If(user Is Nothing, Application.ProductName, user.Name.Value)
            Contract.Assume(hostName IsNot Nothing)
            Dim gameStats = WC3.GameStats.FromMapAndArgument(map, hostName, argument)

            Dim totalSlotCount = map.LobbySlots.Count
            Select Case gameStats.observers
                Case WC3.GameObserverOption.FullObservers, WC3.GameObserverOption.Referees
                    totalSlotCount = 12
            End Select

            Dim gameDescription = New WC3.LocalGameDescription(
                                            Name:=argument.NamedValue("name"),
                                            gameStats:=gameStats,
                                            hostport:=_portHandle.Port,
                                            gameId:=AllocateGameId(),
                                            EntryKey:=0,
                                            totalSlotCount:=totalSlotCount,
                                            GameType:=map.FilterGameType,
                                            state:=0,
                                            UsedSlotCount:=0,
                                            ageClock:=New SystemClock())

            Dim gameSettings = WC3.GameSettings.FromArgument(map, gameDescription, argument)

            Return _gameServer.QueueAddGameSet(gameSettings)
        End Function
        Public Function QueueAddGameFromArguments(argument As Commands.CommandArgument,
                                                  user As BotUser) As Task(Of WC3.GameSet)
            Contract.Requires(argument IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of WC3.GameSet))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncAddGameFromArguments(argument, user)).Unwrap.AssumeNotNull
        End Function
        Public Function QueueChangeListenPort(portHandle As PortPool.PortHandle) As Task
            Contract.Requires(portHandle IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() ChangeListenPort(portHandle))
        End Function
        Public Function QueueGetListenPort() As Task(Of UShort)
            Contract.Ensures(Contract.Result(Of Task(Of UShort))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _portHandle.Port)
        End Function

        <SuppressMessage("Microsoft.Contracts", "Requires-23-223")>
        <ContractVerification(False)>
        Private Function AsyncAddAdminGame(name As InvariantString, password As String) As Task(Of GameSet)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of GameSet))() IsNot Nothing)

            Dim sha1Checksum = CByte(20).Range()
            Dim slot1 = New Slot(index:=0,
                                 raceUnlocked:=False,
                                 Color:=Protocol.PlayerColor.Red,
                                 contents:=New SlotContentsOpen,
                                 locked:=Slot.LockState.Frozen,
                                 team:=0)
            Dim slot2 = slot1.With(index:=1,
                                   color:=Protocol.PlayerColor.Blue,
                                   contents:=New SlotContentsClosed)
            Dim slots = MakeRist(slot1, slot2)
            Contract.Assume(slots.Count = 2)
            Dim map = New WC3.Map(streamFactory:=Nothing,
                                  advertisedPath:="Maps\AdminGame.w3x",
                                  fileSize:=1,
                                  fileChecksumCRC32:=&H12345678UI,
                                  mapChecksumSHA1:=sha1Checksum,
                                  mapChecksumXORO:=&H2357BDUI,
                                  ismelee:=False,
                                  usesCustomForces:=True,
                                  usesFixedPlayerSettings:=True,
                                  name:="Admin Game",
                                  playableWidth:=256,
                                  playableHeight:=56,
                                  lobbySlots:=slots)

            Dim hostName = Application.ProductName
            Contract.Assume(hostName IsNot Nothing)
            Dim gameDescription = New WC3.LocalGameDescription(
                                          name:=name,
                                          GameStats:=WC3.GameStats.FromMapAndArgument(map, hostName, New Commands.CommandArgument("")),
                                          gameid:=AllocateGameId(),
                                          entryKey:=0,
                                          totalSlotCount:=map.LobbySlots.Count,
                                          gameType:=map.FilterGameType,
                                          state:=0,
                                          usedSlotCount:=0,
                                          hostPort:=_portHandle.Port,
                                          ageClock:=New SystemClock())
            Dim gameSettings = WC3.GameSettings.FromArgument(map,
                                                             gameDescription,
                                                             New Commands.CommandArgument("-permanent -noul -i=0"),
                                                             isAdminGame:=True,
                                                             adminPassword:=password)

            Return _gameServer.QueueAddGameSet(gameSettings)
        End Function
        Public Function QueueAddAdminGame(name As InvariantString, password As String) As Task(Of GameSet)
            Contract.Requires(password IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of GameSet))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() AsyncAddAdminGame(name, password)).Unwrap.AssumeNotNull
        End Function

        Private Function IncludeCommandImpl(command As ICommand(Of IBotComponent)) As Task(Of IDisposable) Implements IBotComponent.IncludeCommand
            Return IncludeCommand(command)
        End Function
        Public Function IncludeCommand(command As ICommand(Of GameServerManager)) As Task(Of IDisposable)
            Contract.Requires(command IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return _commands.IncludeCommand(command).AsTask()
        End Function
    End Class
End Namespace
