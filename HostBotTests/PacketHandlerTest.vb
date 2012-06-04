Imports Strilbrary.Collections
Imports Strilbrary.Threading
Imports Strilbrary.Values
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic

<TestClass()>
Public Class PacketHandlerTest
    Private Function MakeTestPacketHandler() As Tinker.PacketHandlerRaw(Of Byte)
        Return New Tinker.PacketHandlerRaw(Of Byte)(
            HeaderSize:=1,
            keyExtractor:=Function(header)
                              If header(0) = 255 Then Throw New InvalidOperationException("Mock Exception")
                              Return header(0)
                          End Function)
    End Function

    <TestMethod()>
    Public Sub ValueTest()
        Dim flag = 0UI
        Dim p = MakeTestPacketHandler()
        p.IncludeHandler(key:=1,
                     handler:=Function(data) Task.Factory.StartNew(Sub() flag = data.TakeExact(4).ToUInt32))
        Dim result = p.HandlePacket(ByteRist(1, &H12, &H34, &H56, &H78))
        WaitUntilTaskSucceeds(result)
        Assert.IsTrue(flag = &H78563412)
    End Sub

    <TestMethod()>
    Public Sub SelectTest()
        Dim flag1 = True
        Dim flag2 = False
        Dim p = MakeTestPacketHandler()
        p.IncludeHandler(key:=1,
                     handler:=Function(data) Task.Factory.StartNew(Sub() flag1 = False))
        p.IncludeHandler(key:=2,
                     handler:=Function(data) Task.Factory.StartNew(Sub() flag2 = True))
        Dim result = p.HandlePacket(ByteRist(2, &H12, &H34, &H56, &H78))
        WaitUntilTaskSucceeds(result)
        Assert.IsTrue(flag1)
        Assert.IsTrue(flag2)
    End Sub

    <TestMethod()>
    Public Sub DoubleHandleTest()
        Dim flag1 = False
        Dim flag2 = False
        Dim p = MakeTestPacketHandler()
        p.IncludeHandler(key:=1,
                     handler:=Function(pickle) Task.Factory.StartNew(Sub() flag1 = True))
        p.IncludeHandler(key:=1,
                     handler:=Function(pickle) Task.Factory.StartNew(Sub() flag2 = True))
        Dim result = p.HandlePacket(ByteRist(1, &H12, &H34, &H56, &H78))
        WaitUntilTaskSucceeds(result)
        Assert.IsTrue(flag1)
        Assert.IsTrue(flag2)
    End Sub

    <TestMethod()>
    Public Sub HandleFailTest()
        Dim p = MakeTestPacketHandler()
        p.IncludeHandler(key:=1,
                     handler:=Function(pickle)
                                  Throw New InvalidOperationException("Mock Exception")
                              End Function)
        Dim result = p.HandlePacket(ByteRist(1, &H12, &H34, &H56, &H78))
        WaitUntilTaskFails(result)
    End Sub

    <TestMethod()>
    Public Sub HandleFutureFailTest()
        Dim p = MakeTestPacketHandler()
        p.IncludeHandler(key:=1,
                     handler:=Function(pickle) Task.Factory.StartNew(Sub() Throw New InvalidOperationException("Mock Exception")))
        Dim result = p.HandlePacket(ByteRist(1, &H12, &H34, &H56, &H78))
        WaitUntilTaskFails(result)
    End Sub

    <TestMethod()>
    Public Sub ExtractKeyFailTest()
        Dim flag = True
        Dim p = MakeTestPacketHandler()
        p.IncludeHandler(key:=1,
                         handler:=Function(pickle) Task.Factory.StartNew(Sub() flag = False))
        Dim result = p.HandlePacket(ByteRist(255, &H12, &H34, &H56, &H78))
        WaitUntilTaskFails(result)
        Assert.IsTrue(flag)
    End Sub

    <TestMethod()>
    Public Sub MissingHandlerTest()
        Dim p = MakeTestPacketHandler()
        Dim result = p.HandlePacket(ByteRist(1, 2, 3, 4))
        WaitUntilTaskFails(result)
    End Sub

    <TestMethod()>
    Public Sub DisposedHandlerTest()
        Dim p = MakeTestPacketHandler()
        p.IncludeHandler(key:=1,
                     handler:=Function(pickle) Task.Factory.StartNew(Sub()
                                                                     End Sub)
                     ).Dispose()
        Dim result = p.HandlePacket(ByteRist(1, 2, 3, 4))
        WaitUntilTaskFails(result)
    End Sub

    <TestMethod()>
    Public Sub SelectNonDisposedHandlerTest()
        Dim flag1 = False
        Dim flag2 = True
        Dim p = MakeTestPacketHandler()
        p.IncludeHandler(key:=1,
                     handler:=Function(pickle) Task.Factory.StartNew(Sub() flag1 = True))
        p.IncludeHandler(key:=1,
                     handler:=Function(pickle) Task.Factory.StartNew(Sub() flag2 = False)
                     ).Dispose()
        Dim result = p.HandlePacket(ByteRist(1, &H12, &H34, &H56, &H78))
        WaitUntilTaskSucceeds(result)
        Assert.IsTrue(flag1)
        Assert.IsTrue(flag2)
    End Sub
End Class
