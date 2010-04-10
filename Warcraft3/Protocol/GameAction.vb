﻿Imports Tinker.Pickling

Namespace WC3.Protocol
    <DebuggerDisplay("{ToString}")>
    Public NotInheritable Class GameAction
        Implements IEquatable(Of GameAction)

        Private Shared ReadOnly SharedJar As New GameActionJar()
        Private ReadOnly _id As GameActionId
        Private ReadOnly _payload As Object

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_payload IsNot Nothing)
        End Sub

        Public Sub New(ByVal id As GameActionId, ByVal payload As Object)
            Contract.Requires(payload IsNot Nothing)
            Contract.Ensures(Me.Id = id)
            Contract.Ensures(Me.Payload Is payload)
            Me._id = id
            Me._payload = payload
        End Sub

        Public ReadOnly Property Id As GameActionId
            Get
                Return _id
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
            Return value.Id.KeyValue(value.Payload)
        End Operator
        Public Shared Widening Operator CType(ByVal value As KeyValuePair(Of GameActionId, Object)) As GameAction
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Contract.Assume(value.Value IsNot Nothing)
            Return New GameAction(value.Key, value.Value)
        End Operator

        Public Overloads Function Equals(ByVal other As GameAction) As Boolean Implements System.IEquatable(Of GameAction).Equals
            If other Is Nothing Then Return False
            Return SharedJar.Pack(Me).SequenceEqual(SharedJar.Pack(other))
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, GameAction))
        End Function
        Public Overrides Function GetHashCode() As Integer
            Return _id.GetHashCode()
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

        Public Overrides Function Children(ByVal data As IReadableList(Of Byte)) As IEnumerable(Of ISimpleJar)
            Return SubJar.Children(data)
        End Function
    End Class
End Namespace
