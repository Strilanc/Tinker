Imports Strilbrary.Numerics
Imports Strilbrary.Enumeration
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker.Bnet

'''<remarks>Tested cd keys are banned or fake.</remarks>
<TestClass()>
Public Class ProductCredentialsTest
    <TestMethod()>
    Public Sub ROCAuthenticationTest()
        Dim key = "EDKBRTRXG88Z9V8M84HY2XVW7N".ToWC3CDKeyCredentials(0UI.Bytes, 0UI.Bytes)
        Assert.IsTrue(key.Product = ProductType.Warcraft3ROC)
        Assert.IsTrue(key.PublicKey = 1208212)
        Assert.IsTrue(key.Length = 26)
        Assert.IsTrue(key.AuthenticationProof.HasSameItemsAs({196, 113, 19, 179, 113, 10, 105, 13, 102, 167, 215, 44, 208, 16, 1, 235, 117, 98, 60, 242}))
    End Sub
    <TestMethod()>
    Public Sub TFTAuthenticationTest()
        Dim key = "M68YC4278JJXXVJMKRP8ETN4TC".ToWC3CDKeyCredentials(0UI.Bytes, 0UI.Bytes)
        Assert.IsTrue(key.Product = ProductType.Warcraft3TFT)
        Assert.IsTrue(key.PublicKey = 2818526)
        Assert.IsTrue(key.Length = 26)
        Assert.IsTrue(key.AuthenticationProof.HasSameItemsAs({251, 78, 164, 23, 174, 8, 33, 230, 25, 59, 223, 85, 108, 168, 51, 6, 54, 69, 96, 24}))
    End Sub
    <TestMethod()>
    Public Sub AlternateRepresentationTest()
        Dim key = "M68Y-C427 8JJX-XVJM KRP8-ETN4 TC".ToWC3CDKeyCredentials(0UI.Bytes, 0UI.Bytes)
        Assert.IsTrue(key.Product = ProductType.Warcraft3TFT)
        Assert.IsTrue(key.PublicKey = 2818526)
        Assert.IsTrue(key.Length = 26)
    End Sub
End Class
