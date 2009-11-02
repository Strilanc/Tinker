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
Public Class CommandArgumentsTest
    <TestMethod()>
    Public Sub EmptyTest()
        Dim result = New CommandArguments("")
        Assert.IsTrue(result.RawArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.OptionalArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.NamedOptionalArguments.Count = 0)
        Assert.IsTrue(result.NamedArguments.Count = 0)
    End Sub

    <TestMethod()>
    Public Sub SpaceTest()
        Dim result = New CommandArguments(" ")
        Assert.IsTrue(result.RawArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.OptionalArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.NamedOptionalArguments.Count = 0)
        Assert.IsTrue(result.NamedArguments.Count = 0)

        result = New CommandArguments(" test ")
        Assert.IsTrue(result.RawArguments.HasSameItemsAs({"test"}))
        Assert.IsTrue(result.OptionalArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.NamedOptionalArguments.Count = 0)
        Assert.IsTrue(result.NamedArguments.Count = 0)
    End Sub

    <TestMethod()>
    Public Sub RawTest()
        Dim result = New CommandArguments("test")
        Assert.IsTrue(result.RawArguments.HasSameItemsAs({"test"}))
        Assert.IsTrue(result.OptionalArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.NamedOptionalArguments.Count = 0)
        Assert.IsTrue(result.NamedArguments.Count = 0)
    End Sub

    <TestMethod()>
    Public Sub OptionalTest()
        Dim result = New CommandArguments("-test")
        Assert.IsTrue(result.RawArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.OptionalArguments.HasSameItemsAs({"test"}))
        Assert.IsTrue(result.NamedOptionalArguments.Count = 0)
        Assert.IsTrue(result.NamedArguments.Count = 0)
    End Sub

    <TestMethod()>
    Public Sub NamedTest()
        Dim result = New CommandArguments("test=1")
        Assert.IsTrue(result.RawArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.OptionalArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.NamedOptionalArguments.Count = 0)
        Assert.IsTrue(result.NamedArguments.Count = 1)
        Assert.IsTrue(result.NamedArguments.ContainsKey("test"))
        Assert.IsTrue(result.NamedArguments("test") = "1")
    End Sub

    <TestMethod()>
    Public Sub OptionalNamedTest()
        Dim result = New CommandArguments("-test=2")
        Assert.IsTrue(result.RawArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.OptionalArguments.HasSameItemsAs({}))
        Assert.IsTrue(result.NamedArguments.Count = 0)
        Assert.IsTrue(result.NamedOptionalArguments.Count = 1)
        Assert.IsTrue(result.NamedOptionalArguments.ContainsKey("test"))
        Assert.IsTrue(result.NamedOptionalArguments("test") = "2")
    End Sub

    <TestMethod()>
    Public Sub ComboTest()
        Dim result = New CommandArguments("test1 -test2 test3=test4 -test5=test6")
        Assert.IsTrue(result.RawArguments.HasSameItemsAs({"test1"}))
        Assert.IsTrue(result.OptionalArguments.HasSameItemsAs({"test2"}))
        Assert.IsTrue(result.NamedArguments.Count = 1)
        Assert.IsTrue(result.NamedArguments("test3") = "test4")
        Assert.IsTrue(result.NamedOptionalArguments.Count = 1)
        Assert.IsTrue(result.NamedOptionalArguments("test5") = "test6")
    End Sub

    <TestMethod()>
    Public Sub DelimitedTest()
        Dim result = New CommandArguments("test1=<test> test2=<test test> test3=(test test) test4={test test} test5=[test test]")
        Assert.IsTrue(result.NamedArguments("test1") = "test")
        Assert.IsTrue(result.NamedArguments("test2") = "test test")
        Assert.IsTrue(result.NamedArguments("test3") = "test test")
        Assert.IsTrue(result.NamedArguments("test4") = "test test")
        Assert.IsTrue(result.NamedArguments("test5") = "test test")
    End Sub

    <TestMethod()>
    <ExpectedException(GetType(ArgumentException))>
    Public Sub DuplicateTest()
        Dim result = New CommandArguments("test=1 test=2")
    End Sub

    <TestMethod()>
    <ExpectedException(GetType(ArgumentException))>
    Public Sub OptionalDuplicateTest()
        Dim result = New CommandArguments("-test=1 -test=2")
    End Sub

    <TestMethod()>
    <ExpectedException(GetType(ArgumentException))>
    Public Sub UnterminatedTest()
        Dim result = New CommandArguments("test=(1")
    End Sub
End Class
