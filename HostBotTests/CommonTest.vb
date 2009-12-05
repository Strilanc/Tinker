Imports Strilbrary
Imports Strilbrary.Streams
Imports Strilbrary.Threading
Imports Strilbrary.Enumeration
Imports Strilbrary.Numerics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker
Imports Tinker.Commands

<TestClass()>
Public Class CommonTest
    <TestMethod()>
    Public Sub SplitTextTest_WordSplit()
        Assert.IsTrue(SplitText("test", maxLineLength:=5).HasSameItemsAs({"test"}))
        Assert.IsTrue(SplitText("test", maxLineLength:=4).HasSameItemsAs({"test"}))
        Assert.IsTrue(SplitText("test", maxLineLength:=3).HasSameItemsAs({"tes", "t"}))
        Assert.IsTrue(SplitText("test", maxLineLength:=2).HasSameItemsAs({"te", "st"}))
        Assert.IsTrue(SplitText("test", maxLineLength:=1).HasSameItemsAs({"t", "e", "s", "t"}))
    End Sub

    <TestMethod()>
    Public Sub SplitTextTest_WordBoundary()
        Assert.IsTrue(SplitText("test test test", maxLineLength:=4).HasSameItemsAs({"test", "test", "test"}))
        Assert.IsTrue(SplitText("test test test test test", maxLineLength:=5).HasSameItemsAs({"test", "test", "test", "test", "test"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=10).HasSameItemsAs({"tes tested"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=6).HasSameItemsAs({"tes", "tested"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=5).HasSameItemsAs({"tes t", "ested"}))
        Assert.IsTrue(SplitText("tes test-", maxLineLength:=5).HasSameItemsAs({"tes", "test-"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=4).HasSameItemsAs({"tes", "test", "ed"}))
        Assert.IsTrue(SplitText("abcd abcdef abc     ", maxLineLength:=4).HasSameItemsAs({"abcd", "abcd", "ef", "abc ", "   "}))
        Assert.IsTrue(SplitText(" a ", maxLineLength:=1).HasSameItemsAs({"", "a", ""}))
        Assert.IsTrue(SplitText("   ", maxLineLength:=1).HasSameItemsAs({" ", " "}))
        Assert.IsTrue(SplitText("  ", maxLineLength:=1).HasSameItemsAs({" ", ""}))
        Assert.IsTrue(SplitText(" a a ", maxLineLength:=1).HasSameItemsAs({"", "a", "a", ""}))
    End Sub

    <TestMethod()>
    Public Sub SplitTextTest_LineBoundary()
        Assert.IsTrue(SplitText("tes" + Environment.NewLine + "tes", maxLineLength:=10).HasSameItemsAs({"tes", "tes"}))
        Assert.IsTrue(SplitText("tes" + Environment.NewLine + "tested", maxLineLength:=4).HasSameItemsAs({"tes", "test", "ed"}))
    End Sub
End Class
