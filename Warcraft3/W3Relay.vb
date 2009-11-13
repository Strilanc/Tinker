Namespace WC3
    Public Class RemoteGameDescription
        Private ReadOnly _name As String
        Private ReadOnly _stats As GameStats
        Private ReadOnly _totalSlotCount As Integer
        Private ReadOnly _usedPlayerSlotCount As Integer
        Private ReadOnly _hostAddress As Net.IPAddress
        Public ReadOnly hostPort As UShort
        Public ReadOnly entryId As UInt32
        Public ReadOnly entryKey As UInt32
        Public ReadOnly age As TimeSpan
        Public ReadOnly type As GameTypes
        Public ReadOnly state As Bnet.Packet.GameStates

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_totalSlotCount > 0)
            Contract.Invariant(_totalSlotCount <= 12)
            Contract.Invariant(_usedPlayerSlotCount >= 0)
            Contract.Invariant(_usedPlayerSlotCount <= _totalSlotCount)
            Contract.Invariant(_stats IsNot Nothing)
            Contract.Invariant(_name IsNot Nothing)
            Contract.Invariant(_hostAddress IsNot Nothing)
        End Sub

        Public Sub New(ByVal name As String,
                       ByVal stats As GameStats,
                       ByVal hostPort As UShort,
                       ByVal hostAddress As Net.IPAddress,
                       ByVal gameId As UInt32,
                       ByVal lanKey As UInteger,
                       ByVal elapsedSeconds As UInt32,
                       ByVal totalPlayerSlotCount As Integer,
                       ByVal usedPlayerSlotCount As Integer,
                       ByVal type As GameTypes,
                       ByVal state As Bnet.Packet.GameStates)
            Contract.Requires(name IsNot Nothing)
            Contract.Requires(stats IsNot Nothing)
            Contract.Requires(hostAddress IsNot Nothing)
            Contract.Requires(totalPlayerSlotCount > 0)
            Contract.Requires(totalPlayerSlotCount <= 12)
            Contract.Requires(usedPlayerSlotCount >= 0)
            Contract.Requires(usedPlayerSlotCount < totalPlayerSlotCount)
            Me._name = name
            Me._stats = stats
            Me._totalSlotCount = totalPlayerSlotCount
            Me._usedPlayerSlotCount = usedPlayerSlotCount
            Me.hostPort = hostPort
            Me.type = type
            Me.entryId = gameId
            Me.entryKey = lanKey
            Me.age = CInt(elapsedSeconds).Seconds
            Me._hostAddress = hostAddress
        End Sub

        Public ReadOnly Property Name As String
            Get
                Contract.Ensures(Contract.Result(Of String)() IsNot Nothing)
                Return _name
            End Get
        End Property
        Public ReadOnly Property Stats As GameStats
            Get
                Contract.Ensures(Contract.Result(Of GameStats)() IsNot Nothing)
                Return _stats
            End Get
        End Property
        Public ReadOnly Property HostAddress As Net.IPAddress
            Get
                Contract.Ensures(Contract.Result(Of Net.IPAddress)() IsNot Nothing)
                Return _hostAddress
            End Get
        End Property
        Public ReadOnly Property TotalSlotCount() As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() > 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= 12)
                Return _totalSlotCount
            End Get
        End Property
        Public ReadOnly Property UsedSlotCount As Integer
            Get
                Contract.Ensures(Contract.Result(Of Integer)() >= 0)
                Contract.Ensures(Contract.Result(Of Integer)() <= TotalSlotCount)
                Return _usedPlayerSlotCount
            End Get
        End Property
    End Class
    Public Class LocalGameDescription
        Private ReadOnly _name As String
        Private ReadOnly _stats As GameStats
        Private ReadOnly _totalSlotCount As Integer
        Private ReadOnly _usedPlayerSlotCount As Integer
        Public ReadOnly entryId As UInt32
        Public ReadOnly entryKey As UInt32
        Public ReadOnly age As TimeSpan
        Public ReadOnly type As GameTypes
        Public ReadOnly state As Bnet.Packet.GameStates
    End Class

    Public Class W3RelayServer
        Private ReadOnly games As New Dictionary(Of UInteger, RemoteGameDescription)
        Private n As UInteger
        Public accepter As New W3ConnectionAccepter()

        Public Function AddGame(ByVal remoteGame As RemoteGameDescription) As UInteger
            Dim r = n
            games(r) = remoteGame
            n += 1UI
            Return r
        End Function

        Private Sub accept(ByVal connector As W3ConnectingPlayer)
            If Not games.ContainsKey(connector.GameId) Then Throw New IO.InvalidDataException()
            Dim game = games(connector.GameId)
            FutureCreateConnectedTcpClient(game.HostAddress, game.hostPort).CallWhenValueReady(
                Sub(result, exception)
                    If exception IsNot Nothing Then
                        connector.Socket.Disconnect(expected:=False, reason:="Failed to interconnect with game host.")
                        Return
                    End If

                    Dim w = New W3Socket(New PacketSocket(result))
                    w.SendPacket(Packet.MakeKnock(connector.Name,
                                                    connector.ListenPort,
                                                    CUShort(connector.RemoteEndPoint.Port),
                                                    game.entryId,
                                                    game.entryKey,
                                                    connector.PeerKey,
                                                    connector.RemoteEndPoint.Address))

                    StreamRelay.CreateRelay(w.Socket.SubStream, connector.Socket.Socket.SubStream)
                End Sub
            )
        End Sub
    End Class

    Public Class StreamRelay
        Public Shared Function CreateRelay(ByVal stream1 As IO.Stream, ByVal stream2 As IO.Stream) As FutureDisposable
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
                producer:=Function() src.FutureRead(buffer, 0, buffer.Length),
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