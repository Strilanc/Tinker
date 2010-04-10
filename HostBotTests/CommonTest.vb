Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker

<TestClass()>
Public Class CommonTest
    <TestMethod()>
    Public Sub SplitTextTest_WordSplit()
        Assert.IsTrue(SplitText("test", maxLineLength:=5).SequenceEqual({"test"}))
        Assert.IsTrue(SplitText("test", maxLineLength:=4).SequenceEqual({"test"}))
        Assert.IsTrue(SplitText("test", maxLineLength:=3).SequenceEqual({"tes", "t"}))
        Assert.IsTrue(SplitText("test", maxLineLength:=2).SequenceEqual({"te", "st"}))
        Assert.IsTrue(SplitText("test", maxLineLength:=1).SequenceEqual({"t", "e", "s", "t"}))
    End Sub

    <TestMethod()>
    Public Sub SplitTextTest_WordBoundary()
        Assert.IsTrue(SplitText("test test test", maxLineLength:=4).SequenceEqual({"test", "test", "test"}))
        Assert.IsTrue(SplitText("test test test test test", maxLineLength:=5).SequenceEqual({"test", "test", "test", "test", "test"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=10).SequenceEqual({"tes tested"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=6).SequenceEqual({"tes", "tested"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=5).SequenceEqual({"tes t", "ested"}))
        Assert.IsTrue(SplitText("tes test-", maxLineLength:=5).SequenceEqual({"tes", "test-"}))
        Assert.IsTrue(SplitText("tes tested", maxLineLength:=4).SequenceEqual({"tes", "test", "ed"}))
        Assert.IsTrue(SplitText("abcd abcdef abc     ", maxLineLength:=4).SequenceEqual({"abcd", "abcd", "ef", "abc ", "   "}))
        Assert.IsTrue(SplitText(" a ", maxLineLength:=1).SequenceEqual({"", "a", ""}))
        Assert.IsTrue(SplitText("   ", maxLineLength:=1).SequenceEqual({" ", " "}))
        Assert.IsTrue(SplitText("  ", maxLineLength:=1).SequenceEqual({" ", ""}))
        Assert.IsTrue(SplitText(" a a ", maxLineLength:=1).SequenceEqual({"", "a", "a", ""}))
    End Sub

    <TestMethod()>
    Public Sub SplitTextTest_LineBoundary()
        Assert.IsTrue(SplitText("tes" + Environment.NewLine + "tes", maxLineLength:=10).SequenceEqual({"tes", "tes"}))
        Assert.IsTrue(SplitText("tes" + Environment.NewLine + "tested", maxLineLength:=4).SequenceEqual({"tes", "test", "ed"}))
    End Sub
End Class
