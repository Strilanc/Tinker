Namespace WC3
    'Verification disabled because of many warnings in generated code
    <ContractVerification(False)>
    Public Class W3ServerControl
        Private ReadOnly inQueue As CallQueue = MakeControlCallQueue(Me)
        Private ReadOnly _manager As WC3.GameServerManager
        Private ReadOnly _server As GameServer
        Private ReadOnly _hooks As New List(Of Task(Of IDisposable))
        Private ReadOnly _games As New Dictionary(Of Game, WC3.GameManager)
        Private ReadOnly _gameSets As New List(Of GameSet)
        Private ReadOnly gameTabs As Components.TabManager

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_manager IsNot Nothing)
            Contract.Invariant(_server IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
            Contract.Invariant(_games IsNot Nothing)
            Contract.Invariant(_gameSets IsNot Nothing)
            Contract.Invariant(gameTabs IsNot Nothing)
        End Sub

        Private Shadows Sub OnDisposed(sender As Object, e As System.EventArgs) Handles Me.Disposed
            _hooks.DisposeAllAsync()
        End Sub

        Public Sub New(manager As WC3.GameServerManager)
            Contract.Assert(manager IsNot Nothing)
            InitializeComponent()
            'if games is nothing then games = new TabControlIHookableSet(tabsserver)

            Me._manager = manager
            Me._server = manager.Server
            gameTabs = New Components.TabManager(Me.tabsServer)

            Me.txtInfo.Text = ""
            logServer.SetLogger(_server.Logger, "Server")
            'Dim map = child.Settings.Map

            'Dim info = "Map Name\n{0}\n\n" +
            '"Relative Path\n{1}\n\n" +
            '"Map Type\n{2}\n\n" +
            '"Player Count\n{3}\n\n" +
            '"Playable Size\n{4} x {5}\n\n" +
            '"File Size\n{6:###,###,###,###} bytes\n\n" +
            '"File Checksum (crc32)\n{7}\n\n" +
            '"Map Checksum (xoro)\n{8}\n\n" +
            '"Map Checksum (sha1)\n{9}\n"
            'info = info.Replace("\n", Environment.NewLine)
            'info = info.Frmt(map.name,
            'map.RelativePath,
            'If(map.isMelee, "Melee", "Custom"),
            'map.NumPlayerSlots,
            'map.playableWidth,
            'map.playableHeight,
            'map.FileSize,
            'map.FileChecksumCRC32.Bytes.ToHexString,
            'map.MapChecksumXORO.Bytes.ToHexString,
            'map.MapChecksumSHA1.ToHexString)
            'txtInfo.Text = info

            _hooks.Add(_server.ObserveGameSets(AddressOf OnAddedGameSet, AddressOf OnRemovedGameSet))
            _hooks.Add(_server.ObserveGames(AddressOf OnAddedGame, AddressOf OnRemovedGame))

            BeginUpdateStateDisplay()
        End Sub

        Private Async Sub BeginUpdateStateDisplay()
            Dim port = Await Me._manager.QueueGetListenPort()
            UpdateStateDisplay(port)
        End Sub
        Private Sub UpdateStateDisplay(port As UShort)
            If IsDisposed Then Return
            Dim s = New System.Text.StringBuilder()
            s.AppendLine("Listen Port: {0}".Frmt(port))
            s.AppendLine("Games: {0}".Frmt(_gameSets.Count))
            For Each game In _gameSets
                s.AppendLine("")
                s.AppendLine(game.GameSettings.GameDescription.Name)
                s.AppendLine("ID: {0}".Frmt(game.GameSettings.GameDescription.GameId))
                s.AppendLine("Map: {0}".Frmt(game.GameSettings.GameDescription.GameStats.AdvertisedPath))
            Next game
            txtInfo.Text = s.ToString
        End Sub

        Private Sub OnCommand(sender As CommandControl, argument As String) Handles comServer.IssuedCommand
            Contract.Requires(argument IsNot Nothing)
            Tinker.Components.UIInvokeCommand(_manager, argument)
        End Sub

        Private Sub OnAddedGame(gameSet As GameSet, game As Game)
            inQueue.QueueAction(Sub()
                                    _games(game) = New WC3.GameManager(game.Name, _manager.Bot, game)
                                    gameTabs.Add(_games(game))
                                    BeginUpdateStateDisplay()
                                End Sub)
        End Sub
        Private Sub OnRemovedGame(gameSet As GameSet, game As Game)
            inQueue.QueueAction(Sub()
                                    gameTabs.Remove(_games(game))
                                    _games.Remove(game)
                                    BeginUpdateStateDisplay()
                                End Sub)
        End Sub
        Private Sub OnAddedGameSet(gameSet As GameSet)
            inQueue.QueueAction(Sub()
                                    _gameSets.Add(gameSet)
                                    BeginUpdateStateDisplay()
                                End Sub)
        End Sub
        Private Sub OnRemovedGameSet(gameSet As GameSet)
            inQueue.QueueAction(Sub()
                                    _gameSets.Remove(gameSet)
                                    BeginUpdateStateDisplay()
                                End Sub)
        End Sub
    End Class
End Namespace
