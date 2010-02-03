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
                        {"state", SlotContents.State.Open},
                        {"cpu", 0},
                        {"team", 1},
                        {"color", Slot.PlayerColor.Teal},
                        {"race", Slot.Races.Orc},
                        {"difficulty", Slot.ComputerLevel.Normal},
                        {"handicap", 100}
                    })

        Dim g = New WC3.Game("test", TestSettings, New ManualClock())
        Dim s1 = New WC3.Slot(1, True) : s1.Contents = New WC3.SlotContentsOpen(s1)
        Dim s2 = New WC3.Slot(2, True) : s2.Contents = New WC3.SlotContentsClosed(s2)
        Dim s3 = New WC3.Slot(3, True) : s3.Contents = New WC3.SlotContentsComputer(s3, Slot.ComputerLevel.Insane)
        Dim s4 = New WC3.Slot(4, True) : s4.Contents = New WC3.SlotContentsPlayer(s4, TestPlayer)
        JarTest(jar,
                value:=SlotJar.PackSlot(s1, Nothing),
                data:={0, 255, 0, 0, 0, 0, 96, 1, 100})
        JarTest(jar,
                value:=SlotJar.PackSlot(s2, Nothing),
                data:={0, 255, 1, 0, 0, 0, 96, 1, 100})
        JarTest(jar,
                value:=SlotJar.PackSlot(s3, Nothing),
                data:={0, 255, 2, 1, 0, 0, 96, 2, 100})
        JarTest(jar,
                value:=SlotJar.PackSlot(s4, Nothing),
                data:={1, 254, 2, 0, 0, 0, 96, 1, 100})
    End Sub
End Class
