Imports Strilbrary.Collections
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Tinker
Imports Strilbrary.Values

<TestClass()>
Public Class GameStatsTest
    <TestMethod()>
    Public Sub JarTest_TypicalDota()
        Dim testData As Byte() = New Byte() {
                                   &H1, &H3, &H49, &H7, &H1, &H1, &H77, &H1, &H89, &H79, &H1, &HCB, &H31, &H57, &H17, &H4D,
                                   &HCB, &H61, &H71, &H73, &H5D, &H45, &H6F, &H77, &H19, &H6F, &H6D, &H6F, &H61, &H65, &H5D,
                                   &H45, &H2B, &H6F, &H75, &H41, &H21, &H41, &H6D, &H6D, &H2B, &H73, &H75, &H61, &H73, &H73,
                                   &H21, &H77, &HC1, &H37, &H2F, &H37, &H35, &H2F, &H77, &H33, &HD9, &H79, &H1, &H4D, &H61,
                                   &H65, &H65, &H69, &HF5, &H75, &H6F, &H6F, &H63, &H65, &H61, &H67, &HC7, &H61, &H69, &H6F,
                                   &H1, &H1, &HF3, &H35, &HC9, &H89, &H1F, &H71, &HD5, &HC9, &H41, &H4D, &H3B, &H29, &H43,
                                   &H39, &H6F, &H6B, &H59, &HAF, &H17, &HA3, &HCD, &H9B, &H6F, &H0}
        Dim jar = New WC3.Protocol.GameStatsJar()

        Dim stats = jar.Parse(testData.AsReadableList).Value
        Assert.IsTrue(Not stats.randomHero)
        Assert.IsTrue(Not stats.randomRace)
        Assert.IsTrue(Not stats.allowFullSharedControl)
        Assert.IsTrue(stats.lockTeams)
        Assert.IsTrue(stats.teamsTogether)
        Assert.IsTrue(stats.observers = WC3.GameObserverOption.NoObservers)
        Assert.IsTrue(stats.visibility = WC3.GameVisibilityOption.MapDefault)
        Assert.IsTrue(stats.speed = WC3.GameSpeedOption.Fast)
        Assert.IsTrue(stats.PlayableWidth = 120)
        Assert.IsTrue(stats.PlayableHeight = 118)
        Assert.IsTrue(stats.mapChecksumXORO = 374747339)
        Assert.IsTrue(stats.MapChecksumSHA1.SequenceEqual({&HF3, &H35, &H88, &H1E, &H71, &HD4, &HC8, &H41, &H4D, &H29, &H42, &H39, &H6F, &H6B, &H58, &HAE, &HA3, &HCD, &H9A, &H6F}))
        Assert.IsTrue(stats.AdvertisedPath = "Maps\Download\DotA Allstars v6.64.w3x")
        Assert.IsTrue(stats.HostName = "Madeitonceagain")

        'Cycle back
        Dim weirdPos = testData.Length.FloorMultiple(8) 'the last block has undefined bits in the header
        Assert.IsTrue(jar.Pack(stats).Data.Take(weirdPos).SequenceEqual(testData.Take(weirdPos)))
        Assert.IsTrue(jar.Pack(stats).Data.Skip(weirdPos + 1).SequenceEqual(testData.Skip(weirdPos + 1)))
    End Sub
End Class
