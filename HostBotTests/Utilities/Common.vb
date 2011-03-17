Imports System.Runtime.CompilerServices
Imports Strilbrary.Threading
Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Strilbrary.Values
Imports Tinker.Pickling
Imports Tinker
Imports System.Collections.Generic

Friend Module TestingCommon
    Public Function ByteRist(ParamArray values As Byte()) As IRist(Of Byte)
        Return values.AsRist()
    End Function

    <Extension()>
    Public Function FixedClock(t As TimeSpan) As Strilbrary.Time.IClock
        Dim c = New Strilbrary.Time.ManualClock()
        c.Advance(t)
        Return c
    End Function
    <Extension()>
    Public Function WaitValue(Of T)(task As Task(Of T)) As T
        task.Wait()
        Return task.Result
    End Function
    Public Sub ExpectException(Of E As Exception)(action As Action)
        Try
            Call action()
        Catch ex As E
            Return
        Catch ex As Exception
            Assert.Fail("Expected an exception of type " + GetType(E).ToString + " but hit an exception type " + ex.GetType.ToString)
        End Try
        Assert.Fail("Expected an exception of type " + GetType(E).ToString + " but hit no exception.")
    End Sub
    Public Function BlockOnTaskValue(Of T)(task As Task(Of T)) As Task(Of T)
        Dim b As Boolean
        Try
            b = task.Wait(millisecondsTimeout:=10000)
        Catch ex As Exception
            b = True
        End Try
        If b Then
            Return task
        Else
            Throw New InvalidOperationException("The future did not terminate properly.")
        End If
    End Function
    Public Sub WaitUntilTaskSucceeds(task As Task)
        Assert.IsTrue(task.Wait(millisecondsTimeout:=10000))
        Assert.IsTrue(task.Status = TaskStatus.RanToCompletion)
    End Sub
    Public Sub WaitUntilTaskFails(task As Task)
        Try
            Assert.IsTrue(task.Wait(millisecondsTimeout:=10000))
        Catch ex As Exception
        End Try
        Assert.IsTrue(task.Status = TaskStatus.Faulted)
    End Sub
    Public Sub ExpectTaskToIdle(task As Task)
        Assert.IsTrue(Not task.Wait(millisecondsTimeout:=10))
    End Sub

    Friend Sub JarTest(Of T)(jar As IJar(Of T),
                             equater As Func(Of T, T, Boolean),
                             value As T,
                             data As IEnumerable(Of Byte),
                             Optional appendSafe As Boolean = True,
                             Optional requireAllData As Boolean = True,
                             Optional description As String = Nothing)
        Dim parsed = jar.Parse(data.ToRist)
        Assert.IsTrue(equater(parsed.Value, value))
        Assert.IsTrue(parsed.UsedDataCount = data.Count)
        If description IsNot Nothing Then
            Assert.IsTrue(jar.Describe(value) = description)
            Assert.IsTrue(jar.Describe(parsed.Value) = description)
        End If

        Assert.IsTrue(jar.Pack(value).SequenceEqual(data))
        Assert.IsTrue(equater(jar.Parse(jar.Describe(value)), value))

        Dim control = jar.MakeControl()
        control.Value = value
        Assert.IsTrue(equater(control.Value, value))

        If data.Count > 0 Then
            Try
                Dim parsed2 = jar.Parse(data.Take(data.Count - 1).ToRist)
                Assert.IsTrue(Not requireAllData)
            Catch ex As Exception
                Assert.IsTrue(requireAllData)
            End Try
        End If
        If appendSafe Then
            Dim parsed2 = jar.Parse(data.Concat({1, 2, 3}).ToRist)
            Assert.IsTrue(equater(parsed2.Value, value))
            Assert.IsTrue(parsed2.UsedDataCount >= data.Count)
            If description IsNot Nothing Then
                Assert.IsTrue(jar.Describe(parsed2.Value) = description)
            End If
        Else
            Try
                Dim parsed2 = jar.Parse(data.Concat({1, 2, 3}).ToRist)
                Assert.IsFalse(equater(parsed2.Value, value))
                Assert.IsTrue(parsed2.UsedDataCount >= data.Count)
            Catch ex As PicklingException
                'caller indicated this might happen, so it's fine
            End Try
        End If
    End Sub
    Friend Sub JarTest(Of T As IEquatable(Of T))(jar As IJar(Of T),
                                                 value As T,
                                                 data As IEnumerable(Of Byte),
                                                 Optional appendSafe As Boolean = True,
                                                 Optional requireAllData As Boolean = True,
                                                 Optional description As String = Nothing)
        JarTest(jar, Function(a As T, b As T) a.Equals(b), value, data, appendSafe, requireAllData, description)
    End Sub
    Friend Sub JarTest(jar As IJar(Of NamedValueMap),
                       value As NamedValueMap,
                       data As IEnumerable(Of Byte),
                       Optional appendSafe As Boolean = True,
                       Optional requireAllData As Boolean = True)
        JarTest(jar,
                Function(e1 As NamedValueMap, e2 As NamedValueMap) ObjectEqual(e1, e2),
                value,
                data,
                requireAllData:=requireAllData,
                appendSafe:=appendSafe)
    End Sub
    Friend Sub JarTest(jar As IJar(Of NamedValueMap),
                       value As Dictionary(Of InvariantString, Object),
                       data As IEnumerable(Of Byte),
                       Optional appendSafe As Boolean = True,
                       Optional requireAllData As Boolean = True)
        JarTest(jar,
                New NamedValueMap(value),
                data,
                requireAllData:=requireAllData,
                appendSafe:=appendSafe)
    End Sub

    Private Function TryCastToBigInteger(v As Object) As Numerics.BigInteger?
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
    Public Function ObjectEqual(v1 As Object, v2 As Object) As Boolean
        If v1.GetType Is v2.GetType AndAlso v1.GetType.IsGenericType Then
            If v1.GetType.GetGenericTypeDefinition Is GetType(KeyValuePair(Of ,)) Then
                Dim mKey = v1.GetType.GetProperty("Key")
                Dim mValue = v1.GetType.GetProperty("Value")
                If Not ObjectEqual(mKey.GetValue(v1, Nothing), mKey.GetValue(v2, Nothing)) Then Return False
                If Not ObjectEqual(mValue.GetValue(v1, Nothing), mValue.GetValue(v2, Nothing)) Then Return False
                Return True
            ElseIf v1.GetType.GetGenericTypeDefinition Is GetType(Maybe(Of )) Then
                Dim mHasValue = v1.GetType.GetProperty("HasValue")
                Dim mValue = v1.GetType.GetProperty("Value")
                If Not ObjectEqual(mHasValue.GetValue(v1, Nothing), mHasValue.GetValue(v2, Nothing)) Then Return False
                If True.Equals(mHasValue.GetValue(v1, Nothing)) Then
                    If Not ObjectEqual(mValue.GetValue(v1, Nothing), mValue.GetValue(v2, Nothing)) Then Return False
                End If
                Return True
            End If
        End If

        Dim n1 = TryCastToBigInteger(v1)
        Dim n2 = TryCastToBigInteger(v2)
        If TypeOf v1 Is NamedValueMap Then v1 = CType(v1, NamedValueMap).ToDictionary
        If TypeOf v2 Is NamedValueMap Then v2 = CType(v2, NamedValueMap).ToDictionary
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
    Private Function ListEqual(l1 As Collections.IEnumerable, l2 As Collections.IEnumerable) As Boolean
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
    Friend Function DictionaryEqual(Of TKey, TVal)(d1 As Dictionary(Of TKey, TVal),
                                                   d2 As Dictionary(Of TKey, TVal)) As Boolean
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