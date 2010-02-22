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
        Dim jar = New SlotJar("test")
        JarTest(jar,
                data:={1, 255, 0, 0, 1, 2, 2, 1, 100},
                value:=New Dictionary(Of InvariantString, Object) From {
                        {"pid", 1},
                        {"dl", 255},
                        {"state", SlotState.Open},
                        {"cpu", 0},
                        {"team", 1},
                        {"color", PlayerColor.Teal},
                        {"race", Races.Orc},
                        {"difficulty", ComputerLevel.Normal},
                        {"handicap", 100}
                    })

        Dim g = New WC3.Game("test", TestSettings, New ManualClock())
        Dim s1 = New Slot(index:=0, raceUnlocked:=True, color:=PlayerColor.Red, team:=0, contents:=New SlotContentsOpen)
        Dim s2 = s1.WithIndex(1).WithContents(New SlotContentsClosed)
        Dim s3 = s1.WithIndex(2).WithContents(New SlotContentsComputer(ComputerLevel.Insane))
        Dim s4 = s1.WithIndex(3).WithContents(New SlotContentsPlayer(TestPlayer))
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
