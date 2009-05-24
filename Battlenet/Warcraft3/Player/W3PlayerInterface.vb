Namespace Warcraft3
    Public Enum W3PlayerStates As Integer
        normal = 0 'in an instance waiting for countdown
        counting = 1 'counting down
        overcounting = 2 'finished counting down, waiting for launch
        loading = 3 'loading the map
        ready = 4 'ready to start
        dead = 5
    End Enum
    Public Enum HostTestResults As Integer
        fail = -1
        test = 0
        pass = 1
    End Enum
    Public Class W3PlayerPingRecord
        Public ReadOnly salt As UInteger
        Public ReadOnly time As Integer
        Public Sub New(ByVal salt As UInteger, ByVal time As Integer)
            Me.salt = salt
            Me.time = time
        End Sub
    End Class
    Public Enum W3PlayerLeaveTypes As Byte
        disc = 1
        lose = 7
        melee_lose = 8
        win = 9
        draw = 10
        obs = 11
        lobby = 13
    End Enum

    Public Interface IW3Player
        ReadOnly Property soul() As IW3PlayerPart
        ReadOnly Property lobby() As IW3PlayerLobby
        ReadOnly Property load_screen() As IW3PlayerLoadScreen
        ReadOnly Property gameplay() As IW3PlayerGameplay
        ReadOnly Property name() As String
        ReadOnly Property game() As IW3Game
        ReadOnly Property index() As Byte
        ReadOnly Property is_fake() As Boolean
        ReadOnly Property num_p2p_connections_P() As Integer
        ReadOnly Property canHost() As HostTestResults
        ReadOnly Property latency_P() As Double
        ReadOnly Property p2p_key() As UInteger
        ReadOnly Property listen_port() As UShort
        ReadOnly Property remote_ip_external() As Byte()
        ReadOnly Property remote_port_external() As UShort
        Property admin_tries() As Integer
        Property voted_to_start() As Boolean
        ReadOnly Property ready_to_play() As Boolean

        Function f_SendPacket(ByVal pk As W3Packet) As IFuture(Of Outcome)
        Function f_QueuePing(ByVal record As W3PlayerPingRecord) As IFuture
        Function disconnect_R(ByVal expected As Boolean, ByVal leave_type As W3PlayerLeaveTypes) As IFuture
    End Interface
    Public Interface IW3PlayerPart
        ReadOnly Property player() As IW3Player
        ReadOnly Property get_percent_dl() As Byte
        Sub receivePacket_L(ByVal id As W3PacketId, ByVal vals As Dictionary(Of String, Object))

        Function Description() As String
    End Interface
    Public Interface IW3PlayerLobby
        Inherits IW3PlayerPart

        Property getting_map_from_bot() As Boolean
        ReadOnly Property downloaded_map_size_P() As Integer

        Function f_BufferMap() As IFuture
        Function f_StartCountdown() As IFuture
        ReadOnly Property overcounted() As Boolean
    End Interface
    Public Interface IW3PlayerLoadScreen
        Inherits IW3PlayerPart
        Function f_Start() As IFuture
        Property ready() As Boolean
    End Interface
    Public Interface IW3PlayerGameplay
        Inherits IW3PlayerPart

        Function f_Start() As IFuture
        Function f_Stop() As IFuture
        ReadOnly Property tock_time() As Integer
        Function f_QueueTick(ByVal record As TickRecord) As IFuture
    End Interface
End Namespace