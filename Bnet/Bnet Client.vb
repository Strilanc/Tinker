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

Imports Tinker.Pickling

Namespace Bnet
    Public Enum ClientState As Integer
        Disconnected
        InitiatingTCPConnection
        TCPConnectionEstablished
        AuthenticatingProgram
        EnterUserCredentials
        AuthenticatingUser
        WaitingForEnterChat
        Channel
        CreatingGame
        AdvertisingGame
    End Enum

    'verification disabled due to large amounts of impure methods
    <ContractVerification(False)>
    Public NotInheritable Class Client
        Inherits DisposableWithTask

        Public Shared ReadOnly BnetServerPort As UShort = 6112
        Private Shared ReadOnly RefreshPeriod As TimeSpan = 20.Seconds

        Private ReadOnly outQueue As CallQueue
        Private ReadOnly inQueue As CallQueue

        Private ReadOnly _productInfoProvider As IProductInfoProvider
        Private ReadOnly _clock As IClock
        Private ReadOnly _profile As Bot.ClientProfile
        Private ReadOnly _productAuthenticator As IProductAuthenticator
        Private ReadOnly _logger As Logger
        Private ReadOnly _packetHandlerLogger As PacketHandlerLogger(Of Protocol.PacketId)
        Private _socket As PacketSocket
        Private WithEvents _wardenClient As Warden.Client
        Private _connectCanceller As New CancellationTokenSource()

        'game
        Private Class AdvertisementEntry
            Public ReadOnly BaseGameDescription As WC3.LocalGameDescription
            Public ReadOnly IsPrivate As Boolean
            Private ReadOnly _futureInitialAdvertisedDescription As New TaskCompletionSource(Of WC3.LocalGameDescription)
            Private _failCount As UInt32
            Public Sub New(gameDescription As WC3.LocalGameDescription, isPrivate As Boolean)
                Me.BaseGameDescription = gameDescription
                Me.IsPrivate = isPrivate
                _futureInitialAdvertisedDescription.Task.ConsiderExceptionsHandled()
            End Sub

            Public Sub IncreaseNameFailCount()
                _failCount += 1UI
            End Sub
            Public Sub TryMarkAsSucceeded()
                _futureInitialAdvertisedDescription.TrySetResult(GetCurrentCandidateGameDescriptionWithFixedAge())
            End Sub
            Public Sub TryMarkAsFailed()
                _futureInitialAdvertisedDescription.TrySetException(New InvalidOperationException("Removed before advertising succeeded."))
            End Sub

            Public ReadOnly Property EventualAdvertisedDescription As Task(Of WC3.LocalGameDescription)
                Get
                    Contract.Ensures(Contract.Result(Of Task(Of WC3.LocalGameDescription))() IsNot Nothing)
                    Return _futureInitialAdvertisedDescription.Task
                End Get
            End Property

            Private Function GetCurrentGameName() As String
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Const SuffixCharacters As String = "~!@#$%^&*(+-=,./\'"":;"
                Dim result = BaseGameDescription.Name
                Dim suffix = (From i In _failCount.Bytes.ConvertFromBaseToBase(256, CUInt(SuffixCharacters.Length))
                              Select SuffixCharacters(i)
                              ).AsString
                If result.Length + suffix.Length > Bnet.Protocol.Packets.ClientToServer.MaxGameNameLength Then
                    result = result.Substring(0, Bnet.Protocol.Packets.ClientToServer.MaxGameNameLength - suffix.Length)
                End If
                result += suffix
                Return result
            End Function
            Public Function GetCurrentCandidateGameDescriptionWithFixedAge() As WC3.LocalGameDescription
                Dim useFull = False

                Dim gameState = Protocol.GameStates.Unknown0x10.EnumUInt32WithSet(Protocol.GameStates.Full, useFull
                                                              ).EnumUInt32WithSet(Protocol.GameStates.Private, IsPrivate)

                Dim gameType = BaseGameDescription.GameType Or WC3.Protocol.GameTypes.UnknownButSeen0
                gameType = gameType.EnumUInt32WithSet(WC3.Protocol.GameTypes.PrivateGame, IsPrivate)
                Select Case BaseGameDescription.GameStats.Observers
                    Case WC3.GameObserverOption.FullObservers, WC3.GameObserverOption.Referees
                        gameType = gameType Or WC3.Protocol.GameTypes.ObsFull
                    Case WC3.GameObserverOption.ObsOnDefeat
                        gameType = gameType Or WC3.Protocol.GameTypes.ObsOnDeath
                    Case WC3.GameObserverOption.NoObservers
                        gameType = gameType Or WC3.Protocol.GameTypes.ObsNone
                End Select

                Return BaseGameDescription.With(name:=GetCurrentGameName(),
                                                gameType:=gameType,
                                                state:=gameState,
                                                ageClock:=BaseGameDescription.AgeClock.Stopped())
            End Function
        End Class
        Private ReadOnly _advertisementList As New List(Of AdvertisementEntry)
        Private _curAdvertisement As AdvertisementEntry
        Private _gameCanceller As New CancellationTokenSource()
        Private _reportedListenPort As UShort

        'connection
        Private _reconnecter As IConnecter
        Private _userCredentials As ClientAuthenticator
        Private _allowRetryConnect As Boolean

        Public Event StateChanged(sender As Client, oldState As ClientState, newState As ClientState)
        Public Event AdvertisedGame(sender As Client, gameDescription As WC3.LocalGameDescription, [private] As Boolean, refreshed As Boolean)

        Private _lastChannel As InvariantString
        Private _state As ClientState

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_advertisementList IsNot Nothing)
            Contract.Invariant(_packetHandlerLogger IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_profile IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_productAuthenticator IsNot Nothing)
            Contract.Invariant(_productInfoProvider IsNot Nothing)
            Contract.Invariant((_socket IsNot Nothing) = (_state >= ClientState.TCPConnectionEstablished))
            Contract.Invariant((_wardenClient Is Nothing) OrElse (_state <= ClientState.AuthenticatingProgram))
            Contract.Invariant((_state <= ClientState.EnterUserCredentials) OrElse (_userCredentials IsNot Nothing))
            Contract.Invariant((_curAdvertisement IsNot Nothing) = (_state >= ClientState.CreatingGame))
        End Sub

        Public Sub New(profile As Bot.ClientProfile,
                       productInfoProvider As IProductInfoProvider,
                       productAuthenticator As IProductAuthenticator,
                       clock As IClock,
                       Optional logger As Logger = Nothing)
            Contract.Assume(profile IsNot Nothing)
            Contract.Assume(productInfoProvider IsNot Nothing)
            Contract.Assume(clock IsNot Nothing)
            Contract.Assume(productAuthenticator IsNot Nothing)
            Me._profile = profile
            Me._productInfoProvider = productInfoProvider
            Me._productAuthenticator = productAuthenticator
            Me._clock = clock
            Me._logger = If(logger, New Logger)
            Me.inQueue = MakeTaskedCallQueue()
            Me.outQueue = MakeTaskedCallQueue()
            Me._packetHandlerLogger = Protocol.MakeBnetPacketHandlerLogger(Me._logger)
        End Sub
        <Pure()>
        Public Shared Function MakeProductAuthenticator(profile As Bot.ClientProfile,
                                                        clock As IClock,
                                                        logger As Logger) As IProductAuthenticator
            Contract.Requires(profile IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Contract.Requires(logger IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IProductAuthenticator)() IsNot Nothing)

            If profile.CKLServerAddress <> "" Then
                Dim data = profile.CKLServerAddress.Split(":"c)
                If data.Length <> 2 Then Throw New InvalidOperationException("Invalid CKL server address in profile.")
                Dim remoteHost = data(0)
                Dim port = UShort.Parse(data(1).AssumeNotNull, CultureInfo.InvariantCulture)
                Return New CKL.Client(remoteHost, port, clock, logger)
            End If

            Return New CDKeyProductAuthenticator(profile.cdKeyROC, profile.cdKeyTFT)
        End Function

        Public Sub Init()
            'Handled packets
            IncludeQueuedPacketHandler(Protocol.Packets.ServerToClient.Ping,
                                       Sub(value) SendPacket(Protocol.MakePing(salt:=value.Value)))
            IncludeQueuedPacketHandler(Protocol.Packets.ServerToClient.ChatEvent,
                                       Sub(value)
                                           Dim vals = value.Value
                                           Dim eventId = vals.ItemAs(Of Protocol.ChatEventId)("event id")
                                           Dim text = vals.ItemAs(Of String)("text")
                                           If eventId = Protocol.ChatEventId.Channel Then _lastChannel = text
                                       End Sub)
            IncludeQueuedPacketHandler(Protocol.Packets.ServerToClient.MessageBox,
                                       Sub(value)
                                           Dim vals = value.Value
                                           Dim msg = "MESSAGE BOX FROM BNET: {0}: {1}".Frmt(vals.ItemAs(Of String)("caption"), vals.ItemAs(Of String)("text"))
                                           Logger.Log(msg, LogMessageType.Problem)
                                       End Sub)
            IncludeQueuedPacketHandler(Protocol.Packets.ServerToClient.Warden, AddressOf ReceiveWarden)

            'Ignored or handled-only-at-specific-times packets
            For Each ignoredPacket In New Protocol.Packets.Definition() {
                        Protocol.Packets.ServerToClient.ProgramAuthenticationBegin,
                        Protocol.Packets.ServerToClient.ProgramAuthenticationFinish,
                        Protocol.Packets.ServerToClient.UserAuthenticationBegin,
                        Protocol.Packets.ServerToClient.UserAuthenticationFinish,
                        Protocol.Packets.ServerToClient.Null,
                        Protocol.Packets.ServerToClient.CreateGame3,
                        Protocol.Packets.ServerToClient.GetFileTime,
                        Protocol.Packets.ServerToClient.GetIconData,
                        Protocol.Packets.ServerToClient.QueryGamesList(_clock),
                        Protocol.Packets.ServerToClient.EnterChat,
                        Protocol.Packets.ServerToClient.FriendsUpdate,
                        Protocol.Packets.ServerToClient.RequiredWork}
                _packetHandlerLogger.TryIncludeLogger(ignoredPacket.Id, ignoredPacket.Jar)
            Next
        End Sub

        Public ReadOnly Property Clock As IClock
            Get
                Contract.Ensures(Contract.Result(Of IClock)() IsNot Nothing)
                Return _clock
            End Get
        End Property
        Public ReadOnly Property Profile As Bot.ClientProfile
            Get
                Contract.Ensures(Contract.Result(Of Bot.ClientProfile)() IsNot Nothing)
                Return _profile
            End Get
        End Property
        Public ReadOnly Property Logger As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property
        Public ReadOnly Property UserName() As InvariantString
            Get
                If _userCredentials Is Nothing Then Throw New InvalidOperationException("No credentials to get username from.")
                Return Me._userCredentials.UserName
            End Get
        End Property
        Public Function QueueGetState() As Task(Of ClientState)
            Contract.Ensures(Contract.Result(Of Task(Of ClientState))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _state)
        End Function

        Private Function IncludeQueuedPacketHandler(Of T)(packetDefinition As Protocol.Packets.Definition(Of T),
                                                          handler As Action(Of IPickle(Of T))) As IDisposable
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _packetHandlerLogger.IncludeHandler(
                packetDefinition.Id,
                packetDefinition.Jar,
                Function(value) inQueue.QueueAction(Sub() handler(value)))
        End Function

        Public Async Function QueueIncludePacketHandler(Of T)(packetDefinition As Protocol.Packets.Definition(Of T),
                                                              handler As Func(Of IPickle(Of T), Task)) As Task(Of IDisposable)
            Contract.Requires(packetDefinition IsNot Nothing)
            Contract.Requires(handler IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Await inQueue.AwaitableEntrance()
            Return _packetHandlerLogger.IncludeHandler(packetDefinition.Id, packetDefinition.Jar, handler)
        End Function

        Private Sub SendText(text As String)
            Contract.Requires(_state >= ClientState.Channel)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(text.Length > 0)
            Dim isBnetCommand = text.StartsWith("/", StringComparison.Ordinal)

            Select Case _state
                Case ClientState.Channel
                    'fine
                Case ClientState.CreatingGame, ClientState.AdvertisingGame
                    If Not isBnetCommand Then
                        Throw New InvalidOperationException("Can't send normal messages when in a game (can still send commands).")
                    End If
            End Select

            Dim lines = SplitText(text, maxLineLength:=Protocol.Packets.ClientToServer.MaxChatCommandTextLength)
            If isBnetCommand AndAlso lines.LazyCount > 1 Then
                Throw New InvalidOperationException("Can't send multi-line commands or commands larger than {0} characters.".Frmt(Protocol.Packets.ClientToServer.MaxChatCommandTextLength))
            End If
            For Each line In lines
                Contract.Assume(line IsNot Nothing)
                If line.Length = 0 Then Continue For
                SendPacket(Protocol.MakeChatCommand(line))
            Next line
        End Sub
        Public Function QueueSendText(text As String) As Task
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(text.Length > 0)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendText(text))
        End Function

        Private Sub SendWhisper(username As String, text As String)
            Contract.Requires(username IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            Contract.Requires(username.Length > 0)
            Contract.Requires(text.Length > 0)
            Contract.Requires(_state >= ClientState.Channel)

            Dim prefix = "/w {0} ".Frmt(username)
            Contract.Assume(prefix.Length >= 5)
            If prefix.Length >= Protocol.Packets.ClientToServer.MaxChatCommandTextLength \ 2 Then
                Throw New ArgumentOutOfRangeException("username", "Username is too long.")
            End If

            For Each line In SplitText(text, maxLineLength:=Protocol.Packets.ClientToServer.MaxChatCommandTextLength - prefix.Length)
                Contract.Assume(line IsNot Nothing)
                SendPacket(Protocol.MakeChatCommand(prefix + line))
            Next line
        End Sub
        Public Function QueueSendWhisper(userName As String, text As String) As Task
            Contract.Requires(userName IsNot Nothing)
            Contract.Requires(userName.Length > 0)
            Contract.Requires(text IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendWhisper(userName, text))
        End Function

        Private Sub SetReportedListenPort(port As UShort)
            If port = Me._reportedListenPort Then Return
            Me._reportedListenPort = port
            SendPacket(Protocol.MakeNetGamePort(Me._reportedListenPort))
        End Sub

        Private Sub OnSocketDisconnected(sender As PacketSocket, expected As Boolean, reason As String)
            inQueue.QueueAction(Sub() Disconnect(expected, reason))
        End Sub

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return QueueDisconnect(expected:=True, reason:="Disposed")
        End Function
        Private Sub ChangeState(newState As ClientState)
            Contract.Ensures(Me._state = newState)
            Dim oldState = _state
            _state = newState
            outQueue.QueueAction(Sub() RaiseEvent StateChanged(Me, oldState, newState))
        End Sub

        Private Async Sub BeginHandlingPackets()
            Contract.Assume(Me._state >= ClientState.TCPConnectionEstablished)
            Try
                Do
                    Dim data = Await _socket.AsyncReadPacket
                    Await _packetHandlerLogger.HandlePacket(data)
                Loop
            Catch ex As Exception
                QueueDisconnect(expected:=False, reason:="Error receiving packet: {0}".Frmt(ex.Summarize))
            End Try
        End Sub
        Private Async Function AwaitReceive(Of T)(packet As Protocol.Packets.Definition(Of T),
                                                  Optional ct As CancellationToken = Nothing) As Task(Of T)
            Contract.Assume(packet IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of Task(Of T))() IsNot Nothing)
            Dim r = New TaskCompletionSource(Of T)()
            Using d1 = IncludeQueuedPacketHandler(packet, Sub(pickle) r.TrySetResult(pickle.Value)),
                  d2 = ct.Register(Sub() r.TrySetCanceled())
                Return Await r.Task
            End Using
        End Function

        Public Async Function QueueConnectWith(socket As PacketSocket, clientCDKeySalt As UInt32, Optional reconnector As IConnecter = Nothing) As Task
            Contract.Assume(socket IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)

            Await inQueue.AwaitableEntrance()

            If Me._state <> ClientState.Disconnected AndAlso Me._state <> ClientState.TCPConnectionEstablished Then
                Throw New InvalidOperationException("Must disconnect before connecting again.")
            End If
            _reconnecter = reconnector

            Try
                Me._socket = socket
                Me._socket.Name = "BNET"
                ChangeState(ClientState.AuthenticatingProgram)

                'Reset the class future for the connection outcome
                _connectCanceller.Cancel()
                _connectCanceller = New CancellationTokenSource()
                Dim ct = _connectCanceller.Token

                'Introductions
                socket.SubStream.Write({1}, 0, 1) 'protocol version
                SendPacket(Protocol.MakeAuthenticationBegin(_productInfoProvider.MajorVersion, New Net.IPAddress(GetCachedIPAddressBytes(external:=False))))

                BeginHandlingPackets()

                Dim authBeginVals = Await AwaitReceive(Protocol.Packets.ServerToClient.ProgramAuthenticationBegin, ct)
                If ct.IsCancellationRequested Then Throw New TaskCanceledException()
                If authBeginVals.ItemAs(Of Protocol.ProgramAuthenticationBeginLogOnType)("logon type") <> Protocol.ProgramAuthenticationBeginLogOnType.Warcraft3 Then
                    Throw New IO.InvalidDataException("Unrecognized logon type from server.")
                End If
                Dim serverCdKeySalt = authBeginVals.ItemAs(Of UInt32)("server cd key salt")
                Dim revisionCheckSeed = authBeginVals.ItemAs(Of String)("revision check seed")
                Dim revisionCheckInstructions = authBeginVals.ItemAs(Of String)("revision check challenge")

                Dim keys = Await _productAuthenticator.AsyncAuthenticate(clientCDKeySalt.Bytes, serverCdKeySalt.Bytes)
                If ct.IsCancellationRequested Then Throw New TaskCanceledException()

                'revision check
                If revisionCheckInstructions = "" Then
                    Throw New IO.InvalidDataException("Received an invalid revision check challenge from bnet. Try connecting again.")
                End If
                Dim revisionCheckResponse = _productInfoProvider.GenerateRevisionCheck(My.Settings.war3path, revisionCheckSeed, revisionCheckInstructions)
                SendPacket(Protocol.MakeAuthenticationFinish(
                    version:=_productInfoProvider.ExeVersion,
                    revisionCheckResponse:=revisionCheckResponse,
                    clientCDKeySalt:=clientCDKeySalt,
                    cdKeyOwner:=My.Settings.cdKeyOwner,
                    exeInformation:="war3.exe {0} {1}".Frmt(_productInfoProvider.LastModifiedTime.ToString("MM/dd/yy hh:mm:ss", CultureInfo.InvariantCulture),
                                                            _productInfoProvider.FileSize),
                    productAuthentication:=keys))

                BeginConnectToBNLSServer(keys)

                Dim authFinishVals = Await AwaitReceive(Protocol.Packets.ServerToClient.ProgramAuthenticationFinish, ct)
                If ct.IsCancellationRequested Then Throw New TaskCanceledException()
                Dim result = authFinishVals.ItemAs(Of Protocol.ProgramAuthenticationFinishResult)("result")
                If result <> Protocol.ProgramAuthenticationFinishResult.Passed Then
                    Throw New IO.InvalidDataException("Program authentication failed with error: {0} {1}.".Frmt(result, authFinishVals.ItemAs(Of String)("info")))
                End If
                ChangeState(ClientState.EnterUserCredentials)
            Catch ex As Exception
                QueueDisconnect(expected:=False, reason:="Failed to complete connection: {0}.".Frmt(ex.Summarize))
                Throw
            End Try
        End Function
        Private Sub BeginConnectToBNLSServer(keys As ProductCredentialPair)
            'Parse address setting
            Dim remoteHost = ""
            Dim remotePort = 0US
            Dim remoteEndPointArg = If(My.Settings.bnls, "").ToInvariant
            If remoteEndPointArg <> "" Then
                Dim hostPortPair = remoteEndPointArg.ToString.Split(":"c)
                remoteHost = hostPortPair(0)
                If hostPortPair.Length <> 2 OrElse Not UShort.TryParse(hostPortPair(1), remotePort) Then
                    Logger.Log("Invalid bnls server format specified. Expected hostname:port.", LogMessageType.Problem)
                End If
            End If

            'Attempt BNLS connection
            Dim seed = keys.AuthenticationROC.AuthenticationProof.TakeExact(4).ToUInt32
            If remoteHost = "" Then
                _wardenClient = Warden.Client.MakeMock(_logger)
            Else
                _wardenClient = Warden.Client.MakeConnect(remoteHost:=remoteHost,
                                                          remotePort:=remotePort,
                                                          seed:=seed,
                                                          cookie:=seed,
                                                          logger:=Logger,
                                                          clock:=_clock)
            End If
        End Sub

        Public Async Function QueueLogOn(credentials As ClientAuthenticator) As Task
            Contract.Assume(credentials IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Await inQueue.AwaitableEntrance()
            If _state <> ClientState.EnterUserCredentials Then
                Throw New InvalidOperationException("Incorrect state for login.")
            End If

            _connectCanceller.Cancel()
            _connectCanceller = New CancellationTokenSource()
            Dim ct = _connectCanceller.Token

            Me._userCredentials = credentials
            ChangeState(ClientState.AuthenticatingUser)
            SendPacket(Protocol.MakeAccountLogOnBegin(credentials))
            Logger.Log("Initiating logon with username {0}.".Frmt(credentials.UserName), LogMessageType.Typical)

            'Begin authentication
            Dim authBeginVals = Await AwaitReceive(Protocol.Packets.ServerToClient.UserAuthenticationBegin, ct)
            If ct.IsCancellationRequested Then Throw New TaskCanceledException()
            Dim authBeginResult = authBeginVals.ItemAs(Of Protocol.UserAuthenticationBeginResult)("result")
            Dim accountPasswordSalt = authBeginVals.ItemAs(Of IRist(Of Byte))("account password salt")
            Dim serverPublicKey = authBeginVals.ItemAs(Of IRist(Of Byte))("server public key")

            If authBeginResult <> Protocol.UserAuthenticationBeginResult.Passed Then Throw New IO.InvalidDataException("User authentication failed with error: {0}".Frmt(authBeginResult))
            If Me._userCredentials Is Nothing Then Throw New InvalidStateException("Received AccountLogOnBegin before credentials specified.")
            Dim clientProof = Me._userCredentials.ClientPasswordProof(accountPasswordSalt, serverPublicKey)
            Dim expectedServerProof = Me._userCredentials.ServerPasswordProof(accountPasswordSalt, serverPublicKey)

            SendPacket(Protocol.MakeAccountLogOnFinish(clientProof))

            'Finish authentication
            Dim authFinishVals = Await AwaitReceive(Protocol.Packets.ServerToClient.UserAuthenticationFinish, ct)
            If ct.IsCancellationRequested Then Throw New TaskCanceledException()
            Dim result = authFinishVals.ItemAs(Of Protocol.UserAuthenticationFinishResult)("result")
            Dim serverProof = authFinishVals.ItemAs(Of IRist(Of Byte))("server password proof")

            'validate
            If result <> Protocol.UserAuthenticationFinishResult.Passed Then
                Dim errorInfo = ""
                Select Case result
                    Case Protocol.UserAuthenticationFinishResult.IncorrectPassword
                        errorInfo = "(Note: This can happen due to a bnet bug. You might want to try again.)"
                    Case Protocol.UserAuthenticationFinishResult.CustomError
                        errorInfo = "({0})".Frmt(authFinishVals.ItemAs(Of Maybe(Of String))("custom error info").Value)
                End Select
                Throw New IO.InvalidDataException("User authentication failed with error: {0} {1}".Frmt(result, errorInfo))
            ElseIf Not expectedServerProof.SequenceEqual(serverProof) Then
                Throw New IO.InvalidDataException("The server's password proof was incorrect.")
            End If

            ChangeState(ClientState.WaitingForEnterChat)
            _allowRetryConnect = True
            Logger.Log("Logged on with username {0}.".Frmt(Me._userCredentials.UserName), LogMessageType.Typical)

            'Game port
            SetReportedListenPort(6112)

            'Enter chat
            SendPacket(Protocol.MakeEnterChat())
            Await AwaitReceive(Protocol.Packets.ServerToClient.EnterChat, ct)
            If ct.IsCancellationRequested Then Throw New TaskCanceledException()
            Logger.Log("Entered chat", LogMessageType.Typical)

            EnterChannel(Profile.initialChannel)
        End Function

        Private Async Sub Disconnect(expected As Boolean, reason As String)
            Contract.Assume(reason IsNot Nothing)
            If _socket IsNot Nothing Then
                _socket.QueueDisconnect(expected, reason)
                RemoveHandler _socket.Disconnected, AddressOf OnSocketDisconnected
                _socket = Nothing
            ElseIf _state = ClientState.Disconnected Then
                Return
            End If

            'Finalize class futures
            _connectCanceller.Cancel()
            _gameCanceller.Cancel()
            _curAdvertisement = Nothing

            ChangeState(ClientState.Disconnected)
            Logger.Log("Disconnected ({0})".Frmt(reason), LogMessageType.Negative)
            If _wardenClient IsNot Nothing Then
                _wardenClient.Dispose()
                _wardenClient = Nothing
            End If

            If Not expected AndAlso _allowRetryConnect AndAlso _reconnecter IsNot Nothing Then
                _allowRetryConnect = False
                Await _clock.AsyncWait(5.Seconds)
                Logger.Log("Attempting to reconnect...", LogMessageType.Positive)
                Try
                    Dim socket = Await _reconnecter.ConnectAsync(Logger)
                    Await QueueConnectWith(socket, GenerateSecureBytesNewRNG(4).ToUInt32(), _reconnecter)
                    Await QueueLogOn(_userCredentials.WithNewGeneratedKeys())
                Catch ex As Exception
                    ex.RaiseAsUnexpected("Reconnect failed")
                    Logger.Log("Reconnect attempt failed: {0}".Frmt(ex.Message), LogMessageType.Problem)
                End Try
            End If
        End Sub
        Public Function QueueDisconnect(expected As Boolean, reason As String) As Task
            Contract.Requires(reason IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() Disconnect(expected, reason))
        End Function

        Private Sub EnterChannel(channel As String)
            SendPacket(Protocol.MakeJoinChannel(Protocol.JoinChannelType.ForcedJoin, channel))
            ChangeState(ClientState.Channel)
            TryStartAdvertising()
        End Sub

        Private Sub CheckStopAdvertising()
            If _curAdvertisement Is Nothing Then Return
            If _advertisementList.Contains(_curAdvertisement) Then Return

            SendPacket(Protocol.MakeCloseGame3())
            _curAdvertisement = Nothing
            _gameCanceller.Cancel()
            EnterChannel(_lastChannel)
        End Sub
        Private Async Sub TryStartAdvertising()
            If _curAdvertisement IsNot Nothing Then Return
            If _advertisementList.None() Then Return

            _gameCanceller.Cancel()
            _gameCanceller = New CancellationTokenSource()
            Dim ct = _gameCanceller.Token
            _curAdvertisement = _advertisementList.First()

            'Create game
            ChangeState(ClientState.CreatingGame)
            SetReportedListenPort(_curAdvertisement.BaseGameDescription.Port)
            Do
                SendPacket(Protocol.MakeCreateGame3(_curAdvertisement.GetCurrentCandidateGameDescriptionWithFixedAge()))
                Dim createSucceeded = 0 = Await AwaitReceive(Protocol.Packets.ServerToClient.CreateGame3, ct)
                If ct.IsCancellationRequested Then Return
                If createSucceeded Then Exit Do

                'Failed to create, try again with a different name
                _curAdvertisement.IncreaseNameFailCount()
            Loop
            _curAdvertisement.TryMarkAsSucceeded()
            outQueue.QueueAction(Sub() RaiseEvent AdvertisedGame(Me, _curAdvertisement.GetCurrentCandidateGameDescriptionWithFixedAge(), _curAdvertisement.IsPrivate, refreshed:=False))

            'Refresh game periodically
            ChangeState(ClientState.AdvertisingGame)
            If _curAdvertisement.IsPrivate Then Return
            Do
                Await _clock.AsyncWait(RefreshPeriod)
                If ct.IsCancellationRequested Then Exit Do

                SendPacket(Protocol.MakeCreateGame3(_curAdvertisement.GetCurrentCandidateGameDescriptionWithFixedAge()))
                Dim refreshSucceeded = 0 = Await AwaitReceive(Protocol.Packets.ServerToClient.CreateGame3)
                If ct.IsCancellationRequested Then Exit Do

                If Not refreshSucceeded Then
                    'No idea why a refresh would fail, better return to channel
                    _gameCanceller.Cancel()
                    _curAdvertisement = Nothing
                    EnterChannel(_lastChannel)
                    Exit Do
                End If
                outQueue.QueueAction(Sub() RaiseEvent AdvertisedGame(Me, _curAdvertisement.GetCurrentCandidateGameDescriptionWithFixedAge(), _curAdvertisement.IsPrivate, refreshed:=True))
            Loop
        End Sub

        Public Async Function QueueIncludeAdvertisableGame(gameDescription As WC3.LocalGameDescription,
                                                           isPrivate As Boolean) As Task(Of WC3.LocalGameDescription)
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of WC3.LocalGameDescription))() IsNot Nothing)
            Await inQueue.AwaitableEntrance()
            Dim entry = (From e In _advertisementList
                         Where e.BaseGameDescription.Equals(gameDescription)
                         ).SingleOrDefault
            If entry Is Nothing Then
                entry = New AdvertisementEntry(gameDescription, isPrivate)
                _advertisementList.Add(entry)
                TryStartAdvertising()
            End If
            Return Await entry.EventualAdvertisedDescription
        End Function

        Public Async Function QueueRemoveAdvertisableGame(gameDescription As WC3.LocalGameDescription) As Task(Of Boolean)
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Await inQueue.AwaitableEntrance()
            Dim entry = (From e In _advertisementList.ToList
                         Where e.BaseGameDescription.Equals(gameDescription)
                         ).SingleOrDefault
            If entry Is Nothing Then Return False
            entry.TryMarkAsFailed()
            _advertisementList.Remove(entry)
            CheckStopAdvertising()
            Return True
        End Function

        Public Async Function QueueRemoveAllAdvertisableGames() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Await inQueue.AwaitableEntrance()
            _advertisementList.Clear()
            CheckStopAdvertising()
        End Function

        Private Sub SendPacket(packet As Protocol.Packet)
            Contract.Requires(Me._state >= ClientState.TCPConnectionEstablished)
            Contract.Requires(packet IsNot Nothing)

            Try
                Logger.Log(Function() "Sending {0} to {1}".Frmt(packet.Id, _socket.Name), LogMessageType.DataEvent)
                Logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(packet.Id, _socket.Name, packet.Payload.Description), LogMessageType.DataParsed)
                If _socket Is Nothing Then
                    Logger.Log("Disconnected but tried to send {0}.".Frmt(packet.Id), LogMessageType.Problem)
                    Return
                End If
                _socket.WritePacket({Protocol.Packets.PacketPrefixValue, packet.Id}, packet.Payload.Data)
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse
                                       TypeOf ex Is InvalidOperationException OrElse
                                       TypeOf ex Is ObjectDisposedException OrElse
                                       TypeOf ex Is Net.Sockets.SocketException
                Disconnect(expected:=False, reason:="Error sending {0} to {1}: {2}".Frmt(packet.Id, _socket.Name, ex.Summarize))
                ex.RaiseAsUnexpected("Error sending {0} to {1}".Frmt(packet.Id, _socket.Name))
            End Try
        End Sub
        Public Function QueueSendPacket(packet As Protocol.Packet) As Task
            Contract.Requires(packet IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() SendPacket(packet))
        End Function

        Private Sub ReceiveWarden(pickle As IPickle(Of IRist(Of Byte)))
            Contract.Requires(pickle IsNot Nothing)
            If _state < ClientState.WaitingForEnterChat Then Throw New IO.InvalidDataException("Warden packet in unexpected place.")
            Dim encryptedData = pickle.Value
            _wardenClient.QueueSendWardenData(encryptedData).ConsiderExceptionsHandled()
        End Sub
        Private Sub OnWardenReceivedResponseData(sender As Warden.Client, data As IRist(Of Byte)) Handles _wardenClient.ReceivedWardenData
            Contract.Requires(data IsNot Nothing)
            inQueue.QueueAction(Sub() SendPacket(Protocol.MakeWarden(data)))
        End Sub
        Private Sub OnWardenFail(sender As Warden.Client, exception As Exception) Handles _wardenClient.Failed
            Contract.Requires(sender IsNot Nothing)
            Contract.Requires(exception IsNot Nothing)
            Call Async Sub()
                     Await sender.Activated
                     QueueDisconnect(expected:=False, reason:="Warden/BNLS Error: {0}.".Frmt(exception.Summarize))
                 End Sub
            If sender.Activated.Status <> TaskStatus.RanToCompletion AndAlso sender.Activated.Status <> TaskStatus.Faulted Then
                Logger.Log("Lost connection to BNLS server: {0}".Frmt(exception.Summarize), LogMessageType.Problem)
            End If
            exception.RaiseAsUnexpected("Warden/BNLS Error")
        End Sub
    End Class
End Namespace
