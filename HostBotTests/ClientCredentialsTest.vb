Imports Strilbrary.Enumeration
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports HostBot.Bnet
Imports HostBot.Warcraft3
Imports Strilbrary
Imports System.Numerics
Imports Strilbrary.Numerics

<TestClass()>
Public Class ClientCredentialsTest
    Private Shared ReadOnly creds As New ClientCredentials(
        username:="AuthTest",
        password:="HostBot123",
        privatekey:=BigInteger.Parse("37562757284532284543721581696906433407594704334921877140391950043405102871633"))

    <TestMethod()>
    Public Sub PublicKeyTest()
        Assert.IsTrue(creds.PublicKeyBytes.HasSameItemsAs("4D F4 F3 3F D4 62 9D 9A A2 2A C5 4D 34 11 F5 08 52 18 FC 8D B9 36 36 BC 00 A4 CE 69 FE 92 2C 55".FromHexStringToBytes))
    End Sub

    <TestMethod()>
    Public Sub PasswordProofTest()
        Dim accountSalt = "F5 2D 0C 22 A9 1D 99 60 D9 0E 19 EA 08 8A D2 05 21 45 F4 4E F8 94 2F 30 11 23 1B C3 96 E6 DC D6".FromHexStringToBytes
        Dim serverPublicKeyBytes = "3C 3E 88 7F 0B 68 FD 7B D2 73 C2 B1 71 2F EF 94 D8 87 8F 25 95 34 70 42 5C 86 54 76 29 C3 CE DE".FromHexStringToBytes

        Dim clientProof = creds.ClientPasswordProof(accountSalt, serverPublicKeyBytes)
        Dim serverProof = creds.ServerPasswordProof(accountSalt, serverPublicKeyBytes)

        Assert.IsTrue(clientProof.HasSameItemsAs("65 97 0A 54 70 92 9B 1B 03 B6 92 02 8C B1 EB D3 4F 56 B9 8D".FromHexStringToBytes))
        Assert.IsTrue(serverProof.HasSameItemsAs("36 23 E0 FB FB 49 E8 01 D9 F0 6D 1E 22 25 89 59 19 6E 55 90".FromHexStringToBytes))
    End Sub
End Class
