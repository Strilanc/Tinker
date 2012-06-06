Imports Tinker.Pickling

Namespace Bnet
    Public Enum ClientState As Integer
        Disconnected
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

        Private Shared ReadOnly WC3_DEFAULT_LISTEN_PORT As UShort = 6112
        Private Shared ReadOnly RefreshPeriod As TimeSpan = 20.Seconds

        Private ReadOnly outQueue As CallQueue
        Private ReadOnly inQueue As CallQueue

        Private ReadOnly _productInfoProvider As IProductInfoProvider
        Private ReadOnly _clock As IClock
        Private ReadOnly _profile As Bot.ClientProfile
        Private ReadOnly _productAuthenticator As IProductAuthenticator
        Private ReadOnly _logger As Logger
        Private ReadOnly _manualPacketHandler As New PacketPusher(Of Protocol.PacketId)()
        Private _socket As PacketSocket
        Private _connectionToken As New CancellationTokenSource()

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
        Private _reportedListenPort As UShort?

        'connection
        Private _reconnecter As IConnecter
        Private _userCredentials As ClientCredentials
        Private _allowRetryConnect As Boolean

        Public Event StateChanged(sender As Client, oldState As ClientState, newState As ClientState)
        Public Event AdvertisedGame(sender As Client, gameDescription As WC3.LocalGameDescription, [private] As Boolean, refreshed As Boolean)

        Private _lastChannel As InvariantString
        Private _state As ClientState

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_advertisementList IsNot Nothing)
            Contract.Invariant(_manualPacketHandler IsNot Nothing)
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_profile IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_productAuthenticator IsNot Nothing)
            Contract.Invariant(_productInfoProvider IsNot Nothing)
            Contract.Invariant((_socket IsNot Nothing) = (_state > ClientState.Disconnected))
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
        End Sub

        Public Sub Init()
            IncludePacketHandlerAsync(Protocol.Packets.ServerToClient.Ping,
                                      Function(salt) TrySendPacketAsync(Protocol.MakePing(salt)))
            IncludePacketHandlerAsync(Protocol.Packets.ServerToClient.ChatEvent,
                                      Async Function(vals)
                                          Dim eventId = vals.ItemAs(Of Protocol.ChatEventId)("event id")
                                          Dim text = vals.ItemAs(Of String)("text")
                                          If eventId = Protocol.ChatEventId.Channel Then
                                              Await inQueue
                                              _lastChannel = text
                                          End If
                                      End Function)
            IncludePacketHandlerAsync(Protocol.Packets.ServerToClient.MessageBox,
                                      Function(vals)
                                          Dim msg = "MESSAGE BOX FROM BNET: {0}: {1}".Frmt(vals.ItemAs(Of String)("caption"), vals.ItemAs(Of String)("text"))
                                          Logger.Log(msg, LogMessageType.Problem)
                                          Return CompletedTask()
                                      End Function)

            'Packets which may be safely ignored should be logged instead of killing the client
            IncludeLogger(Protocol.Packets.ServerToClient.Null)
            IncludeLogger(Protocol.Packets.ServerToClient.GetFileTime)
            IncludeLogger(Protocol.Packets.ServerToClient.GetIconData)
            IncludeLogger(Protocol.Packets.ServerToClient.QueryGamesList(_clock))
            IncludeLogger(Protocol.Packets.ServerToClient.EnterChat)
            IncludeLogger(Protocol.Packets.ServerToClient.FriendsUpdate)
            IncludeLogger(Protocol.Packets.ServerToClient.RequiredWork)
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
        Public Async Function GetStateAsync() As Task(Of ClientState)
            await inQueue
            Return _state
        End Function

        Public Async Sub IncludePacketPickleHandlerAsync(Of T)(packetDefinition As Protocol.Packets.Definition(Of T),
                                                               handler As Func(Of IPickle(Of T), Task),
                                                               Optional ct As CancellationToken = Nothing)
            Contract.Assume(packetDefinition IsNot Nothing)
            Contract.Assume(handler IsNot Nothing)
            Await inQueue

            Using walker = _manualPacketHandler.CreateWalker()
                While Not ct.IsCancellationRequested
                    Dim walkToken = {ct, _connectionToken.Token}.AnyCancelledToken()
                    Try
                        While Not walkToken.IsCancellationRequested
                            Dim pickle = Await walker.WalkAsync(packetDefinition.Id, packetDefinition.Jar, walkToken)
                            Await handler(pickle)
                        End While
                    Catch ex As TaskCanceledException
                        'Connection cancelled. Handler may still be active.
                    End Try
                End While
            End Using
        End Sub
        Public Sub IncludePacketHandlerAsync(Of T)(packetDefinition As Protocol.Packets.Definition(Of T),
                                                   handler As Func(Of T, Task),
                                                   Optional ct As CancellationToken = Nothing)
            IncludePacketPickleHandlerAsync(packetDefinition, Function(e As IPickle(Of T)) handler(e.Value), ct)
        End Sub
        Private Sub IncludeLogger(Of T)(packetDefinition As Protocol.Packets.Definition(Of T), Optional ct As CancellationToken = Nothing)
            IncludePacketHandlerAsync(packetDefinition, Function() CompletedTask(), ct)
        End Sub

        Public Async Function SendTextAsync(text As NonNull(Of String)) As Task
            await inQueue

            Contract.Assume(text.Value.Length > 0)
            Dim isBnetCommand = text.Value.StartsWith("/", StringComparison.Ordinal)

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

            Contract.Assume(_state >= ClientState.Channel)
            For Each line In lines
                Contract.Assume(line IsNot Nothing)
                If line.Length = 0 Then Continue For
                Call Async Sub() Await TrySendPacketAsync(Protocol.MakeChatCommand(line))
            Next line
        End Function

        Public Async Function SendWhisperAsync(userName As InvariantString, text As NonNull(Of String)) As Task
            Contract.Assume(userName.Length > 0)
            Contract.Assume(text.Value.Length > 0)

            await inQueue
            Contract.Assume(_state >= ClientState.Channel)

            Dim prefix = "/w {0} ".Frmt(userName)
            Contract.Assume(prefix.Length >= 5)
            If prefix.Length >= Protocol.Packets.ClientToServer.MaxChatCommandTextLength \ 2 Then
                Throw New ArgumentOutOfRangeException("username", "Username is too long.")
            End If

            For Each line In SplitText(text, maxLineLength:=Protocol.Packets.ClientToServer.MaxChatCommandTextLength - prefix.Length)
                Contract.Assume(line IsNot Nothing)
                Call Async Sub() Await TrySendPacketAsync(Protocol.MakeChatCommand(prefix + line))
            Next line
        End Function

        Private Sub SetReportedListenPortPresync(port As UShort)
            If port = Me._reportedListenPort Then Return
            Me._reportedListenPort = port
            Call Async Sub() Await TrySendPacketAsync(Protocol.MakeNetGamePort(Me._reportedListenPort.Value))
        End Sub

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            _manualPacketHandler.Dispose()
            Return DisconnectAsync(expected:=True, reason:="Disposed")
        End Function
        Private Sub ChangeStatePresync(newState As ClientState)
            Contract.Requires(SynchronizationContext.Current Is inQueue)
            Contract.Ensures(Me._state = newState)
            Dim oldState = _state
            _state = newState
            outQueue.QueueAction(Sub() RaiseEvent StateChanged(Me, oldState, newState))
        End Sub

        Private Async Sub BeginHandlingPacketsAsync(ct As CancellationToken)
            Await inQueue
            If Me._state <= ClientState.Disconnected Then Return

            Try
                Do
                    Dim rawPacket = Await _socket.AsyncReadPacket()
                    If ct.IsCancellationRequested Then Return
                    Dim id = DirectCast(rawPacket(1), Bnet.Protocol.PacketId)
                    Dim body = rawPacket.SkipExact(4)
                    Dim description = New TaskCompletionSource(Of Func(Of String))()
                    Logger.Log(Function() "Received {0} from {1}".Frmt(id, "bnet"), LogMessageType.DataEvent)
                    Logger.FutureLog(Function() "Received {0} from {1}: Parsing...".Frmt(id, "bnet"), description.Task, LogMessageType.DataParsed)
                    Call Async Sub()
                             Try
                                 Dim parsed = Await _manualPacketHandler.Push(id, body)
                                 If parsed Is Nothing Then Throw New IO.IOException("Unhandled packet: {0}".Frmt(id))
                                 If parsed.Data.Count < body.Count Then Logger.Log("Data left over after parsing.", LogMessageType.Problem)
                                 Dim r = Function() "Received {0} from {1}: {2}".Frmt(id, "bnet", parsed.Description())
                                 description.SetResult(r)
                             Catch ex As Exception
                                 description.SetException(ex)
                             End Try
                         End Sub()
                Loop
            Catch ex As Exception
                Call Async Sub() Await DisconnectAsync(expected:=False, reason:="Error receiving packet: {0}".Frmt(ex.Summarize))
            End Try
        End Sub

        Public Async Function ConnectAsync(socket As NonNull(Of PacketSocket), clientCDKeySalt As UInt32, Optional reconnector As IConnecter = Nothing) As Task
            Await inQueue

            If Me._state <> ClientState.Disconnected Then
                Throw New InvalidOperationException("Must disconnect before connecting again.")
            End If
            _reconnecter = reconnector

            Try
                _connectionToken.Cancel()
                _connectionToken = New CancellationTokenSource()
                Dim ct = _connectionToken.Token

                Me._socket = socket
                Dim onSocketDisconnected = Async Sub(sender As PacketSocket, expected As Boolean, reason As String)
                                               Await DisconnectAsync(expected, reason)
                                           End Sub
                AddHandler _socket.Disconnected, onSocketDisconnected
                ct.Register(Sub() RemoveHandler _socket.Disconnected, onSocketDisconnected)
                Me._socket.Name = "BNET"
                ChangeStatePresync(ClientState.AuthenticatingProgram)

                'Reset the class future for the connection outcome
                Using walker = _manualPacketHandler.CreateWalker()
                    'Introductions
                    socket.Value.SubStream.Write({1}, 0, 1) 'protocol version

                    BeginHandlingPacketsAsync(ct)

                    Dim authBeginVals = Await TradePacketsAsync(
                        Protocol.MakeAuthenticationBegin(_productInfoProvider.MajorVersion, New Net.IPAddress(GetCachedIPAddressBytes(external:=False))),
                        Protocol.Packets.ServerToClient.ProgramAuthenticationBegin)
                    If authBeginVals.ItemAs(Of Protocol.ProgramAuthenticationBeginLogOnType)("logon type") <> Protocol.ProgramAuthenticationBeginLogOnType.Warcraft3 Then
                        Throw New IO.InvalidDataException("Unrecognized logon type from server.")
                    End If
                    Dim serverCdKeySalt = authBeginVals.ItemAs(Of UInt32)("server cd key salt")
                    Dim revisionCheckSeed = authBeginVals.ItemAs(Of String)("revision check seed")
                    Dim revisionCheckInstructions = authBeginVals.ItemAs(Of String)("revision check challenge")

                    Dim keys = Await _productAuthenticator.AsyncAuthenticate(clientCDKeySalt.Bytes, serverCdKeySalt.Bytes)
                    If ct.IsCancellationRequested Then Throw New TaskCanceledException()
                    BeginConnectToBNLSServerPresync(walker.Split(), keys, ct)

                    'revision check
                    If revisionCheckInstructions = "" Then
                        Throw New IO.InvalidDataException("Received an invalid revision check challenge from bnet. Try connecting again.")
                    End If
                    Dim revisionCheckResponse = _productInfoProvider.GenerateRevisionCheck(My.Settings.war3path, revisionCheckSeed, revisionCheckInstructions)

                    Dim authFinishVals = Await TradePacketsAsync(
                        Protocol.MakeAuthenticationFinish(
                            _productInfoProvider.ExeVersion,
                            revisionCheckResponse,
                            clientCDKeySalt,
                            My.Settings.cdKeyOwner,
                            _productInfoProvider.ExeInfo,
                            keys),
                        Protocol.Packets.ServerToClient.ProgramAuthenticationFinish)
                    Dim result = authFinishVals.ItemAs(Of Protocol.ProgramAuthenticationFinishResult)("result")
                    If result <> Protocol.ProgramAuthenticationFinishResult.Passed Then
                        Throw New IO.InvalidDataException("Program authentication failed with error: {0} {1}.".Frmt(result, authFinishVals.ItemAs(Of String)("info")))
                    End If
                    ChangeStatePresync(ClientState.EnterUserCredentials)
                End Using
            Catch ex As Exception
                Call Async Sub() Await DisconnectAsync(expected:=False, reason:="Failed to complete connection: {0}.".Frmt(ex.Summarize))
                Throw
            End Try
        End Function
        Private Async Sub BeginConnectToBNLSServerPresync(walker As PacketWalker(Of Protocol.PacketId), keys As NonNull(Of ProductCredentialPair), ct As CancellationToken)
            If ct.IsCancellationRequested Then Return

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

            'Connect
            If remoteHost = "" Then
                Try
                    Await walker.WalkValueAsync(Protocol.Packets.ServerToClient.Warden, ct)
                    Logger.Log("Warning: No BNLS server set, but received a Warden packet.", LogMessageType.Problem)
                Catch ex As TaskCanceledException
                    'ignore cancellation
                End Try
                walker.Dispose()
                Return
            End If
            Dim bnlsSocket As Warden.Socket
            Try
                Logger.Log("Connecting to bnls server at {0}:{1}...".Frmt(remoteHost, remotePort), LogMessageType.Positive)
                Dim seed = keys.Value.AuthenticationROC.AuthenticationProof.TakeExact(4).ToUInt32()
                bnlsSocket = Await Warden.Socket.ConnectToAsync(remoteHost, remotePort, seed, seed, Clock, Logger)
                Logger.Log("Connected to bnls server.", LogMessageType.Positive)
            Catch ex As Exception
                ex.RaiseAsUnexpected("Connecting to bnls server.")
                Logger.Log("Error connecting to bnls server at {0}:{1}: {2}".Frmt(remoteHost, remotePort, ex.Summarize), LogMessageType.Problem)
                walker.Dispose()
                Return
            End Try
            ct.Register(Sub() bnlsSocket.Dispose())

            'Asynchronously forward bnet warden to bnls
            Call Async Sub()
                     Do
                         Try
                             Dim wardenData = Await walker.WalkValueAsync(Protocol.Packets.ServerToClient.Warden, ct)
                             bnlsSocket.QueueSendWardenData(wardenData).ConsiderExceptionsHandled()
                         Catch ex As TaskCanceledException
                             Return
                         Finally
                             walker.Dispose()
                         End Try
                     Loop
                 End Sub

            'Forward bnls to bnet warden
            Try
                Await bnlsSocket.QueueRunAsync(ct, Async Sub(data) Await TrySendPacketAsync(Protocol.MakeWarden(data)))
            Catch ex As Exception
                ex.RaiseAsUnexpected("Warden/BNLS Error")
                Call Async Sub() Await DisconnectAsync(expected:=False, reason:="Warden/BNLS Error: {0}.".Frmt(ex.Summarize))
            End Try
        End Sub

        Public Async Function LogOnAsync(credentials As NonNull(Of ClientCredentials)) As Task
            await inQueue
            If _state <> ClientState.EnterUserCredentials Then
                Throw New InvalidOperationException("Incorrect state for login.")
            End If

            Me._userCredentials = credentials
            ChangeStatePresync(ClientState.AuthenticatingUser)
            Logger.Log("Initiating logon with username {0}.".Frmt(credentials.Value.UserName), LogMessageType.Typical)
            'Begin authentication
            Dim authBeginVals = Await TradePacketsAsync(
                Protocol.MakeUserAuthenticationBegin(credentials),
                Protocol.Packets.ServerToClient.UserAuthenticationBegin)
            Dim authBeginResult = authBeginVals.ItemAs(Of Protocol.UserAuthenticationBeginResult)("result")
            Dim accountPasswordSalt = authBeginVals.ItemAs(Of IRist(Of Byte))("account password salt")
            Dim serverPublicKey = authBeginVals.ItemAs(Of IRist(Of Byte))("server public key")

            If authBeginResult <> Protocol.UserAuthenticationBeginResult.Passed Then Throw New IO.InvalidDataException("User authentication failed with error: {0}".Frmt(authBeginResult))
            If Me._userCredentials Is Nothing Then Throw New InvalidStateException("Received AccountLogOnBegin before credentials specified.")
            Dim clientProof = Me._userCredentials.ClientPasswordProof(accountPasswordSalt, serverPublicKey)
            Dim expectedServerProof = Me._userCredentials.ServerPasswordProof(accountPasswordSalt, serverPublicKey)

            'Finish authentication
            Dim authFinishVals = Await TradePacketsAsync(
                Protocol.MakeUserAuthenticationFinish(clientProof),
                Protocol.Packets.ServerToClient.UserAuthenticationFinish)
            Dim result = authFinishVals.ItemAs(Of Protocol.UserAuthenticationFinishResult)("result")
            Dim serverProof = authFinishVals.ItemAs(Of IRist(Of Byte))("server password proof")

            'validate
            If result <> Protocol.UserAuthenticationFinishResult.Passed Then
                Dim errorInfo = ""
                Select Case result
                    Case Protocol.UserAuthenticationFinishResult.IncorrectPassword
                        errorInfo = "(Note: This can happen due to a bnet bug. You might want to try again.)"
                    Case Protocol.UserAuthenticationFinishResult.CustomError
                        errorInfo = "({0})".Frmt(authFinishVals.ItemAs(Of NullableValue(Of String))("custom error info").Value)
                End Select
                Throw New IO.InvalidDataException("User authentication failed with error: {0} {1}".Frmt(result, errorInfo))
            ElseIf Not expectedServerProof.SequenceEqual(serverProof) Then
                Throw New IO.InvalidDataException("The server's password proof was incorrect.")
            End If

            ChangeStatePresync(ClientState.WaitingForEnterChat)
            _allowRetryConnect = True
            Logger.Log("Logged on with username {0}.".Frmt(Me._userCredentials.UserName), LogMessageType.Typical)

            'Game port
            SetReportedListenPortPresync(WC3_DEFAULT_LISTEN_PORT)

            'Enter chat
            Await TradePacketsAsync(
                Protocol.MakeEnterChat(),
                Protocol.Packets.ServerToClient.EnterChat)
            Logger.Log("Entered chat", LogMessageType.Typical)

            EnterChannelPresync(Profile.initialChannel)
        End Function

        Public Async Function DisconnectAsync(expected As Boolean, reason As NonNull(Of String)) As Task
            await inQueue

            If _socket IsNot Nothing Then
                Call Async Sub() Await _socket.QueueDisconnect(expected, reason)
                _socket = Nothing
            ElseIf _state = ClientState.Disconnected Then
                Return
            End If

            _connectionToken.Cancel()
            _gameCanceller.Cancel()
            _curAdvertisement = Nothing
            _reportedListenPort = Nothing

            ChangeStatePresync(ClientState.Disconnected)
            Logger.Log("Disconnected ({0})".Frmt(reason), LogMessageType.Negative)
            If Not expected Then Call Async Sub() Await TryReconnectPresync()
        End Function
        Private Async Function TryReconnectPresync() As Task(Of Boolean)
            If _state <> ClientState.Disconnected Then Return False
            If Not _allowRetryConnect Then Return False
            _allowRetryConnect = False
            If _reconnecter Is Nothing Then Return False
            Await _clock.Delay(5.Seconds)
            Logger.Log("Attempting to reconnect...", LogMessageType.Positive)
            Try
                Dim socket = Await _reconnecter.ConnectAsync(Logger)
                Using rng = New System.Security.Cryptography.RNGCryptoServiceProvider()
                    Await ConnectAsync(socket, rng.GenerateBytes(4).ToUInt32(), _reconnecter)
                    Await LogOnAsync(_userCredentials.WithNewGeneratedKeys(rng))
                End Using
                Return True
            Catch ex As Exception
                ex.RaiseAsUnexpected("Reconnect failed")
                Logger.Log("Reconnect attempt failed: {0}".Frmt(ex.Message), LogMessageType.Problem)
                Return False
            End Try
        End Function

        Private Sub EnterChannelPresync(channel As InvariantString)
            Call Async Sub() Await TrySendPacketAsync(Protocol.MakeJoinChannel(Protocol.JoinChannelType.ForcedJoin, channel))
            ChangeStatePresync(ClientState.Channel)
            TryStartAdvertisingPresync()
        End Sub

        Private Sub CheckStopAdvertisingPresync()
            If _curAdvertisement Is Nothing Then Return
            If _advertisementList.Contains(_curAdvertisement) Then Return

            Call Async Sub() Await TrySendPacketAsync(Protocol.MakeCloseGame3())
            _curAdvertisement = Nothing
            _gameCanceller.Cancel()
            EnterChannelPresync(_lastChannel)
        End Sub
        Private Async Sub TryStartAdvertisingPresync()
            If _curAdvertisement IsNot Nothing Then Return
            If _advertisementList.None() Then Return

            _gameCanceller.Cancel()
            _gameCanceller = New CancellationTokenSource()
            Dim ct = _gameCanceller.Token
            _curAdvertisement = _advertisementList.First()

            'Create game
            ChangeStatePresync(ClientState.CreatingGame)
            SetReportedListenPortPresync(_curAdvertisement.BaseGameDescription.Port)
            Do
                Dim createResult = Await TradePacketsAsync(
                    Protocol.MakeCreateGame3(_curAdvertisement.GetCurrentCandidateGameDescriptionWithFixedAge()),
                    Protocol.Packets.ServerToClient.CreateGame3)
                If ct.IsCancellationRequested Then Return
                Dim createSucceeded = 0 = createResult
                If createSucceeded Then Exit Do

                'Failed to create, try again with a different name
                _curAdvertisement.IncreaseNameFailCount()
            Loop
            _curAdvertisement.TryMarkAsSucceeded()
            Call Async Sub() Await outQueue.QueueAction(Sub() RaiseEvent AdvertisedGame(Me, _curAdvertisement.GetCurrentCandidateGameDescriptionWithFixedAge(), _curAdvertisement.IsPrivate, refreshed:=False))

            'Refresh game periodically
            ChangeStatePresync(ClientState.AdvertisingGame)
            If _curAdvertisement.IsPrivate Then Return
            Do
                Await _clock.Delay(RefreshPeriod)
                If ct.IsCancellationRequested Then Exit Do

                Dim refreshResult = Await TradePacketsAsync(
                    Protocol.MakeCreateGame3(_curAdvertisement.GetCurrentCandidateGameDescriptionWithFixedAge()),
                    Protocol.Packets.ServerToClient.CreateGame3)
                If ct.IsCancellationRequested Then Return
                Dim refreshSucceeded = 0 = refreshResult

                If Not refreshSucceeded Then
                    'No idea why a refresh would fail, better return to channel
                    _gameCanceller.Cancel()
                    _curAdvertisement = Nothing
                    EnterChannelPresync(_lastChannel)
                    Exit Do
                End If
                Call Async Sub() Await outQueue.QueueAction(Sub() RaiseEvent AdvertisedGame(Me, _curAdvertisement.GetCurrentCandidateGameDescriptionWithFixedAge(), _curAdvertisement.IsPrivate, refreshed:=True))
            Loop
        End Sub

        Public Async Function IncludeAdvertisableGameAsync(gameDescription As NonNull(Of WC3.LocalGameDescription),
                                                           isPrivate As Boolean) As Task(Of WC3.LocalGameDescription)
            await inQueue
            Dim entry = _advertisementList.SingleOrDefault(Function(e) e.BaseGameDescription.Equals(gameDescription.Value))
            If entry Is Nothing Then
                entry = New AdvertisementEntry(gameDescription, isPrivate)
                _advertisementList.Add(entry)
                TryStartAdvertisingPresync()
            End If
            Return Await entry.EventualAdvertisedDescription
        End Function

        Public Async Function ExcludeAdvertisableGameAsync(gameDescription As NonNull(Of WC3.LocalGameDescription)) As Task(Of Boolean)
            await inQueue
            Dim entry = _advertisementList.SingleOrDefault(Function(e) e.BaseGameDescription.Equals(gameDescription.Value))
            If entry Is Nothing Then Return False
            entry.TryMarkAsFailed()
            _advertisementList.Remove(entry)
            CheckStopAdvertisingPresync()
            Return True
        End Function

        Public Async Function ClearAdvertisableGamesAsync() As Task
            await inQueue
            _advertisementList.Clear()
            CheckStopAdvertisingPresync()
        End Function

        Public Async Function TradePacketsAsync(Of T)(packet As Protocol.Packet,
                                                      expectedPacket As Protocol.Packets.Definition(Of T)) As Task(Of T)
            Using walker = _manualPacketHandler.CreateWalker()
                If Not Await TrySendPacketAsync(packet) Then Throw New TaskCanceledException("Failed to send packet.")
                Return Await walker.WalkValueAsync(expectedPacket, _connectionToken.Token)
            End Using
        End Function
        Public Async Function TrySendPacketAsync(packet As NonNull(Of Protocol.Packet)) As Task(Of Boolean)
            await inQueue

            Dim id = packet.Value.Id
            Dim payload = packet.Value.Payload
            If _socket Is Nothing Then
                Logger.Log("Disconnected but tried to send {0}.".Frmt(id), LogMessageType.Problem)
                Return False
            End If
            Try
                Logger.Log(Function() "Sending {0} to {1}".Frmt(id, _socket.Name), LogMessageType.DataEvent)
                Logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(id, _socket.Name, payload.Description), LogMessageType.DataParsed)
                _socket.WritePacket({Protocol.Packets.PacketPrefixValue, id}, payload.Data)
                Return True
            Catch ex As Exception
                Call Async Sub() Await DisconnectAsync(expected:=False, reason:="Error sending {0} to {1}: {2}".Frmt(id, _socket.Name, ex.Summarize))
                ex.RaiseAsUnexpected("Error sending {0} to {1}".Frmt(id, _socket.Name))
                Return False
            End Try
        End Function
    End Class
End Namespace
