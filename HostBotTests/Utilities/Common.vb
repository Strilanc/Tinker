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
                Function(d1, d2) BnetProtocolTest.DictionaryEqual(d1, d2),
                value,
                data,
                requireAllData:=requireAllData,
                appendSafe:=appendSafe)
    End Sub
End Module