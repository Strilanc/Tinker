Namespace Warcraft3
    Public Class TickRecord
        Public ReadOnly length As UShort
        Public ReadOnly start_time As Integer
        Public ReadOnly Property end_time() As Integer
            Get
                Return length + start_time
            End Get
        End Property

        Public Sub New(ByVal length As UShort, ByVal start_time As Integer)
            Me.length = length
            Me.start_time = start_time
        End Sub

        'Private checksum As Byte() = Nothing
        'Public Function provide_value(ByVal checksum As Byte()) As Boolean
        '    SyncLock Me
        '        If Me.checksum Is Nothing Then
        '            Me.checksum = checksum
        '            Return True
        '        End If
        '    End SyncLock

        '    Return ArraysEqual(Me.checksum, checksum)
        'End Function
    End Class

    Partial Public Class W3Player
        Public Class W3PlayerGameplay
            Inherits W3Player.W3PlayerPart
            Implements IW3PlayerGameplay

            Private ReadOnly tick_queue As New Queue(Of TickRecord)
            Private total_tick_time_P As Integer = 0

            Public Sub New(ByVal container As W3Player)
                MyBase.New(container)
            End Sub

            Public Sub Start()
                player.soul = Me
                Me.player.load_screen.Stop()
                handlers(W3PacketId.PLAYERS_CONNECTED) = AddressOf receivePacket_PLAYERS_CONNECTED_L
                handlers(W3PacketId.ACCEPT_HOST) = AddressOf receivePacket_ACCEPT_HOST_L
                handlers(W3PacketId.GAME_ACTION) = AddressOf receivePacket_GAME_DATA_L
                handlers(W3PacketId.TOCK) = AddressOf receivePacket_GAME_TICK_GUEST_L
                handlers(W3PacketId.CLIENT_DROP_LAGGER) = AddressOf receivePacket_DROP_LAGGER_L
            End Sub
            Public Sub [Stop]()
                handlers(W3PacketId.PLAYERS_CONNECTED) = Nothing
                handlers(W3PacketId.ACCEPT_HOST) = Nothing
                handlers(W3PacketId.GAME_ACTION) = Nothing
                handlers(W3PacketId.TOCK) = Nothing
                handlers(W3PacketId.CLIENT_DROP_LAGGER) = Nothing
            End Sub

            Private Sub queue_tick_L(ByVal record As TickRecord)
                If player.is_fake Then Return
                tick_queue.Enqueue(record)
            End Sub

#Region "Networking"
            Private Sub receivePacket_DROP_LAGGER_L(ByVal vals As Dictionary(Of String, Object))
                player.game.gameplay.f_DropLagger()
            End Sub
            Private Sub receivePacket_ACCEPT_HOST_L(ByVal vals As Dictionary(Of String, Object))
                player.send_packet_L(W3Packet.MakePacket_CONFIRM_HOST())
            End Sub
            Private Sub receivePacket_PLAYERS_CONNECTED_L(ByVal vals As Dictionary(Of String, Object))
                Dim n = CUInt(vals("player bitflags"))
                player.num_p2p_connections = 0
                For p As Byte = 1 To 12
                    player.num_p2p_connections += CInt(n And &H1)
                    n >>= 1
                Next p
            End Sub
            Private Sub receivePacket_GAME_DATA_L(ByVal vals As Dictionary(Of String, Object))
                vals = CType(vals("subpacket"), Dictionary(Of String, Object))
                Dim id = CByte(vals("id"))
                Dim data = CType(vals("data"), Byte())
                player.game.gameplay.f_QueueGameData(Me, concat(New Byte() {id}, data))
            End Sub
            Private Sub receivePacket_GAME_TICK_GUEST_L(ByVal vals As Dictionary(Of String, Object))
                If tick_queue.Count <= 0 Then
                    player.logger.log("Banned behavior: {0} responded to a tick which wasn't sent.".frmt(player.name), LogMessageTypes.Problem)
                    player.disconnect_L(True, W3PlayerLeaveTypes.disc)
                    Return
                End If

                Dim record = tick_queue.Dequeue()
                total_tick_time_P += record.length

                'Dim checksum = CType(vals("game state checksum"), Byte())
                'If synced Then
                '    If Not record.provide_value(checksum) Then
                '        synced = False
                '        game.broadcast_message_R("Desync detected.".frmt(name, unpackHexString(checksum)))
                '    End If
                'End If
            End Sub
#End Region

#Region "Interface"
            Private ReadOnly Property _total_tick_time_P() As Integer Implements IW3PlayerGameplay.tock_time
                Get
                    Return total_tick_time_P
                End Get
            End Property
            Private Function _queue_tick_R(ByVal record As TickRecord) As IFuture Implements IW3PlayerGameplay.f_QueueTick
                Return player.ref.enqueue(Function() eval(AddressOf queue_tick_L, record))
            End Function
#End Region

            Public Overrides Function Description() As String
                Return MyBase.Description() _
                        + "DT={0}gms".frmt(player.game.gameplay.game_time - Me.total_tick_time_P)
            End Function
            Private Function _f_start() As IFuture Implements IW3PlayerGameplay.f_Start
                Return player.ref.enqueue(AddressOf Start)
            End Function
            Private Function _f_stop() As IFuture Implements IW3PlayerGameplay.f_Stop
                Return player.ref.enqueue(AddressOf [Stop])
            End Function
        End Class
    End Class
End Namespace
