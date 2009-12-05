Imports Strilbrary
Imports Strilbrary.Streams
Imports Strilbrary.Threading
Imports Strilbrary.Enumeration
Imports Strilbrary.Numerics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker

<TestClass()>
Public Class PacketStreamTest
    <TestMethod()>
    Public Sub WritePacketTest_Once()
        Dim m = New IO.MemoryStream()
        Dim p = New PacketStreamer(m, 2, 2, 1000)
        p.WritePacket({0, 1, 2, 3, 4, 5, 6, 7, 8, 9})
        m.Position = 0
        Assert.IsTrue(m.Length = 10)
        Assert.IsTrue(m.ReadBytesExact(10).HasSameItemsAs({0, 1, 10, 0, 4, 5, 6, 7, 8, 9}))
    End Sub
    <TestMethod()>
    Public Sub WritePacketTest_Twice()
        Dim m = New IO.MemoryStream()
        Dim p = New PacketStreamer(m, 2, 2, 1000)
        p.WritePacket({0, 1, 2, 3, 4, 5, 6, 7, 8, 9})
        p.WritePacket({0, 0, 0, 0, &HFF})
        m.Position = 0
        Assert.IsTrue(m.Length = 15)
        Assert.IsTrue(m.ReadBytesExact(15).HasSameItemsAs({0, 1, 10, 0, 4, 5, 6, 7, 8, 9, 0, 0, 5, 0, &HFF}))
    End Sub

    <TestMethod()>
    Public Sub ReadPacketTest_Once()
        Dim m = New IO.MemoryStream()
        Dim d = New Byte() {0, 1, 10, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        m.Write(d, 0, d.Length)
        m.Position = 0
        Dim p = New PacketStreamer(m, 2, 1, 1000)
        Dim f = p.AsyncReadPacket()
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(f.Value.HasSameItemsAs({0, 1, 10, 3, 4, 5, 6, 7, 8, 9}))
    End Sub
    <TestMethod()>
    Public Sub ReadPacketTest_Twice()
        Dim m = New IO.MemoryStream()
        Dim d = New Byte() {0, 1, 10, 3, 4, 5, 6, 7, 8, 9, 0, 0, 4, &HFF, 0, 0, 0, 0, 0, 0}
        m.Write(d, 0, d.Length)
        m.Position = 0
        Dim p = New PacketStreamer(m, 2, 1, 1000)
        BlockOnFuture(p.AsyncReadPacket())
        Dim f = p.AsyncReadPacket()
        BlockOnFuture(f)
        Assert.IsTrue(f.State = FutureState.Succeeded)
        Assert.IsTrue(f.Value.HasSameItemsAs({0, 0, 4, &HFF}))
    End Sub
End Class
