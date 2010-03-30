Imports Strilbrary.Values
Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker.Bnet.Protocol
Imports Tinker.Bnet
Imports Tinker.WC3
Imports Tinker

<TestClass()>
Public Class BnetProtocolTest
    Private Shared ReadOnly TestDate As New Date(Year:=2000, Month:=1, Day:=1)
    Private Shared ReadOnly TestDateData As Byte() = TestDate.ToFileTime.BitwiseToUInt64.Bytes()

    <TestMethod()>
    Public Sub ClientChatCommandTest()
        JarTest(Packets.ClientToServer.ChatCommand.Jar,
                data:={116, 101, 115, 116, 0},
                value:="test")
    End Sub
    <TestMethod()>
    Public Sub ClientCloseGame3Test()
        EmptyJarTest(Packets.ClientToServer.CloseGame3.Jar)
    End Sub
    <TestMethod()>
    Public Sub ClientCreateGame3Test()
        JarTest(Packets.ClientToServer.CreateGame3.Jar,
                data:=New Byte() {2, 0, 0, 0,
                     25, 0, 0, 0,
                     0, 1 << (11 - 8), 0, 0,
                     &HFF, &H3, 0, 0,
                     0, 0, 0, 0,
                     116, 101, 115, 116, 0,
                     97, 0,
                     &H33,
                     &H31, &H32, &H30, &H30, &H30, &H30, &H30, &H30
                    }.Concat(New WC3.Protocol.GameStatsJar().Pack(TestStats)),
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"game state", GameStates.Full},
                        {"seconds since creation", 25UI},
                        {"game type", Tinker.WC3.Protocol.GameTypes.PrivateGame},
                        {"unknown1=1023", 1023UI},
                        {"is ladder", 0UI},
                        {"name", "test"},
                        {"password", "a"},
                        {"num free slots", 3UI},
                        {"game id", 33UI},
                        {"statstring", TestStats}
                    })
    End Sub
    <TestMethod()>
    Public Sub ClientEnterChatTest()
        JarTest(Packets.ClientToServer.EnterChat.Jar,
                data:={97, 0,
                       98, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"username", "a"},
                        {"statstring", "b"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ClientGetFileTimeTest()
        JarTest(Packets.ClientToServer.GetFileTime.Jar,
                data:={4, 0, 0, 0,
                       0, 0, 0, 0,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"request id", 4UI},
                        {"unknown", 0UI},
                        {"filename", "test"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ClientGetIconDataTest()
        EmptyJarTest(Packets.ClientToServer.GetIconData.Jar)
    End Sub
    <TestMethod()>
    Public Sub ClientJoinChannelTest()
        JarTest(Packets.ClientToServer.JoinChannel.Jar,
                data:={2, 0, 0, 0,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"join type", JoinChannelType.ForcedJoin},
                        {"channel", "test"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ClientNetGamePortTest()
        JarTest(Packets.ClientToServer.NetGamePort.Jar,
                data:={&HE0, &H17},
                value:=6112)
    End Sub
    <TestMethod()>
    Public Sub ClientNullTest()
        EmptyJarTest(Packets.ClientToServer.Null.Jar)
    End Sub
    <TestMethod()>
    Public Sub ClientPingTest()
        JarTest(Packets.ClientToServer.Ping.Jar,
                data:={0, 1, 0, 0},
                value:=256)
    End Sub
    <TestMethod()>
    Public Sub ClientProgramAuthenticationBeginTest()
        JarTest(Packets.ClientToServer.ProgramAuthenticationBegin.Jar,
                data:={1, 0, 0, 0,
                       54, 56, 120, 105,
                       51, 114, 97, 119,
                       20, 0, 0, 0,
                       117, 115, 101, 110,
                       127, 0, 0, 1,
                       10, 0, 0, 0,
                       52, 0, 0, 0,
                       9, 4, 0, 0,
                       117, 115, 97, 0,
                       85, 110, 105, 116, 101, 100, 32, 83, 116, 97, 116, 101, 115, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"protocol", 1UI},
                        {"platform", "ix86"},
                        {"product", "war3"},
                        {"product major version", 20UI},
                        {"product language", "usen"},
                        {"internal ip", Net.IPAddress.Loopback},
                        {"time zone offset", 10UI},
                        {"location id", 52UI},
                        {"language id", MPQ.LanguageId.English},
                        {"country abrev", "usa"},
                        {"country name", "United States"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ClientProgramAuthenticationFinishTest()
        Dim rocCred = "EDKBRTRXG88Z9V8M84HY2XVW7N".ToWC3CDKeyCredentials(0UI.Bytes, 0UI.Bytes)
        Dim tftCred = "M68YC4278JJXXVJMKRP8ETN4TC".ToWC3CDKeyCredentials(0UI.Bytes, 0UI.Bytes)
        Dim credJar = New ProductCredentialsJar()
        JarTest(Packets.ClientToServer.ProgramAuthenticationFinish.Jar,
                data:=New Byte() {
                       76, 0, 0, 0,
                       1, 2, 3, 4,
                       42, 0, 0, 0,
                       2, 0, 0, 0,
                       0, 0, 0, 0
                       }.Concat(credJar.Pack(rocCred)
                       ).Concat(credJar.Pack(tftCred)).Concat(New Byte() _
                       {105, 110, 102, 111, 0,
                        66, 111, 116, 0}),
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"client cd key salt", 76UI},
                        {"exe version", New Byte() {1, 2, 3, 4}.AsReadableList},
                        {"revision check response", 42UI},
                        {"# cd keys", 2UI},
                        {"is spawn", 0UI},
                        {"ROC cd key", rocCred},
                        {"TFT cd key", tftCred},
                        {"exe info", "info"},
                        {"owner", "Bot"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ClientQueryGamesListTest()
        JarTest(Packets.ClientToServer.QueryGamesList.Jar,
                data:={0, 8, 0, 0,
                       0, 0, 8, 0,
                       0, 0, 0, 0,
                       20, 0, 0, 0,
                       116, 101, 115, 116, 0,
                       112, 97, 115, 115, 0,
                       115, 116, 97, 116, 115, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filter", WC3.Protocol.GameTypes.PrivateGame},
                        {"filter mask", WC3.Protocol.GameTypes.SizeLarge},
                        {"unknown0", 0UI},
                        {"list count", 20UI},
                        {"game name", "test"},
                        {"game password", "pass"},
                        {"game stats", "stats"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ClientUserAuthenticationBeginTest()
        Dim key = CByte(32).Range.ToArray
        JarTest(Packets.ClientToServer.UserAuthenticationBegin.Jar,
                data:=key.Concat(
                      {116, 101, 115, 116, 0}),
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"client public key", key.AsReadableList},
                        {"username", "test"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ClientUserAuthenticationFinishTest()
        Dim proof = CByte(20).Range.ToReadableList
        JarTest(Packets.ClientToServer.UserAuthenticationFinish.Jar,
                equater:=Function(e1 As IReadableList(Of Byte), e2 As IReadableList(Of Byte)) e1.SequenceEqual(e2),
                data:=proof,
                value:=proof)
    End Sub
    <TestMethod()>
    Public Sub ClientWardenTest()
        Dim data = CByte(50).Range.ToReadableList
        JarTest(Packets.ClientToServer.Warden.Jar,
                equater:=Function(e1 As IReadableList(Of Byte), e2 As IReadableList(Of Byte)) e1.SequenceEqual(e2),
                data:=data,
                value:=data,
                appendSafe:=False,
                requireAllData:=False)
    End Sub

    <TestMethod()>
    Public Sub ServerChatEventTest()
        JarTest(Packets.ServerToClient.ChatEvent.Jar,
                data:={5, 0, 0, 0,
                       0, 0, 0, 0,
                       25, 0, 0, 0,
                       127, 0, 0, 1,
                       &HEF, &HBE, &HAD, &HDE,
                       &HAD, &HDE, &HEF, &HBE,
                       116, 101, 115, 116, 0,
                       101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"event id", ChatEventId.Talk},
                        {"flags", 0UI},
                        {"ping", 25UI},
                        {"ip", Net.IPAddress.Loopback},
                        {"acc#", &HDEADBEEFUI},
                        {"authority", &HBEEFDEADUI},
                        {"username", "test"},
                        {"text", "est"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerClanInfoTest()
        JarTest(Packets.ServerToClient.ClanInfo.Jar,
                data:={0,
                       110, 97, 108, 99,
                       4},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"unknown", CByte(0)},
                        {"clan tag", "clan"},
                        {"rank", ClanRank.Leader}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerCreateGame3Test()
        JarTest(Packets.ServerToClient.CreateGame3.Jar,
                data:={0, 0, 0, 0},
                value:=0UI)
    End Sub
    <TestMethod()>
    Public Sub ServerEnterChatTest()
        JarTest(Packets.ServerToClient.EnterChat.Jar,
                data:={116, 101, 115, 116, 0,
                       0,
                       101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"chat username", "test"},
                        {"statstring", ""},
                        {"account username", "est"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerFriendsUpdateTest()
        JarTest(Packets.ServerToClient.FriendsUpdate.Jar,
                data:={0,
                       1,
                       2,
                       51, 114, 97, 119,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"entry number", CByte(0)},
                        {"location id", CByte(1)},
                        {"status", CByte(2)},
                        {"product id", "war3"},
                        {"location", "test"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerGetFileTimeTest()
        JarTest(Packets.ServerToClient.GetFileTime.Jar,
                data:=New Byte() {1, 0, 0, 0,
                       0, 0, 0, 0}.Concat(
                       TestDateData).Concat({
                       116, 101, 115, 116, 0}),
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"request id", 1UI},
                        {"unknown", 0UI},
                        {"filetime", TestDate},
                        {"filename", "test"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerGetIconDataTest()
        JarTest(Packets.ServerToClient.GetIconData.Jar,
                data:=TestDateData.Concat({
                      116, 101, 115, 116, 0}),
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"filetime", TestDate},
                        {"filename", "test"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerMessageBoxTest()
        JarTest(Packets.ServerToClient.MessageBox.Jar,
                data:={1, 0, 0, 0,
                       116, 101, 115, 116, 0,
                       101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"style", 1UI},
                        {"text", "test"},
                        {"caption", "est"}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerNullTest()
        EmptyJarTest(Packets.ServerToClient.Null.Jar)
    End Sub
    <TestMethod()>
    Public Sub ServerPingTest()
        JarTest(Packets.ServerToClient.Ping.Jar,
                data:={3, 1, 0, 0},
                value:=259UI)
    End Sub
    <TestMethod()>
    Public Sub ServerProgramAuthenticationBeginTest()
        Dim sig = CByte(128).Range.ToReadableList
        JarTest(Packets.ServerToClient.ProgramAuthenticationBegin.Jar,
                data:=New Byte() {2, 0, 0, 0,
                       42, 0, 0, 0,
                       1, 0, 0, 0}.Concat(
                       TestDateData).Concat({
                       116, 101, 115, 116, 0,
                       101, 115, 116, 0}).Concat(
                       sig),
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"logon type", ProgramAuthenticationBeginLogOnType.Warcraft3},
                        {"server cd key salt", 42UI},
                        {"udp value", 1UI},
                        {"mpq filetime", TestDate},
                        {"revision check seed", "test"},
                        {"revision check challenge", "est"},
                        {"server signature", sig}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerProgramAuthenticationFinishTest()
        JarTest(Packets.ServerToClient.ProgramAuthenticationFinish.Jar,
                data:={0, 0, 0, 0,
                       116, 101, 115, 116, 0},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"result", ProgramAuthenticationFinishResult.Passed},
                        {"info", "test"}
                    })
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
                     }.Concat(New WC3.Protocol.GameStatsJar().Pack(TestStats)
                ).ToReadableList
        Dim value = Packets.ServerToClient.QueryGamesList.Jar.Parse(New Byte() _
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
        value = Packets.ServerToClient.QueryGamesList.Jar.Parse(New Byte() _
                    {0, 0, 0, 0,
                     0, 0, 0, 0}.AsReadableList).Value
        Assert.IsTrue(value.Result = QueryGameResponse.Ok)
        Assert.IsTrue(value.Games.Count = 0)
    End Sub
    <TestMethod()>
    Public Sub ServerRequiredWorkTest()
        JarTest(Packets.ServerToClient.RequiredWork.Jar,
                data:={116, 101, 115, 116, 0},
                value:="test")
    End Sub
    <TestMethod()>
    Public Sub ServerUserAuthenticationBeginTest()
        Dim key = CByte(32).Range.ToReadableList
        Dim salt = key.Reverse.ToReadableList
        JarTest(Packets.ServerToClient.UserAuthenticationBegin.Jar,
                data:=New Byte() {0, 0, 0, 0}.Concat(salt).Concat(key),
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"result", UserAuthenticationBeginResult.Passed},
                        {"account password salt", salt},
                        {"server public key", key}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerUserAuthenticationFinishTest()
        Dim proof = CByte(20).Range.ToReadableList
        JarTest(Packets.ServerToClient.UserAuthenticationFinish.Jar,
                data:=New Byte() {0, 0, 0, 0}.Concat(
                      proof).Concat(
                      {116, 101, 115, 116, 0}),
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"result", UserAuthenticationFinishResult.Passed},
                        {"server password proof", proof},
                        {"custom error info", Tuple.Create(True, "test")}
                    })
    End Sub
    <TestMethod()>
    Public Sub ServerWardenTest()
        Dim data = CByte(50).Range.ToReadableList
        JarTest(Packets.ServerToClient.Warden.Jar,
                equater:=Function(e1 As IReadableList(Of Byte), e2 As IReadableList(Of Byte)) e1.SequenceEqual(e2),
                data:=data,
                value:=data,
                appendSafe:=False,
                requireAllData:=False)
    End Sub
End Class
