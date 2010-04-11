Imports Tinker.Pickling

Namespace WC3.Protocol
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class GameAction
        Implements IEquatable(Of GameAction)

        Private Shared ReadOnly SharedJar As New GameActionJar()
        Private ReadOnly _definition As GameActions.Definition
        Private ReadOnly _payload As Object

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_definition IsNot Nothing)
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(ByVal definition As GameActions.Definition, ByVal payload As Object)
            Contract.Requires(payload IsNot Nothing)
            Contract.Ensures(Me.Definition Is definition)
            Contract.Ensures(Me.Payload Is payload)
            Me._definition = definition
            Me._payload = payload
        End Sub
        Public Shared Function FromDefinitionAndValue(Of TPayload)(ByVal definition As GameActions.Definition(Of TPayload),
                                                                   ByVal payload As TPayload) As GameAction
            Contract.Requires(definition IsNot Nothing)
            Contract.Requires(payload IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Return New GameAction(definition, payload)
        End Function

        Public ReadOnly Property Definition As GameActions.Definition
            Get
                Return _definition
            End Get
        End Property
        Public ReadOnly Property Payload As Object
            Get
                Contract.Ensures(Contract.Result(Of Object)() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Public Shared Widening Operator CType(ByVal value As GameAction) As KeyValuePair(Of GameActionId, Object)
            Contract.Requires(value IsNot Nothing)
            Return value.Definition.Id.KeyValue(value.Payload)
        End Operator
        Public Shared Widening Operator CType(ByVal value As KeyValuePair(Of GameActionId, Object)) As GameAction
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Contract.Assume(value.Value IsNot Nothing)
            Return New GameAction(GameActions.DefinitionFor(value.Key), value.Value)
        End Operator

        Public Overloads Function Equals(ByVal other As GameAction) As Boolean Implements System.IEquatable(Of GameAction).Equals
            If other Is Nothing Then Return False
            If other.Definition IsNot Me.Definition Then Return False
            Return SharedJar.Pack(Me).SequenceEqual(SharedJar.Pack(other))
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, GameAction))
        End Function
        Public Overrides Function GetHashCode() As Integer
            Return _definition.Id.GetHashCode()
        End Function
        Public Overrides Function ToString() As String
            Return SharedJar.Describe(Me)
        End Function
    End Class

    Public NotInheritable Class GameActionJar
        Inherits BaseJar(Of GameAction)

        Private Shared ReadOnly SubJar As New KeyPrefixedJar(Of GameActionId)(
            keyJar:=New EnumByteJar(Of GameActionId)(),
            valueJars:=GameActions.AllDefinitions.ToDictionary(
                keySelector:=Function(e) e.Id,
                elementSelector:=Function(e) e.Jar))

        <ContractVerification(False)>
        Public Overrides Function Pack(ByVal value As GameAction) As IEnumerable(Of Byte)
            Return SubJar.Pack(value)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As ParsedValue(Of GameAction)
            Dim parsed = SubJar.Parse(data)
            Return parsed.WithValue(CType(parsed.Value, GameAction))
        End Function

        <ContractVerification(False)>
        Public Overrides Function Describe(ByVal value As GameAction) As String
            Return SubJar.Describe(value)
        End Function
        Public Overrides Function Parse(ByVal text As String) As GameAction
            Return SubJar.Parse(text)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of GameAction)
            Dim subControl = SubJar.MakeControl()
            Return New DelegatedValueEditor(Of GameAction)(
                Control:=subControl.Control,
                eventAdder:=Sub(action) AddHandler subControl.ValueChanged, Sub() action(),
                getter:=Function() subControl.Value,
                setter:=Sub(value) subControl.Value = value,
                disposer:=Sub() subControl.Dispose())
        End Function
    End Class
End Namespace
