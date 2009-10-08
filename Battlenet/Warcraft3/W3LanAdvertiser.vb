Imports HostBot.Commands
Imports HostBot.Warcraft3
Imports System.Net.Sockets
Imports HostBot.Links

Public NotInheritable Class W3LanAdvertiser
    Inherits FutureDisposable
    Implements IBotWidget

    Private ReadOnly games1 As New HashSet(Of LanGame)
    Private ReadOnly idmap As New Dictionary(Of UInteger, LanGame)
    Private ReadOnly headermap As New Dictionary(Of W3GameHeader, LanGame)
    Private ReadOnly socket As UdpClient
    Public Const WidgetTypeName As String = "LanAdvertiser"

    Private ReadOnly logger As Logger
    Public ReadOnly name As String

    Public ReadOnly serverListenPort As UShort
    Private ReadOnly remoteHost As String
    Private ReadOnly remotePort As UShort
    Private ReadOnly lock As New Object()
    Public ReadOnly parent As MainBot
    Private portHandle As PortPool.PortHandle

    Private createCount As UInteger
    Private WithEvents refreshTimer As New System.Timers.Timer(3000)


    <ContractInvariantMethod()> Private Sub ObjectInvariant()
        Contract.Invariant(refreshTimer IsNot Nothing)
        Contract.Invariant(name IsNot Nothing)
        Contract.Invariant(parent IsNot Nothing)
        Contract.Invariant(createCount >= 0)
        Contract.Invariant(logger IsNot Nothing)
    End Sub

#Region "Inner"
    Private NotInheritable Class LanGame
        Public ReadOnly id As UInteger
        Public ReadOnly creation_time As Integer
        Public ReadOnly header As W3GameHeader
        Public Sub New(ByVal id As UInteger, ByVal header As W3GameHeader)
            Me.id = id
            Me.creation_time = Environment.TickCount
            Me.header = header
        End Sub
    End Class
    Private NotInheritable Class AdvertisingLinkMember
        Inherits FutureDisposable
        Implements IGameSourceSink

        Private ReadOnly parent As W3LanAdvertiser
        Private ReadOnly games As New HashSet(Of W3GameHeader)

        Private Event DisposedLink(ByVal sender As Links.IGameSource, ByVal partner As IGameSink) Implements IGameSource.DisposedLink
        Private Event AddedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal server As W3Server) Implements IGameSource.AddedGame
        Private Event RemovedGame(ByVal sender As IGameSource, ByVal game As W3GameHeader, ByVal reason As String) Implements IGameSource.RemovedGame

        Public Sub New(ByVal parent As W3LanAdvertiser)
            'contract bug wrt interface event implementation requires this:
            'Contract.Requires(parent IsNot Nothing)
            Contract.Assume(parent IsNot Nothing)
            Me.parent = parent
            DisposeLink.CreateOneWayLink(parent, Me)
        End Sub

        Public Sub AddGame(ByVal game As W3GameHeader, ByVal server As W3Server) Implements IGameSourceSink.AddGame
            If games.Contains(game) Then Return
            games.Add(game)
            parent.AddGame(game)
            RaiseEvent AddedGame(Me, game, server)
            If server IsNot Nothing Then
                server.QueueOpenPort(parent.serverListenPort).CallWhenReady(
                    Sub(listenException)
                        If listenException IsNot Nothing Then
                            RemoveGame(game, reason:=listenException.Message)
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

        Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
            If Not finalizing Then
                RaiseEvent DisposedLink(Me, Nothing)
            End If
        End Sub
    End Class
#End Region

#Region "Life"
    Public Sub New(ByVal parent As MainBot,
                   ByVal name As String,
                   ByVal serverListenPortHandle As PortPool.PortHandle,
                   Optional ByVal remoteHostName As String = "localhost",
                   Optional ByVal remotePort As UShort = 6112,
                   Optional ByVal logger As Logger = Nothing)
        Me.New(parent, name, serverListenPortHandle.Port, remoteHostName, remotePort, logger)
        Me.serverListenPort = serverListenPortHandle.Port
        Me.portHandle = serverListenPortHandle
    End Sub
    Public Sub New(ByVal parent As MainBot,
                   ByVal name As String,
                   ByVal serverListenPort As UShort,
                   Optional ByVal remoteHostName As String = "localhost",
                   Optional ByVal remotePort As UShort = 6112,
                   Optional ByVal logger As Logger = Nothing)
        Me.logger = If(logger, New Logger)
        Me.name = name
        Me.parent = parent
        Me.remoteHost = remoteHostName
        Me.remotePort = remotePort
        Me.serverListenPort = serverListenPort
        Me.socket = New UdpClient()
        Me.refreshTimer.Start()
    End Sub

    Private Sub _Stop() Implements IBotWidget.[Stop]
        Dispose()
    End Sub
    Protected Overrides Sub PerformDispose(ByVal finalizing As Boolean)
        If Not finalizing Then
            SyncLock lock
                ClearGames()

                'stop sending data
                refreshTimer.Stop()
                socket.Close()
            End SyncLock

            'break links with other components
            parent.QueueRemoveWidget(WidgetTypeName, name)

            If Me.portHandle IsNot Nothing Then
                logger.Log("Returned port {0} to the pool.".Frmt(Me.serverListenPort), LogMessageType.Positive)
                Me.portHandle.Dispose()
                Me.portHandle = Nothing
            End If

            'Log
            logger.Log("Shutdown Advertiser", LogMessageType.Negative)
            RaiseEvent ClearStateStrings()
        End If
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
            createCount += CByte(1)
            id = createCount
            game = New LanGame(id, gameHeader)
            idmap(id) = game
            headermap(gameHeader) = game
            games1.Add(game)
        End SyncLock

        'Log
        logger.Log("Added game " + game.header.Name, LogMessageType.Positive)
        RaiseEvent AddStateString("{0}={1}".Frmt(id, gameHeader.Name), False)

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
            send(pk, remoteHost, remotePort)
        End SyncLock

        'Log
        logger.Log("Removed game " + game.header.Name, LogMessageType.Negative)
        RaiseEvent RemoveStateString("{0}={1}".Frmt(game.id, game.header.Name))
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
    Public Sub Refresh() Handles refreshTimer.Elapsed
        SyncLock lock
            For Each game In games1
                Dim pk = W3Packet.MakeLanDescribeGame(
                                game.creation_time,
                                MainBot.WC3MajorVersion,
                                game.id,
                                game.header,
                                serverListenPort)
                send(pk, remoteHost, remotePort)
            Next game
        End SyncLock
    End Sub

    '''<summary>Sends a UDP packet to the specified remote host and port.</summary>
    Private Sub send(ByVal pk As W3Packet, ByVal remote_host As String, ByVal remote_port As UShort)
        Try
            'pack
            Dim data = pk.Payload.Data.ToArray()
            data = Concat({W3Packet.PacketPrefixValue, pk.id}, CUShort(data.Length + 4).Bytes(), data)

            'Log
            logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(pk.id, remote_host, remote_port), LogMessageType.DataEvent)
            logger.Log(pk.Payload.Description, LogMessageType.DataParsed)
            logger.Log(Function() "Sending {0} to {1}: {2}".Frmt(data.ToHexString, remote_host, remote_port), LogMessageType.DataRaw)

            'Send
            socket.Send(data, data.Length, remote_host, remote_port)

        Catch e As Pickling.PicklingException
            'Ignore
            logger.Log("Error packing {0}: {1} (skipped)".Frmt(pk.id, e), LogMessageType.Negative)
        Catch e As Exception
            'Fail
            logger.Log("Error sending {0}: {1}".Frmt(pk.id, e), LogMessageType.Problem)
            e.RaiseAsUnexpected("Exception rose past {0}.send".Frmt(Me.GetType.Name))
        End Try
    End Sub
#End Region

#Region "IBotWidget"
    Private Event AddStateString(ByVal state As String, ByVal insert_at_top As Boolean) Implements IBotWidget.AddStateString
    Private Event RemoveStateString(ByVal state As String) Implements IBotWidget.RemoveStateString
    Private Event ClearStateStrings() Implements IBotWidget.ClearStateStrings
    Private Sub command(ByVal text As String) Implements IBotWidget.ProcessCommand
        parent.LanCommands.ProcessLocalText(Me, text, logger)
    End Sub
    Private Sub hooked() Implements IBotWidget.Hooked
        Dim game_names As List(Of String)
        SyncLock lock
            game_names = (From game In games1 Select "{0}={1}".Frmt(game.id, game.header.Name)).ToList
        End SyncLock
        For Each game_name In game_names
            RaiseEvent AddStateString(game_name, False)
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
            Return WidgetTypeName
        End Get
    End Property
#End Region
End Class
