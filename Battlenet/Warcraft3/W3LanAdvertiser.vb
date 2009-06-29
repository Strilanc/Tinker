Imports HostBot.Warcraft3
Imports System.Net.Sockets
Imports HostBot.Links

Public Class W3LanAdvertiser
    Inherits NotifyingDisposable
    Implements IBotWidget

    Private ReadOnly games1 As New HashSet(Of LanGame)
    Private ReadOnly idmap As New Dictionary(Of UInteger, LanGame)
    Private ReadOnly headermap As New Dictionary(Of W3GameHeader, LanGame)
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

#Region "Inner"
    Private Class LanGame
        Public ReadOnly id As UInteger
        Public ReadOnly creation_time As Integer
        Public ReadOnly header As W3GameHeader
        Public Sub New(ByVal id As UInteger, ByVal header As W3GameHeader)
            Me.id = id
            Me.creation_time = Environment.TickCount
            Me.header = header
        End Sub
    End Class
    Private Class AdvertisingLinkMember
        Inherits NotifyingDisposable
        Implements IGameSourceSink

        Private ReadOnly parent As W3LanAdvertiser
        Private ReadOnly games As New HashSet(Of W3GameHeader)

        Private Event DisposedLink(ByVal sender As Links.IGameSource, ByVal partner As IGameSink) Implements IGameSource.DisposedLink
        Private Event AddedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal server As IW3Server) Implements IGameSource.AddedGame
        Private Event RemovedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal reason As String) Implements IGameSource.RemovedGame

        Public Sub New(ByVal parent As W3LanAdvertiser)
            Contract.Requires(parent IsNot Nothing)
            Me.parent = parent
            DisposeLink.CreateOneWayLink(parent, Me)
        End Sub

        Public Sub AddGame(ByVal game As W3GameHeader, ByVal server As IW3Server) Implements IGameSourceSink.AddGame
            If games.Contains(game) Then Return
            games.Add(game)
            parent.AddGame(game)
            RaiseEvent AddedGame(Me, game, server)
            If server IsNot Nothing Then
                server.f_OpenPort(parent.server_listen_port).CallWhenValueReady(
                    Sub(listened)
                                                                                    If Not listened.succeeded Then
                                                                                        RemoveGame(game, listened.Message)
                                                                                    End If
                                                                                End Sub
                )
            End If
        End Sub
        Public Sub RemoveGame(ByVal game As W3GameHeader, ByVal reason As String) Implements IGameSourceSink.RemoveGame
            If Not games.Contains(game) Then Return
            games.Remove(game)
            parent.RemoveGame(game)
            RaiseEvent RemovedGame(Me, game, reason)
        End Sub
        Public Sub SetAdvertisingOptions(ByVal [private] As Boolean) Implements IGameSourceSink.SetAdvertisingOptions
            'no distinction between public/private on lan
        End Sub

        Protected Overrides Sub PerformDispose()
            RaiseEvent DisposedLink(Me, Nothing)
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

    Private Sub _Stop() Implements IBotWidget.[Stop]
        Dispose()
    End Sub
    Protected Overrides Sub PerformDispose()
        SyncLock lock
            ClearGames()

            'stop sending data
            refresh_timer.Stop()
            socket.Close()
        End SyncLock

        'break links with other components
        parent.f_RemoveWidget(TYPE_NAME, name)

        If Me.pool_port IsNot Nothing Then
            logger.log("Returned port {0} to the pool.".frmt(Me.server_listen_port), LogMessageTypes.Positive)
            Me.pool_port.Dispose()
            Me.pool_port = Nothing
        End If

        'Log
        logger.log("Shutdown Advertiser", LogMessageTypes.Negative)
        RaiseEvent clear_state_strings()
    End Sub
#End Region

#Region "State"
    Public Function MakeAdvertisingLinkMember() As IGameSourceSink
        Contract.Ensures(Contract.Result(Of IGameSourceSink)() IsNot Nothing)
        Return New AdvertisingLinkMember(Me)
    End Function

    '''<summary>Adds a game to be advertised</summary>
    Public Function AddGame(ByVal gameHeader As W3GameHeader) As UInteger
        Dim id As UInteger
        Dim game As LanGame

        'Create
        SyncLock lock
            If headermap.ContainsKey(gameHeader) Then Return headermap(gameHeader).id
            create_count += CByte(1)
            id = create_count
            game = New LanGame(id, gameHeader)
            idmap(id) = game
            headermap(gameHeader) = game
            games1.Add(game)
        End SyncLock

        'Log
        logger.log("Added game " + game.header.name, LogMessageTypes.Positive)
        RaiseEvent add_state_string(id.ToString + "=" + gameHeader.name, False)

        Return id
    End Function

    '''<summary>Removes a game to be advertised.</summary>
    Public Function RemoveGame(ByVal id As UInteger) As Boolean
        Dim game As LanGame

        SyncLock lock
            'Remove
            If Not idmap.ContainsKey(id) Then Return False
            game = idmap(id)
            games1.Remove(game)
            idmap.Remove(id)
            headermap.Remove(game.header)

            'Notify
            Dim pk = W3Packet.MakeLanDestroyGame(id)
            send(pk, remote_host, remote_port)
        End SyncLock

        'Log
        logger.log("Removed game " + game.header.name, LogMessageTypes.Negative)
        RaiseEvent remove_state_string(game.id.ToString + "=" + game.header.name)
        Return True
    End Function
    Public Function RemoveGame(ByVal header As W3GameHeader) As Boolean
        Dim game As LanGame

        SyncLock lock
            'Remove
            If Not headermap.ContainsKey(header) Then Return False
            game = headermap(header)
        End SyncLock

        Return RemoveGame(game.id)
    End Function

    Public Sub ClearGames()
        SyncLock lock
            For Each game In games1.ToList
                RemoveGame(game.id)
            Next game
        End SyncLock
    End Sub
#End Region

#Region "Networking"
    '''<summary>Resends game data to the target address.</summary>
    Public Sub refresh() Handles refresh_timer.Elapsed
        SyncLock lock
            For Each game In games1
                Dim pk = W3Packet.MakeLanDescribeGame(
                                game.creation_time,
                                MainBot.Wc3MajorVersion,
                                game.id,
                                game.header,
                                server_listen_port)
                send(pk, remote_host, remote_port)
            Next game
        End SyncLock
    End Sub

    '''<summary>Sends a UDP packet to the specified remote host and port.</summary>
    Private Sub send(ByVal pk As W3Packet, ByVal remote_host As String, ByVal remote_port As UShort)
        Try
            'pack
            Dim data = pk.payload.Data.ToArray()
            data = Concat({New Byte() {W3Packet.PACKET_PREFIX, pk.id}, CUShort(data.Length + 4).bytes(ByteOrder.LittleEndian), data})

            'Log
            logger.log(Function() "Sending {0} to {1}: {2}".frmt(pk.id, remote_host, remote_port), LogMessageTypes.DataEvent)
            logger.log(pk.payload.Description, LogMessageTypes.DataParsed)
            logger.log(Function() "Sending {0} to {1}: {2}".frmt(data.ToHexString, remote_host, remote_port), LogMessageTypes.DataRaw)

            'Send
            socket.Send(data, data.Length, remote_host, remote_port)

        Catch e As Pickling.PicklingException
            'Ignore
            logger.log("Error packing {0}: {1} (skipped)".frmt(pk.id, e.Message), LogMessageTypes.Negative)
        Catch e As Exception
            'Fail
            logger.log("Error sending {0}: {1}".frmt(pk.id, e.Message()), LogMessageTypes.Problem)
            Logging.LogUnexpectedException("Exception rose past {0}.send".frmt(Me.GetType.Name), e)
        End Try
    End Sub
#End Region

#Region "IBotWidget"
    Private Event add_state_string(ByVal state As String, ByVal insert_at_top As Boolean) Implements IBotWidget.AddStateString
    Private Event remove_state_string(ByVal state As String) Implements IBotWidget.RemoveStateString
    Private Event clear_state_strings() Implements IBotWidget.ClearStateStrings
    Private Sub command(ByVal text As String) Implements IBotWidget.ProcessCommand
        parent.LanCommands.ProcessLocalText(Me, text, logger)
    End Sub
    Private Sub hooked() Implements IBotWidget.Hooked
        Dim game_names As List(Of String)
        SyncLock lock
            game_names = (From game In games1 Select "{0}={1}".frmt(game.id, game.header.name)).ToList
        End SyncLock
        For Each game_name In game_names
            RaiseEvent add_state_string(game_name, False)
        Next game_name
    End Sub
    Private ReadOnly Property _logger() As Logger Implements IBotWidget.Logger
        Get
            Return logger
        End Get
    End Property
    Private ReadOnly Property _name() As String Implements IBotWidget.Name
        Get
            Return name
        End Get
    End Property
    Private ReadOnly Property _typeName() As String Implements IBotWidget.TypeName
        Get
            Return TYPE_NAME
        End Get
    End Property
#End Region
End Class
