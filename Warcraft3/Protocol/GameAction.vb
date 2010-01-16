Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class GameAction
        Public ReadOnly id As W3GameActionId
        Private ReadOnly _payload As IPickle(Of Object)

        Private Sub New(ByVal payload As IPickle(Of PrefixPickle(Of W3GameActionId)))
            Contract.Requires(payload IsNot Nothing)
            Me._payload = payload.Value.Payload
            Me.id = payload.Value.Key
        End Sub

        Public ReadOnly Property Payload As IPickle(Of Object)
            Get
                Contract.Ensures(Contract.Result(Of IPickle(Of Object))() IsNot Nothing)
                Return _payload
            End Get
        End Property

        Public Shared Function FromData(ByVal data As IReadableList(Of Byte)) As GameAction
            Contract.Requires(data IsNot Nothing)
            Contract.Ensures(Contract.Result(Of GameAction)() IsNot Nothing)
            Return New GameAction(GameActions.packetJar.Parse(data))
        End Function

        Public Overrides Function ToString() As String
            Return "{0} = {1}".Frmt(id, Payload.Description.Value())
        End Function
    End Class

    Public NotInheritable Class W3GameActionJar
        Inherits BaseJar(Of GameAction)
        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Public Overrides Function Pack(Of TValue As GameAction)(ByVal value As TValue) As Pickling.IPickle(Of TValue)
            Return New Pickle(Of TValue)(Name, value, Concat({value.id}, value.Payload.Data.ToArray).AsReadableList)
        End Function

        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As Pickling.IPickle(Of GameAction)
            Dim val = GameAction.FromData(data)
            Dim n = val.Payload.Data.Count
            Return New Pickle(Of GameAction)(Name, val, data.SubView(0, n + 1))
        End Function
    End Class
End Namespace
