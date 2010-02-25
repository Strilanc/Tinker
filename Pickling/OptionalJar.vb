Namespace Pickling
    Public NotInheritable Class OptionalJar(Of T)
        Inherits BaseAnonymousJar(Of Tuple(Of Boolean, T))

        Private ReadOnly _subJar As IAnonymousJar(Of T)

        <ContractInvariantMethod()> Private Sub ObjectInvariant()
            Contract.Invariant(_subJar IsNot Nothing)
        End Sub

        Public Sub New(ByVal subJar As IAnonymousJar(Of T))
            Contract.Requires(subJar IsNot Nothing)
            Me._subJar = subJar
        End Sub

        Public Overrides Function Pack(Of TValue As Tuple(Of Boolean, T))(ByVal value As TValue) As IPickle(Of TValue)
            Contract.Assume(value IsNot Nothing)
            If value.Item1 Then
                Contract.Assume(value.Item2 IsNot Nothing)
                Dim pickle = _subJar.Pack(value.Item2)
                Return New Pickle(Of TValue)(value, pickle.Data, pickle.Description)
            Else
                Return New Pickle(Of TValue)(value, New Byte() {}.AsReadableList, Function() "[Not Included]")
            End If
        End Function

        <ContractVerification(False)>
        Public Overrides Function Parse(ByVal data As IReadableList(Of Byte)) As IPickle(Of Tuple(Of Boolean, T))
            If data.Count > 0 Then
                Dim pickle = _subJar.Parse(data)
                Return New Pickle(Of Tuple(Of Boolean, T))(Tuple(True, pickle.Value), pickle.Data, pickle.Description)
            Else
                Return New Pickle(Of Tuple(Of Boolean, T))(Tuple(False, CType(Nothing, T)), data, Function() "[Not Included]")
            End If
        End Function
    End Class
End Namespace
