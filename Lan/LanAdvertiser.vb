Imports System.Net.Sockets

Namespace Lan
    Public NotInheritable Class Advertiser
        Inherits FutureDisposable
        Public Shared ReadOnly LanTargetPort As UShort = 6112

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue()
        Private ReadOnly outQueue As ICallQueue = New TaskedCallQueue()

        Private ReadOnly _games As New Dictionary(Of UInt32, LanGame)
        Private ReadOnly _viewGames As New AsyncViewableCollection(Of LanGame)(outQueue:=outQueue)
        Private ReadOnly _socket As New UdpClient
        Private ReadOnly _logger As Logger
        Private ReadOnly _defaultTargetHost As String

        Private ReadOnly refreshTimer As New System.Timers.Timer(3000)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_games IsNot Nothing)
            Contract.Invariant(_viewGames IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_defaultTargetHost IsNot Nothing)
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

        Public ReadOnly Property Logger() As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property

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

        Private Sub AddGame(ByVal gameDescription As WC3.LocalGameDescription,
                            Optional ByVal targets As IEnumerable(Of String) = Nothing)
            Contract.Requires(gameDescription IsNot Nothing)
            If _games.ContainsKey(gameDescription.GameId) Then
                Dim gameSet = _games(gameDescription.GameId)
                Contract.Assume(gameSet IsNot Nothing)
                If gameSet.GameDescription Is gameDescription Then Return
                Throw New InvalidOperationException("There is already a game being advertised with id = {0}.".Frmt(gameDescription.GameId))
            End If

            Dim lanGame = New LanGame(gameDescription, If(targets, {_defaultTargetHost}))
            _games(gameDescription.GameId) = lanGame
            RefreshGame(lanGame)

            _logger.Log("Added game {0}".Frmt(gameDescription.Name), LogMessageType.Positive)
            _viewGames.Add(lanGame)
        End Sub
        Public Function QueueAddGame(ByVal gameDescription As WC3.LocalGameDescription,
                                     Optional ByVal targetHosts As IEnumerable(Of String) = Nothing) As IFuture
            Contract.Requires(gameDescription IsNot Nothing)
            Return inQueue.QueueAction(Sub() AddGame(gameDescription, targetHosts))
        End Function

        Private Function RemoveGame(ByVal id As UInt32) As Boolean
            If Not _games.ContainsKey(id) Then Return False
            Dim game = _games(id)
            Contract.Assume(game IsNot Nothing)

            'Advertise game closed
            Dim pk = WC3.Protocol.MakeLanDestroyGame(game.GameDescription.GameId)
            For Each host In game.TargetHosts
                SendPacket(pk, host, LanTargetPort)
            Next host

            _games.Remove(id)
            _logger.Log("Removed game {0}".Frmt(game.GameDescription.Name), LogMessageType.Negative)
            _viewGames.Remove(game)
            Return True
        End Function
        Public Function QueueRemoveGame(ByVal id As UInt32) As IFuture(Of Boolean)
            Return inQueue.QueueFunc(Function() RemoveGame(id))
        End Function

        Private Sub ClearGames()
            For Each id In _games.Keys.ToArray
                RemoveGame(id)
            Next id
        End Sub
        Public Function QueueClearGames() As IFuture
            Return inQueue.QueueAction(AddressOf ClearGames)
        End Function

        Private Sub RefreshAll()
            For Each game In _games.Values
                Contract.Assume(game IsNot Nothing)
                RefreshGame(game)
            Next game
        End Sub
        Private Sub RefreshGame(ByVal game As LanGame)
            Contract.Requires(game IsNot Nothing)
            Dim pk = WC3.Protocol.MakeLanGameDetails(New CachedExternalValues().WC3MajorVersion, game.GameDescription)
            For Each host In game.TargetHosts
                SendPacket(pk, host, LanTargetPort)
            Next host
        End Sub
        Private Sub SendPacket(ByVal pk As WC3.Protocol.Packet, ByVal targetHost As String, ByVal targetPort As UShort)
            Contract.Requires(pk IsNot Nothing)
            Try
                'pack
                Dim data = pk.Payload.Data.ToArray()
                data = Concat({WC3.Protocol.Packets.PacketPrefix, pk.id}, CUShort(data.Length + 4).Bytes(), data)

                'Log
                _logger.Log(Function() "Sending {0} to {1}".Frmt(pk.id, targetHost), LogMessageType.DataEvent)
                _logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(pk.id, targetHost, pk.Payload.Description.Value), LogMessageType.DataParsed)
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

        Private Function CreateGamesAsyncView(ByVal adder As Action(Of Lan.Advertiser, LanGame),
                                              ByVal remover As Action(Of Lan.Advertiser, LanGame)) As IDisposable
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IDisposable)() IsNot Nothing)
            Return _viewGames.BeginSync(adder:=Sub(sender, game) adder(Me, game),
                                        remover:=Sub(sender, game) remover(Me, game))
        End Function
        Public Function QueueCreateGamesAsyncView(ByVal adder As Action(Of Lan.Advertiser, LanGame),
                                                  ByVal remover As Action(Of Lan.Advertiser, LanGame)) As IFuture(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of IFuture(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() CreateGamesAsyncView(adder, remover))
        End Function
    End Class
End Namespace
