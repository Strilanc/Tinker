Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class SlotJar
        Inherits TupleJar

        Public Sub New()
            MyBase.New({New ByteJar().Named("pid").Weaken(),
                        New ByteJar().Named("dl").Weaken(),
                        New EnumByteJar(Of Protocol.SlotState)().Named("state").Weaken(),
                        New ByteJar().Named("cpu").Weaken(),
                        New ByteJar().Named("team").Weaken(),
                        New EnumByteJar(Of Protocol.PlayerColor)().Named("color").Weaken(),
                        New EnumByteJar(Of Protocol.Races)().Named("race").Weaken(),
                        New EnumByteJar(Of Protocol.ComputerLevel)().Named("difficulty").Weaken(),
                        New ByteJar().Named("handicap").Weaken()},
                       useSingleLineDescription:=True)
        End Sub

        Public Shared Function PackSlot(slot As Slot,
                                        Optional receiver As Player = Nothing) As NamedValueMap
            Contract.Requires(receiver IsNot Nothing)
            Contract.Ensures(Contract.Result(Of NamedValueMap)() IsNot Nothing)
            Dim pid = slot.Contents.DataPlayerIndex(receiver)
            Return New Dictionary(Of InvariantString, Object) From {
                    {"pid", If(pid Is Nothing, CByte(0), pid.Value.Index)},
                    {"dl", slot.Contents.DataDownloadPercent(receiver)},
                    {"state", slot.Contents.DataState(receiver)},
                    {"cpu", If(slot.Contents.ContentType = SlotContents.Type.Computer, CByte(1), CByte(0))},
                    {"team", slot.Team},
                    {"color", If(slot.Team = slot.ObserverTeamIndex, Protocol.PlayerColor.Observer, slot.Color)},
                    {"race", If(slot.RaceUnlocked, slot.Race Or Protocol.Races.Unlocked, slot.Race)},
                    {"difficulty", slot.Contents.DataComputerLevel},
                    {"handicap", slot.Handicap}}
        End Function
    End Class
End Namespace
