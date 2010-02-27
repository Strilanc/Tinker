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
            gameId:=42,
            entryKey:=0,
            peerKey:=0,
            internalAddress:=Net.IPAddress.Loopback).Payload.Data.ToArray

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
            Dim result = server.QueueAddGameSet(New WC3.GameSettings(TestMap, TestDesc, TestArgument))
            BlockOnFuture(result)
            Assert.IsTrue(result.State = FutureState.Succeeded)
        End Using
    End Sub

    <TestMethod()>
    Public Sub DuplicateGameTest()
        Dim clock = New ManualClock()
        Using server = New WC3.GameServer(clock:=clock)
            server.QueueAddGameSet(New WC3.GameSettings(TestMap, TestDesc, TestArgument))
            Dim result = server.QueueAddGameSet(New WC3.GameSettings(TestMap, TestDesc, TestArgument))
            BlockOnFuture(result)
            Assert.IsTrue(result.State = FutureState.Failed)
        End Using
    End Sub

    <TestMethod()>
    Public Sub EnterGameTest()
        Dim clock = New ManualClock()
        Using server = New WC3.GameServer(clock:=clock)
            server.QueueAddGameSet(New WC3.GameSettings(TestMap, TestDesc, TestArgument))
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
            Dim response = WC3.Protocol.Packets.Greet.Jar.Parse(packet.Skip(4).ToReadableList)
            'Check not closed
            Assert.IsTrue(Not testStream.RetrieveClosed(timeout:=100))
            'Cleanup
            testStream.EnqueueClosed()
        End Using
    End Sub
End Class
