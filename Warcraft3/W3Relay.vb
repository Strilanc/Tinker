Namespace WC3
    Public NotInheritable Class W3RelayServer
        Implements IDisposable

        Private Class GamePair
            Private _remoteGame As RemoteGameDescription
            Private _localGame As LocalGameDescription

            <ContractInvariantMethod()> Private Sub ObjectInvariant()
                Contract.Invariant(_remoteGame IsNot Nothing)
                Contract.Invariant(_localGame IsNot Nothing)
            End Sub

            Public Sub New(ByVal remoteGame As RemoteGameDescription,
                           ByVal localGame As LocalGameDescription)
                Contract.Requires(remoteGame IsNot Nothing)
                Contract.Requires(localGame IsNot Nothing)
                Me._remoteGame = remoteGame
                Me._localGame = localGame
            End Sub
            Public Sub Update(ByVal game As RemoteGameDescription)
                Contract.Requires(game IsNot Nothing)
                Contract.Assume(game.Age.Ticks >= 0)
                Me._remoteGame = New RemoteGameDescription(_remoteGame.Name,
                                                           _remoteGame.GameStats,
                                                           _remoteGame.Address.WithPort(_remoteGame.Port),
                                                           _remoteGame.GameId,
                                                           _remoteGame.EntryKey,
                                                           game.TotalSlotCount,
                                                           game.GameType,
                                                           game.GameState,
                                                           game.UsedSlotCount,
                                                           clock:=New SystemClock(),
                                                           baseage:=game.Age)
                Me._localGame = New LocalGameDescription(_localGame.Name,
                                                         _localGame.GameStats,
                                                         _localGame.Port,
                                                         _localGame.GameId,
                                                         _localGame.EntryKey,
                                                         game.TotalSlotCount,
                                                         game.GameType,
                                                         game.GameState,
                                                         game.UsedSlotCount,
                                                         baseage:=game.Age,
                                                         clock:=New SystemClock())
            End Sub
            Public ReadOnly Property RemoteGame As RemoteGameDescription
                Get
                    Contract.Ensures(Contract.Result(Of RemoteGameDescription)() IsNot Nothing)
                    Return _remoteGame
                End Get
            End Property
            Public ReadOnly Property LocalGame As LocalGameDescription
                Get
                    Contract.Ensures(Contract.Result(Of LocalGameDescription)() IsNot Nothing)
                    Return _localGame
                End Get
            End Property
        End Class

        Private ReadOnly _games As New Dictionary(Of UInteger, GamePair)
        Private ReadOnly _portHandle As PortPool.PortHandle
        Private ReadOnly _accepter As W3ConnectionAccepter
        Private ReadOnly _clock As IClock
        Private _gameCount As UInteger

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_clock IsNot Nothing)
            Contract.Invariant(_portHandle IsNot Nothing)
            Contract.Invariant(_games IsNot Nothing)
            Contract.Invariant(_accepter IsNot Nothing)
        End Sub

        Public Sub New(ByVal portHandle As PortPool.PortHandle,
                       ByVal clock As IClock)
            Contract.Requires(portHandle IsNot Nothing)
            Contract.Requires(clock IsNot Nothing)
            Me._portHandle = portHandle
            Me._clock = clock
            Me._accepter = New W3ConnectionAccepter(clock)
            Me._accepter.Accepter.OpenPort(portHandle.Port)
        End Sub
        Public Function AddGame(ByVal game As RemoteGameDescription) As UInteger
            Contract.Requires(game IsNot Nothing)
            _gameCount += 1UI
            Contract.Assume(game.Age.Ticks >= 0)
            Dim localGame = New LocalGameDescription(name:=game.Name,
                                                     GameStats:=game.GameStats,
                                                     hostport:=_portHandle.Port,
                                                     gameid:=_gameCount,
                                                     entrykey:=game.EntryKey,
                                                     totalslotcount:=game.TotalSlotCount,
                                                     gametype:=game.GameType,
                                                     state:=game.GameState,
                                                     usedSlotCount:=game.UsedSlotCount,
                                                     baseage:=game.Age,
                                                     clock:=New SystemClock())
            _games(_gameCount) = New GamePair(game, localGame)
            Return _gameCount
        End Function
        Public ReadOnly Property TryGetLocalGameDescription(ByVal gameId As UInteger) As LocalGameDescription
            Get
                If Not _games.ContainsKey(gameId) Then Return Nothing
                Dim pair = _games(gameId)
                Contract.Assume(pair IsNot Nothing)
                Return pair.LocalGame()
            End Get
        End Property
        Public Function TryUpdateGameDescription(ByVal gameId As UInteger, ByVal game As RemoteGameDescription) As Boolean
            Contract.Requires(game IsNot Nothing)
            If Not _games.ContainsKey(gameId) Then Return False
            Dim pair = _games(gameId)
            Contract.Assume(pair IsNot Nothing)
            pair.Update(game)
            Return True
        End Function
        Public Sub RemoveGame(ByVal gameId As UInteger)
            If Not _games.ContainsKey(gameId) Then Return
            _games.Remove(gameId)
        End Sub

        Private Sub Accept(ByVal knockData As Protocol.KnockData, ByVal socket As W3Socket)
            Contract.Requires(knockData IsNot Nothing)
            Contract.Requires(socket IsNot Nothing)
            If Not _games.ContainsKey(knockData.GameId) Then Throw New IO.InvalidDataException()
            Dim game = _games(knockData.GameId)
            Contract.Assume(game IsNot Nothing)
            AsyncTcpConnect(game.RemoteGame.Address, game.RemoteGame.Port).ContinueWith(
                Sub(task)
                    If task.Status = TaskStatus.Faulted Then
                        socket.Disconnect(expected:=False, reason:="Failed to interconnect with game host: {0}.".Frmt(task.Exception.Summarize))
                        Return
                    End If

                    Dim w = New W3Socket(New PacketSocket(stream:=task.Result.GetStream,
                                                          localendpoint:=CType(task.Result.Client.LocalEndPoint, Net.IPEndPoint),
                                                          remoteendpoint:=CType(task.Result.Client.RemoteEndPoint, Net.IPEndPoint),
                                                          clock:=_clock))
                    w.SendPacket(Protocol.MakeKnock(knockData.Name,
                                                    knockData.ListenPort,
                                                    CUShort(knockData.InternalEndPoint.Port),
                                                    game.RemoteGame.GameId,
                                                    game.RemoteGame.EntryKey,
                                                    knockData.PeerKey,
                                                    knockData.InternalEndPoint.Address))

                    StreamRelay.InterShunt(w.Socket.SubStream, socket.Socket.SubStream)
                End Sub
            )
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            _portHandle.Dispose()
            _accepter.Accepter.CloseAllPorts()
        End Sub
    End Class

    Public NotInheritable Class StreamRelay
        Private Sub New()
        End Sub

        Public Shared Function InterShunt(ByVal stream1 As IO.Stream, ByVal stream2 As IO.Stream) As DisposableWithTask
            Contract.Requires(stream1 IsNot Nothing)
            Contract.Requires(stream2 IsNot Nothing)
            Contract.Ensures(Contract.Result(Of DisposableWithTask)() IsNot Nothing)
            Dim result = New DisposableWithTask
            Shunt(stream1, stream2).AssumeNotNull().ContinueWithAction(Sub() result.Dispose())
            Shunt(stream2, stream1).AssumeNotNull().ContinueWithAction(Sub() result.Dispose())
            result.ChainEventualDisposalTo(stream1)
            result.ChainEventualDisposalTo(stream2)
            Return result
        End Function
        Private Shared Async Function Shunt(ByVal src As IO.Stream, ByVal dst As IO.Stream) As Task
            Contract.Assume(src IsNot Nothing)
            Contract.Assume(dst IsNot Nothing)
            'Contract.Ensures(Contract.Result(Of Task)() IsNot Nothing) 'AsyncCTP causes code contracts to fail in this case
            Try
                Dim buffer(0 To 4096 - 1) As Byte
                Do
                    Dim numRead = Await src.ReadAsync(buffer, 0, buffer.Length)
                    If numRead = 0 Then Throw New IO.IOException("End of stream")
                    dst.Write(buffer, 0, numRead)
                Loop
            Catch ex As Exception
                'ignore (to match old behaviour, should fix)
            End Try
        End Function
    End Class
End Namespace
