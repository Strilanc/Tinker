Imports Strilbrary.Time
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports System.Threading

<TestClass()>
Public Class DeadManSwitchTest
    <TestMethod()>
    Public Sub ConstructTest()
        Dim c = New ManualClock()
        Dim s = New ManualResetEvent(initialState:=False)
        Dim d = New DeadManSwitch(period:=1.Milliseconds, clock:=c)
        AddHandler d.Triggered, Sub() s.Set()
        c.Advance(2.Milliseconds)
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=10))
    End Sub

    <TestMethod()>
    Public Sub ArmTest()
        Dim c = New ManualClock()
        Dim s = New ManualResetEvent(initialState:=False)
        Dim d = New DeadManSwitch(period:=1.Milliseconds, clock:=c)
        WaitUntilTaskSucceeds(d.QueueArm())
        AddHandler d.Triggered, Sub() s.Set()
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=10))
        c.Advance(2.Milliseconds)
        Assert.IsTrue(s.WaitOne(millisecondsTimeout:=10000))
    End Sub
    <TestMethod()>
    Public Sub ResetTest()
        Dim c = New ManualClock()
        Dim s = New ManualResetEvent(initialState:=False)
        Dim d = New DeadManSwitch(period:=3.Milliseconds, clock:=c)
        WaitUntilTaskSucceeds(d.QueueArm())
        AddHandler d.Triggered, Sub() s.Set()
        c.Advance(2.Milliseconds)
        WaitUntilTaskSucceeds(d.QueueReset())
        c.Advance(2.Milliseconds)
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=10))
        c.Advance(2.Milliseconds)
        Assert.IsTrue(s.WaitOne(millisecondsTimeout:=10000))
    End Sub
    <TestMethod()>
    Public Sub DisarmTest()
        Dim c = New ManualClock()
        Dim s = New ManualResetEvent(initialState:=False)
        Dim d = New DeadManSwitch(period:=3.Milliseconds, clock:=c)
        WaitUntilTaskSucceeds(d.QueueArm())
        AddHandler d.Triggered, Sub() s.Set()
        c.Advance(2.Milliseconds)
        WaitUntilTaskSucceeds(d.QueueDisarm())
        c.Advance(2.Milliseconds)
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=10))
    End Sub
End Class
