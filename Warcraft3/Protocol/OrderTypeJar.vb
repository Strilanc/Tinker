Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class OrderTypeJar
        Inherits EnumUInt32Jar(Of OrderId)

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name, checkDefined:=False)
        End Sub

        Protected Overrides Function ValueToString(ByVal value As OrderId) As String
            If value >= &HD0000 AndAlso value < &HE0000 Then
                Return MyBase.ValueToString(value)
            Else
                Return GameActions.TypeIdString(value)
            End If
        End Function
    End Class
End Namespace
