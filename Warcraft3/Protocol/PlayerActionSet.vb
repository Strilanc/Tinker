Imports Tinker.Pickling

Namespace WC3.Protocol
    Public Class PlayerActionSet
        Implements IEquatable(Of PlayerActionSet)

        Private ReadOnly _id As PlayerId
        Private ReadOnly _actions As IReadableList(Of GameAction)
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_actions IsNot Nothing)
        End Sub
        Public Sub New(ByVal id As PlayerId, ByVal actions As IReadableList(Of GameAction))
            Contract.Requires(actions IsNot Nothing)
            Me._id = id
            Me._actions = actions
        End Sub
        Public ReadOnly Property Id As PlayerId
            Get
                Return _id
            End Get
        End Property
        Public ReadOnly Property Actions As IReadableList(Of GameAction)
            Get
                Contract.Ensures(Contract.Result(Of IReadableList(Of GameAction))() IsNot Nothing)
                Return _actions
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return Id.GetHashCode
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, PlayerActionSet))
        End Function
        Public Overloads Function Equals(ByVal other As PlayerActionSet) As Boolean Implements IEquatable(Of PlayerActionSet).Equals
            If other Is Nothing Then Return False
            If Me.Id <> other.Id Then Return False
            If Me.Actions.Count <> other.Actions.Count Then Return False
            If (From pair In Me.Actions.Zip(other.Actions)
                Where Not pair.Item1.Payload.Data.SequenceEqual(pair.Item2.Payload.Data)).Any Then Return False
            Return True
        End Function
    End Class
    Public Class PlayerActionSetJar
        Inherits BaseJar(Of PlayerActionSet)

        Private Shared ReadOnly DataJar As New TupleJar(
                New PlayerIdJar().Named("source"),
                New GameActionJar().Repeated.DataSizePrefixed(prefixSize:=2).Named("actions"))

        Public Overrides Function Pack(Of TValue As PlayerActionSet)(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            Dim pickle = DataJar.Pack(New NamedValueMap(New Dictionary(Of InvariantString, Object) From {
                                            {"source", value.Id},
                                            {"actions", value.Actions}}))
            Return pickle.With(jar:=Me, value:=value)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of PlayerActionSet)
            Dim pickle = DataJar.Parse(data)
            Dim id = pickle.Value.ItemAs(Of PlayerId)("source")
            Dim value = New PlayerActionSet(pickle.Value.ItemAs(Of PlayerId)("source"),
                                            pickle.Value.ItemAs(Of IReadableList(Of GameAction))("actions"))
            Return pickle.With(jar:=Me, value:=value)
        End Function

        Public Overrides Function MakeControl() As IValueEditor(Of PlayerActionSet)
            Dim subControl = DataJar.MakeControl()
            Return New DelegatedValueEditor(Of PlayerActionSet)(
                Control:=subControl.Control,
                eventAdder:=Sub(action) AddHandler subControl.ValueChanged, Sub() action(),
                getter:=Function() New PlayerActionSet(subControl.Value.ItemAs(Of PlayerId)("source"),
                                                       subControl.Value.ItemAs(Of IReadableList(Of GameAction))("actions")),
                setter:=Sub(value) subControl.Value = New Dictionary(Of InvariantString, Object) From {
                            {"source", value.Id},
                            {"actions", value.Actions}})
        End Function
    End Class
End Namespace
