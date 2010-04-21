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
                Where Not pair.Item1.Equals(pair.Item2)).Any Then Return False
            Return True
        End Function
    End Class
    Public Class SpecificPlayerActionSet
        Inherits PlayerActionSet
        Implements IEquatable(Of SpecificPlayerActionSet)

        Private ReadOnly _player As Player
        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_player IsNot Nothing)
        End Sub
        Public Sub New(ByVal player As Player, ByVal actions As IReadableList(Of GameAction))
            MyBase.New(player.Id, actions)
            Contract.Requires(player IsNot Nothing)
            Contract.Requires(actions IsNot Nothing)
            Me._player = player
        End Sub
        Public ReadOnly Property Player As Player
            Get
                Contract.Ensures(Contract.Result(Of Player)() IsNot Nothing)
                Return _player
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return _player.GetHashCode
        End Function
        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, SpecificPlayerActionSet))
        End Function
        Public Overloads Function Equals(ByVal other As SpecificPlayerActionSet) As Boolean Implements IEquatable(Of SpecificPlayerActionSet).Equals
            Return MyBase.Equals(DirectCast(other, PlayerActionSet)) AndAlso Me.Player Is other.Player
        End Function
    End Class
    Public Class PlayerActionSetJar
        Inherits BaseConversionJar(Of PlayerActionSet, NamedValueMap)

        Private Shared ReadOnly DataJar As New TupleJar(
                New PlayerIdJar().Named("source"),
                New GameActionJar().Repeated.DataSizePrefixed(prefixSize:=2).Named("actions"))

        Public Overrides Function SubJar() As IJar(Of NamedValueMap)
            Return DataJar
        End Function
        Public Overrides Function PackRaw(ByVal value As PlayerActionSet) As NamedValueMap
            Return New Dictionary(Of InvariantString, Object) From {
                    {"source", value.Id},
                    {"actions", value.Actions}}
        End Function
        Public Overrides Function ParseRaw(ByVal value As NamedValueMap) As PlayerActionSet
            Return New PlayerActionSet(value.ItemAs(Of PlayerId)("source"),
                                       value.ItemAs(Of IReadableList(Of GameAction))("actions"))
        End Function
    End Class
End Namespace
