Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class SlotJar
        Inherits TupleJar

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name, True,
                    New ByteJar("pid").Weaken,
                    New ByteJar("dl").Weaken,
                    New EnumByteJar(Of SlotContents.State)("state").Weaken,
                    New ByteJar("cpu").Weaken,
                    New ByteJar("team").Weaken,
                    New EnumByteJar(Of Slot.PlayerColor)("color").Weaken,
                    New EnumByteJar(Of Slot.Races)("race").Weaken,
                    New EnumByteJar(Of Slot.ComputerLevel)("difficulty").Weaken,
                    New ByteJar("handicap").Weaken)
        End Sub

        Public Shared Function PackSlot(ByVal slot As Slot,
                                        ByVal receiver As Player) As Dictionary(Of InvariantString, Object)
            Contract.Requires(slot IsNot Nothing)
            Contract.Requires(receiver IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, Object))() IsNot Nothing)
            Dim pid = slot.Contents.DataPlayerIndex(receiver)
            Return New Dictionary(Of InvariantString, Object) From {
                    {"pid", If(pid Is Nothing, 0, pid.Value.Index)},
                    {"dl", slot.Contents.DataDownloadPercent(receiver)},
                    {"state", slot.Contents.DataState(receiver)},
                    {"cpu", If(slot.Contents.ContentType = SlotContentType.Computer, 1, 0)},
                    {"team", slot.Team},
                    {"color", If(slot.Team = slot.ObserverTeamIndex, slot.PlayerColor.Observer, slot.color)},
                    {"race", If(slot.RaceUnlocked, slot.race Or slot.Races.Unlocked, slot.race)},
                    {"difficulty", slot.Contents.DataComputerLevel},
                    {"handicap", slot.handicap}}
        End Function
    End Class
End Namespace
