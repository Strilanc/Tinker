﻿Imports Strilbrary.Collections
Imports Strilbrary.Streams
Imports Strilbrary.Values
Imports Strilbrary.Time
Imports Strilbrary.Threading
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Tinker.WC3
Imports Tinker.WC3.Protocol
Imports Tinker.Pickling
Imports System.Collections.Generic

<TestClass()>
Public Class DownloadManagerTest
    Private Class TestGame
        Public Shared ReadOnly HostPid As New PlayerId(1)
        Private Shared ReadOnly _data As Byte() = CByte(1).Repeated(5000).ToArray
        Private ReadOnly _startPlayerHoldPoint As New HoldPoint(Of Download.IPlayerDownloadAspect)()
        Private Shared ReadOnly SharedMap As New Map(
            streamFactory:=Function() New IO.MemoryStream(_data, writable:=False).AsRandomReadableStream.AsNonNull,
            advertisedPath:="Maps\TestMap",
            fileSize:=CUInt(_data.Length),
            fileChecksumCRC32:=_data.CRC32,
            mapChecksumxoro:=1,
            mapChecksumSHA1:=CByte(1).Repeated(20),
            playableWidth:=256,
            playableHeight:=256,
            isMelee:=True,
            usesCustomForces:=False,
            usesFixedPlayerSettings:=False,
            name:="Test",
            lobbySlots:=MakeRist(New Slot(index:=0, raceUnlocked:=False, color:=PlayerColor.Red, team:=0, contents:=New SlotContentsOpen)))
        Private ReadOnly outQueue As CallQueue = MakeTaskedCallQueue()
        Private ReadOnly _players As New ObservableCollection(Of TestPlayer)(outQueue:=outQueue)
        Private ReadOnly _logger As New Logger
        Public ReadOnly Property Logger As Logger
            Get
                Return _logger
            End Get
        End Property
        Public ReadOnly Property Map As Map
            Get
                Return SharedMap
            End Get
        End Property
        Public ReadOnly Property StartPlayerHoldPoint As IHoldPoint(Of Download.IPlayerDownloadAspect)
            Get
                Return _startPlayerHoldPoint
            End Get
        End Property
        Public Function QueueSendMapPiece(player As Download.IPlayerDownloadAspect, position As UInteger) As Task
            Return CType(player, TestPlayer).QueueSendPacket(MakeMapFileData(
                    position,
                    CByte(1).Repeated(CInt(Math.Min(Packets.MaxFileDataSize, Map.FileSize - position))).ToRist,
                    player.Id,
                    HostPid))
        End Function
        Public Function AddPlayer(player As TestPlayer) As Task
            SyncLock Me
                _players.Add(player)
                Return _startPlayerHoldPoint.Hold(player)
            End SyncLock
        End Function
        Public Sub RemovePlayer(player As TestPlayer)
            SyncLock Me
                _players.Remove(player)
            End SyncLock
        End Sub
    End Class
    Private Class TestPlayer
        Inherits DisposableWithTask
        Implements Download.IPlayerDownloadAspect
        Private ReadOnly _failFuture As New TaskCompletionSource(Of Boolean)()
        Private ReadOnly _discFuture As New TaskCompletionSource(Of Boolean)()
        Private ReadOnly _pid As PlayerId
        Private ReadOnly _pq As New Queue(Of Packet)()
        Private ReadOnly _lock As New System.Threading.ManualResetEvent(initialState:=False)
        Private ReadOnly _handler As PacketHandlerLogger(Of Protocol.PacketId) = Protocol.MakeW3PacketHandlerLogger("TestSource", New Logger)
        Private ReadOnly _logger As Logger
        Private ReadOnly _name As InvariantString
        Public Sub New(pid As PlayerId,
                       logger As Logger,
                       Optional name As InvariantString? = Nothing)
            Me._pid = pid
            Me._logger = logger
            Me._name = If(name Is Nothing, "TestPlayer{0}".Frmt(pid.Index), name.Value.ToString)
            _failFuture.Task.ConsiderExceptionsHandled()
            _discFuture.Task.ConsiderExceptionsHandled()
        End Sub
        Public Function MakePacketOtherPlayerJoined() As Packet Implements Download.IPlayerDownloadAspect.MakePacketOtherPlayerJoined
            Return MakeOtherPlayerJoined(Name,
                                         ID,
                                         0,
                                         ByteRist(0),
                                         Net.IPAddress.Loopback.WithPort(6112))
        End Function
        Public ReadOnly Property Name As InvariantString Implements Download.IPlayerDownloadAspect.Name
            Get
                Return _name
            End Get
        End Property
        Public ReadOnly Property ID As PlayerId Implements Download.IPlayerDownloadAspect.Id
            Get
                Return _pid
            End Get
        End Property
        Private Function QueueAddPacketHandler(Of T)(packetDefinition As Packets.Definition(Of T),
                                                     handler As Func(Of T, Task)) As Task(Of IDisposable) _
                                                     Implements Download.IPlayerDownloadAspect.QueueAddPacketHandler
            SyncLock Me
                Return _handler.IncludeHandler(
                    packetDefinition.Id,
                    packetDefinition.Jar,
                    Async Function(pickle)
                        Try
                            Await handler(pickle.Value)
                        Catch ex As Exception
                            _failFuture.TrySetException(ex)
                        End Try
                    End Function).AsTask
            End SyncLock
        End Function
        Public Function QueueSendPacket(packet As Packet) As Task Implements Download.IPlayerDownloadAspect.QueueSendPacket
            SyncLock Me
                _pq.Enqueue(packet)
                _lock.Set()
                Return CompletedTask()
            End SyncLock
        End Function

        Public Async Function InjectReceivedPacket(packet As Packet) As Task
            Try
                Dim t As Task
                SyncLock Me
                    t = _handler.HandlePacket(Concat(
                            {Packets.PacketPrefix, packet.Id},
                            CUShort(packet.Payload.Data.Count + 4).Bytes,
                            packet.Payload.Data).ToRist())
                End SyncLock
                Await t
            Catch ex As Exception
                _failFuture.SetException(ex)
            End Try
        End Function
        Public Function TryInterceptSentPacket(Optional timeoutMilliseconds As Integer = 10000) As Packet
            SyncLock Me
                If _pq.Count > 0 Then Return _pq.Dequeue
                _lock.Reset()
            End SyncLock
            _lock.WaitOne(timeoutMilliseconds)
            SyncLock Me
                If _pq.Count > 0 Then Return _pq.Dequeue
            End SyncLock
            Return Nothing
        End Function
        Public Sub ExpectNoPacket(Optional timeoutMilliseconds As Integer = 10)
            Dim packet = TryInterceptSentPacket(timeoutMilliseconds)
            If packet IsNot Nothing Then
                Throw New IO.InvalidDataException("Unexpected packet.")
            End If
        End Sub
        Public Sub ExpectSentPacket(expected As Packet, Optional timeoutMilliseconds As Integer = 10000)
            Dim received = TryInterceptSentPacket(timeoutMilliseconds)
            If received Is Nothing Then Throw New IO.IOException("No sent packet intercepted.")
            If expected.id <> received.id Then
                Throw New IO.InvalidDataException("Incorrect packet type: received {0} instead of {1}.".Frmt(received.id, expected.id))
            ElseIf Not expected.Payload.Data.SequenceEqual(received.Payload.Data) Then
                Throw New IO.InvalidDataException("Incorrect packet contents: received {0} instead of {1}.".Frmt(received.Payload.Description, expected.Payload.Description))
            End If
        End Sub
        Public ReadOnly Property FailFuture As Task
            Get
                Return _failFuture.Task
            End Get
        End Property
        Public ReadOnly Property DiscFuture As Task
            Get
                Return _discFuture.Task
            End Get
        End Property
        Public Function InjectClientMapInfo(state As MapTransferState, position As UInt32) As Task
            Return InjectReceivedPacket(MakeClientMapInfo(state, position))
        End Function
        Public Function InjectMapDataReceived(position As UInt32, senderPid As PlayerId) As Task
            Return InjectReceivedPacket(MakeMapFileDataReceived(senderPid, ID, position))
        End Function
        Public Function QueueDisconnect(expected As Boolean, reportedResult As PlayerLeaveReason, reasonDescription As String) As Task Implements Download.IPlayerDownloadAspect.QueueDisconnect
            _logger.Log("{0} Disconnected: {1}, {2}".Frmt(Me.Name, reportedResult, reasonDescription), LogMessageType.Negative)
            _discFuture.TrySetResult(True)
            Return _discFuture.Task
        End Function
    End Class

    <TestMethod()>
    Public Sub NoDownload()
        Dim game = New TestGame()
        Dim clock = New ManualClock()
        Dim dm = New Download.Manager(clock, game.Map, game.Logger, allowDownloads:=True, allowUploads:=True)
        dm.Start(game.StartPlayerHoldPoint, AddressOf game.QueueSendMapPiece)
        Dim player = New TestPlayer(New PlayerId(2), game.Logger)
        WaitUntilTaskSucceeds(game.AddPlayer(player))

        player.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)
        clock.Advance(Download.Manager.UpdatePeriod)
        player.ExpectNoPacket()
    End Sub

    <TestMethod()>
    Public Sub HostDownload()
        Dim game = New TestGame()
        Dim clock = New ManualClock()
        Dim dm = New Download.Manager(clock, game.Map, game.Logger, allowDownloads:=True, allowUploads:=True)
        dm.Start(game.StartPlayerHoldPoint, AddressOf game.QueueSendMapPiece)
        Dim dler = New TestPlayer(New PlayerId(2), game.Logger)
        WaitUntilTaskSucceeds(game.AddPlayer(dler))

        dler.InjectClientMapInfo(MapTransferState.Idle, 0)
        dler.ExpectNoPacket()
        clock.Advance(Download.Manager.UpdatePeriod)
        For p = Packets.MaxFileDataSize To game.Map.FileSize Step Packets.MaxFileDataSize
            dler.ExpectSentPacket(MakeMapFileData(p - Packets.MaxFileDataSize,
                                                  Enumerable.Repeat(CByte(1), Packets.MaxFileDataSize).ToRist,
                                                  dler.ID,
                                                  TestGame.HostPid))
            dler.InjectClientMapInfo(MapTransferState.Idle, p)
        Next p
        dler.ExpectSentPacket(MakeMapFileData(game.Map.FileSize.FloorMultiple(Packets.MaxFileDataSize),
                                              CByte(1).Repeated(CInt(game.Map.FileSize Mod Packets.MaxFileDataSize)),
                                              dler.ID,
                                              TestGame.HostPid))
        dler.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)
        dler.ExpectNoPacket()

        ExpectTaskToIdle(dler.FailFuture)
    End Sub

    <TestMethod()>
    Public Sub PeerDownload()
        Dim game = New TestGame()
        Dim clock = New ManualClock()
        Dim dm = New Download.Manager(clock, game.Map, game.Logger, allowDownloads:=True, allowUploads:=False)
        dm.Start(game.StartPlayerHoldPoint, AddressOf game.QueueSendMapPiece)
        Dim uler = New TestPlayer(New PlayerId(2), game.Logger)
        Dim dler = New TestPlayer(New PlayerId(3), game.Logger)
        WaitUntilTaskSucceeds(game.AddPlayer(dler))
        WaitUntilTaskSucceeds(game.AddPlayer(uler))

        dler.InjectClientMapInfo(MapTransferState.Idle, 0)
        uler.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)
        Call Async Sub() Await dler.InjectReceivedPacket(MakePeerConnectionInfo({uler.ID}))
        Call Async Sub() Await uler.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID}))
        uler.ExpectNoPacket()
        dler.ExpectNoPacket()
        clock.Advance(Download.Manager.UpdatePeriod)

        dler.ExpectSentPacket(MakeSetDownloadSource(uler.ID))
        uler.ExpectSentPacket(MakeSetUploadTarget(dler.ID, 0))
        dler.ExpectNoPacket()
        uler.ExpectNoPacket()
        dler.InjectClientMapInfo(MapTransferState.Downloading, 0)
        uler.InjectClientMapInfo(MapTransferState.Uploading, game.Map.FileSize)
        dler.InjectClientMapInfo(MapTransferState.Downloading, game.Map.FileSize)
        dler.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)
        uler.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)

        ExpectTaskToIdle(dler.FailFuture)
        ExpectTaskToIdle(uler.FailFuture)
    End Sub

    <TestMethod()>
    Public Sub PeerDownloadFail_NoUploader()
        Dim game = New TestGame()
        Dim clock = New ManualClock()
        Dim dm = New Download.Manager(clock, game.Map, game.Logger, allowDownloads:=True, allowUploads:=False)
        dm.Start(game.StartPlayerHoldPoint, AddressOf game.QueueSendMapPiece)
        Dim uler = New TestPlayer(New PlayerId(2), game.Logger)
        Dim dler = New TestPlayer(New PlayerId(3), game.Logger)
        WaitUntilTaskSucceeds(game.AddPlayer(dler))
        WaitUntilTaskSucceeds(game.AddPlayer(uler))

        dler.InjectClientMapInfo(MapTransferState.Idle, 0)
        uler.InjectClientMapInfo(MapTransferState.Idle, 0)
        Call Async Sub() Await dler.InjectReceivedPacket(MakePeerConnectionInfo({uler.ID}))
        Call Async Sub() Await uler.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID}))
        clock.Advance(Download.Manager.UpdatePeriod)

        uler.ExpectNoPacket()
        dler.ExpectNoPacket()
        ExpectTaskToIdle(dler.FailFuture)
        ExpectTaskToIdle(uler.FailFuture)
    End Sub
    <TestMethod()>
    Public Sub PeerDownloadFail_NoPeerConnectionInfo()
        Dim game = New TestGame()
        Dim clock = New ManualClock()
        Dim dm = New Download.Manager(clock, game.Map, game.Logger, allowDownloads:=True, allowUploads:=False)
        dm.Start(game.StartPlayerHoldPoint, AddressOf game.QueueSendMapPiece)
        Dim uler = New TestPlayer(New PlayerId(2), game.Logger)
        Dim dler = New TestPlayer(New PlayerId(3), game.Logger)
        WaitUntilTaskSucceeds(game.AddPlayer(dler))
        WaitUntilTaskSucceeds(game.AddPlayer(uler))

        dler.InjectClientMapInfo(MapTransferState.Idle, 0)
        uler.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)
        clock.Advance(Download.Manager.UpdatePeriod)

        uler.ExpectNoPacket()
        dler.ExpectNoPacket()
        ExpectTaskToIdle(dler.FailFuture)
        ExpectTaskToIdle(uler.FailFuture)
    End Sub

    <TestMethod()>
    Public Sub PeerDownload_TimeoutSwitch()
        Dim game = New TestGame()
        Dim clock = New ManualClock()
        Dim dm = New Download.Manager(clock, game.Map, game.Logger, allowDownloads:=True, allowUploads:=False)
        dm.Start(game.StartPlayerHoldPoint, AddressOf game.QueueSendMapPiece)
        Dim dler = New TestPlayer(New PlayerId(2), game.Logger, "dler")
        Dim uler1 = New TestPlayer(New PlayerId(3), game.Logger, "uler1")
        Dim uler2 = New TestPlayer(New PlayerId(4), game.Logger, "uler2")

        'Start initial transfer
        WaitUntilTaskSucceeds(game.AddPlayer(dler))
        WaitUntilTaskSucceeds(game.AddPlayer(uler1))
        dler.InjectClientMapInfo(MapTransferState.Idle, 0)
        uler1.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)
        Call Async Sub() Await dler.InjectReceivedPacket(MakePeerConnectionInfo({uler1.ID}))
        WaitUntilTaskSucceeds(uler1.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID})))
        uler1.ExpectNoPacket()
        dler.ExpectNoPacket()
        clock.Advance(Download.Manager.UpdatePeriod)

        'Check for transfer
        dler.ExpectSentPacket(MakeSetDownloadSource(uler1.ID))
        uler1.ExpectSentPacket(MakeSetUploadTarget(dler.ID, 0))
        dler.ExpectNoPacket()
        uler1.ExpectNoPacket()

        'Cancel and start new transfer
        WaitUntilTaskSucceeds(game.AddPlayer(uler2))
        uler2.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)
        Call Async Sub() Await uler2.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID}))
        WaitUntilTaskSucceeds(dler.InjectReceivedPacket(MakePeerConnectionInfo({uler1.ID, uler2.ID})))
        clock.Advance(Download.Manager.FreezePeriod)
        dler.ExpectNoPacket()
        uler1.ExpectNoPacket()
        uler2.ExpectNoPacket()
        clock.Advance(Download.Manager.UpdatePeriod)

        'Check for cancellation
        dler.ExpectSentPacket(MakeOtherPlayerLeft(uler1.ID, PlayerLeaveReason.Disconnect))
        uler1.ExpectSentPacket(MakeOtherPlayerLeft(dler.ID, PlayerLeaveReason.Disconnect))
        dler.ExpectSentPacket(uler1.MakePacketOtherPlayerJoined())
        uler1.ExpectSentPacket(dler.MakePacketOtherPlayerJoined())
        dler.ExpectNoPacket()
        uler1.ExpectNoPacket()
        uler2.ExpectNoPacket()
        clock.Advance(Download.Manager.UpdatePeriod)

        'Check for new transfer
        dler.ExpectSentPacket(MakeSetDownloadSource(uler2.ID))
        uler2.ExpectSentPacket(MakeSetUploadTarget(dler.ID, 0))
        dler.ExpectNoPacket()
        uler1.ExpectNoPacket()
        uler2.ExpectNoPacket()

        ExpectTaskToIdle(dler.FailFuture)
        ExpectTaskToIdle(uler1.FailFuture)
        ExpectTaskToIdle(uler2.FailFuture)
    End Sub

    <TestMethod()>
    Public Sub PeerDownload_SlowSwitch()
        Dim game = New TestGame()
        Dim clock = New ManualClock()
        Dim dm = New Download.Manager(clock, game.Map, game.Logger, allowDownloads:=True, allowUploads:=False)
        dm.Start(game.StartPlayerHoldPoint, AddressOf game.QueueSendMapPiece)
        Dim dler = New TestPlayer(New PlayerId(2), game.Logger, "dler")
        Dim uler1 = New TestPlayer(New PlayerId(3), game.Logger, "uler1")
        Dim uler2 = New TestPlayer(New PlayerId(4), game.Logger, "uler2")

        'Start initial transfer
        WaitUntilTaskSucceeds(game.AddPlayer(dler))
        WaitUntilTaskSucceeds(game.AddPlayer(uler1))
        dler.InjectClientMapInfo(MapTransferState.Idle, 0)
        uler1.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)
        Call Async Sub() Await dler.InjectReceivedPacket(MakePeerConnectionInfo({uler1.ID}))
        WaitUntilTaskSucceeds(uler1.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID})))
        uler1.ExpectNoPacket()
        dler.ExpectNoPacket()
        clock.Advance(Download.Manager.UpdatePeriod)

        'Check for transfer
        dler.ExpectSentPacket(MakeSetDownloadSource(uler1.ID))
        uler1.ExpectSentPacket(MakeSetUploadTarget(dler.ID, 0))
        dler.ExpectNoPacket()
        uler1.ExpectNoPacket()

        'Add second peer
        WaitUntilTaskSucceeds(game.AddPlayer(uler2))
        uler2.InjectClientMapInfo(MapTransferState.Idle, game.Map.FileSize)
        Call Async Sub() Await uler2.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID}))
        Call Async Sub() Await dler.InjectReceivedPacket(MakePeerConnectionInfo({uler1.ID, uler2.ID}))

        'Go slowly
        Dim dt = Download.Manager.MinSwitchPeriod
        Dim i = 0UI
        While dt >= Download.Manager.UpdatePeriod
            clock.Advance(Download.Manager.UpdatePeriod)
            dt -= Download.Manager.UpdatePeriod
            i += 1UI
            WaitUntilTaskSucceeds(dler.InjectClientMapInfo(MapTransferState.Downloading, i))
        End While
        dler.ExpectNoPacket()
        uler1.ExpectNoPacket()
        uler2.ExpectNoPacket()

        'Check for cancellation
        clock.Advance(Download.Manager.UpdatePeriod)
        dler.ExpectSentPacket(MakeOtherPlayerLeft(uler1.ID, PlayerLeaveReason.Disconnect))
        uler1.ExpectSentPacket(MakeOtherPlayerLeft(dler.ID, PlayerLeaveReason.Disconnect))
        dler.ExpectSentPacket(uler1.MakePacketOtherPlayerJoined())
        uler1.ExpectSentPacket(dler.MakePacketOtherPlayerJoined())
        dler.ExpectNoPacket()
        uler1.ExpectNoPacket()
        uler2.ExpectNoPacket()
        WaitUntilTaskSucceeds(dler.InjectClientMapInfo(MapTransferState.Idle, i))
        clock.Advance(Download.Manager.UpdatePeriod)

        'Check for new transfer
        dler.ExpectSentPacket(MakeSetDownloadSource(uler2.ID))
        uler2.ExpectSentPacket(MakeSetUploadTarget(dler.ID, i))
        dler.ExpectNoPacket()
        uler1.ExpectNoPacket()
        uler2.ExpectNoPacket()

        ExpectTaskToIdle(dler.FailFuture)
        ExpectTaskToIdle(uler1.FailFuture)
        ExpectTaskToIdle(uler2.FailFuture)
    End Sub
End Class
