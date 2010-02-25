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
        Dim jar = New DwordStringJar()
        JarTest(jar, "test", {Asc("t"), Asc("s"), Asc("e"), Asc("t")})
        JarTest(jar, "a", {Asc("a"), 0, 0, 0})
        ExpectException(Of PicklingException)(Sub() jar.Pack("12345"))
    End Sub
    <TestMethod()>
    Public Sub TextHexValueJarTest()
        Dim jar = New TextHexValueJar("test", digitCount:=8, ByteOrder:=ByteOrder.BigEndian)
        JarTest(jar, &HDEADBEEFUI, "deadbeef".ToAscBytes)
        JarTest(jar, 1, "00000001".ToAscBytes)
        JarTest(jar, 0, "00000000".ToAscBytes)
        ExpectException(Of PicklingException)(Sub() jar.Pack(&H100000000UL))
        jar = New TextHexValueJar("test", digitCount:=6, ByteOrder:=ByteOrder.LittleEndian)
        JarTest(jar, &HDEADBEUI, "ebdaed".ToAscBytes)
        JarTest(jar, 1, "100000".ToAscBytes)
        JarTest(jar, 0, "000000".ToAscBytes)
        ExpectException(Of PicklingException)(Sub() jar.Pack(&HDEADBEEFUL))
    End Sub
    <TestMethod()>
    Public Sub FileTimeJarTest()
        Dim jar = New FileTimeJar("test")
        JarTest(jar, New Date(Year:=2010, Month:=1, Day:=16), {0, 224, 177, 95, 96, 150, 202, 1})
        JarTest(jar, New Date(Year:=2009, Month:=2, Day:=16), {0, 96, 185, 9, 235, 143, 201, 1})
    End Sub
    <TestMethod()>
    Public Sub IPAddressJarTest()
        Dim jar = New IPAddressJar()
        Dim equater = Function(a1 As Net.IPAddress, a2 As Net.IPAddress) a1.GetAddressBytes.SequenceEqual(a1.GetAddressBytes)
        JarTest(jar, equater, New Net.IPAddress({0, 0, 0, 0}), {0, 0, 0, 0})
        JarTest(jar, equater, Net.IPAddress.Loopback, {127, 0, 0, 1})
    End Sub
    <TestMethod()>
    Public Sub IPEndPointTest()
        Dim jar = New IPEndPointJar()
        Dim equater = Function(a1 As Net.IPEndPoint, a2 As Net.IPEndPoint) a1.Address.GetAddressBytes.SequenceEqual(a1.Address.GetAddressBytes) AndAlso a1.Port = a2.Port
        JarTest(jar, equater, New Net.IPEndPoint(New Net.IPAddress({0, 0, 0, 0}), 0), {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, equater, New Net.IPEndPoint(Net.IPAddress.Loopback, 6112), {2, 0, &H17, &HE0, 127, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0})
    End Sub
End Class
