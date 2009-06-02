Imports HostBot.Warcraft3
Imports System.Net.Sockets
Imports HostBot.Links

Public Class W3LanAdvertiser
    Implements IBotWidget
    Implements IDependencyLinkServant

    Private ReadOnly games As New Dictionary(Of UInteger, LanGame)
    Private ReadOnly socket As UdpClient
    Public Const TYPE_NAME As String = "LanAdvertiser"

    Private ReadOnly logger As Logger
    Private ReadOnly name As String

    Private ReadOnly server_listen_port As UShort
    Private ReadOnly remote_host As String
    Private ReadOnly remote_port As UShort
    Private ReadOnly lock As New Object()
    Private ReadOnly parent As MainBot
    Private pool_port As PortPool.PortHandle

    Private create_count As UInteger = 0
    Private WithEvents refresh_timer As New System.Timers.Timer(3000)

    Public Event killed() Implements IDependencyLinkServant.Closed

#Region "Inner"
    Private Class LanGame
        Public ReadOnly id As UInteger
        Public ReadOnly map As W3Map
        Public ReadOnly name As String
        Public ReadOnly creation_time As Integer
        Public ReadOnly map_settings As W3Map.MapSettings
        Public Sub New(ByVal name As String, ByVal id As UInteger, ByVal map As W3Map, ByVal map_settings As W3Map.MapSettings)
            Me.id = id
            Me.name = name
            Me.map = map
            Me.creation_time = Environment.TickCount
            Me.map_settings = map_settings
        End Sub
    End Class
    Private Class AdvertisingLinkMember
        Implements IAdvertisingLinkMember

        Private WithEvents parent As W3LanAdvertiser
        Private gameid As UInteger = 0

        Public Sub New(ByVal parent As W3LanAdvertiser)
            Me.parent = parent
        End Sub

        Private Event break(ByVal sender As Links.IAdvertisingLinkMember, ByVal partner As Links.IAdvertisingLinkMember) Implements Links.IAdvertisingLinkMember.break
        Private Event started_advertising(ByVal sender As Links.IAdvertisingLinkMember, ByVal server As IW3Server, ByVal name As String, ByVal map As Warcraft3.W3Map, ByVal options As System.Collections.Generic.IList(Of String)) Implements Links.IAdvertisingLinkMember.started_advertising
        Private Event stopped_advertising(ByVal sender As Links.IAdvertisingLinkMember, ByVal reason As String) Implements Links.IAdvertisingLinkMember.stopped_advertising

        Public Sub start_advertising(ByVal server As IW3Server, ByVal name As String, ByVal map As Warcraft3.W3Map, ByVal options As IList(Of String)) Implements Links.IAdvertisingLinkMember.start_advertising
            If gameid <> 0 Then Return
            gameid = parent.add_game(name, map, New W3Map.MapSettings(options))
            RaiseEvent started_advertising(Me, server, name, map, options)
            If server IsNot Nothing Then
                Dim listened = server.f_OpenPort(parent.server_listen_port)
                FutureSub.Call(listened, AddressOf server_listened_result)
            End If
        End Sub
        Private Sub server_listened_result(ByVal listened As Outcome)
            If Not listened.succeeded Then
                stop_advertising(listened.message)
            End If
        End Sub
        Public Sub stop_advertising(ByVal reason As String) Implements Links.IAdvertisingLinkMember.stop_advertising
            If gameid = 0 Then Return
            parent.remove_game(gameid)
            gameid = 0
            RaiseEvent stopped_advertising(Me, reason)
        End Sub
        Public Sub set_advertising_options(ByVal [private] As Boolean) Implements Links.IAdvertisingLinkMember.set_advertising_options
            'no distinction between public/private on lan
        End Sub

        Private Sub parent_killed() Handles parent.killed
            RaiseEvent break(Me, Nothing)
        End Sub
    End Class
#End Region

#Region "Life"
    Public Sub New(ByVal parent As MainBot,
                   ByVal name As String,
                   ByVal server_listen_pool_port As PortPool.PortHandle,
                   Optional ByVal remote_host As String = "localhost",
                   Optional ByVal remote_port As UShort = 6112,
                   Optional ByVal logger As Logger = Nothing)
        Me.New(parent, name, server_listen_pool_port.port, remote_host, remote_port, logger)
        Me.server_listen_port = server_listen_pool_port.port
        Me.pool_port = server_listen_pool_port
    End Sub
    Public Sub New(ByVal parent As MainBot,
                   ByVal name As String,
                   ByVal server_listen_port As UShort,
                   Optional ByVal remote_host As String = "localhost",
                   Optional ByVal remote_port As UShort = 6112,
                   Optional ByVal logger As Logger = Nothing)
        Me.logger = If(logger, New Logger)
        Me.name = name
        Me.parent = parent
        Me.remote_host = remote_host
        Me.remote_port = remote_port
        Me.server_listen_port = server_listen_port
        Me.socket = New UdpClient()
        Me.refresh_timer.Start()
    End Sub

    Public Sub Kill() Implements IBotWidget.stop, IDependencyLinkServant.close
        SyncLock lock
            clear_games()

            'stop sending data
            refresh_timer.Stop()
            socket.Close()
        End SyncLock

        'break links with other components
        parent.remove_widget_R(TYPE_NAME, name)

        If Me.pool_port IsNot Nothing Then
            logger.log("Returned port {0} to the pool.".frmt(Me.server_listen_port), LogMessageTypes.Positive)
            Me.pool_port.Dispose()
            Me.pool_port = Nothing
        End If

        'Log
        logger.log("Shutdown Advertiser", LogMessageTypes.Negative)
        RaiseEvent clear_state_strings()
        RaiseEvent killed()
    End Sub
#End Region

#Region "State"
    Public Function make_advertising_link_member() As IAdvertisingLinkMember
        Return New AdvertisingLinkMember(Me)
    End Function

    '''<summary>Adds a game to be advertised</summary>
    Public Function add_game(ByVal name As String, ByVal map As W3Map, ByVal map_settings As W3Map.MapSettings) As UInteger
        Dim id As UInteger
        Dim game As LanGame

        'Create
        SyncLock lock
            create_count += CByte(1)
            id = create_count
            game = New LanGame(name, id, map, map_settings)
            games(id) = game
        End SyncLock

        'Log
        logger.log("Added game " + game.name, LogMessageTypes.Positive)
        RaiseEvent add_state_string(id.ToString + "=" + name, False)

        Return id
    End Function

    '''<summary>Removes a game to be advertised.</summary>
    Public Function remove_game(ByVal id As UInteger) As Boolean
        Dim game As LanGame

        SyncLock lock
            'Remove
            If Not games.ContainsKey(id) Then Return False
            game = games(id)
            games.Remove(id)

            'Notify
            Dim pk = W3Packet.MakePacket_LAN_DESTROY_GAME(id)
            send(pk, remote_host, remote_port)
        End SyncLock

        'Log
        logger.log("Removed game " + game.name, LogMessageTypes.Negative)
        RaiseEvent remove_state_string(game.id.ToString + "=" + game.name)
        Return True
    End Function

    Public Sub clear_games()
        SyncLock lock
            For Each game In games.Values.ToList
                remove_game(game.id)
            Next game
        End SyncLock
    End Sub
#End Region

#Region "Networking"
    '''<summary>Resends game data to the target address.</summary>
    Public Sub refresh() Handles refresh_timer.Elapsed
        SyncLock lock
            For Each game In games.Values
                Dim pk = W3Packet.MakePacket_LAN_DESCRIBE_GAME( _
                                server_listen_port,
                                game.creation_time,
                                game.name,
                                My.Resources.ProgramName,
                                MainBot.Wc3MajorVersion,
                                game.id,
                                game.map,
                                game.map_settings)
                send(pk, remote_host, remote_port)
            Next game
        End SyncLock
    End Sub

    '''<summary>Sends a UDP packet to the specified remote host and port.</summary>
    Private Sub send(ByVal pk As W3Packet, ByVal remote_host As String, ByVal remote_port As UShort)
        Try
            'pack
            Dim data = pk.payload.getData.ToArray()
            data = concat({W3Packet.PACKET_PREFIX, pk.id}, CUShort(data.Length + 4).bytes(ByteOrder.LittleEndian), data)

            'Log
            logger.log(Function() "Sending {0} to {1}: {2}".frmt(pk.id, remote_host, remote_port), LogMessageTypes.DataEvent)
            logger.log(Function() pk.payload.toString(), LogMessageTypes.DataParsed)
            logger.log(Function() "Sending {0} to {1}: {2}".frmt(unpackHexString(data), remote_host, remote_port), LogMessageTypes.DataRaw)

            'Send
            socket.Send(data, data.Length, remote_host, remote_port)

        Catch e As Pickling.PicklingException
            'Ignore
            logger.log("Error packing {0}: {1} (skipped)".frmt(pk.id, e.Message), LogMessageTypes.Negative)
        Catch e As Exception
            'Fail
            logger.log("Error sending {0}: {1}".frmt(pk.id, e.Message()), LogMessageTypes.Problem)
            Logging.logUnexpectedException("Exception rose past {0}.send".frmt(Me.GetType.Name), e)
        End Try
    End Sub
#End Region

#Region "IBotWidget"
    Private Event add_state_string(ByVal state As String, ByVal insert_at_top As Boolean) Implements IBotWidget.add_state_string
    Private Event remove_state_string(ByVal state As String) Implements IBotWidget.remove_state_string
    Private Event clear_state_strings() Implements IBotWidget.clear_state_strings
    Private Sub command(ByVal text As String) Implements IBotWidget.command
        parent.lan_commands.processLocalText(Me, text, logger)
    End Sub
    Private Sub hooked() Implements IBotWidget.hooked
        Dim game_names As List(Of String)
        SyncLock lock
            game_names = (From game In games.Values Select "{0}={1}".frmt(game.id, game.name)).ToList
        End SyncLock
        For Each game_name In game_names
            RaiseEvent add_state_string(game_name, False)
        Next game_name
    End Sub
    Private Function get_logger() As Logger Implements IBotWidget.logger
        Return logger
    End Function
    Private Function get_name() As String Implements IBotWidget.name
        Return name
    End Function
    Private Function get_type_name() As String Implements IBotWidget.type_name
        Return TYPE_NAME
    End Function
#End Region
End Class
