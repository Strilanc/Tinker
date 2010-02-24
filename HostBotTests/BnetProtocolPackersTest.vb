Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Strilbrary.Values
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker
Imports Tinker.Bnet
Imports Tinker.Bnet.Protocol

<TestClass()>
Public Class BnetProtocolPackersTest
    <TestMethod()>
    Public Sub MakeAccountLogOnBeginTest()
        MakeAccountLogOnBegin(New ClientCredentials("test", "password"))
    End Sub
    <TestMethod()>
    Public Sub MakeAccountLogOnFinishTest()
        MakeAccountLogOnFinish(Enumerable.Repeat(CByte(1), 20).ToReadableList)
    End Sub
    <TestMethod()>
    Public Sub MakeAuthenticationBeginTest()
        MakeAuthenticationBegin(majorVersion:=24, localIPAddress:=Net.IPAddress.Loopback)
    End Sub
    <TestMethod()>
    Public Sub MakeAuthenticationFinishTest()
        MakeAuthenticationFinish(version:=New Byte() {1, 2, 3, 4}.AsReadableList,
                                 revisionCheckResponse:=1,
                                 clientCDKeySalt:=2,
                                 cdKeyOwner:="test",
                                 exeInformation:="info",
                                 productAuthentication:=New ProductCredentialPair(
                                     "EDKBRTRXG88Z9V8M84HY2XVW7N".ToWC3CDKeyCredentials(clientSalt:={1, 2, 3, 4}, serverSalt:={1, 2, 3, 4}),
                                     "M68YC4278JJXXVJMKRP8ETN4TC".ToWC3CDKeyCredentials(clientSalt:={1, 2, 3, 4}, serverSalt:={1, 2, 3, 4})))
    End Sub
    <TestMethod()>
    Public Sub MakeChatCommandTest()
        MakeChatCommand("test")
    End Sub
    <TestMethod()>
    Public Sub MakeCloseGame3Test()
        MakeCloseGame3()
    End Sub
    <TestMethod()>
    Public Sub MakeCreateGame3Test()
        MakeCreateGame3(SharedTestObjects.TestDesc)
    End Sub
    <TestMethod()>
    Public Sub MakeEnterChatTest()
        MakeEnterChat()
    End Sub
    <TestMethod()>
    Public Sub MakeGetFileTimeTest()
        MakeGetFileTime("test", 1)
    End Sub
    <TestMethod()>
    Public Sub MakeJoinChannelTest()
        MakeJoinChannel(JoinChannelType.FirstJoin, "test")
    End Sub
    <TestMethod()>
    Public Sub MakeNetGamePortTest()
        MakeNetGamePort(6112)
    End Sub
    <TestMethod()>
    Public Sub MakePingTest()
        MakePing(52)
    End Sub
    <TestMethod()>
    Public Sub MakeQueryGamesListTest()
        MakeQueryGamesList()
    End Sub
    <TestMethod()>
    Public Sub MakeWardenTest()
        MakeWarden(New Byte() {0}.AsReadableList)
    End Sub
End Class
