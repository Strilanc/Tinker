Imports Tinker.Pickling

Namespace WC3.Protocol
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class GameAction
        Private ReadOnly _id As GameActionId
        Private ReadOnly _payload As ISimplePickle

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public Sub New(ByVal id As GameActionId, ByVal payload As ISimplePickle)
            Contract.Requires(payload IsNot Nothing)
            Me._id = id
            Me._payload = payload
        End Sub

        Public Shared Function FromValue(Of T)(ByVal actionDefinition As GameActions.Definition(Of T),
                                               ByVal value As T) As GameAction
            Contract.Requires(actionDefinition IsNot Nothing)
            Contract.Requires(value IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Return New GameAction(actionDefinition.Id, actionDefinition.Jar.Pack(value))
        End Function

        Public ReadOnly Property Id As GameActionId
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
            Return "{0}: {1}".Frmt(Id, Payload.Description.Value())
        End Function

        Public Shared Widening Operator CType(ByVal value As GameAction) As KeyValuePair(Of GameActionId, ISimplePickle)
            Contract.Requires(value IsNot Nothing)
            Return New KeyValuePair(Of GameActionId, ISimplePickle)(value.Id, value.Payload)
        End Operator
        Public Shared Widening Operator CType(ByVal value As KeyValuePair(Of GameActionId, ISimplePickle)) As GameAction
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Return New GameAction(value.Key, value.Value)
        End Operator
    End Class

    Public NotInheritable Class GameActionJar
        Inherits BaseJar(Of GameAction)

        Private Shared ReadOnly SubJar As New KeyPrefixedJar(Of GameActionId)(
            keyJar:=New EnumByteJar(Of GameActionId)(),
            valueJars:=GameActions.AllDefinitions.ToDictionary(
                keySelector:=Function(e) e.Id,
                elementSelector:=Function(e) DirectCast(e.Jar, ISimpleJar).AsNonNull))

        Public Overrides Function Pack(Of TValue As GameAction)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickle = SubJar.Pack(CType(value, KeyValuePair(Of GameActionId, ISimplePickle)))
            Return pickle.With(jar:=Me, value:=value)
        End Function

        'verification disabled due to stupid verifier (1.2.30118.5)
        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of GameAction)
            Dim pickle = SubJar.Parse(data)
            Return pickle.With(jar:=Me, value:=CType(pickle.Value, GameAction))
        End Function

        Public Overrides Function ControlToValue(ByVal control As Control) As GameAction
            Return SubJar.ControlToValue(control)
        End Function
        Public Overrides Function ValueToControl(ByVal value As GameAction) As Control
            Return SubJar.ValueToControl(value)
        End Function
    End Class
End Namespace
