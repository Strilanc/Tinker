Imports System.Net.Sockets

Namespace Lan
    Public NotInheritable Class Advertiser
        Inherits FutureDisposable
        Public Shared ReadOnly LanAdvertiserTypeName As InvariantString = "LanAdvertiser"
        Public Shared ReadOnly LanTargetPort As UShort = 6112

        Private ReadOnly _games As New Dictionary(Of UInt32, LanGame)
        Private ReadOnly _socket As New UdpClient
        Private ReadOnly _logger As Logger
        Private ReadOnly _defaultTargetHost As String

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue()
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue()

        Private createCount As UInteger
        Private ReadOnly refreshTimer As New System.Timers.Timer(3000)

        Public Event AddedGame(ByVal sender As Lan.Advertiser, ByVal game As LanGame)
        Public Event RemovedGame(ByVal sender As Lan.Advertiser, ByVal game As LanGame)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_games IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_defaultTargetHost IsNot Nothing)
            Contract.Invariant(createCount >= 0)
            Contract.Invariant(refreshTimer IsNot Nothing)
        End Sub

        Public NotInheritable Class LanGame
            Private ReadOnly _gameDescription As WC3.LocalGameDescription
            Private ReadOnly _targetHosts As List(Of String)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_gameDescription IsNot Nothing)
                Contract.Invariant(_targetHosts IsNot Nothing)
            End Sub

            Public Sub New(ByVal gameDescription As WC3.LocalGameDescription,
                           ByVal targetHosts As IEnumerable(Of String))
                Contract.Requires(gameDescription IsNot Nothing)
                Contract.Requires(targetHosts IsNot Nothing)
                Me._gameDescription = gameDescription
                Me._targetHosts = targetHosts.ToList
            End Sub

            Public ReadOnly Property GameDescription As WC3.LocalGameDescription
                Get
                    Contract.Ensures(Contract.Result(Of WC3.LocalGameDescription)() IsNot Nothing)
                    Return _gameDescription
                End Get
            End Property
            Public ReadOnly Property TargetHosts As IEnumerable(Of String)
                Get
                    Contract.Ensures(Contract.Result(Of IEnumerable(Of String))() IsNot Nothing)
                    Return _targetHosts
                End Get
            End Property
        End Class

        Public Sub New(Optional ByVal defaultTargetHost As String = "localhost",
                       Optional ByVal logger As Logger = Nothing)
            Contract.Assume(defaultTargetHost IsNot Nothing)

            Me._logger = If(logger, New Logger)
            Me._defaultTargetHost = defaultTargetHost

            Me.refreshTimer.Start()
            AddHandler refreshTimer.Elapsed, Sub() inQueue.QueueAction(Sub() RefreshAll())
        End Sub

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As ifuture
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(
                Sub()
                    refreshTimer.Stop()
                    refreshTimer.Dispose()
                    ClearGames()
                    _socket.Close()
                End Sub)
        End Function

        Private Sub AddGame(ByVal game As WC3.LocalGameDescription,
                            Optional ByVal targets As IEnumerable(Of String) = Nothing)
            Contract.Requires(game IsNot Nothing)
            If _games.ContainsKey(game.GameId) Then
                If _games(game.GameId).GameDescription Is game Then Return
                Throw New InvalidOperationException("There is already a game being advertised with id = {0}.".Frmt(game.GameId))
            End If

            Dim lanGame = New LanGame(game, If(targets, {_defaultTargetHost}))
            _games(game.GameId) = lanGame
            RefreshGame(lanGame)

            _logger.Log("Added game {0}".Frmt(game.Name), LogMessageType.Positive)
            outQueue.QueueAction(Sub() RaiseEvent AddedGame(Me, lanGame))
        End Sub
        Private Function RemoveGame(ByVal id As UInt32) As Boolean
            If Not _games.ContainsKey(id) Then Return False
            Dim game = _games(id)
            Contract.Assume(game IsNot Nothing)

            'Advertise game closed
            Dim pk = WC3.Packet.MakeLanDestroyGame(game.GameDescription.GameId)
            For Each host In game.TargetHosts
                SendPacket(pk, host, LanTargetPort)
            Next host

            _games.Remove(id)
            _logger.Log("Removed game {0}".Frmt(game.GameDescription.Name), LogMessageType.Negative)
            outQueue.QueueAction(Sub() RaiseEvent RemovedGame(Me, game))
            Return True
        End Function
        Private Sub ClearGames()
            For Each id In _games.Keys.ToArray
                RemoveGame(id)
            Next id
        End Sub

        Private Sub RefreshAll()
            For Each game In _games.Values
                Contract.Assume(game IsNot Nothing)
                RefreshGame(game)
            Next game
        End Sub
        Private Sub RefreshGame(ByVal game As LanGame)
            Contract.Requires(game IsNot Nothing)
            Dim pk = WC3.Packet.MakeLanDescribeGame(New CachedExternalValues().WC3MajorVersion, game.GameDescription)
            For Each host In game.TargetHosts
                SendPacket(pk, host, LanTargetPort)
            Next host
        End Sub
        Private Sub SendPacket(ByVal pk As WC3.Packet, ByVal targetHost As String, ByVal targetPort As UShort)
            Contract.Requires(pk IsNot Nothing)
            Try
                'pack
                Dim data = pk.Payload.Data.ToArray()
                data = Concat({WC3.Packet.PacketPrefixValue, pk.id}, CUShort(data.Length + 4).Bytes(), data)

                'Log
                _logger.Log(Function() "Sending {0} to {1}".Frmt(pk.id, targetHost), LogMessageType.DataEvent)
                _logger.Log(pk.Payload.Description, LogMessageType.DataParsed)
                _logger.Log(Function() "{0}: {1}".Frmt(pk.id, data.ToHexString), LogMessageType.DataRaw)

                'Send
                _socket.Send(data, data.Length, targetHost, targetPort)

            Catch e As Pickling.PicklingException
                'Ignore
                _logger.Log("Error packing {0}: {1} (skipped)".Frmt(pk.id, e), LogMessageType.Negative)
            Catch e As Exception
                'Fail
                _logger.Log("Error sending {0}: {1}".Frmt(pk.id, e), LogMessageType.Problem)
                e.RaiseAsUnexpected("Exception rose past {0}.send".Frmt(Me.GetType.Name))
            End Try
        End Sub

        Public ReadOnly Property Logger() As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property
        Private ReadOnly Property Type() As InvariantString
            Get
                Return LanAdvertiserTypeName
            End Get
        End Property

        Public Function QueueAddGame(ByVal gameDescription As WC3.LocalGameDescription,
                                     Optional ByVal targetHosts As IEnumerable(Of String) = Nothing) As IFuture
            Contract.Requires(gameDescription IsNot Nothing)
            Return inQueue.QueueAction(Sub() AddGame(gameDescription, targetHosts))
        End Function
        Public Function QueueRemoveGame(ByVal id As UInt32) As IFuture(Of Boolean)
            Return inQueue.QueueFunc(Function() RemoveGame(id))
        End Function
        Public Function QueueClearGames() As IFuture
            Return inQueue.QueueAction(AddressOf ClearGames)
        End Function

        Public Shared Function CreateLanAdmin(ByVal name As InvariantString,
                                              ByVal password As String,
                                              Optional ByVal remoteHost As String = "localhost",
                                              Optional ByVal listenPort As UShort = 0) As Lan.Advertiser
            Dim sha1Checksum = (From b In Enumerable.Range(0, 20) Select CByte(b)).ToArray.AsReadableList
            Contract.Assume(sha1Checksum.Count = 20)
            Dim map = New WC3.Map("Maps\",
                                  "Maps\AdminGame.w3x",
                                  filesize:=1,
                                  fileChecksumCRC32:=&H12345678UI,
                                  mapChecksumSHA1:=sha1Checksum,
                                  mapChecksumXORO:=&H2357BDUI,
                                  slotCount:=2)
            Contract.Assume(map.Slots(1) IsNot Nothing)
            map.Slots(1).Contents = New WC3.SlotContentsComputer(map.Slots(1), WC3.Slot.ComputerLevel.Normal)
            Dim hostName = Application.ProductName
            Contract.Assume(hostName IsNot Nothing)
            Dim header = New WC3.LocalGameDescription("Admin Game",
                                          New WC3.GameStats(map, hostName, New Commands.CommandArgument("")),
                                          gameid:=1,
                                          entryKey:=0,
                                          totalSlotCount:=map.NumPlayerSlots,
                                          gameType:=map.GameType,
                                          state:=0,
                                          usedSlotCount:=0,
                                          hostPort:=0)
            Throw New NotImplementedException
            'Dim settings = New WC3.ServerSettings(map:=map,
            'header:=header,
            'allowUpload:=False,
            'defaultSlotLockState:=WC3.Slot.Lock.Frozen,
            'instances:=0,
            'password:=password,
            'isAdminGame:=True,
            'argument:=New Commands.CommandArgument("-permanent"))
            'Dim server = CreateServer(name, settings)
            'Dim lan As WC3.LanAdvertiser
            'lan = New WC3.LanAdvertiser(Me, name, remoteHost)
            'Try
            'AddWidget(lan)
            'lan.QueueAddGame(header)
            'Catch e As Exception
            'lan.Dispose()
            'Throw
            'End Try

            'Dim result = server.QueueOpenPort(listenPort)
            'result.CallWhenReady(
            'Sub(exception)
            'Contract.Assume(lan IsNot Nothing)
            'Contract.Assume(server IsNot Nothing)
            'If exception IsNot Nothing Then
            'server.QueueKill()
            'lan.Dispose()
            'Else
            'DisposeLink.CreateOneWayLink(lan, server)
            'DisposeLink.CreateOneWayLink(server, lan)
            'End If
            'End Sub
            ')
            'Return result
        End Function

        Private Function WeaveGames(ByVal adder As AddedGameEventHandler,
                                    ByVal remover As RemovedGameEventHandler) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)

            'Report current games
            For Each game In _games.Values
                Dim game_ = game
                outQueue.QueueAction(Sub() adder(Me, game_))
            Next game

            'Report future games
            AddHandler AddedGame, adder
            AddHandler RemovedGame, remover
            Return New DelegatedDisposable(
                Sub()
                    RemoveHandler AddedGame, adder
                    RemoveHandler RemovedGame, remover
                End Sub)
        End Function
        Public Function QueueWeaveGames(ByVal adder As AddedGameEventHandler,
                                        ByVal remover As RemovedGameEventHandler) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() WeaveGames(adder, remover))
        End Function
    End Class
End Namespace
