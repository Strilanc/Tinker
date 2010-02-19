Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class OrderIdJar
        Inherits EnumUInt32Jar(Of OrderId)

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name, checkDefined:=False)
        End Sub

        Protected Overrides Function ValueToString(ByVal value As OrderId) As String
            If value >= &HD0000 AndAlso value < &HE0000 Then
                If value.EnumValueIsDefined() Then
                    Return value.ToString
                Else
                    Return "0x" + CUInt(value).ToString("X5", CultureInfo.InvariantCulture)
                End If
            Else
                Return GameActions.TypeIdString(value)
            End If
        End Function
    End Class
End Namespace
