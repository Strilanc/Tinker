Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Strilbrary.Values

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

    <TestMethod()>
    Public Sub TimesTest()
        Assert.IsTrue(5.Seconds.Times(5) = 25.Seconds)
    End Sub

    <TestMethod()>
    Public Sub ToUValueTest()
        Assert.IsTrue(New Byte() {3, 4, 5}.ToUValue = &H50403)
        Assert.IsTrue(New Byte() {3, 4, 5}.ToUValue(Strilbrary.Values.ByteOrder.BigEndian) = &H30405)
    End Sub
    <TestMethod()>
    Public Sub FindFileMatchingTest()
    End Sub

    <TestMethod()>
    Public Sub HasBitSetTest()
        Assert.IsTrue(CByte(1).HasBitSet(0))
        Assert.IsTrue(1US.HasBitSet(0))
        Assert.IsTrue(1UI.HasBitSet(0))
        Assert.IsTrue(1UL.HasBitSet(0))

        Assert.IsTrue(CByte(2).HasBitSet(1))
        Assert.IsTrue(2US.HasBitSet(1))
        Assert.IsTrue(2UI.HasBitSet(1))
        Assert.IsTrue(2UL.HasBitSet(1))

        Assert.IsTrue(Not CByte(2).HasBitSet(2))
        Assert.IsTrue(Not 2US.HasBitSet(2))
        Assert.IsTrue(Not 2UI.HasBitSet(2))
        Assert.IsTrue(Not 2UL.HasBitSet(2))
    End Sub
    <TestMethod()>
    Public Sub WithBitSetTo()
        Assert.IsTrue(CByte(1).WithBitSetTo(2, True) = 5)
        Assert.IsTrue(1US.WithBitSetTo(2, True) = 5)
        Assert.IsTrue(1UI.WithBitSetTo(2, True) = 5)
        Assert.IsTrue(1UL.WithBitSetTo(2, True) = 5)

        Assert.IsTrue(CByte(1).WithBitSetTo(0, False) = 0)
        Assert.IsTrue(1US.WithBitSetTo(0, False) = 0)
        Assert.IsTrue(1UI.WithBitSetTo(0, False) = 0)
        Assert.IsTrue(1UL.WithBitSetTo(0, False) = 0)
    End Sub
    <Flags()>
    Private Enum F32 As UInteger
        f1 = 1 << 1
        f2 = 1 << 2
        f3 = 1 << 3
    End Enum
    <TestMethod()>
    Public Sub EnumIncludesTest()
        Assert.IsTrue((F32.f1 Or F32.f2).EnumIncludes(F32.f1))
        Assert.IsTrue((F32.f1 Or F32.f2).EnumIncludes(F32.f2))
        Assert.IsTrue(Not (F32.f1 Or F32.f2).EnumIncludes(F32.f3))

        Assert.IsTrue((F32.f1 Or F32.f2).EnumUInt32Includes(F32.f1))
        Assert.IsTrue((F32.f1 Or F32.f2).EnumUInt32Includes(F32.f2))
        Assert.IsTrue(Not (F32.f1 Or F32.f2).EnumUInt32Includes(F32.f3))
    End Sub
    <TestMethod()>
    Public Sub EnumWithTest()
        Assert.IsTrue(F32.f1.EnumWith(F32.f2) = (F32.f1 Or F32.f2))
        Assert.IsTrue(F32.f1.EnumWith(F32.f1) = F32.f1)
    End Sub
    <TestMethod()>
    Public Sub EnumWithSetTest()
        Assert.IsTrue(F32.f1.EnumUInt32WithSet(F32.f2, True) = (F32.f1 Or F32.f2))
        Assert.IsTrue(F32.f1.EnumUInt32WithSet(F32.f2, False) = F32.f1)
        Assert.IsTrue(F32.f1.EnumUInt32WithSet(F32.f1, False) = 0)
    End Sub
    <TestMethod()>
    Public Sub BitsTest()
        Assert.IsTrue(CByte(5).Bits.SequenceEqual({True, False, True}.Concat(False.Repeated(8 - 3))))
        Assert.IsTrue(5US.Bits.SequenceEqual({True, False, True}.Concat(False.Repeated(16 - 3))))
        Assert.IsTrue(5UI.Bits.SequenceEqual({True, False, True}.Concat(False.Repeated(32 - 3))))
        Assert.IsTrue(5UL.Bits.SequenceEqual({True, False, True}.Concat(False.Repeated(64 - 3))))
    End Sub

    <TestMethod()>
    Public Sub ToUnsignedBigIntegerTest()
        Assert.IsTrue(New Byte() {}.ToUnsignedBigInteger = 0)
        Assert.IsTrue(New Byte() {2, 1}.ToUnsignedBigInteger = 258)
        Assert.IsTrue(New Byte() {2, 1}.ToUnsignedBigInteger(base:=10) = 12)
    End Sub
    <TestMethod()>
    Public Sub UnsignedDigitsTest()
        Assert.IsTrue(New Numerics.BigInteger(12).UnsignedDigits(base:=10).SequenceEqual({2, 1}))
        Assert.IsTrue(New Numerics.BigInteger(258).ToUnsignedBytes.SequenceEqual({2, 1}))
    End Sub
    <TestMethod()>
    Public Sub ConvertFromBaseToBaseTest()
        Assert.IsTrue(New Byte() {2, 1}.ConvertFromBaseToBase(256, 10).SequenceEqual({8, 5, 2}))
    End Sub

    <TestMethod()>
    Public Sub PaddedToTest()
        Assert.IsTrue(New Byte() {5, 6}.PaddedTo(1).SequenceEqual({5, 6}))
        Assert.IsTrue(New Byte() {}.PaddedTo(1).SequenceEqual({0}))
    End Sub

    <TestMethod()>
    Public Sub SHA1Test()
        Assert.IsTrue(Strilbrary.Values.ToAsciiBytes("The quick brown fox jumps over the lazy dog").SHA1.SequenceEqual({&H2F, &HD4, &HE1, &HC6, &H7A, &H2D, &H28, &HFC, &HED, &H84, &H9E, &HE1, &HBB, &H76, &HE7, &H39, &H1B, &H93, &HEB, &H12}))
    End Sub
    <TestMethod()>
    Public Sub CRC32Test()
        Assert.IsTrue(Strilbrary.Values.ToAsciiBytes("The quick brown fox jumps over the lazy dog").CRC32 = 1095738169)
    End Sub

    <TestMethod()>
    Public Sub TeamVersusStringToTeamSizesTest()
        Assert.IsTrue(TeamVersusStringToTeamSizes("5v5").SequenceEqual({5, 5}))
    End Sub
End Class
