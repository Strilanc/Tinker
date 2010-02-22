Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class SlotJar
        Inherits TupleJar

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name, True,
                    New ByteJar("pid").Weaken,
                    New ByteJar("dl").Weaken,
                    New EnumByteJar(Of Protocol.SlotState)("state").Weaken,
                    New ByteJar("cpu").Weaken,
                    New ByteJar("team").Weaken,
                    New EnumByteJar(Of Protocol.PlayerColor)("color").Weaken,
                    New EnumByteJar(Of Protocol.Races)("race").Weaken,
                    New EnumByteJar(Of Protocol.ComputerLevel)("difficulty").Weaken,
                    New ByteJar("handicap").Weaken)
        End Sub

        Public Shared Function PackSlot(ByVal slot As Slot,
                                        Optional ByVal receiver As Player = Nothing) As Dictionary(Of InvariantString, Object)
            Contract.Requires(slot IsNot Nothing)
            Contract.Requires(receiver IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Dictionary(Of InvariantString, Object))() IsNot Nothing)
            Dim pid = slot.Contents.DataPlayerIndex(receiver)
            Return New Dictionary(Of InvariantString, Object) From {
                    {"pid", If(pid Is Nothing, 0, pid.Value.Index)},
                    {"dl", slot.Contents.DataDownloadPercent(receiver)},
                    {"state", slot.Contents.DataState(receiver)},
                    {"cpu", If(slot.Contents.ContentType = SlotContents.Type.Computer, 1, 0)},
                    {"team", slot.Team},
                    {"color", If(slot.Team = slot.ObserverTeamIndex, Protocol.PlayerColor.Observer, slot.Color)},
                    {"race", If(slot.RaceUnlocked, slot.Race Or Protocol.Races.Unlocked, slot.Race)},
                    {"difficulty", slot.Contents.DataComputerLevel},
                    {"handicap", slot.Handicap}}
        End Function
    End Class
End Namespace
