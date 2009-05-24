Namespace Warcraft3
    Partial Public Class W3Player
        Implements IW3Player
        Private soul As IW3PlayerPart
        Private ReadOnly lobby As W3PlayerLobby
        Private ReadOnly load_screen As W3PlayerLoadScreen
        Private ReadOnly gameplay As W3PlayerGameplay
        Private ReadOnly index As Byte
        Private ReadOnly game As IW3Game
        Private ReadOnly name As String
        Private ReadOnly listen_port As UShort = 0
        Private ReadOnly p2p_key As UInteger = 0
        Private ReadOnly is_fake As Boolean = False
        Private ReadOnly future_can_host As New Future(Of Boolean)
        Private Const MAX_NAME_LENGTH As Integer = 15
        Private WithEvents socket As W3Socket = Nothing
        Private ReadOnly logger As MultiLogger
        Private ReadOnly ref As ICallQueue
        Private ReadOnly eventRef As ICallQueue
        Private ReadOnly ping_queue As New Queue(Of W3PlayerPingRecord)
        Private num_p2p_connections As Integer = 0
        Private latency As Double
        Private voted_to_start As Boolean = False
        Private admin_tries As Integer = 0
        Private ready As Boolean
        Private ReadOnly Property remote_ip_external() As Byte() Implements IW3Player.remote_ip_external
            Get
                If is_fake Then
                    Return New Byte() {0, 0, 0, 0}
                Else
                    Return socket.getRemoteIp()
                End If
            End Get
        End Property
        Private ReadOnly Property remote_port_external() As UShort Implements IW3Player.remote_port_external
            Get
                If is_fake Then
                    Return 0
                Else
                    Return socket.getRemotePort
                End If
            End Get
        End Property

        '''<summary>Creates a fake player.</summary>
        Public Sub New(ByVal ref As ICallQueue, _
                       ByVal index As Byte, _
                       ByVal game As IW3Game, _
                       ByVal name As String, _
                       Optional ByVal logger As MultiLogger = Nothing)
            Me.lobby = New W3PlayerLobby(Me)
            Me.load_screen = New W3PlayerLoadScreen(Me)
            Me.gameplay = New W3PlayerGameplay(Me)
            Me.logger = If(logger, New MultiLogger)
            Me.eventRef = New ThreadedCallQueue("{0} {1} eventRef".frmt(Me.GetType.Name, name))
            Me.ref = If(ref, New ThreadedCallQueue("{0} {1} ref".frmt(Me.GetType.Name, name)))
            Me.game = game
            Me.index = index
            If name.Length > MAX_NAME_LENGTH Then
                name = name.Substring(0, MAX_NAME_LENGTH)
            End If
            Me.name = name
            is_fake = True
            Me.lobby.Start()
        End Sub

        '''<summary>Creates a real player.</summary>
        '''<remarks>Real players are assigned a game by the lobby.</remarks>
        Public Sub New(ByVal ref As ICallQueue, _
                       ByVal index As Byte, _
                       ByVal game As IW3Game, _
                       ByVal p As W3ConnectingPlayer, _
                       Optional ByVal logging As MultiLogger = Nothing)
            Me.lobby = New W3PlayerLobby(Me)
            Me.load_screen = New W3PlayerLoadScreen(Me)
            Me.gameplay = New W3PlayerGameplay(Me)
            Me.logger = If(logging, New MultiLogger)
            p.socket.logger = Me.logger
            Me.eventRef = New ThreadedCallQueue("{0} {1} eventRef".frmt(Me.GetType.Name, name))
            Me.ref = If(ref, New ThreadedCallQueue("{0} {1} ref".frmt(Me.GetType.Name, name)))
            Me.game = game
            Me.p2p_key = p.p2p_key

            Me.socket = p.socket
            Me.name = p.name
            Me.listen_port = p.listen_port
            Me.index = index
            Me.socket.set_reading(True)

            Me.lobby.Start()
            threadedCall(AddressOf testCanHost_T, Me.GetType.Name + "[" + Me.name + "].TestCanHost")
        End Sub

        '''<summary>Determines if a player can host by attempting to connect to them.</summary>
        Private Sub testCanHost_T()
            If is_fake Then
                future_can_host.setValue(False)
                Return
            End If

            Try
                Dim testing_socket = New Net.Sockets.TcpClient
                testing_socket.Connect(New Net.IPAddress(remote_ip_external), listen_port)
                Dim success = testing_socket.Connected
                testing_socket.Close()
                future_can_host.setValue(success)
            Catch e As Exception
                future_can_host.setValue(False)
            End Try
        End Sub

        Private ReadOnly Property canHost() As HostTestResults
            Get
                If Not future_can_host.isReady Then
                    Return HostTestResults.test
                ElseIf future_can_host.getValue() Then
                    Return HostTestResults.pass
                Else
                    Return HostTestResults.fail
                End If
            End Get
        End Property

        '''<summary>Disconnects this player and removes them from the system.</summary>
        Private Sub disconnect_L(ByVal expected As Boolean, ByVal leave_type As W3PlayerLeaveTypes)
            If Not Me.is_fake Then
                socket.disconnect()
            End If
            game.f_RemovePlayer(Me, expected, leave_type)
        End Sub

        Private Sub queue_ping_L(ByVal record As W3PlayerPingRecord)
            If Me.is_fake Then Return
            ping_queue.Enqueue(record)
            If ping_queue.Count > 20 Then
                logger.log(name + " has not responded to pings for a significant amount of time.", LogMessageTypes.Problem)
                disconnect_L(True, W3PlayerLeaveTypes.disc)
            End If
        End Sub

        Private Function send_packet_L(ByVal pk As W3Packet) As Outcome
            If Me.is_fake Then Return success("Fake player doesn't need to have packets sent.")
            Return socket.SendPacket(pk)
        End Function

        Private Sub catch_socket_received_packet(ByVal sender As W3Socket, ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object)) Handles socket.ReceivedPacket
            ref.enqueue(Function() eval(AddressOf soul.receivePacket_L, id, vals))
        End Sub
        Private Sub catch_socket_disconnected() Handles socket.Disconnected
            ref.enqueue(Function() eval(AddressOf disconnect_L, False, W3PlayerLeaveTypes.disc))
        End Sub

#Region "Interface"
        Private ReadOnly Property _latency() As Double Implements IW3Player.latency_P
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
        Private ReadOnly Property _listen_port() As UShort Implements IW3Player.listen_port
            Get
                Return listen_port
            End Get
        End Property
        Private ReadOnly Property _canHost() As HostTestResults Implements IW3Player.canHost
            Get
                Return canHost
            End Get
        End Property
        Private ReadOnly Property _is_fake() As Boolean Implements IW3Player.is_fake
            Get
                Return is_fake
            End Get
        End Property
        Private ReadOnly Property _num_p2p_connections_P() As Integer Implements IW3Player.num_p2p_connections_P
            Get
                Return num_p2p_connections
            End Get
        End Property

        Protected Overridable ReadOnly Property _p2p_key() As UInteger Implements IW3Player.p2p_key
            Get
                Return p2p_key
            End Get
        End Property

        Private Property _voted_to_start() As Boolean Implements IW3Player.voted_to_start
            Get
                Return voted_to_start
            End Get
            Set(ByVal value As Boolean)
                voted_to_start = value
            End Set
        End Property
        Private Property _admin_tries() As Integer Implements IW3Player.admin_tries
            Get
                Return admin_tries
            End Get
            Set(ByVal value As Integer)
                admin_tries = value
            End Set
        End Property

        Private Function _disconnect_R(ByVal expected As Boolean, ByVal leave_type As W3PlayerLeaveTypes) As IFuture Implements IW3Player.disconnect_R
            Return ref.enqueue(Function() eval(AddressOf disconnect_L, expected, leave_type))
        End Function
        Public ReadOnly Property _game() As IW3Game Implements IW3Player.game
            Get
                Return game
            End Get
        End Property
        Private Function _send_packet_R(ByVal pk As W3Packet) As IFuture(Of Outcome) Implements IW3Player.f_SendPacket
            Return ref.enqueue(Function() send_packet_L(pk))
        End Function
        Private Function _queue_ping_R(ByVal record As W3PlayerPingRecord) As IFuture Implements IW3Player.f_QueuePing
            Return ref.enqueue(Function() eval(AddressOf queue_ping_L, record))
        End Function
        Private ReadOnly Property _soul() As IW3PlayerPart Implements IW3Player.soul
            Get
                Return soul
            End Get
        End Property
        Private ReadOnly Property _soul_1() As IW3PlayerLobby Implements IW3Player.lobby
            Get
                Return lobby
            End Get
        End Property
        Private ReadOnly Property _soul_2() As IW3PlayerLoadScreen Implements IW3Player.load_screen
            Get
                Return load_screen
            End Get
        End Property
        Private ReadOnly Property _soul_3() As IW3PlayerGameplay Implements IW3Player.gameplay
            Get
                Return gameplay
            End Get
        End Property
#End Region

        Public ReadOnly Property ready_to_play() As Boolean Implements IW3Player.ready_to_play
            Get
                Return ready
            End Get
        End Property
    End Class
End Namespace
