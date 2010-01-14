Imports Strilbrary.Values
Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker.Bnet.Packet
Imports Tinker.Bnet
Imports Tinker.WC3

<TestClass()>
Public Class BnetPacketTest
    <TestMethod()>
    Public Sub ClientChatCommandText()
        Assert.IsTrue(ClientPackets.ChatCommand.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"text", "test"}
                }).Data.SequenceEqual(
                    {116, 101, 115, 116, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientCloseGame3()
        Assert.IsTrue(ClientPackets.CloseGame3.Pack(New Dictionary(Of InvariantString, Object)() From {
                }).Data.SequenceEqual(
                    {}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientCreateGame3()
        Dim testMap = New Map(
                folder:="Test:\Maps",
                relativePath:="test",
                fileChecksumCRC32:=1,
                filesize:=1,
                mapChecksumSHA1:=(From i In Enumerable.Range(0, 20) Select CByte(i)).ToArray.AsReadableList,
                mapChecksumXORO:=1,
                slotCount:=2)
        Dim testArgument = New Tinker.Commands.CommandArgument("")
        Dim testStats = New GameStats(
                Map:=testMap,
                hostName:="StrilancHost",
                argument:=testArgument)
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
                    {"statstring", testStats}
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
                    }.Concat(New GameStatsJar("test").Pack(testStats).Data)
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientEnterChat()
        Assert.IsTrue(ClientPackets.EnterChat.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"username", "a"},
                    {"statstring", "b"}
                }).Data.SequenceEqual(
                    {97, 0,
                     98, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientGetFileTime()
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
    Public Sub ClientGetIconData()
        Assert.IsTrue(ClientPackets.GetIconData.Pack(New Dictionary(Of InvariantString, Object)() From {
                }).Data.SequenceEqual(
                    {}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientJoinChannel()
        Assert.IsTrue(ClientPackets.JoinChannel.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"join type", JoinChannelType.ForcedJoin},
                    {"channel", "test"}
                }).Data.SequenceEqual(
                    {2, 0, 0, 0,
                     116, 101, 115, 116, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientNetGamePort()
        Assert.IsTrue(ClientPackets.NetGamePort.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"port", 6112}
                }).Data.SequenceEqual(
                    {&HE0, &H17}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientNull()
        Assert.IsTrue(ClientPackets.Null.Pack(New Dictionary(Of InvariantString, Object)() From {
                }).Data.SequenceEqual(
                    {}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientPing()
        Assert.IsTrue(ClientPackets.Ping.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"salt", 256}
                }).Data.SequenceEqual(
                    {0, 1, 0, 0}
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientProgramAuthenticationBegin()
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
    Public Sub ClientProgramAuthenticationFinish()
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
    Public Sub ClientQueryGamesList()
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
    Public Sub ClientUserAuthenticationBegin()
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
    Public Sub ClientUserAuthenticationFinish()
        Dim proof = (From i In Enumerable.Range(0, 20)
                     Select CByte(i)).ToArray
        Assert.IsTrue(ClientPackets.UserAuthenticationFinish.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"client password proof", proof.AsReadableList}
                }).Data.SequenceEqual(
                    proof
                ))
    End Sub
    <TestMethod()>
    Public Sub ClientWarden()
        Dim data = (From i In Enumerable.Range(0, 50)
                    Select CByte(i)).ToArray
        Assert.IsTrue(ClientPackets.Warden.Pack(New Dictionary(Of InvariantString, Object)() From {
                    {"encrypted data", data.AsReadableList}
                }).Data.SequenceEqual(
                    data
                ))
    End Sub
End Class
