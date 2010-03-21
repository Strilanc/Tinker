Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class SlotJar
        Inherits TupleJar

        Public Sub New()
            MyBase.New(True,
                    New ByteJar().Named("pid"),
                    New ByteJar().Named("dl"),
                    New EnumByteJar(Of Protocol.SlotState)().Named("state"),
                    New ByteJar().Named("cpu"),
                    New ByteJar().Named("team"),
                    New EnumByteJar(Of Protocol.PlayerColor)().Named("color"),
                    New EnumByteJar(Of Protocol.Races)().Named("race"),
                    New EnumByteJar(Of Protocol.ComputerLevel)().Named("difficulty"),
                    New ByteJar().Named("handicap"))
        End Sub

        Public Shared Function PackSlot(ByVal slot As Slot,
                                        Optional ByVal receiver As Player = Nothing) As NamedValueMap
            Contract.Requires(slot IsNot Nothing)
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
