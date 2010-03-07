Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Strilbrary.Values
Imports Strilbrary.Time
Imports Strilbrary.Threading
Imports Tinker.Pickling
Imports System.Collections.Generic

<TestClass()>
Public Class HoldPointTest
    <TestMethod()>
    Public Sub NoHandlerTest()
        Dim p = New HoldPoint(Of Object)()
        Dim r = p.Hold(Nothing)
        WaitUntilTaskSucceeds(r)
    End Sub
    <TestMethod()>
    Public Sub ActionHandlerTest()
        Dim p = New HoldPoint(Of Object)()
        Dim flag = 0
        p.IncludeActionHandler(Sub(arg) If arg Is Nothing Then flag += 1)
        Dim r = p.Hold(Nothing)
        WaitUntilTaskSucceeds(r)
        Assert.IsTrue(flag = 1)
    End Sub
    <TestMethod()>
    Public Sub FutureHandlerTest()
        Dim p = New HoldPoint(Of Object)()
        Dim f = New TaskCompletionSource(Of Boolean)()
        p.IncludeFutureHandler(Function() f.Task)
        Dim r = p.Hold(Nothing)
        ExpectTaskToIdle(r)
        f.SetResult(True)
        WaitUntilTaskSucceeds(r)
    End Sub
    <TestMethod()>
    Public Sub ActionHandlerExceptionTest()
        Dim p = New HoldPoint(Of Object)()
        p.IncludeActionHandler(Sub() Throw New InvalidOperationException())
        Dim r = p.Hold(Nothing)
        WaitUntilTaskFails(r)
    End Sub
    <TestMethod()>
    Public Sub FutureHandlerExceptionTest()
        Dim p = New HoldPoint(Of Object)()
        p.IncludeFutureHandler(Function() As Task
                                   Throw New InvalidOperationException()
                               End Function)
        Dim r = p.Hold(Nothing)
        WaitUntilTaskFails(r)
    End Sub
    <TestMethod()>
    Public Sub HybridHandlerTest()
        Dim p = New HoldPoint(Of Object)()
        Dim flag = 0
        Dim f = New TaskCompletionSource(Of Boolean)()
        p.IncludeFutureHandler(Function() f.Task)
        p.IncludeActionHandler(Sub() flag += 1)
        Dim r = p.Hold(Nothing)
        ExpectTaskToIdle(r)
        f.SetResult(True)
        WaitUntilTaskSucceeds(r)
        Assert.IsTrue(flag = 1)
    End Sub
    <TestMethod()>
    Public Sub DisposeHandlerTest()
        Dim p = New HoldPoint(Of Object)()
        Dim flag = 0
        Dim f = New TaskCompletionSource(Of Boolean)()
        p.IncludeFutureHandler(Function() f.Task).Dispose()
        p.IncludeActionHandler(Sub() flag += 1).Dispose()
        Dim r = p.Hold(Nothing)
        WaitUntilTaskSucceeds(r)
        Assert.IsTrue(flag = 0)
    End Sub
End Class
