Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Strilbrary.Values
Imports Tinker.Pickling
Imports System.Collections.Generic
Imports TinkerTests.PicklingTest
Imports Tinker.Bnet.Protocol

<TestClass()>
Public Class BnetProtocolJarsTest
    <TestMethod()>
    Public Sub DwordStringJarTest()
        Dim jar = New DwordStringJar("test")
        JarTest(jar, "test", {Asc("t"), Asc("s"), Asc("e"), Asc("t")})
        JarTest(jar, "a", {Asc("a"), 0, 0, 0})
        ExpectException(Of PicklingException)(Sub() jar.Pack("12345"))
    End Sub
    <TestMethod()>
    Public Sub TextHexValueJarTest()
        Dim jar = New TextHexValueJar("test", numDigits:=8, ByteOrder:=ByteOrder.BigEndian)
        JarTest(jar, &HDEADBEEFUI, "deadbeef".ToAscBytes)
        JarTest(jar, 1, "00000001".ToAscBytes)
        JarTest(jar, 0, "00000000".ToAscBytes)
        ExpectException(Of PicklingException)(Sub() jar.Pack(&H100000000UL))
        jar = New TextHexValueJar("test", numDigits:=6, ByteOrder:=ByteOrder.LittleEndian)
        JarTest(jar, &HDEADBEUI, "ebdaed".ToAscBytes)
        JarTest(jar, 1, "100000".ToAscBytes)
        JarTest(jar, 0, "000000".ToAscBytes)
        ExpectException(Of PicklingException)(Sub() jar.Pack(&HDEADBEEFUL))
    End Sub
    <TestMethod()>
    Public Sub FileTimeJar()
        Dim jar = New FileTimeJar("test")
        JarTest(jar, New Date(Year:=2010, Month:=1, Day:=16), {0, 224, 177, 95, 96, 150, 202, 1})
        JarTest(jar, New Date(Year:=2009, Month:=2, Day:=16), {0, 96, 185, 9, 235, 143, 201, 1})
    End Sub
End Class
