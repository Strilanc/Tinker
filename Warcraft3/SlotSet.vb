Namespace WC3
    Public NotInheritable Class SlotSet
        Implements IEnumerable(Of Slot)
        Private ReadOnly _slots As IRist(Of Slot)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_slots IsNot Nothing)
        End Sub

        Public Sub New(ByVal slots As IEnumerable(Of Slot))
            Contract.Requires(slots IsNot Nothing)
            Me._slots = slots.ToReadableList
        End Sub

        Public ReadOnly Property Slots As IRist(Of Slot)
            Get
                Contract.Ensures(Contract.Result(Of IRist(Of Slot))() IsNot Nothing)
                Return _slots
            End Get
        End Property

        '''<summary>Creates a SlotSet with the same slots, except any slot matching a replacement slot (by index) is replaced by the new slot.</summary>
        Public Function WithSlotsUpdatedByIndex(ByVal ParamArray replacementSlots() As Slot) As SlotSet
            Contract.Requires(replacementSlots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)
            Return Me.WithSlotsReplaced(replacementSlots.AsEnumerable)
        End Function
        '''<summary>Creates a SlotSet with the same slots, except any slot matching a replacement slot (by index) is replaced by the new slot.</summary>
        Public Function WithSlotsReplaced(ByVal replacementSlots As IEnumerable(Of Slot)) As SlotSet
            Contract.Requires(replacementSlots IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)
            Return New SlotSet(From oldSlot In _slots
                               Select (From slot In replacementSlots
                                       Where slot.Index = oldSlot.Index
                                       ).FirstOrDefault([Default]:=oldSlot))
        End Function

        '''<summary>Returns any slot matching a string. Checks index, color and player name.</summary>
        <Pure()>
        <ContractVerification(False)>
        Public Function FindMatchingSlot(ByVal query As InvariantString) As Slot
            Dim best = (From slot In _slots
                        Let match = slot.Matches(query)
                        Let contentType = slot.Contents.ContentType
                        ).MaxRelativeTo(Function(item) item.match * 3 - item.contentType)
            Contract.Assume(best IsNot Nothing)
            If best.match = Slot.Match.None Then Throw New OperationFailedException("No matching slot found.")
            Return best.slot
        End Function

        Public Function WithEncodeHCL(ByVal settings As GameSettings) As SlotSet
            Contract.Requires(settings IsNot Nothing)
            Contract.Ensures(Contract.Result(Of SlotSet)() IsNot Nothing)
            Dim useableSlots = From slot In _slots
                               Where slot.Contents.Moveable
                               Where slot.Contents.ContentType <> SlotContents.Type.Empty
            Dim encodedHandicaps = settings.EncodedHCLMode(From slot In useableSlots Select slot.Handicap)
            Return Me.WithSlotsReplaced(From pair In useableSlots.Zip(encodedHandicaps)
                                        Let slot = pair.Item1
                                        Let handicap = pair.Item2
                                        Select slot.With(handicap:=handicap))
        End Function

        <Pure()>
        Public Function TryFindPlayerSlot(ByVal player As Player) As Slot?
            Contract.Requires(player IsNot Nothing)
            Return (From slot In _slots
                    From resident In slot.Contents.EnumPlayers
                    Where player Is resident
                    Select slot
                    ).FirstOrNullableDefault
        End Function
        <Pure()>
        Public Function FindPlayerSlot(ByVal player As Player) As Slot
            Contract.Requires(player IsNot Nothing)
            Dim result = TryFindPlayerSlot(player)
            If Not result.HasValue Then Throw New InvalidOperationException("No such player in a slot.")
            Return result.Value
        End Function

        Public Function GetEnumerator() As IEnumerator(Of Slot) Implements IEnumerable(Of Slot).GetEnumerator
            Return _slots.GetEnumerator
        End Function
        Private Function GetEnumeratorObj() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return _slots.GetEnumerator
        End Function
    End Class
End Namespace
