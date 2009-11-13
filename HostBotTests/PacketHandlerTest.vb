Imports Strilbrary
Imports Strilbrary.Threading
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()>
Public Class PacketHandlerTest
    Private Class SimplePacketHandler
        Inherits HostBot.PacketHandler(Of Byte)
        Protected Overrides Function ExtractKey(ByVal header As Strilbrary.ViewableList(Of Byte)) As Byte
            If header(0) = 255 Then Throw New InvalidOperationException("Mock Exception")
            Return header(0)
        End Function
        Public Overrides ReadOnly Property HeaderSize As Integer
            Get
                Return 1
            End Get
        End Property
    End Class

    <TestMethod()>
    Public Sub ValueTest()
        Dim flag = 0UI
        Dim p = New SimplePacketHandler()
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub() flag = pickle.Value))
        Dim result = p.HandlePacket(New Byte() {1, &H12, &H34, &H56, &H78}.ToView)
        BlockOnFuture(result)
        Assert.IsTrue(flag = &H78563412)
        Assert.IsTrue(result.State = FutureState.Succeeded)
    End Sub

    <TestMethod()>
    Public Sub SelectTest()
        Dim flag1 = True
        Dim flag2 = False
        Dim p = New SimplePacketHandler()
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub() flag1 = False))
        p.AddHandler(Of UInt32)(key:=2,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub() flag2 = True))
        Dim result = p.HandlePacket(New Byte() {2, &H12, &H34, &H56, &H78}.ToView)
        BlockOnFuture(result)
        Assert.IsTrue(flag1)
        Assert.IsTrue(flag2)
        Assert.IsTrue(result.State = FutureState.Succeeded)
    End Sub

    <TestMethod()>
    Public Sub DoubleHandleTest()
        Dim flag1 = False
        Dim flag2 = False
        Dim p = New SimplePacketHandler()
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub() flag1 = True))
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub() flag2 = True))
        Dim result = p.HandlePacket(New Byte() {1, &H12, &H34, &H56, &H78}.ToView)
        BlockOnFuture(result)
        Assert.IsTrue(flag1)
        Assert.IsTrue(flag2)
        Assert.IsTrue(result.State = FutureState.Succeeded)
    End Sub

    <TestMethod()>
    Public Sub HandleFailTest()
        Dim p = New SimplePacketHandler()
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle)
                                             Throw New InvalidOperationException("Mock Exception")
                                         End Function)
        Dim result = p.HandlePacket(New Byte() {1, &H12, &H34, &H56, &H78}.ToView)
        BlockOnFuture(result)
        Assert.IsTrue(result.State = FutureState.Failed)
    End Sub

    <TestMethod()>
    Public Sub HandleFutureFailTest()
        Dim p = New SimplePacketHandler()
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub()
                                                                           Throw New InvalidOperationException("Mock Exception")
                                                                       End Sub))
        Dim result = p.HandlePacket(New Byte() {1, &H12, &H34, &H56, &H78}.ToView)
        BlockOnFuture(result)
        Assert.IsTrue(result.State = FutureState.Failed)
    End Sub

    <TestMethod()>
    Public Sub ExtractKeyFailTest()
        Dim flag = True
        Dim p = New SimplePacketHandler()
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub()
                                                                           flag = False
                                                                       End Sub))
        Dim result = p.HandlePacket(New Byte() {255, &H12, &H34, &H56, &H78}.ToView)
        BlockOnFuture(result)
        Assert.IsTrue(result.State = FutureState.Failed)
        Assert.IsTrue(flag)
    End Sub

    <TestMethod()>
    Public Sub MissingHandlerTest()
        Dim p = New SimplePacketHandler()
        Dim result = p.HandlePacket(New Byte() {1, 2, 3, 4}.ToView)
        BlockOnFuture(result)
        Assert.IsTrue(result.State = FutureState.Failed)
    End Sub

    <TestMethod()>
    Public Sub DisposedHandlerTest()
        Dim p = New SimplePacketHandler()
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub()
                                                                       End Sub)).Dispose()
        Dim result = p.HandlePacket(New Byte() {1, 2, 3, 4}.ToView)
        BlockOnFuture(result)
        Assert.IsTrue(result.State = FutureState.Failed)
    End Sub

    <TestMethod()>
    Public Sub SelectNonDisposedHandlerTest()
        Dim flag1 = False
        Dim flag2 = True
        Dim p = New SimplePacketHandler()
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub() flag1 = True))
        p.AddHandler(Of UInt32)(key:=1,
                                jar:=New HostBot.Pickling.Jars.UInt32Jar("test"),
                                handler:=Function(pickle) TaskedAction(Sub() flag2 = False)
                                ).Dispose()
        Dim result = p.HandlePacket(New Byte() {1, &H12, &H34, &H56, &H78}.ToView)
        BlockOnFuture(result)
        Assert.IsTrue(flag1)
        Assert.IsTrue(flag2)
        Assert.IsTrue(result.State = FutureState.Succeeded)
    End Sub
End Class
