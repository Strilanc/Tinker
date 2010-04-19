Imports Strilbrary.Collections
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
            advertisedPath:="TestMap",
            fileSize:=CUInt(_data.Length),
            fileChecksumCRC32:=_data.CRC32,
            mapChecksumxoro:=1,
            mapChecksumSHA1:=CByte(1).Repeated(20).ToReadableList,
            playableWidth:=256,
            playableHeight:=256,
            isMelee:=True,
            usesCustomForces:=False,
            usesFixedPlayerSettings:=False,
            name:="Test",
            lobbySlots:={New Slot(index:=0, raceUnlocked:=False, color:=PlayerColor.Red, team:=0, contents:=New SlotContentsOpen)}.AsReadableList)
        Private ReadOnly outQueue As New TaskedCallQueue()
        Private ReadOnly _players As New AsyncViewableCollection(Of TestPlayer)(outQueue:=outQueue)
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
        Public Function QueueSendMapPiece(ByVal player As Download.IPlayerDownloadAspect, ByVal position As UInteger) As Task
            Return CType(player, TestPlayer).QueueSendPacket(MakeMapFileData(
                    position,
                    CByte(1).Repeated(CInt(Math.Min(Packets.MaxFileDataSize, Map.FileSize - position))).ToReadableList,
                    player.Id,
                    HostPid))
        End Function
        Public Function AddPlayer(ByVal player As TestPlayer) As Task
            SyncLock Me
                _players.Add(player)
                Return _startPlayerHoldPoint.Hold(player)
            End SyncLock
        End Function
        Public Sub RemovePlayer(ByVal player As TestPlayer)
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
        Private ReadOnly _handler As New W3PacketHandler("TestSource")
        Private ReadOnly _logger As Logger
        Private ReadOnly _name As InvariantString
        Public Sub New(ByVal pid As PlayerId,
                       ByVal logger As Logger,
                       Optional ByVal name As InvariantString? = Nothing)
            Me._pid = pid
            Me._logger = logger
            Me._name = If(name Is Nothing, "TestPlayer{0}".Frmt(pid.Index), name.Value.ToString)
            _failFuture.IgnoreExceptions()
            _discFuture.IgnoreExceptions()
        End Sub
        Public Function MakePacketOtherPlayerJoined() As Packet Implements Download.IPlayerDownloadAspect.MakePacketOtherPlayerJoined
            Return MakeOtherPlayerJoined(Name,
                                         ID,
                                         0,
                                         New Byte() {0}.AsReadableList,
                                         New Net.IPEndPoint(Net.IPAddress.Loopback, 6112))
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
        Private Function QueueAddPacketHandler(Of T)(ByVal packetDefinition As Packets.Definition(Of T),
                                                     ByVal handler As Func(Of IPickle(Of T), Task)) As Task(Of IDisposable) _
                                                     Implements Download.IPlayerDownloadAspect.QueueAddPacketHandler
            SyncLock Me
                Return _handler.AddHandler(packetDefinition.Id, Function(data)
                                                                    Dim result = handler(packetDefinition.Jar.ParsePickle(data))
                                                                    result.Catch(Sub(ex) _failFuture.TrySetException(ex.InnerExceptions))
                                                                    Return result
                                                                End Function).AsTask
            End SyncLock
        End Function
        Public Function QueueSendPacket(ByVal packet As Packet) As Task Implements Download.IPlayerDownloadAspect.QueueSendPacket
            SyncLock Me
                _pq.Enqueue(packet)
                _lock.Set()
                Dim result = New TaskCompletionSource(Of Boolean)()
                result.SetResult(True)
                Return result.Task
            End SyncLock
        End Function

        Public Function InjectReceivedPacket(ByVal packet As Packet) As Task
            SyncLock Me
                Return _handler.HandlePacket(
                        New Byte() {Packets.PacketPrefix, packet.Id}.Concat(
                                    CUShort(packet.Payload.Data.Count + 4).Bytes).Concat(
                                    packet.Payload.Data).ToReadableList
                ).Catch(Sub(ex) _failFuture.SetException(ex.InnerExceptions))
            End SyncLock
        End Function
        Public Function TryInterceptSentPacket(Optional ByVal timeoutMilliseconds As Integer = 10000) As Packet
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
        Public Sub ExpectNoPacket(Optional ByVal timeoutMilliseconds As Integer = 10)
            Dim packet = TryInterceptSentPacket(timeoutMilliseconds)
            If packet IsNot Nothing Then
                Throw New IO.InvalidDataException("Unexpected packet.")
            End If
        End Sub
        Public Sub ExpectSentPacket(ByVal expected As Packet, Optional ByVal timeoutMilliseconds As Integer = 10000)
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
        Public Function InjectClientMapInfo(ByVal state As MapTransferState, ByVal position As UInt32) As Task
            Return InjectReceivedPacket(MakeClientMapInfo(state, position))
        End Function
        Public Function InjectMapDataReceived(ByVal position As UInt32, ByVal senderPid As PlayerId) As Task
            Return InjectReceivedPacket(MakeMapFileDataReceived(senderPid, ID, position))
        End Function
        Public Function QueueDisconnect(ByVal expected As Boolean, ByVal reportedResult As PlayerLeaveReason, ByVal reasonDescription As String) As Task Implements Download.IPlayerDownloadAspect.QueueDisconnect
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
                                                  Enumerable.Repeat(CByte(1), Packets.MaxFileDataSize).ToReadableList,
                                                  dler.ID,
                                                  TestGame.HostPid))
            dler.InjectClientMapInfo(MapTransferState.Idle, p)
        Next p
        dler.ExpectSentPacket(MakeMapFileData(game.Map.FileSize.FloorMultiple(Packets.MaxFileDataSize),
                                              CByte(1).Repeated(CInt(game.Map.FileSize Mod Packets.MaxFileDataSize)).ToReadableList,
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
        dler.InjectReceivedPacket(MakePeerConnectionInfo({uler.ID}))
        uler.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID}))
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
        dler.InjectReceivedPacket(MakePeerConnectionInfo({uler.ID}))
        uler.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID}))
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
        dler.InjectReceivedPacket(MakePeerConnectionInfo({uler1.ID}))
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
        uler2.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID}))
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
        dler.InjectReceivedPacket(MakePeerConnectionInfo({uler1.ID}))
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
        uler2.InjectReceivedPacket(MakePeerConnectionInfo({dler.ID}))
        dler.InjectReceivedPacket(MakePeerConnectionInfo({uler1.ID, uler2.ID}))

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
