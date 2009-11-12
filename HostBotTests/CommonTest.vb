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
    Public Sub SplitTextTest_SpaceBoundary()
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=10).HasSameItemsAs({"tes tested"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=6).HasSameItemsAs({"tes", "tested"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=5).HasSameItemsAs({"tes t", "ested"}))
        Assert.IsTrue(SplitText("tes test-", maxLineLength:=5).HasSameItemsAs({"tes", "test-"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=4).HasSameItemsAs({"tes", "test", "ed"}))
    End Sub

    <TestMethod()>
    Public Sub SplitTextTest_LineBoundary()
        Assert.IsTrue(SplitText("tes" + Environment.NewLine + "tes", maxLineLength:=10).HasSameItemsAs({"tes", "tes"}))
        Assert.IsTrue(SplitText("tes" + Environment.NewLine + "tested", maxLineLength:=4).HasSameItemsAs({"tes", "test", "ed"}))
    End Sub
End Class
