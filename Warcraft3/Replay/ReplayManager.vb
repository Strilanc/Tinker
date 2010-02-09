Namespace WC3.Replay
    Public Class ReplayManager
        Inherits FutureDisposable

        Private ReadOnly inQueue As ICallQueue = New TaskedCallQueue
        Private ReadOnly _writer As ReplayWriter
        Private ReadOnly _hooks As New List(Of IDisposable)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(inQueue IsNot Nothing)
            Contract.Invariant(_writer IsNot Nothing)
            Contract.Invariant(_hooks IsNot Nothing)
        End Sub

        Private Sub New(ByVal writer As ReplayWriter)
            Contract.Requires(writer IsNot Nothing)
            Me._writer = writer
        End Sub

        Private Sub Wire(ByVal game As Game)
            Dim tickHandler As Game.TickEventHandler =
                    Sub(sender, duration, actions) inQueue.QueueAction(Sub() OnTick(duration, actions))
            Dim chatHandler As Game.PlayerTalkedEventHandler =
                    Sub(sender, speaker, text, receivers) inQueue.QueueAction(Sub() OnChat(speaker, text, receivers))
            Dim leaveHandler As Game.PlayerLeftEventHandler =
                    Sub(sender, gameState, leaver, leaveType, reason) inQueue.QueueAction(Sub() Onleave(leaver, leaveType))
            Dim launchHandler As Game.LaunchedEventHandler =
                    Sub(sender, usingLoadInGame) inQueue.QueueAction(Sub() _writer.AddGameStarted())

            AddHandler Game.Tick, tickHandler
            AddHandler Game.PlayerTalked, chatHandler
            AddHandler Game.PlayerLeft, leaveHandler
            AddHandler Game.Launched, launchHandler

            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler Game.Tick, tickHandler))
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler Game.PlayerTalked, chatHandler))
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler Game.PlayerLeft, leaveHandler))
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler Game.Launched, launchHandler))

            Game.FutureDisposed.CallWhenReady(Sub() Me.Dispose())
        End Sub

        Public Shared Function StartRecordingFrom(ByVal game As Game,
                                                  ByVal players As IEnumerable(Of Player),
                                                  ByVal slots As IEnumerable(Of Slot),
                                                  ByVal randomSeed As UInt32) As ReplayManager
            'Choose location
            Dim folder = GetDataFolderPath("Replays")
            Dim baseFilename = "{0} - {1}".Frmt(game.Settings.GameDescription.Name, DateTime.Now().ToString("MMM d, yyyy"))
            Dim filename = IO.Path.Combine(folder, baseFilename + ".w3g")
            Dim i = 0
            While IO.File.Exists(filename)
                i += 1
                filename = IO.Path.Combine(folder, baseFilename + " - {0}.w3g".Frmt(i))
            End While

            'Start
            Dim writer = New Replay.ReplayWriter(stream:=New IO.FileStream(filename, IO.FileMode.CreateNew, IO.FileAccess.Write, IO.FileShare.None).AsRandomWritableStream,
                                                 wc3Version:=New CachedExternalValues().WC3MajorVersion,
                                                 wc3BuildNumber:=New CachedExternalValues().WC3BuildNumber,
                                                 host:=players.First,
                                                 players:=players.Skip(1),
                                                 gameDesc:=game.Settings.GameDescription,
                                                 Map:=game.Map,
                                                 slots:=slots,
                                                 randomSeed:=randomSeed)

            'Construct
            Dim result = New ReplayManager(writer)
            result.Wire(game)
            Return result
        End Function

        Private Sub OnTick(ByVal duration As UShort,
                           ByVal actions As IReadableList(Of Tuple(Of Player, Protocol.PlayerActionSet)))
            Contract.Requires(actions IsNot Nothing)
            _writer.AddTick(duration, (From action In actions Select action.Item2).ToArray.AsReadableList)
        End Sub
        Private Sub OnChat(ByVal speaker As Player,
                           ByVal text As String,
                           ByVal receivers As Protocol.ChatReceiverType?)
            Contract.Requires(speaker IsNot Nothing)
            Contract.Requires(text IsNot Nothing)
            If receivers Is Nothing Then
                _writer.AddLobbyChatMessage(speaker.PID, text)
            Else
                _writer.AddGameChatMessage(speaker.PID, text, receivers.Value)
            End If
        End Sub
        Private Sub Onleave(ByVal leaver As Player,
                            ByVal result As Protocol.PlayerLeaveType)
            _writer.AddPlayerLeft(0, leaver.PID, result, 0)
        End Sub

        Protected Overrides Function PerformDispose(ByVal finalizing As Boolean) As IFuture
            If finalizing Then Return Nothing
            Return inQueue.QueueFunc(
                Function()
                    _writer.Dispose()
                    For Each hook In _hooks
                        hook.Dispose()
                    Next
                    Return _writer.FutureDisposed
                End Function).Defuturized
        End Function
    End Class
End Namespace
