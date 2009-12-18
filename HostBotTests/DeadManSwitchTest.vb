Imports Strilbrary.Misc
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports System.Threading

<TestClass()>
Public Class DeadManSwitchTest
    <TestMethod()>
    Public Sub ConstructTest()
        Dim s = New ManualResetEvent(initialState:=False)
        Dim d = New DeadManSwitch(period:=25.Milliseconds)
        AddHandler d.Triggered, Sub() s.Set()
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=50))
    End Sub

    <TestMethod()>
    Public Sub ArmTest()
        Dim s = New ManualResetEvent(initialState:=False)
        Dim d = New DeadManSwitch(period:=25.Milliseconds)
        d.Arm()
        AddHandler d.Triggered, Sub() s.Set()
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=10)) 'time-sensitive
        Assert.IsTrue(s.WaitOne(millisecondsTimeout:=1000))
    End Sub
    <TestMethod()>
    Public Sub ResetTest()
        Dim s = New ManualResetEvent(initialState:=False)
        Dim d = New DeadManSwitch(period:=50.Milliseconds)
        d.Arm()
        AddHandler d.Triggered, Sub() s.Set()
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=25)) 'time-sensitive
        d.Reset()
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=25)) 'time-sensitive
        Assert.IsTrue(s.WaitOne(millisecondsTimeout:=1000))
    End Sub
    <TestMethod()>
    Public Sub DisarmTest()
        Dim s = New ManualResetEvent(initialState:=False)
        Dim d = New DeadManSwitch(period:=25.Milliseconds)
        d.Arm()
        AddHandler d.Triggered, Sub() s.Set()
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=10)) 'time-sensitive
        d.Disarm()
        Assert.IsTrue(Not s.WaitOne(millisecondsTimeout:=50))
    End Sub
End Class
