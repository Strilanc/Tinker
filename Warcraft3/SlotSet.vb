Namespace WC3
    Public NotInheritable Class SlotSet
        Private ReadOnly _slots As IReadableList(Of Slot)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_slots IsNot Nothing)
        End Sub

        Public Sub New(ByVal slots As IEnumerable(Of Slot))
            Contract.Requires(slots IsNot Nothing)
            Me._slots = slots.ToReadableList
        End Sub

        Public ReadOnly Property Slots As IReadableList(Of Slot)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of Slot))() IsNot Nothing)
                Return _slots
            End Get
        End Property

        Public Function WithSlotsReplaced(ByVal ParamArray newSlots() As Slot) As SlotSet
            Contract.Requires(newSlots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)
            Return Me.WithSlotsReplaced(newSlots.AsEnumerable)
        End Function
        Public Function WithSlotsReplaced(ByVal newSlots As IEnumerable(Of Slot)) As SlotSet
            Contract.Requires(newSlots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)
            Return New SlotSet(From oldSlot In Slots
                               Let newSlot = (From slot In newSlots Where slot.Index = oldSlot.Index).FirstOrDefault
                               Select If(newSlot, oldSlot))
        End Function

        '''<summary>Returns any slot matching a string. Checks index, color and player name.</summary>
        <Pure()>
        Public Function FindMatchingSlot(ByVal query As InvariantString) As Slot
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Dim best = (From slot In _slots
                        Let match = slot.Matches(query)
                        Let contentType = slot.Contents.ContentType
                        ).MaxProjection(Function(item) item.match * 3 - item.contentType).Item1
            If best.match = Slot.Match.None Then Throw New OperationFailedException("No matching slot found.")
            Return best.slot
        End Function

        Public Function WithEncodeHCL(ByVal settings As GameSettings) As SlotSet
            Contract.Requires(settings IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)
            Dim useableSlots = (From slot In _slots
                                Where slot.Contents.Moveable
                                Where slot.Contents.ContentType <> SlotContents.Type.Empty
                                ).ToArray
            Dim encodedHandicaps = settings.EncodedHCLMode(From slot In useableSlots Select slot.Handicap)
            Return Me.WithSlotsReplaced(Enumerable.Zip(useableSlots,
                                                       encodedHandicaps,
                                                       Function(slot, handicap) slot.WithHandicap(handicap)))
        End Function

        <Pure()>
        Public Function TryFindPlayerSlot(ByVal player As Player) As Slot
            Contract.Requires(player IsNot Nothing)
            Return (From slot In _slots
                    From resident In slot.Contents.EnumPlayers
                    Where player Is resident
                    Select slot).FirstOrDefault
        End Function
        <Pure()>
        Public Function FindPlayerSlot(ByVal player As Player) As Slot
            Contract.Requires(player IsNot Nothing)
            Contract.Ensures(Contract.Result(Of Slot)() IsNot Nothing)
            Dim result = TryFindPlayerSlot(player)
            If result Is Nothing Then Throw New InvalidOperationException("No such player in a slot.")
            Return result
        End Function
    End Class
End Namespace
