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
                Me._remoteGame = remoteGame
                Me._localGame = localGame
            End Sub
            Public Sub Update(ByVal game As RemoteGameDescription)
                Contract.Requires(game IsNot Nothing)
                Me._remoteGame = New RemoteGameDescription(_remoteGame.Name,
                                                           _remoteGame.GameStats,
                                                           New Net.IPEndPoint(_remoteGame.Address, _remoteGame.Port),
                                                           _remoteGame.GameId,
                                                           _remoteGame.EntryKey,
                                                           game.TotalSlotCount,
                                                           game.GameType,
                                                           game.GameState,
                                                           game.UsedSlotCount,
                                                           game.Age)
                Me._localGame = New LocalGameDescription(_localGame.Name,
                                                         _localGame.GameStats,
                                                         _localGame.Port,
                                                         _localGame.GameId,
                                                         _localGame.EntryKey,
                                                         game.TotalSlotCount,
                                                         game.GameType,
                                                         game.GameState,
                                                         game.UsedSlotCount,
                                                         game.Age)
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
            _gameCount += 1UI
            Dim localGame = New LocalGameDescription(game.Name,
                                                     game.GameStats,
                                                     _portHandle.Port,
                                                     _gameCount,
                                                     game.EntryKey,
                                                     game.TotalSlotCount,
                                                     game.GameType,
                                                     game.GameState,
                                                     game.UsedSlotCount,
                                                     game.Age)
            _games(_gameCount) = New GamePair(game, localGame)
            Return _gameCount
        End Function
        Public ReadOnly Property TryGetLocalGameDescription(ByVal gameId As UInteger) As LocalGameDescription
            Get
                If Not _games.ContainsKey(gameId) Then Return Nothing
                Return _games(gameId).LocalGame
            End Get
        End Property
        Public Function TryUpdateGameDescription(ByVal gameId As UInteger, ByVal game As RemoteGameDescription) As Boolean
            Contract.Requires(game IsNot Nothing)
            If Not _games.ContainsKey(gameId) Then Return False
            _games(gameId).Update(game)
            Return True
        End Function
        Public Sub RemoveGame(ByVal gameId As UInteger)
            If Not _games.ContainsKey(gameId) Then Return
            _games.Remove(gameId)
        End Sub

        Private Sub Accept(ByVal connector As W3ConnectingPlayer)
            If Not _games.ContainsKey(connector.GameId) Then Throw New IO.InvalidDataException()
            Dim game = _games(connector.GameId)
            AsyncTcpConnect(game.RemoteGame.Address, game.RemoteGame.Port).CallWhenValueReady(
                Sub(result, exception)
                    If exception IsNot Nothing Then
                        connector.Socket.Disconnect(expected:=False, reason:="Failed to interconnect with game host.")
                        Return
                    End If

                    Dim w = New W3Socket(New PacketSocket(stream:=result.GetStream,
                                                          localendpoint:=CType(result.Client.LocalEndPoint, Net.IPEndPoint),
                                                          remoteendpoint:=CType(result.Client.RemoteEndPoint, Net.IPEndPoint),
                                                          clock:=_clock))
                    w.SendPacket(Packet.MakeKnock(connector.Name,
                                                  connector.ListenPort,
                                                  CUShort(connector.RemoteEndPoint.Port),
                                                  game.RemoteGame.GameId,
                                                  game.RemoteGame.EntryKey,
                                                  connector.PeerKey,
                                                  connector.RemoteEndPoint.Address))

                    StreamRelay.InterShunt(w.Socket.SubStream, connector.Socket.Socket.SubStream)
                End Sub
            )
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            _portHandle.Dispose()
            _accepter.Accepter.CloseAllPorts()
        End Sub
    End Class

    Public Class StreamRelay
        Public Shared Function InterShunt(ByVal stream1 As IO.Stream, ByVal stream2 As IO.Stream) As FutureDisposable
            Contract.Requires(stream1 IsNot Nothing)
            Contract.Requires(stream2 IsNot Nothing)
            Contract.Ensures(Contract.Result(Of FutureDisposable)() IsNot Nothing)
            Dim result = New FutureDisposable
            Shunt(stream1, stream2).CallWhenReady(Sub() result.Dispose())
            Shunt(stream2, stream1).CallWhenReady(Sub() result.Dispose())
            result.FutureDisposed.CallWhenReady(Sub() stream1.Dispose())
            result.FutureDisposed.CallWhenReady(Sub() stream2.Dispose())
            Return result
        End Function
        Private Shared Function Shunt(ByVal src As IO.Stream, ByVal dst As IO.Stream) As ifuture
            Contract.Requires(src IsNot Nothing)
            Contract.Requires(dst IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ifuture)() IsNot Nothing)
            Dim buffer(0 To 4096 - 1) As Byte
            Return AsyncProduceConsumeUntilError2(
                producer:=Function() src.AsyncRead(buffer, 0, buffer.Length),
                consumer:=Sub(numRead)
                              If numRead = 0 Then Throw New IO.IOException("End of stream")
                              dst.Write(buffer, 0, numRead)
                          End Sub,
                errorHandler:=Sub(exception)
                                  'ignore
                              End Sub
            )
        End Function
    End Class
End Namespace
