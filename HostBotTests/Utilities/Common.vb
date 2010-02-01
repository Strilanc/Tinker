Imports Strilbrary.Threading
Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Strilbrary.Values
Imports Tinker.Pickling
Imports System.Collections.Generic

Friend Module TestingCommon
    Public Sub ExpectException(Of E As Exception)(ByVal action As Action)
        Try
            Call action()
        Catch ex As E
            Return
        Catch ex As Exception
            Assert.Fail("Expected an exception of type " + GetType(E).ToString + " but hit an exception type " + ex.GetType.ToString)
        End Try
        Assert.Fail("Expected an exception of type " + GetType(E).ToString + " but hit no exception.")
    End Sub
    Public Function BlockOnFutureValue(Of T)(ByVal future As IFuture(Of T)) As IFuture(Of T)
        If BlockOnFuture(future) Then
            Return future
        Else
            Throw New InvalidOperationException("The future did not terminate properly.")
        End If
    End Function
    Public Function BlockOnFuture(ByVal future As IFuture) As Boolean
        Return BlockOnFuture(future, New TimeSpan(0, 0, seconds:=100))
    End Function
    Public Function BlockOnFuture(ByVal future As IFuture,
                                  ByVal timeout As TimeSpan) As Boolean
        Dim waitHandle = New System.Threading.ManualResetEvent(initialState:=False)
        AddHandler future.Ready, Sub() waitHandle.Set()
        If future.State <> FutureState.Unknown Then waitHandle.Set()
        Return waitHandle.WaitOne(timeout)
    End Function
    Public Sub WaitUntilFutureSucceeds(ByVal future As IFuture)
        Assert.IsTrue(BlockOnFuture(future))
        Assert.IsTrue(future.State = FutureState.Succeeded)
    End Sub

    Friend Sub JarTest(Of T)(ByVal jar As IJar(Of T),
                             ByVal equater As Func(Of T, T, Boolean),
                             ByVal value As T,
                             ByVal data As IList(Of Byte),
                             Optional ByVal appendSafe As Boolean = True,
                             Optional ByVal requireAllData As Boolean = True,
                             Optional ByVal description As String = Nothing)
        Dim packed = jar.Pack(value)
        Dim parsed = jar.Parse(data.AsReadableList)
        Assert.IsTrue(equater(packed.Value, value))
        Assert.IsTrue(equater(parsed.Value, value))
        Assert.IsTrue(packed.Data.SequenceEqual(data))
        Assert.IsTrue(parsed.Data.SequenceEqual(data))
        If description IsNot Nothing Then
            Assert.IsTrue(packed.Description.Value = description)
            Assert.IsTrue(parsed.Description.Value = description)
        Else
            Assert.IsTrue(packed.Description.Value = parsed.Description.Value)
        End If

        If data.Count > 0 Then
            Try
                jar.Parse(data.Take(data.Count - 1).ToArray.AsReadableList)
                Assert.IsTrue(Not requireAllData)
            Catch ex As Exception
                Assert.IsTrue(requireAllData)
            End Try
        End If
        If appendSafe Then
            Dim data2 = {data, New Byte() {1, 2, 3}}.Fold.ToList
            Dim parsed2 = jar.Parse(data2.AsReadableList)
            Assert.IsTrue(equater(parsed2.Value, value))
            Assert.IsTrue(parsed2.Data.SequenceEqual(data))
            If description IsNot Nothing Then
                Assert.IsTrue(parsed2.Description.Value = description)
            Else
                Assert.IsTrue(packed.Description.Value = parsed2.Description.Value)
            End If
        Else
            Try
                Dim data2 = {data, New Byte() {1, 2, 3}}.Fold.ToList
                Dim parsed2 = jar.Parse(data2.AsReadableList)
                Assert.IsFalse(equater(parsed2.Value, value))
                Assert.IsFalse(parsed2.Data.SequenceEqual(data))
            Catch ex As PicklingException
                'caller indicated this might happen, so it's fine
            End Try
        End If
    End Sub
    Friend Sub JarTest(Of T As IEquatable(Of T))(ByVal jar As IJar(Of T),
                                                 ByVal value As T,
                                                 ByVal data As IList(Of Byte),
                                                 Optional ByVal appendSafe As Boolean = True,
                                                 Optional ByVal requireAllData As Boolean = True,
                                                 Optional ByVal description As String = Nothing)
        JarTest(jar, Function(a As T, b As T) a.Equals(b), value, data, appendSafe, requireAllData, description)
    End Sub
    Friend Sub JarTest(ByVal jar As IJar(Of Dictionary(Of InvariantString, Object)),
                       ByVal value As Dictionary(Of InvariantString, Object),
                       ByVal data As IList(Of Byte),
                       Optional ByVal appendSafe As Boolean = True,
                       Optional ByVal requireAllData As Boolean = True)
        JarTest(jar,
                Function(d1, d2) DictionaryEqual(d1, d2),
                value,
                data,
                requireAllData:=requireAllData,
                appendSafe:=appendSafe)
    End Sub

    Private Function TryCastToBigInteger(ByVal v As Object) As Numerics.BigInteger?
        If TypeOf v Is SByte Then Return CSByte(v)
        If TypeOf v Is Int16 Then Return CShort(v)
        If TypeOf v Is Int32 Then Return CInt(v)
        If TypeOf v Is Int64 Then Return CLng(v)
        If TypeOf v Is Byte Then Return CByte(v)
        If TypeOf v Is UInt16 Then Return CUShort(v)
        If TypeOf v Is UInt32 Then Return CUInt(v)
        If TypeOf v Is UInt64 Then Return CULng(v)
        If TypeOf v Is Numerics.BigInteger Then Return CType(v, Numerics.BigInteger)
        Return Nothing
    End Function
    Private Function ObjectEqual(ByVal v1 As Object, ByVal v2 As Object) As Boolean
        Dim n1 = TryCastToBigInteger(v1)
        Dim n2 = TryCastToBigInteger(v2)
        If n1 IsNot Nothing AndAlso n2 IsNot Nothing Then
            Return n1.Value = n2.Value
        ElseIf TypeOf v1 Is Dictionary(Of InvariantString, Object) AndAlso TypeOf v2 Is Dictionary(Of InvariantString, Object) Then
            Return DictionaryEqual(CType(v1, Dictionary(Of InvariantString, Object)), CType(v2, Dictionary(Of InvariantString, Object)))
        ElseIf TypeOf v1 Is Collections.IEnumerable AndAlso TypeOf v2 Is Collections.IEnumerable Then
            Return ListEqual(CType(v1, Collections.IEnumerable), CType(v2, Collections.IEnumerable))
        Else
            Return v1.Equals(v2)
        End If
    End Function
    Private Function ListEqual(ByVal l1 As Collections.IEnumerable, ByVal l2 As Collections.IEnumerable) As Boolean
        Dim e1 = l1.GetEnumerator
        Dim e2 = l2.GetEnumerator
        Do
            Dim b1 = e1.MoveNext
            Dim b2 = e2.MoveNext
            If b1 <> b2 Then Return False
            If Not b1 Then Return True
            If Not ObjectEqual(e1.Current, e2.Current) Then Return False
        Loop
    End Function
    Friend Function DictionaryEqual(Of TKey, TVal)(ByVal d1 As Dictionary(Of TKey, TVal),
                                                   ByVal d2 As Dictionary(Of TKey, TVal)) As Boolean
        For Each pair In d1
            If Not d2.ContainsKey(pair.Key) Then Return False
            If Not ObjectEqual(d2(pair.Key), pair.Value) Then Return False
        Next pair
        For Each pair In d2
            If Not d1.ContainsKey(pair.Key) Then Return False
            If Not ObjectEqual(d1(pair.Key), pair.Value) Then Return False
        Next pair
        Return True
    End Function
End Module