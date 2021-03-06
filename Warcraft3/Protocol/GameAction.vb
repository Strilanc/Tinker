﻿Imports Tinker.Pickling

Namespace WC3.Protocol
    <DebuggerDisplay("{ToString()}")>
    Public NotInheritable Class GameAction
        Implements IEquatable(Of GameAction)

        Private Shared ReadOnly SharedJar As New GameActionJar()
        Private ReadOnly _definition As GameActions.Definition
        Private ReadOnly _payload As Object

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_definition IsNot Nothing)
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Private Sub New(definition As GameActions.Definition, payload As Object)
            Contract.Requires(definition IsNot Nothing)
            Contract.Requires(payload IsNot Nothing)
            Contract.Ensures(Me.Definition Is definition)
            Contract.Ensures(Me.Payload Is payload)
            Me._definition = definition
            Me._payload = payload
        End Sub
        <CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", justification:="Helps type inference.")>
        Public Shared Function FromDefinitionAndValue(Of TPayload)(definition As GameActions.Definition(Of TPayload),
                                                                   payload As TPayload) As GameAction
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

        Public Function ToKeyValue() As IKeyValue(Of GameActionId, Object)
            Contract.Ensures(Contract.Result(Of IKeyValue(Of GameActionId, Object))() IsNot Nothing)
            Return Definition.Id.KeyValue(Payload)
        End Function
        Public Shared Function FromKeyValue(value As IKeyValue(Of GameActionId, Object)) As GameAction
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Contract.Assume(value.Value IsNot Nothing)
            Return New GameAction(GameActions.DefinitionFor(value.Key), value.Value)
        End Function

        Public Overloads Function Equals(other As GameAction) As Boolean Implements System.IEquatable(Of GameAction).Equals
            If other Is Nothing Then Return False
            If other.Definition IsNot Me.Definition Then Return False
            Return SharedJar.Pack(Me).SequenceEqual(SharedJar.Pack(other))
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
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
        Inherits BaseConversionJar(Of GameAction, IKeyValue(Of GameActionId, Object))

        Private Shared ReadOnly DataJar As New KeyPrefixedJar(Of GameActionId)(
            keyJar:=New EnumByteJar(Of GameActionId)(),
            valueJars:=GameActions.AllDefinitions.ToDictionary(
                keySelector:=Function(e) e.Id,
                elementSelector:=Function(e) e.Jar))

        Public Overrides Function SubJar() As IJar(Of IKeyValue(Of GameActionId, Object))
            Return DataJar
        End Function
        Public Overrides Function PackRaw(value As GameAction) As IKeyValue(Of GameActionId, Object)
            Return value.ToKeyValue()
        End Function
        Public Overrides Function ParseRaw(value As IKeyValue(Of GameActionId, Object)) As GameAction
            Return GameAction.FromKeyValue(value)
        End Function
    End Class
End Namespace
