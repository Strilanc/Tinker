Imports Strilbrary.Enumeration
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports HostBot.Bnet
Imports HostBot.Warcraft3
Imports Strilbrary
Imports System.Numerics
Imports Strilbrary.Numerics

<TestClass()>
Public Class BnetAuthenticationTest
    <TestMethod()>
    Public Sub PasswordProofsTest()
        Dim result = GenerateClientServerPasswordProofs(userName:="AuthTest",
                                                        password:="HostBot123",
                                                        accountSalt:="F5 2D 0C 22 A9 1D 99 60 D9 0E 19 EA 08 8A D2 05 21 45 F4 4E F8 94 2F 30 11 23 1B C3 96 E6 DC D6".FromHexStringToBytes.ToView,
                                                        serverPublicKeyBytes:="4D F4 F3 3F D4 62 9D 9A A2 2A C5 4D 34 11 F5 08 52 18 FC 8D B9 36 36 BC 00 A4 CE 69 FE 92 2C 55".FromHexStringToBytes.ToView,
                                                        clientPrivateKey:=BigInteger.Parse("37562757284532284543721581696906433407594704334921877140391950043405102871633"),
                                                        clientPublicKeyBytes:="3C 3E 88 7F 0B 68 FD 7B D2 73 C2 B1 71 2F EF 94 D8 87 8F 25 95 34 70 42 5C 86 54 76 29 C3 CE DE".FromHexStringToBytes.ToView)
        Assert.IsTrue(result.Value1.HasSameItemsAs("D1 20 18 61 71 43 07 DD 8D 27 CA 22 A4 E8 E3 6B 0E 17 BF 71".FromHexStringToBytes))
        Assert.IsTrue(result.Value2.HasSameItemsAs("EA A7 BE 17 94 DF 26 32 C9 05 BD 48 15 60 4C 3C C3 8B 50 4B".FromHexStringToBytes))
    End Sub
End Class
