﻿Imports System.Net.Sockets
Imports Tinker.Pickling

Namespace Lan
    '''<summary>Advertises lan games via UDP. Warcraft 3 will display such games in the local area network games list.</summary>
    Public NotInheritable Class UDPAdvertiser
        Inherits DisposableWithTask
        Public Shared ReadOnly LanTargetPort As UShort = 6112
        Private Shared ReadOnly RefreshPeriod As TimeSpan = 3.Seconds

        Private ReadOnly inQueue As CallQueue = MakeTaskedCallQueue()
        Private ReadOnly outQueue As CallQueue = MakeTaskedCallQueue()

        Private ReadOnly _games As New Dictionary(Of UInt32, LanGame)
        Private ReadOnly _viewGames As New ObservableCollection(Of LanGame)(outQueue:=outQueue)
        Private ReadOnly _socket As New UdpClient
        Private ReadOnly _logger As Logger
        Private ReadOnly _defaultTargetHost As String
        Private ReadOnly _refreshStopper As IDisposable
        Private ReadOnly _infoProvider As IProductInfoProvider
        Private ReadOnly _clock As IClock

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(outQueue IsNot Nothing)
            Contract.Invariant(_games IsNot Nothing)
            Contract.Invariant(_viewGames IsNot Nothing)
            Contract.Invariant(_socket IsNot Nothing)
            Contract.Invariant(_logger IsNot Nothing)
            Contract.Invariant(_defaultTargetHost IsNot Nothing)
            Contract.Invariant(_refreshStopper IsNot Nothing)
            Contract.Invariant(_infoProvider IsNot Nothing)
        End Sub

        Public NotInheritable Class LanGame
            Private ReadOnly _gameDescription As WC3.LocalGameDescription
            Private ReadOnly _targetHosts As IRist(Of String)

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_gameDescription IsNot Nothing)
                Contract.Invariant(_targetHosts IsNot Nothing)
            End Sub

            Public Sub New(gameDescription As WC3.LocalGameDescription,
                           targetHosts As IRist(Of String))
                Contract.Requires(gameDescription IsNot Nothing)
                Contract.Requires(targetHosts IsNot Nothing)
                Me._gameDescription = gameDescription
                Me._targetHosts = targetHosts
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

        Public Sub New(infoProvider As IProductInfoProvider,
                       clock As IClock,
                       Optional defaultTargetHost As String = "localhost",
                       Optional logger As Logger = Nothing)
            Contract.Requires(infoProvider IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Contract.Requires(defaultTargetHost IsNot Nothing)

            Me._infoProvider = infoProvider
            Me._logger = If(logger, New Logger)
            Me._defaultTargetHost = defaultTargetHost
            Me._clock = clock

            Me._refreshStopper = clock.AsyncRepeat(RefreshPeriod, Sub() inQueue.QueueAction(Sub() RefreshAll()))
        End Sub

        Public ReadOnly Property Logger() As Logger
            Get
                Contract.Ensures(Contract.Result(Of Logger)() IsNot Nothing)
                Return _logger
            End Get
        End Property
        Public ReadOnly Property Clock As IClock
            Get
                Return _clock
            End Get
        End Property

        Private Sub AddGame(gameDescription As WC3.LocalGameDescription,
                            Optional targets As IRist(Of String) = Nothing)
            Contract.Requires(gameDescription IsNot Nothing)
            If _games.ContainsKey(gameDescription.GameId) Then
                Dim gameSet = _games(gameDescription.GameId)
                Contract.Assume(gameSet IsNot Nothing)
                If gameSet.GameDescription Is gameDescription Then Return
                Throw New InvalidOperationException("There is already a game being advertised with id = {0}.".Frmt(gameDescription.GameId))
            End If

            Dim lanGame = New LanGame(gameDescription, If(targets, MakeRist(_defaultTargetHost)))
            _games(gameDescription.GameId) = lanGame
            RefreshGame(lanGame)

            _logger.Log("Added game {0}".Frmt(gameDescription.Name), LogMessageType.Positive)
            _viewGames.Add(lanGame)
        End Sub
        Public Function QueueAddGame(gameDescription As WC3.LocalGameDescription,
                                     Optional targetHosts As IRist(Of String) = Nothing) As Task
            Contract.Requires(gameDescription IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(Sub() AddGame(gameDescription, targetHosts))
        End Function

        Private Function RemoveGame(id As UInt32) As Boolean
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
        Public Function QueueRemoveGame(id As UInt32) As Task(Of Boolean)
            Contract.Ensures(Contract.Result(Of Task(Of Boolean))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() RemoveGame(id))
        End Function

        Private Sub ClearGames()
            For Each id In _games.Keys.Cache
                RemoveGame(id)
            Next id
        End Sub
        Public Function QueueClearGames() As Task
            Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing)
            Return inQueue.QueueAction(AddressOf ClearGames)
        End Function

        Private Sub RefreshAll()
            For Each game In _games.Values
                Contract.Assume(game IsNot Nothing)
                RefreshGame(game)
            Next game
        End Sub
        Private Sub RefreshGame(game As LanGame)
            Contract.Requires(game IsNot Nothing)
            Dim pk = WC3.Protocol.MakeLanGameDetails(_infoProvider.MajorVersion, game.GameDescription)
            For Each host In game.TargetHosts
                SendPacket(pk, host, LanTargetPort)
            Next host
        End Sub
        Private Sub SendPacket(pk As WC3.Protocol.Packet, targetHost As String, targetPort As UShort)
            Contract.Requires(pk IsNot Nothing)
            Try
                'pack
                Dim data = pk.Payload.Data.ToArray
                data = Concat(Of Byte)({WC3.Protocol.Packets.PacketPrefix, pk.Id}, CUShort(data.Length + 4).Bytes, data).ToArray

                'Log
                _logger.Log(Function() "Sending to {0}: {1}".Frmt(targetHost, data.ToHexString), LogMessageType.DataRaw)
                _logger.Log(Function() "Sending {0} to {1}".Frmt(pk.Id, targetHost), LogMessageType.DataEvent)
                _logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(pk.Id, targetHost, pk.Payload.Description), LogMessageType.DataParsed)

                'Send
                _socket.Send(data, data.Length, targetHost, targetPort)

            Catch ex As Exception When TypeOf ex Is InvalidOperationException OrElse
                                       TypeOf ex Is ObjectDisposedException OrElse
                                       TypeOf ex Is SocketException OrElse
                                       TypeOf ex Is IO.IOException
                _logger.Log("Error sending {0}: {1}".Frmt(pk.Id, ex), LogMessageType.Problem)
                ex.RaiseAsUnexpected("Exception rose past {0}.send".Frmt(Me.GetType.Name))
            End Try
        End Sub

        Public Function ObserveGames(adder As Action(Of LanGame),
                                     remover As Action(Of LanGame)) As Task(Of IDisposable)
            Contract.Requires(adder IsNot Nothing)
            Contract.Requires(remover IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Task(Of IDisposable))() IsNot Nothing)
            Return inQueue.QueueFunc(Function() _viewGames.Observe(adder, remover))
        End Function

        Protected Overrides Function PerformDispose(finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return inQueue.QueueAction(
                Sub()
                    ClearGames()
                    _refreshStopper.Dispose()
                    _socket.Close()
                End Sub)
        End Function
    End Class
End Namespace
