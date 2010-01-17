Imports Strilbrary.Values
Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker.Bnet.Protocol
Imports Tinker.Bnet
Imports Tinker.WC3

<TestClass()>
Public Class BnetProtocolTest
    Private Shared ReadOnly TestDate As New Date(Year:=2000, Month:=1, Day:=1)
    Private Shared ReadOnly TestDateData As Byte() = TestDate.ToFileTime.BitwiseToUInt64.Bytes()

    Private Shared Function TryCastToNumber(ByVal v As Object) As Numerics.BigInteger?
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
    Private Shared Function ObjectEqual(ByVal v1 As Object, ByVal v2 As Object) As Boolean
        Dim n1 = TryCastToNumber(v1)
        Dim n2 = TryCastToNumber(v2)
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
    Private Shared Function ListEqual(ByVal l1 As Collections.IEnumerable, ByVal l2 As Collections.IEnumerable) As Boolean
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
    Friend Shared Function DictionaryEqual(Of TKey, TVal)(ByVal d1 As Dictionary(Of TKey, TVal),
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

    <TestMethod()>
    Public Sub ClientChatCommandTest()
        Assert.IsTrue(ClientPackets.ChatCommand.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"text", "test"}
                }).Data.SequenceEqual(
                    {116, 101, 115, 116, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientCloseGame3Test()
        Assert.IsTrue(ClientPackets.CloseGame3.Pack(New Dictionary(Of InvariantString, Object)() From {
                }).Data.SequenceEqual(
                    {}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientCreateGame3Test()
        Assert.IsTrue(ClientPackets.CreateGame3.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"game state", GameStates.Full},
                    {"seconds since creation", 25},
                    {"game type", Tinker.WC3.GameTypes.PrivateGame},
                    {"unknown1=1023", 1023},
                    {"is ladder", 0},
                    {"name", "test"},
                    {"password", "a"},
                    {"num free slots", 3},
                    {"game id", 33},
                    {"statstring", TestStats}
                }).Data.SequenceEqual(New Byte() _
                    {2, 0, 0, 0,
                     25, 0, 0, 0,
                     0, 1 << (11 - 8), 0, 0,
                     &HFF, &H3, 0, 0,
                     0, 0, 0, 0,
                     116, 101, 115, 116, 0,
                     97, 0,
                     &H33,
                     &H31, &H32, &H30, &H30, &H30, &H30, &H30, &H30
                    }.Concat(New GameStatsJar("test").Pack(TestStats).Data)
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientEnterChatTest()
        Assert.IsTrue(ClientPackets.EnterChat.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"username", "a"},
                    {"statstring", "b"}
                }).Data.SequenceEqual(
                    {97, 0,
                     98, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientGetFileTimeTest()
        Assert.IsTrue(ClientPackets.GetFileTime.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"request id", 4},
                    {"unknown", 0},
                    {"filename", "test"}
                }).Data.SequenceEqual(
                    {4, 0, 0, 0,
                     0, 0, 0, 0,
                     116, 101, 115, 116, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientGetIconDataTest()
        Assert.IsTrue(ClientPackets.GetIconData.Pack(New Dictionary(Of InvariantString, Object)() From {
                }).Data.SequenceEqual(
                    {}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientJoinChannelTest()
        Assert.IsTrue(ClientPackets.JoinChannel.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"join type", JoinChannelType.ForcedJoin},
                    {"channel", "test"}
                }).Data.SequenceEqual(
                    {2, 0, 0, 0,
                     116, 101, 115, 116, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientNetGamePortTest()
        Assert.IsTrue(ClientPackets.NetGamePort.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"port", 6112}
                }).Data.SequenceEqual(
                    {&HE0, &H17}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientNullTest()
        Assert.IsTrue(ClientPackets.Null.Pack(New Dictionary(Of InvariantString, Object)() From {
                }).Data.SequenceEqual(
                    {}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientPingTest()
        Assert.IsTrue(ClientPackets.Ping.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"salt", 256}
                }).Data.SequenceEqual(
                    {0, 1, 0, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientProgramAuthenticationBeginTest()
        Assert.IsTrue(ClientPackets.ProgramAuthenticationBegin.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"protocol", 1},
                    {"platform", "ix86"},
                    {"product", "war3"},
                    {"product major version", 20},
                    {"product language", "usen"},
                    {"internal ip", Net.IPAddress.Loopback},
                    {"time zone offset", 10},
                    {"location id", 52},
                    {"language id", MPQ.LanguageId.English},
                    {"country abrev", "usa"},
                    {"country name", "United States"}
                }).Data.SequenceEqual(
                    {1, 0, 0, 0,
                     54, 56, 120, 105,
                     51, 114, 97, 119,
                     20, 0, 0, 0,
                     117, 115, 101, 110,
                     127, 0, 0, 1,
                     10, 0, 0, 0,
                     52, 0, 0, 0,
                     9, 4, 0, 0,
                     117, 115, 97, 0,
                     85, 110, 105, 116, 101, 100, 32, 83, 116, 97, 116, 101, 115, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientProgramAuthenticationFinishTest()
        Dim rocCred = "EDKBRTRXG88Z9V8M84HY2XVW7N".ToWC3CDKeyCredentials(0UI.Bytes, 0UI.Bytes)
        Dim tftCred = "M68YC4278JJXXVJMKRP8ETN4TC".ToWC3CDKeyCredentials(0UI.Bytes, 0UI.Bytes)
        Dim credJar = New ProductCredentialsJar("test")
        Assert.IsTrue(ClientPackets.ProgramAuthenticationFinish.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"client cd key salt", 76},
                    {"exe version", New Byte() {1, 2, 3, 4}.AsReadableList},
                    {"revision check response", 42},
                    {"# cd keys", 2},
                    {"is spawn", 0},
                    {"ROC cd key", rocCred},
                    {"TFT cd key", tftCred},
                    {"exe info", "info"},
                    {"owner", "Bot"}
                }).Data.SequenceEqual(New Byte() _
                    {76, 0, 0, 0,
                     1, 2, 3, 4,
                     42, 0, 0, 0,
                     2, 0, 0, 0,
                     0, 0, 0, 0
                    }.Concat(credJar.Pack(rocCred).Data
                    ).Concat(credJar.Pack(tftCred).Data).Concat(New Byte() _
                    {105, 110, 102, 111, 0,
                     66, 111, 116, 0})
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientQueryGamesListTest()
        Assert.IsTrue(ClientPackets.QueryGamesList.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"filter", GameTypes.PrivateGame},
                    {"filter mask", GameTypes.SizeLarge},
                    {"unknown0", 0},
                    {"list count", 20},
                    {"game name", "test"},
                    {"game password", "pass"},
                    {"game stats", "stats"}
                }).Data.SequenceEqual(
                    {0, 8, 0, 0,
                     0, 0, 8, 0,
                     0, 0, 0, 0,
                     20, 0, 0, 0,
                     116, 101, 115, 116, 0,
                     112, 97, 115, 115, 0,
                     115, 116, 97, 116, 115, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientUserAuthenticationBeginTest()
        Dim key = (From i In Enumerable.Range(0, 32)
                   Select CByte(i)).ToArray
        Assert.IsTrue(ClientPackets.UserAuthenticationBegin.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"client public key", key.AsReadableList},
                    {"username", "test"}
                }).Data.SequenceEqual(
                    key.Concat(
                    {116, 101, 115, 116, 0})
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientUserAuthenticationFinishTest()
        Dim proof = (From i In Enumerable.Range(0, 20)
                     Select CByte(i)).ToArray.AsReadableList
        Assert.IsTrue(ClientPackets.UserAuthenticationFinish.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"client password proof", proof}
                }).Data.SequenceEqual(
                    proof
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientWardenTest()
        Dim data = (From i In Enumerable.Range(0, 50)
                    Select CByte(i)).ToArray.AsReadableList
        Assert.IsTrue(ClientPackets.Warden.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"encrypted data", data}
                }).Data.SequenceEqual(
                    data
                ))
    End Sub

    <TestMethod()>
    Public Sub ServerChatEventTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"event id", ChatEventId.Talk},
                    {"flags", 0UI},
                    {"ping", 25UI},
                    {"ip", Net.IPAddress.Loopback},
                    {"acc#", &HDEADBEEFUI},
                    {"authority", &HBEEFDEADUI},
                    {"username", "test"},
                    {"text", "est"}
                }, ServerPackets.ChatEvent.Parse(New Byte() _
                    {5, 0, 0, 0,
                     0, 0, 0, 0,
                     25, 0, 0, 0,
                     127, 0, 0, 1,
                     &HEF, &HBE, &HAD, &HDE,
                     &HAD, &HDE, &HEF, &HBE,
                     116, 101, 115, 116, 0,
                     101, 115, 116, 0}.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerClanInfoTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"unknown", CByte(0)},
                    {"clan tag", "clan"},
                    {"rank", ClanRank.Leader}
                }, ServerPackets.ClanInfo.Parse(New Byte() _
                    {0,
                     110, 97, 108, 99,
                     4}.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerCreateGame3Test()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"result", 0UI}
                }, ServerPackets.CreateGame3.Parse(New Byte() _
                    {0, 0, 0, 0}.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerEnterChatTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"chat username", "test"},
                    {"statstring", ""},
                    {"account username", "est"}
                }, ServerPackets.EnterChat.Parse(New Byte() _
                    {116, 101, 115, 116, 0,
                     0,
                     101, 115, 116, 0}.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerFriendsUpdateTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"entry number", CByte(0)},
                    {"location id", CByte(1)},
                    {"status", CByte(2)},
                    {"product id", "war3"},
                    {"location", "test"}
                }, ServerPackets.FriendsUpdate.Parse(New Byte() _
                    {0,
                     1,
                     2,
                     51, 114, 97, 119,
                     116, 101, 115, 116, 0}.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerGetFileTimeTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"request id", 1UI},
                    {"unknown", 0UI},
                    {"filetime", TestDate},
                    {"filename", "test"}
                }, ServerPackets.GetFileTime.Parse(New Byte() _
                    {1, 0, 0, 0,
                     0, 0, 0, 0}.Concat(
                     TestDateData).Concat({
                     116, 101, 115, 116, 0}).ToArray.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerGetIconDataTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"filetime", TestDate},
                    {"filename", "test"}
                }, ServerPackets.GetIconData.Parse(
                    TestDateData.Concat({
                     116, 101, 115, 116, 0}).ToArray.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerMessageBoxTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"style", 1UI},
                    {"text", "test"},
                    {"caption", "est"}
                }, ServerPackets.MessageBox.Parse(New Byte() _
                    {1, 0, 0, 0,
                     116, 101, 115, 116, 0,
                     101, 115, 116, 0}.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerNullTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                }, ServerPackets.Null.Parse(New Byte() _
                    {}.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerPingTest()
        Assert.IsTrue(ServerPackets.Ping.Parse(New Byte() {3, 1, 0, 0}.AsReadableList).Value = 259UI)
    End Sub
    <TestMethod()>
    Public Sub ServerProgramAuthenticationBeginTest()
        Dim sig = (From e In Enumerable.Range(0, 128)
                   Select CByte(e)
                  ).ToArray.AsReadableList
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"logon type", ProgramAuthenticationBeginLogOnType.Warcraft3},
                    {"server cd key salt", 42UI},
                    {"udp value", 1UI},
                    {"mpq filetime", TestDate},
                    {"revision check seed", "test"},
                    {"revision check challenge", "est"},
                    {"server signature", sig}
                }, ServerPackets.ProgramAuthenticationBegin.Parse(New Byte() _
                    {2, 0, 0, 0,
                     42, 0, 0, 0,
                     1, 0, 0, 0}.Concat(
                     TestDateData).Concat({
                     116, 101, 115, 116, 0,
                     101, 115, 116, 0}).Concat(
                     sig).ToArray.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerProgramAuthenticationFinishTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"result", ProgramAuthenticationFinishResult.Passed},
                    {"info", "test"}
                }, ServerPackets.ProgramAuthenticationFinish.Parse(New Byte() _
                    {0, 0, 0, 0,
                     116, 101, 115, 116, 0}.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerQueryGamesListTest()
        'multiple games
        Dim testGameData = New Byte() _
                    {8, 0, 0, 0,
                     255, 255, 255, 255,
                     2, 0, &H17, &HE0, 127, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
                     1, 0, 0, 0,
                     5, 0, 0, 0,
                     116, 101, 115, 116, 0,
                     0,
                     67,
                     65, &H32, &H30, &H30, &H30, &H30, &H30, &H30
                     }.Concat(New GameStatsJar("test").Pack(TestStats).Data
                ).ToArray.AsReadableList
        Dim value = ServerPackets.QueryGamesList.Parse(New Byte() _
                    {2, 0, 0, 0}.
                    Concat(testGameData).
                    Concat(testGameData).
                ToArray.AsReadableList).Value
        Assert.IsTrue(value.Result = QueryGameResponse.Ok)
        Assert.IsTrue(value.Games.Count = 2)
        For Each game In value.Games
            Assert.IsTrue(game.Equals(TestDesc))
        Next game

        'single game
        value = ServerPackets.QueryGamesList.Parse(New Byte() _
                    {0, 0, 0, 0,
                     0, 0, 0, 0}.AsReadableList).Value
        Assert.IsTrue(value.Result = QueryGameResponse.Ok)
        Assert.IsTrue(value.Games.Count = 0)
    End Sub
    <TestMethod()>
    Public Sub ServerRequiredWorkTest()
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"filename", "test"}
                }, ServerPackets.RequiredWork.Parse(New Byte() _
                    {116, 101, 115, 116, 0}.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerUserAuthenticationBeginTest()
        Dim key = (From i In Enumerable.Range(0, 32)
                   Select CByte(i)).ToArray.AsReadableList
        Dim salt = key.Reverse.ToArray.AsReadableList
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"result", UserAuthenticationBeginResult.Passed},
                    {"account password salt", salt},
                    {"server public key", key}
                }, ServerPackets.UserAuthenticationBegin.Parse(New Byte() _
                    {0, 0, 0, 0}.Concat(salt).Concat(key).ToArray.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerUserAuthenticationFinishTest()
        Dim proof = (From i In Enumerable.Range(0, 20)
                     Select CByte(i)).ToArray.AsReadableList
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"result", UserAuthenticationFinishResult.Passed},
                    {"server password proof", proof},
                    {"custom error info", "test"}
                }, ServerPackets.UserAuthenticationFinish.Parse(New Byte() _
                    {0, 0, 0, 0}.Concat(
                    proof).Concat(
                    {116, 101, 115, 116, 0}).ToArray.AsReadableList
                ).Value))
    End Sub
    <TestMethod()>
    Public Sub ServerWardenTest()
        Dim data = (From i In Enumerable.Range(0, 50)
                    Select CByte(i)).ToArray.AsReadableList
        Assert.IsTrue(DictionaryEqual(New Dictionary(Of InvariantString, Object)() From {
                    {"encrypted data", data}
                }, ServerPackets.Warden.Parse(
                    data
                ).Value))
    End Sub
End Class
