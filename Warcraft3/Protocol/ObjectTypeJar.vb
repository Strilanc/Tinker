Imports Tinker.Pickling

Namespace WC3.Protocol
    Public NotInheritable Class ObjectTypeJar
        Inherits UInt32Jar
        Protected Overrides Function ValueToString(ByVal value As UInteger) As String
            Return GameActions.TypeIdString(value)
        End Function
    End Class
End Namespace
