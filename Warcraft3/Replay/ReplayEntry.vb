Imports Tinker.Pickling

Namespace WC3.Replay
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class ReplayEntry
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

        Public Overrides Function ToString() As String
            Return "{0}: {1}".Frmt(Id, Payload.Description)
        End Function

        Public Shared Widening Operator CType(ByVal value As ReplayEntry) As KeyValuePair(Of ReplayEntryId, ISimplePickle)
            Contract.Requires(value IsNot Nothing)
            Return New KeyValuePair(Of ReplayEntryId, ISimplePickle)(value.Id, value.Payload)
        End Operator
        Public Shared Widening Operator CType(ByVal value As KeyValuePair(Of ReplayEntryId, ISimplePickle)) As ReplayEntry
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return New ReplayEntry(value.Key, value.Value)
        End Operator
    End Class

    Public NotInheritable Class ReplayEntryJar
        Inherits BaseJar(Of ReplayEntry)

        Private Shared ReadOnly SubJar As New KeyPrefixedJar(Of ReplayEntryId)(
            keyJar:=New EnumByteJar(Of ReplayEntryId)(),
            valueJars:=Replay.Format.AllDefinitions.ToDictionary(
                keySelector:=Function(e) e.Id,
                elementSelector:=Function(e) DirectCast(e.Jar, ISimpleJar).AsNonNull))

        Public Overrides Function Pack(Of TValue As ReplayEntry)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickle = SubJar.Pack(CType(value, KeyValuePair(Of ReplayEntryId, ISimplePickle)))
            Return pickle.With(jar:=Me, value:=value)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of ReplayEntry)
            Dim pickle = SubJar.Parse(data)
            Return pickle.With(jar:=Me, value:=CType(pickle.Value, ReplayEntry))
        End Function

        Public Overrides Function Describe(ByVal value As ReplayEntry) As String
            Return SubJar.Describe(value)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of ReplayEntry)
            Dim subControl = SubJar.MakeControl()
            Return New DelegatedValueEditor(Of ReplayEntry)(
                Control:=subControl.Control,
                eventAdder:=Sub(action) AddHandler subControl.ValueChanged, Sub() action(),
                getter:=Function() subControl.Value,
                setter:=Sub(value) subControl.Value = value)
        End Function
    End Class
End Namespace
