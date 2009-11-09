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
Public Class CommandArgumentTest
    <TestMethod()>
    Public Sub EmptyTest()
        Dim result = New CommandArgument("")
        Assert.IsTrue(result.Count = 0)
    End Sub

    <TestMethod()>
    Public Sub SpaceTest()
        Dim result = New CommandArgument(" ")
        Assert.IsTrue(result.Count = 0)

        result = New CommandArgument(" test ")
        Assert.IsTrue(result.Count = 1)
        Assert.IsTrue(result.TryGetRawValue(0) = "test")
    End Sub

    <TestMethod()>
    Public Sub RawTest()
        Dim result = New CommandArgument("test")
        Assert.IsTrue(result.Count = 1)
        Assert.IsTrue(result.TryGetRawValue(0) = "test")
        Assert.IsTrue(result.TryGetRawValue(1) Is Nothing)
        Assert.IsTrue(result.RawValue(0) = "test")
    End Sub

    <TestMethod()>
    Public Sub OptionalTest()
        Dim result = New CommandArgument("-test")
        Assert.IsTrue(result.Count = 1)
        Assert.IsTrue(result.HasOptionalSwitch("test"))
        Assert.IsTrue(Not result.HasOptionalSwitch("abc"))
    End Sub

    <TestMethod()>
    Public Sub NamedTest()
        Dim result = New CommandArgument("test=1")
        Assert.IsTrue(result.Count = 1)
        Assert.IsTrue(result.NamedValue("test") = "1")
    End Sub

    <TestMethod()>
    Public Sub OptionalNamedTest()
        Dim result = New CommandArgument("-test=2")
        Assert.IsTrue(result.Count = 1)
        Assert.IsTrue(result.TryGetOptionalNamedValue("test") = "2")
        Assert.IsTrue(result.TryGetOptionalNamedValue("xrr") Is Nothing)
    End Sub

    <TestMethod()>
    Public Sub ComboTest()
        Dim result = New CommandArgument("test1 -test2 test3=test4 -test5=test6")
        Assert.IsTrue(result.Count = 4)
        Assert.IsTrue(result.RawValue(0) = "test1")
        Assert.IsTrue(result.HasOptionalSwitch("test2"))
        Assert.IsTrue(result.NamedValue("test3") = "test4")
        Assert.IsTrue(result.TryGetOptionalNamedValue("test5") = "test6")
    End Sub

    <TestMethod()>
    Public Sub DelimitedTest()
        Dim result = New CommandArgument("<test> <test test> <test test test> test1=<test> test2=<test test> test3=(test test) test4={test test} test5=[test test]")
        Assert.IsTrue(result.Count = 8)
        Assert.IsTrue(result.RawValue(0) = "test")
        Assert.IsTrue(result.RawValue(1) = "test test")
        Assert.IsTrue(result.RawValue(2) = "test test test")
        Assert.IsTrue(result.NamedValue("test1") = "test")
        Assert.IsTrue(result.NamedValue("test2") = "test test")
        Assert.IsTrue(result.NamedValue("test3") = "test test")
        Assert.IsTrue(result.NamedValue("test4") = "test test")
        Assert.IsTrue(result.NamedValue("test5") = "test test")
    End Sub

    <TestMethod()>
    <ExpectedException(GetType(ArgumentException))>
    Public Sub DuplicateTest()
        Dim result = New CommandArgument("test=1 test=2")
    End Sub

    <TestMethod()>
    <ExpectedException(GetType(ArgumentException))>
    Public Sub SwitchDuplicateTest()
        Dim result = New CommandArgument("-test -test")
    End Sub

    <TestMethod()>
    <ExpectedException(GetType(ArgumentException))>
    Public Sub OptionalDuplicateTest()
        Dim result = New CommandArgument("-test=1 -test=2")
    End Sub

    <TestMethod()>
    <ExpectedException(GetType(ArgumentException))>
    Public Sub UnterminatedTest()
        Dim result = New CommandArgument("test=(1")
    End Sub

    <TestMethod()>
    Public Sub TokenizeTest()
        Assert.IsTrue(CommandArgument.Tokenize("a b").HasSameItemsAs({"a", "b"}))
        Assert.IsTrue(CommandArgument.Tokenize("<a b>").HasSameItemsAs({"a b"}))
        Assert.IsTrue(CommandArgument.Tokenize("(a b)").HasSameItemsAs({"a b"}))
        Assert.IsTrue(CommandArgument.Tokenize("[a b]").HasSameItemsAs({"a b"}))
        Assert.IsTrue(CommandArgument.Tokenize("{a b}").HasSameItemsAs({"a b"}))
        Assert.IsTrue(CommandArgument.Tokenize("[{a b}]").HasSameItemsAs({"{a b}"}))
        Assert.IsTrue(CommandArgument.Tokenize("t=[{a b}]").HasSameItemsAs({"t={a b}"}))
        Assert.IsTrue(CommandArgument.Tokenize("t=a b").HasSameItemsAs({"t=a", "b"}))
        Assert.IsTrue(CommandArgument.Tokenize("a <b c> d={e f}").HasSameItemsAs({"a", "b c", "d=e f"}))
    End Sub
End Class
