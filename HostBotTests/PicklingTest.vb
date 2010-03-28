Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Strilbrary.Values
Imports Tinker.Pickling
Imports System.Collections.Generic

<TestClass()>
Public Class PicklingTest
#Region "Numeric Jars"
    <TestMethod()>
    Public Sub ByteJarTest()
        Dim jar = New ByteJar()
        JarTest(jar, 0, {0})
        JarTest(jar, 1, {1})
        JarTest(jar, Byte.MaxValue, {Byte.MaxValue})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))

        jar = New ByteJar(showHex:=True)
        JarTest(jar, 0, {0})
        JarTest(jar, 1, {1})
        JarTest(jar, Byte.MaxValue, {Byte.MaxValue})
    End Sub
    <TestMethod()>
    Public Sub UInt16JarTest()
        Dim jar = New UInt16Jar()
        JarTest(jar, 0, {0, 0})
        JarTest(jar, 1, {1, 0})
        JarTest(jar, 256, {0, 1})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))

        jar = New UInt16Jar(showHex:=True)
        JarTest(jar, 0, {0, 0})
        JarTest(jar, 1, {1, 0})
        JarTest(jar, 256, {0, 1})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF})

        jar = New UInt16Jar(ByteOrder:=ByteOrder.BigEndian)
        JarTest(jar, 0, {0, 0})
        JarTest(jar, 1, {0, 1})
        JarTest(jar, 256, {1, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF})
    End Sub
    <TestMethod()>
    Public Sub UInt32JarTest()
        Dim jar = New UInt32Jar()
        JarTest(jar, 0, {0, 0, 0, 0})
        JarTest(jar, 1, {1, 0, 0, 0})
        JarTest(jar, 256, {0, 1, 0, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF, 0, 0})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3}.AsReadableList))

        jar = New UInt32Jar(showHex:=True)
        JarTest(jar, 0, {0, 0, 0, 0})
        JarTest(jar, 1, {1, 0, 0, 0})
        JarTest(jar, 256, {0, 1, 0, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF, 0, 0})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF})

        jar = New UInt32Jar(ByteOrder:=ByteOrder.BigEndian)
        JarTest(jar, 0, {0, 0, 0, 0})
        JarTest(jar, 1, {0, 0, 0, 1})
        JarTest(jar, 256, {0, 0, 1, 0})
        JarTest(jar, UInt16.MaxValue, {0, 0, &HFF, &HFF})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF})
    End Sub
    <TestMethod()>
    Public Sub UInt64JarTest()
        Dim jar = New UInt64Jar()
        JarTest(jar, 0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 1, {1, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 256, {0, 1, 0, 0, 0, 0, 0, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF, 0, 0, 0, 0, 0, 0})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF, 0, 0, 0, 0})
        JarTest(jar, UInt64.MaxValue, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3, 4, 5, 6, 7}.AsReadableList))

        jar = New UInt64Jar(showHex:=True)
        JarTest(jar, 0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 1, {1, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 256, {0, 1, 0, 0, 0, 0, 0, 0})
        JarTest(jar, UInt16.MaxValue, {&HFF, &HFF, 0, 0, 0, 0, 0, 0})
        JarTest(jar, UInt32.MaxValue, {&HFF, &HFF, &HFF, &HFF, 0, 0, 0, 0})
        JarTest(jar, UInt64.MaxValue, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})

        jar = New UInt64Jar(ByteOrder:=ByteOrder.BigEndian)
        JarTest(jar, 0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, 1, {0, 0, 0, 0, 0, 0, 0, 1})
        JarTest(jar, 256, {0, 0, 0, 0, 0, 0, 1, 0})
        JarTest(jar, UInt16.MaxValue, {0, 0, 0, 0, 0, 0, &HFF, &HFF})
        JarTest(jar, UInt32.MaxValue, {0, 0, 0, 0, &HFF, &HFF, &HFF, &HFF})
        JarTest(jar, UInt64.MaxValue, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})
    End Sub
    <TestMethod()>
    Public Sub Float32JarTest()
        Dim jar = New Float32Jar()
        Dim m = New IO.MemoryStream()
        Dim br = New IO.BinaryWriter(m)

        m.Position = 0
        br.Write(CSng(1))
        JarTest(jar, 1, m.ToArray)

        m.Position = 0
        br.Write(CSng(2))
        JarTest(jar, 2, m.ToArray)

        m.Position = 0
        br.Write(Single.PositiveInfinity)
        JarTest(jar, Single.PositiveInfinity, m.ToArray)

        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub Float64JarTest()
        Dim jar = New Float64Jar()
        Dim m = New IO.MemoryStream()
        Dim br = New IO.BinaryWriter(m)

        m.Position = 0
        br.Write(CDbl(1))
        JarTest(jar, 1, m.ToArray)

        m.Position = 0
        br.Write(CDbl(2))
        JarTest(jar, 2, m.ToArray)

        m.Position = 0
        br.Write(Double.PositiveInfinity)
        JarTest(jar, Double.PositiveInfinity, m.ToArray)

        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3, 4, 5, 6, 7}.AsReadableList))
    End Sub
#End Region

#Region "Enum"
    Private Enum E8 As Byte
        E0 = 0
        E3 = 3
    End Enum
    <Flags()>
    Private Enum F8 As Byte
        F1 = 1 << 1
        F7 = 1 << 7
    End Enum
    <TestMethod()>
    Public Sub EnumByteJarTest_Value()
        Dim jar = New EnumByteJar(Of E8)(checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, E8.E0, {0})
        JarTest(jar, Function(e1, e2) e1 = e2, E8.E3, {3})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        jar.Parse(New Byte() {2}.AsReadableList)

        jar = New EnumByteJar(Of E8)(checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, E8.E0, {0})
        JarTest(jar, Function(e1, e2) e1 = e2, E8.E3, {3})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {2}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub EnumByteJarTest_Flags()
        Dim jar = New EnumByteJar(Of F8)(checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F1, {2})
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F7, {128})
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F1 Or F8.F7, {130})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        jar.Parse(New Byte() {1}.AsReadableList)
        jar.Parse(New Byte() {3}.AsReadableList)

        jar = New EnumByteJar(Of F8)(checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F1, {2})
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F7, {128})
        JarTest(jar, Function(e1, e2) e1 = e2, F8.F1 Or F8.F7, {130})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {3}.AsReadableList))
    End Sub
    Private Enum E16 As UInt16
        E0 = 0
        E3 = 3
        EM = UInt16.MaxValue
    End Enum
    <Flags()>
    Private Enum F16 As UInt16
        F1 = 1 << 1
        F7 = 1 << 7
        FM = 1US << 15
    End Enum
    <TestMethod()>
    Public Sub EnumUInt16JarTest_Value()
        Dim jar = New EnumUInt16Jar(Of E16)(checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, E16.E0, {0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E16.E3, {3, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E16.EM, {&HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0}.AsReadableList))
        jar.Parse(New Byte() {2, 0}.AsReadableList)

        jar = New EnumUInt16Jar(Of E16)(checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, E16.E0, {0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E16.E3, {3, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E16.EM, {&HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {2, 0}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub EnumUInt16JarTest_Flags()
        Dim jar = New EnumUInt16Jar(Of F16)(checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F1, {2, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F7, {128, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F1 Or F16.F7, {130, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.FM, {0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0}.AsReadableList))
        jar.Parse(New Byte() {1, 0}.AsReadableList)
        jar.Parse(New Byte() {3, 0}.AsReadableList)

        jar = New EnumUInt16Jar(Of F16)(checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F1, {2, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F7, {128, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.F1 Or F16.F7, {130, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F16.FM, {0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {3, 0}.AsReadableList))
    End Sub
    Private Enum E32 As UInt32
        E0 = 0
        E3 = 3
        EM = UInt32.MaxValue
    End Enum
    <Flags()>
    Private Enum F32 As UInt32
        F1 = 1 << 1
        F7 = 1 << 7
        FM = 1UI << 31
    End Enum
    <TestMethod()>
    Public Sub EnumUInt32JarTest_Value()
        Dim jar = New EnumUInt32Jar(Of E32)(checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, E32.E0, {0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E32.E3, {3, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E32.EM, {&HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0}.AsReadableList))
        jar.Parse(New Byte() {2, 0, 0, 0}.AsReadableList)

        jar = New EnumUInt32Jar(Of E32)(checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, E32.E0, {0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E32.E3, {3, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E32.EM, {&HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {2, 0, 0, 0}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub EnumUInt32JarTest_Flags()
        Dim jar = New EnumUInt32Jar(Of F32)(checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F1, {2, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F7, {128, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F1 Or F32.F7, {130, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.FM, {0, 0, 0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        jar.Parse(New Byte() {1, 0, 0, 0}.AsReadableList)
        jar.Parse(New Byte() {3, 0, 0, 0}.AsReadableList)

        jar = New EnumUInt32Jar(Of F32)(checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F1, {2, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F7, {128, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.F1 Or F32.F7, {130, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F32.FM, {0, 0, 0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {3, 0, 0, 0}.AsReadableList))
    End Sub
    Private Enum E64 As UInt64
        E0 = 0
        E3 = 3
        EM = UInt64.MaxValue
    End Enum
    <Flags()>
    Private Enum F64 As UInt64
        F1 = 1 << 1
        F7 = 1 << 7
        FM = 1UL << 63
    End Enum
    <TestMethod()>
    Public Sub EnumUInt64JarTest_Value()
        Dim jar = New EnumUInt64Jar(Of E64)(checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, E64.E0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E64.E3, {3, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E64.EM, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        jar.Parse(New Byte() {2, 0, 0, 0, 0, 0, 0, 0}.AsReadableList)

        jar = New EnumUInt64Jar(Of E64)(checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, E64.E0, {0, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E64.E3, {3, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, E64.EM, {&HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {2, 0, 0, 0, 0, 0, 0, 0}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub EnumUInt64JarTest_Flags()
        Dim jar = New EnumUInt64Jar(Of F64)(checkDefined:=False)
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F1, {2, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F7, {128, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F1 Or F64.F7, {130, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.FM, {0, 0, 0, 0, 0, 0, 0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        jar.Parse(New Byte() {1, 0, 0, 0, 0, 0, 0, 0}.AsReadableList)
        jar.Parse(New Byte() {3, 0, 0, 0, 0, 0, 0, 0}.AsReadableList)

        jar = New EnumUInt64Jar(Of F64)(checkDefined:=True)
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F1, {2, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F7, {128, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.F1 Or F64.F7, {130, 0, 0, 0, 0, 0, 0, 0})
        JarTest(jar, Function(e1, e2) e1 = e2, F64.FM, {0, 0, 0, 0, 0, 0, 0, &H80})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0, 0, 0, 0, 0, 0, 0}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {3, 0, 0, 0, 0, 0, 0, 0}.AsReadableList))
    End Sub
#End Region

    <TestMethod()>
    Public Sub DataJarTest()
        Dim jar = New DataJar()
        Dim equater As Func(Of IReadableList(Of Byte), IReadableList(Of Byte), Boolean) = Function(x, y) x.SequenceEqual(y)
        JarTest(jar, equater, New Byte() {}.AsReadableList, {}, appendSafe:=False)
        JarTest(jar, equater, New Byte() {1}.AsReadableList, {1}, appendSafe:=False)
        JarTest(jar, equater, New Byte() {1, 2, 3}.AsReadableList, {1, 2, 3}, appendSafe:=False)
        JarTest(jar, equater, New Byte() {7, 6, 5}.AsReadableList, {7, 6, 5}, appendSafe:=False)
    End Sub

    <TestMethod()>
    Public Sub ItemCountPrefixedFramingJarTest()
        Dim jar = New ItemCountPrefixedFramingJar(Of UInt16)(New UInt16Jar(), prefixSize:=1)
        Dim equater As Func(Of IReadableList(Of UInt16), IReadableList(Of UInt16), Boolean) = Function(x, y) x.SequenceEqual(y)
        JarTest(jar, equater, New UShort() {}.ToReadableList, {0})
        JarTest(jar, equater, New UShort() {0}.ToReadableList, {1, 0, 0})
        JarTest(jar, equater, New UShort() {1}.ToReadableList, {1, 1, 0})
        JarTest(jar, equater, New UShort() {1, 2, UInt16.MaxValue}.ToReadableList, {3, 1, 0, 2, 0, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))

        jar = New ItemCountPrefixedFramingJar(Of UInt16)(New UInt16Jar(), prefixSize:=4)
        JarTest(jar, equater, New UShort() {}.ToReadableList, {0, 0, 0, 0})
        JarTest(jar, equater, New UShort() {0}.ToReadableList, {1, 0, 0, 0, 0, 0})
        JarTest(jar, equater, New UShort() {1}.ToReadableList, {1, 0, 0, 0, 1, 0})
        JarTest(jar, equater, New UShort() {1, 2, UInt16.MaxValue}.ToReadableList, {3, 0, 0, 0, 1, 0, 2, 0, &HFF, &HFF})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 0, 0, 0, 5}.AsReadableList))
    End Sub
    <TestMethod()>
    Public Sub RepeatedFramingJarTest()
        Dim jar = New RepeatedFramingJar(Of UInt16)(New UInt16Jar())
        Dim equater As Func(Of IReadableList(Of UInt16), IReadableList(Of UInt16), Boolean) = Function(x, y) x.SequenceEqual(y)
        JarTest(jar, equater, New UInt16() {}.AsReadableList, {}, appendSafe:=False)
        JarTest(jar, equater, New UInt16() {0}.AsReadableList, {0, 0}, appendSafe:=False)
        JarTest(jar, equater, New UInt16() {1}.AsReadableList, {1, 0}, appendSafe:=False)
        JarTest(jar, equater, New UInt16() {1, 2, UInt16.MaxValue}.AsReadableList, {1, 0, 2, 0, &HFF, &HFF}, appendSafe:=False)
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3}.AsReadableList))
    End Sub

    <TestMethod()>
    Public Sub StringJarTest()
        Dim jar As StringJar = New UTF8Jar()
        JarTest(jar, "", {}, appendSafe:=False, requireAllData:=True)
        JarTest(jar, "a", {Asc("a")}, appendSafe:=False, requireAllData:=True)
        JarTest(jar, "ab", {Asc("a"), Asc("b")}, appendSafe:=False, requireAllData:=True)
        jar = New UTF8Jar(minCharCount:=2)
        ExpectException(Of PicklingException)(Sub() jar.Pack(""))
        ExpectException(Of PicklingException)(Sub() jar.Pack("a"))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {Asc("a")}.AsReadableList))
        JarTest(jar, "abc", {Asc("a"), Asc("b"), Asc("c")}, appendSafe:=False, requireAllData:=True)
        jar = New UTF8Jar(maxCharCount:=1)
        ExpectException(Of PicklingException)(Sub() jar.Pack("ab"))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {Asc("a"), Asc("b")}.AsReadableList))
        JarTest(jar, "", {}, appendSafe:=False, requireAllData:=True)
        JarTest(jar, "a", {Asc("a")}, appendSafe:=False, requireAllData:=True)
        jar = New ASCIIJar()
        JarTest(jar, "", {}, appendSafe:=False, requireAllData:=True)
        JarTest(jar, "a", {Asc("a")}, appendSafe:=False, requireAllData:=True)
        JarTest(jar, "ab", {Asc("a"), Asc("b")}, appendSafe:=False, requireAllData:=True)
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {255}.AsReadableList))
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {128}.AsReadableList))
        jar.Parse(New Byte() {127}.AsReadableList)
        jar.Parse(New Byte() {31}.AsReadableList)
        jar.Parse(New Byte() {0}.AsReadableList)
    End Sub

    <TestMethod()>
    Public Sub TupleJarTest()
        Dim jar = New TupleJar(New UInt32Jar().Named("32"), New UInt16Jar().Named("16"))
        JarTest(jar, New Dictionary(Of InvariantString, Object)() From {{"32", 0UI}, {"16", 0US}}, {0, 0, 0, 0, 0, 0})
        JarTest(jar, New Dictionary(Of InvariantString, Object)() From {{"32", UInt32.MaxValue}, {"16", 0US}}, {&HFF, &HFF, &HFF, &HFF, 0, 0})
        JarTest(jar, New Dictionary(Of InvariantString, Object)() From {{"32", 0UI}, {"16", UInt16.MaxValue}}, {0, 0, 0, 0, &HFF, &HFF})
        JarTest(jar, New Dictionary(Of InvariantString, Object)() From {{"32", 1UI}, {"16", 2US}}, {1, 0, 0, 0, 2, 0})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {1, 2, 3, 4, 5}.AsReadableList))
    End Sub

    <TestMethod()>
    Public Sub DataSizePrefixedJarTest()
        Dim jar = New SizePrefixedFramingJar(Of Byte)(New ByteJar(), prefixSize:=2)
        JarTest(jar, 5, {1, 0, 5})
        JarTest(jar, 3, {1, 0, 3})

        Dim jar2 = New SizePrefixedFramingJar(Of String)(New UTF8Jar().NullTerminated, prefixSize:=1)
        JarTest(jar2, "", {1, 0})
        JarTest(jar2, "A", {2, Asc("A"), 0})
        JarTest(jar2, New String("A"c, 254), New Byte() {255}.Concat(CByte(Asc("A")).Repeated(254)).Concat({0}).ToList)
        ExpectException(Of PicklingException)(Sub() jar2.Pack(New String("A"c, 255)))
    End Sub

    <TestMethod()>
    Public Sub OptionalJarTest()
        Dim jar = New OptionalFramingJar(Of Byte)(New ByteJar())
        Dim equater = Function(e1 As Tuple(Of Boolean, Byte), e2 As Tuple(Of Boolean, Byte)) e1.Equals(e2)
        JarTest(jar, equater, Tuple.Create(True, CByte(5)), {5})
        JarTest(jar, equater, Tuple.Create(False, CByte(0)), {}, appendSafe:=False)

        Dim jar2 = New OptionalFramingJar(Of String)(New UTF8Jar().NullTerminated)
        Dim equater2 = Function(e1 As Tuple(Of Boolean, String), e2 As Tuple(Of Boolean, String)) e1.Equals(e2)
        JarTest(jar2, equater2, Tuple.Create(True, ""), {0})
        JarTest(jar2, equater2, Tuple.Create(False, CStr(Nothing)), {}, appendSafe:=False)
        JarTest(jar2, equater2, Tuple.Create(True, "A"), {Asc("A"), 0})
        ExpectException(Of PicklingException)(Sub() jar2.Parse(New Byte() {92}.AsReadableList))
    End Sub

    <TestMethod()>
    Public Sub ChecksumPrefixedJarTest()
        Dim jar = New ChecksumPrefixedFramingJar(Of Byte)(New ByteJar(), checksumSize:=1, checksumFunction:=Function(a) {a(0) Xor CByte(1)}.AsReadableList)
        JarTest(jar, 5, {4, 5})
        JarTest(jar, 3, {2, 3})
        JarTest(jar, 2, {3, 2})
        ExpectException(Of PicklingException)(Sub() jar.Parse(New Byte() {3, 3}.AsReadableList))
    End Sub

    <TestMethod()>
    Public Sub KeyPrefixedJarTest()
        Dim jar = New KeyPrefixedJar(Of UInt32)(keyJar:=New UInt32Jar(), valueJars:=New Dictionary(Of UInt32, NonNull(Of ISimpleJar)) From {
                                                    {3, New ByteJar()},
                                                    {5, New UInt16Jar()}})
        JarTest(jar,
                equater:=Function(e1, e2) e1.Key = e2.Key AndAlso ObjectEqual(e1.Value.Value, e2.Value.Value),
                value:=New KeyValuePair(Of UInt32, ISimplePickle)(3UI, CByte(10).Pickled(New ByteJar(), New Byte() {}.AsReadableList)),
                data:={3, 0, 0, 0,
                       10})
        JarTest(jar,
                equater:=Function(e1, e2) e1.Key = e2.Key AndAlso ObjectEqual(e1.Value.Value, e2.Value.Value),
                value:=New KeyValuePair(Of UInt32, ISimplePickle)(5UI, 10US.Pickled(New UInt16Jar(), New Byte() {}.AsReadableList)),
                data:={5, 0, 0, 0,
                       10, 0})
    End Sub
End Class
