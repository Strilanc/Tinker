Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class ObjectTypeJar
        Inherits UInt32Jar

        Public Sub New(ByVal name As InvariantString)
            MyBase.New(name)
        End Sub

        Protected Overrides Function ValueToString(ByVal value As UInteger) As String
            Return GameActions.TypeIdString(value)
        End Function
    End Class
End Namespace
