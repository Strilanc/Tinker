﻿Namespace WC3.Replay
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
            Contract.Requires(game IsNot Nothing)

            Dim tickHandler As Game.TickEventHandler =
                    Sub(sender, duration, actions) inQueue.QueueAction(Sub() OnTick(duration, actions))
            Dim chatHandler As Game.PlayerTalkedEventHandler =
                    Sub(sender, speaker, text, receivers) inQueue.QueueAction(Sub() OnChat(speaker, text, receivers))
            Dim leaveHandler As Game.PlayerLeftEventHandler =
                    Sub(sender, gameState, leaver, leaveType, reason) inQueue.QueueAction(Sub() Onleave(leaver, leaveType))
            Dim launchHandler As Game.LaunchedEventHandler =
                    Sub(sender, usingLoadInGame) inQueue.QueueAction(Sub() _writer.AddGameStarted())

            AddHandler game.Tick, tickHandler
            AddHandler game.PlayerTalked, chatHandler
            AddHandler game.PlayerLeft, leaveHandler
            AddHandler game.Launched, launchHandler

            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler game.Tick, tickHandler))
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler game.PlayerTalked, chatHandler))
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler game.PlayerLeft, leaveHandler))
            Me._hooks.Add(New DelegatedDisposable(Sub() RemoveHandler game.Launched, launchHandler))

            game.FutureDisposed.CallWhenReady(Sub() Me.Dispose())
        End Sub

        Public Shared Function StartRecordingFrom(ByVal defaultFilename As String,
                                                  ByVal game As Game,
                                                  ByVal players As IEnumerable(Of Player),
                                                  ByVal slots As IEnumerable(Of Slot),
                                                  ByVal randomSeed As UInt32) As ReplayManager
            Contract.Requires(game IsNot Nothing)
            Contract.Requires(players IsNot Nothing)
            Contract.Requires(slots IsNot Nothing)

            'Choose location
            Dim folder = GetDataFolderPath("Replays")
            If defaultFilename Is Nothing Then
                defaultFilename = "{0} @ {1}, {2}".Frmt(game.Settings.GameDescription.Name,
                                                        game.Map.Name,
                                                        (From p In players Select New String(CType(p.Name, Char()).Take(5).ToArray)).StringJoin(" "))
            End If

            'Strip invalid characters
            defaultFilename = New String((From c In defaultFilename
                                          Select If(IO.Path.GetInvalidFileNameChars.Contains(c), "."c, c)).ToArray)

            'Append a number if necessary
            Dim filename = IO.Path.Combine(folder, defaultFilename + ".w3g")
            Dim i = 1
            While IO.File.Exists(filename)
                i += 1
                filename = IO.Path.Combine(folder, defaultFilename + " - {0}.w3g".Frmt(i))
            End While

            'Start
            Dim file = New IO.FileStream(filename, IO.FileMode.CreateNew, IO.FileAccess.Write, IO.FileShare.None)
            Contract.Assume(file.CanWrite)
            Contract.Assume(file.CanSeek)
            Dim writer = New Replay.ReplayWriter(stream:=file.AsRandomWritableStream,
                                                 wc3Version:=New CachedExternalValues().WC3MajorVersion,
                                                 wc3BuildNumber:=My.Settings.ReplayBuildNumber,
                                                 host:=players.First.AssumeNotNull,
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
            Contract.Requires(leaver IsNot Nothing)
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
