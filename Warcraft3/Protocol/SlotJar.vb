Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class SlotJar
        Inherits TupleJar

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name,
                    New ByteJar("player index").Weaken,
                    New ByteJar("dl percent").Weaken,
                    New EnumByteJar(Of SlotContents.State)("slot state").Weaken,
                    New ByteJar("is computer").Weaken,
                    New ByteJar("team index").Weaken,
                    New EnumByteJar(Of Slot.PlayerColor)("color").Weaken,
                    New EnumByteJar(Of Slot.Races)("race").Weaken,
                    New EnumByteJar(Of Slot.ComputerLevel)("computer difficulty").Weaken,
                    New ByteJar("handicap").Weaken)
        End Sub

        Public Shared Function PackSlot(ByVal slot As Slot,
                                        ByVal receiver As Player) As Dictionary(Of InvariantString, Object)
            Contract.Requires(slot IsNot Nothing)
            Contract.Requires(receiver IsNot Nothing)
            Dim pid = slot.Contents.DataPlayerIndex(receiver)
            Return New Dictionary(Of InvariantString, Object) From {
                    {"team index", slot.Team},
                    {"color", If(slot.Team = slot.ObserverTeamIndex, slot.PlayerColor.Observer, slot.color)},
                    {"race", If(slot.RaceUnlocked, slot.race Or slot.Races.Unlocked, slot.race)},
                    {"handicap", slot.handicap},
                    {"is computer", If(slot.Contents.ContentType = SlotContentType.Computer, 1, 0)},
                    {"computer difficulty", slot.Contents.DataComputerLevel},
                    {"slot state", slot.Contents.DataState(receiver)},
                    {"player index", If(pid Is Nothing, 0, pid.Value.Index)},
                    {"dl percent", slot.Contents.DataDownloadPercent(receiver)}}
        End Function
    End Class
End Namespace
