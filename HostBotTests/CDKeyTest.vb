Imports Strilbrary
Imports Strilbrary.Streams
Imports Strilbrary.Threading
Imports Strilbrary.Enumeration
Imports Strilbrary.Numerics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports HostBot

'''<remarks>Tested cd keys are banned or fake.</remarks>
<TestClass()>
Public Class CDKeyTest
    <TestMethod()>
    Public Sub FromWC3StyleKeyTest_ROC()
        Dim key = Bnet.CDKey.FromWC3StyleKey("EDKBRTRXG88Z9V8M84HY2XVW7N")
        Assert.IsTrue(key.Product = Bnet.CDKeyProduct.Warcraft3ROC)
        Assert.IsTrue(key.PublicKey = 1208212)
        Assert.IsTrue(key.PrivateKey.HasSameItemsAs({125, 196, 155, 236, 116, 236, 234, 59, 147, 117}))
    End Sub
    <TestMethod()>
    Public Sub FromWC3StyleKeyTest_TFT()
        Dim key = Bnet.CDKey.FromWC3StyleKey("M68YC4278JJXXVJMKRP8ETN4TC")
        Assert.IsTrue(key.Product = Bnet.CDKeyProduct.Warcraft3TFT)
        Assert.IsTrue(key.PublicKey = 2818526)
        Assert.IsTrue(key.PrivateKey.HasSameItemsAs({46, 106, 145, 205, 32, 140, 239, 159, 236, 62}))
    End Sub

    <TestMethod()>
    Public Sub FromWC3StyleKeyTest_KeyCycle()
        Dim key1 = Bnet.CDKey.FromWC3StyleKey("EDKBRTRXG88Z9V8M84HY2XVW7N")
        Dim key2 = Bnet.CDKey.ToWC3StyleKey(key1.Product, key1.PublicKey, key1.PrivateKey.ToArray)
        Assert.IsTrue(key2.Key = "EDKBRTRXG88Z9V8M84HY2XVW7N")
    End Sub
    <TestMethod()>
    Public Sub FromWC3StyleKeyTest_DataCycle()
        Dim key1 = Bnet.CDKey.ToWC3StyleKey(Bnet.CDKeyProduct.Warcraft3TFT, 24, New Byte() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10})
        Dim key2 = Bnet.CDKey.FromWC3StyleKey(key1.Key)
        Assert.IsTrue(key2.Product = Bnet.CDKeyProduct.Warcraft3TFT)
        Assert.IsTrue(key2.PublicKey = 24)
        Assert.IsTrue(key2.PrivateKey.HasSameItemsAs({1, 2, 3, 4, 5, 6, 7, 8, 9, 10}))
    End Sub
End Class
