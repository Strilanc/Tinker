Namespace Warcraft3
    Public Enum W3PlayerStates
        Lobby
        Loading
        Playing
    End Enum
    Partial Public Class W3Player
        Implements IW3Player
        Private state As W3PlayerStates = W3PlayerStates.Lobby
        Private ReadOnly index As Byte
        Private ReadOnly game As IW3Game
        Private ReadOnly name As String
        Private ReadOnly listenPort As UShort
        Private ReadOnly peerKey As UInteger
        Private ReadOnly isFake As Boolean
        Private ReadOnly testCanHost As IFuture(Of Boolean)
        Private Const MAX_NAME_LENGTH As Integer = 15
        Private ReadOnly socket As W3Socket
        Private ReadOnly logger As Logger
        Private ReadOnly ref As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly eref As ICallQueue = New ThreadPooledCallQueue
        Private ReadOnly pingQueue As New Queue(Of W3PlayerPingRecord)
        Private numPeerConnections As Integer
        Private latency As Double
        Private hasVotedToStart As Boolean
        Private numAdminTries As Integer

        <ContractInvariantMethod()> Protected Sub Invariant()
            Contract.Invariant(numPeerConnections >= 0)
            Contract.Invariant(numPeerConnections <= 12)
            Contract.Invariant(latency >= 0)
            Contract.Invariant(Not Double.IsNaN(latency))
            Contract.Invariant(Not Double.IsInfinity(latency))
            Contract.Invariant(pingQueue IsNot Nothing)
            Contract.Invariant(logger IsNot Nothing)
            Contract.Invariant(ref IsNot Nothing)
            Contract.Invariant(eref IsNot Nothing)
            Contract.Invariant(socket Is Nothing = isFake)
            Contract.Invariant(testCanHost IsNot Nothing)
            Contract.Invariant(numAdminTries >= 0)
            Contract.Invariant(name IsNot Nothing)
            Contract.Invariant(game IsNot Nothing)
            Contract.Invariant(index > 0)
            Contract.Invariant(index <= 12)
        End Sub

        '''<summary>Creates a fake player.</summary>
        Public Sub New(ByVal index As Byte,
                       ByVal game As IW3Game,
                       ByVal name As String,
                       Optional ByVal logger As Logger = Nothing)
            Contract.Requires(index > 0)
            Contract.Requires(index <= 12)
            Contract.Requires(game IsNot Nothing)
            Contract.Requires(name IsNot Nothing)

            Me.logger = If(logger, New Logger)
            Me.game = game
            Me.index = index
            If name.Length > MAX_NAME_LENGTH Then
                name = name.Substring(0, MAX_NAME_LENGTH)
            End If
            Me.name = name
            isFake = True
            LobbyStart()
            Me.testCanHost = False.Futurize()

            Contract.Assume(Not Double.IsNaN(latency))
            Contract.Assume(Not Double.IsInfinity(latency))
        End Sub

        '''<summary>Creates a real player.</summary>
        '''<remarks>Real players are assigned a game by the lobby.</remarks>
        Public Sub New(ByVal index As Byte,
                       ByVal game As IW3Game,
                       ByVal connectingPlayer As W3ConnectingPlayer,
                       Optional ByVal logging As Logger = Nothing)
            Contract.Requires(index > 0)
            Contract.Requires(index <= 12)
            Contract.Requires(game IsNot Nothing)
            Contract.Requires(connectingPlayer IsNot Nothing)

            Me.logger = If(logging, New Logger)
            connectingPlayer.Socket.logger = Me.logger
            Me.game = game
            Me.peerKey = connectingPlayer.PeerKey

            Me.socket = connectingPlayer.Socket
            Me.name = connectingPlayer.Name
            Me.listenPort = connectingPlayer.ListenPort
            Me.index = index
            AddHandler socket.ReceivedPacket, AddressOf c_ReceivedPacket
            AddHandler socket.Disconnected, AddressOf c_Disconnected

            handlers(W3PacketId.Pong) = AddressOf ReceivePong
            handlers(W3PacketId.Leaving) = AddressOf ReceiveLeaving
            handlers(W3PacketId.NonGameAction) = AddressOf ReceiveNonGameAction
            handlers(W3PacketId.MapFileDataReceived) = AddressOf IgnorePacket
            handlers(W3PacketId.MapFileDataProblem) = AddressOf IgnorePacket

            LobbyStart()
            Me.socket.SetReading(True)

            'Test hosting
            Me.testCanHost = ThreadedFunc(
                Function()
                    If isFake Then  Return False

                    Try
                        Dim testingSocket = New Net.Sockets.TcpClient
                        testingSocket.Connect(socket.RemoteEndPoint.Address, listenPort)
                        testingSocket.Close()
                        Return True
                    Catch e As Exception
                        Return False
                    End Try
                End Function,
                Me.GetType.Name + "[" + Me.name + "].TestCanHost"
            )

            Contract.Assume(Not Double.IsNaN(latency))
            Contract.Assume(Not Double.IsInfinity(latency))
        End Sub

        Private ReadOnly Property CanHost() As HostTestResults
            Get
                If Not testCanHost.IsReady Then
                    Return HostTestResults.Test
                ElseIf testCanHost.GetValue() Then
                    Return HostTestResults.Pass
                Else
                    Return HostTestResults.Fail
                End If
            End Get
        End Property

        '''<summary>Disconnects this player and removes them from the system.</summary>
        Private Sub Disconnect(ByVal expected As Boolean,
                               ByVal leaveType As W3PlayerLeaveTypes,
                               ByVal reason As String)
            Contract.Requires(reason IsNot Nothing)
            If Not Me.isFake Then
                socket.disconnect(reason)
            End If
            game.f_RemovePlayer(Me, expected, leaveType, reason)
        End Sub

        Private Sub QueuePing(ByVal record As W3PlayerPingRecord)
            Contract.Requires(record IsNot Nothing)
            If Me.isFake Then Return
            pingQueue.Enqueue(record)
            If pingQueue.Count > 20 Then
                logger.log(name + " has not responded to pings for a significant amount of time.", LogMessageTypes.Problem)
                Disconnect(True, W3PlayerLeaveTypes.Disconnect, "no pongs for pings")
            End If
        End Sub

        Private Function SendPacket(ByVal pk As W3Packet) As Outcome
            Contract.Requires(pk IsNot Nothing)
            If Me.isFake Then Return success("Fake player doesn't need to have packets sent.")
            Return socket.SendPacket(pk)
        End Function

        Private Sub c_ReceivedPacket(ByVal sender As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))
            ref.QueueAction(Sub() ReceivePacket(id, vals))
        End Sub
        Private Sub c_Disconnected(ByVal sender As W3Socket, ByVal reason As String)
            ref.QueueAction(Sub() Disconnect(False, W3PlayerLeaveTypes.Disconnect, reason))
        End Sub

#Region "Interface"
        Private ReadOnly Property _RemoteEndPoint As Net.IPEndPoint Implements IW3Player.RemoteEndPoint
            Get
                Return socket.RemoteEndPoint
            End Get
        End Property
        Private ReadOnly Property _latency() As Double Implements IW3Player.latency
            Get
                Return latency
            End Get
        End Property
        Private ReadOnly Property _index() As Byte Implements IW3Player.index
            Get
                Return index
            End Get
        End Property
        Private ReadOnly Property _name() As String Implements IW3Player.name
            Get
                Return name
            End Get
        End Property
        Private ReadOnly Property _ListenPort() As UShort Implements IW3Player.ListenPort
            Get
                Return listenPort
            End Get
        End Property
        Private ReadOnly Property _canHost() As HostTestResults Implements IW3Player.canHost
            Get
                Return CanHost
            End Get
        End Property
        Private ReadOnly Property _IsFake() As Boolean Implements IW3Player.IsFake
            Get
                Return isFake
            End Get
        End Property
        Private ReadOnly Property _numPeerConnections() As Integer Implements IW3Player.numPeerConnections
            Get
                Return numPeerConnections
            End Get
        End Property
        Protected Overridable ReadOnly Property _peerKey() As UInteger Implements IW3Player.peerKey
            Get
                Return peerKey
            End Get
        End Property
        Private Property _HasVotedToStart() As Boolean Implements IW3Player.HasVotedToStart
            Get
                Return hasVotedToStart
            End Get
            Set(ByVal value As Boolean)
                hasVotedToStart = value
            End Set
        End Property
        Private Property _NumAdminTries() As Integer Implements IW3Player.NumAdminTries
            Get
                Return numAdminTries
            End Get
            Set(ByVal value As Integer)
                numAdminTries = value
            End Set
        End Property
        Private Function _f_Disconnect(ByVal expected As Boolean, ByVal leave_type As W3PlayerLeaveTypes, ByVal reason As String) As IFuture Implements IW3Player.Disconnect
            Return ref.QueueAction(Sub() Disconnect(expected, leave_type, reason))
        End Function
        Public ReadOnly Property _game() As IW3Game Implements IW3Player.game
            Get
                Return game
            End Get
        End Property
        Private Function _f_SendPacket(ByVal pk As W3Packet) As IFuture(Of Outcome) Implements IW3Player.f_SendPacket
            Return ref.QueueFunc(Function()
                                     Contract.Assume(pk IsNot Nothing)
                                     Return SendPacket(pk)
                                 End Function)
        End Function
        Private Function _f_QueuePing(ByVal record As W3PlayerPingRecord) As IFuture Implements IW3Player.f_QueuePing
            Return ref.QueueAction(Sub() QueuePing(record))
        End Function
#End Region
    End Class
End Namespace
