Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Tinker.Bnet
Imports System.Numerics
Imports Strilbrary.Values
Imports Strilbrary.Enumeration
Imports System.Collections.Generic

<TestClass()>
Public Class BnetClientTest
    Private Class TestExternalProvider
        Implements Tinker.IExternalValues

        Public Function GenerateRevisionCheck(ByVal folder As String, ByVal seedString As String, ByVal challengeString As String) As UInteger Implements Tinker.IExternalValues.GenerateRevisionCheck
            Return 0
        End Function

        Public ReadOnly Property WC3ExeVersion As Strilbrary.Collections.IReadableList(Of Byte) Implements Tinker.IExternalValues.WC3ExeVersion
            Get
                Return New Byte() {1, 2, 3, 4}.AsReadableList
            End Get
        End Property

        Public ReadOnly Property WC3FileSize As UInteger Implements Tinker.IExternalValues.WC3FileSize
            Get
                Return 10
            End Get
        End Property

        Public ReadOnly Property WC3LastModifiedTime As Date Implements Tinker.IExternalValues.WC3LastModifiedTime
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
        profile.cdKeyROC = "EDKBRTRXG88Z9V8M84HY2XVW7N"
        profile.cdKeyTFT = "M68YC4278JJXXVJMKRP8ETN4TC"
        Dim client = New Bnet.Client(profile, New TestExternalProvider())
        Dim stream = New TestStream()
        Dim socket = New PacketSocket(stream,
                                      New Net.IPEndPoint(Net.IPAddress.Loopback, 6112),
                                      New Net.IPEndPoint(Net.IPAddress.Loopback, 6112))
        Dim clientCdKeySalt = 13UI
        Dim fConnect = client.QueueConnect(socket, clientCdKeySalt)
        Assert.IsTrue(stream.RetrieveWriteData(1).SequenceEqual({1}))

        'program auth begin (C->S)
        Dim packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Bnet.PacketId.ProgramAuthenticationBegin)
        Dim body = Bnet.Packet.ClientPackets.ProgramAuthenticationBegin.Parse(packet.SubToArray(4).AsReadableList)
        Assert.IsTrue(CUInt(body.Value("protocol")) = 0)

        'ping
        stream.EnqueueRead({&HFF, &H25, &H8, &H0, &H6A, &H55, &H70, &H1C})
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Bnet.PacketId.Ping)
        body = Bnet.Packet.ClientPackets.Ping.Parse(packet.SubToArray(4).AsReadableList)
        Assert.IsTrue(CUInt(body.Value("salt")) = New Byte() {&H6A, &H55, &H70, &H1C}.ToUInt32)

        'program auth begin (S->C)
        Dim serverCdKeySalt = 19UI
        stream.EnqueuedReadPacket(
            preheader:={&HFF, Bnet.PacketId.ProgramAuthenticationBegin},
            sizeByteCount:=2,
            body:=Bnet.Packet.ServerPackets.ProgramAuthenticationBegin.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"logon type", Bnet.Packet.ProgramAuthenticationBeginLogOnType.Warcraft3},
                    {"server cd key salt", serverCdKeySalt},
                    {"udp value", 0},
                    {"mpq filetime", Now()},
                    {"revision check seed", "[not tested]"},
                    {"revision check challenge", "[not tested]"},
                    {"server signature", (From i In Enumerable.Range(0, 128) Select CByte(i)).ToArray.AsReadableList}
                }).Data)

        'program auth finish (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Bnet.PacketId.ProgramAuthenticationFinish)
        body = Bnet.Packet.ClientPackets.ProgramAuthenticationFinish.Parse(packet.SubToArray(4).AsReadableList)
        Assert.IsTrue(CUInt(body.Value("client cd key salt")) = 13)
        Assert.IsTrue(CUInt(body.Value("# cd keys")) = 2)
        Assert.IsTrue(CUInt(body.Value("is spawn")) = 0)
        Assert.IsTrue(CType(body.Value("ROC cd key"), Bnet.ProductCredentials).Product = Bnet.ProductType.Warcraft3ROC)
        Assert.IsTrue(CType(body.Value("ROC cd key"), Bnet.ProductCredentials).Length = 26)
        Assert.IsTrue(CType(body.Value("ROC cd key"), Bnet.ProductCredentials).PublicKey = 1208212)
        Assert.IsTrue(CType(body.Value("ROC cd key"), Bnet.ProductCredentials).AuthenticationProof.SequenceEqual(profile.cdKeyROC.ToWC3CDKeyCredentials(clientCdKeySalt.Bytes, serverCdKeySalt.Bytes).AuthenticationProof))
        Assert.IsTrue(CType(body.Value("TFT cd key"), Bnet.ProductCredentials).Product = Bnet.ProductType.Warcraft3TFT)
        Assert.IsTrue(CType(body.Value("TFT cd key"), Bnet.ProductCredentials).Length = 26)
        Assert.IsTrue(CType(body.Value("TFT cd key"), Bnet.ProductCredentials).PublicKey = 2818526)
        Assert.IsTrue(CType(body.Value("TFT cd key"), Bnet.ProductCredentials).AuthenticationProof.SequenceEqual(profile.cdKeyTFT.ToWC3CDKeyCredentials(clientCdKeySalt.Bytes, serverCdKeySalt.Bytes).AuthenticationProof))

        'program auth finish (S->C)
        stream.EnqueuedReadPacket(
            preheader:={&HFF, Bnet.PacketId.ProgramAuthenticationFinish},
            sizeByteCount:=2,
            body:=Bnet.Packet.ServerPackets.ProgramAuthenticationFinish.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"result", Bnet.Packet.ProgramAuthenticationFinishResult.Passed},
                    {"info", ""}
                }).Data)
        Assert.IsTrue(BlockOnFuture(fConnect))
        Dim credentials = New Bnet.ClientCredentials(profile.userName, profile.password, privateKey:=2)
        client.QueueLogOn(credentials)

        'user auth begin (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Bnet.PacketId.UserAuthenticationBegin)
        body = Bnet.Packet.ClientPackets.UserAuthenticationBegin.Parse(packet.SubToArray(4).AsReadableList)
        Assert.IsTrue(CType(body.Value("client public key"), IReadableList(Of Byte)).SequenceEqual(credentials.PublicKeyBytes))
        Assert.IsTrue(CStr(body.Value("username")) = profile.userName)

        'user auth begin (S->C)
        Dim accountSalt = Linq.Enumerable.Repeat(CByte(1), 32).ToArray.AsReadableList
        Dim serverPublicKey = Linq.Enumerable.Repeat(CByte(2), 32).ToArray.AsReadableList
        stream.EnqueuedReadPacket(
            preheader:={&HFF, Bnet.PacketId.UserAuthenticationBegin},
            sizeByteCount:=2,
            body:=Bnet.Packet.ServerPackets.UserAuthenticationBegin.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"result", Bnet.Packet.UserAuthenticationBeginResult.Passed},
                    {"account password salt", accountSalt},
                    {"server public key", serverPublicKey}
                }).Data)

        'user auth finish (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Bnet.PacketId.UserAuthenticationFinish)
        body = Bnet.Packet.ClientPackets.UserAuthenticationFinish.Parse(packet.SubToArray(4).AsReadableList)
        Assert.IsTrue(CType(body.Value("client password proof"), IReadableList(Of Byte)).SequenceEqual(credentials.ClientPasswordProof(accountSalt, serverPublicKey)))

        'user auth finish (S->C)
        stream.EnqueuedReadPacket(
            preheader:={&HFF, Bnet.PacketId.UserAuthenticationFinish},
            sizeByteCount:=2,
            body:=Bnet.Packet.ServerPackets.UserAuthenticationFinish.Pack(New Dictionary(Of InvariantString, Object) From {
                    {"result", Bnet.Packet.UserAuthenticationFinishResult.Passed},
                    {"server password proof", credentials.ServerPasswordProof(accountSalt, serverPublicKey)},
                    {"custom error info", ""}
                }).Data)

        'clan info (S->C)
        stream.EnqueueRead({&HFF, &H75, &HA, &H0, &H0, &H44, &H45, &H6F, &H47, &H2})

        'net game port (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Bnet.PacketId.NetGamePort)
        body = Bnet.Packet.ClientPackets.NetGamePort.Parse(packet.SubToArray(4).AsReadableList)

        'enter chat (C->S)
        packet = stream.RetrieveWritePacket()
        Assert.IsTrue(packet(1) = Bnet.PacketId.EnterChat)
        body = Bnet.Packet.ClientPackets.EnterChat.Parse(packet.SubToArray(4).AsReadableList)

        stream.EnqueueClosed()
        client.Dispose()
    End Sub
End Class
