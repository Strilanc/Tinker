Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Tinker.Bnet
Imports System.Numerics
Imports Strilbrary.Values
Imports System.Collections.Generic

<TestClass()>
Public Class BnetClientTest
    Private Class TestExternalProvider
        Implements Tinker.IExternalValues

        Public Function GenerateRevisionCheck(ByVal folder As String, ByVal seedString As String, ByVal challengeString As String) As UInteger Implements IExternalValues.GenerateRevisionCheck
            Return 0
        End Function

        Public ReadOnly Property WC3ExeVersion As IReadableList(Of Byte) Implements IExternalValues.WC3ExeVersion
            Get
                Return New Byte() {1, 2, 3, 4}.AsReadableList
            End Get
        End Property

        Public ReadOnly Property WC3FileSize As UInteger Implements IExternalValues.WC3FileSize
            Get
                Return 10
            End Get
        End Property

        Public ReadOnly Property WC3LastModifiedTime As Date Implements IExternalValues.WC3LastModifiedTime
            Get
                Return Now()
            End Get
        End Property
    End Class

    <TestMethod()>
    Public Sub ConnectAndLoginTest()
        Dim profile = New Bot.ClientProfile("default")
        profile.userName = "Tinker"
        profile.password = "rekniT"
        Dim keyRoc = "EDKBRTRXG88Z9V8M84HY2XVW7N"
        Dim keyTft = "M68YC4278JJXXVJMKRP8ETN4TC"
        Dim clock = New Strilbrary.Time.ManualClock()
        Dim client = New Bnet.Client(profile,
                                     New TestExternalProvider(),
                                     New CDKeyProductAuthenticator(keyRoc, keyTft),
                                     clock)
        Dim stream = New TestStream()
        Dim socket = New PacketSocket(stream,
                                      New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
                                      New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
                                      clock)
        Dim clientCdKeySalt = 13UI
        Dim fConnect = client.QueueConnect(socket, clientCdKeySalt)
        Assert.IsTrue(stream.RetrieveWriteData(1).SequenceEqual({1}))

        'program auth begin (C->S)
        Dim packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Protocol.PacketId.ProgramAuthenticationBegin)
        Dim body = Protocol.Packets.ClientToServer.ProgramAuthenticationBegin.Jar.Parse(packet.Skip(4).ToReadableList)
        Assert.IsTrue(CUInt(body.Value("protocol")) = 0)

        'ping
        stream.EnqueueRead({&HFF, &H25, &H8, &H0, &H6A, &H55, &H70, &H1C})
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Protocol.PacketId.Ping)
        Assert.IsTrue(Protocol.Packets.ClientToServer.Ping.Jar.Parse(packet.Skip(4).ToReadableList).Value = New Byte() {&H6A, &H55, &H70, &H1C}.ToUInt32)

        'program auth begin (S->C)
        Dim serverCdKeySalt = 19UI
        stream.EnqueuedReadPacket(
            preheader:={&HFF, Protocol.PacketId.ProgramAuthenticationBegin},
            sizeByteCount:=2,
            body:=Protocol.Packets.ServerToClient.ProgramAuthenticationBegin.Jar.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"logon type", Protocol.ProgramAuthenticationBeginLogOnType.Warcraft3},
                    {"server cd key salt", serverCdKeySalt},
                    {"udp value", 0},
                    {"mpq filetime", Now()},
                    {"revision check seed", "[not tested]"},
                    {"revision check challenge", "[not tested]"},
                    {"server signature", CByte(128).Range.ToReadableList}
                }).Data)

        'program auth finish (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Protocol.PacketId.ProgramAuthenticationFinish)
        body = Protocol.Packets.ClientToServer.ProgramAuthenticationFinish.Jar.Parse(packet.Skip(4).ToReadableList)
        Assert.IsTrue(CUInt(body.Value("client cd key salt")) = 13)
        Assert.IsTrue(CUInt(body.Value("# cd keys")) = 2)
        Assert.IsTrue(CUInt(body.Value("is spawn")) = 0)
        Assert.IsTrue(CType(body.Value("ROC cd key"), Bnet.ProductCredentials).Product = Bnet.ProductType.Warcraft3ROC)
        Assert.IsTrue(CType(body.Value("ROC cd key"), Bnet.ProductCredentials).Length = 26)
        Assert.IsTrue(CType(body.Value("ROC cd key"), Bnet.ProductCredentials).PublicKey = 1208212)
        Assert.IsTrue(CType(body.Value("ROC cd key"), Bnet.ProductCredentials).AuthenticationProof.SequenceEqual(keyRoc.ToWC3CDKeyCredentials(clientCdKeySalt.Bytes, serverCdKeySalt.Bytes).AuthenticationProof))
        Assert.IsTrue(CType(body.Value("TFT cd key"), Bnet.ProductCredentials).Product = Bnet.ProductType.Warcraft3TFT)
        Assert.IsTrue(CType(body.Value("TFT cd key"), Bnet.ProductCredentials).Length = 26)
        Assert.IsTrue(CType(body.Value("TFT cd key"), Bnet.ProductCredentials).PublicKey = 2818526)
        Assert.IsTrue(CType(body.Value("TFT cd key"), Bnet.ProductCredentials).AuthenticationProof.SequenceEqual(keyTft.ToWC3CDKeyCredentials(clientCdKeySalt.Bytes, serverCdKeySalt.Bytes).AuthenticationProof))

        'program auth finish (S->C)
        stream.EnqueuedReadPacket(
            preheader:={&HFF, Protocol.PacketId.ProgramAuthenticationFinish},
            sizeByteCount:=2,
            body:=Protocol.Packets.ServerToClient.ProgramAuthenticationFinish.Jar.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"result", Protocol.ProgramAuthenticationFinishResult.Passed},
                    {"info", ""}
                }).Data)
        WaitUntilTaskSucceeds(fConnect)
        Dim credentials = New Bnet.ClientCredentials(profile.userName, profile.password, privateKey:=2)
        client.QueueLogOn(credentials)

        'user auth begin (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Protocol.PacketId.UserAuthenticationBegin)
        body = Protocol.Packets.ClientToServer.UserAuthenticationBegin.Jar.Parse(packet.Skip(4).ToReadableList)
        Assert.IsTrue(CType(body.Value("client public key"), IReadableList(Of Byte)).SequenceEqual(credentials.PublicKeyBytes))
        Assert.IsTrue(CStr(body.Value("username")) = profile.userName)

        'user auth begin (S->C)
        Dim accountSalt = CByte(1).Repeated(32).ToReadableList
        Dim serverPublicKey = CByte(2).Repeated(32).ToReadableList
        stream.EnqueuedReadPacket(
            preheader:={&HFF, Protocol.PacketId.UserAuthenticationBegin},
            sizeByteCount:=2,
            body:=Protocol.Packets.ServerToClient.UserAuthenticationBegin.Jar.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"result", Protocol.UserAuthenticationBeginResult.Passed},
                    {"account password salt", accountSalt},
                    {"server public key", serverPublicKey}
                }).Data)

        'user auth finish (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Protocol.PacketId.UserAuthenticationFinish)
        Assert.IsTrue(credentials.ClientPasswordProof(accountSalt, serverPublicKey).SequenceEqual(Protocol.Packets.ClientToServer.UserAuthenticationFinish.Jar.Parse(packet.Skip(4).ToReadableList).Value))

        'user auth finish (S->C)
        stream.EnqueuedReadPacket(
            preheader:={&HFF, Protocol.PacketId.UserAuthenticationFinish},
            sizeByteCount:=2,
            body:=Protocol.Packets.ServerToClient.UserAuthenticationFinish.Jar.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"result", Protocol.UserAuthenticationFinishResult.Passed},
                    {"server password proof", credentials.ServerPasswordProof(accountSalt, serverPublicKey)},
                    {"custom error info", Tuple.Create(False, CStr(Nothing))}
                }).Data)

        'clan info (S->C)
        stream.EnqueueRead({&HFF, &H75, &HA, &H0, &H0, &H44, &H45, &H6F, &H47, &H2})

        'net game port (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Protocol.PacketId.NetGamePort)
        Protocol.Packets.ClientToServer.NetGamePort.Jar.Parse(packet.Skip(4).ToReadableList)

        'enter chat (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Protocol.PacketId.EnterChat)
        Protocol.Packets.ClientToServer.EnterChat.Jar.Parse(packet.Skip(4).ToReadableList)

        stream.EnqueueClosed()
        client.Dispose()
    End Sub
End Class
