Imports Strilbrary.Numerics
Imports Strilbrary.Enumeration
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker.Bnet

'''<remarks>Tested cd keys are banned or fake.</remarks>
<TestClass()>
Public Class CDKeyTest
    <TestMethod()>
    Public Sub FromWC3StyleKeyTest_ROC()
        Dim key = "EDKBRTRXG88Z9V8M84HY2XVW7N".ToWC3CDKeyCredentials()
        Assert.IsTrue(key.Product = ProductType.Warcraft3ROC)
        Assert.IsTrue(key.PublicKey = 1208212)
        Assert.IsTrue(key.AuthenticationProof(0UI.Bytes, 0UI.Bytes).HasSameItemsAs({196, 113, 19, 179, 113, 10, 105, 13, 102, 167, 215, 44, 208, 16, 1, 235, 117, 98, 60, 242}))
        Assert.IsTrue(key.AuthenticationProof(0UI.Bytes, 1UI.Bytes).HasSameItemsAs({15, 228, 202, 183, 58, 51, 189, 248, 37, 118, 4, 16, 169, 125, 61, 64, 65, 75, 117, 48}))
        Assert.IsTrue(key.AuthenticationProof(1UI.Bytes, 0UI.Bytes).HasSameItemsAs({208, 46, 134, 126, 177, 163, 48, 44, 150, 179, 249, 186, 166, 54, 69, 70, 209, 142, 122, 1}))
    End Sub
    <TestMethod()>
    Public Sub FromWC3StyleKeyTest_TFT()
        Dim key = "M68YC4278JJXXVJMKRP8ETN4TC".ToWC3CDKeyCredentials()
        Assert.IsTrue(key.Product = ProductType.Warcraft3TFT)
        Assert.IsTrue(key.PublicKey = 2818526)
        Assert.IsTrue(key.AuthenticationProof(0UI.Bytes, 0UI.Bytes).HasSameItemsAs({251, 78, 164, 23, 174, 8, 33, 230, 25, 59, 223, 85, 108, 168, 51, 6, 54, 69, 96, 24}))
        Assert.IsTrue(key.AuthenticationProof(0UI.Bytes, 1UI.Bytes).HasSameItemsAs({92, 140, 190, 130, 71, 108, 49, 229, 157, 212, 176, 177, 206, 230, 24, 129, 105, 56, 123, 236}))
        Assert.IsTrue(key.AuthenticationProof(1UI.Bytes, 0UI.Bytes).HasSameItemsAs({238, 179, 153, 15, 144, 134, 125, 31, 219, 93, 213, 50, 138, 158, 246, 138, 181, 4, 120, 66}))
    End Sub
End Class
