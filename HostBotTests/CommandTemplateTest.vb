Imports Strilbrary
Imports Strilbrary.Streams
Imports Strilbrary.Threading
Imports Strilbrary.Enumeration
Imports Strilbrary.Numerics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports HostBot
Imports HostBot.Commands

<TestClass()>
Public Class CommandTemplateTest
    <TestMethod()>
    Public Sub EmptyTest()
        Dim t = New CommandTemplate("")
        Assert.IsTrue(t.TryFindMismatch("") Is Nothing)
        Assert.IsTrue(t.TryFindMismatch("test") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-test") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-test=1") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("test=1") IsNot Nothing)
    End Sub

    <TestMethod()>
    Public Sub RawTest()
        Dim t = New CommandTemplate("test")
        Assert.IsTrue(t.TryFindMismatch("") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw") Is Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw raw") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-op") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw -op") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-opnm=1") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("name=1") IsNot Nothing)
    End Sub

    <TestMethod()>
    Public Sub OptionalRawTest()
        Dim t = New CommandTemplate("?test")
        Assert.IsTrue(t.TryFindMismatch("") Is Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw") Is Nothing)

        Assert.IsTrue(t.TryFindMismatch("raw raw") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-op") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-test") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw -test") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-opnm=1") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("name=1") IsNot Nothing)
    End Sub

    <TestMethod()>
    Public Sub OptionalSwitchTest()
        Dim t = New CommandTemplate("-test")
        Assert.IsTrue(t.TryFindMismatch("") Is Nothing)
        Assert.IsTrue(t.TryFindMismatch("-test") Is Nothing)

        Assert.IsTrue(t.TryFindMismatch("raw") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw raw") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-op") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw -test") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-opnm=1") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("name=1") IsNot Nothing)
    End Sub

    <TestMethod()>
    Public Sub NamedTest()
        Dim t = New CommandTemplate("name=test")
        Assert.IsTrue(t.TryFindMismatch("name=1") Is Nothing)

        Assert.IsTrue(t.TryFindMismatch("") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw raw") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-op") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-test") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw -test") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-opnm=1") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-name=1") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("name=1 tes") IsNot Nothing)
    End Sub

    <TestMethod()>
    Public Sub OptionalNamedTest()
        Dim t = New CommandTemplate("-name=test")
        Assert.IsTrue(t.TryFindMismatch("-name=1") Is Nothing)
        Assert.IsTrue(t.TryFindMismatch("") Is Nothing)

        Assert.IsTrue(t.TryFindMismatch("raw") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw raw") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-op") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-test") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw -test") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-opnm=1") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("name=1") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-name=<1 2>") Is Nothing)
        Assert.IsTrue(t.TryFindMismatch("-name=1 tes") IsNot Nothing)
    End Sub

    <TestMethod()>
    Public Sub ComboTest()
        Dim t = New CommandTemplate("raw name=1 -op=2 -switch")
        Assert.IsTrue(t.TryFindMismatch("raw name=1 -op=2 -switch") Is Nothing)

        Assert.IsTrue(t.TryFindMismatch("") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("name=1") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-op=2") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("-switch") IsNot Nothing)
        Assert.IsTrue(t.TryFindMismatch("raw raw name=1 -op=2 -switch") IsNot Nothing)
    End Sub
End Class
