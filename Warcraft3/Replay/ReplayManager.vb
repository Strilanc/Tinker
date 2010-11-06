Namespace WC3.Replay
    ''' <summary>
    ''' Wires a Game to a ReplayWriter.
    ''' </summary>
    Public Class ReplayManager
        Inherits DisposableWithTask

        Private ReadOnly inQueue As CallQueue = New TaskedCallQueue
        Private ReadOnly _writer As ReplayWriter
        Private ReadOnly _infoProvider As IProductInfoProvider
        Private ReadOnly _hooks As New List(Of IDisposable)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_writer IsNot Nothing)
            Contract.Invariant(_infoProvider IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
        End Sub

        Private Sub New(ByVal writer As ReplayWriter,
                        ByVal infoProvider As IProductInfoProvider)
            Contract.Requires(writer IsNot Nothing)
            Contract.Requires(infoProvider IsNot Nothing)
            Me._writer = writer
            Me._infoProvider = infoProvider
        End Sub

        Private Sub Wire(ByVal game As Game)
            Contract.Requires(game IsNot Nothing)

            Dim tickHandler As Game.TickEventHandler =
                    Sub(sender, duration, actualActions, visibleActions) inQueue.QueueAction(Sub() OnTick(duration, visibleActions))
            Dim chatHandler As Game.PlayerTalkedEventHandler =
                    Sub(sender, speaker, text, receivers) inQueue.QueueAction(Sub() OnChat(speaker, text, receivers))
            Dim leaveHandler As Game.PlayerLeftEventHandler =
                    Sub(sender, gameState, leaver, leaveType, reason) inQueue.QueueAction(Sub() Onleave(leaver, leaveType))
            Dim launchHandler As Game.RecordGameStartedEventHandler =
                    Sub(sender) inQueue.QueueAction(Sub() _writer.WriteEntry(MakeGameStarted()))

            AddHandler game.Tick, tickHandler
            AddHandler game.PlayerTalked, chatHandler
            AddHandler game.PlayerLeft, leaveHandler
            AddHandler game.RecordGameStarted, launchHandler

            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler game.Tick, tickHandler))
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler game.PlayerTalked, chatHandler))
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler game.PlayerLeft, leaveHandler))
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler game.RecordGameStarted, launchHandler))

            game.ChainEventualDisposalTo(Me)
        End Sub

        Public Shared Function StartRecordingFrom(ByVal defaultFileName As String,
                                                  ByVal game As Game,
                                                  ByVal players As IEnumerable(Of Player),
                                                  ByVal slots As IEnumerable(Of Slot),
                                                  ByVal randomSeed As UInt32,
                                                  ByVal infoProvider As IProductInfoProvider) As ReplayManager
            Contract.Requires(game IsNot Nothing)
            Contract.Requires(players IsNot Nothing)
            Contract.Requires(slots IsNot Nothing)

            'Choose location
            Dim folder = GetDataFolderPath("Replays")
            If defaultFileName Is Nothing Then
                defaultFileName = "{0}, {1}".Frmt(game.Settings.GameDescription.Name,
                                                  DateTime.Now().ToString("MMM d, yyyy, H:mm:ss tt", CultureInfo.InvariantCulture))
            End If

            'Strip invalid characters
            defaultFileName = (From c In defaultFileName
                               Select If(IO.Path.GetInvalidFileNameChars.Contains(c), "."c, c)
                               ).AsString

            'Append a number if necessary
            Dim filename = IO.Path.Combine(folder, defaultFileName + ".w3g")
            Dim i = 1
            While IO.File.Exists(filename)
                i += 1
                filename = IO.Path.Combine(folder, "{0} - {1}.w3g".Frmt(defaultFileName, i))
            End While

            'Start
            Dim file = New IO.FileStream(filename, IO.FileMode.CreateNew, IO.FileAccess.Write, IO.FileShare.None)
            Contract.Assume(file.CanWrite)
            Contract.Assume(file.CanSeek)
            Dim writer = New Replay.ReplayWriter(stream:=file.AsRandomWritableStream,
                                                 settings:=ReplaySettings.Online,
                                                 wc3Version:=infoProvider.MajorVersion,
                                                 replayVersion:=My.Settings.ReplayBuildNumber)

            Dim playerCount = CUInt(players.Count)
            If playerCount <= 0 Then Throw New ArgumentOutOfRangeException("players", "No players.")
            If playerCount > 12 Then Throw New ArgumentOutOfRangeException("players", "Too many players.")
            Dim primaryPlayer = players.First.AssumeNotNull
            Dim secondaryPlayers = players.Skip(1)
            writer.WriteEntry(MakeStartOfReplay(primaryPlayer.Id,
                                                primaryPlayer.Name,
                                                primaryPlayer.PeerData,
                                                game.Settings.GameDescription.Name,
                                                game.Settings.GameDescription.GameStats,
                                                playerCount,
                                                game.Settings.GameDescription.GameType))

            For Each player In secondaryPlayers
                Contract.Assume(player IsNot Nothing)
                writer.WriteEntry(MakePlayerJoined(player.Id, player.Name, player.PeerData))
            Next player

            writer.WriteEntry(MakeLobbyState(slots,
                                             randomSeed,
                                             game.Settings.Map.LayoutStyle,
                                             CByte(game.Settings.Map.LobbySlots.Count)))

            writer.WriteEntry(MakeLoadStarted1())
            writer.WriteEntry(MakeLoadStarted2())

            'Construct
            Dim result = New ReplayManager(writer, infoProvider)
            result.Wire(game)
            Return result
        End Function

        Private Sub OnTick(ByVal duration As UShort,
                           ByVal visibleActionStreaks As IReadableList(Of IReadableList(Of Protocol.PlayerActionSet)))
            Contract.Requires(visibleActionStreaks IsNot Nothing)
            For Each visibleActionStreak In visibleActionStreaks.SkipLast(1)
                Contract.Assume(visibleActionStreak IsNot Nothing)
                _writer.WriteEntry(MakeTickPreOverflow(visibleActionStreak))
            Next visibleActionStreak
            _writer.WriteEntry(MakeTick(duration, If(visibleActionStreaks.LastOrDefault,
                                                     New Protocol.PlayerActionSet() {}.AsReadableList)))
        End Sub
        Private Sub OnChat(ByVal speaker As Player,
                           ByVal text As String,
                           ByVal receivingGroup As Protocol.ChatGroup?)
            Contract.Requires(speaker IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            If receivingGroup Is Nothing Then
                _writer.WriteEntry(MakeLobbyChatMessage(speaker.Id, text))
            Else
                _writer.WriteEntry(MakeGameChatMessage(speaker.Id, text, receivingGroup.Value))
            End If
        End Sub
        Private Sub Onleave(ByVal leaver As Player,
                            ByVal reportedResult As Protocol.PlayerLeaveReason)
            Contract.Requires(leaver IsNot Nothing)
            _writer.WriteEntry(MakePlayerLeft(0, leaver.Id, reportedResult, 0))
        End Sub

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As Task
            If finalizing Then Return Nothing
            Return inQueue.QueueFunc(
                Function()
                    _writer.Dispose()
                    For Each hook In _hooks
                        hook.Dispose()
                    Next
                    Return _writer.DisposalTask
                End Function).Unwrap
        End Function
    End Class
End Namespace
