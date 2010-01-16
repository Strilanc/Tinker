Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Strilbrary.Time
Imports Strilbrary.Values
Imports Strilbrary.Threading
Imports Strilbrary.Collections

<TestClass()>
Public Class GameServerTest
    Private Shared ReadOnly KnockData As Byte() = WC3.Protocol.MakeKnock(
            name:="Strilanc",
            listenPort:=6112,
            sendingPort:=6112,
            gameId:=1,
            entryKey:=0,
            peerKey:=0,
            internalAddress:=Net.IPAddress.Loopback).Payload.Data.ToArray
    Private Shared ReadOnly TestMap As New WC3.Map(
            folder:="Test:\Maps",
            relativePath:="test",
            fileChecksumCRC32:=1,
            filesize:=1,
            mapChecksumSHA1:=(From i In Enumerable.Range(0, 20) Select CByte(i)).ToArray.AsReadableList,
            mapChecksumXORO:=1,
            slotCount:=2)
    Private Shared ReadOnly TestArgument As New Commands.CommandArgument("")
    Private Shared ReadOnly TestStats As New WC3.GameStats(
            map:=TestMap,
            hostName:="StrilancHost",
            argument:=TestArgument)
    Private Shared ReadOnly TestDescription As New WC3.LocalGameDescription(
            name:="Fun Game",
            gamestats:=TestStats,
            gameid:=1,
            entryKey:=0,
            hostport:=0,
            totalSlotCount:=TestMap.NumPlayerSlots,
            gameType:=TestMap.GameType,
            state:=Bnet.Protocol.GameStates.Unknown0x10,
            usedSlotCount:=0,
            clock:=New ManualClock())

    <TestMethod()>
    Public Sub MissGameTest()
        Dim clock = New ManualClock()
        Using server = New WC3.GameServer(clock:=clock)
            Dim testStream = New TestStream()
            Dim socket = New WC3.W3Socket(New PacketSocket(
                            stream:=testStream,
                            localEndPoint:=New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
                            remoteEndPoint:=New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
                            clock:=clock))

            server.QueueAcceptSocket(socket)
            testStream.EnqueueRead(KnockData)
            Assert.IsTrue(testStream.RetrieveClosed)
        End Using
    End Sub

    <TestMethod()>
    Public Sub TimeoutTest()
        Dim clock = New ManualClock()
        Using server = New WC3.GameServer(clock:=clock)
            Dim testStream = New TestStream()
            Dim socket = New WC3.W3Socket(New PacketSocket(
                                    stream:=testStream,
                                    localEndPoint:=New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
                                    remoteEndPoint:=New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
                                    clock:=clock))

            BlockOnFuture(server.QueueAcceptSocket(socket))
            clock.Advance(1.Minutes)
            Assert.IsTrue(testStream.RetrieveClosed)
        End Using
    End Sub

    <TestMethod()>
    Public Sub AddGameSetTest()
        Dim clock = New ManualClock()
        Using server = New WC3.GameServer(clock:=clock)
            Dim result = server.QueueAddGameSet(New WC3.GameSettings(TestMap, TestDescription, TestArgument))
            BlockOnFuture(result)
            Assert.IsTrue(result.State = FutureState.Succeeded)
        End Using
    End Sub

    <TestMethod()>
    Public Sub DuplicateGameTest()
        Dim clock = New ManualClock()
        Using server = New WC3.GameServer(clock:=clock)
            server.QueueAddGameSet(New WC3.GameSettings(TestMap, TestDescription, TestArgument))
            Dim result = server.QueueAddGameSet(New WC3.GameSettings(TestMap, TestDescription, TestArgument))
            BlockOnFuture(result)
            Assert.IsTrue(result.State = FutureState.Failed)
        End Using
    End Sub

    <TestMethod()>
    Public Sub EnterGameTest()
        Dim clock = New ManualClock()
        Using server = New WC3.GameServer(clock:=clock)
            server.QueueAddGameSet(New WC3.GameSettings(TestMap, TestDescription, TestArgument))
            'Prep data
            Dim testStream = New TestStream()
            testStream.EnqueueRead({WC3.Protocol.Packets.PacketPrefix, WC3.Protocol.PacketId.Knock})
            testStream.EnqueueRead(CUShort(KnockData.Length + 4).Bytes)
            testStream.EnqueueRead(KnockData)
            'Connect
            Dim socket = New WC3.W3Socket(New PacketSocket(
                            stream:=testStream,
                            localEndPoint:=New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
                            remoteEndPoint:=New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
                            clock:=clock))
            server.QueueAcceptSocket(socket)
            'Try read Greet
            Dim packet = testStream.RetrieveWritePacket()
            Assert.IsTrue(packet(1) = WC3.Protocol.PacketId.Greet)
            Dim response = WC3.Protocol.Packets.Greet.Parse(packet.SubToArray(4).AsReadableList)
            'Check not closed
            Assert.IsTrue(Not testStream.RetrieveClosed(timeout:=100))
            'Cleanup
            testStream.EnqueueClosed()
        End Using
    End Sub
End Class
