Imports Tinker.Pickling

Namespace WC3.Replay
    <DebuggerDisplay("{ToString()}")>
    Public NotInheritable Class ReplayEntry
        Implements IEquatable(Of ReplayEntry)

        Private Shared ReadOnly SharedJar As New ReplayEntryJar()
        Private ReadOnly _definition As Format.Definition
        Private ReadOnly _payload As Object

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
            Contract.Invariant(_definition IsNot Nothing)
        End Sub

        Private Sub New(ByVal definition As Format.Definition, ByVal payload As Object)
            Contract.Requires(definition IsNot Nothing)
            Contract.Requires(payload IsNot Nothing)
            Contract.Ensures(Me.Definition Is definition)
            Contract.Ensures(Me.Payload Is payload)
            Me._definition = definition
            Me._payload = payload
        End Sub
        <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", justification:="Helps type inference.")>
        Public Shared Function FromDefinitionAndValue(Of TPayload)(ByVal definition As Format.Definition(Of TPayload),
                                                                   ByVal payload As TPayload) As ReplayEntry
            Contract.Requires(definition IsNot Nothing)
            Contract.Requires(payload IsNot Nothing)
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Return New ReplayEntry(definition, payload)
        End Function

        Public ReadOnly Property Definition As Format.Definition
            Get
                Contract.Ensures(Contract.Result(Of Format.Definition)() IsNot Nothing)
                Return _definition
            End Get
        End Property
        Public ReadOnly Property Payload As Object
            Get
                Contract.Ensures(Contract.Result(Of Object)() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Public Shared Widening Operator CType(ByVal value As ReplayEntry) As KeyValuePair(Of ReplayEntryId, Object)
            Contract.Requires(value IsNot Nothing)
            Return value.Definition.Id.KeyValue(value.Payload)
        End Operator
        Public Shared Widening Operator CType(ByVal value As KeyValuePair(Of ReplayEntryId, Object)) As ReplayEntry
            Contract.Ensures(Contract.Result(Of ReplayEntry)() IsNot Nothing)
            Contract.Assume(value.Value IsNot Nothing)
            Return New ReplayEntry(Format.DefinitionFor(value.Key), value.Value)
        End Operator

        Public Overloads Function Equals(ByVal other As ReplayEntry) As Boolean Implements System.IEquatable(Of ReplayEntry).Equals
            If other Is Nothing Then Return False
            Return SharedJar.Pack(Me).SequenceEqual(SharedJar.Pack(other))
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, ReplayEntry))
        End Function
        Public Overrides Function GetHashCode() As Integer
            Return _definition.Id.GetHashCode()
        End Function
        Public Overrides Function ToString() As String
            Return SharedJar.Describe(Me)
        End Function
    End Class

    Public NotInheritable Class ReplayEntryJar
        Inherits BaseConversionJar(Of ReplayEntry, KeyValuePair(Of ReplayEntryId, Object))

        Private Shared ReadOnly DataJar As New KeyPrefixedJar(Of ReplayEntryId)(
            keyJar:=New EnumByteJar(Of ReplayEntryId)(),
            valueJars:=Replay.Format.AllDefinitions.ToDictionary(
                keySelector:=Function(e) e.Id,
                elementSelector:=Function(e) e.Jar))

        Public Overrides Function SubJar() As IJar(Of KeyValuePair(Of ReplayEntryId, Object))
            Return DataJar
        End Function
        <ContractVerification(False)>
        Public Overrides Function PackRaw(ByVal value As ReplayEntry) As KeyValuePair(Of ReplayEntryId, Object)
            Return value
        End Function
        Public Overrides Function ParseRaw(ByVal value As KeyValuePair(Of ReplayEntryId, Object)) As ReplayEntry
            Return value
        End Function
    End Class
End Namespace
