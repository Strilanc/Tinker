Imports Tinker.Pickling

Namespace WC3.Replay
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class ReplayEntry
        Private Shared ReadOnly EntryJar As New KeyPrefixedJar(Of ReplayEntryId)(
            keyJar:=New EnumByteJar(Of ReplayEntryId)(),
            valueJars:=Replay.Format.AllDefinitions.ToDictionary(
                keySelector:=Function(e) e.Id,
                elementSelector:=Function(e) DirectCast(e.Jar, ISimpleJar).AsNonNull))

        Private ReadOnly _id As ReplayEntryId
        Private ReadOnly _payload As ISimplePickle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(ByVal id As ReplayEntryId, ByVal payload As ISimplePickle)
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub

        Public Shared Function FromValue(Of T)(ByVal definition As Format.Definition(Of T),
                                               ByVal value As T) As ReplayEntry
            Contract.Requires(definition IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return New ReplayEntry(definition.Id, definition.Jar.Pack(value))
        End Function

        Public ReadOnly Property Id As ReplayEntryId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Payload As ISimplePickle
            Get
                Contract.Ensures(Contract.Result(Of ISimplePickle)() IsNot Nothing)
                Return _payload
            End Get
        End Property

        <ContractVerification(False)>
        Public Shared Function FromData(ByVal data As IReadableList(Of Byte)) As ReplayEntry
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Dim pickle = EntryJar.Parse(data)
            Return New ReplayEntry(pickle.Value.Key, pickle.Value.Value)
        End Function

        Public Overrides Function ToString() As String
            Return "{0}: {1}".Frmt(id, Payload.Description.Value())
        End Function
    End Class

    Public NotInheritable Class ReplayEntryJar
        Inherits BaseJar(Of ReplayEntry)

        Public Overrides Function Pack(Of TValue As ReplayEntry)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Return value.Pickled(New Byte() {value.Id}.Concat(value.Payload.Data).ToReadableList)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of ReplayEntry)
            Dim value = ReplayEntry.FromData(data)
            Dim datum = data.SubView(0, value.Payload.Data.Count + 1) 'include the id
            Return value.Pickled(datum)
        End Function
    End Class
End Namespace
