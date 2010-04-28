Imports Strilbrary.Collections
Imports Strilbrary.Time
Imports Strilbrary.Values
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports Tinker.WC3
Imports Tinker
Imports Tinker.Pickling
Imports Tinker.WC3.Protocol

<TestClass()>
Public Class WC3ProtocolJarsTest
    <TestMethod()>
    Public Sub SlotJarTest()
        Dim jar = New SlotJar()
        JarTest(jar,
                data:={1, 255, 0, 0, 1, 2, 2, 1, 100},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"pid", CByte(1)},
                        {"dl", CByte(255)},
                        {"state", SlotState.Open},
                        {"cpu", CByte(0)},
                        {"team", CByte(1)},
                        {"color", PlayerColor.Teal},
                        {"race", Races.Orc},
                        {"difficulty", ComputerLevel.Normal},
                        {"handicap", CByte(100)}
                    })

        Dim g = WC3.Game.FromSettings(TestSettings, "test", New ManualClock(), New Logger)
        g.Start()
        Dim s1 = New Slot(index:=0, raceUnlocked:=True, color:=PlayerColor.Red, team:=0, contents:=New SlotContentsOpen)
        Dim s2 = s1.With(index:=1, contents:=New SlotContentsClosed)
        Dim s3 = s1.With(index:=2, contents:=New SlotContentsComputer(ComputerLevel.Insane))
        Dim s4 = s1.With(index:=3, contents:=New SlotContentsPlayer(TestPlayer))
        JarTest(jar,
                value:=SlotJar.PackSlot(s1),
                data:={0, 255, 0, 0, 0, 0, 96, 1, 100})
        JarTest(jar,
                value:=SlotJar.PackSlot(s2),
                data:={0, 255, 1, 0, 0, 0, 96, 1, 100})
        JarTest(jar,
                value:=SlotJar.PackSlot(s3),
                data:={0, 255, 2, 1, 0, 0, 96, 2, 100})
        JarTest(jar,
                value:=SlotJar.PackSlot(s4),
                data:={1, 254, 2, 0, 0, 0, 96, 1, 100})
    End Sub
End Class
